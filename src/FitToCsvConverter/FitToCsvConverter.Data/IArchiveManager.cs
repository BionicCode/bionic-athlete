namespace FitToCsvConverter.Data;

using BionicCode.Utilities.Net;

public interface IArchiveManager
{
    Task CreateArchivesAsync(FileBatches conversionInfoBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchiveAsync(string archivePath, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchivesAsync(IEnumerable<string> archivePaths, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    bool IsFileTypeSupportedArchive(string filePath);
}