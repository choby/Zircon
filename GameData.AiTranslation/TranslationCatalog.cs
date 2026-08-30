using Library.SystemModels;
using MirDB;

namespace GameData.AiTranslation;

internal sealed record TranslationFieldRule(
    Type ObjectType,
    string Property,
    string Description,
    string[] Rules,
    Func<DBObject, bool>? Predicate = null)
{
    public bool Matches(DBObject item) =>
        ObjectType.IsAssignableFrom(item.GetType()) && (Predicate?.Invoke(item) ?? true);
}

internal static class TranslationCatalog
{
    private static readonly string[] NameRules =
    [
        "根据《传奇3》游戏信息汉化为简体中文名称，保持和官方《传奇3》中文名称一致，并在同类型对象中保持术语一致。"
    ];

    private static readonly string[] TextRules =
    [
        "根据《传奇3》游戏信息汉化为简体中文，保持原意、换行、数字和格式标记。",
        "不得增删或改写占位符、颜色标记、按钮目标和变量编号。"
    ];

    public static readonly IReadOnlyList<TranslationFieldRule> Rules =
    [
        Name<BundleInfo>(nameof(BundleInfo.Description), "礼包显示说明"),
        Name<CastleInfo>(nameof(CastleInfo.Name), "城堡名称"),
        Text<CompanionInfo>(nameof(CompanionInfo.Description), "伙伴说明"),
        Text<CompanionSpeech>(nameof(CompanionSpeech.Speech), "伙伴台词"),
        Name<CurrencyInfo>(nameof(CurrencyInfo.Name), "货币显示名称", "导入时会同步更新 NPC 货币动作和检查中的名称引用。"),
        Name<CurrencyInfo>(nameof(CurrencyInfo.Abbreviation), "货币简称"),
        Name<DungeonInfo>(nameof(DungeonInfo.Name), "地牢名称"),
        Text<DungeonInfo>(nameof(DungeonInfo.Description), "地牢说明"),
        Name<FameInfo>(nameof(FameInfo.Name), "声望名称"),
        Text<FameInfo>(nameof(FameInfo.Description), "声望说明"),
        Name<FishingInfo>(nameof(FishingInfo.Name), "钓鱼数据名称"),
        Name<HelpInfo>(nameof(HelpInfo.Title), "帮助主题标题"),
        Text<HelpInfo>(nameof(HelpInfo.Description), "帮助主题说明"),
        Name<HelpPageInfo>(nameof(HelpPageInfo.Title), "帮助页面标题"),
        Name<HelpItemInfo>(nameof(HelpItemInfo.Title), "帮助条目标题"),
        Text<HelpItemInfo>(nameof(HelpItemInfo.Content), "帮助条目正文"),
        Name<InstanceInfo>(nameof(InstanceInfo.Name), "副本名称"),
        Name<ItemInfo>(nameof(ItemInfo.ItemName), "物品名称", "货币掉落物由稳定的 CurrencyType/DropItem 关系保护。"),
        Text<ItemInfo>(nameof(ItemInfo.Description), "物品说明"),
        Name<LootBoxInfo>(nameof(LootBoxInfo.Description), "战利品箱说明"),
        Name<MagicInfo>(nameof(MagicInfo.Name), "技能名称"),
        Text<MagicInfo>(nameof(MagicInfo.Description), "技能说明"),
        Name<MapInfo>(nameof(MapInfo.Description), "地图显示名称；FileName 不会被导出"),
        Name<MapRegion>(nameof(MapRegion.Description), "地图区域名称"),
        Name<MilestoneInfo>(nameof(MilestoneInfo.Title), "里程碑标题"),
        Name<MilestoneInfo>(nameof(MilestoneInfo.Category), "里程碑分类"),
        Text<MilestoneInfo>(nameof(MilestoneInfo.Description), "里程碑说明"),
        Text<MilestoneInfo>(nameof(MilestoneInfo.Task), "里程碑任务显示文本"),
        Name<MonsterInfo>(nameof(MonsterInfo.MonsterName), "怪物名称"),
        Name<NPCInfo>(nameof(NPCInfo.NPCName), "NPC 名称", "必须保持下划线数量和位置结构；下划线前后可分别汉化。"),
        Name<NPCPage>(nameof(NPCPage.Description), "NPC 页面管理名称"),
        Text<NPCPage>(nameof(NPCPage.Say), "NPC 对话正文"),
        Text<NPCAction>(nameof(NPCAction.StringParameter1), "NPC 消息动作文本", predicate: item => ((NPCAction)item).ActionType == NPCActionType.Message),
        Name<QuestInfo>(nameof(QuestInfo.QuestName), "任务名称"),
        Text<QuestInfo>(nameof(QuestInfo.AcceptText), "任务接受文本"),
        Text<QuestInfo>(nameof(QuestInfo.ProgressText), "任务进行中文本"),
        Text<QuestInfo>(nameof(QuestInfo.CompletedText), "任务完成文本"),
        Text<QuestInfo>(nameof(QuestInfo.ArchiveText), "任务归档文本"),
        Text<QuestTask>(nameof(QuestTask.MobDescription), "任务目标显示文本"),
        Name<SetInfo>(nameof(SetInfo.SetName), "套装名称"),
        Name<StoreInfo>(nameof(StoreInfo.Filter), "商城自定义分类", "逗号分隔的分类必须逐项汉化；相同源分类必须使用完全相同的译名。"),
        Name<WorldEventInfo>(nameof(WorldEventInfo.Description), "世界事件名称"),
        Name<PlayerEventInfo>(nameof(PlayerEventInfo.Description), "玩家事件名称"),
        Name<MonsterEventInfo>(nameof(MonsterEventInfo.Description), "怪物事件名称"),
        Text<BaseEventAction>(nameof(BaseEventAction.StringParameter1), "事件发送给玩家的消息", predicate: item => ((BaseEventAction)item).Type == EventActionType.PlayerMessage)
    ];

    public static TranslationFieldRule? Find(DBObject item, string property) =>
        Rules.FirstOrDefault(rule => rule.Property == property && rule.Matches(item));

    private static TranslationFieldRule Name<T>(string property, string description, string? extraRule = null, Func<DBObject, bool>? predicate = null)
        where T : DBObject
    {
        string[] rules = extraRule is null ? NameRules : [.. NameRules, extraRule];
        return new TranslationFieldRule(typeof(T), property, description, rules, predicate);
    }

    private static TranslationFieldRule Text<T>(string property, string description, string? extraRule = null, Func<DBObject, bool>? predicate = null)
        where T : DBObject
    {
        string[] rules = extraRule is null ? TextRules : [.. TextRules, extraRule];
        return new TranslationFieldRule(typeof(T), property, description, rules, predicate);
    }
}
