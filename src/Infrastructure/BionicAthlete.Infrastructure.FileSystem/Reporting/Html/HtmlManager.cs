namespace BionicAthlete.Infrastructure.FileSystem.Reporting.Html;

using System;
using System.Collections.Immutable;
using System.Text;

public sealed class HtmlManager
{
    /// <inheritdoc />
    public static async Task WriteToFileAsync(
        ReportManifest reportManifest,
        string reportDirectoryPath,
        string htmlFilePath,
        string manifestFilePath,
        ReportInfo reportInfo,
        string html,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportInfo);
        ArgumentNullException.ThrowIfNull(reportManifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlFilePath);
        ArgumentNullException.ThrowIfNull(html);

        await File.WriteAllTextAsync(htmlFilePath, html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        ImmutableArray<ReportDiagnostic> diagnostics = reportInfo.Diagnostics.IsDefault
            ? ImmutableArray<ReportDiagnostic>.Empty
            : reportInfo.Diagnostics;
        var package = new HtmlReportPackage(
            reportDirectoryPath,
            htmlFilePath,
            manifestFilePath,
            pdfFilePath,
            options.OutputTarget,
            options.PageSettings,
            diagnostics);

        string manifestFilePath = Path.Combine(reportDirectoryPath, "report-manifest.json");
        await ReportManifestManager.WriteManifestAsync(manifestFilePath, reportManifest, cancellationToken).ConfigureAwait(false);
    }
}
