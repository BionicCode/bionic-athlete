namespace BionicAthlete.Presentation.Reporting;

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Shared.Logging;
using BionicCode.Utilities.Net;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Renders generated activity-report HTML to PDF through a hidden per-operation WebView2 host.
/// </summary>
public sealed partial class WebView2PdfExporter : IReportPdfExporter
{
    private readonly IApplicationLogger<WebView2PdfExporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2PdfExporter"/> class.
    /// </summary>
    /// <param name="manifestUpdater">Manifest updater used after the PDF file is physically generated.</param>
    public WebView2PdfExporter(IApplicationLogger<WebView2PdfExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReportPdfExportResult> ExportToPdfAsync(
        PdfExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is UriExportRequest uriRequest)
        {
            ArgumentExceptionAdvanced.ThrowIfFalse(IsAllowedPdfRenderScheme(
                uriRequest.SourceUri),
                nameof(request),
                $"The URI scheme '{uriRequest.SourceUri.Scheme}' is not supported for PDF export. Supported schemes are: file, http, https.");

            if (uriRequest.SourceUri.IsFile
                && !File.Exists(uriRequest.SourceUri.LocalPath))
            {
                throw new FileNotFoundException("The specified HTML file does not exist.", uriRequest.SourceUri.LocalPath);
            }
            else if (request is HtmlContentExportRequest htmlContentRequest
                && string.IsNullOrWhiteSpace(htmlContentRequest.HtmlDocument.Content))
            {
                throw new ArgumentException("The provided HTML content is null or whitespace.", nameof(request));
            }
        }
        else if (request is HtmlContentExportRequest htmlContentRequest
                    && string.IsNullOrWhiteSpace(htmlContentRequest.HtmlDocument.Content))
        {
            throw new ArgumentException("The provided HTML content is null or whitespace.", nameof(request));
        }

        string? outputDirectoryPath = Path.GetDirectoryName(request.OutputPdfFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            _ = Directory.CreateDirectory(outputDirectoryPath);
        }

        _logger.LogDebugMessage($"Starting export to PDF.");

        using HiddenWebView2Host reportHost = await EnsureWebHostAsync(request, cancellationToken).ConfigureAwait(true);
        CoreWebView2PrintSettings printSettings = WebView2PrintSettingsMapper.CreatePrintSettings(
            reportHost.Browser.CoreWebView2.Environment,
            request.PageSettings);
        bool isPdfGenerated = await reportHost.Browser.CoreWebView2.PrintToPdfAsync(request.OutputPdfFilePath, printSettings).ConfigureAwait(true);
        if (!isPdfGenerated)
        {
            throw new PdfExportException("WebView2 reported PDF generation failure.");
        }

        FileInfo pdfFileInfo = new(request.OutputPdfFilePath);
        if (!pdfFileInfo.Exists
            || pdfFileInfo.Length == 0)
        {
            throw new PdfExportException("WebView2 completed PDF generation but did not produce a non-empty PDF file.");
        }

        return new ReportPdfExportResult(
            request.OutputPdfFilePath,
            pdfFileInfo.Length,
            ImmutableArray<ReportDiagnostic>.Empty);
    }

    private static bool IsAllowedPdfRenderScheme(Uri uri) => uri.Scheme == Uri.UriSchemeFile
        || uri.Scheme == Uri.UriSchemeHttp
        || uri.Scheme == Uri.UriSchemeHttps;

    private async Task<HiddenWebView2Host> EnsureWebHostAsync(PdfExportRequest request, CancellationToken cancellationToken)
    {
        WebView2RenderResult renderResult = await RenderRequestAsync(request, cancellationToken).ConfigureAwait(true);
        WebView2StatusReport statusReport = renderResult.StatusReport;
        HiddenWebView2Host reportHost = renderResult.Host;

        string exportSubject = GetExportSubject(request);

        // +1 to account for the initial attempt in addition to the configured retries
        int totalAttempts = request.RetryCount + 1;
        int runCount = 0;
        bool canRetry = true;
        while (statusReport.Status is not WebView2Status.Success && canRetry)
        {
            runCount++;
            canRetry = runCount < totalAttempts;

            _logger.LogDebugMessage($"Waiting for WebView2 to report readiness for PDF export for '{exportSubject}' (attempt #{runCount} of {totalAttempts}).");

            switch (statusReport.Status)
            {
                case WebView2Status.Success:
                    _logger.LogDebugMessage($"WebView2 reported successful navigation and readiness for PDF export for '{exportSubject}'.");
                    break;
                case WebView2Status.ProcessFailed:
                    reportHost.Dispose();

                    var failureArgs = new HandleProcessFailureArgs(
                        Request: request,
                        StatusReport: statusReport,
                        CanRetry: canRetry,
                        ExportSubject: exportSubject,
                        CancellationToken: cancellationToken);
                    renderResult = await HandleProcessFailure(failureArgs);
                    reportHost = renderResult.Host;
                    statusReport = renderResult.StatusReport;
                    break;
                case WebView2Status.UnsupportedContent:
                    throw new PdfExportException($"WebView2 reported unsupported content at '{exportSubject}' while attempting to render the source for PDF export.");
                case WebView2Status.WebErrorOccurred:
                case WebView2Status.Timeout:
                    reportHost.Dispose();

                    var errorArgs = new HandleWebErrorArgs(
                        Request: request,
                        StatusReport: statusReport,
                        CanRetry: canRetry,
                        CancellationToken: cancellationToken);
                    renderResult = await HandleWebError(errorArgs);
                    reportHost = renderResult.Host;
                    statusReport = renderResult.StatusReport;
                    break;
                case WebView2Status.Cancelled:
                    string message = $"PDF export was cancelled while waiting for WebView2 to report readiness for '{exportSubject}'.";
                    _logger.LogDebugMessage(message);
                    throw new OperationCanceledException(
                        message,
                        statusReport.Exception,
                        cancellationToken);
            }
        }

        // Defensive check to ensure that if we exited the loop without success,
        // it's because we exhausted our retries and not because of an unexpected status value.
        // However, it's expected that an exception is thrown by the error handlers inside the loop if rendering the source has failed.
        if (statusReport.Status is not WebView2Status.Success)
        {
            string message = $"WebView2 failed to report readiness for PDF export for '{exportSubject}' after {totalAttempts} attempt(s). Last reported status: {statusReport.Status}.";
            _logger.LogErrorMessage(message);
            throw new PdfExportException(message, statusReport.Exception);
        }

        _logger.LogDebugMessage($"WebView2 was able to render the source '{exportSubject}' and is ready for PDF export after {runCount} attempt(s).");

        return reportHost;
    }

    private static string GetExportSubject(PdfExportRequest request)
            => request is UriExportRequest uriRequest
                ? $"source URI '{uriRequest.SourceUri.AbsolutePath}'"
                : request is HtmlContentExportRequest
                    ? "HTML content"
                    : throw new NotImplementedException("Unsupported request type.");

    private async Task<WebView2RenderResult> HandleProcessFailure(HandleProcessFailureArgs failureArgs)
    {
        WebView2StatusReport statusReport = failureArgs.StatusReport;
        Debug.Assert(statusReport.ProcessFailedData.HasValue, "ProcessFailedData should be present for ProcessFailed status.");
        ProcessFailedData processFailedData = statusReport.ProcessFailedData.Value;

        string processDescription = string.IsNullOrWhiteSpace(processFailedData.ProcessDescription)
            ? processFailedData.FailureKind.ToString() ?? string.Empty
            : processFailedData.ProcessDescription;
        string failureReportMessage = $"WebView2 process '{processDescription}' exited with code '{processFailedData.ExitCode}' while preparing for PDF export for '{failureArgs.ExportSubject}'." +
            $"{Environment.NewLine}Reason: {processFailedData.Reason}" +
            $"{Environment.NewLine}Source: {processFailedData.FailureSourceModulePath}";
        _logger.LogErrorMessage(failureReportMessage);

        if (failureArgs.CanRetry)
        {
            _logger.LogDebugMessage($"Attempting to recover from process '{processDescription}' exit by recreating WebView2 for '{failureArgs.ExportSubject}'.");
            return await RenderRequestAsync(failureArgs.Request, failureArgs.CancellationToken);
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

    private async Task<WebView2RenderResult> HandleWebError(HandleWebErrorArgs errorArgs)
    {
        WebView2StatusReport statusReport = errorArgs.StatusReport;
        Debug.Assert(statusReport.WebErrorData.HasValue, "WebErrorData should be present for WebErrorOccurred status.");
        WebErrorData webErrorData = statusReport.WebErrorData.Value;

        string failureReportMessage = $"WebView2 encountered an error '{webErrorData.DetailedWebErrorStatus}'. HTTP error code '{webErrorData.HttpStatusCode}'. Navigation ID: '{webErrorData.NavigationId}'.";
        _logger.LogErrorMessage(failureReportMessage);
        if (errorArgs.CanRetry)
        {
            _logger.LogDebugMessage($"Attempting to recover from web error '{webErrorData.DetailedWebErrorStatus}'.");
            return await RenderRequestAsync(errorArgs.Request, errorArgs.CancellationToken);
        }

        throw new PdfExportException(failureReportMessage, statusReport.Exception);
    }

    private async Task<WebView2RenderResult> RenderRequestAsync(PdfExportRequest request, CancellationToken cancellationToken)
    {
        HiddenWebView2Host reportHost = await HiddenWebView2Host.CreateAsync(cancellationToken).ConfigureAwait(true);
        var readinessWaiter = new WebView2CompletionWaiter(reportHost.Browser);
        Task<WebView2StatusReport> statusReportTask = readinessWaiter.WaitForCompletionAsync(request.Timeout, cancellationToken);

        string exportSubject = GetExportSubject(request);
        if (request is UriExportRequest uriRequest)
        {
            _logger.LogDebugMessage($"Navigating WebView2 to source URI '{exportSubject}'.");
            reportHost.Browser.CoreWebView2.Navigate(uriRequest.SourceUri.AbsoluteUri);
        }
        else if (request is HtmlContentExportRequest htmlContentRequest)
        {
            _logger.LogDebugMessage($"Navigating WebView2 to '{exportSubject}'.");
            reportHost.Browser.CoreWebView2.NavigateToString(htmlContentRequest.HtmlDocument.Content);
        }
        else
        {
            throw new NotImplementedException("Unsupported request type.");
        }

        WebView2StatusReport statusReport = await statusReportTask.ConfigureAwait(true);

        return new WebView2RenderResult(statusReport, reportHost);
    }

    internal record class WebView2RenderResult(WebView2StatusReport StatusReport, HiddenWebView2Host Host);
}
