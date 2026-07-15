using System.Text.Json;
using AIUsage.Core.Proxy.Canonical;
using AIUsage.Core.Proxy.Protocols.OpenAIChat;
using Xunit;

namespace AIUsage.Core.Tests;

internal static class CanonicalGoldenTestSupport
{
    internal static JsonDocument ReadFixture(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AIUsage.Windows.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new Xunit.Sdk.XunitException("Could not locate the AIUsage solution root.");
        }

        var path = new[]
        {
            current.FullName,
            "QuotaBackend",
            "Tests",
            "QuotaBackendTests",
            "Fixtures",
            "v1",
        }.Concat(relativePath).ToArray();
        return JsonDocument.Parse(File.ReadAllBytes(Path.Combine(path)));
    }

    internal static JsonElement Project(
        CanonicalBuildResult<OpenAIChatCompletionRequestWire> result) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["payload"] = JsonSerializer.SerializeToElement(result.Payload),
            ["lossyNotes"] = result.LossyNotes.Select(Project).ToArray(),
        });

    internal static JsonElement Project(CanonicalRequest request)
    {
        var value = new Dictionary<string, object?>
        {
            ["modelHint"] = request.ModelHint,
            ["system"] = request.System.Select(Project).ToArray(),
            ["items"] = request.Items.Select(Project).ToArray(),
            ["tools"] = request.Tools.Select(Project).ToArray(),
            ["generationConfig"] = Project(request.GenerationConfig),
            ["metadata"] = Project(request.Metadata),
            ["rawExtensions"] = request.RawExtensions.Select(Project).ToArray(),
        };
        if (request.ToolConfig is not null)
        {
            value["toolConfig"] = Project(request.ToolConfig);
        }

        return JsonSerializer.SerializeToElement(value);
    }

    internal static JsonElement Project(CanonicalResponse response)
    {
        var value = new Dictionary<string, object?>
        {
            ["items"] = response.Items.Select(Project).ToArray(),
            ["stop"] = Project(response.Stop),
            ["rawExtensions"] = response.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "id", response.Id);
        Add(value, "model", response.Model);
        if (response.Usage is not null)
        {
            value["usage"] = Project(response.Usage);
        }

        return JsonSerializer.SerializeToElement(value);
    }

    internal static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(
            expected.ValueKind == actual.ValueKind,
            $"JSON kind differs at {path}: expected {expected.ValueKind}, actual {actual.ValueKind}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject()
                    .ToDictionary(property => property.Name, StringComparer.Ordinal);
                var actualProperties = actual.EnumerateObject()
                    .ToDictionary(property => property.Name, StringComparer.Ordinal);
                Assert.Equal(
                    expectedProperties.Keys.Order(StringComparer.Ordinal),
                    actualProperties.Keys.Order(StringComparer.Ordinal));
                foreach (var property in expectedProperties)
                {
                    AssertJsonEquivalent(
                        property.Value.Value,
                        actualProperties[property.Key].Value,
                        $"{path}.{property.Key}");
                }

                break;
            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (var index = 0; index < expectedItems.Length; index++)
                {
                    AssertJsonEquivalent(expectedItems[index], actualItems[index], $"{path}[{index}]");
                }

                break;
            case JsonValueKind.Number:
                if (expected.TryGetInt64(out var expectedInteger)
                    && actual.TryGetInt64(out var actualInteger))
                {
                    Assert.Equal(expectedInteger, actualInteger);
                }
                else
                {
                    Assert.Equal(expected.GetDouble(), actual.GetDouble());
                }

                break;
            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), actual.GetString());
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                Assert.Equal(expected.GetBoolean(), actual.GetBoolean());
                break;
            case JsonValueKind.Null:
                break;
            default:
                throw new Xunit.Sdk.XunitException($"Unsupported JSON kind at {path}: {expected.ValueKind}");
        }
    }

    private static Dictionary<string, object?> Project(CanonicalGenerationConfig config)
    {
        var value = new Dictionary<string, object?>
        {
            ["stopSequences"] = config.StopSequences,
        };
        Add(value, "maxOutputTokens", config.MaxOutputTokens);
        Add(value, "temperature", config.Temperature);
        Add(value, "topP", config.TopP);
        Add(value, "topK", config.TopK);
        Add(value, "stream", config.Stream);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalToolConfig config)
    {
        var value = new Dictionary<string, object?>();
        if (config.Choice is not null)
        {
            value["choice"] = Project(config.Choice);
        }

        Add(value, "parallelCallsAllowed", config.ParallelCallsAllowed);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalToolChoice choice) =>
        choice switch
        {
            CanonicalNoneToolChoice => new() { ["type"] = "none" },
            CanonicalAutoToolChoice => new() { ["type"] = "auto" },
            CanonicalRequiredToolChoice => new() { ["type"] = "required" },
            CanonicalSpecificToolChoice specific => new()
            {
                ["type"] = "specific",
                ["name"] = specific.Name,
            },
            CanonicalUnknownToolChoice unknown => new()
            {
                ["type"] = "unknown",
                ["value"] = unknown.Value,
            },
            _ => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical tool choice {choice.GetType().Name}."),
        };

    private static Dictionary<string, object?> Project(CanonicalToolDefinition tool)
    {
        var flags = new Dictionary<string, object?>();
        Add(flags, "eagerInputStreaming", tool.Flags.EagerInputStreaming);
        Add(flags, "strict", tool.Flags.Strict);

        var value = new Dictionary<string, object?>
        {
            ["kind"] = tool.Kind.Value,
            ["execution"] = tool.Execution.Value,
            ["flags"] = flags,
            ["rawExtensions"] = tool.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "name", tool.Name);
        Add(value, "description", tool.Description);
        if (tool.InputSchema is not null)
        {
            value["inputSchema"] = Project(tool.InputSchema);
        }

        Add(value, "vendorType", tool.VendorType);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalConversationItem item) =>
        item switch
        {
            CanonicalMessage message => Project(message),
            CanonicalReasoningItem reasoning => Project(reasoning),
            CanonicalToolCall toolCall => Project(toolCall),
            CanonicalToolResult toolResult => Project(toolResult),
            _ => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical conversation item {item.GetType().Name}."),
        };

    private static Dictionary<string, object?> Project(CanonicalMessage message)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "message",
            ["role"] = message.Role.Value,
            ["parts"] = message.Parts.Select(Project).ToArray(),
            ["metadata"] = Project(message.Metadata),
            ["rawExtensions"] = message.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "phase", message.Phase);
        Add(value, "name", message.Name);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalReasoningItem reasoning)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "reasoning",
            ["rawExtensions"] = reasoning.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "summaryText", reasoning.SummaryText);
        Add(value, "fullText", reasoning.FullText);
        Add(value, "encryptedContent", reasoning.EncryptedContent);
        Add(value, "signature", reasoning.Signature);
        Add(value, "redacted", reasoning.Redacted);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalToolCall toolCall) =>
        new()
        {
            ["type"] = "tool_call",
            ["id"] = toolCall.Id,
            ["name"] = toolCall.Name,
            ["inputJSON"] = toolCall.InputJson,
            ["status"] = toolCall.Status.Value,
            ["partial"] = toolCall.Partial,
            ["rawExtensions"] = toolCall.RawExtensions.Select(Project).ToArray(),
        };

    private static Dictionary<string, object?> Project(CanonicalToolResult toolResult)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "tool_result",
            ["toolCallID"] = toolResult.ToolCallId,
            ["parts"] = toolResult.Parts.Select(Project).ToArray(),
            ["rawExtensions"] = toolResult.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "isError", toolResult.IsError);
        Add(value, "rawTextFallback", toolResult.RawTextFallback);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalContentPart part) =>
        part switch
        {
            CanonicalTextPart text => new()
            {
                ["type"] = "text",
                ["text"] = text.Text,
                ["rawExtensions"] = text.RawExtensions.Select(Project).ToArray(),
            },
            CanonicalImagePart image => Project(image),
            CanonicalDocumentPart document => Project(document),
            CanonicalFileReferencePart file => Project(file),
            CanonicalReasoningTextPart reasoning => new()
            {
                ["type"] = "reasoning_text",
                ["text"] = reasoning.Text,
                ["rawExtensions"] = reasoning.RawExtensions.Select(Project).ToArray(),
            },
            CanonicalUnknownPart unknown => Project(unknown),
            _ => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical content part {part.GetType().Name}."),
        };

    private static Dictionary<string, object?> Project(CanonicalFileReferencePart file)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "file_ref",
            ["rawExtensions"] = file.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "fileID", file.FileId);
        Add(value, "filename", file.Filename);
        Add(value, "mimeType", file.MimeType);
        Add(value, "downloadable", file.Downloadable);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalStop stop)
    {
        var value = new Dictionary<string, object?>
        {
            ["reason"] = stop.Reason.Value,
        };
        Add(value, "sequence", stop.Sequence);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalUsage usage)
    {
        var value = new Dictionary<string, object?>();
        Add(value, "inputTokens", usage.InputTokens);
        Add(value, "outputTokens", usage.OutputTokens);
        Add(value, "totalTokens", usage.TotalTokens);
        Add(value, "cacheCreationInputTokens", usage.CacheCreationInputTokens);
        Add(value, "cacheReadInputTokens", usage.CacheReadInputTokens);
        Add(value, "reasoningTokens", usage.ReasoningTokens);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalImagePart image)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "image",
            ["source"] = image.Source.Value,
            ["data"] = image.Data,
            ["rawExtensions"] = image.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "mediaType", image.MediaType);
        Add(value, "detail", image.Detail);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalUnknownPart unknown)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "unknown",
            ["vendorType"] = unknown.Type,
            ["rawExtensions"] = unknown.RawExtensions.Select(Project).ToArray(),
        };
        if (unknown.Payload is { } payload)
        {
            value["payload"] = payload;
        }

        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalDocumentPart document)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "document",
            ["source"] = Project(document.Source),
            ["rawExtensions"] = document.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "title", document.Title);
        Add(value, "context", document.Context);
        if (document.Citations is { } citations)
        {
            value["citations"] = citations;
        }

        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalDocumentSource source) =>
        source switch
        {
            CanonicalInlineTextDocumentSource inline => new()
            {
                ["type"] = "inline_text",
                ["text"] = inline.Text,
            },
            CanonicalUrlDocumentSource url => new()
            {
                ["type"] = "url",
                ["url"] = url.Url,
            },
            CanonicalBase64DocumentSource base64 => Project(base64),
            CanonicalFileIdDocumentSource file => new()
            {
                ["type"] = "file_id",
                ["fileID"] = file.FileId,
            },
            CanonicalUnknownDocumentSource unknown => new()
            {
                ["type"] = "unknown",
                ["value"] = unknown.Value,
            },
            _ => throw new Xunit.Sdk.XunitException(
                $"Unsupported canonical document source {source.GetType().Name}."),
        };

    private static Dictionary<string, object?> Project(CanonicalBase64DocumentSource source)
    {
        var value = new Dictionary<string, object?>
        {
            ["type"] = "base64",
            ["data"] = source.Data,
        };
        Add(value, "mediaType", source.MediaType);
        return value;
    }

    private static Dictionary<string, object?> Project(CanonicalVendorExtension extensionValue) =>
        new()
        {
            ["vendor"] = extensionValue.Vendor,
            ["key"] = extensionValue.Key,
            ["value"] = extensionValue.Value,
        };

    private static Dictionary<string, object?> Project(CanonicalLossyNote note)
    {
        var value = new Dictionary<string, object?>
        {
            ["code"] = note.Code,
            ["message"] = note.Message,
            ["severity"] = note.Severity.Value,
            ["rawExtensions"] = note.RawExtensions.Select(Project).ToArray(),
        };
        Add(value, "itemIndex", note.ItemIndex);
        Add(value, "path", note.Path);
        return value;
    }

    private static Dictionary<string, object?> Project(
        IReadOnlyDictionary<string, JsonElement> values) =>
        values.ToDictionary(
            property => property.Key,
            property => (object?)property.Value,
            StringComparer.Ordinal);

    private static void Add<T>(Dictionary<string, object?> value, string key, T? item)
    {
        if (item is not null)
        {
            value[key] = item;
        }
    }
}
