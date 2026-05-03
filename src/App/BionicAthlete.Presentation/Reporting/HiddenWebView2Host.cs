namespace BionicAthlete.Presentation.Reporting;

using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

internal sealed class HiddenWebView2Host : IDisposable
{
    private bool _isDisposed;

    private HiddenWebView2Host(Window hostWindow, WebView2 browser)
    {
        HostWindow = hostWindow;
        Browser = browser;
    }

    public Window HostWindow { get; }

    public WebView2 Browser { get; }

    public static async Task<HiddenWebView2Host> CreateAsync(CancellationToken cancellationToken)
    {
        var browser = new WebView2();
        HardenBrowserForHiddenPdfRendering(browser.CoreWebView2);

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
            return new HiddenWebView2Host(hostWindow, browser);

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

    private static void HardenBrowserForHiddenPdfRendering(CoreWebView2 coreWebView)
    {
        CoreWebView2Settings settings = coreWebView.Settings;

        // Native host integration: keep closed unless explicitly needed.
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;

        // Hidden renderer: no user/debug/browser UI.
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;

        // Avoid profile side effects.
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;

        // Hidden/non-interactive rendering hygiene.
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsPinchZoomEnabled = false;
        settings.IsSwipeNavigationEnabled = false;

        // Optional, but usually good for PDF export correctness:
        // don't turn WebView2's built-in error page into a printable "valid" document.
        settings.IsBuiltInErrorPageEnabled = false;

        // Keep enabled for chart/UI libraries.
        settings.IsScriptEnabled = true;

        // Keep enabled for external URLs.
        settings.IsReputationCheckingRequired = true;

        // Don't handle here so that the export scope can handle it in content validation and error reporting.
        //coreWebView.DownloadStarting += (_, e) =>
        //{
        //    e.Handled = true;
        //    e.Cancel = true;
        //};

        coreWebView.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
        };

        coreWebView.PermissionRequested += (_, e) =>
        {
            e.Handled = true;
            e.State = CoreWebView2PermissionState.Deny;
        };

        coreWebView.ScriptDialogOpening += (_, e) =>
        {
            e.Accept();
        };

        coreWebView.LaunchingExternalUriScheme += (_, e) =>
        {
            e.Cancel = true;
        };
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
