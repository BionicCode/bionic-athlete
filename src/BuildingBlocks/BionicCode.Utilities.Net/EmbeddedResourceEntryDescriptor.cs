namespace BionicCode.Utilities.Net;

using System.Reflection;

public class EmbeddedResourceEntryDescriptor : FileDescriptor, IEquatable<EmbeddedResourceEntryDescriptor>
{
    private readonly WriteOnce<EqualityComparer<FileDescriptor>> _comparer;
    private readonly string _fileName;
    private readonly string _fileNameWithoutExtension;
    private readonly FileExtension _fileExtension;

    public EmbeddedResourceEntryDescriptor(string resourceName, string fileName, Assembly embeddedResourceAssembly) : base(FileDescriptorKind.EmbeddedResourceEntry)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(resourceName);
        FileSystemPathValidator.ThrowIfInvalidFileName(fileName);
        ArgumentNullExceptionAdvanced.ThrowIfNull(embeddedResourceAssembly);

        ResourceName = resourceName;
        _fileName = fileName;
        _fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        _fileExtension = FileExtension.FromFileName(fileName);
        EmbeddedResourceAssembly = embeddedResourceAssembly;
        _comparer = new WriteOnce<EqualityComparer<FileDescriptor>>();
    }

    /// <summary>
    /// Gets the name of the embedded resource.
    /// </summary>
    public string ResourceName { get; }
    public Assembly EmbeddedResourceAssembly { get; }

    protected override EqualityComparer<FileDescriptor> Comparer
    {
        get
        {
            if (!_comparer.IsSet)
            {
                _comparer.SetValue(EqualityComparer<FileDescriptor>.Create(
                    (x, y) => x is EmbeddedResourceEntryDescriptor descriptorX
                        && y is EmbeddedResourceEntryDescriptor descriptorY
                        && string.Equals(descriptorX.ResourceName, descriptorY.ResourceName, StringComparison.Ordinal)
                        && descriptorX.EmbeddedResourceAssembly == descriptorY.EmbeddedResourceAssembly,
                    obj => obj is EmbeddedResourceEntryDescriptor descriptor
                        ? HashCode.Combine(descriptor.ResourceName.GetHashCode(StringComparison.Ordinal), descriptor.EmbeddedResourceAssembly.GetHashCode())
                        : 0));
            }

            return _comparer;
        }
    }

    public async Task<Stream> GetFileAsync() => EmbeddedResourceAssembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource '{ResourceName}'");

    public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
    {
        await using Stream resourceStream = EmbeddedResourceAssembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource '{ResourceName}'");
        await resourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    public bool Equals(EmbeddedResourceEntryDescriptor? other) => Comparer.Equals(this, other);
    public override int GetHashCode() => Comparer.GetHashCode(this);
    protected override FileExtension GetFileExtension() => _fileExtension;
    protected override string GetName() => _fileName;
    protected override string GetNameWithoutExtension() => _fileNameWithoutExtension;
    public override bool Equals(object? obj) => obj is EmbeddedResourceEntryDescriptor other && Equals(other);

    public static bool operator ==(EmbeddedResourceEntryDescriptor? left, EmbeddedResourceEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(EmbeddedResourceEntryDescriptor? left, EmbeddedResourceEntryDescriptor? right) => !(left == right);
}