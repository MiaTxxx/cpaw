using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols.Anthropic;

internal sealed class ClaudeStreamEventWireJsonConverter : JsonConverter<ClaudeStreamEventWire>
{
    public override ClaudeStreamEventWire Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("type", out var typeProperty)
            || typeProperty.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Claude stream event requires string type.");
        }

        return typeProperty.GetString() switch
        {
            "message_start" => Deserialize<ClaudeMessageStartEventWire>(root, options),
            "content_block_start" => Deserialize<ClaudeContentBlockStartEventWire>(root, options),
            "content_block_delta" => Deserialize<ClaudeContentBlockDeltaEventWire>(root, options),
            "content_block_stop" => Deserialize<ClaudeContentBlockStopEventWire>(root, options),
            "message_delta" => Deserialize<ClaudeMessageDeltaEventWire>(root, options),
            "message_stop" => Deserialize<ClaudeMessageStopEventWire>(root, options),
            "ping" => Deserialize<ClaudePingEventWire>(root, options),
            var discriminator => throw new JsonException(
                $"Unsupported Claude stream event type {discriminator}."),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ClaudeStreamEventWire value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case ClaudeMessageStartEventWire messageStart:
                JsonSerializer.Serialize(writer, messageStart, options);
                return;
            case ClaudeContentBlockStartEventWire blockStart:
                JsonSerializer.Serialize(writer, blockStart, options);
                return;
            case ClaudeContentBlockDeltaEventWire blockDelta:
                JsonSerializer.Serialize(writer, blockDelta, options);
                return;
            case ClaudeContentBlockStopEventWire blockStop:
                JsonSerializer.Serialize(writer, blockStop, options);
                return;
            case ClaudeMessageDeltaEventWire messageDelta:
                JsonSerializer.Serialize(writer, messageDelta, options);
                return;
            case ClaudeMessageStopEventWire messageStop:
                JsonSerializer.Serialize(writer, messageStop, options);
                return;
            case ClaudePingEventWire ping:
                JsonSerializer.Serialize(writer, ping, options);
                return;
            default:
                throw new JsonException("Unsupported Claude stream event variant.");
        }
    }

    private static T Deserialize<T>(JsonElement root, JsonSerializerOptions options)
        where T : ClaudeStreamEventWire =>
        root.Deserialize<T>(options)
        ?? throw new JsonException("Claude stream event could not be decoded.");
}
