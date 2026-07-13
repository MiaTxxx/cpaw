using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Contracts;
using AIUsage.Core.Domain;
using DomainRawQuotaWindow = AIUsage.Core.Domain.RawQuotaWindow;

namespace AIUsage.Application.Providers;

public static class ProviderUsageContractMapper
{
    public static RawUsageSnapshot ToDomain(ProviderUsage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var windows = new List<DomainRawQuotaWindow>(3);
        AddWindow(windows, source.Primary, RawQuotaWindowSlot.Primary);
        AddWindow(windows, source.Secondary, RawQuotaWindowSlot.Secondary);
        AddWindow(windows, source.Tertiary, RawQuotaWindowSlot.Tertiary);

        return RawUsageSnapshot.Create(
            new ProviderId(source.Provider),
            source.Label,
            MapAccount(source),
            MapTimestamp(source.FetchedAt),
            MapSource(source.Source),
            windows,
            MapFacts(source.Extra));
    }

    private static void AddWindow(
        List<DomainRawQuotaWindow> windows,
        AIUsage.Contracts.RawQuotaWindow? source,
        RawQuotaWindowSlot slot)
    {
        if (source is null)
        {
            return;
        }

        windows.Add(new DomainRawQuotaWindow(slot)
        {
            UsedPercent = source.UsedPercent,
            RemainingPercent = source.RemainingPercent,
            ResetAt = source.ResetAt is null ? null : MapTimestamp(source.ResetAt),
            ResetDescription = source.ResetDescription,
            Entitlement = source.Entitlement,
            Remaining = source.Remaining,
            Unlimited = source.Unlimited,
            Label = source.Label,
        });
    }

    private static ProviderAccountReference? MapAccount(ProviderUsage source)
    {
        if (source.AccountId is null
            && source.AccountEmail is null
            && source.AccountName is null
            && source.AccountLogin is null
            && source.AccountPlan is null)
        {
            return null;
        }

        return ProviderAccountReference.Create(
            source.AccountId is null ? null : new ProviderAccountId(source.AccountId),
            source.AccountEmail,
            source.AccountName,
            source.AccountLogin,
            source.AccountPlan);
    }

    private static UsageSource? MapSource(SourceInfo? source) =>
        source is null
            ? null
            : UsageSource.Create(
                source.Mode,
                source.Type,
                source.BrowserName,
                source.Profile,
                source.DefaultsDomain,
                source.Roots,
                source.EnvVar);

    private static ProviderTimestamp MapTimestamp(Iso8601Timestamp timestamp) =>
        ProviderTimestamp.Parse(timestamp.OriginalText);

    private static ImmutableDictionary<string, ProviderFact> MapFacts(
        IEnumerable<KeyValuePair<string, JsonElement>> values) =>
        values.ToImmutableDictionary(
            pair => pair.Key,
            pair => MapFact(pair.Value),
            StringComparer.Ordinal);

    private static ProviderFact MapFact(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null => ProviderFact.Null,
            JsonValueKind.True => ProviderFact.FromBoolean(true),
            JsonValueKind.False => ProviderFact.FromBoolean(false),
            JsonValueKind.Number when value.TryGetInt64(out var integer) =>
                ProviderFact.FromInteger(integer),
            JsonValueKind.Number => ProviderFact.FromNumber(value.GetDouble()),
            JsonValueKind.String => ProviderFact.FromString(value.GetString()!),
            JsonValueKind.Array => ProviderFact.FromArray(value.EnumerateArray().Select(MapFact)),
            JsonValueKind.Object => ProviderFact.FromObject(
                value.EnumerateObject().Select(property =>
                    new KeyValuePair<string, ProviderFact>(property.Name, MapFact(property.Value)))),
            _ => throw new ArgumentException("Provider facts must be valid JSON values.", nameof(value)),
        };
}
