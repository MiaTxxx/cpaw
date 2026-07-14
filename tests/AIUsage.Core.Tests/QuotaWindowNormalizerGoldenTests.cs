using System.Text.Json;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class QuotaWindowNormalizerGoldenTests
{
    [Fact]
    public void Percent_window_derives_used_from_remaining() =>
        AssertWindowFixture("percent-derived-used.json", QuotaWindowInterpretation.Percent);

    [Fact]
    public void Percent_window_preserves_explicit_used_percent() =>
        AssertWindowFixture("percent-explicit-used.json", QuotaWindowInterpretation.Percent);

    [Fact]
    public void Entitlement_window_defaults_missing_counts_to_zero() =>
        AssertWindowFixture("entitlement-missing-values.json", QuotaWindowInterpretation.Entitlement);

    [Fact]
    public void Entitlement_window_treats_unlimited_as_unbounded() =>
        AssertWindowFixture("entitlement-unlimited.json", QuotaWindowInterpretation.Entitlement);

    [Fact]
    public void Quota_window_does_not_derive_missing_used_percent() =>
        AssertWindowFixture("quota-missing-used.json", QuotaWindowInterpretation.Quota);

    [Fact]
    public void Quota_window_treats_unlimited_as_unbounded() =>
        AssertWindowFixture("quota-unlimited.json", QuotaWindowInterpretation.Quota);

    [Fact]
    public void Normalization_uses_the_tightest_available_remaining_percent()
    {
        using var fixture = ReadFixture("tightest-remaining.json");
        var input = fixture.RootElement.GetProperty("input");
        var expected = fixture.RootElement.GetProperty("expected");
        var candidates = input.GetProperty("windows")
            .EnumerateArray()
            .Select(window => new QuotaWindowNormalizationInput(
                window.GetProperty("label").GetString()!,
                new RawQuotaWindow(RawQuotaWindowSlot.Primary)
                {
                    RemainingPercent = ReadOptionalDouble(window, "remainingPercent"),
                },
                QuotaWindowInterpretation.Quota));

        var result = QuotaWindowNormalizer.Normalize(candidates);

        Assert.Equal(ReadOptionalDouble(expected, "remainingPercent"), result.RemainingPercent);
    }

    [Fact]
    public void Normalization_preserves_status_threshold_boundaries()
    {
        using var fixture = ReadFixture("status-boundaries.json");
        var input = fixture.RootElement.GetProperty("input");
        var expectedEntries = fixture.RootElement
            .GetProperty("expected")
            .GetProperty("entries")
            .EnumerateArray()
            .ToArray();
        var remainingPercentages = input.GetProperty("remainingPercentages")
            .EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.Null ? null : (double?)value.GetDouble())
            .ToArray();

        Assert.Equal(expectedEntries.Length, remainingPercentages.Length);
        for (var index = 0; index < remainingPercentages.Length; index++)
        {
            var result = QuotaWindowNormalizer.Normalize(
            [
                new QuotaWindowNormalizationInput(
                    "Status Fixture",
                    new RawQuotaWindow(RawQuotaWindowSlot.Primary)
                    {
                        RemainingPercent = remainingPercentages[index],
                    },
                    QuotaWindowInterpretation.Quota),
            ]);
            var (status, statusLabel) = ProjectStatus(result.State);

            Assert.Equal(ReadOptionalDouble(expectedEntries[index], "remainingPercent"), result.RemainingPercent);
            Assert.Equal(expectedEntries[index].GetProperty("status").GetString(), status);
            Assert.Equal(expectedEntries[index].GetProperty("statusLabel").GetString(), statusLabel);
        }
    }

    private static (string Status, string StatusLabel) ProjectStatus(QuotaWindowState state) =>
        state switch
        {
            QuotaWindowState.Active => ("healthy", "Active"),
            QuotaWindowState.Healthy => ("healthy", "Healthy"),
            QuotaWindowState.Watch => ("watch", "Watch"),
            QuotaWindowState.Critical => ("critical", "Critical"),
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };

    private static void AssertWindowFixture(
        string fileName,
        QuotaWindowInterpretation interpretation)
    {
        using var fixture = ReadFixture(fileName);
        var input = fixture.RootElement.GetProperty("input");
        var expected = fixture.RootElement.GetProperty("expected");

        var result = QuotaWindowNormalizer.Normalize(
        [
            new QuotaWindowNormalizationInput(
                input.GetProperty("label").GetString()!,
                ReadRawWindow(input.GetProperty("window")),
                interpretation),
        ]);

        AssertWindow(expected, Assert.Single(result.Windows));
    }

    private static RawQuotaWindow ReadRawWindow(JsonElement element) =>
        new(RawQuotaWindowSlot.Primary)
        {
            UsedPercent = ReadOptionalDouble(element, "usedPercent"),
            RemainingPercent = ReadOptionalDouble(element, "remainingPercent"),
            ResetAt = ReadOptionalString(element, "resetAt") is { } resetAt
                ? ProviderTimestamp.Parse(resetAt)
                : null,
            ResetDescription = ReadOptionalString(element, "resetDescription"),
            Entitlement = ReadOptionalInt64(element, "entitlement"),
            Remaining = ReadOptionalInt64(element, "remaining"),
            Unlimited = ReadOptionalBoolean(element, "unlimited"),
        };

    private static void AssertWindow(JsonElement expected, NormalizedQuotaWindow actual)
    {
        Assert.Equal(expected.GetProperty("label").GetString(), actual.Label);
        Assert.Equal(ReadOptionalDouble(expected, "remainingPercent"), actual.RemainingPercent);
        Assert.Equal(ReadOptionalDouble(expected, "usedPercent"), actual.UsedPercent);
        Assert.Equal(expected.GetProperty("value").GetString(), actual.Value);
        Assert.Equal(expected.GetProperty("note").GetString(), actual.Note);
        Assert.Equal(ReadOptionalString(expected, "resetAt"), actual.ResetAt?.OriginalText);
    }

    private static double? ReadOptionalDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetDouble()
            : null;

    private static long? ReadOptionalInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetInt64()
            : null;

    private static bool? ReadOptionalBoolean(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetBoolean()
            : null;

    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    private static JsonDocument ReadFixture(string fileName) =>
        JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(fileName)));

    private static string GetFixturePath(string fileName)
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
            "quota",
            fileName);
    }
}
