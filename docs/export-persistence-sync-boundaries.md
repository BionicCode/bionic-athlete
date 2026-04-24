# Export, Persistence, and Sync Boundaries

## Purpose

This document describes the current boundary between:

- decoded FIT import data,
- CSV export requests and results,
- planned local persistence contracts,
- and planned remote sync contracts.

The current implementation step adds the first decoded-model-based CSV exporter. Local persistence and remote sync remain separate concerns.

## Current source model

The imported FIT activity tree still lives in `FitToCsvConverter.Data`:

- `FitActivity`
- `FitSession`
- `FitLap`
- `FitRecord`
- `FitField`

This is the imported source model. It is not the CSV schema, not the local database schema, and not the future remote API schema.

`FitField` still carries transitional mutable compatibility state through `FitFieldState`:

- `DisplayName`
- `ColumnName`
- `IsIncludedInExport`
- optional edited decoded values

That state is acceptable for the current CSV step, but it should not become the permanent cross-boundary contract.

## Export boundary

### Entry points

The CSV export boundary is defined by:

- `ICsvActivityExporter`
- `CsvExportRequest`
- `CsvNodeExportRequest`
- `CsvColumnSelection`
- `CsvExportResult`
- `ExportedArtifact`

The current CSV exporter exposes two implemented data views:

- `FitExportDataView.StructuredMachine`: the default View B projection for normal machine-readable export.
- `FitExportDataView.RawCanonical`: the optional View A diagnostic/raw export for lossless ancillary message-family inspection.

View C, the human-readable presentation projection, remains a separate future target and must not be produced by parsing View B CSV output.

The UI or application layer does not send `DataField` or `ExportData` directly to the exporter.

Instead, current UI-facing selection state is mapped into:

- `CsvExportColumnRequest`
- `CsvExportRequestFactory`

`CsvExportRequestFactory` is the seam between UI/application selection state and the final exporter contract.

### Request flow

Current flow:

1. `ExportData` gathers the current checkbox and naming state from `DataField` / `FitField`.
2. `ExportData` maps that state to `CsvExportColumnRequest` values.
3. `CsvExportRequestFactory` groups selected columns by `FitNodeType`, applies deterministic ordering, and generates per-node CSV file paths.
4. `ICsvActivityExporter` writes View B by default from the decoded source model and groups generated artifacts under stable bundle paths such as `core/`, `metadata/`, `analytics/`, and `raw_unmapped/`.
5. `MainViewModel` passes the generated `ExportedArtifact` files into the existing ZIP archive flow.
6. The archive flow uses `ExportedArtifact.BundlePath` so ZIP entry paths match the manifest artifact paths exactly.

### Ordering and naming rules

- Column identity is based on `FitExportColumnKey`, not CLR property names.
- Column ordering is deterministic: explicit `Order`, then original source field name as a stable tie-breaker.
- Output file naming is deterministic per node:
  - `<source>_activity.csv`
  - `<source>_session.csv`
  - `<source>_lap.csv`
  - `<source>_record.csv`
- Default bundle paths are grouped:
  - `core/<source>_activity.csv`
  - `core/<source>_session.csv`
  - `core/<source>_lap.csv`
  - `core/<source>_record.csv`
  - `metadata/<source>_metadata.csv`
  - `analytics/<source>_analytics.csv`
  - `raw_unmapped/<source>_raw_unmapped.csv`
- Effective export column names come from the mapped request, which currently reads `FitField.State.ColumnName`.

### Value shaping

- Export always reads `FitField.GetEffectiveDecodedValues()`.
- Edited values override immutable original decoded values when present.
- Multi-value FIT fields are currently written into a single CSV cell using ` | ` in source order.
- The immutable FIT source values remain available through `FitField.Original.OriginalValues`.

### View A versus View B

View A remains the raw canonical source view and must preserve standard FIT messages, developer fields, and unknown/vendor/unmapped content even when higher-level semantics are incomplete.
View B is a projection from that source and intentionally reduces default bundle clutter by consolidating ancillary metadata, analytics, and raw unmapped content into grouped machine-friendly artifacts.
The View B manifest includes profile coverage generated from the repository Garmin profile catalog so consumers can distinguish public standard fields, developer fields, and preserved unknown/unmapped fields.

## Persistence boundary

The current step introduces local persistence contracts only. There is no concrete SQLite implementation yet.

Current persistence boundary:

- `IActivityHistoryStore`
- `ImportedActivityEnvelope`
- `StoredActivityRecord`
- `StoredActivitySummary`
- `ActivityHistoryQuery`
- `ActivityPersistenceResult`
- `ActivityFingerprint`

### Intent

These contracts are for durable local history, deduplication, query, and future sync tracking.

Important rules:

- Local persistence is the planned long-term application source of truth.
- The decoded FIT tree is an import/input model, not the permanent storage schema.
- CSV output must not become the canonical persistence model.

### Local implementation direction

Concrete local storage is deferred.

The current architectural recommendation remains:

- likely local-first desktop storage,
- likely SQLite,
- likely EF Core as the stronger medium-term fit once persistence is implemented for multiple health domains.

That recommendation is not a prerequisite of the current CSV export step.

## Remote sync boundary

Remote sync is future-boundary-only in this step.

Current sync contracts:

- `IActivitySyncClient`
- `SyncRequestMetadata`
- `PushActivitiesResult`
- `SyncConflict`

### Rules

- The desktop application must send application-level HTTPS requests, not SQL.
- The hosted endpoint validates requests and generates parameterized SQL internally.
- The desktop application must not depend on PHP details, MySQL syntax, or MariaDB syntax.

### Future sync concerns already modeled

- API versioning: `SyncRequestMetadata.ApiVersion`
- Idempotency: `SyncRequestMetadata.ClientRequestId`
- Client identity: `SyncRequestMetadata.ClientApplicationId`
- Conflict handling: `PushActivitiesResult.Conflicts`

Planned policy direction:

- local-first conflict handling,
- immutable imported activities deduplicated by fingerprint,
- future editable data resolved with explicit optimistic concurrency instead of silent overwrite.

## What is intentionally not coupled

The export, persistence, and sync boundaries must not depend on:

- WPF types,
- ViewModel types,
- `DataField`,
- Garmin SDK runtime types,
- raw SQL text,
- PHP implementation details,
- MySQL or MariaDB SQL syntax.

## Immediate implementation status

Implemented now:

- decoded-model CSV export request/result contracts,
- request factory from selection state to exporter contract,
- CSV exporter,
- `MainViewModel` wiring to the decoded-model exporter,
- local persistence abstractions,
- future sync abstractions.

Deferred:

- concrete SQLite persistence,
- remote HTTPS sync implementation,
- dedicated long-term export schema independent from `FitFieldState`,
- non-CSV export formats.
