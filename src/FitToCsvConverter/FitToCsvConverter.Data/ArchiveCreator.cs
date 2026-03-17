namespace FitToCsvConverter.Data;

using System.IO.Compression;
using BionicCode.Utilities.Net;

public static class ArchiveCreator
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

    public static async Task CreateArchivesAsync(FileBatches conversionInfoBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(conversionInfoBatches);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressReporter);

        int completedCount = 0;
        foreach (FileBatch batch in conversionInfoBatches.Batches)
        {
            string zipFileName = $"{batch.BatchName}.zip";
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
                    string newFileName = Path.Combine(
                        batch.BatchName,
                        Path.GetExtension(sourceFileDescriptor.FullPath));
                    string newFilePath = Path.Combine(
                        sourceFileDescriptor.Location,
                        newFileName);

                    progressReporter.Report(new ProgressData
                    {
                        Progress = (double)completedCount / conversionInfoBatches.TotalConversionCount,
                        Message = $"Renaming file from {sourceFileDescriptor.Name} to {newFileName}"
                    });

                    await using var originalFile = new FileStream(sourceFileDescriptor.FullPath, s_readFileStreamOptions);
                    await using var renamedFile = new FileStream(newFilePath, s_createFileStreamOptions);
                    await originalFile.CopyToAsync(renamedFile, cancellationToken);
                    sourceFileDescriptor = new FileDescriptor(newFileName, sourceFileDescriptor.IsRenamingRequired);
                }

                progressReporter.Report(new ProgressData
                {
                    Progress = (double)completedCount / conversionInfoBatches.TotalConversionCount,
                    Message = $"Packing file {completedCount + 1} of {conversionInfoBatches.TotalConversionCount} to {zipFileName}: {sourceFileDescriptor.Name}"
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
}