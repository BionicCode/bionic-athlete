namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Collections.Immutable;

public sealed class PathSegmentList : IImmutableList<PathSegment>
{
    private readonly ImmutableList<PathSegment> _segments;
    private readonly WriteOnce<string> _toStringCache;

    public PathSegmentList(IEnumerable<PathSegment> segments, bool isDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(segments);

        _segments = [.. segments];
        _toStringCache = new WriteOnce<string>();
        IsDirectory = isDirectory;
    }

    public PathDescriptor ToPathDescriptor() => new(ToString(), IsDirectory);

    public override string ToString()
    {
        string toStringValue = string.Empty;

        if (_toStringCache is null
            || _segments is null
            || _segments.IsEmpty)
        {
            // This instance is a default(T) instance or was created using the implicit default constructor, which means it is uninitialized and therefore invalid.
            // Under normal construction, a valid instance will always have at least one segment.
            return string.Empty;
        }
        else if (_toStringCache.IsSet)
        {
            return _toStringCache;
        }
        else if (_segments.Count == 1)
        {
            toStringValue = _segments.First().Name;
        }
        else
        {
            using var pathBuilder = PooledStringBuilder.GetOrCreate();
            int index = 0;
            PathSegment segment = _segments.ElementAt(index);
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
            if (_segments.Count > 1
                && ((segment.Kind is PathSegmentKind.FullyQualifiedRoot
                && !Path.EndsInDirectorySeparator(segment.Name))
                || segment.IsSpecial
                || segment.Kind is PathSegmentKind.DirectoryName))
            {
                _ = pathBuilder.Append(Path.DirectorySeparatorChar);
            }

            for (; index < _segments.Count; index++)
            {
                segment = _segments.ElementAt(index);
                _ = pathBuilder.Append(segment.Name);

                // We append a directory separator character after each segment except for the last one to ensure a correct path representation.
                if (index < _segments.Count - 1)
                {
                    _ = pathBuilder.Append(Path.DirectorySeparatorChar);
                }
            }

            toStringValue = pathBuilder.ToString();
        }

        _toStringCache.SetValue(toStringValue);
        return _toStringCache;
    }

    public int Count => _segments.Count;
    public bool IsEmpty => _segments.IsEmpty;

    public bool IsDirectory { get; }

    public PathSegment this[int index] => _segments[index];
    public IEnumerator<PathSegment> GetEnumerator() => _segments.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _segments.GetEnumerator();
    public PathSegmentList Add(PathSegment item) => new(_segments.Add(item), IsDirectory);
    public PathSegmentList AddRange(IEnumerable<PathSegment> items) => new (_segments.AddRange(items), IsDirectory);
    public PathSegmentList Clear() => new(_segments.Clear(), IsDirectory);
    public bool Contains(PathSegment item) => _segments.Contains(item);
    public void CopyTo(PathSegment[] array, int arrayIndex) => _segments.CopyTo(array, arrayIndex);
    public PathSegmentList Remove(PathSegment item, IEqualityComparer<PathSegment>? equalityComparer) => new PathSegmentList(_segments.Remove(item, equalityComparer), IsDirectory);
    public PathSegmentList RemoveAll(Predicate<PathSegment> match) => new PathSegmentList(_segments.RemoveAll(match), IsDirectory);
    public int IndexOf(PathSegment item) => _segments.IndexOf(item);
    public PathSegmentList Insert(int index, PathSegment item) => new(_segments.Insert(index, item), IsDirectory);
    public PathSegmentList InsertRange(int index, IEnumerable<PathSegment> items) => new(_segments.InsertRange(index, items), IsDirectory);
    public PathSegmentList RemoveAt(int index) => new(_segments.RemoveAt(index), IsDirectory);
    public PathSegmentList RemoveRange(IEnumerable<PathSegment> items, IEqualityComparer<PathSegment>? equalityComparer) => new PathSegmentList(_segments.RemoveRange(items, equalityComparer), IsDirectory);
    public PathSegmentList RemoveRange(int index, int count) => new PathSegmentList(_segments.RemoveRange(index, count), IsDirectory);
    public PathSegmentList Replace(PathSegment oldValue, PathSegment newValue, IEqualityComparer<PathSegment>? equalityComparer) => new PathSegmentList(_segments.Replace(oldValue, newValue, equalityComparer), IsDirectory);
    public PathSegmentList SetItem(int index, PathSegment value) => new PathSegmentList(_segments.SetItem(index, value), IsDirectory);

    #region Explicit IImmutableList Implementation
    IImmutableList<PathSegment> IImmutableList<PathSegment>.Add(PathSegment value) => Add(value);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.AddRange(IEnumerable<PathSegment> items) => AddRange(items);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.Clear() => Clear();
    public int IndexOf(PathSegment item, int index, int count, IEqualityComparer<PathSegment>? equalityComparer) => _segments.IndexOf(item, index, count, equalityComparer);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.Insert(int index, PathSegment element) => Insert(index, element);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.InsertRange(int index, IEnumerable<PathSegment> items) => InsertRange(index, items);
    public int LastIndexOf(PathSegment item, int index, int count, IEqualityComparer<PathSegment>? equalityComparer) => _segments.LastIndexOf(item, index, count, equalityComparer);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.Remove(PathSegment value, IEqualityComparer<PathSegment>? equalityComparer) => Remove(value, equalityComparer);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.RemoveAll(Predicate<PathSegment> match) => RemoveAll(match);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.RemoveAt(int index) => RemoveAt(index);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.RemoveRange(IEnumerable<PathSegment> items, IEqualityComparer<PathSegment>? equalityComparer) => RemoveRange(items, equalityComparer);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.RemoveRange(int index, int count) => RemoveRange(index, count);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.Replace(PathSegment oldValue, PathSegment newValue, IEqualityComparer<PathSegment>? equalityComparer) => Replace(oldValue, newValue, equalityComparer);
    IImmutableList<PathSegment> IImmutableList<PathSegment>.SetItem(int index, PathSegment value) => SetItem(index, value);
    #endregion Explicit IImmutableList Implementation

    public static implicit operator string?(PathSegmentList segments) => segments?.ToString() ?? null;
    public static implicit operator PathDescriptor(PathSegmentList segments) => segments?.ToPathDescriptor() ?? PathDescriptor.Empty;
}

public static class PathSegmentListHelpers
{
    public static PathSegmentList ToPathSegmentList(this IEnumerable<PathSegment> segments, bool isDirectory) => new(segments, isDirectory);
}