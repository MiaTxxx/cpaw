using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalOpenAIChatRequestGoldenTests
{
    [Theory]
    [InlineData("content-variants.json")]
    [InlineData("empty-defaults.json")]
    [InlineData("rich-tool-loop.json")]
    public void OpenAI_chat_request_matches_swift_canonical_projection(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "request",
            "openai-chat",
            fixtureName);
        var root = fixture.RootElement;
        var request = WireJson.Deserialize<OpenAIChatCompletionRequestWire>(root.GetProperty("input"));

        var actual = CanonicalRequestMapper.FromOpenAIChat(request);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
