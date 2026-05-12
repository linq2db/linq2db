---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Query Filters and Associations Refactoring

## Context
Entity-level filtering (soft-delete flags, tenant discriminators) required manual .Where() calls on every query. Associations that navigated to filtered entities bypassed those conditions. The associations model also had structural issues that caused incorrect LEFT JOIN removal in certain subquery patterns.

## Decision
Commit `0f19c27` (May 23, 2020, FC:127) introduced QueryFilter registration on MappingSchema. Filters are LINQ expressions attached to an entity type; they are automatically applied whenever the entity appears in a query. IgnoreFilters() was added as an escape hatch. The same commit refactored AssociationContext out of TableBuilder, rewrote the GROUP BY + GROUPING SETS/ROLLUP/CUBE path, and moved AssociationHelper to a dedicated class.

## Why
The associations refactoring was needed to support QueryFilter propagation through association navigation. Commit body notes that AssociatedTableContext was removed in the process; queries with LeftJoin removal heuristics were corrected.

## Consequences
- Global filters are stored per-entity in MappingSchema and threaded through ExpressionBuilder.BuildContext.
- Source/LinqToDB/Linq/Builder/AssociationHelper.cs and AssociationContext.cs are the primary files.
- QueryFilters interact with EagerLoading (follow-up fixes in `0a3ed8e`, May 2020).

## Sources
- Commit `0f19c27` -- [Feature] Supporting Query Filters. Associations refactoring (#2198) (Svyatoslav Danyliv, 2020-05-23)
