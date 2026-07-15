using System.Collections.Immutable;
using AIUsage.Core.Proxy.Protocols.Anthropic;

namespace AIUsage.Core.Proxy.Canonical;

public static partial class CanonicalResponseMapper
{
    public static CanonicalResponse FromClaude(ClaudeMessageResponseWire response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new CanonicalResponse(
            response.Id,
            response.Model,
            CanonicalRequestMapper.MapClaudeBlocks(response.Role, response.Content),
            new CanonicalStop(
                MapClaudeStopReason(response.StopReason),
                response.StopSequence),
            new CanonicalUsage(
                response.Usage.InputTokens,
                response.Usage.OutputTokens,
                TotalTokens: null,
                response.Usage.CacheCreationInputTokens,
                response.Usage.CacheReadInputTokens,
                ReasoningTokens: null),
            ImmutableArray<CanonicalVendorExtension>.Empty);
    }

    private static CanonicalStopReason MapClaudeStopReason(string? reason) =>
        reason switch
        {
            "end_turn" or "stop" or null => CanonicalStopReason.EndTurn,
            "tool_use" => CanonicalStopReason.ToolUse,
            "max_tokens" => CanonicalStopReason.MaxTokens,
            "pause_turn" => CanonicalStopReason.PauseTurn,
            "refusal" => CanonicalStopReason.Refusal,
            "model_context_window_exceeded" => CanonicalStopReason.ModelContextWindowExceeded,
            _ => new CanonicalStopReason(reason),
        };
}
