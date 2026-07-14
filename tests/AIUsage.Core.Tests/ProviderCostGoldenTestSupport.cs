using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

internal static class ProviderCostGoldenTestSupport
{
    internal static JsonDocument ReadFixture(string providerDirectory, string fileName) =>
        JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(providerDirectory, fileName)));

    internal static RawUsageSnapshot CreateSnapshot(
        JsonElement root,
        ProviderId providerId,
        string sourceLabel)
    {
        var input = root.GetProperty("input");
        var account = input.TryGetProperty("accountEmail", out var accountEmail)
            ? ProviderAccountReference.Create(email: accountEmail.GetString())
            : null;
        var facts = input.GetProperty("extra")
            .EnumerateObject()
            .Select(property => new KeyValuePair<string, ProviderFact>(
                property.Name,
                ReadFact(property.Value)));

        return RawUsageSnapshot.Create(
            providerId,
            sourceLabel,
            account,
            ProviderTimestamp.Parse(root.GetProperty("clock").GetString()!),
            null,
            [],
            facts);
    }

    internal static void AssertCostPeriods(JsonElement expected, ProviderCostSummary actual)
    {
        AssertPeriod(expected.GetProperty("today"), actual.Today);
        AssertPeriod(expected.GetProperty("week"), actual.Week);
        AssertPeriod(expected.GetProperty("month"), actual.Month);
        AssertPeriod(expected.GetProperty("overall"), actual.Overall);
    }

    internal static void AssertModels(
        JsonElement expected,
        ImmutableArray<ProviderModelCost>? actual)
    {
        var expectedModels = expected.EnumerateArray().ToArray();
        Assert.True(actual.HasValue);
        Assert.Equal(expectedModels.Length, actual.Value.Length);
        for (var index = 0; index < expectedModels.Length; index++)
        {
            var expectedModel = expectedModels[index];
            var actualModel = actual.Value[index];
            Assert.Equal(expectedModel.GetProperty("model").GetString(), actualModel.Model);
            Assert.Equal(expectedModel.GetProperty("totalTokens").GetInt64(), actualModel.TotalTokens);
            Assert.Equal(expectedModel.GetProperty("inputTokens").GetInt64(), actualModel.InputTokens);
            Assert.Equal(expectedModel.GetProperty("outputTokens").GetInt64(), actualModel.OutputTokens);
            Assert.Equal(expectedModel.GetProperty("cacheReadTokens").GetInt64(), actualModel.CacheReadTokens);
            Assert.Equal(expectedModel.GetProperty("cacheCreateTokens").GetInt64(), actualModel.CacheCreateTokens);
            Assert.Equal(expectedModel.GetProperty("estimatedCostUsd").GetDouble(), actualModel.EstimatedCostUsd);
            Assert.Equal(expectedModel.GetProperty("percentage").GetDouble(), actualModel.Percentage);
        }
    }

    internal static void AssertTimeline(
        JsonElement expected,
        ImmutableArray<ProviderCostTimelinePoint> actual)
    {
        var expectedPoints = expected.EnumerateArray().ToArray();
        Assert.Equal(expectedPoints.Length, actual.Length);
        for (var index = 0; index < expectedPoints.Length; index++)
        {
            var expectedPoint = expectedPoints[index];
            var actualPoint = actual[index];
            Assert.Equal(expectedPoint.GetProperty("bucket").GetString(), actualPoint.Bucket);
            Assert.Equal(expectedPoint.GetProperty("label").GetString(), actualPoint.Label);
            Assert.Equal(expectedPoint.GetProperty("usd").GetDouble(), actualPoint.Usd);
            Assert.Equal(expectedPoint.GetProperty("tokens").GetInt64(), actualPoint.Tokens);
            Assert.Equal(ReadOptionalInt64(expectedPoint, "inputTokens"), actualPoint.InputTokens);
            Assert.Equal(ReadOptionalInt64(expectedPoint, "outputTokens"), actualPoint.OutputTokens);
            Assert.Equal(ReadOptionalInt64(expectedPoint, "cacheReadTokens"), actualPoint.CacheReadTokens);
            Assert.Equal(ReadOptionalInt64(expectedPoint, "cacheCreateTokens"), actualPoint.CacheCreateTokens);
        }
    }

    internal static void AssertModelTimelines(
        JsonElement expected,
        ImmutableArray<ProviderModelTimeline>? actual)
    {
        var expectedModels = expected.EnumerateArray().ToArray();
        Assert.True(actual.HasValue);
        Assert.Equal(expectedModels.Length, actual.Value.Length);
        for (var index = 0; index < expectedModels.Length; index++)
        {
            var expectedModel = expectedModels[index];
            var actualModel = actual.Value[index];
            Assert.Equal(expectedModel.GetProperty("model").GetString(), actualModel.Model);
            AssertTimeline(expectedModel.GetProperty("hourly"), actualModel.Hourly);
            AssertTimeline(expectedModel.GetProperty("daily"), actualModel.Daily);
        }
    }

    private static ProviderFact ReadFact(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Null => ProviderFact.Null,
            JsonValueKind.True => ProviderFact.FromBoolean(true),
            JsonValueKind.False => ProviderFact.FromBoolean(false),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => ProviderFact.FromInteger(integer),
            JsonValueKind.Number => ProviderFact.FromNumber(element.GetDouble()),
            JsonValueKind.String => ProviderFact.FromString(element.GetString()!),
            JsonValueKind.Array => ProviderFact.FromArray(element.EnumerateArray().Select(ReadFact)),
            JsonValueKind.Object => ProviderFact.FromObject(element.EnumerateObject().Select(property =>
                new KeyValuePair<string, ProviderFact>(property.Name, ReadFact(property.Value)))),
            _ => throw new InvalidDataException($"Unsupported fixture value kind {element.ValueKind}."),
        };

    private static void AssertPeriod(JsonElement expected, ProviderCostPeriod actual)
    {
        Assert.Equal(expected.GetProperty("usd").GetDouble(), actual.Usd);
        Assert.Equal(expected.GetProperty("tokens").GetInt64(), actual.Tokens);
        Assert.Equal(expected.GetProperty("rangeLabel").GetString(), actual.RangeLabel);
    }

    private static long? ReadOptionalInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetInt64()
            : null;

    private static string GetFixturePath(string providerDirectory, string fileName)
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
            "normalization",
            "provider",
            providerDirectory,
            fileName);
    }
}
