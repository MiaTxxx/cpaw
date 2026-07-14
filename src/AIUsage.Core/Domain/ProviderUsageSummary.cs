namespace AIUsage.Core.Domain;

public sealed record ProviderUsageSummary
{
    private ProviderUsageSummary(
        ProviderId providerId,
        ProviderAccountReference? account,
        ProviderTimestamp observedAt,
        UsageSource? source,
        QuotaWindowNormalization quota)
    {
        ProviderId = providerId;
        Account = account;
        ObservedAt = observedAt;
        Source = source;
        Quota = quota;
    }

    public ProviderId ProviderId { get; }

    public ProviderAccountReference? Account { get; }

    public ProviderTimestamp ObservedAt { get; }

    public UsageSource? Source { get; }

    public QuotaWindowNormalization Quota { get; }

    public static ProviderUsageSummary Create(
        RawUsageSnapshot rawSnapshot,
        QuotaWindowNormalization quota)
    {
        ArgumentNullException.ThrowIfNull(rawSnapshot);
        ArgumentNullException.ThrowIfNull(quota);
        if (!quota.IsDerivedFrom(rawSnapshot))
        {
            throw new ArgumentException(
                "Normalized quota windows must be derived from the raw usage snapshot.",
                nameof(quota));
        }

        return new ProviderUsageSummary(
            rawSnapshot.ProviderId,
            rawSnapshot.Account,
            rawSnapshot.ObservedAt,
            rawSnapshot.Source,
            quota);
    }
}
