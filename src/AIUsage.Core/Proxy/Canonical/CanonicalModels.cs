using System.Collections.Immutable;
using System.Text.Json;

namespace AIUsage.Core.Proxy.Canonical;

public sealed record CanonicalVendorExtension
{
    public CanonicalVendorExtension(string vendor, string key, JsonElement value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendor);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Vendor = vendor;
        Key = key;
        Value = value.Clone();
    }

    public string Vendor { get; }

    public string Key { get; }

    public JsonElement Value { get; }
}

public readonly record struct CanonicalRole
{
    private readonly string? _value;

    public CanonicalRole(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalRole System { get; } = new("system");

    public static CanonicalRole User { get; } = new("user");

    public static CanonicalRole Assistant { get; } = new("assistant");

    public static CanonicalRole Tool { get; } = new("tool");
}

public readonly record struct CanonicalToolDefinitionKind
{
    private readonly string? _value;

    public CanonicalToolDefinitionKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalToolDefinitionKind Function { get; } = new("function");
}

public readonly record struct CanonicalToolExecution
{
    private readonly string? _value;

    public CanonicalToolExecution(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalToolExecution Client { get; } = new("client");
}

public readonly record struct CanonicalItemStatus
{
    private readonly string? _value;

    public CanonicalItemStatus(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalItemStatus Completed { get; } = new("completed");
}

public sealed record CanonicalGenerationConfig(
    long? MaxOutputTokens,
    double? Temperature,
    double? TopP,
    long? TopK,
    ImmutableArray<string> StopSequences,
    bool? Stream);

public sealed record CanonicalToolDefinitionFlags(
    bool? EagerInputStreaming,
    bool? Strict);

public sealed record CanonicalToolDefinition(
    CanonicalToolDefinitionKind Kind,
    string? Name,
    string? Description,
    ImmutableDictionary<string, JsonElement>? InputSchema,
    CanonicalToolExecution Execution,
    string? VendorType,
    CanonicalToolDefinitionFlags Flags,
    ImmutableArray<CanonicalVendorExtension> RawExtensions);

public abstract record CanonicalToolChoice;

public sealed record CanonicalNoneToolChoice : CanonicalToolChoice;

public sealed record CanonicalAutoToolChoice : CanonicalToolChoice;

public sealed record CanonicalRequiredToolChoice : CanonicalToolChoice;

public sealed record CanonicalSpecificToolChoice(string Name) : CanonicalToolChoice;

public sealed record CanonicalUnknownToolChoice(string Value) : CanonicalToolChoice;

public sealed record CanonicalToolConfig(
    CanonicalToolChoice? Choice,
    bool? ParallelCallsAllowed);

public sealed record CanonicalRequest(
    string ModelHint,
    ImmutableArray<CanonicalContentPart> System,
    ImmutableArray<CanonicalConversationItem> Items,
    ImmutableArray<CanonicalToolDefinition> Tools,
    CanonicalToolConfig? ToolConfig,
    CanonicalGenerationConfig GenerationConfig,
    ImmutableDictionary<string, JsonElement> Metadata,
    ImmutableArray<CanonicalVendorExtension> RawExtensions);

public readonly record struct CanonicalStopReason
{
    private readonly string? _value;

    public CanonicalStopReason(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalStopReason EndTurn { get; } = new("end_turn");

    public static CanonicalStopReason ToolUse { get; } = new("tool_use");

    public static CanonicalStopReason MaxTokens { get; } = new("max_tokens");

    public static CanonicalStopReason PauseTurn { get; } = new("pause_turn");

    public static CanonicalStopReason Refusal { get; } = new("refusal");

    public static CanonicalStopReason ModelContextWindowExceeded { get; } =
        new("model_context_window_exceeded");

    public static CanonicalStopReason Error { get; } = new("error");
}

public sealed record CanonicalStop(
    CanonicalStopReason Reason,
    string? Sequence);

public sealed record CanonicalUsage(
    long? InputTokens,
    long? OutputTokens,
    long? TotalTokens,
    long? CacheCreationInputTokens,
    long? CacheReadInputTokens,
    long? ReasoningTokens);

public sealed record CanonicalResponse(
    string? Id,
    string? Model,
    ImmutableArray<CanonicalConversationItem> Items,
    CanonicalStop Stop,
    CanonicalUsage? Usage,
    ImmutableArray<CanonicalVendorExtension> RawExtensions);

public abstract record CanonicalConversationItem;

public sealed record CanonicalMessage(
    CanonicalRole Role,
    string? Phase,
    ImmutableArray<CanonicalContentPart> Parts,
    string? Name,
    ImmutableDictionary<string, JsonElement> Metadata,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalConversationItem;

public abstract record CanonicalContentPart(
    ImmutableArray<CanonicalVendorExtension> RawExtensions);

public sealed record CanonicalTextPart(
    string Text,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalContentPart(RawExtensions);

public readonly record struct CanonicalImageSource
{
    private readonly string? _value;

    public CanonicalImageSource(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalImageSource Base64 { get; } = new("base64");

    public static CanonicalImageSource Url { get; } = new("url");

    public static CanonicalImageSource FileId { get; } = new("file_id");
}

public sealed record CanonicalImagePart(
    CanonicalImageSource Source,
    string Data,
    string? MediaType,
    string? Detail,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalContentPart(RawExtensions);

public abstract record CanonicalDocumentSource;

public sealed record CanonicalInlineTextDocumentSource(string Text) : CanonicalDocumentSource;

public sealed record CanonicalUrlDocumentSource(string Url) : CanonicalDocumentSource;

public sealed record CanonicalBase64DocumentSource(
    string Data,
    string? MediaType) : CanonicalDocumentSource;

public sealed record CanonicalFileIdDocumentSource(string FileId) : CanonicalDocumentSource;

public sealed record CanonicalUnknownDocumentSource : CanonicalDocumentSource
{
    public CanonicalUnknownDocumentSource(JsonElement value)
    {
        Value = value.Clone();
    }

    public JsonElement Value { get; }
}

public sealed record CanonicalDocumentPart(
    CanonicalDocumentSource Source,
    string? Title,
    string? Context,
    JsonElement? Citations,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalContentPart(RawExtensions);

public sealed record CanonicalFileReferencePart(
    string? FileId,
    string? Filename,
    string? MimeType,
    bool? Downloadable,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalContentPart(RawExtensions);

public sealed record CanonicalReasoningTextPart(
    string Text,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalContentPart(RawExtensions);

public sealed record CanonicalUnknownPart : CanonicalContentPart
{
    public CanonicalUnknownPart(
        string type,
        JsonElement? payload,
        ImmutableArray<CanonicalVendorExtension> rawExtensions)
        : base(rawExtensions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        Type = type;
        Payload = payload?.Clone();
    }

    public string Type { get; }

    public JsonElement? Payload { get; }
}

public sealed record CanonicalToolCall(
    string Id,
    string Name,
    string InputJson,
    CanonicalItemStatus Status,
    bool Partial,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalConversationItem;

public sealed record CanonicalToolResult(
    string ToolCallId,
    bool? IsError,
    ImmutableArray<CanonicalContentPart> Parts,
    string? RawTextFallback,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalConversationItem;

public sealed record CanonicalReasoningItem(
    string? SummaryText,
    string? FullText,
    string? EncryptedContent,
    string? Signature,
    bool? Redacted,
    ImmutableArray<CanonicalVendorExtension> RawExtensions)
    : CanonicalConversationItem;

public readonly record struct CanonicalLossySeverity
{
    private readonly string? _value;

    public CanonicalLossySeverity(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "warning";

    public static CanonicalLossySeverity Warning { get; } = new("warning");
}

public sealed record CanonicalLossyNote(
    string Code,
    string Message,
    CanonicalLossySeverity Severity,
    long? ItemIndex,
    string? Path,
    ImmutableArray<CanonicalVendorExtension> RawExtensions);

public sealed record CanonicalBuildResult<TPayload>(
    TPayload Payload,
    ImmutableArray<CanonicalLossyNote> LossyNotes);
