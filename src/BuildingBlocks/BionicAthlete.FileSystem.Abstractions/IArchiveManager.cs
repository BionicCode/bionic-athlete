namespace BionicAthlete.FileSystem.Abstractions;

using System.Collections.Frozen;
using BionicCode.Utilities.Net;

public interface IArchiveManager
{
    Task CreateArchivesAsync(FileBatches<ArchiveContentBatch> fileBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FileSystemPathDescriptor> ExtractArchiveAsync(FileSystemPathDescriptor archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FileSystemPathDescriptor> ExtractArchivesAsync(IEnumerable<FileSystemPathDescriptor> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    bool IsFileTypeSupportedArchive(FileSystemPathDescriptor filePath);

    /// <summary>
    /// A set of supported archive file extensions, including the leading dot (e.g., ".zip"). The set is immutable and thread-safe.
    /// </summary>
    FrozenSet<FileExtension> SupportedArchiveFileExtensions { get; }
}
