---
area: GLOBAL
kind: patterns
sources: [conventions, decisions, gh-themes]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# GLOBAL — Patterns

## Applicable conventions

- [column-alignment.md](../../conventions/column-alignment.md)
- [legacy-patterns.md](../../conventions/legacy-patterns.md)
- [naming.md](../../conventions/naming.md)
- [nullable-handling.md](../../conventions/nullable-handling.md)
- [public-api-discipline.md](../../conventions/public-api-discipline.md)

## Recorded decisions affecting this area

- [Remove Nemerle support](../../history/decisions/2012-remove-nemerle.md)
- [Remove DbManager](../../history/decisions/2013-remove-dbmanager.md)
- [Add SAP HANA provider](../../history/decisions/2014-saphana-provider.md)
- [New Merge API (PR #686)](../../history/decisions/2017-new-merge-api.md)
- [Project reorganization](../../history/decisions/2017-project-reorganization.md)
- [Move Merge API to SqlQuery](../../history/decisions/2018-merge-api-to-query.md)
- [Add MySqlConnector provider (PR #1470)](../../history/decisions/2018-mysqlconnector-provider.md)
- [Release 2.0 / .NET Standard support (PR #1155)](../../history/decisions/2018-release-2-0-features.md)
- [Enable Nullable Reference Types project-wide](../../history/decisions/2019-nullable-reference-types.md)
- [Refactoring of ADO.NET providers integration via TypeMapper/TypeWrapper](../../history/decisions/2020-ado-provider-integration-refactor.md)
- [Eager Loading (LoadWith/ThenLoad) via separate detail queries](../../history/decisions/2020-eager-loading.md)
- [InsertWithOutput and DML OUTPUT clause support](../../history/decisions/2020-insert-with-output.md)
- [Query Filters and Associations Refactoring](../../history/decisions/2020-query-filters.md)
- [Replace ADO.NET interfaces with concrete base classes (BREAKING)](../../history/decisions/2021-ado-interface-removal.md)
- [Replace event-hook callbacks with typed Interceptor interfaces](../../history/decisions/2021-interceptor-infrastructure.md)
- [Remove SQL Server 2000 provider](../../history/decisions/2021-sqlserver2000-removal.md)
- [ClickHouse provider addition](../../history/decisions/2022-clickhouse-provider.md)
- [Lock MappingSchema for thread safety](../../history/decisions/2022-lock-mappingschema.md)
- [Remoting refactor and new transport layer](../../history/decisions/2022-remoting-refactor.md)
- [New scaffolding framework and CLI tool (replace T4)](../../history/decisions/2022-scaffold-framework.md)
- [Refactor mapping attribute resolution](../../history/decisions/2023-attributes-refactor.md)
- [DataOptions and options-class refactoring (BREAKING)](../../history/decisions/2023-data-options-refactor.md)
- [Move linq2db.EntityFrameworkCore to main repository](../../history/decisions/2024-efc-repo-merge.md)
- [Refactor provider configuration and versioning](../../history/decisions/2024-provider-versioning.md)
- [LINQ Query parsing v6 refactoring](../../history/decisions/2024-sql-gen-v6-refactor.md)
- [Move AsyncExtensions to separate namespace LinqToDB.Async (BREAKING)](../../history/decisions/2025-async-namespace.md)
- [LinqToDB.Remote.Http.Client/Server transport](../../history/decisions/2025-http-remote-transport.md)
- [Internal APIs cleanup and namespace separation (follow-up)](../../history/decisions/2025-internal-namespace-separation.md)
- [Migrate LINQPad driver to main repository](../../history/decisions/2025-migrate-linqpad-to-repo.md)
- [Reorganize namespaces: move all internals to LinqToDB.Internal.* (BREAKING)](../../history/decisions/2025-namespace-reorg.md)
- [Remove old mappings for members with translations (BREAKING)](../../history/decisions/2025-remove-legacy-member-mappings.md)
- [Remove .NET 6 support (BREAKING)](../../history/decisions/2025-remove-net6.md)
- [UseOptions / UseMappingSchema configuration API](../../history/decisions/2025-useoptions-api.md)
- [Window functions, string.Join, and DistinctBy translation](../../history/decisions/2025-window-functions.md)
- [YDB provider preview](../../history/decisions/2025-ydb-provider.md)

## See also

- [INDEX.md](INDEX.md) — area architecture
- [issues.md](issues.md) — GitHub themes (recurring topics)
- [tech-debt.md](tech-debt.md) — detected debt + cross-area patterns
- [decisions.md](decisions.md) — area + GLOBAL decision cross-references
