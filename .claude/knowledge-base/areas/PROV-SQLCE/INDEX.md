---
area: PROV-SQLCE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 11/11
coverage_tier_2: 10/10
---

# PROV-SQLCE -- SQL Server Compact Edition Provider

SQL Server Compact Edition (SQL CE 4.0) is a single-version, single-driver, file-based embedded database engine (`*.sdf` files). Unlike the SQL Server family there is no version matrix, no ADO.NET provider selection, and no server connection -- the entire engine ships as `System.Data.SqlServerCe.dll`. The provider reflects these constraints throughout: no MERGE, no UPDATE-JOIN, no window functions, no boolean columns, no `INSERT OR UPDATE`, no set-operation DISTINCT, no subquery-column references, and paging via SQL CE's OFFSET/FETCH dialect.

## Key types

| Type | File | Role |
|---|---|---|
| `SqlCeDataProvider` | `Internal/DataProvider/SqlCe/SqlCeDataProvider.cs` | `DynamicDataProviderBase<SqlCeProviderAdapter>` entry point; configures `SqlProviderFlags`, wires bulk copy, schema provider, and member translator |
| `SqlCeSqlBuilder` | `Internal/DataProvider/SqlCe/SqlCeSqlBuilder.cs` | `BasicSqlBuilder` override; implements CE-specific paging, identity reset, object quoting, MERGE rejection |
| `SqlCeSqlOptimizer` | `Internal/DataProvider/SqlCe/SqlCeSqlOptimizer.cs` | `BasicSqlOptimizer` override; corrects SKIP/ORDER BY requirements, Boolean-comparison rewriting, UPDATE-JOIN detection |
| `SqlCeProviderAdapter` | `Internal/DataProvider/SqlCe/SqlCeProviderAdapter.cs` | `IDynamicProviderAdapter` singleton; loads `System.Data.SqlServerCe` via reflection, wraps `SqlCeEngine`, `SqlCeParameter`, `SqlCeDataReader` |
| `SqlCeMappingSchema` | `Internal/DataProvider/SqlCe/SqlCeMappingSchema.cs` | `LockedMappingSchema`; registers `SqlTypes` scalar types, forces `string` -> `NVarChar(255)`, `decimal` -> `Decimal(18,10)`, CE string/binary/datetime literal converters |
| `SqlCeBulkCopy` | `Internal/DataProvider/SqlCe/SqlCeBulkCopy.cs` | `BasicBulkCopy` override; single-strategy (`MultipleRows` only via `MultipleRowsCopy2`), with `SET IDENTITY_INSERT ON/OFF` guard |
| `SqlCeSchemaProvider` | `Internal/DataProvider/SqlCe/SqlCeSchemaProvider.cs` | `SchemaProviderBase` override; queries `INFORMATION_SCHEMA` views directly via `GetSchema()` and raw SQL; no stored-proc introspection |
| `SqlCeDmlService` | `Internal/DataProvider/SqlCe/SqlCeDmlService.cs` | `DmlServiceBase` override; detects `DB_E_NOTABLE` (0x80040E37) for table-not-found via HResult or message fallback |
| `SqlCeSqlExpressionConvertVisitor` | `Internal/DataProvider/SqlCe/SqlCeSqlExpressionConvertVisitor.cs` | `SqlExpressionConvertVisitor` override; handles `%` modulo cast, `LEN` LENGTH workaround, case-sensitive string predicates via `Convert(VARBINARY,...)`, datetime conversions |
| `SqlCeMemberTranslator` | `Internal/DataProvider/SqlCe/Translation/SqlCeMemberTranslator.cs` | `ProviderMemberTranslatorDefault` override; assembles date/math/string/guid/aggregate sub-translators; includes `Now`/`ServerNow`/`ZonedNow` and date-truncation overrides (PR #5467, #5517) |
| `SqlCeTools` | `DataProvider/SqlCe/SqlCeTools.cs` | Public static entry point; `GetDataProvider()`, `CreateDataConnection()`, `CreateDatabase()` (via `SqlCeEngine`), `DropDatabase()`, provider detection |
| `SqlCeOptions` | `DataProvider/SqlCe/SqlCeOptions.cs` | `DataProviderOptions<SqlCeOptions>` record; `BulkCopyType` (default `MultipleRows`), `InlineFunctionParameters` (SQL CE 3.0 workaround) |
| `SqlCeHints` | `DataProvider/SqlCe/SqlCeHints.cs` + `.generated.cs` | Table hints: `HoldLock`, `NoLock`, `PagLock`, `RowLock`, `TabLock`, `UpdLock`, `XLock`, `Index`; `WithIndex()` for named index hints |
| `SqlCeSpecificExtensions` | `DataProvider/SqlCe/SqlCeSpecificExtensions.cs` | `AsSqlCe<T>()` fluent conversion for `ITable<T>` and `IQueryable<T>` |
| `ISqlCeSpecificTable<T>` | `DataProvider/SqlCe/ISqlCeSpecificTable.cs` | Marker interface for table-scoped CE hints |
| `ISqlCeSpecificQueryable<T>` | `DataProvider/SqlCe/ISqlCeSpecificQueryable.cs` | Marker interface for query-scoped CE hints |

## Subsystems

### Provider adapter (dynamic loading)

`SqlCeProviderAdapter` (singleton, `Lock`-guarded) loads `System.Data.SqlServerCe` assembly via `Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName)` at `SqlCeProviderAdapter.cs:82`. Wraps `SqlCeConnection`, `SqlCeDataReader`, `SqlCeParameter`, `SqlCeEngine` via `TypeMapper`. Assembly constants: `AssemblyName = "System.Data.SqlServerCe"`, `ProviderFactoryName = "System.Data.SqlServerCe.4.0"` (`SqlCeProviderAdapter.cs:18-20`). There is no provider-selection enum or version matrix -- a single adapter instance serves all connections.

`SqlCeEngine` is exposed as a typed wrapper (`SqlCeProviderAdapter.cs:149-169`) with `CreateDatabase()` and `Dispose()`. `SqlCeTools.CreateDatabase()` invokes it through `DataTools.CreateFileDatabase` to create `.sdf` files (`SqlCeTools.cs:62-73`).

A `SqlDecimal` -> `decimal` workaround (`ConvertToDecimal`, `SqlCeProviderAdapter.cs:120-144`) handles a CE provider bug: the native `GetDecimal` throws `OverflowException` for values with excess scale; the adapter retries with `SqlDecimal.ConvertToPrecScale` at decreasing scale.

### SQL generation divergences from SQL Server

`SqlCeSqlBuilder` overrides several `BasicSqlBuilder` behaviors:

- **Paging** (`SqlCeSqlBuilder.cs:31-48`): TOP for skip-less queries (`TOP ({0})`); OFFSET/FETCH for skip queries (`OFFSET {n} ROWS` / `FETCH NEXT {n} ROWS ONLY`). `OffsetFirst = true` -- OFFSET precedes FETCH in output.
- **Identity after INSERT** (`SqlCeSqlBuilder.cs:85`): `SELECT @@IDENTITY` (not `SCOPE_IDENTITY()`; CE has no scoping).
- **Truncate + identity reset** (`SqlCeSqlBuilder.cs:72-86`): each identity field emits a separate `ALTER TABLE ... ALTER COLUMN ... IDENTITY(1, 1)` statement. `CommandCount()` returns `1 + identityFields.Count` for truncate-with-reset.
- **Object quoting** (`SqlCeSqlBuilder.cs:128-163`): `[name]` brackets for fields and tables; no schema/catalog prefix -- `BuildObjectName` ignores schema and catalog (`SqlCeSqlBuilder.cs:171-181`).
- **MERGE** (`SqlCeSqlBuilder.cs:195-198`): throws `LinqToDBException` -- CE has no MERGE statement.
- **IS DISTINCT FROM** (`SqlCeSqlBuilder.cs:200`): falls back to `BuildIsDistinctPredicateFallback`.
- **Multiple same-name columns** (`SqlCeSqlBuilder.cs:51-60`): `CanSkipRootAliases` returns `false` -- CE rejects duplicate column names in SELECT.
- **VALUES syntax** (`SqlCeSqlBuilder.cs:47`): `IsValuesSyntaxSupported = false`.
- **Data type mapping** (`SqlCeSqlBuilder.cs:89-126`): `Char`/`VarChar` -> `NChar`/`NVarChar`; `SmallMoney` -> `Decimal(10,4)`; `DateTime2`/`Time`/`Date`/`SmallDateTime` -> `DateTime`; `NVarChar` capped at 4000; `Binary`/`VarBinary` capped at 8000.

### Query optimization

`SqlCeSqlOptimizer.TransformStatement` (`SqlCeSqlOptimizer.cs:23-46`) applies four CE-specific passes:

1. **`CorrectSkipAndColumns`** (`SqlCeSqlOptimizer.cs:96-153`): If a query has `SKIP` but no `ORDER BY`, injects a default ORDER BY on the first non-aggregate column (or first table field). If GROUP BY is present with no SELECT columns, injects `SELECT 1` (CE rejects `SELECT *` on grouped queries).
2. **`CorrectFunctionParameters`** (`SqlCeSqlOptimizer.cs:155-195`): When `InlineFunctionParameters = true` (SQL CE 3.0 compat), marks all `SqlParameter` values inside `SqlFunction` and `SqlCoalesceExpression` as non-query-parameters (forced inline literals).
3. **`CorrectBooleanComparison`** (`SqlCeSqlOptimizer.cs:202-224`): Rewrites `IsTrue(subquery-with-one-column)` -> `EXISTS(subquery with WHERE column IS TRUE)`. CE has no boolean column type.
4. **`FinalizeUpdate`** (`SqlCeSqlOptimizer.cs:48-94`): Detects UPDATE-JOINs and throws `LinqToDBException("SqlCe does not support UPDATE query with JOIN.")` unless the query only touches one table. `GetAlternativeDelete` is applied for DELETE statements.

`SqlCeSqlExpressionConvertVisitor` handles expression-level rewrites:
- `%` on non-integer type -> cast left operand to `int` first (`SqlCeSqlExpressionConvertVisitor.cs:28-47`).
- `LENGTH` -> `LEN(value + ".") - 1` (LEN trims trailing spaces; appending "." avoids that) (`SqlCeSqlExpressionConvertVisitor.cs:56-70`).
- Case-sensitive `StartsWith`/`EndsWith`/`Contains` -> `Convert(VARBINARY, SUBSTRING(...))` comparison (CE has no `COLLATE` clause) (`SqlCeSqlExpressionConvertVisitor.cs:76-152`).
- `NULLIF` not supported (`SupportsNullIf = false`).
- DateTime conversions: time-only extraction via `CAST(CONVERT(NChar, x, 114) as DateTime)`, date truncation via `CAST(CONVERT(nvarchar(10), x, 101) AS datetime)` (`SqlCeSqlExpressionConvertVisitor.cs:154-217`).

### Mapping schema

`SqlCeMappingSchema` (`SqlCeMappingSchema.cs`) registers:
- All SQL Server `SqlTypes` scalars (`SqlBinary`, `SqlBoolean`, `SqlByte`, `SqlDateTime`, `SqlDecimal`, `SqlDouble`, `SqlGuid`, `SqlInt16`, `SqlInt32`, `SqlInt64`, `SqlMoney`, `SqlSingle`, `SqlString`, `SqlXml`).
- `string` default -> `NVarChar(255)` (CE is Unicode-only).
- `decimal` default -> `Decimal(18, 10)` (CE `DECIMAL = DECIMAL(18,0)` by default; schema overrides to retain scale).
- String literal conversion uses `+` concatenation with `nchar()` for control characters. Binary literals use `0x` hex prefix.
- `DateTime` value-to-SQL literal uses format `'yyyy-MM-dd HH:mm:ss.fff'` (`SqlCeMappingSchema.cs:19-22`, `BuildDateTime`).
- `DateTimeOffset` value-to-SQL literal strips the offset: `((DateTimeOffset)v).DateTime` is passed to `BuildDateTime` (`SqlCeMappingSchema.cs:56`). This means offset information is silently discarded in inline SQL literals -- only the local datetime portion is emitted.
- XML values coerced to `NVarChar` in `SetParameter` (`SqlCeDataProvider.cs:88-95`).

### Parameter coercions in SetParameter

`SqlCeDataProvider.SetParameter` (`SqlCeDataProvider.cs:79-102`) applies these coercions before delegating to the base:
- `DateOnly` -> `DateTime` via `d.ToDateTime(TimeOnly.MinValue)` (guarded by `#if SUPPORTS_DATEONLY`) (`SqlCeDataProvider.cs:82-84`).
- `DateTimeOffset` -> `LocalDateTime` (`SqlCeDataProvider.cs:87`): offset is stripped; the local portion is passed as a `DateTime` parameter. CE has no `DateTimeOffset` column type.
- `DataType.Xml` -> `DataType.NVarChar`; `SqlXml`/`XDocument`/`XmlDocument` values are serialized to string (`SqlCeDataProvider.cs:91-98`).

### Bulk copy

`SqlCeBulkCopy` inherits `BasicBulkCopy` and overrides only `MultipleRowsCopy` / `MultipleRowsCopyAsync` (`SqlCeBulkCopy.cs`). There is no provider-native bulk insert path (no `SqlCeBulkCopy` native API exists): the only strategy is `MultipleRows` (row-by-row batch inserts via `MultipleRowsCopy2`). `RowByRow` is not overridden, so the base class fallback applies. `KeepIdentity` wraps the batch with `SET IDENTITY_INSERT ... ON/OFF`.

Three overloads exist: sync `MultipleRowsCopy<T>` (`SqlCeBulkCopy.cs:11-30`), async with `IEnumerable<T>` (`SqlCeBulkCopy.cs:32-54`), and async with `IAsyncEnumerable<T>` (`SqlCeBulkCopy.cs:56-78`). All three share the same `IDENTITY_INSERT` guard pattern and delegate to `MultipleRowsCopy2` / `MultipleRowsCopy2Async`.

In `SqlCeDataProvider.BulkCopy` (`SqlCeDataProvider.cs:163-198`), if `BulkCopyType.Default` is requested, the effective type comes from `SqlCeOptions.BulkCopyType` (default `MultipleRows`). There is no `ProviderSpecific` bulk copy path.

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
- **Date**: `DateFunctionsTranslator` -- `DATEPART(...)`, `DATEADD(...)`, `MakeDateTime` via string concatenation + `CAST`, date truncation via `CONVERT(nvarchar(10), x, 101)`, `GetDate()`. No `DateTimeOffset` support (errors on use).
- **Math**: `SqlCeMathMemberTranslator` -- `Round` always passes explicit `0` precision when none given (CE requires explicit scale for ROUND).
- **String**: `SqlCeStringMemberTranslator` -- `LPAD` via `REPLICATE + concatenation`; `String.Join` via `CONCAT_WS` emulation with `SUBSTRING` trim.
- **Guid**: `GuidMemberTranslator` -- `ToString()` -> `LOWER(CAST(x AS char(36)))`.
- **Aggregate**: `SqlCeAggregateFunctionsMemberTranslator` -- `IsCountDistinctSupported = false`, `IsAggregationDistinctSupported = false`.

**Delta -- PR #5467 (`d779808a2`):** `DateFunctionsTranslator` now overrides three `Now`-family methods:
- `TranslateNow` (`SqlCeMemberTranslator.cs:232-237`): emits `GetDate()` with `ParametersNullabilityType.NotNullable`. Previously, `Now` translation fell through to the base class default (`CURRENT_TIMESTAMP`).
- `TranslateServerNow` (`SqlCeMemberTranslator.cs:226-230`): delegates to `TranslateNow`. CE is an embedded engine with no server-side timezone; there is no distinction between local and server time.
- `TranslateZonedNow` (`SqlCeMemberTranslator.cs:239-243`): also emits `GetDate()`. CE has no `DateTimeOffset` support so there is no zoned-time concept.

**Delta -- PR #5517 (`c3f260f68`):** `DateFunctionsTranslator` now overrides `TranslateDateTimeTruncationToDate` (`SqlCeMemberTranslator.cs:213-223`): emits `CAST(CONVERT(nvarchar(10), <expr>, 101) AS datetime)`. This is the LINQ member-translator path for `DateTime.Date` truncation, distinct from (and parallel to) the existing `SqlCeSqlExpressionConvertVisitor` path which handled legacy expression-tree rewrites. Both paths produce the same SQL.

### Provider flags summary

Set in `SqlCeDataProvider` constructor (`SqlCeDataProvider.cs:31-39`):

| Flag | Value | Consequence |
|---|---|---|
| `IsSubQueryColumnSupported` | `false` | No column references into subqueries |
| `IsCountSubQuerySupported` | `false` | COUNT inside subquery not allowed |
| `IsApplyJoinSupported` | `true` | CROSS/OUTER APPLY supported |
| `IsInsertOrUpdateSupported` | `false` | No MERGE/UPSERT |
| `IsDistinctSetOperationsSupported` | `false` | UNION/INTERSECT/EXCEPT DISTINCT not supported |
| `IsUpdateFromSupported` | `false` | No UPDATE ... FROM |
| `SupportsBooleanType` | `false` | No BIT column in boolean context |
| `IsWindowFunctionsSupported` | `false` | No OVER() |
| `IsOrderByAggregateFunctionSupported` | `false` | No ORDER BY in aggregate |
| `TableOptions` | `None` | No temp tables, no IF NOT EXISTS |

## Files (Tier 1 / Tier 2)

### Tier 1 (11 files, all read)
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

### Tier 2 (10 files, all read)
| File | Notes |
|---|---|
| `DataProvider/SqlCe/SqlCeOptions.cs` | Provider options record |
| `DataProvider/SqlCe/SqlCeFactory.cs` | `IDataProviderFactory` pluggable factory |
| `DataProvider/SqlCe/SqlCeHints.cs` | Table and scope hints |
| `DataProvider/SqlCe/ISqlCeSpecificTable.cs` | Marker interface |
| `DataProvider/SqlCe/ISqlCeSpecificQueryable.cs` | Marker interface |
| `DataProvider/SqlCe/SqlCeSpecificExtensions.cs` | `AsSqlCe()` fluent entry |
| `Internal/DataProvider/SqlCe/SqlCeSpecificQueryable.cs` | Concrete queryable wrapper |
| `Internal/DataProvider/SqlCe/SqlCeSpecificTable.cs` | Concrete table wrapper |
| `DataProvider/SqlCe/SqlCeHints.generated.cs` | Generated per-hint extension methods |
| `DataProvider/SqlCe/SqlCeHints.tt` | T4 template (Tier 3, counted only) |

## Inbound / outbound dependencies

**Inbound:** `ProviderName.SqlCe`, `SqlCeTools.ProviderDetector`, `SqlCeFactory`. Tests reference `SqlCeDataProvider`.

**Outbound:** `BasicSqlBuilder`, `BasicSqlOptimizer`, `BasicBulkCopy`, `SqlExpressionConvertVisitor`, `ProviderMemberTranslatorDefault`, `DateFunctionsTranslatorBase`, `SchemaProviderBase`, `DmlServiceBase`, `DynamicDataProviderBase<T>`, `TypeMapper`, `DataTools.CreateFileDatabase/DropFileDatabase`. Runtime: `System.Data.SqlServerCe`.

## Known issues / debt

- **`IsIdentity` always `false` in schema provider** (`SqlCeSchemaProvider.cs:106`): CE schema API does not report identity columns.
- **`DB_E_NOTABLE` HResult unreliable**: `SqlCeDmlService` falls back to message-string check.
- **`FixEmptySelect` suppressed**: intentionally no-op'd because `CorrectSkipAndColumns` handles the same case.
- **`InlineFunctionParameters` targets CE 3.0**: off by default but may be confusing.
- **No provider-native bulk insert**: CE has no bulk-copy API.
- **Paging quirk with SKIP + no ORDER BY**: synthetic ORDER BY may produce non-deterministic results.
- **`NVarChar` max 4000 hardcoded** -- no auto-upgrade to `NTEXT`.
- **`DateTimeOffset` offset silently discarded in two places**: `SetParameter` (`SqlCeDataProvider.cs:87`) and mapping schema (`SqlCeMappingSchema.cs:56`).
- **Dual date-truncation paths**: `DateTime.Date` truncation is implemented in both `SqlCeSqlExpressionConvertVisitor` and `DateFunctionsTranslator.TranslateDateTimeTruncationToDate` (PR #5517).

## See also

- [architecture/overview.md](../../architecture/overview.md), [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md)

<details><summary>Coverage</summary>

- Tier 1: 11 / 11, Tier 2: 10 / 10, Tier 3: 1 (`SqlCeHints.tt`)

Read this run (delta sha 4a478ff14):
- `SqlCeBulkCopy.cs` -- three-overload structure confirmed; no body changes
- `SqlCeDataProvider.cs` -- `DateOnly`/`DateTimeOffset` coercions documented
- `SqlCeMappingSchema.cs` -- `DateTimeOffset` literal path (offset stripped) documented
- `SqlCeMemberTranslator.cs` -- PR #5467: `TranslateNow`/`TranslateServerNow`/`TranslateZonedNow` emit `GetDate()`; PR #5517: `TranslateDateTimeTruncationToDate` emits `CAST(CONVERT(nvarchar(10),x,101) AS datetime)`

</details>
