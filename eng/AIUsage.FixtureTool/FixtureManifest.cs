using System.Text.Json.Serialization;

namespace AIUsage.FixtureTool;

public sealed class FixtureManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("cases")]
    public List<FixtureManifestEntry> Cases { get; init; } = [];
}

public sealed class FixtureManifestEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;
}
