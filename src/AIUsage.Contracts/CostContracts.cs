using System.Text.Json.Serialization;

namespace AIUsage.Contracts;

public sealed record CostSummaryInfo
{
    [JsonPropertyName("today")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostPeriod? Today { get; init; }

    [JsonPropertyName("week")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostPeriod? Week { get; init; }

    [JsonPropertyName("month")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostPeriod? Month { get; init; }

    [JsonPropertyName("overall")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostPeriod? Overall { get; init; }

    [JsonPropertyName("timeline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostTimelineInfo? Timeline { get; init; }

    [JsonPropertyName("modelBreakdown")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelCostInfo>? ModelBreakdown { get; init; }

    [JsonPropertyName("modelBreakdownToday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelCostInfo>? ModelBreakdownToday { get; init; }

    [JsonPropertyName("modelBreakdownWeek")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelCostInfo>? ModelBreakdownWeek { get; init; }

    [JsonPropertyName("modelBreakdownOverall")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelCostInfo>? ModelBreakdownOverall { get; init; }

    [JsonPropertyName("modelTimelines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelTimelineSeries>? ModelTimelines { get; init; }
}

public sealed record ModelCostInfo
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("totalTokens")]
    public required long TotalTokens { get; init; }

    [JsonPropertyName("inputTokens")]
    public required long InputTokens { get; init; }

    [JsonPropertyName("outputTokens")]
    public required long OutputTokens { get; init; }

    [JsonPropertyName("cacheReadTokens")]
    public required long CacheReadTokens { get; init; }

    [JsonPropertyName("cacheCreateTokens")]
    public required long CacheCreateTokens { get; init; }

    [JsonPropertyName("estimatedCostUsd")]
    public required double EstimatedCostUsd { get; init; }

    [JsonPropertyName("percentage")]
    public required double Percentage { get; init; }
}

public sealed record ModelTimelineSeries
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("hourly")]
    public required IReadOnlyList<CostTimelinePoint> Hourly { get; init; }

    [JsonPropertyName("daily")]
    public required IReadOnlyList<CostTimelinePoint> Daily { get; init; }
}

public sealed record CostPeriod
{
    [JsonPropertyName("usd")]
    public required double Usd { get; init; }

    [JsonPropertyName("tokens")]
    public required long Tokens { get; init; }

    [JsonPropertyName("rangeLabel")]
    public required string RangeLabel { get; init; }
}

public sealed record CostTimelineInfo
{
    [JsonPropertyName("hourly")]
    public required IReadOnlyList<CostTimelinePoint> Hourly { get; init; }

    [JsonPropertyName("daily")]
    public required IReadOnlyList<CostTimelinePoint> Daily { get; init; }
}

public sealed record CostTimelinePoint
{
    [JsonPropertyName("bucket")]
    public required string Bucket { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("usd")]
    public required double Usd { get; init; }

    [JsonPropertyName("tokens")]
    public required long Tokens { get; init; }

    [JsonPropertyName("inputTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? InputTokens { get; init; }

    [JsonPropertyName("outputTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? OutputTokens { get; init; }

    [JsonPropertyName("cacheReadTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CacheReadTokens { get; init; }

    [JsonPropertyName("cacheCreateTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CacheCreateTokens { get; init; }
}
