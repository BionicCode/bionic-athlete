namespace BionicCode.Utilities.Net;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

internal static class SetEqualityComparerHelpers
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
    public static int ComputeHashCode<TItem>(ISet<TItem> obj, IEqualityComparer<TItem> comparer)
    {
        // Use an cumulative and therefore order-independent set hash
        int hashCode = RuntimeHelpers.GetHashCode(comparer);
        foreach (TItem item in obj)
        {
            hashCode ^= item is null
                ? 0
                : comparer.GetHashCode(item);
        }

        return hashCode;
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
    public static bool IsSetEqual<TSet2Comparer>(ISet<string>? set1, ISet<FileSystemInfo>? set2, Func<IEqualityComparer<string>> set1ComparerProvider, Func<IEqualityComparer<TSet2Comparer>> set2ComparerProvider)
    {
        (bool isEqualByReference, bool isGenerallyEqual) = IsSetOutlineEqual(set1, set2, set1ComparerProvider, set2ComparerProvider);
        if (isEqualByReference)
        {
            return true;
        }

        if (!isGenerallyEqual)
        {
            return false;
        }

        IEqualityComparer<string> comparer = set1ComparerProvider();
        foreach (FileSystemInfo item in set2!)
        {
            if (!set1!.Contains(item.FullName, comparer!))
            {
                return false;
            }
        }

        return true;
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
    public static bool IsSetEqual(ISet<FileSystemInfo>? set1, ISet<FileSystemInfo>? set2, Func<IEqualityComparer<FileSystemInfo>> set1ComparerProvider, Func<IEqualityComparer<FileSystemInfo>> set2ComparerProvider)
    {
        (bool isEqualByReference, bool isGenerallyEqual) = IsSetOutlineEqual(set1, set2, set1ComparerProvider, set2ComparerProvider);
        if (isEqualByReference)
        {
            return true;
        }

        if (!isGenerallyEqual)
        {
            return false;
        }

        IEqualityComparer<FileSystemInfo> comparer = set1ComparerProvider();
        foreach (FileSystemInfo item in set1!)
        {
            if (!set2!.Contains(item, comparer!))
            {
                return false;
            }
        }

        return true;
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
    public static bool IsSetEqual(ISet<string>? set1, ISet<string>? set2, Func<IEqualityComparer<string>> set1ComparerProvider, Func<IEqualityComparer<string>> set2ComparerProvider)
    {
        (bool isEqualByReference, bool isGenerallyEqual) = IsSetOutlineEqual(set1, set2, set1ComparerProvider, set2ComparerProvider);
        if (isEqualByReference)
        {
            return true;
        }

        if (!isGenerallyEqual)
        {
            return false;
        }

        IEqualityComparer<string> comparer = set1ComparerProvider();
        foreach (string item in set1!)
        {
            if (!set2!.Contains(item, comparer!))
            {
                return false;
            }
        }

        return true;
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
    public static bool IsSetEqual<TItem>(ISet<TItem>? set1, ISet<TItem>? set2, Func<IEqualityComparer<TItem>> set1ComparerProvider, Func<IEqualityComparer<TItem>> set2ComparerProvider)
    {
        (bool isEqualByReference, bool isGenerallyEqual) = IsSetOutlineEqual(set1, set2, set1ComparerProvider, set2ComparerProvider);
        if (isEqualByReference)
        {
            return true;
        }

        if (!isGenerallyEqual)
        {
            return false;
        }

        IEqualityComparer<TItem> comparer = set1ComparerProvider();
        foreach (TItem item in set1!)
        {
            if (!set2!.Contains(item, comparer!))
            {
                return false;
            }
        }

        return true;
    }

    public static (bool isEqualByReference, bool isGenerallyEqual) IsSetOutlineEqual<TItem1, TItem2, TSet1Comparer, TSet2Comparer>(
        ISet<TItem1>? set1,
        ISet<TItem2>? set2,
        Func<IEqualityComparer<TSet1Comparer>> set1ComparerProvider,
        Func<IEqualityComparer<TSet2Comparer>> set2ComparerProvider)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(set1ComparerProvider);
        ArgumentNullExceptionAdvanced.ThrowIfNull(set2ComparerProvider);

        // If they're the exact same instance, they're equal.
        if (ReferenceEquals(set1, set2))
        {
            return (true, true);
        }

        // They're not both null, so if either is null, they're not equal.
        if (set1 == null || set2 == null)
        {
            return (false, false);
        }

        if (set1.Count != set2.Count)
        {
            return (false, false);
        }

        /* 
         * We can't use ISet<T>.SetEquals here because the sets may have different types.
         * While e.g. HashSet<T>.SetEquals can handle this case, equality comparison becomes unnecessarily expensive 
         * and semantically different since comparers are ignored (for that particular case).
         * So we need to compare the elements manually using the correct well-known comparers.
         */

        // If the comparers are not the same instance, we can't guarantee that they will consider the same elements equal, so we return false.
        IEqualityComparer<TSet1Comparer> set1Comparer = set1ComparerProvider.Invoke();
        IEqualityComparer<TSet2Comparer> set2Comparer = set2ComparerProvider.Invoke();
        if (!ReferenceEquals(set1Comparer, set2Comparer))
        {
            return (false, false);
        }

        return (false, true);
    }
}