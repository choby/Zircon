using MirDB;

namespace Server.Web.Models;

public sealed record GameDataColumnDefinition(
    string Field,
    string Title,
    string? Width = null,
    bool Editable = true);

public sealed class GameDataTableDefinition
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required Type ModelType { get; init; }
    public required IReadOnlyList<GameDataColumnDefinition> Columns { get; init; }
    public IReadOnlyList<GameDataRelationDefinition> Relations { get; set; } = [];
}

public sealed class GameDataRelationDefinition
{
    public required string Property { get; init; }
    public required string Title { get; init; }
    public required Type ItemType { get; init; }
    public required bool Aggregate { get; init; }
    public required IReadOnlyList<GameDataColumnDefinition> Columns { get; init; }
}

public sealed class GameDataViewDefinition
{
    public required string Key { get; init; }
    public required string Route { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required IReadOnlyList<GameDataTableDefinition> Tables { get; init; }
}

public sealed record GameDataReferenceOption(int Index, string Label, DBObject Value);
