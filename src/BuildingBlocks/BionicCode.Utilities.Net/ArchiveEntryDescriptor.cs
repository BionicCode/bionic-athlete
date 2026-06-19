namespace BionicCode.Utilities.Net;

public sealed class ArchiveEntryDescriptor : FileDescriptor, IEquatable<ArchiveEntryDescriptor>
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

    protected override bool EqualsCore(FileDescriptor? other) => other is ArchiveEntryDescriptor descriptorOther
        && SourceFile.Equals(descriptorOther.SourceFile)
        && EntryPath.Equals(descriptorOther.EntryPath);

    protected override int GetHashCodeCore()
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = HashCode.Combine(SourceFile, EntryPath);
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
    public static implicit operator string(ArchiveEntryDescriptor archiveEntryDescriptor) => archiveEntryDescriptor?.EntryPath ?? string.Empty;

    // Override to silence warnings about non-overridden equality members in derived classes.
    // The actual equality comparison logic is implemented in the base class and relies on the type of the file descriptor,
    // so we can safely delegate to the base implementation here.
    public override bool Equals(object? obj) => obj is ArchiveEntryDescriptor other && Equals(other);
    public override int GetHashCode() => base.GetHashCode();
}
