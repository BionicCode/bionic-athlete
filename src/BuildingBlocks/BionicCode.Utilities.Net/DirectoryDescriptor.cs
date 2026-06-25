namespace BionicCode.Utilities.Net;

using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SystemIoPath = System.IO.Path;

/// <summary>
/// Describes a directory that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("DirectoryName = {Name}, Location = {Location}, IsRelative = {IsRelative}")]
public readonly struct DirectoryDescriptor : IEquatable<DirectoryDescriptor>
{
    /// <summary>
    /// Represents the symbol used to refer to the current directory in file system paths.
    /// </summary>
    /// <remarks>The value is a period (.) without the platform-specific directory separator character.
    /// This symbol is commonly used in file system operations to indicate the current working directory.</remarks>
    /// <value>The string representing the current directory symbol. Usually <c>.</c>.</value>
    public static readonly string CurrentDirectorySymbol = ".";

    /// <summary>
    /// Represents the symbol used to refer to the parent directory, including the platform-specific directory separator
    /// character.
    /// </summary>
    /// <remarks>This value is two dots (..) without the platform-specific directory separator character.
    /// It can be used when constructing or parsing file system paths that reference a parent directory.</remarks>
    /// <value>The string representing the parent directory symbol. Usually <c>..</c>.</value>
    public static readonly string ParentDirectorySymbol = "..";

    /// <summary>
    /// Represents the separator character used to separate embedded file names from their parent directory names in a path string.
    /// </summary>
    /// <value>The string representing the embedded file name separator. Usually <c>.</c> e.g., <c>/resourcesfile.txt</c>.</value>
    public static readonly string EmbeddedFileNameSeparator = ".";

    public static readonly string UncRootDirectorySymbol = @"\\";

    public static readonly FrozenSet<string> SpecialDirectorySymbols = FrozenSet.Create(CurrentDirectorySymbol, ParentDirectorySymbol, ".", "..");
    public static readonly FrozenSet<char> DirectorySeparatorChars = FrozenSet.Create(SystemIoPath.DirectorySeparatorChar, SystemIoPath.AltDirectorySeparatorChar);
    public static readonly FrozenSet<string> DirectorySeparatorStrings = DirectorySeparatorChars.Select(c => c.ToString()).ToFrozenSet();

    /// <summary>
    /// Platform-specific special directory path symbols for true relative (unrooted) paths.
    /// </summary>
    /// <remarks>These symbols represent the current and parent directory in a relative, unrooted context. 
    /// They are used to identify paths that are explicitly relative without an implicit or explicit drive root. 
    /// For example, a path like <c>.\Temp</c> is considered a relative, unrooted path because it starts with the current directory symbol 
    /// and does not have an explicit drive root.
    /// <para/>The set contains:
    /// <list type="bullet">
    /// <item><c>.</c> - This symbol has the same meaning as completely omitting any symbol and only provide the directory name: <c>./temp</c> and <c>temp</c> are equivalent.</item>
    /// <item><c>..</c> - This symbol represents the parent directory in a relative, unrooted context.</item>
    /// </list>
    /// </remarks>
    public static readonly FrozenDictionary<string, string> SpecialRelativeUnrootedDirectorySymbolNormalizationTable = FrozenDictionary.Create(
        new KeyValuePair<string, string>(CurrentDirectorySymbol, CurrentDirectorySymbol),
        new KeyValuePair<string, string>(".", CurrentDirectorySymbol),
        new KeyValuePair<string, string>(ParentDirectorySymbol, ParentDirectorySymbol),
        new KeyValuePair<string, string>("..", ParentDirectorySymbol));

    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    public static DirectoryDescriptor Empty { get; } = default(DirectoryDescriptor) with { IsEmpty = true };

    private readonly WriteOnce<PathDescriptor> _path;
    private readonly WriteOnce<DirectoryDescriptor> _location;
    private readonly WriteOnce<string> _name;
    private readonly WriteOnce<bool> _isEmpty;

    // Create a synthetic absolute path by combining the relative base path with a fixed synthetic root.
    // This allows us to resolve the relative path against the base path using Path.GetFullPath() (which only works with absolute paths).
    // We later remove the synthetic root to get the final relative path result. 
    // Note: The synthetic path is virtual or lexical. It must not exists since Path.GetFullPath() does not check for the existence of the path.
    // Uses forward slash to ensure platform agnostic behavior.
    private static readonly string s_syntheticRoot = OperatingSystem.IsWindows()
        ? @$"C:{SystemIoPath.DirectorySeparatorChar}__synthetic_path_root__"
        : @$"{SystemIoPath.DirectorySeparatorChar}__synthetic_path_root__";

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="name">The directory name.</param>
    /// <param name="location">The parent directory path without the name. Can be absolute or relative.</param>
    public DirectoryDescriptor(string name, string location) : this(SystemIoPath.Join(location, name))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="fullPath">The full path of the directory. Can be absolute or relative.</param>
    public DirectoryDescriptor(string fullPath)
    {
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(fullPath);

        _name = new WriteOnce<string>();
        _location = new WriteOnce<DirectoryDescriptor>();
        _isEmpty = false;

        _path = new PathDescriptor(fullPath, PathKind.Directory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDescriptor"/> struct from a directory name and location.
    /// </summary>
    /// <param name="directoryInfo">The <see cref="DirectoryInfo"/> representing the directory.</param>
    public DirectoryDescriptor(DirectoryInfo directoryInfo) : this(directoryInfo?.FullName ?? throw new ArgumentNullException(nameof(directoryInfo)))
    {
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
    /// <param name="appendingLocationSegments">An array of relative <see cref="DirectoryDescriptor"/> directory segments to append to the current directory path. Each segment must be a relative
    /// directory path without an explicit drive root.</param>
    /// <returns>A new <see cref="DirectoryDescriptor"/> representing the combined path.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the following conditions are met: 
    /// a segment is not relative, 
    /// a segment has an explicit drive root, 
    /// or a segment is implicitly drive rooted when <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>.</exception>
    public DirectoryDescriptor Combine(bool isImplicitRootAllowed = false, params DirectoryDescriptor[] appendingLocationSegments)
    {
        string fullPath = CombineInternal(null, appendingLocationSegments.OrEmpty(), isImplicitRootAllowed);
        return new(fullPath);
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
    /// <param name="appendingLocationSegments">An <see cref="IEnumerable"/>&lt;<see cref="DirectoryDescriptor"/>&gt; of relative directory segments to append to the current directory path. Each segment must be a relative
    /// directory path without an explicit drive root.</param>
    /// <returns>A new <see cref="DirectoryDescriptor"/> representing the combined path.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the following conditions are met: 
    /// a segment is not relative, 
    /// a segment has an explicit drive root, 
    /// or a segment is implicitly drive rooted when <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>.</exception>
    public DirectoryDescriptor Combine(IEnumerable<DirectoryDescriptor> appendingLocationSegments, bool isImplicitRootAllowed = false)
    {
        string fullPath = CombineInternal(null, appendingLocationSegments.OrEmpty(), isImplicitRootAllowed);
        return new(fullPath);
    }

    /// <summary>
    /// Combines the current directory path with one or more relative directory segments and a relative file
    /// path, returning a new <see cref="FileSystemPathDescriptor"/> representing the resulting file path.
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
    public FileSystemPathDescriptor Combine(FileSystemPathDescriptor relativeFilePath, bool isImplicitRootAllowed = false, params DirectoryDescriptor[] appendingLocationSegments) => Combine(relativeFilePath, appendingLocationSegments, isImplicitRootAllowed);

    /// <summary>
    /// Combines the current directory path with one or more relative directory segments and a relative file
    /// path, returning a new <see cref="FileSystemPathDescriptor"/> representing the resulting file path.
    /// </summary>
    /// <remarks>All directory segments in <paramref name="appendingLocationSegments"/> must be relative and must not have an
    /// explicit drive root. If <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>, segments with an implicit drive root (e.g. <c>/subdir</c>) are not permitted.
    /// The method resolves special path symbols such as ".." and "." when combining paths.</remarks>
    /// <param name="relativeFilePath">A relative file path to append to the combined directory path. Must be relative or set to the default value to
    /// omit.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow directory segments that are implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <param name="appendingLocationSegments">An <see cref="IEnumerable"/>&lt;<see cref="DirectoryDescriptor"/>&gt; of relative directory segments to append to the current directory path. Each segment must be a relative
    /// directory path without an explicit drive root.</param>
    /// <returns>A new <see cref="DirectoryDescriptor"/> representing the combined path.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the following conditions are met: 
    /// a segment is not relative, 
    /// a segment has an explicit drive root, 
    /// a segment is implicitly drive rooted when <paramref name="isImplicitRootAllowed"/> is <see langword="false"/>
    /// or <paramref name="relativeFilePath"/> is not relative.</exception>
    public FileSystemPathDescriptor Combine(FileSystemPathDescriptor relativeFilePath, IEnumerable<DirectoryDescriptor> appendingLocationSegments, bool isImplicitRootAllowed = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(relativeFilePath);
        ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");
        ArgumentNullExceptionAdvanced.ThrowIfNull(appendingLocationSegments);

        string path = CombineInternal(relativeFilePath, appendingLocationSegments.OrEmpty(), isImplicitRootAllowed);

        return new FileSystemPathDescriptor(path);
    }

    private string CombineInternal(FileSystemPathDescriptor? relativeFilePath, IEnumerable<DirectoryDescriptor> appendingLocationSegments, bool isImplicitRootAllowed = false)
    {
        // Combine the current directory path with each of the provided relative directory segments in order. Each segment is validated to ensure it is a relative path
        // without an explicit drive root, and if implicit roots are not allowed, it must not be implicitly drive rooted.
        // Provided relative paths are resolved against the current base path (the current DirectoryDescriptor) to produce a final combined path
        // that correctly resolves special path symbols like ".." and ".". If the current DirectoryDescriptor is a relative path, the resulting path will be relative too.
        PathDescriptor combinedPath = Path;
        foreach (DirectoryDescriptor segment in appendingLocationSegments)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(segment.IsRelative, $"All '{nameof(appendingLocationSegments)}' must be relative directory paths. The segment '{segment.PathString}' is not relative.");
            ArgumentExceptionAdvanced.ThrowIfTrue(segment.HasExplicitDriveRoot, $"All '{nameof(appendingLocationSegments)}' must not have an explicit drive root. The segment '{segment.PathString}' has an explicit drive root.");

            PathSegmentList segmentPath = segment.Path.NormalizedPath.Segments;
            if (segment.HasImplicitDriveRoot)
            {
                ArgumentExceptionAdvanced.ThrowIfFalse(isImplicitRootAllowed, $"The segment '{segment.PathString}' is implicitly drive rooted. The argument '{nameof(isImplicitRootAllowed)}' must be set to TRUE to allow implicit drive rooted segments.");

                // Remove the leading directory separator to prevent it from being treated as a rooted path segment in the combined path.
                segmentPath = segmentPath[1..];
            }

            try
            {
                combinedPath = ResolveRelativePath(combinedPath.NormalizedPath.Segments, segmentPath);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(
                    $"Invalid argument '{appendingLocationSegments}'. The provided relative directory path is invalid and exceeded the path depth of the current '{nameof(DirectoryDescriptor)}.{nameof(PathString)}' value by traversing too many parent directories.",
                    ex);
            }
        }

        if (relativeFilePath is null)
        {
            return combinedPath;
        }

        PathDescriptor combinedFilePath = ResolveRelativePath(combinedPath.Segments, relativeFilePath.Path.Segments);
        return combinedFilePath;
    }

    /// <summary>
    /// Converts the current directory path to an absolute path using the specified absolute base directory.
    /// </summary>
    /// <remarks>The base directory <paramref name="absoluteBaseDirectory"/> must be absolute. If the current path is already absolute, the base
    /// directory is ignored. This method does not support resolving relative paths with explicit drive roots, as the
    /// current drive context is required. And it replaces implicit drive roots with the provided base directory. For example, if the current path is "\Temp" and the base directory is "C:\Base", the resulting absolute path will be "C:\Base\Temp".</remarks>
    /// <param name="absoluteBaseDirectory">An absolute directory path that serves as the base for resolving the current relative directory path.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow the current directory to be implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <returns>A new DirectoryDescriptor representing the absolute path resolved from the current path and the specified base
    /// directory.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="absoluteBaseDirectory"/> represents a relative path.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the current path has an explicit drive root but is relative, making it impossible to resolve to an absolute path using the provided base directory.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="absoluteBaseDirectory"/> is <see langword="default"/>.</exception>
    public DirectoryDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory, bool isImplicitRootAllowed = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(
            absoluteBaseDirectory.IsRelative,
            $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");

        if (IsDefaultInstance
            || IsEmpty)
        {
            return DirectoryDescriptor.Empty;
        }

        // If the current path is already absolute we can return it as is without combining with the base directory.
        if (!IsRelative)
        {
            return this;
        }

        if (HasExplicitDriveRoot)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{PathString}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        PathSegmentList currentRelativePath = Path.NormalizedPath.Segments;
        if (HasImplicitDriveRoot)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(isImplicitRootAllowed, $"The current path '{PathString}' is implicitly drive rooted. The argument '{nameof(isImplicitRootAllowed)}' must be set to TRUE to allow implicit drive rooted paths.");

            // If implicit rooted paths are allowed and the current path starts with a directory separator, we treat it as implicitly rooted and remove the leading separator to combine it with the base directory.
            currentRelativePath = currentRelativePath[1..];
        }

        return new(ResolveRelativePath(absoluteBaseDirectory.Path.NormalizedPath.Segments, currentRelativePath));
    }

    /// <summary>
    /// Converts the current directory path to an absolute path using the specified absolute base directory.
    /// </summary>
    /// <remarks>The base directory <paramref name="absoluteBaseDirectory"/> must be absolute. If the current path is already absolute, the base
    /// directory is ignored. This method does not support resolving relative paths with explicit drive roots, as the
    /// current drive context is required. And it replaces implicit drive roots with the provided base directory.
    /// For example, if the current path is "\Temp" and the base directory is "C:\Base", the resulting absolute path will be "C:\Base\Temp" with the <paramref name="relativeFilePath"/> being appended.</remarks>
    /// <param name="absoluteBaseDirectory">An absolute directory path that serves as the base for resolving the current relative directory path.</param>
    /// <param name="isImplicitRootAllowed"><see langword="true"/> to allow the current directory to be implicitly drive rooted. In that case <c>/subdir</c> will be treated as <c>./subdir</c> (aka <c>subdir</c>); otherwise, <see langword="false"/> to disallow conversion of implicit rooted paths. 
    /// If <see langword="false"/>, any segment with an implicit drive root will cause an exception.</param>
    /// <param name="relativeFilePath">A relative file path to append to the combined directory path. Must be relative or set to the default value to
    /// omit.</param>
    /// <returns>A new DirectoryDescriptor representing the absolute path resolved from the current path and the specified base
    /// directory.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="absoluteBaseDirectory"/> represents a relative path or <paramref name="relativeFilePath"/> is not relative.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the current path has an explicit drive root but is relative, making it impossible to resolve to an absolute path using the provided base directory.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="absoluteBaseDirectory"/> is <see langword="default"/>.</exception>
    public FileSystemPathDescriptor ToAbsolutePath(DirectoryDescriptor absoluteBaseDirectory, FileSystemPathDescriptor relativeFilePath, bool isImplicitRootAllowed = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(relativeFilePath);
        ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");

        if (IsDefaultInstance
            || IsEmpty)
        {
            return FileSystemPathDescriptor.Empty;
        }

        // If the current path is already absolute we can return it as is without combining with the base directory.
        if (!IsRelative)
        {
            return ResolveRelativePath(this, relativeFilePath);
        }

        ArgumentNullExceptionAdvanced.ThrowIfDefault(absoluteBaseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(absoluteBaseDirectory.IsRelative, $"The argument '{nameof(absoluteBaseDirectory)}' must be an absolute directory path.");

        if (HasExplicitDriveRoot)
        {
            // If the current path has an explicit drive root but is relative, we cannot resolve it to an absolute path without knowing the current drive. Therefore, we throw an exception in this case.
            throw new InvalidOperationException($"Cannot convert to an absolute path because the current path '{PathString}' has an explicit drive root but is relative. An absolute base directory cannot be used to resolve this path.");
        }

        DirectoryDescriptor absolutePathDescriptor = ToAbsolutePath(absoluteBaseDirectory, isImplicitRootAllowed);
        return ResolveRelativePath(absolutePathDescriptor, relativeFilePath);
    }

    #region Helpers
    /// <summary>
    /// Factory method that creates a new <see cref="DirectoryDescriptor"/> representing the current directory as a relative, unrooted base path.
    /// </summary>
    /// <param name="directoryPathWithoutLeadingSeparator">An optional unrooted relative directory path to append to the current directory symbol. 
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
    public static DirectoryDescriptor CreateDirectoryRelativeToCurrent(string? directoryPathWithoutLeadingSeparator = null)
    {
        if (!string.IsNullOrWhiteSpace(directoryPathWithoutLeadingSeparator))
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(
                SystemIoPath.IsPathRooted(directoryPathWithoutLeadingSeparator),
                $"Invalid argument {nameof(directoryPathWithoutLeadingSeparator)}. The path '{directoryPathWithoutLeadingSeparator}' must not be rooted.");
            FileSystemPathValidator.ThrowIfInvalidDirectoryPath(directoryPathWithoutLeadingSeparator);

            return directoryPathWithoutLeadingSeparator switch
            {
                // Is already realtive to current e.g. ".\subdir"
                _ when directoryPathWithoutLeadingSeparator.StartsWith($"{CurrentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}", StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),

                // Is already realtive to current e.g. "./subdir"
                _ when directoryPathWithoutLeadingSeparator.StartsWith($"{CurrentDirectorySymbol}{SystemIoPath.AltDirectorySeparatorChar}", StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),

                // Is already realtive to current e.g. "..\subdir"
                _ when directoryPathWithoutLeadingSeparator.StartsWith($"{ParentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}", StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),

                // Is already realtive to current e.g. "../subdir"
                _ when directoryPathWithoutLeadingSeparator.StartsWith($"{ParentDirectorySymbol}{SystemIoPath.AltDirectorySeparatorChar}", StringComparison.Ordinal) => new(directoryPathWithoutLeadingSeparator),

                // Is current directory symbol e.g. "."
                _ when directoryPathWithoutLeadingSeparator.Equals(CurrentDirectorySymbol, StringComparison.Ordinal) => new($"{CurrentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}"),

                // Is parent directory symbol e.g. ".."
                _ when directoryPathWithoutLeadingSeparator.Equals(ParentDirectorySymbol, StringComparison.Ordinal) => new($"{ParentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}"),

                // Is potentially a file starting with a dot e.g. ".gitignore"
                _ when directoryPathWithoutLeadingSeparator.StartsWith(CurrentDirectorySymbol, StringComparison.Ordinal)
                    && directoryPathWithoutLeadingSeparator.Length > 1 => new($"{CurrentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}{directoryPathWithoutLeadingSeparator}"),

                // Empty or whitespace only, which defaults to current directory, or a true relative path e.g. "subdir"
                _ => string.IsNullOrWhiteSpace(directoryPathWithoutLeadingSeparator)
                    ? new($"{CurrentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}")
                    : new(SystemIoPath.Join(CurrentDirectorySymbol, directoryPathWithoutLeadingSeparator))
            };
        }

        return new(CurrentDirectorySymbol);
    }

    /// <summary>
    /// Factory method that creates a new <see cref="DirectoryDescriptor"/> representing a directory relative to its parent directory by the specified
    /// number of levels, joined with an optional relative path.
    /// </summary>
    /// <param name="levels">The number of parent directory levels to traverse. Must be greater than or equal to 1. Defaults to 1.</param>
    /// <param name="directoryPathWithoutLeadingSeparator">An optional unrooted relative directory path to append after traversing the parent directories. Must not be a rooted
    /// path. For example, <c>../subdirOfParentOrSiblingOfCurrent/subdir</c> or <c>subdirOfCurrent/subdir</c> or <c>./subdirOfCurrent/subdir</c> are all valid unrooted relative paths. 
    /// On the contrary, <c>/subdir/subdir2</c> or <c>c:subdir/subdir2</c> are invalid since they symbolize a rooted relative path (relative to current working directory or a drive).
    /// <para/>If <see langword="null"/> or empty, only the parent directory traversal is used.</param>
    /// <remarks>The resulting descriptor uses the path symbol <c>..</c> for each parent directory level and joins them with the specified relative path.
    /// <para/>For example, <paramref name="levels"/> = 2 and <paramref name="directoryPathWithoutLeadingSeparator"/> = "subdir" would result in a path like <c>../../subdir</c>.
    /// <br/>And <paramref name="levels"/> = 1 and <paramref name="directoryPathWithoutLeadingSeparator"/> = <see langword="null"/> would result in a path like <c>../</c>.
    /// <br/>And <paramref name="levels"/> = 2 and <paramref name="directoryPathWithoutLeadingSeparator"/> = "./subdir/subdir2" would result in a path like <c>../../subdir/subdir2</c>.</remarks>
    /// <returns>A <see cref="DirectoryDescriptor"/> representing an unrooted relative directory path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="levels"/> is less than 1.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPathWithoutLeadingSeparator"/> is a rooted path.</exception>
    public static DirectoryDescriptor CreateDirectoryRelativeToParent(int levels = 1, string? directoryPathWithoutLeadingSeparator = null)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(levels, 1);
        using PooledStringBuilder pathBuilder = StringBuilderFactory.GetOrCreate()
            .Append($"{ParentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}", levels);

        if (!string.IsNullOrEmpty(directoryPathWithoutLeadingSeparator))
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(SystemIoPath.IsPathRooted(
                directoryPathWithoutLeadingSeparator),
                $"The argument '{nameof(directoryPathWithoutLeadingSeparator)}' must not be a rooted path.");
            FileSystemPathValidator.ThrowIfInvalidDirectoryPath(directoryPathWithoutLeadingSeparator);

            string path = directoryPathWithoutLeadingSeparator.StartsWith($"{CurrentDirectorySymbol}{SystemIoPath.DirectorySeparatorChar}", StringComparison.Ordinal)
                || directoryPathWithoutLeadingSeparator.StartsWith($"{CurrentDirectorySymbol}{SystemIoPath.AltDirectorySeparatorChar}", StringComparison.Ordinal)
                ? directoryPathWithoutLeadingSeparator[2..]
                : directoryPathWithoutLeadingSeparator;

            _ = pathBuilder
                .Append(path);
        }

        return new(pathBuilder.ToString());
    }

    /// <summary>
    /// Resolves a relative path against a base path, ensuring that the resulting path does not escape above the base path by clamping to the path root.
    /// </summary>
    /// <remarks>The method resolves the provided relative file path against the provided base directory path, ensuring that the resulting path does not escape above the base path by clamping to the path root.
    /// For example, if the base path is <c>"C:\Base"</c> and the relative file path is <c>"..\..\..\file.txt"</c>, the resulting resolved path will be <c>"C:\file.txt"</c> instead of escaping above the base path.
    /// </remarks>
    /// <param name="basePath">The <see cref="DirectoryDescriptor"/> for the base path against which to resolve the relative path. Can be relative or absolute.</param>
    /// <param name="relativeDirectoryPath">The <see cref="DirectoryDescriptor"/> for the relative directory path to resolve against the relative or absolute base path <paramref name="basePath"/>.</param>
    /// <param name="baseDirectoryPathParameterName">Optional. The name of the parameter representing the base path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <param name="relativeDirectoryPathParameterName">Optional. The name of the parameter representing the relative directory path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <returns>The resolved directory path.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="basePath"/> or <paramref name="relativeDirectoryPath"/> is <see langword="default"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="relativeDirectoryPath"/> is not a relative directory path.</exception>
    public static DirectoryDescriptor ResolveRelativePath(
        DirectoryDescriptor basePath,
        DirectoryDescriptor relativeDirectoryPath,
        [CallerArgumentExpression(nameof(basePath))] string? basePathParameterName = null,
        [CallerArgumentExpression(nameof(relativeDirectoryPath))] string? relativePathParameterName = null)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(basePath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeDirectoryPath);
        ArgumentExceptionAdvanced.ThrowIfFalse(relativeDirectoryPath.IsRelative, $"The argument '{nameof(relativeDirectoryPath)}' must be a relative directory path.");

        string resolvedPath = ResolveRelativePath(basePath.Path.NormalizedPath.Segments, relativeDirectoryPath.Path.NormalizedPath.Segments, basePathParameterName, relativePathParameterName);
        return new(resolvedPath);
    }

    /// <summary>
    /// Resolves a relative path against a base path, ensuring that the resulting path does not escape above the base path by clamping to the path root.
    /// </summary>
    /// <remarks>The method resolves the provided relative file path against the provided base directory path, ensuring that the resulting path does not escape above the base path by clamping to the path root.
    /// For example, if the base path is <c>"C:\Base"</c> and the relative file path is <c>"..\..\..\file.txt"</c>, the resulting resolved path will be <c>"C:\file.txt"</c> instead of escaping above the base path.
    /// </remarks>
    /// <param name="basePath">The <see cref="DirectoryDescriptor"/> for the base path against which to resolve the relative path. Can be relative or absolute.</param>
    /// <param name="relativeFilePath">The <see cref="FileSystemPathDescriptor"/> for the relative file path to resolve against the relative or absolute base path <paramref name="basePath"/>.</param>
    /// <param name="baseDirectoryPathParameterName">Optional. The name of the parameter representing the base path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <param name="relativeFilePathParameterName">Optional. The name of the parameter representing the relative file path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <returns>The resolved file path.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="basePath"/> or <paramref name="relativeFilePath"/> is <see langword="default"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="relativeFilePath"/> is not a relative file path.</exception>
    public static FileSystemPathDescriptor ResolveRelativePath(
        DirectoryDescriptor basePath,
        FileSystemPathDescriptor relativeFilePath,
        [CallerArgumentExpression(nameof(basePath))] string? baseDirectoryPathParameterName = null,
        [CallerArgumentExpression(nameof(relativeFilePath))] string? relativeFilePathParameterName = null)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(basePath);
        ArgumentNullExceptionAdvanced.ThrowIfNull(relativeFilePath);
        ArgumentExceptionAdvanced.ThrowIfFalse(relativeFilePath.IsRelative, $"The argument '{nameof(relativeFilePath)}' must be a relative file path.");

        string resolvedPath = ResolveRelativePath(basePath.Path.NormalizedPath.Segments, relativeFilePath.Path.NormalizedPath.Segments, baseDirectoryPathParameterName, relativeFilePathParameterName);
        return new(resolvedPath);
    }

    /// <summary>
    /// Resolves a relative path against a base path, ensuring that the resulting path does not escape above the base path by clamping to the path root.
    /// </summary>
    /// <param name="normalizedBasePath">The base path against which to resolve the relative path. Can be relative or absolute.</param>
    /// <param name="normalizedRelativePath">The relative path to resolve against the relative or absolute base path <paramref name="normalizedBasePath"/>.</param>
    /// <param name="basePathParameterName">Optional. The name of the parameter representing the base path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <param name="relativePathParameterName">Optional. The name of the parameter representing the relative path. If not provided, the method will capture the caller argument expression to resolve the caller's original argument name.</param>
    /// <returns>The resolved path.</returns>
    private static PathDescriptor ResolveRelativePath(
        PathSegmentList normalizedBasePath,
        PathSegmentList normalizedRelativePath,
        [CallerArgumentExpression(nameof(normalizedBasePath))] string? basePathParameterName = null,
        [CallerArgumentExpression(nameof(normalizedRelativePath))] string? relativePathParameterName = null)
    {
        PathSegmentList resolvedPathSegments = normalizedBasePath.AddRange(normalizedRelativePath);
        string temporaryResolvedPath = resolvedPathSegments!;

        // Create a new PathDescriptor to validate and normalize the combined path segments.
        // This will resolve any special path symbols like ".." and "."
        // and also ensure that the resulting path does not escape above the base path
        // by clamping to the path root.
        var temporaryResolvedPathDescriptor = new PathDescriptor(temporaryResolvedPath, normalizedRelativePath.PathKind);
        PathDescriptor resolvedPath = temporaryResolvedPathDescriptor.NormalizedPath;

        return resolvedPath;
    }

    public static IEnumerable<PathSegment> EnumerateDirectoryPathSegments(string path)
    {
        // Use DirectoryDescriptor to leverage the existing path parsing and validation logic
        var descriptor = new DirectoryDescriptor(path);
        return descriptor.EnumeratePathSegments();
    }
    #endregion Helpers

    /// <summary>
    /// Indicates whether the instance was created usinig 'default' or the implicit default constructor.
    /// In that case, all fields will be null and the instance is considered invalid.
    /// </summary>
    private bool IsDefaultInstance => _path is null
        && _name is null
        && _location is null
        && _isEmpty is null;

    public override string ToString() => Path.ToString();
    public bool Equals(DirectoryDescriptor other) => s_pathEqualityComparer.Equals(this, other);
    public override int GetHashCode() => s_pathEqualityComparer.GetHashCode(this);

    public IEnumerable<PathSegment> EnumeratePathSegments()
    {
        if (IsDefaultInstance || IsEmpty)
        {
            yield break;
        }

        foreach (PathSegment pathSegment in Path.Segments)
        {
            yield return pathSegment;
        }
    }

    public PathDescriptor Path
    {
        get
        {
            // Can only be NULL when instance is default or the implicit default constructor was used to create this instance.
            // In both cases the instance is considered invalid.
            // Since string.Empty is not considered valid under normal construction returning string.Empty is fine to communicate an uninitialized compiler default state and least disturbing.
            if (IsDefaultInstance || IsEmpty)
            {
                return PathDescriptor.Empty;
            }

            return _path;
        }
        private init => _path = value;
    }

    public string Name
    {
        get
        {
            if (IsDefaultInstance || IsEmpty)
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
    }

    // The parent directory path without the name. Can be absolute or relative.
    public DirectoryDescriptor Location
    {
        get
        {
            if (IsDefaultInstance || IsEmpty)
            {
                return Empty;
            }

            if (!_location.IsSet)
            {
                // Collect all segments except the last one (the current directory's name)
                // to form the parent directory path.
                var parentPathSegments = Path.Segments
                    .Take(Path.Segments.Count - 1)
                    .ToPathSegmentList(PathKind.Directory, Path.Segments.IsNormalized);

                DirectoryDescriptor parentDirectoryDescriptor = parentPathSegments.IsEmpty
                    ? Empty
                    : new DirectoryDescriptor(parentPathSegments.ToPathDescriptor());
                _location.SetValue(parentDirectoryDescriptor);
            }

            return _location;
        }

        private init => _location = value;
    }

    public string PathString => Path;

    public bool TryGetPathRoot(out PathSegment pathRoot)
    {
        if (IsDefaultInstance || IsEmpty)
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
    /// Gets a value indicating whether the current path is relative rather than absolute.
    /// </summary>
    /// <remarks>In general, relative paths are interpreted as relative to a current working directory (e.g. <c>/subdir</c>) or relative to the current drive (e.g. <c>c:subdir</c>) or truly relative to an arbitrary base directory (e.g. <c>subdir</c> or <c>./subdir</c> or <c>../subdir</c>). Absolute paths specify a complete path from the root of the file system and are not dependent on the current working directory or current drive.</remarks>
    /// <value><see langword="true"/> if the path is relative (rooted or unrooted) or <see langword="false"/> if the path is absolute.</value>
    public bool IsRelative => Path.IsRelative;

    public PathKind PathKind => Path.PathKind;

    /// <summary>
    /// Gets a value indicating whether the directory has an explicit drive root.
    /// </summary>
    /// <remarks>A directory has an explicit drive root if it is an absolute path or a relative path with an explicit root like "C:Temp".
    /// <br/>The property will treat paths like "/Temp" as implicitly drive rooted.</remarks>
    /// <value><see langword="true"/> if the directory has an explicit drive root like "C:Temp" or is an absolute path like "C:\User\Temp"; otherwise, <see langword="false"/>.</value>
    public bool HasExplicitDriveRoot => TryGetPathRoot(out PathSegment pathRoot) && pathRoot.Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeDriveRoot;

    /// <summary>
    /// Gets a value indicating whether the path has an implicit drive root (for example, <c>/subdir</c>).
    /// </summary>
    /// <remarks>An implicit drive root is present when the path is rooted and its root consists of a single
    /// character, typically representing a directory separator, for example <c>/subdir</c>.</remarks>
    public bool HasImplicitDriveRoot => TryGetPathRoot(out PathSegment pathRoot) && pathRoot.Kind is PathSegmentKind.RelativeRoot;

    /// <summary>
    /// Gets a value indicating whether the directory at the specified path currently exists.
    /// </summary>
    /// <value><see langword="true"/> if the directory exists or <see langword="false"/> an error occurred during the check or the directory does not exist at the time of the check.</value>
    public bool IsExisting => Directory.Exists(Path);

    public bool IsRoot => Path.HasRoot && Path.Segments.Count == 1;

    private bool IsEmpty
    {
        get => _isEmpty;
        init => _isEmpty = value;
    }

    public static bool operator ==(DirectoryDescriptor left, DirectoryDescriptor right) => left.Equals(right);
    public static bool operator !=(DirectoryDescriptor left, DirectoryDescriptor right) => !(left == right);

    public override bool Equals(object? obj) => obj is DirectoryDescriptor other && Equals(other);

    private static bool IsSpecialDirectorySymbol(string value) => SpecialRelativeUnrootedDirectorySymbolNormalizationTable.ContainsKey(value);

    public static implicit operator string(DirectoryDescriptor path) => path.ToString();
}
