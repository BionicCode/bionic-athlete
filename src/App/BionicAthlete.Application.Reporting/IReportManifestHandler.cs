namespace BionicAthlete.Application.Reporting;

using BionicCode.Utilities.Net;

public interface IReportManifestHandler
{
    Task<ReportManifest> GetOrCreateManifestAsync(ReportDescriptor reportInfo, DirectoryDescriptor location, CancellationToken cancellationToken);
    Task WriteManifestAsync(DirectoryDescriptor destinationFolder, ReportManifest manifest, CancellationToken cancellationToken);
}