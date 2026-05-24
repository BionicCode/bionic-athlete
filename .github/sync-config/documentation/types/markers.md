# Markers

Marker delimiter object used by marker-scoped managed scopes.

## Shape

```json
{
  "start": "<!-- BEGIN REPOSITORY SPECIFICS -->",
  "end": "<!-- END REPOSITORY SPECIFICS -->"
}
```

## Placement

| Property | Value |
|---|---|
| Placement | Nested object |
| Valid parent | [`ManifestEntry`](manifest-entry.md) |
| Parent property | `markers` |

## Attributes

| Attribute | Required | Type | Description |
|---|---:|---|---|
| `start` | Yes | string | Non-empty start delimiter. |
| `end` | Yes | string | Non-empty end delimiter. |

## Rules

- `start` and `end` must not be identical.
- `markers` is required when `managed_scope` is `outside_markers` or `inside_markers`.
- `markers` is forbidden when `managed_scope` is `whole_file`.
- Marker delimiters are exact text delimiters, not regular expressions.
- Marker matching does not trim, normalize whitespace, change case, or use similar-looking text.
- Start and end delimiters may appear anywhere in a line or as full lines.
- A marker block starts immediately after the matched start delimiter and ends immediately before the matched end delimiter.
- Multiple marker blocks are allowed.
- Adjacent marker blocks are allowed.
- Empty inside content and empty outside content are allowed.
- Nested marker blocks are rejected.
- Source and target marker blocks are matched by occurrence order.
- Source and target marker block count mismatch fails before write.
- When a target file exists, composed output preserves marker delimiter slices from the target file.
- Marker-scoped synchronization is text-file behavior and uses strict UTF-8 decoding and encoding.
