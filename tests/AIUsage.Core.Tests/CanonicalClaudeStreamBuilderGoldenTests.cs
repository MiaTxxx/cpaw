using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeStreamBuilderGoldenTests
{
    [Fact]
    public void Missing_message_id_generates_Claude_style_UUID()
    {
        var output = CanonicalClaudeStreamBuilder.Build(new CanonicalStreamMessageStarted(
            CanonicalRole.Assistant,
            MessageId: null,
            Model: null,
            RawExtensions: []));

        var messageStart = Assert.IsType<ClaudeMessageStartEventWire>(Assert.Single(output));
        Assert.True(Guid.TryParseExact(messageStart.Message.Id, "D", out _));
        Assert.Equal(messageStart.Message.Id.ToUpperInvariant(), messageStart.Message.Id);
        Assert.Equal("claude", messageStart.Message.Model);
    }

    [Theory]
    [InlineData("rich-lifecycle.json")]
    [InlineData("boundary-events.json")]
    public void Canonical_stream_events_match_swift_Claude_builder(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "stream",
            "claude-builder",
            fixtureName);
        var root = fixture.RootElement;
        var actual = root.GetProperty("input").GetProperty("events")
            .EnumerateArray()
            .SelectMany(streamEvent => CanonicalClaudeStreamBuilder.Build(Parse(streamEvent)))
            .ToArray();
        var projected = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["events"] = actual,
        });

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }

    private static CanonicalStreamEvent Parse(JsonElement streamEvent)
    {
        var rawExtensions = ParseExtensions(streamEvent);
        return streamEvent.GetProperty("type").GetString() switch
        {
            "message_started" => new CanonicalStreamMessageStarted(
                new CanonicalRole(streamEvent.GetProperty("role").GetString()!),
                ReadOptionalString(streamEvent, "messageID"),
                ReadOptionalString(streamEvent, "model"),
                rawExtensions),
            "content_part_started" => new CanonicalStreamContentPartStarted(
                streamEvent.GetProperty("index").GetInt64(),
                new CanonicalStreamPartKind(streamEvent.GetProperty("kind").GetString()!),
                ReadOptionalString(streamEvent, "toolCallID"),
                ReadOptionalString(streamEvent, "toolName"),
                rawExtensions),
            "content_part_delta" => new CanonicalStreamContentPartDelta(
                streamEvent.GetProperty("index").GetInt64(),
                new CanonicalStreamPartKind(streamEvent.GetProperty("kind").GetString()!),
                ReadOptionalString(streamEvent, "textDelta"),
                ReadOptionalString(streamEvent, "jsonDelta"),
                rawExtensions),
            "content_part_stopped" => new CanonicalStreamContentPartStopped(
                streamEvent.GetProperty("index").GetInt64()),
            "message_delta" => new CanonicalStreamMessageDelta(
                ParseStop(streamEvent),
                ParseUsage(streamEvent),
                rawExtensions),
            "message_stopped" => new CanonicalStreamMessageStopped(),
            "error" => new CanonicalStreamError(
                streamEvent.GetProperty("message").GetString()!,
                rawExtensions),
            var type => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical stream fixture event {type}."),
        };
    }

    private static CanonicalStop? ParseStop(JsonElement streamEvent)
    {
        if (!streamEvent.TryGetProperty("stop", out var stop)
            || stop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return new CanonicalStop(
            new CanonicalStopReason(stop.GetProperty("reason").GetString()!),
            ReadOptionalString(stop, "sequence"));
    }

    private static CanonicalUsage? ParseUsage(JsonElement streamEvent)
    {
        if (!streamEvent.TryGetProperty("usage", out var usage)
            || usage.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return new CanonicalUsage(
            ReadOptionalInt64(usage, "inputTokens"),
            ReadOptionalInt64(usage, "outputTokens"),
            ReadOptionalInt64(usage, "totalTokens"),
            ReadOptionalInt64(usage, "cacheCreationInputTokens"),
            ReadOptionalInt64(usage, "cacheReadInputTokens"),
            ReadOptionalInt64(usage, "reasoningTokens"));
    }

    private static ImmutableArray<CanonicalVendorExtension> ParseExtensions(
        JsonElement streamEvent)
    {
        if (!streamEvent.TryGetProperty("rawExtensions", out var extensions))
        {
            return [];
        }

        return extensions.EnumerateArray()
            .Select(extension => new CanonicalVendorExtension(
                extension.GetProperty("vendor").GetString()!,
                extension.GetProperty("key").GetString()!,
                extension.GetProperty("value")))
            .ToImmutableArray();
    }

    private static string? ReadOptionalString(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    private static long? ReadOptionalInt64(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.GetInt64()
            : null;
}
