namespace BionicAthlete.Application.Reporting;

/// <summary>
/// Exports a generated View C HTML report package to PDF using UI-bound infrastructure.
/// </summary>
public interface IReportPdfExporter
{
    /// <summary>
    /// Renders the generated HTML report to a PDF file.
    /// </summary>
    /// <param name="request">The PDF export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated PDF result.</returns>
    Task<ReportPdfExportResult> ExportHtmlToPdfAsync(
        PdfExportRequest request,
        CancellationToken cancellationToken = default);
}
