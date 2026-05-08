namespace BionicCode.Utilities.Net;

using System.Diagnostics;

/// <summary>
/// Describes a directory that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRelative = {IsRelative}")]
public readonly struct DirectoryDescriptor : IEquatable<DirectoryDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="name">The directory name.</param>
    /// <param name="location">The parent directory path without the name. Can be absolute or relative.</param>
    public DirectoryDescriptor(string name, string location)
    {
        FileSystemPathValidator.ThrowIfInvalidDirectoryName(name);
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(location);

        Name = FileHelpers.NormalizeDirectoryName(name);
        Location = FileHelpers.NormalizeFileSystemPath(location);
        FullPath = Path.Combine(Location, Name);
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="fullPath">The full path of the directory. Can be absolute or relative.</param>
    public DirectoryDescriptor(string fullPath)
    {
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(fullPath);

        string normalizedFullPath = FileHelpers.NormalizeFileSystemPath(fullPath);
        Name = Path.GetFileName(normalizedFullPath);
        Location = Path.GetDirectoryName(normalizedFullPath) ?? string.Empty;
        FullPath = Path.Combine(Location, Name);
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    public DirectoryDescriptor Combine(params DirectoryDescriptor[] additionalLocationSegments)
    {
        ArgumentExceptionAdvanced.ThrowIfAny(additionalLocationSegments, item => item == default);

        IEnumerable<string> pathSegments = additionalLocationSegments
            .Select(segment => segment.FullPath)
            .Concat([Name]);
        string combinedPath = pathSegments.JoinToString(Path.DirectorySeparatorChar.ToString());
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(combinedPath);

        return new DirectoryDescriptor(combinedPath);
    }

    public override string ToString() => FullPath;
    public bool Equals(DirectoryDescriptor other) => s_pathEqualityComparer.Equals(this, other);
    public override int GetHashCode() => HashCode.Combine(Name, Location, FullPath, IsRelative);

    public string Name { get; }

    // The parent directory path without the name. Can be absolute or relative.
    public string Location { get; }
    public string FullPath { get; }
    public bool IsRelative { get; }

    public bool IsExisting => Directory.Exists(FullPath);

    public static bool operator ==(DirectoryDescriptor left, DirectoryDescriptor right) => left.Equals(right);
    public static bool operator !=(DirectoryDescriptor left, DirectoryDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is DirectoryDescriptor other && Equals(other);
}
