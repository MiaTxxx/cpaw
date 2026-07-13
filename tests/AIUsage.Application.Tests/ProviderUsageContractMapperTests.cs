using System.Globalization;
using System.Text.Json;
using AIUsage.Application.Providers;
using AIUsage.Contracts;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Application.Tests;

public sealed class ProviderUsageContractMapperTests
{
    [Fact]
    public void Provider_usage_contract_maps_to_a_transport_agnostic_raw_snapshot()
    {
        var contract = DeserializeFixture("contracts/provider/provider-usage-full.json");

        var snapshot = ProviderUsageContractMapper.ToDomain(contract);

        Assert.Equal(new ProviderId("codex"), snapshot.ProviderId);
        Assert.Equal("Codex Fixture", snapshot.Label);
        Assert.Equal("2030-01-02T03:04:05Z", snapshot.ObservedAt.OriginalText);
        Assert.Equal(ParseTimestamp("2030-01-02T03:04:05Z"), snapshot.ObservedAt.Value);
        Assert.NotNull(snapshot.Account);
        Assert.Equal(new ProviderAccountId("fixture-account"), snapshot.Account.Id);
        Assert.Equal("alice@example.test", snapshot.Account.Email);
        Assert.Equal("Fixture User", snapshot.Account.Name);
        Assert.Equal("fixture-login", snapshot.Account.Login);
        Assert.Equal("pro", snapshot.Account.Plan);
        Assert.NotNull(snapshot.Source);
        Assert.Equal("local", snapshot.Source.Mode);
        Assert.Equal("authFile", snapshot.Source.Type);
        Assert.Equal("Fixture Browser", snapshot.Source.BrowserName);
        Assert.Equal("/fixture/codex", Assert.Single(snapshot.Source.Roots!.Value));

        Assert.Collection(
            snapshot.QuotaWindows,
            primary =>
            {
                Assert.Equal(RawQuotaWindowSlot.Primary, primary.Slot);
                Assert.Equal(25, primary.UsedPercent);
                Assert.Equal(75, primary.RemainingPercent);
                Assert.Equal("2030-01-03T03:04:05+00:00", primary.ResetAt?.OriginalText);
                Assert.Equal(ParseTimestamp("2030-01-03T03:04:05+00:00"), primary.ResetAt?.Value);
                Assert.Equal("Resets tomorrow", primary.ResetDescription);
                Assert.Equal(1000, primary.Entitlement);
                Assert.Equal(750, primary.Remaining);
                Assert.False(primary.Unlimited);
                Assert.Equal("5h", primary.Label);
            },
            secondary =>
            {
                Assert.Equal(RawQuotaWindowSlot.Secondary, secondary.Slot);
                Assert.Equal(40, secondary.UsedPercent);
                Assert.Equal(60, secondary.RemainingPercent);
                Assert.Equal("2030-01-09T03:04:05Z", secondary.ResetAt?.OriginalText);
                Assert.Equal(ParseTimestamp("2030-01-09T03:04:05Z"), secondary.ResetAt?.Value);
                Assert.Null(secondary.Entitlement);
            });

        var nested = Assert.IsType<ProviderFact.ObjectValue>(snapshot.Facts["nested"]);
        Assert.Equal(9, Assert.IsType<ProviderFact.IntegerValue>(nested.Properties["count"]).Value);
        Assert.True(Assert.IsType<ProviderFact.BooleanValue>(nested.Properties["ok"]).Value);
        Assert.IsType<ProviderFact.NullValue>(snapshot.Facts["nullable"]);
        Assert.Equal(0.5, Assert.IsType<ProviderFact.NumberValue>(snapshot.Facts["ratio"]).Value);
    }

    [Fact]
    public void Provider_facts_preserve_every_frozen_value_kind_and_64_bit_integers()
    {
        var snapshot = ProviderUsageContractMapper.ToDomain(DeserializeFixture(
            "contracts/provider/provider-usage-extra-all-json-kinds.json"));

        Assert.True(Assert.IsType<ProviderFact.BooleanValue>(snapshot.Facts["boolean"]).Value);
        Assert.Equal(0.125, Assert.IsType<ProviderFact.NumberValue>(snapshot.Facts["double"]).Value);
        Assert.Equal(
            4_294_967_296,
            Assert.IsType<ProviderFact.IntegerValue>(snapshot.Facts["integer"]).Value);
        Assert.IsType<ProviderFact.NullValue>(snapshot.Facts["null"]);
        Assert.Equal(
            "fixture-value",
            Assert.IsType<ProviderFact.StringValue>(snapshot.Facts["string"]).Value);

        var array = Assert.IsType<ProviderFact.ArrayValue>(snapshot.Facts["array"]);
        Assert.Collection(
            array.Items,
            item => Assert.Equal("fixture-item", Assert.IsType<ProviderFact.StringValue>(item).Value),
            item => Assert.Equal(7, Assert.IsType<ProviderFact.IntegerValue>(item).Value),
            item => Assert.Equal(2.5, Assert.IsType<ProviderFact.NumberValue>(item).Value),
            item => Assert.False(Assert.IsType<ProviderFact.BooleanValue>(item).Value),
            item => Assert.IsType<ProviderFact.NullValue>(item));

        var nested = Assert.IsType<ProviderFact.ObjectValue>(snapshot.Facts["object"]);
        var nestedArray = Assert.IsType<ProviderFact.ArrayValue>(nested.Properties["nestedArray"]);
        Assert.Equal(2, nestedArray.Items.Length);
        Assert.IsType<ProviderFact.NullValue>(nested.Properties["nestedNull"]);
    }

    [Fact]
    public void Minimal_provider_usage_keeps_its_offset_and_absent_optional_facts()
    {
        var snapshot = ProviderUsageContractMapper.ToDomain(DeserializeFixture(
            "contracts/provider/provider-usage-minimal-offset-time.json"));

        Assert.Equal("2030-01-02T11:04:05+08:00", snapshot.ObservedAt.OriginalText);
        Assert.Equal(TimeSpan.FromHours(8), snapshot.ObservedAt.Value.Offset);
        Assert.Equal(ParseTimestamp("2030-01-02T11:04:05+08:00"), snapshot.ObservedAt.Value);
        Assert.Null(snapshot.Account);
        Assert.Null(snapshot.Source);
        Assert.Empty(snapshot.QuotaWindows);
        Assert.Empty(snapshot.Facts);
    }

    [Fact]
    public void Provider_timestamps_preserve_high_precision_source_text()
    {
        const string timestamp = "2030-01-02T03:04:05.123456789Z";
        var contract = new ProviderUsage
        {
            Provider = "fixture-provider",
            Label = "Fixture",
            FetchedAt = Iso8601Timestamp.Parse(timestamp),
            Extra = SafeJsonMetadata.Empty,
        };

        var snapshot = ProviderUsageContractMapper.ToDomain(contract);

        Assert.Equal(timestamp, snapshot.ObservedAt.OriginalText);
        Assert.Equal(Iso8601Timestamp.Parse(timestamp).Value, snapshot.ObservedAt.Value);
    }

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);

    private static ProviderUsage DeserializeFixture(string relativePath)
    {
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(relativePath)));
        return ContractJson.Deserialize<ProviderUsage>(
            envelope.RootElement.GetProperty("input").GetRawText());
    }

    private static string GetFixturePath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AIUsage.Windows.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return Path.Combine(
            current.FullName,
            "QuotaBackend",
            "Tests",
            "QuotaBackendTests",
            "Fixtures",
            "v1",
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
