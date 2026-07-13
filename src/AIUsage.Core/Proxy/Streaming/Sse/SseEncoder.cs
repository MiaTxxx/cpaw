using System.Text;

namespace AIUsage.Core.Proxy.Streaming.Sse;

public static class SseEncoder
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[] Encode(SseFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateEvent(frame.Event);

        var data = StrictUtf8.GetString(frame.DataUtf8);
        var normalizedData = data.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var builder = new StringBuilder();
        if (frame.Event is not null)
        {
            builder.Append("event: ").Append(frame.Event).Append('\n');
        }

        foreach (var line in normalizedData.Split('\n'))
        {
            builder.Append("data: ").Append(line).Append('\n');
        }

        builder.Append('\n');
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void ValidateEvent(string? eventName)
    {
        if (eventName?.IndexOfAny(['\r', '\n', '\0']) >= 0)
        {
            throw new ArgumentException("SSE event names cannot contain CR, LF, or NUL.", nameof(eventName));
        }
    }
}
