namespace BionicCode.Utilities.Net;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

public readonly record struct PathSegment(string Name, bool IsSpecial, bool IsRoot);

public readonly struct PathDescriptor : IEquatable<PathDescriptor>
{
    private static readonly FileSystemPathEqualityComparer s_pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    private PathDescriptor(ImmutableList<PathSegment> segments, bool isRelative, bool isRooted)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(segments);

        Segments = segments;
        IsRelative = isRelative;
        IsRooted = isRooted;
    }

    public PathDescriptor(string path)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(path);
        FileSystemPathValidator.ThrowIfInvalidDirectoryPath(path);

        var segments = new List<PathSegment>();
        int startIndex = 0;

        string pathRoot = Path.GetPathRoot(path) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pathRoot))
        {
            var rootSegment = new PathSegment
            {
                Name = pathRoot,
                IsSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(pathRoot),
                IsRoot = true
            };

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
                var segment = new PathSegment
                {
                    Name = segmentName,
                    IsSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(segmentName),
                    IsRoot = false
                };
                startIndex = endIndex + 1;

                segments.Add(segment);
            }
        }

        if (startIndex < path.Length)
        {
            string segmentName = path[startIndex..];
            string segmentNameWithoutTrailingSeparator = Path.TrimEndingDirectorySeparator(segmentName);
            var segment = new PathSegment
            {
                Name = segmentNameWithoutTrailingSeparator,
                IsSpecial = DirectoryDescriptor.SpecialDirectorySymbols.Contains(segmentNameWithoutTrailingSeparator),
                IsRoot = false
            };
            segments.Add(segment);
        }

        IsRelative = !Path.IsPathFullyQualified(path);
        IsRooted = !string.IsNullOrEmpty(Path.GetPathRoot(path));
        Segments = segments.ToImmutableList();
    }

    public string PathString => ToString();
    public ImmutableList<PathSegment> Segments { get; }
    public bool IsRelative { get; }
    public bool IsRooted { get; }
    public bool HasRoot => IsRooted
        && Segments is not null
        && Segments.Count > 0
        && Segments[0].IsRoot;

    public override string ToString() => Segments is null
        ? string.Empty
        : string.Join(Path.DirectorySeparatorChar, Segments.Select(s => Path.TrimEndingDirectorySeparator(s.Name)));

    public bool Equals(PathDescriptor other) => s_pathEqualityComparer.Equals(this, other)
        && IsRelative == other.IsRelative
        && IsRooted == other.IsRooted;

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(s_pathEqualityComparer.GetHashCode(this));
        hashCode.Add(IsRelative);
        hashCode.Add(IsRooted);

        return hashCode.ToHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PathDescriptor other && Equals(other);

    public static bool operator ==(PathDescriptor left, PathDescriptor right) => left.Equals(right);
    public static bool operator !=(PathDescriptor left, PathDescriptor right) => !(left == right);
}
