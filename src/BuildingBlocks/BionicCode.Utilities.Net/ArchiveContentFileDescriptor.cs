namespace BionicCode.Utilities.Net;

using System.Diagnostics;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("SourceFileName = {SourceFileName}, Location = {Location}, RelativeArchiveEntryFilePath = {RelativeArchiveEntryFilePath}, IsRenamingRequired = {IsRenamingRequired}")]
public readonly struct ArchiveContentFileDescriptor : IEquatable<ArchiveContentFileDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveContentFileDescriptor"/> struct from a relative file path.
    /// </summary>
    /// <param name="sourceFilePath">The relative or absolute source file path.</param>
    /// <param name="relativeArchiveEntryLocation">
    /// Optional: The <c>string</c> representing the relative path to use inside an archive. 
    /// <br/>Provide an argument if the file must not be located at the archive's root. If not provided, the file is located at the root of the archive by default 
    /// and <see cref="RelativeArchiveEntryFilePath"/> will return the source file name.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when the provided file path is not relative.</exception>
    public ArchiveContentFileDescriptor(string sourceFilePath, string? relativeFilePath = null)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(relativeFilePath);

        relativeFilePath ??= Path.DirectorySeparatorChar.ToString();
        if (Path.IsPathFullyQualified(relativeFilePath))
        {
            throw new ArgumentException($"The argument '{nameof(relativeFilePath)}' must be a relative file path, relative to the archives content root.", nameof(relativeFilePath));
        }

        SourceFilePath = NormalizeSourceFilePath(sourceFilePath);
        RelativeArchiveEntryFilePath = NormalizeArchiveEntryName(FileHelpers.NormalizeFileSystemPath(relativeFilePath));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveContentFileDescriptor"/> struct from a full source path and a relative archive entry location.
    /// </summary>
    /// <param name="sourceFilePath">The <see cref="FileDescriptor"/> representing the relative or absolute source file path.</param>
    /// <param name="relativeArchiveEntryLocation">
    /// Optional: The <see cref="DirectoryDescriptor"/> representing the relative path to use inside an archive. 
    /// <br/>Provide an argument if the file must not be located at the archive's root. If not provided, the file is located at the root of the archive by default 
    /// and <see cref="RelativeArchiveEntryFilePath"/> will return the source file name.
    /// </param>
    public ArchiveContentFileDescriptor(FileDescriptor sourceFilePath, DirectoryDescriptor relativeArchiveEntryLocation = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(sourceFilePath);
        if (relativeArchiveEntryLocation != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(relativeArchiveEntryLocation.IsRelative, $"The argument '{nameof(relativeArchiveEntryLocation)}' must be a relative path.");
        }
        else
        {
            // Create a default root entry location with path symbol ".\"
            relativeArchiveEntryLocation = DirectoryDescriptor.CreateDirectoryRelativeToCurrent();
        }

        SourceFilePath = sourceFilePath;

        FileDescriptor relativeFilePath = sourceFilePath.Combine(relativeArchiveEntryLocation);
        RelativeArchiveEntryFilePath = NormalizeArchiveEntryName(relativeFilePath.FullPath);
    }

    public override string ToString() => $"Source file: {SourceFilePath}; Relative archive entry file: {RelativeArchiveEntryFilePath}";

    /// <summary>
    /// Gets the full file system path represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="SourceFileName"/> properties.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file path in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old path and <see cref="SourceFilePath"/> is the new path.
    /// </remarks>
    public FileDescriptor SourceFilePath { get; }
    public FileDescriptor RelativeArchiveEntryFilePath { get; }

    private static FileDescriptor NormalizeArchiveEntryName(string archiveEntryName)
        => new(archiveEntryName.Replace('\\', '/').TrimStart('/'));

    private static FileDescriptor NormalizeSourceFilePath(string sourceFilePath)
    {
        string normalizedSourceFilePath = FileHelpers.NormalizeFileSystemPath(sourceFilePath);
        return new FileDescriptor(normalizedSourceFilePath);
    }

    /// <summary>
    /// Compares a <see cref="FileDescriptor"/> to this instance using the <see cref="FileSystemPathEqualityComparer"/> to compare two <see cref="ArchiveContentFileDescriptor"/> instances based on platform specific file system naming rules.
    /// </summary>
    /// <param name="other">The other <see cref="ArchiveContentFileDescriptor"/> too compare to.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ArchiveContentFileDescriptor other) => s_pathEqualityComparer.Equals(SourceFilePath, other.SourceFilePath)
        && s_pathEqualityComparer.Equals(RelativeArchiveEntryFilePath, other.RelativeArchiveEntryFilePath);

    public override int GetHashCode() => s_pathEqualityComparer.GetHashCode(SourceFilePath) ^ s_pathEqualityComparer.GetHashCode(RelativeArchiveEntryFilePath);

    public static bool operator ==(ArchiveContentFileDescriptor left, ArchiveContentFileDescriptor right) => left.Equals(right);
    public static bool operator !=(ArchiveContentFileDescriptor left, ArchiveContentFileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is ArchiveContentFileDescriptor other && Equals(other);
}
