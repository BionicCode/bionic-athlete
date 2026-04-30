namespace BionicAthlete.Presentation.Reporting;

using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

internal sealed class WebView2ReportReadinessWaiter
{
    private readonly WebView2 _browser;

    public WebView2ReportReadinessWaiter(WebView2 browser)
    {
        ArgumentNullException.ThrowIfNull(browser);

        _browser = browser;
    }

    public async Task WaitForReportReadyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        TaskCompletionSource readyCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource timeoutCancellationTokenSource = new(timeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellationTokenSource.Token);
        using CancellationTokenRegistration cancellationRegistration = linkedCancellationTokenSource.Token.Register(
            state =>
            {
                var stateTuple = ((TaskCompletionSource CompletionSource, CancellationToken CancellationToken, CancellationToken TimeoutToken))state!;
                if (stateTuple.CancellationToken.IsCancellationRequested)
                {
                    _ = stateTuple.CompletionSource.TrySetCanceled(stateTuple.CancellationToken);
                }
                else if (stateTuple.TimeoutToken.IsCancellationRequested)
                {
                    _ = stateTuple.CompletionSource.TrySetException(new TimeoutException("Timed out waiting for the HTML report to signal ReportReady."));
                }
            },
            (readyCompletionSource, cancellationToken, timeoutCancellationTokenSource.Token));

        _browser.NavigationCompleted += OnNavigationCompleted;
        _browser.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        try
        {
            await readyCompletionSource.Task.ConfigureAwait(true);
        }
        finally
        {
            _browser.NavigationCompleted -= OnNavigationCompleted;
            _browser.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }

        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs eventArgs)
        {
            if (!eventArgs.IsSuccess)
            {
                _ = readyCompletionSource.TrySetException(
                    new PdfExportException($"Report navigation failed with WebView2 error status '{eventArgs.WebErrorStatus}'."));
            }
        }

        void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs eventArgs)
        {
            try
            {
                using JsonDocument message = JsonDocument.Parse(eventArgs.WebMessageAsJson);
                if (message.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                string? type = message.RootElement.TryGetProperty("type", out JsonElement typeElement)
                    ? typeElement.GetString()
                    : null;

                switch (type)
                {
                    case "ReportReady":
                        _ = readyCompletionSource.TrySetResult();
                        break;
                    case "ReportFailed":
                        string failureMessage = message.RootElement.TryGetProperty("message", out JsonElement messageElement)
                            ? messageElement.GetString() ?? "The HTML report signaled ReportFailed."
                            : "The HTML report signaled ReportFailed.";
                        _ = readyCompletionSource.TrySetException(new PdfExportException(failureMessage));
                        break;
                }
            }
            catch (JsonException exception)
            {
                _ = readyCompletionSource.TrySetException(
                    new PdfExportException("The HTML report sent an invalid readiness message.", exception));
            }
        }
    }
}
