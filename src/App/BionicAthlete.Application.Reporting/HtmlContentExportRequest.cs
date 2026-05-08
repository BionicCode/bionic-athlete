namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application;
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
        FileDescriptor outputPdfFilePath,
        DirectoryDescriptor rootOutputDirectoryPath,
        HtmlDocument htmlDocument,
        PageSettings pageSettings,
        TimeSpan timeout,
        int retryCount,
        IReportManifestBuilder? manifestBuilder,
        ReportDescriptor reportDescriptor) : base(outputPdfFilePath, rootOutputDirectoryPath, pageSettings, timeout, retryCount, manifestBuilder, reportDescriptor)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(outputPdfFilePath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(rootOutputDirectoryPath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(htmlDocument);
        ArgumentNullExceptionAdvanced.ThrowIfNull(pageSettings);
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);

        HtmlDocument = htmlDocument;
    }

    public HtmlDocument HtmlDocument { get; }
}
