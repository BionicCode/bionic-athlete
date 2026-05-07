namespace BionicAthlete.Application.Reporting;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BionicAthlete.Application.Exporting;
using BionicAthlete.Infrastructure.FileSystem;
using BionicCode.Utilities.Net;

/// <summary>
/// Updates View C report manifests after UI-bound PDF generation succeeds.
/// </summary>
internal class ReportManifestHandler : IReportManifestHandler
{
    private static readonly JsonSerializerOptions s_manifestJsonOptions = CreateManifestJsonOptions();
    private readonly JsonFileManager<ReportManifest> _jsonFileManager = new();

    internal static JsonSerializerOptions ManifestJsonOptions => s_manifestJsonOptions;

    public async Task WriteManifestAsync(
        string destinationFolder,
        ReportManifest manifest,
        CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(manifest);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(destinationFolder);

        string manifestFilePath = Path.Combine(destinationFolder, ArtifactNames.ManifestFileName);
        await _jsonFileManager.WriteAsync(manifest, Encoding.UTF8, manifestFilePath, isOverWriteAllowed: true, cancellationToken).ConfigureAwait(false);
        manifest.SetIsCommitted();
    }

    public async Task<ReportManifest> GetOrCreateManifestAsync(ReportDescriptor reportInfo, DirectoryDescriptor location, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportInfo);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(location);

        string manifestFilePath = Path.Combine(location.FullPath, ArtifactNames.ManifestFileName);
        if (File.Exists(manifestFilePath))
        {
            return await _jsonFileManager.ReadAsync(manifestFilePath, cancellationToken).ConfigureAwait(false);
        }

        ReportManifest manifest = new(
            reportInfo.ReportSchemaVersion,
            reportInfo.RendererVersion,
            reportInfo.ReportId,
            reportInfo.SourceFilePath,
            reportInfo.GeneratedAtUtc,
            reportInfo.OutputTarget,
            reportInfo.PagePreset,
            reportInfo.SectionIds,
            reportInfo.Diagnostics);
        manifest.AddArtifact(ArtifactKind.ReportManifest, ArtifactNames.ManifestFileName);

        return manifest;
    }

    private static JsonSerializerOptions CreateManifestJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public Task WriteManifestAsync(DirectoryDescriptor destinationFolder, ReportManifest manifest, CancellationToken cancellationToken) => throw new NotImplementedException();
}
