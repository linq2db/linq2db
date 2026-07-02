---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# New Merge API (PR #686)

## Context
linq2db MERGE/UPSERT support was limited and provider-specific. Users needed a unified, fluent API that could express the full MERGE statement semantics (matched, not-matched, not-matched-by-source clauses) portably across providers that supported it.

## Decision
A new Merge API was introduced via PR #686. Commit subject: New merge api (#686); 105 files changed, 11098 insertions, 1563 deletions. This is the largest single-commit change in the era by file count and insertion count.

## Why
The commit body references PR #686 but provides no inline rationale. The scale (105 files, 11000+ insertions) and the explicit API label indicate a greenfield public API addition.

## Consequences
- New public fluent Merge API surface added to the library.
- Provider-specific MERGE SQL generation added or extended for all supporting providers.
- Test coverage introduced across 105 files including new MergeTests suites.
- Established the foundation that the 2018 merge-to-query migration later built on.

## Sources
- Commit `1af71f1` -- New merge api (#686) (MaceWindu, 2017-10-27)
- PR #686
- File anchors: `Source/LinqToDB/DataProvider/`, `Tests/Linq/Update/MergeTests*.cs`
