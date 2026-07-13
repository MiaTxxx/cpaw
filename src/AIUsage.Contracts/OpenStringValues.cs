using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Contracts;

[JsonConverter(typeof(ProviderStatusJsonConverter))]
public sealed record ProviderStatus
{
    public ProviderStatus(string value)
    {
        Value = RequireNonEmpty(value, nameof(ProviderStatus));
    }

    public string Value { get; }

    public static readonly ProviderStatus Healthy = new("healthy");
    public static readonly ProviderStatus Watch = new("watch");
    public static readonly ProviderStatus Critical = new("critical");
    public static readonly ProviderStatus Error = new("error");
    public static readonly ProviderStatus Idle = new("idle");
    public static readonly ProviderStatus Tracking = new("tracking");

    public bool IsKnown => Value is "healthy" or "watch" or "critical" or "error" or "idle" or "tracking";

    public override string ToString() => Value;

    private static string RequireNonEmpty(string? value, string typeName) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException($"{typeName} cannot be empty.", nameof(value));
}

[JsonConverter(typeof(AlertToneJsonConverter))]
public sealed record AlertTone
{
    public AlertTone(string value)
    {
        Value = !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("AlertTone cannot be empty.", nameof(value));
    }

    public string Value { get; }

    public static readonly AlertTone Critical = new("critical");
    public static readonly AlertTone Watch = new("watch");
    public static readonly AlertTone Neutral = new("neutral");

    public bool IsKnown => Value is "critical" or "watch" or "neutral";

    public override string ToString() => Value;
}

internal sealed class ProviderStatusJsonConverter : JsonConverter<ProviderStatus>
{
    public override ProviderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(ReadNonEmptyString(ref reader, nameof(ProviderStatus)));

    public override void Write(Utf8JsonWriter writer, ProviderStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);

    private static string ReadNonEmptyString(ref Utf8JsonReader reader, string typeName) =>
        reader.TokenType == JsonTokenType.String
            ? RequireNonEmpty(reader.GetString(), typeName)
            : throw new JsonException($"{typeName} must be a JSON string.");

    private static string RequireNonEmpty(string? value, string typeName) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new JsonException($"{typeName} cannot be empty.");
}

internal sealed class AlertToneJsonConverter : JsonConverter<AlertTone>
{
    public override AlertTone Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || string.IsNullOrWhiteSpace(reader.GetString()))
        {
            throw new JsonException("AlertTone must be a non-empty JSON string.");
        }

        return new AlertTone(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, AlertTone value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
