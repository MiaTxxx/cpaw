using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

public sealed record ProviderUsageSummary
{
    private ProviderUsageSummary(
        ProviderId providerId,
        ProviderAccountReference? account,
        ProviderTimestamp observedAt,
        UsageSource? source,
        QuotaWindowNormalization quota,
        ProviderTimestamp? nextResetAt,
        ProviderUsageCategory category,
        string? accountLabel,
        ProviderCostSummary? cost)
    {
        ProviderId = providerId;
        Account = account;
        ObservedAt = observedAt;
        Source = source;
        Quota = quota;
        NextResetAt = nextResetAt;
        Category = category;
        AccountLabel = accountLabel;
        Cost = cost;
    }

    public ProviderId ProviderId { get; }

    public ProviderAccountReference? Account { get; }

    public ProviderTimestamp ObservedAt { get; }

    public UsageSource? Source { get; }

    public QuotaWindowNormalization Quota { get; }

    public ProviderTimestamp? NextResetAt { get; }

    public ProviderUsageCategory Category { get; }

    public string? AccountLabel { get; }

    public ProviderCostSummary? Cost { get; }

    public ImmutableArray<string>? UnpricedModels => Cost?.UnpricedModels;

    public static ProviderUsageSummary Create(
        RawUsageSnapshot rawSnapshot,
        QuotaWindowNormalization quota,
        ProviderTimestamp? nextResetAt = null) =>
        Create(
            rawSnapshot,
            quota,
            nextResetAt,
            ProviderUsageCategory.Snapshot,
            accountLabel: null,
            cost: null);

    internal static ProviderUsageSummary Create(
        RawUsageSnapshot rawSnapshot,
        QuotaWindowNormalization quota,
        ProviderTimestamp? nextResetAt,
        ProviderUsageCategory category,
        string? accountLabel,
        ProviderCostSummary? cost)
    {
        ArgumentNullException.ThrowIfNull(rawSnapshot);
        ArgumentNullException.ThrowIfNull(quota);
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (!quota.IsDerivedFrom(rawSnapshot))
        {
            throw new ArgumentException(
                "Normalized quota windows must be derived from the raw usage snapshot.",
                nameof(quota));
        }
        if (cost is not null && !cost.IsDerivedFrom(rawSnapshot))
        {
            throw new ArgumentException(
                "Cost summaries must be derived from the raw usage snapshot.",
                nameof(cost));
        }

        return new ProviderUsageSummary(
            rawSnapshot.ProviderId,
            rawSnapshot.Account,
            rawSnapshot.ObservedAt,
            rawSnapshot.Source,
            quota,
            nextResetAt,
            category,
            accountLabel,
            cost);
    }
}

public enum ProviderUsageCategory
{
    Snapshot,
    Quota,
    LocalCost,
}
