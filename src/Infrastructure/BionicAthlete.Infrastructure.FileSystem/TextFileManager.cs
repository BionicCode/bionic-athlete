namespace BionicAthlete.Infrastructure.FileSystem;

using System.Text;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public sealed class TextFileManager : IFileManager<string>
{
    private readonly Encoding _defaultEncoding = Encoding.UTF8;
    private readonly ITemporaryFileManager _temporaryFileManager;

    public TextFileManager(ITemporaryFileManager temporaryFileManager)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(temporaryFileManager));

        _temporaryFileManager = temporaryFileManager;
    }

    public string Read(FileDescriptor filePath, Encoding encoding)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        encoding ??= _defaultEncoding;

        return File.ReadAllText(filePath.FullPath, encoding);
    }

    public async Task<string> ReadAsync(FileDescriptor filePath, Encoding encoding, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        encoding ??= _defaultEncoding;

        return await File.ReadAllTextAsync(filePath.FullPath, encoding, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads the content of the specified file using UTF-8 encoding.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read operation. The task result contains the file content as a string.</returns>
    public Task<string> ReadAsync(FileDescriptor filePath, CancellationToken cancellationToken) => ReadAsync(filePath, Encoding.UTF8, cancellationToken);
    public void Write(string value, Encoding encoding, FileDescriptor filePath, bool isOverWriteAllowed)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(value);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        encoding ??= _defaultEncoding;

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath.FullPath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath.FullPath}' already exists and overwriting is not allowed.");
        }

        File.WriteAllText(filePath.FullPath, value, encoding);
    }

    public async Task WriteAsync(string value, Encoding encoding, FileDescriptor filePath, bool isOverWriteAllowed, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(value);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        encoding ??= _defaultEncoding;

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath.FullPath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath}' already exists and overwriting is not allowed.");
        }

        await File.WriteAllTextAsync(filePath.FullPath, value, encoding, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task WriteAsync(string value, FileDescriptor filePath, bool isOverWriteAllowed) => WriteAsync(value, Encoding.UTF8, filePath, isOverWriteAllowed, CancellationToken.None);

    /// <inheritdoc/>>
    public async Task<FileDescriptor> WriteTemporaryAsync(string value, Encoding encoding, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(value);

        FileDescriptor destination = _temporaryFileManager.CreateTemporaryFilePath();
        await File.WriteAllTextAsync(destination.FullPath, value, encoding, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }

    /// <inheritdoc/>>
    public async Task<FileDescriptor> WriteTemporaryAsync(string value, Encoding encoding, string subdirectoryName, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(value);

        FileDescriptor destination = _temporaryFileManager.CreateTemporaryFilePath(subdirectoryName, Path.GetTempFileName());
        await File.WriteAllTextAsync(destination.FullPath, value, encoding, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }
}
