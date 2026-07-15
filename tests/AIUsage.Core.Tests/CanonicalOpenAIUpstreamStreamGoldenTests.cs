using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class CanonicalOpenAIUpstreamStreamGoldenTests
{
    [Theory]
    [InlineData("lifecycle-switches.json")]
    [InlineData("tool-buffering-completion.json")]
    [InlineData("finish-usage-matrix.json")]
    public void OpenAI_upstream_stream_sequences_match_swift_canonical_projection(string fixtureName)
    {
        using var fixture = CanonicalGoldenTestSupport.ReadFixture(
            "canonical",
            "stream",
            "openai-upstream",
            fixtureName);
        var root = fixture.RootElement;
        var projectedSequences = root.GetProperty("input").GetProperty("sequences")
            .EnumerateArray()
            .Select(sequence =>
            {
                var mapper = new CanonicalOpenAIUpstreamStreamMapper(
                    new CanonicalRole(sequence.GetProperty("role").GetString()!));
                var actual = sequence.GetProperty("events")
                    .EnumerateArray()
                    .SelectMany(streamEvent => mapper.Map(Parse(streamEvent)))
                    .ToArray();
                return new Dictionary<string, object?>
                {
                    ["label"] = sequence.GetProperty("label").GetString(),
                    ["events"] = CanonicalGoldenTestSupport.Project(actual).GetProperty("events"),
                };
            })
            .ToArray();
        var projected = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["sequences"] = projectedSequences,
        });

        CanonicalGoldenTestSupport.AssertJsonEquivalent(
            root.GetProperty("expected"),
            projected,
            "$");
    }

    private static OpenAIUpstreamStreamEvent Parse(JsonElement streamEvent)
    {
        return streamEvent.GetProperty("type").GetString() switch
        {
            "text_delta" => new OpenAIUpstreamTextDelta(
                streamEvent.GetProperty("text").GetString()!),
            "reasoning_summary_delta" => new OpenAIUpstreamReasoningSummaryDelta(
                streamEvent.GetProperty("text").GetString()!),
            "tool_call_started" => new OpenAIUpstreamToolCallStarted(
                streamEvent.GetProperty("index").GetInt64(),
                streamEvent.GetProperty("id").GetString()!,
                streamEvent.GetProperty("name").GetString()!),
            "tool_call_arguments_delta" => new OpenAIUpstreamToolCallArgumentsDelta(
                streamEvent.GetProperty("index").GetInt64(),
                streamEvent.GetProperty("arguments_delta").GetString()!),
            "completed" => new OpenAIUpstreamCompleted(
                streamEvent.TryGetProperty("finish_reason", out var finishReason)
                    ? finishReason.GetString()
                    : null,
                ParseUsage(streamEvent)),
            var type => throw new Xunit.Sdk.XunitException(
                $"Unsupported upstream fixture event {type}."),
        };
    }

    private static OpenAIUpstreamUsage? ParseUsage(JsonElement streamEvent)
    {
        if (!streamEvent.TryGetProperty("usage", out var usage)
            || usage.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        long? ReadOptional(JsonElement value, string propertyName) =>
            value.TryGetProperty(propertyName, out var property)
                && property.ValueKind != JsonValueKind.Null
                ? property.GetInt64()
                : null;

        long? cachedTokens = null;
        if (usage.TryGetProperty("prompt_tokens_details", out var details)
            && details.ValueKind == JsonValueKind.Object)
        {
            cachedTokens = ReadOptional(details, "cached_tokens");
        }

        return new OpenAIUpstreamUsage(
            usage.GetProperty("prompt_tokens").GetInt64(),
            usage.GetProperty("completion_tokens").GetInt64(),
            usage.GetProperty("total_tokens").GetInt64(),
            ReadOptional(usage, "prompt_cache_hit_tokens"),
            ReadOptional(usage, "prompt_cache_miss_tokens"),
            cachedTokens);
    }
}
