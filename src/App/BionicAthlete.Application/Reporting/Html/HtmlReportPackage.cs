namespace BionicAthlete.Application.Reporting.Html;

using System.Collections.Immutable;
using BionicAthlete.Application.Reporting;

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
    ReportOutputTarget OutputTarget,
    PageSettings PageSettings,
    ImmutableArray<ReportDiagnostic> Diagnostics);
