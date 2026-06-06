namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Exporting;
using BionicCode.Utilities.Net;

public interface IReportManifestBuilder
{
    bool IsDirty { get; }

    void AddArtifact(ArtifactKind artifactKind, FileSystemPathDescriptor relativeArtifactFilePath);
    Task<ReportManifest> BuildAsync(CancellationToken cancellationToken);
}