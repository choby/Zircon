using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MirDB;
using Server.Envir;
using Server.Web.Models;

namespace Server.Web.Services;

public sealed class RuntimeDataService(AdminAuditService audit)
{
    public IReadOnlyList<RuntimeCollectionDescriptor> Collections { get; } =
    [
        View("accounts", "账号", nameof(SEnvir.AccountInfoList), "AccountView",
            ("EMailAddress", "邮箱"), ("RealName", "真实姓名"), ("BirthDate", "出生日期"), ("Referral", "推荐人"),
            ("CreationIP", "创建 IP"), ("CreationDate", "创建日期"), ("LastIP", "最后 IP"), ("LastLogin", "最后登录"),
            ("Activated", "已激活"), ("Banned", "封禁"), ("ExpiryDate", "到期时间"), ("BanReason", "封禁原因"),
            ("Admin", "管理员"), ("Observer", "观察者")),
        View("characters", "角色", nameof(SEnvir.CharacterInfoList), "CharacterView",
            ("Account", "账号"), ("CharacterName", "角色名"), ("Class", "职业"), ("Gender", "性别"),
            ("Level", "等级"), ("HairType", "发型"), ("Deleted", "已删除"), ("Experience", "经验"), ("Caption", "称号")),
        View("drops", "用户掉落", nameof(SEnvir.UserDropList), "UserDropView",
            ("Account", "账号"), ("Item", "物品"), ("Progress", "进度"), ("DropCount", "掉落次数")),
        View("payments", "支付", nameof(SEnvir.GameGoldPaymentList), "GameGoldPaymentView",
            ("CharacterName", "角色名"), ("Name", "名称"), ("PaymentDate", "支付日期"), ("Account", "账号"),
            ("TransactionID", "交易号"), ("TransactionType", "交易类型"), ("Status", "状态"), ("GameGoldAmount", "元宝数量"),
            ("Payer_EMail", "付款邮箱"), ("Payer_ID", "付款人 ID"), ("Price", "价格"), ("Currency", "币种"), ("Fee", "手续费"), ("Error", "错误")),
        View("sales", "商店销售", nameof(SEnvir.GameStoreSaleList), "GameStoreSaleView",
            ("Account", "账号"), ("Item", "物品"), ("Date", "日期"), ("Price", "价格"), ("Count", "数量"), ("HuntGold", "赏金")),
        View("conquest", "攻城战统计", nameof(SEnvir.UserConquestStatsList), "UserConquestStatsView",
            ("WarStartDate", "战争开始"), ("CastleName", "城堡"), ("CharacterName", "角色"), ("GuildName", "行会"),
            ("Level", "等级"), ("Class", "职业"), ("BossDamageTaken", "Boss 承伤"), ("BossDamageDealt", "Boss 伤害"),
            ("BossDeathCount", "Boss 死亡"), ("BossKillCount", "Boss 击杀"), ("PvPDamageTaken", "PvP 承伤"),
            ("PvPDamageDealt", "PvP 伤害"), ("PvPDeathCount", "PvP 死亡"), ("PvPKillCount", "PvP 击杀")),
        View("mail", "用户邮件", nameof(SEnvir.MailInfoList), "UserMailView",
            ("Sender", "发件人"), ("Recipient", "收件人"), ("Subject", "主题"), ("Message", "内容"),
            ("Date", "日期"), ("Gold", "金币"), ("HuntGold", "赏金"), ("Read", "已读")),
        View("npcs", "NPC 数据", nameof(SEnvir.GameNPCList), "NPCListView",
            ("Category", "分类"), ("TypeValue", "类型值"), ("IntValue1", "整数值 1"))
    ];

    public RuntimeCollectionDescriptor Get(string key) => Collections.Single(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    public Task<IReadOnlyList<RuntimeDataRow>> ReadAsync(
        RuntimeCollectionDescriptor descriptor,
        int maximum = 1_000,
        CancellationToken cancellationToken = default) =>
        SEnvir.InvokeOnGameThreadAsync<IReadOnlyList<RuntimeDataRow>>(() => ReadOnGameThread(descriptor, maximum), cancellationToken);

    public Task UpdateAsync(
        RuntimeCollectionDescriptor descriptor,
        int index,
        string expectedETag,
        IReadOnlyDictionary<string, string?> values,
        string user,
        CancellationToken cancellationToken = default) =>
        SEnvir.InvokeOnGameThreadAsync(() =>
        {
            DBObject target = GetItems(descriptor).Single(item => item.Index == index);
            if (!string.Equals(ComputeETag(target), expectedETag, StringComparison.Ordinal))
                throw new InvalidOperationException("运行时数据已发生变化，请刷新后重试。");

            foreach (PropertyInfo property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!values.TryGetValue(property.Name, out string? text) || property.SetMethod?.IsPublic != true ||
                    !IsSimple(property.PropertyType) || IsSensitive(property.Name)) continue;
                property.SetValue(target, ConvertValue(text, property.PropertyType));
            }

            SEnvir.Session.Save(true);
            audit.Record(user, "RuntimeData.Update", $"{target.GetType().Name} #{target.Index}");
            return true;
        }, cancellationToken);

    private static IReadOnlyList<RuntimeDataRow> ReadOnGameThread(RuntimeCollectionDescriptor descriptor, int maximum)
    {
        List<RuntimeDataRow> rows = [];
        foreach (DBObject item in GetItems(descriptor).Take(maximum))
        {
            Dictionary<string, object?> values = [];
            foreach (RuntimeColumnDefinition column in descriptor.Columns)
            {
                PropertyInfo? property = item.GetType().GetProperty(column.Field, BindingFlags.Public | BindingFlags.Instance);
                if (property?.GetIndexParameters().Length == 0 && !IsSensitive(property.Name))
                    values[property.Name] = property.GetValue(item);
            }
            rows.Add(new RuntimeDataRow { Index = item.Index, ETag = ComputeETag(item), Values = values });
        }
        return rows;
    }

    private static IEnumerable<DBObject> GetItems(RuntimeCollectionDescriptor descriptor)
    {
        FieldInfo field = typeof(SEnvir).GetField(descriptor.FieldName, BindingFlags.Public | BindingFlags.Static)!;
        object? collection = field.GetValue(null);
        if (collection is null) return [];
        FieldInfo? bindingField = collection.GetType().GetField("Binding", BindingFlags.Public | BindingFlags.Instance);
        return bindingField?.GetValue(collection) is IEnumerable binding ? binding.Cast<DBObject>() : [];
    }

    private static string ComputeETag(DBObject item)
    {
        StringBuilder text = new(item.Index.ToString());
        foreach (PropertyInfo property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(property => property.GetIndexParameters().Length == 0 && IsSimple(property.PropertyType) && !IsSensitive(property.Name)))
            text.Append('\0').Append(property.Name).Append('=').Append(property.GetValue(item));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private static bool IsSensitive(string name) =>
        name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("SecurityAnswer", StringComparison.OrdinalIgnoreCase);

    private static object? ConvertValue(string? text, Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        if (string.IsNullOrWhiteSpace(text) && Nullable.GetUnderlyingType(type) is not null) return null;
        if (actual == typeof(string)) return text ?? string.Empty;
        if (actual == typeof(DateTime)) return DateTime.Parse(text!, System.Globalization.CultureInfo.InvariantCulture);
        if (actual == typeof(TimeSpan)) return TimeSpan.Parse(text!, System.Globalization.CultureInfo.InvariantCulture);
        if (actual.IsEnum) return Enum.Parse(actual, text!, true);
        return Convert.ChangeType(text, actual, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool IsSimple(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(decimal) ||
               actual == typeof(DateTime) || actual == typeof(TimeSpan);
    }

    private static RuntimeCollectionDescriptor View(
        string key, string title, string fieldName, string legacyView,
        params (string Field, string Title)[] columns) =>
        new(key, title, fieldName, legacyView, columns.Select(column => new RuntimeColumnDefinition(column.Field, column.Title)).ToArray());
}
