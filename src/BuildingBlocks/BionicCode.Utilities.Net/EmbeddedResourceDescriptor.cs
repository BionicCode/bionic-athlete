namespace BionicCode.Utilities.Net;

using System.Reflection;

public sealed class EmbeddedResourceDescriptor : FileDescriptor, IEquatable<EmbeddedResourceDescriptor>
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

    protected override bool EqualsCore(FileDescriptor? other) => other is EmbeddedResourceDescriptor descriptorOther
        && ResourceName.Equals(descriptorOther.ResourceName, StringComparison.Ordinal)
        && EmbeddedResourceAssembly == descriptorOther.EmbeddedResourceAssembly;

    protected override int GetHashCodeCore()
    {
        if (!_hashCode.IsSet)
        {
            int hashCode = HashCode.Combine(ResourceName, EmbeddedResourceAssembly);

            _hashCode.SetValue(hashCode);
        }

        return _hashCode;
    }

    public Stream OpenRead() => EmbeddedResourceAssembly.GetManifestResourceStream(ResourceName)
        ?? throw new FileNotFoundException($"Failed to get manifest resource stream for embedded resource '{ResourceName}'");

    public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
    {
        await using Stream resourceStream = OpenRead();
        await resourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    public bool Equals(EmbeddedResourceDescriptor? other) => base.Equals(other);
    protected override FileExtension GetFileExtension() => _fileExtension;
    protected override string GetName() => _fileName;
    protected override string GetNameWithoutExtension() => _fileNameWithoutExtension;

    public static bool operator ==(EmbeddedResourceDescriptor? left, EmbeddedResourceDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(EmbeddedResourceDescriptor? left, EmbeddedResourceDescriptor? right) => !(left == right);
    public static implicit operator string(EmbeddedResourceDescriptor embeddedResourceDescriptor) => embeddedResourceDescriptor?.ToString() ?? string.Empty;

    // Override to silence warnings about non-overridden equality members in derived classes.
    // The actual equality comparison logic is implemented in the base class and relies on the type of the file descriptor,
    // so we can safely delegate to the base implementation here.
    public override bool Equals(object? obj) => obj is EmbeddedResourceDescriptor other && Equals(other);

    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => ResourceName;
}