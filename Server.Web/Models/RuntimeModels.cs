namespace Server.Web.Models;

public sealed record OrphanDiagnosticRow(
    string ObjectType,
    string Association,
    int TotalRows,
    int LinkedRows,
    int CleanableOrphans,
    int ExistingTemporaryOrphans,
    int MarkedTemporary,
    string SampleIndices);

public sealed record RuntimeCollectionDescriptor(
    string Key,
    string DisplayName,
    string FieldName,
    string LegacyView,
    IReadOnlyList<RuntimeColumnDefinition> Columns);

public sealed record RuntimeColumnDefinition(string Field, string Title, bool Editable = true);

public sealed class RuntimeDataRow
{
    public required int Index { get; init; }
    public required string ETag { get; init; }
    public required Dictionary<string, object?> Values { get; init; }
}
