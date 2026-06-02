namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using SystemIoPath = System.IO.Path;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("FileName = {Name}, Location = {Location}, OriginalFullPath = {OriginalFullPath}, OriginalName = {OriginalName}, IsRelative = {IsRelative}")]
public readonly struct FileDescriptor : IEquatable<FileDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    private readonly WriteOnce<PathDescriptor> _path;
    private readonly WriteOnce<PathDescriptor> _location;
    private readonly WriteOnce<string> _name;
    private readonly WriteOnce<string> _nameWithoutExtension;
    private readonly WriteOnce<string> _extension;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="fileName">The file name including the file extension.</param>
    /// <param name="location">The directory (location) of the file. Can be absolute or relative.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor location) : this(SystemIoPath.Join(location, fileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source file path.
    /// </summary>
    /// <param name="filePath">The full file path. The file path can be absolute or relative.</param>
    public FileDescriptor(string filePath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);

        _name = new WriteOnce<string>();
        _location = new WriteOnce<PathDescriptor>();
        _nameWithoutExtension = new WriteOnce<string>();
        _extension = new WriteOnce<string>();

        _path = new PathDescriptor(filePath, isDirectory: false);
        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
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
            "The provided location must be directory path relative to the assembly.", nameof(relativeLocation));
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

    public FileDescriptor Rename(string newFileName)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(newFileName);

        return this with 
        { 
            Name = newFileName, 
            Extension = FileExtension.FromFileName(newFileName) 
            Path = 
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
        string directory path = Path.GetDirectoryName(newFilePath) ?? newFilePath;

        string normalizedFileLocation = FileHelpers.NormalizeFileSystemPath(directory path);
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
        string directory path = newFilePath.Location.PathString;

        string originalFilePath = Path.Combine(directory path, originalName);

        return newFilePath with
        {
            OriginalFullPath = originalFilePath,
            OriginalName = originalName
        };
    }

    public FileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute directory path.", nameof(baseDirectory));

        if (IsRelative)
        {
            return this;
        }

        string relativePath = Path.GetRelativePath(baseDirectory.PathString, FullPath);
        return new FileDescriptor(relativePath);
    }

    public FileDescriptor Combine(params DirectoryDescriptor[] precedingLocationSegments)
    {
        ArgumentExceptionAdvanced.ThrowIfAny(precedingLocationSegments, item => item == default);

        IEnumerable<string> pathSegments = precedingLocationSegments
            .Select(segment => segment.PathString)
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

    public string PathString => Path.PathString;
    public bool TryGetPathRoot(out PathSegment pathRoot)
    {
        if (IsDefaultInstance)
        {
            pathRoot = PathSegment.Empty;
            return false;
        }

        if (Path.HasRoot)
        {
            pathRoot = Path.Segments[0];
            return pathRoot.IsRoot;
        }

        pathRoot = PathSegment.Empty;
        return false;
    }

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

    public bool IsDefaultInstance => _path is null && _name is null && _location is null;

    public bool IsExisting => IsEmbeddedResource
        ? EmbeddedResourceAssembly.GetManifestResourceNames().Contains(FullPath)
        : File.Exists(FullPath);
    public string NameWithoutExtension { get; private init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>Set <see cref="OriginalName"/> to preserve the original file name and use <see cref="Name"/> for the current file name. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.</remarks>
    public string Name
    {
        get
        {
            if (_name is null
                || _path is null
                || Path.Segments.IsEmpty)
            {
                return string.Empty;
            }

            if (!_name.IsSet)
            {
                string name;
                if (Path.Segments.Count == 1)
                {
                    PathSegment pathSegment = Path.Segments[0];
                    name = pathSegment.Kind is PathSegmentKind.DirectoryName
                        ? pathSegment.Name
                        : string.Empty;
                }
                else
                {
                    name = Path.Segments[^1].Name;
                }

                _name.SetValue(name);
            }

            return _name;
        }
        private init
        {

            _name = value;
        }
    }

    /// <summary>
    /// Gets the <see cref="DirectoryDescriptor"/> that specifies the location associated with the file described by this <see cref="FileDescriptor"/>.
    /// </summary>
    public PathDescriptor Location
    {
        get
        {
            if (_path is null
                || Path.Segments is null
                || Path.Segments.Count == 0)
            {
                return PathDescriptor.Empty;
            }

            if (!_location.IsSet)
            {
                PathDescriptor parentPath;
                var parentPathSegments = Path.Segments
                    .Take(Path.Segments.Count - 1)
                    .ToPathSegmentList(isDirectory: true);

                if (parentPathSegments.Count == 1)
                {
                    parentPath = parentPathSegments[0].Kind is PathSegmentKind.DirectoryName
                         ? PathDescriptor.Empty
                         : parentPathSegments;
                }
                else
                {
                    parentPath = parentPathSegments;
                }

                _location.SetValue(parentPath);
            }

            return _location;
        }
        private init
        {

            _location = value;
        }
    }

    public IEnumerable<PathSegment> EnumeratePathSegments()
    {

        foreach (PathSegment pathSegment in Path.Segments)
        {
            yield return pathSegment;
        }
    }

    /// <summary>
    /// Gets a <see cref="PathDescriptor"/> representing the full file system path of the file represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="Name"/> properties.
    /// </remarks>
    public PathDescriptor Path
    {
        get
        {
            // Can only be NULL when instance is default or the implicit default constructor was used to create this instance.
            // In both cases the instance is considered invalid.
            // Since string.Empty is not considered valid under normal construction returning string.Empty is fine to communicate an uninitialized compiler default state and least disturbing.
            if (_path is null)
            {
                return PathDescriptor.Empty;
            }

            return _path;
        }
        private init
        {

            _path = value;
        }
    }

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension { get; private init; }

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

    public static bool operator ==(FileDescriptor left, FileDescriptor right) => left.Equals(right);
    public static bool operator !=(FileDescriptor left, FileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is FileDescriptor other && Equals(other);

    public static implicit operator string(FileDescriptor path) => path.ToString();
}
