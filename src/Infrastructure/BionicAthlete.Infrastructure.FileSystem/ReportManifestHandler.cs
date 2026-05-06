namespace BionicAthlete.Infrastructure.FileSystem;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BionicAthlete.Application.Exporting;
using BionicAthlete.Application.Reporting;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

/// <summary>
/// Updates View C report manifests after UI-bound PDF generation succeeds.
/// </summary>
public class ReportManifestHandler : IReportManifestHandler
{
    private static readonly JsonSerializerOptions s_manifestJsonOptions = CreateManifestJsonOptions();

    internal static JsonSerializerOptions ManifestJsonOptions => s_manifestJsonOptions;

    public async Task WriteManifestAsync(
        string destinationFolder,
        ReportManifest manifest,
        CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(manifest);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(destinationFolder);

        string manifestFilePath = Path.Combine(destinationFolder, ArtifactNames.ManifestFileName);
        string json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        manifest.SetIsCommitted();
    }

    public async Task<ReportManifest> GetOrCreateManifestAsync(ReportDescriptor reportInfo, string outputFolder, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportInfo);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFolder);

        string manifestFilePath = Path.Combine(outputFolder, ArtifactNames.ManifestFileName);
        if (File.Exists(manifestFilePath))
        {
            await using var jsonFile = new FileStream(manifestFilePath, FileHelpers.ReadOnlyOptions);
            return await JsonSerializer.DeserializeAsync<ReportManifest>(jsonFile, ManifestJsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("The report manifest could not be deserialized.");
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
}
