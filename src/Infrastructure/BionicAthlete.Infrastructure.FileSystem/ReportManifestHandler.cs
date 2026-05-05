namespace BionicAthlete.Infrastructure.FileSystem;

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BionicAthlete.Application;
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
        string manifestFilePath = Path.Combine(destinationFolder, ArtifactNames.ManifestFileName);
        string json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public ReportManifest CreateManifest(ReportDescriptor reportInfo)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportInfo);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.Add(new ReportManifestArtifact(ArtifactKind.ReportManifest, ArtifactNames.ManifestFileName, ArtifactMediaType.Json));

        return new ReportManifest(
            reportInfo.ReportSchemaVersion,
            reportInfo.RendererVersion,
            reportInfo.ReportId,
            reportInfo.SourceFilePath,
            reportInfo.GeneratedAtUtc,
            reportInfo.OutputTarget,
            reportInfo.PagePreset,
            artifacts.ToImmutable(),
            reportInfo.SectionIds,
            reportInfo.Diagnostics);
    }

    /// <inheritdoc />
    public async Task AddOrUpdateArtifactToManifestAsync(
        string manifestFilePath,
        string relativeArtifactFilePath,
        string artifactKind,
        string artifactMediaType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeArtifactFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactMediaType);

        string json = await File.ReadAllTextAsync(manifestFilePath, cancellationToken).ConfigureAwait(false);
        ReportManifest manifest = JsonSerializer.Deserialize<ReportManifest>(
            json,
            ManifestJsonOptions)
            ?? throw new InvalidOperationException("The report manifest could not be deserialized.");

        var reportManifestArtifact = new ReportManifestArtifact(artifactKind, relativeArtifactFilePath, artifactMediaType);
        ReportManifest updatedManifest = UpdateManifest(manifest, reportManifestArtifact);
        string updatedJson = JsonSerializer.Serialize(updatedManifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, updatedJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public ReportManifest UpdateManifest(ReportManifest currentManifest, ReportManifestArtifact reportManifestArtifact)
    {
        ArgumentNullException.ThrowIfNull(currentManifest);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(reportManifestArtifact);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.AddRange(currentManifest.Artifacts.Where(artifact => !artifact.ArtifactKind.Equals(reportManifestArtifact.ArtifactKind, StringComparison.OrdinalIgnoreCase)));
        artifacts.Add(reportManifestArtifact);

        return currentManifest with { Artifacts = artifacts.ToImmutable() };
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
