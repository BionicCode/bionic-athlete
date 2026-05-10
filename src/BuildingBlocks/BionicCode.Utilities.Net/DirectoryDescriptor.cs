namespace BionicCode.Utilities.Net;

using System.Collections.Frozen;
using System.Diagnostics;

/// <summary>
/// Describes a directory that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("DirectoryName = {Name}, Location = {Location}, IsRelative = {IsRelative}")]
public readonly struct DirectoryDescriptor : IEquatable<DirectoryDescriptor>
{
    /// <summary>
    /// Represents the symbol used to refer to the current directory in file system paths.
    /// </summary>
    /// <remarks>The value combines a period (.) with the platform-specific directory separator character.
    /// This symbol is commonly used in file system operations to indicate the current working directory.</remarks>
    /// <value>The string representing the current directory symbol. Usually <c>.</c> followed by the platform-specific directory separator character, for example <c>.\</c> on Windows.</value>
    public static readonly string CurrentDirectorySymbol = $".{Path.DirectorySeparatorChar}";

    /// <summary>
    /// Represents the symbol used to refer to the parent directory, including the platform-specific directory separator
    /// character.
    /// </summary>
    /// <remarks>This value combines two dots (..) with the appropriate directory separator for the current
    /// operating system. It can be used when constructing or parsing file system paths that reference a parent
    /// directory.</remarks>
    /// <value>The string representing the parent directory symbol. Usually <c>..</c> followed by the platform-specific directory separator character, for example <c>..\</c> on Windows.</value>
    public static readonly string ParentDirectorySymbol = $"..{Path.DirectorySeparatorChar}";

    public static readonly FrozenSet<string> SpecialDirectorySymbols = FrozenSet.Create(CurrentDirectorySymbol, ParentDirectorySymbol, ".", "..");

    /// <summary>
    /// Platform-specific special directory path symbols for true relative (unrooted) paths.
    /// </summary>
    /// <remarks>These symbols represent the current and parent directory in a relative, unrooted context. They are used to identify paths that are explicitly relative without an implicit or explicit drive root. For example, a path like <c>.\Temp</c> is considered a relative, unrooted path because it starts with the current directory symbol and does not have an explicit drive root.
    /// <para/>The set contains:
    /// <list type="bullet">
    /// <item><c>.\</c> on Windows or <c>./</c> on Unix-like systems and Windows. 
    /// <br/>This symbol has the same meaning as completely omitting any symbol and only provide the directory name: <c>./temp</c> and <c>temp</c> are equivalent.</item>
    /// <item><c>..\</c> on Windows or <c>../</c> on Unix-like systems and Windows</item>
    /// </list>
    /// </remarks>
    public static readonly FrozenDictionary<string, string> SpecialRelativeUnrootedDirectorySymbolSet = FrozenDictionary.Create(
        new KeyValuePair<string, string>(CurrentDirectorySymbol, CurrentDirectorySymbol),
        new KeyValuePair<string, string>(".", CurrentDirectorySymbol),
        new KeyValuePair<string, string>(ParentDirectorySymbol, ParentDirectorySymbol),
        new KeyValuePair<string, string>("..", ParentDirectorySymbol));

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

        if (IsSpecialDirectorySymbol(normalizedFullPath)
            && SpecialRelativeUnrootedDirectorySymbolSet.TryGetValue(normalizedFullPath, out string? symbol))
        {
            // Special directory symbols like "." and ".." are treated as relative paths with the symbol as the full path (e.g. "./" or "../")
            // and the location as the symbol "." or ".." respectively,
            // while the name is empty since they do not represent a specific directory name but rather a relative reference to the current or parent directory.
            Name = string.Empty;
            FullPath = symbol;
            Location = normalizedFullPath;
            IsRelative = true;

            return;
        }

        FullPath = normalizedFullPath;
        IsRelative = !Path.IsPathFullyQualified(FullPath);
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
    /// Creates a new <see cref="DirectoryDescriptor"/> representing the current directory as a relative, unrooted base path.
    /// </summary>
    /// <param name="directoryPathWithoutLeadingSeparator">An optional relative directory path to append to the current directory symbol. 
    /// <br/>Must not be a rooted path. For example, <c>subdirOfCurrent/subdir</c> or <c>./subdirOfCurrent/subdir</c> are valid unrooted relative paths. 
    /// <br/>On the contrary, <c>/subdir/subdir2</c> or <c>c:subdir/subdir2</c> are invalid 
    /// since they symbolize a rooted relative path (relative to current working directory or a drive).</param>
    /// <remarks>This method is useful when a relative directory base is required without specifying an
    /// absolute or rooted path where rooted means relative to a drive. The resulting descriptor uses the path symbol <c>"./"</c> unless the provided <paramref name="directoryPathWithoutLeadingSeparator"/> overrides it.
    /// <para/>For example, if <paramref name="directoryPathWithoutLeadingSeparator"/> is <c>"subdir"</c> or <c>"./subdir"</c>, the resulting descriptor will represent <c>"./subdir"</c>.
    /// And if <paramref name="directoryPathWithoutLeadingSeparator"/> is <see langword="null"/> or empty or only consists of whitespace, the resulting descriptor will represent <c>"./"</c> (which is the default behavior when no path is provided).
    /// <br/>And if <paramref name="directoryPathWithoutLeadingSeparator"/> is <c>"../subdir"</c>, the resulting descriptor will represent <c>"../subdir"</c> as well.</remarks>
    /// <returns>A <see cref="DirectoryDescriptor"/> instance initialized to the current directory using a relative, unrooted
    /// path symbol <c>"./"</c> joined with the specified relative path if provided.</returns>
    public static DirectoryDescriptor CreateRelativeToCurrentDirectory(string? directoryPathWithoutLeadingSeparator = null)
    {
        if (!string.IsNullOrWhiteSpace(directoryPathWithoutLeadingSeparator))
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(
                Path.IsPathRooted(directoryPathWithoutLeadingSeparator),
                $"Invalid argument {nameof(directoryPathWithoutLeadingSeparator)}. The path '{directoryPathWithoutLeadingSeparator}' must not be rooted.");
            FileSystemPathValidator.ThrowIfInvalidDirectoryPath(directoryPathWithoutLeadingSeparator);

            return directoryPathWithoutLeadingSeparator switch
            {
                _ when directoryPathWithoutLeadingSeparator.StartsWith(CurrentDirectorySymbol, StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),
                _ when directoryPathWithoutLeadingSeparator.StartsWith(ParentDirectorySymbol, StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),
                _ => new($"{CurrentDirectorySymbol}{directoryPathWithoutLeadingSeparator}")
            };
        }

        return new(CurrentDirectorySymbol);
    }

    /// <summary>
    /// Creates a new <see cref="DirectoryDescriptor"/> representing a directory relative to its parent directory by the specified
    /// number of levels, joined with an optional relative path.
    /// </summary>
    /// <param name="levels">The number of parent directory levels to traverse. Must be greater than or equal to 1. Defaults to 1.</param>
    /// <param name="directoryPathWithoutLeadingSeparator">An optional relative directory path to append after traversing the parent directories. Must not be a rooted
    /// path. For example, <c>../subdirOfParentOrSiblingOfCurrent/subdir</c> or <c>subdirOfCurrent/subdir</c> or <c>./subdirOfCurrent/subdir</c> are all valid unrooted relative paths. On the contrary, <c>/subdir/subdir2</c> or <c>c:subdir/subdir2</c> are invalid since they symbolize a rooted relative path (relative to current working directory or a drive).
    /// <para/>If <see langword="null"/> or empty, only the parent directory traversal is used.</param>
    /// <remarks>The resulting descriptor uses the path symbol <c>..</c> for each parent directory level and joins them with the specified relative path.
    /// <para/>For example, <paramref name="levels"/> = 2 and <paramref name="directoryPathWithoutLeadingSeparator"/> = "subdir" would result in a path like <c>../../subdir</c>.
    /// <br/>And <paramref name="levels"/> = 1 and <paramref name="directoryPathWithoutLeadingSeparator"/> = <see langword="null"/> would result in a path like <c>../</c>.
    /// <br/>And <paramref name="levels"/> = 2 and <paramref name="directoryPathWithoutLeadingSeparator"/> = "./subdir/subdir2" would result in a path like <c>../../subdir/subdir2</c>.</remarks>
    /// <returns>A <see cref="DirectoryDescriptor"/> representing an unrooted relative directory path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="levels"/> is less than 1.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPathWithoutLeadingSeparator"/> is a rooted path.</exception>
    public static DirectoryDescriptor CreateRelativeToParentDirectory(int levels = 1, string? directoryPathWithoutLeadingSeparator = null)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(levels, 1);
        using PooledStringBuilder pathBuilder = StringBuilderFactory.GetOrCreate()
            .Append(ParentDirectorySymbol, 0, levels);

        if (!string.IsNullOrEmpty(directoryPathWithoutLeadingSeparator))
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(Path.IsPathRooted(directoryPathWithoutLeadingSeparator), $"The argument '{nameof(directoryPathWithoutLeadingSeparator)}' must not be a rooted path.");
            FileSystemPathValidator.ThrowIfInvalidDirectoryPath(directoryPathWithoutLeadingSeparator);

            _ = pathBuilder
                .Append(Path.DirectorySeparatorChar)
                .Append(directoryPathWithoutLeadingSeparator.TrimStart(CurrentDirectorySymbol));
        }

        return new(pathBuilder.ToString());
    }

    /// <summary>
    /// Combines the current directory path with one or more relative directory segments, 
    /// returning a new <see cref="DirectoryDescriptor"/> representing the resulting path.
    /// </summary>
    /// <remarks>All directory segments in <paramref name="appendingLocationSegments"/> must be relative and must not have an
    /// explicit drive root. If <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>, segments with an implicit drive root (e.g. <c>/subdir</c>) are not permitted.
    /// The method resolves special path symbols such as ".." and "." when combining paths.</remarks>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow directory segments that are implicitly drive rooted; otherwise, <see langword="false"/>. If <see langword="false"/>, any segment with
    /// an implicit drive root will cause an exception.</param>
    /// <param name="appendingLocationSegments">An array of relative directory segments to append to the current directory path. Each segment must be a relative
    /// directory path without an explicit drive root.</param>
    /// <returns>A new <see cref="DirectoryDescriptor"/> representing the combined path.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the following conditions are met: 
    /// a segment is not relative, 
    /// a segment has an explicit drive root, 
    /// or a segment is implicitly drive rooted when <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>.</exception>
    public DirectoryDescriptor Combine(bool isImplicitRootAllowed = false, params DirectoryDescriptor[] appendingLocationSegments) => Combine(default, isImplicitRootAllowed, appendingLocationSegments);

    /// <summary>
    /// Combines the current directory path with one or more relative directory segments and an optional relative file
    /// path, returning a new <see cref="DirectoryDescriptor"/> representing the resulting path.
    /// </summary>
    /// <remarks>All directory segments in <paramref name="appendingLocationSegments"/> must be relative and must not have an
    /// explicit drive root. If <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>, segments with an implicit drive root (e.g. <c>/subdir</c>) are not permitted.
    /// The method resolves special path symbols such as ".." and "." when combining paths.</remarks>
    /// <param name="relativeFilePath">A relative file path to append to the combined directory path. Must be relative or set to the default value to
    /// omit.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow directory segments that are implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <param name="appendingLocationSegments">An array of relative directory segments to append to the current directory path. Each segment must be a relative
    /// directory path without an explicit drive root.</param>
    /// <returns>A new <see cref="DirectoryDescriptor"/> representing the combined path.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the following conditions are met: 
    /// a segment is not relative, 
    /// a segment has an explicit drive root, 
    /// a segment is implicitly drive rooted when <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>
    /// or <paramref name="relativeFilePath"/> is not relative.</exception>
    public DirectoryDescriptor Combine(FileDescriptor relativeFilePath, bool isImplicitRootAllowed = false, params DirectoryDescriptor[] appendingLocationSegments)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(appendingLocationSegments);

        if (relativeFilePath != default)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");
        }

        string basePath = FullPath;
        foreach (DirectoryDescriptor segment in appendingLocationSegments)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(segment.IsRelative, $"All '{nameof(appendingLocationSegments)}' must be relative directory paths. The segment '{segment.FullPath}' is not relative.");
            ArgumentExceptionAdvanced.ThrowIfTrue(segment.HasExplicitDriveRoot, $"All '{nameof(appendingLocationSegments)}' must not have an explicit drive root. The segment '{segment.FullPath}' has an explicit drive root.");

            string segmentPath = segment.FullPath;
            if (segment.HasImplicitDriveRoot)
            {
                if (!isImplicitRootAllowed)
                {
                    ArgumentExceptionAdvanced.ThrowIfTrue(isImplicitRootAllowed, $"The segment '{segment.FullPath}' is implicitly drive rooted. The argument '{nameof(isImplicitRootAllowed)}' must be set to TRUE to allow implicit drive rooted segments.");
                }

                // Remove the leading directory separator to prevent it from being treated as a rooted path segment in the combined path.
                segmentPath = segmentPath[1..];
            }

            // Use Path.GetFullPath to resolve special path symbols like "../" and "./"
            basePath = Path.GetFullPath(segmentPath, basePath);
        }

        if (relativeFilePath != default)
        {
            basePath = Path.GetFullPath(relativeFilePath.FullPath, basePath);
        }

        return new DirectoryDescriptor(basePath);
    }

    /// <summary>
    /// Converts the current directory path to an absolute path using the specified absolute base directory.
    /// </summary>
    /// <remarks>The base directory <paramref name="absoluteBaseDirectory"/> must be absolute. If the current path is already absolute, the base
    /// directory is ignored. This method does not support resolving relative paths with explicit drive roots, as the
    /// current drive context is required. And it replaces implicit drive roots with the provided base directory. For example, if the current path is "\Temp" and the base directory is "C:\Base", the resulting absolute path will be "C:\Base\Temp".</remarks>
    /// <param name="absoluteBaseDirectory">An absolute directory path that serves as the base for resolving the current relative directory path.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow directory segments that are implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <returns>A new DirectoryDescriptor representing the absolute path resolved from the current path and the specified base
    /// directory.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="absoluteBaseDirectory"/> represents a relative path.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the current path has an explicit drive root but is relative, making it impossible to resolve to an absolute path using the provided base directory.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="absoluteBaseDirectory"/> is <see langword="default"/>.</exception>
    public DirectoryDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory, bool isImplicitRootAllowed = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");

        if (HasExplicitDriveRoot && IsRelative)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{FullPath}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        return Combine(isImplicitRootAllowed, absoluteBaseDirectory);
    }

    /// <summary>
    /// Converts the current directory path to an absolute path using the specified absolute base directory.
    /// </summary>
    /// <remarks>The base directory <paramref name="absoluteBaseDirectory"/> must be absolute. If the current path is already absolute, the base
    /// directory is ignored. This method does not support resolving relative paths with explicit drive roots, as the
    /// current drive context is required. And it replaces implicit drive roots with the provided base directory. For example, if the current path is "\Temp" and the base directory is "C:\Base", the resulting absolute path will be "C:\Base\Temp".</remarks>
    /// <param name="absoluteBaseDirectory">An absolute directory path that serves as the base for resolving the current relative directory path.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow directory segments that are implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <param name="relativeFilePath">A relative file path to append to the combined directory path. Must be relative or set to the default value to
    /// omit.</param>
    /// <returns>A new DirectoryDescriptor representing the absolute path resolved from the current path and the specified base
    /// directory.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="absoluteBaseDirectory"/> represents a relative path or <paramref name="relativeFilePath"/> is not relative.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the current path has an explicit drive root but is relative, making it impossible to resolve to an absolute path using the provided base directory.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="absoluteBaseDirectory"/> is <see langword="default"/>.</exception>
    public DirectoryDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory, FileDescriptor relativeFilePath, bool isImplicitRootAllowed = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");
        ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");

        if (HasExplicitDriveRoot && IsRelative)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{FullPath}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        return Combine(relativeFilePath, isImplicitRootAllowed, absoluteBaseDirectory);
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
    /// <remarks>In general, relative paths are interpreted as relative to a current working directory (e.g. <c>/subdir</c>) or relative to the current drive (e.g. <c>c:subdir</c>) or truly relative to an arbitrary base directory (e.g. <c>subdir</c> or <c>./subdir</c> or <c>../subdir</c>). Absolute paths specify a complete path from the root of the file system and are not dependent on the current working directory or current drive.</remarks>
    /// <value><see langword="true"/> if the path is relative (rooted or unrooted) or <see langword="false"/> if the path is absolute.</value>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets a value indicating whether the directory has an explicit drive root.
    /// </summary>
    /// <remarks>A directory has an explicit drive root if it is an absolute path or a relative path with an explicit root like "C:Temp".
    /// <br/>The property will treat paths like "/Temp" as implicitly drive rooted.</remarks>
    /// <value><see langword="true"/> if the directory has an explicit drive root like "C:Temp" or is an absolute path like "C:\User\Temp"; otherwise, <see langword="false"/>.</value>
    public bool HasExplicitDriveRoot => Path.IsPathRooted(FullPath) && (Path.GetPathRoot(FullPath)?.Length ?? 0) > 1;

    /// <summary>
    /// Gets a value indicating whether the path has an implicit drive root (for example, <c>/subdir</c>).
    /// </summary>
    /// <remarks>An implicit drive root is present when the path is rooted and its root consists of a single
    /// character, typically representing a directory separator, for example <c>/subdir</c>.</remarks>
    public bool HasImplicitDriveRoot => Path.IsPathRooted(FullPath) && (Path.GetPathRoot(FullPath)?.Length ?? 0) == 1;

    /// <summary>
    /// Gets a value indicating whether the directory at the specified path currently exists.
    /// </summary>
    /// <value><see langword="true"/> if the directory exists or <see langword="false"/> an error occurred during the check or the directory does not exist at the time of the check.</value>
    public bool IsExisting => Directory.Exists(FullPath);

    public static bool operator ==(DirectoryDescriptor left, DirectoryDescriptor right) => left.Equals(right);
    public static bool operator !=(DirectoryDescriptor left, DirectoryDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is DirectoryDescriptor other && Equals(other);

    private static bool IsSpecialDirectorySymbol(string value) => SpecialRelativeUnrootedDirectorySymbolSet.ContainsKey(value);
}
