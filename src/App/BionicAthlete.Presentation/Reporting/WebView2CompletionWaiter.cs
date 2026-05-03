namespace BionicAthlete.Presentation.Reporting;

using System.Text.Json;
using BionicAthlete.Application.Reporting;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

internal sealed class WebView2CompletionWaiter
{
    private readonly WebView2 _browser;

    public WebView2CompletionWaiter(WebView2 browser)
    {
        ArgumentNullException.ThrowIfNull(browser);

        _browser = browser;
    }

    public async Task<WebView2StatusReport> WaitForCompletionAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        TaskCompletionSource<WebView2StatusReport> readyCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource timeoutCancellationTokenSource = new(timeout);

        _browser.CoreWebView2.ProcessFailed += OnProcessFailed;
        _browser.CoreWebView2.DownloadStarting += OnDownloadStarting;

        // REVIEW::Check whether we really must observe this event.
        // It is not clear whether current code base dynamically generates web messages from inside the HTML reports.
        // This would be unexpected and unnecessary. The HTML report should ideally not contain any JavaScript.
        // But Codex must have observed the OnWebMessageReceived event for a good reason that needs to be identified and validated.
        _browser.NavigationCompleted += OnNavigationCompleted;

        try
        {
            return await readyCompletionSource.Task
                .WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException exception)
        {
            _browser.CoreWebView2.Stop();
            var webView2StatusReport = new WebView2StatusReport(
                WebView2Status.Cancelled,
                null,
                null,
                    "PDF rendering operation was cancelled.",
                    timeout,
                    exception);
            _ = readyCompletionSource.TrySetResult(webView2StatusReport);

            return webView2StatusReport;
        }
        catch (TimeoutException exception)
        {
            _browser.CoreWebView2.Stop();
            var webView2StatusReport = new WebView2StatusReport(
                WebView2Status.Timeout,
                null,
                null,
                    "PDF rendering operation timed out.",
                    timeout,
                    exception);
            _ = readyCompletionSource.TrySetResult(webView2StatusReport);

            return webView2StatusReport;
        }
        finally
        {
            _browser.CoreWebView2.ProcessFailed -= OnProcessFailed;
            _browser.CoreWebView2.DownloadStarting -= OnDownloadStarting;
            _browser.NavigationCompleted -= OnNavigationCompleted;
        }

        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs eventArgs)
        {
            if (eventArgs.IsSuccess)
            {
                var webView2StatusReport = new WebView2StatusReport(
                    WebView2Status.Success,
                    new WebErrorData(
                        eventArgs.WebErrorStatus,
                        eventArgs.HttpStatusCode,
                        eventArgs.NavigationId),
                    null,
                    "PDF rendered successfully.",
                    timeout,
                    null);
                _ = readyCompletionSource.TrySetResult(webView2StatusReport);
            }
            else if (eventArgs.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled
                && cancellationToken.IsCancellationRequested)
            {
                var webView2StatusReport = new WebView2StatusReport(
                    WebView2Status.Cancelled,
                    new WebErrorData(
                        eventArgs.WebErrorStatus,
                        eventArgs.HttpStatusCode,
                        eventArgs.NavigationId),
                    null,
                    "PDF rendering operation cancelled by caller.",
                    timeout,
                    null);
                _ = readyCompletionSource.TrySetCanceled(cancellationToken);
            }
            else
            {
                var webView2StatusReport = new WebView2StatusReport(
                    WebView2Status.WebErrorOccurred,
                    new WebErrorData(
                        eventArgs.WebErrorStatus,
                        eventArgs.HttpStatusCode,
                        eventArgs.NavigationId),
                    null,
                    $"PDF rendering operation failed with WebView2 error status '{eventArgs.WebErrorStatus}'.",
                    timeout,
                    null);
                _ = readyCompletionSource.TrySetResult(webView2StatusReport);
            }
        }

        void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs eventArgs)
        {
            var webView2StatusReport = new WebView2StatusReport(
                WebView2Status.ProcessFailed,
                null,
                new ProcessFailedData(
                    eventArgs.FailureSourceModulePath,
                    eventArgs.ProcessDescription,
                eventArgs.ProcessFailedKind,
                eventArgs.Reason,
                    eventArgs.ExitCode),
                    "WebView2 process failed.",
                    timeout,
                    null);
            _ = readyCompletionSource.TrySetResult(webView2StatusReport);
        }

        void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Handled = true;
            e.Cancel = true;

            var webView2StatusReport = new WebView2StatusReport(
                WebView2Status.UnsupportedContent,
                null,
                null,
                    $"Source with URI '{e.DownloadOperation.Uri}' can't be rendered in WebView2.",
                    timeout,
                    null);
            _ = readyCompletionSource.TrySetResult(webView2StatusReport);
        }
    }
}

internal readonly record struct WebView2StatusReport(
    WebView2Status Status,
    WebErrorData? WebErrorData,
    ProcessFailedData? ProcessFailedData,
    string Message,
    TimeSpan Timeout,
    Exception? Exception);

internal readonly record struct ProcessFailedData(
    string FailureSourceModulePath,
    string ProcessDescription,
    CoreWebView2ProcessFailedKind? FailureKind,
    CoreWebView2ProcessFailedReason? Reason,
    int ExitCode);

internal readonly record struct WebErrorData(
    CoreWebView2WebErrorStatus? DetailedWebErrorStatus,
    int HttpStatusCode,
    ulong NavigationId);

internal enum WebView2Status
{
    Success,
    ProcessFailed,
    UnsupportedContent,
    WebErrorOccurred,
    Timeout,
    Cancelled
}