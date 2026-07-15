using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeResponseGoldenTests
{
    [Fact]
    public void Claude_response_matches_swift_canonical_projection()
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "response",
            "claude",
            "mixed-blocks.json");
        var root = fixture.RootElement;
        var response = WireJson.Deserialize<ClaudeMessageResponseWire>(
            root.GetProperty("input"));

        var actual = CanonicalResponseMapper.FromClaude(response);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }
}
