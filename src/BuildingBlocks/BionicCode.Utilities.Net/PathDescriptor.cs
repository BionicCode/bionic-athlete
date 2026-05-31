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
    private readonly WriteOnce<int> _depth;
    private readonly WriteOnce<PathDescriptor> _normalizedPath;
    private readonly WriteOnce<int> _resolvedDepth;

    public static PathDescriptor Empty { get; } = new PathDescriptor() with { Segments = new PathSegmentList(ImmutableList<PathSegment>.Empty, true) };

    private PathDescriptor(PathSegmentList segments)
    {
        _hashCodeCache = new WriteOnce<int>();
        _depth = new WriteOnce<int>();
        _resolvedDepth = new WriteOnce<int>();
        _normalizedPath = new WriteOnce<PathDescriptor>();

        Segments = segments;
        IsRelative = Segments[0].Kind is not PathSegmentKind.FullyQualifiedRoot;
        IsDirectoryPath = segments.IsDirectory;
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
        _depth = new WriteOnce<int>();
        _resolvedDepth = new WriteOnce<int>();
        _normalizedPath = new WriteOnce<PathDescriptor>();

        var segments = new List<PathSegment>();
        int startIndex = 0;

        string normalizedPath = FileHelpers.NormalizeDirectorySeparators(path);
        string pathRoot = Path.GetPathRoot(normalizedPath) ?? string.Empty;
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
            PathSegment segment = isDirectory
                ? CreateDirectorySegment(segmentNameWithoutTrailingSeparator)
                : CreateFileSegment(segmentNameWithoutTrailingSeparator);
            segments.Add(segment);
        }

        Segments = new PathSegmentList(segments, isDirectory);
        IsRelative = Segments[0].Kind is not PathSegmentKind.FullyQualifiedRoot;
        IsDirectoryPath = isDirectory;
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
    public PathSegmentList Segments { get; private init; }

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
    /// <value>The positive depth of the normalized path which is the resolved number of segments excluding the root segment if it exists.</value>
    public int Depth
    {
        get
        {
            if (Segments is null
                || Segments.Count == 0
                || _depth is null)
            {
                return 0;
            }

            if (!_depth.IsSet)
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
    public PathDescriptor NormilizedPath
    {
        get
        {
            if (_normalizedPath is null)
            {
                return PathSegmentList.Empty;
            }

            if (!_normalizedPath.IsSet)
            {
                PathSegmentList normalizedSegments = GetNormalizedPath();
                var normalizedPathDescriptor = new PathDescriptor(normalizedSegments);
                _normalizedPath.SetValue(normalizedPathDescriptor);
            }

            return _normalizedPath;
        }
    }

    private int CalculateClampedPathDepth()
    {
        if (Segments is null
            || Segments.Count == 0)
        {
            return 0;
        }

        PathSegmentList normalizedSegments = GetNormalizedPath();
        return normalizedSegments.Count;
    }

    private PathSegmentList GetNormalizedPath()
    {
        if (Segments is null
            || Segments.Count == 0)
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

        return normalizedSegments.ToPathSegmentList(IsDirectoryPath);
    }

    private int CalculateCurrentPathDepthDelta()
    {
        if (Segments is null
            || Segments.Count == 0)
        {
            return 0;
        }

        int depth = 0;
        int skipCount = HasRoot && Segments[0].IsRoot
            ? 1
            : 0;
        foreach (PathSegment pathSegment in Segments.Skip(skipCount))
        {
            if (pathSegment.IsSpecial)
            {
                if (pathSegment.Name.Equals(DirectoryDescriptor.ParentDirectorySymbol, StringComparison.Ordinal))
                {
                    depth--;
                }
                else if (pathSegment.Name.Equals(DirectoryDescriptor.CurrentDirectorySymbol, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            else
            {
                depth++;
            }
        }

        return depth;
    }

    /// <summary>
    /// Gets a depthDelta indicating whether the represented path is fully qualified or not.
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
    /// <depthDelta><see langword="true"/> if the path is relative i.e. not fully qualified; otherwise, <see langword="false"/>.</depthDelta>
    public bool IsRelative { get; }
    public bool IsDirectoryPath { get; }

    /// <summary>
    /// Gets a depthDelta indicating whether the represented path starts with a root segment.
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
    /// <depthDelta><see langword="true"/> if the segment is the root of a path; otherwise, <see langword="false"/>.</depthDelta>
    public bool HasRoot => Segments is not null
        && Segments.Count > 0
        && Segments[0].IsRoot;

    public override string ToString() => Segments?.ToString() ?? string.Empty;

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
