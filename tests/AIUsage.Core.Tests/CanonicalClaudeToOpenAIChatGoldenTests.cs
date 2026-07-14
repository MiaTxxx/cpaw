using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalClaudeToOpenAIChatGoldenTests
{
    [Fact]
    public void Claude_rich_tool_loop_builds_the_swift_openai_chat_payload()
    {
        using var fixture = ReadFixture("rich-tool-loop.json");
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
            "bridge",
            "claude-to-openai-chat",
            fileName);
    }
}
