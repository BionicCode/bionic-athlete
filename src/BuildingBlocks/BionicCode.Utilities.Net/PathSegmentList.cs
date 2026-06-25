namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Collections.Immutable;

public sealed class PathSegmentList : IImmutableList<PathSegment>
{
    private readonly ImmutableList<PathSegment> _segments;

    public static readonly PathSegmentList Empty = new(ImmutableList<PathSegment>.Empty, PathKind.Undefined, isNormalized: true);

    /// <summary>
    /// Initializes a new instance of the <see cref="PathSegmentList"/> class with the specified segments, path kind, and normalization state.
    /// </summary>
    /// <param name="segments">The collection of path segments.</param>
    /// <param name="pathKind">The kind of path.</param>
    /// <param name="isNormalized">Indicates whether the path is normalized. This is when the path has been processed and resolved to remove any redundant or unnecessary elements, such as "." or ".." segments.</param>
    public PathSegmentList(IEnumerable<PathSegment> segments, PathKind pathKind, bool isNormalized)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(segments);

        _segments = [.. segments];
        PathKind = pathKind;
        IsNormalized = isNormalized;
    }

    public PathDescriptor ToPathDescriptor() => IsEmpty
        ? PathDescriptor.Empty
        : new PathDescriptor(this, IsNormalized, pathStringBuilder: null);

    public override string ToString() => IsEmpty
        ? string.Empty
        : ToPathDescriptor().ToString();

    public int Count => _segments.Count;
    public bool IsEmpty => _segments.IsEmpty;

    public PathKind PathKind { get; }

    /// <summary>
    /// Gets a value indicating whether the path is normalized. 
    /// </summary>
    /// <remarks>A normalized path is one that has been processed or resolved to remove any redundant or unnecessary elements, such as "." or ".." segments. 
    /// Normalization can help ensure that paths are in a consistent format and can be compared accurately.</remarks>
    public bool IsNormalized { get; }

    public bool IsEmbeddedAssemblyPath { get; private init; }

    public PathSegment this[int index] => _segments[index];

    public PathSegmentList this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(_segments.Count);
            return new(_segments.Skip(offset).Take(length), PathKind, IsNormalized);
        }
    }

    public IEnumerator<PathSegment> GetEnumerator() => _segments.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _segments.GetEnumerator();
    public PathSegmentList Add(PathSegment item) => new(_segments.Add(item), PathKind, IsNormalized);
    public PathSegmentList AddRange(IEnumerable<PathSegment> items) => new(_segments.AddRange(items), PathKind, IsNormalized);
    public PathSegmentList Clear() => new(_segments.Clear(), PathKind, IsNormalized);
    public bool Contains(PathSegment item) => _segments.Contains(item);
    public void CopyTo(PathSegment[] array, int arrayIndex) => _segments.CopyTo(array, arrayIndex);
    public PathSegmentList Remove(PathSegment item, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.Remove(item, equalityComparer), PathKind, IsNormalized);
    public PathSegmentList RemoveAll(Predicate<PathSegment> match) => new(_segments.RemoveAll(match), PathKind, IsNormalized);
    public int IndexOf(PathSegment item) => _segments.IndexOf(item);
    public PathSegmentList Insert(int index, PathSegment item) => new(_segments.Insert(index, item), PathKind, IsNormalized);
    public PathSegmentList InsertRange(int index, IEnumerable<PathSegment> items) => new(_segments.InsertRange(index, items), PathKind, IsNormalized);
    public PathSegmentList RemoveAt(int index) => new(_segments.RemoveAt(index), PathKind, IsNormalized);
    public PathSegmentList RemoveRange(IEnumerable<PathSegment> items, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.RemoveRange(items, equalityComparer), PathKind, IsNormalized);
    public PathSegmentList RemoveRange(int index, int count) => new(_segments.RemoveRange(index, count), PathKind, IsNormalized);
    public PathSegmentList Replace(PathSegment oldValue, PathSegment newValue, IEqualityComparer<PathSegment>? equalityComparer) => new(_segments.Replace(oldValue, newValue, equalityComparer), PathKind, IsNormalized);
    public PathSegmentList SetItem(int index, PathSegment value) => new(_segments.SetItem(index, value), PathKind, IsNormalized);

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
    public static PathSegmentList ToPathSegmentList(this IEnumerable<PathSegment> segments, PathKind pathKind, bool isNormalized) => new(segments, pathKind, isNormalized);
}