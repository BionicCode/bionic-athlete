namespace BionicAthlete.Application.Reporting;

using System.Collections.Immutable;
using BionicCode.Utilities.Net;

/// <summary>
/// Result of a successful View C PDF export.
/// </summary>
/// <param name="PdfFilePath">Generated PDF file path.</param>
/// <param name="PdfFileLength">Generated PDF file length in bytes.</param>
/// <param name="Diagnostics">Non-fatal diagnostics from settings mapping or manifest updates.</param>
public sealed record PdfExportResult(
    bool IsSuccessful,
    FileDescriptor PdfFilePath,
    long PdfFileLength,
    ImmutableArray<ReportDiagnostic> Diagnostics);
