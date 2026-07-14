using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

public sealed record NormalizedQuotaWindow
{
    internal NormalizedQuotaWindow(
        string label,
        double? remainingPercent,
        double? usedPercent,
        string value,
        string note,
        ProviderTimestamp? resetAt)
    {
        Label = label;
        RemainingPercent = remainingPercent;
        UsedPercent = usedPercent;
        Value = value;
        Note = note;
        ResetAt = resetAt;
    }

    public string Label { get; }

    public double? RemainingPercent { get; }

    public double? UsedPercent { get; }

    public string Value { get; }

    public string Note { get; }

    public ProviderTimestamp? ResetAt { get; }
}

public sealed record QuotaWindowNormalizationInput
{
    public QuotaWindowNormalizationInput(
        string label,
        RawQuotaWindow window,
        QuotaWindowInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(window);
        if (!Enum.IsDefined(interpretation))
        {
            throw new ArgumentOutOfRangeException(nameof(interpretation));
        }

        Label = label;
        Window = window;
        Interpretation = interpretation;
    }

    public string Label { get; }

    public RawQuotaWindow Window { get; }

    public QuotaWindowInterpretation Interpretation { get; }
}

public sealed record QuotaWindowNormalization
{
    internal QuotaWindowNormalization(
        ImmutableArray<NormalizedQuotaWindow> windows,
        double? remainingPercent,
        QuotaWindowState state)
    {
        Windows = windows;
        RemainingPercent = remainingPercent;
        State = state;
    }

    public ImmutableArray<NormalizedQuotaWindow> Windows { get; }

    public double? RemainingPercent { get; }

    public QuotaWindowState State { get; }
}

public enum QuotaWindowInterpretation
{
    Percent,
    Entitlement,
    Quota,
}

public enum QuotaWindowState
{
    Active,
    Healthy,
    Watch,
    Critical,
}
