using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.Anthropic;

public sealed record ClaudeMessageRequestWire
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<ClaudeMessageWire> Messages { get; init; }

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? System { get; init; }

    [JsonPropertyName("max_tokens")]
    public required long MaxTokens { get; init; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; init; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; init; }

    [JsonPropertyName("top_k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TopK { get; init; }

    [JsonPropertyName("stop_sequences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StopSequences { get; init; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; init; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ClaudeToolWire>? Tools { get; init; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeToolChoiceWire? ToolChoice { get; init; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeMetadataWire? Metadata { get; init; }

    [JsonPropertyName("thinking")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeThinkingConfigWire? Thinking { get; init; }

    [JsonPropertyName("output_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeOutputConfigWire? OutputConfig { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeMessageWire
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required ClaudeMessageContentWire Content { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeToolWire
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("input_schema")]
    public required Dictionary<string, JsonElement> InputSchema { get; init; }

    [JsonPropertyName("eager_input_streaming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EagerInputStreaming { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeToolChoiceWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("disable_parallel_tool_use")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DisableParallelToolUse { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeMetadataWire
{
    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeThinkingConfigWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("budget_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? BudgetTokens { get; init; }

    [JsonPropertyName("display")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Display { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeOutputConfigWire
{
    [JsonPropertyName("effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Effort { get; init; }

    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeOutputFormatWire? Format { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeOutputFormatWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Schema { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeTokenCountRequestWire
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<ClaudeMessageWire> Messages { get; init; }

    // Anthropic accepts either a string or an array of structured system blocks.
    // Keeping the JSON value intact mirrors Swift's decode/encode shape preservation.
    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? System { get; init; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ClaudeToolWire>? Tools { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeTokenCountResponseWire
{
    [JsonPropertyName("input_tokens")]
    public required long InputTokens { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeMessageResponseWire
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required IReadOnlyList<ClaudeContentBlockWire> Content { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("stop_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopReason { get; init; }

    [JsonPropertyName("stop_sequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopSequence { get; init; }

    [JsonPropertyName("usage")]
    public required ClaudeUsageWire Usage { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeUsageWire
{
    [JsonPropertyName("input_tokens")]
    public required long InputTokens { get; init; }

    [JsonPropertyName("output_tokens")]
    public required long OutputTokens { get; init; }

    [JsonPropertyName("cache_creation_input_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CacheCreationInputTokens { get; init; }

    [JsonPropertyName("cache_read_input_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CacheReadInputTokens { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

[JsonConverter(typeof(ClaudeStreamEventWireJsonConverter))]
public abstract record ClaudeStreamEventWire;

public sealed record ClaudeContentBlockDeltaEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("index")]
    public required long Index { get; init; }

    [JsonPropertyName("delta")]
    public required ClaudeContentDeltaWire Delta { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "content_block_delta");
}

public sealed record ClaudeContentDeltaWire : IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("partial_json")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartialJson { get; init; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    [JsonPropertyName("thinking")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thinking { get; init; }

    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; init; }

    [JsonPropertyName("citation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Citation { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        var missingProperty = Type switch
        {
            "text_delta" when Text is null => "text",
            "input_json_delta" when PartialJson is null => "partial_json",
            "thinking_delta" when Thinking is null => "thinking",
            "signature_delta" when Signature is null => "signature",
            _ => null,
        };

        if (missingProperty is not null)
        {
            throw new JsonException($"Claude {Type} requires {missingProperty}.");
        }
    }
}

public sealed record ClaudeMessageStartEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required ClaudeMessageStartWire Message { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "message_start");
}

public sealed record ClaudeMessageStartWire
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required IReadOnlyList<ClaudeContentBlockWire> Content { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("stop_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopReason { get; init; }

    [JsonPropertyName("stop_sequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopSequence { get; init; }

    [JsonPropertyName("usage")]
    public required ClaudeUsageWire Usage { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

[JsonConverter(typeof(ClaudeMessageContentWireJsonConverter))]
public abstract record ClaudeMessageContentWire;

public sealed record ClaudeTextMessageContentWire : ClaudeMessageContentWire
{
    public required string Text { get; init; }
}

public sealed record ClaudeBlocksMessageContentWire : ClaudeMessageContentWire
{
    public required IReadOnlyList<ClaudeContentBlockWire> Blocks { get; init; }
}

[JsonConverter(typeof(ClaudeContentBlockWireJsonConverter))]
public abstract record ClaudeContentBlockWire
{
    [JsonIgnore]
    public abstract string Type { get; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeTextContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "text";

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("cache_control")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? CacheControl { get; init; }

}

public sealed record ClaudeImageContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "image";

    [JsonPropertyName("source")]
    public required ClaudeImageSourceWire Source { get; init; }

}

public sealed record ClaudeImageSourceWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("media_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MediaType { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; init; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeDocumentContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "document";

    [JsonPropertyName("source")]
    public required Dictionary<string, JsonElement> Source { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; init; }

    [JsonPropertyName("citations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Citations { get; init; }

    [JsonPropertyName("cache_control")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? CacheControl { get; init; }

}

public sealed record ClaudeToolUseContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "tool_use";

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("input")]
    public required Dictionary<string, JsonElement> Input { get; init; }

}

public sealed record ClaudeToolResultContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "tool_result";

    [JsonPropertyName("tool_use_id")]
    public required string ToolUseId { get; init; }

    // Kept raw until the malformed tool_result behavior is explicitly decided.
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Content { get; init; }

    [JsonPropertyName("is_error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsError { get; init; }

}

public sealed record ClaudeThinkingContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "thinking";

    [JsonPropertyName("thinking")]
    public required string Thinking { get; init; }

    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; init; }

}

public sealed record ClaudeRedactedThinkingContentBlockWire : ClaudeContentBlockWire
{
    public override string Type => "redacted_thinking";

    [JsonPropertyName("data")]
    public required string Data { get; init; }
}

public sealed record ClaudeUnknownContentBlockWire : ClaudeContentBlockWire
{
    [JsonIgnore]
    public required string Discriminator { get; init; }

    public override string Type => Discriminator;

    [JsonIgnore]
    public required JsonElement Value { get; init; }
}

public sealed record ClaudeContentBlockStartEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("index")]
    public required long Index { get; init; }

    [JsonPropertyName("content_block")]
    public required ClaudeContentBlockWire ContentBlock { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "content_block_start");
}

public sealed record ClaudeContentBlockStopEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("index")]
    public required long Index { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "content_block_stop");
}

public sealed record ClaudeMessageDeltaEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("delta")]
    public required ClaudeMessageDeltaContentWire Delta { get; init; }

    [JsonPropertyName("usage")]
    public required ClaudeUsageDeltaWire Usage { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "message_delta");
}

public sealed record ClaudeMessageDeltaContentWire
{
    [JsonPropertyName("stop_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopReason { get; init; }

    [JsonPropertyName("stop_sequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopSequence { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeUsageDeltaWire
{
    [JsonPropertyName("output_tokens")]
    public required long OutputTokens { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeMessageStopEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "message_stop");
}

public sealed record ClaudePingEventWire : ClaudeStreamEventWire, IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        ClaudeWireDiscriminator.Require(Type, "ping");
}

public sealed record ClaudeFileScopeWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeFileObjectWire
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    [JsonPropertyName("mime_type")]
    public required string MimeType { get; init; }

    [JsonPropertyName("size_bytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("created_at")]
    public required WireIso8601Timestamp CreatedAt { get; init; }

    [JsonPropertyName("downloadable")]
    public required bool Downloadable { get; init; }

    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClaudeFileScopeWire? Scope { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeFilesListResponseWire
{
    [JsonPropertyName("data")]
    public required IReadOnlyList<ClaudeFileObjectWire> Data { get; init; }

    [JsonPropertyName("has_more")]
    public required bool HasMore { get; init; }

    [JsonPropertyName("first_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstId { get; init; }

    [JsonPropertyName("last_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastId { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeDeletedFileResponseWire
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("deleted")]
    public required bool Deleted { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeErrorResponseWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("error")]
    public required ClaudeErrorWire Error { get; init; }

    [JsonPropertyName("request_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestId { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ClaudeErrorWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

internal static class ClaudeWireDiscriminator
{
    public static void Require(string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new JsonException($"Expected Claude {expected} event.");
        }
    }
}
