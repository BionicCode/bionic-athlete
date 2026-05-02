namespace BionicAthlete.Presentation.Reporting;

using System.Collections.Immutable;
using System.IO;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Application.Reporting.Html;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Renders generated activity-report HTML to PDF through a hidden per-operation WebView2 host.
/// </summary>
public sealed class WebView2ReportPdfExporter : IReportPdfExporter
{
    private readonly IReportManifestManager _manifestUpdater;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2ReportPdfExporter"/> class.
    /// </summary>
    /// <param name="manifestUpdater">Manifest updater used after the PDF file is physically generated.</param>
    public WebView2ReportPdfExporter(IReportManifestManager manifestUpdater)
    {
        ArgumentNullException.ThrowIfNull(manifestUpdater);

        _manifestUpdater = manifestUpdater;
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
            throw new FileNotFoundException("The specified HTML file does not exist.", request.ReportPackage.HtmlFilePath);
        }

        string? outputDirectoryPath = Path.GetDirectoryName(request.OutputPdfFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            _ = Directory.CreateDirectory(outputDirectoryPath);
        }

        using HiddenWebView2ReportHost reportHost = await HiddenWebView2ReportHost.CreateAsync(cancellationToken).ConfigureAwait(true);
        var readinessWaiter = new WebView2ReportReadinessWaiter(reportHost.Browser);
        Task reportReadyTask = readinessWaiter.WaitForReportReadyAsync(request.Timeout, cancellationToken);

        reportHost.Browser.CoreWebView2.Navigate(request.SourceUri.AbsoluteUri);
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
        var readinessWaiter = new WebView2ReportReadinessWaiter(reportHost.Browser);
        Task reportReadyTask = readinessWaiter.WaitForReportReadyAsync(request.Timeout, cancellationToken);

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

    //public Task<ReportPdfExportResult> ExportToPdfAsync(PdfExportRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
