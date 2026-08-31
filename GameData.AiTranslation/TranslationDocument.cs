using System.Text.Json.Serialization;

namespace GameData.AiTranslation;

internal sealed class TranslationDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("targetLanguage")]
    public string TargetLanguage { get; set; } = "zh-CN";

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }

    [JsonPropertyName("sourceDatabaseSha256")]
    public string SourceDatabaseSha256 { get; set; } = string.Empty;

    [JsonPropertyName("sourceDatabaseVersion")]
    public string? SourceDatabaseVersion { get; set; }

    [JsonPropertyName("entryCount")]
    public int EntryCount { get; set; }

    [JsonPropertyName("catalogProtectionHash")]
    public string CatalogProtectionHash { get; set; } = string.Empty;

    [JsonPropertyName("aiInstructions")]
    public string AiInstructions { get; set; } = "只修改 entries[].translation，其他字段禁止修改。";

    [JsonPropertyName("entries")]
    public List<TranslationEntry> Entries { get; set; } = [];
}

internal sealed class TranslationEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("translation")]
    public string Translation { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("rules")]
    public string[] Rules { get; set; } = [];

    [JsonPropertyName("isIdentity")]
    public bool IsIdentity { get; set; }

    [JsonPropertyName("protectionHash")]
    public string ProtectionHash { get; set; } = string.Empty;
}

internal sealed record ExportResult(int EntryCount, string TranslationFile, string InstructionsFile);
internal sealed record ImportResult(int ChangedCount, string? BackupDirectory);
internal sealed record RemoteTranslationResult(int TranslatedCount, int SkippedCount, string BackupFile);

internal sealed class CodexTranslationResponse
{
    [JsonPropertyName("translations")]
    public List<CodexTranslationItem> Translations { get; set; } = [];
}

internal sealed class CodexTranslationItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("translation")]
    public string Translation { get; set; } = string.Empty;
}
