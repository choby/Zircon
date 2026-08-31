using System.Collections;
using System.Reflection;
using MirDB;
using Server.Envir;
using Server.Web.Models;

namespace Server.Web.Services;

public sealed class OrphanDiagnosticService(AdminAuditService audit)
{
    private static readonly (string Child, string Parent, string ParentProperty, string ParentLink)[] Ownerships =
    [
        ("CharacterInfo", "AccountInfo", "Account", "Characters"),
        ("UserCurrency", "AccountInfo", "Account", "Currencies"),
        ("AuctionInfo", "AccountInfo", "Account", "Auctions"),
        ("MailInfo", "AccountInfo", "Account", "Mail"),
        ("UserDrop", "AccountInfo", "Account", "UserDrops"),
        ("UserCompanion", "AccountInfo", "Account", "Companions"),
        ("UserCompanionUnlock", "AccountInfo", "Account", "CompanionUnlocks"),
        ("BlockInfo", "AccountInfo", "Account", "BlockingList"),
        ("BlockInfo", "AccountInfo", "BlockedAccount", "BlockedByList"),
        ("UserFortuneInfo", "AccountInfo", "Account", "Fortunes"),
        ("CharacterBeltLink", "CharacterInfo", "Character", "BeltLinks"),
        ("AutoPotionLink", "CharacterInfo", "Character", "AutoPotionLinks"),
        ("UserMagic", "CharacterInfo", "Character", "Magics"),
        ("UserMagic", "UserDiscipline", "Discipline", "Magics"),
        ("UserDiscipline", "CharacterInfo", "Character", "Discipline"),
        ("UserCompanion", "CharacterInfo", "Character", "Companion"),
        ("BuffInfo", "CharacterInfo", "Character", "Buffs"),
        ("BuffInfo", "AccountInfo", "Account", "Buffs"),
        ("RefineInfo", "CharacterInfo", "Character", "Refines"),
        ("UserQuest", "CharacterInfo", "Character", "Quests"),
        ("UserQuest", "AccountInfo", "Account", "Quests"),
        ("FriendInfo", "CharacterInfo", "Character", "Friends"),
        ("FriendInfo", "CharacterInfo", "FriendedCharacter", "FriendedBy"),
        ("UserItemStat", "UserItem", "Item", "AddedStats"),
        ("UserQuestTask", "UserQuest", "Quest", "Tasks"),
        ("UserMilestoneLog", "CharacterInfo", "Character", "MilestoneLogs"),
        ("UserMilestone", "CharacterInfo", "Character", "Milestones"),
        ("GuildMemberInfo", "GuildInfo", "Guild", "Members"),
        ("GuildMemberInfo", "AccountInfo", "Account", "GuildMember"),
        ("UserConquest", "GuildInfo", "Guild", "Conquest")
    ];

    public Task<IReadOnlyList<OrphanDiagnosticRow>> ScanAsync(bool clean, string user, CancellationToken cancellationToken = default) =>
        SEnvir.InvokeOnGameThreadAsync<IReadOnlyList<OrphanDiagnosticRow>>(() => ScanOnGameThread(clean, user), cancellationToken);

    private IReadOnlyList<OrphanDiagnosticRow> ScanOnGameThread(bool clean, string user)
    {
        Dictionary<string, (Type Type, IEnumerable Rows)> collections = GetCollections();
        List<Association> associations = [];
        foreach (var item in Ownerships)
        {
            if (!collections.TryGetValue(item.Child, out var child) || !collections.TryGetValue(item.Parent, out var parent)) continue;
            PropertyInfo? parentProperty = child.Type.GetProperty(item.ParentProperty);
            PropertyInfo? linkProperty = parent.Type.GetProperty(item.ParentLink);
            if (parentProperty is not null && linkProperty is not null)
                associations.Add(new Association(child.Type, parent.Type, parentProperty, linkProperty, child.Rows));
        }

        List<OrphanDiagnosticRow> results = [];
        foreach (IGrouping<Type, Association> group in associations.GroupBy(item => item.ChildType).OrderBy(item => item.Key.Name))
        {
            Association[] links = group.ToArray();
            int total = 0, linked = 0, orphaned = 0, temporary = 0, marked = 0;
            List<int> samples = [];
            foreach (DBObject child in links[0].Rows.Cast<DBObject>())
            {
                total++;
                if (IsDeleted(child)) continue;
                bool hasLink = links.Any(link => IsLinked(child, link));
                if (hasLink) { linked++; continue; }
                if (child.IsTemporary) { temporary++; continue; }
                orphaned++;
                if (samples.Count < 40) samples.Add(child.Index);
                if (clean) { child.IsTemporary = true; marked++; }
            }

            results.Add(new OrphanDiagnosticRow(
                group.Key.Name,
                string.Join(", ", links.Select(link => $"{link.ParentType.Name}.{link.LinkProperty.Name}").Distinct()),
                total, linked, orphaned, temporary, marked, string.Join(", ", samples)));
        }

        if (clean && results.Sum(item => item.MarkedTemporary) > 0) SEnvir.Session.Save(true);
        audit.Record(user, clean ? "Orphan.Clean" : "Orphan.Scan", $"{results.Sum(item => item.TotalRows)} rows; {results.Sum(item => item.CleanableOrphans)} orphans");
        return results;
    }

    private static bool IsLinked(DBObject child, Association association)
    {
        if (association.ParentProperty.GetValue(child) is not DBObject parent || IsDeleted(parent)) return false;
        object? link = association.LinkProperty.GetValue(parent);
        return link is IList list ? list.Contains(child) : ReferenceEquals(link, child);
    }

    private static Dictionary<string, (Type Type, IEnumerable Rows)> GetCollections()
    {
        Dictionary<string, (Type, IEnumerable)> result = new(StringComparer.Ordinal);
        foreach (FieldInfo field in typeof(SEnvir).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(DBCollection<>)) continue;
            object? collection = field.GetValue(null);
            if (collection is null) continue;
            Type type = field.FieldType.GetGenericArguments()[0];
            if (field.FieldType.GetField("Binding")?.GetValue(collection) is IEnumerable rows)
                result[type.Name] = (type, rows);
        }
        return result;
    }

    private static readonly PropertyInfo? IsDeletedProperty = typeof(DBObject).GetProperty("IsDeleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static bool IsDeleted(DBObject item) => IsDeletedProperty?.GetValue(item) is true;

    private sealed record Association(Type ChildType, Type ParentType, PropertyInfo ParentProperty, PropertyInfo LinkProperty, IEnumerable Rows);
}
