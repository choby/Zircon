using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library;
using Library.SystemModels;
using MirDB;
using Server;
using Server.DBModels;
using Server.Envir;
using Server.Web.Models;

namespace Server.Web.Services;

/// <summary>
/// Owns the single writable System.db session used by the Web administrator.
/// The UI supplies explicit, per-View column allowlists; this service only coordinates
/// the original MirDB objects, relationships, converters, saving and index semantics.
/// </summary>
public sealed class GameDataSessionService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AdminAuditService _audit;
    private readonly IGameServerController _gameServer;
    private Session _session;

    public GameDataSessionService(AdminAuditService audit, IGameServerController gameServer)
    {
        _audit = audit;
        _gameServer = gameServer;
        _session = CreateSession();
        SystemDataBootstrapper.Apply(_session);
    }

    public async Task<IReadOnlyList<object>> ReadAsync(GameDataTableDefinition table, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return GetObjects(table.ModelType).Where(item => !item.IsTemporary).Cast<object>().ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task CreateAsync(GameDataTableDefinition table, DBObject values, string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject target = CreateObject(table.ModelType);
            CopyAllowedValues(table, values, target);
            ValidateObject(target);
            _session.Save(true);
            _audit.Record(user, "GameData.Create", $"{table.ModelType.Name} #{target.Index}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task UpdateAsync(GameDataTableDefinition table, DBObject values, string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject target = GetObjects(table.ModelType).Single(item => item.Index == values.Index);
            CopyAllowedValues(table, values, target);
            ValidateObject(target);
            _session.Save(true);
            _audit.Record(user, "GameData.Update", $"{table.ModelType.Name} #{target.Index}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(GameDataTableDefinition table, int index, string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject target = GetObjects(table.ModelType).Single(item => item.Index == index);
            target.Delete();
            _session.Save(true);
            _audit.Record(user, "GameData.Delete", $"{table.ModelType.Name} #{index}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<object>> ReadRelationAsync(
        GameDataTableDefinition table, int parentIndex, GameDataRelationDefinition relation,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject parent = GetObjects(table.ModelType).Single(item => item.Index == parentIndex);
            return GetRelation(parent, relation).Cast<object>().ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task CreateRelationAsync(
        GameDataTableDefinition table, int parentIndex, GameDataRelationDefinition relation, DBObject values,
        string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject parent = GetObjects(table.ModelType).Single(item => item.Index == parentIndex);
            IBindingList list = GetRelation(parent, relation);
            DBObject target = (DBObject)(list.AddNew() ?? throw new InvalidOperationException("无法创建关联记录。"));
            CopyAllowedValues(relation.ItemType, relation.Columns, values, target);
            ValidateObject(target);
            _session.Save(true);
            _audit.Record(user, "GameData.Relation.Create", $"{table.ModelType.Name} #{parentIndex} / {relation.Property} #{target.Index}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task UpdateRelationAsync(
        GameDataTableDefinition table, int parentIndex, GameDataRelationDefinition relation, DBObject values,
        string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject parent = GetObjects(table.ModelType).Single(item => item.Index == parentIndex);
            DBObject target = GetRelation(parent, relation).Cast<DBObject>().Single(item => item.Index == values.Index);
            CopyAllowedValues(relation.ItemType, relation.Columns, values, target);
            ValidateObject(target);
            _session.Save(true);
            _audit.Record(user, "GameData.Relation.Update", $"{table.ModelType.Name} #{parentIndex} / {relation.Property} #{target.Index}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteRelationAsync(
        GameDataTableDefinition table, int parentIndex, GameDataRelationDefinition relation, int childIndex,
        string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject parent = GetObjects(table.ModelType).Single(item => item.Index == parentIndex);
            IBindingList list = GetRelation(parent, relation);
            DBObject target = list.Cast<DBObject>().Single(item => item.Index == childIndex);
            if (relation.Aggregate) target.Delete();
            else list.Remove(target);
            _session.Save(true);
            _audit.Record(user, "GameData.Relation.Delete", $"{table.ModelType.Name} #{parentIndex} / {relation.Property} #{childIndex}");
        }
        catch
        {
            _session = CreateSession();
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task InsertAfterAsync(GameDataTableDefinition table, int index, string user, CancellationToken cancellationToken = default)
    {
        if (_gameServer.State != GameServerState.Stopped)
            throw new InvalidOperationException("指定位置插入会移动索引和 Users.db 引用，只能在游戏服停止后执行。");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            MethodInfo method = typeof(Session).GetMethod(nameof(Session.InsertObjectAfter))!.MakeGenericMethod(table.ModelType);
            method.Invoke(_session, [index]);
            _session.Save(true);
            _audit.Record(user, "GameData.InsertAfter", $"{table.ModelType.Name} after #{index}");
        }
        finally { _gate.Release(); }
    }

    public async Task<string> ExportJsonAsync(GameDataTableDefinition table, IReadOnlyCollection<int>? indices = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DBObject[] selected = GetObjects(table.ModelType)
                .Where(item => indices is null || indices.Contains(item.Index))
                .ToArray();
            Array typed = Array.CreateInstance(table.ModelType, selected.Length);
            Array.Copy(selected, typed, selected.Length);
            return JsonSerializer.Serialize(typed, typed.GetType(), CreateJsonOptions(table.ModelType));
        }
        finally { _gate.Release(); }
    }

    public async Task<(int Imported, int Resolved)> ImportJsonAsync(
        GameDataTableDefinition table,
        string json,
        string user,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("导入文件为空。");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _session.Save(true);
            try
            {
                ImportReferenceResolver.SetDeferredResolution(true);
                Type arrayType = table.ModelType.MakeArrayType();
                Array imported = (Array?)JsonSerializer.Deserialize(json, arrayType, CreateJsonOptions(table.ModelType)) ??
                                 throw new JsonException("导入文件不是有效的数据数组。");
                (int resolved, int remaining) = ImportReferenceResolver.ResolvePendingReferences(_session);
                if (remaining != 0)
                    throw new InvalidDataException($"导入包含 {remaining} 个无法解析的引用，已回滚本次导入。");
                foreach (DBObject item in imported.Cast<DBObject>()) ValidateObject(item);
                _session.Save(true);
                _audit.Record(user, "GameData.Import", $"{table.ModelType.Name}: {imported.Length} rows, {resolved} references resolved");
                return (imported.Length, resolved);
            }
            catch
            {
                ImportReferenceResolver.SetDeferredResolution(true);
                _session = CreateSession();
                throw;
            }
        }
        finally { _gate.Release(); }
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { _session = CreateSession(); }
        finally { _gate.Release(); }
    }

    public async Task<string> SaveAndGetPathAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _session.Save(true);
            return _session.SystemPath;
        }
        finally { _gate.Release(); }
    }

    public IReadOnlyList<GameDataReferenceOption> GetReferenceOptions(Type referenceType)
    {
        return GetObjects(referenceType)
            .Where(item => !item.IsTemporary)
            .Select(item => new GameDataReferenceOption(item.Index, $"{item.Index}: {item}", item))
            .ToArray();
    }

    public DBObject? ResolveReference(Type referenceType, int? index) => index is null
        ? null
        : GetObjects(referenceType).SingleOrDefault(item => item.Index == index.Value);

    public async Task<IReadOnlyList<MapRegionAdminModel>> GetMapRegionsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return GetObjects(typeof(MapRegion)).Cast<MapRegion>()
                .Where(region => region.Map is not null)
                .Select(region => new MapRegionAdminModel(
                    region.Index, region.Map.Index, region.Map.FileName, region.Map.Description,
                    region.Description, region.RegionType.ToString(), region.Size, ComputeETag(region)))
                .ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<MapCellPoint>> GetMapRegionPointsAsync(int regionIndex, int mapWidth, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            MapRegion region = GetObjects(typeof(MapRegion)).Cast<MapRegion>().Single(item => item.Index == regionIndex);
            return region.GetPoints(mapWidth).Select(point => new MapCellPoint(point.X, point.Y)).ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task<string> SaveMapRegionAsync(
        int regionIndex, int mapWidth, int mapHeight, string expectedETag,
        IReadOnlyCollection<MapCellPoint> points, string user, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            MapRegion region = GetObjects(typeof(MapRegion)).Cast<MapRegion>().Single(item => item.Index == regionIndex);
            if (!string.Equals(ComputeETag(region), expectedETag, StringComparison.Ordinal))
                throw new InvalidOperationException("地图区域已被其他管理员修改，请重新加载。");

            HashSet<Point> validated = points
                .Where(point => point.X >= 0 && point.X < mapWidth && point.Y >= 0 && point.Y < mapHeight)
                .Select(point => new Point(point.X, point.Y))
                .ToHashSet();
            if (validated.Count != points.Count) throw new InvalidOperationException("地图区域包含越界坐标。");

            if (validated.Count * 64 > mapWidth * mapHeight)
            {
                BitArray bits = new(mapWidth * mapHeight);
                foreach (Point point in validated) bits[point.Y * mapWidth + point.X] = true;
                region.BitRegion = bits;
                region.PointRegion = null!;
            }
            else
            {
                region.BitRegion = null!;
                region.PointRegion = validated.ToArray();
            }

            region.Size = validated.Count;
            _session.Save(true);
            _audit.Record(user, "MapRegion.Save", $"MapRegion #{regionIndex}, {validated.Count} cells");
            return ComputeETag(region);
        }
        finally { _gate.Release(); }
    }

    private static Session CreateSession()
    {
        Session session = new(SessionMode.System) { BackUpDelay = 60 };
        session.Initialize(Assembly.GetAssembly(typeof(ItemInfo))!, Assembly.GetAssembly(typeof(AccountInfo))!);
        return session;
    }

    private IEnumerable<DBObject> GetObjects(Type modelType)
    {
        object collection = _session.GetCollection(modelType);
        FieldInfo bindingField = collection.GetType().GetField("Binding", BindingFlags.Instance | BindingFlags.Public)!;
        return ((IEnumerable)bindingField.GetValue(collection)!).Cast<DBObject>();
    }

    private DBObject CreateObject(Type modelType)
    {
        object collection = _session.GetCollection(modelType);
        return (DBObject)collection.GetType().GetMethod("CreateNewObject", BindingFlags.Instance | BindingFlags.Public)!.Invoke(collection, null)!;
    }

    private static IBindingList GetRelation(DBObject parent, GameDataRelationDefinition relation) =>
        (IBindingList)(parent.GetType().GetProperty(relation.Property)?.GetValue(parent) ??
                       throw new InvalidOperationException($"无法读取关联 {parent.GetType().Name}.{relation.Property}。"));

    private static void CopyAllowedValues(GameDataTableDefinition table, DBObject source, DBObject target)
        => CopyAllowedValues(table.ModelType, table.Columns, source, target);

    private static void CopyAllowedValues(
        Type modelType, IReadOnlyList<GameDataColumnDefinition> columns, DBObject source, DBObject target)
    {
        foreach (GameDataColumnDefinition column in columns.Where(column => column.Editable))
        {
            PropertyInfo? property = modelType.GetProperty(column.Field, BindingFlags.Instance | BindingFlags.Public);
            if (property?.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) continue;
            property.SetValue(target, property.GetValue(source));
        }
    }

    private void ValidateObject(DBObject item)
    {
        switch (item)
        {
            case DungeonMapInfo dungeonMap when dungeonMap.Map is null:
                throw new InvalidDataException("DungeonMapInfo 必须引用地图。");
            case DungeonMapInfo dungeonMap:
                DungeonMapInfo? duplicate = GetObjects(typeof(DungeonMapInfo)).Cast<DungeonMapInfo>()
                    .FirstOrDefault(other => other != dungeonMap && other.Map == dungeonMap.Map);
                if (duplicate is not null)
                    throw new InvalidDataException($"地图“{dungeonMap.Map.ServerDescription}”已经属于副本“{duplicate.Dungeon?.Name}”。");
                break;
            case MineInfo mine when mine.Region is not null && mine.Map != mine.Region.Map:
                throw new InvalidDataException("采矿区域必须属于当前采矿地图。");
            case MovementInfo movement:
                ValidateMovementRegion(movement.SourceRegion, "源区域");
                ValidateMovementRegion(movement.DestinationRegion, "目标区域");
                break;
            case NPCInfo npc when npc.Region is not null && npc.Region.RegionType is not (RegionType.None or RegionType.Npc):
                throw new InvalidDataException("NPC 区域只能使用 None 或 Npc 类型。");
            case RespawnInfo respawn when respawn.Region is not null && respawn.Region.RegionType is not (RegionType.None or RegionType.Spawn or RegionType.SpawnConnection):
                throw new InvalidDataException("刷新区域只能使用 None、Spawn 或 SpawnConnection 类型。");
            case FishingInfo fishing when fishing.Region is not null && fishing.Region.RegionType is not (RegionType.None or RegionType.Spawn):
                throw new InvalidDataException("钓鱼区域只能使用 None 或 Spawn 类型。");
            case MilestoneInfoTask milestone when milestone.Region is not null && milestone.Region.RegionType is not (RegionType.None or RegionType.Area):
                throw new InvalidDataException("里程碑区域只能使用 None 或 Area 类型。");
            case CurrencyInfo currency when currency.DropItem is not null && currency.DropItem.ItemType != ItemType.Currency:
                throw new InvalidDataException("货币掉落物品必须是 Currency 类型。");
        }
    }

    private static void ValidateMovementRegion(MapRegion? region, string field)
    {
        if (region is null) return;
        if (region.RegionType is not (RegionType.None or RegionType.Connection or RegionType.SpawnConnection))
            throw new InvalidDataException($"{field}只能使用 None、Connection 或 SpawnConnection 类型。");
    }

    private JsonSerializerOptions CreateJsonOptions(Type modelType)
    {
        Type converterType = typeof(DBObjectArrayConverter<>).MakeGenericType(modelType);
        JsonConverter converter = (JsonConverter)Activator.CreateInstance(converterType, _session)!;
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { converter }
        };
    }

    private static string ComputeETag(DBObject item)
    {
        StringBuilder value = new(item.Index.ToString());
        foreach (PropertyInfo property in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.GetIndexParameters().Length == 0 &&
                                        (property.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))))
            value.Append('\0').Append(property.Name).Append('=').Append(property.GetValue(item));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString())));
    }

    public void Dispose() => _gate.Dispose();
}
