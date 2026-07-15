using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;

namespace AIUsage.Core.Proxy.Canonical;

public static partial class CanonicalRequestMapper
{
    public static CanonicalRequest FromOpenAIChat(OpenAIChatCompletionRequestWire request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (system, items) = MapOpenAIChatMessages(request.Messages);
        return new CanonicalRequest(
            request.Model,
            system,
            items,
            request.Tools?.Select(MapOpenAIChatTool).ToImmutableArray() ?? [],
            MapOpenAIChatToolConfig(request.ToolChoice, request.ParallelToolCalls),
            new CanonicalGenerationConfig(
                request.MaxTokens,
                request.Temperature,
                request.TopP,
                TopK: null,
                request.Stop?.ToImmutableArray() ?? [],
                request.Stream),
            ImmutableDictionary<string, JsonElement>.Empty,
            []);
    }

    private static (
        ImmutableArray<CanonicalContentPart> System,
        ImmutableArray<CanonicalConversationItem> Items) MapOpenAIChatMessages(
            IReadOnlyList<OpenAIChatMessageWire> messages)
    {
        var system = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        var items = ImmutableArray.CreateBuilder<CanonicalConversationItem>();

        foreach (var message in messages)
        {
            var role = new CanonicalRole(message.Role);
            var parts = MapOpenAIChatContent(message.Content);
            if (role.Value == CanonicalRole.System.Value)
            {
                system.AddRange(parts);
                continue;
            }

            if (role.Value == CanonicalRole.Tool.Value)
            {
                items.Add(new CanonicalToolResult(
                    message.ToolCallId ?? string.Empty,
                    IsError: null,
                    parts,
                    FlattenOpenAIChatText(parts),
                    []));
                continue;
            }

            if (role.Value == CanonicalRole.Assistant.Value
                && !string.IsNullOrEmpty(message.ReasoningContent))
            {
                items.Add(new CanonicalReasoningItem(
                    SummaryText: null,
                    FullText: message.ReasoningContent,
                    EncryptedContent: null,
                    Signature: null,
                    Redacted: null,
                    RawExtensions: []));
            }

            if (!parts.IsEmpty)
            {
                items.Add(new CanonicalMessage(
                    role,
                    Phase: null,
                    parts,
                    message.Name,
                    ImmutableDictionary<string, JsonElement>.Empty,
                    []));
            }

            if (message.ToolCalls is not null)
            {
                foreach (var toolCall in message.ToolCalls)
                {
                    items.Add(new CanonicalToolCall(
                        toolCall.Id,
                        toolCall.Function.Name,
                        toolCall.Function.Arguments,
                        CanonicalItemStatus.Completed,
                        Partial: false,
                        RawExtensions: []));
                }
            }
        }

        return (system.ToImmutable(), items.ToImmutable());
    }

    private static ImmutableArray<CanonicalContentPart> MapOpenAIChatContent(
        OpenAIMessageContentWire? content)
    {
        if (content is null)
        {
            return [];
        }

        if (content is OpenAITextMessageContentWire text)
        {
            return text.Text.Length == 0
                ? []
                : [new CanonicalTextPart(text.Text, [])];
        }

        if (content is not OpenAIPartsMessageContentWire parts)
        {
            throw new NotSupportedException(
                $"OpenAI chat content {content.GetType().Name} is not mapped by this canonical slice.");
        }

        var mapped = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        foreach (var part in parts.Parts)
        {
            switch (part)
            {
                case OpenAITextContentPartWire textPart:
                    mapped.Add(new CanonicalTextPart(textPart.Text, []));
                    break;
                case OpenAIImageUrlContentPartWire imagePart:
                    mapped.Add(new CanonicalImagePart(
                        CanonicalImageSource.Url,
                        imagePart.ImageUrl.Url,
                        MediaType: null,
                        imagePart.ImageUrl.Detail,
                        []));
                    break;
                case OpenAIFileContentPartWire filePart:
                    mapped.Add(new CanonicalFileReferencePart(
                        filePart.File.FileId,
                        filePart.File.Filename,
                        MimeType: null,
                        Downloadable: null,
                        RawExtensions: []));
                    break;
                case OpenAIUnknownContentPartWire:
                    break;
                default:
                    throw new NotSupportedException(
                        $"OpenAI chat content part {part.GetType().Name} is not mapped by this canonical slice.");
            }
        }

        return mapped.ToImmutable();
    }

    private static string? FlattenOpenAIChatText(
        ImmutableArray<CanonicalContentPart> parts)
    {
        var text = parts
            .OfType<CanonicalTextPart>()
            .Select(part => part.Text)
            .ToArray();
        return text.Length == 0 ? null : string.Join("\n", text);
    }

    private static CanonicalToolDefinition MapOpenAIChatTool(OpenAIChatToolWire tool) =>
        new(
            tool.Type == "function"
                ? CanonicalToolDefinitionKind.Function
                : new CanonicalToolDefinitionKind(tool.Type),
            tool.Function.Name,
            tool.Function.Description,
            tool.Function.Parameters?.ToImmutableDictionary(
                property => property.Key,
                property => property.Value.Clone(),
                StringComparer.Ordinal),
            CanonicalToolExecution.Client,
            tool.Type,
            new CanonicalToolDefinitionFlags(
                EagerInputStreaming: null,
                Strict: null),
            []);

    private static CanonicalToolConfig? MapOpenAIChatToolConfig(
        JsonElement? choice,
        bool? parallelToolCalls)
    {
        if (choice is null)
        {
            return parallelToolCalls is null
                ? null
                : new CanonicalToolConfig(Choice: null, parallelToolCalls);
        }

        return new CanonicalToolConfig(
            MapOpenAIChatToolChoice(choice.Value),
            parallelToolCalls);
    }

    private static CanonicalToolChoice MapOpenAIChatToolChoice(JsonElement choice)
    {
        if (choice.ValueKind == JsonValueKind.String)
        {
            return choice.GetString() switch
            {
                "none" => new CanonicalNoneToolChoice(),
                "required" => new CanonicalRequiredToolChoice(),
                _ => new CanonicalAutoToolChoice(),
            };
        }

        if (choice.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "OpenAI chat tool_choice must be a string or an object.");
        }

        if (!choice.TryGetProperty("type", out var type)
            || type.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                "OpenAI chat tool_choice object requires a string type.");
        }

        string? functionName = null;
        if (choice.TryGetProperty("function", out var function)
            && function.ValueKind != JsonValueKind.Null)
        {
            if (function.ValueKind != JsonValueKind.Object
                || !function.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    "OpenAI chat tool_choice function requires a string name.");
            }

            functionName = name.GetString();
        }

        if (type.GetString() == "function" && functionName is not null)
        {
            return new CanonicalSpecificToolChoice(functionName);
        }

        return new CanonicalAutoToolChoice();
    }
}
