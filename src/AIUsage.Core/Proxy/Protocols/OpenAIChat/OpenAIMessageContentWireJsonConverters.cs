using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.OpenAIChat;

internal sealed class OpenAIMessageContentWireJsonConverter : JsonConverter<OpenAIMessageContentWire>
{
    public override OpenAIMessageContentWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            return new OpenAITextMessageContentWire
            {
                Text = root.GetString() ?? string.Empty,
            };
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("OpenAI message content must be a string or array.");
        }

        var parts = new List<OpenAIContentPartWire>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                throw new JsonException("OpenAI message content parts cannot contain null.");
            }

            parts.Add(element.Deserialize<OpenAIContentPartWire>(options)
                ?? throw new JsonException("OpenAI message content part could not be decoded."));
        }

        return new OpenAIPartsMessageContentWire
        {
            Parts = parts,
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OpenAIMessageContentWire value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case OpenAITextMessageContentWire text:
                writer.WriteStringValue(text.Text);
                return;
            case OpenAIPartsMessageContentWire parts:
                writer.WriteStartArray();
                foreach (var part in parts.Parts)
                {
                    JsonSerializer.Serialize(writer, part, options);
                }

                writer.WriteEndArray();
                return;
            default:
                throw new JsonException("Unsupported OpenAI message content variant.");
        }
    }
}

internal sealed class OpenAIContentPartWireJsonConverter : JsonConverter<OpenAIContentPartWire>
{
    public override OpenAIContentPartWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("OpenAI content part must be an object.");
        }

        var discriminator = OpenAIChatJsonElementReader.ReadRequiredString(root, "type");
        return discriminator switch
        {
            "text" => ReadText(root),
            "image_url" => ReadImageUrl(root),
            "file" or "input_file" => ReadFile(root, discriminator),
            _ => new OpenAIUnknownContentPartWire
            {
                Discriminator = discriminator,
                Value = root.Clone(),
            },
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OpenAIContentPartWire value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case OpenAITextContentPartWire text:
                writer.WriteStartObject();
                writer.WriteString("type", text.Type);
                writer.WriteString("text", text.Text);
                WriteExtensionData(writer, text.AdditionalProperties);
                writer.WriteEndObject();
                return;
            case OpenAIImageUrlContentPartWire image:
                writer.WriteStartObject();
                writer.WriteString("type", image.Type);
                writer.WritePropertyName("image_url");
                writer.WriteStartObject();
                writer.WriteString("url", image.ImageUrl.Url);
                if (image.ImageUrl.Detail is not null)
                {
                    writer.WriteString("detail", image.ImageUrl.Detail);
                }

                WriteExtensionData(writer, image.ImageUrl.AdditionalProperties);
                writer.WriteEndObject();
                WriteExtensionData(writer, image.AdditionalProperties);
                writer.WriteEndObject();
                return;
            case OpenAIFileContentPartWire file:
                writer.WriteStartObject();
                writer.WriteString("type", file.Type);
                writer.WritePropertyName("file");
                writer.WriteStartObject();
                if (file.File.FileId is not null)
                {
                    writer.WriteString("file_id", file.File.FileId);
                }

                if (file.File.Filename is not null)
                {
                    writer.WriteString("filename", file.File.Filename);
                }

                WriteExtensionData(writer, file.File.AdditionalProperties);
                writer.WriteEndObject();
                WriteExtensionData(writer, file.AdditionalProperties);
                writer.WriteEndObject();
                return;
            case OpenAIUnknownContentPartWire unknown:
                if (unknown.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Unknown OpenAI content part must be an object.");
                }

                unknown.Value.WriteTo(writer);
                return;
            default:
                throw new JsonException("Unsupported OpenAI content part variant.");
        }
    }

    private static OpenAITextContentPartWire ReadText(JsonElement root) =>
        new()
        {
            Text = OpenAIChatJsonElementReader.ReadRequiredString(root, "text"),
            AdditionalProperties = ReadExtensionData(root, "type", "text"),
        };

    private static OpenAIImageUrlContentPartWire ReadImageUrl(JsonElement root)
    {
        var imageUrl = OpenAIChatJsonElementReader.ReadRequiredObject(root, "image_url");
        return new OpenAIImageUrlContentPartWire
        {
            ImageUrl = new OpenAIImageUrlWire
            {
                Url = OpenAIChatJsonElementReader.ReadRequiredString(imageUrl, "url"),
                Detail = OpenAIChatJsonElementReader.ReadOptionalString(imageUrl, "detail"),
                AdditionalProperties = ReadExtensionData(imageUrl, "url", "detail"),
            },
            AdditionalProperties = ReadExtensionData(root, "type", "image_url"),
        };
    }

    private static OpenAIFileContentPartWire ReadFile(JsonElement root, string discriminator)
    {
        OpenAIFileDescriptorWire file;
        if (discriminator == "file" && root.TryGetProperty("file", out var nestedFile))
        {
            if (nestedFile.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("OpenAI file content part file must be an object.");
            }

            file = new OpenAIFileDescriptorWire
            {
                FileId = OpenAIChatJsonElementReader.ReadOptionalString(nestedFile, "file_id"),
                Filename = OpenAIChatJsonElementReader.ReadOptionalString(nestedFile, "filename"),
                AdditionalProperties = ReadExtensionData(nestedFile, "file_id", "filename"),
            };
        }
        else
        {
            file = new OpenAIFileDescriptorWire
            {
                FileId = OpenAIChatJsonElementReader.ReadOptionalString(root, "file_id"),
                Filename = OpenAIChatJsonElementReader.ReadOptionalString(root, "filename"),
            };
        }

        return new OpenAIFileContentPartWire
        {
            File = file,
            AdditionalProperties = ReadExtensionData(root, "type", "file", "file_id", "filename"),
        };
    }

    private static Dictionary<string, JsonElement>? ReadExtensionData(
        JsonElement root,
        params string[] knownProperties)
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

    private static void WriteExtensionData(
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
