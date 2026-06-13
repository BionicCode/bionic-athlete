namespace BionicCode.Utilities.Net;

using System.Reflection;

public class EmbeddedResourceDescriptor : FileDescriptor, IEquatable<EmbeddedResourceDescriptor>
{
    private readonly string _fileName;
    private readonly string _fileNameWithoutExtension;
    private readonly FileExtension _fileExtension;
    private readonly WriteOnce<int> _hashCode;

    public EmbeddedResourceDescriptor(string resourceName, string fileName, Assembly embeddedResourceAssembly) : base(FileDescriptorKind.EmbeddedResource)
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
        if (x is EmbeddedResourceDescriptor descriptorX
            && y is EmbeddedResourceDescriptor descriptorY)
        {
            return descriptorX.ResourceName.Equals(descriptorY.ResourceName, StringComparison.Ordinal)
                && descriptorX.EmbeddedResourceAssembly == descriptorY.EmbeddedResourceAssembly;
        }

        if (x is EmbeddedResourceDescriptor ^ y is EmbeddedResourceDescriptor)
        {
            return false;
        }

        return x?.Equals(y) ?? (y is null);
    }

    protected override int GetHashCodeCore(FileDescriptor? x)
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = x is EmbeddedResourceDescriptor descriptor
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

    public bool Equals(EmbeddedResourceDescriptor? other) => EqualsCore(this, other);
    protected override FileExtension GetFileExtension() => _fileExtension;
    protected override string GetName() => _fileName;
    protected override string GetNameWithoutExtension() => _fileNameWithoutExtension;

    public static bool operator ==(EmbeddedResourceDescriptor? left, EmbeddedResourceDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(EmbeddedResourceDescriptor? left, EmbeddedResourceDescriptor? right) => !(left == right);
    public static implicit operator string(EmbeddedResourceDescriptor embeddedResourceDescriptor) => embeddedResourceDescriptor?.ResourceName ?? string.Empty;

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