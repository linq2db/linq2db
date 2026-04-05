# LinqToDB Provider Capability Matrix

> **Required:** Read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md) before any implementation. This file contains global rules, required namespaces, architecture constraints, and documentation navigation.

> You are here if you need to:
> - verify whether a specific SQL feature (MERGE, CTE, bulk copy, OUTPUT/RETURNING, upsert) is supported by the target provider
> - avoid generating SQL patterns that will fail or behave incorrectly on a given database

AI-facing reference: lists SQL feature support per provider.
Use this table to avoid generating SQL patterns that are unsupported by the target provider.

Version-conditional capabilities are noted with a version qualifier (e.g. `v8.0+`).
Entries marked ❌ will cause a `LinqToDBException` or incorrect SQL at runtime.

**Note on data source:** Capabilities are sourced from `SqlProviderFlags` and provider builders.
The **Upsert** column reflects `SqlProviderFlags.IsInsertOrUpdateSupported`. 
See [Notes](#notes) section below for exceptions where the flag is true but the feature is not practically supported (ClickHouse, YDB).

---

## Capability Flags by Provider

| Provider | `ProviderName` constant | MERGE | CTE | Window Functions | APPLY / LATERAL | Upsert | OUTPUT / RETURNING | Bulk Copy |
|---|---|:---:|:---:|:---:|:---:|:---:|:---|:---|
| Access | `ProviderName.Access` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ClickHouse | `ProviderName.ClickHouse` | ❌ | ✅ | ✅ | ❌ | ❌ | ⚠️ limited | ✅ native |
| DB2 | `ProviderName.DB2` | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ | ⚠️ opt-in |
| Firebird | `ProviderName.Firebird` | ✅ | ✅ | ✅ v3+ | ✅ v4+ | ✅ | ✅ RETURNING | ❌ |
| Informix | `ProviderName.Informix` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ native |
| MySQL / MariaDB | `ProviderName.MySql` | ❌ | ✅ v8.0+ | ✅ v8.0+ | ✅ v8.0+ | ✅ | ❌ | ⚠️ opt-in |
| Oracle | `ProviderName.Oracle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ RETURNING INTO | ⚠️ opt-in |
| PostgreSQL | `ProviderName.PostgreSQL` | ✅ v15+ | ✅ | ✅ | ✅ v9.3+ | ✅ v9.5+ | ✅ RETURNING | ⚠️ opt-in |
| SAP HANA | `ProviderName.SapHana` | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ⚠️ opt-in |
| SQL Server CE | `ProviderName.SqlCe` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| SQLite | `ProviderName.SQLite` | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ RETURNING v3.35+ | ❌ |
| SQL Server | `ProviderName.SqlServer` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ OUTPUT | ✅ native |
| Sybase | `ProviderName.Sybase` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ⚠️ opt-in |
| YDB | `ProviderName.Ydb` | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ RETURNING | ✅ native |

---

## Column Definitions

**MERGE**
SQL MERGE statement (combined INSERT/UPDATE/DELETE in a single DML).
Exposed as `LinqExtensions.Merge()` → fluent builder → `Merge()` terminal.
Providers that do not support it throw `LinqToDBException` at query generation time.

**CTE**
`WITH` clause (Common Table Expressions), including non-recursive CTEs.
Exposed as `LinqExtensions.AsCte()`. Recursive CTEs are supported on a subset of providers;
on providers that support recursive CTEs some may have additional restrictions on using
JOIN conditions inside the recursive part — check the matrix row for your provider.

**Window Functions**
`OVER (PARTITION BY ... ORDER BY ...)` analytical functions.
Exposed via `Sql.Ext` window function helpers.

**APPLY / LATERAL**
`CROSS APPLY` / `OUTER APPLY` (SQL Server, Oracle, SAP HANA) or
`LATERAL JOIN` (PostgreSQL v9.3+, MySQL v8.0+, Firebird v4+).
Used internally by LinqToDB for correlated subqueries and `LoadWith`.

**Upsert**
`INSERT OR UPDATE` semantics (single statement that inserts or updates).
Each provider translates the operation to its native syntax (e.g., `ON DUPLICATE KEY UPDATE` for MySQL,
`ON CONFLICT DO UPDATE` for PostgreSQL/SQLite, `MERGE` for SQL Server/DB2/Oracle/Firebird).
Exposed as `DataExtensions.InsertOrUpdate()` / `InsertOrReplace()`.

**OUTPUT / RETURNING**
Ability to return column values from INSERT / UPDATE / DELETE statements.
Exposed as `LinqExtensions.Output()` / `OutputInto()`.
SQL Server emits `OUTPUT` into a table variable; other providers use `RETURNING`.
The exact syntax and supported DML operations vary by provider.

**Bulk Copy**
Native provider-level bulk insert, bypassing row-by-row INSERT overhead.
Exposed as `DataContextExtensions.BulkCopy()` / `BulkCopyAsync()` with `BulkCopyOptions.BulkCopyType`.
`✅ native` — provider uses a native driver API by default (`BulkCopyType.ProviderSpecific` is the default).
`⚠️ opt-in` — native bulk copy is available but requires explicitly setting `BulkCopyType.ProviderSpecific`;
  the default is `BulkCopyType.MultipleRows` (multi-row INSERT batches).
`❌` — no native bulk copy; only `MultipleRows` (multi-row INSERT) or `RowByRow` modes are available.

---

## Notes

**Upsert Limitations**

The `SqlProviderFlags.IsInsertOrUpdateSupported` flag may be `true` for providers where the actual implementation is not supported at runtime:

- **ClickHouse Upsert**: `InsertOrUpdate()` / `InsertOrReplace()` are not supported and will throw
  `LinqToDBException` at query build time — ClickHouse cannot provide the row-count feedback
  required for correct upsert emulation. Use provider-specific alternatives instead.

- **YDB Upsert**: `InsertOrUpdate()` / `InsertOrReplace()` are not implemented for YDB and will
  throw `LinqToDBException` at runtime. Do not call these methods against YDB.

---

Other Notes

- **MariaDB**: shares the `MySql` version flags; MariaDB has added some features
  earlier than MySQL (e.g. window functions since MariaDB 10.2, CTEs since 10.2).
  LinqToDB uses the same flags for both — check your actual server version.

- **PostgreSQL MERGE**: requires PostgreSQL 15 or later. The `MERGE` statement was
  standardised and added to PostgreSQL in version 15. Earlier versions will fail at
  the database level even though LinqToDB does not block generation.

- **ClickHouse**: does not support SQL transactions in the ACID sense;
  `TransactionScope` and `BeginTransaction` may silently succeed or have no effect
  depending on the adapter and ClickHouse engine.

- **Sybase Bulk Copy**: `BulkCopyType.ProviderSpecific` is available but the default is `MultipleRows`
  because the native Sybase bulk copy API has known issues with `bit` columns and identity fields.
  Set `BulkCopyType.ProviderSpecific` explicitly only if your table does not use those column types.

- **Oracle Bulk Copy**: `BulkCopyType.ProviderSpecific` (ODP.NET `OracleBulkCopy`) falls back to
  `MultipleRows` when column names require SQL identifier escaping — a known ODP.NET limitation.

- **Informix Bulk Copy**: `BulkCopyType.ProviderSpecific` is the default and uses the IDS native
  bulk copy API or the DB2 bulk copy API depending on the adapter in use.
  Falls back to `MultipleRows` if neither adapter is available at runtime.

- **Version-conditional capabilities**: LinqToDB detects the server version at provider
  initialisation time and adjusts feature support accordingly.
  The version you pass to `DataOptions.UseXxx(...)` determines which features are active.

---

## See also

- `LinqToDB.LinqToDBArchitecture` — architecture overview (XML documentation class, namespace `LinqToDB`).
- [`docs/architecture.md`](architecture.md) — extended architectural model.
- [`docs/ai-tags.md`](ai-tags.md) — machine-readable metadata specification.
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — common mistakes and how to avoid them.
- [`docs/provider-setup.md`](provider-setup.md) — provider configuration reference (ProviderName constants, UseXxx methods, NuGet packages).
