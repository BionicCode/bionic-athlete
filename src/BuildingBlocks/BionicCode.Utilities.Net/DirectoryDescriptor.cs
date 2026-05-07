namespace BionicCode.Utilities.Net;

using System.Diagnostics;

/// <summary>
/// Describes a directory that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRelative = {IsRelative}")]
public readonly record struct DirectoryDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="name">The directory name.</param>
    /// <param name="location">The directory path without the name.</param>
    public DirectoryDescriptor(string name, string location)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(location);

        Name = name;
        Location = location;
        FullPath = Path.Combine(Location, Name);
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="fullPath">The full path of the directory.</param>
    public DirectoryDescriptor(string fullPath)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(fullPath);

        string pathWithoutTrailingSeparator = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Name = Path.GetFileName(pathWithoutTrailingSeparator);
        Location = Path.GetDirectoryName(pathWithoutTrailingSeparator) ?? string.Empty;
        FullPath = Path.Combine(Location, Name);
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    public override string ToString() => FullPath;

    public string Name { get; init; }
    public string Location { get; init; }
    public string FullPath { get; }
    public bool IsRelative { get; }
}
