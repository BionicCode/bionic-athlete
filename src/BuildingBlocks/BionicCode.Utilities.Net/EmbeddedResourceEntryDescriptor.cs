namespace BionicCode.Utilities.Net;

using System.Reflection;

public class EmbeddedResourceEntryDescriptor : FileDescriptor, IEquatable<EmbeddedResourceEntryDescriptor>
{
    private readonly string _fileName;
    private readonly string _fileNameWithoutExtension;
    private readonly FileExtension _fileExtension;
    private readonly WriteOnce<int> _hashCode;

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
        _hashCode = new WriteOnce<int>();
    }

    /// <summary>
    /// Gets the name of the embedded resource.
    /// </summary>
    public string ResourceName { get; }
    public Assembly EmbeddedResourceAssembly { get; }

    protected override bool EqualsCore(FileDescriptor? x, FileDescriptor? y)
    {
        if (x is EmbeddedResourceEntryDescriptor descriptorX
            && y is EmbeddedResourceEntryDescriptor descriptorY)
        {
            return descriptorX.ResourceName.Equals(descriptorY.ResourceName, StringComparison.Ordinal)
                && descriptorX.EmbeddedResourceAssembly == descriptorY.EmbeddedResourceAssembly;
        }

        if (x is EmbeddedResourceEntryDescriptor ^ y is EmbeddedResourceEntryDescriptor)
        {
            return false;
        }

        return x?.Equals(y) ?? (y is null);
    }

    protected override int GetHashCodeCore(FileDescriptor? x)
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = x is EmbeddedResourceEntryDescriptor descriptor
                ? HashCode.Combine(descriptor.ResourceName.GetHashCode(StringComparison.Ordinal), descriptor.EmbeddedResourceAssembly.GetHashCode())
                : x?.GetHashCode() ?? 0;

            _hashCode.SetValue(hashCode);
        }

        return _hashCode;
    }

    public async Task<Stream> GetFileAsync() => EmbeddedResourceAssembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource '{ResourceName}'");

    public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
    {
        await using Stream resourceStream = EmbeddedResourceAssembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource '{ResourceName}'");
        await resourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    public bool Equals(EmbeddedResourceEntryDescriptor? other) => EqualsCore(this, other);
    public override int GetHashCode() => GetHashCodeCore(this);
    protected override FileExtension GetFileExtension() => _fileExtension;
    protected override string GetName() => _fileName;
    protected override string GetNameWithoutExtension() => _fileNameWithoutExtension;
    public override bool Equals(object? obj) => obj is EmbeddedResourceEntryDescriptor other && Equals(other);

    public static bool operator ==(EmbeddedResourceEntryDescriptor? left, EmbeddedResourceEntryDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(EmbeddedResourceEntryDescriptor? left, EmbeddedResourceEntryDescriptor? right) => !(left == right);
}