using System.Collections.Immutable;
using System.Globalization;

namespace AIUsage.Core.Domain;

public static class QuotaWindowNormalizer
{
    public static QuotaWindowNormalization Normalize(
        IEnumerable<QuotaWindowNormalizationInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var inputCopy = inputs.ToImmutableArray();
        var windows = inputCopy.Select(NormalizeWindow).ToImmutableArray();
        var remainingPercent = windows
            .Where(window => window.RemainingPercent is not null)
            .Select(window => window.RemainingPercent!.Value)
            .Cast<double?>()
            .Min();

        return new QuotaWindowNormalization(
            windows,
            remainingPercent,
            ResolveState(remainingPercent),
            inputCopy.Select(input => input.Window).ToImmutableArray());
    }

    private static NormalizedQuotaWindow NormalizeWindow(QuotaWindowNormalizationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Interpretation switch
        {
            QuotaWindowInterpretation.Percent => NormalizePercent(input.Label, input.Window),
            QuotaWindowInterpretation.Entitlement => NormalizeEntitlement(input.Label, input.Window),
            QuotaWindowInterpretation.Quota => NormalizeQuota(input.Label, input.Window),
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }

    private static NormalizedQuotaWindow NormalizePercent(string label, RawQuotaWindow window)
    {
        var remainingPercent = window.RemainingPercent;
        var usedPercent = window.UsedPercent ?? (remainingPercent is null ? null : 100 - remainingPercent);

        return new NormalizedQuotaWindow(
            label,
            remainingPercent,
            usedPercent,
            remainingPercent is null ? "Tracked" : $"{FormatPercent(remainingPercent.Value)} left",
            FormatNote(window, "Live snapshot"),
            window.ResetAt);
    }

    private static NormalizedQuotaWindow NormalizeQuota(string label, RawQuotaWindow window)
    {
        if (window.Unlimited is true)
        {
            return new NormalizedQuotaWindow(
                label,
                null,
                null,
                "Unlimited",
                window.ResetDescription ?? "No cap detected",
                window.ResetAt);
        }

        var remainingPercent = window.RemainingPercent;
        return new NormalizedQuotaWindow(
            label,
            remainingPercent,
            window.UsedPercent,
            remainingPercent is null ? "Unknown" : $"{FormatPercent(remainingPercent.Value)} left",
            FormatNote(window, "Live snapshot"),
            window.ResetAt);
    }

    private static NormalizedQuotaWindow NormalizeEntitlement(string label, RawQuotaWindow window)
    {
        if (window.Unlimited is true)
        {
            return new NormalizedQuotaWindow(
                label,
                null,
                null,
                "Unlimited",
                window.ResetDescription ?? "No fixed cap detected",
                window.ResetAt);
        }

        var remainingPercent = window.RemainingPercent;
        var usedPercent = window.UsedPercent ?? (remainingPercent is null ? null : 100 - remainingPercent);
        var note = $"{FormatCount(window.Entitlement ?? 0)} total";
        if (window.ResetDescription is not null)
        {
            note = $"{note} • {window.ResetDescription}";
        }

        return new NormalizedQuotaWindow(
            label,
            remainingPercent,
            usedPercent,
            $"{FormatCount(window.Remaining ?? 0)} left",
            note,
            window.ResetAt);
    }

    private static QuotaWindowState ResolveState(double? remainingPercent) =>
        remainingPercent switch
        {
            null => QuotaWindowState.Active,
            <= 12 => QuotaWindowState.Critical,
            <= 30 => QuotaWindowState.Watch,
            _ => QuotaWindowState.Healthy,
        };

    private static string FormatNote(RawQuotaWindow window, string fallback) =>
        window.ResetDescription
        ?? window.ResetAt?.Value.ToLocalTime().ToString("MMM d, HH:mm", CultureInfo.InvariantCulture)
        ?? fallback;

    private static string FormatPercent(double value) =>
        $"{value.ToString("0.#", CultureInfo.CurrentCulture)}%";

    private static string FormatCount(long value) =>
        value.ToString("N0", CultureInfo.CurrentCulture);
}
