---
area: GLOBAL
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove DbManager

## Context
DbManager was an older, higher-level wrapper API present in early linq2db releases. It predated the current DataConnection abstraction and carried legacy patterns that conflicted with the library design direction.

## Decision
DbManager was removed from the codebase. The commit subject is remove DbManager; 28 files were changed with 570 insertions and 3025 deletions, showing a net deletion of legacy code.

## Why
No rationale is in the commit body. The net-negative line count (-2455 lines) and scope across 28 files indicate this was a deliberate public-API removal, not an incremental deprecation.

## Consequences
- Callers depending on DbManager required migration to DataConnection or equivalent.
- Removed a parallel session-management code path, reducing maintenance surface.

## Sources
- Commit `bc0fbcd` -- remove DbManager (MaceWindu, 2013-01-28)
