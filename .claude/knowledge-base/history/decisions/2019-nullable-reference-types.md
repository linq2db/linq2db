---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Enable Nullable Reference Types project-wide

## Context
The codebase accumulated a large number of implicit null-reference patterns across the core library, providers, and tools. C# 8.0 introduced nullable reference types (NRT) as a compiler feature. The v3.0 migration window in mid-2019 provided an opportunity to annotate the full codebase before the public API expanded further.

## Decision
Commit `86541aa` (Aug 10, 2019, FC:524) enabled nullable reference types across the entire solution by setting Nullable enable in the project props. Existing code was annotated in the same pass; the T4 WPF test project was merged into the main T4 test project at the same time.

## Why
The change was timed with the v3.0 branch refactoring (`bd21cf6`, Aug 6, 2019) that migrated to VS2019 and C# 8. Enabling NRT at the start of the 3.x line rather than retrofitting it later reduced the annotation debt. Commit body states revert equality comparers nullability changes as the only carve-out, indicating pragmatic handling of generic comparer constraints.

## Consequences
- All public API members carry explicit nullability annotations from the 3.x line onward.
- New contributors see compiler warnings on incorrect null handling from first build.
- Follow-up passes (`023d540`, Mar 2020, FC:161) completed remaining annotations in linq2db.Tools.

## Sources
- Commit `86541aa` -- Enable nullable reference types (#1858) (MaceWindu, 2019-08-10)
- Commit `bd21cf6` -- Version 3.0 refactorings (#1819) (MaceWindu, 2019-08-06)
- Commit `023d540` -- add remaining NRT annotations (MaceWindu, 2020-03-29)
