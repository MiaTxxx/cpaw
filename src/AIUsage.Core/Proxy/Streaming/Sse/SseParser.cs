using System.Runtime.InteropServices;
using System.Text;

namespace AIUsage.Core.Proxy.Streaming.Sse;

public sealed class SseParser
{
    private static readonly byte[] EventFieldPrefix = "event:"u8.ToArray();
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SseParserOptions options;
    private readonly List<byte> lineBuffer = [];
    private readonly List<string> dataLines = [];
    private string? currentEvent;
    private int bufferedFrameBytes;
    private int currentLineBytes;
    private long streamOffset;
    private bool previousWasCarriageReturn;
    private bool terminalSeen;
    private bool completed;
    private bool faulted;

    public SseParser(SseParserOptions? options = null)
    {
        this.options = options ?? new SseParserOptions();
        if (this.options.MaxFrameBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxFrameBytes must be positive.");
        }
    }

    public void Append(ReadOnlySpan<byte> chunk, Action<SseFrame> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        EnsureActive();

        try
        {
            foreach (var value in chunk)
            {
                streamOffset++;
                if (terminalSeen)
                {
                    throw CreateException(SseParseErrorCode.TrailingDataAfterTerminal, additionalBytes: 1);
                }

                if (previousWasCarriageReturn)
                {
                    if (value == (byte)'\n')
                    {
                        AddCurrentLineByte();
                        EnsureAggregateWithinLimit(canDeferForResponsesEventPrefix: false);
                        ProcessLine(onFrame);
                        previousWasCarriageReturn = false;
                        continue;
                    }

                    ProcessLine(onFrame);
                    previousWasCarriageReturn = false;
                    if (terminalSeen)
                    {
                        throw CreateException(SseParseErrorCode.TrailingDataAfterTerminal, additionalBytes: 1);
                    }
                }

                AddCurrentLineByte();
                if (value == (byte)'\r')
                {
                    EnsureAggregateWithinLimit(canDeferForResponsesEventPrefix: false);
                    previousWasCarriageReturn = true;
                }
                else if (value == (byte)'\n')
                {
                    EnsureAggregateWithinLimit(canDeferForResponsesEventPrefix: false);
                    ProcessLine(onFrame);
                }
                else
                {
                    lineBuffer.Add(value);
                    FlushPreviousResponsesFrameAtEventPrefix(onFrame);
                    EnsureAggregateWithinLimit(canDeferForResponsesEventPrefix: true);
                }
            }
        }
        catch (SseParseException)
        {
            faulted = true;
            throw;
        }
    }

    public void Complete(Action<SseFrame> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        EnsureActive();

        try
        {
            if (previousWasCarriageReturn)
            {
                ProcessLine(onFrame);
                previousWasCarriageReturn = false;
            }
            else if (currentLineBytes > 0)
            {
                EnsureAggregateWithinLimit(canDeferForResponsesEventPrefix: false);
                if (!options.FlushAtEndOfStream)
                {
                    throw CreateException(SseParseErrorCode.UnexpectedEndOfStream);
                }

                ProcessLine(onFrame);
            }

            if (options.FramingMode != SseFramingMode.ChatDataLineCompatibility && dataLines.Count > 0)
            {
                if (!options.FlushAtEndOfStream)
                {
                    throw CreateException(SseParseErrorCode.UnexpectedEndOfStream);
                }

                Dispatch(onFrame);
            }
            else if (!options.FlushAtEndOfStream && bufferedFrameBytes > 0)
            {
                throw CreateException(SseParseErrorCode.UnexpectedEndOfStream);
            }

            if (options.TerminationPolicy.RequiresTerminalFrame && !terminalSeen)
            {
                throw CreateException(SseParseErrorCode.MissingTerminalFrame);
            }

            completed = true;
        }
        catch (SseParseException)
        {
            faulted = true;
            throw;
        }
    }

    private void ProcessLine(Action<SseFrame> onFrame)
    {
        var lineWireBytes = currentLineBytes;
        string line;
        try
        {
            line = StrictUtf8.GetString(lineBuffer.ToArray());
        }
        catch (DecoderFallbackException)
        {
            throw CreateException(SseParseErrorCode.InvalidUtf8);
        }
        finally
        {
            lineBuffer.Clear();
        }

        currentLineBytes = 0;
        if (options.FramingMode == SseFramingMode.ChatDataLineCompatibility)
        {
            ProcessChatLine(line, onFrame);
            bufferedFrameBytes = 0;
            return;
        }

        if (line.Length == 0)
        {
            AddCompletedLineBytes(lineWireBytes);
            Dispatch(onFrame);
            return;
        }

        if (line[0] == ':')
        {
            AddCompletedLineBytes(lineWireBytes);
            return;
        }

        var separator = line.IndexOf(':');
        var field = separator >= 0 ? line[..separator] : line;
        var fieldValue = GetFieldValue(line, separator);
        AddCompletedLineBytes(lineWireBytes);

        if (field == "event")
        {
            currentEvent = fieldValue;
        }
        else if (field == "data")
        {
            dataLines.Add(fieldValue);
        }
    }

    private void ProcessChatLine(string line, Action<SseFrame> onFrame)
    {
        if (!line.StartsWith("data:", StringComparison.Ordinal))
        {
            return;
        }

        var data = line["data:".Length..].Trim();
        Emit(onFrame, SseFrame.FromText(null, data));
    }

    private void Dispatch(Action<SseFrame> onFrame)
    {
        if (dataLines.Count == 0)
        {
            currentEvent = null;
            bufferedFrameBytes = 0;
            return;
        }

        var frame = SseFrame.FromText(currentEvent, string.Join('\n', dataLines));
        currentEvent = null;
        dataLines.Clear();
        bufferedFrameBytes = 0;
        Emit(onFrame, frame);
    }

    private void Emit(Action<SseFrame> onFrame, SseFrame frame)
    {
        terminalSeen |= options.TerminationPolicy.IsTerminal(frame);
        onFrame(frame);
    }

    private string GetFieldValue(string line, int separator)
    {
        if (separator < 0)
        {
            return string.Empty;
        }

        var value = line[(separator + 1)..];
        if (options.FramingMode != SseFramingMode.Standard)
        {
            return value.Trim();
        }

        return value.Length > 0 && value[0] == ' ' ? value[1..] : value;
    }

    private void FlushPreviousResponsesFrameAtEventPrefix(Action<SseFrame> onFrame)
    {
        if (options.FramingMode != SseFramingMode.ResponsesEventDataCompatibility ||
            dataLines.Count == 0 ||
            lineBuffer.Count != EventFieldPrefix.Length ||
            !CollectionsMarshal.AsSpan(lineBuffer).SequenceEqual(EventFieldPrefix))
        {
            return;
        }

        Dispatch(onFrame);
        if (terminalSeen)
        {
            throw CreateException(SseParseErrorCode.TrailingDataAfterTerminal);
        }
    }

    private void AddCurrentLineByte()
    {
        if (currentLineBytes >= options.MaxFrameBytes)
        {
            throw CreateException(SseParseErrorCode.FrameTooLarge, additionalBytes: 1);
        }

        currentLineBytes++;
    }

    private void EnsureAggregateWithinLimit(bool canDeferForResponsesEventPrefix)
    {
        if (bufferedFrameBytes <= options.MaxFrameBytes - currentLineBytes)
        {
            return;
        }

        if (canDeferForResponsesEventPrefix && IsPossibleResponsesEventPrefix())
        {
            return;
        }

        throw CreateException(SseParseErrorCode.FrameTooLarge);
    }

    private bool IsPossibleResponsesEventPrefix() =>
        options.FramingMode == SseFramingMode.ResponsesEventDataCompatibility &&
        dataLines.Count > 0 &&
        lineBuffer.Count < EventFieldPrefix.Length &&
        EventFieldPrefix.AsSpan(0, lineBuffer.Count).SequenceEqual(CollectionsMarshal.AsSpan(lineBuffer));

    private void AddCompletedLineBytes(int lineWireBytes)
    {
        if (bufferedFrameBytes > options.MaxFrameBytes - lineWireBytes)
        {
            throw CreateException(SseParseErrorCode.FrameTooLarge);
        }

        bufferedFrameBytes += lineWireBytes;
    }

    private void EnsureActive()
    {
        if (faulted)
        {
            throw CreateException(SseParseErrorCode.ParserFaulted);
        }

        if (completed)
        {
            throw CreateException(SseParseErrorCode.ParserCompleted);
        }
    }

    private SseParseException CreateException(SseParseErrorCode code, int additionalBytes = 0)
    {
        var totalBytes = (long)bufferedFrameBytes + currentLineBytes + additionalBytes;
        return new SseParseException(
            code,
            streamOffset,
            (int)Math.Min(totalBytes, int.MaxValue),
            options.MaxFrameBytes);
    }
}
