namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRenamingRequired = {IsRenamingRequired}")]
public readonly record struct FileDescriptor
{
    private readonly string _filePath;
    private readonly FileSystemPathEqualityComparer _pathEqualityComparer = FileSystemPathEqualityComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="name">The source file name.</param>
    /// <param name="location">The source directory.</param>
    public FileDescriptor(string name, DirectoryDescriptor location)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(location);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(name);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFileName(Name);
        Location = location;
        _filePath = Path.Combine(Location.FullPath, Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = location.IsRelative;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source path.
    /// </summary>
    /// <param name="filePath">The full source file path.</param>
    public FileDescriptor(string filePath)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(filePath);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Extension = FileExtension.FromFileName(Name);
        Location = new DirectoryDescriptor(Path.GetDirectoryName(filePath) ?? string.Empty);
        _filePath = filePath;
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = !Path.IsPathFullyQualified(FullPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and a <see cref="DirectoryDescriptor"/> that describes the location of the file.
    /// </summary>
    /// <param name="fileName">The file name including extension.</param>
    /// <param name="relativeLocation">The directory descriptor representing the relative location of the file in the provided <paramref name="embeddedResourceAssembly"/>.</param>
    /// <param name="embeddedResourceAssembly">The <see cref="Assembly"/> that the specified file is located in.</param>
    public FileDescriptor(string fileName, DirectoryDescriptor relativeLocation, Assembly embeddedResourceAssembly)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeLocation);
        ArgumentNullExceptionAdvanced.ThrowIfNull(embeddedResourceAssembly);

        IsEmbeddedResource = true;
        EmbeddedResourceAssembly = embeddedResourceAssembly;
        Name = Path.GetFileName(fileName);
        Extension = FileExtension.FromFileName(Name);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(Name);
        Location = relativeLocation;
        _filePath = $"{Location}.{Name}"; // The full name of the embedded resource is typically in the format "Namespace.Folder.FileName"
        OriginalName = Name;
        OriginalFullPath = _filePath;
        IsRelative = relativeLocation.IsRelative;
    }

    public FileDescriptor GetPathRelativeTo(DirectoryDescriptor baseDirectory)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(baseDirectory);
        ArgumentExceptionAdvanced.ThrowIfTrue(baseDirectory.IsRelative, "Base directory must be an absolute path.", nameof(baseDirectory));

        if (IsRelative)
        {
            return this;
        }

        string relativePath = Path.GetRelativePath(baseDirectory.FullPath, FullPath);
        return new FileDescriptor(relativePath);
    }

    public override string ToString() => FullPath;

    public bool HasRenamingInformation => !_pathEqualityComparer.Equals(OriginalFullPath, FullPath)
        || !_pathEqualityComparer.Equals(OriginalName, Name);
    public Assembly EmbeddedResourceAssembly { get; }
    public bool IsEmbeddedResource { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>Set <see cref="OriginalName"/> to preserve the original file name and use <see cref="Name"/> for the current file name. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.</remarks>
    public string Name { get; }
    public string NameWithoutExtension { get; }

    /// <summary>
    /// Gets the <see cref="DirectoryDescriptor"/> that specifies the location associated with the file described by this <see cref="FileDescriptor"/>.
    /// </summary>
    public DirectoryDescriptor Location { get; }

    /// <summary>
    /// Gets the full file system path represented by this instance.
    /// </summary>
    /// <remarks>This value is derived from the <see cref="Location"/> and <see cref="Name"/> properties.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file path in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old path and <see cref="FullPath"/> is the new path.
    /// </remarks>
    public string FullPath => _filePath;

    /// <summary>
    /// Gets the file extension associated with the file.
    /// </summary>
    public FileExtension Extension { get; }

    /// <summary>
    /// The original full path of the file before any moving, renaming or copying operations. For embedded resources, 
    /// this is typically in the format "Namespace.Folder.FileName".
    /// </summary>
    /// <remarks>
    /// This value is set during the initialization of the <see cref="FileDescriptor"/> and remains unchanged 
    /// even if the file is renamed or copied. If not explicitly set via initializer the property returns <see cref="FullPath"/>.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file path in <see cref="OriginalFullPath"/>.
    /// This can be useful if you need to provide renaming or moving related file information where <see cref="OriginalFullPath"/> is the old path and <see cref="FullPath"/> is the new path.
    /// </remarks>
    /// <value>The original full path of the file. The default value is the same as <see cref="FullPath"/>.</value>
    public string OriginalFullPath { get; init; }
    public bool IsRelative { get; }

    /// <summary>
    /// Gets the original file name before any renaming operations.
    /// </summary>
    /// <remarks>
    /// This value is set during the initialization of the <see cref="FileDescriptor"/> and remains unchanged 
    /// even if the file is renamed. If not explicitly set via initializer the property returns <see cref="Name"/>.
    /// <para/>Use this to allow the <see cref="FileDescriptor"/> to carry the original file or old name in <see cref="OriginalName"/>. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.
    /// </remarks>
    /// <value>The original file name. The default value is the same as <see cref="Name"/>.</value>
    public string OriginalName { get; init; }
}
