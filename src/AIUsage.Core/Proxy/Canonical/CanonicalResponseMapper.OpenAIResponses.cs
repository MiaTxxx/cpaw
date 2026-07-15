using System.Collections.Immutable;
using System.Text.Json;
using AIUsage.Core.Proxy.Protocols.OpenAIResponses;

namespace AIUsage.Core.Proxy.Canonical;

public static partial class CanonicalResponseMapper
{
    public static CanonicalResponse FromOpenAIResponses(OpenAIResponsesResponseWire response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var items = ImmutableArray.CreateBuilder<CanonicalConversationItem>();
        var hasFunctionCall = false;
        var hasPendingHostedTool = false;
        foreach (var output in response.Output)
        {
            var type = ReadRequiredString(output, "type", "OpenAI Responses output item");
            switch (type)
            {
                case "reasoning":
                    items.Add(MapOpenAIResponsesReasoning(output));
                    break;
                case "message":
                    items.Add(MapOpenAIResponsesMessage(output));
                    break;
                case "function_call":
                    hasFunctionCall = true;
                    items.Add(MapOpenAIResponsesFunctionCall(output));
                    break;
                case "function_call_output":
                    items.Add(MapOpenAIResponsesFunctionCallOutput(output));
                    break;
                case "compaction":
                    items.Add(new CanonicalCompactionItem(
                        ReadOptionalString(output, "id", "OpenAI Responses compaction"),
                        ReadRequiredString(output, "encrypted_content", "OpenAI Responses compaction"),
                        []));
                    break;
                case "file_search_call":
                case "web_search_call":
                    var hosted = MapOpenAIResponsesHostedToolEvent(output, type);
                    items.Add(hosted);
                    hasPendingHostedTool |= HostedToolStatusRequiresPauseTurn(hosted.Status.Value);
                    break;
                default:
                    items.Add(new CanonicalHostedToolEvent(
                        type,
                        null,
                        CanonicalItemStatus.Unknown,
                        JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                        {
                            ["type"] = type,
                        }),
                        []));
                    break;
            }
        }

        var stopReason = MapOpenAIResponsesStopReason(
            response.Status,
            hasPendingHostedTool,
            hasFunctionCall);

        return new CanonicalResponse(
            response.Id,
            response.Model,
            items.ToImmutable(),
            new CanonicalStop(stopReason, Sequence: null),
            MapOpenAIResponsesUsage(response.Usage),
            ImmutableArray<CanonicalVendorExtension>.Empty);
    }

    private static CanonicalReasoningItem MapOpenAIResponsesReasoning(JsonElement item) =>
        new(
            ReadJoinedText(item, "summary"),
            ReadJoinedText(item, "content"),
            ReadOptionalString(item, "encrypted_content", "OpenAI Responses reasoning item"),
            Signature: null,
            Redacted: null,
            RawExtensions: []);

    private static CanonicalMessage MapOpenAIResponsesMessage(JsonElement item)
    {
        var parts = ImmutableArray.CreateBuilder<CanonicalContentPart>();
        var content = ReadRequiredArray(item, "content", "OpenAI Responses message");
        foreach (var part in content.EnumerateArray())
        {
            var type = ReadRequiredString(part, "type", "OpenAI Responses message content");
            switch (type)
            {
                case "output_text":
                    parts.Add(new CanonicalTextPart(
                        ReadRequiredString(part, "text", "OpenAI Responses output_text content"),
                        []));
                    break;
                case "refusal":
                    parts.Add(new CanonicalRefusalPart(
                        ReadOptionalString(part, "refusal", "OpenAI Responses refusal content")
                            ?? string.Empty,
                        []));
                    break;
                default:
                    parts.Add(new CanonicalUnknownPart(type, null, []));
                    break;
            }
        }

        return new CanonicalMessage(
            new CanonicalRole(ReadRequiredString(item, "role", "OpenAI Responses message")),
            ReadOptionalString(item, "phase", "OpenAI Responses message"),
            parts.ToImmutable(),
            Name: null,
            ImmutableDictionary<string, JsonElement>.Empty,
            RawExtensions: []);
    }

    private static CanonicalToolCall MapOpenAIResponsesFunctionCall(JsonElement item)
    {
        var status = MapOpenAIResponsesItemStatus(
            ReadOptionalString(item, "status", "OpenAI Responses function_call"));
        return new CanonicalToolCall(
            ReadRequiredString(item, "call_id", "OpenAI Responses function_call"),
            ReadRequiredString(item, "name", "OpenAI Responses function_call"),
            ReadRequiredString(item, "arguments", "OpenAI Responses function_call"),
            status,
            Partial: status.Value != CanonicalItemStatus.Completed.Value,
            RawExtensions: []);
    }

    private static CanonicalToolResult MapOpenAIResponsesFunctionCallOutput(JsonElement item)
    {
        var callId = ReadRequiredString(item, "call_id", "OpenAI Responses function_call_output");
        if (!item.TryGetProperty("output", out var output))
        {
            throw new InvalidDataException(
                "OpenAI Responses function_call_output requires output.");
        }

        if (output.ValueKind == JsonValueKind.String)
        {
            var text = output.GetString() ?? string.Empty;
            return new CanonicalToolResult(
                callId,
                IsError: null,
                text.Length == 0 ? [] : [new CanonicalTextPart(text, [])],
                text,
                []);
        }

        if (output.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "OpenAI Responses function_call_output output must be a string or array.");
        }

        return new CanonicalToolResult(
            callId,
            IsError: null,
            output.EnumerateArray().Select(MapOpenAIResponsesInputContent).ToImmutableArray(),
            RawTextFallback: null,
            []);
    }

    private static CanonicalContentPart MapOpenAIResponsesInputContent(JsonElement content)
    {
        var type = ReadRequiredString(content, "type", "OpenAI Responses input content");
        return type switch
        {
            "input_text" or "output_text" => new CanonicalTextPart(
                ReadRequiredString(content, "text", $"OpenAI Responses {type} content"),
                []),
            "input_image" => MapOpenAIResponsesImage(content),
            "input_file" => new CanonicalFileReferencePart(
                ReadOptionalString(content, "file_id", "OpenAI Responses input_file content"),
                ReadOptionalString(content, "filename", "OpenAI Responses input_file content"),
                MimeType: null,
                Downloadable: null,
                RawExtensions: []),
            _ => throw new NotSupportedException(
                $"OpenAI Responses input content {type} is not mapped by this canonical slice."),
        };
    }

    private static CanonicalImagePart MapOpenAIResponsesImage(JsonElement content)
    {
        var fileId = ReadOptionalString(content, "file_id", "OpenAI Responses input_image content");
        var imageUrl = ReadOptionalString(content, "image_url", "OpenAI Responses input_image content");
        return new CanonicalImagePart(
            fileId is null ? CanonicalImageSource.Url : CanonicalImageSource.FileId,
            fileId ?? imageUrl ?? string.Empty,
            MediaType: null,
            ReadOptionalString(content, "detail", "OpenAI Responses input_image content"),
            RawExtensions: []);
    }

    private static CanonicalHostedToolEvent MapOpenAIResponsesHostedToolEvent(
        JsonElement item,
        string type) =>
        new(
            type,
            ReadRequiredString(item, "id", $"OpenAI Responses {type}"),
            MapOpenAIResponsesItemStatus(
                ReadOptionalString(item, "status", $"OpenAI Responses {type}")),
            item,
            []);

    private static CanonicalUsage? MapOpenAIResponsesUsage(OpenAIResponsesUsageWire? usage)
    {
        if (usage is null)
        {
            return null;
        }

        var cachedTokens = usage.InputTokensDetails?.CachedTokens;
        return new CanonicalUsage(
            cachedTokens is long cached
                ? Math.Max(usage.InputTokens - cached, 0)
                : usage.InputTokens,
            usage.OutputTokens,
            usage.TotalTokens,
            CacheCreationInputTokens: null,
            cachedTokens,
            ReasoningTokens: null);
    }

    private static CanonicalItemStatus MapOpenAIResponsesItemStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "completed" => CanonicalItemStatus.Completed,
            "in_progress" => CanonicalItemStatus.InProgress,
            "incomplete" => CanonicalItemStatus.Incomplete,
            "failed" => CanonicalItemStatus.Failed,
            null => CanonicalItemStatus.Unknown,
            _ => new CanonicalItemStatus(status),
        };

    private static CanonicalStopReason MapOpenAIResponsesStopReason(
        string? responseStatus,
        bool hasPendingHostedTool,
        bool hasFunctionCall)
    {
        if (responseStatus?.ToLowerInvariant() is "incomplete" or "in_progress"
            && hasPendingHostedTool)
        {
            return CanonicalStopReason.PauseTurn;
        }

        if (responseStatus == "incomplete")
        {
            return CanonicalStopReason.MaxTokens;
        }

        if (hasFunctionCall)
        {
            return CanonicalStopReason.ToolUse;
        }

        return responseStatus == "failed"
            ? CanonicalStopReason.Error
            : CanonicalStopReason.EndTurn;
    }

    private static bool HostedToolStatusRequiresPauseTurn(string? status) =>
        status?.ToLowerInvariant() is not ("completed" or "failed" or "cancelled" or "canceled");

    private static string? ReadJoinedText(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"OpenAI Responses {propertyName} must be an array.");
        }

        var text = value.EnumerateArray()
            .Select(part => ReadRequiredString(
                part,
                "text",
                $"OpenAI Responses {propertyName} item"));
        var joined = string.Join("\n", text);
        return joined.Length == 0 ? null : joined;
    }

    private static JsonElement ReadRequiredArray(
        JsonElement value,
        string propertyName,
        string context)
    {
        RequireObject(value, context);
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{context} requires array {propertyName}.");
        }

        return property;
    }

    private static string ReadRequiredString(
        JsonElement value,
        string propertyName,
        string context)
    {
        RequireObject(value, context);
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{context} requires string {propertyName}.");
        }

        return property.GetString()!;
    }

    private static string? ReadOptionalString(
        JsonElement value,
        string propertyName,
        string context)
    {
        RequireObject(value, context);
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{context} requires string {propertyName} when present.");
        }

        return property.GetString();
    }

    private static void RequireObject(JsonElement value, string context)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{context} must be an object.");
        }
    }
}
