using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Server.Web.Services;

/// <summary>
/// Server.Web 的显示层中文文案。这里的转换只用于浏览器呈现，持久化键和值始终保持原样。
/// </summary>
public static class UiText
{
    private static readonly IReadOnlyDictionary<string, string> GameDataFields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Abbreviation"] = "缩写", ["Account"] = "账号", ["AcceptText"] = "接受文本", ["Accuracy"] = "准确",
        ["Action"] = "操作",
        ["Actions"] = "操作", ["ActionType"] = "操作类型", ["AI"] = "AI", ["AllowRejoin"] = "允许重新加入",
        ["AllowRT"] = "允许随机传送", ["AllowTeleport"] = "允许传送", ["AllowTT"] = "允许回城传送",
        ["Amount"] = "数量", ["Announce"] = "公告", ["Arguments"] = "参数", ["ArchiveText"] = "归档文本",
        ["AttackDelay"] = "攻击延迟", ["AttackSpawnRegion"] = "进攻方出生区域", ["AutoOpen"] = "自动开启",
        ["Agility"] = "敏捷", ["Available"] = "可用", ["AverageMonsterExperience"] = "怪物平均经验", ["AverageMonsterLevel"] = "怪物平均等级",
        ["Background"] = "背景", ["BagWeight"] = "背包负重", ["BaseCost"] = "基础消耗", ["BindRegion"] = "绑定区域",
        ["Border"] = "边界", ["Bound"] = "绑定", ["BuffIcon"] = "增益图标", ["BuffStats"] = "增益属性",
        ["ButtonID"] = "按钮 ID", ["Buttons"] = "按钮", ["CanAutoPath"] = "允许自动寻路", ["CanAutoPot"] = "允许自动喝药",
        ["CanDeathDrop"] = "死亡时可掉落", ["CanDrop"] = "可丢弃", ["CanHorse"] = "允许骑马",
        ["CanMarriageRecall"] = "允许夫妻传送", ["CanMine"] = "允许挖矿", ["CanPush"] = "可推动",
        ["CanRepair"] = "可修理", ["CanSell"] = "可出售", ["CanStore"] = "可存放", ["CanTame"] = "可驯服",
        ["CanTrade"] = "可交易", ["CastleRegion"] = "城堡区域", ["Category"] = "分类", ["Chance"] = "几率",
        ["CheckType"] = "检查类型", ["Checks"] = "检查", ["Choice"] = "可选", ["Class"] = "职业",
        ["CompletedText"] = "完成文本", ["CompanionSpeeches"] = "宠物对白", ["ConnectRegion"] = "连接区域",
        ["Content"] = "内容", ["Contents"] = "内容", ["CooldownTimeInMinutes"] = "冷却时间（分钟）",
        ["CoolEye"] = "识破隐身", ["Cost"] = "费用", ["Count"] = "数量", ["Currency"] = "货币",
        ["DataCategory"] = "数据分类", ["DataType"] = "数据类型", ["DaysOfWeek"] = "星期", ["Delay"] = "延迟",
        ["Description"] = "描述", ["DestinationPage"] = "目标页面", ["DestinationRegion"] = "目标区域",
        ["DialogType"] = "对话类型", ["Direction"] = "方向", ["Discount"] = "折扣", ["DropItem"] = "掉落物品",
        ["Drops"] = "掉落", ["DropSet"] = "掉落组", ["Durability"] = "持久度", ["Duration"] = "持续时间",
        ["EasterEvent"] = "复活节活动", ["EasterEventChance"] = "复活节活动几率", ["Effect"] = "效果",
        ["EntryPage"] = "入口页面", ["EventSpawn"] = "活动刷新", ["ExchangeRate"] = "金币兑换率",
        ["Experience"] = "经验", ["Experience1"] = "经验 1", ["Experience2"] = "经验 2", ["Experience3"] = "经验 3",
        ["ExteriorEffect"] = "外观效果", ["FaceImage"] = "头像", ["FailPage"] = "失败页面", ["FieldType"] = "字段类型",
        ["Fight"] = "战斗规则", ["FileName"] = "文件名", ["Filter"] = "筛选条件", ["FinishNPC"] = "结束 NPC",
        ["Flag"] = "标记", ["Flags"] = "旗帜",
        ["Floor"] = "楼层", ["FocusPoints"] = "专注点数", ["Gates"] = "城门", ["Goods"] = "商品",
        ["GoodsIndex"] = "商品索引", ["Grade"] = "品级", ["Guards"] = "守卫", ["HandWeight"] = "腕力负重",
        ["Health"] = "生命值", ["HuntGoldPrice"] = "狩猎金币价格", ["Icon"] = "图标", ["Image"] = "图像",
        ["Images"] = "图像", ["Index"] = "索引", ["Instance"] = "副本", ["InstanceParameter1"] = "副本参数 1",
        ["IntParameter1"] = "整数参数 1", ["IntParameter2"] = "整数参数 2", ["Item"] = "物品",
        ["ItemEffect"] = "物品效果", ["ItemName"] = "物品名称", ["ItemParameter"] = "物品参数",
        ["ItemParameter1"] = "物品参数 1", ["ItemRewards"] = "物品奖励", ["Items"] = "物品",
        ["IsBoss"] = "是否首领", ["ItemStats"] = "物品属性", ["ItemType"] = "物品类型", ["Level"] = "等级", ["LevelCost"] = "等级消耗",
        ["Light"] = "光照", ["Magic"] = "技能", ["Mana"] = "魔法值", ["Map"] = "地图", ["MapIcon"] = "地图图标",
        ["MapParameter1"] = "地图参数 1", ["Maps"] = "地图", ["MaxAC"] = "最大物理防御", ["MaxAmount"] = "最大数量",
        ["MaxBasePower"] = "最大基础威力", ["MaxInstances"] = "最大副本数", ["MaximumLevel"] = "最高等级",
        ["MaxDC"] = "最大破坏力", ["MaxLevelPower"] = "最大等级威力", ["MaxMC"] = "最大自然魔法",
        ["MaxMR"] = "最大魔法防御", ["MaxPlayerCount"] = "最多玩家", ["MaxPlayerLevel"] = "最高等级",
        ["MaxSC"] = "最大灵魂魔法",
        ["MaxTriggers"] = "最大触发次数", ["MaxValue"] = "最大值", ["MinAmount"] = "最小数量",
        ["MinAC"] = "最小物理防御", ["MinBasePower"] = "最小基础威力", ["MinDC"] = "最小破坏力",
        ["MinimumLevel"] = "最低等级", ["MinLevelPower"] = "最小等级威力", ["MinMC"] = "最小自然魔法",
        ["MinMR"] = "最小魔法防御", ["MinSC"] = "最小灵魂魔法", ["MinValue"] = "最小值",
        ["MinPlayerCount"] = "最少玩家", ["MinPlayerLevel"] = "最低等级", ["Mining"] = "采矿",
        ["MiniMap"] = "小地图", ["MobDescription"] = "怪物描述", ["Monster"] = "怪物",
        ["MonsterDetails"] = "怪物明细", ["MonsterInfo"] = "怪物信息", ["MonsterInfoStats"] = "怪物属性",
        ["MonsterName"] = "怪物名称", ["MonsterParameter1"] = "怪物参数 1", ["MoveDelay"] = "移动延迟",
        ["Music"] = "音乐", ["Name"] = "名称", ["NeedHole"] = "需要洞口", ["NeedInstance"] = "需要副本",
        ["NeedItem"] = "所需物品", ["NeedLevel1"] = "所需等级 1", ["NeedLevel2"] = "所需等级 2",
        ["NeedLevel3"] = "所需等级 3", ["NeedSpawn"] = "所需刷新点", ["NPCName"] = "NPC 名称",
        ["ObjectiveRegion"] = "目标区域", ["Operator"] = "运算符", ["Order"] = "顺序", ["Pages"] = "页面",
        ["PartCount"] = "碎片数量", ["PartOnly"] = "仅碎片", ["PerfectCatch"] = "完美捕获",
        ["Price"] = "价格", ["ProgressText"] = "进度文本", ["Property"] = "属性", ["Quantity"] = "数量",
        ["Quest"] = "任务", ["QuestName"] = "任务名称", ["QuestParameter"] = "任务参数", ["QuestType"] = "任务类型",
        ["Rate"] = "倍率", ["Rarity"] = "稀有度", ["ReconnectMap"] = "重连地图", ["ReconnectRegion"] = "重连区域",
        ["RedZone"] = "红名区域", ["Region"] = "区域", ["RegionParameter"] = "区域参数",
        ["RegionParameter1"] = "区域参数 1", ["Regions"] = "区域", ["RegionType"] = "区域类型",
        ["RepairCost"] = "修理费用", ["RequiredAmount"] = "所需数量", ["RequiredClass"] = "所需职业",
        ["RequiredExperience"] = "所需经验", ["RequiredGender"] = "所需性别", ["RequiredGold"] = "所需金币",
        ["RequiredItem"] = "所需物品", ["RequiredItemSingleUse"] = "一次性物品", ["RequiredLevel"] = "所需等级",
        ["RequiredType"] = "需求类型", ["Requirement"] = "要求", ["Requirements"] = "要求",
        ["ResetWhenMax"] = "达到最大值时重置", ["RespawnIndex"] = "刷新索引", ["RespawnParameter1"] = "刷新参数 1",
        ["Respawns"] = "刷新点", ["RestockTimeInMinutes"] = "补货时间（分钟）", ["Restrict"] = "限制",
        ["Reward"] = "奖励", ["RewardAmount"] = "奖励数量", ["Rewards"] = "奖励", ["Role"] = "用途",
        ["SafeZoneOnly"] = "仅可在安全区加入", ["SavePlace"] = "保存位置", ["Say"] = "对话内容",
        ["School"] = "流派", ["SellRate"] = "出售倍率", ["Set"] = "套装", ["SetName"] = "套装名称",
        ["SetStats"] = "套装属性", ["Shape"] = "外形", ["ShowCount"] = "显示数量",
        ["ShowOnDungeonFinder"] = "在副本查找器中显示", ["ShowTimer"] = "显示计时器", ["Size"] = "大小",
        ["SkillDelay"] = "技能延迟", ["SkipValidation"] = "跳过验证", ["SlotSize"] = "栏位数量",
        ["SourceRegion"] = "来源区域", ["SpawnMultiplier"] = "刷新倍率", ["Speech"] = "对白",
        ["StackSize"] = "堆叠上限", ["StartClass"] = "初始职业", ["StartItem"] = "初始物品",
        ["StartNPC"] = "开始 NPC", ["StartTime"] = "开始时间", ["Stat"] = "属性", ["Stats"] = "属性",
        ["StatParameter1"] = "属性参数 1", ["StringParameter1"] = "字符串参数 1", ["SuccessPage"] = "成功页面",
        ["Task"] = "任务内容", ["Tasks"] = "任务内容", ["ThrowQuality"] = "抛竿品质",
        ["TimeLimitInMinutes"] = "时间限制（分钟）", ["Title"] = "标题", ["TrackingType"] = "追踪类型",
        ["TriggerValue"] = "触发值", ["Triggers"] = "触发条件", ["Type"] = "类型", ["Types"] = "类型",
        ["Undead"] = "不死系", ["UnlockItem"] = "解锁物品", ["Value"] = "值", ["ValueID"] = "值 ID",
        ["Values"] = "值", ["ValueType"] = "值类型", ["ViewRange"] = "视野范围", ["Weather"] = "天气",
        ["WearWeight"] = "穿戴负重", ["Weight"] = "重量", ["X"] = "X 坐标", ["Y"] = "Y 坐标"
    };

    private static readonly IReadOnlyDictionary<string, string> ConfigSections = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Network"] = "网络", ["System"] = "系统", ["Control"] = "登录与角色控制", ["Mail"] = "邮件",
        ["WebServer"] = "Web 服务器", ["Players"] = "玩家", ["Monsters"] = "怪物", ["Items"] = "物品",
        ["Rates"] = "倍率", ["Fishing"] = "钓鱼", ["AdminWeb"] = "Web 管理端"
    };

    private static readonly IReadOnlyDictionary<string, string> ConfigFields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["IPAddress"] = "IP 地址", ["Port"] = "游戏端口", ["TimeOut"] = "连接超时时间", ["PingDelay"] = "Ping 间隔",
        ["UserCountPort"] = "用户数查询端口", ["MaxPacket"] = "最大数据包数", ["PacketBanTime"] = "数据包封禁时长",
        ["SyncRemotePreffix"] = "远程同步地址前缀", ["CheckVersion"] = "检查客户端版本", ["VersionPath"] = "版本文件路径",
        ["MapPath"] = "地图路径", ["MasterPassword"] = "主密码", ["SyncKey"] = "同步密钥", ["ClientPath"] = "客户端路径",
        ["ReleaseDate"] = "发布日期", ["TestServer"] = "测试服务器", ["StarterGuildName"] = "新手行会名称",
        ["LazyLoadMaps"] = "延迟加载地图", ["EasterEventEnd"] = "复活节活动结束时间",
        ["HalloweenEventEnd"] = "万圣节活动结束时间", ["ChristmasEventEnd"] = "圣诞节活动结束时间",
        ["DBSaveDelay"] = "数据库保存间隔", ["EncryptionEnabled"] = "启用数据库加密", ["EncryptionKey"] = "数据库加密密钥",
        ["AllowLogin"] = "允许登录", ["AllowNewAccount"] = "允许注册账号", ["AllowChangePassword"] = "允许修改密码",
        ["AllowRequestPasswordReset"] = "允许申请密码重置", ["AllowWebResetPassword"] = "允许网页重置密码",
        ["AllowManualResetPassword"] = "允许手动重置密码", ["AllowDeleteAccount"] = "允许删除账号",
        ["AllowManualActivation"] = "允许手动激活", ["AllowWebActivation"] = "允许网页激活",
        ["AllowRequestActivation"] = "允许申请激活", ["AllowSystemDBSync"] = "允许同步 System.db",
        ["AllowNewCharacter"] = "允许创建角色", ["AllowDeleteCharacter"] = "允许删除角色", ["AllowStartGame"] = "允许进入游戏",
        ["RelogDelay"] = "重新登录延迟", ["AllowWarrior"] = "允许战士", ["AllowWizard"] = "允许法师",
        ["AllowTaoist"] = "允许道士", ["AllowAssassin"] = "允许刺客", ["MailServer"] = "邮件服务器",
        ["MailPort"] = "邮件端口", ["MailUseSSL"] = "邮件使用 SSL", ["MailAccount"] = "邮件账号",
        ["MailPassword"] = "邮件密码", ["MailFrom"] = "发件人地址", ["MailDisplayName"] = "发件人显示名称",
        ["EnableWebServer"] = "启用 Web 服务器", ["WebPrefix"] = "Web 监听前缀", ["WebCommandLink"] = "Web 命令链接",
        ["ActivationSuccessLink"] = "激活成功链接", ["ActivationFailLink"] = "激活失败链接",
        ["ResetSuccessLink"] = "重置成功链接", ["ResetFailLink"] = "重置失败链接", ["DeleteSuccessLink"] = "删除成功链接",
        ["DeleteFailLink"] = "删除失败链接", ["BuyPrefix"] = "购买监听前缀", ["BuyAddress"] = "购买地址",
        ["IPNPrefix"] = "IPN 监听前缀", ["ReceiverEMail"] = "收款邮箱", ["ProcessGameGold"] = "处理游戏币订单",
        ["AllowBuyGameGold"] = "允许购买游戏币", ["MaxViewRange"] = "最大视野范围", ["ShoutDelay"] = "喊话延迟",
        ["GlobalDelay"] = "全局聊天延迟", ["MaxLevel"] = "最高等级", ["DayCycleCount"] = "昼夜循环次数",
        ["SkillExp"] = "技能经验倍率", ["AllowObservation"] = "允许观察", ["AllowWaypoints"] = "允许传送点",
        ["MaxWaypoints"] = "最大传送点数", ["BrownDuration"] = "褐名持续时间", ["PKPointRate"] = "PK 点数倍率",
        ["PKPointTickRate"] = "PK 点数增长间隔", ["RedPoint"] = "红名点数", ["PvPCurseDuration"] = "玩家对战诅咒时长",
        ["PvPCurseRate"] = "玩家对战诅咒概率", ["AutoReviveDelay"] = "自动复活延迟",
        ["RankChangeResetDelay"] = "排名变更重置间隔", ["EnableStruck"] = "启用受击效果", ["EnableHermit"] = "启用隐士系统",
        ["DeadDuration"] = "尸体保留时间", ["HarvestDuration"] = "采集持续时间", ["MysteryShipRegionIndex"] = "神秘船区域索引",
        ["LairRegionIndex"] = "巢穴区域索引", ["DropDuration"] = "掉落保留时间", ["DropDistance"] = "掉落距离",
        ["DropLayers"] = "掉落层数", ["TorchRate"] = "火把倍率", ["MaxGemPurity"] = "宝石最高纯度",
        ["SpecialRepairDelay"] = "特殊修理延迟", ["MaxLuck"] = "最大幸运", ["MaxCurse"] = "最大诅咒",
        ["CurseRate"] = "诅咒概率", ["LuckRate"] = "幸运概率", ["MaxStrength"] = "最大强度",
        ["StrengthAddRate"] = "强度增加概率", ["StrengthLossRate"] = "强度降低概率",
        ["DropVisibleOtherPlayers"] = "其他玩家可见掉落", ["EnableFortune"] = "启用幸运系统",
        ["AdminStartInGamemasterMode"] = "管理员初始为游戏管理员模式", ["AdminStartInObserverMode"] = "管理员初始为观察者模式",
        ["AdminStartInSupermanMode"] = "管理员初始为无敌模式", ["ExperienceRate"] = "经验倍率", ["DropRate"] = "掉落倍率",
        ["GoldRate"] = "金币倍率", ["SkillRate"] = "技能倍率", ["CompanionRate"] = "宠物倍率",
        ["FishEnablePerfectCatch"] = "启用完美捕获", ["FishNibbleChanceBase"] = "基础咬钩几率",
        ["FishPointsRequired"] = "所需钓鱼点数", ["FishPointSuccessRewardMin"] = "成功奖励最小点数",
        ["FishPointSuccessRewardMax"] = "成功奖励最大点数", ["FishPointFailureRewardMin"] = "失败奖励最小点数",
        ["FishPointFailureRewardMax"] = "失败奖励最大点数", ["AdminWebEnabled"] = "启用 Web 管理端",
        ["AdminWebHost"] = "Web 管理端主机", ["AdminWebPort"] = "Web 管理端端口", ["AdminUserName"] = "管理员账号",
        ["AdminAutoStartGameServer"] = "自动启动游戏服务器"
    };

    private static readonly IReadOnlyDictionary<string, string> EnumLabels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["MirClass.Warrior"] = "战士", ["MirClass.Wizard"] = "法师", ["MirClass.Taoist"] = "道士", ["MirClass.Assassin"] = "刺客",
        ["MirGender.Male"] = "男", ["MirGender.Female"] = "女", ["RequiredGender.Male"] = "男",
        ["RequiredGender.Female"] = "女", ["RequiredGender.None"] = "不限性别", ["RequiredClass.None"] = "不限职业",
        ["RequiredClass.Warrior"] = "战士", ["RequiredClass.Wizard"] = "法师", ["RequiredClass.Taoist"] = "道士",
        ["RequiredClass.Assassin"] = "刺客", ["RequiredClass.All"] = "全部职业", ["Rarity.Common"] = "普通",
        ["Rarity.Superior"] = "高级", ["Rarity.Elite"] = "稀世", ["LightSetting.Default"] = "默认",
        ["LightSetting.Light"] = "明亮", ["LightSetting.Night"] = "夜晚", ["LightSetting.Twilight"] = "黄昏",
        ["FightSetting.None"] = "默认", ["FightSetting.Safe"] = "安全区", ["FightSetting.Fight"] = "战斗区",
        ["InstanceType.Player"] = "单人", ["InstanceType.Group"] = "组队", ["InstanceType.Guild"] = "行会",
        ["InstanceType.Castle"] = "城堡", ["DungeonMapRole.Entrance"] = "入口", ["DungeonMapRole.Lobby"] = "大厅",
        ["DungeonMapRole.Floor"] = "楼层", ["DungeonMapRole.SideRoom"] = "侧室", ["DungeonMapRole.Transition"] = "过渡区",
        ["DungeonMapRole.Hub"] = "枢纽", ["DungeonMapRole.Maze"] = "迷宫", ["DungeonMapRole.BossFloor"] = "首领层",
        ["RegionType.None"] = "无", ["RegionType.Area"] = "区域", ["RegionType.Connection"] = "连接区",
        ["RegionType.Spawn"] = "刷新区", ["RegionType.Npc"] = "NPC 区", ["RegionType.SpawnConnection"] = "刷新连接区",
        ["RegionType.Path"] = "路径", ["Weather.None"] = "无", ["Weather.Rain"] = "雨", ["Weather.Snow"] = "雪",
        ["Weather.Fog"] = "雾", ["Weather.Lightning"] = "雷电", ["CurrencyType.Gold"] = "金币",
        ["CurrencyType.GameGold"] = "游戏币", ["CurrencyType.HuntGold"] = "狩猎金币", ["CurrencyType.Other"] = "其他",
        ["CurrencyType.FP"] = "声望点", ["CurrencyType.CP"] = "贡献点", ["CurrencyCategory.Basic"] = "基础",
        ["CurrencyCategory.Player"] = "玩家", ["CurrencyCategory.Event"] = "活动", ["CurrencyCategory.Map"] = "地图",
        ["CurrencyCategory.Other"] = "其他", ["QuestType.General"] = "普通", ["QuestType.Daily"] = "每日",
        ["QuestType.Weekly"] = "每周", ["QuestType.Repeatable"] = "可重复", ["QuestType.Story"] = "剧情",
        ["QuestType.Account"] = "账号", ["QuestTaskType.KillMonster"] = "击杀怪物", ["QuestTaskType.GainItem"] = "获得物品",
        ["QuestTaskType.VisitRegion"] = "到达区域", ["MilestoneGrade.Low"] = "低", ["MilestoneGrade.Medium"] = "中",
        ["MilestoneGrade.High"] = "高", ["MagicSchool.None"] = "无", ["MagicSchool.Passive"] = "被动",
        ["MagicSchool.Active"] = "主动", ["MagicSchool.Toggle"] = "切换", ["MagicSchool.Fire"] = "火系",
        ["MagicSchool.Ice"] = "冰系", ["MagicSchool.Lightning"] = "雷系", ["MagicSchool.Wind"] = "风系",
        ["MagicSchool.Holy"] = "神圣系", ["MagicSchool.Dark"] = "暗黑系", ["MagicSchool.Phantom"] = "幻影系",
        ["MagicSchool.Physical"] = "物理", ["MagicSchool.Horse"] = "骑术", ["MagicSchool.Discipline"] = "修炼",
        ["Element.None"] = "无", ["Element.Fire"] = "火", ["Element.Ice"] = "冰", ["Element.Lightning"] = "雷",
        ["Element.Wind"] = "风", ["Element.Holy"] = "神圣", ["Element.Dark"] = "暗黑", ["Element.Phantom"] = "幻影",
        ["ItemType.Nothing"] = "无", ["ItemType.Consumable"] = "消耗品", ["ItemType.Weapon"] = "武器",
        ["ItemType.Armour"] = "衣服", ["ItemType.Torch"] = "火把", ["ItemType.Helmet"] = "头盔",
        ["ItemType.Necklace"] = "项链", ["ItemType.Bracelet"] = "手镯", ["ItemType.Ring"] = "戒指",
        ["ItemType.Shoes"] = "鞋", ["ItemType.Poison"] = "毒药", ["ItemType.Amulet"] = "护身符",
        ["ItemType.Meat"] = "肉", ["ItemType.Ore"] = "矿石", ["ItemType.Book"] = "技能书",
        ["ItemType.Scroll"] = "卷轴", ["ItemType.Flower"] = "花", ["ItemType.System"] = "系统物品",
        ["ItemType.Emblem"] = "徽章", ["ItemType.Shield"] = "盾牌", ["ItemType.Costume"] = "时装",
        ["ItemType.Hook"] = "鱼钩", ["ItemType.Float"] = "浮漂", ["ItemType.Bait"] = "鱼饵",
        ["ItemType.Finder"] = "探测器", ["ItemType.Reel"] = "渔轮", ["ItemType.Currency"] = "货币",
        ["ItemType.Bundle"] = "礼包", ["NPCCategory.None"] = "无", ["NPCCategory.Store"] = "商店",
        ["NPCCategory.Equipment"] = "装备", ["NPCCategory.Storage"] = "仓库", ["NPCCategory.Transaction"] = "交易",
        ["NPCCategory.Teleportation"] = "传送", ["NPCCategory.Helper"] = "助手", ["NPCCategory.Event"] = "活动",
        ["NPCCategory.Common"] = "普通", ["MovementEffect.None"] = "无", ["MovementEffect.SpecialRepair"] = "特殊修理",
        ["DaysOfWeek.None"] = "不限", ["DaysOfWeek.Sunday"] = "星期日", ["DaysOfWeek.Monday"] = "星期一",
        ["DaysOfWeek.Tuesday"] = "星期二", ["DaysOfWeek.Wednesday"] = "星期三", ["DaysOfWeek.Thursday"] = "星期四",
        ["DaysOfWeek.Friday"] = "星期五", ["DaysOfWeek.Saturday"] = "星期六", ["DaysOfWeek.Weekday"] = "工作日",
        ["DaysOfWeek.Weekend"] = "周末", ["Operator.Equal"] = "等于", ["Operator.NotEqual"] = "不等于",
        ["Operator.LessThan"] = "小于", ["Operator.LessThanOrEqual"] = "小于或等于", ["Operator.GreaterThan"] = "大于",
        ["Operator.GreaterThanOrEqual"] = "大于或等于", ["EventTrackingType.Global"] = "全局",
        ["EventTrackingType.Player"] = "玩家", ["EventTrackingType.Group"] = "组队", ["EventTrackingType.Guild"] = "行会",
        ["EventTrackingType.Instance"] = "副本"
    };

    private static readonly IReadOnlyDictionary<string, string> IdentifierWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["None"] = "无", ["All"] = "全部", ["Any"] = "任意", ["Other"] = "其他", ["Default"] = "默认",
        ["Player"] = "玩家", ["Group"] = "组队", ["Guild"] = "行会", ["Account"] = "账号", ["Character"] = "角色",
        ["Monster"] = "怪物", ["Item"] = "物品", ["Map"] = "地图", ["Region"] = "区域", ["Instance"] = "副本",
        ["Quest"] = "任务", ["Magic"] = "技能", ["Companion"] = "宠物", ["Castle"] = "城堡", ["Event"] = "活动",
        ["Data"] = "数据", ["Value"] = "值", ["Type"] = "类型", ["Field"] = "字段", ["List"] = "列表",
        ["Add"] = "添加", ["Remove"] = "移除", ["Clear"] = "清除", ["Change"] = "更改", ["Set"] = "设置",
        ["Give"] = "给予", ["Take"] = "扣除", ["Check"] = "检查", ["Has"] = "拥有", ["Can"] = "可以",
        ["Start"] = "开始", ["Stop"] = "停止", ["Reset"] = "重置", ["Enter"] = "进入", ["Leave"] = "离开",
        ["Kill"] = "击杀", ["Die"] = "死亡", ["Death"] = "死亡", ["Damage"] = "伤害", ["Spawn"] = "刷新",
        ["Teleport"] = "传送", ["Gold"] = "金币", ["Currency"] = "货币", ["Message"] = "消息", ["Timer"] = "计时器",
        ["Level"] = "等级", ["Class"] = "职业", ["Gender"] = "性别", ["Weapon"] = "武器", ["Horse"] = "坐骑",
        ["Marriage"] = "结婚", ["Divorce"] = "离婚", ["Random"] = "随机", ["Fame"] = "声望", ["Roll"] = "掷骰结果",
        ["Min"] = "最低", ["Max"] = "最高", ["Less"] = "小于", ["Greater"] = "大于", ["Equal"] = "等于",
        ["Not"] = "不", ["Completed"] = "已完成", ["Accepted"] = "已接受", ["Daily"] = "每日", ["Weekly"] = "每周",
        ["General"] = "普通", ["Story"] = "剧情", ["Repeatable"] = "可重复", ["Fire"] = "火", ["Ice"] = "冰",
        ["Lightning"] = "雷", ["Wind"] = "风", ["Holy"] = "神圣", ["Dark"] = "暗黑", ["Phantom"] = "幻影",
        ["Attack"] = "攻击", ["Resistance"] = "抗性", ["Safe"] = "安全", ["Fight"] = "战斗", ["Global"] = "全局",
        ["Store"] = "商店", ["Storage"] = "仓库", ["Equipment"] = "装备", ["Repair"] = "修理", ["Refine"] = "精炼",
        ["Success"] = "成功", ["Failure"] = "失败", ["Low"] = "低", ["Medium"] = "中", ["High"] = "高",
        ["Male"] = "男", ["Female"] = "女", ["Warrior"] = "战士", ["Wizard"] = "法师", ["Taoist"] = "道士",
        ["Assassin"] = "刺客", ["Up"] = "上", ["Down"] = "下", ["Left"] = "左", ["Right"] = "右",
        ["Active"] = "主动", ["Passive"] = "被动", ["Physical"] = "物理", ["Special"] = "特殊", ["Bound"] = "绑定",
        ["Read"] = "已读", ["Online"] = "在线", ["Offline"] = "离线", ["Busy"] = "忙碌", ["Away"] = "离开"
    };

    private static readonly IReadOnlyDictionary<string, string> AuditActions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Login"] = "登录", ["LoginFailed"] = "登录失败", ["Logout"] = "退出登录", ["Server.Start"] = "启动服务器",
        ["Server.Stop"] = "停止服务器", ["Configuration.Save"] = "保存服务器配置", ["Configuration.Reload"] = "重新载入服务器配置",
        ["Configuration.CheckVersion"] = "检查客户端版本", ["Configuration.LocalSync"] = "本地同步数据库",
        ["Configuration.RemoteSync"] = "远程同步数据库", ["Configuration.Reencrypt"] = "转换数据库加密",
        ["GameData.Create"] = "新增游戏数据", ["GameData.Update"] = "修改游戏数据", ["GameData.Delete"] = "删除游戏数据",
        ["GameData.Relation.Create"] = "新增关联数据", ["GameData.Relation.Update"] = "修改关联数据",
        ["GameData.Relation.Delete"] = "删除关联数据", ["GameData.InsertAfter"] = "插入游戏数据",
        ["GameData.Import"] = "导入游戏数据", ["MapRegion.Save"] = "保存地图区域", ["Orphan.Scan"] = "扫描孤立数据",
        ["Orphan.Clean"] = "清理孤立数据", ["RuntimeData.Update"] = "修改运行时数据", ["Plugin.Load"] = "加载插件",
        ["Plugin.SetEnabled"] = "修改插件启用状态"
    };

    public static string GameDataField(string field) => GameDataFields.GetValueOrDefault(field, SplitIdentifier(field));
    public static string ConfigSection(string section) => ConfigSections.GetValueOrDefault(section, section);
    public static string ConfigField(string field) => ConfigFields.GetValueOrDefault(field, SplitIdentifier(field));
    public static string AuditAction(string action) => AuditActions.GetValueOrDefault(action, LocalizeAuditPrefix(action));
    public static string AuditDetail(string detail)
    {
        if (detail == "Invalid administrator credentials") return "管理员账号或密码无效";
        if (detail == "Administrator signed in") return "管理员已登录";
        if (detail == "Administrator signed out") return "管理员已退出";
        if (detail == "Reloaded Server.ini") return "已重新载入 Server.ini";
        if (detail == "Client hash changed") return "客户端版本哈希已变化";
        if (detail == "Client hash unchanged") return "客户端版本哈希未变化";
        if (detail == "Encryption enabled") return "已启用数据库加密";
        if (detail == "Encryption disabled") return "已禁用数据库加密";
        if (detail == "Start requested from dashboard") return "从服务器总览请求启动";
        if (detail == "Stop requested from dashboard") return "从服务器总览请求停止";
        if (detail.StartsWith("Updated ", StringComparison.Ordinal) && detail.EndsWith(" configuration values", StringComparison.Ordinal))
            return $"已更新 {detail[8..^21]} 项配置";
        return detail.Replace(" after #", "，插入于 #", StringComparison.Ordinal)
            .Replace(" rows; ", " 行；", StringComparison.Ordinal)
            .Replace(" orphans", " 个孤立项", StringComparison.Ordinal)
            .Replace(" references resolved", " 个引用已解析", StringComparison.Ordinal)
            .Replace(" rows, ", " 行，", StringComparison.Ordinal)
            .Replace(" cells", " 个单元格", StringComparison.Ordinal);
    }

    public static string Value(object? value)
    {
        if (value is null) return "—";
        if (value is bool boolean) return boolean ? "是" : "否";
        return value.GetType().IsEnum ? EnumValue(value.GetType(), value.ToString() ?? string.Empty) : value.ToString() ?? "—";
    }

    public static string EnumValue(Type enumType, string name)
    {
        if (name.Contains(','))
            return string.Join("、", name.Split(',', StringSplitOptions.TrimEntries).Select(part => EnumValue(enumType, part)));

        MemberInfo? member = enumType.GetMember(name).FirstOrDefault();
        DescriptionAttribute? description = member?.GetCustomAttribute<DescriptionAttribute>();
        if (!string.IsNullOrWhiteSpace(description?.Description)) return description.Description;

        object? statDescription = member?.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().Name == "StatDescription");
        string? title = statDescription?.GetType().GetProperty("Title")?.GetValue(statDescription) as string;
        if (!string.IsNullOrWhiteSpace(title))
        {
            if (enumType.Name == "Stat" && name.StartsWith("Min", StringComparison.Ordinal)) return $"最小{title}";
            if (enumType.Name == "Stat" && name.StartsWith("Max", StringComparison.Ordinal)) return $"最大{title}";
            if (enumType.Name == "Stat" && name.EndsWith("Attack", StringComparison.Ordinal)) return $"{title}攻击";
            if (enumType.Name == "Stat" && name.EndsWith("Resistance", StringComparison.Ordinal)) return $"{title}抗性";
            return title;
        }

        return EnumLabels.GetValueOrDefault($"{enumType.Name}.{name}", SplitIdentifier(name));
    }

    private static string LocalizeAuditPrefix(string action)
    {
        if (action.StartsWith("GameData.", StringComparison.Ordinal)) return $"游戏数据：{SplitIdentifier(action[9..])}";
        if (action.StartsWith("Map.", StringComparison.Ordinal)) return $"地图：{SplitIdentifier(action[4..])}";
        if (action.StartsWith("Orphan.", StringComparison.Ordinal)) return $"孤立数据：{SplitIdentifier(action[7..])}";
        return action;
    }

    private static string SplitIdentifier(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        StringBuilder result = new();
        for (int index = 0; index < text.Length; index++)
        {
            if (index > 0 && char.IsUpper(text[index]) &&
                (!char.IsUpper(text[index - 1]) || index + 1 < text.Length && char.IsLower(text[index + 1]))) result.Append(' ');
            result.Append(text[index]);
        }
        string[] words = result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(word => IdentifierWords.GetValueOrDefault(word, word)));
    }
}
