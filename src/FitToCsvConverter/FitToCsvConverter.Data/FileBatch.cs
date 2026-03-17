namespace FitToCsvConverter.Data;

using System.IO.Compression;
using System.Text;
using BionicCode.Utilities.Net;

public readonly struct FileBatch
{
    public IEnumerable<FileDescriptor> FileDescriptors { get; init; }
    public int FileDescriptorsCount { get; }
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
    public FileBatch(IEnumerable<FileDescriptor> sourceFiles, int sourceFilesCount, string destinationDirectory, string batchName, Encoding encoding, CompressionLevel compressionLevel)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(destinationDirectory);
        ArgumentNullExceptionAdvanced.ThrowIfNull(encoding);

        FileDescriptors = sourceFiles ?? Enumerable.Empty<FileDescriptor>();
        FileDescriptorsCount = sourceFilesCount;
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
    public FileBatch(string destinationDirectory, string batchName, Encoding encoding) : this(Enumerable.Empty<FileDescriptor>(), 0, destinationDirectory, batchName, encoding, CompressionLevel.SmallestSize)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding. <see cref="Encoding.UTF8"/> is used as default encoding and <see cref="CompressionLevel.SmallestSize"/> is used as default compression level."
    /// </summary>
    /// <param name="sourceFiles">The source files to be converted.</param>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(IEnumerable<FileDescriptor> sourceFiles, int sourceFilesCount, string destinationDirectory, string batchName) : this(sourceFiles, sourceFilesCount, destinationDirectory, batchName, Encoding.UTF8, CompressionLevel.SmallestSize)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBatch"/> struct with the specified source files, destination directory, batch name, and encoding. <see cref="Encoding.UTF8"/> is used as default encoding and <see cref="CompressionLevel.SmallestSize"/> is used as default compression level."
    /// </summary>
    /// <param name="destinationDirectory">The directory where the converted files will be saved.</param>
    /// <param name="batchName">The name of the batch which is used as the conversion output file name.</param>
    /// <param name="encoding">The encoding to be used for the converted files.</param>
    public FileBatch(string destinationDirectory, string batchName) : this(Enumerable.Empty<FileDescriptor>(), 0, destinationDirectory, batchName, Encoding.UTF8, CompressionLevel.SmallestSize)
    {
    }
}
