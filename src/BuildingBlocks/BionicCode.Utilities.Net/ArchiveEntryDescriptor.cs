namespace BionicCode.Utilities.Net;

public class ArchiveEntryDescriptor : FileDescriptor, IEquatable<ArchiveEntryDescriptor>
{
    private readonly WriteOnce<EqualityComparer<FileDescriptor>> _comparer;

    public ArchiveEntryDescriptor(FileDescriptor sourceFile, FileSystemPathDescriptor entryName) : base(FileDescriptorKind.ArchiveEntry)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sourceFile);
        ArgumentNullExceptionAdvanced.ThrowIfNull(entryName);
        ArgumentExceptionAdvanced.ThrowIfFalse(entryName.IsRelative, $"The argument '{nameof(entryName)}' must be a relative path and unrooted path like \"Directory/example.txt\".");
        ArgumentExceptionAdvanced.ThrowIfTrue(entryName.IsRooted, $"The argument '{nameof(entryName)}' must be a relative path and cannot be rooted.");
        ArgumentExceptionAdvanced.ThrowIfTrue(ReferenceEquals(entryName, FileSystemPathDescriptor.Empty) || entryName.Path.Segments.IsEmpty, $"The argument '{nameof(entryName)}' cannot be an empty path.");
        PathSegment leadingPathSegment = entryName.Path.Segments[0];
        ArgumentExceptionAdvanced.ThrowIfTrue(leadingPathSegment.IsSpecial && leadingPathSegment.Kind is PathSegmentKind.ParentDirectory, $"The argument '{nameof(entryName)}' cannot start with a parent directory symbol '..'.");

        SourceFile = sourceFile;
        EntryName = entryName;

        _comparer = new WriteOnce<EqualityComparer<FileDescriptor>>();
    }

    /// <summary>
    /// Gets the <see cref="FileDescriptor"/> representing the source file to include in an archive.
    /// </summary>
    public FileDescriptor SourceFile { get; }

    /// <summary>
    /// Gets the <see cref="FileSystemPathDescriptor"/> representing the entry's relative path to use inside an archive including the file name.
    /// </summary>
    /// <value>The <see cref="FileSystemPathDescriptor"/> representing the <b>relative path</b> to use inside an archive.</value>
    public FileSystemPathDescriptor EntryName { get; }

    protected override EqualityComparer<FileDescriptor> Comparer
    {
        get
        {
            if (!_comparer.IsSet)
            {
                _comparer.SetValue(EqualityComparer<FileDescriptor>.Create(
                    (x, y) => x is ArchiveEntryDescriptor pathX
                        && y is ArchiveEntryDescriptor pathY
                        && FileSystemPathEqualityComparer.Instance.Equals(pathX.SourceFile, pathY.SourceFile)
                        && FileSystemPathEqualityComparer.Instance.Equals(pathX.EntryName, pathY.EntryName),
                    obj => obj is ArchiveEntryDescriptor path
                        ? HashCode.Combine(FileSystemPathEqualityComparer.Instance.GetHashCode(path.SourceFile), FileSystemPathEqualityComparer.Instance.GetHashCode(path.EntryName))
                        : 0));
            }

            return _comparer;
        }
    }

    public bool Equals(ArchiveEntryDescriptor? other) => Comparer.Equals(this, other);
    public override int GetHashCode() => Comparer.GetHashCode(this);
    protected override FileExtension GetFileExtension() => EntryName.Extension;
    protected override string GetName() => EntryName.Name;
    protected override string GetNameWithoutExtension() => EntryName.NameWithoutExtension;
    public override bool Equals(object? obj) => obj is ArchiveEntryDescriptor other && Equals(other);

    public static bool operator ==(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => !(left == right);
}
