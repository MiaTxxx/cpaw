using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.Anthropic;

internal sealed class ClaudeMessageContentWireJsonConverter : JsonConverter<ClaudeMessageContentWire>
{
    public override ClaudeMessageContentWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            return new ClaudeTextMessageContentWire
            {
                Text = root.GetString()!,
            };
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Claude message content must be a string or array.");
        }

        var blocks = new List<ClaudeContentBlockWire>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                throw new JsonException("Claude message content blocks cannot contain null.");
            }

            blocks.Add(element.Deserialize<ClaudeContentBlockWire>(options)
                ?? throw new JsonException("Claude content block could not be decoded."));
        }

        return new ClaudeBlocksMessageContentWire
        {
            Blocks = blocks,
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ClaudeMessageContentWire value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case ClaudeTextMessageContentWire text:
                writer.WriteStringValue(text.Text);
                return;
            case ClaudeBlocksMessageContentWire blocks:
                writer.WriteStartArray();
                foreach (var block in blocks.Blocks)
                {
                    JsonSerializer.Serialize(writer, block, options);
                }

                writer.WriteEndArray();
                return;
            default:
                throw new JsonException("Unsupported Claude message content variant.");
        }
    }
}

internal sealed class ClaudeContentBlockWireJsonConverter : JsonConverter<ClaudeContentBlockWire>
{
    private static readonly string[] TextProperties = ["type", "text", "cache_control"];
    private static readonly string[] ImageProperties = ["type", "source"];
    private static readonly string[] ImageSourceProperties = ["type", "media_type", "data", "url"];
    private static readonly string[] DocumentProperties =
        ["type", "source", "title", "context", "citations", "cache_control"];
    private static readonly string[] ToolUseProperties = ["type", "id", "name", "input"];
    private static readonly string[] ToolResultProperties =
        ["type", "tool_use_id", "content", "is_error"];
    private static readonly string[] ThinkingProperties = ["type", "thinking", "signature"];
    private static readonly string[] RedactedThinkingProperties = ["type", "data"];

    public override ClaudeContentBlockWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Claude content block must be an object.");
        }

        var discriminator = ReadRequiredString(root, "type");
        return discriminator switch
        {
            "text" => ReadText(root),
            "image" => ReadImage(root),
            "document" => ReadDocument(root),
            "tool_use" => ReadToolUse(root),
            "tool_result" => ReadToolResult(root),
            "thinking" => ReadThinking(root),
            "redacted_thinking" => ReadRedactedThinking(root),
            _ => new ClaudeUnknownContentBlockWire
            {
                Discriminator = discriminator,
                Value = root.Clone(),
            },
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ClaudeContentBlockWire value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case ClaudeTextContentBlockWire text:
                writer.WriteStartObject();
                writer.WriteString("type", text.Type);
                writer.WriteString("text", text.Text);
                WriteOptionalDictionary(writer, "cache_control", text.CacheControl, options);
                WriteExtensionData(writer, text.AdditionalProperties, TextProperties);
                writer.WriteEndObject();
                return;
            case ClaudeImageContentBlockWire image:
                writer.WriteStartObject();
                writer.WriteString("type", image.Type);
                writer.WritePropertyName("source");
                WriteImageSource(writer, image.Source);
                WriteExtensionData(writer, image.AdditionalProperties, ImageProperties);
                writer.WriteEndObject();
                return;
            case ClaudeDocumentContentBlockWire document:
                writer.WriteStartObject();
                writer.WriteString("type", document.Type);
                writer.WritePropertyName("source");
                JsonSerializer.Serialize(writer, document.Source, options);
                WriteOptionalString(writer, "title", document.Title);
                WriteOptionalString(writer, "context", document.Context);
                WriteOptionalElement(writer, "citations", document.Citations);
                WriteOptionalDictionary(writer, "cache_control", document.CacheControl, options);
                WriteExtensionData(writer, document.AdditionalProperties, DocumentProperties);
                writer.WriteEndObject();
                return;
            case ClaudeToolUseContentBlockWire toolUse:
                writer.WriteStartObject();
                writer.WriteString("type", toolUse.Type);
                writer.WriteString("id", toolUse.Id);
                writer.WriteString("name", toolUse.Name);
                writer.WritePropertyName("input");
                JsonSerializer.Serialize(writer, toolUse.Input, options);
                WriteExtensionData(writer, toolUse.AdditionalProperties, ToolUseProperties);
                writer.WriteEndObject();
                return;
            case ClaudeToolResultContentBlockWire toolResult:
                writer.WriteStartObject();
                writer.WriteString("type", toolResult.Type);
                writer.WriteString("tool_use_id", toolResult.ToolUseId);
                WriteOptionalElement(writer, "content", toolResult.Content);
                if (toolResult.IsError is { } isError)
                {
                    writer.WriteBoolean("is_error", isError);
                }

                WriteExtensionData(writer, toolResult.AdditionalProperties, ToolResultProperties);
                writer.WriteEndObject();
                return;
            case ClaudeThinkingContentBlockWire thinking:
                writer.WriteStartObject();
                writer.WriteString("type", thinking.Type);
                writer.WriteString("thinking", thinking.Thinking);
                WriteOptionalString(writer, "signature", thinking.Signature);
                WriteExtensionData(writer, thinking.AdditionalProperties, ThinkingProperties);
                writer.WriteEndObject();
                return;
            case ClaudeRedactedThinkingContentBlockWire redactedThinking:
                writer.WriteStartObject();
                writer.WriteString("type", redactedThinking.Type);
                writer.WriteString("data", redactedThinking.Data);
                WriteExtensionData(
                    writer,
                    redactedThinking.AdditionalProperties,
                    RedactedThinkingProperties);
                writer.WriteEndObject();
                return;
            case ClaudeUnknownContentBlockWire unknown:
                if (unknown.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Unknown Claude content block must be an object.");
                }

                if (!unknown.Value.TryGetProperty("type", out var unknownType)
                    || unknownType.ValueKind != JsonValueKind.String
                    || !StringComparer.Ordinal.Equals(unknownType.GetString(), unknown.Discriminator))
                {
                    throw new JsonException(
                        "Unknown Claude content block discriminator must match its payload type.");
                }

                writer.WriteStartObject();
                writer.WriteString("type", unknown.Discriminator);
                var writtenProperties = new HashSet<string>(StringComparer.Ordinal) { "type" };
                foreach (var property in unknown.Value.EnumerateObject())
                {
                    if (property.NameEquals("type"))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                    writtenProperties.Add(property.Name);
                }

                WriteExtensionData(writer, unknown.AdditionalProperties, writtenProperties);
                writer.WriteEndObject();
                return;
            default:
                throw new JsonException("Unsupported Claude content block variant.");
        }
    }

    private static ClaudeTextContentBlockWire ReadText(JsonElement root) =>
        new()
        {
            Text = ReadRequiredString(root, "text"),
            CacheControl = ReadOptionalDictionary(root, "cache_control"),
            AdditionalProperties = ReadExtensionData(root, TextProperties),
        };

    private static ClaudeImageContentBlockWire ReadImage(JsonElement root)
    {
        var source = ReadRequiredObject(root, "source");
        return new ClaudeImageContentBlockWire
        {
            Source = new ClaudeImageSourceWire
            {
                Type = ReadRequiredString(source, "type"),
                MediaType = ReadOptionalString(source, "media_type"),
                Data = ReadOptionalString(source, "data"),
                Url = ReadOptionalString(source, "url"),
                AdditionalProperties = ReadExtensionData(source, ImageSourceProperties),
            },
            AdditionalProperties = ReadExtensionData(root, ImageProperties),
        };
    }

    private static ClaudeDocumentContentBlockWire ReadDocument(JsonElement root) =>
        new()
        {
            Source = ReadRequiredDictionary(root, "source"),
            Title = ReadOptionalString(root, "title"),
            Context = ReadOptionalString(root, "context"),
            Citations = ReadOptionalElement(root, "citations"),
            CacheControl = ReadOptionalDictionary(root, "cache_control"),
            AdditionalProperties = ReadExtensionData(root, DocumentProperties),
        };

    private static ClaudeToolUseContentBlockWire ReadToolUse(JsonElement root) =>
        new()
        {
            Id = ReadRequiredString(root, "id"),
            Name = ReadRequiredString(root, "name"),
            Input = ReadRequiredDictionary(root, "input"),
            AdditionalProperties = ReadExtensionData(root, ToolUseProperties),
        };

    private static ClaudeToolResultContentBlockWire ReadToolResult(JsonElement root) =>
        new()
        {
            ToolUseId = ReadRequiredString(root, "tool_use_id"),
            Content = ReadOptionalElement(root, "content"),
            IsError = ReadOptionalBoolean(root, "is_error"),
            AdditionalProperties = ReadExtensionData(root, ToolResultProperties),
        };

    private static ClaudeThinkingContentBlockWire ReadThinking(JsonElement root) =>
        new()
        {
            Thinking = ReadRequiredString(root, "thinking"),
            Signature = ReadOptionalString(root, "signature"),
            AdditionalProperties = ReadExtensionData(root, ThinkingProperties),
        };

    private static ClaudeRedactedThinkingContentBlockWire ReadRedactedThinking(JsonElement root) =>
        new()
        {
            Data = ReadRequiredString(root, "data"),
            AdditionalProperties = ReadExtensionData(root, RedactedThinkingProperties),
        };

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Claude content block requires string {propertyName}.");
        }

        return property.GetString()!;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Claude content block {propertyName} must be a string.");
        }

        return property.GetString();
    }

    private static bool? ReadOptionalBoolean(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new JsonException($"Claude content block {propertyName} must be a boolean.");
        }

        return property.GetBoolean();
    }

    private static JsonElement ReadRequiredObject(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Claude content block requires object {propertyName}.");
        }

        return property;
    }

    private static Dictionary<string, JsonElement> ReadRequiredDictionary(
        JsonElement root,
        string propertyName)
    {
        var property = ReadRequiredObject(root, propertyName);
        return property.EnumerateObject().ToDictionary(
            item => item.Name,
            item => item.Value.Clone(),
            StringComparer.Ordinal);
    }

    private static JsonElement? ReadOptionalElement(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.Clone();
    }

    private static Dictionary<string, JsonElement>? ReadOptionalDictionary(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Claude content block {propertyName} must be an object.");
        }

        return property.EnumerateObject().ToDictionary(
            item => item.Name,
            item => item.Value.Clone(),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, JsonElement>? ReadExtensionData(
        JsonElement root,
        IReadOnlyCollection<string> knownProperties)
    {
        Dictionary<string, JsonElement>? extensionData = null;
        foreach (var property in root.EnumerateObject())
        {
            if (knownProperties.Contains(property.Name, StringComparer.Ordinal))
            {
                continue;
            }

            extensionData ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extensionData[property.Name] = property.Value.Clone();
        }

        return extensionData;
    }

    private static void WriteImageSource(Utf8JsonWriter writer, ClaudeImageSourceWire source)
    {
        writer.WriteStartObject();
        writer.WriteString("type", source.Type);
        WriteOptionalString(writer, "media_type", source.MediaType);
        WriteOptionalString(writer, "data", source.Data);
        WriteOptionalString(writer, "url", source.Url);
        WriteExtensionData(writer, source.AdditionalProperties, ImageSourceProperties);
        writer.WriteEndObject();
    }

    private static void WriteOptionalString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is not null)
        {
            writer.WriteString(propertyName, value);
        }
    }

    private static void WriteOptionalElement(
        Utf8JsonWriter writer,
        string propertyName,
        JsonElement? value)
    {
        if (value is not { } element)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        element.WriteTo(writer);
    }

    private static void WriteOptionalDictionary(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyDictionary<string, JsonElement>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, value, options);
    }

    private static void WriteExtensionData(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, JsonElement>? extensionData,
        IEnumerable<string> reservedProperties)
    {
        if (extensionData is null)
        {
            return;
        }

        foreach (var property in extensionData)
        {
            if (reservedProperties.Contains(property.Key, StringComparer.Ordinal))
            {
                continue;
            }

            writer.WritePropertyName(property.Key);
            property.Value.WriteTo(writer);
        }
    }
}
