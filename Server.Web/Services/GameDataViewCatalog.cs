using Library.SystemModels;
using Library.MirDB;
using MirDB;
using Server.Web.Models;

namespace Server.Web.Services;

public sealed class GameDataViewCatalog
{
    public IReadOnlyList<GameDataViewDefinition> Views { get; }
    private readonly IReadOnlyDictionary<string, GameDataViewDefinition> _byKey;

    public GameDataViewCatalog()
    {
        Views =
        [
            View<BaseStat>("base-stats", "/data/base-stats", "基础属性", "BaseStatView", "成长", "角色职业与等级基础属性。",
                "Class", "Level", "Health", "Mana", "BagWeight", "WearWeight", "HandWeight", "Accuracy", "Agility", "MinAC", "MaxAC", "MinMR", "MaxMR", "MinDC", "MaxDC", "MinMC", "MaxMC", "MinSC", "MaxSC"),
            View<MagicInfo>("magics", "/data/magics", "技能数据", "MagicInfoView", "成长", "技能、等级需求、经验与说明。",
                "Index", "Name", "Magic", "School", "RequiredClass", "Icon", "BaseCost", "LevelCost", "MinBasePower", "MaxBasePower", "MinLevelPower", "MaxLevelPower", "NeedLevel1", "NeedLevel2", "NeedLevel3", "Experience1", "Experience2", "Experience3", "Delay", "Description", "Property"),
            View<FameInfo>("fame", "/data/fame", "声望数据", "FameInfoView", "成长", "声望、属性与奖励。",
                "Name", "Shape", "Description", "Cost", "Order", "Stat", "Amount", "Item"),
            View<DisciplineInfo>("disciplines", "/data/disciplines", "修炼数据", "DisciplineInfoView", "成长", "修炼等级、经验、金币与专注点要求。",
                "Level", "RequiredLevel", "RequiredExperience", "RequiredGold", "FocusPoints"),
            View<CompanionInfo>("companions", "/data/companions", "伙伴数据", "CompanionInfoView", "成长", "伙伴信息、对白、等级与技能。",
                "MonsterInfo", "Price", "Available", "UnlockItem", "Currency", "Description", "Speech"),
            View<CurrencyInfo>("currencies", "/data/currencies", "货币数据", "CurrencyInfoView", "成长", "货币、掉落物品、兑换率与图像。",
                "Name", "Abbreviation", "Type", "DropItem", "ExchangeRate", "Category", "Image", "Amount"),
            View<HelpInfo>("help", "/data/help", "帮助内容", "HelpInfoView", "成长", "游戏内帮助主题、页面和内容。",
                "Title", "Order", "Description", "Content", "Items"),

            View<MapInfo>("maps", "/data/maps", "地图数据", "MapInfoView", "地图", "地图属性、环境与限制。",
                "FileName", "Description", "MiniMap", "Light", "Fight", "RequiredClass", "AllowRT", "CanHorse", "AllowTT", "SkillDelay", "ReconnectMap", "MinimumLevel", "MaximumLevel", "CanMine", "CanAutoPath", "Music", "CanMarriageRecall", "Weather", "Background", "Size"),
            View<MapRegion>("map-regions", "/data/map-regions", "地图区域", "MapRegionView", "地图", "地图区域类型与范围数据；可视化编辑仍使用地图编辑器。",
                "Map", "Description", "RegionType", "Size"),
            View<InstanceInfo>("instances", "/data/instances", "实例数据", "InstanceInfoView", "地图", "实例入口、限制、计时与奖励规则。",
                "Name", "MaxInstances", "ShowOnDungeonFinder", "AllowRejoin", "SavePlace", "MinPlayerLevel", "MaxPlayerLevel", "MinPlayerCount", "MaxPlayerCount", "ConnectRegion", "ReconnectRegion", "Type", "CooldownTimeInMinutes", "SafeZoneOnly", "RequiredItem", "RequiredItemSingleUse", "TimeLimitInMinutes", "AllowTeleport", "ShowTimer"),
            View<DungeonInfo>("dungeons", "/data/dungeons", "地牢数据", "DungeonInfoView", "地图", "地牢及楼层地图配置。",
                "Name", "Description", "SpawnMultiplier", "AverageMonsterLevel", "AverageMonsterExperience", "Floor", "Role", "Map"),
            View<MovementInfo>("movements", "/data/movements", "移动连接", "MovementInfoView", "地图", "区域间移动、实例与物品条件。",
                "SourceRegion", "DestinationRegion", "Icon", "NeedItem", "NeedSpawn", "Effect", "RequiredClass", "NeedInstance", "NeedHole", "SkipValidation"),
            View<SafeZoneInfo>("safe-zones", "/data/safe-zones", "安全区", "SafeZoneInfoView", "地图", "安全区、绑定区域与边界。",
                "Region", "BindRegion", "StartClass", "RedZone", "Border"),
            View<RespawnInfo>("respawns", "/data/respawns", "怪物刷新", "RespawnInfoView", "地图", "怪物刷新点、延迟与掉落组。",
                "Monster", "Region", "Delay", "Count", "DropSet", "EventSpawn", "Announce", "EasterEventChance", "RespawnIndex"),
            View<FishingInfo>("fishing", "/data/fishing", "钓鱼数据", "FishingInfoView", "地图", "钓鱼区域、品质与掉落。",
                "Name", "Region", "PerfectCatch", "ThrowQuality", "Item", "Chance"),
            View<CastleInfo>("castles", "/data/castles", "城堡数据", "CastleInfoView", "地图", "城堡地图、区域、目标和奖励。",
                "Name", "Map", "StartTime", "Duration", "ObjectiveRegion", "AttackSpawnRegion", "Item", "Discount", "CastleRegion", "Monster", "X", "Y", "Direction", "RepairCost"),
            new GameDataViewDefinition
            {
                Key = "events", Route = "/data/events", Title = "事件数据", Category = "地图",
                Description = "世界、玩家和怪物三类事件及触发、动作与属性。",
                Tables =
                [
                    Table<WorldEventInfo>("world", "世界事件", "Description", "MaxValue", "TrackingType", "ResetWhenMax"),
                    Table<PlayerEventInfo>("player", "玩家事件", "Description", "MaxValue", "TrackingType", "ResetWhenMax"),
                    Table<MonsterEventInfo>("monster", "怪物事件", "Description", "MaxValue", "TrackingType", "ResetWhenMax")
                ]
            },

            View<NPCInfo>("npcs", "/data/npcs", "NPC 数据", "NPCInfoView", "NPC 与任务", "NPC、区域、入口页面和要求。",
                "NPCName", "Image", "EntryPage", "Region", "FaceImage", "Category", "MapIcon", "GoodsIndex", "Requirement", "IntParameter1", "QuestParameter", "Class", "DaysOfWeek"),
            View<NPCPage>("npc-pages", "/data/npc-pages", "NPC 页面", "NPCPageView", "NPC 与任务", "NPC 对话页面、检查、动作与商品。",
                "Description", "DialogType", "Say", "SuccessPage", "FailPage", "Arguments", "Currency", "CheckType", "Operator", "StringParameter1", "IntParameter1", "IntParameter2", "ItemParameter1", "StatParameter1", "ActionType", "MapParameter1", "InstanceParameter1", "ButtonID", "DestinationPage", "ValueID", "DataCategory", "FieldType", "ValueType", "DataType", "ItemType", "Item", "Rate", "GoodsIndex"),
            View<QuestInfo>("quests", "/data/quests", "任务数据", "QuestInfoView", "NPC 与任务", "任务文本、条件、目标和奖励。",
                "QuestName", "QuestType", "AcceptText", "ProgressText", "CompletedText", "ArchiveText", "StartNPC", "FinishNPC", "Requirement", "IntParameter1", "QuestParameter", "Class", "Task", "ItemParameter", "Amount", "RegionParameter", "MobDescription", "Monster", "Map", "Chance", "DropSet", "Item", "Bound", "Duration", "Choice"),
            View<MilestoneInfo>("milestones", "/data/milestones", "里程碑", "MilestoneInfoView", "NPC 与任务", "里程碑条件、任务与奖励。",
                "Title", "Category", "Description", "Reward", "RewardAmount", "RequiredClass", "Task", "ShowCount", "Grade", "Magic", "Class", "Item", "Monster", "Region", "Currency", "Instance", "Amount", "Type", "Quest"),

            View<ItemInfo>("items", "/data/items", "物品数据", "ItemInfoView", "物品", "物品属性、限制、价格和外观。",
                "Index", "ItemName", "ItemType", "RequiredClass", "RequiredGender", "RequiredType", "RequiredAmount", "Shape", "ItemEffect", "ExteriorEffect", "Image", "Weight", "Durability", "Price", "StackSize", "SellRate", "StartItem", "CanRepair", "CanSell", "CanStore", "CanTrade", "CanDrop", "CanDeathDrop", "CanAutoPot", "Rarity", "Description", "Set", "BuffIcon", "PartCount"),
            View<ItemInfoStat>("item-stats", "/data/item-stats", "物品属性", "ItemInfoStatView", "物品", "物品附加属性。",
                "Item", "Stat", "Amount"),
            View<SetInfo>("sets", "/data/sets", "套装数据", "SetInfoView", "物品", "套装及套装属性。",
                "SetName", "Class", "Level", "Stat", "Amount"),
            View<StoreInfo>("stores", "/data/stores", "商店数据", "StoreInfoView", "物品", "商品、价格、期限和可用条件。",
                "Item", "Price", "HuntGoldPrice", "Filter", "Available", "Duration"),
            View<WeaponCraftStatInfo>("weapon-craft-stats", "/data/weapon-craft-stats", "武器制作属性", "WeaponCraftStatInfoView", "物品", "武器制作属性范围与权重。",
                "RequiredClass", "Stat", "MinValue", "MaxValue", "Weight"),
            View<BundleInfo>("bundles", "/data/bundles", "礼包数据", "BundleInfoView", "物品", "礼包内容、格数与自动开启。",
                "Index", "Description", "Type", "SlotSize", "AutoOpen", "Item", "Amount"),
            View<LootBoxInfo>("loot-boxes", "/data/loot-boxes", "宝箱数据", "LootBoxInfoView", "物品", "宝箱货币和物品内容。",
                "Index", "Description", "Currency", "Item", "Amount"),

            View<MonsterInfo>("monsters", "/data/monsters", "怪物数据", "MonsterInfoView", "怪物", "怪物基础属性、行为与标记。",
                "MonsterName", "Image", "AI", "Level", "Experience", "ViewRange", "CoolEye", "AttackDelay", "MoveDelay", "IsBoss", "Undead", "CanPush", "CanTame", "Flag", "FaceImage"),
            View<MonsterInfoStat>("monster-stats", "/data/monster-stats", "怪物属性", "MonsterInfoStatView", "怪物", "怪物附加属性。",
                "Monster", "Stat", "Amount"),
            View<DropInfo>("drops", "/data/drops", "掉落数据", "DropInfoView", "怪物", "怪物掉落物品、概率和数量。",
                "Monster", "Item", "Chance", "Amount", "DropSet", "PartOnly", "EasterEvent")
        ];

        _byKey = Views.ToDictionary(view => view.Key, StringComparer.OrdinalIgnoreCase);

        IReadOnlyDictionary<Type, LegacyRelationSpec[]> relations = new Dictionary<Type, LegacyRelationSpec[]>
        {
            [typeof(MapInfo)] =
            [
                Rel("Regions", ["Description", "Size"]),
                Rel("Guards", ["Monster", "X", "Y", "Direction"]),
                Rel("Mining", ["Item", "Chance", "Region", "Quantity", "RestockTimeInMinutes"]),
                Rel("BuffStats", ["Stat", "Amount"])
            ],
            [typeof(WorldEventInfo)] =
            [
                Rel("Triggers", ["Type", "Value", "MaxTriggers"]),
                Rel("Actions", ["Type", "TriggerValue", "StringParameter1", "MonsterParameter1", "RespawnParameter1", "MapParameter1", "RegionParameter1", "InstanceParameter1", "Restrict"],
                    Rel("Stats", ["Stat", "Amount"]))
            ],
            [typeof(PlayerEventInfo)] =
            [
                Rel("Triggers", ["Type", "Value", "MapParameter1", "RegionParameter1", "InstanceParameter1", "MaxTriggers", "StringParameter1"]),
                Rel("Actions", ["Type", "StringParameter1", "MonsterParameter1", "RespawnParameter1", "MapParameter1", "RegionParameter1", "InstanceParameter1", "TriggerValue", "ItemParameter1", "Restrict"],
                    Rel("Stats", ["Stat", "Amount"]))
            ],
            [typeof(MonsterEventInfo)] =
            [
                Rel("Triggers", ["Monster", "DropSet", "Value", "MaxTriggers", "Type", "InstanceParameter1", "RegionParameter1", "MapParameter1"]),
                Rel("Actions", ["TriggerValue", "Type", "StringParameter1", "MonsterParameter1", "RespawnParameter1", "MapParameter1", "RegionParameter1", "InstanceParameter1", "ItemParameter1", "Restrict"],
                    Rel("Stats", ["Stat", "Amount"]))
            ],
            [typeof(FameInfo)] =
            [
                Rel("BuffStats", ["Stat", "Amount"]),
                Rel("ItemRewards", ["Amount", "Item"])
            ],
            [typeof(NPCInfo)] =
            [
                Rel("Requirements", ["Requirement", "IntParameter1", "QuestParameter", "Class", "DaysOfWeek"])
            ],
            [typeof(HelpInfo)] =
            [
                Rel("Pages", ["Title", "Order"], Rel("Items", ["Title", "Order", "Content"]))
            ],
            [typeof(CurrencyInfo)] =
            [
                Rel("Images", ["Image", "Amount"])
            ],
            [typeof(BundleInfo)] =
            [
                Rel("Contents", ["Amount", "Item"])
            ],
            [typeof(QuestInfo)] =
            [
                Rel("Requirements", ["Requirement", "IntParameter1", "QuestParameter", "Class"]),
                Rel("Tasks", ["Task", "ItemParameter", "Amount", "RegionParameter", "MobDescription"],
                    Rel("MonsterDetails", ["Monster", "Map", "Chance", "Amount", "DropSet"])),
                Rel("Rewards", ["Item", "Amount", "Bound", "Duration", "Class", "Choice"])
            ],
            [typeof(SetInfo)] =
            [
                Rel("SetStats", ["Stat", "Amount", "Class", "Level"])
            ],
            [typeof(ItemInfo)] =
            [
                Rel("ItemStats", ["Stat", "Amount"]),
                Rel("Drops", ["Monster", "Chance", "Amount", "DropSet", "PartOnly"])
            ],
            [typeof(InstanceInfo)] =
            [
                Rel("Maps", ["Map", "RespawnIndex"]),
                Rel("BuffStats", ["Stat", "Amount"])
            ],
            [typeof(CompanionInfo)] =
            [
                Rel("CompanionSpeeches", ["Speech", "Action"])
            ],
            [typeof(MonsterInfo)] =
            [
                Rel("MonsterInfoStats", ["Stat", "Amount"]),
                Rel("Respawns", ["Region", "Delay", "Count"]),
                Rel("Drops", ["Item", "Chance", "Amount", "DropSet", "PartOnly", "EasterEvent"])
            ],
            [typeof(FishingInfo)] =
            [
                Rel("Drops", ["Item", "Chance", "PerfectCatch", "ThrowQuality"])
            ],
            [typeof(NPCPage)] =
            [
                Rel("Checks", ["FailPage", "CheckType", "Operator", "StringParameter1", "IntParameter1", "IntParameter2", "ItemParameter1", "StatParameter1"]),
                Rel("Actions", ["ActionType", "StringParameter1", "IntParameter1", "IntParameter2", "MapParameter1", "InstanceParameter1", "ItemParameter1", "StatParameter1"]),
                Rel("Buttons", ["ButtonID", "DestinationPage"]),
                Rel("Values", ["ValueID", "DataCategory", "FieldType", "ValueType", "DataType"]),
                Rel("Types", ["ItemType"]),
                Rel("Goods", ["Item", "Rate", "GoodsIndex"])
            ],
            [typeof(CastleInfo)] =
            [
                Rel("Flags", ["Y", "X", "Monster"]),
                Rel("Gates", ["Y", "X", "Monster", "RepairCost"]),
                Rel("Guards", ["Direction", "Y", "X", "Monster", "RepairCost"])
            ],
            [typeof(DungeonInfo)] =
            [
                Rel("Maps", ["Floor", "Role", "Map"])
            ],
            [typeof(LootBoxInfo)] =
            [
                Rel("Contents", ["Amount", "Item"])
            ],
            [typeof(MilestoneInfo)] =
            [
                Rel("Tasks", ["Class", "Item", "Monster", "Region", "Currency", "Instance", "Amount", "Type", "Quest", "Magic"])
            ]
        };

        foreach (GameDataTableDefinition table in Views.SelectMany(view => view.Tables))
        {
            if (!relations.TryGetValue(table.ModelType, out LegacyRelationSpec[]? definitions)) continue;
            table.Relations = definitions.Select(definition => BuildRelation(table.ModelType, definition)).ToArray();
        }
    }

    public GameDataViewDefinition Get(string key) => _byKey.TryGetValue(key, out GameDataViewDefinition? view)
        ? view
        : throw new KeyNotFoundException($"未知游戏数据页面：{key}");

    private static GameDataViewDefinition View<T>(
        string key, string route, string title, string legacyView, string category, string description,
        params string[] fields) where T : MirDB.DBObject => new()
    {
        Key = key,
        Route = route,
        Title = title,
        Category = category,
        Description = $"{description} 对应 Windows 端 {legacyView}。",
        Tables = [Table<T>(key, title, fields)]
    };

    private static GameDataTableDefinition Table<T>(string key, string title, params string[] fields)
        where T : MirDB.DBObject
    {
        Type modelType = typeof(T);
        IReadOnlyList<GameDataColumnDefinition> columns = fields
            .Where(field => field != nameof(MirDB.DBObject.Index) && modelType.GetProperty(field) is not null)
            .Distinct(StringComparer.Ordinal)
            .Select(field => new GameDataColumnDefinition(field, Caption(field)))
            .ToArray();

        return new GameDataTableDefinition { Key = key, Title = title, ModelType = modelType, Columns = columns };
    }

    private static string Caption(string field) => field switch
    {
        "Index" => "序号", "Name" => "名称", "Description" => "描述", "Title" => "标题",
        "Amount" => "数量", "Item" => "物品", "Monster" => "怪物", "Map" => "地图",
        "Region" => "区域", "Level" => "等级", "Class" => "职业", "Category" => "分类",
        "Price" => "价格", "Type" => "类型", "Chance" => "概率", "Count" => "数量",
        "Delay" => "延迟", "Order" => "顺序", "Stat" => "属性", "Currency" => "货币",
        _ => SplitWords(field)
    };

    private static GameDataRelationDefinition BuildRelation(Type ownerType, LegacyRelationSpec definition)
    {
        System.Reflection.PropertyInfo property = ownerType.GetProperty(definition.Property) ??
            throw new InvalidOperationException($"{ownerType.Name}.{definition.Property} 不存在。");
        Type itemType = property.PropertyType.GetGenericArguments()[0];
        bool aggregate = property.GetCustomAttributes(typeof(AssociationAttribute), true)
            .OfType<AssociationAttribute>().SingleOrDefault()?.Aggregate == true;
        IReadOnlyList<GameDataColumnDefinition> columns = definition.Columns
            .Select(field => itemType.GetProperty(field) is not null
                ? new GameDataColumnDefinition(field, Caption(field))
                : throw new InvalidOperationException($"{itemType.Name}.{field} 不存在。"))
            .ToArray();
        return new GameDataRelationDefinition
        {
            Property = definition.Property,
            Title = Caption(definition.Property),
            ItemType = itemType,
            Aggregate = aggregate,
            Columns = columns,
            Relations = definition.Relations.Select(child => BuildRelation(itemType, child)).ToArray()
        };
    }

    private static LegacyRelationSpec Rel(string property, string[] columns, params LegacyRelationSpec[] relations) =>
        new(property, columns, relations);

    private sealed record LegacyRelationSpec(
        string Property,
        IReadOnlyList<string> Columns,
        IReadOnlyList<LegacyRelationSpec> Relations);

    private static string SplitWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        System.Text.StringBuilder result = new();
        for (int index = 0; index < text.Length; index++)
        {
            if (index > 0 && char.IsUpper(text[index]) && !char.IsUpper(text[index - 1])) result.Append(' ');
            result.Append(text[index]);
        }
        return result.ToString();
    }
}
