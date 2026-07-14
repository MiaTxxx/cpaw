using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

public sealed record ProviderCostPeriod
{
    internal ProviderCostPeriod(double usd, long tokens, string rangeLabel)
    {
        Usd = usd;
        Tokens = tokens;
        RangeLabel = rangeLabel;
    }

    public double Usd { get; }

    public long Tokens { get; }

    public string RangeLabel { get; }
}

public sealed record ProviderCostTimelinePoint
{
    internal ProviderCostTimelinePoint(
        string bucket,
        string label,
        double usd,
        long tokens,
        long? inputTokens,
        long? outputTokens,
        long? cacheReadTokens,
        long? cacheCreateTokens)
    {
        Bucket = bucket;
        Label = label;
        Usd = usd;
        Tokens = tokens;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheReadTokens = cacheReadTokens;
        CacheCreateTokens = cacheCreateTokens;
    }

    public string Bucket { get; }

    public string Label { get; }

    public double Usd { get; }

    public long Tokens { get; }

    public long? InputTokens { get; }

    public long? OutputTokens { get; }

    public long? CacheReadTokens { get; }

    public long? CacheCreateTokens { get; }
}

public sealed record ProviderCostTimeline
{
    internal ProviderCostTimeline(
        ImmutableArray<ProviderCostTimelinePoint> hourly,
        ImmutableArray<ProviderCostTimelinePoint> daily)
    {
        Hourly = hourly;
        Daily = daily;
    }

    public ImmutableArray<ProviderCostTimelinePoint> Hourly { get; }

    public ImmutableArray<ProviderCostTimelinePoint> Daily { get; }
}

public sealed record ProviderModelCost
{
    internal ProviderModelCost(
        string model,
        long totalTokens,
        long inputTokens,
        long outputTokens,
        long cacheReadTokens,
        long cacheCreateTokens,
        double estimatedCostUsd,
        double percentage)
    {
        Model = model;
        TotalTokens = totalTokens;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheReadTokens = cacheReadTokens;
        CacheCreateTokens = cacheCreateTokens;
        EstimatedCostUsd = estimatedCostUsd;
        Percentage = percentage;
    }

    public string Model { get; }

    public long TotalTokens { get; }

    public long InputTokens { get; }

    public long OutputTokens { get; }

    public long CacheReadTokens { get; }

    public long CacheCreateTokens { get; }

    public double EstimatedCostUsd { get; }

    public double Percentage { get; }
}

public sealed record ProviderModelTimeline
{
    internal ProviderModelTimeline(
        string model,
        ImmutableArray<ProviderCostTimelinePoint> hourly,
        ImmutableArray<ProviderCostTimelinePoint> daily)
    {
        Model = model;
        Hourly = hourly;
        Daily = daily;
    }

    public string Model { get; }

    public ImmutableArray<ProviderCostTimelinePoint> Hourly { get; }

    public ImmutableArray<ProviderCostTimelinePoint> Daily { get; }
}

public sealed record ProviderCostSummary
{
    private readonly RawUsageSnapshot sourceSnapshot;

    private ProviderCostSummary(
        RawUsageSnapshot sourceSnapshot,
        ProviderCostPeriod today,
        ProviderCostPeriod week,
        ProviderCostPeriod month,
        ProviderCostPeriod overall,
        ProviderCostTimeline timeline,
        ImmutableArray<ProviderModelCost>? modelBreakdown,
        ImmutableArray<ProviderModelCost>? modelBreakdownToday,
        ImmutableArray<ProviderModelCost>? modelBreakdownWeek,
        ImmutableArray<ProviderModelCost>? modelBreakdownOverall,
        ImmutableArray<ProviderModelTimeline>? modelTimelines,
        ImmutableArray<string>? unpricedModels)
    {
        this.sourceSnapshot = sourceSnapshot;
        Today = today;
        Week = week;
        Month = month;
        Overall = overall;
        Timeline = timeline;
        ModelBreakdown = modelBreakdown;
        ModelBreakdownToday = modelBreakdownToday;
        ModelBreakdownWeek = modelBreakdownWeek;
        ModelBreakdownOverall = modelBreakdownOverall;
        ModelTimelines = modelTimelines;
        UnpricedModels = unpricedModels;
    }

    public ProviderCostPeriod Today { get; }

    public ProviderCostPeriod Week { get; }

    public ProviderCostPeriod Month { get; }

    public ProviderCostPeriod Overall { get; }

    public ProviderCostTimeline Timeline { get; }

    public ImmutableArray<ProviderModelCost>? ModelBreakdown { get; }

    public ImmutableArray<ProviderModelCost>? ModelBreakdownToday { get; }

    public ImmutableArray<ProviderModelCost>? ModelBreakdownWeek { get; }

    public ImmutableArray<ProviderModelCost>? ModelBreakdownOverall { get; }

    public ImmutableArray<ProviderModelTimeline>? ModelTimelines { get; }

    public ImmutableArray<string>? UnpricedModels { get; }

    internal static ProviderCostSummary Create(
        RawUsageSnapshot sourceSnapshot,
        ProviderCostPeriod today,
        ProviderCostPeriod week,
        ProviderCostPeriod month,
        ProviderCostPeriod overall,
        ProviderCostTimeline timeline,
        ImmutableArray<ProviderModelCost>? modelBreakdown,
        ImmutableArray<ProviderModelCost>? modelBreakdownToday,
        ImmutableArray<ProviderModelCost>? modelBreakdownWeek,
        ImmutableArray<ProviderModelCost>? modelBreakdownOverall,
        ImmutableArray<ProviderModelTimeline>? modelTimelines,
        ImmutableArray<string>? unpricedModels)
    {
        ArgumentNullException.ThrowIfNull(sourceSnapshot);
        ArgumentNullException.ThrowIfNull(today);
        ArgumentNullException.ThrowIfNull(week);
        ArgumentNullException.ThrowIfNull(month);
        ArgumentNullException.ThrowIfNull(overall);
        ArgumentNullException.ThrowIfNull(timeline);
        if (unpricedModels is { IsDefault: true })
        {
            throw new ArgumentException("Unpriced model labels must be initialized.", nameof(unpricedModels));
        }

        if (unpricedModels is { } models && models.Any(model => model is null))
        {
            throw new ArgumentException(
                "Unpriced model labels cannot contain null values.",
                nameof(unpricedModels));
        }

        return new ProviderCostSummary(
            sourceSnapshot,
            today,
            week,
            month,
            overall,
            timeline,
            modelBreakdown,
            modelBreakdownToday,
            modelBreakdownWeek,
            modelBreakdownOverall,
            modelTimelines,
            unpricedModels);
    }

    internal bool IsDerivedFrom(RawUsageSnapshot rawSnapshot) =>
        ReferenceEquals(sourceSnapshot, rawSnapshot);
}
