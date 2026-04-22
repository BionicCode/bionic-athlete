# Structured CSV Export

## Purpose

`ICsvActivityExporter` currently implements the machine-parseable export target only.
This path is intended for structured data interchange, deterministic tests, aggregation, and later re-import.

The current exporter does not implement the future human-readable presentation export target.

## Entry points

- [`ICsvActivityExporter`](../src/FitToCsvConverter.Data/Exporting/ICsvActivityExporter.cs)
- [`CsvExportRequest`](../src/FitToCsvConverter.Data/Exporting/CsvExportRequest.cs)
- [`FitExportOptions`](../src/FitToCsvConverter.Data/Exporting/FitExportOptions.cs)
- [`CsvActivityExporter`](../src/FitToCsvConverter.Data/Exporting/CsvActivityExporter.cs)

## Structured CSV rules

- `FitExportTarget.StructuredCsv` is the only supported export target in the current pass.
- Column normalization policy lives on `FitExportOptions`, not on `FitField` or other decoded-model types.
- Selected FIT fields are exported from effective decoded values, so edited values still override the original decoded values.
- Invalid FIT sentinels are normalized to missing values before writing CSV.
  - The exporter uses the Garmin FIT SDK 21.195.0 base-type invalid markers for numeric types.
  - Missing structured CSV cells are blank by default.
- Timestamps and durations are treated as different categories.
  - Timestamps are written as ISO-8601 values in UTC.
  - Optional local-time duplicates can be added with `IncludeLocalTimeColumns`.
  - Durations are numeric time spans, not timestamps, and are normalized to seconds.
- Speed columns are normalized per column.
  - Metric export uses `km/h`.
  - Imperial export uses `mph`.
- Distance columns are normalized per column.
  - Metric export uses `km`.
  - Imperial export uses `mi`.
- Headers include explicit unit or timestamp qualifiers by default.
- Array-valued FIT fields still map to one CSV column and are joined in source order using ` | `.

## Current limitations

- Presentation-oriented scaling, report layouts, charting, and Garmin-Connect-style readable formatting are intentionally deferred.
- Structured CSV currently keeps arrays in a single cell rather than expanding them into separate columns.
- The exporter supports the current FIT activity tree only: activity, session, lap, and record node CSV outputs.

## Deferred presentation export

The future presentation export target is expected to handle concerns such as:

- human-facing date and time formatting,
- readable duration formatting such as `hh:mm:ss`,
- report-oriented unit scaling,
- summary blocks,
- charts or workbook-style presentation.

Those policies must stay outside the decoded source model and outside the structured CSV rules implemented in this step.
