namespace BionicAthlete.Infrastructure.FileSystem;

using System.Collections.Frozen;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public class ZipArchiveManager : IArchiveManager, IZipArchiveManager
{
    public FrozenSet<FileExtension> SupportedArchiveFileExtensions { get; }

    private readonly ITemporaryFileManager _temporaryFileManager;

    public ZipArchiveManager(ITemporaryFileManager temporaryFileManager)
    {
        _temporaryFileManager = temporaryFileManager;
        SupportedArchiveFileExtensions = new HashSet<FileExtension>([FileExtensions.Zip])
            .ToFrozenSet();
    }

    public async IAsyncEnumerable<FileDescriptor> ExtractArchivesAsync(IEnumerable<FileDescriptor> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePaths);

        foreach (FileDescriptor archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (FileDescriptor extractedFile in ExtractArchiveAsync(archivePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return extractedFile;
            }
        }
    }

    public async IAsyncEnumerable<FileDescriptor> ExtractArchiveAsync(FileDescriptor archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePath);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsFileTypeSupportedArchive(archivePath))
        {
            throw new NotSupportedException($"Invalid file type: only .zip files are supported. Found: '{archivePath}'.");
        }

        await using ZipArchive zip = await ZipFile.OpenAsync(archivePath.FullPath, ZipArchiveMode.Read, cancellationToken).ConfigureAwait(false);

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

            string subDirectoryName = archivePath.NameWithoutExtension;
            FileDescriptor destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(subDirectoryName, entry.Name);
            _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
            await entry.ExtractToFileAsync(destinationFilePath.FullPath, overwrite: true, cancellationToken).ConfigureAwait(false);

            // Check if file is a nested ZIP and extract recursively
            if (FileExtension.FromFileName(entry.Name).Equals(FileExtensions.Zip))
            {
                await foreach (FileDescriptor nestedFile in ExtractArchiveAsync(destinationFilePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
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

    public async Task CreateArchivesAsync(FileBatches<ArchiveContentBatch> fileBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(fileBatches);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressReporter);

        int totalFileCount = fileBatches.Batches.Sum(batch => batch.FileDescriptorsCount);
        int completedCount = 1;
        foreach (ArchiveContentBatch batch in fileBatches.Batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string zipFileName = $"{batch.BatchName}{FileExtensions.Zip}";
            string zipFilePath = Path.Combine(batch.DestinationDirectory, zipFileName);
            string temporaryDestinationFolderPath = Path.Combine(_temporaryFileManager.TemporaryDirectoryPath.FullPath, batch.BatchName);
            await using var zipFile = new FileStream(
                zipFilePath,
                FileHelpers.WriteOnlyCreateOrOverwriteOptions);
            await using ZipArchive zipArchive = await ZipArchive.CreateAsync(zipFile, ZipArchiveMode.Create, leaveOpen: false, batch.Encoding, cancellationToken);

            foreach (ArchiveContentFileDescriptor fileDescriptor in batch.FileDescriptors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ArchiveContentFileDescriptor sourceFileDescriptor = fileDescriptor;

                if (sourceFileDescriptor.IsEmbeddedResource)
                {
                    FileDescriptor destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(batch.BatchName, sourceFileDescriptor.Name);
                    _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
                    await using Stream resourceStream = sourceFileDescriptor.EmbeddedResourceAssembly.GetManifestResourceStream(sourceFileDescriptor.FullPath) ?? throw new InvalidOperationException($"Failed to get manifest resource stream for embedded resource: {sourceFileDescriptor.Location}");
                    await using var destinationStream = new FileStream(destinationFilePath.FullPath, FileHelpers.WriteOnlyCreateOrOverwriteOptions);
                    await resourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                    sourceFileDescriptor = new(destinationFilePath.FullPath, sourceFileDescriptor.RelativeArchiveEntryDirectoryPath);
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
                    FileDescriptor destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(batch.BatchName, temporaryFileName);
                    _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);

                    // Don't rename the original files but create a copy with the new name in the same location and delete it after packing it to the zip archive
                    File.Copy(sourceFileDescriptor.OriginalFullPath, destinationFilePath.FullPath, overwrite: true);
                    sourceFileDescriptor = new(destinationFilePath.FullPath, sourceFileDescriptor.RelativeArchiveEntryDirectoryPath);
                }

                progressReporter.Report(new ProgressData
                {
                    Progress = completedCount,
                    MaxValue = totalFileCount,
                    Message = $"Packing file #{completedCount} of {totalFileCount} files to {zipFileName}: {sourceFileDescriptor.RelativeArchiveEntryDirectoryPath}"
                });

                cancellationToken.ThrowIfCancellationRequested();

                // Preserve exporter-provided bundle paths so grouped CSV artifacts stay grouped inside the ZIP.
                _ = await zipArchive.CreateEntryFromFileAsync(sourceFileDescriptor.FullPath, sourceFileDescriptor.RelativeArchiveEntryDirectoryPath.FullPath, batch.CompressionLevel, cancellationToken);
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

    public bool IsFileTypeSupportedArchive(FileDescriptor filePath) => SupportedArchiveFileExtensions.Contains(filePath.Extension);
}
