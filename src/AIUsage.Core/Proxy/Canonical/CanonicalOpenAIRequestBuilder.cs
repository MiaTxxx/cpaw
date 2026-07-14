using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;

namespace AIUsage.Core.Proxy.Canonical;

public static class CanonicalOpenAIRequestBuilder
{
    public static CanonicalBuildResult<OpenAIChatCompletionRequestWire> BuildChatCompletionRequest(
        CanonicalRequest request,
        string? modelOverride = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = new List<OpenAIChatMessageWire>();
        if (BuildMessageContent(request.System) is { } systemContent)
        {
            messages.Add(new OpenAIChatMessageWire
            {
                Role = "system",
                Content = systemContent,
            });
        }

        var index = 0;
        while (index < request.Items.Length)
        {
            switch (request.Items[index])
            {
                case CanonicalMessage message when message.Role.Value == "assistant":
                    var toolCalls = ImmutableArray.CreateBuilder<OpenAIChatToolCallWire>();
                    var nextIndex = index + 1;
                    while (nextIndex < request.Items.Length
                        && request.Items[nextIndex] is CanonicalToolCall toolCall)
                    {
                        toolCalls.Add(BuildToolCall(toolCall));
                        nextIndex++;
                    }

                    var assistantContent = BuildMessageContent(message.Parts);
                    if (assistantContent is not null || toolCalls.Count > 0)
                    {
                        messages.Add(new OpenAIChatMessageWire
                        {
                            Role = message.Role.Value,
                            Content = assistantContent,
                            Name = message.Name,
                            ToolCalls = toolCalls.Count == 0 ? null : toolCalls.ToImmutable(),
                        });
                    }

                    index = nextIndex;
                    break;
                case CanonicalMessage message:
                    if (BuildMessageContent(message.Parts) is not { } content)
                    {
                        throw new NotSupportedException(
                            $"Canonical {message.Role.Value} message has no OpenAI chat content.");
                    }

                    messages.Add(new OpenAIChatMessageWire
                    {
                        Role = message.Role.Value,
                        Content = content,
                        Name = message.Name,
                    });
                    index++;
                    break;
                case CanonicalToolCall toolCall:
                    messages.Add(new OpenAIChatMessageWire
                    {
                        Role = "assistant",
                        ToolCalls = [BuildToolCall(toolCall)],
                    });
                    index++;
                    break;
                case CanonicalToolResult toolResult:
                    messages.Add(new OpenAIChatMessageWire
                    {
                        Role = "tool",
                        Content = BuildToolResultContent(toolResult),
                        ToolCallId = toolResult.ToolCallId,
                    });
                    index++;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Canonical item {request.Items[index].GetType().Name} is not mapped by this chat slice.");
            }
        }

        var payload = new OpenAIChatCompletionRequestWire
        {
            Model = modelOverride ?? request.ModelHint,
            Messages = messages,
            Temperature = request.GenerationConfig.Temperature,
            TopP = request.GenerationConfig.TopP,
            MaxTokens = request.GenerationConfig.MaxOutputTokens,
            Stop = request.GenerationConfig.StopSequences.IsEmpty
                ? null
                : request.GenerationConfig.StopSequences,
            Stream = request.GenerationConfig.Stream,
            Tools = BuildTools(request.Tools),
            ToolChoice = BuildToolChoice(request.ToolConfig?.Choice),
            ParallelToolCalls = request.ToolConfig?.ParallelCallsAllowed,
        };
        return new CanonicalBuildResult<OpenAIChatCompletionRequestWire>(payload, []);
    }

    private static OpenAIMessageContentWire? BuildMessageContent(
        ImmutableArray<CanonicalContentPart> parts)
    {
        if (parts.IsDefaultOrEmpty)
        {
            return null;
        }

        var converted = parts.Select(BuildContentPart).ToArray();
        return converted.Length == 1 && converted[0] is OpenAITextContentPartWire text
            ? new OpenAITextMessageContentWire { Text = text.Text }
            : new OpenAIPartsMessageContentWire { Parts = converted };
    }

    private static OpenAIMessageContentWire BuildToolResultContent(CanonicalToolResult result)
    {
        if (result.Parts.IsDefaultOrEmpty)
        {
            return new OpenAITextMessageContentWire { Text = result.RawTextFallback ?? string.Empty };
        }

        var converted = result.Parts.Select(BuildContentPart).ToArray();
        if (converted.All(part => part is OpenAITextContentPartWire))
        {
            var text = converted
                .Cast<OpenAITextContentPartWire>()
                .Select(part => part.Text)
                .Where(value => value.Length > 0);
            var joined = string.Join("\n", text);
            return new OpenAITextMessageContentWire
            {
                Text = joined.Length == 0 ? result.RawTextFallback ?? string.Empty : joined,
            };
        }

        return new OpenAIPartsMessageContentWire { Parts = converted };
    }

    private static OpenAIContentPartWire BuildContentPart(CanonicalContentPart part) =>
        part switch
        {
            CanonicalTextPart text => new OpenAITextContentPartWire { Text = text.Text },
            CanonicalImagePart image when image.Source.Value == "base64" && image.MediaType is not null =>
                new OpenAIImageUrlContentPartWire
                {
                    ImageUrl = new OpenAIImageUrlWire
                    {
                        Url = $"data:{image.MediaType};base64,{image.Data}",
                        Detail = image.Detail,
                    },
                },
            CanonicalImagePart image when image.Source.Value == "url" =>
                new OpenAIImageUrlContentPartWire
                {
                    ImageUrl = new OpenAIImageUrlWire
                    {
                        Url = image.Data,
                        Detail = image.Detail,
                    },
                },
            CanonicalDocumentPart { Source: CanonicalFileIdDocumentSource file } document =>
                new OpenAIFileContentPartWire
                {
                    File = new OpenAIFileDescriptorWire
                    {
                        FileId = file.FileId,
                        Filename = document.Title,
                    },
                },
            _ => throw new NotSupportedException(
                $"Canonical content part {part.GetType().Name} is not mapped by this chat slice."),
        };

    private static OpenAIChatToolCallWire BuildToolCall(CanonicalToolCall toolCall) =>
        new()
        {
            Id = toolCall.Id,
            Type = "function",
            Function = new OpenAIChatFunctionCallWire
            {
                Name = toolCall.Name,
                Arguments = toolCall.InputJson,
            },
        };

    private static IReadOnlyList<OpenAIChatToolWire>? BuildTools(
        ImmutableArray<CanonicalToolDefinition> tools)
    {
        if (tools.IsDefaultOrEmpty)
        {
            return null;
        }

        return tools.Select(tool =>
        {
            if (tool.Kind.Value != "function" || tool.Name is null)
            {
                throw new NotSupportedException(
                    $"Canonical tool kind {tool.Kind.Value} is not mapped by this chat slice.");
            }

            return new OpenAIChatToolWire
            {
                Type = "function",
                Function = new OpenAIChatFunctionWire
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = tool.InputSchema?.ToDictionary(
                        property => property.Key,
                        property => property.Value.Clone(),
                        StringComparer.Ordinal),
                },
            };
        }).ToArray();
    }

    private static JsonElement? BuildToolChoice(CanonicalToolChoice? choice) =>
        choice switch
        {
            null => null,
            CanonicalNoneToolChoice => JsonSerializer.SerializeToElement("none"),
            CanonicalAutoToolChoice => JsonSerializer.SerializeToElement("auto"),
            CanonicalRequiredToolChoice => JsonSerializer.SerializeToElement("required"),
            CanonicalSpecificToolChoice specific => JsonSerializer.SerializeToElement(new
            {
                type = "function",
                function = new { name = specific.Name },
            }),
            _ => throw new NotSupportedException(
                $"Canonical tool choice {choice.GetType().Name} is not mapped by this chat slice."),
        };
}
