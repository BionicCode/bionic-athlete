namespace BionicCode.Utilities.Net;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Describes a file that can be included in a conversion or archive batch.
/// </summary>
[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRenamingRequired = {IsRenamingRequired}")]
public readonly record struct ArchiveContentFileDescriptor
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and directory.
    /// </summary>
    /// <param name="name">The source file name.</param>
    /// <param name="location">The source directory.</param>
    /// <param name="archiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, <paramref name="name"/> is used.
    /// </param>
    public ArchiveContentFileDescriptor(string name, DirectoryDescriptor location, string? archiveEntryName = null)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(location);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(name);
        Extension = FileExtension.FromFileName(Name);
        Location = location;
        _filePath = Path.Combine(Location.FullPath, Name);
        ArchiveEntryName = NormalizeArchiveEntryName(archiveEntryName ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a full source path.
    /// </summary>
    /// <param name="filePath">The full source file path.</param>
    /// <param name="archiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, the source file name is used.
    /// </param>
    public ArchiveContentFileDescriptor(string filePath, string? archiveEntryName = null)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        EmbeddedResourceAssembly = null!;
        IsEmbeddedResource = false;
        Name = Path.GetFileName(filePath);
        Extension = FileExtension.FromFileName(Name);
        Location = new DirectoryDescriptor(Path.GetDirectoryName(filePath) ?? string.Empty);
        _filePath = filePath;
        ArchiveEntryName = NormalizeArchiveEntryName(archiveEntryName ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptor"/> struct from a file name and a <see cref="DirectoryDescriptor"/> that describes the location of the file.
    /// </summary>
    /// <param name="fileName">The file name including extension.</param>
    /// <param name="relativeLocation">The directory descriptor representing the relative location of the file in the provided <paramref name="embeddedResourceAssembly"/>.</param>
    /// <param name="archiveEntryName">
    /// The relative path to use inside an archive.
    /// When <see langword="null"/>, the source file name is used.
    /// </param>
    /// <param name="embeddedResourceAssembly">The <see cref="Assembly"/> that the specified file is located in.</param>
    public ArchiveContentFileDescriptor(string fileName, DirectoryDescriptor relativeLocation, Assembly embeddedResourceAssembly, string? archiveEntryName = null)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeLocation);
        ArgumentNullExceptionAdvanced.ThrowIfNull(embeddedResourceAssembly);

        IsEmbeddedResource = true;
        EmbeddedResourceAssembly = embeddedResourceAssembly;
        Name = Path.GetFileName(fileName);
        Extension = FileExtension.FromFileName(Name);
        Location = relativeLocation;
        _filePath = $"{Location}.{Name}"; // The full name of the embedded resource is typically in the format "Namespace.Folder.FileName"
        ArchiveEntryName = NormalizeArchiveEntryName(archiveEntryName ?? Name);
        OriginalName = Name;
        OriginalFullPath = _filePath;
    }

    public override string ToString() => FullPath;

    public bool HasRenamingInformation { get; init; }
    public Assembly EmbeddedResourceAssembly { get; }
    public bool IsEmbeddedResource { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>Set <see cref="OriginalName"/> to preserve the original file name and use <see cref="Name"/> for the current file name. 
    /// This can be useful if you need to provide renaming related information where <see cref="OriginalName"/> is the old name and <see cref="Name"/> is the new name.</remarks>
    public string Name { get; init; }

    /// <summary>
    /// Gets the <see cref="DirectoryDescriptor"/> that specifies the location associated with the file described by this <see cref="FileDescriptor"/>.
    /// </summary>
    public DirectoryDescriptor Location { get; init; }

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
    /// Gets the relative path that should be used for the file inside an archive.
    /// </summary>
    /// <remarks>
    /// This value can contain forward slash directory separators, for example <c>core/activity.csv</c>.
    /// </remarks>
    public string ArchiveEntryName { get; init; }

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

    private static string NormalizeArchiveEntryName(string archiveEntryName)
        => archiveEntryName.Replace('\\', '/').TrimStart('/');
}
