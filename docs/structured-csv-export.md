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

## Bundle contents

The exporter treats export output as data views over the same decoded FIT source:

- View A: raw canonical FIT view. This is the exhaustive, lossless source view used for debugging, auditability, and later persistence ingestion.
- View B: structured machine view. This is the default user-facing CSV projection and is optimized for stable machine parsing.
- View C: human-readable presentation view. This is intentionally deferred.

The default `FitExportDataView.StructuredMachine` package emits View B only:

- `core/` node CSVs for selected activity-tree levels (`activity`, `session`, `lap`, `record`)
- grouped metadata CSVs under `metadata/`
- grouped analytics CSVs under `analytics/`
- one consolidated unknown/unmapped table under `raw_unmapped/`
- one manifest JSON file at the bundle root

The default package does not include the raw-lossless View A artifact pile.
Use `FitExportDataView.RawCanonical` when a diagnostic or persistence-ingestion workflow needs one raw CSV per ancillary FIT message family under `raw_lossless/`.

Ancillary message families are exported automatically when they are present on `FitActivity.AncillaryData`.
They are not filtered through the UI field-selection layer because their purpose is completeness and auditability.

## Structured CSV rules

- `FitExportTarget.StructuredCsv` is the only supported export target in the current pass.
- `FitExportDataView.StructuredMachine` is the default CSV data view.
- `FitExportDataView.RawCanonical` is an explicit debug/raw export view, not the default normal export.
- Column normalization policy lives on `FitExportOptions`, not on `FitField` or other decoded-model types.
- Garmin SDK message and standard-field identifiers are normalized to canonical `snake_case` at the decoder boundary so the machine schema matches FitCSVTool-style profile naming instead of CLR-style SDK casing.
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
- Session CSVs may append derived completeness columns when the source activity can support them.
  - `active_calories = total_calories - metabolic_calories`
  - `moving_time` uses direct `total_moving_time` when present and otherwise derives it from record intervals where movement is detected
  - `avg_moving_speed = total_distance / moving_time`
  - `max_avg_power_20min` is the maximum rolling 20-minute average from the record power stream using one-second sample-hold interpolation

## Manifest semantics

The manifest is machine-readable and is intended to answer two questions:

1. Which message families and fields made it into this bundle?
2. Which values are direct FIT data versus exporter-derived completeness values?

The field dictionary currently classifies entries with these values:

- `DirectStandardFit`
- `DirectDeveloperField`
- `DerivedFromFit`
- `DerivedFromRestoredFitMessages`
- `GarminConnectOnlyOrUnconfirmed`
- `Unavailable`
- `RawPreservedField`
- `UnmappedField`
- `UnknownMessageFamily`
- `VendorOrFutureField`

Direct tree fields and direct ancillary fields are exported with source message and field metadata.
Developer fields retain their developer-field identity and any Garmin SDK metadata that was available during decode.
Unknown and vendor-specific data is preserved rather than dropped; when the SDK cannot provide a semantic name, the export keeps the raw `unknown_*` field naming plus source metadata in the manifest.
`exportName` reflects the actual column name written to one artifact, while `canonicalName` is the stable cross-artifact identifier.
This distinction matters because names like `total_work` can legitimately appear in more than one node family (`session.total_work`, `lap.total_work`).

Manifest artifact paths are the physical paths used inside the ZIP bundle.
For example, a manifest artifact path of `core/example_session.csv` must correspond to the actual ZIP entry path `core/example_session.csv`.

The manifest also includes a `profileCoverage` section generated from `docs/reference/garmin-fit/Profile.xlsx`.
This catalog is used only to validate public standard FIT metadata.
It is not used to decide whether developer fields or unknown/vendor fields from a specific source file should be dropped.
Profile coverage classifies exported fields as:

- `MatchedPublicStandardProfile`
- `DeveloperField`
- `UnknownOrUnmappedPreservedField`

## Current limitations

- Presentation-oriented scaling, report layouts, charting, and Garmin-Connect-style readable formatting are intentionally deferred.
- Structured CSV currently keeps arrays in a single cell rather than expanding them into separate columns.
- View B intentionally consolidates ancillary data instead of exposing every raw ancillary CSV by default.
- PDF/report equivalence is intentionally honest rather than speculative.
  - When a Garmin Connect value is not directly present in the FIT file and is not reliably derivable from FIT data, the manifest should classify it as `GarminConnectOnlyOrUnconfirmed` instead of fabricating it as source-native FIT data.

## Deferred presentation export

The future presentation export target is expected to handle concerns such as:

- human-facing date and time formatting,
- readable duration formatting such as `hh:mm:ss`,
- report-oriented unit scaling,
- summary blocks,
- charts or workbook-style presentation.

Those policies must stay outside the decoded source model and outside the structured CSV rules implemented in this step.
