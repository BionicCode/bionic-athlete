namespace BionicAthlete.Application.Reporting;

using BionicCode.Utilities.Net;

/// <summary>
/// Request for rendering an existing HTML resource like a file or URL to PDF.
/// </summary>
public sealed class UriExportRequest : PdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UriExportRequest"/> class.
    /// </summary>
    /// <param name="outputPdfFilePath">Destination PDF file path.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    /// <param name="sourceUri">The <see cref="Uri"/> that references the source which must be exported to PDF.</param>
    public UriExportRequest(
        string outputPdfFilePath,
        Uri sourceUri,
        PdfPageSettings pageSettings,
        TimeSpan timeout) : base(outputPdfFilePath, pageSettings, timeout)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPdfFilePath);
        ArgumentNullException.ThrowIfNull(pageSettings);

        SourceUri = sourceUri;
    }

    public Uri SourceUri { get; }
}
