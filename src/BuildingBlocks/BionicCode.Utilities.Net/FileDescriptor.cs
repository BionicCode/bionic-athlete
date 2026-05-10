namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("FileName = {Name}, Location = {Location}, OriginalFullPath = {OriginalFullPath}, OriginalName = {OriginalName}, IsRelative = {IsRelative}")]
public readonly struct FileDescriptor : IEquatable<FileDescriptor>
{
    private readonly string _filePath;
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="fileName">The file name including the file extension.</param>
    /// <param name="location">The directory (location) of the file. Can be absolute or relative.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor location)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(location);
        FileSystemPathValidator.ThrowIfInvalidFileName(fileName);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = fileName;
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFileName(Name);
        Location = location;
        _filePath = Path.Combine(Location.FullPath, Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = location.IsRelative;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source sharedDirectoryPath.
    /// </summary>
    /// <param name="filePath">The full file sharedDirectoryPath. The sharedDirectoryPath can be absolute or relative.</param>
    public FileDescriptor(string filePath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);

        string normalizedFilePath = FileHelpers.NormalizeFileSystemPath(filePath);
        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(normalizedFilePath);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFilePath(Name);

        // The exception should never happen at this point since the validation in FileSystemPathValidator.ThrowIfInvalidFilePath ensures that the filePath is valid and contains a valid directory part.
        // However, we still need to handle the case where GetDirectoryName returns null instead silencing the analyzer warning using the null-forgiving operator       .
        Location = new DirectoryDescriptor(Path.GetDirectoryName(normalizedFilePath) ?? throw new ArgumentException($"The provided argument '{nameof(filePath)}' does not contain a valid directory. Found: '{filePath}'", nameof(filePath)));
        _filePath = normalizedFilePath;
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and a <see cref="DirectoryDescriptor"/> that describes the location of the file.
    /// </summary>
    /// <param name="fileName">The file name including extension.</param>
    /// <param name="relativeLocation">The directory descriptor representing the relative location of the file in the provided <paramref name="embeddedResourceAssembly"/>.</param>
    /// <param name="embeddedResourceAssembly">The <see cref="Assembly"/> that the specified file is located in.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor relativeLocation, Assembly embeddedResourceAssembly)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(fileName);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeLocation);
        ArgumentExceptionAdvanced.ThrowIfFalse(
            relativeLocation.IsRelative,
            "The provided location must be a relative sharedDirectoryPath.", nameof(relativeLocation));
        ArgumentNullExceptionAdvanced.ThrowIfNull(embeddedResourceAssembly);

        IsEmbeddedResource = true;
        EmbeddedResourceAssembly = embeddedResourceAssembly;
        Name = fileName;
        Extension = FileExtension.FromFileName(Name);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Name);

        // If name without extension is empty then the extension is the file name. For example, ".gitignore" is a valid file name but treated as extension by the Path API.
        NameWithoutExtension = string.IsNullOrWhiteSpace(fileNameWithoutExtension)
            ? Name
            : fileNameWithoutExtension;
        Location = relativeLocation;
        _filePath = $"{Location}.{Name}"; // The full name of the embedded resource is typically in the format "Namespace.Folder.FileName"
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = relativeLocation.IsRelative;
    }

    public static FileDescriptor CreateWithOriginalPath(string newFilePath, string originalFilePath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(newFilePath);
        FileSystemPathValidator.ThrowIfInvalidFilePath(originalFilePath);

        string normalizedFilePath = FileHelpers.NormalizeFileSystemPath(newFilePath);
        string normalizedOriginalFullPath = FileHelpers.NormalizeFileSystemPath(originalFilePath);

        return new FileDescriptor(normalizedFilePath)
        {
            OriginalFullPath = normalizedOriginalFullPath,
            OriginalName = Path.GetFileName(normalizedOriginalFullPath)
        };
    }

    public static FileDescriptor CreateWithOriginalPath(FileDescriptor newFilePath, string originalFilePath)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(newFilePath);
        FileSystemPathValidator.ThrowIfInvalidFilePath(originalFilePath);

        string normalizedOriginalFullPath = FileHelpers.NormalizeFileSystemPath(originalFilePath);
        return newFilePath with
        {
            OriginalFullPath = normalizedOriginalFullPath,
            OriginalName = Path.GetFileName(normalizedOriginalFullPath)
        };
    }

    public static FileDescriptor CreateWithOriginalName(string newFilePath, string originalName)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(newFilePath);
        FileSystemPathValidator.ThrowIfInvalidFileName(originalName);

        // GetDirectoryName returns NULL for root directories. In this case we just use 'newFilePath'.
        string sharedDirectoryPath = Path.GetDirectoryName(newFilePath) ?? newFilePath;

        string normalizedFileLocation = FileHelpers.NormalizeFileSystemPath(sharedDirectoryPath);
        string originalFilePath = Path.Combine(normalizedFileLocation, originalName);
        return new FileDescriptor(newFilePath)
        {
            OriginalFullPath = originalFilePath,
            OriginalName = originalName
        };
    }

    public static FileDescriptor CreateWithOriginalName(FileDescriptor newFilePath, string originalName)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(newFilePath);
        FileSystemPathValidator.ThrowIfInvalidFileName(originalName);

        // FileDescriptor.Location is already valid and normalized due to the validation in the constructor, so we can directly use it.
        string sharedDirectoryPath = newFilePath.Location.FullPath;

        string originalFilePath = Path.Combine(sharedDirectoryPath, originalName);

        return newFilePath with
        {
            OriginalFullPath = originalFilePath,
            OriginalName = originalName
        };
    }

    public FileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute sharedDirectoryPath.", nameof(baseDirectory));

        if (IsRelative)
        {
            return this;
        }

        string relativePath = Path.GetRelativePath(baseDirectory.FullPath, FullPath);
        return new FileDescriptor(relativePath);
    }

    public FileDescriptor Combine(params DirectoryDescriptor[] precedingLocationSegments)
    {
        ArgumentExceptionAdvanced.ThrowIfAny(precedingLocationSegments, item => item == default);

        IEnumerable<string> pathSegments = precedingLocationSegments
            .Select(segment => segment.FullPath)
            .Concat([Name]);
        string combinedPath = pathSegments.JoinToString(Path.DirectorySeparatorChar.ToString());
        FileSystemPathValidator.ThrowIfInvalidFilePath(combinedPath);

        return new FileDescriptor(combinedPath);
    }

    public FileDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");

        if (HasExplicitDriveRoot && IsRelative)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{FullPath}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        return Combine(absoluteBaseDirectory);
    }

    public override string ToString() => FullPath;

    /// <summary>
    /// Compares a <see cref="FileDescriptor"/> to this instance using the <see cref="FileSystemPathEqualityComparer"/> to compare two <see cref="FileDescriptor"/> instances based on platform specific file system naming rules.
    /// </summary>
    /// <param name="other">The other <see cref="FileDescriptor"/> too compare to.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(FileDescriptor other) => s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode() => s_pathEqualityComparer.GetHashCode(this);

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
    /// Gets the full file system sharedDirectoryPath represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="Name"/> properties.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file sharedDirectoryPath in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old sharedDirectoryPath and <see cref="FullPath"/> is the new sharedDirectoryPath.
    /// </remarks>
    public string FullPath { get; }

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension { get; }

    /// <summary>
    /// The original full sharedDirectoryPath of the file before any moving, renaming or copying operations. For embedded resources, 
    /// this is typically in the format "Namespace.Folder.FileName".
    /// </summary>
    /// <remarks>
    /// This value is set during the initialization of the <see cref="FileDescriptor"/> and remains unchanged 
    /// even if the file is renamed or copied. If not explicitly set via initializer the property returns <see cref="FullPath"/>.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file sharedDirectoryPath in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old sharedDirectoryPath and <see cref="FullPath"/> is the new sharedDirectoryPath.
    /// </remarks>
    /// <value>The original full sharedDirectoryPath of the file. The default value is the same as <see cref="FullPath"/>.</value>
    public string OriginalFullPath { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the current path is relative rather than absolute.
    /// </summary>
    /// <remarks>In general, relative paths are interpreted as relative to a current working directory or relative to the current drive. Absolute paths specify a complete path from the root of the file system and are not dependent on the current working directory or current drive.</remarks>
    /// <value><see langword="true"/> if the path is relative or <see langword="false"/> if the path is absolute.</value>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets a value indicating whether the directory has an explicit drive root.
    /// </summary>
    /// <remarks>A directory has an explicit drive root if it is an absolute path or a relative path with an explicit root like "C:Temp".
    /// <br/>The property will treat paths like "/Temp" as implicitly drive rooted.</remarks>
    /// <value><see langword="true"/> if the directory has an explicit drive root like "C:Temp" or is an absolute path like "C:\User\Temp"; otherwise, <see langword="false"/>.</value>
    public bool HasExplicitDriveRoot => Path.IsPathRooted(FullPath) && (Path.GetPathRoot(FullPath)?.Length ?? 0) > 1;

    /// <summary>
    /// Gets a value indicating whether the file path is rooted. A rooted path is a path that starts with a root directory, such as "C:\" on Windows or "/" on Unix-based systems. 
    /// </summary>
    /// <remarks> Rooted paths can be either absolute or relative with an explicit drive root like "C:Temp" and "C:/User/Temp" or with an implicit drive root like "/Temp" or "/example.txt" where the root drive resolves to the current working directory's drive. 
    /// <para/>In contrast to <see cref="HasExplicitDriveRoot"/> this property will also return <see langword="true"/> for paths with an implicit drive root.</remarks>
    public bool IsRooted => Path.IsPathRooted(FullPath);

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

    public static bool operator ==(FileDescriptor left, FileDescriptor right) => left.Equals(right);
    public static bool operator !=(FileDescriptor left, FileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is FileDescriptor other && Equals(other);
}
