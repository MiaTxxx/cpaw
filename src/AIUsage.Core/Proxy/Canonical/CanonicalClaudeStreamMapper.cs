using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.Anthropic;

namespace AIUsage.Core.Proxy.Canonical;

public static class CanonicalClaudeStreamMapper
{
    private static readonly ImmutableArray<CanonicalVendorExtension> NoExtensions =
        ImmutableArray<CanonicalVendorExtension>.Empty;

    public static ImmutableArray<CanonicalStreamEvent> Map(ClaudeStreamEventWire streamEvent)
    {
        ArgumentNullException.ThrowIfNull(streamEvent);

        return streamEvent switch
        {
            ClaudeMessageStartEventWire messageStart => One(new CanonicalStreamMessageStarted(
                new CanonicalRole(messageStart.Message.Role),
                messageStart.Message.Id,
                messageStart.Message.Model,
                NoExtensions)),
            ClaudeContentBlockStartEventWire blockStart => One(MapBlockStart(blockStart)),
            ClaudeContentBlockDeltaEventWire blockDelta => MapBlockDelta(blockDelta),
            ClaudeContentBlockStopEventWire blockStop => One(
                new CanonicalStreamContentPartStopped(blockStop.Index)),
            ClaudeMessageDeltaEventWire messageDelta => One(MapMessageDelta(messageDelta)),
            ClaudeMessageStopEventWire => One(new CanonicalStreamMessageStopped()),
            ClaudePingEventWire => ImmutableArray<CanonicalStreamEvent>.Empty,
            _ => throw new ArgumentOutOfRangeException(
                nameof(streamEvent),
                streamEvent.GetType().Name,
                "Unsupported Claude stream event."),
        };
    }

    private static CanonicalStreamContentPartStarted MapBlockStart(
        ClaudeContentBlockStartEventWire blockStart)
    {
        var (kind, toolCallId, toolName) = blockStart.ContentBlock switch
        {
            ClaudeTextContentBlockWire => (CanonicalStreamPartKind.Text, null, null),
            ClaudeToolUseContentBlockWire toolUse =>
                (CanonicalStreamPartKind.ToolCall, toolUse.Id, toolUse.Name),
            ClaudeThinkingContentBlockWire or ClaudeRedactedThinkingContentBlockWire =>
                (CanonicalStreamPartKind.Reasoning, null, null),
            ClaudeImageContentBlockWire => (new CanonicalStreamPartKind("image"), null, null),
            ClaudeDocumentContentBlockWire =>
                (new CanonicalStreamPartKind("document"), null, null),
            ClaudeToolResultContentBlockWire =>
                (new CanonicalStreamPartKind("tool_result"), null, null),
            ClaudeUnknownContentBlockWire unknown =>
                (new CanonicalStreamPartKind(unknown.Type), null, null),
            _ => throw new ArgumentOutOfRangeException(
                nameof(blockStart),
                blockStart.ContentBlock.GetType().Name,
                "Unsupported Claude content block."),
        };

        return new CanonicalStreamContentPartStarted(
            blockStart.Index,
            kind,
            toolCallId,
            toolName,
            NoExtensions);
    }

    private static ImmutableArray<CanonicalStreamEvent> MapBlockDelta(
        ClaudeContentBlockDeltaEventWire blockDelta) =>
        blockDelta.Delta.Type switch
        {
            "text_delta" => One(new CanonicalStreamContentPartDelta(
                blockDelta.Index,
                CanonicalStreamPartKind.Text,
                blockDelta.Delta.Text,
                JsonDelta: null,
                NoExtensions)),
            "input_json_delta" => One(new CanonicalStreamContentPartDelta(
                blockDelta.Index,
                CanonicalStreamPartKind.ToolCall,
                TextDelta: null,
                blockDelta.Delta.PartialJson,
                NoExtensions)),
            "thinking_delta" => One(new CanonicalStreamContentPartDelta(
                blockDelta.Index,
                CanonicalStreamPartKind.Reasoning,
                blockDelta.Delta.Thinking,
                JsonDelta: null,
                NoExtensions)),
            "signature_delta" => One(new CanonicalStreamContentPartDelta(
                blockDelta.Index,
                CanonicalStreamPartKind.Reasoning,
                TextDelta: null,
                JsonDelta: null,
                ImmutableArray.Create(new CanonicalVendorExtension(
                    "claude",
                    "signature_delta",
                    JsonSerializer.SerializeToElement(blockDelta.Delta.Signature))))),
            _ => ImmutableArray<CanonicalStreamEvent>.Empty,
        };

    private static CanonicalStreamMessageDelta MapMessageDelta(
        ClaudeMessageDeltaEventWire messageDelta) =>
        new(
            messageDelta.Delta.StopReason is { } stopReason
                ? new CanonicalStop(MapStopReason(stopReason), messageDelta.Delta.StopSequence)
                : null,
            new CanonicalUsage(
                InputTokens: null,
                messageDelta.Usage.OutputTokens,
                TotalTokens: null,
                CacheCreationInputTokens: null,
                CacheReadInputTokens: null,
                ReasoningTokens: null),
            NoExtensions);

    private static CanonicalStopReason MapStopReason(string reason) =>
        reason switch
        {
            "end_turn" or "stop" => CanonicalStopReason.EndTurn,
            "tool_use" => CanonicalStopReason.ToolUse,
            "max_tokens" => CanonicalStopReason.MaxTokens,
            "pause_turn" => CanonicalStopReason.PauseTurn,
            "refusal" => CanonicalStopReason.Refusal,
            "model_context_window_exceeded" => CanonicalStopReason.ModelContextWindowExceeded,
            _ => new CanonicalStopReason(reason),
        };

    private static ImmutableArray<CanonicalStreamEvent> One(CanonicalStreamEvent streamEvent) =>
        ImmutableArray.Create(streamEvent);
}
