namespace BionicAthlete.Infrastructure.FileSystem;

using System.Collections.Frozen;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public class ZipArchiveManager : IArchiveManager, IZipArchiveManager
{
    public const string ZipFileExtension = ".zip";
    public FrozenSet<string> SupportedArchiveFileExtensions { get; }

    private readonly ITemporaryFileManager _temporaryFileManager;

    public ZipArchiveManager(ITemporaryFileManager temporaryFileManager)
    {
        _temporaryFileManager = temporaryFileManager;
        SupportedArchiveFileExtensions = new HashSet<string>([ZipFileExtension], StringComparer.OrdinalIgnoreCase)
            .ToFrozenSet();
    }

    public async IAsyncEnumerable<string> ExtractArchivesAsync(IEnumerable<string> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePaths);

        foreach (string archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (string extractedFile in ExtractArchiveAsync(archivePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return extractedFile;
            }
        }
    }

    public async IAsyncEnumerable<string> ExtractArchiveAsync(string archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePath);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsFileTypeSupportedArchive(archivePath))
        {
            throw new NotSupportedException($"Invalid file type: only .zip files are supported. Found: '{archivePath}'.");
        }

        await using ZipArchive zip = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Read, cancellationToken).ConfigureAwait(false);

        int count = 1;
        IProgress<ProgressData>? progressReporter = progressReporterFactory?.Invoke(zip.Entries.Count, $"Extracting archive: {archivePath}");
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressReporter?.Report(new ProgressData
            {
                Progress = count,
                MaxValue = zip.Entries.Count,
                Message = $"Extracting file: {count} of {zip.Entries.Count} from archive: {entry.Name}"
            });

            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            string subDirectoryName = Path.GetFileNameWithoutExtension(archivePath);
            string destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(subDirectoryName, entry.Name);
            _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
            await entry.ExtractToFileAsync(destinationFilePath, overwrite: true, cancellationToken).ConfigureAwait(false);

            if (Path.GetExtension(entry.Name).Equals(ZipFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                await foreach (string nestedFile in ExtractArchiveAsync(destinationFilePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
                {
                    yield return nestedFile;
                }
            }
            else
            {
                yield return destinationFilePath;
            }

            count++;
        }

        progressReporter?.Report(new ProgressData
        {
            Progress = zip.Entries.Count,
            MaxValue = zip.Entries.Count,
            Message = $"Completed extracting archive: {archivePath}"
        });
    }

    public async Task CreateArchivesAsync(FileBatches fileBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(fileBatches);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressReporter);

        int totalFileCount = fileBatches.Batches.Sum(batch => batch.FileDescriptorsCount);
        int completedCount = 1;
        foreach (FileBatch batch in fileBatches.Batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string zipFileName = $"{batch.BatchName}{ZipFileExtension}";
            string zipFilePath = Path.Combine(batch.DestinationDirectory, zipFileName);
            string temporaryDestinationFolderPath = Path.Combine(_temporaryFileManager.TemporaryDirectoryPath, batch.BatchName);
            await using var zipFile = new FileStream(
                zipFilePath,
                FileHelpers.WriteOnlyCreateOrOverwriteOptions);
            await using ZipArchive zipArchive = await ZipArchive.CreateAsync(zipFile, ZipArchiveMode.Create, leaveOpen: false, batch.Encoding, cancellationToken);

            foreach (FileDescriptor fileDescriptor in batch.FileDescriptors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileDescriptor sourceFileDescriptor = fileDescriptor;

                if (sourceFileDescriptor.IsEmbeddedResource)
                {
                    string destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(batch.BatchName, sourceFileDescriptor.Name);
                    _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
                    await using Stream resourceStream = sourceFileDescriptor.EmbeddedResourceAssembly.GetManifestResourceStream(sourceFileDescriptor.FullPath) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource: {sourceFileDescriptor.Location}");
                    await using var destinationStream = new FileStream(destinationFilePath, FileHelpers.WriteOnlyCreateOrOverwriteOptions);
                    await resourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                    sourceFileDescriptor = new FileDescriptor(destinationFilePath, sourceFileDescriptor.HasRenamingInformation);
                }

                if (sourceFileDescriptor.HasRenamingInformation)
                {
                    progressReporter.Report(new ProgressData
                    {
                        Progress = completedCount,
                        MaxValue = totalFileCount,
                        Message = $"Renaming file from {sourceFileDescriptor.OriginalName} to {sourceFileDescriptor.Name}"
                    });

                    cancellationToken.ThrowIfCancellationRequested();

                    string temporaryFileName = _temporaryFileManager.MakeFileNameUnique(sourceFileDescriptor.Name);
                    string destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(batch.BatchName, temporaryFileName);
                    _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);

                    // Don't rename the original files but create a copy with the new name in the same location and delete it after packing it to the zip archive
                    File.Copy(sourceFileDescriptor.OriginalFullPath, destinationFilePath, overwrite: true);
                    sourceFileDescriptor = new FileDescriptor(destinationFilePath, sourceFileDescriptor.HasRenamingInformation);
                }

                progressReporter.Report(new ProgressData
                {
                    Progress = completedCount,
                    MaxValue = totalFileCount,
                    Message = $"Packing file #{completedCount} of {totalFileCount} files to {zipFileName}: {sourceFileDescriptor.ArchiveEntryName}"
                });

                cancellationToken.ThrowIfCancellationRequested();

                // Preserve exporter-provided bundle paths so grouped CSV artifacts stay grouped inside the ZIP.
                _ = await zipArchive.CreateEntryFromFileAsync(sourceFileDescriptor.FullPath, sourceFileDescriptor.ArchiveEntryName, batch.CompressionLevel, cancellationToken);
                completedCount++;
            }
        }

        progressReporter.Report(new ProgressData
        {
            Progress = totalFileCount,
            MaxValue = totalFileCount,
            Message = "All ZIP archives have been successfully created."
        });
    }

    public bool IsFileTypeSupportedArchive(string filePath) => SupportedArchiveFileExtensions.Contains(Path.GetExtension(filePath));
}
