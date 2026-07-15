using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeResponseBuilderGoldenTests
{
    [Fact]
    public void Missing_response_identity_uses_Claude_defaults()
    {
        var result = CanonicalClaudeResponseBuilder.BuildMessageResponse(new CanonicalResponse(
            Id: null,
            Model: null,
            Items: [],
            Stop: new CanonicalStop(CanonicalStopReason.EndTurn, Sequence: null),
            Usage: null,
            RawExtensions: []));

        Assert.True(Guid.TryParseExact(result.Payload.Id, "D", out _));
        Assert.Equal(result.Payload.Id.ToUpperInvariant(), result.Payload.Id);
        Assert.Equal("claude", result.Payload.Model);
        Assert.Equal("assistant", result.Payload.Role);
        Assert.Equal("message", result.Payload.Type);
        var content = Assert.IsType<ClaudeTextContentBlockWire>(Assert.Single(result.Payload.Content));
        Assert.Empty(content.Text);
        Assert.Equal(0, result.Payload.Usage.InputTokens);
        Assert.Equal(0, result.Payload.Usage.OutputTokens);
        Assert.Empty(result.LossyNotes);
    }

    [Theory]
    [InlineData("rich-content.json")]
    [InlineData("boundary-lossy.json")]
    public void Canonical_response_matches_swift_Claude_builder(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "response",
            "claude-builder",
            fixtureName);
        var root = fixture.RootElement;
        var input = root.GetProperty("input");
        var response = ParseResponse(input.GetProperty("response"));
        var originalModel = ReadOptionalString(input, "originalModel");

        var actual = CanonicalClaudeResponseBuilder.BuildMessageResponse(
            response,
            originalModel);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }

    private static CanonicalResponse ParseResponse(JsonElement value) =>
        new(
            ReadOptionalString(value, "id"),
            ReadOptionalString(value, "model"),
            value.GetProperty("items").EnumerateArray()
                .Select(ParseItem)
                .ToImmutableArray(),
            ParseStop(value.GetProperty("stop")),
            value.TryGetProperty("usage", out var usage)
                ? ParseUsage(usage)
                : null,
            ParseExtensions(value));

    private static CanonicalConversationItem ParseItem(JsonElement value) =>
        value.GetProperty("type").GetString() switch
        {
            "message" => ParseMessage(value),
            "tool_call" => new CanonicalToolCall(
                value.GetProperty("id").GetString()!,
                value.GetProperty("name").GetString()!,
                value.GetProperty("inputJSON").GetString()!,
                new CanonicalItemStatus(value.GetProperty("status").GetString()!),
                value.GetProperty("partial").GetBoolean(),
                ParseExtensions(value)),
            "tool_result" => new CanonicalToolResult(
                value.GetProperty("toolCallID").GetString()!,
                ReadOptionalBoolean(value, "isError"),
                ParseParts(value),
                ReadOptionalString(value, "rawTextFallback"),
                ParseExtensions(value)),
            "reasoning" => new CanonicalReasoningItem(
                ReadOptionalString(value, "summaryText"),
                ReadOptionalString(value, "fullText"),
                ReadOptionalString(value, "encryptedContent"),
                ReadOptionalString(value, "signature"),
                ReadOptionalBoolean(value, "redacted"),
                ParseExtensions(value)),
            "compaction" => new CanonicalCompactionItem(
                ReadOptionalString(value, "id"),
                ReadOptionalString(value, "encryptedContent"),
                ParseExtensions(value)),
            "hosted_tool_event" => new CanonicalHostedToolEvent(
                value.GetProperty("vendorType").GetString()!,
                ReadOptionalString(value, "callID"),
                new CanonicalItemStatus(value.GetProperty("status").GetString()!),
                ReadOptionalElement(value, "payload"),
                ParseExtensions(value)),
            var type => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical response fixture item {type}."),
        };

    private static CanonicalMessage ParseMessage(JsonElement value) =>
        new(
            new CanonicalRole(value.GetProperty("role").GetString()!),
            ReadOptionalString(value, "phase"),
            ParseParts(value),
            ReadOptionalString(value, "name"),
            ParseDictionary(value.GetProperty("metadata")),
            ParseExtensions(value));

    private static ImmutableArray<CanonicalContentPart> ParseParts(JsonElement value) =>
        value.GetProperty("parts").EnumerateArray()
            .Select(ParsePart)
            .ToImmutableArray();

    private static CanonicalContentPart ParsePart(JsonElement value) =>
        value.GetProperty("type").GetString() switch
        {
            "text" => new CanonicalTextPart(
                value.GetProperty("text").GetString()!,
                ParseExtensions(value)),
            "image" => new CanonicalImagePart(
                new CanonicalImageSource(value.GetProperty("source").GetString()!),
                value.GetProperty("data").GetString()!,
                ReadOptionalString(value, "mediaType"),
                ReadOptionalString(value, "detail"),
                ParseExtensions(value)),
            "document" => new CanonicalDocumentPart(
                ParseDocumentSource(value.GetProperty("source")),
                ReadOptionalString(value, "title"),
                ReadOptionalString(value, "context"),
                ReadOptionalElement(value, "citations"),
                ParseExtensions(value)),
            "file_ref" => new CanonicalFileReferencePart(
                ReadOptionalString(value, "fileID"),
                ReadOptionalString(value, "filename"),
                ReadOptionalString(value, "mimeType"),
                ReadOptionalBoolean(value, "downloadable"),
                ParseExtensions(value)),
            "reasoning_text" => new CanonicalReasoningTextPart(
                value.GetProperty("text").GetString()!,
                ParseExtensions(value)),
            "refusal" => new CanonicalRefusalPart(
                value.GetProperty("text").GetString()!,
                ParseExtensions(value)),
            "unknown" => new CanonicalUnknownPart(
                value.GetProperty("vendorType").GetString()!,
                ReadOptionalElement(value, "payload"),
                ParseExtensions(value)),
            var type => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical response fixture part {type}."),
        };

    private static CanonicalDocumentSource ParseDocumentSource(JsonElement value) =>
        value.GetProperty("type").GetString() switch
        {
            "inline_text" => new CanonicalInlineTextDocumentSource(
                value.GetProperty("text").GetString()!),
            "url" => new CanonicalUrlDocumentSource(
                value.GetProperty("url").GetString()!),
            "base64" => new CanonicalBase64DocumentSource(
                value.GetProperty("data").GetString()!,
                ReadOptionalString(value, "mediaType")),
            "file_id" => new CanonicalFileIdDocumentSource(
                value.GetProperty("fileID").GetString()!),
            "unknown" => new CanonicalUnknownDocumentSource(
                value.GetProperty("value")),
            var type => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical response fixture document source {type}."),
        };

    private static CanonicalStop ParseStop(JsonElement value) =>
        new(
            new CanonicalStopReason(value.GetProperty("reason").GetString()!),
            ReadOptionalString(value, "sequence"));

    private static CanonicalUsage ParseUsage(JsonElement value) =>
        new(
            ReadOptionalInt64(value, "inputTokens"),
            ReadOptionalInt64(value, "outputTokens"),
            ReadOptionalInt64(value, "totalTokens"),
            ReadOptionalInt64(value, "cacheCreationInputTokens"),
            ReadOptionalInt64(value, "cacheReadInputTokens"),
            ReadOptionalInt64(value, "reasoningTokens"));

    private static ImmutableArray<CanonicalVendorExtension> ParseExtensions(
        JsonElement value) =>
        value.GetProperty("rawExtensions").EnumerateArray()
            .Select(extension => new CanonicalVendorExtension(
                extension.GetProperty("vendor").GetString()!,
                extension.GetProperty("key").GetString()!,
                extension.GetProperty("value")))
            .ToImmutableArray();

    private static ImmutableDictionary<string, JsonElement> ParseDictionary(
        JsonElement value) =>
        value.EnumerateObject().ToImmutableDictionary(
            property => property.Name,
            property => property.Value.Clone(),
            StringComparer.Ordinal);

    private static JsonElement? ReadOptionalElement(
        JsonElement value,
        string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.Clone()
            : null;

    private static string? ReadOptionalString(
        JsonElement value,
        string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    private static bool? ReadOptionalBoolean(
        JsonElement value,
        string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.GetBoolean()
            : null;

    private static long? ReadOptionalInt64(
        JsonElement value,
        string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
        && property.ValueKind != JsonValueKind.Null
            ? property.GetInt64()
            : null;
}
