namespace AIUsage.Core.Domain;

public static class OpenCodeCostSummarizer
{
    public static ProviderUsageSummary Summarize(RawUsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var cost = ProviderCostFactsParser.ParseOpenCode(snapshot);
        var quota = QuotaWindowNormalizer.Normalize([]);
        return ProviderUsageSummary.Create(
            snapshot,
            quota,
            nextResetAt: null,
            category: ProviderUsageCategory.LocalCost,
            accountLabel: null,
            cost: cost);
    }
}
