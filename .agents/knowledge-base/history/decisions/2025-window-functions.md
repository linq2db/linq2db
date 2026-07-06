---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Window functions, string.Join, and DistinctBy translation

## Context
SQL window functions (OVER PARTITION BY) had no LINQ API in linq2db. string.Join as an aggregate and DistinctBy (from .NET 6 LINQ) also lacked SQL translations.

## Decision
Commit `9cc8fae` (#5168) added LINQ-based window function APIs (Over(), PartitionBy(), OrderBy(), Rows()/Range()), string.Join translation to GROUP_CONCAT/STRING_AGG/LISTAGG per provider, and DistinctBy translation via DISTINCT ON (PostgreSQL) or a subquery rewrite.

## Why
Window functions are a standard SQL:2003 feature supported by all modern providers. The LINQ API design uses fluent builder syntax that maps directly to the SQL OVER clause structure.

## Consequences
- Source/LinqToDB/Linq/Builder/WindowFunctionBuilder.cs added.
- Sql.Over() is the entry point for window function expressions.
- Provider support varies: SQL Server 2012+, PostgreSQL 8.4+, Oracle 8i+ supported; older providers emit a not-supported exception at runtime.

## Sources
- Commit `9cc8fae` -- Window functions, string.Join and DistinctBy translation (#5168) (MaceWindu, 2025)
