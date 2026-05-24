# FIT Export Glossary

This glossary defines the export vocabulary used by the decoded FIT exporter, manifest, and tests.

## Data Views

- **View A**: The raw canonical FIT view. It is the exhaustive decoded source view used for audit, debugging, and later local persistence ingestion.
- **View B**: The structured machine view. It is the default CSV projection for users and downstream tools.
- **View C**: The future human-readable presentation view. It is intentionally not implemented by the current exporter.
- **Raw canonical view**: Same as View A. It preserves standard, developer, unknown, and vendor/future data with source metadata.
- **Structured machine view**: Same as View B. It applies deterministic grouping, units, canonical names, aliases, and documented derived or mapped convenience columns.
- **Human-readable presentation view**: Same as View C. It will later handle readable labels, local-time display, formatted durations, summaries, charts, and reports.

## Field Origins

- **Public standard FIT field**: A field whose message and field identity matches the public Garmin FIT profile catalog generated from `docs/reference/garmin-fit/Profile.xlsx`.
- **Developer field**: A FIT developer/custom field described by FIT developer-data metadata. It is not part of the public standard profile but is source-native FIT data.
- **Unknown/unmapped field**: A field preserved from the source FIT file where the SDK/profile metadata did not provide a public semantic name.
- **Vendor/future field**: A preserved field that may represent vendor-private data or a newer FIT field not known to the pinned public profile.
- **Raw unmapped**: The View B `raw_unmapped/` output group. It consolidates unknown, unmapped, vendor, or future fields into a long-format machine-readable table.
- **Direct field**: A value exported directly from a decoded FIT field without formula derivation.
- **Mapped unknown field**: A preserved unknown/unmapped FIT field that is promoted into a structured View B convenience column because reference evidence maps it to a Garmin Connect label.
- **Derived field**: A value calculated from one or more decoded FIT fields using a documented formula or algorithm.
- **Unavailable field**: A field or Garmin Connect-visible value that the exporter cannot source or derive from the current decoded FIT data.
- **GarminConnectOnlyOrUnconfirmed**: A Garmin Connect-visible value that is not confirmed as source-native FIT data and is not reliably derivable from the current FIT file.

## Manifest and Naming

- **Profile coverage**: The manifest report that classifies exported fields as matched public standard profile, developer field, or unknown/unmapped preserved field.
- **Field dictionary**: The manifest section describing each exported, preserved, derived, mapped, or audit-only field identity.
- **Artifact**: One generated file in an export result, such as a node CSV, consolidated CSV, raw-lossless CSV, or manifest JSON.
- **Bundle path**: The path an artifact uses inside the ZIP bundle and in the manifest, for example `core/reference_session.csv`.
- **Canonical name**: A stable machine identifier, usually `<message_family>.<field_name>`, such as `session.total_work`.
- **Export name**: The actual column name written to a specific artifact. It can be overridden by export request state and may repeat across different message families.
- **Alias**: A secondary label attached for cross-reference or later presentation use.
- **Garmin Connect alias**: An alias sourced from Garmin Connect PDF/reference output. It is never the primary machine identity.

## Field Classification Values

- **DirectStandardFit**: Use when a value is exported directly from a decoded public standard FIT field.
- **DirectDeveloperField**: Use when a value is exported directly from a decoded FIT developer field.
- **DerivedFromFit**: Use when a value is formula-derived from decoded FIT data that is already present in View A.
- **DerivedFromRestoredFitMessages**: Use when a value is derived from restored FIT messages that were previously omitted from the activity tree. Prefer a more specific classification when possible.
- **MappedFromUnmappedFitField**: Use when a structured convenience value is mapped from a preserved unknown/unmapped FIT field, such as `session.unknown_178`, and reference evidence supports the Garmin Connect alias.
- **GarminConnectOnlyOrUnconfirmed**: Use when a Garmin Connect-visible value is not confirmed in the source FIT file and does not have a reliable derivation.
- **Unavailable**: Use when a value is known but absent from the source and no reliable derivation is available.
- **RawPreservedField**: Use for source data preserved primarily for audit/debug completeness rather than semantic projection.
- **UnmappedField**: Use for a field inside a known message family where no public profile/developer semantic name is available.
- **UnknownMessageFamily**: Use for a preserved message family that the decoder cannot map to a known public message family.
- **VendorOrFutureField**: Use for preserved data that likely represents vendor-private or future profile content.

## Edge 840 Reference Mapped Fields

For the current Edge 840 reference activity, the exporter can expose these View B convenience columns:

- `session.est_sweat_loss [ml]` from `session.unknown_178`, alias `Est. Sweat Loss`.
- `session.beginning_potential [%]` from `session.unknown_205`, alias `Beginning Potential`.
- `session.ending_potential [%]` from `session.unknown_206`, alias `Ending Potential`.
- `session.min_stamina [%]` from `session.unknown_207`, alias `Min Stamina`.

These values are available in this activity export as inferred aliases from preserved unknown FIT session fields.
They are not currently public standard FIT fields in `Profile.xlsx`, and the exporter must not claim Garmin officially documents those semantic names unless an official source does so.

The manifest records them with:

- `classification = MappedFromUnmappedFitField`
- `provenance.kind = MappedFromUnmappedFitField`
- `provenance.sourceFields` containing the exact `session.unknown_*` source field
- Garmin Connect alias metadata with confidence and reference notes
- profile coverage classified as `UnknownOrUnmappedPreservedField`, not `MatchedPublicStandardProfile`
