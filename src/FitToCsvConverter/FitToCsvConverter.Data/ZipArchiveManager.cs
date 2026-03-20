namespace FitToCsvConverter.Data;

using System.IO.Compression;
using System.Runtime.CompilerServices;
using BionicCode.Utilities.Net;

public class ZipArchiveManager : IArchiveManager
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
    private readonly string _temporaryExtractionDestinationDirectory = Path.GetTempPath();

    public async IAsyncEnumerable<string> ExtractArchivesAsync(IEnumerable<string> archivePaths, IProgress<ProgressData>? progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePaths);

        foreach (string archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (string extractedFile in ExtractArchiveAsync(archivePath, progressReporter, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return extractedFile;
            }
        }
    }

    public async IAsyncEnumerable<string> ExtractArchiveAsync(string archivePath, IProgress<ProgressData>? progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(archivePath);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsFileTypeSupportedArchive(archivePath))
        {
            throw new NotSupportedException($"Invalid file type: only .zip files are supported. Found: '{archivePath}'.");
        }

        progressReporter?.Report(new ProgressData
        {
            Progress = 0.0,
            MaxValue = 1.0,
            Message = $"Extracting archive: {archivePath}"
        });

        await using ZipArchive zip = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Read, cancellationToken).ConfigureAwait(false);

        int count = 0;
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressReporter?.Report(new ProgressData
            {
                Progress = (double)count / zip.Entries.Count,
                MaxValue = 1.0,
                Message = $"Extracting file: {count + 1} of {zip.Entries.Count} from archive: {entry.Name}"
            });

            string destinationFileName = Path.Combine(_temporaryExtractionDestinationDirectory, entry.FullName);
            await entry.ExtractToFileAsync(destinationFileName, overwrite: true, cancellationToken).ConfigureAwait(false);

            yield return destinationFileName;
        }

        progressReporter?.Report(new ProgressData
        {
            Progress = 1.0,
            MaxValue = 1.0,
            Message = $"Finished extracting archive: {archivePath}"
        });
    }

    public async Task CreateArchivesAsync(FileBatches conversionInfoBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(conversionInfoBatches);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressReporter);

        int completedCount = 0;
        foreach (FileBatch batch in conversionInfoBatches.Batches)
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
                        Progress = (double)completedCount / conversionInfoBatches.BatchesCount,
                        Message = $"Renaming file from {sourceFileDescriptor.OriginalName} to {sourceFileDescriptor.Name}"
                    });

                    File.Move(sourceFileDescriptor.OriginalFullPath, sourceFileDescriptor.FullPath);
                    sourceFileDescriptor = new FileDescriptor(sourceFileDescriptor.FullPath, sourceFileDescriptor.IsRenamingRequired);
                }

                progressReporter.Report(new ProgressData
                {
                    Progress = (double)completedCount / conversionInfoBatches.BatchesCount,
                    Message = $"Packing file {completedCount + 1} of {conversionInfoBatches.BatchesCount} to {zipFileName}: {sourceFileDescriptor.Name}"
                });

                _ = await zipArchive.CreateEntryFromFileAsync(sourceFileDescriptor.FullPath, sourceFileDescriptor.Name, batch.CompressionLevel, cancellationToken);
            }
        }

        progressReporter.Report(new ProgressData
        {
            Progress = 1.0,
            Message = "All fit files have been successfully exported."
        });
    }

    public bool IsFileTypeSupportedArchive(string filePath)
    {
        bool result = Path.GetExtension(filePath).Equals(ZipFileExtension, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}