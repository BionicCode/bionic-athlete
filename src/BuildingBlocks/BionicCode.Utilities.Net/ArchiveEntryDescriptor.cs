namespace BionicCode.Utilities.Net;

public class ArchiveEntryDescriptor : FileDescriptor, IEquatable<ArchiveEntryDescriptor>
{
    private readonly WriteOnce<int> _hashCode;

    public ArchiveEntryDescriptor(FileDescriptor sourceFile, string entryName) : base(FileDescriptorKind.ArchiveEntry)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sourceFile);

        var entryNameDescriptor = new PathDescriptor(entryName, PathKind.File);
        PathDescriptor normalizedEntryPathDescriptor = entryNameDescriptor.NormalizedPath;
        ArgumentExceptionAdvanced.ThrowIfTrue(normalizedEntryPathDescriptor.HasRoot, $"The argument '{nameof(entryName)}' must be a relative path and cannot have a root.");
        ArgumentExceptionAdvanced.ThrowIfTrue(normalizedEntryPathDescriptor.Segments.IsEmpty, $"The argument '{nameof(entryName)}' cannot be an empty path.");
        PathSegment leadingPathSegment = normalizedEntryPathDescriptor.Segments[0];
        ArgumentExceptionAdvanced.ThrowIfTrue(leadingPathSegment.IsSpecial && leadingPathSegment.Kind is PathSegmentKind.ParentDirectory, $"The argument '{nameof(entryName)}' cannot start with a parent directory symbol '..'.");

        SourceFile = sourceFile;
        EntryName = normalizedEntryPathDescriptor;

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
    public PathDescriptor EntryName { get; }

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
    protected override FileExtension GetFileExtension() => FileExtension.FromFileName(EntryName.Segments[^1].Name);
    protected override string GetName() => EntryName.Segments[^1].Name;
    protected override string GetNameWithoutExtension() => Path.GetFileNameWithoutExtension(EntryName.Segments[^1].Name);

    public static bool operator ==(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(ArchiveEntryDescriptor? left, ArchiveEntryDescriptor? right) => !(left == right);
    public static implicit operator string(ArchiveEntryDescriptor archiveEntryDescriptor) => archiveEntryDescriptor?.EntryName ?? string.Empty;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (ReferenceEquals(obj, null))
        {
            return false;
        }

        throw new NotImplementedException();
    }
}
