namespace AIUsage.Core.Domain;

public static class ClaudeCostSummarizer
{
    public static ProviderUsageSummary Summarize(RawUsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var cost = ProviderCostFactsParser.ParseClaude(snapshot);
        var quota = QuotaWindowNormalizer.Normalize([]);
        var accountLabel = NormalizeAccountLabel(snapshot.Account?.Email);

        return ProviderUsageSummary.Create(
            snapshot,
            quota,
            nextResetAt: null,
            category: ProviderUsageCategory.LocalCost,
            accountLabel: accountLabel,
            cost: cost);
    }

    private static string? NormalizeAccountLabel(string? email)
    {
        var trimmed = email?.Trim();
        return trimmed is not null && trimmed.Contains('@', StringComparison.Ordinal)
            ? trimmed
            : null;
    }
}
