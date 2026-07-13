using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AIUsage.Core.Proxy.Protocols;

[JsonConverter(typeof(WireIso8601TimestampJsonConverter))]
public sealed record WireIso8601Timestamp
{
    private static readonly Regex WireFormat = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private WireIso8601Timestamp(string originalText, DateTimeOffset value)
    {
        OriginalText = originalText;
        Value = value;
    }

    public string OriginalText { get; }

    public DateTimeOffset Value { get; }

    public static WireIso8601Timestamp Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!TryParse(text, out var timestamp))
        {
            throw new FormatException("The value is not a supported wire ISO 8601 timestamp.");
        }

        return timestamp;
    }

    public static bool TryParse(string? text, out WireIso8601Timestamp timestamp)
    {
        if (text is not null
            && WireFormat.IsMatch(text)
            && DateTimeOffset.TryParseExact(
                NormalizeForDateTimeOffset(text),
                [
                    "yyyy-MM-dd'T'HH:mm:ssK",
                    "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                ],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var value))
        {
            timestamp = new WireIso8601Timestamp(text, value);
            return true;
        }

        timestamp = null!;
        return false;
    }

    private static string NormalizeForDateTimeOffset(string text)
    {
        var decimalPoint = text.IndexOf('.', StringComparison.Ordinal);
        if (decimalPoint < 0)
        {
            return text;
        }

        var offsetStart = text[^1] == 'Z' ? text.Length - 1 : text.Length - 6;
        var fractionalDigits = offsetStart - decimalPoint - 1;
        if (fractionalDigits <= 7)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, decimalPoint + 8), text.AsSpan(offsetStart));
    }

    public override string ToString() => OriginalText;
}

internal sealed class WireIso8601TimestampJsonConverter : JsonConverter<WireIso8601Timestamp>
{
    public override WireIso8601Timestamp Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a wire ISO 8601 timestamp string.");
        }

        var text = reader.GetString();
        if (!WireIso8601Timestamp.TryParse(text, out var timestamp))
        {
            throw new JsonException("Invalid wire ISO 8601 timestamp.");
        }

        return timestamp;
    }

    public override void Write(
        Utf8JsonWriter writer,
        WireIso8601Timestamp value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.OriginalText);
}
