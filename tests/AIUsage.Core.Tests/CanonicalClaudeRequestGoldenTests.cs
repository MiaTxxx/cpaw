using System.Text.Json;
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
        using var fixture = ReadFixture(fileName);
        var root = fixture.RootElement;
        var request = WireJson.Deserialize<ClaudeMessageRequestWire>(root.GetProperty("input"));

        var actual = CanonicalRequestMapper.FromClaude(request);
        var projected = CanonicalGoldenTestSupport.Project(actual);

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }

    private static JsonDocument ReadFixture(string fileName) =>
        JsonDocument.Parse(File.ReadAllBytes(GetFixturePath(fileName)));

    private static string GetFixturePath(string fileName)
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
            "canonical",
            "request",
            "claude",
            fileName);
    }
}
