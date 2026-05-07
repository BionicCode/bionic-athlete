namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Exporting;
using BionicCode.Utilities.Net;

public class ReportManifestBuilder : IReportManifestBuilder
{
    private static readonly ReportManifestHandler s_handler = new();
    private readonly ReportManifest _reportManifest;
    private readonly DirectoryDescriptor _outputFolder;
    public bool IsDirty { get; private set; }

    private ReportManifestBuilder(ReportManifest manifest, DirectoryDescriptor outputFolder)
    {
        _reportManifest = manifest;
        _outputFolder = outputFolder;
    }

    public static async Task<ReportManifestBuilder> CreateAsync(ReportDescriptor reportDescriptor, DirectoryDescriptor outputFolder, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(outputFolder);

        ReportManifest manifest = await s_handler.GetOrCreateManifestAsync(reportDescriptor, outputFolder, cancellationToken);
        return new ReportManifestBuilder(manifest, outputFolder);
    }

    public static ReportManifestBuilder Create(ReportManifest manifest, DirectoryDescriptor outputFolder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(manifest);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(outputFolder);

        return new ReportManifestBuilder(manifest, outputFolder);
    }

    public void AddArtifact(ArtifactKind artifactKind, string relativeArtifactFilePath)
    {
        _reportManifest.AddArtifact(artifactKind, relativeArtifactFilePath);
        IsDirty = true;
    }

    public async Task<ReportManifest> BuildAsync(CancellationToken cancellationToken)
    {
        if (IsDirty)
        {
            await s_handler.WriteManifestAsync(_outputFolder, _reportManifest, cancellationToken);
            IsDirty = false;
        }

        return _reportManifest;
    }
}
