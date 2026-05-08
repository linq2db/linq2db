---
area: METADATA
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Dynamic columns support (PR #1073)

## Context
linq2db entities required all mapped columns to be statically declared on the entity type. Some database patterns -- particularly document-store hybrid schemas and EAV tables -- store data in variable columns that cannot be enumerated at compile time. Users requested a way to map these extra columns without declaring them as explicit properties.

## Decision
Dynamic column support was added via PR #1073. Commit subject: Dynamic columns (#1073); 63 files changed, 4540 insertions, 486 deletions. The feature introduces a mechanism to capture unmapped columns into a dictionary-like member on the entity.

## Why
The commit body references PR #1073 but provides no inline rationale. The feature directly addresses the compile-time constraint that prevented linq2db from being used with schema-flexible tables.

## Consequences
- New [DynamicColumnsStore] attribute (or equivalent) added to the public mapping API.
- MappingSchema extended to detect and route dynamic column members.
- Query builder updated to read/write dynamic columns via the dictionary member.
- Enabled use of linq2db with EAV and document-hybrid schemas that were previously unsupported.

## Sources
- Commit `92cff08` -- Dynamic columns (#1073) (MaceWindu, 2018-07-25)
- PR #1073
- File anchors: `Source/LinqToDB/Mapping/`, `Tests/Linq/Linq/DynamicColumnsTests.cs`
