using System.Collections.Immutable;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;

namespace AIUsage.Core.Proxy.Canonical;

public static partial class CanonicalResponseMapper
{
    public static CanonicalResponse FromOpenAIChat(OpenAIChatCompletionResponseWire response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var firstChoice = response.Choices.FirstOrDefault()
            ?? throw new InvalidDataException(
                "OpenAI chat response must contain at least one choice.");
        var (_, items) = CanonicalRequestMapper.MapOpenAIChatMessages(
            [firstChoice.Message]);

        return new CanonicalResponse(
            response.Id,
            response.Model,
            items,
            new CanonicalStop(MapOpenAIChatStopReason(firstChoice.FinishReason), Sequence: null),
            MapOpenAIChatUsage(response.Usage),
            ImmutableArray<CanonicalVendorExtension>.Empty);
    }

    private static CanonicalStopReason MapOpenAIChatStopReason(string? reason) =>
        reason switch
        {
            "stop" or "end_turn" or null => CanonicalStopReason.EndTurn,
            "tool_calls" => CanonicalStopReason.ToolUse,
            "length" => CanonicalStopReason.MaxTokens,
            "pause_turn" => CanonicalStopReason.PauseTurn,
            "refusal" or "content_filter" => CanonicalStopReason.Refusal,
            "model_context_window_exceeded" => CanonicalStopReason.ModelContextWindowExceeded,
            _ => new CanonicalStopReason(reason),
        };

    private static CanonicalUsage? MapOpenAIChatUsage(OpenAIChatUsageWire? usage)
    {
        if (usage is null)
        {
            return null;
        }

        var cachedTokens = usage.PromptCacheHitTokens
            ?? usage.PromptTokensDetails?.CachedTokens;
        var inputTokens = usage.PromptCacheMissTokens
            ?? (cachedTokens is long cached
                ? Math.Max(usage.PromptTokens - cached, 0)
                : usage.PromptTokens);

        return new CanonicalUsage(
            inputTokens,
            usage.CompletionTokens,
            usage.TotalTokens,
            CacheCreationInputTokens: null,
            cachedTokens,
            ReasoningTokens: null);
    }
}
