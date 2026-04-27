namespace BionicAthlete.Training.Domain;

using System.Collections.Frozen;
using BionicCode.Utilities.Net;

public interface IArchiveManager
{
    Task CreateArchivesAsync(FileBatches fileBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchiveAsync(string archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExtractArchivesAsync(IEnumerable<string> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    bool IsFileTypeSupportedArchive(string filePath);

    /// <summary>
    /// A set of supported archive file extensions, including the leading dot (e.g., ".zip"). The set is immutable and thread-safe.
    /// </summary>
    FrozenSet<string> SupportedArchiveFileExtensions { get; }
}
