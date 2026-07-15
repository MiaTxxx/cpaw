using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalOpenAIChatResponseGoldenTests
{
    [Fact]
    public void OpenAI_chat_response_matches_swift_canonical_projection()
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "response",
            "openai-chat",
            "rich-tool-loop.json");
        var root = fixture.RootElement;
        var response = WireJson.Deserialize<OpenAIChatCompletionResponseWire>(
            root.GetProperty("input"));

        var actual = CanonicalResponseMapper.FromOpenAIChat(response);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
