namespace BionicAthlete.Infrastructure.FileSystem;

using System.Collections.Frozen;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public class ZipArchiveManager : IArchiveManager, IZipArchiveManager
{
    public FrozenSet<FileExtension> SupportedArchiveFileExtensions { get; }

    private static readonly ArchiveEntryComparer s_entryComparer = new ArchiveEntryComparer();
    private readonly ITemporaryFileManager _temporaryFileManager;

    public ZipArchiveManager(ITemporaryFileManager temporaryFileManager)
    {
        _temporaryFileManager = temporaryFileManager;
        SupportedArchiveFileExtensions = new HashSet<FileExtension>([FileExtensions.Zip])
            .ToFrozenSet();
    }

    public async IAsyncEnumerable<FileSystemPathDescriptor> ExtractArchivesAsync(IEnumerable<FileSystemPathDescriptor> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePaths);

        foreach (FileSystemPathDescriptor archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (FileSystemPathDescriptor extractedFile in ExtractArchiveAsync(archivePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return extractedFile;
            }
        }
    }

    public async IAsyncEnumerable<FileSystemPathDescriptor> ExtractArchiveAsync(FileSystemPathDescriptor archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
            FileSystemPathDescriptor destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(subDirectoryName, entry.Name);
            _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
            await entry.ExtractToFileAsync(destinationFilePath.FullPath, overwrite: true, cancellationToken).ConfigureAwait(false);

            // Check if file is a nested ZIP and extract recursively
            if (FileExtension.FromFileName(entry.Name).Equals(FileExtensions.Zip))
            {
                await foreach (FileSystemPathDescriptor nestedFile in ExtractArchiveAsync(destinationFilePath, progressReporterFactory, cancellationToken).ConfigureAwait(false))
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
            string temporaryDestinationFolderPath = Path.Combine(_temporaryFileManager.TemporaryDirectoryPath.PathString, batch.BatchName);
            await using var zipFile = new FileStream(
                zipFilePath,
                FileHelpers.WriteOnlyCreateOrOverwriteOptions);
            await using ZipArchive zipArchive = await ZipArchive.CreateAsync(zipFile, ZipArchiveMode.Create, leaveOpen: false, batch.Encoding, cancellationToken);

            HashSet<PathDescriptor> entryPaths = new(s_entryComparer);
            foreach (ArchiveEntryDescriptor archiveEntryDescriptor in batch.FileDescriptors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!entryPaths.Add(archiveEntryDescriptor.EntryPath))
                {
                    throw new InvalidOperationException($"Duplicate archive entry path detected: '{archiveEntryDescriptor.EntryPath}'");
                }

                FileDescriptor sourceFileDescriptor = archiveEntryDescriptor.SourceFile;

                progressReporter.Report(new ProgressData
                {
                    Progress = completedCount,
                    MaxValue = totalFileCount,
                    Message = $"Packing file #{completedCount} of {totalFileCount} files to {zipFileName}: {archiveEntryDescriptor.EntryPath}"
                });

                if (sourceFileDescriptor is EmbeddedResourceDescriptor embeddedResourceFileDescriptor)
                {
                    ZipArchiveEntry entry = zipArchive.CreateEntry(archiveEntryDescriptor.EntryPath, batch.CompressionLevel);
                    await using Stream entryStream = await entry.OpenAsync(cancellationToken);
                    await embeddedResourceFileDescriptor.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(true);
                }
                else if (sourceFileDescriptor is FileSystemPathDescriptor fileSystemPathDescriptor)
                {
                    _ = await zipArchive.CreateEntryFromFileAsync(sourceFileDescriptor, archiveEntryDescriptor.EntryPath, batch.CompressionLevel, cancellationToken);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported file descriptor type: {sourceFileDescriptor.GetType().FullName}. Only {typeof(EmbeddedResourceDescriptor).FullName} and {typeof(FileSystemPathDescriptor).FullName} are supported.");
                }

                cancellationToken.ThrowIfCancellationRequested();
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

    public bool IsFileTypeSupportedArchive(FileSystemPathDescriptor filePath) => filePath is not null
        && SupportedArchiveFileExtensions.Contains(filePath.Extension);
}
