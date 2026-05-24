# View C Report Export

## Purpose

View C is the human-readable presentation export over decoded FIT activity data.
It is separate from View A raw canonical data and View B structured machine CSV output.

View C v1 generates a normal folder export:

```text
report/
  activity-report.html
  activity-report.pdf          optional when PDF export is requested
  report-manifest.json
```

The HTML report is a first-class output, not a temporary implementation detail.
PDF export renders the same generated HTML through a UI-bound WebView2 service.

## Boundaries

Neutral report generation lives in `FitToCsvConverter.Reporting` and targets `net10.0`.
It may depend on decoded FIT data from `FitToCsvConverter.Data`, but it must not reference WPF, WebView2, `Window`, controls, file dialogs, or UI services.

UI-bound report rendering lives in `FitToCsvConverter.Controls.Reporting`.
That boundary owns the hidden WebView2 host, readiness waiting, per-operation print settings, and PDF generation.

Allowed flow:

```text
View command
  -> MainViewModel.PrepareHumanReadableReportAsync(...)
  -> FitToCsvConverter.Reporting projection + HTML package
  -> View-owned WebView2 PDF exporter when PDF is requested
```

Forbidden flow:

```text
ViewModel -> WebView2
Reporting -> WPF Window
Data/model -> WebView2
```

## Output Targets

`ActivityReportOutputTarget` currently distinguishes:

- `HtmlOnly`: writes `activity-report.html` and `report-manifest.json`.
- `PdfFromGeneratedHtml`: writes the HTML package, then the UI layer may render `activity-report.pdf` from it.
- `HtmlAndPdf`: explicit future-friendly spelling for callers that want both outputs. In v1 it has the same generated-file semantics as `PdfFromGeneratedHtml`.

All targets keep the HTML report package.
View C v1 does not create ZIP packages.

## Determinism

For the same decoded activity and `ActivityReportExportOptions`, the HTML renderer is deterministic.

The renderer does not use ambient current time, random IDs, remote URLs, remote fonts, CDN resources, or culture-dependent formatting unless those values are supplied through the options.
The export timestamp is passed explicitly through `ActivityReportExportOptions.ExportTimestampUtc`.

## HTML And Print Design

The v1 HTML report is self-contained:

- inline CSS,
- inline SVG charts,
- no CDN,
- no remote fonts,
- no internet dependency.

The CSS is intentionally print-oriented and includes:

- `@media print`,
- `@page`,
- A4 portrait preset,
- US Letter portrait preset,
- `break-inside: avoid`,
- `break-after: avoid`,
- `page-break-inside: avoid`,
- `page-break-after: avoid`,
- `thead { display: table-header-group; }`.

These rules are applied around metric cards, section headers, chart panels, chart legends, key tables, and provenance callouts where practical.

## Chart Strategy

View C v1 uses a small internal inline SVG chart renderer.
This keeps the report offline, deterministic, testable, and print-safe.
No browser chart library or CDN dependency is required.

Large record streams are downsampled deterministically before chart rendering.
If richer charts are needed later, a local bundled library can be considered, but View C should still avoid internet dependencies by default.

## WebView2 PDF Export

PDF generation belongs to the UI/presentation boundary because WebView2 is UI-thread and control-bound.

The production PDF path:

1. creates a hidden WebView2 host for the operation,
2. initializes `CoreWebView2`,
3. navigates to the generated HTML file,
4. waits for the report to signal `ReportReady`,
5. creates fresh `CoreWebView2PrintSettings` from `browser.CoreWebView2.Environment.CreatePrintSettings()`,
6. maps neutral `ActivityReportPageSettings` into those settings,
7. calls `PrintToPdfAsync(outputPath, printSettings)`,
8. checks the returned success value,
9. verifies the PDF exists and is non-empty,
10. updates the report manifest with the PDF artifact,
11. disposes the hidden host.

Print settings are per operation.
They are not cached or shared across WebView2 instances.

## Report Readiness

The generated HTML remains user-openable in normal browsers.
The readiness script checks WebView2 messaging before posting:

```javascript
if (window.chrome?.webview?.postMessage) {
  window.chrome.webview.postMessage({
    type: "ReportReady",
    schemaVersion: 1
  });
}
```

The report posts `ReportReady` only after DOM readiness, font readiness when available, image decoding, inline SVG presence, and two animation-frame ticks.
If report-side readiness fails, it posts `ReportFailed` with a message when WebView2 messaging is available.

The WebView2 PDF exporter fails on navigation error, `ReportFailed`, timeout, cancellation, `PrintToPdfAsync` returning `false`, or an empty/missing PDF file.

## Report Sections

The v1 one-activity report includes these section families where data is available:

- Overview,
- Timing,
- Power,
- Heart Rate,
- Cadence / Speed,
- Respiration / Temperature,
- Stamina / Hydration,
- Laps / Intervals,
- Device and Source Metadata,
- Data Quality and Provenance.

Stamina and sweat-loss values remain clearly labeled as inferred aliases from preserved unknown FIT fields when they are sourced that way.
The report does not claim those fields are public standard FIT profile fields.

## Limitations

View C v1 is one-activity only.
It is designed so later report types can project from persisted history, date ranges, training summaries, nutrition, body metrics, or coaching timelines without parsing View B CSV output.

Deferred work:

- history/range reports,
- nutrition/body metrics reports,
- XLSX workbooks,
- report ZIP packaging,
- interactive preview,
- physical/user printing,
- product-wide project renaming.
