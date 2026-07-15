using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeRequestGoldenTests
{
    [Theory]
    [InlineData("rich-tool-loop.json")]
    [InlineData("content-variants.json")]
    [InlineData("empty-defaults.json")]
    public void Claude_request_matches_swift_canonical_projection(string fileName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "request",
            "claude",
            fileName);
        var root = fixture.RootElement;
        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(root.GetProperty("input"));

        var actual = CanonicalRequestMapper.FromClaude(request);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
