using System.Collections;
using System.Text;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;
using AIUsage.Core.Proxy.Protocols.OpenAIResponses;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class WireJsonTests
{
    private const string ValidClaudeRequest = """
        {
          "model":"claude-fixture",
          "messages":[{"role":"user","content":"hello"}],
          "max_tokens":32
        }
        """;

    [Fact]
    public void Deserialize_overloads_share_the_same_validated_behavior()
    {
        var fromString = WireJson.Deserialize<ClaudeMessageRequestWire>(ValidClaudeRequest);
        var fromUtf8 = WireJson.Deserialize<ClaudeMessageRequestWire>(Encoding.UTF8.GetBytes(ValidClaudeRequest));
        using var document = JsonDocument.Parse(ValidClaudeRequest);
        var fromElement = WireJson.Deserialize<ClaudeMessageRequestWire>(document.RootElement);
        var fromStringType = Assert.IsType<ClaudeMessageRequestWire>(
            WireJson.Deserialize(ValidClaudeRequest, typeof(ClaudeMessageRequestWire)));
        var fromUtf8Type = Assert.IsType<ClaudeMessageRequestWire>(
            WireJson.Deserialize(Encoding.UTF8.GetBytes(ValidClaudeRequest), typeof(ClaudeMessageRequestWire)));
        var fromElementType = Assert.IsType<ClaudeMessageRequestWire>(
            WireJson.Deserialize(document.RootElement, typeof(ClaudeMessageRequestWire)));

        var results = new[] { fromString, fromUtf8, fromElement, fromStringType, fromUtf8Type, fromElementType };
        Assert.All(results, result =>
        {
            Assert.Equal("claude-fixture", result.Model);
            Assert.Equal(32, result.MaxTokens);
            var message = Assert.Single(result.Messages);
            Assert.Equal("user", message.Role);
            var content = Assert.IsType<ClaudeTextMessageContentWire>(message.Content);
            Assert.Equal("hello", content.Text);
        });
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"model\":null,\"messages\":[],\"max_tokens\":1}")]
    [InlineData("{\"model\":\"claude-fixture\",\"messages\":null,\"max_tokens\":1}")]
    [InlineData("{\"model\":\"claude-fixture\",\"messages\":[null],\"max_tokens\":1}")]
    [InlineData("{\"model\":\"claude-fixture\",\"messages\":[{\"role\":\"user\",\"content\":null}],\"max_tokens\":1}")]
    public void Explicit_nulls_in_non_null_object_graph_are_rejected(string json)
    {
        var exception = Assert.Throws<WireJsonException>(
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(json));

        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void All_deserialize_overloads_reject_a_null_root()
    {
        using var document = JsonDocument.Parse("null");
        var utf8 = "null"u8.ToArray();
        Action[] deserialize =
        [
            () => WireJson.Deserialize<ClaudeMessageRequestWire>("null"),
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(utf8),
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(document.RootElement),
            () => WireJson.Deserialize("null", typeof(ClaudeMessageRequestWire)),
            () => WireJson.Deserialize(utf8, typeof(ClaudeMessageRequestWire)),
            () => WireJson.Deserialize(document.RootElement, typeof(ClaudeMessageRequestWire)),
        ];

        Assert.All(deserialize, action => Assert.Throws<WireJsonException>(action));
    }

    [Fact]
    public void Undefined_JsonElement_is_reported_as_a_safe_decode_failure()
    {
        var exception = Assert.Throws<WireJsonException>(
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(default(JsonElement)));

        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void JsonElement_null_collection_items_are_rejected()
    {
        const string json = """
            {"model":"gpt-fixture","input":[null]}
            """;

        Assert.Throws<WireJsonException>(() => WireJson.Deserialize<OpenAIResponsesRequestWire>(json));
    }

    [Fact]
    public void JsonExtensionData_is_preserved_but_excluded_from_null_validation()
    {
        const string json = """
            {
              "model":"claude-fixture",
              "messages":[],
              "max_tokens":1,
              "future_nullable_field":null
            }
            """;

        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(json);

        Assert.NotNull(request.AdditionalProperties);
        Assert.Equal(JsonValueKind.Null, request.AdditionalProperties["future_nullable_field"].ValueKind);
    }

    [Fact]
    public void Decode_failures_do_not_expose_payload_or_inner_exceptions()
    {
        const string secret = "wire-secret-value-45897";
        var json = $$"""
            {"model":"{{secret}}","messages":[]
            """;

        Action[] deserialize =
        [
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(json),
            () => WireJson.Deserialize<ClaudeMessageRequestWire>(Encoding.UTF8.GetBytes(json)),
        ];

        Assert.All(deserialize, action =>
        {
            var exception = Assert.Throws<WireJsonException>(action);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, exception.ToString(), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Core_wire_boundary_does_not_reference_desktop_daemon_contracts()
    {
        var references = typeof(WireJson).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => reference.Name == "AIUsage.Contracts");
    }

    [Fact]
    public void Public_boundary_rejects_foreign_types_without_executing_their_object_graph()
    {
        ThrowingGetter.GetterInvoked = false;
        ThrowingEnumerable.EnumeratorInvoked = false;

        Assert.Throws<ArgumentException>(() => WireJson.Deserialize<ThrowingGetter>("{}"));
        Assert.Throws<ArgumentException>(() => WireJson.Deserialize<ThrowingEnumerable>("[]"));
        Assert.Throws<ArgumentException>(() => WireJson.Deserialize(
            "{}",
            typeof(ProxyHttpError<ProxyHttpError<ThrowingEnumerable>>)));

        Assert.False(ThrowingGetter.GetterInvoked);
        Assert.False(ThrowingEnumerable.EnumeratorInvoked);
    }

    [Fact]
    public void Tolerant_usage_converter_behavior_is_preserved()
    {
        const string json = """
            {
              "choices":[{"future":"malformed-choice"}],
              "usage":{
                "prompt_tokens":"not-a-number",
                "completion_tokens":null,
                "prompt_tokens_details":{"cached_tokens":"not-a-number"}
              }
            }
            """;

        var chunk = WireJson.Deserialize<OpenAIChatStreamChunkWire>(json);

        Assert.Empty(chunk.Choices);
        Assert.NotNull(chunk.Usage);
        Assert.Equal(0, chunk.Usage.PromptTokens);
        Assert.Equal(0, chunk.Usage.CompletionTokens);
        Assert.Equal(0, chunk.Usage.TotalTokens);
        Assert.Null(chunk.Usage.PromptTokensDetails);
    }

    [Fact]
    public void Tolerant_stream_converter_drops_choices_with_explicit_null_delta()
    {
        const string json = """
            {
              "choices":[{"index":0,"delta":null}]
            }
            """;

        var chunk = WireJson.Deserialize<OpenAIChatStreamChunkWire>(json);

        Assert.Empty(chunk.Choices);
    }

    [Fact]
    public void Codex_error_converter_is_validated_by_the_same_boundary()
    {
        const string json = """
            {"error":{"message":null,"type":"fixture_error"}}
            """;

        Assert.Throws<WireJsonException>(() => WireJson.Deserialize<CodexErrorResponseWire>(json));
    }

    private sealed class ThrowingGetter
    {
        public static bool GetterInvoked { get; set; }

        public string Secret
        {
            get
            {
                GetterInvoked = true;
                throw new InvalidOperationException("getter-secret");
            }
        }
    }

    private sealed class ThrowingEnumerable : IEnumerable<string>
    {
        public static bool EnumeratorInvoked { get; set; }

        public IEnumerator<string> GetEnumerator()
        {
            EnumeratorInvoked = true;
            throw new InvalidOperationException("enumerator-secret");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
