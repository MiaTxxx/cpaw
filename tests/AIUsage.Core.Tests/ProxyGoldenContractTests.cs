using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;
using AIUsage.Core.Proxy.Protocols.OpenAIResponses;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class ProxyGoldenContractTests
{
    public static TheoryData<string, Type> ProxyFixtures => new()
    {
        { "claude/message-request-full.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-document-known.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-image-sources.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-known-optional-nulls-omitted.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-system-blocks-message-blocks.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-system-string-message-text.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-tool-result-blocks-known.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-tool-result-string.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-response-full.json", typeof(ClaudeMessageResponseWire) },
        { "claude/message-response-redacted-thinking.json", typeof(ClaudeMessageResponseWire) },
        { "claude/stream-content-block-delta.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/token-count-request-structured-system.json", typeof(ClaudeTokenCountRequestWire) },
        { "claude/token-count-response-large.json", typeof(ClaudeTokenCountResponseWire) },
        { "claude/error-api-without-request-id.json", typeof(ClaudeErrorResponseWire) },
        { "claude/error-rate-limit-with-request-id.json", typeof(ClaudeErrorResponseWire) },
        { "claude/file-deleted.json", typeof(ClaudeDeletedFileResponseWire) },
        { "claude/files-list-full.json", typeof(ClaudeFilesListResponseWire) },
        { "claude/openai-upstream-file-deleted.json", typeof(OpenAIDeletedFileResponseWire) },
        { "claude/openai-upstream-file-list-full.json", typeof(OpenAIFileListResponseWire) },
        { "claude/openai-upstream-file-object-full.json", typeof(OpenAIFileObjectWire) },
        { "claude/stream-content-block-delta-citations.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/stream-content-block-delta-signature.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/stream-content-block-delta-text.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/stream-content-block-delta-thinking.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/stream-content-block-start-text.json", typeof(ClaudeContentBlockStartEventWire) },
        { "claude/stream-content-block-start-thinking.json", typeof(ClaudeContentBlockStartEventWire) },
        { "claude/stream-content-block-start-tool-use.json", typeof(ClaudeContentBlockStartEventWire) },
        { "claude/stream-content-block-stop.json", typeof(ClaudeContentBlockStopEventWire) },
        { "claude/stream-message-delta.json", typeof(ClaudeMessageDeltaEventWire) },
        { "claude/stream-message-start-full.json", typeof(ClaudeMessageStartEventWire) },
        { "codex/error-all-fields.json", typeof(CodexErrorResponseWire) },
        { "codex/responses-request-tool-loop.json", typeof(OpenAIResponsesRequestWire) },
        { "codex/responses-response-mixed.json", typeof(OpenAIResponsesResponseWire) },
        { "codex/stream-completed.json", typeof(OpenAIResponsesCompletedEventWire) },
        { "codex/stream-output-item-added.json", typeof(OpenAIResponsesOutputItemAddedEventWire) },
        { "codex/stream-output-item-done.json", typeof(OpenAIResponsesOutputItemDoneEventWire) },
        { "codex/stream-output-text-delta.json", typeof(OpenAIResponsesOutputTextDeltaEventWire) },
        { "codex/stream-reasoning-summary-text-delta.json", typeof(OpenAIResponsesReasoningSummaryTextDeltaEventWire) },
        { "codex/stream-function-call-arguments-delta.json", typeof(OpenAIResponsesFunctionCallArgumentsDeltaEventWire) },
        { "codex/stream-function-call-arguments-done.json", typeof(OpenAIResponsesFunctionCallArgumentsDoneEventWire) },
        { "opencode/chat-request-input-file-normalized.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-request-tool-loop.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-request-unknown-content-preserved.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-response-malformed-usage.json", typeof(OpenAIChatCompletionResponseWire) },
        { "opencode/error-all-fields.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/error-message-only.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/chat-response-cache-usage.json", typeof(OpenAIChatCompletionResponseWire) },
        { "opencode/stream-choice-malformed-usage-ignored.json", typeof(OpenAIChatStreamChunkWire) },
        { "opencode/stream-choice-usage.json", typeof(OpenAIChatStreamChunkWire) },
        { "opencode/stream-malformed-choice-dropped.json", typeof(OpenAIChatStreamChunkWire) },
        { "opencode/stream-tool-delta.json", typeof(OpenAIChatStreamChunkWire) },
        { "opencode/stream-usage-only.json", typeof(OpenAIChatStreamChunkWire) },
    };

    public static TheoryData<string, Type> ProxyDecodeFailureFixtures => new()
    {
        { "claude/message-request-document-cache-control-invalid.json", typeof(ClaudeMessageRequestWire) },
        { "claude/message-request-text-cache-control-invalid.json", typeof(ClaudeMessageRequestWire) },
        { "opencode/chat-request-invalid-content-scalar.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-request-content-part-missing-type.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-response-malformed-choice-rejected.json", typeof(OpenAIChatCompletionResponseWire) },
        { "opencode/error-missing-body.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/error-missing-message.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/error-null-body.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/error-null-message.json", typeof(OpenAIErrorResponseWire) },
    };

    [Theory]
    [MemberData(nameof(ProxyFixtures))]
    public void Windows_proxy_wire_contracts_round_trip_Swift_goldens(string relativePath, Type contractType)
    {
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(relativePath)));
        var input = envelope.RootElement.GetProperty("input");
        var expected = envelope.RootElement.GetProperty("expected");

        var value = WireJson.Deserialize(input, contractType);
        Assert.NotNull(value);

        using var actual = JsonDocument.Parse(JsonSerializer.Serialize(value, contractType));
        AssertJsonEquivalent(expected, actual.RootElement, "$expected");
    }

    [Theory]
    [MemberData(nameof(ProxyDecodeFailureFixtures))]
    public void Windows_proxy_wire_contracts_reject_Swift_decode_failure_goldens(
        string relativePath,
        Type contractType)
    {
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(relativePath)));
        var input = envelope.RootElement.GetProperty("input");

        var exception = Assert.Throws<WireJsonException>(
            () => WireJson.Deserialize(input, contractType));

        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(input.GetRawText(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAI_chat_message_content_uses_a_typed_union()
    {
        const string json = """
            {
              "model":"gpt-fixture-chat",
              "messages":[
                {"role":"system","content":"Fixture instructions"},
                {
                  "role":"user",
                  "content":[
                    {"type":"text","text":"Fixture text","future_text_hint":true},
                    {
                      "type":"image_url",
                      "image_url":{"url":"https://example.test/fixture.png","detail":"high","future_image_hint":1},
                      "future_part_hint":"image"
                    },
                    {"type":"file","file_id":"file_fixture_flat","filename":"fixture.txt"},
                    {"type":"future_audio","audio":{"id":"audio_fixture"},"nullable":null}
                  ]
                }
              ]
            }
            """;

        var request = WireJson.Deserialize<OpenAIChatCompletionRequestWire>(json);

        var textContent = Assert.IsType<OpenAITextMessageContentWire>(request.Messages[0].Content);
        Assert.Equal("Fixture instructions", textContent.Text);

        var partsContent = Assert.IsType<OpenAIPartsMessageContentWire>(request.Messages[1].Content);
        Assert.Collection(
            partsContent.Parts,
            part =>
            {
                var text = Assert.IsType<OpenAITextContentPartWire>(part);
                Assert.Equal("Fixture text", text.Text);
                Assert.True(text.AdditionalProperties?["future_text_hint"].GetBoolean());
            },
            part =>
            {
                var image = Assert.IsType<OpenAIImageUrlContentPartWire>(part);
                Assert.Equal("https://example.test/fixture.png", image.ImageUrl.Url);
                Assert.Equal(1, image.ImageUrl.AdditionalProperties?["future_image_hint"].GetInt32());
                Assert.Equal("image", image.AdditionalProperties?["future_part_hint"].GetString());
            },
            part =>
            {
                var file = Assert.IsType<OpenAIFileContentPartWire>(part);
                Assert.Equal("file_fixture_flat", file.File.FileId);
                Assert.Equal("fixture.txt", file.File.Filename);
            },
            part =>
            {
                var unknown = Assert.IsType<OpenAIUnknownContentPartWire>(part);
                Assert.Equal("future_audio", unknown.Type);
                Assert.Equal(JsonValueKind.Null, unknown.Value.GetProperty("nullable").ValueKind);
            });

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(request));
        var serializedParts = roundTrip.RootElement.GetProperty("messages")[1].GetProperty("content");
        Assert.Equal("file", serializedParts[2].GetProperty("type").GetString());
        Assert.Equal("file_fixture_flat", serializedParts[2].GetProperty("file").GetProperty("file_id").GetString());
        Assert.False(serializedParts[2].TryGetProperty("file_id", out _));
        Assert.Equal("audio_fixture", serializedParts[3].GetProperty("audio").GetProperty("id").GetString());
    }

    [Theory]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":{}}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":true}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[null]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":null}]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":7}]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":null}]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":\"image_url\",\"image_url\":null}]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":\"image_url\",\"image_url\":{\"url\":null}}]}]}")]
    [InlineData("{\"model\":\"fixture\",\"messages\":[{\"role\":\"user\",\"content\":[{\"type\":\"file\",\"file\":null}]}]}")]
    public void OpenAI_chat_message_content_rejects_invalid_known_shapes(string json)
    {
        var exception = Assert.Throws<WireJsonException>(
            () => WireJson.Deserialize<OpenAIChatCompletionRequestWire>(json));

        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(json, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Claude_message_content_uses_a_typed_union_with_known_block_variants()
    {
        var textRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-system-string-message-text.json");
        var textContent = Assert.IsType<ClaudeTextMessageContentWire>(textRequest.Messages[0].Content);
        Assert.Equal("Inspect the synthetic fixture.", textContent.Text);

        var blocksRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-system-blocks-message-blocks.json");
        var blocksContent = Assert.IsType<ClaudeBlocksMessageContentWire>(blocksRequest.Messages[0].Content);
        var textBlock = Assert.IsType<ClaudeTextContentBlockWire>(Assert.Single(blocksContent.Blocks));
        Assert.Equal("Preserve the message block array.", textBlock.Text);

        var imageRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-image-sources.json");
        var imageBlocks = Assert.IsType<ClaudeBlocksMessageContentWire>(imageRequest.Messages[0].Content).Blocks;
        Assert.Collection(
            imageBlocks,
            block =>
            {
                var image = Assert.IsType<ClaudeImageContentBlockWire>(block);
                Assert.Equal("base64", image.Source.Type);
                Assert.Equal("image/png", image.Source.MediaType);
                Assert.Equal("<fixture-base64-image>", image.Source.Data);
            },
            block =>
            {
                var image = Assert.IsType<ClaudeImageContentBlockWire>(block);
                Assert.Equal("url", image.Source.Type);
                Assert.Equal("https://example.test/fixture-image.png", image.Source.Url);
            });

        var documentRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-document-known.json");
        var document = Assert.IsType<ClaudeDocumentContentBlockWire>(
            Assert.Single(Assert.IsType<ClaudeBlocksMessageContentWire>(documentRequest.Messages[0].Content).Blocks));
        Assert.Equal("text", document.Source["type"].GetString());
        Assert.Equal("Fixture document", document.Title);
        Assert.True(document.Citations?.GetProperty("enabled").GetBoolean());

        var stringResultRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-tool-result-string.json");
        var stringResult = Assert.IsType<ClaudeToolResultContentBlockWire>(
            Assert.Single(Assert.IsType<ClaudeBlocksMessageContentWire>(stringResultRequest.Messages[0].Content).Blocks));
        Assert.Equal("toolu_fixture_string", stringResult.ToolUseId);
        Assert.Equal("Synthetic tool result", stringResult.Content?.GetString());
        Assert.True(stringResult.IsError);

        var blocksResultRequest = DeserializeFixture<ClaudeMessageRequestWire>(
            "claude/message-request-tool-result-blocks-known.json");
        var blocksResult = Assert.IsType<ClaudeToolResultContentBlockWire>(
            Assert.Single(Assert.IsType<ClaudeBlocksMessageContentWire>(blocksResultRequest.Messages[0].Content).Blocks));
        Assert.Equal(JsonValueKind.Array, blocksResult.Content?.ValueKind);
        Assert.Equal(2, blocksResult.Content?.GetArrayLength());

        var redactedResponse = DeserializeFixture<ClaudeMessageResponseWire>(
            "claude/message-response-redacted-thinking.json");
        var redacted = Assert.IsType<ClaudeRedactedThinkingContentBlockWire>(
            Assert.Single(redactedResponse.Content));
        Assert.Equal("<fixture-redacted-thinking>", redacted.Data);
    }

    [Fact]
    public void Claude_unknown_content_blocks_preserve_the_complete_payload()
    {
        const string json = """
            {
              "role":"assistant",
              "content":[{
                "type":"future_block",
                "nested":{"enabled":true},
                "nullable":null
              }]
            }
            """;

        var message = WireJson.Deserialize<ClaudeMessageWire>(json);
        var content = Assert.IsType<ClaudeBlocksMessageContentWire>(message.Content);
        var unknown = Assert.IsType<ClaudeUnknownContentBlockWire>(Assert.Single(content.Blocks));

        Assert.Equal("future_block", unknown.Type);
        Assert.True(unknown.Value.GetProperty("nested").GetProperty("enabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, unknown.Value.GetProperty("nullable").ValueKind);

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(message));
        Assert.Equal(
            JsonValueKind.Null,
            roundTrip.RootElement.GetProperty("content")[0].GetProperty("nullable").ValueKind);
    }

    [Fact]
    public void Claude_known_content_blocks_do_not_allow_extension_data_to_shadow_reserved_fields()
    {
        static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value);

        var cases = new (ClaudeContentBlockWire Block, string[] Present, string[] Absent)[]
        {
            (
                new ClaudeTextContentBlockWire
                {
                    Text = "typed text",
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["text"] = Element("shadow"),
                        ["cache_control"] = Element(new { type = "shadow" }),
                        ["future"] = Element(true),
                    },
                },
                ["type", "text"],
                ["cache_control"]),
            (
                new ClaudeImageContentBlockWire
                {
                    Source = new ClaudeImageSourceWire
                    {
                        Type = "url",
                        Url = "https://example.test/typed.png",
                        AdditionalProperties = new Dictionary<string, JsonElement>
                        {
                            ["type"] = Element("shadow"),
                            ["media_type"] = Element("shadow"),
                            ["data"] = Element("shadow"),
                            ["url"] = Element("https://example.test/shadow.png"),
                            ["future_source"] = Element(true),
                        },
                    },
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["source"] = Element(new { type = "shadow" }),
                        ["future"] = Element(true),
                    },
                },
                ["type", "source"],
                []),
            (
                new ClaudeDocumentContentBlockWire
                {
                    Source = new Dictionary<string, JsonElement> { ["type"] = Element("text") },
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["source"] = Element(new { type = "shadow" }),
                        ["title"] = Element("shadow"),
                        ["context"] = Element("shadow"),
                        ["citations"] = Element(new { enabled = true }),
                        ["cache_control"] = Element(new { type = "shadow" }),
                        ["future"] = Element(true),
                    },
                },
                ["type", "source"],
                ["title", "context", "citations", "cache_control"]),
            (
                new ClaudeToolUseContentBlockWire
                {
                    Id = "toolu_typed",
                    Name = "typed_tool",
                    Input = new Dictionary<string, JsonElement> { ["query"] = Element("typed") },
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["id"] = Element("shadow"),
                        ["name"] = Element("shadow"),
                        ["input"] = Element(new { query = "shadow" }),
                        ["future"] = Element(true),
                    },
                },
                ["type", "id", "name", "input"],
                []),
            (
                new ClaudeToolResultContentBlockWire
                {
                    ToolUseId = "toolu_typed",
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["tool_use_id"] = Element("shadow"),
                        ["content"] = Element("shadow"),
                        ["is_error"] = Element(true),
                        ["future"] = Element(true),
                    },
                },
                ["type", "tool_use_id"],
                ["content", "is_error"]),
            (
                new ClaudeThinkingContentBlockWire
                {
                    Thinking = "typed thinking",
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["thinking"] = Element("shadow"),
                        ["signature"] = Element("shadow"),
                        ["future"] = Element(true),
                    },
                },
                ["type", "thinking"],
                ["signature"]),
            (
                new ClaudeRedactedThinkingContentBlockWire
                {
                    Data = "typed data",
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["type"] = Element("shadow"),
                        ["data"] = Element("shadow"),
                        ["future"] = Element(true),
                    },
                },
                ["type", "data"],
                []),
        };

        foreach (var testCase in cases)
        {
            using var serialized = JsonDocument.Parse(
                JsonSerializer.Serialize<ClaudeContentBlockWire>(testCase.Block));
            var root = serialized.RootElement;

            Assert.Equal(testCase.Block.Type, root.GetProperty("type").GetString());
            Assert.True(root.GetProperty("future").GetBoolean());
            foreach (var propertyName in testCase.Present)
            {
                Assert.Single(root.EnumerateObject(), property => property.NameEquals(propertyName));
            }

            foreach (var propertyName in testCase.Absent)
            {
                Assert.False(root.TryGetProperty(propertyName, out _));
            }

            if (testCase.Block is ClaudeImageContentBlockWire)
            {
                var source = root.GetProperty("source");
                Assert.Equal("url", source.GetProperty("type").GetString());
                Assert.Equal("https://example.test/typed.png", source.GetProperty("url").GetString());
                Assert.True(source.GetProperty("future_source").GetBoolean());
                Assert.Single(source.EnumerateObject(), property => property.NameEquals("type"));
                Assert.Single(source.EnumerateObject(), property => property.NameEquals("url"));
                Assert.False(source.TryGetProperty("media_type", out _));
                Assert.False(source.TryGetProperty("data", out _));
            }
        }
    }

    [Fact]
    public void Claude_unknown_content_blocks_validate_and_preserve_their_discriminator()
    {
        static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value);

        var unknown = new ClaudeUnknownContentBlockWire
        {
            Discriminator = "future_block",
            Value = Element(new
            {
                type = "future_block",
                nested = new { enabled = true },
            }),
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["type"] = Element("shadow"),
                ["added"] = Element(new[] { 1, 2 }),
            },
        };

        using var serialized = JsonDocument.Parse(
            JsonSerializer.Serialize<ClaudeContentBlockWire>(unknown));
        var root = serialized.RootElement;

        Assert.Single(root.EnumerateObject(), property => property.NameEquals("type"));
        Assert.Equal("future_block", root.GetProperty("type").GetString());
        Assert.True(root.GetProperty("nested").GetProperty("enabled").GetBoolean());
        Assert.Equal(2, root.GetProperty("added").GetArrayLength());

        var mismatch = unknown with
        {
            Value = Element(new { type = "different_block" }),
        };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize<ClaudeContentBlockWire>(mismatch));
    }

    [Theory]
    [InlineData(typeof(ClaudeMessageWire), "{\"role\":\"user\",\"content\":7}")]
    [InlineData(typeof(ClaudeMessageWire), "{\"role\":\"user\",\"content\":[null]}")]
    [InlineData(typeof(ClaudeMessageWire), "{\"role\":\"user\",\"content\":[{}]}")]
    [InlineData(typeof(ClaudeMessageWire), "{\"role\":\"user\",\"content\":[{\"type\":null}]}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"text\",\"text\":null}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"image\",\"source\":null}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"image\",\"source\":{\"type\":null}}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"document\",\"source\":[]}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"tool_use\",\"id\":null,\"name\":\"fixture\",\"input\":{}}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"tool_use\",\"id\":\"toolu\",\"name\":\"fixture\",\"input\":[]}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"tool_result\",\"tool_use_id\":null}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"thinking\",\"thinking\":null}")]
    [InlineData(typeof(ClaudeContentBlockWire), "{\"type\":\"redacted_thinking\",\"data\":null}")]
    public void Claude_message_content_rejects_invalid_known_shapes(Type contractType, string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(json, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAI_chat_tools_and_tool_calls_use_typed_contracts()
    {
        using var requestEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("opencode/chat-request-tool-loop.json")));
        var request = WireJson.Deserialize<OpenAIChatCompletionRequestWire>(
            requestEnvelope.RootElement.GetProperty("input"));

        var tool = Assert.Single(request.Tools ?? []);
        Assert.Equal("function", tool.Type);
        Assert.Equal("lookup_fixture", tool.Function.Name);
        Assert.Equal("Reads synthetic fixture data", tool.Function.Description);
        Assert.Equal("object", tool.Function.Parameters?["type"].GetString());

        var requestToolCall = Assert.Single(request.Messages[2].ToolCalls ?? []);
        Assert.Equal("call_fixture_001", requestToolCall.Id);
        Assert.Equal("function", requestToolCall.Type);
        Assert.Equal("lookup_fixture", requestToolCall.Function.Name);
        Assert.Equal("{\"query\":\"usage\"}", requestToolCall.Function.Arguments);

        using var responseEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("opencode/chat-response-cache-usage.json")));
        var response = WireJson.Deserialize<OpenAIChatCompletionResponseWire>(
            responseEnvelope.RootElement.GetProperty("input"));
        var responseToolCall = Assert.Single(response.Choices[0].Message.ToolCalls ?? []);
        Assert.Equal("call_fixture_002", responseToolCall.Id);
        Assert.Equal("lookup_fixture", responseToolCall.Function.Name);

        using var streamEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("opencode/stream-tool-delta.json")));
        var stream = WireJson.Deserialize<OpenAIChatStreamChunkWire>(
            streamEnvelope.RootElement.GetProperty("input"));
        var deltaToolCall = Assert.Single(stream.Choices[0].Delta.ToolCalls ?? []);
        Assert.Equal(0, deltaToolCall.Index);
        Assert.Equal("call_fixture_stream_001", deltaToolCall.Id);
        Assert.Equal("lookup_fixture", deltaToolCall.Function?.Name);
        Assert.Equal("{\"query\":", deltaToolCall.Function?.Arguments);
    }

    [Fact]
    public void OpenAI_chat_tool_contracts_preserve_unknown_properties_and_64_bit_delta_indices()
    {
        const string json = """
            {
              "model":"gpt-fixture-chat",
              "messages":[{
                "role":"assistant",
                "tool_calls":[{
                  "id":"call_fixture",
                  "type":"future_function",
                  "function":{
                    "name":"lookup_fixture",
                    "arguments":"{}",
                    "future_function_hint":true
                  },
                  "future_call_hint":{"mode":"fixture"}
                }]
              }],
              "tools":[{
                "type":"future_function",
                "function":{
                  "name":"lookup_fixture",
                  "parameters":{"type":"object"},
                  "future_definition_hint":1
                },
                "future_tool_hint":"keep"
              }]
            }
            """;

        var request = WireJson.Deserialize<OpenAIChatCompletionRequestWire>(json);

        var tool = Assert.Single(request.Tools ?? []);
        Assert.Equal("future_function", tool.Type);
        Assert.Equal(1, tool.Function.AdditionalProperties?["future_definition_hint"].GetInt32());
        Assert.Equal("keep", tool.AdditionalProperties?["future_tool_hint"].GetString());
        var call = Assert.Single(request.Messages[0].ToolCalls ?? []);
        Assert.Equal("future_function", call.Type);
        Assert.True(call.Function.AdditionalProperties?["future_function_hint"].GetBoolean());
        Assert.Equal("fixture", call.AdditionalProperties?["future_call_hint"].GetProperty("mode").GetString());

        const string deltaJson = """
            {
              "tool_calls":[{
                "index":4294967296,
                "id":"call_fixture_delta",
                "type":"function",
                "function":{"arguments":"{\"query\":"},
                "future_delta_hint":null
              }]
            }
            """;
        var delta = WireJson.Deserialize<OpenAIChatDeltaWire>(deltaJson);
        var deltaCall = Assert.Single(delta.ToolCalls ?? []);
        Assert.Equal(4_294_967_296, deltaCall.Index);
        Assert.Equal(JsonValueKind.Null, deltaCall.AdditionalProperties?["future_delta_hint"].ValueKind);

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(request));
        Assert.Equal(
            "keep",
            roundTrip.RootElement.GetProperty("tools")[0].GetProperty("future_tool_hint").GetString());
    }

    [Theory]
    [InlineData(typeof(OpenAIChatToolWire), "{\"type\":null,\"function\":{\"name\":\"fixture\"}}")]
    [InlineData(typeof(OpenAIChatToolWire), "{\"type\":\"function\",\"function\":null}")]
    [InlineData(typeof(OpenAIChatFunctionWire), "{\"name\":null}")]
    [InlineData(typeof(OpenAIChatFunctionWire), "{\"name\":\"fixture\",\"parameters\":[]}")]
    [InlineData(typeof(OpenAIChatToolCallWire), "{\"id\":null,\"type\":\"function\",\"function\":{\"name\":\"fixture\",\"arguments\":\"{}\"}}")]
    [InlineData(typeof(OpenAIChatToolCallWire), "{\"id\":\"call\",\"type\":\"function\",\"function\":null}")]
    [InlineData(typeof(OpenAIChatFunctionCallWire), "{\"name\":\"fixture\",\"arguments\":null}")]
    [InlineData(typeof(OpenAIChatToolCallDeltaWire), "{}")]
    [InlineData(typeof(OpenAIChatToolCallDeltaWire), "{\"index\":null}")]
    public void OpenAI_chat_tool_contracts_reject_required_nulls_and_invalid_shapes(
        Type contractType,
        string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(json, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void External_wire_preserves_unknown_root_properties()
    {
        const string json = """
            {
              "model":"future-model",
              "messages":[],
              "max_tokens":1,
              "future_transport_hint":{"mode":"fixture","count":2}
            }
            """;

        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(json);
        Assert.NotNull(request);
        Assert.NotNull(request.AdditionalProperties);
        Assert.Equal("fixture", request.AdditionalProperties["future_transport_hint"].GetProperty("mode").GetString());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(request));
        Assert.Equal(2, roundTrip.RootElement.GetProperty("future_transport_hint").GetProperty("count").GetInt32());
    }

    [Fact]
    public void External_usage_token_counts_are_64_bit()
    {
        const long expectedTokens = 4_294_967_296;
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath("codex/responses-response-mixed.json")));
        var response = WireJson.Deserialize<OpenAIResponsesResponseWire>(
            envelope.RootElement.GetProperty("input").GetRawText());

        Assert.NotNull(response);
        Assert.NotNull(response.Usage);
        Assert.Equal(expectedTokens, response.Usage.InputTokens);
    }

    [Fact]
    public void Codex_stream_indices_are_64_bit()
    {
        using var outputEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("codex/stream-output-text-delta.json")));
        var outputDelta = WireJson.Deserialize<OpenAIResponsesOutputTextDeltaEventWire>(
            outputEnvelope.RootElement.GetProperty("input"));

        Assert.Equal(4_294_967_298, outputDelta.OutputIndex);
        Assert.Equal(4_294_967_299, outputDelta.ContentIndex);

        const string reasoningJson = """
            {
              "type":"future.reasoning.delta",
              "output_index":4294967300,
              "summary_index":4294967304,
              "delta":"fixture"
            }
            """;
        var reasoningDelta = WireJson.Deserialize<OpenAIResponsesReasoningSummaryTextDeltaEventWire>(
            reasoningJson);

        Assert.Equal(4_294_967_300, reasoningDelta.OutputIndex);
        Assert.Equal(4_294_967_304, reasoningDelta.SummaryIndex);
    }

    [Fact]
    public void Codex_optional_stream_fields_explicit_nulls_are_omitted_like_Swift()
    {
        const string json = """
            {
              "type":"future.function.arguments.done",
              "item_id":null,
              "output_index":4294967302,
              "arguments":null,
              "name":null,
              "item":null
            }
            """;

        var value = WireJson.Deserialize<OpenAIResponsesFunctionCallArgumentsDoneEventWire>(json);

        Assert.Equal("future.function.arguments.done", value.Type);
        Assert.Null(value.ItemId);
        Assert.Null(value.Arguments);
        Assert.Null(value.Name);
        Assert.Null(value.Item);

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(value));
        Assert.False(roundTrip.RootElement.TryGetProperty("item_id", out _));
        Assert.False(roundTrip.RootElement.TryGetProperty("arguments", out _));
        Assert.False(roundTrip.RootElement.TryGetProperty("name", out _));
        Assert.False(roundTrip.RootElement.TryGetProperty("item", out _));
    }

    [Fact]
    public void Codex_stream_wire_preserves_unknown_root_and_nested_properties()
    {
        const string json = """
            {
              "type":"future.function.arguments.done",
              "output_index":4294967302,
              "item":{
                "type":"future_function_call",
                "call_id":"call_fixture",
                "name":"lookup_fixture",
                "arguments":"{}",
                "future_nested_hint":{"priority":2}
              },
              "future_event_hint":{"mode":"fixture"}
            }
            """;

        var value = WireJson.Deserialize<OpenAIResponsesFunctionCallArgumentsDoneEventWire>(json);

        Assert.Equal("future.function.arguments.done", value.Type);
        Assert.Equal("future_function_call", value.Item?.Type);
        Assert.Equal(2, value.Item?.AdditionalProperties?["future_nested_hint"].GetProperty("priority").GetInt32());
        Assert.Equal("fixture", value.AdditionalProperties?["future_event_hint"].GetProperty("mode").GetString());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(value));
        Assert.Equal(
            2,
            roundTrip.RootElement
                .GetProperty("item")
                .GetProperty("future_nested_hint")
                .GetProperty("priority")
                .GetInt32());
        Assert.Equal(
            "fixture",
            roundTrip.RootElement
                .GetProperty("future_event_hint")
                .GetProperty("mode")
                .GetString());
    }

    [Theory]
    [InlineData(typeof(OpenAIResponsesOutputItemAddedEventWire), "{\"output_index\":0,\"item\":{}}")]
    [InlineData(typeof(OpenAIResponsesOutputItemAddedEventWire), "{\"type\":\"future\",\"output_index\":0}")]
    [InlineData(typeof(OpenAIResponsesOutputItemAddedEventWire), "{\"type\":null,\"output_index\":0,\"item\":{}}")]
    [InlineData(typeof(OpenAIResponsesOutputItemAddedEventWire), "{\"type\":\"future\",\"output_index\":0,\"item\":null}")]
    [InlineData(typeof(OpenAIResponsesOutputItemAddedEventWire), "{\"type\":\"future\",\"output_index\":0,\"item\":[]}")]
    [InlineData(typeof(OpenAIResponsesOutputItemDoneEventWire), "{\"type\":\"future\",\"output_index\":0,\"item\":null}")]
    [InlineData(typeof(OpenAIResponsesOutputItemDoneEventWire), "{\"type\":\"future\",\"output_index\":0,\"item\":\"not-an-object\"}")]
    [InlineData(typeof(OpenAIResponsesOutputTextDeltaEventWire), "{\"type\":\"future\",\"item_id\":null,\"output_index\":0,\"content_index\":0,\"delta\":\"\"}")]
    [InlineData(typeof(OpenAIResponsesOutputTextDeltaEventWire), "{\"type\":\"future\",\"item_id\":\"item\",\"output_index\":0,\"content_index\":0}")]
    [InlineData(typeof(OpenAIResponsesOutputTextDeltaEventWire), "{\"type\":\"future\",\"item_id\":\"item\",\"output_index\":0,\"content_index\":0,\"delta\":null}")]
    [InlineData(typeof(OpenAIResponsesReasoningSummaryTextDeltaEventWire), "{\"type\":\"future\",\"output_index\":0,\"delta\":null}")]
    [InlineData(typeof(OpenAIResponsesFunctionCallArgumentsDeltaEventWire), "{\"type\":\"future\",\"output_index\":0,\"delta\":null}")]
    [InlineData(typeof(OpenAIResponsesFunctionCallArgumentsDoneEventWire), "{\"type\":null,\"output_index\":0}")]
    [InlineData(typeof(OpenAIResponsesFunctionCallArgumentsDoneEventWire), "{\"type\":\"future\",\"output_index\":0,\"item\":{\"type\":\"function_call\",\"call_id\":null,\"name\":\"fixture\",\"arguments\":\"{}\"}}")]
    public void Codex_stream_wire_rejects_required_nulls(Type contractType, string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Claude_token_count_system_preserves_string_and_structured_shapes()
    {
        const string stringSystemJson = """
            {
              "model":"claude-fixture-token-count",
              "messages":[{"role":"user","content":"Count this fixture."}],
              "system":"Use synthetic fixture data only."
            }
            """;

        var stringSystem = WireJson.Deserialize<ClaudeTokenCountRequestWire>(stringSystemJson);
        Assert.NotNull(stringSystem);
        Assert.Equal(JsonValueKind.String, stringSystem.System?.ValueKind);

        using var stringRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(stringSystem));
        Assert.Equal(
            "Use synthetic fixture data only.",
            stringRoundTrip.RootElement.GetProperty("system").GetString());

        using var envelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("claude/token-count-request-structured-system.json")));
        var structuredSystem = WireJson.Deserialize<ClaudeTokenCountRequestWire>(
            envelope.RootElement.GetProperty("input").GetRawText());

        Assert.NotNull(structuredSystem);
        Assert.Equal(JsonValueKind.Array, structuredSystem.System?.ValueKind);

        using var structuredRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(structuredSystem));
        Assert.Equal(
            "ephemeral",
            structuredRoundTrip.RootElement
                .GetProperty("system")[1]
                .GetProperty("cache_control")
                .GetProperty("type")
                .GetString());
    }

    [Fact]
    public void Claude_token_count_wire_preserves_unknown_properties()
    {
        const string requestJson = """
            {
              "model":"claude-fixture-token-count",
              "messages":[{
                "role":"user",
                "content":"Count this fixture.",
                "future_message_hint":{"priority":2}
              }],
              "system":[{"type":"future_system_block","value":true}],
              "tools":[{
                "name":"fixture_tool",
                "input_schema":{"type":"object"},
                "future_tool_hint":"keep"
              }],
              "future_request_hint":{"mode":"fixture"}
            }
            """;

        var request = WireJson.Deserialize<ClaudeTokenCountRequestWire>(requestJson);
        Assert.NotNull(request);
        Assert.Equal("fixture", request.AdditionalProperties?["future_request_hint"].GetProperty("mode").GetString());
        Assert.Equal(2, request.Messages[0].AdditionalProperties?["future_message_hint"].GetProperty("priority").GetInt32());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(request));
        Assert.True(roundTrip.RootElement.GetProperty("system")[0].GetProperty("value").GetBoolean());
        Assert.Equal(
            "keep",
            roundTrip.RootElement.GetProperty("tools")[0].GetProperty("future_tool_hint").GetString());

        const string responseJson = """
            {
              "input_tokens":4294967296,
              "future_count_metadata":{"unit":"tokens"}
            }
            """;

        var response = WireJson.Deserialize<ClaudeTokenCountResponseWire>(responseJson);
        Assert.NotNull(response);
        Assert.Equal(
            "tokens",
            response.AdditionalProperties?["future_count_metadata"].GetProperty("unit").GetString());

        using var responseRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(response));
        Assert.Equal(
            "tokens",
            responseRoundTrip.RootElement
                .GetProperty("future_count_metadata")
                .GetProperty("unit")
                .GetString());
    }

    [Fact]
    public void Claude_request_configuration_is_typed_and_preserves_nested_unknown_properties()
    {
        const string json = """
            {
              "model":"claude-fixture-config",
              "messages":[{"role":"user","content":"Configure the fixture."}],
              "max_tokens":64,
              "tools":[{
                "name":"lookup_fixture",
                "description":"Reads fixture data",
                "input_schema":{"type":"object","properties":{"query":{"type":"string"}}},
                "eager_input_streaming":true,
                "future_tool_hint":{"mode":"fixture"}
              }],
              "tool_choice":{
                "type":"tool",
                "name":"lookup_fixture",
                "disable_parallel_tool_use":true,
                "future_choice_hint":1
              },
              "metadata":{"user_id":"fixture-user","future_metadata_hint":true},
              "thinking":{
                "type":"enabled",
                "budget_tokens":4294967296,
                "display":"summarized",
                "future_thinking_hint":"keep"
              },
              "output_config":{
                "effort":"medium",
                "format":{
                  "type":"json_schema",
                  "schema":{"type":"object"},
                  "future_format_hint":false
                },
                "future_output_hint":null
              }
            }
            """;

        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(json);

        var tool = Assert.Single(request.Tools ?? []);
        Assert.Equal("object", tool.InputSchema["type"].GetString());
        Assert.True(tool.EagerInputStreaming);
        Assert.Equal("fixture", tool.AdditionalProperties?["future_tool_hint"].GetProperty("mode").GetString());
        Assert.Equal("lookup_fixture", request.ToolChoice?.Name);
        Assert.Equal(1, request.ToolChoice?.AdditionalProperties?["future_choice_hint"].GetInt32());
        Assert.True(request.Metadata?.AdditionalProperties?["future_metadata_hint"].GetBoolean());
        Assert.Equal(4_294_967_296, request.Thinking?.BudgetTokens);
        Assert.Equal("keep", request.Thinking?.AdditionalProperties?["future_thinking_hint"].GetString());
        Assert.Equal("object", request.OutputConfig?.Format?.Schema?["type"].GetString());
        Assert.False(request.OutputConfig?.Format?.AdditionalProperties?["future_format_hint"].GetBoolean());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(request));
        Assert.Equal(
            JsonValueKind.Null,
            roundTrip.RootElement.GetProperty("output_config").GetProperty("future_output_hint").ValueKind);
    }

    [Theory]
    [InlineData(typeof(ClaudeToolWire), "{\"name\":null,\"input_schema\":{}}")]
    [InlineData(typeof(ClaudeToolWire), "{\"name\":\"fixture\",\"input_schema\":null}")]
    [InlineData(typeof(ClaudeToolWire), "{\"name\":\"fixture\",\"input_schema\":[]}")]
    [InlineData(typeof(ClaudeToolChoiceWire), "{\"type\":null}")]
    [InlineData(typeof(ClaudeThinkingConfigWire), "{\"type\":null}")]
    [InlineData(typeof(ClaudeOutputFormatWire), "{\"type\":null}")]
    [InlineData(typeof(ClaudeOutputFormatWire), "{\"type\":\"json_schema\",\"schema\":[]}")]
    public void Claude_request_configuration_rejects_required_nulls_and_invalid_object_shapes(
        Type contractType,
        string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Claude_stream_and_files_wire_preserve_unknown_properties()
    {
        const string streamJson = """
            {
              "type":"content_block_start",
              "index":4294967296,
              "content_block":{
                "type":"tool_use",
                "id":"toolu_fixture",
                "name":"lookup_fixture",
                "input":{"query":"quota"},
                "future_block_hint":{"mode":"fixture"}
              },
              "future_event_hint":true
            }
            """;

        var stream = WireJson.Deserialize<ClaudeContentBlockStartEventWire>(streamJson);
        Assert.Equal(4_294_967_296, stream.Index);
        Assert.True(stream.AdditionalProperties?["future_event_hint"].GetBoolean());
        Assert.Equal(
            "fixture",
            stream.ContentBlock.AdditionalProperties?["future_block_hint"].GetProperty("mode").GetString());

        using var streamRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(stream));
        Assert.Equal(
            "fixture",
            streamRoundTrip.RootElement
                .GetProperty("content_block")
                .GetProperty("future_block_hint")
                .GetProperty("mode")
                .GetString());

        const string filesJson = """
            {
              "data":[{
                "id":"file_fixture",
                "type":"file",
                "filename":"fixture.jsonl",
                "mime_type":"application/jsonl",
                "size_bytes":4294967296,
                "created_at":"2030-01-02T03:04:05Z",
                "downloadable":true,
                "scope":{"type":"workspace","id":"workspace_fixture","future_scope_hint":1},
                "future_file_hint":"keep"
              }],
              "has_more":false,
              "first_id":"file_fixture",
              "last_id":"file_fixture",
              "future_list_hint":null
            }
            """;

        var files = WireJson.Deserialize<ClaudeFilesListResponseWire>(filesJson);
        var file = Assert.Single(files.Data);
        Assert.Equal(4_294_967_296, file.SizeBytes);
        Assert.Equal("keep", file.AdditionalProperties?["future_file_hint"].GetString());
        Assert.Equal(1, file.Scope?.AdditionalProperties?["future_scope_hint"].GetInt32());
        Assert.Equal(JsonValueKind.Null, files.AdditionalProperties?["future_list_hint"].ValueKind);
    }

    [Fact]
    public void OpenAI_upstream_file_wire_uses_64_bit_epoch_fields_and_omits_optional_nulls()
    {
        using var objectEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("claude/openai-upstream-file-object-full.json")));
        var file = WireJson.Deserialize<OpenAIFileObjectWire>(objectEnvelope.RootElement.GetProperty("input"));

        Assert.Equal(4_294_967_296, file.Bytes);
        Assert.Equal(4_294_967_297, file.CreatedAt);

        using var listEnvelope = JsonDocument.Parse(File.ReadAllBytes(
            GetFixturePath("claude/openai-upstream-file-list-full.json")));
        var list = WireJson.Deserialize<OpenAIFileListResponseWire>(listEnvelope.RootElement.GetProperty("input"));
        var minimalFile = list.Data[1];

        Assert.Null(minimalFile.Bytes);
        Assert.Null(minimalFile.CreatedAt);
        Assert.Null(minimalFile.Filename);
        Assert.Null(minimalFile.Purpose);
        Assert.Null(minimalFile.Status);
        Assert.Null(minimalFile.MimeType);
        Assert.Null(minimalFile.Deleted);

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(minimalFile));
        Assert.Equal(2, roundTrip.RootElement.EnumerateObject().Count());
        Assert.Equal("file_fixture_upstream_002", roundTrip.RootElement.GetProperty("id").GetString());
        Assert.Equal("file", roundTrip.RootElement.GetProperty("object").GetString());

        const string nullHasMoreJson = """
            {"object":"list","data":[],"has_more":null}
            """;
        var listWithoutHasMore = WireJson.Deserialize<OpenAIFileListResponseWire>(nullHasMoreJson);
        Assert.Null(listWithoutHasMore.HasMore);

        using var listRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(listWithoutHasMore));
        Assert.False(listRoundTrip.RootElement.TryGetProperty("has_more", out _));
    }

    [Fact]
    public void OpenAI_upstream_file_wire_preserves_unknown_properties()
    {
        const string json = """
            {
              "object":"list",
              "data":[{
                "id":"file_fixture",
                "object":"file",
                "future_file_hint":{"mode":"fixture"}
              }],
              "future_list_hint":true
            }
            """;

        var list = WireJson.Deserialize<OpenAIFileListResponseWire>(json);

        Assert.True(list.AdditionalProperties?["future_list_hint"].GetBoolean());
        Assert.Equal(
            "fixture",
            list.Data[0].AdditionalProperties?["future_file_hint"].GetProperty("mode").GetString());

        const string deletedJson = """
            {
              "id":"file_fixture",
              "object":"file",
              "deleted":true,
              "future_delete_hint":1
            }
            """;
        var deleted = WireJson.Deserialize<OpenAIDeletedFileResponseWire>(deletedJson);
        Assert.Equal(1, deleted.AdditionalProperties?["future_delete_hint"].GetInt32());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(list));
        Assert.True(roundTrip.RootElement.GetProperty("future_list_hint").GetBoolean());
        Assert.Equal(
            "fixture",
            roundTrip.RootElement
                .GetProperty("data")[0]
                .GetProperty("future_file_hint")
                .GetProperty("mode")
                .GetString());

        using var deletedRoundTrip = JsonDocument.Parse(JsonSerializer.Serialize(deleted));
        Assert.Equal(1, deletedRoundTrip.RootElement.GetProperty("future_delete_hint").GetInt32());
    }

    [Theory]
    [InlineData(typeof(OpenAIFileObjectWire), "{\"id\":null,\"object\":\"file\"}")]
    [InlineData(typeof(OpenAIFileObjectWire), "{\"id\":\"file_fixture\",\"object\":null}")]
    [InlineData(typeof(OpenAIFileListResponseWire), "{\"object\":null,\"data\":[]}")]
    [InlineData(typeof(OpenAIFileListResponseWire), "{\"object\":\"list\",\"data\":null}")]
    [InlineData(typeof(OpenAIFileListResponseWire), "{\"object\":\"list\",\"data\":[null]}")]
    [InlineData(typeof(OpenAIDeletedFileResponseWire), "{\"id\":null,\"object\":\"file\",\"deleted\":true}")]
    [InlineData(typeof(OpenAIDeletedFileResponseWire), "{\"id\":\"file_fixture\",\"object\":null,\"deleted\":true}")]
    [InlineData(typeof(OpenAIDeletedFileResponseWire), "{\"id\":\"file_fixture\",\"object\":\"file\",\"deleted\":null}")]
    [InlineData(typeof(OpenAIDeletedFileResponseWire), "{\"id\":\"file_fixture\",\"object\":\"file\"}")]
    public void OpenAI_upstream_file_wire_rejects_required_nulls_and_missing_members(
        Type contractType,
        string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData(typeof(ClaudeMessageStartEventWire), "{\"type\":\"message_start\",\"message\":null}")]
    [InlineData(typeof(ClaudeContentBlockStartEventWire), "{\"type\":\"content_block_start\",\"index\":0,\"content_block\":null}")]
    [InlineData(typeof(ClaudeContentBlockDeltaEventWire), "{\"type\":\"content_block_delta\",\"index\":0,\"delta\":null}")]
    [InlineData(typeof(ClaudeMessageDeltaEventWire), "{\"type\":\"message_delta\",\"delta\":null,\"usage\":{\"output_tokens\":0}}")]
    [InlineData(typeof(ClaudeFilesListResponseWire), "{\"data\":null,\"has_more\":false}")]
    [InlineData(typeof(ClaudeFilesListResponseWire), "{\"data\":[null],\"has_more\":false}")]
    public void Claude_stream_and_files_wire_reject_required_nulls(Type contractType, string json)
    {
        var exception = Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));

        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":null}}")]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"thinking\",\"thinking\":null}}")]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":null,\"name\":\"fixture\",\"input\":{}}}")]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":\"toolu_fixture\",\"name\":null,\"input\":{}}}")]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":\"toolu_fixture\",\"name\":\"fixture\",\"input\":null}}")]
    [InlineData("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":\"toolu_fixture\",\"name\":\"fixture\",\"input\":\"not-an-object\"}}")]
    public void Claude_known_content_block_union_rejects_invalid_required_shapes(string json)
    {
        Assert.Throws<WireJsonException>(() => WireJson.Deserialize<ClaudeContentBlockStartEventWire>(json));
    }

    [Theory]
    [InlineData("text_delta", "text")]
    [InlineData("input_json_delta", "partial_json")]
    [InlineData("thinking_delta", "thinking")]
    [InlineData("signature_delta", "signature")]
    public void Claude_known_delta_union_rejects_variant_required_nulls(string deltaType, string propertyName)
    {
        var json = $$"""
            {
              "type":"content_block_delta",
              "index":0,
              "delta":{"type":"{{deltaType}}","{{propertyName}}":null}
            }
            """;

        Assert.Throws<WireJsonException>(() => WireJson.Deserialize<ClaudeContentBlockDeltaEventWire>(json));
    }

    [Theory]
    [InlineData(typeof(ClaudeMessageStartEventWire), "{\"type\":\"future_event\",\"message\":{\"id\":\"msg_fixture\",\"type\":\"message\",\"role\":\"assistant\",\"content\":[],\"model\":\"claude-fixture\",\"usage\":{\"input_tokens\":0,\"output_tokens\":0}}}")]
    [InlineData(typeof(ClaudeContentBlockStartEventWire), "{\"type\":\"future_event\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}")]
    [InlineData(typeof(ClaudeContentBlockDeltaEventWire), "{\"type\":\"future_event\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"fixture\"}}")]
    [InlineData(typeof(ClaudeContentBlockStopEventWire), "{\"type\":\"future_event\",\"index\":0}")]
    [InlineData(typeof(ClaudeMessageDeltaEventWire), "{\"type\":\"future_event\",\"delta\":{},\"usage\":{\"output_tokens\":0}}")]
    public void Claude_stream_events_reject_wrong_discriminators(Type contractType, string json)
    {
        Assert.Throws<WireJsonException>(() => WireJson.Deserialize(json, contractType));
    }

    [Fact]
    public void Typed_nested_wire_objects_preserve_unknown_properties()
    {
        const string json = """
            {
              "prompt_tokens":1,
              "completion_tokens":2,
              "total_tokens":3,
              "future_usage_counter":{"kind":"fixture","value":4}
            }
            """;

        var usage = WireJson.Deserialize<OpenAIChatUsageWire>(json);
        Assert.NotNull(usage);
        Assert.NotNull(usage.AdditionalProperties);
        Assert.Equal(4, usage.AdditionalProperties["future_usage_counter"].GetProperty("value").GetInt32());

        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(usage));
        Assert.Equal(
            "fixture",
            roundTrip.RootElement.GetProperty("future_usage_counter").GetProperty("kind").GetString());
    }

    [Fact]
    public void All_typed_proxy_token_count_index_and_size_properties_are_64_bit()
    {
        var invalidProperties = typeof(ClaudeMessageRequestWire).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("AIUsage.Core.Proxy.Protocols", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties())
            .Where(property => property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Count", StringComparison.OrdinalIgnoreCase)
                || property.Name == "Index"
                || property.Name == "SizeBytes")
            .Where(property => property.PropertyType.IsValueType)
            .Where(property => property.PropertyType != typeof(long) && property.PropertyType != typeof(long?))
            .Select(property => $"{property.DeclaringType?.FullName}.{property.Name}: {property.PropertyType.Name}")
            .ToArray();

        Assert.Empty(invalidProperties);
    }

    [Fact]
    public void OpenAI_stream_drops_malformed_optional_shapes_like_Swift()
    {
        const string json = """
            {
              "choices":[{"future":"malformed-choice"}],
              "usage":{
                "prompt_tokens_details":{"cached_tokens":"not-a-number"}
              }
            }
            """;

        var chunk = WireJson.Deserialize<OpenAIChatStreamChunkWire>(json);

        Assert.NotNull(chunk);
        Assert.Empty(chunk.Choices);
        Assert.NotNull(chunk.Usage);
        Assert.Equal(0, chunk.Usage.PromptTokens);
        Assert.Null(chunk.Usage.PromptTokensDetails);
    }

    [Fact]
    public void Codex_request_id_remains_transport_metadata_and_never_enters_JSON_body()
    {
        const string json = """
            {
              "error":{"message":"Fixture error","type":"fixture_error"},
              "request_id":"req_fixture_header",
              "future_error_field":true
            }
            """;
        var body = WireJson.Deserialize<CodexErrorResponseWire>(json);
        Assert.NotNull(body);

        body = body with
        {
            AdditionalProperties = new Dictionary<string, JsonElement>(body.AdditionalProperties ?? [])
            {
                ["error"] = JsonSerializer.SerializeToElement(new { message = "shadow" }),
                ["request_id"] = JsonSerializer.SerializeToElement("req_shadow"),
            },
        };

        var transportError = new ProxyHttpError<CodexErrorResponseWire>(429, body, "req_fixture_header");
        using var serializedBody = JsonDocument.Parse(JsonSerializer.Serialize(body));
        using var serializedWrapper = JsonDocument.Parse(JsonSerializer.Serialize(transportError));

        Assert.False(serializedBody.RootElement.TryGetProperty("request_id", out _));
        Assert.Single(serializedBody.RootElement.EnumerateObject(), property => property.NameEquals("error"));
        Assert.True(serializedBody.RootElement.GetProperty("future_error_field").GetBoolean());
        Assert.False(serializedWrapper.RootElement.TryGetProperty("RequestId", out _));
        Assert.Equal("req_fixture_header", transportError.RequestId);
    }

    private static T DeserializeFixture<T>(string relativePath)
    {
        using var envelope = JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(relativePath)));
        return WireJson.Deserialize<T>(envelope.RootElement.GetProperty("input"));
    }

    private static string GetFixturePath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AIUsage.Windows.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return Path.Combine(
            current.FullName,
            "QuotaBackend",
            "Tests",
            "QuotaBackendTests",
            "Fixtures",
            "v1",
            "contracts",
            "proxy",
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(expected.ValueKind == actual.ValueKind, $"JSON kind differs at {path}: expected {expected.ValueKind}, actual {actual.ValueKind}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToDictionary(property => property.Name, StringComparer.Ordinal);
                var actualProperties = actual.EnumerateObject().ToDictionary(property => property.Name, StringComparer.Ordinal);
                Assert.True(
                    expectedProperties.Keys.Order(StringComparer.Ordinal).SequenceEqual(actualProperties.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal),
                    $"JSON properties differ at {path}");
                foreach (var property in expectedProperties)
                {
                    AssertJsonEquivalent(property.Value.Value, actualProperties[property.Key].Value, $"{path}.{property.Key}");
                }

                break;
            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (var index = 0; index < expectedItems.Length; index++)
                {
                    AssertJsonEquivalent(expectedItems[index], actualItems[index], $"{path}[{index}]");
                }

                break;
            case JsonValueKind.Number:
                if (expected.TryGetInt64(out var expectedInteger) && actual.TryGetInt64(out var actualInteger))
                {
                    Assert.True(expectedInteger == actualInteger, $"JSON integer differs at {path}");
                }
                else
                {
                    Assert.True(expected.GetDouble().Equals(actual.GetDouble()), $"JSON number differs at {path}");
                }

                break;
            case JsonValueKind.String:
                Assert.True(expected.GetString() == actual.GetString(), $"JSON string differs at {path}");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                Assert.True(expected.GetBoolean() == actual.GetBoolean(), $"JSON boolean differs at {path}");
                break;
            case JsonValueKind.Null:
                break;
            default:
                throw new Xunit.Sdk.XunitException($"Unsupported JSON kind at {path}: {expected.ValueKind}");
        }
    }
}
