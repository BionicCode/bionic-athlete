namespace FitToCsvConverter.Controls;

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public class HtmlPrinter : IDisposable
{
    public DialogKind DialogKind { get; set; }
    public bool IsDisposed { get; private set; }
    private static readonly TimeSpan s_waitForDialogOpenedDelay = TimeSpan.FromSeconds(2);
    private bool _isInitialized;
    private WebView2? _browser;
    private Window? _browserHost;
    private TaskCompletionSource? _printTaskCompletionSource;
    private CoreWebView2PrintSettings? _printSettings;
    private TaskCompletionSource? _initializationTaskCompletionSource;
    private readonly string _defaultPdfDestinationFilePath = Path.Combine(Path.GetTempPath(), $"html_export_{Guid.NewGuid()}.pdf");
    private string? _pdfDestinationFilePath;

    private string PdfDestinationFilePath
    {
        get => string.IsNullOrWhiteSpace(_pdfDestinationFilePath)
            ? _defaultPdfDestinationFilePath
            : _pdfDestinationFilePath;
        set => _pdfDestinationFilePath = value;
    }

    public async Task PrintAsync(Uri htmlDocumentPath, WebView2PrintSettingsData printSettings)
    {
        ArgumentNullException.ThrowIfNull(printSettings);

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        ArgumentNullException.ThrowIfNull(htmlDocumentPath);

        await PrintInternalAsync(htmlDocumentPath, null, printSettings);
    }

    public async Task PrintAsync(string htmlContent, WebView2PrintSettingsData printSettings)
    {
        ArgumentNullException.ThrowIfNull(printSettings);

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML content is empty or NULL", nameof(htmlContent));
        }

        await PrintInternalAsync(null, htmlContent, printSettings);
    }

    private async Task PrintInternalAsync(Uri? htmlDocumentPath, string? htmlContent, WebView2PrintSettingsData printSettings)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        _printTaskCompletionSource = new TaskCompletionSource();
        PdfDestinationFilePath = printSettings.DestinationFilePath;
        UpdatePrintSettings(printSettings);

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _browser!.Source = htmlDocumentPath ?? throw new ArgumentNullException(nameof(htmlDocumentPath));
        }
        else
        {
            _browser!.NavigateToString(htmlContent);
        }

        await _printTaskCompletionSource.Task;
    }

    private void UpdatePrintSettings(WebView2PrintSettingsData printSettings)
    {
        if (printSettings is null
            || _printSettings is null)
        {
            return;
        }

        if (printSettings.ShouldPrintBackgrounds.HasValue)
        {
            _printSettings.ShouldPrintBackgrounds = printSettings.ShouldPrintBackgrounds.Value;
        }

        if (printSettings.ShouldPrintHeaderAndFooter.HasValue)
        {
            _printSettings.ShouldPrintHeaderAndFooter = printSettings.ShouldPrintHeaderAndFooter.Value;
        }

        if (!string.IsNullOrWhiteSpace(printSettings.HeaderTitle))
        {
            _printSettings.HeaderTitle = printSettings.HeaderTitle;
        }

        if (!string.IsNullOrWhiteSpace(printSettings.FooterUri))
        {
            _printSettings.FooterUri = printSettings.FooterUri;
        }

        if (printSettings.Orientation.HasValue)
        {
            _printSettings.Orientation = printSettings.Orientation.Value;
        }

        if (printSettings.ColorMode.HasValue)
        {
            _printSettings.ColorMode = printSettings.ColorMode.Value;
        }

        if (printSettings.Duplex.HasValue)
        {
            _printSettings.Duplex = printSettings.Duplex.Value;
        }

        if (printSettings.MediaSize.HasValue)
        {
            _printSettings.MediaSize = printSettings.MediaSize.Value;
        }

        if (printSettings.Collation.HasValue)
        {
            _printSettings.Collation = printSettings.Collation.Value;
        }

        if (printSettings.Copies.HasValue)
        {
            _printSettings.Copies = printSettings.Copies.Value;
        }

        if (printSettings.PagesPerSide.HasValue)
        {
            _printSettings.PagesPerSide = printSettings.PagesPerSide.Value;
        }

        if (printSettings.ScaleFactor.HasValue)
        {
            _printSettings.ScaleFactor = printSettings.ScaleFactor.Value;
        }

        if (printSettings.PageWidth.HasValue)
        {
            _printSettings.PageWidth = printSettings.PageWidth.Value;
        }

        if (printSettings.PageHeight.HasValue)
        {
            _printSettings.PageHeight = printSettings.PageHeight.Value;
        }

        if (printSettings.MarginTop.HasValue)
        {
            _printSettings.MarginTop = printSettings.MarginTop.Value;
        }

        if (printSettings.MarginBottom.HasValue)
        {
            _printSettings.MarginBottom = printSettings.MarginBottom.Value;
        }
    }

    private async Task InitializeAsync()
    {
        _browser = new WebView2();
        _browser.NavigationCompleted += OnNavigationCompleted;
        await LoadSystemPrintHostAsync(_browser, isCloseAfterLoadedEnabled: true);
        await _browser.EnsureCoreWebView2Async();
        _printSettings = _browser.CoreWebView2.Environment.CreatePrintSettings();
        _isInitialized = true;
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var browser = (WebView2)sender!;

        switch (DialogKind)
        {
            case DialogKind.Hidden:
                _ = await browser.CoreWebView2.PrintToPdfAsync(PdfDestinationFilePath);
                break;
            case DialogKind.BrowserPrintDialog:
                await LoadBrowserPrintHostAsync(_browser);
                _ = await browser.CoreWebView2.ExecuteScriptAsync("window.print(); window.chrome.webview.postMessage('Printed');");
                CloseBrowserHost();
                break;
            case DialogKind.SystemPrintDialog:
            case DialogKind.Default:
            default:
                browser.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);

                // Wait for print dialog to open.
                // This is necessary to avoid a race condition for the client.
                // If we return too early printing itself will generally work.
                // However, the client code would not be properly synchronized.
                // For example, if we would return immediately (before the print dialog has opened)
                // and the client would directly dispose the HtmlPrinter then the printing would be aborted prematurely (the print dialog would never show).
                // It's important to wait for the dialog to open befoer we allow the client code to continue execution.
                // The print dialog is an external process that we can't observe. That's why we have to use a less grcafull way (Task:delay).
                await Task.Delay(HtmlPrinter.s_waitForDialogOpenedDelay);
                break;
        }

        _printTaskCompletionSource?.SetResult();
    }

    private async Task LoadBrowserPrintHostAsync(WebView2 browser)
    {
        _initializationTaskCompletionSource = new TaskCompletionSource();
        _browserHost = new Window
        {
            Content = browser,
            Opacity = 0,
            Width = double.NaN,
            Height = double.NaN,
            WindowStyle = WindowStyle.SingleBorderWindow,
            WindowState = WindowState.Normal,
            ShowInTaskbar = true
        };

        _browserHost.Closed += OnBrowserHostClosed;
        _browserHost.Loaded += OnBrowserPrintHostLoaded;
        _browserHost.Show();
        await _initializationTaskCompletionSource.Task;
    }

    private async Task LoadSystemPrintHostAsync(WebView2 browser, bool isCloseAfterLoadedEnabled)
    {
        _initializationTaskCompletionSource = new TaskCompletionSource();
        _browserHost = new Window
        {
            Content = browser,
            Opacity = 0,
            Width = double.NaN,
            Height = double.NaN,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Minimized,
            ShowInTaskbar = false
        };

        _browserHost.Closed += OnBrowserHostClosed;
        if (isCloseAfterLoadedEnabled)
        {
            _browserHost.Loaded += OnSystemPrintHostLoaded;
        }
        else
        {
            _initializationTaskCompletionSource.SetResult();
        }

        _browserHost.Show();
        await _initializationTaskCompletionSource.Task;
    }

    private void CloseBrowserHost()
      => _browserHost?.Close();

    private void OnBrowserPrintHostLoaded(object sender, RoutedEventArgs e)
      => _initializationTaskCompletionSource?.SetResult();

    private void OnSystemPrintHostLoaded(object sender, RoutedEventArgs e)
    {
        CloseBrowserHost();
        _initializationTaskCompletionSource?.SetResult();
    }

    private void OnBrowserHostClosed(object? sender, EventArgs e)
      => _browserHost = null;

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _browser?.Dispose();
                _browser = null;
                _browserHost?.Close();
                _browserHost = null;
            }

            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public enum DialogKind
{
    Default = 0,
    BrowserPrintDialog,
    SystemPrintDialog,
    Hidden
}

[Serializable]
public class HtmlPrinterExternalBrowserDialogException : Exception
{
    private const string MessageBody = "Failed to start external browser to show the browser print dialog: {0}. Consider to configure the HtmlPrinter to use the system's print dialog.";
    public HtmlPrinterExternalBrowserDialogException() { }
    public HtmlPrinterExternalBrowserDialogException(string message) : base(string.Format(HtmlPrinterExternalBrowserDialogException.MessageBody, message)) { }
    public HtmlPrinterExternalBrowserDialogException(string message, Exception inner) : base(string.Format(HtmlPrinterExternalBrowserDialogException.MessageBody, message), inner) { }
    protected HtmlPrinterExternalBrowserDialogException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Represents the print settings for printing HTML content using WebView2.
/// </summary>
/// <param name="DestinationFilePath">The file path where the printed output will be saved.</param>
/// <param name="ShouldPrintBackgrounds">Indicates whether backgrounds should be printed.</param>
/// <param name="ShouldPrintHeaderAndFooter">Indicates whether headers and footers should be printed.</param>
/// <param name="HeaderTitle">The title to be printed in the header.</param>
/// <param name="FooterUri">The URI to be printed in the footer.</param>
/// <param name="Orientation">The orientation of the printed content.</param>
/// <param name="ColorMode">The color mode of the printed content.</param>
/// <param name="Duplex">The duplex mode of the printed content.</param>
/// <param name="MediaSize">The media size of the printed content.</param>
/// <param name="Collation">The collation mode of the printed content.</param>
/// <param name="Copies">The number of copies to be printed.</param>
/// <param name="PagesPerSide">The number of pages to be printed on each side.</param>
/// <param name="ScaleFactor">The scale factor for the printed content.</param>
/// <param name="PageWidth">The width of the printed page.</param>
/// <param name="PageHeight">The height of the printed page.</param>
/// <param name="MarginTop">The top margin of the printed page.</param>
/// <param name="MarginBottom">The bottom margin of the printed page.</param>
/// <param name="MarginLeft">The left margin of the printed page.</param>
/// <param name="MarginRight">The right margin of the printed page.</param>
/// <param name="PageRanges">The page ranges to be printed.</param>
public record class WebView2PrintSettingsData(
    [Required] string DestinationFilePath,
    bool? ShouldPrintBackgrounds = true,
    bool? ShouldPrintHeaderAndFooter = null,
    string? HeaderTitle = null,
    string? FooterUri = null,
    CoreWebView2PrintOrientation? Orientation = null,
    CoreWebView2PrintColorMode? ColorMode = null,
    CoreWebView2PrintDuplex? Duplex = null,
    CoreWebView2PrintMediaSize? MediaSize = null,
    CoreWebView2PrintCollation? Collation = null,
    int? Copies = null,
    int? PagesPerSide = null,
    double? ScaleFactor = null,
    double? PageWidth = null,
    double? PageHeight = null,
    double? MarginTop = null,
    double? MarginBottom = null,
    double? MarginLeft = null,
    double? MarginRight = null,
    string? PageRanges = null
);
