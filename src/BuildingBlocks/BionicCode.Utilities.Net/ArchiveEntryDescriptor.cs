namespace BionicCode.Utilities.Net;

public class ArchiveEntryDescriptor : FileDescriptor, IEquatable<ArchiveEntryDescriptor>
{
    private readonly WriteOnce<int> _hashCode;

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

        _hashCode = new WriteOnce<int>();
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

    protected override bool EqualsCore(FileDescriptor? x, FileDescriptor? y)
    {
        if (x is ArchiveEntryDescriptor descriptorX
            && y is ArchiveEntryDescriptor descriptorY)
        {
            return descriptorX.SourceFile.Equals(descriptorY.SourceFile)
                && descriptorX.EntryName.Equals(descriptorY.EntryName);
        }

        if (x is ArchiveEntryDescriptor ^ y is ArchiveEntryDescriptor)
        {
            return false;
        }

        return x?.Equals(y) ?? (y is null);
    }

    protected override int GetHashCodeCore(FileDescriptor? x)
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = x is ArchiveEntryDescriptor descriptor
                ? HashCode.Combine(descriptor.SourceFile.GetHashCode(), descriptor.EntryName.GetHashCode())
                : x?.GetHashCode() ?? 0;

            _hashCode.SetValue(hashCode);
        }

        return _hashCode;
    }

    public bool Equals(ArchiveEntryDescriptor? other) => EqualsCore(this, other);
    public override int GetHashCode() => GetHashCodeCore(this);
    protected override FileExtension GetFileExtension() => EntryName.Extension;
    protected override string GetName() => EntryName.Name;
    protected override string GetNameWithoutExtension() => EntryName.NameWithoutExtension;
    public override bool Equals(object? obj) => obj is ArchiveEntryDescriptor other && Equals(other);

    public static bool operator ==(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => !(left == right);
}
