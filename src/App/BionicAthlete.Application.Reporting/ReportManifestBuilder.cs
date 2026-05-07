namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Exporting;
using BionicCode.Utilities.Net;

public class ReportManifestBuilder : IReportManifestBuilder
{
    private static readonly ReportManifestHandler s_handler = new();
    private readonly ReportManifest _reportManifest;
    private readonly string _outputFolder;
    public bool IsDirty { get; private set; }

    private ReportManifestBuilder(ReportManifest manifest, string outputFolder)
    {
        _reportManifest = manifest;
        _outputFolder = outputFolder;
    }

    public static async Task<ReportManifestBuilder> CreateAsync(ReportDescriptor reportDescriptor, string outputFolder, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFolder);

        ReportManifest manifest = await s_handler.GetOrCreateManifestAsync(reportDescriptor, outputFolder, cancellationToken);
        return new ReportManifestBuilder(manifest, outputFolder);
    }

    public static ReportManifestBuilder Create(ReportManifest manifest, string outputFolder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(manifest);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFolder);

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
