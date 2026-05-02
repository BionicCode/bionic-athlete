namespace BionicAthlete.Application.Reporting;

/// <summary>
/// Exports a generated View C HTML report package to PDF using UI-bound infrastructure.
/// </summary>
public interface IReportPdfExporter
{
    /// <summary>
    /// Renders the the <see cref="PdfExportRequest"/> to a PDF file.
    /// </summary>
    /// <param name="request">The PDF export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated PDF result.</returns>
    Task<ReportPdfExportResult> ExportToPdfAsync(
        PdfExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the generated HTML report to a PDF file.
    /// </summary>
    /// <param name="request">The PDF export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated PDF result.</returns>
    Task<ReportPdfExportResult> ExportToPdfAsync(
        PdfExportRequest request,
        CancellationToken cancellationToken = default);
}
