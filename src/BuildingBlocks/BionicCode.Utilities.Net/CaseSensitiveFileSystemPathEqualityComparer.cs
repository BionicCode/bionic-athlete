namespace BionicCode.Utilities.Net;

using System;

/// <summary>
/// Provides a file system path equality comparer that performs case-sensitive comparisons using ordinal string
/// comparison rules.
/// </summary>
/// <remarks>Use this comparer when file system path comparisons must distinguish between uppercase and lowercase
/// characters, such as on case-sensitive file systems as implemented on Linux. 
/// <para/>This class is a singleton; use the <see cref="Instance"/> property to access
/// the shared instance.</remarks>
public sealed class CaseSensitiveFileSystemPathEqualityComparer : FileSystemPathEqualityComparer
{
    public static new CaseSensitiveFileSystemPathEqualityComparer Instance { get; } = new();

    private CaseSensitiveFileSystemPathEqualityComparer() : base(StringComparer.Ordinal) { }
}
