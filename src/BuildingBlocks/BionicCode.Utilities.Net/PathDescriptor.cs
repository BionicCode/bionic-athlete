namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a canonical and validated file path as an ordered collection of <see cref="PathSegment"/> instances.
/// </summary>
public readonly struct PathDescriptor : IEquatable<PathDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;
    private static readonly WriteOnce<PathStringBuilder> s_defaultPathStringBuilder = new();
    private readonly WriteOnce<int>? _hashCodeCache;
    private readonly WriteOnce<int>? _depth;
    private readonly WriteOnce<PathDescriptor>? _normalizedPath;
    private readonly bool _isNormalized;
    private readonly PathStringBuilder? _pathStringBuilder;
    private readonly Dictionary<PathStringBuilder, string>? _pathStringCache;
    private readonly PathSegmentList? _segments;

    public static PathDescriptor Empty { get; } = new PathDescriptor(isNormalized: true);

    private PathDescriptor(bool isNormalized)
    {
        _hashCodeCache = new WriteOnce<int>();
        _depth = new WriteOnce<int>();
        _normalizedPath = new WriteOnce<PathDescriptor>();
        _pathStringCache = [];

        _segments = PathSegmentList.Empty;
        _isNormalized = isNormalized;
        IsRelative = false;
        PathKind = PathKind.Undefined;
    }

    internal PathDescriptor(PathSegmentList segments, bool isNormalized, PathStringBuilder? pathStringBuilder) : this(isNormalized)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty((IEnumerable)segments);

        _segments = segments;
        _pathStringBuilder = pathStringBuilder;
        IsRelative = !Segments.IsEmpty && Segments[0].Kind is not PathSegmentKind.FullyQualifiedRoot;
        PathKind = segments.PathKind;
    }

    public PathDescriptor(string path, PathKind pathKind, PathStringBuilder pathStringBuilder) : this(path, pathKind)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(pathStringBuilder);
        _pathStringBuilder = pathStringBuilder;
    }

    public PathDescriptor(string path, PathKind pathKind) : this(isNormalized: false)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(path);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PathKind>(pathKind);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(pathKind, [PathKind.Undefined]);

        PathKind = pathKind;
        switch (pathKind)
        {
            case PathKind.File:
                FileSystemPathValidator.ThrowIfInvalidFilePath(path);
                break;
            case PathKind.Directory:
                FileSystemPathValidator.ThrowIfInvalidDirectoryPath(path);
                break;
            default:
                throw new NotImplementedException("Currently unsupported path kind.");
        }

        string normalizedPath = FileHelpers.NormalizeDirectorySeparators(path);
        string pathRoot = Path.GetPathRoot(normalizedPath) ?? string.Empty;
        int startIndex = 0;
        var segments = new List<PathSegment>();
        if (!string.IsNullOrWhiteSpace(pathRoot))
        {
            bool isRootRelative = !Path.IsPathFullyQualified(pathRoot);
            bool isDriveRoot = pathRoot.EndsWith(Path.VolumeSeparatorChar)
                || pathRoot.EndsWith(Path.DirectorySeparatorChar);
            PathSegment rootSegment = CreateRootSegment(pathRoot, isRootRelative, isDriveRoot);
            segments.Add(rootSegment);

            // Adjust startIndex to ignore any leading directory separator characters after the root.
            // For example, Path.GetPathRoot returns "\\server\share" for UNC paths like "\\server\share\dir\a" - without the trailing directory separator.
            // So we have to look-ahead to check if there is a directory separator character after the root
            // to determine the correct starting index for the first path segment after the root.
            int nextCharacterIndex = pathRoot.Length;
            startIndex = normalizedPath.Length > nextCharacterIndex && DirectoryDescriptor.DirectorySeparatorChars.Contains(normalizedPath[nextCharacterIndex])
                ? nextCharacterIndex + 1
                : nextCharacterIndex;
        }

        for (int endIndex = startIndex; endIndex < normalizedPath.Length; endIndex++)
        {
            if (DirectoryDescriptor.DirectorySeparatorChars.Contains(normalizedPath[endIndex]))
            {
                // Index notation is exclusive for the end index,
                // so it will give us the correct segment name without including the separator character.
                string segmentName = normalizedPath[startIndex..endIndex];
                PathSegment segment = CreateDirectorySegment(segmentName);
                startIndex = endIndex + 1;

                segments.Add(segment);
            }
        }

        if (startIndex < normalizedPath.Length)
        {
            string segmentName = normalizedPath[startIndex..];
            string segmentNameWithoutTrailingSeparator = Path.TrimEndingDirectorySeparator(segmentName);
            PathSegment segment = PathKind switch
            {
                PathKind.Directory => CreateDirectorySegment(segmentNameWithoutTrailingSeparator),
                PathKind.File => CreateFileSegment(segmentNameWithoutTrailingSeparator),
                _ => throw new NotImplementedException("Currently unsupported path kind.")
            };
            segments.Add(segment);
        }

        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(
            segments,
            $"The provided path '{path}' does not contain any valid segments after normalization.",
            nameof(path));

        _segments = new PathSegmentList(segments, PathKind, isNormalized: false);
        IsRelative = !Segments.IsEmpty && Segments[0].Kind is not PathSegmentKind.FullyQualifiedRoot;
    }

    private static PathSegment CreateRootSegment(string pathRoot, bool isRootRelative, bool isDriveRoot)
    {
        PathSegmentKind segmentKind = isRootRelative
            ? (isDriveRoot
                ? PathSegmentKind.RelativeDriveRoot
                : PathSegmentKind.RelativeRoot)
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
    public PathSegmentList Segments
    {
        get => IsDefaultInstance ? PathSegmentList.Empty : _segments!;
        private init => _segments = value;
    }

    /// <summary>
    /// Gets the clamped depth of the path, which is defined as the number of segments in the path excluding the root segment if it exists.
    /// </summary>
    /// <remarks>The normalized/resolved depth of the path is calculated by counting the number of segments in the <see cref="Segments"/> collection 
    /// excluding the first segment if it is a root segment. A depth of 0 means a root-only path. 
    /// <br/>The depth is clamped to a minimum of 0, meaning that if there are more ".." segments than actual directory segments, the depth will not go below the root (0).
    /// <para/>
    /// "." is the current directory symbol and does not affect the depth, while ".." is the parent directory symbol and decreases the depth by one but never below 0.
    /// <para/>
    /// Examples: 
    /// <list type="bullet">
    /// <item>
    /// <term>"C:\"</term>
    /// <description>"C:\" has a depth of 0 because the root segment is ignored. </description></item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\file.txt"</term>
    /// <description>"C:\folder\subfolder\..\file.txt" is normalized: its original depth of 4 is reduced to a depth of 2 after resolving special directory symbols like "." and ".." 
    /// (ignored root segment: "C:\". Relevant segments: "folder", "file.txt").</description>
    /// </item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\..\..\..\..\"</term>
    /// <description>"C:\folder\subfolder\..\..\..\..\..\file.txt" is normalized: its original depth of 7 is clamped to "C:\" with a depth of 0 after resolving special directory symbols like "." and ".." 
    /// (ignored root segment: "C:\". Relevant segments: none).</description>
    /// </item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\..\..\..\..\file.txt"</term>
    /// <description>"C:\folder\subfolder\..\..\..\..\..\file.txt" is normalized: its original depth of 8 is clamped to "C:\file.txt" with a depth of 1 after resolving special directory symbols like "." and ".." 
    /// (ignored root segment: "C:\". Relevant segments: "file.txt").</description>
    /// </item>
    /// <term>"C:\folder\subfolder\.\file.txt"</term>
    /// <description>"C:\folder\subfolder\.\file.txt" is normalized: its original depth of 4 is reduced to "C:\folder\subfolder\file.txt" with a depth of 3 after resolving special directory symbols like "." and ".." 
    /// (ignored root segment: "C:\". Relevant segments: "file.txt").</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <hashCode>The positive depth of the normalized path which is the resolved number of segments excluding the root segment if it exists.</hashCode>
    public int Depth
    {
        get
        {
            if (IsDefaultInstance)
            {
                return 0;
            }

            if (!_depth!.IsSet)
            {
                int clampedDepth = CalculateClampedPathDepth();
                _depth.SetValue(clampedDepth);
            }

            return _depth;
        }
    }

    /// <summary>
    /// Gets a normalized version of the path, which is defined as a path with all special directory symbols like "." and ".." resolved and removed from the path segments.
    /// </summary>
    /// <remarks>The normalized/resolved path is calculated by counting the number of segments in the <see cref="Segments"/> collection 
    /// excluding the first segment if it is a root segment. A depth of 0 means a root-only path. 
    /// <br/>The depth is clamped to a minimum of 0, meaning that if there are more ".." segments than actual directory segments, the depth will not go below the root (0).
    /// <para/>
    /// "." is the current directory symbol and does not affect the depth, while ".." is the parent directory symbol and decreases the depth by one but never below 0.
    /// <para/>
    /// Examples: 
    /// <list type="bullet">
    /// <item>
    /// <term>"C:\"</term>
    /// <description>Normalized result: "C:\"</description></item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\file.txt"</term>
    /// <description>Normalized result: "C:\folder\file.txt"</description>
    /// </item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\..\..\..\..\"</term>
    /// <description>Normalized result: "C:\". The resulting path was clamped to the root due to excessive ".." segments.</description>
    /// </item>
    /// <item>
    /// <term>"C:\folder\subfolder\..\..\..\..\..\file.txt"</term>
    /// <description>Normalized result: "C:\file.txt". The resulting path was clamped to the root due to excessive ".." segments.</description>
    /// </item>
    /// <term>"C:\folder\subfolder\.\file.txt"</term>
    /// <description>Normalized result: "C:\folder\subfolder\file.txt"</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    public PathDescriptor NormalizedPath
    {
        get
        {
            if (IsDefaultInstance)
            {
                return PathDescriptor.Empty;
            }

            if (_isNormalized)
            {
                return this;
            }

            if (!_normalizedPath!.IsSet)
            {
                PathSegmentList normalizedSegments = GetNormalizedPath();
                PathDescriptor normalizedPathDescriptor = normalizedSegments.IsEmpty
                    ? PathDescriptor.Empty
                    : new PathDescriptor(normalizedSegments, isNormalized: true, _pathStringBuilder);
                _normalizedPath.SetValue(normalizedPathDescriptor);
            }

            return _normalizedPath;
        }
    }

    private int CalculateClampedPathDepth()
    {
        if (IsDefaultInstance)
        {
            return 0;
        }

        PathSegmentList normalizedSegments = GetNormalizedPath();
        return normalizedSegments.Count;
    }

    private PathSegmentList GetNormalizedPath()
    {
        if (IsDefaultInstance)
        {
            return PathSegmentList.Empty;
        }

        PathSegment firstSegment = Segments[0];

        // We isolate and protect the first segment if it is:
        // - a fully qualified root segment like "C:\" on Windows or "/" on Unix-based systems to maintains a valid rooted shape like e.g., "C:\Directory"
        // - a relative root segment like "\" to maintains a valid rooted shape like e.g., "\Directory"
        // - a relative drive rooted segment like "C:" to maintains a valid rooted shape like e.g., "C:Directory"
        // - a special segment representing the parent directory ("..") to maintain a valid relative path shape like "../Directory"
        int protectedSegmentCount = (HasRoot && firstSegment.IsRoot)
            ? 1
            : 0;
        List<PathSegment> normalizedSegments = [.. Segments.Take(protectedSegmentCount)];
        foreach (PathSegment pathSegment in Segments.Skip(protectedSegmentCount))
        {
            if (pathSegment.IsSpecial)
            {
                if (pathSegment.Name.Equals(DirectoryDescriptor.ParentDirectorySymbol, StringComparison.Ordinal))
                {
                    // If there are no root segments (normalizedSegments.Count can become 0)
                    // and normalizedSegments is empty OR the last element is a ".." segment (and not a directory name segment),
                    // we have to add the following consecutive ".." segments to the normalized path
                    // to maintain a valid relative path shape like "../../Directory"
                    if (normalizedSegments.Count == 0
                        || normalizedSegments.Last().Name.Equals(DirectoryDescriptor.ParentDirectorySymbol, StringComparison.Ordinal))
                    {
                        normalizedSegments.Add(pathSegment);
                    }
                    // Ensure we never remove a protected leading segment (if any) to maintain a valid path shape like e.g., "C:\Directory" or "../Directory"
                    else if (normalizedSegments.Count > protectedSegmentCount)
                    {
                        // Remove last segment
                        normalizedSegments.RemoveAt(normalizedSegments.Count - 1);
                    }
                }
                else if (pathSegment.Name.Equals(DirectoryDescriptor.CurrentDirectorySymbol, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            else
            {
                normalizedSegments.Add(pathSegment);
            }
        }

        return normalizedSegments.ToPathSegmentList(PathKind, isNormalized: true);
    }

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
    public bool HasRoot => !IsDefaultInstance
        && Segments.Count > 0
        && Segments[0].IsRoot;

    public PathKind PathKind { get; }

    public override string ToString()
    {
        if (IsDefaultInstance)
        {
            return string.Empty;
        }

        PathStringBuilder? pathStringBuilder = _pathStringBuilder ?? s_defaultPathStringBuilder;
        if (pathStringBuilder is null)
        {
            s_defaultPathStringBuilder.SetValue(new FileSystemPathStringBuilder());
            pathStringBuilder = s_defaultPathStringBuilder;
        }

        if (!_pathStringCache!.TryGetValue(pathStringBuilder, out string? cachedValue))
        {
            cachedValue = pathStringBuilder.BuildString(Segments);
            _pathStringCache.Add(pathStringBuilder, cachedValue);
        }

        return cachedValue;
    }

    public string ToString(PathStringBuilder pathStringBuilder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(pathStringBuilder);

        if (IsDefaultInstance)
        {
            return string.Empty;
        }

        if (!_pathStringCache!.TryGetValue(pathStringBuilder, out string? cachedValue))
        {
            cachedValue = pathStringBuilder.BuildString(Segments);
            _pathStringCache.Add(pathStringBuilder, cachedValue);
        }

        return cachedValue;
    }

    public bool Equals(PathDescriptor other) => s_pathEqualityComparer.Equals(this, other);

    public override int GetHashCode()
    {
        // Can only be NULL when instance is default or the implicit default constructor was used to create this instance.
        // In both cases the instance is considered invalid.
        // Since string.Empty is not considered valid under normal construction returning string.Empty is fine to communicate an uninitialized compiler default state and least disturbing.
        if (IsDefaultInstance)
        {
            return 0;
        }

        if (!_hashCodeCache!.IsSet)
        {
            int hashCode = s_pathEqualityComparer.GetHashCode(this);
            _hashCodeCache.SetValue(hashCode);
        }

        return _hashCodeCache;
    }

    private bool IsDefaultInstance => _segments is null
        && _depth is null
        && _hashCodeCache is null
        && _normalizedPath is null
        && _pathStringCache is null;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PathDescriptor other && Equals(other);

    public static bool operator ==(PathDescriptor left, PathDescriptor right) => left.Equals(right);
    public static bool operator !=(PathDescriptor left, PathDescriptor right) => !(left == right);

    public static implicit operator string(PathDescriptor path) => path.ToString();
}

public abstract class PathStringBuilder
{
    protected char DirectorySeparatorChar { get; }

    protected PathStringBuilder(char directorySeparatorChar) => DirectorySeparatorChar = directorySeparatorChar;

    public abstract string BuildString(PathSegmentList pathSegments);
}

public class FileSystemPathStringBuilder : PathStringBuilder
{
    protected FileSystemPathStringBuilder(char directorySeparatorChar) : base(directorySeparatorChar) { }
    public FileSystemPathStringBuilder() : base(Path.DirectorySeparatorChar) { }

    public override string BuildString(PathSegmentList pathSegments)
    {
        string toStringValue = string.Empty;

        if (pathSegments is null
            || pathSegments.IsEmpty)
        {
            // This instance is a default(T) instance or was created using the implicit default constructor, which means it is uninitialized and therefore invalid.
            // Under normal construction, a valid instance will always have at least one segment.
            return string.Empty;
        }
        else if (pathSegments.Count == 1)
        {
            toStringValue = pathSegments[0].Name;
        }
        else
        {
            using var pathBuilder = PooledStringBuilder.GetOrCreate();
            int index = 0;
            PathSegment segment = pathSegments[0];
            index++;
            _ = pathBuilder.Append(segment.Name);

            // We append a directory separator only if the first segment is
            // - a root segment that is fully qualified and without a trailing separator (e.g., "\\server\share") and at least one more segment is following.
            // - not root segment (normal segment name or special directory name like "." and "..") and at least one more segment is following.
            //
            // We never append a directory separator if the first segment is
            // - the only/last segment.
            // - a root segment that is fully qualified (e.g., "C:\" on Windows or "/" on Unix-based systems) and has a trailing separator when followed by at last one more segment.
            // - a root segment that is not fully qualified (e.g., "C:" or "\" on Windows) 
            // - a file name segment (e.g., "file.txt"), since it would always be the last or only segment.
            if (pathSegments.Count > 1
                && ((segment.Kind is PathSegmentKind.FullyQualifiedRoot
                && !Path.EndsInDirectorySeparator(segment.Name))
                || segment.IsSpecial
                || segment.Kind is PathSegmentKind.DirectoryName))
            {
                _ = pathBuilder.Append(DirectorySeparatorChar);
            }

            for (; index < pathSegments.Count; index++)
            {
                segment = pathSegments.ElementAt(index);
                _ = pathBuilder.Append(segment.Name);

                // We append a directory separator character after each segment except for the last one to ensure a correct path representation.
                if (index < pathSegments.Count - 1)
                {
                    _ = pathBuilder.Append(DirectorySeparatorChar);
                }
            }

            toStringValue = pathBuilder.ToString();
        }

        return toStringValue;
    }
}
