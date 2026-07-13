using System.Text.Json.Serialization;

namespace AIUsage.Contracts;

public sealed record DashboardSnapshot
{
    [JsonPropertyName("generatedAt")]
    public required string GeneratedAt { get; init; }

    [JsonPropertyName("overview")]
    public required DashboardOverview Overview { get; init; }

    [JsonPropertyName("providers")]
    public required IReadOnlyList<ProviderResult> Providers { get; init; }
}

public sealed record DashboardOverview
{
    [JsonPropertyName("generatedAt")]
    public required string GeneratedAt { get; init; }

    [JsonPropertyName("activeProviders")]
    public required long ActiveProviders { get; init; }

    [JsonPropertyName("attentionProviders")]
    public required long AttentionProviders { get; init; }

    [JsonPropertyName("criticalProviders")]
    public required long CriticalProviders { get; init; }

    [JsonPropertyName("resetSoonProviders")]
    public required long ResetSoonProviders { get; init; }

    [JsonPropertyName("localCostMonthUsd")]
    public required double LocalCostMonthUsd { get; init; }

    [JsonPropertyName("localWeekTokens")]
    public required long LocalWeekTokens { get; init; }

    [JsonPropertyName("stats")]
    public required IReadOnlyList<StatInfo> Stats { get; init; }

    [JsonPropertyName("alerts")]
    public required IReadOnlyList<AlertInfo> Alerts { get; init; }
}

public sealed record StatInfo
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("note")]
    public required string Note { get; init; }
}

public sealed record AlertInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("tone")]
    public required AlertTone Tone { get; init; }

    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
