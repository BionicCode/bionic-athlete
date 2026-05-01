namespace BionicAthlete.Infrastructure.FileSystem;

using System.Text;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public sealed class TextFileManager : IFileManager<string>
{
    private readonly ITemporaryFileManager _temporaryFileManager;

    public TextFileManager(ITemporaryFileManager temporaryFileManager)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(temporaryFileManager));

        _temporaryFileManager = temporaryFileManager;
    }

    public async Task<string> ReadAsync(string filePath, Encoding encoding, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(filePath));
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(encoding));

        return await File.ReadAllTextAsync(filePath, encoding, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads the content of the specified file using UTF-8 encoding.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read operation. The task result contains the file content as a string.</returns>
    public Task<string> ReadAsync(string filePath, CancellationToken cancellationToken) => ReadAsync(filePath, Encoding.UTF8, cancellationToken);

    public async Task WriteAsync(string value, Encoding encoding, string filePath, bool isOverWriteAllowed, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(value));
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(filePath));
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(encoding));

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath}' already exists and overwriting is not allowed.");
        }

        await File.WriteAllTextAsync(filePath, value, encoding, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task WriteAsync(string value, string filePath, bool isOverWriteAllowed) => WriteAsync(value, Encoding.UTF8, filePath, isOverWriteAllowed, CancellationToken.None);

    public async Task<string> WriteTemporaryAsync(string value, Encoding encoding, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        string destination = _temporaryFileManager.CreateTemporaryFilePath();
        await File.WriteAllTextAsync(destination, value, encoding, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }

    public async Task<string> WriteTemporaryAsync(string value, Encoding encoding, string subdirectoryName, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        string destination = _temporaryFileManager.CreateTemporaryFilePath(subdirectoryName, Path.GetTempFileName());
        await File.WriteAllTextAsync(destination, value, encoding, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }
}
