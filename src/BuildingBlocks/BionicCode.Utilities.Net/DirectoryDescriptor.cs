namespace BionicCode.Utilities.Net;

using System.Diagnostics;

/// <summary>
/// Describes a directory that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("SourceFileName = {SourceFileName}, Location = {Location}, IsSourceRelative = {IsSourceRelative}")]
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

        Name = name;
        Location = FileHelpers.NormalizeFileSystemPath(location);
        FullPath = Path.Join(Location, Name);
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
        FullPath = normalizedFullPath;
        IsRelative = !Path.IsPathFullyQualified(FullPath);

        if (IsSpecialDirectorySymbol(normalizedFullPath))
        {
            // Special directory symbols like "." and ".." are treated as relative paths with the symbol as the name and the location as the current directory symbol "."
            Name = string.Empty;
            Location = normalizedFullPath;

            return;
        }

        Name = Path.GetFileName(normalizedFullPath);
        Location = Path.GetDirectoryName(normalizedFullPath) ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="directoryInfo">The <see cref="DirectoryInfo"/> representing the directory.</param>
    public DirectoryDescriptor(DirectoryInfo directoryInfo) : this(directoryInfo?.FullName ?? throw new ArgumentNullException(nameof(directoryInfo)))
    {
    }

    /// <summary>
    /// Creates a new directory descriptor representing the current directory as a relative, unrooted base path.
    /// </summary>
    /// <remarks>This method is useful when a relative directory base is required without specifying an
    /// absolute or rooted path where rooted means relative to a drive. The resulting descriptor uses the path symbol <c>".\"</c>.</remarks>
    /// <returns>A <see cref="DirectoryDescriptor"/> instance initialized to the current directory using a relative, unrooted
    /// path symbol <c>".\"</c>.</returns>
    public static DirectoryDescriptor CreateRelativeUnrootedBase() => new($".{Path.DirectorySeparatorChar}");

    public DirectoryDescriptor Combine(FileDescriptor relativeFilePath = default, params DirectoryDescriptor[] appendingLocationSegments)
    {
        ArgumentExceptionAdvanced.ThrowIfAny(appendingLocationSegments, item => item == default);
        if (relativeFilePath != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");
        }

        IEnumerable<string> pathSegments = appendingLocationSegments
            .Select(segment => segment.FullPath)
            .Prepend(FullPath);
        if (relativeFilePath != default)
        {
            pathSegments = pathSegments.Concat([relativeFilePath.FullPath]);
        }

        string combinedPath = pathSegments.JoinToString(Path.DirectorySeparatorChar.ToString());
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(combinedPath);

        return new DirectoryDescriptor(combinedPath);
    }

    /// <summary>
    /// Converts the current directory path to an absolute path using the specified absolute base directory.
    /// </summary>
    /// <remarks>The base directory <paramref name="absoluteBaseDirectory"/> must be absolute. If the current path is already absolute, the base
    /// directory is ignored. This method does not support resolving relative paths with explicit drive roots, as the
    /// current drive context is required. And it replaces implicit drive roots with the provided base directory. For example, if the current path is "\Temp" and the base directory is "C:\Base", the resulting absolute path will be "C:\Base\Temp".</remarks>
    /// <param name="absoluteBaseDirectory">An absolute directory path that serves as the base for resolving the current relative directory path.</param>
    /// <returns>A new DirectoryDescriptor representing the absolute path resolved from the current path and the specified base
    /// directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current path has an explicit drive root but is relative, making it impossible to resolve to an
    /// absolute path using the provided base directory.</exception>
    public DirectoryDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");

        if (HasExplicitDriveRoot && IsRelative)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{FullPath}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        return new(Path.Combine(absoluteBaseDirectory.FullPath, FullPath));
    }

    public override string ToString() => FullPath;
    public bool Equals(DirectoryDescriptor other) => s_pathEqualityComparer.Equals(this, other);
    public override int GetHashCode() => s_pathEqualityComparer.GetHashCode(this);

    public string Name { get; }

    // The parent directory path without the name. Can be absolute or relative.
    public string Location { get; }
    public string FullPath { get; }

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
    /// Gets a value indicating whether the directory at the specified path currently exists.
    /// </summary>
    /// <value><see langword="true"/> if the directory exists or <see langword="false"/> an error occurred during the check or the directory does not exist at the time of the check.</value>
    public bool IsExisting => Directory.Exists(FullPath);

    public static bool operator ==(DirectoryDescriptor left, DirectoryDescriptor right) => left.Equals(right);
    public static bool operator !=(DirectoryDescriptor left, DirectoryDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is DirectoryDescriptor other && Equals(other);

    private static bool IsSpecialDirectorySymbol(string value) => value is "." or "..";
}
