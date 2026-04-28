namespace BionicAthlete.Presentation.Reporting;

using System.Collections.Immutable;
using BionicAthlete.Training.Reporting;

/// <summary>
/// Result of a successful View C PDF export.
/// </summary>
/// <param name="PdfFilePath">Generated PDF file path.</param>
/// <param name="PdfFileLength">Generated PDF file length in bytes.</param>
/// <param name="Diagnostics">Non-fatal diagnostics from settings mapping or manifest updates.</param>
public sealed record ActivityReportPdfExportResult(
    string PdfFilePath,
    long PdfFileLength,
    ImmutableArray<ActivityReportDiagnostic> Diagnostics);
