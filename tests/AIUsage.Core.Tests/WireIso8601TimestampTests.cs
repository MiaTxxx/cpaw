using System.Text.Json;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class WireIso8601TimestampTests
{
    [Theory]
    [InlineData("2030-01-02T03:04:05Z")]
    [InlineData("2030-01-02T11:04:05+08:00")]
    [InlineData("2030-01-02T03:04:05.123456789012Z")]
    public void Parse_preserves_valid_wire_literals(string text)
    {
        var timestamp = WireIso8601Timestamp.Parse(text);

        Assert.Equal(text, timestamp.OriginalText);
        Assert.Equal(text, timestamp.ToString());
        Assert.Equal(
            text,
            JsonSerializer.Deserialize<WireIso8601Timestamp>(JsonSerializer.Serialize(timestamp))!.OriginalText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2030-01-02")]
    [InlineData("2030-01-02 03:04:05Z")]
    [InlineData("2030-01-02T03:04:05")]
    [InlineData("2030-02-30T03:04:05Z")]
    public void Parse_rejects_values_without_the_contract_time_shape(string text)
    {
        Assert.False(WireIso8601Timestamp.TryParse(text, out _));
        Assert.Throws<FormatException>(() => WireIso8601Timestamp.Parse(text));
    }

    [Fact]
    public void Claude_file_wire_rejects_unzoned_created_at_without_echoing_payload()
    {
        const string secret = "fixture-secret-created-at";
        var json = $$"""
            {
              "id":"file_fixture",
              "type":"file",
              "filename":"fixture.txt",
              "mime_type":"text/plain",
              "size_bytes":1,
              "created_at":"{{secret}}",
              "downloadable":true
            }
            """;

        var exception = Assert.Throws<WireJsonException>(
            () => WireJson.Deserialize<ClaudeFileObjectWire>(json));

        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(secret, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void High_precision_value_uses_ticks_without_normalizing_the_wire_literal()
    {
        var highPrecision = WireIso8601Timestamp.Parse("2030-01-02T03:04:05.123456789Z");
        var tickPrecision = WireIso8601Timestamp.Parse("2030-01-02T03:04:05.1234567Z");

        Assert.Equal(tickPrecision.Value, highPrecision.Value);
        Assert.Equal("2030-01-02T03:04:05.123456789Z", highPrecision.OriginalText);
    }
}
