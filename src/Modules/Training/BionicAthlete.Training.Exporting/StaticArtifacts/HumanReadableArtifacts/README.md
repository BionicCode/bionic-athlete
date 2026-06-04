# FIT Activity Export Bundle

Purpose: structured machine-readable cycling activity export for coaching analysis.

Start here:
1. Read `manifest.json` for schema, artifact paths, field classifications, units, aliases, and provenance.
2. Use `core/*_session.csv` for summary metrics.
3. Use `core/*_record.csv` for time-series analysis.
4. Use `core/*_lap.csv` for lap/interval structure.
5. Use `analytics/*_analytics.csv` for events, zones, HRV, and derived analytics.
6. Use `metadata/*_metadata.csv` for device/profile/source metadata.
7. Use `raw_unmapped/*_raw_unmapped.csv` only when investigating preserved unknown/vendor fields.

Data views:
- View A: raw canonical FIT view, diagnostic/persistence-oriented, not included by default.
- View B: structured machine view, default CSV export in this ZIP.
- View C: future human-readable report view, not included.

Important:
- Garmin Connect aliases are secondary labels, not canonical field names.
- `MappedFromUnmappedFitField` means the value is inferred from preserved unknown FIT fields and matched to Garmin Connect reference output.
- Derived fields include provenance in `manifest.json`.