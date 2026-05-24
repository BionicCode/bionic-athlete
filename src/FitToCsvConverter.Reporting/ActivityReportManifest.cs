namespace FitToCsvConverter.Reporting;

using System.Collections.Immutable;

/// <summary>
/// Machine-readable manifest for a View C report folder.
/// </summary>
/// <param name="ReportSchemaVersion">Report manifest schema version.</param>
/// <param name="RendererVersion">Renderer version.</param>
/// <param name="ReportId">Stable report identifier.</param>
/// <param name="SourceFilePath">Source FIT file path when known.</param>
/// <param name="ExportTimestampUtc">Export timestamp supplied through <see cref="ActivityReportExportOptions"/>.</param>
/// <param name="OutputTarget">Requested output target.</param>
/// <param name="PagePreset">Page preset used by the package.</param>
/// <param name="Artifacts">Generated artifacts relative to the report folder.</param>
/// <param name="IncludedSections">Section identifiers included in the HTML report.</param>
/// <param name="Diagnostics">Warnings or caveats emitted by projection/rendering.</param>
public sealed record ActivityReportManifest(
    int ReportSchemaVersion,
    string RendererVersion,
    string ReportId,
    string SourceFilePath,
    DateTimeOffset ExportTimestampUtc,
    ActivityReportOutputTarget OutputTarget,
    ActivityReportPagePreset PagePreset,
    ImmutableArray<ActivityReportManifestArtifact> Artifacts,
    ImmutableArray<string> IncludedSections,
    ImmutableArray<ActivityReportDiagnostic> Diagnostics);

/// <summary>
/// A file written as part of a report package.
/// </summary>
/// <param name="ArtifactKind">Artifact kind, such as <c>HtmlReport</c> or <c>PdfReport</c>.</param>
/// <param name="RelativePath">Path relative to the report folder.</param>
/// <param name="MediaType">Media type for the artifact.</param>
public sealed record ActivityReportManifestArtifact(string ArtifactKind, string RelativePath, string MediaType);
