namespace BionicAthlete.Infrastructure.FileSystem;

using System;
using System.Text;
using System.Text.Json;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public sealed class JsonFileManager<TValue> : IFileManager<TValue>
{
    private readonly ITemporaryFileManager _temporaryFileManager;

    public JsonFileManager(ITemporaryFileManager temporaryFileManager)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(temporaryFileManager));

        _temporaryFileManager = temporaryFileManager;
    }

    public async Task<TValue> ReadAsync(string filePath, Encoding encoding, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(filePath));
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(encoding));

        await using var fileStream = new FileStream(filePath, FileHelpers.ReadOptions);
        TValue? value = await JsonSerializer.DeserializeAsync<TValue>(fileStream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return value ?? throw new InvalidOperationException($"Deserialization of file '{filePath}' resulted in a null value.");
    }

    /// <summary>
    /// Asynchronously reads the content of the specified file using UTF-8 encoding.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read operation. The task result contains the file content as a string.</returns>
    public Task<TValue> ReadAsync(string filePath, CancellationToken cancellationToken) => ReadAsync(filePath, Encoding.UTF8, cancellationToken);

    public async Task WriteAsync(TValue value, Encoding encoding, string filePath, bool isOverWriteAllowed, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(value));
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(nameof(filePath));
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(encoding));

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath}' already exists and overwriting is not allowed.");
        }

        FileStreamOptions fileStreamOptions = isOverWriteAllowed
            ? FileHelpers.WriteOnlyCreateOrOverwriteOptions
            : FileHelpers.WriteOnlyCreateOptions;
        await using var fileStream = new FileStream(filePath, fileStreamOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task WriteAsync(TValue value, string filePath, bool isOverWriteAllowed) => WriteAsync(value, Encoding.UTF8, filePath, isOverWriteAllowed, CancellationToken.None);

    public async Task<string> WriteTemporaryAsync(TValue value, Encoding encoding, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        string destination = _temporaryFileManager.CreateTemporaryFilePath();
        await using var fileStream = new FileStream(destination, FileHelpers.WriteOnlyCreateOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }

    public async Task<string> WriteTemporaryAsync(TValue value, Encoding encoding,string subdirectoryName, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        string destination = _temporaryFileManager.CreateTemporaryFilePath(subdirectoryName, Path.GetTempFileName());
        await using var fileStream = new FileStream(destination, FileHelpers.WriteOnlyCreateOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }
}
