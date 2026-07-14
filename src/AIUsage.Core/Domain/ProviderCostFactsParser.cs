using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

internal static class ProviderCostFactsParser
{
    internal static ProviderCostSummary ParseClaude(RawUsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Parse(snapshot, overallRangeLabel: "Overall");
    }

    private static ProviderCostSummary Parse(
        RawUsageSnapshot snapshot,
        string overallRangeLabel)
    {
        var facts = snapshot.Facts;
        return ProviderCostSummary.Create(
            snapshot,
            ReadPeriod(facts, "today", "Today"),
            ReadPeriod(facts, "currentWeek", "This week"),
            ReadPeriod(facts, "currentMonth", "This month"),
            ReadPeriod(facts, "overall", overallRangeLabel, forceDefaultLabel: true),
            new ProviderCostTimeline(
                ReadTimelinePoints(facts, "timeline.hourly"),
                ReadTimelinePoints(facts, "timeline.daily")),
            ReadModelCosts(facts, "currentMonth.models"),
            ReadModelCosts(facts, "today.models"),
            ReadModelCosts(facts, "currentWeek.models"),
            ReadModelCosts(facts, "overall.models"),
            ReadModelTimelines(facts, "timeline.byModel"),
            ReadStringArray(facts, "overall.unpricedModels"));
    }

    private static ProviderCostPeriod ReadPeriod(
        ImmutableDictionary<string, ProviderFact> facts,
        string prefix,
        string fallbackLabel,
        bool forceDefaultLabel = false)
    {
        var label = forceDefaultLabel
            ? fallbackLabel
            : ReadString(facts, $"{prefix}.key") ?? fallbackLabel;
        return new ProviderCostPeriod(
            ReadDouble(facts, $"{prefix}.estimatedCostUsd") ?? 0,
            ReadLong(facts, $"{prefix}.totalTokens") ?? 0,
            label);
    }

    private static ImmutableArray<ProviderCostTimelinePoint> ReadTimelinePoints(
        ImmutableDictionary<string, ProviderFact> facts,
        string key)
    {
        var items = ReadArray(facts, key);
        if (items.IsDefaultOrEmpty)
        {
            return [];
        }

        var points = ImmutableArray.CreateBuilder<ProviderCostTimelinePoint>();
        foreach (var item in items)
        {
            if (item is not ProviderFact.ObjectValue value
                || ReadString(value.Properties, "bucket") is not { } bucket
                || ReadString(value.Properties, "label") is not { } label)
            {
                continue;
            }

            points.Add(new ProviderCostTimelinePoint(
                bucket,
                label,
                ReadDouble(value.Properties, "usd") ?? 0,
                ReadLong(value.Properties, "tokens") ?? 0,
                ReadLong(value.Properties, "inputTokens"),
                ReadLong(value.Properties, "outputTokens"),
                ReadLong(value.Properties, "cacheReadTokens"),
                ReadLong(value.Properties, "cacheCreateTokens")));
        }

        return points.ToImmutable();
    }

    private static ImmutableArray<ProviderModelCost>? ReadModelCosts(
        ImmutableDictionary<string, ProviderFact> facts,
        string key)
    {
        var models = ParseModelCosts(ReadArray(facts, key));
        return models.IsEmpty ? null : models;
    }

    private static ImmutableArray<ProviderModelCost> ParseModelCosts(
        ImmutableArray<ProviderFact> items)
    {
        if (items.IsDefaultOrEmpty)
        {
            return [];
        }

        var models = ImmutableArray.CreateBuilder<ProviderModelCost>();
        foreach (var item in items)
        {
            if (item is not ProviderFact.ObjectValue value
                || ReadString(value.Properties, "model") is not { } model)
            {
                continue;
            }

            models.Add(new ProviderModelCost(
                model,
                ReadLong(value.Properties, "totalTokens") ?? 0,
                ReadLong(value.Properties, "inputTokens") ?? 0,
                ReadLong(value.Properties, "outputTokens") ?? 0,
                ReadLong(value.Properties, "cacheReadTokens") ?? 0,
                ReadLong(value.Properties, "cacheCreateTokens") ?? 0,
                ReadDouble(value.Properties, "estimatedCostUsd") ?? 0,
                ReadDouble(value.Properties, "percentage") ?? 0));
        }

        return models.ToImmutable();
    }

    private static ImmutableArray<ProviderModelTimeline>? ReadModelTimelines(
        ImmutableDictionary<string, ProviderFact> facts,
        string key)
    {
        var items = ReadArray(facts, key);
        if (items.IsDefaultOrEmpty)
        {
            return null;
        }

        var timelines = ImmutableArray.CreateBuilder<ProviderModelTimeline>();
        foreach (var item in items)
        {
            if (item is not ProviderFact.ObjectValue value
                || ReadString(value.Properties, "model") is not { } model)
            {
                continue;
            }

            var hourly = ReadTimelinePoints(value.Properties, "hourly");
            var daily = ReadTimelinePoints(value.Properties, "daily");
            if (hourly.IsDefaultOrEmpty && daily.IsDefaultOrEmpty)
            {
                continue;
            }

            timelines.Add(new ProviderModelTimeline(model, hourly, daily));
        }

        return timelines.Count == 0 ? null : timelines.ToImmutable();
    }

    private static ImmutableArray<string>? ReadStringArray(
        ImmutableDictionary<string, ProviderFact> facts,
        string key)
    {
        var items = ReadArray(facts, key);
        if (items.IsDefaultOrEmpty)
        {
            return null;
        }

        var values = items
            .OfType<ProviderFact.StringValue>()
            .Select(value => value.Value)
            .ToImmutableArray();
        return values.IsEmpty ? null : values;
    }

    private static string? ReadString(
        ImmutableDictionary<string, ProviderFact> facts,
        string key) =>
        facts.TryGetValue(key, out var fact) && fact is ProviderFact.StringValue value
            ? value.Value
            : null;

    private static ImmutableArray<ProviderFact> ReadArray(
        ImmutableDictionary<string, ProviderFact> facts,
        string key) =>
        facts.TryGetValue(key, out var fact) && fact is ProviderFact.ArrayValue array
            ? array.Items
            : [];

    private static double? ReadDouble(
        ImmutableDictionary<string, ProviderFact> facts,
        string key) =>
        facts.TryGetValue(key, out var fact)
            ? ReadDouble(fact)
            : null;

    private static double? ReadDouble(ProviderFact fact) =>
        fact switch
        {
            ProviderFact.NumberValue number => number.Value,
            ProviderFact.IntegerValue integer => integer.Value,
            _ => null,
        };

    private static long? ReadLong(
        ImmutableDictionary<string, ProviderFact> facts,
        string key) =>
        facts.TryGetValue(key, out var fact)
            ? ReadLong(fact)
            : null;

    private static long? ReadLong(ProviderFact fact)
    {
        const double longUpperBoundExclusive = 9_223_372_036_854_775_808d;

        if (fact is ProviderFact.IntegerValue integer)
        {
            return integer.Value;
        }

        if (fact is not ProviderFact.NumberValue number
            || !double.IsFinite(number.Value)
            || number.Value < long.MinValue
            || number.Value >= longUpperBoundExclusive)
        {
            return null;
        }

        return (long)Math.Truncate(number.Value);
    }
}
