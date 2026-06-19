namespace BionicCode.Utilities.Net;

using System.Diagnostics;

/// <summary>
/// Represents a file descriptor that can be used to describe files in various contexts, 
/// such as file system paths, embedded resources, or archive entries. 
/// This abstract class provides a common interface for different types of file descriptors 
/// and implements equality comparison based on the file descriptor's kind and specific properties defined in derived classes.
/// </summary>
/// <remarks>Derived classes should implement the type immutable.</remarks>
[DebuggerDisplay("FileName = {Name}, Location = {Location}, OriginalFullPath = {OriginalFullPath}, OriginalName = {OriginalName}, IsRelative = {IsRelative}")]
public abstract class FileDescriptor : IEquatable<FileDescriptor>
{
    private readonly WriteOnce<FileExtension> _extension;
    private readonly WriteOnce<string> _name;
    private readonly WriteOnce<string> _nameWithoutExtension;
    private readonly WriteOnce<int> _hashCodeCache;

    protected FileDescriptor(FileDescriptorKind kind)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<FileDescriptorKind>(kind);

        _hashCodeCache = new WriteOnce<int>();
        _extension = new WriteOnce<FileExtension>();
        _name = new WriteOnce<string>();
        _nameWithoutExtension = new WriteOnce<string>();

        Kind = kind;
    }

    protected abstract FileExtension GetFileExtension();
    protected abstract string GetName();
    protected abstract string GetNameWithoutExtension();
    protected abstract bool EqualsCore(FileDescriptor? other);
    protected abstract int GetHashCodeCore();

    public bool Equals(FileDescriptor? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind && EqualsCore(other);
    }

    public override int GetHashCode()
    {
        if (!_hashCodeCache.IsSet)
        {
            int hashCode = HashCode.Combine(Kind, GetHashCodeCore());
            _hashCodeCache.SetValue(hashCode);
        }

        return _hashCodeCache;
    }

    public override string? ToString() => Extension != default
        ? $"{Name}{Extension}"
        : Name;

    public override bool Equals(object? obj) => obj is FileDescriptor other && Equals(other);

    public FileDescriptorKind Kind { get; }

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension
    {
        get
        {
            if (!_extension.IsSet)
            {
                FileExtension extension = GetFileExtension();
                if (extension == default)
                {
                    extension = FileExtension.Empty;
                }

                _extension.SetValue(extension);
            }

            return _extension;

        }
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>Set <see cref="OriginalName"/> to preserve the original file name and use <see cref="Name"/> for the current file name. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.</remarks>
    public string Name
    {
        get
        {
            if (!_name.IsSet)
            {
                string name = GetName() ?? string.Empty;
                _name.SetValue(name);
            }

            return _name;
        }
    }

    public string NameWithoutExtension
    {
        get
        {
            if (!_nameWithoutExtension.IsSet)
            {
                string nameWithoutExtension = GetNameWithoutExtension() ?? string.Empty;
                _nameWithoutExtension.SetValue(nameWithoutExtension);
            }

            return _nameWithoutExtension;
        }
    }

    public static bool operator ==(FileDescriptor? left, FileDescriptor? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(FileDescriptor? left, FileDescriptor? right) => !(left == right);
}
