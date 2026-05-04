namespace BionicAthlete.FileSystem.Abstractions;

using BionicAthlete.Application.Reporting;

public interface IReportManifestHandler
{
    Task AddArtifactToManifestAsync(string manifestFilePath, string relativeArtifactFilePath, string artifactKind, string artifactMediaType, CancellationToken cancellationToken = default);
    ReportManifest CreateManifest(ReportInfo reportInfo);
    ReportManifest UpdateManifest(ReportManifest currentManifest, ReportManifestArtifact reportManifestArtifact);
    Task WriteManifestAsync(string destinationFolder, ReportManifest manifest, CancellationToken cancellationToken);
}