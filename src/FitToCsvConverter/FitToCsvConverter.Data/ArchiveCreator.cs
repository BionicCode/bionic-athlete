namespace FitToCsvConverter.Data;

using System.IO.Compression;
using BionicCode.Utilities.Net;

public static class ArchiveCreator
{

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
                new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.ReadWrite,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous
                });
            await using ZipArchive zipArchive = await ZipArchive.CreateAsync(zipFile, ZipArchiveMode.Create, leaveOpen: false, batch.Encoding, cancellationToken);
            foreach (FileDescriptor sourceFileDescriptor in batch.FileDescriptors)
            {
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