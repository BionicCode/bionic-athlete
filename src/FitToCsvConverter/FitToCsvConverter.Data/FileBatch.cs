namespace FitToCsvConverter.Data;

using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text;
using BionicCode.Utilities.Net;

public readonly struct FileBatch
{
    private readonly List<FileDescriptor> _fileDescriptorsInternal;
    public ReadOnlyCollection<FileDescriptor> FileDescriptors { get; init; }
    public int ConversionCount => _fileDescriptorsInternal.Count;
    public Encoding Encoding { get; }
    public string DestinationDirectory { get; }
    public string BatchName { get; }
    public CompressionLevel CompressionLevel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding.
    /// </summary>
    /// <param name="sourceFiles">The source files to be converted.</param>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(IEnumerable<FileDescriptor> sourceFiles, string destinationDirectory, string batchName, Encoding encoding, CompressionLevel compressionLevel)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(destinationDirectory);
        ArgumentNullExceptionAdvanced.ThrowIfNull(encoding);

        _fileDescriptorsInternal = [.. sourceFiles];
        FileDescriptors = _fileDescriptorsInternal.AsReadOnly();
        DestinationDirectory = destinationDirectory;
        BatchName = string.IsNullOrWhiteSpace(batchName)
            ? DateTime.Now.ToString("yyyyMMddHHmmss")
            : batchName;
        Encoding = encoding;
        CompressionLevel = compressionLevel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding. <see cref="CompressionLevel.SmallestSize"/> is used as default compression level."
    /// </summary>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(string destinationDirectory, string batchName, Encoding encoding) : this(Enumerable.Empty<FileDescriptor>(), destinationDirectory, batchName, encoding, CompressionLevel.SmallestSize)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding. <see cref="Encoding.UTF8"/> is used as default encoding and <see cref="CompressionLevel.SmallestSize"/> is used as default compression level."
    /// </summary>
    /// <param name="sourceFiles">The source files to be converted.</param>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(IEnumerable<FileDescriptor> sourceFiles, string destinationDirectory, string batchName) : this(sourceFiles, destinationDirectory, batchName, Encoding.UTF8, CompressionLevel.SmallestSize)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding. <see cref="Encoding.UTF8"/> is used as default encoding and <see cref="CompressionLevel.SmallestSize"/> is used as default compression level."
    /// </summary>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(string destinationDirectory, string batchName) : this(Enumerable.Empty<FileDescriptor>(), destinationDirectory, batchName, Encoding.UTF8, CompressionLevel.SmallestSize)
    {
    }

    public FileBatch AddFileToBatch(FileDescriptor sourceFile)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(sourceFile);

        // Preserve read-only-ness of this struct type
        var seed = _fileDescriptorsInternal.ToList();
        seed.Add(sourceFile);
        return this with { FileDescriptors = new ReadOnlyCollection<FileDescriptor>(seed) };
    }
}

public readonly struct FileDescriptor
{
    private readonly string _filePath;

    public FileDescriptor(string name, string location)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(location);

        Name = name;
        Location = location;
        _filePath = Path.Combine(Location, Name);
    }

    public FileDescriptor(string filePath)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        Name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        _filePath = filePath;
    }

    public string Name { get; }
    public string Location { get; }
    public string FullPath => _filePath;
}
