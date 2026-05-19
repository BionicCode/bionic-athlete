namespace BionicCode.Utilities.Net;

using System.Collections.Immutable;

public readonly record struct PathSegment(string Name, bool IsSpecial, bool IsRoot);

public readonly record struct PathDescriptor(ImmutableList<PathSegment> Segments, bool IsRelative, bool IsRooted)
{
    public string Path => ToString();
    public override string ToString() => string.Join(System.IO.Path.DirectorySeparatorChar, Segments.Select(s => s.Name));
};
