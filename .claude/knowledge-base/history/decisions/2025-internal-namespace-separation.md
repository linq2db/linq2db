---
area: GLOBAL
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Internal APIs cleanup and namespace separation (follow-up)

## Context
After the 2025 namespace reorganization (`639d598`), some internal types remained in public-facing namespaces and several [InternalsVisibleTo] entries were stale. The API baseline infrastructure flagged these gaps.

## Decision
Commit `ea0ad40` (#5027) continued the internal-API separation: additional types were relocated to LinqToDB.Internal.*, stale [InternalsVisibleTo] entries were removed, and the API compatibility baseline was updated.

## Why
Incremental follow-up to the main namespace reorganization. The API baseline tooling made it possible to identify remaining gaps systematically.

## Consequences
- Further reduction of accidental public surface.
- [InternalsVisibleTo] list in LinqToDB.csproj was shortened.

## Sources
- Commit `ea0ad40` -- Internal APIs cleanup / namespace separation (#5027) (MaceWindu, 2025)
