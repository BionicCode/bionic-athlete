namespace BionicAthlete.Infrastructure.FileSystem;

using BionicAthlete.Application.Exporting;
using BionicAthlete.Application.Reporting;
using BionicCode.Utilities.Net;

public class ReportManifestBuilder
{
    private static readonly ReportManifestHandler s_handler = new();
    private readonly ReportManifest _reportManifest;
    private readonly string _outputFolder;

    private ReportManifestBuilder(ReportManifest manifest, string outputFolder)
    {
        _reportManifest = manifest;
        _outputFolder = outputFolder;
    }

    public static async Task<ReportManifestBuilder> CreateAsync(ReportDescriptor reportDescriptor, string outputFolder, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFolder);

        ReportManifest manifest = await ReportManifestHandler.GetOrCreateManifestAsync(reportDescriptor, outputFolder, cancellationToken);
        return new ReportManifestBuilder(manifest, outputFolder);
    }

    public static ReportManifestBuilder Create(ReportManifest manifest, string outputFolder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(manifest);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFolder);

        return new ReportManifestBuilder(manifest, outputFolder);
    }

    public void AddArtifact(ArtifactKind artifactKind, string relativeArtifactFilePath) => _reportManifest.AddArtifact(artifactKind, relativeArtifactFilePath);

    public async Task<ReportManifest> BuildAsync(CancellationToken cancellationToken)
    {
        await s_handler.WriteManifestAsync(_outputFolder, _reportManifest, cancellationToken);

        return _reportManifest;
    }
}
