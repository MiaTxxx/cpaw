using System.Collections.Immutable;

namespace AIUsage.Core.Proxy.Canonical;

public sealed class CanonicalOpenAIUpstreamStreamMapper
{
    private readonly CanonicalRole _role;
    private bool _didEmitMessageStart;
    private long _nextContentIndex;
    private long? _textContentIndex;
    private long? _reasoningContentIndex;
    private readonly Dictionary<long, long> _toolContentIndices = [];
    private readonly Dictionary<long, List<string>> _pendingToolArgumentDeltas = [];
    private readonly HashSet<long> _openContentIndices = [];

    public CanonicalOpenAIUpstreamStreamMapper()
        : this(CanonicalRole.Assistant)
    {
    }

    public CanonicalOpenAIUpstreamStreamMapper(CanonicalRole role)
    {
        _role = role;
    }

    public ImmutableArray<CanonicalStreamEvent> Map(OpenAIUpstreamStreamEvent upstreamEvent)
    {
        ArgumentNullException.ThrowIfNull(upstreamEvent);

        var mappedEvents = ImmutableArray.CreateBuilder<CanonicalStreamEvent>();
        EmitMessageStartIfNeeded(mappedEvents);

        switch (upstreamEvent)
        {
            case OpenAIUpstreamTextDelta textDelta:
                CloseReasoningContentIfNeeded(mappedEvents);
                var textIndex = EnsureTextContentPartStarted(mappedEvents);
                mappedEvents.Add(new CanonicalStreamContentPartDelta(
                    textIndex,
                    CanonicalStreamPartKind.Text,
                    textDelta.Text,
                    JsonDelta: null,
                    []));
                break;

            case OpenAIUpstreamReasoningSummaryDelta reasoningDelta:
                CloseTextContentIfNeeded(mappedEvents);
                var reasoningIndex = EnsureReasoningContentPartStarted(mappedEvents);
                mappedEvents.Add(new CanonicalStreamContentPartDelta(
                    reasoningIndex,
                    CanonicalStreamPartKind.Reasoning,
                    reasoningDelta.Text,
                    JsonDelta: null,
                    []));
                break;

            case OpenAIUpstreamToolCallStarted toolStarted:
                CloseReasoningContentIfNeeded(mappedEvents);
                CloseTextContentIfNeeded(mappedEvents);
                var toolIndex = EnsureToolContentPartStarted(
                    toolStarted.UpstreamIndex,
                    toolStarted.Id,
                    toolStarted.Name,
                    mappedEvents);
                _openContentIndices.Add(toolIndex);
                FlushPendingToolArgumentDeltas(
                    toolStarted.UpstreamIndex,
                    toolIndex,
                    mappedEvents);
                break;

            case OpenAIUpstreamToolCallArgumentsDelta toolArguments:
                if (_toolContentIndices.TryGetValue(toolArguments.UpstreamIndex, out var contentIndex))
                {
                    mappedEvents.Add(new CanonicalStreamContentPartDelta(
                        contentIndex,
                        CanonicalStreamPartKind.ToolCall,
                        TextDelta: null,
                        toolArguments.ArgumentsDelta,
                        []));
                }
                else if (!string.IsNullOrEmpty(toolArguments.ArgumentsDelta))
                {
                    if (!_pendingToolArgumentDeltas.TryGetValue(
                            toolArguments.UpstreamIndex,
                            out var pending))
                    {
                        pending = [];
                        _pendingToolArgumentDeltas[toolArguments.UpstreamIndex] = pending;
                    }

                    pending.Add(toolArguments.ArgumentsDelta);
                }

                break;

            case OpenAIUpstreamCompleted completed:
                CloseReasoningContentIfNeeded(mappedEvents);
                CloseTextContentIfNeeded(mappedEvents);
                foreach (var upstreamIndex in _pendingToolArgumentDeltas.Keys.Order())
                {
                    var pendingContentIndex = EnsureToolContentPartStarted(
                        upstreamIndex,
                        ToolCallId: null,
                        ToolName: null,
                        mappedEvents);
                    FlushPendingToolArgumentDeltas(upstreamIndex, pendingContentIndex, mappedEvents);
                }

                foreach (var openContentIndex in _openContentIndices.Order())
                {
                    mappedEvents.Add(new CanonicalStreamContentPartStopped(openContentIndex));
                }

                _openContentIndices.Clear();
                mappedEvents.Add(new CanonicalStreamMessageDelta(
                    completed.FinishReason is not null
                        ? new CanonicalStop(MapStopReason(completed.FinishReason), Sequence: null)
                        : null,
                    completed.Usage is not null ? MapUsage(completed.Usage) : null,
                    []));
                mappedEvents.Add(new CanonicalStreamMessageStopped());
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(upstreamEvent),
                    upstreamEvent.GetType().Name,
                    "Unsupported OpenAI upstream stream event.");
        }

        return mappedEvents.ToImmutable();
    }

    private void EmitMessageStartIfNeeded(
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_didEmitMessageStart)
        {
            return;
        }

        _didEmitMessageStart = true;
        mappedEvents.Add(new CanonicalStreamMessageStarted(
            _role,
            MessageId: null,
            Model: null,
            []));
    }

    private long EnsureTextContentPartStarted(
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_textContentIndex is { } existingIndex)
        {
            return existingIndex;
        }

        var index = _nextContentIndex++;
        _textContentIndex = index;
        _openContentIndices.Add(index);
        mappedEvents.Add(new CanonicalStreamContentPartStarted(
            index,
            CanonicalStreamPartKind.Text,
            ToolCallId: null,
            ToolName: null,
            []));
        return index;
    }

    private long EnsureReasoningContentPartStarted(
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_reasoningContentIndex is { } existingIndex)
        {
            return existingIndex;
        }

        var index = _nextContentIndex++;
        _reasoningContentIndex = index;
        _openContentIndices.Add(index);
        mappedEvents.Add(new CanonicalStreamContentPartStarted(
            index,
            CanonicalStreamPartKind.Reasoning,
            ToolCallId: null,
            ToolName: null,
            []));
        return index;
    }

    private long EnsureToolContentPartStarted(
        long upstreamIndex,
        string? ToolCallId,
        string? ToolName,
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_toolContentIndices.TryGetValue(upstreamIndex, out var existingIndex))
        {
            return existingIndex;
        }

        var index = _nextContentIndex++;
        _toolContentIndices[upstreamIndex] = index;
        _openContentIndices.Add(index);
        mappedEvents.Add(new CanonicalStreamContentPartStarted(
            index,
            CanonicalStreamPartKind.ToolCall,
            ToolCallId,
            ToolName,
            []));
        return index;
    }

    private void CloseTextContentIfNeeded(
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_textContentIndex is not { } index || !_openContentIndices.Remove(index))
        {
            return;
        }

        _textContentIndex = null;
        mappedEvents.Add(new CanonicalStreamContentPartStopped(index));
    }

    private void CloseReasoningContentIfNeeded(
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (_reasoningContentIndex is not { } index || !_openContentIndices.Remove(index))
        {
            return;
        }

        _reasoningContentIndex = null;
        mappedEvents.Add(new CanonicalStreamContentPartStopped(index));
    }

    private void FlushPendingToolArgumentDeltas(
        long upstreamIndex,
        long contentIndex,
        ImmutableArray<CanonicalStreamEvent>.Builder mappedEvents)
    {
        if (!_pendingToolArgumentDeltas.Remove(upstreamIndex, out var deltas))
        {
            return;
        }

        foreach (var delta in deltas.Where(delta => !string.IsNullOrEmpty(delta)))
        {
            mappedEvents.Add(new CanonicalStreamContentPartDelta(
                contentIndex,
                CanonicalStreamPartKind.ToolCall,
                TextDelta: null,
                delta,
                []));
        }
    }

    private static CanonicalUsage MapUsage(OpenAIUpstreamUsage usage) =>
        new(
            usage.EffectiveInputTokens,
            usage.CompletionTokens,
            usage.TotalTokens,
            CacheCreationInputTokens: null,
            usage.EffectiveCachedTokens,
            ReasoningTokens: null);

    private static CanonicalStopReason MapStopReason(string reason) =>
        reason switch
        {
            "stop" or "end_turn" => CanonicalStopReason.EndTurn,
            "tool_calls" => CanonicalStopReason.ToolUse,
            "length" => CanonicalStopReason.MaxTokens,
            "pause_turn" => CanonicalStopReason.PauseTurn,
            "refusal" or "content_filter" => CanonicalStopReason.Refusal,
            "model_context_window_exceeded" => CanonicalStopReason.ModelContextWindowExceeded,
            _ => new CanonicalStopReason(reason),
        };
}
