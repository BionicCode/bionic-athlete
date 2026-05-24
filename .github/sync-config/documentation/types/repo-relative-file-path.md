# RepoRelativeFilePath

Repository-relative file path string.

## Placement

| Property | Value |
|---|---|
| Placement | Scalar string |
| Valid parent | [`ManifestEntry`](manifest-entry.md) |
| Parent properties | `source_path`, `target_path` |

## Examples

Allowed:

```text
.editorconfig
Directory.Build.props
AGENTS.md
src/AGENTS.md
.github/copilot-instructions.md
.github/instructions/test.instructions.md
```

Rejected:

```text
/AGENTS.md
../AGENTS.md
src/../AGENTS.md
src/
src//AGENTS.md
_sync-files-from-manifest-workflow/generated.md
```

## Rules

- Must use forward slashes.
- Must be relative to the repository root.
- Must point to a file-like path, not a directory.
- Must not contain empty path segments.
- Must not contain `.` or `..` path segments.
- `target_path` must not be under `_sync-files-from-manifest-workflow/`, which is reserved workflow scratch space.
- Existing target paths and target parent directories must not be symlinks.
