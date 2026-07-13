using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsage.Contracts;
using Xunit;

namespace AIUsage.Contracts.Tests;

public sealed class GoldenContractTests
{
    public static TheoryData<string, Type> ContractFixtures => new()
    {
        { "contracts/account/account-credential-all-fields.json", typeof(LegacyAccountCredentialWire) },
        { "contracts/dashboard/dashboard-snapshot-mixed.json", typeof(DashboardSnapshot) },
        { "contracts/provider/provider-result-failure-no-summary.json", typeof(ProviderResult) },
        { "contracts/provider/provider-result-success.json", typeof(ProviderResult) },
        { "contracts/provider/provider-summary-full.json", typeof(ProviderSummary) },
        { "contracts/provider/provider-usage-full.json", typeof(ProviderUsage) },
    };

    [Theory]
    [MemberData(nameof(ContractFixtures))]
    public void CSharp_contracts_round_trip_Swift_goldens(string relativePath, Type contractType)
    {
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(relativePath)));
        var input = envelope.RootElement.GetProperty("input");
        var expected = envelope.RootElement.GetProperty("expected");

        var value = contractType == typeof(LegacyAccountCredentialWire)
            ? JsonSerializer.Deserialize(input.GetRawText(), contractType)
            : ContractJson.Deserialize(input.GetRawText(), contractType);
        Assert.NotNull(value);

        var serialized = contractType == typeof(LegacyAccountCredentialWire)
            ? JsonSerializer.Serialize(value, contractType)
            : ContractJson.Serialize(value, contractType);
        using var actual = JsonDocument.Parse(serialized);
        AssertJsonEquivalent(expected, actual.RootElement, "$expected");
    }

    [Theory]
    [InlineData("degraded")]
    [InlineData("future-provider-state")]
    public void Provider_status_preserves_unknown_wire_values(string wireValue)
    {
        var status = JsonSerializer.Deserialize<ProviderStatus>($"\"{wireValue}\"");

        Assert.NotNull(status);
        Assert.False(status.IsKnown);
        Assert.Equal(wireValue, status.Value);
        Assert.Equal($"\"{wireValue}\"", JsonSerializer.Serialize(status));
    }

    [Fact]
    public void Alert_tone_preserves_unknown_wire_values()
    {
        var tone = JsonSerializer.Deserialize<AlertTone>("\"investigate\"");

        Assert.NotNull(tone);
        Assert.False(tone.IsKnown);
        Assert.Equal("investigate", tone.Value);
        Assert.Equal("\"investigate\"", JsonSerializer.Serialize(tone));
    }

    [Fact]
    public void Failure_result_omits_absent_optional_fields()
    {
        var result = new ProviderResult
        {
            Id = "warp:fixture",
            ProviderId = "warp",
            Ok = false,
            Error = "not logged in",
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.False(json.RootElement.TryGetProperty("accountId", out _));
        Assert.False(json.RootElement.TryGetProperty("usage", out _));
        Assert.False(json.RootElement.TryGetProperty("summary", out _));
        Assert.Equal("not logged in", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void Token_counts_are_64_bit()
    {
        const long tokenCount = 4_294_967_296;
        var period = JsonSerializer.Deserialize<CostPeriod>($$"""{"usd":12.34,"tokens":{{tokenCount}},"rangeLabel":"Overall"}""");

        Assert.NotNull(period);
        Assert.Equal(tokenCount, period.Tokens);
    }

    [Theory]
    [InlineData("{\"accessToken\":\"ordinary-looking-value\"}")]
    [InlineData("{\"nested\":{\"api_key\":\"ordinary-looking-value\"}}")]
    [InlineData("{\"opaque\":\"Bearer fixture-secret\"}")]
    public void Safe_metadata_rejects_sensitive_content(string json)
    {
        Assert.Throws<JsonException>(() => SafeJsonMetadata.Parse(json));
    }

    [Fact]
    public void Contract_json_rejects_explicit_null_for_required_values()
    {
        const string json = """
            {"id":null,"providerId":"warp","ok":false,"error":"not logged in"}
            """;

        Assert.Throws<JsonException>(() => ContractJson.Deserialize<ProviderResult>(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Open_string_values_reject_empty_construction(string value)
    {
        Assert.Throws<ArgumentException>(() => new ProviderStatus(value));
        Assert.Throws<ArgumentException>(() => new AlertTone(value));
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

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(expected.ValueKind == actual.ValueKind, $"JSON kind differs at {path}: expected {expected.ValueKind}, actual {actual.ValueKind}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToDictionary(property => property.Name, StringComparer.Ordinal);
                var actualProperties = actual.EnumerateObject().ToDictionary(property => property.Name, StringComparer.Ordinal);
                Assert.True(
                    expectedProperties.Keys.Order(StringComparer.Ordinal).SequenceEqual(actualProperties.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal),
                    $"JSON properties differ at {path}");
                foreach (var property in expectedProperties)
                {
                    AssertJsonEquivalent(property.Value.Value, actualProperties[property.Key].Value, $"{path}.{property.Key}");
                }

                break;
            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (var index = 0; index < expectedItems.Length; index++)
                {
                    AssertJsonEquivalent(expectedItems[index], actualItems[index], $"{path}[{index}]");
                }

                break;
            case JsonValueKind.Number:
                if (expected.TryGetInt64(out var expectedInteger) && actual.TryGetInt64(out var actualInteger))
                {
                    Assert.True(expectedInteger == actualInteger, $"JSON integer differs at {path}");
                }
                else
                {
                    Assert.True(expected.GetDouble().Equals(actual.GetDouble()), $"JSON number differs at {path}");
                }

                break;
            case JsonValueKind.String:
                Assert.True(expected.GetString() == actual.GetString(), $"JSON string differs at {path}");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                Assert.True(expected.GetBoolean() == actual.GetBoolean(), $"JSON boolean differs at {path}");
                break;
            case JsonValueKind.Null:
                break;
            default:
                throw new Xunit.Sdk.XunitException($"Unsupported JSON kind at {path}: {expected.ValueKind}");
        }
    }

    private sealed record LegacyAccountCredentialWire
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("providerId")]
        public required string ProviderId { get; init; }

        [JsonPropertyName("accountLabel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccountLabel { get; init; }

        [JsonPropertyName("authMethod")]
        public required string AuthMethod { get; init; }

        [JsonPropertyName("credential")]
        public required string Credential { get; init; }

        [JsonPropertyName("createdAt")]
        public required string CreatedAt { get; init; }

        [JsonPropertyName("lastUsedAt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LastUsedAt { get; init; }

        [JsonPropertyName("metadata")]
        public required IReadOnlyDictionary<string, string> Metadata { get; init; }
    }
}
