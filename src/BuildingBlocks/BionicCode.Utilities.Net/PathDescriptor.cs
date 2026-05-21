namespace BionicCode.Utilities.Net;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a canonical and validated file system path as an ordered collection of <see cref="PathSegment"/> instances.
/// </summary>
public readonly struct PathDescriptor : IEquatable<PathDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;
    private readonly WriteOnce<int> _hashCodeCache;
    private readonly WriteOnce<string> _toStringCache;

    internal PathDescriptor(FileDescriptor filePath) : this(filePath.FullPath, isDirectory: false)
    {
    }

    internal PathDescriptor(DirectoryDescriptor directoryPath) : this(directoryPath.FullPath, isDirectory: true)
    {
    }

    public PathDescriptor(string path, bool isDirectory)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(path);
        if (isDirectory)
        {
            FileSystemPathValidator.ThrowIfInvalidDirectoryPath(path);
        }
        else
        {
            FileSystemPathValidator.ThrowIfInvalidFilePath(path);
        }

        _hashCodeCache = new WriteOnce<int>();
        _toStringCache = new WriteOnce<string>();

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
            ? GetSpecialSegmentKind(segmentName)
            : PathSegmentKind.DirectoryName;
        var segment = new PathSegment(segmentName, segmentKind);
        return segment;
    }

    private static PathSegment CreateFileSegment(string segmentName)
    {
        bool isSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(segmentName);
        PathSegmentKind segmentKind = isSpecial
            ? GetSpecialSegmentKind(segmentName)
            : PathSegmentKind.FileName;
        var segment = new PathSegment(segmentName, segmentKind);
        return segment;
    }

    private static PathSegmentKind GetSpecialSegmentKind(string segmentName) => segmentName switch
    {
        _ when DirectoryDescriptor.CurrentDirectorySymbol.Equals(segmentName, StringComparison.Ordinal) => PathSegmentKind.CurrentDirectory,
        _ when DirectoryDescriptor.ParentDirectorySymbol.Equals(segmentName, StringComparison.Ordinal) => PathSegmentKind.ParentDirectory,
        _ => throw new NotImplementedException($"Currently unsupported special directory symbol: '{segmentName}'.")
    };

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
        string toStringValue = string.Empty;

        if (_toStringCache is null
            || Segments is null
            || Segments.Count == 0)
        {
            // This instance is a default(T) instance or was created using the implicit default constructor, which means it is uninitialized and therefore invalid.
            // Under normal construction, a valid instance will always have at least one segment.
            return string.Empty;
        }
        else if (_toStringCache.IsSet)
        {
            return _toStringCache;
        }
        else if (Segments.Count == 1)
        {
            toStringValue = Segments[0].Name;
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

            toStringValue = pathBuilder.ToString();
        }

        _toStringCache.SetValue(toStringValue);
        return _toStringCache;
    }

    public bool Equals(PathDescriptor other) => s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode()
    {
        // Can only be NULL when instance is default or the implicit default constructor was used to create this instance.
        // In both cases the instance is considered invalid.
        // Since string.Empty is not considered valid under normal construction returning string.Empty is fine to communicate an uninitialized compiler default state and least disturbing.
        if (_hashCodeCache is null)
        {
            return 0;
        }

        if (!_hashCodeCache.IsSet)
        {
            _hashCodeCache.SetValue(s_pathEqualityComparer.GetHashCode(this));
        }

        return _hashCodeCache;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PathDescriptor other && Equals(other);

    public static bool operator ==(PathDescriptor left, PathDescriptor right) => left.Equals(right);
    public static bool operator !=(PathDescriptor left, PathDescriptor right) => !(left == right);
}
