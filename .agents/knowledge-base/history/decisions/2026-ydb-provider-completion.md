---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# YDB provider: schema API, CLI/T4/LINQPad scaffolding, main CI matrix

## Context

The YDB provider shipped as a preview in 2025 (commit `91954d9`, #5218) with `GetSchemaProvider()` unimplemented and no CLI/T4/LINQPad scaffolding, excluded from the main CI test matrix "until it exits preview" per the original decision.

## Decision

PR #5564 (commit `c11b104`, 219 files, 5631 insertions/781 deletions) closed those gaps: a `YdbSchemaProvider` (tables/columns via `DbConnection.GetSchema` -- YDB has no SQL-queryable catalog -- primary keys via a typed `[Wrapper]` over the SDK's internal `YdbSchema.DescribeTable`, batched once per connection; YDB has no FKs or stored procedures); CLI scaffold support (`DatabaseType.Ydb`) with generated fixtures across the Default/All/Fluent/NoMetadata/T4 templates; a LINQPad driver (netcore-only, with sync-over-async deadlock avoidance for `DbConnection.Open()`/`DescribeTable` on LINQPad's UI `SynchronizationContext`); an Azure CI pipeline (docker-based `ydbplatform/local-ydb`, gated on `[ydb.all]`/`[all]`) bringing YDB into the same test-matrix mechanism as the other providers; and a SQL-generation fix hoisting non-correlated IN/EXISTS/set-operator subqueries into named-variable CTEs (`$cte = (...)`) since YDB rejects an inline subquery in those operand positions. The SDK dependency was bumped 0.31.0 -> 0.32.0 (removes legacy `TableClient`/`SchemeClient`, unused by the linq2db adapter; adds `YdbDataSource.DescribeTable`).

## Why

`GetSchema`-based introspection was necessary because YDB exposes no SQL-queryable system catalog the way other providers' schema providers assume. The CTE-hoisting fix was necessary groundwork for LINQ query shapes (correlated aggregates, `Contains` over a subquery) that previously reached YDB as an inline subquery and failed at the server; it complements, rather than duplicates, the separately-recorded correlated-subquery validation decision, which rejects subqueries YDB cannot execute at all rather than rewriting them.

## Consequences

- `YdbDataProvider.GetSchemaProvider()` now returns a working provider; `Internal/DataProvider/Ydb/YdbSchemaProvider.cs` is new.
- YDB is scaffoldable via the `dotnet-linq2db` CLI and usable as a LINQPad driver.
- YDB runs in the main Azure CI test matrix (`[ydb.all]`/`[all]` gate) rather than being excluded pending preview exit.
- `TestProvName.AllYdb` introduced and adopted across ~95 test-attribute usages (mirrors `AllDuckDB`/`AllClickHouse`), while `ProviderName.Ydb` is kept where the literal provider name is semantically required (mapping `Configuration=`, per-provider exception assertions).
- `TypeMapper`'s `[WrappedBindingFlags]` attribute extended to methods (was constructor-only) to let `[Wrapper]` bind the SDK's internal `DescribeTable`.

## Sources

- Commit `c11b104` -- Finish the YDB provider: schema API, NuGet bump, test enablement (#5564) (MaceWindu, 2026-06-15)
- PR #5564
- File anchors: `Source/LinqToDB/Internal/DataProvider/Ydb/YdbSchemaProvider.cs`, `Source/LinqToDB.LINQPad/DatabaseProviders/` (YDB driver), `Build/Azure/scripts/ydb.sh`
