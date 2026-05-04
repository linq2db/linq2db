---
area: PROV-MYSQL
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 11/11
coverage_tier_2: 16/16
---

# PROV-MYSQL

MySQL and MariaDB data provider. Covers two independent axes:
- **Product axis**: MySQL 5.7, MySQL 8.0+, MariaDB 10+
- **Client axis**: `MySql.Data` (Oracle official connector) and `MySqlConnector` (open-source replacement)

The two axes compose into six concrete `IDataProvider` instances. Both axes are fully orthogonal — every product version works with either client.

## Subsystems

### Provider registration and auto-detection

`MySqlTools` (`Source/LinqToDB/DataProvider/MySql/MySqlTools.cs`) is the public entry point. It exposes `GetDataProvider(version, provider, connectionString, ...)`, `CreateDataConnection(...)`, `ResolveMySql(path, assemblyName)` for assembly-resolver registration, and the `AutoDetectProvider` flag delegated to `MySqlProviderDetector`.

`MySqlProviderDetector` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderDetector.cs:12`) extends `ProviderDetectorBase<MySqlProvider, MySqlVersion>` with `DefaultVersion = MySqlVersion.MySql57`. Detection logic:

1. `DetectProvider(ConnectionOptions, MySqlProvider)` — resolves which ADO.NET client to use. Checks `options.ProviderName` against known string constants; falls back to probing for `MySql.Data.dll` on disk next to the linq2db assembly.
2. `DetectServerVersion(DbConnection, DbTransaction?)` — runs `SELECT VERSION()`, parses the version string, detects the `-MariaDB` suffix, returns one of `MySql57 / MySql80 / MariaDB10`. MariaDB < 10 (which is version 5.x based on MySQL 5.x) maps to `MySql57`.
3. The ClickHouse-over-MySQL protocol guard: if `ProviderName` or `ConfigurationString` contains `"ClickHouse"` the detector returns `null` immediately (`MySqlProviderDetector.cs:27`).

Six singletons live in `MySqlProviderDetector` as lazy statics, one per `(product, client)` combination.

`MySqlFactory` (`Source/LinqToDB/DataProvider/MySql/MySqlFactory.cs`) handles XML-config / connection-string `providerName` attributes and maps version strings (`"5.7"`, `"8.0"`, `"10"`, etc.) to `MySqlVersion` before delegating to `MySqlTools.GetDataProvider`.

### ADO.NET adapter layer

`MySqlProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderAdapter.cs:19`) is the abstract base for both client adapters. It loads the chosen assembly at runtime via reflection and `TypeMapper` wrappers. Two sealed inner classes:

- `MySqlData.MySqlDataProviderAdapter` (`MySqlProviderAdapter.cs:153`) — loads `MySql.Data.dll`. Maps `MySqlDecimal`, `MySqlDateTime`, `MySqlGeometry`. `BulkCopy = null` (no native bulk-copy in MySql.Data). `GetDateTimeOffsetMethodName = null` (no `DateTimeOffset` support). `IsPackageProceduresSupported = false`.
- `MySqlConnector.MySqlConnectorProviderAdapter` (`MySqlProviderAdapter.cs:307`) — loads `MySqlConnector.dll`. Version-guards: bulk copy available from 0.67, MySqlDecimal from ~2.0, DateOnly from 2.0. `GetDateTimeOffsetMethodName = "GetDateTimeOffset"`. `IsPackageProceduresSupported = true`.

Capability flags on the base class that differ between clients:
- `MySqlDecimalType` — `null` for MySqlConnector < 2.0.
- `BulkCopy` — non-null only for MySqlConnector ≥ 0.67; MySql.Data has no native bulk-copy API.
- `IsDateOnlySupported` — `true` for MySqlConnector ≥ 2.0.
- `GetDateTimeOffsetMethodName` — non-null only for MySqlConnector.

### Product/version matrix

`MySqlDataProvider` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlDataProvider.cs:27`) is the abstract base, extending `DynamicDataProviderBase<MySqlProviderAdapter>`. It holds `Version` and `Provider` properties.

Six sealed subclasses at `MySqlDataProvider.cs:19–24` cover all (version, client) combinations:

| Class | ProviderName constant | Version | Client |
|---|---|---|---|
| `MySql57DataProviderMySqlData` | `MySql57MySqlData` | `MySql57` | `MySqlData` |
| `MySql57DataProviderMySqlConnector` | `MySql57MySqlConnector` | `MySql57` | `MySqlConnector` |
| `MySql80DataProviderMySqlData` | `MySql80MySqlData` | `MySql80` | `MySqlData` |
| `MySql80DataProviderMySqlConnector` | `MySql80MySqlConnector` | `MySql80` | `MySqlConnector` |
| `MariaDB10DataProviderMySqlData` | `MariaDB10MySqlData` | `MariaDB10` | `MySqlData` |
| `MariaDB10DataProviderMySqlConnector` | `MariaDB10MySqlConnector` | `MariaDB10` | `MySqlConnector` |

`SqlProviderFlags` per version (`MySqlDataProvider.cs:35–57`):
- `IsCommonTableExpressionsSupported` — only when `version > MySql57`
- `IsAllSetOperationsSupported` / `IsDistinctSetOperationsSupported` — only when `version > MySql57`
- `IsApplyJoinSupported` / `IsCrossApplyJoinSupportsCondition` / `IsOuterApplyJoinSupportsCondition` — only `MySql80` (MariaDB explicitly excluded via comment referencing MDEV-6373/19078)
- `IsWindowFunctionsSupported` — `version >= MySql80`
- `SupportedCorrelatedSubqueriesLevel` — `null` (unlimited) for `MySql80` only; `1` for `MySql57` and `MariaDB10`

`CreateSqlBuilder` dispatches on `Version`:
```
MySql57 → MySql57SqlBuilder
MySql80 → MySql80SqlBuilder
_ (MariaDB10) → MariaDBSqlBuilder
```

`GetMappingSchema` dispatches on `(provider, version)` producing one of six `MySqlMappingSchema` subclasses.

### SQL builder hierarchy

```
BasicSqlBuilder<MySqlOptions>
  └── MySqlSqlBuilder           (abstract base — all shared SQL emission)
        ├── MySql57SqlBuilder   (MySQL 5.7 quirks)
        ├── MySql80SqlBuilder   (MySQL 8.0+ additions)
        └── MariaDBSqlBuilder   (MariaDB additions)
```

All three share `MySqlSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlBuilder.cs`). Key behaviors in the base:

**Identifier quoting**: backtick quoting (`\`` escaping of embedded backticks) via `Convert` (`MySqlSqlBuilder.cs:302–338`). All identifier convert types (table, field, alias, database, schema, package, CTE, procedure) use backticks.

**Parameter syntax**: `@name` prefix for both query and command parameters (`MySqlSqlBuilder.cs:306–317`).

**LIMIT/OFFSET syntax**: MySQL uses `LIMIT skip, take` not `OFFSET skip LIMIT take`. Implemented in `BuildOffsetLimit` (`MySqlSqlBuilder.cs:59–75`); calls `SqlOptimizer.ConvertSkipTake` first, then emits `LIMIT {skip}, {take}`. When skip is null falls back to base `LIMIT {take}`.

**Upsert**: `BuildInsertOrUpdateQuery` (`MySqlSqlBuilder.cs:366–408`) emits `INSERT ... ON DUPLICATE KEY UPDATE ...` when there are update items. When no update items are present, converts to `INSERT IGNORE` by string-patching `"INSERT"` → `"INSERT IGNORE"` in the accumulated SQL buffer.

**Hints infrastructure**: `HintBuilder` (`MySqlSqlBuilder.cs:561`) accumulates hint text; `StartStatementQueryExtensions` injects `QB_NAME(queryName)` if the select has a query block name; `FinalizeBuildQuery` wraps the accumulated hints in `/*+ ... */` and splices them into the statement at `_hintPosition`. Table hints and index hints (placed after the table alias) are handled in `BuildTableExtensions`.

**NULL-safe equality**: `BuildIsDistinctPredicate` emits `expr1 <=> expr2` (MySQL's null-safe equality operator).

**Type mappings in CAST**: a large dispatch table in `BuildDataTypeFromDataType` (`MySqlSqlBuilder.cs:82–147`) maps LinqToDB `DataType` to MySQL cast-safe type names (`SIGNED`, `UNSIGNED`, `CHAR(N)`, `BINARY(N)`, `DECIMAL`, `JSON`, etc.). FLOAT is mapped to DOUBLE in CASTs due to MySQL bug #87794.

**For CREATE TABLE**: a second dispatch table (`MySqlSqlBuilder.cs:150–233`) maps to full MySQL DDL type names including `TINYINT UNSIGNED`, `SMALLINT UNSIGNED`, `INT UNSIGNED`, `BIGINT UNSIGNED`, `TINYBLOB`/`BLOB`/`MEDIUMBLOB`/`LONGBLOB` by size, text types by size, and `BIT(n)` with size derived from the .NET type when no explicit length is given.

**Temporary tables**: `CreateTemporaryTable`/`DropTemporaryTable` use `CREATE TEMPORARY TABLE` / `DROP TEMPORARY TABLE` syntax; `IsTemporaryTable` (`MySqlSqlBuilder.cs:486`) resolves `TableOptions` flags.

**MERGE**: explicitly throws `LinqToDBException` — MySQL has no native MERGE statement (`MySqlSqlBuilder.cs:507`).

**ROLLUP/CUBE**: emits MySQL-specific `GROUP BY ... WITH ROLLUP` / `WITH CUBE` (`MySqlSqlBuilder.cs:511–550`).

**Object name**: `BuildObjectName` (`MySqlSqlBuilder.cs:429–451`) supports `database.table` but skips schema (MySQL has no schemas — `database` IS the schema). Package is supported for stored procedure calls.

**UPDATE quirk**: `BuildUpdateClause` emits the FROM clause as the UPDATE clause (MySQL's multi-table UPDATE syntax uses `UPDATE t1, t2` rather than `UPDATE t1 FROM t2`).

**Identity**: `CommandCount` returns 2 when `NeedsIdentity`; second command is `SELECT LAST_INSERT_ID()` (`MySqlSqlBuilder.cs:50`).

**Per-subclass overrides**:
- `MySql57SqlBuilder` (`MySqlSqlBuilder.cs`→`MySql57SqlBuilder.cs`): adds `FROM DUAL` when `WHERE` is present but no `FROM` clause (MySQL 5.7 requires FROM in WHERE queries). Overrides FLOAT/DOUBLE casts to use DECIMAL (FLOAT/DOUBLE in CAST added only in MySQL 8.0.17).
- `MySql80SqlBuilder`: enables `SupportsColumnAliasesInSource = true`. Implements `INNER JOIN LATERAL` / `LEFT JOIN LATERAL` for `CrossApply`/`OuterApply` (falls back to `INNER JOIN` / `LEFT JOIN` when the joined table is a function). Adds `VECTOR(n)` type in `BuildDataTypeFromDataType`.
- `MariaDBSqlBuilder`: enables `SupportsColumnAliasesInSource = true`. Adds `VECTOR(n)` type but requires explicit length (no default size in MariaDB — falls through to base if `Length == null`).

### SQL optimizer

`MySqlSqlOptimizer` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlOptimizer.cs:9`) extends `BasicSqlOptimizer`.

- `RequiresCastingNullValueForSetOperations = true`
- `CreateConvertVisitor` returns `MySqlSqlExpressionConvertVisitor`
- `TransformStatement` applies two post-base transforms:
  - `CorrectMySqlUpdate` — MySQL forbids referencing the UPDATE target table in a subquery within the same statement. Any non-target `SqlTable` node that refers to the same table is wrapped in a sub-select (`new SelectQuery { DoNotRemove = true }`). Also calls `SqlQueryColumnNestingCorrector` if changes were made. SKIP in UPDATE throws `LinqToDBException` with `ErrorHelper.MySql.Error_SkipInUpdate`.
  - `PrepareDelete` — when the DELETE has a single unjoined table and no SKIP/TAKE, sets `Alias = "$"` to produce a table alias; MySQL DELETE syntax requires the alias form in multi-table cases.

`MySqlSqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs`):
- `ConvertConversion`: suppresses CAST when converting decimal→float/double (avoids precision loss via intermediate cast).
- `ConvertSqlBinaryExpression`: adjusts `|` bitwise OR precedence (MySQL gives `|` lower priority than `&`); flattens string-concatenation `+` chains into multi-argument `Concat(...)` calls.
- `ConvertSearchStringPredicate`: case-insensitive `Contains` translates to `LOCATE(search, data) > 0`; case-sensitive adds `COLLATE utf8_bin` to the data expression.
- `ConvertSqlFunction`: maps `LENGTH` pseudo-function to `CHAR_LENGTH`.
- `WrapColumnExpression`: wraps `uint`, `ulong`, `long`, `double`, `decimal` values and parameters in mandatory CAST nodes for set operations.

### Type mapping and mapping schemas

`MySqlMappingSchema` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlMappingSchema.cs:16`) is a `LockedMappingSchema` keyed on `ProviderName.MySql`. Root-level conversions:
- String SQL literal: single-quote with `\'` and `\\` escaping (`MySqlMappingSchema.cs:60–82`).
- Binary: `0x` hex literal.
- `float[]` (vector): hex-encoding of little-endian IEEE 754 floats.
- `BitArray` → `DataParameter(DataType.UInt64)` — MySQL BIT fields mapped to `ulong`.
- `byte[]` → `float[]` `FromDatabase` conversion for VECTOR columns.
- `ReadOnlyMemory<float>` → `float[]` `FromDatabase` (net8.0+).

Hierarchy of 9 `LockedMappingSchema` subclasses, each chaining `parent`:
```
MySqlMappingSchema (base)
  ├── MySql57MappingSchema  → MySqlData57MappingSchema / MySqlConnector57MappingSchema
  ├── MySql80MappingSchema  → MySqlData80MappingSchema / MySqlConnector80MappingSchema
  └── MariaDB10MappingSchema→ MySqlDataMariaDB10MappingSchema / MySqlConnectorMariaDB10MappingSchema
```
Each leaf also chains the adapter's own `MappingSchema` (set during `MySqlProviderAdapter` construction) which registers provider-type scalar types (`MySqlDecimal`, `MySqlDateTime`).

### Bulk copy

`MySqlBulkCopy` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlBulkCopy.cs:15`) extends `BasicBulkCopy`.

- `MaxParameters = 32767` — MySQL multi-row INSERT limit.
- `MaxSqlLength = 327670` — conservative packet limit.
- **Provider-specific path** (MySqlConnector ≥ 0.67): `ProviderSpecificCopy[Async]` creates `MySqlProviderAdapter.MySqlConnector.MySqlBulkCopy` via `Adapter.BulkCopy.Create(connection, transaction)`, configures column mappings (ordinal → name), batches using `EnumerableHelper.Batch` to honor `MaxBatchSize`, calls `WriteToServer` / `WriteToServerAsync`. Supports `RowsCopiedCallback` via `MySqlRowsCopied` event (`MySqlBulkCopy.cs:154`).
- **Fallback** (MySql.Data or no BulkCopy adapter): `MultipleRowsCopy1` / `MultipleRowsCopy1Async` from base.
- `GetInsertInto`: when `ConflictAction.Ignore` is set, emits `INSERT IGNORE INTO` instead of `INSERT INTO` for multi-row bulk inserts.

`MySqlDataProvider.BulkCopy` (`MySqlDataProvider.cs:207–249`) resolves effective `BulkCopyType` from `MySqlOptions.Default` when the caller passes `BulkCopyType.Default`, then delegates to `MySqlBulkCopy`.

### Schema provider

`MySqlSchemaProvider` (`Source/LinqToDB/Internal/DataProvider/MySql/MySqlSchemaProvider.cs:15`) extends `SchemaProviderBase`. Queries `INFORMATION_SCHEMA` directly (not `GetSchema()` API) due to known bugs in both connectors. Key overrides:

- `GetProcedureSchemaExecutesProcedure = true` — MySql executes the procedure to retrieve result schema.
- `GetTables`: queries `INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE()`. The `TableID` is lowercase catalog + `..` + name to compensate for case-folding differences in FK queries (`MySqlSchemaProvider.cs:65`).
- `GetColumns`: reads `EXTRA` column to detect `auto_increment` (IsIdentity) and `VIRTUAL STORED`/`VIRTUAL GENERATED` (SkipOnInsert/SkipOnUpdate). VECTOR column length is divided by 4 (bytes per float) (`MySqlSchemaProvider.cs:139`).
- `GetProcedureParameters`: avoids `GetSchema("PROCEDURE PARAMETERS")` which returns incorrect results in both connectors.
- `GetProcedureSchema` / `GetProcedureResultColumns`: filters out the fake `@_cnet_param_` column rows that MySql.Data and MySqlConnector inject for output-parameter-only procedures (`MySqlSchemaProvider.cs:372–373`).
- `GetDataType`: `tinyint(1)` / `tinyint` with size 1 → `bool`; `bool` → `bool`; unsigned variants → `byte`/`ushort`/`uint`/`ulong`; geometry types → `byte[]`; `vector` → `float[]`.
- `ForeignKeyColumnComparison`: returns `OrdinalIgnoreCase` when the column name is all-lowercase (MySQL lowercases FK schema names in some versions).

### Member translator

`MySqlMemberTranslator` (`Source/LinqToDB/Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs:17`) extends `ProviderMemberTranslatorDefault`. Overrides:

- `DateFunctionsTranslator`: date-part extraction uses `EXTRACT(part FROM expr)` or dedicated functions (`DayOfYear`, `WeekDay`). `DateAdd` uses `DATE_ADD(expr, INTERVAL n unit)`. `MakeDateTime` builds `STR_TO_DATE(...)` from zero-padded string parts (`MySqlMemberTranslator.cs:154`). Milliseconds use `MICROSECOND() DIV 1000`. UTC timestamp → `UTC_TIMESTAMP()`.
- `MySqlStringMemberTranslator`: `String.Join` → `GROUP_CONCAT(value SEPARATOR sep ORDER BY ... )` with ORDER BY, DISTINCT, and null/empty string handling.
- `GuidMemberTranslator`: `Guid.ToString()` → `LOWER(CAST(guid AS CHAR(36)))`.
- `TranslateNewGuidMethod`: → `Uuid()` (non-pure function, side-effects tracked).
- `SqlTypesTranslation`: maps `Sql.SqlTypes.Float/Real` to `DECIMAL(29,10)` (because MySQL FLOAT is unsuitable for type functions), `Bit` → `BOOLEAN`, `TinyInt` → `INT16`.

### Public API surface

**`MySqlVersion`** (`Source/LinqToDB/DataProvider/MySql/MySqlVersion.cs`):
- `AutoDetect` (0), `MySql57`, `MySql80`, `MariaDB10`

**`MySqlProvider`** (`Source/LinqToDB/DataProvider/MySql/MySqlProvider.cs`):
- `AutoDetect` (0), `MySqlData`, `MySqlConnector`

**`MySqlOptions`** (`Source/LinqToDB/DataProvider/MySql/MySqlOptions.cs`): sealed record with single parameter `BulkCopyType` (default `MultipleRows`). Extends `DataProviderOptions<MySqlOptions>`. Used by `MySqlDataProvider` to resolve effective bulk copy mode.

**`MySqlHints`** (`Source/LinqToDB/DataProvider/MySql/MySqlHints.cs` + `MySqlHints.generated.cs`): static partial class. Exposes:
- `MySqlHints.Table.*` — optimizer hint constants (join-order, table-level, index-level) and classic index hints (`USE INDEX`, `IGNORE INDEX`, `FORCE INDEX` + per-join/group-by variants).
- `MySqlHints.Query.*` — query-level optimizer hint constants including `SET_VAR`, `RESOURCE_GROUP`, `MaxExecutionTime(int)`.
- `MySqlHints.SubQuery.*` — row-lock hints: `FOR UPDATE`, `FOR SHARE`, `LOCK IN SHARE MODE`, `NOWAIT`, `SKIP LOCKED`.
- Extension methods: `TableHint`, `TablesInScopeHint`, `TableIndexHint`, `QueryHint`, `SubQueryHint`, `SubQueryTableHint`, `QueryBlockHint`. Scope routing to `Sql.QueryExtensionScope.*` happens via `[Sql.QueryExtension]` attributes targeting `ProviderName.MySql`.
- `SubQueryTableHintExtensionBuilder` (`MySqlHints.cs:628`) — implements the `FOR SHARE` MariaDB suppression: when the builder detects `ProviderName.MariaDB10` in the mapping schema's configuration list, it emits `-- ` (comment-out) before the hint. This makes `FOR SHARE` a no-op on MariaDB.
- Generated file adds strongly-typed per-hint methods (e.g. `JoinFixedOrderHint`, `BkaHint`, `UseIndexHint`, etc.) as three-way overloads (table, in-scope, query-block) per optimizer hint constant.

**`MySqlExtensions`** (`Source/LinqToDB/DataProvider/MySql/MySqlExtensions.cs`): full-text search via `Match(ext, search, columns)` and `MatchRelevance(ext, modifier, search, columns)` mapping to `MATCH({columns, ', '}) AGAINST ({search}{modifier?})`. Three modifiers: `NaturalLanguage` (default, no suffix), `Boolean` → `IN BOOLEAN MODE`, `WithQueryExpansion` → `WITH QUERY EXPANSION`.

**`MySqlSpecificExtensions`**: `AsMySql<T>()` wrapping `ITable<T>` / `IQueryable<T>` to `IMySqlSpecificTable<T>` / `IMySqlSpecificQueryable<T>`.

**`IMySqlSpecificTable<T>`, `IMySqlSpecificQueryable<T>`, `IMySqlExtensions`**: marker interfaces enabling MySQL-specific extension method dispatch.

### Parameter and type mapping quirks

`MySqlDataProvider.SetParameter` (`MySqlDataProvider.cs:140–168`):
- `float[]` (vector) on MySql.Data: converts to `byte[]` via `Buffer.BlockCopy` because MySql.Data does not accept `float[]` directly.
- `MySqlDecimal` parameter: unwraps to string and changes DataType to `VarChar` — MySql.Data crashes on `DataType.Decimal` with large decimals.
- `DateOnly` without `IsDateOnlySupported`: converts to `DateTime`.

`SetParameterType` (`MySqlDataProvider.cs:170–202`):
- `VarNumeric` → `DbType.Decimal` (MySql.Data trims fractional part otherwise).
- `Date` / `DateTime2` → `DbType.DateTime` (MySql.Data trims time part otherwise).
- `BitArray` → `DbType.UInt64`.
- `Vector32` → provider-specific `MySqlDbType.Vector` enum value (different enum values for each connector: `242` for both but different enum types).

## Key types

| Type | File | Role |
|---|---|---|
| `MySqlDataProvider` | `Internal/DataProvider/MySql/MySqlDataProvider.cs` | Abstract base provider; 6 sealed subclasses |
| `MySqlSqlBuilder` | `Internal/DataProvider/MySql/MySqlSqlBuilder.cs` | Abstract SQL emitter; all shared MySQL SQL |
| `MySql57SqlBuilder` | `Internal/DataProvider/MySql/MySql57SqlBuilder.cs` | MySQL 5.7 overrides |
| `MySql80SqlBuilder` | `Internal/DataProvider/MySql/MySql80SqlBuilder.cs` | MySQL 8.0 overrides (LATERAL, VECTOR) |
| `MariaDBSqlBuilder` | `Internal/DataProvider/MySql/MariaDBSqlBuilder.cs` | MariaDB 10 overrides (VECTOR) |
| `MySqlSqlOptimizer` | `Internal/DataProvider/MySql/MySqlSqlOptimizer.cs` | Statement rewriting (UPDATE/DELETE fixups) |
| `MySqlSqlExpressionConvertVisitor` | `Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs` | Expression-level rewrites |
| `MySqlProviderAdapter` | `Internal/DataProvider/MySql/MySqlProviderAdapter.cs` | Runtime ADO.NET bridge (both clients) |
| `MySqlProviderDetector` | `Internal/DataProvider/MySql/MySqlProviderDetector.cs` | Auto-detection logic |
| `MySqlMappingSchema` | `Internal/DataProvider/MySql/MySqlMappingSchema.cs` | Type mapping (9 subclasses) |
| `MySqlBulkCopy` | `Internal/DataProvider/MySql/MySqlBulkCopy.cs` | Bulk insert (native + multi-row fallback) |
| `MySqlSchemaProvider` | `Internal/DataProvider/MySql/MySqlSchemaProvider.cs` | INFORMATION_SCHEMA queries |
| `MySqlMemberTranslator` | `Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs` | LINQ member → SQL function |
| `MySqlTools` | `DataProvider/MySql/MySqlTools.cs` | Public factory / registration |
| `MySqlVersion` | `DataProvider/MySql/MySqlVersion.cs` | Product/version enum |
| `MySqlProvider` | `DataProvider/MySql/MySqlProvider.cs` | ADO.NET client enum |
| `MySqlOptions` | `DataProvider/MySql/MySqlOptions.cs` | Provider options (bulk copy type) |
| `MySqlHints` | `DataProvider/MySql/MySqlHints.cs` + `.generated.cs` | Hint constants + extension methods |
| `MySqlExtensions` | `DataProvider/MySql/MySqlExtensions.cs` | Full-text search (`MATCH...AGAINST`) |

## Files (Tier 1 / Tier 2)

### Tier 1 (11 files, all visited)

| File | Purpose |
|---|---|
| `Internal/DataProvider/MySql/MySqlDataProvider.cs` | Core provider base + 6 sealed subclasses |
| `Internal/DataProvider/MySql/MySqlSqlBuilder.cs` | Version-agnostic SQL emitter |
| `Internal/DataProvider/MySql/MySqlSqlOptimizer.cs` | Statement-level rewrites |
| `DataProvider/MySql/MySqlTools.cs` | Public registration entry point |
| `DataProvider/MySql/MySqlVersion.cs` | Product/version enum |
| `DataProvider/MySql/MySqlProvider.cs` | ADO.NET client enum |
| `DataProvider/MySql/MySqlOptions.cs` | Provider options |
| `Internal/DataProvider/MySql/MySqlProviderAdapter.cs` | ADO.NET adapter (both clients) |
| `Internal/DataProvider/MySql/MySqlProviderDetector.cs` | Auto-detect logic |
| `Internal/DataProvider/MySql/MySqlMappingSchema.cs` | Type mapping (9 subclasses) |
| `Internal/DataProvider/MySql/MySqlBulkCopy.cs` | Bulk copy strategy |

### Tier 2 (16 files, all visited)

| File | Purpose |
|---|---|
| `Internal/DataProvider/MySql/MySql57SqlBuilder.cs` | MySQL 5.7 builder (`FROM DUAL`, FLOAT cast) |
| `Internal/DataProvider/MySql/MySql80SqlBuilder.cs` | MySQL 8.0 builder (LATERAL, VECTOR) |
| `Internal/DataProvider/MySql/MariaDBSqlBuilder.cs` | MariaDB builder (VECTOR) |
| `Internal/DataProvider/MySql/MySqlSchemaProvider.cs` | Schema discovery via INFORMATION_SCHEMA |
| `Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs` | Expression rewrites |
| `Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs` | Member → SQL function |
| `Internal/DataProvider/MySql/MySqlSpecificQueryable.cs` | Wrapped queryable (internal impl) |
| `Internal/DataProvider/MySql/MySqlSpecificTable.cs` | Wrapped table (internal impl) |
| `DataProvider/MySql/MySqlHints.cs` | Hint constants + extension methods |
| `DataProvider/MySql/MySqlHints.generated.cs` | Generated per-hint typed methods |
| `DataProvider/MySql/MySqlExtensions.cs` | Full-text search extensions |
| `DataProvider/MySql/MySqlSpecificExtensions.cs` | `AsMySql()` adapter |
| `DataProvider/MySql/IMySqlExtensions.cs` | Marker interface |
| `DataProvider/MySql/IMySqlSpecificQueryable.cs` | Marker interface |
| `DataProvider/MySql/IMySqlSpecificTable.cs` | Marker interface |
| `DataProvider/MySql/MySqlFactory.cs` | XML-config factory |

### Tier 3

0 files (no generated/obj files under these paths).

## Known issues / debt

1. **`FOR SHARE` on MariaDB silently commented-out**: `SubQueryTableHintExtensionBuilder` detects MariaDB by checking the mapping schema's configuration list and emits `-- ` before the hint text (`MySqlHints.cs:635`). This is a runtime behavior difference that is invisible to callers.

2. **MySql.Data `decimal` crash workaround**: `SetParameter` converts `MySqlDecimal` values to string and changes `DataType` to `VarChar` to avoid a crash in MySql.Data 8.x when large decimal values are passed as `DataType.Decimal` (`MySqlDataProvider.cs:151–158`). The comment links to the connector source: `MySQL.Data/src/Types/MySqlDecimal.cs#L103`.

3. **`float[]` parameter requires `byte[]` for MySql.Data**: vector parameters require `Buffer.BlockCopy` to produce `byte[]` for MySql.Data, silently converting in `SetParameter` (`MySqlDataProvider.cs:143–148`). MySqlConnector accepts `float[]` directly.

4. **No MERGE support**: `BuildMergeStatement` throws unconditionally (`MySqlSqlBuilder.cs:507`). MySQL has no `MERGE` statement.

5. **MySqlConnector `MySqlDecimal` gated on assembly version**: `GetMySqlDecimalMethodName` is `null` for MySqlConnector < 2.0, so decimal precision read from the data reader silently falls back to `double`. The check is based on assembly version `>= 2.0`, not `>= 2.1.0` (see comment in adapter, `MySqlProviderAdapter.cs:302`).

6. **`RETURNING` not supported** (MySQL): the `OUTPUT` / `RETURNING` clause is not mapped for MySQL/MySql80. MariaDB supports `RETURNING` but this is not surfaced in the builder hierarchy either — `BuildOutputSubclause` in `MySqlSqlBuilder.BuildInsertQuery` calls the base without MariaDB-specific override.

7. **Correlated subquery depth**: `SupportedCorrelatedSubqueriesLevel = 1` for MySql57 and MariaDB10 (unlimited only for MySql80) — subqueries with multiple levels of parent reference are rewritten aggressively, which can degrade query readability.

## Inbound / outbound dependencies

### Inbound (consumers of this area)
- [TESTS-LINQ](../TESTS-LINQ/INDEX.md) — per-provider MySQL and MariaDB test fixtures.

### Outbound (dependencies of this area)
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`, `ISqlBuilder`, `ISqlOptimizer`.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) — `DynamicDataProviderBase`, `ProviderDetectorBase`, `BasicBulkCopy`, `BulkCopyReader`, `TypeMapper`, `SchemaProviderBase`, `MemberTranslatorBase` (via `ProviderMemberTranslatorDefault`).
- [MAPPING](../MAPPING/INDEX.md) — `LockedMappingSchema`, `MappingSchema`.
- [SQL-AST](../SQL-AST/INDEX.md) — all `SqlStatement`, `SelectQuery`, `SqlTable`, `SqlField`, etc. node types consumed by builders and optimizer.

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) — `BasicSqlBuilder` base class documentation.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) — `DynamicDataProviderBase`, `ProviderDetectorBase`, `BasicBulkCopy`, `TypeMapper`.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) — `LockedMappingSchema`, mapping schema chain model.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) — first PROV-* area; structural reference for pattern comparison.
- [PROV-POSTGRES/INDEX.md](../PROV-POSTGRES/INDEX.md) — second PROV-* area; `RETURNING`, native bulk copy comparison.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11 ✓
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlDataProvider.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlOptimizer.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlTools.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlVersion.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlProvider.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlOptions.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderAdapter.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderDetector.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlMappingSchema.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlBulkCopy.cs
- Tier 2 (visited / total): 16 / 16 (100%) ✓
  - Source/LinqToDB/Internal/DataProvider/MySql/MySql57SqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySql80SqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MariaDBSqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSchemaProvider.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSpecificQueryable.cs
  - Source/LinqToDB/Internal/DataProvider/MySql/MySqlSpecificTable.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlHints.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlHints.generated.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlExtensions.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlSpecificExtensions.cs
  - Source/LinqToDB/DataProvider/MySql/IMySqlExtensions.cs
  - Source/LinqToDB/DataProvider/MySql/IMySqlSpecificQueryable.cs
  - Source/LinqToDB/DataProvider/MySql/IMySqlSpecificTable.cs
  - Source/LinqToDB/DataProvider/MySql/MySqlFactory.cs
- Tier 3 (skipped, logged): 0
</details>
