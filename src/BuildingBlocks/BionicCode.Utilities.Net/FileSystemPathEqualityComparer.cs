namespace BionicCode.Utilities.Net;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

/// <summary>
/// A base class for equality comparers that compare file system paths represented as strings. This class provides a common implementation for normalizing file system paths and comparing them using a specified string comparer. The actual comparison logic is delegated to the underlying string comparer, which can be either case-sensitive or case-insensitive depending on the operating system's file system semantics.
/// </summary>
/// <remarks>On Linux, file system paths are case-sensitive, so the default comparer is case-sensitive. On Windows and other platforms with case-insensitive file systems, the default comparer is case-insensitive. This class also provides methods for comparing <see cref="FileSystemInfo"/> objects by their full paths.
/// <para/>To obtain an instance of an actual file system comparer, use the <see cref="Instance"/> property.</remarks>
public abstract class FileSystemPathEqualityComparer : StringComparer,
    IEqualityComparer<FileSystemInfo>,
    IEqualityComparer<string>,
    IEqualityComparer<FileDescriptor>,
    IEqualityComparer<PathDescriptor>,
    IEqualityComparer<DirectoryDescriptor>
{
    protected StringComparer Comparer { get; }
    public static FileSystemPathEqualityComparer Instance { get; } = OperatingSystem.IsLinux()
        ? CaseSensitiveFileSystemPathEqualityComparer.Instance
        : CaseInsensitiveFileSystemPathEqualityComparer.Instance;

    protected FileSystemPathEqualityComparer(StringComparer comparer) => Comparer = comparer;

    public override bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        string xNormalized = FileHelpers.NormalizeFileSystemPath(x);
        string yNormalized = FileHelpers.NormalizeFileSystemPath(y);

        return Comparer.Equals(xNormalized, yNormalized);
    }

    public virtual bool Equals(FileSystemInfo? x, FileSystemInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        string xNormalized = FileHelpers.NormalizeFileSystemPath(x.FullName);
        string yNormalized = FileHelpers.NormalizeFileSystemPath(y.FullName);

        return Comparer.Equals(xNormalized, yNormalized);
    }

    public virtual bool Equals(FileDescriptor x, FileDescriptor y)
    {
        string? xNormalized = x.FullPath is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(x.FullPath);
        string? yNormalized = y.FullPath is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(y.FullPath);

        return Comparer.Equals(xNormalized, yNormalized);
    }

    public virtual bool Equals(DirectoryDescriptor x, DirectoryDescriptor y)
    {
        string? xNormalizedFullPath = x.FullPath is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(x.FullPath);
        string? yNormalizedFullPath = y.FullPath is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(y.FullPath);

        return Comparer.Equals(xNormalizedFullPath, yNormalizedFullPath);
    }

    public virtual bool Equals(PathDescriptor x, PathDescriptor y)
    {
        string? xNormalizedFullPath = x.PathString is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(x.PathString);
        string? yNormalizedFullPath = y.PathString is null
            ? null
            : FileHelpers.NormalizeFileSystemPath(y.PathString);

        if (!Comparer.Equals(xNormalizedFullPath, yNormalizedFullPath))
        {
            return false;
        }

        if (!x.Segments.SequenceEqual(y.Segments))
        {
            return false;
        }

        return true;
    }

    public virtual bool Equals(IEqualityComparer<string>? other) => ReferenceEquals(this, other);

    internal static bool Equals(IEqualityComparer<string>? x, IEqualityComparer<string>? y) => ReferenceEquals(x, y);

    public override bool Equals(object? obj) => obj is IEqualityComparer<string> other && Equals(other);

    public override int GetHashCode(string? obj) => obj is not null
        ? Comparer.GetHashCode(FileHelpers.NormalizeFileSystemPath(obj))
        : 0;

    public int GetHashCode([DisallowNull] FileSystemInfo obj) => obj is FileSystemInfo fileSystemInfo
        ? Comparer.GetHashCode(FileHelpers.NormalizeFileSystemPath(fileSystemInfo.FullName))
        : 0;

    public int GetHashCode([DisallowNull] FileDescriptor fileDescriptor) => string.IsNullOrWhiteSpace(fileDescriptor.FullPath)
        ? 0
        : Comparer.GetHashCode(FileHelpers.NormalizeFileSystemPath(fileDescriptor.FullPath));

    public int GetHashCode([DisallowNull] DirectoryDescriptor directoryDescriptor) => string.IsNullOrWhiteSpace(directoryDescriptor.FullPath)
        ? 0
        : Comparer.GetHashCode(FileHelpers.NormalizeFileSystemPath(directoryDescriptor.FullPath));

    public int GetHashCode([DisallowNull] PathDescriptor pathDescriptor) => string.IsNullOrWhiteSpace(pathDescriptor.PathString)
        ? 0
        : HashCode.Combine(
            Comparer.GetHashCode(FileHelpers.NormalizeFileSystemPath(pathDescriptor.PathString)),
            pathDescriptor.Segments
                .Select(static segment => segment.GetHashCode())
                .Aggregate(0, static (a, b) => HashCode.Combine(a, b)));

    public override int GetHashCode() => Comparer.GetHashCode();

    public override int Compare(string? x, string? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return Comparer.Compare(FileHelpers.NormalizeFileSystemPath(x), FileHelpers.NormalizeFileSystemPath(y));
    }
}
