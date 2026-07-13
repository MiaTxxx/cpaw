namespace AIUsage.Core.Proxy.Streaming.Sse;

public enum SseFramingMode
{
    Standard,
    ResponsesEventDataCompatibility,
    ChatDataLineCompatibility,
}

public sealed record SseParserOptions
{
    public const int DefaultMaxFrameBytes = 4 * 1024 * 1024;

    public SseFramingMode FramingMode { get; init; } = SseFramingMode.Standard;

    public int MaxFrameBytes { get; init; } = DefaultMaxFrameBytes;

    public bool FlushAtEndOfStream { get; init; }

    public SseTerminationPolicy TerminationPolicy { get; init; } = SseTerminationPolicy.None;
}

public sealed class SseTerminationPolicy
{
    private enum PolicyKind
    {
        None,
        DataEqualsDone,
        EventNames,
    }

    private readonly PolicyKind kind;
    private readonly IReadOnlySet<string>? terminalEvents;

    private SseTerminationPolicy(PolicyKind kind, IReadOnlySet<string>? terminalEvents = null)
    {
        this.kind = kind;
        this.terminalEvents = terminalEvents;
    }

    public static SseTerminationPolicy None { get; } = new(PolicyKind.None);

    public static SseTerminationPolicy DataEqualsDone { get; } = new(PolicyKind.DataEqualsDone);

    public static SseTerminationPolicy EventNames(params string[] eventNames)
    {
        ArgumentNullException.ThrowIfNull(eventNames);
        if (eventNames.Length == 0 || eventNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty terminal event is required.", nameof(eventNames));
        }

        return new SseTerminationPolicy(
            PolicyKind.EventNames,
            new HashSet<string>(eventNames, StringComparer.Ordinal));
    }

    internal bool RequiresTerminalFrame => kind != PolicyKind.None;

    internal bool IsTerminal(SseFrame frame) => kind switch
    {
        PolicyKind.None => false,
        PolicyKind.DataEqualsDone => frame.IsDone,
        PolicyKind.EventNames => frame.Event is not null && terminalEvents!.Contains(frame.Event),
        _ => false,
    };
}
