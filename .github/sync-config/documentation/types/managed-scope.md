# ManagedScope

Declares which portion of the target file is managed by the workflow.

## Placement

| Property | Value |
|---|---|
| Placement | Enum string |
| Valid parent | [`ManifestEntry`](manifest-entry.md) |
| Parent property | `managed_scope` |

## Values

| Value | Supported | Markers | Description |
|---|---:|---|---|
| `whole_file` | Yes | Forbidden | The entire target file is managed by the source file content. |
| `outside_markers` | Yes | Required | Source owns content outside marker blocks. Target owns content inside marker blocks and the marker delimiters. |
| `inside_markers` | Yes | Required | Source owns content inside marker blocks. Target owns content outside marker blocks and the marker delimiters. |

## Notes

Marker-scoped synchronization is text-file behavior. Marker-scoped files are decoded and encoded as strict UTF-8.

Marker validation criteria:

- `markers` is required for `outside_markers` and `inside_markers`.
- `markers` is forbidden for `whole_file`.
- `markers.start` and `markers.end` are required non-empty strings.
- `markers.start` and `markers.end` must not be identical.
- Marker delimiters are matched as exact text, not regular expressions.
- Source and target marker blocks are matched by occurrence order.
