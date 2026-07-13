using System.Text.Json;
using AIUsage.Contracts;
using Xunit;

namespace AIUsage.Contracts.Tests;

public sealed class Iso8601TimestampTests
{
    [Theory]
    [InlineData("2026-07-13T01:02:03Z")]
    [InlineData("2026-07-13T01:02:03.1Z")]
    [InlineData("2026-07-13T01:02:03.1234567+08:00")]
    [InlineData("2026-07-13T01:02:03.12345678Z")]
    [InlineData("2026-07-13T01:02:03.123456789012-04:30")]
    [InlineData("2026-07-13T01:02:03-04:30")]
    public void Parse_accepts_strict_wire_formats_and_preserves_text(string text)
    {
        var timestamp = Iso8601Timestamp.Parse(text);

        Assert.Equal(text, timestamp.OriginalText);
        Assert.Equal(text, timestamp.ToString());
        Assert.Equal(text, JsonSerializer.Deserialize<Iso8601Timestamp>(JsonSerializer.Serialize(timestamp))!.OriginalText);
    }

    [Fact]
    public void Value_uses_tick_precision_without_normalizing_the_wire_literal()
    {
        var highPrecision = Iso8601Timestamp.Parse("2026-07-13T01:02:03.123456789Z");
        var tickPrecision = Iso8601Timestamp.Parse("2026-07-13T01:02:03.1234567Z");

        Assert.Equal(tickPrecision.Value, highPrecision.Value);
        Assert.Equal("2026-07-13T01:02:03.123456789Z", highPrecision.OriginalText);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("2026-07-13")]
    [InlineData("2026-07-13 01:02:03Z")]
    [InlineData("2026-07-13T01:02:03")]
    [InlineData("2026-02-30T01:02:03Z")]
    [InlineData("2026-07-13T01:02:03+8:00")]
    public void Parse_rejects_non_contract_values(string text)
    {
        Assert.False(Iso8601Timestamp.TryParse(text, out _));
        Assert.Throws<FormatException>(() => Iso8601Timestamp.Parse(text));
    }

    [Fact]
    public void Json_converter_does_not_echo_invalid_payload()
    {
        const string secret = "credential-secret";

        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Iso8601Timestamp>($"\"{secret}\""));

        Assert.DoesNotContain(secret, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Json_converter_rejects_non_string_tokens_without_echoing_payload()
    {
        const string json = "{\"credential-secret\":true}";

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Iso8601Timestamp>(json));

        Assert.DoesNotContain("credential-secret", exception.ToString(), StringComparison.Ordinal);
    }
}
