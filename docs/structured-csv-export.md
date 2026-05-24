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

For shared terminology, see [FIT Export Glossary](fit-export-glossary.md).

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

## View B consolidation algorithm

View B is projected from View A and avoids dumping the raw-lossless artifact pile into the default ZIP.
The routing rules are:

- `core/`: selected activity-tree node outputs for `activity`, `session`, `lap`, and `record`.
- `metadata/`: consolidated long-format output for ancillary metadata families such as `file_id`, `file_creator`, `device_info`, `device_settings`, `user_profile`, `sport`, `training_settings`, `timestamp_correlation`, and `zones_target`.
- `analytics/`: consolidated long-format output for ancillary analytic/event families such as `event`, `time_in_zone`, `split_summary`, and `hrv`.
- `raw_unmapped/`: consolidated long-format output for unknown message families and unmapped/vendor/future fields that are preserved but not confidently modeled.
- `raw_lossless/`: View A diagnostic/raw mode only. It emits one raw CSV per ancillary message family when `FitExportDataView.RawCanonical` is explicitly requested.

Unknown/unmapped data is excluded from `metadata/` and `analytics/` when the exporter lacks enough semantic confidence to place it in those grouped views.
It remains available in `raw_unmapped/`.
A raw field may be promoted into a structured View B convenience column only when there is documented source evidence and manifest provenance.
Otherwise it remains raw-only.

Duplicate emission is avoided by making View B the default bundle and keeping View A raw-family CSVs out of that bundle.
Every `ExportedArtifact.BundlePath` must match both the ZIP entry path and the manifest `artifactFileName`.

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
- `MappedFromUnmappedFitField`
- `GarminConnectOnlyOrUnconfirmed`
- `Unavailable`
- `RawPreservedField`
- `UnmappedField`
- `UnknownMessageFamily`
- `VendorOrFutureField`

Use `MappedFromUnmappedFitField` for values that are sourced from preserved unknown FIT fields and mapped to Garmin Connect reference labels, but are not public standard FIT fields and are not formula-derived.
For the current Edge 840 reference activity, this applies to:

- `session.est_sweat_loss [ml]` from `session.unknown_178`
- `session.beginning_potential [%]` from `session.unknown_205`
- `session.ending_potential [%]` from `session.unknown_206`
- `session.min_stamina [%]` from `session.unknown_207`

These are inferred aliases from preserved unknown session fields.
Do not describe them as officially named Garmin FIT profile fields unless `Profile.xlsx` or another official Garmin source publishes those names.

These are inferred aliases from preserved unknown session fields.
Do not describe them as officially named Garmin FIT profile fields unless `Profile.xlsx` or another official Garmin source publishes those names.

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

Mapped unknown fields are intentionally reported as `UnknownOrUnmappedPreservedField` in profile coverage.
They must not be counted as `MatchedPublicStandardProfile` simply because View B provides a Garmin Connect alias.

## Provenance metadata

Every formula-derived or mapped View B field carries a `provenance` object in the field dictionary.

Formula-derived fields use:

- `provenance.kind = FormulaDerived`
- `provenance.formula`
- `provenance.sourceFields`
- `provenance.sourceMessageFamilies`
- `provenance.unit`
- `provenance.roundingOrTolerance`
- optional notes describing algorithm caveats

Current formula-derived fields include:

- `session.active_calories = session.total_calories - session.metabolic_calories`
- `session.avg_moving_speed = session.total_distance / moving_time`
- `record.max_avg_power_20min` from `record.power` and `record.timestamp`

Mapped unknown fields use:

- `provenance.kind = MappedFromUnmappedFitField`
- `provenance.sourceFields`, such as `session.unknown_178`
- `provenance.sourceEvidence`
- `provenance.mappingReason`
- Garmin Connect alias metadata and confidence
- notes stating that the source is not publicly named in `Profile.xlsx`

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
