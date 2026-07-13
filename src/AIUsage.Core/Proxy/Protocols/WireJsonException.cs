using System.Text.Json;

namespace AIUsage.Core.Proxy.Protocols;

public sealed class WireJsonException : JsonException
{
    internal WireJsonException(Type contractType, string message, string? path = null)
        : base(message, path, lineNumber: null, bytePositionInLine: null)
    {
        ContractType = contractType;
    }

    public Type ContractType { get; }
}
