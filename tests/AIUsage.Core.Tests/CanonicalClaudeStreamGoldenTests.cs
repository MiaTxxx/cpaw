using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeStreamGoldenTests
{
    [Fact]
    public void Claude_stream_event_union_round_trips_runtime_variant()
    {
        const string json = """
            {"type":"message_stop","future_marker":true}
            """;
        var streamEvent = WireJson.Deserialize<ClaudeStreamEventWire>(json);

        var encoded = JsonSerializer.Serialize(streamEvent);
        var roundTripped = WireJson.Deserialize<ClaudeStreamEventWire>(encoded);

        var messageStop = Assert.IsType<ClaudeMessageStopEventWire>(roundTripped);
        Assert.True(messageStop.AdditionalProperties?["future_marker"].GetBoolean());
    }

    [Theory]
    [InlineData("rich-lifecycle.json")]
    [InlineData("boundary-events.json")]
    public void Claude_stream_events_match_swift_canonical_projection(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "stream",
            "claude",
            fixtureName);
        var root = fixture.RootElement;
        var actual = root.GetProperty("input").GetProperty("events")
            .EnumerateArray()
            .SelectMany(streamEvent => CanonicalClaudeStreamMapper.Map(
                WireJson.Deserialize<ClaudeStreamEventWire>(streamEvent)))
            .ToArray();
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
