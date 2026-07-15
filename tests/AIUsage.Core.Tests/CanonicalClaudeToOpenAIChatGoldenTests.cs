using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeToOpenAIChatGoldenTests
{
    [Theory]
    [InlineData("document-url-lossy.json")]
    [InlineData("rich-tool-loop.json")]
    public void Claude_request_builds_the_swift_openai_chat_payload(string fileName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "bridge",
            "claude-to-openai-chat",
            fileName);
        var root = fixture.RootElement;
        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(root.GetProperty("input"));
        var canonical = CanonicalRequestMapper.FromClaude(request);

        var actual = CanonicalOpenAIRequestBuilder.BuildChatCompletionRequest(
            canonical,
            "gpt-4o-mini");
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
