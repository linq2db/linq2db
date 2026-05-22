---
area: PROV-CLICKHOUSE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 10/10
coverage_tier_2: 13/13
---

# PROV-CLICKHOUSE

ClickHouse is an OLAP column-store database with a single-dialect linq2db provider that routes through one of three ADO.NET clients. It is the most type-system-rich provider in the codebase: 50+ `DataType` cases, no transactions, no SQL MERGE, no bound parameters at query time, and three distinct bulk-copy strategies depending on which client is loaded.

## Subsystems

### Public surface (`Source/LinqToDB/DataProvider/ClickHouse/`)

**`ClickHouseProvider` enum** -- declares the three supported clients: `Octonica`, `ClickHouseDriver`, `MySqlConnector`, plus `AutoDetect`. This is the user-facing discriminator passed to `ClickHouseTools` and `ClickHouseProviderDetector`.

**`ClickHouseTools`** -- static registration entry point, parallel to `SqlServerTools`, `MySqlTools`, etc. Exposes `AutoDetectProvider` flag, `GetDataProvider(provider, connectionString, ...)`, and three `CreateDataConnection` overloads. Delegates everything to the `ClickHouseProviderDetector` singleton (`Source/LinqToDB/DataProvider/ClickHouse/ClickHouseTools.cs:10`).

**`ClickHouseOptions`** -- `DataProviderOptions<ClickHouseOptions>` record with two fields: `BulkCopyType` (default `ProviderSpecific`) and `UseStandardCompatibleAggregates` (default `false`). When `UseStandardCompatibleAggregates = true`, the optimizer rewrites `MIN`/`MAX`/`SUM`/`AVG` to their `minOrNull`/`maxOrNull`/`sumOrNull`/`avgOrNull` forms for SQL-standard NULL-propagation behavior.

**`ClickHouseSpecificExtensions`** -- provides `AsClickHouse<T>()` on `ITable<T>` and `IQueryable<T>`, returning `IClickHouseSpecificTable<T>` / `IClickHouseSpecificQueryable<T>` that unlock hint extension methods.

**`IClickHouseSpecificTable<T>` / `IClickHouseSpecificQueryable<T>`** -- marker interfaces; internal implementations are `ClickHouseSpecificTable<T>` and `ClickHouseSpecificQueryable<T>`.

**`ClickHouseHints`** (partial class, hand-written + T4-generated) -- hint constants and extension methods:
- `Table.Final = "FINAL"` -- the FINAL modifier applied to a single table (triggers merge of all parts for ReplacingMergeTree reads).
- `Join.*` -- 15 join-algorithm constants (OUTER, SEMI, ANTI, ANY, ASOF, GLOBAL \*, ALL \*) plus corresponding `JoinXHint()` extensions on both table and queryable.
- `Query.Settings = "SETTINGS"` and `SettingsHint()` / `QueryHint()` for appending `SETTINGS key=val` after the query.
- `FinalHint()`, `FinalInScopeHint()` for per-table or scope-wide FINAL application.
- `TablesInScopeHint()` for broadcast to all tables in a LINQ scope.
- The T4 template (`ClickHouseHints.tt`) generates `ClickHouseHints.generated.cs` with one typed overload per join variant for both queryable and table, keeping the hand-written file uncluttered.

**`ClickHouseRetryPolicy`** -- extends `RetryPolicyBase` with exponential backoff. Handles transient errors via `ClickHouseTransientExceptionDetector`. The `GetNextDelay` override halves the delay interval for error code `3` (ConnectionClosed / memory-optimized path). Mirrors the PROV-SQLSERVER `SqlServerRetryPolicy` pattern (`Source/LinqToDB/DataProvider/ClickHouse/ClickHouseRetryPolicy.cs:87`).

**`ClickHouseTransientExceptionDetector`** -- static class with a `ConcurrentDictionary<Type, Func<Exception, IEnumerable<int>>>` registry. Providers self-register their exception types during adapter construction. Currently only Octonica registers; error codes recognized as transient: `2` (InvalidConnectionState), `3` (ConnectionClosed), `16` (NetworkError). `ShouldRetryOn` also catches bare `TimeoutException`. MySqlConnector path relies on `DbExceptionTransientExceptionDetector` (ADO `IsTransient` flag, conditional on `ADO_IS_TRANSIENT` compile define) (`Source/LinqToDB/DataProvider/ClickHouse/ClickHouseTransientExceptionDetector.cs:43`).

**`ClickHouseFactory`** -- `DataProviderFactoryBase` implementation used by the connection-string factory registration pathway. Maps `assemblyName` attribute to `ClickHouseProvider` enum value (`Source/LinqToDB/DataProvider/ClickHouse/ClickHouseFactory.cs:17`).

### ADO.NET driver matrix

ClickHouse supports three ADO.NET client libraries, each with distinct capability levels:

| Aspect | Octonica (`Octonica.ClickHouseClient`) | ClickHouse.Driver (`ClickHouse.Driver`) | MySqlConnector (MySQL protocol) |
|---|---|---|---|
| Assembly | `Octonica.ClickHouseClient` | `ClickHouse.Driver` | `MySqlConnector` |
| Provider enum | `ClickHouseProvider.Octonica` | `ClickHouseProvider.ClickHouseDriver` | `ClickHouseProvider.MySqlConnector` |
| Bulk copy API | `ClickHouseColumnWriter` (column-oriented) | `ClickHouseBulkCopy` (v < 1.0) or `ClickHouseClient.InsertBinaryAsync` (v 1.0+) | None -- falls back to multi-row INSERT |
| Special types | `DateTimeOffset` readers, `byte[]`/`string` FixedString duality, `DateOnly` (conditional) | `ClickHouseDecimal` (256-bit), `GetIPAddress`, `GetBigInteger`, no DateTimeOffset reader | All values arrive as strings; requires NaN/Infinity parsing for float/double |
| UUID handling | `byte[16]` or `Guid.Parse` string | string `"D"` format | string `"D"` format |
| `IsDBNullAllowed` | Always `true` (Octonica bug workaround) | Schema table `AllowDBNull` column | Schema table |
| Parameters | Disabled (all parameters inlined as literals) | Disabled | Disabled |

`ClickHouseProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseProviderAdapter.cs`) wraps each client via the `TypeMapper`/`TypeWrapper` pattern (see [INTERNAL-API](../INTERNAL-API/INDEX.md)). For `MySqlConnector`, it delegates directly to `MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector)` and wraps the result (`ClickHouseProviderAdapter.cs:103`). Singletons are double-checked-locked per provider (`ClickHouseProviderAdapter.cs:23-28`).

**Driver version detection** -- `ClickHouse.Driver` v1.0 introduced `ClickHouseClient` / `InsertOptions` types. The adapter probes for them at load time and populates `CreateDriverClientFactory` / `CreateDriverInsertOptionsFactory` only when present (`ClickHouseProviderAdapter.cs:221-235`).

### Provider detection and MySql protocol pass-through

`ClickHouseProviderDetector` (`Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseProviderDetector.cs`) is the auto-detection implementation. Detection priority when `provider == AutoDetect`:

1. String-matching on `ProviderName` / `ConfigurationString`: strings containing both "Octonica" and "ClickHouse" -> Octonica; both "ClickHouse" and "MySql" -> MySqlConnector; "ClickHouse.Driver" -> ClickHouseDriver.
2. Assembly probe fallback: checks for `Octonica.ClickHouseClient.dll`, `ClickHouse.Driver.dll`, `MySqlConnector.dll` in the assembly directory in that priority order.

The cross-provider relationship with PROV-MYSQL: `ClickHouseProviderDetector` does **not** call into `MySqlProviderDetector`. Instead, when ClickHouse-over-MySQL is selected, the ClickHouse adapter wraps `MySqlProviderAdapter` directly (`ClickHouseProviderAdapter.cs:103-117`). The PROV-MYSQL `MySqlProviderDetector` reciprocally short-circuits on connection strings containing "ClickHouse" to avoid claiming those connections as MySQL (see [PROV-MYSQL](../PROV-MYSQL/INDEX.md)).

### SQL builder (`ClickHouseSqlBuilder`)

`ClickHouseSqlBuilder` extends `BasicSqlBuilder` with the following ClickHouse-specific behaviors:

**Identifiers** -- backtick quoting (`` ` ``) for non-ASCII-alphanumeric identifiers; FQN format is `database.table` (schema component not used; temporary tables omit the database prefix) (`ClickHouseSqlBuilder.cs:45-53`).

**Type name generation** -- `BuildDataTypeFromDataType` -> `BuildTypeName`: produces ClickHouse-native type strings (`UInt8`, `Int256`, `Decimal128(scale)`, `DateTime64(precision)`, `IPv4`, `UUID`, etc.). Nullable columns wrap the inner type: `Nullable(T)` -- except JSON, which has a separate nullable syntax (`ClickHouseSqlBuilder.cs:135`). Enum8/Enum16 columns must have `DbType` set explicitly; type-name generation throws (`ClickHouseSqlBuilder.cs:180`).

**DDL** -- `BuildEndCreateTableStatement` appends `ENGINE = MergeTree() ORDER BY (pk)` for tables with a primary key, or `ENGINE = Memory()` otherwise (`ClickHouseSqlBuilder.cs:198-228`). `CREATE TEMPORARY TABLE` is used for all temporary variants. `DROP TABLE IF EXISTS` is supported.

**DML mutations** -- ClickHouse uses `ALTER TABLE ... DELETE WHERE ...` and `ALTER TABLE ... UPDATE SET ... WHERE ...` (not standard `DELETE/UPDATE`). Table aliases are disabled for these statements because ClickHouse ALTER mutations do not support aliases (`ClickHouseSqlBuilder.cs:291`, `ClickHouseSqlBuilder.cs:341`). An explicit guard throws on correlated DELETE/UPDATE (joins not supported in mutation context) via `ErrorHelper.ClickHouse.Error_CorrelatedDelete` / `Error_CorrelatedUpdate`.

**MERGE** -- `BuildMergeStatement` throws unconditionally; ClickHouse has no SQL MERGE (`ClickHouseSqlBuilder.cs:31`).

**Parameters** -- `BuildParameter` throws unconditionally; all query parameters are inlined by `ClickHouseSqlOptimizer.DisableParameters` before the builder runs (`ClickHouseSqlBuilder.cs:32`).

**LIMIT/OFFSET** -- `LIMIT skip, take` form (comma-separated); when TAKE is absent but SKIP is present, emits `18446744073709551615` (UInt64 max) as the effective unlimited take (`ClickHouseSqlBuilder.cs:408`).

**CTE** -- column list syntax is not supported (ClickHouse GitHub issue #22932); `FixCteAliases` in the optimizer propagates SELECT column aliases directly onto the CTE body columns (`ClickHouseSqlOptimizer.cs:65`). `MATERIALIZED` CTE hint is supported; `NOT MATERIALIZED` is not (`ClickHouseSqlBuilder.cs:424-425`). `RECURSIVE` keyword required.

**INSERT FROM SELECT** -- CTE (`WITH` clause) is repositioned to appear before the SELECT clause rather than before INSERT, matching ClickHouse's grammar (`ClickHouseSqlBuilder.cs:433`).

**Set operations** -- `UNION` maps to `UNION DISTINCT`, `EXCEPT` to `EXCEPT DISTINCT`, `INTERSECT` to `INTERSECT DISTINCT`; all-variants are supported (`ClickHouseSqlBuilder.cs:484`).

**Join hints** -- `BuildJoinType` inspects the last `JoinHint` extension on a join and prepends `GLOBAL` or `ALL` qualifiers, then emits the standard join keyword (`ClickHouseSqlBuilder.cs:517`).

**Table hints** -- `BuildTableExtensions` emits `TableHint` and `TablesInScopeHint` extensions with `" "` (space) as the prefix separator before the first hint token (`ClickHouseSqlBuilder.cs:512`). PR #5449 fixed a missing space between the table name and the hint keyword (e.g. `FINAL`) that caused malformed SQL when a `TableHint` was applied.

**DISTINCT predicate** -- uses the IS DISTINCT fallback implementation.

**InsertOrUpdate** -- the provider flag `IsInsertOrUpdateSupported = true` is set deliberately, but the SQL builder and optimizer do not implement it -- the intent is to let the flag flow into query pipeline diagnostics rather than generate broken SQL. The comment in `ClickHouseDataProvider.cs:46` states "we enable InsertOrUpdate deliberately here and then throw exception from SqlBuilder".

### SQL optimizer (`ClickHouseSqlOptimizer`)

`ClickHouseSqlOptimizer` extends `BasicSqlOptimizer`. Key transformations in `FinalizeStatement`:

1. **`DisableParameters`** -- converts every `SqlParameter` to an inline literal by setting `IsQueryParameter = false`. This is unconditional; no provider configuration overrides it (`ClickHouseSqlOptimizer.cs:47`).
2. **`FixCteAliases`** -- copies CTE field names into the SELECT column aliases of the CTE body, and sets `DoNotSetAliases = true` to block later rewriting (`ClickHouseSqlOptimizer.cs:65`).
3. **`CorrectUpdateSetters`** -- standard basic-optimizer update-setter normalization.

The optimizer creates `ClickHouseSqlExpressionConvertVisitor` via `CreateConvertVisitor`, passing the `ClickHouseOptions` instance to enable `UseStandardCompatibleAggregates` checks at visitor creation time.

### Expression conversion visitor (`ClickHouseSqlExpressionConvertVisitor`)

Key rewrites:

- **Bitwise operators** -- `|` -> `bitOr`, `&` -> `bitAnd`, `^` -> `bitXor`; unary bitwise NOT -> `bitNot`; unary negation -> `negate` (`ClickHouseSqlExpressionConvertVisitor.cs:107`).
- **Decimal arithmetic** -- `%` and `/` on Decimal types cast operands to `Double` first, then cast the result back (ClickHouse issue #39287) (`ClickHouseSqlExpressionConvertVisitor.cs:120`).
- **String concatenation** -- `+` on strings is flattened into an n-ary `concat(...)` call.
- **LIKE** -- `ESCAPE` clause stripped (not supported); `%` and `_` are the escape characters.
- **`startsWith` / `endsWith` / `Contains`** -- mapped to `startsWith`, `endsWith`, and `position` / `positionCaseInsensitive` functions.
- **Aggregate functions** -- when `UseStandardCompatibleAggregates` is set and the result can be nullable, `MIN`/`MAX`/`SUM`/`AVG` are renamed to `minOrNull`/`maxOrNull`/`sumOrNull`/`avgOrNull`.
- **Type casts** -- `ClickHouseConvertFunctions` dictionary maps each `DataType` to its ClickHouse `toXxx` function name (37 entries); `TRY_CONVERT` pseudofunction maps to the `toXxxOrNull` variant. For `Decimal*` and `DateTime64` the scale/precision parameter is appended as a second argument.
- **TRIM for FixedString->String** -- wraps in `TRIM(TRAILING '\x00' FROM ...)`.
- **Interval nulls** -- skip the CAST wrapping for `NULL` literals of interval types (ClickHouse does not support `NULL` literals in interval contexts) (`ClickHouseSqlExpressionConvertVisitor.cs:462`).

### Mapping schema (`ClickHouseMappingSchema`)

`ClickHouseMappingSchema` is a `LockedMappingSchema` with three concrete sub-schemas:

- **`OctonicaMappingSchema`** -- adds `byte[]`->`Guid` and `DateTimeOffset`->`DateTime` (UTC extraction) conversions for Octonica wire-format quirks.
- **`ClientMappingSchema`** -- adds `ClickHouseDecimal`->SQL converter for the `ClickHouse.Driver` 256-bit decimal type; incorporates the `DriverDecimalType` mapping schema emitted by the adapter.
- **`MySqlMappingSchema`** -- adds `string`->many numeric converters to handle ClickHouse's habit of returning `"nan"` strings for float NaN via the MySQL protocol (ClickHouse issue #39297) (`ClickHouseMappingSchema.cs:906`).

Default scalar mappings of note:
- `DateTime` / `DateTimeOffset` -> `DateTime64(7)` (precision 7).
- `decimal` -> `Decimal128(10)` (precision 29, scale 10).
- `TimeSpan` -> `IntervalSecond`.
- `IPAddress` -> `IPv6`.
- `BigInteger` -> `Int256`.
- `DateOnly` -> `Date32` (conditional on `SUPPORTS_DATEONLY`).

Literal generators produce typed function-call syntax: `toUInt8(n)`, `toDecimal128('n', scale)`, `toDate32('yyyy-MM-dd')`, `toIPv4('a.b.c.d')`, etc. String literals use single-quote with backslash-escape for `'` and `\`.

`DEFAULT_DATETIME64_PRECISION = 7`, `DEFAULT_DECIMAL_SCALE = 10`, `DEFAULT_FIXED_STRING_LENGTH = 100` are the provider-wide defaults (`ClickHouseMappingSchema.cs:23`).

### Bulk copy (`ClickHouseBulkCopy`)

Three `ProviderSpecific` paths, selected at runtime by presence of adapter factory methods:

1. **Octonica `ClickHouseColumnWriter`** -- columnar streaming via `CreateColumnWriter` / `CreateColumnWriterAsync`. Batches rows using `EnumerableHelper.Batch`, builds per-column typed lists, calls `WriteTable` / `WriteTableAsync` per batch, finalizes with `EndWrite` / `EndWriteAsync`. Configures `ClickHouseColumnSettings` for `byte[]` and `string`/enum FixedString columns. Supports `IEnumerable<T>` and `IAsyncEnumerable<T>` (`ClickHouseBulkCopy.cs:153`).
2. **`ClickHouse.Driver` v1.0+ `ClickHouseClient.InsertBinaryAsync`** -- creates a `ClickHouseClient` from the connection string directly, calls `InsertBinaryAsync(table, columns, rows, options, ct)`. Respects `MaxBatchSize`, `MaxDegreeOfParallelism`, `WithoutSession`, `BulkCopyTimeout` via `InsertOptions` (`ClickHouseBulkCopy.cs:594`).
3. **`ClickHouse.Driver` < 1.0 `ClickHouseBulkCopy`** -- wraps the driver's `ClickHouseBulkCopy` class, uses `WriteToServerAsync(IDataReader, ct)`. Supports `WithoutSession` by rebuilding the connection without the `UseSession` flag (`ClickHouseBulkCopy.cs:496`).
4. **Multi-row INSERT fallback** (`MultipleRowsCopy1`) -- used for MySqlConnector and when provider-specific paths fail to initialize. Standard `INSERT INTO t (cols) VALUES (r1), (r2), ...` batch strategy inherited from `BasicBulkCopy`.

The default `BulkCopyType` from `ClickHouseOptions` is `ProviderSpecific`, which selects path 1-3 depending on provider.

### Schema provider (`ClickHouseSchemaProvider`)

Queries `system.tables` and `system.columns` (the ClickHouse system catalog) using LINQ-mapped entities. The `Table` entity maps to `[Table("tables", Database = "system")]` and the `Column` entity to `[Table("columns", Database = "system")]` (`ClickHouseSchemaProvider.cs:205`, `ClickHouseSchemaProvider.cs:217`). Scope is always the current database (`database()` function); cross-database schema loading is not implemented.

Type parsing: `PreParseTypeName` strips `LowCardinality(...)` and `Nullable(...)` wrappers before looking up the type in `_typeMap`. `GetTypeMapping` handles fixed-name types, plus parametric types with `StartsWith` matching (Enum8/16, FixedString, DateTime64, Decimal32/64/128/256, Decimal with precision-based variant selection) (`ClickHouseSchemaProvider.cs:131`).

Foreign keys, stored procedures, and functions are not supported. Views are detected by checking whether `engine` ends with `"View"`. Primary keys are read from the `primary_key` column (comma-separated field list) of `system.tables` (`ClickHouseSchemaProvider.cs:57`).

### Member translator (`ClickHouseMemberTranslator`)

Extends `ProviderMemberTranslatorDefault` with:

- **Date functions** -- `toYear`, `toQuarter`, `toMonth`, `toDayOfYear`, `toDayOfMonth`, `toISOWeek`, `toHour`, `toMinute`, `toSecond`, `toDayOfWeek` (+1 for ISO weekday), `toUnixTimestamp64Milli % 1000` for milliseconds. Date arithmetic via `addYears/Months/Quarters/Weeks/Days/Hours/Minutes/Seconds`; millisecond `addDays` converts through `toUnixTimestamp64Nano` / `fromUnixTimestamp64Nano` (`ClickHouseMemberTranslator.cs:110`).
- **`MakeDateTime`** -- maps to `makeDateTime(y,m,d,H,M,S)` or `makeDateTime64(...)` with milliseconds.
- **Truncation to date** -- `TranslateDateTimeTruncationToDate` and `TranslateDateTimeOffsetTruncationToDate` both cast to `DataType.Date32` (`ClickHouseMemberTranslator.cs:170-179`). The `DateTimeOffset` override is a separate method but produces an identical `Date32` cast (PR #5517 was the context for this path).
- **Truncation to time** -- complex expression: `toInt64((toUnixTimestamp64Nano(toDateTime64(t, 7)) - toUnixTimestamp64Nano(toDateTime64(toDate32(t), 7))) / 100)` yielding TimeSpan ticks (`ClickHouseMemberTranslator.cs:183`).
- **`now()` translation** (PR #5467):
  - `TranslateNow` -- emits `now()` with no arguments and `ParametersNullabilityType.NotNullable` (`ClickHouseMemberTranslator.cs:227`). Used for `Sql.GetDate()` / `DateTime.Now`.
  - `TranslateServerNow` -- delegates directly to `TranslateNow` (i.e. emits `now()`), with an inline comment explaining that `CURRENT_TIMESTAMP` is a ClickHouse alias for `now()` but triggers parser bugs in some CH versions (`ClickHouseMemberTranslator.cs:219`). This avoids the base class default of emitting `CURRENT_TIMESTAMP`.
  - `TranslateUtcNow` -- emits `now('UTC')` with a `"UTC"` string argument (`ClickHouseMemberTranslator.cs:233`). Used for `DateTime.UtcNow` / `Sql.CurrentTimestampUtc`.
  - `TranslateZonedNow` -- emits `now()` without arguments, using the provided `dbDataType` (`ClickHouseMemberTranslator.cs:240`).
  - `TranslateZonedUtcNow` -- emits `now('UTC')` with `"UTC"` argument, using the provided `dbDataType` (`ClickHouseMemberTranslator.cs:247`).
- **Math** -- `roundBankers` for midpoint rounding.
- **Strings** -- `lowerUTF8`, `upperUTF8`, `lengthUTF8`. `string.Join` translates to `arrayStringConcat(groupArray(...), sep)` or `arrayStringConcat(groupUniqArray(...), sep)` for distinct; supports ORDER BY through `arraySort` with tuple key selectors and per-type descending-direction encoding (`ClickHouseMemberTranslator.cs:257`).
- **Guid** -- `generateUUIDv4()` for `Guid.NewGuid()`; `Guid.ToString()` -> `lower(toString(uuid))`.
- **SqlTypes** -- `Money`/`SmallMoney` -> `Decimal128`; `DateTime2`/`DateTimeOffset` -> `DateTime64`.

## Key types

| Type | File | Role |
|---|---|---|
| `ClickHouseDataProvider` (abstract) | `Internal/.../ClickHouseDataProvider.cs` | Base data provider; three concrete sealed subclasses per driver |
| `ClickHouseOctonicaDataProvider` | same | Concrete for Octonica |
| `ClickHouseDriverDataProvider` | same | Concrete for ClickHouse.Driver |
| `ClickHouseMySqlDataProvider` | same | Concrete for MySqlConnector |
| `ClickHouseSqlBuilder` | `Internal/.../ClickHouseSqlBuilder.cs` | SQL text generation |
| `ClickHouseSqlOptimizer` | `Internal/.../ClickHouseSqlOptimizer.cs` | Statement rewrite; owns DisableParameters and FixCteAliases |
| `ClickHouseSqlExpressionConvertVisitor` | `Internal/.../ClickHouseSqlExpressionConvertVisitor.cs` | Expression-level rewrites; LIKE, bitwise, casts, aggregate suffixes |
| `ClickHouseMappingSchema` | `Internal/.../ClickHouseMappingSchema.cs` | Type mappings, literal generators, three driver-specific sub-schemas |
| `ClickHouseProviderAdapter` | `Internal/.../ClickHouseProviderAdapter.cs` | ADO.NET type wrapping; contains `OctonicaWrappers` and `DriverWrappers` |
| `ClickHouseProviderDetector` | `Internal/.../ClickHouseProviderDetector.cs` | Auto-detection by name/file probe |
| `ClickHouseBulkCopy` | `Internal/.../ClickHouseBulkCopy.cs` | Three ProviderSpecific paths + multi-row fallback |
| `ClickHouseSchemaProvider` | `Internal/.../ClickHouseSchemaProvider.cs` | Schema via `system.tables` / `system.columns` |
| `ClickHouseMemberTranslator` | `Internal/.../Translation/ClickHouseMemberTranslator.cs` | LINQ member -> SQL function mapping |
| `ClickHouseTools` | `DataProvider/ClickHouse/ClickHouseTools.cs` | Public registration API |
| `ClickHouseOptions` | `DataProvider/ClickHouse/ClickHouseOptions.cs` | Provider options record |
| `ClickHouseProvider` (enum) | `DataProvider/ClickHouse/ClickHouseProvider.cs` | Client selector |
| `ClickHouseHints` | `DataProvider/ClickHouse/ClickHouseHints.cs` + `.generated.cs` | Hint constants and extension methods |
| `ClickHouseRetryPolicy` | `DataProvider/ClickHouse/ClickHouseRetryPolicy.cs` | Exponential backoff retry |
| `ClickHouseTransientExceptionDetector` | `DataProvider/ClickHouse/ClickHouseTransientExceptionDetector.cs` | Transient error classification |
| `ClickHouseSpecificExtensions` | `DataProvider/ClickHouse/ClickHouseSpecificExtensions.cs` | `AsClickHouse()` cast |
| `IClickHouseSpecificTable<T>` | `DataProvider/ClickHouse/IClickHouseSpecificTable.cs` | Provider-specific table interface |
| `IClickHouseSpecificQueryable<T>` | `DataProvider/ClickHouse/IClickHouseSpecificQueryable.cs` | Provider-specific queryable interface |
| `ClickHouseFactory` | `DataProvider/ClickHouse/ClickHouseFactory.cs` | Factory for connection-string registration |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full)

| File | Role |
|---|---|
| `Internal/DataProvider/ClickHouse/ClickHouseDataProvider.cs` | Provider base + three concrete subclasses; `SqlProviderFlags`, `SetParameter`, `BulkCopy` dispatch |
| `Internal/DataProvider/ClickHouse/ClickHouseSqlBuilder.cs` | SQL text generation |
| `Internal/DataProvider/ClickHouse/ClickHouseSqlOptimizer.cs` | Statement finalization; DisableParameters, FixCteAliases |
| `Internal/DataProvider/ClickHouse/ClickHouseProviderAdapter.cs` | ADO.NET type wrappers; Octonica and Driver wrappers; MySql delegation |
| `Internal/DataProvider/ClickHouse/ClickHouseProviderDetector.cs` | Auto-detection by name/file probe |
| `Internal/DataProvider/ClickHouse/ClickHouseMappingSchema.cs` | Type mappings and literal generators; three sub-schemas |
| `Internal/DataProvider/ClickHouse/ClickHouseBulkCopy.cs` | Three provider-specific bulk-copy paths |
| `DataProvider/ClickHouse/ClickHouseTools.cs` | Public registration entry point |
| `DataProvider/ClickHouse/ClickHouseProvider.cs` | `ClickHouseProvider` enum |
| `DataProvider/ClickHouse/ClickHouseOptions.cs` | Provider options record |

### Tier 2 (read in full -- 100%)

| File | Notes |
|---|---|
| `Internal/DataProvider/ClickHouse/ClickHouseSqlExpressionConvertVisitor.cs` | Bitwise/decimal/LIKE/cast/aggregate rewrites |
| `Internal/DataProvider/ClickHouse/Translation/ClickHouseMemberTranslator.cs` | Date, math, string, Guid LINQ member translations |
| `Internal/DataProvider/ClickHouse/ClickHouseSchemaProvider.cs` | system.tables / system.columns queries |
| `Internal/DataProvider/ClickHouse/ClickHouseSpecificQueryable.cs` | Thin wrapper implementing `IClickHouseSpecificQueryable<T>` |
| `Internal/DataProvider/ClickHouse/ClickHouseSpecificTable.cs` | Thin wrapper implementing `IClickHouseSpecificTable<T>` |
| `DataProvider/ClickHouse/ClickHouseHints.cs` | Hint constants and hand-written extension methods |
| `DataProvider/ClickHouse/ClickHouseHints.generated.cs` | T4-generated per-hint typed overloads |
| `DataProvider/ClickHouse/ClickHouseRetryPolicy.cs` | Retry policy with Octonica/MySqlConnector transient detection |
| `DataProvider/ClickHouse/ClickHouseTransientExceptionDetector.cs` | Runtime exception-type registry; error code classification |
| `DataProvider/ClickHouse/ClickHouseSpecificExtensions.cs` | `AsClickHouse()` extension methods |
| `DataProvider/ClickHouse/IClickHouseSpecificQueryable.cs` | Public queryable marker interface |
| `DataProvider/ClickHouse/IClickHouseSpecificTable.cs` | Public table marker interface |
| `DataProvider/ClickHouse/ClickHouseFactory.cs` | `DataProviderFactoryBase` for config-based registration |

### Tier 3 (not read)

| File | Reason |
|---|---|
| `DataProvider/ClickHouse/ClickHouseHints.tt` | T4 template; generated output is `ClickHouseHints.generated.cs` |
| `DataProvider/ClickHouse/README.md` | Documentation only; not source |

## Known issues / debt

- **No correlated subqueries** -- `SupportedCorrelatedSubqueriesLevel = 0` is a hard ClickHouse limitation (`ClickHouseDataProvider.cs:54`).
- **No nested joins** -- `IsNestedJoinsSupported = false` (`ClickHouseDataProvider.cs:57`).
- **InsertOrUpdate unimplemented** -- deliberately registered as supported to avoid pipeline assertion failures but will throw at SQL-builder time. No workaround short of raw SQL (`ClickHouseDataProvider.cs:46`).
- **Enum8/Enum16 DDL** -- type-name generation throws; users must provide `DbType` explicitly (`ClickHouseSqlBuilder.cs:180`).
- **JSON type** -- still experimental in ClickHouse; all three providers have known limitations.
- **CTE column list** -- blocked by ClickHouse issue #22932; workaround (`FixCteAliases`) is in place but must be revisited when ClickHouse resolves it (`ClickHouseSqlOptimizer.cs:70`).
- **Engine configuration API** -- `BuildEndCreateTableStatement` only produces `MergeTree(ORDER BY ...)` or `Memory()`; no API to configure `ReplacingMergeTree`, `SummingMergeTree`, `Distributed`, projections, etc. (`ClickHouseSqlBuilder.cs:196`).
- **IPv4 via MySqlConnector** -- ClickHouse issue #39056 means IPv4 values cannot be read back via the MySQL protocol.
- **Octonica `IsDBNullAllowed` workaround** -- always returns `true` because of Octonica issue #55 (`ClickHouseDataProvider.cs:168`).
- **v7 migration TODO** -- `BulkCopyRowsCopied.RowsCopied` is currently `int`; a comment in the new-client bulk-copy path marks it for migration to `long` in v7 (`ClickHouseBulkCopy.cs:638`).
- **`CURRENT_TIMESTAMP` parser bug in ClickHouse** -- `TranslateServerNow` deliberately avoids emitting `CURRENT_TIMESTAMP` and routes through `now()` instead, because `CURRENT_TIMESTAMP` (a ClickHouse alias for `now()`) triggers CH parser bugs in some server versions (`ClickHouseMemberTranslator.cs:221`).

## Inbound / outbound dependencies

**Inbound:**
- Users call `ClickHouseTools.GetDataProvider()` / `CreateDataConnection()`.
- `DataConnection` and `DataOptions` infrastructure via `ClickHouseOptions`.
- `ClickHouseFactory` is invoked by the connection-string factory infrastructure.

**Outbound:**
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `BasicBulkCopy`, `DynamicDataProviderBase<>`, `SchemaProviderBase` from [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md).
- `TypeMapper`, `TypeWrapper`, `IDynamicProviderAdapter` from [INTERNAL-API](../INTERNAL-API/INDEX.md).
- `LockedMappingSchema`, `MappingSchema`, `DataType`, `DbDataType` from [MAPPING](../MAPPING/INDEX.md).
- `SchemaProviderBase`, `ColumnInfo`, `TableInfo` from [METADATA](../METADATA/INDEX.md).
- `MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector)` from PROV-MYSQL -- the MySqlConnector ClickHouse adapter path delegates entirely to the MySQL adapter (`ClickHouseProviderAdapter.cs:103`).
- External assemblies loaded at runtime: `Octonica.ClickHouseClient`, `ClickHouse.Driver`, `MySqlConnector`.

## See also

- [PROV-MYSQL](../PROV-MYSQL/INDEX.md) -- for the MySQL-protocol sharing relationship; `MySqlProviderDetector` excludes ClickHouse strings; `MySqlProviderAdapter` is reused here.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`, `BasicBulkCopy` base classes.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `TypeMapper` / `TypeWrapper` dynamic-adapter infrastructure.
- [MAPPING](../MAPPING/INDEX.md) -- `LockedMappingSchema`, `DataType` enum, `DbDataType`.

<details><summary>Coverage</summary>

### Tier 1 (10/10 read)

- All files in Tier 1 list above.

### Tier 2 (13/13 read)

- All files in Tier 2 list above.

### Tier 3 (not read -- 2 files)

- `Source/LinqToDB/DataProvider/ClickHouse/ClickHouseHints.tt` -- T4 template; output is tracked as `ClickHouseHints.generated.cs` (already read)
- `Source/LinqToDB/DataProvider/ClickHouse/README.md` -- documentation only, not source; content reviewed briefly for type-mapping notes

### Read (this delta run -- sha 4a478ff14):

- `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseSqlBuilder.cs` -- PR #5449: `BuildTableExtensions` uses `" "` prefix separator; fixes missing space before table hint token
- `Source/LinqToDB/Internal/DataProvider/ClickHouse/Translation/ClickHouseMemberTranslator.cs` -- PR #5467: explicit `TranslateNow`/`TranslateServerNow`/`TranslateUtcNow`/`TranslateZonedNow`/`TranslateZonedUtcNow` overrides; PR #5517: `TranslateDateTimeTruncationToDate` + `TranslateDateTimeOffsetTruncationToDate` both cast to `DataType.Date32`

</details>
