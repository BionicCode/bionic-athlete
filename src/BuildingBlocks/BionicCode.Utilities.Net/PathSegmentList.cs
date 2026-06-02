namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Collections.Immutable;

public sealed class PathSegmentList : IImmutableList<PathSegment>
{
    private readonly ImmutableList<PathSegment> _segments;

    public static readonly PathSegmentList Empty = new(ImmutableList<PathSegment>.Empty, isDirectory: false);

    public PathSegmentList(IEnumerable<PathSegment> segments, bool isDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(segments);

        _segments = [.. segments];
        IsDirectory = isDirectory;
    }

    public static PathSegmentList CreateForEmbeddedFilePath(IEnumerable<PathSegment> segments)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(segments);
        return new PathSegmentList(segments, isDirectory: false)
        {
            IsEmbeddedAssemblyPath = true
        };
    }

    public PathDescriptor ToPathDescriptor() => new(ToString(), IsDirectory);

    public override string ToString() => ToPathDescriptor().ToString();

    public int Count => _segments.Count;
    public bool IsEmpty => _segments.IsEmpty;

    public bool IsDirectory { get; }
    public bool IsEmbeddedAssemblyPath { get; private init; }

    public PathSegment this[int index] => _segments[index];

    public PathSegmentList this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(_segments.Count);
            return new(_segments.Skip(offset).Take(length), IsDirectory);
        }
    }

    public IEnumerator<PathSegment> GetEnumerator() => _segments.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _segments.GetEnumerator();
    public PathSegmentList Add(PathSegment item) => new(_segments.Add(item), IsDirectory);
    public PathSegmentList AddRange(IEnumerable<PathSegment> items) => new(_segments.AddRange(items), IsDirectory);
    public PathSegmentList Clear() => new(_segments.Clear(), IsDirectory);
    public bool Contains(PathSegment item) => _segments.Contains(item);
    public void CopyTo(PathSegment[] array, int arrayIndex) => _segments.CopyTo(array, arrayIndex);
    public PathSegmentList Remove(PathSegment item, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.Remove(item, equalityComparer), IsDirectory);
    public PathSegmentList RemoveAll(Predicate<PathSegment> match) => new(_segments.RemoveAll(match), IsDirectory);
    public int IndexOf(PathSegment item) => _segments.IndexOf(item);
    public PathSegmentList Insert(int index, PathSegment item) => new(_segments.Insert(index, item), IsDirectory);
    public PathSegmentList InsertRange(int index, IEnumerable<PathSegment> items) => new(_segments.InsertRange(index, items), IsDirectory);
    public PathSegmentList RemoveAt(int index) => new(_segments.RemoveAt(index), IsDirectory);
    public PathSegmentList RemoveRange(IEnumerable<PathSegment> items, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.RemoveRange(items, equalityComparer), IsDirectory);
    public PathSegmentList RemoveRange(int index, int count) => new(_segments.RemoveRange(index, count), IsDirectory);
    public PathSegmentList Replace(PathSegment oldValue, PathSegment newValue, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.Replace(oldValue, newValue, equalityComparer), IsDirectory);
    public PathSegmentList SetItem(int index, PathSegment value) => new(_segments.SetItem(index, value), IsDirectory);

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

    public static implicit operator string(PathSegmentList? segments) => segments?.ToString() ?? string.Empty;
    public static implicit operator PathDescriptor(PathSegmentList segments) => segments?.ToPathDescriptor() ?? PathDescriptor.Empty;
}

public static class PathSegmentListHelpers
{
    public static PathSegmentList ToPathSegmentList(this IEnumerable<PathSegment> segments, bool isDirectory) => new(segments, isDirectory);
}