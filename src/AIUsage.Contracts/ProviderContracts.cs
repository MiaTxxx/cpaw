using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Contracts;

public sealed record ProviderResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountId { get; init; }

    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderUsage? Usage { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderSummary? Summary { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}

public sealed record ProviderUsage
{
    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountId { get; init; }

    [JsonPropertyName("fetchedAt")]
    public required Iso8601Timestamp FetchedAt { get; init; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SourceInfo? Source { get; init; }

    [JsonPropertyName("accountEmail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountEmail { get; init; }

    [JsonPropertyName("accountName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountName { get; init; }

    [JsonPropertyName("accountLogin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountLogin { get; init; }

    [JsonPropertyName("accountPlan")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountPlan { get; init; }

    [JsonPropertyName("primary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RawQuotaWindow? Primary { get; init; }

    [JsonPropertyName("secondary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RawQuotaWindow? Secondary { get; init; }

    [JsonPropertyName("tertiary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RawQuotaWindow? Tertiary { get; init; }

    [JsonPropertyName("extra")]
    public required SafeJsonMetadata Extra { get; init; }
}

public sealed record SourceInfo
{
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("browserName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrowserName { get; init; }

    [JsonPropertyName("profile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Profile { get; init; }

    [JsonPropertyName("defaultsDomain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultsDomain { get; init; }

    [JsonPropertyName("roots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Roots { get; init; }

    [JsonPropertyName("envVar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnvVar { get; init; }
}

public sealed record RawQuotaWindow
{
    [JsonPropertyName("usedPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? UsedPercent { get; init; }

    [JsonPropertyName("remainingPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? RemainingPercent { get; init; }

    [JsonPropertyName("resetAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Iso8601Timestamp? ResetAt { get; init; }

    [JsonPropertyName("resetDescription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResetDescription { get; init; }

    [JsonPropertyName("entitlement")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Entitlement { get; init; }

    [JsonPropertyName("remaining")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Remaining { get; init; }

    [JsonPropertyName("unlimited")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Unlimited { get; init; }

    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; init; }
}

public sealed record ProviderSummary
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("channel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Channel { get; init; }

    [JsonPropertyName("status")]
    public required ProviderStatus Status { get; init; }

    [JsonPropertyName("statusLabel")]
    public required string StatusLabel { get; init; }

    [JsonPropertyName("theme")]
    public required ThemeInfo Theme { get; init; }

    [JsonPropertyName("sourceLabel")]
    public required string SourceLabel { get; init; }

    [JsonPropertyName("sourceType")]
    public required string SourceType { get; init; }

    [JsonPropertyName("fetchedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Iso8601Timestamp? FetchedAt { get; init; }

    [JsonPropertyName("accountLabel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountLabel { get; init; }

    [JsonPropertyName("membershipLabel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MembershipLabel { get; init; }

    [JsonPropertyName("workspaceLabel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkspaceLabel { get; init; }

    [JsonPropertyName("remainingPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? RemainingPercent { get; init; }

    [JsonPropertyName("nextResetAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Iso8601Timestamp? NextResetAt { get; init; }

    [JsonPropertyName("nextResetLabel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextResetLabel { get; init; }

    [JsonPropertyName("headline")]
    public required HeadlineInfo Headline { get; init; }

    [JsonPropertyName("metrics")]
    public required IReadOnlyList<MetricInfo> Metrics { get; init; }

    [JsonPropertyName("windows")]
    public required IReadOnlyList<WindowInfo> Windows { get; init; }

    [JsonPropertyName("costSummary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CostSummaryInfo? CostSummary { get; init; }

    [JsonPropertyName("models")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ModelInfo>? Models { get; init; }

    [JsonPropertyName("spotlight")]
    public required string Spotlight { get; init; }

    [JsonPropertyName("unpricedModels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UnpricedModels { get; init; }

    [JsonPropertyName("raw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderUsage? Raw { get; init; }

    [JsonPropertyName("sourceFilePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceFilePath { get; init; }

    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; init; }
}

public sealed record ThemeInfo
{
    [JsonPropertyName("accent")]
    public required string Accent { get; init; }

    [JsonPropertyName("glow")]
    public required string Glow { get; init; }
}

public sealed record HeadlineInfo
{
    [JsonPropertyName("eyebrow")]
    public required string Eyebrow { get; init; }

    [JsonPropertyName("primary")]
    public required string Primary { get; init; }

    [JsonPropertyName("secondary")]
    public required string Secondary { get; init; }

    [JsonPropertyName("supporting")]
    public required string Supporting { get; init; }
}

public sealed record MetricInfo
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; init; }
}

public sealed record WindowInfo
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("remainingPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? RemainingPercent { get; init; }

    [JsonPropertyName("usedPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? UsedPercent { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("note")]
    public required string Note { get; init; }

    [JsonPropertyName("resetAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Iso8601Timestamp? ResetAt { get; init; }
}

public sealed record ModelInfo
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; init; }
}

public sealed record AccountCredentialMetadata
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

    [JsonPropertyName("createdAt")]
    public required Iso8601Timestamp CreatedAt { get; init; }

    [JsonPropertyName("lastUsedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Iso8601Timestamp? LastUsedAt { get; init; }

    [JsonPropertyName("metadata")]
    public required SafeJsonMetadata Metadata { get; init; }
}
