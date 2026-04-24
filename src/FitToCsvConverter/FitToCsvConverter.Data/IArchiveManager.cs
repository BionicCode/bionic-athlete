namespace FitToCsvConverter.Data;

using System.Collections.Frozen;
using BionicCode.Utilities.Net;

public interface IArchiveManager
{
    Task CreateArchivesAsync(FileBatches conversionInfoBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchiveAsync(string archivePath, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchivesAsync(IEnumerable<string> archivePaths, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    bool IsFileTypeSupportedArchive(string filePath);

    /// <summary>
    /// A set of supported archive file extensions, including the leading dot (e.g., ".zip"). The set is immutable and thread-safe.
    /// </summary>
    FrozenSet<string> SupportedArchiveFileExtensions { get; }
}