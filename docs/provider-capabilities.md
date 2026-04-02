# LinqToDB Provider Capability Matrix

AI-facing reference: lists SQL feature support per provider as reported by LinqToDB's
`SqlProviderFlags`. Use this table to avoid generating SQL patterns that are unsupported
by the target provider.

Source of truth: each flag is set in the provider's `IDataProvider` implementation.
Version-conditional capabilities are noted with a version qualifier (e.g. `v8.0+`).
Entries marked ❌ will cause a `LinqToDBException` or incorrect SQL at runtime.

---

## Capability Flags by Provider

| Provider | `ProviderName` constant | MERGE | CTE | Window Functions | APPLY / LATERAL | Upsert | OUTPUT / RETURNING |
|---|---|:---:|:---:|:---:|:---:|:---:|:---|
| Access | `ProviderName.Access` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ClickHouse | `ProviderName.ClickHouse` | ❌ | ✅ | ✅ | ❌ | ✅ | ⚠️ limited |
| DB2 | `ProviderName.DB2` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Firebird | `ProviderName.Firebird` | ✅ | ✅ | ✅ v3+ | ✅ v4+ | ❌ | ✅ RETURNING |
| Informix | `ProviderName.Informix` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| MySQL / MariaDB | `ProviderName.MySql` | ❌ | ✅ v8.0+ | ✅ v8.0+ | ✅ v8.0+ | ❌ | ❌ |
| Oracle | `ProviderName.Oracle` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ RETURNING INTO |
| PostgreSQL | `ProviderName.PostgreSQL` | ✅ v15+ | ✅ | ✅ | ✅ v9.3+ | ✅ v9.5+ | ✅ RETURNING |
| SAP HANA | `ProviderName.SapHana` | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| SQL Server CE | `ProviderName.SqlCe` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| SQLite | `ProviderName.SQLite` | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ RETURNING v3.35+ |
| SQL Server | `ProviderName.SqlServer` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ OUTPUT |
| Sybase | `ProviderName.Sybase` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| YDB | `ProviderName.Ydb` | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ RETURNING |

---

## Column Definitions

**MERGE**
SQL MERGE statement (combined INSERT/UPDATE/DELETE in a single DML).
Exposed as `LinqExtensions.Merge()` → fluent builder → `Merge()` terminal.
Providers that do not support it throw `LinqToDBException` at query generation time.

**CTE**
`WITH` clause (Common Table Expressions), including non-recursive CTEs.
Exposed as `LinqExtensions.AsCte()`. Recursive CTEs require CTE support and are
further constrained by `IsRecursiveCTEJoinWithConditionSupported`.

**Window Functions**
`OVER (PARTITION BY ... ORDER BY ...)` analytical functions.
Exposed via `Sql.Ext` window function helpers.

**APPLY / LATERAL**
`CROSS APPLY` / `OUTER APPLY` (SQL Server, Oracle, SAP HANA) or
`LATERAL JOIN` (PostgreSQL v9.3+, MySQL v8.0+, Firebird v4+).
Used internally by LinqToDB for correlated subqueries and `LoadWith`.

**Upsert**
`INSERT OR UPDATE` semantics (single statement that inserts or updates).
Exposed as `DataExtensions.InsertOrUpdate()` / `InsertOrReplace()`.
Note: MySQL `INSERT ... ON DUPLICATE KEY UPDATE` is not mapped to this flag —
use `InsertOrReplace()` with MySQL instead.

**OUTPUT / RETURNING**
Ability to return column values from INSERT / UPDATE / DELETE statements.
Exposed as `LinqExtensions.Output()` / `OutputInto()`.
SQL Server emits `OUTPUT` into a table variable; other providers use `RETURNING`.
The exact syntax and supported DML operations vary by provider.

---

## Notes

- **MariaDB**: shares the `MySql` version flags; MariaDB has added some features
  earlier than MySQL (e.g. window functions since MariaDB 10.2, CTEs since 10.2).
  LinqToDB uses the same flags for both — check your actual server version.

- **PostgreSQL MERGE**: requires PostgreSQL 15 or later. The `MERGE` statement was
  standardised and added to PostgreSQL in version 15. Earlier versions will fail at
  the database level even though LinqToDB does not block generation.

- **ClickHouse**: does not support SQL transactions in the ACID sense;
  `TransactionScope` and `BeginTransaction` may silently succeed or have no effect
  depending on the adapter and ClickHouse engine.

- **Version-conditional flags**: LinqToDB detects the server version at provider
  initialisation time and sets `SqlProviderFlags` accordingly.
  The version you pass to `DataOptions.UseXxx(...)` determines which flags are active.

---

## See also

- `LinqToDB.LinqToDBArchitecture` — architecture overview (XML documentation class, namespace `LinqToDB`).
- [`docs/architecture.md`](architecture.md) — extended architectural model.
- [`docs/ai-tags.md`](ai-tags.md) — machine-readable metadata specification.
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — common mistakes and how to avoid them.
