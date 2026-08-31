using System.Text.Json.Serialization;

namespace Plugin.Abstractions;

public sealed record PluginManifest
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("entryAssembly")]
    public required string EntryAssembly { get; init; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
}
