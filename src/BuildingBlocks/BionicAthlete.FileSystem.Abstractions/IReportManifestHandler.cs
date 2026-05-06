namespace BionicAthlete.FileSystem.Abstractions;

using BionicAthlete.Application.Reporting;

public interface IReportManifestHandler
{
    Task<ReportManifest> GetOrCreateManifestAsync(ReportDescriptor reportInfo, string outputFolder, CancellationToken cancellationToken);
    Task WriteManifestAsync(string destinationFolder, ReportManifest manifest, CancellationToken cancellationToken);
}