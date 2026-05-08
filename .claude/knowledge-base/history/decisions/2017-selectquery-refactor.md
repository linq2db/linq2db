---
area: SQL-AST
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# SelectQuery major refactor

## Context
The SelectQuery type was the central SQL AST node representing a SELECT statement. Over time it had accumulated responsibilities beyond its original scope: sub-query handling, set operations, and clause management were entangled in a single monolithic type. This made it difficult to correctly handle complex queries such as nested sub-selects, unions, and CTEs.

## Decision
SelectQuery was refactored across a series of commits. The work touched the core AST type, all SQL builders, all providers, and the expression translation layer. Four commits form this change:
- `053bf5b` -- SelectQuery refactor (80 files, 5000+ insertions)
- `8f19e14` -- SelectQuery refactor (65 files, 4000+ lines moved)
- `db749fc` -- SelectQuery refactor (35 files, continued restructuring)
- `f9d4492` -- SelectQuery refactor (21 files, finalizing)

## Why
No detailed rationale in commit bodies. The breadth (all providers, all SQL builders, expression translator) and the multi-commit span indicate this was a planned, load-bearing architectural change rather than an opportunistic cleanup.

## Consequences
- All SQL builders and providers required updates to produce and consume the new AST shape.
- Sub-query and set-operation handling centralized within a cleaner type hierarchy.
- Expression translation paths that built SelectQuery nodes updated to match new API.
- The refactor is the largest single architectural change to the SQL-AST area in the 2011-2018 period.

## Sources
- Commit `053bf5b` -- SelectQuery refactor (MaceWindu, 2017-09-10)
- Commit `8f19e14` -- SelectQuery refactor (MaceWindu, 2017-09-11)
- Commit `db749fc` -- SelectQuery refactor (MaceWindu, 2017-09-14)
- Commit `f9d4492` -- SelectQuery refactor (MaceWindu, 2017-09-15)
- File anchors: `Source/LinqToDB/SqlQuery/SelectQuery.cs`, `Source/LinqToDB/SqlProvider/BasicSqlBuilder.cs`
