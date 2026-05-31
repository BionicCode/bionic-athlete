namespace BionicCode.Utilities.Net;
/// <summary>
/// Represents a segment of a file system path represented by <see cref="PathDescriptor"/>. 
/// </summary>
public readonly record struct PathSegment
{
    public static readonly PathSegment Empty = new PathSegment() with { Name = string.Empty };

    /// <summary>
    /// Creates a new instance of the <see cref="PathSegment"/> struct with the specified name and root status. 
    /// </summary>
    /// <param name="name">The name of the path segment. </param>
    /// <param name="kind">The <see cref="PathSegmentKind"/> of the path segment.</param>
    internal PathSegment(string name, PathSegmentKind kind) : this()
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PathSegmentKind>(kind);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(
            kind,
            [PathSegmentKind.Undefined],
            message: $"Invalid argument '{nameof(kind)}'. The argument must be a defined value of the '{nameof(PathSegmentKind)}' enum other than '{PathSegmentKind.Undefined}'.");

        string normalizedName = FileHelpers.NormalizeDirectorySeparators(name);
        // Only root is allowed to contain directory separator characters e.g., "C:\" or "\\server\share" or "\".
        if (kind is not PathSegmentKind.FullyQualifiedRoot and not PathSegmentKind.RelativeRoot)
        {
            ArgumentExceptionAdvanced.ThrowIfContainsAny(
                name,
                DirectoryDescriptor.DirectorySeparatorChars,
                message: $"Invalid argument '{nameof(name)}'. If the argument '{nameof(kind)}' is not a '{nameof(PathSegmentKind.FullyQualifiedRoot)}' or '{nameof(PathSegmentKind.RelativeRoot)}', the segment cannot contain directory separator characters. Directory separator characters are only allowed for the path root segment.");
        }
        else
        {
            // SInce Path.GetPathRoot normalizes directoy separators we must also normalize the name before comparing it to the path root
            // to ensure a valid comparison. For example, on Windows, both "C:\" and "C:/" are valid path roots
            // and should be considered equal after normalization.
            ArgumentExceptionAdvanced.ThrowIfFalse(Path.GetPathRoot(normalizedName)!.Equals(normalizedName, StringComparison.Ordinal),
                message: $@"Invalid argument '{nameof(name)}'. The argument '{nameof(name)}' must be a valid path root if the argument '{nameof(kind)}' is '{nameof(PathSegmentKind.FullyQualifiedRoot)}' or '{nameof(PathSegmentKind.RelativeRoot)}'. Valid path root examples include 'C:\', '\\server\\share', or '\'.");
        }

        Name = normalizedName;
        IsSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(Name);
        ArgumentExceptionAdvanced.ThrowIfTrue(
            IsSpecial && kind is not PathSegmentKind.CurrentDirectory and not PathSegmentKind.ParentDirectory,
            message: $"Invalid argument '{nameof(kind)}'. If the argument '{nameof(name)}' is a special directory symbol like '.' or '..', the argument '{nameof(kind)}' must be either '{nameof(PathSegmentKind.CurrentDirectory)}' or '{nameof(PathSegmentKind.ParentDirectory)}'.");
        ArgumentExceptionAdvanced.ThrowIfTrue(
            IsSpecial && Name.Equals(DirectoryDescriptor.CurrentDirectorySymbol, StringComparison.Ordinal) && kind is not PathSegmentKind.CurrentDirectory,
            message: $"Invalid argument '{nameof(kind)}'. If the argument '{nameof(name)}' is the current directory symbol '.', the argument '{nameof(kind)}' must be '{nameof(PathSegmentKind.CurrentDirectory)}'.");
        ArgumentExceptionAdvanced.ThrowIfTrue(
            IsSpecial && Name.Equals(DirectoryDescriptor.ParentDirectorySymbol, StringComparison.Ordinal) && kind is not PathSegmentKind.ParentDirectory,
            message: $"Invalid argument '{nameof(kind)}'. If the argument '{nameof(name)}' is the parent directory symbol '..', the argument '{nameof(kind)}' must be '{nameof(PathSegmentKind.ParentDirectory)}'.");

        Kind = kind;
    }

    /// <summary>
    /// The name of the path segment.
    /// </summary>
    /// <remarks>Can be a special directory symbol like "." or "..", in which case the property <see cref="IsSpecial"/> is <see langword="true"/>. 
    /// Otherwise, it can be a path root (e.g., "C:\" on Windows or "/" on Unix-based systems), in which case the property <see cref="IsRoot"/> is <see langword="true"/>, or a simple directory name.
    /// <para/>The name is normalized to ensure consistent representation across different platforms. For example, on Windows, both "C:\" and "C:/" are valid path roots and will be normalized to "C:\".
    /// <para/>
    /// The following list shows valid file system path segment names:
    /// <list type="bullet">
    /// <item><term>"C:\"</term><description>A <b>fully qualified</b> path root on Windows. Such path is not relative.</description></item>
    /// <item><term>"/"</term><description>A <b>fully qualified</b> path root on Unix-based systems. Such path is not relative.</description></item>
    /// <item><term>"\\server\share"</term><description>A <b>fully qualified</b> path root for UNC paths. Such path is not relative.</description></item>
    /// <item><term>"C:"</term><description>A <b>drive relative</b> path root on Windows (relative to the current working directory rooted in the specified drive). Such segment is relative.</description></item>
    /// <item><term>"\"</term><description>A <b>root relative</b> path root on Windows (relative to the current working directory rooted in the current drive). Such segment is relative.</description></item>
    /// <item><term>"."</term><description>A special <b>current directory</b> symbol. Such segment is relative.</description></item>
    /// <item><term>".."</term><description>A special <b>parent directory</b> symbol. Such segment is relative.</description></item>
    /// <item><term>subdirectory</term><description>A normal directory path segment name for a subdirectory. It's the equivalent of ".\subdirectory". Such segment is relative.</description></item>
    /// </list>
    /// </remarks>
    /// <value>The name of the path segment.</value>
    public string Name { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the segment is a special directory symbol like "." or "..".
    /// </summary>
    /// <value><see langword="true"/> if the segment is a special directory symbol; otherwise, <see langword="false"/>.</value>
    public bool IsSpecial { get; }

    public PathSegmentKind Kind { get; }

    /// <summary>
    /// Gets a value indicating whether the segment is the root of a path.
    /// </summary>
    /// <remarks>The root segment is the first segment of a path that represents the root directory. 
    /// The following examples show valid file system roots and therefore valid values for the <see cref="Name"/> property:
    /// <list type="bullet">
    /// <item><term>"C:\"</term><description>A <b>fully qualified</b> path root on Windows. Such path is not relative.</description></item>
    /// <item><term>"/"</term><description>A <b>fully qualified</b> path root on Unix-based systems. Such path is not relative.</description></item>
    /// <item><term>"\\server\share"</term><description>A <b>fully qualified</b> path root for UNC paths. Such path is not relative.</description></item>
    /// <item><term>"C:"</term><description>A <b>drive relative</b> path root on Windows (relative to the current working directory rooted in the specified drive). Such path is relative.</description></item>
    /// <item><term>"\"</term><description>A <b>root relative</b> path root on Windows (relative to the current working directory rooted in the current drive). Such path is relative.</description></item>
    /// </list>
    /// </remarks>
    /// <value><see langword="true"/> if the segment is the root of a path; otherwise, <see langword="false"/>.</value>
    public bool IsRoot => Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeRoot or PathSegmentKind.RelativeDriveRoot;

    public bool IsRelative => Kind is not PathSegmentKind.FullyQualifiedRoot;
    public bool IsSegmentName => Kind is PathSegmentKind.DirectoryName or PathSegmentKind.FileName;
}
