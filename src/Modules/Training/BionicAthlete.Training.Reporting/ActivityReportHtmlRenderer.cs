namespace FitBionicAthlete.Training.Reporting;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Renders a View C report as a deterministic, self-contained HTML package.
/// </summary>
public sealed class ActivityReportHtmlRenderer : IActivityReportHtmlRenderer
{
    private const int ReportSchemaVersion = 1;
    private const string RendererVersion = "view-c-html-v1";
    private static readonly JsonSerializerOptions s_manifestJsonOptions = CreateManifestJsonOptions();
    private readonly IReportChartRenderer _chartRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportHtmlRenderer"/> class.
    /// </summary>
    /// <param name="chartRenderer">Renderer used for deterministic inline SVG charts.</param>
    public ActivityReportHtmlRenderer(IReportChartRenderer chartRenderer)
    {
        ArgumentNullException.ThrowIfNull(chartRenderer);

        _chartRenderer = chartRenderer;
    }

    /// <inheritdoc />
    public async Task<HtmlReportPackage> RenderAsync(
        ActivityReport report,
        ActivityReportExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);

        string reportDirectoryPath = Path.Combine(options.OutputDirectoryPath, "report");
        _ = Directory.CreateDirectory(reportDirectoryPath);

        string htmlFilePath = Path.Combine(reportDirectoryPath, "activity-report.html");
        string manifestFilePath = Path.Combine(reportDirectoryPath, "report-manifest.json");
        string? pdfFilePath = options.OutputTarget == ActivityReportOutputTarget.HtmlOnly
            ? null
            : Path.Combine(reportDirectoryPath, "activity-report.pdf");

        string html = RenderHtml(report, options);
        await File.WriteAllTextAsync(htmlFilePath, html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        ImmutableArray<ActivityReportDiagnostic> diagnostics = report.Diagnostics.IsDefault
            ? ImmutableArray<ActivityReportDiagnostic>.Empty
            : report.Diagnostics;
        var package = new HtmlReportPackage(
            reportDirectoryPath,
            htmlFilePath,
            manifestFilePath,
            pdfFilePath,
            options.OutputTarget,
            options.PageSettings,
            diagnostics);

        ActivityReportManifest manifest = CreateManifest(report, package, includePdfArtifact: false);
        await WriteManifestAsync(manifestFilePath, manifest, cancellationToken).ConfigureAwait(false);

        return package;
    }

    private string RenderHtml(ActivityReport report, ActivityReportExportOptions options)
    {
        var builder = new StringBuilder();
        string pageClass = options.PageSettings.PagePreset == ActivityReportPagePreset.UsLetterPortrait
            ? "page-us-letter"
            : "page-a4";

        _ = builder.AppendLine("<!doctype html>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<html lang=\"{HtmlText.Encode(options.Culture.Name)}\">");
        _ = builder.AppendLine("<head>");
        _ = builder.AppendLine("<meta charset=\"utf-8\" />");
        _ = builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<title>{HtmlText.Encode(report.Title)}</title>");
        _ = builder.AppendLine("<style>");
        _ = builder.AppendLine(BuildCss(options));
        _ = builder.AppendLine("</style>");
        _ = builder.AppendLine("<script>");
        _ = builder.AppendLine(BuildReadinessScript());
        _ = builder.AppendLine("</script>");
        _ = builder.AppendLine("</head>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<body class=\"{pageClass}\">");
        _ = builder.AppendLine("<main class=\"report-shell\">");
        _ = builder.AppendLine("<header class=\"report-header avoid-break\">");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<p class=\"eyebrow\">Activity Report</p><h1>{HtmlText.Encode(report.Title)}</h1>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<p>Generated {HtmlText.Encode(report.GeneratedAtUtc.ToString("f", options.Culture))} UTC</p>");
        _ = builder.AppendLine("</header>");

        foreach (ActivityReportSection section in report.Sections)
        {
            RenderSection(builder, section, options);
        }

        _ = builder.AppendLine("</main>");
        _ = builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private void RenderSection(StringBuilder builder, ActivityReportSection section, ActivityReportExportOptions options)
    {
        _ = builder.Append(CultureInfo.InvariantCulture, $"<section id=\"{HtmlText.Encode(section.Id)}\" class=\"report-section\">");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<h2 class=\"section-title avoid-break\">{HtmlText.Encode(section.Title)}</h2>");

        if (!string.IsNullOrWhiteSpace(section.Notes))
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<p class=\"section-note provenance-callout avoid-break\">{HtmlText.Encode(section.Notes)}</p>");
        }

        if (!section.Metrics.IsDefaultOrEmpty)
        {
            _ = builder.AppendLine("<div class=\"metric-grid\">");
            foreach (ActivityReportMetric metric in section.Metrics)
            {
                RenderMetric(builder, metric);
            }

            _ = builder.AppendLine("</div>");
        }

        if (!section.Charts.IsDefaultOrEmpty)
        {
            foreach (ActivityReportChart chart in section.Charts)
            {
                _ = builder.AppendLine("<figure class=\"chart-panel avoid-break\">");
                _ = builder.Append(CultureInfo.InvariantCulture, $"<figcaption>{HtmlText.Encode(chart.Title)}</figcaption>");
                _ = builder.AppendLine(_chartRenderer.RenderChart(chart, options));
                _ = builder.AppendLine("<div class=\"chart-legend avoid-break\">Source: FIT record stream</div>");
                _ = builder.AppendLine("</figure>");
            }
        }

        if (!section.Tables.IsDefaultOrEmpty)
        {
            foreach (ActivityReportTable table in section.Tables)
            {
                RenderTable(builder, table);
            }
        }

        _ = builder.AppendLine("</section>");
    }

    private static void RenderMetric(StringBuilder builder, ActivityReportMetric metric)
    {
        string unit = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : $" <span class=\"metric-unit\">{HtmlText.Encode(metric.Unit)}</span>";
        _ = builder.AppendLine("<article class=\"metric-card avoid-break\">");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<div class=\"metric-label\">{HtmlText.Encode(metric.Label)}</div>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<div class=\"metric-value\">{HtmlText.Encode(metric.FormattedValue)}{unit}</div>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<div class=\"metric-classification\">{HtmlText.Encode(metric.Classification.ToString())}</div>");
        if (!string.IsNullOrWhiteSpace(metric.ProvenanceNote))
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<p class=\"metric-note\">{HtmlText.Encode(metric.ProvenanceNote)}</p>");
        }

        _ = builder.AppendLine("</article>");
    }

    private static void RenderTable(StringBuilder builder, ActivityReportTable table)
    {
        _ = builder.AppendLine("<div class=\"table-panel avoid-break\">");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<h3>{HtmlText.Encode(table.Title)}</h3>");
        _ = builder.AppendLine("<table class=\"report-table key-table\">");
        _ = builder.AppendLine("<thead><tr>");
        foreach (ActivityReportTableColumn column in table.Columns)
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<th>{HtmlText.Encode(column.Header)}</th>");
        }

        _ = builder.AppendLine("</tr></thead><tbody>");
        foreach (ActivityReportTableRow row in table.Rows)
        {
            _ = builder.AppendLine("<tr>");
            foreach (string cell in row.Cells)
            {
                _ = builder.Append(CultureInfo.InvariantCulture, $"<td>{HtmlText.Encode(cell)}</td>");
            }

            _ = builder.AppendLine("</tr>");
        }

        _ = builder.AppendLine("</tbody></table>");
        _ = builder.AppendLine("</div>");
    }

    private static ActivityReportManifest CreateManifest(
        ActivityReport report,
        HtmlReportPackage package,
        bool includePdfArtifact)
    {
        ImmutableArray<ActivityReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ActivityReportManifestArtifact>();
        artifacts.Add(new ActivityReportManifestArtifact("HtmlReport", "activity-report.html", "text/html"));
        artifacts.Add(new ActivityReportManifestArtifact("ReportManifest", "report-manifest.json", "application/json"));
        if (includePdfArtifact && !string.IsNullOrWhiteSpace(package.PdfFilePath))
        {
            artifacts.Add(new ActivityReportManifestArtifact("PdfReport", "activity-report.pdf", "application/pdf"));
        }

        return new ActivityReportManifest(
            ReportSchemaVersion,
            RendererVersion,
            report.ReportId,
            report.SourceFilePath,
            report.GeneratedAtUtc,
            package.OutputTarget,
            package.PageSettings.PagePreset,
            artifacts.ToImmutable(),
            report.Sections.Select(static section => section.Id).ToImmutableArray(),
            package.Diagnostics);
    }

    private static async Task WriteManifestAsync(
        string manifestFilePath,
        ActivityReportManifest manifest,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(manifest, s_manifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildCss(ActivityReportExportOptions options)
    {
        string margin = FormatInches(options.PageSettings.MarginTopInches);
        string marginRight = FormatInches(options.PageSettings.MarginRightInches);
        string marginBottom = FormatInches(options.PageSettings.MarginBottomInches);
        string marginLeft = FormatInches(options.PageSettings.MarginLeftInches);

        return $$"""
            :root {
              color-scheme: light;
              --ink: #17202a;
              --muted: #5d6d7e;
              --paper: #fbf7ef;
              --panel: #ffffff;
              --line: #d8c7ad;
              --accent: #1f6f5b;
              --accent-soft: #dbece4;
              --warning: #8a5a00;
            }

            @page a4-report {
              size: A4 portrait;
              margin: {{margin}} {{marginRight}} {{marginBottom}} {{marginLeft}};
            }

            @page letter-report {
              size: Letter portrait;
              margin: {{margin}} {{marginRight}} {{marginBottom}} {{marginLeft}};
            }

            * {
              box-sizing: border-box;
            }

            body {
              margin: 0;
              background: linear-gradient(135deg, #efe1c4 0%, #f8f0df 42%, #e8efe8 100%);
              color: var(--ink);
              font-family: Georgia, "Times New Roman", serif;
              line-height: 1.42;
            }

            body.page-a4 {
              page: a4-report;
            }

            body.page-us-letter {
              page: letter-report;
            }

            .report-shell {
              max-width: 1040px;
              margin: 0 auto;
              padding: 32px;
            }

            .report-header,
            .report-section,
            .metric-card,
            .chart-panel,
            .table-panel,
            .provenance-callout {
              background: rgba(255, 255, 255, 0.92);
              border: 1px solid var(--line);
              border-radius: 20px;
              box-shadow: 0 18px 42px rgba(72, 45, 16, 0.10);
            }

            .report-header {
              padding: 30px;
              margin-bottom: 24px;
            }

            .eyebrow {
              margin: 0 0 4px;
              color: var(--accent);
              font: 700 0.78rem/1.2 "Trebuchet MS", sans-serif;
              letter-spacing: 0.16em;
              text-transform: uppercase;
            }

            h1,
            h2,
            h3 {
              margin: 0;
              line-height: 1.1;
            }

            h1 {
              font-size: 2.6rem;
            }

            .report-section {
              padding: 24px;
              margin: 24px 0;
            }

            .section-title {
              margin-bottom: 16px;
              font-size: 1.7rem;
            }

            .section-note {
              padding: 12px 14px;
              color: var(--warning);
              background: #fff6da;
            }

            .metric-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
              gap: 14px;
            }

            .metric-card {
              padding: 16px;
            }

            .metric-label {
              color: var(--muted);
              font: 700 0.78rem/1.2 "Trebuchet MS", sans-serif;
              letter-spacing: 0.08em;
              text-transform: uppercase;
            }

            .metric-value {
              margin-top: 7px;
              font-size: 1.65rem;
              font-weight: 700;
            }

            .metric-unit {
              font-size: 0.9rem;
              color: var(--muted);
            }

            .metric-classification,
            .metric-note,
            .chart-legend {
              margin-top: 8px;
              color: var(--muted);
              font-size: 0.8rem;
            }

            .chart-panel,
            .table-panel {
              margin-top: 18px;
              padding: 16px;
            }

            .chart-panel figcaption {
              margin-bottom: 8px;
              font-weight: 700;
            }

            .chart-svg {
              width: 100%;
              height: auto;
            }

            .chart-background {
              fill: #f8fbf8;
            }

            .chart-axis {
              stroke: #7e8b82;
              stroke-width: 1;
            }

            .chart-line {
              stroke: var(--accent);
              stroke-width: 2.4;
              stroke-linecap: round;
              stroke-linejoin: round;
            }

            .chart-label,
            .chart-tick {
              fill: var(--muted);
              font-family: "Trebuchet MS", sans-serif;
              font-size: 12px;
            }

            .report-table {
              width: 100%;
              border-collapse: collapse;
              margin-top: 12px;
              font-size: 0.92rem;
            }

            .report-table th,
            .report-table td {
              border-bottom: 1px solid var(--line);
              padding: 8px 10px;
              text-align: left;
            }

            .report-table th {
              background: var(--accent-soft);
            }

            thead {
              display: table-header-group;
            }

            @media print {
              body {
                background: #ffffff;
              }

              .report-shell {
                max-width: none;
                padding: 0;
              }

              .report-header,
              .report-section,
              .metric-card,
              .chart-panel,
              .chart-legend,
              .key-table,
              .section-title,
              .provenance-callout,
              .avoid-break {
                break-inside: avoid;
                page-break-inside: avoid;
              }

              .section-title {
                break-after: avoid;
                page-break-after: avoid;
              }

              .report-section {
                box-shadow: none;
                margin: 0 0 16px;
              }

              .metric-grid {
                grid-template-columns: repeat(2, minmax(0, 1fr));
              }
            }
            """;
    }

    private static string BuildReadinessScript()
        => """
            (() => {
              const postToWebView = (message) => {
                if (window.chrome?.webview?.postMessage) {
                  window.chrome.webview.postMessage(message);
                }
              };

              const waitForImages = async () => {
                const images = Array.from(document.images || []);
                await Promise.all(images.map((image) => {
                  if (image.complete) {
                    return Promise.resolve();
                  }

                  return image.decode ? image.decode().catch(() => undefined) : Promise.resolve();
                }));
              };

              const waitForFrame = () => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));

              const signalReady = async () => {
                try {
                  if (document.fonts?.ready) {
                    await document.fonts.ready;
                  }

                  await waitForImages();
                  await waitForFrame();
                  postToWebView({ type: "ReportReady", schemaVersion: 1 });
                } catch (error) {
                  postToWebView({
                    type: "ReportFailed",
                    schemaVersion: 1,
                    message: error instanceof Error ? error.message : "Unknown report rendering failure."
                  });
                }
              };

              if (document.readyState === "loading") {
                document.addEventListener("DOMContentLoaded", () => { void signalReady(); }, { once: true });
              } else {
                void signalReady();
              }
            })();
            """;

    private static string FormatInches(double value)
        => string.Create(CultureInfo.InvariantCulture, $"{value:0.###}in");

    private static JsonSerializerOptions CreateManifestJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    internal static ActivityReportManifest CreateManifestForUpdate(HtmlReportPackage package, ActivityReportManifest currentManifest)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(currentManifest);

        ImmutableArray<ActivityReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ActivityReportManifestArtifact>();
        artifacts.AddRange(currentManifest.Artifacts.Where(static artifact => artifact.ArtifactKind != "PdfReport"));
        if (!string.IsNullOrWhiteSpace(package.PdfFilePath))
        {
            artifacts.Add(new ActivityReportManifestArtifact("PdfReport", "activity-report.pdf", "application/pdf"));
        }

        return currentManifest with { Artifacts = artifacts.ToImmutable() };
    }

    internal static JsonSerializerOptions ManifestJsonOptions => s_manifestJsonOptions;
}
