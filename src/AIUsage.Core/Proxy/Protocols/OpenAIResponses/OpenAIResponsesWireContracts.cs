using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.OpenAIResponses;

public sealed record OpenAIResponsesRequestWire
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required IReadOnlyList<JsonElement> Input { get; init; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; init; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; init; }

    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaxOutputTokens { get; init; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; init; }

    [JsonPropertyName("store")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Store { get; init; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<JsonElement>? Tools { get; init; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ToolChoice { get; init; }

    [JsonPropertyName("parallel_tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ParallelToolCalls { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesResponseWire
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("object")]
    public required string Object { get; init; }

    [JsonPropertyName("created_at")]
    public required long CreatedAt { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("output")]
    public required IReadOnlyList<JsonElement> Output { get; init; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; init; }

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIResponsesUsageWire? Usage { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesUsageWire
{
    [JsonPropertyName("input_tokens")]
    public required long InputTokens { get; init; }

    [JsonPropertyName("output_tokens")]
    public required long OutputTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public required long TotalTokens { get; init; }

    [JsonPropertyName("input_tokens_details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIResponsesInputTokensDetailsWire? InputTokensDetails { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesInputTokensDetailsWire
{
    [JsonPropertyName("cached_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CachedTokens { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesCompletedEventWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("response")]
    public required OpenAIResponsesResponseWire Response { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesOutputItemAddedEventWire : IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("item")]
    public required JsonElement Item { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        OpenAIResponsesStreamWireValidation.RequireObject(Item, "item");
}

public sealed record OpenAIResponsesOutputItemDoneEventWire : IJsonOnDeserialized
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("item")]
    public required JsonElement Item { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    void IJsonOnDeserialized.OnDeserialized() =>
        OpenAIResponsesStreamWireValidation.RequireObject(Item, "item");
}

public sealed record OpenAIResponsesOutputTextDeltaEventWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("item_id")]
    public required string ItemId { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("content_index")]
    public required long ContentIndex { get; init; }

    [JsonPropertyName("delta")]
    public required string Delta { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesReasoningSummaryTextDeltaEventWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("item_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemId { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("summary_index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? SummaryIndex { get; init; }

    [JsonPropertyName("delta")]
    public required string Delta { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesFunctionCallArgumentsDeltaEventWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("item_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemId { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("delta")]
    public required string Delta { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesFunctionCallArgumentsDoneEventWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("item_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemId { get; init; }

    [JsonPropertyName("output_index")]
    public required long OutputIndex { get; init; }

    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arguments { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("item")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIResponsesFunctionCallWire? Item { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record OpenAIResponsesFunctionCallWire
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonPropertyName("call_id")]
    public required string CallId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("arguments")]
    public required string Arguments { get; init; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; init; }

    [JsonPropertyName("created_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedBy { get; init; }

    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

internal static class OpenAIResponsesStreamWireValidation
{
    public static void RequireObject(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"OpenAI Responses stream requires object {propertyName}.");
        }
    }
}

[JsonConverter(typeof(CodexErrorResponseWireJsonConverter))]
public sealed record CodexErrorResponseWire
{
    [JsonPropertyName("error")]
    public required CodexErrorBodyWire Error { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record CodexErrorBodyWire
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
