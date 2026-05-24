# Sync Manifest

`sync-manifest.json` describes the files that the reusable workflow manages in a caller repository.

The file is caller-owned and lives at:

```text
.github/sync-config/sync-manifest.json
```

The reusable workflow reads the manifest, validates it against the schema bundled with the workflow version being executed, then runs semantic validation before any source fetch or file write.

## What The Manifest Does

- Declares strict one-to-one mappings from source files to target files.
- Supports source files from multiple repositories in one workflow run.
- Supports nested repository-relative target paths.
- Defines lifecycle, uniqueness, and managed-scope policy for each mapping.
- Lets pull request runs verify that managed targets are present and in sync.
- Lets default-branch maintenance runs create or update one aggregated sync PR.

## Current Version Boundaries

- Target-to-source writes are accepted by the manifest schema but rejected by current execution rules before source fetch or write.
- Two-way synchronization is accepted by the manifest schema but rejected by current execution rules before source fetch or write.
- Rename aliases where source and target basenames differ are rejected.
- Extra tracked files are not deleted.
- Timestamp or newer-wins conflict resolution is not part of the current workflow behavior.
- Large binary assets are outside the intended use case.

## Complete Shape

This example shows the complete manifest structure, including the conditional `markers` child object. A real entry uses either `managed_scope: "whole_file"` without `markers`, or a marker-scoped value with `markers`.

```json
{
  "$schema": "./sync-manifest.schema.json",
  "schema_version": 1,
  "entries": [
    {
      "source_repo": "BionicCode/template-visual-studio-repository",
      "source_ref": "main",
      "source_path": "AGENTS.md",
      "target_path": "AGENTS.md",
      "direction": "source_to_target",
      "lifecycle_policy": "enforce",
      "uniqueness_policy": "none",
      "managed_scope": "outside_markers",
      "markers": {
        "start": "<!-- BEGIN REPOSITORY SPECIFICS -->",
        "end": "<!-- END REPOSITORY SPECIFICS -->"
      }
    }
  ]
}
```

## Type Placement

| Type | Placement | Valid Parent | Parent Property | Description |
|---|---|---|---|---|
| [`ManifestDocument`](types/manifest-document.md) | Top-level object | None | None | Root JSON object for `sync-manifest.json`. |
| [`ManifestEntry`](types/manifest-entry.md) | Array item object | `ManifestDocument` | `entries[]` | One strict managed-file mapping. |
| [`Markers`](types/markers.md) | Nested object | `ManifestEntry` | `markers` | Marker delimiter object for marker-scoped entries. |
| [`RepoRelativeFilePath`](types/repo-relative-file-path.md) | Scalar string | `ManifestEntry` | `source_path`, `target_path` | Repository-relative file path. |
| [`Direction`](types/direction.md) | Enum string | `ManifestEntry` | `direction` | Synchronization direction. |
| [`LifecyclePolicy`](types/lifecycle-policy.md) | Enum string | `ManifestEntry` | `lifecycle_policy` | Missing, changed, and existing target behavior. |
| [`UniquenessPolicy`](types/uniqueness-policy.md) | Enum string | `ManifestEntry` | `uniqueness_policy` | Basename uniqueness policy. |
| [`ManagedScope`](types/managed-scope.md) | Enum string | `ManifestEntry` | `managed_scope` | Portion of the target file managed by the workflow. |

## ManifestDocument Fields

| Field | Required | Type | Child Level | Description |
|---|---:|---|---|---|
| `$schema` | No | string | Top-level scalar | Relative schema reference for editor tooling. Authoritative validation uses the schema bundled with the reusable workflow. |
| `schema_version` | Yes | number | Top-level scalar | Manifest schema version. The current version supports `1`. |
| `entries` | Yes | [`ManifestEntry[]`](types/manifest-entry.md) | Child array | Non-empty list of managed-file mappings. |

Old top-level array manifests are rejected. Wrap the array under `entries` and add `schema_version`.

## ManifestEntry Fields

Each entry is a strict one-to-one mapping and is valid only as an item of `ManifestDocument.entries`.

| Field | Required | Type | Child Level | Description |
|---|---:|---|---|---|
| `source_repo` | Yes | string | Entry scalar | Source repository in `owner/repository` form. |
| `source_ref` | Yes | string | Entry scalar | Source branch, tag, or commit SHA. |
| `source_path` | Yes | [`RepoRelativeFilePath`](types/repo-relative-file-path.md) | Entry scalar | Source file path inside `source_repo`. |
| `target_path` | Yes | [`RepoRelativeFilePath`](types/repo-relative-file-path.md) | Entry scalar | Target file path inside the caller repository. Parent directories are created as needed. |
| `direction` | Yes | [`Direction`](types/direction.md) | Entry scalar | Synchronization direction. The current version executes only `source_to_target`. |
| `lifecycle_policy` | Yes | [`LifecyclePolicy`](types/lifecycle-policy.md) | Entry scalar | Runtime behavior for missing, existing, and changed targets. |
| `uniqueness_policy` | Yes | [`UniquenessPolicy`](types/uniqueness-policy.md) | Entry scalar | Optional repository-wide basename uniqueness enforcement. |
| `managed_scope` | Yes | [`ManagedScope`](types/managed-scope.md) | Entry scalar | Portion of the target file managed by the workflow. |
| `markers` | Conditional | [`Markers`](types/markers.md) | Nested child object | Required for marker-scoped entries and forbidden for `whole_file`. |

## Marker-Scoped Examples

`inside_markers` lets the source own content inside each marker block while the target owns all outside content:

```json
{
  "source_repo": "BionicCode/template-visual-studio-repository",
  "source_ref": "main",
  "source_path": "AGENTS.md",
  "target_path": "AGENTS.md",
  "direction": "source_to_target",
  "lifecycle_policy": "enforce",
  "uniqueness_policy": "none",
  "managed_scope": "inside_markers",
  "markers": {
    "start": "<!-- BEGIN MANAGED SECTION -->",
    "end": "<!-- END MANAGED SECTION -->"
  }
}
```

`outside_markers` lets the source own content outside each marker block while the target owns the inside content:

```json
{
  "source_repo": "BionicCode/template-visual-studio-repository",
  "source_ref": "main",
  "source_path": "AGENTS.md",
  "target_path": "AGENTS.md",
  "direction": "source_to_target",
  "lifecycle_policy": "enforce",
  "uniqueness_policy": "none",
  "managed_scope": "outside_markers",
  "markers": {
    "start": "<!-- BEGIN REPOSITORY SPECIFICS -->",
    "end": "<!-- END REPOSITORY SPECIFICS -->"
  }
}
```

Marker-scoped synchronization is intended for text files. Marker-scoped source and target files are decoded and encoded as strict UTF-8.

## Markers Fields

`Markers` is valid only as the `ManifestEntry.markers` child object.

| Field | Required | Type | Child Level | Description |
|---|---:|---|---|---|
| `start` | Yes | string | Markers scalar | Non-empty marker start delimiter. |
| `end` | Yes | string | Markers scalar | Non-empty marker end delimiter. |

Current marker validation criteria:

- `markers` is required for `managed_scope: "outside_markers"` and `managed_scope: "inside_markers"`.
- `markers` is forbidden for `managed_scope: "whole_file"`.
- `markers.start` and `markers.end` are required non-empty strings.
- `markers.start` and `markers.end` must not be identical.
- Marker matching is exact substring matching.
- Multiple marker blocks and adjacent marker blocks are allowed.
- Nested marker blocks are rejected.
- Source and target marker blocks are matched by occurrence order.
- Existing source and target files must have the same number of marker blocks.

## Mapping Rules

- Each normalized source identity may appear once.
- Each normalized target path may appear once.
- `basename(source_path)` must equal `basename(target_path)`.
- Source and target paths must be repository-relative file paths.
- `target_path` must not be under `_sync-files-from-manifest-workflow/`.

## Authentication

Public source repositories can be read without an extra secret. Private source repositories require the caller workflow to pass the optional `source_token` secret explicitly.
