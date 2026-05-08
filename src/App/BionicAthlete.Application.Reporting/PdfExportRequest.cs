namespace BionicAthlete.Application.Reporting;

using BionicCode.Utilities.Net;

/// <summary>
/// Base class for requests to render to PDF.
/// </summary>
public abstract class PdfExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportRequest"/> class.
    /// </summary>
    /// <param name="outputPdfFilePath">Full destination PDF file path.</param>
    /// <param name="rootOutputDirectoryPath">Root output directory path of all reports.</param>
    /// <param name="pageSettings">Neutral page settings to map into WebView2 print settings.</param>
    /// <param name="timeout">Maximum time to wait for navigation, readiness, and PDF generation.</param>
    protected PdfExportRequest(
        FileDescriptor outputPdfFilePath,
        DirectoryDescriptor rootOutputDirectoryPath,
        PageSettings pageSettings,
        TimeSpan timeout,
        int retryCount,
        IReportManifestBuilder? manifestBuilder,
        ReportDescriptor reportDescriptor)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(outputPdfFilePath);
        ArgumentNullExceptionAdvanced.ThrowIfDefault(rootOutputDirectoryPath);
        ArgumentNullExceptionAdvanced.ThrowIfNull(pageSettings);
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportDescriptor);

        OutputPdfFilePath = outputPdfFilePath;
        RootOutputDirectoryPath = rootOutputDirectoryPath;
        PageSettings = pageSettings;
        RetryCount = retryCount;
        ManifestBuilder = manifestBuilder;
        ReportDescriptor = reportDescriptor;
        Timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;
    }

    public async Task SetRequestCompletedAsync(PdfExportResult pdfExportResult)
    {
        var taskCompletionSource = new TaskCompletionSource();
        OnRequestCompleted(pdfExportResult, taskCompletionSource);
        await taskCompletionSource.Task.ConfigureAwait(false);
    }

    protected virtual void OnRequestCompleted(PdfExportResult pdfExportResult, TaskCompletionSource taskCompletionSource) => RequestCompleted?.Invoke(this, new PdfExportRequestEventArgs(pdfExportResult, taskCompletionSource));

    public event EventHandler<PdfExportRequestEventArgs> RequestCompleted;

    /// <summary>
    /// Gets the destination PDF file path.
    /// </summary>
    public FileDescriptor OutputPdfFilePath { get; }
    public DirectoryDescriptor RootOutputDirectoryPath { get; }

    /// <summary>
    /// Gets the neutral page settings for this operation.
    /// </summary>
    public PageSettings PageSettings { get; }

    /// <summary>
    /// Specifies the number of retries if export failed due to a timeout or WebView2 process failure.
    /// </summary>
    public int RetryCount { get; }
    public IReportManifestBuilder? ManifestBuilder { get; }
    public ReportDescriptor ReportDescriptor { get; }

    /// <summary>
    /// Gets the operation timeout.
    /// </summary>
    public TimeSpan Timeout { get; }
}
