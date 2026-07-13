using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.OpenAIResponses;

internal sealed class CodexErrorResponseWireJsonConverter : JsonConverter<CodexErrorResponseWire>
{
    public override CodexErrorResponseWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("error", out var errorElement)
            || errorElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Codex error response requires an error object.");
        }

        var error = JsonSerializer.Deserialize<CodexErrorBodyWire>(errorElement.GetRawText(), options)
            ?? throw new JsonException("Codex error body decoded to null.");
        Dictionary<string, JsonElement>? extensionData = null;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name is "error" or "request_id")
            {
                continue;
            }

            extensionData ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extensionData[property.Name] = property.Value.Clone();
        }

        return new CodexErrorResponseWire
        {
            Error = error,
            AdditionalProperties = extensionData,
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        CodexErrorResponseWire value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("error");
        JsonSerializer.Serialize(writer, value.Error, options);
        if (value.AdditionalProperties is not null)
        {
            foreach (var property in value.AdditionalProperties)
            {
                if (property.Key is "error" or "request_id")
                {
                    continue;
                }

                writer.WritePropertyName(property.Key);
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
