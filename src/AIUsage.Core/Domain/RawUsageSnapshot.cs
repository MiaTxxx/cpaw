using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

public sealed record RawUsageSnapshot
{
    private RawUsageSnapshot(
        ProviderId providerId,
        string label,
        ProviderAccountReference? account,
        ProviderTimestamp observedAt,
        UsageSource? source,
        ImmutableArray<RawQuotaWindow> quotaWindows,
        ImmutableDictionary<string, ProviderFact> facts)
    {
        ProviderId = providerId;
        Label = label;
        Account = account;
        ObservedAt = observedAt;
        Source = source;
        QuotaWindows = quotaWindows;
        Facts = facts;
    }

    public ProviderId ProviderId { get; }

    public string Label { get; }

    public ProviderAccountReference? Account { get; }

    public ProviderTimestamp ObservedAt { get; }

    public UsageSource? Source { get; }

    public ImmutableArray<RawQuotaWindow> QuotaWindows { get; }

    public ImmutableDictionary<string, ProviderFact> Facts { get; }

    public static RawUsageSnapshot Create(
        ProviderId providerId,
        string label,
        ProviderAccountReference? account,
        ProviderTimestamp observedAt,
        UsageSource? source,
        IEnumerable<RawQuotaWindow> quotaWindows,
        IEnumerable<KeyValuePair<string, ProviderFact>> facts)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(observedAt);
        ArgumentNullException.ThrowIfNull(quotaWindows);
        ArgumentNullException.ThrowIfNull(facts);

        var windowCopy = quotaWindows.ToImmutableArray();
        if (windowCopy.Any(window => window is null))
        {
            throw new ArgumentException("Raw usage snapshots cannot contain null quota windows.", nameof(quotaWindows));
        }

        var duplicateSlot = windowCopy
            .GroupBy(window => window.Slot)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSlot is not null)
        {
            throw new ArgumentException(
                $"Raw usage snapshots cannot contain duplicate {duplicateSlot.Key} quota windows.",
                nameof(quotaWindows));
        }

        var factCopy = ImmutableDictionary.CreateBuilder<string, ProviderFact>(StringComparer.Ordinal);
        foreach (var fact in facts)
        {
            if (fact.Key is null || fact.Value is null)
            {
                throw new ArgumentException("Raw usage snapshots cannot contain null fact keys or values.", nameof(facts));
            }

            factCopy.Add(fact.Key, fact.Value);
        }

        return new RawUsageSnapshot(
            providerId,
            label,
            account,
            observedAt,
            source,
            windowCopy,
            factCopy.ToImmutable());
    }
}

public sealed record UsageSource
{
    private UsageSource(
        string mode,
        string type,
        string? browserName,
        string? profile,
        string? defaultsDomain,
        ImmutableArray<string>? roots,
        string? envVar)
    {
        Mode = mode;
        Type = type;
        BrowserName = browserName;
        Profile = profile;
        DefaultsDomain = defaultsDomain;
        Roots = roots;
        EnvVar = envVar;
    }

    public string Mode { get; }

    public string Type { get; }

    public string? BrowserName { get; }

    public string? Profile { get; }

    public string? DefaultsDomain { get; }

    public ImmutableArray<string>? Roots { get; }

    public string? EnvVar { get; }

    public static UsageSource Create(
        string mode,
        string type,
        string? browserName = null,
        string? profile = null,
        string? defaultsDomain = null,
        IEnumerable<string>? roots = null,
        string? envVar = null)
    {
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(type);
        var rootCopy = roots?.ToImmutableArray();
        if (rootCopy?.Any(root => root is null) == true)
        {
            throw new ArgumentException("Usage source roots cannot contain null paths.", nameof(roots));
        }

        return new UsageSource(mode, type, browserName, profile, defaultsDomain, rootCopy, envVar);
    }
}
