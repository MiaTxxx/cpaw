using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols;

public sealed record ProxyHttpError<TBody>(
    int StatusCode,
    TBody Body,
    [property: JsonIgnore] string? RequestId)
    where TBody : class;
