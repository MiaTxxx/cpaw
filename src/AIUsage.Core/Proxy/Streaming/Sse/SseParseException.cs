namespace AIUsage.Core.Proxy.Streaming.Sse;

public enum SseParseErrorCode
{
    FrameTooLarge,
    InvalidUtf8,
    UnexpectedEndOfStream,
    MissingTerminalFrame,
    TrailingDataAfterTerminal,
    ParserFaulted,
    ParserCompleted,
}

public sealed class SseParseException : Exception
{
    internal SseParseException(
        SseParseErrorCode code,
        long streamOffset,
        int bufferedFrameBytes,
        int limitBytes = 0)
        : base(CreateMessage(code, streamOffset, bufferedFrameBytes, limitBytes))
    {
        Code = code;
        StreamOffset = streamOffset;
        BufferedFrameBytes = bufferedFrameBytes;
        LimitBytes = limitBytes;
    }

    public SseParseErrorCode Code { get; }

    public long StreamOffset { get; }

    public int BufferedFrameBytes { get; }

    public int LimitBytes { get; }

    private static string CreateMessage(
        SseParseErrorCode code,
        long streamOffset,
        int bufferedFrameBytes,
        int limitBytes) =>
        $"SSE parse failed: code={code}, offset={streamOffset}, bufferedBytes={bufferedFrameBytes}, limitBytes={limitBytes}.";
}
