---
area: PROV-SQLITE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 10/10
coverage_tier_2: 10/10
---

# PROV-SQLITE

SQLite provider for linq2db. Covers two ADO.NET clients under one single-version provider class hierarchy. No dialect versioning -- the codebase comments pin the supported SQLite engine range to 3.35.5+ (the minimum shipped with Microsoft.Data.Sqlite 6.0.0+), effectively covering 3.35.5-3.46.1 as of last update (`SQLiteDataProvider.cs:27-30`).

## Subsystems

### Provider class hierarchy

`SQLiteDataProvider` (abstract, `Internal/DataProvider/SQLite/SQLiteDataProvider.cs`) extends `DynamicDataProviderBase<SQLiteProviderAdapter>`. Two concrete sealed subclasses share the same file:

- `SQLiteDataProviderClassic` -- `ProviderName.SQLiteClassic`, `SQLiteProvider.System`
- `SQLiteDataProviderMS` -- `ProviderName.SQLiteMS`, `SQLiteProvider.Microsoft`

No version subclasses exist. The `// currently enabled flags require at least 3.33.0 SQLite` comment at `SQLiteDataProvider.cs:40` explains the floor. `SqlProviderFlags.IsUpdateFromSupported` is implicitly set by `BasicSqlOptimizer` parent depending on the flag value.

### Dual ADO.NET client matrix

Two assembly + namespace pairs are loaded dynamically via `SQLiteProviderAdapter`:

| Client | Assembly | Namespace prefix | Type prefix |
|---|---|---|---|
| System.Data.SQLite | `System.Data.SQLite` | `System.Data.SQLite` | `SQLite` |
| Microsoft.Data.Sqlite | `Microsoft.Data.Sqlite` | `Microsoft.Data.Sqlite` | `Sqlite` |

`SQLiteProviderAdapter.cs:18-22` defines the assembly and namespace constants. `ClearAllPools` is only wired when the loaded assembly version is >= 1.0.55 for System.Data.SQLite or >= 6.0.0 for Microsoft.Data.Sqlite (`SQLiteProviderAdapter.cs:127-129`). `SupportsDateOnly` is `true` only for Microsoft.Data.Sqlite >= 6.0.0 (`SQLiteProviderAdapter.cs:104`); the classic driver stores dates incorrectly and that flag is explicitly false for it.

### Provider detection

`SQLiteProviderDetector` (`Internal/DataProvider/SQLite/SQLiteProviderDetector.cs`) holds two `Lazy<IDataProvider>` singletons: `_SQLiteClassicDataProvider` and `_SQLiteMSDataProvider`. Auto-detection probes `ProviderName`, `ConfigurationString`, and finally the file system (`SQLiteProviderDetector.cs:94-99`): if `System.Data.SQLite.dll` is present next to the calling assembly, `System` wins; otherwise `Microsoft` is the default. Disambiguation uses `"MS"` / `"Microsoft"` / `"Classic"` substrings (`SQLiteProviderDetector.cs:87-92`).

### SQL builder

`SQLiteSqlBuilder` (`Internal/DataProvider/SQLite/SQLiteSqlBuilder.cs`) is a single flat class -- no version subclasses. It extends `BasicSqlBuilder` ([SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)).

Key overrides:

- **Identifier quoting**: `[name]` brackets for fields, aliases, tables, CTEs (`SQLiteSqlBuilder.cs:79-104`). Schema-qualified names split on `.` and re-join with `].[`.
- **Namespace / schema emitted**: the `BuildObjectName` override maps `IsTemporaryOptionSet()` to the `temp.` prefix, and `Database` to an attached-DB name -- SQLite has no server-level schemas (`SQLiteSqlBuilder.cs:148-158`).
- **LIMIT/OFFSET**: `LIMIT {0}` / `OFFSET {0}`. `IsSkipSupported = false`, `IsSkipSupportedIfTake = true` (`SQLiteDataProvider.cs:42-43`).
- **Identity**: `PRIMARY KEY AUTOINCREMENT` attribute; truncate-with-identity-reset sends `UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME=<table>` as command 2 (`SQLiteSqlBuilder.cs:47-53`).
- **PK constraint**: when an identity column exists, removes the trailing `CONSTRAINT ... PRIMARY KEY (...)` clause and relies on `PRIMARY KEY AUTOINCREMENT` alone (`SQLiteSqlBuilder.cs:122-137`).
- **Upsert**: `BuildInsertOrUpdateQuery` delegates to `BuildInsertOrUpdateQueryAsOnConflictUpdateOrNothing` (`SQLiteSqlBuilder.cs:196-199`), emitting `INSERT OR REPLACE / INSERT OR NOTHING` via the `ON CONFLICT` variant. No `MERGE` support -- `BuildMergeStatement` throws `LinqToDBException` (`SQLiteSqlBuilder.cs:166-168`).
- **VALUES table**: SQLite does not support `VALUES(...) AS alias(cols)` syntax; the builder emits a `SELECT ... UNION ALL SELECT ...` structure instead (`SQLiteSqlBuilder.cs:209-246`).
- **IS DISTINCT**: SQLite 3.39.0+ supports standard `DISTINCT FROM`, but the builder still uses `IS` / `IS NOT` syntax for backwards compatibility, with the polarity inverted (`SQLiteSqlBuilder.cs:201-207`).
- **Materialized CTE hint**: `SupportsMaterializedCteHint = true` (`SQLiteSqlBuilder.cs:32`).
- **`SupportsColumnAliasesInSource`**: `false` (`SQLiteSqlBuilder.cs:30`).

### SQL optimizer

`SQLiteSqlOptimizer` (`Internal/DataProvider/SQLite/SQLiteSqlOptimizer.cs`) is a thin wrapper over `BasicSqlOptimizer` ([SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)):

- `RequiresCastingParametersForSetOperations = false` (`SQLiteSqlOptimizer.cs:15`).
- `TransformStatement` rewrites `DELETE` via `GetAlternativeDelete` (correlated subquery form), and `UPDATE` via `GetAlternativeUpdatePostgreSqlite` when `IsUpdateFromSupported`, else `GetAlternativeUpdate` (`SQLiteSqlOptimizer.cs:28-46`).
- `CreateConvertVisitor` returns `SQLiteSqlExpressionConvertVisitor`.

### Expression convert visitor

`SQLiteSqlExpressionConvertVisitor` (`Internal/DataProvider/SQLite/SQLiteSqlExpressionConvertVisitor.cs`) handles three areas:

- **String concatenation**: `+` on `string` -> `||` (`SQLiteSqlExpressionConvertVisitor.cs:21`).
- **XOR emulation**: `^` -> `(a + b) - (a & b) * 2` (`SQLiteSqlExpressionConvertVisitor.cs:23-29`).
- **Case-sensitive LIKE**: when `SearchString.CaseSensitive = true`, wraps the LIKE with a conjunction of `Substr`/`InStr` predicates for `StartsWith`/`EndsWith`/`Contains` (`SQLiteSqlExpressionConvertVisitor.cs:34-101`).
- **DateTime comparisons**: wraps operands in `strftime('%Y-%m-%d %H:%M:%f', expr)` casts when either side is a date/datetime type (`SQLiteSqlExpressionConvertVisitor.cs:132-168`).
- **CAST of Guid**: strips the `CAST` entirely -- SQLite has no GUID affinity, so casting would infer the wrong affinity (`SQLiteSqlExpressionConvertVisitor.cs:189-192`).
- **DateTime/DateOnly conversions**: routes through `Date(expr)` for date-only or `strftime('%Y-%m-%d %H:%M:%f', expr)` for datetime, wrapped in `DoNotOptimize = true` guards to prevent double-wrapping (`SQLiteSqlExpressionConvertVisitor.cs:173-196`).

### Type mapping and dynamic typing

SQLite has only five storage classes: INTEGER, REAL, TEXT, BLOB, NUMERIC. Type affinity is applied to columns, not values. This creates asymmetric handling between the two clients.

**System.Data.SQLite**: reads the column type name from the `CREATE TABLE` statement and uses it to infer the reader method. This works correctly with the standard `GetGuid(i)` call registered in `SQLiteDataProvider.cs:157`.

**Microsoft.Data.Sqlite**: returns values in only four .NET types (`long`, `double`, `string`, `byte[]`). The data provider registers an extensive `SetSqliteField` map (`SQLiteDataProvider.cs:113-154`) covering ~40 SQL type-name -> reader-expression entries, split by `fieldType` (the .NET type the provider reports) and `typeName` (from `GetDataTypeName()`). The multi-path `SetSqliteField` helper also registers `byte[]` and `int` (v1 provider) fallback paths for every combination (`SQLiteDataProvider.cs:168-184`). `AlwaysCheckDbNull = true` is the default (`SQLiteOptions.cs:23`) because the provider may misreport nullability; `IsDBNullAllowed` consults this flag before deferring to the base class (`SQLiteDataProvider.cs:232-238`).

**Guid storage**: controlled by `DataType`/`DbType` at time of parameter setting. TEXT-affinity types (Char, VarChar, NVarChar, NChar, NText, Text, or `DbType = "TEXT"`) produce uppercase string form; all others produce binary (`guid.ToByteArray()`) (`SQLiteDataProvider.cs:247-276`). `SQLiteMappingSchema` mirrors this logic in `ConvertGuidToSql` (`SQLiteMappingSchema.cs:41-58`).

**DateTime storage**: always serialised as `"yyyy-MM-dd HH:mm:ss.fff"` string, parameter type forced to `VarChar` for the classic driver (`SQLiteDataProvider.cs:279-283`). `DateOnly` with `DataType.Date` is formatted as `"yyyy-MM-dd"` (`SQLiteDataProvider.cs:288-296`). The classic driver does not support `DateOnly` natively (`SupportsDateOnly = false`).

**Type normalisation**: `NormalizeTypeName` strips length/precision facets (e.g. `VARCHAR(255)` -> `VARCHAR`) to match reader-expression keys (`SQLiteDataProvider.cs:186-196`).

**Parameter type coercion**: `UInt32` -> `Int64`, `UInt64` -> `Decimal`, `DateTime2` -> `DateTime` (`SQLiteDataProvider.cs:304-312`).

### Mapping schema

`SQLiteMappingSchema` (`Internal/DataProvider/SQLite/SQLiteMappingSchema.cs`) extends `LockedMappingSchema` ([MAPPING](../MAPPING/INDEX.md)). Two child schemas `ClassicMappingSchema` and `MicrosoftMappingSchema` set `ProviderName.SQLiteClassic` / `ProviderName.SQLiteMS` as identifiers but inherit all converters from the shared base.

Converters registered:

- `string` -> `TimeSpan`: parses via `DateTime.Parse(..., DateTimeStyles.NoCurrentDateDefault).TimeOfDay` (`SQLiteMappingSchema.cs:27`).
- `Guid` -> SQL: `ConvertGuidToSql` (binary or uppercase TEXT, see above) (`SQLiteMappingSchema.cs:29`).
- `DateTime` -> SQL: ISO format `'yyyy-MM-dd HH:mm:ss.fff'` or `'yyyy-MM-dd'` for `DataType.Date` (`SQLiteMappingSchema.cs:71-83`).
- `DateTimeOffset` -> SQL: ISO format `'yyyy-MM-dd HH:mm:ss.fffzzz'` via `ConvertDateTimeOffsetToSql` (`SQLiteMappingSchema.cs:31`). Format constant `DATETIMEOFFSET_FORMAT` at `SQLiteMappingSchema.cs:18/22`.
- `string`, `char` -> SQL: standard string-escape with `char()` for control characters (`SQLiteMappingSchema.cs:104-111`).
- `byte[]` / `Binary` -> SQL: hex blob literal `X'...'` (`SQLiteMappingSchema.cs:63-69`).
- `DateOnly` -> SQL: `'yyyy-MM-dd'` via `ConvertDateOnlyToSql` (conditional on `SUPPORTS_DATEONLY`) (`SQLiteMappingSchema.cs:38`).
- Default `string` data type: `NVarChar(255)` (`SQLiteMappingSchema.cs:41`).

### Bulk copy

`SQLiteBulkCopy` (`Internal/DataProvider/SQLite/SQLiteBulkCopy.cs`) extends `BasicBulkCopy` ([INTERNAL-API](../INTERNAL-API/INDEX.md)). SQLite has no native bulk-copy ADO.NET feature; the only mode implemented is `MultipleRows` -> `MultipleRowsCopy1` / `MultipleRowsCopy1Async`.

Limits: `MaxParameters = 998` (SQLite limit is 999; one subtracted for potential provider parameter, `SQLiteBulkCopy.cs:15`); `MaxSqlLength = 1_000_000` (from https://www.sqlite.org/limits.html, `SQLiteBulkCopy.cs:20`).

`GetInsertInto` overrides the `INSERT` prefix to `INSERT OR IGNORE INTO` when `ConflictAction.Ignore` is set (`SQLiteBulkCopy.cs:22-28`). Other conflict actions use the base `INSERT INTO`.

`SQLiteOptions.BulkCopyType` defaults to `BulkCopyType.MultipleRows`. The data provider's `BulkCopy` / `BulkCopyAsync` dispatch resolves the effective type from `SQLiteOptions.Default` when `BulkCopyType.Default` is requested (`SQLiteDataProvider.cs:316-351`).

### Schema provider

`SQLiteSchemaProvider` (`Internal/DataProvider/SQLite/SQLiteSchemaProvider.cs`) extends `SchemaProviderBase` ([INTERNAL-API](../INTERNAL-API/INDEX.md)). All metadata is obtained through `pragma_table_list()`, `pragma_table_info()`, `pragma_foreign_key_list()`, and `pragma_index_list()` table-valued functions -- no `information_schema`, no `sys.*` tables.

Key behaviours:

- **Tables**: queries `pragma_table_list()` filtering `type IN ('table', 'view')` and by default excludes `sqlite_sequence`, `sqlite_schema`, and the `temp` schema (`SQLiteSchemaProvider.cs:82-95`).
- **Columns / identity detection**: identity is inferred from the combination of: single-column PK, column type `INTEGER`, absence of `PRIMARY KEY DESC` in DDL, plus either `AUTOINCREMENT` keyword or absence of `WITHOUT ROWID` (`SQLiteSchemaProvider.cs:155-160`). Length/precision/scale are extracted from the type name string because SQLite has no runtime type system (`SQLiteSchemaProvider.cs:166-170`).
- **Views**: `pragma_table_info` on views returns unreliable type information, so the provider falls back to executing `SELECT * FROM view` with `CommandBehavior.SchemaOnly` and reading the schema table (`SQLiteSchemaProvider.cs:188-225`).
- **Foreign keys**: joins `pragma_foreign_key_list` with `pragma_table_info` to resolve the "to" column when not explicitly stated (`SQLiteSchemaProvider.cs:231-251`).
- **Type inference**: `InferTypeInformation` strips facets from type names, looks up in `_typeMappings` (40+ entries), then falls back to `GetTypeByAffinity` which implements the five SQLite affinity rules (`SQLiteSchemaProvider.cs:288-380`). Unknown types map to `DataType.Variant` / `object` to avoid reader errors (`SQLiteSchemaProvider.cs:345`, `SQLiteSchemaProvider.cs:379`).
- `GetDataType` is not supported and throws `NotSupportedException` (`SQLiteSchemaProvider.cs:258-261`).

### Member translator

`SQLiteMemberTranslator` (`Internal/DataProvider/SQLite/Translation/SQLiteMemberTranslator.cs`) extends `ProviderMemberTranslatorDefault` ([INTERNAL-API](../INTERNAL-API/INDEX.md)) with four inner classes:

- **`DateFunctionsTranslator`**: translates `Sql.DateParts` via `strftime()` format codes (`%Y`, `%m`, `%d`, etc.). Quarter is computed as `((month - 1) / 3) + 1`; Millisecond as `CAST(strftime('%f', d) * 1000 AS int) % 1000`. Date arithmetic via `strftime(format, date, 'N unit')` modifiers. `MakeDateTime` constructs via zero-padded string concatenation then `strftime`.

  **`now` translation** (updated in PR #5467): four separate overrides produce distinct SQL:
  - `TranslateNow` / `TranslateServerNow` -> `DATETIME('now', 'localtime')` -- local wall-clock time (`SQLiteMemberTranslator.cs:226-231`). `TranslateServerNow` delegates to `TranslateNow` (`SQLiteMemberTranslator.cs:221-224`).
  - `TranslateUtcNow` -> `CURRENT_TIMESTAMP` -- UTC epoch reference (`SQLiteMemberTranslator.cs:233-238`).
  - `TranslateZonedNow` -> `DATETIME('now', 'localtime')` (`SQLiteMemberTranslator.cs:240-244`).
  - `TranslateZonedUtcNow` -> `CURRENT_TIMESTAMP` (`SQLiteMemberTranslator.cs:246-250`).

  **`DateTime.Date` truncation** (PR #5517): `TranslateDateTimeTruncationToDate` emits `Date(expr)` preserving the column's `DbDataType` from the input expression -- avoids the prior bug where the result type was incorrectly recast (`SQLiteMemberTranslator.cs:202-209`).

  **Time truncation**: `TranslateDateTimeTruncationToTime` emits `strftime('%H:%M:%f', expr)` (`SQLiteMemberTranslator.cs:212-218`).

- **`StringMemberTranslator`**: `LPad` emulated with `ZEROBLOB` + `HEX` + `REPLACE` + `SUBSTR`; `String.Join` uses `GROUP_CONCAT(value, separator)` with filter support.
- **`GuidMemberTranslator`**: `Guid.ToString()` emulated by reassembling `hex(blob)` substrings into UUID format with `lower()`.
- **`SqlTypesTranslation`**: delegates to `SqlTypesTranslationDefault` (no SQLite-specific overrides).

### Hints

`SQLiteHints` (`DataProvider/SQLite/SQLiteHints.cs`): two table hints:

- `NOT INDEXED` -- suppresses index usage for a table scan.
- `INDEXED BY <index>` -- forces a specific index.

Both are applied via `ISQLiteSpecificTable<TSource>.TableHint(hint)` using `Sql.QueryExtension` with `HintExtensionBuilder`. These are the only hints SQLite supports at the ORM level; query-level optimizer hints (e.g. `PRAGMA`) are outside the hint system.

## Key types

| Type | File | Role |
|---|---|---|
| `SQLiteDataProvider` | `Internal/DataProvider/SQLite/SQLiteDataProvider.cs` | Abstract base; reader-expression setup, param serialisation |
| `SQLiteDataProviderClassic` | (same file) | Concrete: System.Data.SQLite |
| `SQLiteDataProviderMS` | (same file) | Concrete: Microsoft.Data.Sqlite |
| `SQLiteSqlBuilder` | `Internal/DataProvider/SQLite/SQLiteSqlBuilder.cs` | SQL emission; single flat class |
| `SQLiteSqlOptimizer` | `Internal/DataProvider/SQLite/SQLiteSqlOptimizer.cs` | Statement rewrite |
| `SQLiteSqlExpressionConvertVisitor` | `Internal/DataProvider/SQLite/SQLiteSqlExpressionConvertVisitor.cs` | Expression-level rewrites |
| `SQLiteMappingSchema` | `Internal/DataProvider/SQLite/SQLiteMappingSchema.cs` | Type conversion / SQL literals |
| `SQLiteProviderAdapter` | `Internal/DataProvider/SQLite/SQLiteProviderAdapter.cs` | Dynamic assembly load; connection factory |
| `SQLiteProviderDetector` | `Internal/DataProvider/SQLite/SQLiteProviderDetector.cs` | Auto-detect System vs Microsoft |
| `SQLiteBulkCopy` | `Internal/DataProvider/SQLite/SQLiteBulkCopy.cs` | Multi-row INSERT only |
| `SQLiteSchemaProvider` | `Internal/DataProvider/SQLite/SQLiteSchemaProvider.cs` | `pragma_*` based schema discovery |
| `SQLiteMemberTranslator` | `Internal/DataProvider/SQLite/Translation/SQLiteMemberTranslator.cs` | Date/string/Guid function translation |
| `SQLiteTools` | `DataProvider/SQLite/SQLiteTools.cs` | Public entry: `GetDataProvider`, `CreateDataConnection`, file DB utilities |
| `SQLiteOptions` | `DataProvider/SQLite/SQLiteOptions.cs` | `BulkCopyType`, `AlwaysCheckDbNull` options |
| `SQLiteProvider` | `DataProvider/SQLite/SQLiteProvider.cs` | Enum: `AutoDetect`, `System`, `Microsoft` |
| `SQLiteHints` | `DataProvider/SQLite/SQLiteHints.cs` | `INDEXED BY` / `NOT INDEXED` table hints |
| `SQLiteExtensions` | `DataProvider/SQLite/SQLiteExtensions.cs` | FTS3/4/5 functions, rowid, rank access |
| `SQLiteSpecificExtensions` | `DataProvider/SQLite/SQLiteSpecificExtensions.cs` | `.AsSQLite()` queryable wrapper |
| `ISQLiteSpecificTable<T>` | `DataProvider/SQLite/ISQLiteSpecificTable.cs` | Marker interface for hint chaining |
| `ISQLiteExtensions` | `DataProvider/SQLite/ISQLiteExtensions.cs` | Extension-point marker interface |
| `SQLiteSpecificTable<T>` | `Internal/DataProvider/SQLite/SQLiteSpecificTable.cs` | `DatabaseSpecificTable` + `ISQLiteSpecificTable` |
| `SQLiteFactory` | `DataProvider/SQLite/SQLiteFactory.cs` | `IDataProviderFactory` for config-based resolution |

## Full-text search extensions

`SQLiteExtensions` provides LINQ-accessible wrappers for FTS3/4/5 features:

- `Match(entityOrColumn, query)` -- `table MATCH 'query'` or `column MATCH 'query'` (`SQLiteExtensions.cs:33`).
- `MatchTable<T>(table, query)` -- FTS5 table-function syntax `table('query')` (`SQLiteExtensions.cs:52`).
- `RowId<T>(entity)` -- access to hidden `rowid` column (`SQLiteExtensions.cs:73`).
- `Rank<T>(entity)` -- FTS5 `rank` hidden column (`SQLiteExtensions.cs:93`).
- FTS3/4 auxiliary functions: `FTS3Offsets`, `FTS3MatchInfo` (two overloads), `FTS3Snippet` (six overloads).
- FTS5 auxiliary functions: `FTS5bm25` (two overloads), `FTS5Highlight`, `FTS5Snippet`.
- FTS3/4 admin commands (synchronous + async): `FTS3Optimize`, `FTS3Rebuild`, `FTS3IntegrityCheck`, `FTS3Merge`, `FTS3AutoMerge`.
- FTS5 admin commands (synchronous + async): `FTS5AutoMerge`, `FTS5CrisisMerge`, `FTS5Delete`, `FTS5DeleteAll`, `FTS5IntegrityCheck`, `FTS5Merge`, `FTS5Optimize`, `FTS5Pgsz`, `FTS5Rank`, `FTS5Rebuild`, `FTS5UserMerge`.

All function wrappers use `[ExpressionMethod]` and `Sql.Expr<T>` for server-side-only evaluation. Admin commands use raw `dc.Execute()` / `dc.ExecuteAsync()`.

## Supported table options

`SupportedTableOptions` (`SQLiteDataProvider.cs:198-203`): `IsTemporary`, `IsLocalTemporaryStructure`, `IsLocalTemporaryData`, `CreateIfNotExists`, `DropIfExists`. Temporary tables emit `CREATE TEMPORARY TABLE` (`SQLiteSqlBuilder.cs:173-192`).

## SqlProviderFlags notable values

| Flag | Value | Source |
|---|---|---|
| `IsSkipSupported` | `false` | `SQLiteDataProvider.cs:42` |
| `IsSkipSupportedIfTake` | `true` | `SQLiteDataProvider.cs:43` |
| `IsCommonTableExpressionsSupported` | `true` | `SQLiteDataProvider.cs:44` |
| `IsUnionAllOrderBySupported` | `true` | `SQLiteDataProvider.cs:46` |
| `IsDistinctFromSupported` | `true` (3.39.0+) | `SQLiteDataProvider.cs:47` |
| `SupportsPredicatesComparison` | `true` | `SQLiteDataProvider.cs:48` |
| `DefaultMultiQueryIsolationLevel` | `Serializable` | `SQLiteDataProvider.cs:49` |
| `RowConstructorSupport` | Equality, Comparisons, UpdateLiteral, CompareToSelect, Between, Update | `SQLiteDataProvider.cs:64-65` |
| `SupportedCorrelatedSubqueriesLevel` | `null` (unlimited) | `SQLiteDataProvider.cs:61` |

## Files (Tier 1 / Tier 2)

### Tier 1 (10 files)

| File | Purpose |
|---|---|
| `Internal/DataProvider/SQLite/SQLiteDataProvider.cs` | Provider base + two concrete subtypes |
| `Internal/DataProvider/SQLite/SQLiteSqlBuilder.cs` | SQL emission |
| `Internal/DataProvider/SQLite/SQLiteSqlOptimizer.cs` | Statement rewrite |
| `Internal/DataProvider/SQLite/SQLiteProviderAdapter.cs` | Dynamic ADO.NET load |
| `Internal/DataProvider/SQLite/SQLiteProviderDetector.cs` | Auto-detect + singletons |
| `Internal/DataProvider/SQLite/SQLiteMappingSchema.cs` | SQL literals + type converters |
| `Internal/DataProvider/SQLite/SQLiteBulkCopy.cs` | Multi-row INSERT bulk copy |
| `DataProvider/SQLite/SQLiteTools.cs` | Public API entry point |
| `DataProvider/SQLite/SQLiteProvider.cs` | ADO.NET provider enum |
| `DataProvider/SQLite/SQLiteOptions.cs` | Provider options record |

### Tier 2 (10 files)

| File | Purpose |
|---|---|
| `Internal/DataProvider/SQLite/SQLiteSchemaProvider.cs` | `pragma_*` schema discovery |
| `Internal/DataProvider/SQLite/SQLiteSqlExpressionConvertVisitor.cs` | Expression rewrites (XOR, LIKE, datetime) |
| `Internal/DataProvider/SQLite/Translation/SQLiteMemberTranslator.cs` | Date/string/Guid member translation |
| `Internal/DataProvider/SQLite/SQLiteSpecificTable.cs` | `ISQLiteSpecificTable` impl |
| `DataProvider/SQLite/SQLiteHints.cs` | `INDEXED BY` / `NOT INDEXED` hints |
| `DataProvider/SQLite/SQLiteExtensions.cs` | FTS3/4/5 LINQ extensions + admin commands |
| `DataProvider/SQLite/SQLiteSpecificExtensions.cs` | `.AsSQLite()` wrapper |
| `DataProvider/SQLite/ISQLiteSpecificTable.cs` | Marker interface |
| `DataProvider/SQLite/ISQLiteExtensions.cs` | Extension marker interface |
| `DataProvider/SQLite/SQLiteFactory.cs` | Config-based factory |

## Inbound / outbound dependencies

**Inbound**: user code calls `SQLiteTools.GetDataProvider` / `CreateDataConnection`; EFCore companion may call the factory via `IDataProviderFactory`. `SQLiteFactory` is resolved by `DataProviderFactoryBase` on config-string match.

**Outbound**:
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md): `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`.
- [INTERNAL-API](../INTERNAL-API/INDEX.md): `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `SchemaProviderBase`, `TypeMapper`, `MemberTranslatorBase` (via `ProviderMemberTranslatorDefault`).
- [MAPPING](../MAPPING/INDEX.md): `LockedMappingSchema`.

## Known issues / debt

- `UPDATE TAKE/SKIP` is commented out (`SQLiteDataProvider.cs:52-59`): the flag `IsUpdateTakeSupported` / `IsUpdateSkipTakeSupported` is intentionally disabled because Microsoft.Data.Sqlite's runtime does not enable the needed SQLite compilation flag. System.Data.SQLite has it, but enabling it for only one provider adds no value when the other can't use it.
- `IS DISTINCT` emulation is kept as `IS` / `IS NOT` rather than migrating to standard `DISTINCT FROM` (SQLite 3.39.0+). The comment (`SQLiteSqlBuilder.cs:201`) says "keep older implementation for now".
- SQLite does not support `MERGE`; any attempt to use `MergeStatement` throws at runtime (`SQLiteSqlBuilder.cs:166-168`). No compile-time guard exists on `SqlProviderFlags`.
- `GetDataType` in `SQLiteSchemaProvider` throws `NotSupportedException` -- it is expected not to be called because type inference bypasses the base infrastructure entirely (`SQLiteSchemaProvider.cs:258-261`).
- FTS return types (e.g. `FTS5Delete` column list construction, `SQLiteExtensions.cs:630-635`) use `DataParameter.VarChar` hardcoded for all columns -- FTS tables are always TEXT-typed but the code bypasses any provider-specific parameter binding.
- `// TODO: V7: update applicable methods to return affected rows count instead of void/Task` (`SQLiteExtensions.cs:1`) -- bulk FTS command methods return `void`/`Task` rather than affected-row counts.

## See also

- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer` bases
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `BasicBulkCopy`, `SchemaProviderBase`, `TypeMapper`
- [MAPPING](../MAPPING/INDEX.md) -- `LockedMappingSchema`, `MappingSchema`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 10 / 10
- Tier 2 (visited / total): 10 / 10 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta sha 4a478ff14):
- `SQLiteMemberTranslator.cs` -- Now-translation split into 4 virtuals (PR #5467): TranslateNow/TranslateServerNow emit `DATETIME('now', 'localtime')`; TranslateUtcNow/TranslateZonedUtcNow emit `CURRENT_TIMESTAMP`. TranslateDateTimeTruncationToDate emits `Date(expr)` preserving DbDataType (PR #5517).
- `SQLiteMappingSchema.cs` -- `ConvertDateTimeOffsetToSql` registered with `DATETIMEOFFSET_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fffzzz}'"`.

</details>
