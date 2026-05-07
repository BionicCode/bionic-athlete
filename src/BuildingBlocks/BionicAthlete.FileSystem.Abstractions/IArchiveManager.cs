namespace BionicAthlete.FileSystem.Abstractions;

using System.Collections.Frozen;
using BionicCode.Utilities.Net;

public interface IArchiveManager
{
    Task CreateArchivesAsync(FileBatches fileBatches, IProgress<ProgressData> progressReporter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FileDescriptor> ExtractArchiveAsync(FileDescriptor archivePath, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FileDescriptor> ExtractArchivesAsync(IEnumerable<FileDescriptor> archivePaths, Func<int, string, IProgress<ProgressData>>? progressReporterFactory, CancellationToken cancellationToken = default);
    bool IsFileTypeSupportedArchive(FileDescriptor filePath);

    /// <summary>
    /// A set of supported archive file extensions, including the leading dot (e.g., ".zip"). The set is immutable and thread-safe.
    /// </summary>
    FrozenSet<FileExtension> SupportedArchiveFileExtensions { get; }
}
