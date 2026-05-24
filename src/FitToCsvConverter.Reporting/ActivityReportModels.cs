namespace FitToCsvConverter.Reporting;

using System.Collections.Immutable;

/// <summary>
/// Semantic, presentation-oriented report model for one decoded activity.
/// </summary>
/// <param name="ReportId">Stable identifier for this generated report.</param>
/// <param name="Title">Human-readable report title.</param>
/// <param name="SourceFilePath">Original source file path when known.</param>
/// <param name="ActivityStartTimeUtc">Canonical activity start time in UTC when available.</param>
/// <param name="GeneratedAtUtc">Timestamp supplied by the caller for deterministic generation.</param>
/// <param name="Sections">Report sections in render order.</param>
/// <param name="Diagnostics">Warnings or caveats discovered while projecting the report.</param>
public sealed record ActivityReport(
    string ReportId,
    string Title,
    string SourceFilePath,
    DateTimeOffset? ActivityStartTimeUtc,
    DateTimeOffset GeneratedAtUtc,
    ImmutableArray<ActivityReportSection> Sections,
    ImmutableArray<ActivityReportDiagnostic> Diagnostics);

/// <summary>
/// A top-level section in a human-readable activity report.
/// </summary>
/// <param name="Id">Stable section identifier used by HTML anchors and the manifest.</param>
/// <param name="Title">Section title.</param>
/// <param name="Metrics">Summary metrics rendered as cards.</param>
/// <param name="Charts">Charts rendered after summary metrics.</param>
/// <param name="Tables">Tables rendered after charts.</param>
/// <param name="Notes">Optional section-level note.</param>
public sealed record ActivityReportSection(
    string Id,
    string Title,
    ImmutableArray<ActivityReportMetric> Metrics,
    ImmutableArray<ActivityReportChart> Charts,
    ImmutableArray<ActivityReportTable> Tables,
    string? Notes = null);

/// <summary>
/// A report metric with display text and provenance metadata.
/// </summary>
/// <param name="CanonicalName">Stable canonical name, independent of Garmin Connect labels.</param>
/// <param name="Label">Human-readable label for View C.</param>
/// <param name="FormattedValue">Value already formatted for the selected culture and report target.</param>
/// <param name="Unit">Optional display unit.</param>
/// <param name="Classification">How the metric relates to source FIT data.</param>
/// <param name="SourceField">Canonical source field when one exists.</param>
/// <param name="ProvenanceNote">Optional caveat or derivation note.</param>
public sealed record ActivityReportMetric(
    string CanonicalName,
    string Label,
    string FormattedValue,
    string? Unit,
    ActivityReportFieldClassification Classification,
    string? SourceField = null,
    string? ProvenanceNote = null);

/// <summary>
/// A deterministic SVG chart definition.
/// </summary>
/// <param name="Id">Stable chart identifier used as the SVG id.</param>
/// <param name="Title">Chart title.</param>
/// <param name="ValueLabel">Axis/value label.</param>
/// <param name="Unit">Optional value unit.</param>
/// <param name="Points">Data points in source order.</param>
public sealed record ActivityReportChart(
    string Id,
    string Title,
    string ValueLabel,
    string? Unit,
    ImmutableArray<ActivityReportChartPoint> Points);

/// <summary>
/// One point in a report chart.
/// </summary>
/// <param name="TimestampUtc">Source timestamp in UTC.</param>
/// <param name="Value">Numeric value to plot.</param>
public sealed record ActivityReportChartPoint(DateTimeOffset TimestampUtc, double Value);

/// <summary>
/// A deterministic report table.
/// </summary>
/// <param name="Id">Stable table identifier.</param>
/// <param name="Title">Table title.</param>
/// <param name="Columns">Columns in display order.</param>
/// <param name="Rows">Rows in display order.</param>
public sealed record ActivityReportTable(
    string Id,
    string Title,
    ImmutableArray<ActivityReportTableColumn> Columns,
    ImmutableArray<ActivityReportTableRow> Rows);

/// <summary>
/// A report table column.
/// </summary>
/// <param name="Key">Stable column key.</param>
/// <param name="Header">Column header.</param>
public sealed record ActivityReportTableColumn(string Key, string Header);

/// <summary>
/// A report table row.
/// </summary>
/// <param name="Cells">Cell values aligned to <see cref="ActivityReportTable.Columns"/>.</param>
public sealed record ActivityReportTableRow(ImmutableArray<string> Cells);

/// <summary>
/// A warning, caveat, or informational diagnostic emitted while creating a report.
/// </summary>
/// <param name="Code">Stable diagnostic code.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
public sealed record ActivityReportDiagnostic(string Code, string Message);

/// <summary>
/// Result of writing a View C HTML report package to disk.
/// </summary>
/// <param name="ReportDirectoryPath">Physical report folder path.</param>
/// <param name="HtmlFilePath">Physical path to <c>activity-report.html</c>.</param>
/// <param name="ManifestFilePath">Physical path to <c>report-manifest.json</c>.</param>
/// <param name="PdfFilePath">Physical path to <c>activity-report.pdf</c> when a PDF target was requested.</param>
/// <param name="OutputTarget">Requested output target.</param>
/// <param name="PageSettings">Neutral page settings used by the package.</param>
/// <param name="Diagnostics">Warnings emitted while generating the package.</param>
public sealed record HtmlReportPackage(
    string ReportDirectoryPath,
    string HtmlFilePath,
    string ManifestFilePath,
    string? PdfFilePath,
    ActivityReportOutputTarget OutputTarget,
    ActivityReportPageSettings PageSettings,
    ImmutableArray<ActivityReportDiagnostic> Diagnostics);
