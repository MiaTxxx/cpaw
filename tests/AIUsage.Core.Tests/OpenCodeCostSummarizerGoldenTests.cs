using System.Text.Json;
using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class OpenCodeCostSummarizerGoldenTests
{
    [Fact]
    public void Summary_preserves_periods_and_overall_range_while_ignoring_account_and_unpriced_models()
    {
        using var fixture = ReadFixture("periods-account-range.json");
        var root = fixture.RootElement;
        var expected = root.GetProperty("expected");
        var summary = OpenCodeCostSummarizer.Summarize(CreateSnapshot(root));
        var actual = Assert.IsType<ProviderCostSummary>(summary.Cost);

        Assert.Null(summary.AccountLabel);
        Assert.Equal(ProviderUsageCategory.LocalCost, summary.Category);
        ProviderCostGoldenTestSupport.AssertCostPeriods(expected.GetProperty("costSummary"), actual);
        Assert.Empty(actual.Timeline.Hourly);
        Assert.Empty(actual.Timeline.Daily);
        Assert.Null(summary.UnpricedModels);
    }

    [Fact]
    public void Summary_preserves_all_model_breakdowns_and_numeric_conversions()
    {
        using var fixture = ReadFixture("model-breakdowns.json");
        var root = fixture.RootElement;
        var expectedCost = root.GetProperty("expected").GetProperty("costSummary");
        var summary = OpenCodeCostSummarizer.Summarize(CreateSnapshot(root));
        var actual = Assert.IsType<ProviderCostSummary>(summary.Cost);

        ProviderCostGoldenTestSupport.AssertCostPeriods(expectedCost, actual);
        Assert.Empty(actual.Timeline.Hourly);
        Assert.Empty(actual.Timeline.Daily);
        ProviderCostGoldenTestSupport.AssertModels(
            expectedCost.GetProperty("modelBreakdown"),
            actual.ModelBreakdown);
        ProviderCostGoldenTestSupport.AssertModels(
            expectedCost.GetProperty("modelBreakdownToday"),
            actual.ModelBreakdownToday);
        Assert.Null(actual.ModelBreakdownWeek);
        ProviderCostGoldenTestSupport.AssertModels(
            expectedCost.GetProperty("modelBreakdownOverall"),
            actual.ModelBreakdownOverall);
        Assert.Null(actual.ModelTimelines);
        Assert.Null(summary.UnpricedModels);
    }

    [Fact]
    public void Summary_filters_timelines_and_defaults_malformed_values()
    {
        using var fixture = ReadFixture("timelines-defaults.json");
        var root = fixture.RootElement;
        var expectedCost = root.GetProperty("expected").GetProperty("costSummary");
        var summary = OpenCodeCostSummarizer.Summarize(CreateSnapshot(root));
        var actual = Assert.IsType<ProviderCostSummary>(summary.Cost);

        Assert.Null(summary.AccountLabel);
        ProviderCostGoldenTestSupport.AssertCostPeriods(expectedCost, actual);
        ProviderCostGoldenTestSupport.AssertTimeline(
            expectedCost.GetProperty("timeline").GetProperty("hourly"),
            actual.Timeline.Hourly);
        ProviderCostGoldenTestSupport.AssertTimeline(
            expectedCost.GetProperty("timeline").GetProperty("daily"),
            actual.Timeline.Daily);
        ProviderCostGoldenTestSupport.AssertModelTimelines(
            expectedCost.GetProperty("modelTimelines"),
            actual.ModelTimelines);
        Assert.Null(actual.ModelBreakdown);
        Assert.Null(actual.ModelBreakdownToday);
        Assert.Null(actual.ModelBreakdownWeek);
        Assert.Null(actual.ModelBreakdownOverall);
        Assert.Null(summary.UnpricedModels);
    }

    private static RawUsageSnapshot CreateSnapshot(JsonElement root) =>
        ProviderCostGoldenTestSupport.CreateSnapshot(
            root,
            new ProviderId("opencode"),
            "OpenCode");

    private static JsonDocument ReadFixture(string fileName) =>
        ProviderCostGoldenTestSupport.ReadFixture("opencode", fileName);
}
