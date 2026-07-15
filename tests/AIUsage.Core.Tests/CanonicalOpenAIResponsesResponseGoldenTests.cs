using System.Text.Json;
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
    [InlineData("hosted-call-id-matrix.json")]
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

    [Fact]
    public void OpenAI_Responses_stop_priority_matches_swift_canonical_projection()
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "response",
            "openai-responses",
            "stop-priority-matrix.json");
        var root = fixture.RootElement;
        var results = root.GetProperty("input").GetProperty("cases")
            .EnumerateArray()
            .Select(fixtureCase =>
            {
                var response = WireJson.Deserialize<OpenAIResponsesResponseWire>(
                    fixtureCase.GetProperty("response"));
                var canonical = CanonicalResponseMapper.FromOpenAIResponses(response);
                return new Dictionary<string, object?>
                {
                    ["label"] = fixtureCase.GetProperty("label").GetString(),
                    ["reason"] = canonical.Stop.Reason.Value,
                };
            })
            .ToArray();
        var projected = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["results"] = results,
        });

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
