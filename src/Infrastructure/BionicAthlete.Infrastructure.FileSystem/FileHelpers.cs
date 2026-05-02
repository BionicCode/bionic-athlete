namespace BionicAthlete.Infrastructure.FileSystem;

internal static class FileHelpers
{
    /// <summary>
    /// Provides default options for creating a new file with read and write access, no sharing, and asynchronous
    /// sequential operations.
    /// </summary>
    /// <remarks>These options configure file streams to allow both reading and writing, prevent other
    /// processes from accessing the file simultaneously, and optimize for asynchronous, sequential access patterns. Use
    /// this instance when opening files that require exclusive access and efficient sequential I/O.
    /// <para/>If the file already exists, it will be overwritten.</remarks>
    public static readonly FileStreamOptions ReadWriteCreateOrOverwriteOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.ReadWrite,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    /// <summary>
    /// Provides preconfigured options for opening a file for asynchronous, sequential read access.
    /// </summary>
    /// <remarks>These options are suitable for scenarios where a file is read from start to finish in a
    /// single pass, such as streaming or processing large files. The file is opened in read-only mode and shared for
    /// reading by other processes.</remarks>
    public static readonly FileStreamOptions ReadOnlyOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    /// <summary>
    /// Provides preconfigured options for creating a new file with write access, exclusive sharing, and asynchronous
    /// sequential operations.
    /// </summary>
    /// <remarks>Use this instance when creating a new file to ensure the file is created exclusively for
    /// writing, with asynchronous and sequential access optimizations. If the file already exist, an
    /// exception is thrown.</remarks>
    public static readonly FileStreamOptions WriteOnlyCreateOptions = new()
    {
        Mode = FileMode.CreateNew,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    /// <summary>
    /// Provides preconfigured options for creating or overwriting a file with asynchronous, sequential write access.
    /// </summary>
    /// <remarks>Use these options when opening a file stream to ensure the file is created if it does not
    /// exist, or overwritten if it does. The file is opened exclusively for writing, and asynchronous, sequential
    /// operations are optimized.</remarks>
    public static readonly FileStreamOptions WriteOnlyCreateOrOverwriteOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };
}