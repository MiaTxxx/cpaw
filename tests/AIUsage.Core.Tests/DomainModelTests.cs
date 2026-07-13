using System.Text.Json;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void Domain_models_do_not_expose_transport_types_or_json_annotations()
    {
        var domainTypes = typeof(RawUsageSnapshot).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("AIUsage.Core.Domain", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(domainTypes);
        Assert.DoesNotContain(
            domainTypes.SelectMany(type => type.GetCustomAttributes(inherit: true)),
            attribute => attribute.GetType().Namespace == "System.Text.Json.Serialization");
        Assert.DoesNotContain(
            domainTypes.SelectMany(type => type.GetProperties())
                .SelectMany(property => property.GetCustomAttributes(inherit: true)),
            attribute => attribute.GetType().Namespace == "System.Text.Json.Serialization");
        Assert.DoesNotContain(
            domainTypes.SelectMany(type => type.GetProperties())
                .SelectMany(property => EnumerateTypeGraph(property.PropertyType)),
            type => type == typeof(JsonElement) || type.Assembly.GetName().Name == "AIUsage.Contracts");
        Assert.DoesNotContain(
            typeof(RawUsageSnapshot).Assembly.GetReferencedAssemblies(),
            reference => reference.Name == "AIUsage.Contracts");
    }

    [Fact]
    public void Raw_quota_windows_preserve_partial_and_contradictory_provider_facts()
    {
        var window = new RawQuotaWindow(RawQuotaWindowSlot.Primary)
        {
            UsedPercent = 90,
            RemainingPercent = 90,
            Unlimited = null,
        };

        Assert.Equal(90, window.UsedPercent);
        Assert.Equal(90, window.RemainingPercent);
        Assert.Null(window.ResetAt);
        Assert.Null(window.Entitlement);
        Assert.Null(window.Unlimited);
    }

    [Fact]
    public void Domain_identifiers_reject_null_without_inventing_stricter_string_rules()
    {
        Assert.Throws<ArgumentNullException>(() => new ProviderId(null!));
        Assert.Throws<ArgumentNullException>(() => new ProviderAccountId(null!));

    }

    [Fact]
    public void Snapshot_collections_are_isolated_from_mutable_inputs()
    {
        var windows = new List<RawQuotaWindow> { new(RawQuotaWindowSlot.Primary) };
        var roots = new List<string> { "/fixture/root" };
        var arrayItems = new List<ProviderFact> { ProviderFact.FromInteger(1) };
        var objectProperties = new Dictionary<string, ProviderFact>
        {
            ["value"] = ProviderFact.FromInteger(2),
        };
        var facts = new Dictionary<string, ProviderFact>
        {
            ["array"] = ProviderFact.FromArray(arrayItems),
            ["object"] = ProviderFact.FromObject(objectProperties),
        };
        var source = UsageSource.Create("local", "fixture", roots: roots);
        var snapshot = RawUsageSnapshot.Create(
            new ProviderId("fixture"),
            "Fixture",
            null,
            ProviderTimestamp.Parse("2030-01-02T03:04:05Z"),
            source,
            windows,
            facts);

        windows.Clear();
        roots.Clear();
        arrayItems.Clear();
        objectProperties.Clear();
        facts.Clear();

        Assert.Single(snapshot.QuotaWindows);
        Assert.Equal("/fixture/root", Assert.Single(snapshot.Source!.Roots!.Value));
        Assert.Single(Assert.IsType<ProviderFact.ArrayValue>(snapshot.Facts["array"]).Items);
        Assert.Single(Assert.IsType<ProviderFact.ObjectValue>(snapshot.Facts["object"]).Properties);
    }

    [Fact]
    public void Provider_facts_reject_non_json_states_and_compare_as_values()
    {
        Assert.Throws<ArgumentNullException>(() => ProviderFact.FromString(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProviderFact.FromNumber(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProviderFact.FromNumber(double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => ProviderFact.FromArray([ProviderFact.Null, null!]));
        Assert.Throws<ArgumentException>(() => ProviderFact.FromObject(
            [new KeyValuePair<string, ProviderFact>("invalid", null!)]));

        Assert.Equal(
            ProviderFact.FromArray([ProviderFact.FromInteger(1), ProviderFact.FromString("fixture")]),
            ProviderFact.FromArray([ProviderFact.FromInteger(1), ProviderFact.FromString("fixture")]));
        Assert.Equal(
            ProviderFact.FromObject([new KeyValuePair<string, ProviderFact>("value", ProviderFact.FromInteger(1))]),
            ProviderFact.FromObject([new KeyValuePair<string, ProviderFact>("value", ProviderFact.FromInteger(1))]));
    }

    [Fact]
    public void Snapshots_reject_duplicate_window_slots_and_empty_account_references()
    {
        Assert.Throws<ArgumentException>(() => ProviderAccountReference.Create());

        var timestamp = ProviderTimestamp.Parse("2030-01-02T03:04:05Z");
        Assert.Throws<ArgumentException>(() => RawUsageSnapshot.Create(
            new ProviderId("fixture"),
            "Fixture",
            null,
            timestamp,
            null,
            [
                new RawQuotaWindow(RawQuotaWindowSlot.Primary),
                new RawQuotaWindow(RawQuotaWindowSlot.Primary),
            ],
            []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2030-01-02")]
    [InlineData("2030-01-02T03:04:05")]
    [InlineData("2030-02-30T03:04:05Z")]
    public void Provider_timestamps_reject_values_without_a_valid_timezone(string value)
    {
        Assert.False(ProviderTimestamp.TryParse(value, out _));
        Assert.Throws<FormatException>(() => ProviderTimestamp.Parse(value));
    }

    private static IEnumerable<Type> EnumerateTypeGraph(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in EnumerateTypeGraph(elementType))
            {
                yield return nestedType;
            }
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            foreach (var nestedType in EnumerateTypeGraph(genericArgument))
            {
                yield return nestedType;
            }
        }
    }
}
