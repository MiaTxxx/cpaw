using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.Anthropic;

namespace AIUsage.Core.Proxy.Canonical;

public static class CanonicalClaudeStreamBuilder
{
    public static ImmutableArray<ClaudeStreamEventWire> Build(CanonicalStreamEvent streamEvent)
    {
        ArgumentNullException.ThrowIfNull(streamEvent);

        return streamEvent switch
        {
            CanonicalStreamMessageStarted started => Single(BuildMessageStart(started)),
            CanonicalStreamContentPartStarted started => BuildContentPartStart(started),
            CanonicalStreamContentPartDelta delta => BuildContentPartDelta(delta),
            CanonicalStreamContentPartStopped stopped => Single(
                new ClaudeContentBlockStopEventWire
                {
                    Type = "content_block_stop",
                    Index = stopped.Index,
                }),
            CanonicalStreamMessageDelta delta => Single(BuildMessageDelta(delta)),
            CanonicalStreamMessageStopped => Single(
                new ClaudeMessageStopEventWire { Type = "message_stop" }),
            CanonicalStreamError => Single(BuildError()),
            _ => throw new ArgumentOutOfRangeException(
                nameof(streamEvent),
                streamEvent.GetType(),
                "Unsupported canonical stream event."),
        };
    }

    private static ClaudeMessageStartEventWire BuildMessageStart(
        CanonicalStreamMessageStarted started) =>
        new()
        {
            Type = "message_start",
            Message = new ClaudeMessageStartWire
            {
                Id = started.MessageId ?? Guid.NewGuid().ToString("D").ToUpperInvariant(),
                Type = "message",
                Role = started.Role.Value,
                Content = Array.Empty<ClaudeContentBlockWire>(),
                Model = started.Model ?? "claude",
                Usage = new ClaudeUsageWire
                {
                    InputTokens = 0,
                    OutputTokens = 0,
                },
            },
        };

    private static ImmutableArray<ClaudeStreamEventWire> BuildContentPartStart(
        CanonicalStreamContentPartStarted started)
    {
        ClaudeContentBlockWire? contentBlock = started.Kind.Value switch
        {
            "text" => new ClaudeTextContentBlockWire { Text = string.Empty },
            "reasoning" => new ClaudeThinkingContentBlockWire
            {
                Thinking = string.Empty,
            },
            "tool_call" => new ClaudeToolUseContentBlockWire
            {
                Id = started.ToolCallId ?? $"tool_{started.Index}",
                Name = started.ToolName ?? "tool",
                Input = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            },
            _ => null,
        };

        return contentBlock is null
            ? []
            : Single(new ClaudeContentBlockStartEventWire
            {
                Type = "content_block_start",
                Index = started.Index,
                ContentBlock = contentBlock,
            });
    }

    private static ImmutableArray<ClaudeStreamEventWire> BuildContentPartDelta(
        CanonicalStreamContentPartDelta delta)
    {
        if (delta.TextDelta is not null)
        {
            return delta.Kind.Value switch
            {
                "text" => Single(BuildContentDelta(
                    delta.Index,
                    new ClaudeContentDeltaWire
                    {
                        Type = "text_delta",
                        Text = delta.TextDelta,
                    })),
                "reasoning" => Single(BuildContentDelta(
                    delta.Index,
                    new ClaudeContentDeltaWire
                    {
                        Type = "thinking_delta",
                        Thinking = delta.TextDelta,
                    })),
                _ => [],
            };
        }

        if (delta.JsonDelta is not null)
        {
            return Single(BuildContentDelta(
                delta.Index,
                new ClaudeContentDeltaWire
                {
                    Type = "input_json_delta",
                    PartialJson = delta.JsonDelta,
                }));
        }

        if (delta.Kind.Value == "reasoning")
        {
            var extension = delta.RawExtensions.FirstOrDefault(value =>
                value.Vendor == "claude" && value.Key == "signature_delta");
            if (extension is not null && extension.Value.ValueKind == JsonValueKind.String)
            {
                return Single(BuildContentDelta(
                    delta.Index,
                    new ClaudeContentDeltaWire
                    {
                        Type = "signature_delta",
                        Signature = extension.Value.GetString()!,
                    }));
            }
        }

        return [];
    }

    private static ClaudeContentBlockDeltaEventWire BuildContentDelta(
        long index,
        ClaudeContentDeltaWire delta) =>
        new()
        {
            Type = "content_block_delta",
            Index = index,
            Delta = delta,
        };

    private static ClaudeMessageDeltaEventWire BuildMessageDelta(
        CanonicalStreamMessageDelta delta) =>
        new()
        {
            Type = "message_delta",
            Delta = new ClaudeMessageDeltaContentWire
            {
                StopReason = delta.Stop?.Reason.Value,
                StopSequence = delta.Stop?.Sequence,
            },
            Usage = new ClaudeUsageDeltaWire
            {
                OutputTokens = delta.Usage?.OutputTokens ?? 0,
            },
        };

    private static ClaudeMessageDeltaEventWire BuildError() =>
        new()
        {
            Type = "message_delta",
            Delta = new ClaudeMessageDeltaContentWire
            {
                StopReason = "error",
            },
            Usage = new ClaudeUsageDeltaWire
            {
                OutputTokens = 0,
            },
        };

    private static ImmutableArray<ClaudeStreamEventWire> Single(
        ClaudeStreamEventWire streamEvent) =>
        ImmutableArray.Create(streamEvent);
}
