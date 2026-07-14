namespace AIUsage.Core.Domain;

public static class CodexUsageSummarizer
{
    public static ProviderUsageSummary Summarize(RawUsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var primary = FindWindow(snapshot, RawQuotaWindowSlot.Primary);
        var secondary = FindWindow(snapshot, RawQuotaWindowSlot.Secondary);
        var tertiary = FindWindow(snapshot, RawQuotaWindowSlot.Tertiary);
        var inputs = new List<QuotaWindowNormalizationInput>(3);

        AddWindow(inputs, "5h Window", primary);
        AddWindow(inputs, "Weekly Window", secondary);
        AddWindow(inputs, "Code Review", tertiary);

        var quota = QuotaWindowNormalizer.Normalize(inputs);
        var nextResetAt = primary?.ResetAt ?? secondary?.ResetAt;
        return ProviderUsageSummary.Create(snapshot, quota, nextResetAt);
    }

    private static RawQuotaWindow? FindWindow(
        RawUsageSnapshot snapshot,
        RawQuotaWindowSlot slot) =>
        snapshot.QuotaWindows.SingleOrDefault(window => window.Slot == slot);

    private static void AddWindow(
        ICollection<QuotaWindowNormalizationInput> inputs,
        string label,
        RawQuotaWindow? window)
    {
        if (window is null)
        {
            return;
        }

        inputs.Add(new QuotaWindowNormalizationInput(
            label,
            window,
            QuotaWindowInterpretation.Percent));
    }
}
