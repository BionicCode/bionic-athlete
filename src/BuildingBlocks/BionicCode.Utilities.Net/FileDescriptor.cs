namespace BionicCode.Utilities.Net;

using System.Diagnostics;
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
    private readonly WriteOnce<FileExtension> _extension;
    private readonly string _embeddedResourceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="fileName">The file name including the file extension.</param>
    /// <param name="location">The directory (location) of the file. Can be absolute or relative.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor location) : this(SystemIoPath.Join(location, fileName), PathKind.File)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source file newPath.
    /// </summary>
    /// <param name="filePath">The full file newPath. The file newPath can be absolute or relative.</param>
    public FileDescriptor(string filePath, PathKind pathKind)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PathKind>(pathKind);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(
            pathKind,
            [PathKind.Undefined],
            message: $"Invalid argument '{nameof(pathKind)}'. The value '{nameof(PathKind.Undefined)}' is not allowed.");
        ArgumentExceptionAdvanced.ThrowIfEnumNotEqualsAny(
            pathKind,
            [PathKind.File, PathKind.EmbeddedResource],
            message: $"Invalid argument '{nameof(pathKind)}'. The value of the argument '{nameof(pathKind)}' must be either '{nameof(PathKind.File)}' or '{nameof(PathKind.EmbeddedResource)}'.");

        _nameWithoutExtension = new WriteOnce<string>();
        _extension = new WriteOnce<FileExtension>();
        PathKind = pathKind;

        switch (PathKind)
        {
            case PathKind.File:
                FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);
                break;
            case PathKind.EmbeddedResource:
                _embeddedResourceName = filePath;
                _name = string.Empty;
                _location = PathDescriptor.Empty;
                _path = PathDescriptor.Empty;
                return;
            default:
                throw new NotImplementedException("Currently unsupported path kind.");
        }

        _name = new WriteOnce<string>();
        _location = new WriteOnce<PathDescriptor>();
        _path = new PathDescriptor(filePath, PathKind.File);
        IsRelative = Path.IsRelative;
    }

    public FileDescriptor Rename(string newFileName)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(newFileName);

        string newPath = SystemIoPath.Join(Location.PathString, newFileName);
        return this with
        {
            Name = newFileName,
            Extension = FileExtension.FromFileName(newFileName),
            Path = new PathDescriptor(newPath, PathKind.File)
        };
    }

    public FileDescriptor CopyOrMove(DirectoryDescriptor newLocation)
    {
        // Do not allow ending with file name
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(newLocation);

        string newPath = SystemIoPath.Join(newLocation, Name);
        return this with
        {
            Path = new PathDescriptor(newPath, PathKind.File),
            Location = newLocation.Path
        };
    }

    public FileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory, bool isImpicitRootAllowed)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute directory newPath.", nameof(baseDirectory));

        if (!IsRelative)
        {
            return this;
        }

        if (!isImpicitRootAllowed && Path.HasRoot && !HasExplicitDriveRoot)
        {
            throw new InvalidOperationException($"The file path '{FullPath}' has an implicit drive root, which is not allowed when the argument '{nameof(isImpicitRootAllowed)}' is set to false. An implicit drive root is a rooted path that does not have an explicit drive root like 'C:'. An example of an implicit drive rooted path is '/Temp' or '/example.txt', where the root drive resolves to the current working directory's drive.");
        }

        string relativePath = baseDirectory.Combine(this, isImpicitRootAllowed);
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
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory newPath.");

        if (HasExplicitDriveRoot && IsRelative)
        {
            // If the current newPath has an explicit drive root but is relative, we cannot resolve it to an absolute newPath without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute newPath because the current newPath '{FullPath}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this newPath.");
        }

        return Combine(absoluteBaseDirectory);
    }

    public override string ToString() => Path.PathKind is PathKind.EmbeddedResource
        ? _embeddedResourceName
        : Path;

    public string PathString => ToString();
    public bool TryGetPathRoot(out PathSegment pathRoot)
    {
        if (PathKind is PathKind.EmbeddedResource || IsDefaultInstance)
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
    public bool Equals(FileDescriptor other) => PathKind is PathKind.EmbeddedResource
        ? _embeddedResourceName.Equals(other._embeddedResourceName, StringComparison.Ordinal)
        : s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode() => PathKind is PathKind.EmbeddedResource
        ? _embeddedResourceName.GetHashCode(StringComparison.Ordinal)
        : s_pathEqualityComparer.GetHashCode(this);

    public bool IsDefaultInstance => _path is null
        && _name is null
        && _location is null
        && _extension is null
        && _nameWithoutExtension is null;

    public bool IsExisting => PathKind is PathKind.EmbeddedResource
        ? throw new InvalidOperationException("Cannot determine existence of a file descriptor with the path kind 'EmbeddedResource' because it does not represent a file system path. Existence can only be determined for file descriptors with the path kind 'File'.")
        : File.Exists(Path);

    public string NameWithoutExtension
    {
        get
        {
            if (_nameWithoutExtension is null
                || _name is null)
            {
                return string.Empty;
            }

            if (!_nameWithoutExtension.IsSet)
            {
                string nameWithoutExtension = SystemIoPath.GetFileNameWithoutExtension(Name);
                _nameWithoutExtension.SetValue(nameWithoutExtension);
            }

            return _nameWithoutExtension;
        }
    }

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

        private init => _name = value;
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
                    .ToPathSegmentList(PathKind.Directory);

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

        private init => _location = value;
    }

    public IEnumerable<PathSegment> EnumeratePathSegments()
    {

        foreach (PathSegment pathSegment in Path.Segments)
        {
            yield return pathSegment;
        }
    }

    /// <summary>
    /// Gets a <see cref="PathDescriptor"/> representing the full file system newPath of the file represented by this instance.
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

        private init => _path = value;
    }

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension
    {
        get
        {
            if (_extension is null
                || _name is null)
            {
                return FileExtension.Empty;
            }

            if (!_extension.IsSet)
            {
                var extension = FileExtension.FromFileName(Name);
                _extension.SetValue(extension);
            }

            return _extension;

        }
        private init;
    }

    /// <summary>
    /// Gets a value indicating whether the current newPath is relative rather than absolute.
    /// </summary>
    /// <remarks>In general, relative paths are interpreted as relative to a current working directory or relative to the current drive. Absolute paths specify a complete newPath from the root of the file system and are not dependent on the current working directory or current drive.</remarks>
    /// <value><see langword="true"/> if the newPath is relative or <see langword="false"/> if the newPath is absolute.</value>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets a value indicating whether the directory has an explicit drive root.
    /// </summary>
    /// <remarks>A directory has an explicit drive root if it is an absolute newPath or a relative newPath with an explicit root like "C:Temp".
    /// <br/>The property will treat paths like "/Temp" as implicitly drive rooted.</remarks>
    /// <value><see langword="true"/> if the directory has an explicit drive root like "C:Temp" or is an absolute newPath like "C:\User\Temp"; otherwise, <see langword="false"/>.</value>
    public bool HasExplicitDriveRoot => IsRooted && Path.Segments[0].Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeDriveRoot;

    /// <summary>
    /// Gets a value indicating whether the file newPath is rooted. A rooted file path starts with a root directory, such as "C:\" on Windows or "/" on Unix-based systems. 
    /// </summary>
    /// <remarks> Rooted paths can be either absolute or relative with an explicit drive root like "C:Temp" and "C:/User/Temp" or with an implicit drive root like "/Temp" or "/example.txt" where the root drive resolves to the current working directory's drive. 
    /// <para/>In contrast to <see cref="HasExplicitDriveRoot"/> this property will also return <see langword="true"/> for paths with an implicit drive root.</remarks>
    public bool IsRooted => Path.HasRoot;

    public PathKind PathKind { get; }

    public static bool operator ==(FileDescriptor left, FileDescriptor right) => left.Equals(right);
    public static bool operator !=(FileDescriptor left, FileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is FileDescriptor other && Equals(other);

    public static implicit operator string(FileDescriptor path) => path.ToString();
}
