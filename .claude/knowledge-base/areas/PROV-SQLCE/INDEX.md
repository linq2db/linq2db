---
area: PROV-SQLCE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 11/11
coverage_tier_2: 10/10
---

# PROV-SQLCE — SQL Server Compact Edition Provider

SQL Server Compact Edition (SQL CE 4.0) is a single-version, single-driver, file-based embedded database engine (`*.sdf` files). Unlike the SQL Server family there is no version matrix, no ADO.NET provider selection, and no server connection — the entire engine ships as `System.Data.SqlServerCe.dll`. The provider reflects these constraints throughout: no MERGE, no UPDATE-JOIN, no window functions, no boolean columns, no `INSERT OR UPDATE`, no set-operation DISTINCT, no subquery-column references, and paging via SQL CE's OFFSET/FETCH dialect.

## Key types

| Type | File | Role |
|---|---|---|
| `SqlCeDataProvider` | `Internal/DataProvider/SqlCe/SqlCeDataProvider.cs` | `DynamicDataProviderBase<SqlCeProviderAdapter>` entry point; configures `SqlProviderFlags`, wires bulk copy, schema provider, and member translator |
| `SqlCeSqlBuilder` | `Internal/DataProvider/SqlCe/SqlCeSqlBuilder.cs` | `BasicSqlBuilder` override; implements CE-specific paging, identity reset, object quoting, MERGE rejection |
| `SqlCeSqlOptimizer` | `Internal/DataProvider/SqlCe/SqlCeSqlOptimizer.cs` | `BasicSqlOptimizer` override; corrects SKIP/ORDER BY requirements, Boolean-comparison rewriting, UPDATE-JOIN detection |
| `SqlCeProviderAdapter` | `Internal/DataProvider/SqlCe/SqlCeProviderAdapter.cs` | `IDynamicProviderAdapter` singleton; loads `System.Data.SqlServerCe` via reflection, wraps `SqlCeEngine`, `SqlCeParameter`, `SqlCeDataReader` |
| `SqlCeMappingSchema` | `Internal/DataProvider/SqlCe/SqlCeMappingSchema.cs` | `LockedMappingSchema`; registers `SqlTypes` scalar types, forces `string` → `NVarChar(255)`, `decimal` → `Decimal(18,10)`, CE string/binary literal converters |
| `SqlCeBulkCopy` | `Internal/DataProvider/SqlCe/SqlCeBulkCopy.cs` | `BasicBulkCopy` override; single-strategy (`MultipleRows` only via `MultipleRowsCopy2`), with `SET IDENTITY_INSERT ON/OFF` guard |
| `SqlCeSchemaProvider` | `Internal/DataProvider/SqlCe/SqlCeSchemaProvider.cs` | `SchemaProviderBase` override; queries `INFORMATION_SCHEMA` views directly via `GetSchema()` and raw SQL; no stored-proc introspection |
| `SqlCeDmlService` | `Internal/DataProvider/SqlCe/SqlCeDmlService.cs` | `DmlServiceBase` override; detects `DB_E_NOTABLE` (0x80040E37) for table-not-found via HResult or message fallback |
| `SqlCeSqlExpressionConvertVisitor` | `Internal/DataProvider/SqlCe/SqlCeSqlExpressionConvertVisitor.cs` | `SqlExpressionConvertVisitor` override; handles `%` modulo cast, `LEN` LENGTH workaround, case-sensitive string predicates via `Convert(VARBINARY,…)`, datetime conversions |
| `SqlCeMemberTranslator` | `Internal/DataProvider/SqlCe/Translation/SqlCeMemberTranslator.cs` | `ProviderMemberTranslatorDefault` override; assembles date/math/string/guid/aggregate sub-translators; no `DateTimeOffset`, no `COUNT(DISTINCT)`, no aggregate DISTINCT |
| `SqlCeTools` | `DataProvider/SqlCe/SqlCeTools.cs` | Public static entry point; `GetDataProvider()`, `CreateDataConnection()`, `CreateDatabase()` (via `SqlCeEngine`), `DropDatabase()`, provider detection |
| `SqlCeOptions` | `DataProvider/SqlCe/SqlCeOptions.cs` | `DataProviderOptions<SqlCeOptions>` record; `BulkCopyType` (default `MultipleRows`), `InlineFunctionParameters` (SQL CE 3.0 workaround) |
| `SqlCeHints` | `DataProvider/SqlCe/SqlCeHints.cs` + `.generated.cs` | Table hints: `HoldLock`, `NoLock`, `PagLock`, `RowLock`, `TabLock`, `UpdLock`, `XLock`, `Index`; `WithIndex()` for named index hints |
| `SqlCeSpecificExtensions` | `DataProvider/SqlCe/SqlCeSpecificExtensions.cs` | `AsSqlCe<T>()` fluent conversion for `ITable<T>` and `IQueryable<T>` |
| `ISqlCeSpecificTable<T>` | `DataProvider/SqlCe/ISqlCeSpecificTable.cs` | Marker interface for table-scoped CE hints |
| `ISqlCeSpecificQueryable<T>` | `DataProvider/SqlCe/ISqlCeSpecificQueryable.cs` | Marker interface for query-scoped CE hints |

## Subsystems

### Provider adapter (dynamic loading)

`SqlCeProviderAdapter` (singleton, `Lock`-guarded) loads `System.Data.SqlServerCe` assembly via `Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName)` at `SqlCeProviderAdapter.cs:82`. Wraps `SqlCeConnection`, `SqlCeDataReader`, `SqlCeParameter`, `SqlCeEngine` via `TypeMapper`. Assembly constants: `AssemblyName = "System.Data.SqlServerCe"`, `ProviderFactoryName = "System.Data.SqlServerCe.4.0"` (`SqlCeProviderAdapter.cs:18-20`). There is no provider-selection enum or version matrix — a single adapter instance serves all connections.

`SqlCeEngine` is exposed as a typed wrapper (`SqlCeProviderAdapter.cs:149-169`) with `CreateDatabase()` and `Dispose()`. `SqlCeTools.CreateDatabase()` invokes it through `DataTools.CreateFileDatabase` to create `.sdf` files (`SqlCeTools.cs:62-73`).

A `SqlDecimal` → `decimal` workaround (`ConvertToDecimal`, `SqlCeProviderAdapter.cs:120-144`) handles a CE provider bug: the native `GetDecimal` throws `OverflowException` for values with excess scale; the adapter retries with `SqlDecimal.ConvertToPrecScale` at decreasing scale.

### SQL generation divergences from SQL Server

`SqlCeSqlBuilder` overrides several `BasicSqlBuilder` behaviors:

- **Paging** (`SqlCeSqlBuilder.cs:31-48`): TOP for skip-less queries (`TOP ({0})`); OFFSET/FETCH for skip queries (`OFFSET {n} ROWS` / `FETCH NEXT {n} ROWS ONLY`). `OffsetFirst = true` — OFFSET precedes FETCH in output.
- **Identity after INSERT** (`SqlCeSqlBuilder.cs:85`): `SELECT @@IDENTITY` (not `SCOPE_IDENTITY()`; CE has no scoping).
- **Truncate + identity reset** (`SqlCeSqlBuilder.cs:72-86`): each identity field emits a separate `ALTER TABLE … ALTER COLUMN … IDENTITY(1, 1)` statement. `CommandCount()` returns `1 + identityFields.Count` for truncate-with-reset.
- **Object quoting** (`SqlCeSqlBuilder.cs:128-163`): `[name]` brackets for fields and tables; no schema/catalog prefix — `BuildObjectName` ignores schema and catalog (`SqlCeSqlBuilder.cs:171-181`).
- **MERGE** (`SqlCeSqlBuilder.cs:195-198`): throws `LinqToDBException` — CE has no MERGE statement.
- **IS DISTINCT FROM** (`SqlCeSqlBuilder.cs:200`): falls back to `BuildIsDistinctPredicateFallback`.
- **Multiple same-name columns** (`SqlCeSqlBuilder.cs:51-60`): `CanSkipRootAliases` returns `false` — CE rejects duplicate column names in SELECT.
- **VALUES syntax** (`SqlCeSqlBuilder.cs:47`): `IsValuesSyntaxSupported = false`.
- **Data type mapping** (`SqlCeSqlBuilder.cs:89-126`): `Char`/`VarChar` → `NChar`/`NVarChar`; `SmallMoney` → `Decimal(10,4)`; `DateTime2`/`Time`/`Date`/`SmallDateTime` → `DateTime`; `NVarChar` capped at 4000; `Binary`/`VarBinary` capped at 8000.

### Query optimization

`SqlCeSqlOptimizer.TransformStatement` (`SqlCeSqlOptimizer.cs:23-46`) applies four CE-specific passes:

1. **`CorrectSkipAndColumns`** (`SqlCeSqlOptimizer.cs:96-153`): If a query has `SKIP` but no `ORDER BY`, injects a default ORDER BY on the first non-aggregate column (or first table field). If GROUP BY is present with no SELECT columns, injects `SELECT 1` (CE rejects `SELECT *` on grouped queries).
2. **`CorrectFunctionParameters`** (`SqlCeSqlOptimizer.cs:155-195`): When `InlineFunctionParameters = true` (SQL CE 3.0 compat), marks all `SqlParameter` values inside `SqlFunction` and `SqlCoalesceExpression` as non-query-parameters (forced inline literals).
3. **`CorrectBooleanComparison`** (`SqlCeSqlOptimizer.cs:202-224`): Rewrites `IsTrue(subquery-with-one-column)` → `EXISTS(subquery with WHERE column IS TRUE)`. CE has no boolean column type.
4. **`FinalizeUpdate`** (`SqlCeSqlOptimizer.cs:48-94`): Detects UPDATE-JOINs and throws `LinqToDBException("SqlCe does not support UPDATE query with JOIN.")` unless the query only touches one table. `GetAlternativeDelete` is applied for DELETE statements.

`SqlCeSqlExpressionConvertVisitor` handles expression-level rewrites:
- `%` on non-integer type → cast left operand to `int` first (`SqlCeSqlExpressionConvertVisitor.cs:28-47`).
- `LENGTH` → `LEN(value + ".") - 1` (LEN trims trailing spaces; appending "." avoids that) (`SqlCeSqlExpressionConvertVisitor.cs:56-70`).
- Case-sensitive `StartsWith`/`EndsWith`/`Contains` → `Convert(VARBINARY, SUBSTRING(…))` comparison (CE has no `COLLATE` clause) (`SqlCeSqlExpressionConvertVisitor.cs:76-152`).
- `NULLIF` not supported (`SupportsNullIf = false`).
- DateTime conversions: time-only extraction via `CAST(CONVERT(NChar, x, 114) as DateTime)`, date truncation via `CAST(CONVERT(nvarchar(10), x, 101) AS datetime)` (`SqlCeSqlExpressionConvertVisitor.cs:154-217`).

### Mapping schema

`SqlCeMappingSchema` (`SqlCeMappingSchema.cs`) registers:
- All SQL Server `SqlTypes` scalars (`SqlBinary`, `SqlBoolean`, `SqlByte`, `SqlDateTime`, `SqlDecimal`, `SqlDouble`, `SqlGuid`, `SqlInt16`, `SqlInt32`, `SqlInt64`, `SqlMoney`, `SqlSingle`, `SqlString`, `SqlXml`).
- `string` default → `NVarChar(255)` (CE is Unicode-only).
- `decimal` default → `Decimal(18, 10)` (CE `DECIMAL = DECIMAL(18,0)` by default; schema overrides to retain scale).
- String literal conversion uses `+` concatenation with `nchar()` for control characters. Binary literals use `0x` hex prefix.
- XML values coerced to `NVarChar` in `SetParameter` (`SqlCeDataProvider.cs:88-95`).

### Bulk copy

`SqlCeBulkCopy` inherits `BasicBulkCopy` and overrides only `MultipleRowsCopy` / `MultipleRowsCopyAsync` (`SqlCeBulkCopy.cs`). There is no provider-native bulk insert path (no `SqlCeBulkCopy` native API exists): the only strategy is `MultipleRows` (row-by-row batch inserts via `MultipleRowsCopy2`). `RowByRow` is not overridden, so the base class fallback applies. `KeepIdentity` wraps the batch with `SET IDENTITY_INSERT … ON/OFF`.

In `SqlCeDataProvider.BulkCopy` (`SqlCeDataProvider.cs:159-194`), if `BulkCopyType.Default` is requested, the effective type comes from `SqlCeOptions.BulkCopyType` (default `MultipleRows`). There is no `ProviderSpecific` bulk copy path.

### Schema provider

`SqlCeSchemaProvider` (`SqlCeSchemaProvider.cs`) extends `SchemaProviderBase`:
- Tables: `GetSchema("Tables")` filtered to `TABLE` and `VIEW` types.
- Columns: `GetSchema("Columns")`; `IsIdentity` is hardcoded to `false` (CE schema API does not expose identity reliably).
- Primary keys: raw SQL on `INFORMATION_SCHEMA.INDEXES WHERE PRIMARY_KEY = 1`.
- Foreign keys: raw SQL join of `INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS` + `KEY_COLUMN_USAGE`.
- Database name: `Path.GetFileNameWithoutExtension(connection.Database)` (strips `.sdf`).
- Type mapping covers CE's Unicode-only column types (`nvarchar`, `nchar`, `ntext`) plus `rowversion`, `uniqueidentifier`, `image`.

### Member translation

`SqlCeMemberTranslator` composes specialized sub-translators:
- **Date**: `DateFunctionsTranslator` — `DATEPART(…)`, `DATEADD(…)`, `MakeDateTime` via string concatenation + `CAST`, date truncation via `CONVERT(nvarchar(10), x, 101)`, `GetDate()`. No `DateTimeOffset` support (errors on use).
- **Math**: `SqlCeMathMemberTranslator` — `Round` always passes explicit `0` precision when none given (CE requires explicit scale for ROUND).
- **String**: `SqlCeStringMemberTranslator` — `LPAD` via `REPLICATE + concatenation`; `String.Join` via `CONCAT_WS` emulation with `SUBSTRING` trim.
- **Guid**: `GuidMemberTranslator` — `ToString()` → `LOWER(CAST(x AS char(36)))`.
- **Aggregate**: `SqlCeAggregateFunctionsMemberTranslator` — `IsCountDistinctSupported = false`, `IsAggregationDistinctSupported = false`.

### Provider flags summary

Set in `SqlCeDataProvider` constructor (`SqlCeDataProvider.cs:31-39`):

| Flag | Value | Consequence |
|---|---|---|
| `IsSubQueryColumnSupported` | `false` | No column references into subqueries |
| `IsCountSubQuerySupported` | `false` | COUNT inside subquery not allowed |
| `IsApplyJoinSupported` | `true` | CROSS/OUTER APPLY supported |
| `IsInsertOrUpdateSupported` | `false` | No MERGE/UPSERT |
| `IsDistinctSetOperationsSupported` | `false` | UNION/INTERSECT/EXCEPT DISTINCT not supported |
| `IsUpdateFromSupported` | `false` | No UPDATE … FROM |
| `SupportsBooleanType` | `false` | No BIT column in boolean context |
| `IsWindowFunctionsSupported` | `false` | No OVER() |
| `IsOrderByAggregateFunctionSupported` | `false` | No ORDER BY in aggregate |
| `TableOptions` | `None` | No temp tables, no IF NOT EXISTS |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full, all 11)

| File | Role |
|---|---|
| `Internal/DataProvider/SqlCe/SqlCeDataProvider.cs` | Provider entry point |
| `Internal/DataProvider/SqlCe/SqlCeSqlBuilder.cs` | SQL generation |
| `Internal/DataProvider/SqlCe/SqlCeSqlOptimizer.cs` | Query AST transformations |
| `Internal/DataProvider/SqlCe/SqlCeProviderAdapter.cs` | ADO.NET dynamic loader |
| `Internal/DataProvider/SqlCe/SqlCeMappingSchema.cs` | Type/scalar mapping |
| `Internal/DataProvider/SqlCe/SqlCeBulkCopy.cs` | Bulk copy (MultipleRows only) |
| `Internal/DataProvider/SqlCe/SqlCeSchemaProvider.cs` | Schema introspection |
| `Internal/DataProvider/SqlCe/SqlCeDmlService.cs` | DML exception detection |
| `Internal/DataProvider/SqlCe/SqlCeSqlExpressionConvertVisitor.cs` | Expression-level rewrites |
| `Internal/DataProvider/SqlCe/Translation/SqlCeMemberTranslator.cs` | LINQ member translation |
| `DataProvider/SqlCe/SqlCeTools.cs` | Public static API |

### Tier 2 (read in full, all 10)

| File | Notes |
|---|---|
| `DataProvider/SqlCe/SqlCeOptions.cs` | Provider options record |
| `DataProvider/SqlCe/SqlCeFactory.cs` | `IDataProviderFactory` pluggable factory |
| `DataProvider/SqlCe/SqlCeHints.cs` | Table and scope hints (hand-written part) |
| `DataProvider/SqlCe/ISqlCeSpecificTable.cs` | Marker interface |
| `DataProvider/SqlCe/ISqlCeSpecificQueryable.cs` | Marker interface |
| `DataProvider/SqlCe/SqlCeSpecificExtensions.cs` | `AsSqlCe()` fluent entry |
| `Internal/DataProvider/SqlCe/SqlCeSpecificQueryable.cs` | Concrete queryable wrapper |
| `Internal/DataProvider/SqlCe/SqlCeSpecificTable.cs` | Concrete table wrapper |
| `DataProvider/SqlCe/SqlCeHints.generated.cs` | Generated per-hint extension methods |
| `DataProvider/SqlCe/SqlCeHints.tt` | T4 template (Tier 3, counted only) |

### Tier 3 (counted, not read)

- `DataProvider/SqlCe/SqlCeHints.tt` — T4 code-generation template

## Inbound / outbound dependencies

**Inbound (callers / users of this area):**
- `ProviderName.SqlCe` constant (`ProviderName.cs:279`) — referenced by hints, factory, and provider detection.
- `SqlCeTools.ProviderDetector` registered in the provider-detector chain; invoked on every `DataConnection` open with `ProviderName` or `ConfigurationString` containing `"SqlCe"` or `"SqlServerCe"`.
- `SqlCeFactory` loaded via `IDataProviderFactory` configuration mechanism.
- Tests under `Tests/Linq/` and `Tests/DataProvider/` exercise this provider through `SqlCeDataProvider`.

**Outbound (what this area depends on):**
- `BasicSqlBuilder` / `BasicSqlOptimizer` / `BasicBulkCopy` — base classes in `Internal/SqlProvider/`.
- `SqlExpressionConvertVisitor` — base in `Internal/SqlProvider/`.
- `ProviderMemberTranslatorDefault` — base in `Internal/DataProvider/Translation/`.
- `SchemaProviderBase` — base in `Internal/SchemaProvider/`.
- `DmlServiceBase` — base in `Internal/DataProvider/`.
- `DynamicDataProviderBase<T>` — base in `Internal/DataProvider/`.
- `TypeMapper` — reflection wrapper in `Internal/Expressions/Types/`.
- `DataTools.CreateFileDatabase` / `DropFileDatabase` — file-based DB lifecycle utilities.
- `System.Data.SqlServerCe` (runtime, optional) — loaded dynamically; not referenced at compile time.

## Known issues / debt

- **`IsIdentity` always `false` in schema provider** (`SqlCeSchemaProvider.cs:106`): The CE `INFORMATION_SCHEMA.COLUMNS` schema does not report identity columns; callers that rely on `ColumnInfo.IsIdentity` from schema scaffolding will miss identity fields. This is a known limitation of the CE schema API, not a linq2db bug, but it means schema-scaffolded models will lack identity annotations.
- **`DB_E_NOTABLE` HResult unreliable** (`SqlCeDmlService.cs:7-8`): The comment in `SqlCeDmlService` acknowledges that `SqlCeException` exposes HResult incorrectly. The fallback message-string check (`"specified table does not exist"`) is best-effort and locale-sensitive.
- **`FixEmptySelect` suppressed** (`SqlCeSqlOptimizer.cs:197-200`): `FixEmptySelect` is intentionally no-op'd because `CorrectSkipAndColumns` handles the same case. If `CorrectSkipAndColumns` ever changes independently, this no-op could become a latent bug.
- **`InlineFunctionParameters` targets CE 3.0**: The `SqlCeOptions.InlineFunctionParameters` flag exists specifically for SQL CE 3.0 which cannot handle parameterized function arguments. CE 4.0 does not require this. The flag is off by default but may be confusing.
- **No provider-native bulk insert**: CE has no bulk-copy API. All bulk operations go through multi-row `INSERT` batches. Large loads will be slow.
- **Paging quirk with SKIP + no ORDER BY**: `CorrectSkipAndColumns` injects a synthetic ORDER BY, which may produce non-deterministic results when no natural key exists. The comment at `SqlCeSqlOptimizer.cs:135-140` links the CE documentation for this constraint.
- **`NVarChar` max 4000 hardcoded** (`SqlCeSqlBuilder.cs:103-108`): CE's `NVARCHAR` maximum is 4000 (not 4000+ with `MAX`). Values above 4000 must use `NTEXT`. The builder auto-caps at 4000 but does not upgrade to `NTEXT` — callers must use `DataType.NText` explicitly for large text.

## See also

- [architecture/overview.md](../../architecture/overview.md) — query pipeline context
- [areas/PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) — parent dialect family (multi-version, multi-driver contrast)
- `ProviderName.cs:279` — `ProviderName.SqlCe` constant
- [glossary.md](../../glossary.md) — `DynamicDataProviderBase`, `BasicSqlBuilder`, `SqlProviderFlags`, `TypeMapper`

<details><summary>Coverage</summary>

**Tier 1 — read in full: 11/11**

- `Internal/DataProvider/SqlCe/SqlCeDataProvider.cs` — provider entry point, flags, parameter mapping, bulk copy dispatch
- `Internal/DataProvider/SqlCe/SqlCeSqlBuilder.cs` — paging, identity, quoting, MERGE rejection, data type mapping
- `Internal/DataProvider/SqlCe/SqlCeSqlOptimizer.cs` — CorrectSkipAndColumns, CorrectBooleanComparison, FinalizeUpdate, CorrectFunctionParameters
- `Internal/DataProvider/SqlCe/SqlCeProviderAdapter.cs` — dynamic loader, SqlCeEngine wrapper, SqlDecimal workaround
- `Internal/DataProvider/SqlCe/SqlCeMappingSchema.cs` — SqlTypes, NVarChar default, decimal default, string/binary literal converters
- `Internal/DataProvider/SqlCe/SqlCeBulkCopy.cs` — MultipleRows-only strategy, IDENTITY_INSERT guard
- `Internal/DataProvider/SqlCe/SqlCeSchemaProvider.cs` — INFORMATION_SCHEMA queries, IsIdentity=false, FK/PK via raw SQL
- `Internal/DataProvider/SqlCe/SqlCeDmlService.cs` — DB_E_NOTABLE detection, message fallback
- `Internal/DataProvider/SqlCe/SqlCeSqlExpressionConvertVisitor.cs` — modulo cast, LEN workaround, case-sensitive search, datetime conversions
- `Internal/DataProvider/SqlCe/Translation/SqlCeMemberTranslator.cs` — date/math/string/guid/aggregate sub-translators
- `DataProvider/SqlCe/SqlCeTools.cs` — GetDataProvider, CreateDatabase, DropDatabase, ResolveSqlCe

**Tier 2 — read in full: 10/10**

- `DataProvider/SqlCe/SqlCeOptions.cs` — BulkCopyType default, InlineFunctionParameters flag
- `DataProvider/SqlCe/SqlCeFactory.cs` — IDataProviderFactory pluggable factory, delegates to SqlCeTools
- `DataProvider/SqlCe/SqlCeHints.cs` — TableHint/TablesInScopeHint infrastructure, WithIndex overloads
- `DataProvider/SqlCe/ISqlCeSpecificTable.cs` — marker interface
- `DataProvider/SqlCe/ISqlCeSpecificQueryable.cs` — marker interface
- `DataProvider/SqlCe/SqlCeSpecificExtensions.cs` — AsSqlCe() fluent entry for table and queryable
- `Internal/DataProvider/SqlCe/SqlCeSpecificQueryable.cs` — concrete queryable wrapper (one-liner record-like class)
- `Internal/DataProvider/SqlCe/SqlCeSpecificTable.cs` — concrete table wrapper
- `DataProvider/SqlCe/SqlCeHints.generated.cs` — generated per-hint methods (WithHoldLock, WithNoLock, etc.)
- `DataProvider/SqlCe/SqlCeHints.tt` — T4 template (classified Tier 3, counted only, not read)

**Tier 3 — counted, not read: 1**

- `DataProvider/SqlCe/SqlCeHints.tt` — T4 code generation template

</details>
