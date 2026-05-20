namespace BionicCode.Utilities.Net;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

public readonly record struct PathSegment
{
    /// <summary>
    /// Creates a new instance of the <see cref="PathSegment"/> struct with the specified name and root status. 
    /// </summary>
    /// <param name="name">The name of the path segment. </param>
    /// <param name="kind">The <see cref="PathSegmentKind"/> of the path segment.</param>
    internal PathSegment(string name, PathSegmentKind kind) : this()
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PathSegmentKind>(kind);

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
        Kind = kind;
    }

    public static PathSegmentKind GetRootSegmentKind(bool isRelative) => isRelative
        ? PathSegmentKind.RelativeRoot
        : PathSegmentKind.FullyQualifiedRoot;

    public static PathSegmentKind GetSpecialSegmentKind(string segmentName) => segmentName switch
    {
        _ when DirectoryDescriptor.CurrentDirectorySymbol.Equals(segmentName, StringComparison.Ordinal) => PathSegmentKind.CurrentDirectory,
        _ when DirectoryDescriptor.ParentDirectorySymbol.Equals(segmentName, StringComparison.Ordinal) => PathSegmentKind.ParentDirectory,
        _ => throw new NotImplementedException($"Currently unsupported special directory symbol: '{segmentName}'.")
    };

    public static PathSegmentKind GetNormalSegmentKind(bool isDirectory) => isDirectory
        ? PathSegmentKind.DirectoryName
        : PathSegmentKind.FileName;

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
    /// </remarks>
    /// <value>The name of the path segment.</value>
    public string Name { get; }

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
    public bool IsRoot => Kind is PathSegmentKind.FullyQualifiedRoot or PathSegmentKind.RelativeRoot;

    public bool IsRelative => Kind is not PathSegmentKind.FullyQualifiedRoot;
}

public enum PathSegmentKind
{
    FullyQualifiedRoot,
    RelativeRoot,
    CurrentDirectory,
    ParentDirectory,
    DirectoryName,
    FileName
}

public readonly struct PathDescriptor : IEquatable<PathDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    internal PathDescriptor(FileDescriptor filePath) : this(filePath.FullPath, isDirectory: false)
    {
    }

    internal PathDescriptor(DirectoryDescriptor directoryPath) : this(directoryPath.FullPath, isDirectory: true)
    {
    }

    public PathDescriptor(string path, bool isDirectory)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(path);
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(path);

        var segments = new List<PathSegment>();
        int startIndex = 0;

        string pathRoot = Path.GetPathRoot(path) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pathRoot))
        {
            bool isRootRelative = !Path.IsPathFullyQualified(pathRoot);
            PathSegment rootSegment = CreateRootSegment(pathRoot, isRootRelative);
            segments.Add(rootSegment);

            // Adjust startIndex to ignore any leading directory separator characters after the root.
            // For example, Path.GetPathRoot returns "\\server\share" for UNC paths like "\\server\share\dir\a" - without the trailing directory separator.
            // So we have to look-ahead to check if there is a directory separator character after the root
            // to determine the correct starting index for the first path segment after the root.
            int nextCharacterIndex = pathRoot.Length;
            startIndex = path.Length > nextCharacterIndex && DirectoryDescriptor.DirectorySeparatorChars.Contains(path[nextCharacterIndex])
                ? nextCharacterIndex + 1
                : nextCharacterIndex;
        }

        for (int endIndex = startIndex; endIndex < path.Length; endIndex++)
        {
            if (DirectoryDescriptor.DirectorySeparatorChars.Contains(path[endIndex]))
            {
                // Index notation is exclusive for the end index,
                // so it will give us the correct segment name without including the separator character.
                string segmentName = path[startIndex..endIndex];
                PathSegment segment = CreateDirectorySegment(segmentName);
                startIndex = endIndex + 1;

                segments.Add(segment);
            }
        }

        if (startIndex < path.Length)
        {
            string segmentName = path[startIndex..];
            string segmentNameWithoutTrailingSeparator = Path.TrimEndingDirectorySeparator(segmentName);
            PathSegment segment = isDirectory
                ? CreateDirectorySegment(segmentNameWithoutTrailingSeparator)
                : CreateFileSegment(segmentNameWithoutTrailingSeparator);
            segments.Add(segment);
        }

        Segments = [.. segments];
        IsRelative = Segments[0].Kind is not PathSegmentKind.FullyQualifiedRoot;
    }

    private static PathSegment CreateRootSegment(string pathRoot, bool isRootRelative)
    {
        PathSegmentKind segmentKind = isRootRelative
                        ? PathSegmentKind.RelativeRoot
                        : PathSegmentKind.FullyQualifiedRoot;
        var rootSegment = new PathSegment(pathRoot, segmentKind);
        return rootSegment;
    }

    private static PathSegment CreateDirectorySegment(string segmentName)
    {
        bool isSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(segmentName);
        PathSegmentKind segmentKind = isSpecial
            ? PathSegment.GetSpecialSegmentKind(segmentName)
            : PathSegmentKind.DirectoryName;
        var segment = new PathSegment(segmentName, segmentKind);
        return segment;
    }

    private static PathSegment CreateFileSegment(string segmentName)
    {
        bool isSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(segmentName);
        PathSegmentKind segmentKind = isSpecial
            ? PathSegment.GetSpecialSegmentKind(segmentName)
            : PathSegmentKind.FileName;
        var segment = new PathSegment(segmentName, segmentKind);
        return segment;
    }

    public string PathString => ToString();

    /// <summary>
    /// The ordered collection of <see cref="PathSegment"/> instances that compose this path.
    /// </summary>
    /// <remarks>The segments are represented without any directory separator characters except for the root segment, 
    /// which may include a trailing directory separator or only consists of the directory separator.
    /// <para/>
    /// The root segment is the first segment of a path that represents the root directory and defines whether the path is fully qualified. 
    /// The following examples show valid file system path root segment names:
    /// <list type="bullet">
    /// <item><term>"C:\"</term><description>A <b>fully qualified</b> path root on Windows. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "C:\folder\file.txt"</description></item>
    /// <item><term>"/"</term><description>A <b>fully qualified</b> path root on Unix-based systems. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "/folder/file.txt"</description></item>
    /// <item><term>"\\server\share"</term><description>A <b>fully qualified</b> path root for UNC paths. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "\\server\share\folder\file.txt"</description></item>
    /// <item><term>"C:"</term><description>A <b>drive relative</b> path root on Windows (relative to the current working directory rooted in the specified drive). <see cref="IsRelative"/> returns <see langword="true"/>.
    /// <br/>Example: "C:folder\file.txt"</description></item>
    /// <item><term>"\"</term><description>A <b>root relative</b> path root on Windows (relative to the current working directory rooted in the current drive). <see cref="IsRelative"/> returns <see langword="true"/>.
    /// <br/>Example: "\folder\file.txt"</description></item>
    /// </list>
    /// <para/>
    /// See <see cref="PathSegment.Name"/> for more information about possible path segment names.</remarks>
    public ImmutableList<PathSegment> Segments { get; }

    /// <summary>
    /// Gets a value indicating whether the represented path is fully qualified or not.
    /// </summary>
    /// <remarks>The root segment is the first segment of a path that represents the root directory and defines whether the path is fully qualified. 
    /// The following examples show valid file system path roots:
    /// <list type="bullet">
    /// <item><term>"C:\"</term><description>A <b>fully qualified</b> path root on Windows. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "C:\folder\file.txt"</description></item>
    /// <item><term>"/"</term><description>A <b>fully qualified</b> path root on Unix-based systems. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "/folder/file.txt"</description></item>
    /// <item><term>"\\server\share"</term><description>A <b>fully qualified</b> path root for UNC paths. <see cref="IsRelative"/> returns <see langword="false"/>.
    /// <br/>Example: "\\server\share\folder\file.txt"</description></item>
    /// <item><term>"C:"</term><description>A <b>drive relative</b> path root on Windows (relative to the current working directory rooted in the specified drive). <see cref="IsRelative"/> returns <see langword="true"/>.
    /// <br/>Example: "C:folder\file.txt"</description></item>
    /// <item><term>"\"</term><description>A <b>root relative</b> path root on Windows (relative to the current working directory rooted in the current drive). <see cref="IsRelative"/> returns <see langword="true"/>.
    /// <br/>Example: "\folder\file.txt"</description></item>
    /// </list>
    /// </remarks>
    /// <value><see langword="true"/> if the path is relative i.e. not fully qualified; otherwise, <see langword="false"/>.</value>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets a value indicating whether the represented path starts with a root segment.
    /// </summary>
    /// <remarks>The root segment is the first segment of a path that represents the root directory. 
    /// The following examples show valid file system path roots:
    /// <list type="bullet">
    /// <item><term>"C:\"</term><description>A fully qualified path root on Windows.
    /// <br/>Example: "C:\folder\file.txt"</description></item>
    /// <item><term>"/"</term><description>A fully qualified path root on Unix-based systems.
    /// <br/>Example: "/folder/file.txt"</description></item>
    /// <item><term>"\\server\share"</term><description>A fully qualified path root for UNC paths.
    /// <br/>Example: "\\server\share\folder\file.txt"</description></item>
    /// <item><term>"C:"</term><description>A drive relative path root on Windows (relative to the current working directory rooted in the specified drive).
    /// <br/>Example: "C:folder\file.txt"</description></item>
    /// <item><term>"\"</term><description>A root relative path root on Windows (relative to the current working directory rooted in the current drive).
    /// <br/>Example: "\folder\file.txt"</description></item>
    /// </list>
    /// <para/>
    /// If <see cref="HasRoot"/> is <see langword="true"/>, the path can still be relative if the root is not fully qualified (see above list for fully qualified path roots).
    /// </remarks>
    /// <value><see langword="true"/> if the segment is the root of a path; otherwise, <see langword="false"/>.</value>
    public bool HasRoot => Segments is not null
        && Segments.Count > 0
        && Segments[0].IsRoot;

    public override string ToString()
    {
        if (Segments is null
            || Segments.Count == 0)
        {
            return string.Empty;
        }
        else if (Segments.Count == 1)
        {
            return Segments[0].Name;
        }
        else
        {
            using var pathBuilder = PooledStringBuilder.GetOrCreate();
            int index = 0;
            PathSegment segment = Segments[index];
            index++;
            _ = pathBuilder.Append(segment.Name);

            // We append a directory separator only if the first segment is
            // * a root segment that is fully qualified and without a trailing separator (e.g., "\\server\share") and at least one more segment is following.
            // * not root segment (normal segment name or special directory name like "." and "..") and at least one more segment is following.
            //
            // We never append a directory separator if the first segment is
            // * the only/last segment.
            // * a root segment that is fully qualified (e.g., "C:\" on Windows or "/" on Unix-based systems) and has a trailing separator when followed by at last one more segment.
            // * a root segment that is not fully qualified (e.g., "C:" or "\" on Windows) 
            // * a file name segment (e.g., "file.txt"), since it would always be the last or only segment.
            if (Segments.Count > 1
                && ((segment.Kind is PathSegmentKind.FullyQualifiedRoot
                && !Path.EndsInDirectorySeparator(segment.Name))
                || segment.IsSpecial
                || segment.Kind is PathSegmentKind.DirectoryName))
            {
                _ = pathBuilder.Append(Path.DirectorySeparatorChar);
            }

            for (; index < Segments.Count; index++)
            {
                segment = Segments[index];
                _ = pathBuilder.Append(segment.Name);

                // We append a directory separator character after each segment except for the last one to ensure a correct path representation.
                if (index < Segments.Count - 1)
                {
                    _ = pathBuilder.Append(Path.DirectorySeparatorChar);
                }
            }

            return pathBuilder.ToString();
        }
    }

    public bool Equals(PathDescriptor other) => s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(s_pathEqualityComparer.GetHashCode(this));

        return hashCode.ToHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PathDescriptor other && Equals(other);

    public static bool operator ==(PathDescriptor left, PathDescriptor right) => left.Equals(right);
    public static bool operator !=(PathDescriptor left, PathDescriptor right) => !(left == right);
}
