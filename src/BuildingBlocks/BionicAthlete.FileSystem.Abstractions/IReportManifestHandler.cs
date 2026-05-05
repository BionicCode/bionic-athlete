namespace BionicAthlete.FileSystem.Abstractions;

using BionicAthlete.Application.Reporting;

public interface IReportManifestHandler
{
    Task AddOrUpdateArtifactToManifestAsync(string manifestFilePath, string relativeArtifactFilePath, string artifactKind, string artifactMediaType, CancellationToken cancellationToken = default);
    ReportManifest CreateManifest(ReportDescriptor reportInfo);
    ReportManifest UpdateManifest(ReportManifest currentManifest, ReportManifestArtifact reportManifestArtifact);
    Task WriteManifestAsync(string destinationFolder, ReportManifest manifest, CancellationToken cancellationToken);
}