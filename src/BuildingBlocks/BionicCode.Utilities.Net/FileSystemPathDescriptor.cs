namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using SystemIoPath = System.IO.Path;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("FileName = {Name}, Location = {Location}, OriginalFullPath = {OriginalFullPath}, OriginalName = {OriginalName}, IsRelative = {IsRelative}")]
public sealed class FileSystemPathDescriptor : FileDescriptor, IEquatable<FileSystemPathDescriptor>
{
    private readonly WriteOnce<DirectoryDescriptor> _location;
    private readonly WriteOnce<int> _hashCode;
    private readonly bool _isEmptyInstance;

    public static FileSystemPathDescriptor Empty { get; } = new FileSystemPathDescriptor();

    protected override bool EqualsCore(FileDescriptor? other) => other is FileSystemPathDescriptor descriptorOther
        && FileSystemPathEqualityComparer.Instance.Equals(Path, descriptorOther.Path);

    protected override int GetHashCodeCore()
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = Path.GetHashCode();
            _hashCode.SetValue(hashCode);
        }

        return _hashCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemPathDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="fileName">The file name including the file extension.</param>
    /// <param name="location">The directory (location) of the file. Can be absolute or relative.</param>
    public FileSystemPathDescriptor(string fileName, DirectoryDescriptor location)
        : this(SystemIoPath.Join(location, fileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemPathDescriptor"/> struct from a full source file path.
    /// </summary>
    /// <param name="filePath">The full file path. The file path can be absolute or relative.</param>
    /// <param name="isEmbeddedResource">Indicates whether the file is an embedded resource.</param>
    public FileSystemPathDescriptor(string filePath) : base(FileDescriptorKind.FileSystemPath)
    {
        FileSystemPathValidator.ThrowIfInvalidFilePath(filePath);

        _location = new WriteOnce<DirectoryDescriptor>();
        _hashCode = new WriteOnce<int>();
        Path = new PathDescriptor(filePath, PathKind.File);
        IsRelative = Path.IsRelative;
    }

    /// <summary>
    /// Constructor for creating an empty instance of the <see cref="FileSystemPathDescriptor"/> struct. 
    /// </summary>
    /// <remarks>
    /// This constructor is used to create the singleton empty instance returned by the <see cref="Empty"/> property.
    /// </remarks>
    private FileSystemPathDescriptor() : base(FileDescriptorKind.FileSystemPath)
    {
        _isEmptyInstance = true;
        _location = DirectoryDescriptor.Empty;
        _hashCode = new WriteOnce<int>();
        Path = PathDescriptor.Empty;
        IsRelative = false;
    }

    protected override string GetName() => _isEmptyInstance || Path.Segments.Count == 0
        ? string.Empty
        : Path.Segments[^1].Name;

    protected override FileExtension GetFileExtension()
    {
        if (_isEmptyInstance)
        {
            return FileExtension.Empty;
        }

        var extension = FileExtension.FromFileName(Name);
        return extension;
    }

    protected override string GetNameWithoutExtension()
    {
        if (_isEmptyInstance)
        {
            return string.Empty;
        }

        string nameWithoutExtension = SystemIoPath.GetFileNameWithoutExtension(Name);
        return nameWithoutExtension;
    }

    public FileSystemPathDescriptor Rename(string newFileName)
    {
        FileSystemPathValidator.ThrowIfInvalidFileName(newFileName);

        string newPath = SystemIoPath.Join(Location, newFileName);
        return new FileSystemPathDescriptor(newPath);
    }

    public FileSystemPathDescriptor CopyOrMove(DirectoryDescriptor newLocation)
    {
        // Do not allow ending with file name
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(newLocation);

        string newPath = SystemIoPath.Join(newLocation, Name);
        return new FileSystemPathDescriptor(newPath);
    }

    public FileSystemPathDescriptor Combine(bool isImplicitRootAllowed, params DirectoryDescriptor[] precedingLocationSegments)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(precedingLocationSegments);
        ArgumentExceptionAdvanced.ThrowIfAny(precedingLocationSegments, item => item == default);

        if (precedingLocationSegments.Length == 0)
        {
            return this;
        }

        DirectoryDescriptor combinedBasePath = precedingLocationSegments[0];
        FileSystemPathDescriptor combinedFilePath = combinedBasePath.Combine(this, precedingLocationSegments.Skip(1), isImplicitRootAllowed);

        return combinedFilePath;
    }

    public FileSystemPathDescriptor ToAbsolutePath(DirectoryDescriptor baseDirectory, bool isImplicitRootAllowed)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);

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

            return new FileSystemPathDescriptor(rootedPathSegments);
        }

        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, $"The argument '{nameof(baseDirectory)}' must be an absolute directory path.");
        return Combine(isImplicitRootAllowed, baseDirectory);
    }

    public override string ToString() => Path;

    public string PathString => ToString();
    public bool TryGetPathRoot(out PathSegment pathRoot)
    {
        if (Path.HasRoot)
        {
            pathRoot = Path.Segments[0];
            return pathRoot.IsRoot;
        }

        pathRoot = PathSegment.Empty;
        return false;
    }

    /// <summary>
    /// Compares a <see cref="FileSystemPathDescriptor"/> to this instance using the <see cref="FileSystemPathEqualityComparer"/> to compare two <see cref="FileSystemPathDescriptor"/> instances based on platform specific file system naming rules.
    /// </summary>
    /// <param name="other">The other <see cref="FileSystemPathDescriptor"/> too compare to.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(FileSystemPathDescriptor? other) => base.Equals(other);

    public bool IsExisting => File.Exists(Path);

    /// <summary>
    /// Gets the <see cref="DirectoryDescriptor"/> that specifies the location associated with the file described by this <see cref="FileSystemPathDescriptor"/>.
    /// </summary>
    public DirectoryDescriptor Location
    {
        get
        {
            if (!_location.IsSet)
            {
                // Collect all path segments except the last one (file name) to get the parent directory path.
                var parentPathSegments = Path.Segments
                    .Take(Path.Segments.Count - 1)
                    .ToPathSegmentList(PathKind.Directory, Path.Segments.IsNormalized);

                DirectoryDescriptor parentDirectory = parentPathSegments.IsEmpty
                    ? DirectoryDescriptor.Empty
                    : new DirectoryDescriptor(parentPathSegments.ToPathDescriptor());
                _location.SetValue(parentDirectory);
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
    /// Gets a <see cref="PathDescriptor"/> representing the full file system path of the file represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="Name"/> properties.
    /// </remarks>
    public PathDescriptor Path { get; }

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
    public bool HasExplicitDriveRoot => IsRooted
        && Path.Segments[0].Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeDriveRoot;

    /// <summary>
    /// Gets a value indicating whether the file path is rooted. A rooted file path starts with a root directory, such as "C:\" on Windows or "/" on Unix-based systems. 
    /// </summary>
    /// <remarks> Rooted paths can be either absolute or relative with an explicit drive root like "C:Temp" and "C:/User/Temp" or with an implicit drive root like "/Temp" or "/example.txt" where the root drive resolves to the current working directory's drive. 
    /// <para/>In contrast to <see cref="HasExplicitDriveRoot"/> this property will also return <see langword="true"/> for paths with an implicit drive root.</remarks>
    public bool IsRooted => Path.HasRoot;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Is instance scope member.")]
    public PathKind PathKind => PathKind.File;

    public static bool operator ==(FileSystemPathDescriptor? left, FileSystemPathDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(FileSystemPathDescriptor? left, FileSystemPathDescriptor? right) => !(left == right);
    public static implicit operator string(FileSystemPathDescriptor path) => path?.ToString() ?? string.Empty;

    // Override to silence warnings about non-overridden equality members in derived classes.
    // The actual equality comparison logic is implemented in the base class and relies on the type of the file descriptor,
    // so we can safely delegate to the base implementation here.
    public override bool Equals(object? obj) => obj is FileSystemPathDescriptor other && Equals(other);
    public override int GetHashCode() => base.GetHashCode();
}
