using System.Globalization;
using System.Text.RegularExpressions;

namespace AIUsage.Core.Domain;

public sealed record ProviderTimestamp
{
    private static readonly Regex WireFormat = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private ProviderTimestamp(string originalText, DateTimeOffset value)
    {
        OriginalText = originalText;
        Value = value;
    }

    public string OriginalText { get; }

    public DateTimeOffset Value { get; }

    public static ProviderTimestamp Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!TryParse(text, out var timestamp))
        {
            throw new FormatException("The value is not a supported provider timestamp.");
        }

        return timestamp;
    }

    public static bool TryParse(string? text, out ProviderTimestamp timestamp)
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
            timestamp = new ProviderTimestamp(text, value);
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
}
