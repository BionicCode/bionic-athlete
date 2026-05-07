namespace BionicCode.Utilities.Net;

using System;

/// <summary>
/// Provides a file system path equality comparer that performs case-insensitive comparisons using ordinal string
/// comparison rules.
/// </summary>
/// <remarks>Use this comparer when file system path comparisons must ignore case differences, such as on case-insensitive
/// file systems as implemented on Windows or macOS. 
/// <para/>This class is a singleton; use the <see cref="Instance"/> property to access
/// the shared instance.</remarks>
public sealed class CaseInsensitiveFileSystemPathEqualityComparer : FileSystemPathEqualityComparer
{
    public static new CaseInsensitiveFileSystemPathEqualityComparer Instance { get; } = new();

    private CaseInsensitiveFileSystemPathEqualityComparer() : base(StringComparer.OrdinalIgnoreCase) { }
}