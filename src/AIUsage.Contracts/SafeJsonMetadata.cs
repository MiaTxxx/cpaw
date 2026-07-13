using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Contracts;

[JsonConverter(typeof(SafeJsonMetadataConverter))]
public sealed class SafeJsonMetadata : IReadOnlyDictionary<string, JsonElement>
{
    private static readonly HashSet<string> SensitiveKeys =
    [
        "accesstoken", "apikey", "authorization", "authtoken", "clientsecret", "cookie", "cookies", "credential",
        "credentials", "idtoken", "password", "privatekey", "refreshtoken", "secret", "token",
    ];

    private readonly IReadOnlyDictionary<string, JsonElement> values;

    public SafeJsonMetadata(IEnumerable<KeyValuePair<string, JsonElement>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal);
        ValidateObject(copy, "$metadata");
        this.values = copy;
    }

    public static SafeJsonMetadata Empty { get; } = new(Array.Empty<KeyValuePair<string, JsonElement>>());

    public static SafeJsonMetadata Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Safe metadata must be a JSON object.");
        }

        return new SafeJsonMetadata(document.RootElement.EnumerateObject().Select(property =>
            new KeyValuePair<string, JsonElement>(property.Name, property.Value)));
    }

    public int Count => values.Count;

    public IEnumerable<string> Keys => values.Keys;

    public IEnumerable<JsonElement> Values => values.Values;

    public JsonElement this[string key] => values[key];

    public bool ContainsKey(string key) => values.ContainsKey(key);

    public bool TryGetValue(string key, out JsonElement value) => values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator() => values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ValidateObject(IReadOnlyDictionary<string, JsonElement> objectValues, string path)
    {
        foreach (var pair in objectValues)
        {
            ValidateKey(pair.Key, path);
            ValidateValue(pair.Value, $"{path}.{pair.Key}");
        }
    }

    private static void ValidateKey(string key, string path)
    {
        var normalized = new string(key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        if (SensitiveKeys.Contains(normalized))
        {
            throw new JsonException($"Sensitive metadata key is not allowed at {path}.{key}.");
        }
    }

    private static void ValidateValue(JsonElement value, string path)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                ValidateObject(
                    value.EnumerateObject().ToDictionary(
                        property => property.Name,
                        property => property.Value,
                        StringComparer.Ordinal),
                    path);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    ValidateValue(item, $"{path}[{index++}]");
                }

                break;
            case JsonValueKind.String:
                var text = value.GetString()!;
                if (LooksLikeSecret(text))
                {
                    throw new JsonException($"Sensitive metadata value is not allowed at {path}.");
                }

                break;
        }
    }

    private static bool LooksLikeSecret(string value) =>
        value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("sk-", StringComparison.Ordinal)
        || value.StartsWith("ghp_", StringComparison.Ordinal)
        || value.StartsWith("AIza", StringComparison.Ordinal)
        || value.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal);
}

internal sealed class SafeJsonMetadataConverter : JsonConverter<SafeJsonMetadata>
{
    public override SafeJsonMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Safe metadata must be a JSON object.");
        }

        return new SafeJsonMetadata(document.RootElement.EnumerateObject().Select(property =>
            new KeyValuePair<string, JsonElement>(property.Name, property.Value)));
    }

    public override void Write(Utf8JsonWriter writer, SafeJsonMetadata value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var pair in value)
        {
            writer.WritePropertyName(pair.Key);
            pair.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
