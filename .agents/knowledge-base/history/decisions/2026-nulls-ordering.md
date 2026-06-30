---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd242e6399daa081ca
---

# NULLS FIRST/LAST ordering support

## Context

SQL providers differ on where NULL values sort relative to non-null values in ORDER BY: PostgreSQL, Oracle, and DB2 sort NULLs last ascending (largest), while SQL Server, MySQL, and SQLite sort NULLs first ascending (smallest). LINQ OrderBy has no mechanism to control this, leaving callers unable to write portable null-ordering queries. Issue #2068 tracked the gap.

## Decision

Sql.NullsPosition enum and new OrderBy/OrderByDescending/ThenBy/ThenByDescending overloads accepting it were added (commit 2f99880, Jun 5, #5561). SqlOrderByItem carries the position; providers that support native NULLS FIRST/LAST syntax (PostgreSQL, Oracle, DB2, Firebird, SQLite, DuckDB, ClickHouse, SAP HANA) render it directly via IsNullsOrderingSupported (moved from a SqlBuilder virtual to SqlProviderFlags so the optimizer can see it). Non-native providers emulate via a leading CASE WHEN expr IS NULL THEN 0/1 END key, lowered by SqlNullsOrderingLoweringVisitor before optimization. DataOptions.SqlOptions.DefaultNullsPosition was added for a connection-level default, and DefaultNullsOrdering records each provider natural null sort order so redundant NULLS tokens are elided.

## Why

Native rendering avoids extra CASE key overhead on capable providers. Moving the flag to SqlProviderFlags makes it visible to SqlNullsOrderingLoweringVisitor, which lowers emulated positions into CASE keys before optimization so DISTINCT, set-operation, and ROW_NUMBER wrapping passes see them as ordinary order expressions. NULLS position is also integrated into aggregate ORDER BY (String.Join), DistinctBy, UnionBy/ExceptBy/IntersectBy, and MinBy/MaxBy.

## Consequences

- New public API: Sql.NullsPosition, SqlOptions.DefaultNullsPosition, SqlOrderByItem.NullsPosition, new OrderBy/ThenBy overloads.
- SqlProviderFlags.IsNullsOrderingSupported replaces the same virtual on BasicSqlBuilder.
- Comparer-based OrderBy(keySelector, IComparer) overloads now throw instead of silently ignoring the comparer.
- Non-nullable keys skip the emulation CASE.

## Sources

- Commit 2f99880 -- Add NULLS FIRST/LAST support to OrderBy/ThenBy (#5561) (Svyatoslav Danyliv, 2026-06-05)
