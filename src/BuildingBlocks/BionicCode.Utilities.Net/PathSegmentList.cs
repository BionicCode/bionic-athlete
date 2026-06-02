namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Collections.Immutable;

public sealed class PathSegmentList : IImmutableList<PathSegment>
{
    private readonly ImmutableList<PathSegment> _segments;
    private readonly WriteOnce<PathStringBuilder> _defaultPathStringBuilder;
    private readonly Dictionary<Type, string> _pathStringCache;

    public static readonly PathSegmentList Empty = new(ImmutableList<PathSegment>.Empty, isDirectory: false);

    public PathSegmentList(IEnumerable<PathSegment> segments, bool isDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(segments);

        _segments = [.. segments];
        _pathStringCache = [];
        _defaultPathStringBuilder = new WriteOnce<PathStringBuilder>();
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

    public override string ToString()
    {
        if (!_defaultPathStringBuilder.IsSet)
        {
            _defaultPathStringBuilder.SetValue(new FileSystemPathStringBuilder());
        }

        if (!_pathStringCache.TryGetValue(_defaultPathStringBuilder.GetType(), out string? cachedValue))
        {
            cachedValue = _defaultPathStringBuilder.GetValueOrDefault().BuildString(this);
            _pathStringCache.Add(_defaultPathStringBuilder.GetType(), cachedValue);
        }

        return cachedValue;
    }

    public string ToString(PathStringBuilder pathStringBuilder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(pathStringBuilder);

        if (!_pathStringCache.TryGetValue(pathStringBuilder.GetType(), out string? cachedValue))
        {
            cachedValue = pathStringBuilder.BuildString(this);
            _pathStringCache.Add(pathStringBuilder.GetType(), cachedValue);
        }

        return cachedValue;
    }

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

public abstract class PathStringBuilder
{
    public abstract string BuildString(PathSegmentList pathSegments);
}

public class FileSystemPathStringBuilder : PathStringBuilder
{
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
                _ = pathBuilder.Append(Path.DirectorySeparatorChar);
            }

            for (; index < pathSegments.Count; index++)
            {
                segment = pathSegments.ElementAt(index);
                _ = pathBuilder.Append(segment.Name);

                // We append a directory separator character after each segment except for the last one to ensure a correct path representation.
                if (index < pathSegments.Count - 1)
                {
                    _ = pathBuilder.Append(Path.DirectorySeparatorChar);
                }
            }

            toStringValue = pathBuilder.ToString();
        }

        return toStringValue;
    }
}

/// <summary>
/// A <see cref="PathStringBuilder"/> implementation that builds a string representation of the path segments using the embedded resource path format, which uses a dot '.' as a separator between directory names and file name. 
/// </summary>
/// <remarks>For example, for a file named "file.txt" located in a directory "Resources" within the root namespace "MyProject", the resulting string would be "MyProject.Resources.file.txt".</remarks>
public class EmbeddedResourcePathStringBuilder : PathStringBuilder
{
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
            for (int index = 0; index < pathSegments.Count; index++)
            {
                PathSegment segment = pathSegments.ElementAt(index);
                if (index < pathSegments.Count - 1)
                {
                    if (segment.Kind is not PathSegmentKind.DirectoryName)
                    {
                        throw new InvalidOperationException($"Invalid path segment at index '{index}'. All segments except for the last one must be of kind '{nameof(PathSegmentKind.DirectoryName)}' when building an embedded resource path string. Found segment: '{segment.Name}' with kind '{segment.Kind}'.");
                    }

                    _ = pathBuilder.Append(segment.Name)
                        .Append(DirectoryDescriptor.EmbeddedFileNameSeparator);

                    continue;
                }
                else if (index == pathSegments.Count - 1)
                {
                    if (segment.Kind is not PathSegmentKind.FileName)
                    {
                        throw new InvalidOperationException($"Invalid path segment at index '{index}'. The last one must be of kind '{nameof(PathSegmentKind.FileName)}' when building an embedded resource path string. Found segment: '{segment.Name}' with kind '{segment.Kind}'.");
                    }

                    _ = pathBuilder.Append(segment.Name);
                }
            }

            toStringValue = pathBuilder.ToString();
        }

        return toStringValue;
    }
}