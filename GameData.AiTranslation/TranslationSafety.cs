using System.Reflection;
using System.Text.RegularExpressions;
using MirDB;

namespace GameData.AiTranslation;

internal static partial class TranslationSafety
{
    public static void ValidateText(TranslationEntry entry, string translation)
    {
        if (string.IsNullOrWhiteSpace(translation))
            throw new InvalidDataException($"{entry.Id}: 译文不能为空。留空 translation 表示跳过该条目。");

        AssertSameSequence(entry, "任务占位符", UpperPlaceholderRegex(), entry.Source, translation, match => match.Value);
        AssertSameSequence(entry, "NPC 变量编号", ValueMarkerRegex(), entry.Source, translation, match => match.Groups["Key"].Value);
        AssertSameSequence(entry, "颜色代码", ColourMarkerRegex(), entry.Source, translation, match => match.Groups["Key"].Value);
        AssertSameSequence(entry, "按钮目标", ButtonMarkerRegex(), entry.Source, translation, match => match.Groups["Key"].Value);

        if (entry.Type == typeof(Library.SystemModels.NPCInfo).FullName && entry.Property == nameof(Library.SystemModels.NPCInfo.NPCName))
        {
            int sourceUnderscores = entry.Source.Count(character => character == '_');
            int translatedUnderscores = translation.Count(character => character == '_');
            if (sourceUnderscores != translatedUnderscores)
                throw new InvalidDataException($"{entry.Id}: NPC 名称的下划线数量必须保持不变。");
        }

        if (entry.Type == typeof(Library.SystemModels.StoreInfo).FullName && entry.Property == nameof(Library.SystemModels.StoreInfo.Filter))
        {
            int sourceParts = entry.Source.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            int translatedParts = translation.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            if (sourceParts != translatedParts)
                throw new InvalidDataException($"{entry.Id}: 商城分类的逗号分隔项数量必须保持不变。");
        }
    }

    public static HashSet<string> CaptureIdentityCollisionGroups(IEnumerable<DBObject> objects)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (IGrouping<Type, DBObject> typeGroup in objects.GroupBy(item => item.GetType()))
        {
            PropertyInfo[] identities = GetIdentityProperties(typeGroup.Key);
            if (identities.Length == 0) continue;

            foreach (var duplicate in typeGroup
                .Select(item => new { Item = item, Signature = BuildIdentity(item, null, item.GetType(), new HashSet<DBObject>()) })
                .Where(item => item.Signature.Count > 0)
                .GroupBy(item => string.Join('\u001f', item.Signature), StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Select(item => item.Item.Index).Order().ToArray()))
            {
                result.Add(BuildCollisionKey(typeGroup.Key, duplicate));
            }
        }

        return result;
    }

    public static void ValidateIdentityUniqueness(IEnumerable<DBObject> objects, IReadOnlySet<string> baselineCollisions)
    {
        foreach (IGrouping<Type, DBObject> typeGroup in objects.GroupBy(item => item.GetType()))
        {
            PropertyInfo[] identities = GetIdentityProperties(typeGroup.Key);
            if (identities.Length == 0) continue;

            var duplicates = typeGroup
                .Select(item => new { Item = item, Signature = BuildIdentity(item, null, item.GetType(), new HashSet<DBObject>()) })
                .Where(item => item.Signature.Count > 0)
                .GroupBy(item => string.Join('\u001f', item.Signature), StringComparer.Ordinal)
                .Where(group => group.Count() > 1 &&
                                !baselineCollisions.Contains(BuildCollisionKey(typeGroup.Key, group.Select(item => item.Item.Index))))
                .Take(10)
                .ToArray();

            if (duplicates.Length == 0) continue;

            string details = string.Join("; ", duplicates.Select(group =>
                $"[{string.Join(",", group.Select(item => item.Item.Index))}] {string.Join(" / ", group.First().Signature)}"));
            throw new InvalidDataException($"{typeGroup.Key.Name} 汉化后产生重复 Identity: {details}");
        }
    }

    private static string BuildCollisionKey(Type type, IEnumerable<int> indices) =>
        $"{type.FullName}:{string.Join(',', indices.Order())}";

    private static List<string> BuildIdentity(DBObject item, PropertyInfo? parentProperty, Type currentType, HashSet<DBObject> visited)
    {
        if (!visited.Add(item)) return [];

        List<string> result = [];
        foreach (PropertyInfo property in GetIdentityProperties(item.GetType()))
        {
            if (property.PropertyType == currentType) continue;

            if (typeof(DBObject).IsAssignableFrom(property.PropertyType))
            {
                if (IsInverseAssociation(property, parentProperty)) continue;
                if (property.GetValue(item) is DBObject child)
                    result.AddRange(BuildIdentity(child, property, property.PropertyType, visited));
            }
            else
            {
                result.Add(property.GetValue(item)?.ToString() ?? string.Empty);
            }
        }

        visited.Remove(item);
        return result;
    }

    private static bool IsInverseAssociation(PropertyInfo childProperty, PropertyInfo? parentProperty)
    {
        if (parentProperty is null || !parentProperty.PropertyType.IsGenericType ||
            parentProperty.PropertyType.GetGenericTypeDefinition() != typeof(DBBindingList<>))
            return false;

        AssociationAttribute? child = childProperty.GetCustomAttribute<AssociationAttribute>();
        AssociationAttribute? parent = parentProperty.GetCustomAttribute<AssociationAttribute>();
        Type bindingType = parentProperty.PropertyType.GetGenericArguments()[0];
        return child?.Identity == parent?.Identity &&
               parentProperty.DeclaringType == childProperty.PropertyType &&
               bindingType == childProperty.DeclaringType;
    }

    private static PropertyInfo[] GetIdentityProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(IsIdentityAttribute), false))
            .ToArray();

    private static void AssertSameSequence(
        TranslationEntry entry,
        string label,
        Regex regex,
        string source,
        string translation,
        Func<Match, string> selector)
    {
        string[] sourceValues = regex.Matches(source).Select(selector).ToArray();
        string[] translatedValues = regex.Matches(translation).Select(selector).ToArray();
        if (!sourceValues.SequenceEqual(translatedValues, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException($"{entry.Id}: {label}必须保持原有数量、内容和顺序。");
    }

    [GeneratedRegex(@"\[[A-Z][A-Z0-9_]*\]", RegexOptions.Compiled)]
    private static partial Regex UpperPlaceholderRegex();

    [GeneratedRegex(@"\<(?<Key>.*?):.+?\>", RegexOptions.Compiled)]
    private static partial Regex ValueMarkerRegex();

    [GeneratedRegex(@"\{.*?:(?<Key>.+?)\}", RegexOptions.Compiled)]
    private static partial Regex ColourMarkerRegex();

    [GeneratedRegex(@"\[.*?:(?<Key>.+?)\]", RegexOptions.Compiled)]
    private static partial Regex ButtonMarkerRegex();
}
