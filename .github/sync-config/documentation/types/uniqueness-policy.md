# UniquenessPolicy

Declares whether repository-wide basename uniqueness is enforced for an entry.

## Placement

| Property | Value |
|---|---|
| Placement | Enum string |
| Valid parent | [`ManifestEntry`](manifest-entry.md) |
| Parent property | `uniqueness_policy` |

## Values

| Value | Supported | Description |
|---|---:|---|
| `basename_unique` | Yes | Fail validation when any tracked repository file outside the declared target path shares the target basename. |
| `none` | Yes | Do not scan the repository for same-basename files. |

## Notes

- `basename_unique` does not delete extra files.
- `basename_unique` is opt-in per entry.
- Same-basename files are allowed when entries use `uniqueness_policy: none`, provided target paths and source identities remain unique.
