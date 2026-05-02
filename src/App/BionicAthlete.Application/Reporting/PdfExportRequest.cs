namespace BionicAthlete.Application.Reporting;
/// <summary>
/// Base class for requests to render to PDF.
/// </summary>
public abstract class PdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportRequest"/> class.
    /// </summary>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    /// <param name="sourceUri">The <see cref="Uri"/> that references the source which must be exported to PDF.</param>
    protected PdfExportRequest(
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
