using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.OpenAIResponses;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalOpenAIResponsesResponseGoldenTests
{
    [Theory]
    [InlineData("mixed-tool-loop.json")]
    [InlineData("content-hosted-variants.json")]
    public void OpenAI_Responses_response_matches_swift_canonical_projection(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "response",
            "openai-responses",
            fixtureName);
        var root = fixture.RootElement;
        var response = WireJson.Deserialize<OpenAIResponsesResponseWire>(
            root.GetProperty("input"));

        var actual = CanonicalResponseMapper.FromOpenAIResponses(response);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
