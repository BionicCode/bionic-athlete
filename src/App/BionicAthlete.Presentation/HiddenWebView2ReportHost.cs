namespace BionicAthlete.Presentation;

using System.Windows;
using Microsoft.Web.WebView2.Wpf;

internal sealed class HiddenWebView2ReportHost : IDisposable
{
    private bool _isDisposed;

    private HiddenWebView2ReportHost(Window hostWindow, WebView2 browser)
    {
        HostWindow = hostWindow;
        Browser = browser;
    }

    public Window HostWindow { get; }

    public WebView2 Browser { get; }

    public static async Task<HiddenWebView2ReportHost> CreateAsync(CancellationToken cancellationToken)
    {
        var browser = new WebView2();
        var hostWindow = new Window
        {
            Content = browser,
            Opacity = 0,
            Width = 1,
            Height = 1,
            Left = -10000,
            Top = -10000,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Normal,
            ShowInTaskbar = false,
            ShowActivated = false
        };

        bool isCreated = false;
        try
        {
            TaskCompletionSource loadedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            hostWindow.Loaded += OnHostWindowLoaded;
            hostWindow.Show();
            await loadedCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
            await browser.EnsureCoreWebView2Async().ConfigureAwait(true);

            isCreated = true;
            return new HiddenWebView2ReportHost(hostWindow, browser);

            void OnHostWindowLoaded(object sender, RoutedEventArgs routedEventArgs)
            {
                hostWindow.Loaded -= OnHostWindowLoaded;
                _ = loadedCompletionSource.TrySetResult();
            }
        }
        finally
        {
            if (!isCreated)
            {
                hostWindow.Close();
                browser.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        HostWindow.Close();
        Browser.Dispose();
        _isDisposed = true;
    }
}
