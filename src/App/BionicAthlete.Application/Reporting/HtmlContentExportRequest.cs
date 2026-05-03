namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Reporting.Html;
using BionicCode.Utilities.Net;

/// <summary>
/// Request for rendering HTML content to PDF.
/// </summary>
public sealed class HtmlContentExportRequest : PdfExportRequest
{

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlContentExportRequest"/> class.
    /// </summary>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    /// <param name="htmlDocument">The HTML content tto export to PDF.</param>
    public HtmlContentExportRequest(
        string outputPdfFilePath,
        HtmlDocument htmlDocument,
        PdfPageSettings pageSettings,
        TimeSpan timeout,
        int retryCount) : base(outputPdfFilePath, pageSettings, timeout, retryCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPdfFilePath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(htmlDocument);
        ArgumentNullException.ThrowIfNull(pageSettings);

        HtmlDocument = htmlDocument;
    }

    public HtmlDocument HtmlDocument { get; }
}
