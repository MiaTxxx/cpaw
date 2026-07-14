using System.Text.Json;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CodexUsageSummarizerGoldenTests
{
    [Fact]
    public void Summary_preserves_all_window_slots_and_order()
    {
        using var fixture = ReadFixture("all-windows-order.json");
        var root = fixture.RootElement;
        var input = root.GetProperty("input");
        var expected = root.GetProperty("expected");
        var snapshot = CreateSnapshot(root, input);

        var summary = CodexUsageSummarizer.Summarize(snapshot);

        AssertSummary(expected, summary);
    }

    [Fact]
    public void Summary_falls_back_to_secondary_reset_and_ignores_tertiary()
    {
        using var fixture = ReadFixture("secondary-reset-fallback.json");
        var root = fixture.RootElement;
        var expected = root.GetProperty("expected");
        var snapshot = CreateSnapshot(root, root.GetProperty("input"));

        var summary = CodexUsageSummarizer.Summarize(snapshot);

        AssertSummary(expected, summary);
    }

    [Fact]
    public void Summary_preserves_semantic_labels_when_slots_are_missing()
    {
        using var fixture = ReadFixture("missing-slot-matrix.json");
        var root = fixture.RootElement;
        var inputCases = root.GetProperty("input").GetProperty("cases").EnumerateArray().ToArray();
        var expectedCases = root.GetProperty("expected").GetProperty("cases").EnumerateArray().ToArray();

        Assert.Equal(expectedCases.Length, inputCases.Length);
        for (var caseIndex = 0; caseIndex < inputCases.Length; caseIndex++)
        {
            var inputCase = inputCases[caseIndex];
            var expectedCase = expectedCases[caseIndex];
            Assert.Equal(expectedCase.GetProperty("id").GetString(), inputCase.GetProperty("id").GetString());

            var summary = CodexUsageSummarizer.Summarize(
                CreateSnapshot(root, inputCase.GetProperty("input")));
            var expected = expectedCase.GetProperty("summary");

            AssertSummary(expected, summary);
        }
    }

    [Fact]
    public void Summary_orders_windows_by_semantic_slot_not_snapshot_order()
    {
        using var fixture = ReadFixture("all-windows-order.json");
        var root = fixture.RootElement;
        var input = root.GetProperty("input");
        var expectedWindows = root.GetProperty("expected").GetProperty("windows");
        var snapshot = RawUsageSnapshot.Create(
            new ProviderId("codex"),
            "Codex Fixture",
            null,
            ProviderTimestamp.Parse(root.GetProperty("clock").GetString()!),
            null,
            ReadWindows(input).Reverse(),
            []);

        var summary = CodexUsageSummarizer.Summarize(snapshot);

        Assert.Collection(
            summary.Quota.Windows,
            window => AssertWindow(expectedWindows[0], window),
            window => AssertWindow(expectedWindows[1], window),
            window => AssertWindow(expectedWindows[2], window));
    }

    private static RawUsageSnapshot CreateSnapshot(JsonElement root, JsonElement input) =>
        RawUsageSnapshot.Create(
            new ProviderId("codex"),
            "Codex Fixture",
            null,
            ProviderTimestamp.Parse(root.GetProperty("clock").GetString()!),
            null,
            ReadWindows(input),
            []);

    private static IEnumerable<RawQuotaWindow> ReadWindows(JsonElement input)
    {
        if (input.TryGetProperty("primary", out var primary))
        {
            yield return ReadWindow(RawQuotaWindowSlot.Primary, primary);
        }

        if (input.TryGetProperty("secondary", out var secondary))
        {
            yield return ReadWindow(RawQuotaWindowSlot.Secondary, secondary);
        }

        if (input.TryGetProperty("tertiary", out var tertiary))
        {
            yield return ReadWindow(RawQuotaWindowSlot.Tertiary, tertiary);
        }
    }

    private static RawQuotaWindow ReadWindow(RawQuotaWindowSlot slot, JsonElement element) =>
        new(slot)
        {
            UsedPercent = ReadOptionalDouble(element, "usedPercent"),
            RemainingPercent = ReadOptionalDouble(element, "remainingPercent"),
            ResetAt = ReadOptionalString(element, "resetAt") is { } resetAt
                ? ProviderTimestamp.Parse(resetAt)
                : null,
            ResetDescription = ReadOptionalString(element, "resetDescription"),
        };

    private static void AssertSummary(JsonElement expected, ProviderUsageSummary actual)
    {
        var expectedWindows = expected.GetProperty("windows").EnumerateArray().ToArray();

        Assert.Equal(ReadOptionalDouble(expected, "remainingPercent"), actual.Quota.RemainingPercent);
        Assert.Equal(ReadState(expected), actual.Quota.State);
        Assert.Equal(ReadOptionalString(expected, "nextResetAt"), actual.NextResetAt?.OriginalText);
        Assert.Equal(expectedWindows.Length, actual.Quota.Windows.Length);
        for (var windowIndex = 0; windowIndex < expectedWindows.Length; windowIndex++)
        {
            AssertWindow(expectedWindows[windowIndex], actual.Quota.Windows[windowIndex]);
        }
    }

    private static void AssertWindow(JsonElement expected, NormalizedQuotaWindow actual)
    {
        Assert.Equal(expected.GetProperty("label").GetString(), actual.Label);
        Assert.Equal(ReadOptionalDouble(expected, "remainingPercent"), actual.RemainingPercent);
        Assert.Equal(ReadOptionalDouble(expected, "usedPercent"), actual.UsedPercent);
        Assert.Equal(ReadOptionalString(expected, "resetAt"), actual.ResetAt?.OriginalText);
    }

    private static QuotaWindowState ReadState(JsonElement expected) =>
        expected.GetProperty("state").GetString() switch
        {
            "active" => QuotaWindowState.Active,
            "healthy" => QuotaWindowState.Healthy,
            "watch" => QuotaWindowState.Watch,
            "critical" => QuotaWindowState.Critical,
            var state => throw new InvalidDataException($"Unsupported fixture state '{state}'."),
        };

    private static double? ReadOptionalDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetDouble()
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
            "provider",
            "codex",
            fileName);
    }
}
