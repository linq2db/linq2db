---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Reorganize namespaces: move all internals to LinqToDB.Internal.* (BREAKING)

## Context
Internal types in linq2db used the same LinqToDB root namespace as public API. This made it impossible to distinguish public API surface from implementation details at the namespace level, and complicated API compatibility tooling.

## Decision
Commit `639d598` (2025, #4754) moved all internal types to LinqToDB.Internal.* namespaces. Public API types remained in their existing namespaces. The migration was accompanied by [InternalsVisibleTo] cleanup and an initial API baseline (`56edce9`, #4369) to track the surface going forward.

## Why
The separation enables API compatibility tools (Microsoft.DotNet.ApiCompat) to gate on the public surface only. Commit message explicitly flags this as a BREAKING change. The LinqToDB.Internal namespace prefix signals to consumers not to depend on those types.

## Consequences
- Any code that referenced LinqToDB.Linq.Builder.*, LinqToDB.SqlQuery.* (internal AST types), or similar paths received compile errors after upgrade.
- API baseline files were added at Source/*/CompatibilitySuppressions.xml.
- Follow-up `ea0ad40` (#5027) continued the internal-API cleanup and namespace separation.

## Sources
- Commit `639d598` -- Reorganize Namespaces (#4754) (MaceWindu, 2025)
- Commit `56edce9` -- Add API baselines infrastructure (#4369) (MaceWindu, 2024)
- Commit `ea0ad40` -- Internal APIs cleanup / namespace separation (#5027) (MaceWindu, 2025)
