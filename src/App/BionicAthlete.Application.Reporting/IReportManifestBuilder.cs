namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Exporting;

public interface IReportManifestBuilder
{
    bool IsDirty { get; }

    void AddArtifact(ArtifactKind artifactKind, string relativeArtifactFilePath);
    Task<ReportManifest> BuildAsync(CancellationToken cancellationToken);
}