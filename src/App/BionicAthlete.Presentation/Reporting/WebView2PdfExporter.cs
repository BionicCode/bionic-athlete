namespace BionicAthlete.Presentation.Reporting;

using System.Collections.Immutable;
using System.IO;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Application.Reporting.Html;
using BionicAthlete.Shared.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Renders generated activity-report HTML to PDF through a hidden per-operation WebView2 host.
/// </summary>
public sealed class WebView2PdfExporter : IReportPdfExporter
{
    private readonly IReportManifestManager _manifestUpdater;
    private readonly IApplicationLogger<WebView2PdfExporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2PdfExporter"/> class.
    /// </summary>
    /// <param name="manifestUpdater">Manifest updater used after the PDF file is physically generated.</param>
    public WebView2PdfExporter(IReportManifestManager manifestUpdater, IApplicationLogger<WebView2PdfExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(manifestUpdater);
        ArgumentNullException.ThrowIfNull(logger);

        _manifestUpdater = manifestUpdater;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReportPdfExportResult> ExportToPdfAsync(
        UriExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceUri.IsFile
            && !File.Exists(request.SourceUri.LocalPath))
        {
            throw new FileNotFoundException("The specified HTML file does not exist.", request.SourceUri.LocalPath);
        }

        string? outputDirectoryPath = Path.GetDirectoryName(request.OutputPdfFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            _ = Directory.CreateDirectory(outputDirectoryPath);
        }

        _logger.LogDebugMessage($"Starting PDF export for '{request.SourceUri.AbsolutePath}'.");

        HiddenWebView2ReportHost reportHost = await HiddenWebView2ReportHost.CreateAsync(cancellationToken).ConfigureAwait(true);
        var readinessWaiter = new WebView2CompletionWaiter(reportHost.Browser);

        _logger.LogDebugMessage($"Navigating WebView2 to source URI '{request.SourceUri.AbsolutePath}'.");
        Task<WebView2StatusReport> reportReadyTask = readinessWaiter.WaitForCompletionAsync(request.Timeout, cancellationToken);
        reportHost.Browser.CoreWebView2.Navigate(request.SourceUri.AbsoluteUri);

        WebView2StatusReport statusReport = default;

        // +1 to account for the initial attempt in addition to the configured retries
        int totalAttempts = request.RetryCount + 1;
        int runCount = 0;
        do
        {
            runCount++;
            _logger.LogDebugMessage($"Waiting for WebView2 to report readiness for PDF export for '{request.SourceUri.AbsolutePath}' (attempt #{runCount} of {totalAttempts}).");
            bool canRetry = runCount < totalAttempts;

            statusReport = await reportReadyTask.ConfigureAwait(true);
            switch (statusReport.Status)
            {
                case WebView2Status.Success:
                    _logger.LogDebugMessage($"WebView2 reported successful navigation and readiness for PDF export for '{request.SourceUri.AbsolutePath}'.");
                    break;
                case WebView2Status.ProcessFailed:
                    (reportHost, readinessWaiter, reportReadyTask) = await HandleProcessFailure(request, reportHost, readinessWaiter, reportReadyTask, statusReport, canRetry, cancellationToken);
                    break;
                case WebView2Status.UnsupportedContent:
                    throw new PdfExportException($"WebView2 reported unsupported content at '{request.SourceUri.AbsolutePath}' while attempting to render the source for PDF export.");
                case WebView2Status.WebErrorOccurred:
                case WebView2Status.Timeout:
                    (reportHost, readinessWaiter, reportReadyTask) = await HandleWebError(request, reportHost, readinessWaiter, reportReadyTask, statusReport, canRetry, cancellationToken);
                    
                    break;
                case WebView2Status.Cancelled:
                    string message = $"PDF export was cancelled while waiting for WebView2 to report readiness for '{request.SourceUri.AbsolutePath}'.";
                    _logger.LogDebugMessage(message);
                    throw new OperationCanceledException(
                        message, 
                        statusReport.Exception, 
                        cancellationToken);
            }
        } while (statusReport.Status is not WebView2Status.Success && runCount < request.RetryCount);

        CoreWebView2PrintSettings printSettings = WebView2PrintSettingsMapper.CreatePrintSettings(
            reportHost.Browser.CoreWebView2.Environment,
            request.PageSettings);
        bool isPdfGenerated = await reportHost.Browser.CoreWebView2.PrintToPdfAsync(
            request.OutputPdfFilePath,
            printSettings).ConfigureAwait(true);

        if (!isPdfGenerated)
        {
            throw new PdfExportException("WebView2 reported PDF generation failure.");
        }

        FileInfo pdfFileInfo = new(request.OutputPdfFilePath);
        if (!pdfFileInfo.Exists || pdfFileInfo.Length == 0)
        {
            throw new PdfExportException("WebView2 completed PDF generation but did not produce a non-empty PDF file.");
        }

        HtmlReportPackage packageWithPdf = request.ReportPackage with { PdfFilePath = request.OutputPdfFilePath };
        await _manifestUpdater.AddPdfArtifactAsync(packageWithPdf, cancellationToken).ConfigureAwait(true);

        return new ReportPdfExportResult(
            request.OutputPdfFilePath,
            pdfFileInfo.Length,
            ImmutableArray<ReportDiagnostic>.Empty);
    }

    private async Task<(HiddenWebView2ReportHost ReportHost, WebView2CompletionWaiter ReadinessWaiter, Task<WebView2StatusReport> ReportReadyTask)> HandleProcessFailure(
        UriExportRequest request,
        HiddenWebView2ReportHost reportHost,
        WebView2CompletionWaiter readinessWaiter,
        Task<WebView2StatusReport> reportReadyTask,
        WebView2StatusReport statusReport,
        bool canRetry,
        CancellationToken cancellationToken)
    {
        ProcessFailedData processFailedData = statusReport.ProcessFailedData;
        string processDescription = string.IsNullOrWhiteSpace(processFailedData.ProcessDescription)
            ? processFailedData.FailureKind.ToString() ?? string.Empty
            : processFailedData.ProcessDescription;
        string failureReportMessage = $"WebView2 process '{processDescription}' exited with code '{processFailedData.ExitCode}' while preparing for PDF export for '{request.SourceUri.AbsolutePath}'." +
            $"{Environment.NewLine}Reason: {processFailedData.Reason}" +
            $"{Environment.NewLine}Source: {processFailedData.FailureSourceModulePath}";
        _logger.LogErrorMessage(failureReportMessage);

        if (canRetry)
        {
            _logger.LogDebugMessage($"Attempting to recover from process '{processDescription}' exit by recreating WebView2 for '{request.SourceUri.AbsolutePath}'.");
            return await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        }

        throw new PdfExportException(failureReportMessage, statusReport.Exception);

        //switch (processFailedData.FailureKind)
        //{
        //    case CoreWebView2ProcessFailedKind.BrowserProcessExited:
        //        _logger.LogErrorMessage($"WebView2 browser process '{processFailedData.ProcessDescription}' exited with code '{processFailedData.ExitCode}' while preparing for PDF export for '{request.SourceUri.AbsolutePath}'." +
        //            $"{Environment.NewLine}Reason: {processFailedData.Reason}" +
        //            $"{Environment.NewLine}Source: {processFailedData.FailureSourceModulePath}");
        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from browser process exit by recreating WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.RenderProcessExited:
        //        _logger.LogErrorMessage($"WebView2 render process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from render process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.FrameRenderProcessExited:
        //        _logger.LogErrorMessage($"WebView2 frame render process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from frame render process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.GpuProcessExited:
        //        _logger.LogErrorMessage($"WebView2 GPU process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from GPU  process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.UtilityProcessExited:
        //        _logger.LogErrorMessage($"WebView2 utility process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from utility process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
        //        _logger.LogDebugMessage($"WebView2 render process became unresponsive while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogInformationMessage($"Attempting to recover from unresponsive render process by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.SandboxHelperProcessExited:
        //        _logger.LogErrorMessage($"WebView2 sandbox helper process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from sandbox process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.PpapiPluginProcessExited:
        //        _logger.LogErrorMessage($"WebView2 PPAPI plugin process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from PPAPI plugin process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.PpapiBrokerProcessExited:
        //        _logger.LogErrorMessage($"WebView2 PPAPI broker process exited while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from PPAPI broker process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    case CoreWebView2ProcessFailedKind.UnknownProcessExited:
        //        _logger.LogErrorMessage($"WebView2 process exited with unknown process failure kind while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from unknown process exit by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //    default:
        //        _logger.LogErrorMessage($"WebView2 process failed with unknown process failure kind while preparing for PDF export for '{request.SourceUri.AbsolutePath}'.");

        //        if (canRetry)
        //        {
        //            _logger.LogDebugMessage($"Attempting to recover from unspecified process failure by reloading WebView2 for '{request.SourceUri.AbsolutePath}'.");
        //            (reportHost, readinessWaiter, reportReadyTask) = await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        //        }

        //        break;
        //}

        //return (reportHost, readinessWaiter, reportReadyTask);
    }

    private async Task<(HiddenWebView2ReportHost ReportHost, WebView2CompletionWaiter ReadinessWaiter, Task<WebView2StatusReport> ReportReadyTask)> HandleWebError(
        UriExportRequest request,
        HiddenWebView2ReportHost reportHost,
        WebView2CompletionWaiter readinessWaiter,
        Task<WebView2StatusReport> reportReadyTask,
        WebView2StatusReport statusReport,
        bool canRetry,
        CancellationToken cancellationToken)
    {
        ProcessFailedData processFailedData = statusReport.DetailedWebErrorStatus;
        string processDescription = string.IsNullOrWhiteSpace(processFailedData.ProcessDescription)
            ? processFailedData.FailureKind.ToString() ?? string.Empty
            : processFailedData.ProcessDescription;
        string failureReportMessage = $"WebView2 process '{processDescription}' exited with code '{processFailedData.ExitCode}' while preparing for PDF export for '{request.SourceUri.AbsolutePath}'." +
            $"{Environment.NewLine}Reason: {processFailedData.Reason}" +
            $"{Environment.NewLine}Source: {processFailedData.FailureSourceModulePath}";
        _logger.LogErrorMessage(failureReportMessage);

        if (canRetry)
        {
            _logger.LogDebugMessage($"Attempting to recover from process '{processDescription}' exit by recreating WebView2 for '{request.SourceUri.AbsolutePath}'.");
            return await RestartHostAsync(reportHost, request.Timeout, cancellationToken);
        }

        throw new PdfExportException(failureReportMessage, statusReport.Exception);
    }

    private static async Task<(HiddenWebView2ReportHost Host, WebView2CompletionWaiter ReadinessWaiter, Task<WebView2StatusReport> ReportReadyTask)> RestartHostAsync(HiddenWebView2ReportHost reportHost, TimeSpan timeout, CancellationToken cancellationToken)
    {
        reportHost?.Dispose();
        HiddenWebView2ReportHost reportHost = await HiddenWebView2ReportHost.CreateAsync(cancellationToken).ConfigureAwait(true);
        var readinessWaiter = new WebView2CompletionWaiter(reportHost.Browser);
        Task<WebView2StatusReport> reportReadyTask = readinessWaiter.WaitForCompletionAsync(timeout, cancellationToken);

        return (reportHost, readinessWaiter, reportReadyTask);
    }

    /// <inheritdoc />
    public async Task<ReportPdfExportResult> ExportToPdfAsync(
        HtmlContentExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!File.Exists(request.SourceUri.AbsolutePath))
        {
            throw new FileNotFoundException("The generated HTML report does not exist.", request.ReportPackage.HtmlFilePath);
        }

        string? outputDirectoryPath = Path.GetDirectoryName(request.OutputPdfFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            _ = Directory.CreateDirectory(outputDirectoryPath);
        }

        using HiddenWebView2ReportHost reportHost = await HiddenWebView2ReportHost.CreateAsync(cancellationToken).ConfigureAwait(true);
        var readinessWaiter = new WebView2CompletionWaiter(reportHost.Browser);
        Task reportReadyTask = readinessWaiter.WaitForCompletionAsync(request.Timeout, cancellationToken);

        reportHost.Browser.CoreWebView2.Navigate(new Uri(request.ReportPackage.HtmlFilePath).AbsoluteUri);
        await reportReadyTask.ConfigureAwait(true);

        CoreWebView2PrintSettings printSettings = WebView2PrintSettingsMapper.CreatePrintSettings(
            reportHost.Browser.CoreWebView2.Environment,
            request.PageSettings);
        bool isPdfGenerated = await reportHost.Browser.CoreWebView2.PrintToPdfAsync(
            request.OutputPdfFilePath,
            printSettings).ConfigureAwait(true);

        if (!isPdfGenerated)
        {
            throw new PdfExportException("WebView2 reported PDF generation failure.");
        }

        FileInfo pdfFileInfo = new(request.OutputPdfFilePath);
        if (!pdfFileInfo.Exists || pdfFileInfo.Length == 0)
        {
            throw new PdfExportException("WebView2 completed PDF generation but did not produce a non-empty PDF file.");
        }

        HtmlReportPackage packageWithPdf = request.ReportPackage with { PdfFilePath = request.OutputPdfFilePath };
        await _manifestUpdater.AddPdfArtifactAsync(packageWithPdf, cancellationToken).ConfigureAwait(true);

        return new ReportPdfExportResult(
            request.OutputPdfFilePath,
            pdfFileInfo.Length,
            ImmutableArray<ReportDiagnostic>.Empty);
    }

    public Task<ReportPdfExportResult> ExportToPdfAsync(PdfExportRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    //public Task<ReportPdfExportResult> ExportToPdfAsync(PdfExportRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
