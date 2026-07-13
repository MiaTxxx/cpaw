using System.Text;

namespace AIUsage.Core.Proxy.Streaming.Sse;

public sealed class SseFrame
{
    private static readonly byte[] DoneBytes = "[DONE]"u8.ToArray();
    private readonly byte[] data;

    private SseFrame(string? eventName, byte[] dataUtf8)
    {
        Event = eventName;
        data = dataUtf8;
    }

    public string? Event { get; }

    public ReadOnlySpan<byte> DataUtf8 => data;

    public int DataByteCount => data.Length;

    public bool IsDone => data.AsSpan().SequenceEqual(DoneBytes);

    public static SseFrame FromText(string? eventName, string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new SseFrame(eventName, Encoding.UTF8.GetBytes(data));
    }

    internal static SseFrame FromOwnedUtf8(string? eventName, byte[] dataUtf8) =>
        new(eventName, dataUtf8);

    public override string ToString() =>
        $"SseFrame {{ HasEvent = {Event is not null}, DataByteCount = {DataByteCount}, IsDone = {IsDone} }}";
}
