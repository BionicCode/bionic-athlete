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
        FileDescriptor outputPdfFilePath,
        DirectoryDescriptor rootOutputDirectoryPath,
        Uri sourceUri,
        PageSettings pageSettings,
        TimeSpan timeout,
        int retryCount,
        IReportManifestBuilder? manifestBuilder,
        ReportDescriptor reportDescriptor) : base(outputPdfFilePath, rootOutputDirectoryPath, pageSettings, timeout, retryCount, manifestBuilder, reportDescriptor)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sourceUri);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(outputPdfFilePath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(rootOutputDirectoryPath);
        ArgumentNullExceptionAdvanced.ThrowIfNull(pageSettings);
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);

        SourceUri = sourceUri;
    }

    public Uri SourceUri { get; }
}
