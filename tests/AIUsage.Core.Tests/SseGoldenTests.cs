using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsage.Core.Proxy.Streaming.Sse;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class SseGoldenTests
{
    [Theory]
    [InlineData("claude/sse-lifecycle.json")]
    [InlineData("codex/sse-lifecycle.json")]
    [InlineData("opencode/sse-lifecycle.json")]
    public void Sse_lifecycle_matches_Swift_golden(string relativePath)
    {
        var fixture = ReadFixture(relativePath);
        var frames = fixture.Input.Framing switch
        {
            "encoded-frames" => fixture.Input.Frames!.Select(ToFrame).ToArray(),
            "responses-event-data" => ParseLines(
                fixture.Input.Lines!,
                SseFramingMode.ResponsesEventDataCompatibility,
                SseTerminationPolicy.EventNames("response.completed", "response.failed", "response.incomplete")),
            "chat-data-lines" => ParseLines(
                fixture.Input.Lines!,
                SseFramingMode.ChatDataLineCompatibility,
                SseTerminationPolicy.DataEqualsDone),
            _ => throw new Xunit.Sdk.XunitException($"Unknown fixture framing: {fixture.Input.Framing}"),
        };

        AssertFrames(fixture.Expected.Frames, frames);
        var wire = Encoding.UTF8.GetString(frames.SelectMany(SseEncoder.Encode).ToArray());
        Assert.Equal(fixture.Expected.Wire, wire);

        if (fixture.Input.Framing == "encoded-frames")
        {
            var reparsed = ParseBytes(
                Encoding.UTF8.GetBytes(wire),
                SseFramingMode.Standard,
                SseTerminationPolicy.EventNames("message_stop"),
                flushAtEndOfStream: false);
            AssertFrames(fixture.Expected.Frames, reparsed);
        }
    }

    [Fact]
    public void Parser_handles_every_byte_split_and_CRLF()
    {
        var sourceFrames = new[]
        {
            SseFrame.FromText("message_start", "{\"text\":\"你好\"}"),
            SseFrame.FromText("message_stop", "{\"type\":\"message_stop\"}"),
        };
        var lfWire = sourceFrames.SelectMany(SseEncoder.Encode).ToArray();
        var crlfWire = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(lfWire).Replace("\n", "\r\n", StringComparison.Ordinal));

        foreach (var wire in new[] { lfWire, crlfWire })
        {
            var parser = new SseParser(new SseParserOptions
            {
                FramingMode = SseFramingMode.Standard,
                TerminationPolicy = SseTerminationPolicy.EventNames("message_stop"),
            });
            var actual = new List<SseFrame>();
            foreach (var value in wire)
            {
                parser.Append([value], actual.Add);
            }

            parser.Complete(actual.Add);
            Assert.Equal(2, actual.Count);
            Assert.Equal("{\"text\":\"你好\"}", GetData(actual[0]));
            Assert.Equal("message_stop", actual[1].Event);
        }
    }

    [Fact]
    public void Parser_joins_multiple_data_lines()
    {
        var frames = ParseBytes(
            "event: fixture\ndata: one\ndata: two\n\n"u8.ToArray(),
            SseFramingMode.Standard,
            SseTerminationPolicy.None,
            flushAtEndOfStream: false);

        var frame = Assert.Single(frames);
        Assert.Equal("fixture", frame.Event);
        Assert.Equal("one\ntwo", GetData(frame));
    }

    [Fact]
    public void Parser_requires_configured_terminal_frame()
    {
        var parser = new SseParser(new SseParserOptions
        {
            FramingMode = SseFramingMode.ChatDataLineCompatibility,
            TerminationPolicy = SseTerminationPolicy.DataEqualsDone,
            FlushAtEndOfStream = true,
        });
        parser.Append("data: fixture\n"u8, _ => { });

        var exception = Assert.Throws<SseParseException>(() => parser.Complete(_ => { }));
        Assert.Equal(SseParseErrorCode.MissingTerminalFrame, exception.Code);
    }

    [Fact]
    public void Parser_rejects_unterminated_frame_when_EOF_flush_is_disabled()
    {
        var parser = new SseParser(new SseParserOptions
        {
            FramingMode = SseFramingMode.Standard,
            FlushAtEndOfStream = false,
        });
        parser.Append("data: fixture\n"u8, _ => { });

        var exception = Assert.Throws<SseParseException>(() => parser.Complete(_ => { }));
        Assert.Equal(SseParseErrorCode.UnexpectedEndOfStream, exception.Code);
    }

    [Fact]
    public void Frame_limit_and_diagnostics_do_not_expose_payload()
    {
        const string fixtureSecret = "fixture-secret-must-not-appear";
        var parser = new SseParser(new SseParserOptions { MaxFrameBytes = 12 });

        var exception = Assert.Throws<SseParseException>(() => parser.Append(Encoding.UTF8.GetBytes($": {fixtureSecret}"), _ => { }));

        Assert.Equal(SseParseErrorCode.FrameTooLarge, exception.Code);
        Assert.DoesNotContain(fixtureSecret, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(fixtureSecret, SseFrame.FromText("fixture", fixtureSecret).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Frame_limit_accepts_exact_boundary_and_rejects_one_extra_byte()
    {
        var wire = "data: x\n\n"u8.ToArray();
        var exactParser = new SseParser(new SseParserOptions { MaxFrameBytes = wire.Length });
        var frames = new List<SseFrame>();
        exactParser.Append(wire, frames.Add);
        exactParser.Complete(frames.Add);
        Assert.Single(frames);

        var shortParser = new SseParser(new SseParserOptions { MaxFrameBytes = wire.Length - 1 });
        var exception = Assert.Throws<SseParseException>(() => shortParser.Append(wire, _ => { }));
        Assert.Equal(SseParseErrorCode.FrameTooLarge, exception.Code);
    }

    [Fact]
    public void Parser_emits_valid_prefix_before_invalid_UTF8_and_then_becomes_faulted()
    {
        var parser = new SseParser();
        var frames = new List<SseFrame>();
        var validPrefix = "data: visible\n\n"u8.ToArray();
        var chunk = validPrefix.Concat(new byte[] { 0xff, (byte)'\n' }).ToArray();

        var exception = Assert.Throws<SseParseException>(() => parser.Append(chunk, frames.Add));

        Assert.Equal(SseParseErrorCode.InvalidUtf8, exception.Code);
        Assert.Equal("visible", GetData(Assert.Single(frames)));
        Assert.DoesNotContain("[FF]", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("visible", exception.ToString(), StringComparison.Ordinal);

        var faulted = Assert.Throws<SseParseException>(() => parser.Append("data: ignored\n\n"u8, frames.Add));
        Assert.Equal(SseParseErrorCode.ParserFaulted, faulted.Code);
        var completeFaulted = Assert.Throws<SseParseException>(() => parser.Complete(frames.Add));
        Assert.Equal(SseParseErrorCode.ParserFaulted, completeFaulted.Code);
        Assert.Single(frames);
    }

    [Theory]
    [InlineData("data: [DONE]\n", SseFramingMode.ChatDataLineCompatibility)]
    [InlineData("event: response.completed\ndata: {}\n\n", SseFramingMode.Standard)]
    public void Parser_rejects_wire_data_after_terminal_frame(string terminalWire, SseFramingMode mode)
    {
        var policy = mode == SseFramingMode.ChatDataLineCompatibility
            ? SseTerminationPolicy.DataEqualsDone
            : SseTerminationPolicy.EventNames("response.completed");
        var parser = new SseParser(new SseParserOptions { FramingMode = mode, TerminationPolicy = policy });
        var frames = new List<SseFrame>();
        var wire = Encoding.UTF8.GetBytes(terminalWire + "data: trailing\n\n");

        var exception = Assert.Throws<SseParseException>(() => parser.Append(wire, frames.Add));

        Assert.Equal(SseParseErrorCode.TrailingDataAfterTerminal, exception.Code);
        Assert.Single(frames);
    }

    [Fact]
    public void Aggregate_frame_limit_counts_all_lines_and_CRLF_bytes()
    {
        var lfWire = "event: x\ndata: y\n\n"u8.ToArray();
        AssertParsesAtExactLimit(lfWire);
        AssertRejectsAtLimit(lfWire, lfWire.Length - 1);

        var crlfWire = Encoding.UTF8.GetBytes("event: x\r\ndata: y\r\n\r\n");
        AssertParsesAtExactLimit(crlfWire);
        AssertRejectsAtLimit(crlfWire, crlfWire.Length - 1);
    }

    [Fact]
    public void Chat_mode_rejects_unterminated_last_line_when_EOF_flush_is_disabled()
    {
        var parser = new SseParser(new SseParserOptions
        {
            FramingMode = SseFramingMode.ChatDataLineCompatibility,
            FlushAtEndOfStream = false,
        });
        var frames = new List<SseFrame>();
        parser.Append("data: fixture"u8, frames.Add);

        var exception = Assert.Throws<SseParseException>(() => parser.Complete(frames.Add));

        Assert.Equal(SseParseErrorCode.UnexpectedEndOfStream, exception.Code);
        Assert.Empty(frames);
    }

    [Fact]
    public void Standard_mode_preserves_spaces_except_the_single_optional_space_after_colon()
    {
        var source = SseFrame.FromText(" fixture ", " leading and trailing ");
        var wire = SseEncoder.Encode(source);
        var frames = ParseBytes(wire, SseFramingMode.Standard, SseTerminationPolicy.None, flushAtEndOfStream: false);

        var frame = Assert.Single(frames);
        Assert.Equal(source.Event, frame.Event);
        Assert.Equal(GetData(source), GetData(frame));
    }

    [Fact]
    public void Encoder_splits_multiline_data_and_rejects_injected_event_names()
    {
        var frame = SseFrame.FromText("fixture", "line one\nline two");
        var wire = Encoding.UTF8.GetString(SseEncoder.Encode(frame));

        Assert.Equal("event: fixture\ndata: line one\ndata: line two\n\n", wire);
        Assert.Throws<ArgumentException>(() => SseEncoder.Encode(SseFrame.FromText("bad\nevent", "fixture")));
    }

    private static IReadOnlyList<SseFrame> ParseLines(
        IReadOnlyList<string> lines,
        SseFramingMode mode,
        SseTerminationPolicy terminationPolicy)
    {
        var parser = new SseParser(new SseParserOptions
        {
            FramingMode = mode,
            FlushAtEndOfStream = true,
            TerminationPolicy = terminationPolicy,
        });
        var frames = new List<SseFrame>();
        foreach (var line in lines)
        {
            parser.Append(Encoding.UTF8.GetBytes(line + "\n"), frames.Add);
        }

        parser.Complete(frames.Add);
        return frames;
    }

    private static IReadOnlyList<SseFrame> ParseBytes(
        byte[] bytes,
        SseFramingMode mode,
        SseTerminationPolicy terminationPolicy,
        bool flushAtEndOfStream)
    {
        var parser = new SseParser(new SseParserOptions
        {
            FramingMode = mode,
            FlushAtEndOfStream = flushAtEndOfStream,
            TerminationPolicy = terminationPolicy,
        });
        var frames = new List<SseFrame>();
        parser.Append(bytes, frames.Add);
        parser.Complete(frames.Add);
        return frames;
    }

    private static void AssertParsesAtExactLimit(byte[] wire)
    {
        var parser = new SseParser(new SseParserOptions { MaxFrameBytes = wire.Length });
        var frames = new List<SseFrame>();
        parser.Append(wire, frames.Add);
        parser.Complete(frames.Add);
        Assert.Single(frames);
    }

    private static void AssertRejectsAtLimit(byte[] wire, int limit)
    {
        var parser = new SseParser(new SseParserOptions { MaxFrameBytes = limit });
        var exception = Assert.Throws<SseParseException>(() => parser.Append(wire, _ => { }));
        Assert.Equal(SseParseErrorCode.FrameTooLarge, exception.Code);
    }

    private static SseFrame ToFrame(SseFixtureFrame frame) => SseFrame.FromText(frame.Event, frame.Data);

    private static string GetData(SseFrame frame) => Encoding.UTF8.GetString(frame.DataUtf8);

    private static void AssertFrames(IReadOnlyList<SseFixtureFrame> expected, IReadOnlyList<SseFrame> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Event, actual[index].Event);
            Assert.Equal(expected[index].Data, GetData(actual[index]));
        }
    }

    private static SseFixtureEnvelope ReadFixture(string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), "QuotaBackend", "Tests", "QuotaBackendTests", "Fixtures", "v1", "contracts", "proxy", relativePath.Replace('/', Path.DirectorySeparatorChar));
        return JsonSerializer.Deserialize<SseFixtureEnvelope>(File.ReadAllBytes(path))
            ?? throw new Xunit.Sdk.XunitException("SSE fixture decoded to null.");
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AIUsage.Windows.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return current.FullName;
    }

    private sealed record SseFixtureEnvelope
    {
        [JsonPropertyName("input")]
        public required SseFixtureInput Input { get; init; }

        [JsonPropertyName("expected")]
        public required SseFixtureExpected Expected { get; init; }
    }

    private sealed record SseFixtureInput
    {
        [JsonPropertyName("framing")]
        public required string Framing { get; init; }

        [JsonPropertyName("lines")]
        public IReadOnlyList<string>? Lines { get; init; }

        [JsonPropertyName("frames")]
        public IReadOnlyList<SseFixtureFrame>? Frames { get; init; }
    }

    private sealed record SseFixtureExpected
    {
        [JsonPropertyName("frames")]
        public required IReadOnlyList<SseFixtureFrame> Frames { get; init; }

        [JsonPropertyName("wire")]
        public required string Wire { get; init; }
    }

    private sealed record SseFixtureFrame
    {
        [JsonPropertyName("event")]
        public string? Event { get; init; }

        [JsonPropertyName("data")]
        public required string Data { get; init; }
    }
}
