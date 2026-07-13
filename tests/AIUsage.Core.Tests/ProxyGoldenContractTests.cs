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
        { "claude/message-response-full.json", typeof(ClaudeMessageResponseWire) },
        { "claude/stream-content-block-delta.json", typeof(ClaudeContentBlockDeltaEventWire) },
        { "claude/token-count-request-structured-system.json", typeof(ClaudeTokenCountRequestWire) },
        { "claude/token-count-response-large.json", typeof(ClaudeTokenCountResponseWire) },
        { "claude/error-api-without-request-id.json", typeof(ClaudeErrorResponseWire) },
        { "claude/error-rate-limit-with-request-id.json", typeof(ClaudeErrorResponseWire) },
        { "claude/file-deleted.json", typeof(ClaudeDeletedFileResponseWire) },
        { "claude/files-list-full.json", typeof(ClaudeFilesListResponseWire) },
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
        { "opencode/chat-request-tool-loop.json", typeof(OpenAIChatCompletionRequestWire) },
        { "opencode/chat-response-malformed-usage.json", typeof(OpenAIChatCompletionResponseWire) },
        { "opencode/error-all-fields.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/error-message-only.json", typeof(OpenAIErrorResponseWire) },
        { "opencode/chat-response-cache-usage.json", typeof(OpenAIChatCompletionResponseWire) },
        { "opencode/stream-tool-delta.json", typeof(OpenAIChatStreamChunkWire) },
        { "opencode/stream-usage-only.json", typeof(OpenAIChatStreamChunkWire) },
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
