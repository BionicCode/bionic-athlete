namespace FitToCsvConverter.Controls;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public class HtmlPrinter
{
    public DialogKind DialogKind { get; set; }
    private static readonly TimeSpan s_waitForDialogOpenedDelay = TimeSpan.FromSeconds(2);
    private TaskCompletionSource? _printTaskCompletionSource;
    private WebView2PrintSettingsData? _printSettings;
    private TaskCompletionSource? _initializationTaskCompletionSource;
    private readonly string _defaultPdfDestinationFilePath = Path.Combine(Path.GetTempPath(), $"html_export_{Guid.NewGuid()}.pdf");
    private string? _pdfDestinationFilePath;

    public string PdfDestinationFilePath
    {
        get => string.IsNullOrWhiteSpace(_pdfDestinationFilePath)
            ? _defaultPdfDestinationFilePath
            : _pdfDestinationFilePath;
        private set => _pdfDestinationFilePath = value;
    }

    public WebView2PrintSettingsData? PrintSettings => _printSettings;

    public async Task PrintAsync(Uri htmlDocumentPath, WebView2PrintSettingsData printSettings)
    {
        ArgumentNullException.ThrowIfNull(printSettings);
        ArgumentNullException.ThrowIfNull(htmlDocumentPath);

        await PrintInternalAsync(htmlDocumentPath, null, printSettings);
    }

    public async Task PrintAsync(Uri htmlDocumentPath, string pdfDestinationFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfDestinationFilePath);
        ArgumentNullException.ThrowIfNull(htmlDocumentPath);

        _pdfDestinationFilePath = pdfDestinationFilePath;
        await PrintInternalAsync(htmlDocumentPath, null, null);
    }

    public async Task PrintAsync(string htmlContent, WebView2PrintSettingsData printSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);
        ArgumentNullException.ThrowIfNull(printSettings);

        await PrintInternalAsync(null, htmlContent, printSettings);
    }

    public async Task PrintAsync(string htmlContent, string pdfDestinationFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfDestinationFilePath);

        _pdfDestinationFilePath = pdfDestinationFilePath;
        await PrintInternalAsync(null, htmlContent, null);
    }

    private async Task PrintInternalAsync(Uri? htmlDocumentPath, string? htmlContent, WebView2PrintSettingsData? printSettings)
    {
        using WebView2 browser = await CreateBrowserAsync();

        _printTaskCompletionSource = new TaskCompletionSource();
        if (printSettings is not null)
        {
            _printSettings = printSettings;
            PdfDestinationFilePath = _printSettings.DestinationFilePath;
            ApplyPrintSettings(browser);
        }

        // If the print settings are not provided by the caller, we need to expose the default settings via the PrintSettings property.
        _printSettings ??= new WebView2PrintSettingsData(PdfDestinationFilePath, browser.CoreWebView2.Environment.CreatePrintSettings());

        if (htmlDocumentPath is not null)
        {
            browser.Source = htmlDocumentPath;
        }
        else if (!string.IsNullOrWhiteSpace(htmlContent))
        {
            browser.NavigateToString(htmlContent);
        }
        else
        {
            _printTaskCompletionSource.SetResult();
        }

        await _printTaskCompletionSource.Task;
    }

    private void ApplyPrintSettings(WebView2 browser)
    {
        Debug.Assert(_printSettings is not null, "Print settings should not be null at this point.");

        CoreWebView2PrintSettings browserPrintSettings = browser.CoreWebView2.Environment.CreatePrintSettings();
        if (_printSettings.ShouldPrintBackgrounds.HasValue)
        {
            browserPrintSettings.ShouldPrintBackgrounds = _printSettings.ShouldPrintBackgrounds.Value;
        }

        if (_printSettings.ShouldPrintHeaderAndFooter.HasValue)
        {
            browserPrintSettings.ShouldPrintHeaderAndFooter = _printSettings.ShouldPrintHeaderAndFooter.Value;
        }

        if (!string.IsNullOrWhiteSpace(_printSettings.HeaderTitle))
        {
            browserPrintSettings.HeaderTitle = _printSettings.HeaderTitle;
        }

        if (!string.IsNullOrWhiteSpace(_printSettings.FooterUri))
        {
            browserPrintSettings.FooterUri = _printSettings.FooterUri;
        }

        if (_printSettings.Orientation.HasValue)
        {
            browserPrintSettings.Orientation = _printSettings.Orientation.Value;
        }

        if (_printSettings.ColorMode.HasValue)
        {
            browserPrintSettings.ColorMode = _printSettings.ColorMode.Value;
        }

        if (_printSettings.Duplex.HasValue)
        {
            browserPrintSettings.Duplex = _printSettings.Duplex.Value;
        }

        if (_printSettings.MediaSize.HasValue)
        {
            browserPrintSettings.MediaSize = _printSettings.MediaSize.Value;
        }

        if (_printSettings.Collation.HasValue)
        {
            browserPrintSettings.Collation = _printSettings.Collation.Value;
        }

        if (_printSettings.Copies.HasValue)
        {
            browserPrintSettings.Copies = _printSettings.Copies.Value;
        }

        if (_printSettings.PagesPerSide.HasValue)
        {
            browserPrintSettings.PagesPerSide = _printSettings.PagesPerSide.Value;
        }

        if (_printSettings.ScaleFactor.HasValue)
        {
            browserPrintSettings.ScaleFactor = _printSettings.ScaleFactor.Value;
        }

        if (_printSettings.PageWidth.HasValue)
        {
            browserPrintSettings.PageWidth = _printSettings.PageWidth.Value;
        }

        if (_printSettings.PageHeight.HasValue)
        {
            browserPrintSettings.PageHeight = _printSettings.PageHeight.Value;
        }

        if (_printSettings.MarginTop.HasValue)
        {
            browserPrintSettings.MarginTop = _printSettings.MarginTop.Value;
        }

        if (_printSettings.MarginBottom.HasValue)
        {
            browserPrintSettings.MarginBottom = _printSettings.MarginBottom.Value;
        }
    }

    private async Task<WebView2> CreateBrowserAsync()
    {
        var browser = new WebView2();
        browser.NavigationCompleted += OnNavigationCompleted;
        await LoadSystemPrintHostAsync(browser, isCloseAfterLoadedEnabled: true);
        await browser.EnsureCoreWebView2Async();

        return browser;
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
                await LoadBrowserPrintHostAsync(browser);
                _ = await browser.CoreWebView2.ExecuteScriptAsync("window.print(); window.chrome.webview.postMessage('Printed');");
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
        var browserHost = new Window
        {
            Content = browser,
            Opacity = 0,
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.SingleBorderWindow,
            WindowState = WindowState.Normal,
            ShowInTaskbar = true
        };

        browserHost.Loaded += OnBrowserPrintHostLoaded;
        browserHost.Show();
        await _initializationTaskCompletionSource.Task;
    }

    private async Task LoadSystemPrintHostAsync(WebView2 browser, bool isCloseAfterLoadedEnabled)
    {
        _initializationTaskCompletionSource = new TaskCompletionSource();
        var browserHost = new Window
        {
            Content = browser,
            Opacity = 0,
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Minimized,
            ShowInTaskbar = false
        };

        if (isCloseAfterLoadedEnabled)
        {
            browserHost.Loaded += OnSystemPrintHostLoaded;
        }
        else
        {
            _initializationTaskCompletionSource.SetResult();
        }

        browserHost.Show();
        await _initializationTaskCompletionSource.Task;
    }

    private void OnBrowserPrintHostLoaded(object sender, RoutedEventArgs e) => _initializationTaskCompletionSource?.SetResult();

    private void OnSystemPrintHostLoaded(object sender, RoutedEventArgs e)
    {
        var window = (Window)sender;
        window.Close();
        _initializationTaskCompletionSource?.SetResult();
    }
}

public enum DialogKind
{
    Default = 0,
    BrowserPrintDialog,
    SystemPrintDialog,
    Hidden
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
    string? PageRanges = null)
{
    public WebView2PrintSettingsData(string destinationFilePath, CoreWebView2PrintSettings coreWebView2PrintSettings) : this(destinationFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);
        ArgumentNullException.ThrowIfNull(coreWebView2PrintSettings);

        ShouldPrintBackgrounds = coreWebView2PrintSettings.ShouldPrintBackgrounds;
        ShouldPrintHeaderAndFooter = coreWebView2PrintSettings.ShouldPrintHeaderAndFooter;
        HeaderTitle = coreWebView2PrintSettings.HeaderTitle;
        FooterUri = coreWebView2PrintSettings.FooterUri;
        Orientation = coreWebView2PrintSettings.Orientation;
        ColorMode = coreWebView2PrintSettings.ColorMode;
        Duplex = coreWebView2PrintSettings.Duplex;
        MediaSize = coreWebView2PrintSettings.MediaSize;
        Collation = coreWebView2PrintSettings.Collation;
        Copies = coreWebView2PrintSettings.Copies;
        PagesPerSide = coreWebView2PrintSettings.PagesPerSide;
        ScaleFactor = coreWebView2PrintSettings.ScaleFactor;
        PageWidth = coreWebView2PrintSettings.PageWidth;
        PageHeight = coreWebView2PrintSettings.PageHeight;
        MarginTop = coreWebView2PrintSettings.MarginTop;
        MarginBottom = coreWebView2PrintSettings.MarginBottom;
        MarginLeft = coreWebView2PrintSettings.MarginLeft;
        MarginRight = coreWebView2PrintSettings.MarginRight;
        PageRanges = coreWebView2PrintSettings.PageRanges;
    }
}