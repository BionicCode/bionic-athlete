namespace BionicCode.Utilities.Net;

public sealed class ArchiveEntryDescriptor : FileDescriptor, IEquatable<ArchiveEntryDescriptor>
{
    private static readonly ArchivePathStringBuilder s_archivePathStringBuilder = new();
    private readonly WriteOnce<int> _hashCode;

    public ArchiveEntryDescriptor(FileDescriptor sourceFile, string entryName) : base(FileDescriptorKind.ArchiveEntry)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sourceFile);

        var entryNameDescriptor = new PathDescriptor(entryName, PathKind.File, s_archivePathStringBuilder);
        PathDescriptor normalizedEntryPathDescriptor = entryNameDescriptor.NormalizedPath;
        ArgumentExceptionAdvanced.ThrowIfTrue(normalizedEntryPathDescriptor.HasRoot, $"The argument '{nameof(entryName)}' must be a relative path and cannot have a root.");
        ArgumentExceptionAdvanced.ThrowIfTrue(normalizedEntryPathDescriptor.Segments.IsEmpty, $"The argument '{nameof(entryName)}' cannot be an empty path.");
        PathSegment leadingPathSegment = normalizedEntryPathDescriptor.Segments[0];
        ArgumentExceptionAdvanced.ThrowIfTrue(leadingPathSegment.IsSpecial && leadingPathSegment.Kind is PathSegmentKind.ParentDirectory, $"The argument '{nameof(entryName)}' cannot start with a parent directory symbol '..'.");

        SourceFile = sourceFile;
        EntryPath = normalizedEntryPathDescriptor;

        _hashCode = new WriteOnce<int>();
    }

    /// <summary>
    /// Gets the <see cref="FileDescriptor"/> representing the source file to include in an archive.
    /// </summary>
    public FileDescriptor SourceFile { get; }

    /// <summary>
    /// Gets the <see cref="PathDescriptor"/> representing the entry's relative path to use inside an archive including the file name.
    /// </summary>
    /// <value>The <see cref="PathDescriptor"/> representing the <b>relative path</b> to use inside an archive.</value>
    public PathDescriptor EntryPath { get; }

    public string EntryPathString => ToString();

    protected override bool EqualsCore(FileDescriptor? other) => other is ArchiveEntryDescriptor descriptorOther
        && SourceFile.Equals(descriptorOther.SourceFile)
        && ArchiveEntryComparer.Instance.Equals(EntryPath, descriptorOther.EntryPath);

    protected override int GetHashCodeCore()
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = HashCode.Combine(SourceFile, ArchiveEntryComparer.Instance.GetHashCode(EntryPath));
            _hashCode.SetValue(hashCode);
        }

        return _hashCode;
    }

    public bool Equals(ArchiveEntryDescriptor? other) => base.Equals(other);
    protected override FileExtension GetFileExtension() => FileExtension.FromFileName(EntryPath.Segments[^1].Name);
    protected override string GetName() => EntryPath.Segments[^1].Name;
    protected override string GetNameWithoutExtension() => Path.GetFileNameWithoutExtension(EntryPath.Segments[^1].Name);

    public static bool operator ==(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => !(left == right);
    public static implicit operator string(ArchiveEntryDescriptor archiveEntryDescriptor) => archiveEntryDescriptor?.ToString() ?? string.Empty;

    // Override to silence warnings about non-overridden equality members in derived classes.
    // The actual equality comparison logic is implemented in the base class and relies on the type of the file descriptor,
    // so we can safely delegate to the base implementation here.
    public override bool Equals(object? obj) => obj is ArchiveEntryDescriptor other && Equals(other);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => EntryPath;
}

public sealed class ArchivePathStringBuilder : FileSystemPathStringBuilder
{
    public ArchivePathStringBuilder() : base(Path.AltDirectorySeparatorChar) { }
}

/// <summary>
/// Provides a comparer for <see cref="ArchiveEntryDescriptor"/> and <see cref="PathDescriptor"/> instances based archive entry equality.
/// </summary>
/// <remarks>The shared instance is obtained from the <see cref="Instance"/> property. 
/// <para/>This comparer is used to determine equality between archive entries based on their entry paths. 
/// Because archive entries must be unique, the comparison returns <see langword="true"/> if the entry paths are equal; otherwise, <see langword="false"/>. 
/// <br/>If a <see cref="ArchiveEntryDescriptor"/> is passed to the <see cref="ArchiveEntryComparer.Equals(ArchiveEntryDescriptor?, ArchiveEntryDescriptor?)"/> 
/// only the <see cref="ArchiveEntryDescriptor.EntryPath"/> values are compared using an case-sensitive ordinal string comparison.
/// This is because <c>"text.txt"</c> and <c>"Text.txt"</c> can coexist inside the same archive path i.e. are consider two distinct entries.</remarks>
public sealed class ArchiveEntryComparer : IEqualityComparer<ArchiveEntryDescriptor>, IEqualityComparer<PathDescriptor>
{
    /// <summary>
    /// Gets the shared instance of the <see cref="ArchiveEntryComparer"/> class.
    /// </summary>
    public static readonly ArchiveEntryComparer Instance = new();

    private ArchiveEntryComparer()
    { }

    /// <summary>
    /// Compares two <see cref="ArchiveEntryDescriptor"/> instances for equality based on their entry paths.
    /// </summary>
    /// <remarks>This comparison is case-sensitive and uses ordinal string comparison. 
    /// This is because <c>"text.txt"</c> and <c>"Text.txt"</c> can coexist inside the same archive path i.e. are consider two distinct entries.</remarks>
    /// <param name="x">The first <see cref="ArchiveEntryDescriptor"/> to compare.</param>
    /// <param name="y">The second <see cref="ArchiveEntryDescriptor"/> to compare.</param>
    /// <returns><see langword="true"/> if the <see cref="PathDescriptor.NormalizedPath"/> values of the <see cref="ArchiveEntryDescriptor.EntryPath"/> property
    /// of <paramref name="x"/> and <paramref name="y"/> are equal or both <paramref name="x"/> and <paramref name="y"/> parameters are the same reference or <see langword="null"/>; 
    /// otherwise, <see langword="false"/>.</returns>
    public bool Equals(ArchiveEntryDescriptor? x, ArchiveEntryDescriptor? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return Equals(x.EntryPath.NormalizedPath, y.EntryPath.NormalizedPath);
    }

    /// <summary>
    /// Returns a hash code for the specified <see cref="ArchiveEntryDescriptor"/> based on its normalized entry path.
    /// </summary>
    /// <param name="obj">The <see cref="ArchiveEntryDescriptor"/> for which to get the hash code.</param>
    /// <returns>A hash code for the specified <see cref="ArchiveEntryDescriptor"/> based on <see cref="PathDescriptor.NormalizedPath"/> value returned from the <see cref="ArchiveEntryDescriptor.EntryPath"/> property.</returns>
    public int GetHashCode(ArchiveEntryDescriptor? obj) => obj is null
        ? 0
        : GetHashCode(obj.EntryPath);

    /// <summary>
    /// Compares two <see cref="PathDescriptor"/> instances for equality based on their normalized paths. 
    /// </summary>
    /// <remarks>This comparison is case-sensitive and uses ordinal string comparison. 
    /// This is because <c>"text.txt"</c> and <c>"Text.txt"</c> can coexist inside the same archive path i.e. are consider two distinct entries.</remarks>
    /// <param name="x">A <see cref="PathDescriptor"/> instance that is compared to<paramref name="y"/>.</param>
    /// <param name="y">A <see cref="PathDescriptor"/> instance that is compared to<paramref name="x"/>.</param>
    /// <returns><see langword="true"/> if the normalized paths of <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(PathDescriptor x, PathDescriptor y) => StringComparer.Ordinal.Equals(x.NormalizedPath, y.NormalizedPath);

    /// <summary>
    /// Returns a hash code for the specified <see cref="PathDescriptor"/> based on its normalized path.
    /// </summary>
    /// <param name="obj">The <see cref="PathDescriptor"/> for which to get the hash code.</param>
    /// <returns>A hash code for the specified <see cref="PathDescriptor"/> based on its normalized path returned from the <see cref="PathDescriptor.NormalizedPath"/> property.</returns>
    public int GetHashCode(PathDescriptor obj) => StringComparer.Ordinal.GetHashCode(obj.NormalizedPath);
}
