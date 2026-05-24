# ManifestEntry

One strict source-to-target managed-file mapping.

## Shape

```json
{
  "source_repo": "BionicCode/template-visual-studio-repository",
  "source_ref": "main",
  "source_path": "AGENTS.md",
  "target_path": "AGENTS.md",
  "direction": "source_to_target",
  "lifecycle_policy": "enforce",
  "uniqueness_policy": "none",
  "managed_scope": "whole_file"
}
```

Marker-scoped entries include the conditional `markers` child object:

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

## Placement

| Property | Value |
|---|---|
| Placement | Array item object |
| Valid parent | [`ManifestDocument`](manifest-document.md) |
| Parent property | `entries[]` |

## Attributes

| Attribute | Required | Type | Description |
|---|---:|---|---|
| `source_repo` | Yes | string | Source repository in `owner/repository` form. |
| `source_ref` | Yes | string | Source branch, tag, or commit SHA. |
| `source_path` | Yes | [`RepoRelativeFilePath`](repo-relative-file-path.md) | Source file path inside the source repository. |
| `target_path` | Yes | [`RepoRelativeFilePath`](repo-relative-file-path.md) | Target file path inside the caller repository. |
| `direction` | Yes | [`Direction`](direction.md) | Synchronization direction. |
| `lifecycle_policy` | Yes | [`LifecyclePolicy`](lifecycle-policy.md) | Missing/changed/existing target behavior. |
| `uniqueness_policy` | Yes | [`UniquenessPolicy`](uniqueness-policy.md) | Optional basename uniqueness policy. |
| `managed_scope` | Yes | [`ManagedScope`](managed-scope.md) | Portion of the target file managed by the workflow. |
| `markers` | Conditional | [`Markers`](markers.md) | Required for marker scopes and forbidden for `whole_file`. |

## Child Values

| Child Property | Child Type | Description |
|---|---|---|
| `markers` | [`Markers`](markers.md) | Conditional marker delimiter object for marker-scoped entries. |

## Validation Rules

- A normalized source identity is `source_repo + source_ref + source_path`.
- A normalized source identity may appear only once.
- A normalized `target_path` may appear only once.
- `source_path` and `target_path` must have the same basename.
- No implicit rename semantics are supported.
- Unknown entry properties are rejected.
