using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.OpenAIChat;

internal static class OpenAIChatJsonElementReader
{
    internal static JsonElement ReadRequiredObject(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("OpenAI wire value is missing a required object.");
        }

        return value;
    }

    internal static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("OpenAI wire value is missing a required string.");
        }

        return value.GetString() ?? throw new JsonException("OpenAI wire value contains an invalid string.");
    }

    internal static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("OpenAI wire value contains an invalid optional string.");
        }

        return value.GetString();
    }
}

internal sealed class OpenAIChatUsageWireJsonConverter : JsonConverter<OpenAIChatUsageWire>
{
    private static readonly HashSet<string> KnownProperties =
    [
        "prompt_tokens",
        "completion_tokens",
        "total_tokens",
        "prompt_cache_hit_tokens",
        "prompt_cache_miss_tokens",
        "prompt_tokens_details",
    ];

    public override OpenAIChatUsageWire Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("OpenAI usage must be a JSON object.");
        }

        var root = document.RootElement;
        return new OpenAIChatUsageWire
        {
            PromptTokens = ReadTolerantInt64(root, "prompt_tokens"),
            CompletionTokens = ReadTolerantInt64(root, "completion_tokens"),
            TotalTokens = ReadTolerantInt64(root, "total_tokens"),
            PromptCacheHitTokens = ReadOptionalInt64(root, "prompt_cache_hit_tokens"),
            PromptCacheMissTokens = ReadOptionalInt64(root, "prompt_cache_miss_tokens"),
            PromptTokensDetails = ReadOptionalObject<OpenAIChatPromptTokensDetailsWire>(
                root,
                "prompt_tokens_details",
                options),
            AdditionalProperties = ReadExtensionData(root, KnownProperties),
        };
    }

    public override void Write(Utf8JsonWriter writer, OpenAIChatUsageWire value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("prompt_tokens", value.PromptTokens);
        writer.WriteNumber("completion_tokens", value.CompletionTokens);
        writer.WriteNumber("total_tokens", value.TotalTokens);
        WriteOptionalNumber(writer, "prompt_cache_hit_tokens", value.PromptCacheHitTokens);
        WriteOptionalNumber(writer, "prompt_cache_miss_tokens", value.PromptCacheMissTokens);
        if (value.PromptTokensDetails is not null)
        {
            writer.WritePropertyName("prompt_tokens_details");
            JsonSerializer.Serialize(writer, value.PromptTokensDetails, options);
        }

        WriteExtensionData(writer, value.AdditionalProperties);
        writer.WriteEndObject();
    }

    internal static long ReadTolerantInt64(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt64(out var number)
            ? number
            : 0;

    internal static long? ReadOptionalInt64(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt64(out var number)
            ? number
            : null;

    internal static T? ReadOptionalObject<T>(JsonElement root, string propertyName, JsonSerializerOptions options)
        where T : class
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return WireJson.Deserialize<T>(value, options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static Dictionary<string, JsonElement>? ReadExtensionData(
        JsonElement root,
        IReadOnlySet<string> knownProperties)
    {
        Dictionary<string, JsonElement>? extensionData = null;
        foreach (var property in root.EnumerateObject())
        {
            if (knownProperties.Contains(property.Name))
            {
                continue;
            }

            extensionData ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extensionData[property.Name] = property.Value.Clone();
        }

        return extensionData;
    }

    internal static void WriteOptionalNumber(Utf8JsonWriter writer, string propertyName, long? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    internal static void WriteExtensionData(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, JsonElement>? extensionData)
    {
        if (extensionData is null)
        {
            return;
        }

        foreach (var property in extensionData)
        {
            writer.WritePropertyName(property.Key);
            property.Value.WriteTo(writer);
        }
    }
}

internal sealed class OpenAIChatStreamChoiceWireJsonConverter : JsonConverter<OpenAIChatStreamChoiceWire>
{
    private static readonly HashSet<string> KnownProperties =
    [
        "index", "delta", "finish_reason", "usage",
    ];

    public override OpenAIChatStreamChoiceWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("OpenAI stream choice must be a JSON object.");
        }

        var root = document.RootElement;
        var delta = OpenAIChatJsonElementReader.ReadRequiredObject(root, "delta");
        return new OpenAIChatStreamChoiceWire
        {
            Index = ReadRequiredInt64(root, "index"),
            Delta = WireJson.Deserialize<OpenAIChatDeltaWire>(delta, options),
            FinishReason = OpenAIChatJsonElementReader.ReadOptionalString(root, "finish_reason"),
            Usage = OpenAIChatUsageWireJsonConverter.ReadOptionalObject<OpenAIChatUsageWire>(
                root,
                "usage",
                options),
            AdditionalProperties = OpenAIChatUsageWireJsonConverter.ReadExtensionData(root, KnownProperties),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OpenAIChatStreamChoiceWire value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("index", value.Index);
        writer.WritePropertyName("delta");
        JsonSerializer.Serialize(writer, value.Delta, options);
        if (value.FinishReason is not null)
        {
            writer.WriteString("finish_reason", value.FinishReason);
        }

        if (value.Usage is not null)
        {
            writer.WritePropertyName("usage");
            JsonSerializer.Serialize(writer, value.Usage, options);
        }

        OpenAIChatUsageWireJsonConverter.WriteExtensionData(writer, value.AdditionalProperties);
        writer.WriteEndObject();
    }

    private static long ReadRequiredInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt64(out var number))
        {
            throw new JsonException("OpenAI stream choice is missing a required integer.");
        }

        return number;
    }

}

internal sealed class OpenAIChatStreamChunkWireJsonConverter : JsonConverter<OpenAIChatStreamChunkWire>
{
    private static readonly HashSet<string> KnownProperties =
    [
        "id", "object", "created", "model", "choices", "usage",
    ];

    public override OpenAIChatStreamChunkWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("OpenAI stream chunk must be a JSON object.");
        }

        var root = document.RootElement;
        return new OpenAIChatStreamChunkWire
        {
            Id = ReadTolerantString(root, "id"),
            Object = ReadTolerantString(root, "object"),
            Created = OpenAIChatUsageWireJsonConverter.ReadTolerantInt64(root, "created"),
            Model = ReadTolerantString(root, "model"),
            Choices = ReadChoices(root, options),
            Usage = OpenAIChatUsageWireJsonConverter.ReadOptionalObject<OpenAIChatUsageWire>(root, "usage", options),
            AdditionalProperties = OpenAIChatUsageWireJsonConverter.ReadExtensionData(root, KnownProperties),
        };
    }

    public override void Write(Utf8JsonWriter writer, OpenAIChatStreamChunkWire value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("object", value.Object);
        writer.WriteNumber("created", value.Created);
        writer.WriteString("model", value.Model);
        writer.WritePropertyName("choices");
        JsonSerializer.Serialize(writer, value.Choices, options);
        if (value.Usage is not null)
        {
            writer.WritePropertyName("usage");
            JsonSerializer.Serialize(writer, value.Usage, options);
        }

        OpenAIChatUsageWireJsonConverter.WriteExtensionData(writer, value.AdditionalProperties);
        writer.WriteEndObject();
    }

    private static string ReadTolerantString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static IReadOnlyList<OpenAIChatStreamChoiceWire> ReadChoices(
        JsonElement root,
        JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("choices", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        try
        {
            return WireJson.Deserialize<IReadOnlyList<OpenAIChatStreamChoiceWire>>(value, options);
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
