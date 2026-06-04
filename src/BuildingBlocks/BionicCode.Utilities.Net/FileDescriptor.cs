namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using SystemIoPath = System.IO.Path;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("FileName = {Name}, Location = {Location}, OriginalFullPath = {OriginalFullPath}, OriginalName = {OriginalName}, IsRelative = {IsRelative}")]
public readonly struct FileDescriptor : IEquatable<FileDescriptor>
{
    public static FileDescriptor Empty { get; } = default(FileDescriptor) with
    {
        Name = string.Empty,
        Path = PathDescriptor.Empty,
        Location = DirectoryDescriptor.Empty,
        NameWithoutExtension = string.Empty,
        Extension = FileExtension.Empty,
    };

    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    private readonly WriteOnce<PathDescriptor> _path;
    private readonly WriteOnce<DirectoryDescriptor> _location;
    private readonly WriteOnce<string> _name;
    private readonly WriteOnce<string> _nameWithoutExtension;
    private readonly WriteOnce<FileExtension> _extension;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="fileName">The file name including the file extension.</param>
    /// <param name="location">The directory (location) of the file. Can be absolute or relative.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor location)
        : this(SystemIoPath.Join(location, fileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source file path.
    /// </summary>
    /// <param name="filePath">The full file path. The file path can be absolute or relative.</param>
    /// <param name="isEmbeddedResource">Indicates whether the file is an embedded resource.</param>
    public FileDescriptor(string filePath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);

        _nameWithoutExtension = new WriteOnce<string>();
        _extension = new WriteOnce<FileExtension>();
        _name = new WriteOnce<string>();
        _location = new WriteOnce<DirectoryDescriptor>();
        _path = new PathDescriptor(filePath, PathKind.File);
        IsRelative = Path.IsRelative;
    }

    public FileDescriptor Rename(string newFileName)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(newFileName);

        if (IsDefaultInstance)
        {
            return FileDescriptor.Empty;
        }

        if (IsEmbeddedResource)
        {
            return this with
            {
                Name = newFileName
            };
        }

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

        if (IsEmbeddedResource)
        {
            throw new InvalidOperationException($"Cannot copy or move a file descriptor where the '{nameof(IsEmbeddedResource)}' property is true because it does not represent a file system path.");
        }

        if (IsDefaultInstance)
        {
            return FileDescriptor.Empty;
        }

        string newPath = SystemIoPath.Join(newLocation, Name);
        return this with
        {
            Path = new PathDescriptor(newPath, PathKind.File),
            Location = newLocation.Path
        };
    }

    public FileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory, bool isImplicitRootAllowed)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute directory path.", nameof(baseDirectory));

        if (IsDefaultInstance
            || IsEmbeddedResource)
        {
            return FileDescriptor.Empty;
        }

        if (!IsRelative)
        {
            return this;
        }

        if (!isImplicitRootAllowed && Path.HasRoot && !HasExplicitDriveRoot)
        {
            throw new InvalidOperationException($"The file path '{Path}' has an implicit drive root, which is not allowed when the argument '{nameof(isImplicitRootAllowed)}' is set to false. An implicit drive root is a rooted path that does not have an explicit drive root like 'C:'. An example of an implicit drive rooted path is '/Temp' or '/example.txt', where the root drive resolves to the current working directory's drive.");
        }

        string relativePath = baseDirectory.Combine(this, isImplicitRootAllowed);
        return new FileDescriptor(relativePath, isEmbeddedResource: false);
    }

    public FileDescriptor Combine(bool isImplicitRootAllowed, params DirectoryDescriptor[] precedingLocationSegments)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(precedingLocationSegments);
        ArgumentExceptionAdvanced.ThrowIfAny(precedingLocationSegments, item => item == default);

        if (IsDefaultInstance
            || IsEmbeddedResource)
        {
            return FileDescriptor.Empty;
        }

        if (precedingLocationSegments.Length == 0)
        {
            return this;
        }

        DirectoryDescriptor combinedBasePath = precedingLocationSegments[0];
        FileDescriptor combinedFilePath = combinedBasePath.Combine(this, precedingLocationSegments.Skip(1), isImplicitRootAllowed);

        return combinedFilePath;
    }

    public FileDescriptor ToAbsolutePath(DirectoryDescriptor baseDirectory, bool isImplicitRootAllowed)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);

        if (IsDefaultInstance
            || IsEmbeddedResource
            || (!isImplicitRootAllowed && Path.HasRoot && Path.IsRelative))
        {
            return FileDescriptor.Empty;
        }

        if (!IsRelative)
        {
            return this;
        }

        // File path could have the shape of "C:Temp" where it is relative but has an explicit drive root.
        // If the 'baseDirectory' is relative we can use it to resolve the file path to an absolute path because the file path is relative. 
        if (HasExplicitDriveRoot)
        {
            // If the current file path has an explicit drive root but 'baseDirectory' is absolute,
            // we cannot resolve it to an absolute path without. Therefore, we throw an exception in this case.
            if (!baseDirectory.IsRelative)
            {
                throw new InvalidOperationException($"Cannot convert to an absolute file path because the current file path '{Path}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this file path.");
            }

            IEnumerable<PathSegment> baseDirectoryWithoutLeadingSpecialSymbols = baseDirectory.Path.NormalizedPath.Segments.SkipWhile(segment => segment.IsSpecial);

            // Move the drive root from the file path to the base directory and combine the paths.
            // For example, if the file path is "C:Temp?test.txt" and the base directory is "\BaseDirectory", we move the drive root "C:" to the base directory and combine it with the remaining file path "Temp" to get "C:\BaseDirectory\Temp".
            PathSegmentList filePathSegmentsWithoutDriveRoot = Path.NormalizedPath.Segments[1..];
            filePathSegmentsWithoutDriveRoot = filePathSegmentsWithoutDriveRoot.InsertRange(0, baseDirectoryWithoutLeadingSpecialSymbols);
            PathSegmentList rootedPathSegments = filePathSegmentsWithoutDriveRoot.Insert(0, Path.NormalizedPath.Segments[0]);

            return new FileDescriptor(rootedPathSegments, isEmbeddedResource: false);
        }

        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, $"The argument '{nameof(baseDirectory)}' must be an absolute directory path.");
        return Combine(isImplicitRootAllowed, baseDirectory);
    }

    public override string ToString() => IsEmbeddedResource
        ? Name
        : Path;

    public string PathString => ToString();
    public bool TryGetPathRoot(out PathSegment pathRoot)
    {
        if (IsEmbeddedResource || IsDefaultInstance)
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
    public bool Equals(FileDescriptor other) => IsEmbeddedResource
        ? Name.Equals(other.Name, StringComparison.Ordinal)
        : s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode() => IsEmbeddedResource
        ? Name.GetHashCode(StringComparison.Ordinal)
        : s_pathEqualityComparer.GetHashCode(this);

    public bool IsDefaultInstance => _path is null
        && _name is null
        && _location is null
        && _extension is null
        && _nameWithoutExtension is null;

    public bool IsExisting => IsEmbeddedResource
        ? throw new InvalidOperationException("Cannot determine existence of a file descriptor with the path kind 'EmbeddedResource' because it does not represent a file system path. Existence can only be determined for file descriptors with the path kind 'File'.")
        : File.Exists(Path);

    public string NameWithoutExtension
    {
        get
        {
            if (IsDefaultInstance
                || IsEmbeddedResource)
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
        private init => _nameWithoutExtension = value;
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
            if (IsDefaultInstance)
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
    public DirectoryDescriptor Location
    {
        get
        {
            if (IsDefaultInstance
                || IsEmbeddedResource)
            {
                return DirectoryDescriptor.Empty;
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

                var parentDirectory = new DirectoryDescriptor(parentPath);
                _location.SetValue(parentDirectory);
            }

            return _location;
        }

        private init => _location = value;
    }

    public IEnumerable<PathSegment> EnumeratePathSegments()
    {
        if (IsDefaultInstance
            || IsEmbeddedResource)
        {
            yield break;
        }

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
            if (IsDefaultInstance
                || IsEmbeddedResource)
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
            if (IsDefaultInstance
                || IsEmbeddedResource)
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
        private init => _extension = value;
    }

    /// <summary>
    /// Gets a value indicating whether the current file path is relative rather than absolute.
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
    public bool HasExplicitDriveRoot => !IsDefaultInstance
        && !IsEmbeddedResource
        && IsRooted
        && Path.Segments[0].Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeDriveRoot;

    /// <summary>
    /// Gets a value indicating whether the file path is rooted. A rooted file path starts with a root directory, such as "C:\" on Windows or "/" on Unix-based systems. 
    /// </summary>
    /// <remarks> Rooted paths can be either absolute or relative with an explicit drive root like "C:Temp" and "C:/User/Temp" or with an implicit drive root like "/Temp" or "/example.txt" where the root drive resolves to the current working directory's drive. 
    /// <para/>In contrast to <see cref="HasExplicitDriveRoot"/> this property will also return <see langword="true"/> for paths with an implicit drive root.</remarks>
    public bool IsRooted => !IsDefaultInstance
        && !IsEmbeddedResource
        && Path.HasRoot;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Is instance scope member.")]
    public PathKind PathKind => PathKind.File;

    public static bool operator ==(FileDescriptor left, FileDescriptor right) => left.Equals(right);
    public static bool operator !=(FileDescriptor left, FileDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is FileDescriptor other && Equals(other);

    public static implicit operator string(FileDescriptor path) => path.ToString();
}
