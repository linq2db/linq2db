---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# InsertWithOutput and DML OUTPUT clause support

## Context
SQL Server's OUTPUT clause and PostgreSQL's RETURNING allow DML statements to return affected rows without a separate SELECT. Linq2db had no LINQ API for this; callers had to issue a second query to retrieve inserted/updated rows.

## Decision
Commit `c54d259` (Mar 25, 2020, FC:39) added InsertWithOutput and InsertWithOutputInto for SQL Server (OUTPUT) and PostgreSQL (RETURNING). Async overloads, remote context serialization support, and SQL AST nodes (SqlOutputClause) were added in the same PR. The work was later extended to UpdateWithOutput in `4df7c6a` (Jun 2021) and to a general RETURNING statement in 2022.

## Why
Commit body describes the design as materializing the OUTPUT result into a typed projection using SqlOutputClause on the AST, with visitors and serializer support added in parallel. Provider-specific SQL building uses the same BasicSqlBuilder extension point as INSERT/UPDATE.

## Consequences
- SqlOutputClause was added to Source/LinqToDB/SqlQuery/.
- LinqExtensions.Insert.cs carries the public InsertWithOutput* overloads.
- The 2022 RETURNING feature generalized this to DELETE and UPDATE for PostgreSQL/Firebird.

## Sources
- Commit `c54d259` -- InsertWithOutput, InsertWithOutputInto for MSSQL (#1703) (Svyatoslav Danyliv, 2020-03-25)
- Commit `4df7c6a` -- UpdateWithOutput() support (#2321) (Stuart Turner, 2021-06-03)
