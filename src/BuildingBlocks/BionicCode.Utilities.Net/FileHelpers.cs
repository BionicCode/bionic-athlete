namespace BionicCode.Utilities.Net;

public static class FileHelpers
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

    /// <summary>
    /// Normalizes a file system path by trimming any trailing directory separators and replacing alternate directory
    /// separators with the platform-specific directory separator.
    /// </summary>
    /// <param name="path">The file system path to normalize.</param>
    /// <returns>The normalized file system path.</returns>
    public static string NormalizeFileSystemPath(FileDescriptor path) => NormalizeFileSystemPath(path.FullPath);

    /// <summary>
    /// Normalizes a file system path by trimming any trailing directory separators and replacing alternate directory
    /// separators with the platform-specific directory separator.
    /// </summary>
    /// <param name="path">The file system path to normalize.</param>
    /// <returns>The normalized file system path.</returns>
    public static string NormalizeFileSystemPath(DirectoryDescriptor path) => NormalizeFileSystemPath(path.FullPath);

    /// <summary>
    /// Normalizes a file system path by trimming any trailing directory separators and replacing alternate directory
    /// separators with the platform-specific directory separator.
    /// </summary>
    /// <param name="path">The file system path to normalize.</param>
    /// <returns>The normalized file system path.</returns>
    public static string NormalizeFileSystemPath(string path)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(path);

        string trimmed = Path.TrimEndingDirectorySeparator(path);

        return OperatingSystem.IsWindows()
            ? trimmed.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            : trimmed;
    }

    // Remove leading and trailing directory separator characters to ensure canonical directory name representation.
    public static string NormalizeDirectoryName(string name)
    {
        name = Path.TrimEndingDirectorySeparator(name);

        // Reliably strip leading directory separator characters by getting the file name of the path,
        // which returns the last segment of the path after stripping any trailing directory separator characters.
        return Path.GetFileName(name);
    }
}