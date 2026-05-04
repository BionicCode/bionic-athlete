namespace BionicAthlete.FileSystem.Abstractions;

using BionicAthlete.Application.Reporting;

public interface IReportManifestHandler
{
    Task AddPdfArtifactAsync(string manifestFilePath, CancellationToken cancellationToken = default);
    ReportManifest CreateManifest(ReportInfo reportInfo, bool includePdfArtifact);
    ReportManifest UpdateManifest(ReportManifest currentManifest);
    Task WriteManifestAsync(string destinationFolder, ReportManifest manifest, CancellationToken cancellationToken);
}