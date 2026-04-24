namespace FitToCsvConverter.Data;

using System.Collections.Frozen;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using BionicCode.Utilities.Net;

public class ZipArchiveManager : IArchiveManager, IZipArchiveManager
{
    private static readonly FileStreamOptions s_zipFileStreamOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.ReadWrite,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    private static readonly FileStreamOptions s_readFileStreamOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    private static readonly FileStreamOptions s_createFileStreamOptions = new()
    {
        Mode = FileMode.CreateNew,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

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

            string temporaryFileName = _temporaryFileManager.MakeFileNameUnique(entry.Name);
            string destinationFileName = _temporaryFileManager.CreateTemporaryFilePath(temporaryFileName);
            _temporaryFileManager.RegisterTemporaryFilePath(destinationFileName);
            await entry.ExtractToFileAsync(destinationFileName, overwrite: true, cancellationToken).ConfigureAwait(false);

            if (Path.GetExtension(entry.Name).Equals(ZipFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                await foreach (string nestedFile in ExtractArchiveAsync(destinationFileName, progressReporterFactory, cancellationToken).ConfigureAwait(false))
                {
                    yield return nestedFile;
                }
            }
            else
            {
                yield return destinationFileName;
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
            string zipFileName = $"{batch.BatchName}{ZipFileExtension}";
            string zipFilePath = Path.Combine(batch.DestinationDirectory, zipFileName);
            await using var zipFile = new FileStream(
                zipFilePath,
                s_zipFileStreamOptions);
            await using ZipArchive zipArchive = await ZipArchive.CreateAsync(zipFile, ZipArchiveMode.Create, leaveOpen: false, batch.Encoding, cancellationToken);

            foreach (FileDescriptor fileDescriptor in batch.FileDescriptors)
            {
                FileDescriptor sourceFileDescriptor = fileDescriptor;

                if (sourceFileDescriptor.IsRenamingRequired)
                {
                    progressReporter.Report(new ProgressData
                    {
                        Progress = completedCount,
                        MaxValue = totalFileCount,
                        Message = $"Renaming file from {sourceFileDescriptor.OriginalName} to {sourceFileDescriptor.Name}"
                    });

                    string temporaryFileName = _temporaryFileManager.MakeFileNameUnique(sourceFileDescriptor.Name);
                    string destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(temporaryFileName);

                    // Don't rename the original files but create a copy with the new name in the same location and delete it after packing it to the zip archive
                    File.Copy(sourceFileDescriptor.OriginalFullPath, destinationFilePath, overwrite: true);

                    _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
                    sourceFileDescriptor = new FileDescriptor(destinationFilePath, sourceFileDescriptor.IsRenamingRequired);
                }

                progressReporter.Report(new ProgressData
                {
                    Progress = completedCount,
                    MaxValue = totalFileCount,
                    Message = $"Packing file #{completedCount} of {totalFileCount} files to {zipFileName}: {sourceFileDescriptor.Name}"
                });

                _ = await zipArchive.CreateEntryFromFileAsync(sourceFileDescriptor.FullPath, sourceFileDescriptor.Name, batch.CompressionLevel, cancellationToken);
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
