using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Library;
using Library.SystemModels;
using MirDB;
using Server.DBModels;

namespace GameData.AiTranslation;

internal sealed class TranslationService
{
    private const int SchemaVersion = 1;
    private const string TranslationFileName = "translations.json";
    private const string InstructionsFileName = "AI_INSTRUCTIONS.md";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly CommandArguments _options;
    private readonly Assembly[] _assemblies = [typeof(ItemInfo).Assembly, typeof(AccountInfo).Assembly];

    public TranslationService(CommandArguments options)
    {
        _options = options;
    }

    public ExportResult Export()
    {
        ConfigureEncryption();
        Session session = OpenSession();
        List<DBObject> objects = GetSystemObjects(session).ToList();
        List<TranslationEntry> entries = [];

        foreach (DBObject item in objects.OrderBy(item => item.GetType().FullName, StringComparer.Ordinal).ThenBy(item => item.Index))
        {
            foreach (TranslationFieldRule rule in TranslationCatalog.Rules.Where(rule => rule.Matches(item)))
            {
                PropertyInfo property = item.GetType().GetProperty(rule.Property, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"找不到字段 {item.GetType().Name}.{rule.Property}。");
                if (property.PropertyType != typeof(string))
                    throw new InvalidOperationException($"字段 {item.GetType().Name}.{rule.Property} 不是字符串。");

                string source = (string?)property.GetValue(item) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(source)) continue;

                string id = BuildId(item.GetType(), item.Index, property.Name);
                bool isIdentity = property.IsDefined(typeof(IsIdentityAttribute), false);
                string[] rules = isIdentity
                    ? [.. rule.Rules, "该字段参与 JSON Identity；译文不得为空，并且汉化后必须保持 Identity 唯一。"]
                    : rule.Rules;

                TranslationEntry entry = new()
                {
                    Id = id,
                    Type = item.GetType().FullName!,
                    Index = item.Index,
                    Property = property.Name,
                    Source = source,
                    Translation = string.Empty,
                    Context = BuildContext(item, rule.Description),
                    Rules = rules,
                    IsIdentity = isIdentity
                };
                entry.ProtectionHash = HashEntry(entry);
                entries.Add(entry);
            }
        }

        TranslationDocument document = new()
        {
            SchemaVersion = SchemaVersion,
            TargetLanguage = _options.Language,
            GeneratedAtUtc = DateTime.UtcNow,
            SourceDatabaseSha256 = HashFile(session.SystemPath),
            SourceDatabaseVersion = session.SystemDatabaseVersion,
            EntryCount = entries.Count,
            CatalogProtectionHash = HashCatalog(entries),
            AiInstructions = "只修改 entries 数组中每一项的 translation 字段。不得修改、增删或重新生成其他字段；不需要汉化的条目保持空字符串。",
            Entries = entries
        };

        Directory.CreateDirectory(_options.OutputDirectory);
        string translationFile = Path.Combine(_options.OutputDirectory, TranslationFileName);
        string instructionsFile = Path.Combine(_options.OutputDirectory, InstructionsFileName);
        File.WriteAllText(translationFile, JsonSerializer.Serialize(document, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(instructionsFile, BuildAiInstructions(document), new UTF8Encoding(false));

        return new ExportResult(entries.Count, translationFile, instructionsFile);
    }

    public ImportResult Import(bool save)
    {
        string inputFile = _options.InputFile ?? throw new CommandLineException("必须指定 --input。");
        if (!File.Exists(inputFile)) throw new FileNotFoundException("找不到汉化文件。", inputFile);

        TranslationDocument document = JsonSerializer.Deserialize<TranslationDocument>(File.ReadAllText(inputFile), JsonOptions)
            ?? throw new InvalidDataException("汉化文件不是有效的 JSON 文档。");
        ValidateDocument(document);

        string systemPath = Path.Combine(_options.DatabaseDirectory, "System.db");
        string currentDatabaseHash = HashFile(systemPath);
        if (!_options.AllowDatabaseChanges && !string.Equals(currentDatabaseHash, document.SourceDatabaseSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("System.db 自导出后已发生变化。请重新导出，或在确认只有无关修改时使用 --allow-database-changes。");

        ConfigureEncryption();
        Session session = OpenSession();
        string loadedDatabaseHash = HashFile(session.SystemPath);
        List<DBObject> objects = GetSystemObjects(session).ToList();
        HashSet<string> baselineIdentityCollisions = TranslationSafety.CaptureIdentityCollisionGroups(objects);
        Dictionary<(Type Type, int Index), DBObject> objectLookup = objects.ToDictionary(item => (item.GetType(), item.Index));
        Dictionary<string, Type> typeLookup = objects.Select(item => item.GetType()).Distinct()
            .ToDictionary(type => type.FullName!, StringComparer.Ordinal);

        List<(TranslationEntry Entry, DBObject Item, PropertyInfo Property, string Translation)> changes = [];
        foreach (TranslationEntry entry in document.Entries)
        {
            ValidateProtectedEntry(entry);
            if (!typeLookup.TryGetValue(entry.Type, out Type? type))
                throw new InvalidDataException($"{entry.Id}: 数据库中不存在对象类型 {entry.Type}。");
            if (!objectLookup.TryGetValue((type, entry.Index), out DBObject? item))
                throw new InvalidDataException($"{entry.Id}: 数据库中不存在 Index={entry.Index} 的对象。");

            TranslationFieldRule? rule = TranslationCatalog.Find(item, entry.Property);
            if (rule is null)
                throw new InvalidDataException($"{entry.Id}: 该字段不在当前版本的可汉化白名单中。");

            PropertyInfo property = type.GetProperty(entry.Property, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidDataException($"{entry.Id}: 字段不存在。");
            string current = (string?)property.GetValue(item) ?? string.Empty;
            if (!string.Equals(current, entry.Source, StringComparison.Ordinal))
                throw new InvalidDataException($"{entry.Id}: 数据库原文已经变化，预期“{entry.Source}”，实际“{current}”。");

            if (string.IsNullOrEmpty(entry.Translation) || string.Equals(entry.Source, entry.Translation, StringComparison.Ordinal))
                continue;

            TranslationSafety.ValidateText(entry, entry.Translation);
            changes.Add((entry, item, property, entry.Translation));
        }

        ValidateConsistentTranslations(changes);
        ValidateCurrencyItemDependencies(objects, changes);

        Dictionary<string, string> currencyRenames = changes
            .Where(change => change.Item is CurrencyInfo && change.Property.Name == nameof(CurrencyInfo.Name))
            .ToDictionary(change => change.Entry.Source, change => change.Translation, StringComparer.OrdinalIgnoreCase);

        foreach (var change in changes)
            change.Property.SetValue(change.Item, change.Translation);

        SynchronizeCurrencyReferences(objects, currencyRenames);
        TranslationSafety.ValidateIdentityUniqueness(objects, baselineIdentityCollisions);

        if (!save || changes.Count == 0)
            return new ImportResult(changes.Count, null);

        if (!string.Equals(loadedDatabaseHash, HashFile(session.SystemPath), StringComparison.OrdinalIgnoreCase))
            throw new IOException("校验期间 System.db 被其他进程修改。请停止 Server/Server.Web 后重新导出并导入。");

        string backupDirectory = CreateBackup(inputFile);
        session.Save(true);

        Session verificationSession = OpenSession();
        VerifySavedTranslations(verificationSession, changes);
        return new ImportResult(changes.Count, backupDirectory);
    }

    private Session OpenSession()
    {
        string backupRoot = Path.Combine(_options.DatabaseDirectory, "Backup");
        Session session = new(SessionMode.System, _options.DatabaseDirectory, backupRoot)
        {
            BackUp = true,
            BackUpDelay = 0
        };
        session.Initialize(_assemblies);
        return session;
    }

    private void ConfigureEncryption()
    {
        Encryption.SetKey(_options.EncryptionKey);
    }

    private IEnumerable<DBObject> GetSystemObjects(Session session)
    {
        IEnumerable<Type> types = _assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(DBObject)) && !type.IsDefined(typeof(UserObjectAttribute), false))
            .Distinct();

        foreach (Type type in types)
        {
            object collection = session.GetCollection(type);
            FieldInfo bindingField = collection.GetType().GetField("Binding", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"无法读取 {type.Name} 集合。");
            if (bindingField.GetValue(collection) is not IEnumerable binding) continue;
            foreach (DBObject item in binding.Cast<DBObject>().Where(item => !item.IsTemporary))
                yield return item;
        }
    }

    private static void ValidateDocument(TranslationDocument document)
    {
        if (document.SchemaVersion != SchemaVersion)
            throw new InvalidDataException($"不支持的汉化文件版本 {document.SchemaVersion}。");
        if (document.Entries is null) throw new InvalidDataException("汉化文件缺少 entries。");
        if (document.EntryCount != document.Entries.Count)
            throw new InvalidDataException($"汉化条目数量已变化，预期 {document.EntryCount}，实际 {document.Entries.Count}。");
        if (document.Entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() != document.Entries.Count)
            throw new InvalidDataException("汉化文件包含重复 id。");
        if (!string.Equals(document.CatalogProtectionHash, HashCatalog(document.Entries), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("汉化文件的条目集合或受保护字段已被修改。AI 只能修改 translation 字段。");
    }

    private static void ValidateProtectedEntry(TranslationEntry entry)
    {
        string expectedId = $"{entry.Type}:{entry.Index}:{entry.Property}";
        if (!string.Equals(entry.Id, expectedId, StringComparison.Ordinal))
            throw new InvalidDataException($"条目 id 与类型、Index 或属性不一致: {entry.Id}");
        if (!string.Equals(entry.ProtectionHash, HashEntry(entry), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"{entry.Id}: 受保护字段已被修改。AI 只能修改 translation 字段。");
    }

    private static void ValidateConsistentTranslations(
        IEnumerable<(TranslationEntry Entry, DBObject Item, PropertyInfo Property, string Translation)> changes)
    {
        var changeList = changes.ToList();
        var conflicts = changeList
            .GroupBy(change => (change.Entry.Type, change.Entry.Property, change.Entry.Source))
            .Where(group => group.Select(change => change.Translation).Distinct(StringComparer.Ordinal).Count() > 1)
            .Take(10)
            .ToArray();
        if (conflicts.Length > 0)
        {
            string details = string.Join("; ", conflicts.Select(group => $"{group.Key.Type}.{group.Key.Property} “{group.Key.Source}”"));
            throw new InvalidDataException($"相同原文存在不一致译文: {details}");
        }

        ValidateStoreCategories();

        void ValidateStoreCategories()
        {
            Dictionary<string, string> translations = new(StringComparer.OrdinalIgnoreCase);
            foreach (var change in changeList.Where(change => change.Item is StoreInfo && change.Property.Name == nameof(StoreInfo.Filter)))
            {
                string[] sources = change.Entry.Source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                string[] translated = change.Translation.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int index = 0; index < sources.Length; index++)
                {
                    if (translations.TryGetValue(sources[index], out string? existing) &&
                        !string.Equals(existing, translated[index], StringComparison.Ordinal))
                        throw new InvalidDataException($"商城分类“{sources[index]}”存在不一致译文：“{existing}”和“{translated[index]}”。");
                    translations[sources[index]] = translated[index];
                }
            }
        }
    }

    private static void ValidateCurrencyItemDependencies(
        IEnumerable<DBObject> objects,
        IEnumerable<(TranslationEntry Entry, DBObject Item, PropertyInfo Property, string Translation)> changes)
    {
        Dictionary<string, CurrencyType> protectedNames = new(StringComparer.Ordinal)
        {
            ["Gold"] = CurrencyType.Gold,
            ["Fame Point"] = CurrencyType.FP,
            ["Contribution Point"] = CurrencyType.CP
        };
        Dictionary<CurrencyType, CurrencyInfo> currencies = objects.OfType<CurrencyInfo>().ToDictionary(item => item.Type);

        foreach (var change in changes.Where(change => change.Item is ItemInfo && change.Property.Name == nameof(ItemInfo.ItemName)))
        {
            if (!protectedNames.TryGetValue(change.Entry.Source, out CurrencyType currencyType)) continue;
            if (!currencies.TryGetValue(currencyType, out CurrencyInfo? currency) || !ReferenceEquals(currency.DropItem, change.Item))
                throw new InvalidDataException($"{change.Entry.Id}: 默认货币物品没有通过 {currencyType}.DropItem 关联，不能安全汉化名称。");
        }
    }

    private static void SynchronizeCurrencyReferences(IEnumerable<DBObject> objects, IReadOnlyDictionary<string, string> renames)
    {
        if (renames.Count == 0) return;

        foreach (DBObject item in objects)
        {
            switch (item)
            {
                case NPCAction action when action.ActionType is NPCActionType.GiveCurrency or NPCActionType.TakeCurrency &&
                                           action.StringParameter1 is not null && renames.TryGetValue(action.StringParameter1, out string? actionName):
                    action.StringParameter1 = actionName;
                    break;
                case NPCCheck check when check.CheckType == NPCCheckType.Currency &&
                                         check.StringParameter1 is not null && renames.TryGetValue(check.StringParameter1, out string? checkName):
                    check.StringParameter1 = checkName;
                    break;
            }
        }
    }

    private void VerifySavedTranslations(
        Session session,
        IEnumerable<(TranslationEntry Entry, DBObject Item, PropertyInfo Property, string Translation)> changes)
    {
        Dictionary<(string Type, int Index), DBObject> lookup = GetSystemObjects(session)
            .ToDictionary(item => (item.GetType().FullName!, item.Index));
        foreach (var change in changes)
        {
            DBObject saved = lookup[(change.Entry.Type, change.Entry.Index)];
            string? value = (string?)saved.GetType().GetProperty(change.Entry.Property)?.GetValue(saved);
            if (!string.Equals(value, change.Translation, StringComparison.Ordinal))
                throw new IOException($"保存后校验失败: {change.Entry.Id}");
        }
    }

    private string CreateBackup(string inputFile)
    {
        string backupDirectory = Path.Combine(
            _options.DatabaseDirectory,
            "AITranslationBackups",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backupDirectory);

        foreach (string fileName in new[] { "System.db", "Users.db" })
        {
            string source = Path.Combine(_options.DatabaseDirectory, fileName);
            if (File.Exists(source)) File.Copy(source, Path.Combine(backupDirectory, fileName), overwrite: false);
        }

        File.Copy(inputFile, Path.Combine(backupDirectory, Path.GetFileName(inputFile)), overwrite: false);
        return backupDirectory;
    }

    private static string BuildContext(DBObject item, string description)
    {
        string detail = item switch
        {
            ItemInfo value => $"物品类型={value.ItemType}",
            NPCInfo value => $"地图区域={value.Region?.ServerDescription ?? "未设置"}",
            NPCPage value => $"页面={value.Description}",
            NPCAction value => $"NPC页面={value.Page?.Description ?? "未设置"}; 动作={value.ActionType}",
            QuestInfo value => $"起始NPC={value.StartNPC?.NPCName ?? "未设置"}; 完成NPC={value.FinishNPC?.NPCName ?? "未设置"}",
            QuestTask value => $"任务={value.Quest?.QuestName ?? "未设置"}; 类型={value.Task}",
            MapRegion value => $"地图={value.Map?.Description ?? "未设置"}",
            StoreInfo value => $"商城物品={value.Item?.ItemName ?? "未设置"}",
            CompanionSpeech value => $"伙伴={value.Companion?.Description ?? "未设置"}; 动作={value.Action}",
            BaseEventAction value => $"事件动作={value.Type}",
            _ => item.ToString() ?? item.GetType().Name
        };
        return $"{description}; {detail}";
    }

    private static string BuildAiInstructions(TranslationDocument document) => $$"""
        # Zircon 游戏数据 AI 汉化说明

        目标语言：`{{document.TargetLanguage}}`

        请读取同目录下的 `{{TranslationFileName}}`，逐条汉化 `entries` 数组。

        ## 允许修改

        只能修改每个条目的 `translation` 字段。无法确定或无需汉化时保持空字符串。

        ## 禁止修改

        不得修改或删除以下字段，也不得新增、删除、合并、拆分或重新排序条目：

        - `id`
        - `type`
        - `index`
        - `property`
        - `source`
        - `context`
        - `rules`
        - `isIdentity`
        - `protectionHash`
        - 文档顶层元数据

        ## 汉化要求

        - 输出与官方《传奇3》一致的、准确、风格统一的简体中文游戏文本。
        - 严格遵守每条记录的 `rules`。
        - 完整保留 `[PLAYERNAME]` 等占位符。
        - NPC 文本中 `<变量:默认文本>` 的变量、`{文本:颜色}` 的颜色、`[文本:按钮ID]` 的按钮 ID 不得改变；可汉化其中的可见文本。
        - 保留原有换行、数字、标点用途和格式结构。
        - 相同类型、字段和原文必须给出完全相同的译文。
        - 不要汉化未出现在文件中的数据库字段；它们是文件名、枚举、脚本键或运行时关联键。

        完成后保存为有效 UTF-8 JSON。导入工具会校验所有受保护字段、占位符、Identity 唯一性和数据库版本。
        """;

    private static string BuildId(Type type, int index, string property) => $"{type.FullName}:{index}:{property}";

    private static string HashEntry(TranslationEntry entry)
    {
        StringBuilder builder = new();
        Add(entry.Id);
        Add(entry.Type);
        Add(entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(entry.Property);
        Add(entry.Source);
        Add(entry.Context);
        Add(entry.IsIdentity ? "1" : "0");
        foreach (string rule in entry.Rules) Add(rule);
        return HashText(builder.ToString());

        void Add(string value)
        {
            builder.Append(value.Length).Append(':').Append(value);
        }
    }

    private static string HashCatalog(IEnumerable<TranslationEntry> entries) =>
        HashText(string.Join('\n', entries.OrderBy(entry => entry.Id, StringComparer.Ordinal).Select(entry => entry.ProtectionHash)));

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string HashText(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
