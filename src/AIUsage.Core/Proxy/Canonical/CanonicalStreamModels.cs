using System.Collections.Immutable;

namespace AIUsage.Core.Proxy.Canonical;

public abstract record CanonicalStreamEvent;

public readonly record struct CanonicalStreamPartKind
{
    private readonly string? _value;

    public CanonicalStreamPartKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public string Value => _value ?? "unknown";

    public static CanonicalStreamPartKind Text { get; } = new("text");

    public static CanonicalStreamPartKind Reasoning { get; } = new("reasoning");

    public static CanonicalStreamPartKind ToolCall { get; } = new("tool_call");
}

public sealed record CanonicalStreamMessageStarted(
    CanonicalRole Role,
    string? MessageId,
    string? Model,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalStreamEvent;

public sealed record CanonicalStreamContentPartStarted(
    long Index,
    CanonicalStreamPartKind Kind,
    string? ToolCallId,
    string? ToolName,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalStreamEvent;

public sealed record CanonicalStreamContentPartDelta(
    long Index,
    CanonicalStreamPartKind Kind,
    string? TextDelta,
    string? JsonDelta,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalStreamEvent;

public sealed record CanonicalStreamContentPartStopped(long Index) : CanonicalStreamEvent;

public sealed record CanonicalStreamMessageDelta(
    CanonicalStop? Stop,
    CanonicalUsage? Usage,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalStreamEvent;

public sealed record CanonicalStreamMessageStopped : CanonicalStreamEvent;

public sealed record CanonicalStreamError(
    string Message,
    ImmutableArray<CanonicalVendorExtension> RawExtensions) : CanonicalStreamEvent;
