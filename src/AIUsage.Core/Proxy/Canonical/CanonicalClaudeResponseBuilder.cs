using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.Anthropic;

namespace AIUsage.Core.Proxy.Canonical;

public static class CanonicalClaudeResponseBuilder
{
    public static CanonicalBuildResult<ClaudeMessageResponseWire> BuildMessageResponse(
        CanonicalResponse response,
        string? originalModel = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        var content = new List<ClaudeContentBlockWire>();
        var lossyNotes = ImmutableArray.CreateBuilder<CanonicalLossyNote>();

        for (var itemIndex = 0; itemIndex < response.Items.Length; itemIndex++)
        {
            switch (response.Items[itemIndex])
            {
                case CanonicalMessage { Role.Value: "assistant" } message:
                    foreach (var part in message.Parts)
                    {
                        var block = BuildContentBlock(part, itemIndex, lossyNotes);
                        if (block is not null)
                        {
                            content.Add(block);
                        }
                    }
                    break;

                case CanonicalMessage:
                    break;

                case CanonicalToolCall toolCall:
                    content.Add(new ClaudeToolUseContentBlockWire
                    {
                        Id = toolCall.Id,
                        Name = toolCall.Name,
                        Input = ParseToolInput(toolCall.InputJson),
                    });
                    break;

                case CanonicalReasoningItem { Redacted: true } reasoning:
                    content.Add(new ClaudeRedactedThinkingContentBlockWire
                    {
                        Data = ReadRedactedData(reasoning),
                    });
                    break;

                case CanonicalReasoningItem reasoning:
                    var text = reasoning.FullText ?? reasoning.SummaryText;
                    if (text is not null)
                    {
                        content.Add(new ClaudeThinkingContentBlockWire
                        {
                            Thinking = text,
                            Signature = reasoning.Signature,
                        });
                    }
                    break;

                case CanonicalToolResult:
                    AddLossyNote(
                        lossyNotes,
                        "claude_tool_result_skipped_in_response",
                        "Canonical tool_result items are not emitted in Claude assistant responses and were skipped.",
                        itemIndex,
                        $"items[{itemIndex}]");
                    break;

                case CanonicalCompactionItem:
                    AddLossyNote(
                        lossyNotes,
                        "claude_compaction_skipped_in_response",
                        "Canonical compaction items are not representable in Claude assistant responses and were skipped.",
                        itemIndex,
                        $"items[{itemIndex}]");
                    break;

                case CanonicalHostedToolEvent:
                    AddLossyNote(
                        lossyNotes,
                        "claude_hosted_tool_event_skipped_in_response",
                        "Canonical hosted tool events are not representable in Claude assistant responses and were skipped.",
                        itemIndex,
                        $"items[{itemIndex}]");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(response),
                        response.Items[itemIndex].GetType(),
                        "Unsupported canonical response item.");
            }
        }

        if (content.Count == 0)
        {
            content.Add(new ClaudeTextContentBlockWire { Text = string.Empty });
        }

        return new CanonicalBuildResult<ClaudeMessageResponseWire>(
            new ClaudeMessageResponseWire
            {
                Id = response.Id ?? Guid.NewGuid().ToString("D").ToUpperInvariant(),
                Type = "message",
                Role = "assistant",
                Content = content,
                Model = originalModel ?? response.Model ?? "claude",
                StopReason = response.Stop.Reason.Value,
                StopSequence = response.Stop.Sequence,
                Usage = new ClaudeUsageWire
                {
                    InputTokens = response.Usage?.InputTokens ?? 0,
                    OutputTokens = response.Usage?.OutputTokens ?? 0,
                    CacheCreationInputTokens = response.Usage?.CacheCreationInputTokens,
                    CacheReadInputTokens = response.Usage?.CacheReadInputTokens,
                },
            },
            lossyNotes.ToImmutable());
    }

    private static ClaudeContentBlockWire? BuildContentBlock(
        CanonicalContentPart part,
        int itemIndex,
        ImmutableArray<CanonicalLossyNote>.Builder lossyNotes) =>
        part switch
        {
            CanonicalTextPart text => new ClaudeTextContentBlockWire { Text = text.Text },
            CanonicalImagePart { Source.Value: "base64", MediaType: not null } image =>
                new ClaudeImageContentBlockWire
                {
                    Source = new ClaudeImageSourceWire
                    {
                        Type = "base64",
                        MediaType = image.MediaType,
                        Data = image.Data,
                    },
                },
            CanonicalImagePart => SkipImage(itemIndex, lossyNotes),
            CanonicalDocumentPart document => BuildDocument(document),
            CanonicalFileReferencePart file => new ClaudeDocumentContentBlockWire
            {
                Source = JsonDictionary(("type", "file"), ("file_id", file.FileId ?? string.Empty)),
                Title = file.Filename,
            },
            CanonicalReasoningTextPart reasoning => new ClaudeThinkingContentBlockWire
            {
                Thinking = reasoning.Text,
            },
            CanonicalRefusalPart refusal => new ClaudeTextContentBlockWire { Text = refusal.Text },
            CanonicalUnknownPart unknown => SkipUnknown(unknown, itemIndex, lossyNotes),
            _ => throw new ArgumentOutOfRangeException(
                nameof(part),
                part.GetType(),
                "Unsupported canonical content part."),
        };

    private static ClaudeContentBlockWire? SkipImage(
        int itemIndex,
        ImmutableArray<CanonicalLossyNote>.Builder lossyNotes)
    {
        AddLossyNote(
            lossyNotes,
            "claude_non_base64_image_skipped",
            "Only base64 images are emitted to Claude content blocks; non-base64 canonical image was skipped.",
            itemIndex,
            $"items[{itemIndex}].message.parts");
        return null;
    }

    private static ClaudeContentBlockWire? SkipUnknown(
        CanonicalUnknownPart unknown,
        int itemIndex,
        ImmutableArray<CanonicalLossyNote>.Builder lossyNotes)
    {
        AddLossyNote(
            lossyNotes,
            "claude_unknown_part_skipped",
            $"Unknown canonical content part `{unknown.Type}` was skipped in Claude response builder.",
            itemIndex,
            $"items[{itemIndex}].message.parts");
        return null;
    }

    private static ClaudeDocumentContentBlockWire BuildDocument(CanonicalDocumentPart document) =>
        new()
        {
            Source = document.Source switch
            {
                CanonicalFileIdDocumentSource file =>
                    JsonDictionary(("type", "file"), ("file_id", file.FileId)),
                CanonicalInlineTextDocumentSource text =>
                    JsonDictionary(("type", "text"), ("text", text.Text)),
                CanonicalUrlDocumentSource url =>
                    JsonDictionary(("type", "url"), ("url", url.Url)),
                CanonicalBase64DocumentSource base64 => BuildBase64Source(base64),
                CanonicalUnknownDocumentSource unknown => BuildUnknownSource(unknown.Value),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(document),
                    document.Source.GetType(),
                    "Unsupported canonical document source."),
            },
            Title = document.Title,
            Context = document.Context,
            Citations = document.Citations?.Clone(),
        };

    private static Dictionary<string, JsonElement> BuildBase64Source(
        CanonicalBase64DocumentSource source)
    {
        var result = JsonDictionary(("type", "base64"), ("data", source.Data));
        if (source.MediaType is not null)
        {
            result["media_type"] = JsonSerializer.SerializeToElement(source.MediaType);
        }
        return result;
    }

    private static Dictionary<string, JsonElement> BuildUnknownSource(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.Clone(),
                StringComparer.Ordinal);
        }

        return JsonDictionary(("type", "text"), ("text", string.Empty));
    }

    private static Dictionary<string, JsonElement> ParseToolInput(string inputJson)
    {
        try
        {
            using var document = JsonDocument.Parse(inputJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => property.Value.Clone(),
                    StringComparer.Ordinal)
                : new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
    }

    private static string ReadRedactedData(CanonicalReasoningItem reasoning)
    {
        var extension = reasoning.RawExtensions.FirstOrDefault(value =>
            value.Vendor == "claude" && value.Key == "redacted_data");
        return extension is not null && extension.Value.ValueKind == JsonValueKind.String
            ? extension.Value.GetString()!
            : reasoning.EncryptedContent ?? string.Empty;
    }

    private static Dictionary<string, JsonElement> JsonDictionary(
        params (string Key, string Value)[] values) =>
        values.ToDictionary(
            value => value.Key,
            value => JsonSerializer.SerializeToElement(value.Value),
            StringComparer.Ordinal);

    private static void AddLossyNote(
        ImmutableArray<CanonicalLossyNote>.Builder notes,
        string code,
        string message,
        long itemIndex,
        string path) =>
        notes.Add(new CanonicalLossyNote(
            code,
            message,
            CanonicalLossySeverity.Warning,
            itemIndex,
            path,
            []));
}
