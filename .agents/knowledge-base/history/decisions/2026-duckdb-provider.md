---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
---

# DuckDB data provider added

## Context

DuckDB is an in-process analytical database. Users requested LINQ to DB support so they could query DuckDB databases with the same LINQ API as the relational providers. The adapter library is `DuckDB.NET.Data` (plus `DuckDB.NET.Bindings` for native types); linq2db loads both dynamically at runtime via its standard `DynamicDataProviderBase<T>` pattern, the same approach used for Oracle ODP and Ydb.

## Decision

Commit `3537e02c6` (PR #5451, 2026-05-10) added a full DuckDB provider under `Source/LinqToDB/DataProvider/DuckDB/` (public surface) and `Source/LinqToDB/Internal/DataProvider/DuckDB/` (implementation). Key design choices:

- **Identity via explicit sequences.** DuckDB does not allow `GENERATED AS IDENTITY` combined with a `PRIMARY KEY` constraint in the same column definition. The builder (`DuckDBSqlBuilder`) instead emits `CREATE SEQUENCE IF NOT EXISTS <table>_<field>_seq START 1` before the table DDL and sets `DEFAULT nextval(...)` on the identity column. Schema detection treats any column whose `column_default` contains `nextval` as an identity column.
- **Bulk copy via Appender.** The default `BulkCopyType` is `ProviderSpecific`, which routes through `DuckDBBulkCopy.ProviderSpecificCopyImpl` using DuckDB's native Appender API (synchronous, in-process). The Appender requires values for every table column in order; when unmapped columns or identity-with-nextval defaults are present the implementation falls back to `MultipleRows` (multi-row `INSERT`) rather than using `AppendDefault`, because `AppendDefault` does not invoke sequence expressions.
- **Truncate-with-identity-reset workaround.** `ALTER SEQUENCE RESTART` is not implemented in DuckDB and `DROP SEQUENCE` is blocked while the table exists. The truncate path creates a new `_reset` sequence and switches the column `DEFAULT` to it; the old sequence becomes orphaned and is cleaned up when the table is dropped.
- **Type mapping.** BLOB columns arrive as `UnmanagedMemoryStream`; reader expressions convert to `byte[]`. `TIMESTAMPTZ` columns arrive as `DateTime(Kind=Utc)` and are mapped to `DateTimeOffset` via `GetFieldValue<DateTimeOffset>`. `TIMETZ` is handled through `DateTimeOffset` roundtrip. DuckDB-native types (`DuckDBDateOnly`, `DuckDBTimeOnly`, `DuckDBTimestamp`, `DuckDBInterval`) are registered with the type mapper.
- **SqlProviderFlags.** Full CTE, UNION ALL ORDER BY, all set operations, INSERT OR UPDATE (via `ON CONFLICT`), APPLY joins, DISTINCT FROM, and predicate comparisons are all enabled. Default multi-query isolation is Snapshot.

## Why

DuckDB's `GENERATED AS IDENTITY` and `PRIMARY KEY` cannot coexist in a single column definition; the sequence-based workaround is the only available approach in the current DuckDB SQL dialect. The Appender path was chosen as the default for bulk copy because it is the highest-throughput native API for DuckDB's in-process model; the fallback preserves correctness for the identity-column and unmapped-column edge cases.

## Consequences

- `ProviderName.DuckDB` added; `DuckDBDataProvider`, `DuckDBOptions`, `DuckDBFactory`, `DuckDBTools`, `DuckDBSpecificExtensions`, `IDuckDBSpecificTable`, `IDuckDBSpecificQueryable` are new public types.
- `DuckDBOptions.BulkCopyType` defaults to `BulkCopyType.ProviderSpecific` (Appender), overridable per-operation.
- Identity columns in DuckDB tables leave an orphaned sequence on the server when TRUNCATE WITH RESET is followed by DROP (the `_reset` sequence) if the table is dropped normally rather than via linq2db DDL.
- `linq2db.DuckDB` NuGet package added.

## Sources

- Commit `3537e02c6` -- Add DuckDB data provider (#5451) (MaceWindu, 2026-05-10)
- PR #5451
- File anchors: `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs`, `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs`, `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs`, `Source/LinqToDB/DataProvider/DuckDB/DuckDBOptions.cs`
