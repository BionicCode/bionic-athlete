namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRenamingRequired = {IsRenamingRequired}")]
public readonly struct ArchiveContentFileDescriptor : IEquatable<ArchiveContentFileDescriptor>
{
    private readonly string _filePath;
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="name">The source file name.</param>
    /// <param name="location">The source directory.</param>
    /// <param name="relativeArchiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, <paramref name="name"/> is used.
    /// </param>
    public ArchiveContentFileDescriptor(string filePath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(filePath);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFilePath(Name);
        Location = new DirectoryDescriptor(Path.GetDirectoryName(filePath) ?? string.Empty);
        _filePath = filePath;
        RelativeArchiveEntryDirectoryPath = NormalizeArchiveEntryName(Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = Location.IsRelative;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="name">The source file name.</param>
    /// <param name="location">The source directory.</param>
    /// <param name="relativeArchiveEntryDirectory">
    /// The relative directory path to use inside an archive.
    /// When <see langword="null"/>, <paramref name="name"/> is used.
    /// </param>
    public ArchiveContentFileDescriptor(string name, DirectoryDescriptor location, DirectoryDescriptor relativeArchiveEntryDirectory = default)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(name);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(location);
        if (relativeArchiveEntryDirectory != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(
                relativeArchiveEntryDirectory.IsRelative,
                $"The argument '{nameof(relativeArchiveEntryDirectory)}' must be a relative path.");
        }
        else
        {
            relativeArchiveEntryDirectory = new DirectoryDescriptor(name);
        }

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(name);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFileName(Name);
        Location = location;
        _filePath = Path.Combine(Location.FullPath, Name);
        RelativeArchiveEntryDirectoryPath = NormalizeArchiveEntryName(relativeArchiveEntryDirectory.FullPath ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = Location.IsRelative;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source path.
    /// </summary>
    /// <param name="filePath">The full source file path.</param>
    /// <param name="relativeArchiveEntryDirectory">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, the source file name is used.
    /// </param>
    public ArchiveContentFileDescriptor(FileDescriptor filePath, DirectoryDescriptor relativeArchiveEntryDirectory = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);
        if (relativeArchiveEntryDirectory != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(relativeArchiveEntryDirectory.IsRelative, $"The argument '{nameof(relativeArchiveEntryDirectory)}' must be a relative path.");
        }
        else
        {
            relativeArchiveEntryDirectory = new DirectoryDescriptor(filePath.Name);
        }

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = filePath.Name;
        Extension = filePath.Extension;
        NameWithoutExtension = filePath.NameWithoutExtension;
        Location = filePath.Location;
        _filePath = filePath.FullPath;
        RelativeArchiveEntryDirectoryPath = NormalizeArchiveEntryName(relativeArchiveEntryDirectory.FullPath ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = Location.IsRelative;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and a <see cref="DirectoryDescriptor"/> that describes the location of the file.
    /// </summary>
    /// <param name="fileName">The file name including extension.</param>
    /// <param name="relativeLocation">The directory descriptor representing the relative location of the file in the provided <paramref name="embeddedResourceAssembly"/>.</param>
    /// <param name="relativeArchiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, the source file name is used.
    /// </param>
    /// <param name="embeddedResourceAssembly">The <see cref="Assembly"/> that the specified file is located in.</param>
    public ArchiveContentFileDescriptor(string fileName, DirectoryDescriptor relativeLocation, Assembly embeddedResourceAssembly, DirectoryDescriptor relativeArchiveEntryDirectory = default)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(fileName);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeLocation);
        ArgumentNullExceptionAdvanced.ThrowIfNull(embeddedResourceAssembly);
        if (relativeArchiveEntryDirectory != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(
                relativeArchiveEntryDirectory.IsRelative,
                $"The argument '{nameof(relativeArchiveEntryDirectory)}' must be a relative path.");
        }
        else
        {
            relativeArchiveEntryDirectory = new DirectoryDescriptor(fileName);
        }

        IsEmbeddedResource = true;
        EmbeddedResourceAssembly = embeddedResourceAssembly;
        Name = Path.GetFileName(fileName);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFileName(Name);
        Location = relativeLocation;
        _filePath = $"{Location}.{Name}"; // The full name of the embedded resource is typically in the format "Namespace.Folder.FileName"
        RelativeArchiveEntryDirectoryPath = NormalizeArchiveEntryName(relativeArchiveEntryDirectory.FullPath ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = relativeLocation.IsRelative;
    }

    public static ArchiveContentFileDescriptor CreateWithOriginalPath(string filePath, string originalFullPath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);
        FileSystemPathValidator.ThrowIfInvalidFilePath(originalFullPath);

        return new ArchiveContentFileDescriptor(filePath)
        {
            OriginalFullPath = originalFullPath,
            OriginalName = Path.GetFileName(originalFullPath)
        };
    }

    public static ArchiveContentFileDescriptor CreateWithOriginalPath(ArchiveContentFileDescriptor filePath, string originalFullPath)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);
        FileSystemPathValidator.ThrowIfInvalidFilePath(originalFullPath);

        return filePath with
        {
            OriginalFullPath = originalFullPath,
            OriginalName = Path.GetFileName(originalFullPath)
        };
    }

    public static ArchiveContentFileDescriptor CreateWithOriginalName(string filePath, string originalName)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);
        FileSystemPathValidator.ThrowIfInvalidFileName(originalName);

        return new ArchiveContentFileDescriptor(filePath)
        {
            OriginalFullPath = filePath,
            OriginalName = originalName
        };
    }

    public static ArchiveContentFileDescriptor CreateWithOriginalName(ArchiveContentFileDescriptor filePath, string originalName)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);
        FileSystemPathValidator.ThrowIfInvalidFileName(originalName);

        return filePath with
        {
            OriginalFullPath = filePath.FullPath,
            OriginalName = originalName
        };
    }

    public ArchiveContentFileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute path.", nameof(baseDirectory));

        if (IsRelative)
        {
            return this;
        }

        string relativePath = Path.GetRelativePath(baseDirectory.FullPath, FullPath);
        return new ArchiveContentFileDescriptor(relativePath);
    }

    public ArchiveContentFileDescriptor Combine(params DirectoryDescriptor[] additionalLocationSegments)
    {
        ArgumentExceptionAdvanced.ThrowIfAny(additionalLocationSegments, item => item == default);

        IEnumerable<string> pathSegments = additionalLocationSegments
            .Select(segment => segment.FullPath)
            .Concat([Name]);
        string combinedPath = pathSegments.JoinToString(Path.DirectorySeparatorChar.ToString());
        FileSystemPathValidator.ThrowIfInvalidFilePath(combinedPath);

        return new ArchiveContentFileDescriptor(combinedPath);
    }

    public override string ToString() => FullPath;

    public bool HasRenamingInformation => !s_pathEqualityComparer.Equals(OriginalFullPath, FullPath)
        || !s_pathEqualityComparer.Equals(OriginalName, Name);

    public Assembly EmbeddedResourceAssembly { get; }
    public bool IsEmbeddedResource { get; }

    public bool IsExisting => IsEmbeddedResource
        ? EmbeddedResourceAssembly.GetManifestResourceNames().Contains(FullPath)
        : File.Exists(FullPath);

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>Set <see cref="OriginalName"/> to preserve the original file name and use <see cref="Name"/> for the current file name. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.</remarks>
    public string Name { get; }
    public string NameWithoutExtension { get; }

    /// <summary>
    /// Gets the <see cref="DirectoryDescriptor"/> that specifies the location associated with the file described by this <see cref="FileDescriptor"/>.
    /// </summary>
    public DirectoryDescriptor Location { get; }

    /// <summary>
    /// Gets the full file system path represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="Name"/> properties.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file path in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old path and <see cref="FullPath"/> is the new path.
    /// </remarks>
    public string FullPath => _filePath;

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension { get; }

    /// <summary>
    /// Gets the relative path that should be used for the file inside an archive.
    /// </summary>
    /// <remarks>
    /// This value can contain forward slash directory separators, for example <c>core/activity</c>.
    /// </remarks>
    public DirectoryDescriptor RelativeArchiveEntryDirectoryPath { get; }

    /// <summary>
    /// The original full path of the file before any moving, renaming or copying operations. For embedded resources, 
    /// this is typically in the format "Namespace.Folder.FileName".
    /// </summary>
    /// <remarks>
    /// This value is set during the initialization of the <see cref="FileDescriptor"/> and remains unchanged 
    /// even if the file is renamed or copied. If not explicitly set via initializer the property returns <see cref="FullPath"/>.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file path in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old path and <see cref="FullPath"/> is the new path.
    /// </remarks>
    /// <value>The original full path of the file. The default value is the same as <see cref="FullPath"/>.</value>
    public string OriginalFullPath { get; private init; }

    /// <summary>
    /// Gets the original file name before any renaming operations.
    /// </summary>
    /// <remarks>
    /// This value is set during the initialization of the <see cref="FileDescriptor"/> and remains unchanged 
    /// even if the file is renamed. If not explicitly set via initializer the property returns <see cref="Name"/>.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file or old name in <see cref="OriginalName"/>. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.
    /// </remarks>
    /// <value>The original file name. The default value is the same as <see cref="Name"/>.</value>
    public string OriginalName { get; private init; }
    public bool IsRelative { get; }

    private static DirectoryDescriptor NormalizeArchiveEntryName(string archiveEntryName)
        => new(archiveEntryName.Replace('\\', '/').TrimStart('/'));

    /// <summary>
    /// Compares a <see cref="FileDescriptor"/> to this instance using the <see cref="FileSystemPathEqualityComparer"/> to compare two <see cref="ArchiveContentFileDescriptor"/> instances based on platform specific file system naming rules.
    /// </summary>
    /// <param name="other">The other <see cref="ArchiveContentFileDescriptor"/> too compare to.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ArchiveContentFileDescriptor other) => s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(HasRenamingInformation);
        hash.Add(EmbeddedResourceAssembly);
        hash.Add(Name);
        hash.Add(Location);
        hash.Add(FullPath);
        hash.Add(Extension);
        hash.Add(NameWithoutExtension);
        hash.Add(IsRelative);
        hash.Add(RelativeArchiveEntryDirectoryPath);
        hash.Add(OriginalFullPath);
        hash.Add(OriginalName);
        return hash.ToHashCode();
    }

    public static bool operator ==(ArchiveContentFileDescriptor left, ArchiveContentFileDescriptor right) => left.Equals(right);
    public static bool operator !=(ArchiveContentFileDescriptor left, ArchiveContentFileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is ArchiveContentFileDescriptor other && Equals(other);
}
