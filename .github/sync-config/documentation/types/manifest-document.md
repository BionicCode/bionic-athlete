# ManifestDocument

Top-level JSON object consumed by the reusable sync workflow.

## Shape

```json
{
  "$schema": "./sync-manifest.schema.json",
  "schema_version": 1,
  "entries": [
    {
      "source_repo": "BionicCode/template-visual-studio-repository",
      "source_ref": "main",
      "source_path": "README.md",
      "target_path": "README.md",
      "direction": "source_to_target",
      "lifecycle_policy": "seed_once",
      "uniqueness_policy": "none",
      "managed_scope": "whole_file"
    }
  ]
}
```

## Placement

| Property | Value |
|---|---|
| Placement | Top-level object |
| Valid parent | None |
| Parent property | None |

## Attributes

| Attribute | Required | Type | Description |
|---|---:|---|---|
| `$schema` | No | string | Schema path used by editors and local tooling. It does not decide the authoritative runtime schema. |
| `schema_version` | Yes | number | Manifest schema version. The current version supports only `1`. |
| `entries` | Yes | [`ManifestEntry[]`](manifest-entry.md) | Non-empty list of strict source-to-target mappings. |

## Child Values

| Child Property | Child Type | Description |
|---|---|---|
| `entries[]` | [`ManifestEntry`](manifest-entry.md) | One managed-file mapping. |

## Notes

- The manifest must be an object, not an array.
- Unknown top-level properties are rejected.
- Runtime validation uses the schema bundled with the reusable workflow version being executed.
