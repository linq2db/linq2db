---
area: METADATA
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove old MappingSchema

## Context
linq2db went through a MappingSchema redesign. The old schema type was kept in parallel with the new implementation during a transition period. Once the new schema was stable, the old type became dead weight.

## Decision
The old MappingSchema type and its dependents were removed. Subject: remove old MappingSchema; 33 files changed, 272 insertions, 4036 deletions -- a large net removal.

## Why
No explicit rationale in the commit body. The pattern of keeping an old and new type in parallel then removing the old one after stabilization is a common staged migration.

## Consequences
- Code that referenced the old MappingSchema type had to be updated to the new API.
- Reduced the metadata layer to a single canonical schema type.

## Sources
- Commit `3cc3ccb` -- remove old MappingSchema (MaceWindu, 2013-03-23)
