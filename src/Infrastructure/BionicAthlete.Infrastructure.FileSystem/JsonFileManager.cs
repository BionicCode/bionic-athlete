namespace BionicAthlete.Infrastructure.FileSystem;

using System;
using System.Text;
using System.Text.Json;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public sealed class JsonFileManager<TValue> : IFileManager<TValue>
{
    private readonly ITemporaryFileManager _temporaryFileManager;
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public JsonFileManager(ITemporaryFileManager temporaryFileManager)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(nameof(temporaryFileManager));

        _temporaryFileManager = temporaryFileManager;
    }

    public TValue Read(FileSystemPathDescriptor filePath, Encoding encoding)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        using var fileStream = new FileStream(filePath.FullPath, FileHelpers.ReadOnlyOptions);
        TValue? value = JsonSerializer.Deserialize<TValue>(fileStream, JsonSerializerOptions);

        return value ?? throw new InvalidOperationException($"Deserialization of file '{filePath}' resulted in a null value.");
    }

    public async Task<TValue> ReadAsync(FileSystemPathDescriptor filePath, Encoding encoding, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        await using var fileStream = new FileStream(filePath.FullPath, FileHelpers.ReadOnlyOptions);
        TValue? value = await JsonSerializer.DeserializeAsync<TValue>(fileStream, JsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return value ?? throw new InvalidOperationException($"Deserialization of file '{filePath}' resulted in a null value.");
    }

    /// <summary>
    /// Asynchronously reads the content of the specified file using UTF-8 encoding.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read operation. The task result contains the file content as a string.</returns>
    public Task<TValue> ReadAsync(FileSystemPathDescriptor filePath, CancellationToken cancellationToken) => ReadAsync(filePath, Encoding.UTF8, cancellationToken);

    public void Write(TValue value, Encoding encoding, FileSystemPathDescriptor filePath, bool isOverWriteAllowed)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath.FullPath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath}' already exists and overwriting is not allowed.");
        }

        FileStreamOptions fileStreamOptions = isOverWriteAllowed
            ? FileHelpers.WriteOnlyCreateOrOverwriteOptions
            : FileHelpers.WriteOnlyCreateOptions;
        using var fileStream = new FileStream(filePath.FullPath, fileStreamOptions);
        JsonSerializer.Serialize(fileStream, value, JsonSerializerOptions);
    }

    public async Task WriteAsync(TValue value, Encoding encoding, FileSystemPathDescriptor filePath, bool isOverWriteAllowed, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(filePath);

        if (!isOverWriteAllowed)
        {
            ArgumentExceptionAdvanced.ThrowIfTrue(File.Exists(filePath.FullPath), $"Invalid argument '{nameof(filePath)}'. The file at path '{filePath}' already exists and overwriting is not allowed.");
        }

        FileStreamOptions fileStreamOptions = isOverWriteAllowed
            ? FileHelpers.WriteOnlyCreateOrOverwriteOptions
            : FileHelpers.WriteOnlyCreateOptions;
        await using var fileStream = new FileStream(filePath.FullPath, fileStreamOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, JsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task WriteAsync(TValue value, FileSystemPathDescriptor filePath, bool isOverWriteAllowed) => WriteAsync(value, Encoding.UTF8, filePath, isOverWriteAllowed, CancellationToken.None);

    public async Task<FileSystemPathDescriptor> WriteTemporaryAsync(TValue value, Encoding encoding, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);

        FileSystemPathDescriptor destination = _temporaryFileManager.CreateTemporaryFilePath();
        await using var fileStream = new FileStream(destination.FullPath, FileHelpers.WriteOnlyCreateOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, JsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }

    public async Task<FileSystemPathDescriptor> WriteTemporaryAsync(TValue value, Encoding encoding, string subdirectoryName, bool isTemporaryFileManaged, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(subdirectoryName);

        FileSystemPathDescriptor destination = _temporaryFileManager.CreateTemporaryFilePath(subdirectoryName, Path.GetTempFileName());
        await using var fileStream = new FileStream(destination.FullPath, FileHelpers.WriteOnlyCreateOptions);
        await JsonSerializer.SerializeAsync(fileStream, value, JsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (isTemporaryFileManaged)
        {
            _temporaryFileManager.RegisterTemporaryFilePath(destination);
        }

        return destination;
    }
}
