using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols;
using AIUsage.Core.Proxy.Protocols.Anthropic;

namespace AIUsage.Core.Proxy.Canonical;

public static class CanonicalRequestMapper
{
    public static CanonicalRequest FromClaude(ClaudeMessageRequestWire request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var system = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        system.AddRange(MapSystem(request.System));

        var items = ImmutableArray.CreateBuilder<CanonicalConversationItem>();
        foreach (var message in request.Messages)
        {
            foreach (var item in MapMessage(message))
            {
                if (item is CanonicalMessage { Role.Value: "system" } systemMessage)
                {
                    system.AddRange(systemMessage.Parts);
                }
                else
                {
                    items.Add(item);
                }
            }
        }

        var metadata = ImmutableDictionary.CreateBuilder<string, JsonElement>(StringComparer.Ordinal);
        if (request.Metadata?.UserId is { } userId)
        {
            metadata["user_id"] = JsonSerializer.SerializeToElement(userId);
        }

        // Swift Canonical v1 does not promote request-level thinking/output_config
        // or arbitrary wire extension data. Keep parity until dedicated goldens
        // define explicit canonical or lossy-extension semantics for those fields.
        return new CanonicalRequest(
            request.Model,
            system.ToImmutable(),
            items.ToImmutable(),
            request.Tools?.Select(MapTool).ToImmutableArray() ?? [],
            MapToolConfig(request.ToolChoice),
            new CanonicalGenerationConfig(
                request.MaxTokens,
                request.Temperature,
                request.TopP,
                request.TopK,
                request.StopSequences?.ToImmutableArray() ?? [],
                request.Stream),
            metadata.ToImmutable(),
            []);
    }

    private static ImmutableArray<CanonicalContentPart> MapSystem(JsonElement? system)
    {
        if (system is not { } value)
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString() ?? string.Empty;
            return text.Length == 0
                ? []
                : [new CanonicalTextPart(text, [])];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Claude system content must be a string or an array of blocks.");
        }

        var parts = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        foreach (var block in value.EnumerateArray())
        {
            if (block.ValueKind != JsonValueKind.Object
                || !block.TryGetProperty("text", out var textValue)
                || textValue.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = textValue.GetString() ?? string.Empty;
            parts.Add(new CanonicalTextPart(text, ReadRawExtension(block, "cache_control")));
        }

        return parts.ToImmutable();
    }

    private static ImmutableArray<CanonicalConversationItem> MapMessage(ClaudeMessageWire message) =>
        message.Content switch
        {
            ClaudeTextMessageContentWire text =>
            [CreateMessage(message.Role, [new CanonicalTextPart(text.Text, [])])],
            ClaudeBlocksMessageContentWire blocks => MapBlocks(message.Role, blocks.Blocks),
            _ => throw new NotSupportedException(
                $"Unsupported Claude message content type {message.Content.GetType().Name}."),
        };

    private static ImmutableArray<CanonicalConversationItem> MapBlocks(
        string role,
        IReadOnlyList<ClaudeContentBlockWire> blocks)
    {
        var items = ImmutableArray.CreateBuilder<CanonicalConversationItem>();
        var pendingParts = ImmutableArray.CreateBuilder<CanonicalContentPart>();

        void FlushMessage()
        {
            if (pendingParts.Count == 0)
            {
                return;
            }

            items.Add(CreateMessage(role, pendingParts.ToImmutable()));
            pendingParts.Clear();
        }

        foreach (var block in blocks)
        {
            switch (block)
            {
                case ClaudeTextContentBlockWire text:
                    pendingParts.Add(new CanonicalTextPart(
                        text.Text,
                        ReadRawExtension(text.CacheControl, "cache_control")));
                    break;
                case ClaudeDocumentContentBlockWire document:
                    pendingParts.Add(MapDocument(document));
                    break;
                case ClaudeImageContentBlockWire image:
                    pendingParts.Add(MapImage(image));
                    break;
                case ClaudeThinkingContentBlockWire thinking:
                    FlushMessage();
                    items.Add(new CanonicalReasoningItem(
                        SummaryText: null,
                        FullText: thinking.Thinking,
                        EncryptedContent: null,
                        Signature: thinking.Signature,
                        Redacted: null,
                        RawExtensions: []));
                    break;
                case ClaudeToolUseContentBlockWire toolUse:
                    FlushMessage();
                    items.Add(new CanonicalToolCall(
                        toolUse.Id,
                        toolUse.Name,
                        JsonSerializer.Serialize(toolUse.Input),
                        CanonicalItemStatus.Completed,
                        Partial: false,
                        RawExtensions: []));
                    break;
                case ClaudeToolResultContentBlockWire toolResult:
                    FlushMessage();
                    items.Add(MapToolResult(toolResult));
                    break;
                case ClaudeRedactedThinkingContentBlockWire redacted:
                    FlushMessage();
                    items.Add(new CanonicalReasoningItem(
                        SummaryText: null,
                        FullText: null,
                        EncryptedContent: null,
                        Signature: null,
                        Redacted: true,
                        RawExtensions:
                        [
                            new CanonicalVendorExtension(
                                "claude",
                                "redacted_data",
                                JsonSerializer.SerializeToElement(redacted.Data)),
                        ]));
                    break;
                case ClaudeUnknownContentBlockWire unknown:
                    pendingParts.Add(MapUnknown(unknown));
                    break;
            }
        }

        FlushMessage();
        return items.ToImmutable();
    }

    private static CanonicalMessage CreateMessage(
        string role,
        ImmutableArray<CanonicalContentPart> parts) =>
        new(
            new CanonicalRole(role),
            Phase: null,
            parts,
            Name: null,
            Metadata: ImmutableDictionary<string, JsonElement>.Empty,
            RawExtensions: []);

    private static CanonicalDocumentPart MapDocument(ClaudeDocumentContentBlockWire document)
    {
        var sourceType = ReadString(document.Source, "type");
        CanonicalDocumentSource source = sourceType switch
        {
            "file" => new CanonicalFileIdDocumentSource(ReadString(document.Source, "file_id") ?? string.Empty),
            "text" => new CanonicalInlineTextDocumentSource(ReadString(document.Source, "text") ?? string.Empty),
            "url" => new CanonicalUrlDocumentSource(ReadString(document.Source, "url") ?? string.Empty),
            "base64" => new CanonicalBase64DocumentSource(
                ReadString(document.Source, "data") ?? string.Empty,
                ReadString(document.Source, "media_type")),
            _ => new CanonicalUnknownDocumentSource(JsonSerializer.SerializeToElement(document.Source)),
        };

        return new CanonicalDocumentPart(
            source,
            document.Title,
            document.Context,
            document.Citations?.Clone(),
            ReadRawExtension(document.CacheControl, "cache_control"));
    }

    private static CanonicalImagePart MapImage(ClaudeImageContentBlockWire image)
    {
        var source = new CanonicalImageSource(image.Source.Type switch
        {
            "file" => "file_id",
            "file_id" => "file_id",
            _ => image.Source.Type,
        });
        var data = image.Source.Type == "url"
            ? image.Source.Url ?? string.Empty
            : image.Source.Data ?? string.Empty;
        return new CanonicalImagePart(
            source,
            data,
            image.Source.MediaType,
            Detail: null,
            RawExtensions: []);
    }

    private static CanonicalUnknownPart MapUnknown(ClaudeUnknownContentBlockWire unknown) =>
        new(unknown.Discriminator, unknown.Value, []);

    private static CanonicalToolResult MapToolResult(ClaudeToolResultContentBlockWire result)
    {
        var parts = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        string? rawTextFallback = null;

        if (result.Content is { } content)
        {
            if (content.ValueKind == JsonValueKind.String)
            {
                rawTextFallback = content.GetString();
                if (rawTextFallback is not null)
                {
                    parts.Add(new CanonicalTextPart(rawTextFallback, []));
                }
            }
            else if (content.ValueKind == JsonValueKind.Array)
            {
                var textParts = new List<string>();
                foreach (var block in content.EnumerateArray())
                {
                    var mapped = MapToolResultPart(WireJson.Deserialize<ClaudeContentBlockWire>(block));
                    parts.Add(mapped);
                    if (mapped is CanonicalTextPart text)
                    {
                        textParts.Add(text.Text);
                    }
                }

                rawTextFallback = string.Join("\n", textParts);
            }
            else
            {
                throw new NotSupportedException(
                    $"Claude tool-result content kind {content.ValueKind} is not mapped by this canonical slice.");
            }
        }

        return new CanonicalToolResult(
            result.ToolUseId,
            result.IsError,
            parts.ToImmutable(),
            rawTextFallback,
            []);
    }

    private static CanonicalContentPart MapToolResultPart(ClaudeContentBlockWire block) =>
        block switch
        {
            ClaudeTextContentBlockWire text => new CanonicalTextPart(text.Text, []),
            ClaudeImageContentBlockWire image => MapImage(image),
            ClaudeDocumentContentBlockWire document => MapDocument(document),
            ClaudeThinkingContentBlockWire thinking => new CanonicalReasoningTextPart(thinking.Thinking, []),
            ClaudeRedactedThinkingContentBlockWire redacted => new CanonicalUnknownPart(
                redacted.Type,
                JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                {
                    ["data"] = redacted.Data,
                }),
                []),
            ClaudeUnknownContentBlockWire unknown => MapUnknown(unknown),
            ClaudeToolUseContentBlockWire toolUse => new CanonicalUnknownPart(
                toolUse.Type,
                JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                {
                    ["id"] = toolUse.Id,
                    ["name"] = toolUse.Name,
                    ["input"] = toolUse.Input,
                }),
                []),
            ClaudeToolResultContentBlockWire nested => new CanonicalUnknownPart(
                nested.Type,
                JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                {
                    ["tool_use_id"] = nested.ToolUseId,
                    ["content"] = ReadToolResultFallback(nested.Content) ?? string.Empty,
                }),
                []),
            _ => throw new NotSupportedException(
                $"Claude tool-result block type {block.Type} is not mapped."),
        };

    private static string? ReadToolResultFallback(JsonElement? content)
    {
        if (content is not { } value)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var text = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && item.TryGetProperty("text", out _))
            .Select(item => item.GetProperty("text").GetString() ?? string.Empty)
            .ToArray();
        return text.Length == 0 ? null : string.Join("\n", text);
    }

    private static CanonicalToolDefinition MapTool(ClaudeToolWire tool) =>
        new(
            CanonicalToolDefinitionKind.Function,
            tool.Name,
            tool.Description,
            tool.InputSchema.ToImmutableDictionary(
                property => property.Key,
                property => property.Value.Clone(),
                StringComparer.Ordinal),
            CanonicalToolExecution.Client,
            "claude_function",
            new CanonicalToolDefinitionFlags(tool.EagerInputStreaming, Strict: null),
            []);

    private static CanonicalToolConfig? MapToolConfig(ClaudeToolChoiceWire? choice)
    {
        if (choice is null)
        {
            return null;
        }

        CanonicalToolChoice canonicalChoice = choice.Type switch
        {
            "none" => new CanonicalNoneToolChoice(),
            "auto" => new CanonicalAutoToolChoice(),
            "any" => new CanonicalRequiredToolChoice(),
            "tool" when choice.Name is { } name => new CanonicalSpecificToolChoice(name),
            "tool" => new CanonicalRequiredToolChoice(),
            _ => new CanonicalUnknownToolChoice(choice.Type),
        };

        return new CanonicalToolConfig(
            canonicalChoice,
            choice.DisableParallelToolUse is { } disabled ? !disabled : null);
    }

    private static ImmutableArray<CanonicalVendorExtension> ReadRawExtension(
        Dictionary<string, JsonElement>? value,
        string key) =>
        value is null
            ? []
            : [new CanonicalVendorExtension("claude", key, JsonSerializer.SerializeToElement(value))];

    private static ImmutableArray<CanonicalVendorExtension> ReadRawExtension(
        JsonElement value,
        string propertyName) =>
        value.TryGetProperty(propertyName, out var property)
            ? [new CanonicalVendorExtension("claude", propertyName, property.Clone())]
            : [];

    private static string? ReadString(Dictionary<string, JsonElement> values, string key) =>
        values.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
