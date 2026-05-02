namespace BionicAthlete.Application.Reporting;

using System.Collections.Immutable;

/// <summary>
/// Machine-readable manifest for a View C report folder.
/// </summary>
/// <param name="ReportSchemaVersion">Report manifest schema version.</param>
/// <param name="RendererVersion">Renderer version.</param>
/// <param name="ReportId">Stable report identifier.</param>
/// <param name="SourceFilePath">Source FIT file path when known.</param>
/// <param name="ExportTimestampUtc">Export timestamp supplied through <see cref="ReportExportOptions"/>.</param>
/// <param name="OutputTarget">Requested output target.</param>
/// <param name="PagePreset">Page preset used by the package.</param>
/// <param name="Artifacts">Generated artifacts relative to the report folder.</param>
/// <param name="IncludedSections">Section identifiers included in the HTML report.</param>
/// <param name="Diagnostics">Warnings or caveats emitted by projection/rendering.</param>
public sealed record ReportManifest(
    int ReportSchemaVersion,
    string RendererVersion,
    string ReportId,
    string SourceFilePath,
    DateTimeOffset ExportTimestampUtc,
    ReportOutputTarget OutputTarget,
    ReportPagePreset PagePreset,
    ImmutableArray<ReportManifestArtifact> Artifacts,
    ImmutableArray<string> IncludedSections,
    ImmutableArray<ReportDiagnostic> Diagnostics);
