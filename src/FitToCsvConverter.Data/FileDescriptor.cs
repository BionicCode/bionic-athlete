namespace FitToCsvConverter.Data;

using System.Diagnostics;
using BionicCode.Utilities.Net;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRenamingRequired = {IsRenamingRequired}")]
public readonly struct FileDescriptor
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="name">The source file name.</param>
    /// <param name="location">The source directory.</param>
    /// <param name="isRenamingRequired">Whether the source file must be copied with a new name before archiving.</param>
    /// <param name="archiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, <paramref name="name"/> is used.
    /// </param>
    public FileDescriptor(string name, string location, bool isRenamingRequired, string? archiveEntryName = null)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(location);

        Name = name;
        Location = location;
        _filePath = Path.Combine(Location, Name);
        IsRenamingRequired = isRenamingRequired;
        ArchiveEntryName = NormalizeArchiveEntryName(archiveEntryName ?? Name);
        OriginalName = string.Empty;
        OriginalFullPath = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source path.
    /// </summary>
    /// <param name="filePath">The full source file path.</param>
    /// <param name="isRenamingRequired">Whether the source file must be copied with a new name before archiving.</param>
    /// <param name="archiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, the source file name is used.
    /// </param>
    public FileDescriptor(string filePath, bool isRenamingRequired, string? archiveEntryName = null)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        Name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        _filePath = filePath;
        IsRenamingRequired = isRenamingRequired;
        ArchiveEntryName = NormalizeArchiveEntryName(archiveEntryName ?? Name);
        OriginalName = string.Empty;
        OriginalFullPath = string.Empty;
    }

    public bool IsRenamingRequired { get; init; }
    public string Name { get; init; }
    public string Location { get; init; }
    public string FullPath => _filePath;

    /// <summary>
    /// Gets the relative path that should be used for the file inside an archive.
    /// </summary>
    /// <remarks>
    /// This value can contain forward slash directory separators, for example <c>core/activity.csv</c>.
    /// </remarks>
    public string ArchiveEntryName { get; init; }
    public string OriginalFullPath { get; init; }
    public string OriginalName { get; init; }

    private static string NormalizeArchiveEntryName(string archiveEntryName)
        => archiveEntryName.Replace('\\', '/').TrimStart('/');
}
