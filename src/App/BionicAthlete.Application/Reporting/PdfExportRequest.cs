namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Reporting.Html;

//using BionicAthlete.Application.Reporting;

/// <summary>
/// Request for rendering an existing HTML report package to PDF.
/// </summary>
public class PdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportRequest"/> class.
    /// </summary>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    public PdfExportRequest(
        string outputPdfFilePath,
        PdfPageSettings pageSettings,
        TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPdfFilePath);
        ArgumentNullException.ThrowIfNull(pageSettings);

        OutputPdfFilePath = outputPdfFilePath;
        PageSettings = pageSettings;
        Timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;
    }

    /// <summary>
    /// Gets the destination PDF file path.
    /// </summary>
    public string OutputPdfFilePath { get; }

    /// <summary>
    /// Gets the neutral page settings for this operation.
    /// </summary>
    public PdfPageSettings PageSettings { get; }

    /// <summary>
    /// Gets the operation timeout.
    /// </summary>
    public TimeSpan Timeout { get; }
}

/// <summary>
/// Request for rendering an existing HTML report package to PDF.
/// </summary>
public sealed class HtmlToPdfExportRequest : PdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlToPdfExportRequest"/> class.
    /// </summary>
    /// <param name="reportPackage">Generated HTML report package.</param>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    public HtmlToPdfExportRequest(
        HtmlReportPackage reportPackage,
        string outputPdfFilePath,
        PdfPageSettings pageSettings,
        TimeSpan timeout) : base(outputPdfFilePath, pageSettings, timeout)
    {
        ArgumentNullException.ThrowIfNull(reportPackage);

        ReportPackage = reportPackage;
    }

    /// <summary>
    /// Gets the generated HTML report package.
    /// </summary>
    public HtmlReportPackage ReportPackage { get; }
}
