namespace FitToCsvConverter.Controls.Reporting;

using FitToCsvConverter.Reporting;

/// <summary>
/// Request for rendering an existing HTML report package to PDF.
/// </summary>
public sealed class ActivityReportPdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportPdfExportRequest"/> class.
    /// </summary>
    /// <param name="reportPackage">Generated HTML report package.</param>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    public ActivityReportPdfExportRequest(
        HtmlReportPackage reportPackage,
        string outputPdfFilePath,
        ActivityReportPageSettings pageSettings,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(reportPackage);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPdfFilePath);
        ArgumentNullException.ThrowIfNull(pageSettings);

        ReportPackage = reportPackage;
        OutputPdfFilePath = outputPdfFilePath;
        PageSettings = pageSettings;
        Timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;
    }

    /// <summary>
    /// Gets the generated HTML report package.
    /// </summary>
    public HtmlReportPackage ReportPackage { get; }

    /// <summary>
    /// Gets the destination PDF file path.
    /// </summary>
    public string OutputPdfFilePath { get; }

    /// <summary>
    /// Gets the neutral page settings for this operation.
    /// </summary>
    public ActivityReportPageSettings PageSettings { get; }

    /// <summary>
    /// Gets the operation timeout.
    /// </summary>
    public TimeSpan Timeout { get; }
}
