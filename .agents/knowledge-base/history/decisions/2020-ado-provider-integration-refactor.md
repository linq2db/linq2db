---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Refactoring of ADO.NET providers integration via TypeMapper/TypeWrapper

## Context
Every database provider shipped its own ADO.NET client assembly with non-standard types. Linq2db accessed these via reflection or direct assembly references, making provider code fragile when client libraries updated. The approach blocked .NET Core portability for providers that shipped Windows-only assemblies.

## Decision
Commit `c51269b` (Feb 29, 2020, FC:428) introduced a unified TypeMapper/TypeWrapper abstraction that generates IL-based expression trees at startup to wrap any provider type without a compile-time assembly reference. All major providers (Oracle, DB2, Informix, Sybase, SAP HANA, PostgreSQL, MySQL/MySqlConnector, SQLite MS, SQL Server MS, SQL CE) were migrated in this single commit. The AvoidSpecificDataProviderAPI configuration flag was removed; MiniProfiler test providers were added to validate the wrapping path.

## Why
The migration was prerequisite to using Microsoft.Data.SqlClient (#1929) and Npgsql 5.x without hard-coding assembly versions. The TypeMapper approach adds one IL-emit overhead at startup but eliminates per-query reflection. Commit body cites adding a Benchmarks project and TypeWrapper benchmarks to validate the cost.

## Consequences
- Provider adapters are loaded via DynamicDataProviderBase; assembly references are optional at compile time.
- All provider-specific types (bulk copy, schema, hints) go through generated wrappers.
- New provider additions (ClickHouse 2022, YDB 2025) follow the same TypeMapper pattern.
- Source/LinqToDB/Expressions/TypeMapper.cs and TypeWrapper.cs are the canonical entry points.

## Sources
- Commit `c51269b` -- Refactoring of ADO.NET providers integration code (#1961) (MaceWindu, 2020-02-29)
- Commit `68b3f62` -- add support for Microsoft.Data.SqlClient (#1929) (MaceWindu, 2019-10-15)
