using System.Collections;
using MirDB;

namespace Server.Web.Services;

public sealed class GameDataReferenceComparer : IComparer
{
    public static GameDataReferenceComparer Instance { get; } = new();

    private GameDataReferenceComparer()
    {
    }

    public int Compare(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        if (left is DBObject leftObject && right is DBObject rightObject)
        {
            int displayOrder = StringComparer.CurrentCulture.Compare(leftObject.ToString(), rightObject.ToString());
            return displayOrder != 0 ? displayOrder : leftObject.Index.CompareTo(rightObject.Index);
        }

        return StringComparer.CurrentCulture.Compare(UiText.Value(left), UiText.Value(right));
    }
}
