---
area: PROV-DUCKDB
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 13/13
coverage_tier_2: 4/4
---

# PROV-DUCKDB

DuckDB provider added by PR #5451. DuckDB is an embedded, in-process analytical database (OLAP-oriented, file-based or in-memory). The linq2db integration follows the standard `DynamicDataProviderBase<TAdapter>` pattern with a native Appender bulk-copy path.

## Subsystems

### Provider core (`DuckDBDataProvider`)

`DuckDBDataProvider` extends `DynamicDataProviderBase<DuckDBProviderAdapter>` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBDataProvider.cs:23). Key `SqlProviderFlags` set at construction:

- CTE, sub-query ORDER BY, all set operations, APPLY joins (CROSS + OUTER), INSERT-OR-UPDATE, `DISTINCT FROM`, predicate comparison -- all enabled.
- `DefaultMultiQueryIsolationLevel` = `Snapshot` (DuckDB default).
- `RowConstructorSupport` = `CompareToSelect | Update | UpdateLiteral`.

Reader mappings handle DuckDB.NET's non-standard return types:

- `BLOB` columns arrive as `UnmanagedMemoryStream`; converted to `byte[]` via `ReadStreamToBytes` (DuckDBDataProvider.cs:52-54).
- `TIME` columns arrive as `TimeOnly`; converted to `TimeSpan` or `DateTime` as needed (DuckDBDataProvider.cs:58-62).
- `TIMESTAMPTZ` arrives as `DateTime(Kind=Utc)`; converted to `DateTimeOffset` via `GetFieldValue<DateTimeOffset>` (DuckDBDataProvider.cs:64-66).
- `BITSTRING` default reader is `string`; `BitArray` reads use `GetFieldValue<BitArray>` (DuckDBDataProvider.cs:68-70).

`SetParameter` strips the `$` prefix from parameter names (DuckDB.NET expects unprefixed names but BulkCopy may pass prefixed ones) and converts `TimeSpan` -> `DateTimeOffset` for `TimeTZ`, `TimeSpan` -> `TimeOnly` for `Time` (net8+), `Binary` -> `byte[]` (DuckDBDataProvider.cs:127-165). The base `SetParameter` is deliberately NOT called (comment at line 154) to avoid DbType being reset to string after value assignment.

### SQL builder (`DuckDBSqlBuilder`)

`DuckDBSqlBuilder` extends `BasicSqlBuilder<DuckDBOptions>` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs:19).

Notable overrides:

- `BuildDataTypeFromDataType` maps all linq2db `DataType` values to DuckDB SQL types. Includes DuckDB-specific types: `HUGEINT`/`UHUGEINT` (Int128/UInt128), `UHUGEINT`, `BITSTRING`, `INTERVAL`, `BIGNUM`, `TIMETZ`, `TIME_NS`, `TIMESTAMP_NS`/`_MS`/`_S` (precision-dispatched) (DuckDBSqlBuilder.cs:49-111).
- `Convert` (identifier quoting): standard double-quote escaping; parameters rendered as `$name`; `merge_action` pseudo-function maps to lowercase `merge_action` (DuckDBSqlBuilder.cs:118-160).
- `BuildGetIdentity`: emits `RETURNING <identity-field>` instead of a post-insert select (DuckDBSqlBuilder.cs:38-47).
- Identity via sequences: DuckDB does not support `GENERATED AS IDENTITY` with `PRIMARY KEY`. `BuildCreateTableStatement` prepends `CREATE SEQUENCE IF NOT EXISTS {table}_{field}_seq START 1` before the `CREATE TABLE`; `BuildCreateTableFieldType` appends `DEFAULT NEXTVAL('"<seqname>"')` (DuckDBSqlBuilder.cs:189-220). Sequence name formula: `{tableName}_{fieldName}_seq` (DuckDBSqlBuilder.cs:421).
- `BuildTruncateTableStatement`: DuckDB's `ALTER SEQUENCE RESTART` is not implemented; `TRUNCATE` does not reset sequences. Workaround: create a replacement `{seqname}_reset` sequence and `ALTER TABLE ... SET DEFAULT nextval(...)` to it. The old sequence becomes orphaned (DuckDBSqlBuilder.cs:345-388). On `DROP TABLE`, both the primary and reset sequences are dropped (DuckDBSqlBuilder.cs:262-296).
- `BuildJoinType`: `CrossApply` -> `INNER JOIN LATERAL`, `OuterApply` -> `LEFT JOIN LATERAL` (DuckDBSqlBuilder.cs:223-232).
- `BuildObjectName`: strips schema/db for temp tables; supports `[database.][schema.]name` three-part form (DuckDBSqlBuilder.cs:234-260).
- `BuildParameter`: emits explicit `CAST` for `INTERVAL` and `DECIMAL` parameters in binary expressions to guide DuckDB operator overload resolution (DuckDBSqlBuilder.cs:390-414).
- `BuildCreateTableNullAttribute`: emits `NOT NULL` only for non-PK non-nullable fields; PK nullability is implicit (DuckDBSqlBuilder.cs:298-301).

### SQL optimizer (`DuckDBSqlOptimizer` / `DuckDBSqlExpressionConvertVisitor`)

`DuckDBSqlOptimizer` extends `BasicSqlOptimizer` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlOptimizer.cs:12). The `FinalizeStatement` pipeline calls `TuneParameters` after the base pass.

`TuneParameters` (DuckDBSqlOptimizer.cs:84-121) marks parameters as `IsQueryParameter = false` (force inline) for:
- `DataType.BitArray` -- BITSTRING has no provider parameter support.
- `DataType.Time` with `Precision > 6` -- TIME_NS not supported as parameter.
- `DataType.VarNumeric` with `BigInteger` -- BIGNUM not supported as parameter.
- `DuckDBInterval` and `DuckDBTimestamp` (precision > 6) provider types -- read-only provider types.

Parameters in binary expressions get `NeedsCast = true` to trigger the builder's explicit CAST path.

`TransformStatement` applies `GetAlternativeDelete` and `GetAlternativeUpdatePostgreSqlite` (shared with PostgreSQL/SQLite) for DELETE/UPDATE, and `InlineParametersInOutputClause` to force-inline all parameters in RETURNING/OUTPUT clauses (DuckDB restriction) (DuckDBSqlOptimizer.cs:28-51).

`DuckDBSqlExpressionConvertVisitor` extends `SqlExpressionConvertVisitor` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlExpressionConvertVisitor.cs:11):

- `SupportsNullInColumn` = false.
- Case-insensitive `LIKE` -> `ILIKE` (DuckDBSqlExpressionConvertVisitor.cs:15-27).
- `^` (XOR) -> `xor({0},{1})` function call (DuckDBSqlExpressionConvertVisitor.cs:32).
- `+` on strings -> `||` (DuckDBSqlExpressionConvertVisitor.cs:34-35).
- `/` on integer types -> `//` (DuckDB integer division operator) (DuckDBSqlExpressionConvertVisitor.cs:37-39).
- `CharIndex` -> `Position(x IN y)` with offset adjustment for 3-arg form (DuckDBSqlExpressionConvertVisitor.cs:45-79).
- `bool` CAST: routes non-`SqlSearchCondition`/`SqlCaseExpression` booleans through `ConvertBooleanToCase` (DuckDBSqlExpressionConvertVisitor.cs:83-97); `FloorBeforeConvert` applied.

### Provider adapter (`DuckDBProviderAdapter`)

Singleton (`DuckDBProviderAdapter.Instance`, lazy-initialized) loaded from `DuckDB.NET.Data` + `DuckDB.NET.Bindings` assemblies (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs:17-102).

ADO.NET types resolved: `DuckDBConnection`, `DuckDBDataReader`, `DuckDBParameter`, `DuckDBCommand`, `DuckDBTransaction`.

Provider-specific types exposed as `Type` properties: `DuckDBDateOnly` (DATE), `DuckDBTimeOnly` (TIME), `DuckDBTimestamp` (DateTime), `DuckDBInterval` (Interval). Each is registered as a scalar type in the adapter's `MappingSchema` (DuckDBProviderAdapter.cs:46-51).

Literal builders for provider types (DuckDBProviderAdapter.cs:106-228): `BuildTimeOnlyLiteral`, `BuildDateOnlyLiteral`, `BuildTimeSpanLiteral` (precision-aware; emits `infinity`/`-infinity` for TIMESTAMP_NS boundary values and for standard TIMESTAMP boundary), `BuildIntervalLiteral` (months/days/microseconds decomposed).

`CreateAppender` (DuckDBProviderAdapter.cs:85-95): compiled `Func<DbConnection, string?, string?, string, Wrappers.DuckDBAppender>` for the Appender bulk-copy path.

### Mapping schema (`DuckDBMappingSchema`)

`DuckDBMappingSchema` extends `LockedMappingSchema` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBMappingSchema.cs:15). Uses `OrdinalIgnoreCase` column name comparison.

Type-to-`DataType` registrations: `string`->NVarChar, `byte[]`->VarBinary, `TimeSpan`->Interval, `BigInteger`->VarNumeric, `BitArray`->BitArray.

SQL literal converters for: `bool`, `string` (with `chr()` escape for `\x01`), `char`, `byte[]`/`Binary` (BLOB hex `\xHH` per-byte; BITSTRING if `DataType.BitArray`), `Guid` -> `'...'::UUID`, `DateTime` (precision-dispatched to `TIMESTAMP_S/MS/TIMESTAMP/TIMESTAMP_NS`), `BigInteger`, `DateTimeOffset` (TIMESTAMPTZ or TIMETZ), `TimeSpan` (INTERVAL or TIME/TIMETZ, overflow to INTERVAL), `BitArray`, `float`/`double` (NaN/Infinity literals with explicit `::FLOAT`/`::DOUBLE` casts), `DateOnly`, `TimeOnly`.

Infinity boundary helpers `IsPositiveInfinityTsNs` / `IsNegativeInfinityTsNs` (DuckDBMappingSchema.cs:289-311) are `internal static` and shared by `DuckDBProviderAdapter`.

### Bulk copy (`DuckDBBulkCopy`)

`DuckDBBulkCopy` extends `BasicBulkCopy` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs:18). Default bulk mode is `ProviderSpecific` (via `DuckDBOptions`).

`ProviderSpecificCopy` uses the native `DuckDBAppender` (DuckDBBulkCopy.cs:111-163). The appender requires ALL table columns in ordinal order -- `GetTableColumns` (on `DuckDBSchemaProvider`) fetches the column list from `information_schema.columns`. If unmapped columns exist, falls back to `MultipleRowsCopy` (DuckDB `AppendDefault` does not work with `nextval()` identity defaults) (DuckDBBulkCopy.cs:229-231).

`_convertToParameter` (DuckDBBulkCopy.cs:43-62): excludes `BitArray`, `Time` (precision > 6), `DuckDBInterval` from parameterized multi-row path -- same inline rules as the SQL optimizer. Comment at line 42 cross-references the optimizer.

The Appender's `Append` method (DuckDBBulkCopy.cs:280-358) handles type coercions matching `SetParameter`: `TimeSpan`->`DateTimeOffset` for TimeTZ, `TimeSpan`->`TimeOnly` for Time (net8+), `DateTimeOffset`->`DateTime` for DateTime.

`GetMultipleRowsSuffix` returns `ON CONFLICT DO NOTHING` for `ConflictAction.Ignore` (DuckDBBulkCopy.cs:31-38).

### Schema provider (`DuckDBSchemaProvider`)

`DuckDBSchemaProvider` extends `SchemaProviderBase` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSchemaProvider.cs:15). Queries `information_schema` views: tables, columns, key_column_usage, constraint_column_usage, referential_constraints.

System catalogs `["system", "temp"]` are filtered out. Default schema is `"main"`. Identity detection: column is identity if `column_default` contains `"nextval"` (DuckDBSchemaProvider.cs:115).

`GetTableColumns` (DuckDBSchemaProvider.cs:357-373) is a static helper consumed by `DuckDBBulkCopy` to query ordered column names for the Appender path.

Type system: all DuckDB unsigned integers (`UTINYINT`->`byte`, `USMALLINT`->`ushort`, `UINTEGER`->`uint`, `UBIGINT`->`ulong`) plus `HUGEINT`/`UHUGEINT`/`BIGNUM` -> `BigInteger` are registered.

### Member translator (`DuckDBMemberTranslator`)

`DuckDBMemberTranslator` extends `ProviderMemberTranslatorDefault` (Source/LinqToDB/Internal/DataProvider/DuckDB/Translation/DuckDBMemberTranslator.cs:14).

Date functions (`DateFunctionsTranslator`):
- `EXTRACT(part FROM expr)` for all `Sql.DateParts`; `Millisecond` uses `EXTRACT(millisecond FROM ...) % 1000` (DuckDBMemberTranslator.cs:63-68).
- `WeekDay` (dow): result incremented by 1 to match .NET's 0=Sunday offset.
- Date add: interval multiplication -- `increment * INTERVAL '1 <unit>'` (DuckDBMemberTranslator.cs:87-116).
- `MakeDateTime`: `make_timestamp(year, month, day, hour, minute, second)` (DuckDBMemberTranslator.cs:118-145).
- Truncation to DATE: `CAST(expr AS DATE)` (DuckDBMemberTranslator.cs:147-153).
- Truncation to TIME: `CAST(expr AS TIME)` (DuckDBMemberTranslator.cs:155-161).
- `TranslateNow` -> `LOCALTIMESTAMP` (DuckDBMemberTranslator.cs:171-177).
- `TranslateServerNow` / `TranslateZonedNow` -> `CURRENT_TIMESTAMP` (DuckDBMemberTranslator.cs:163-182).
- `TranslateZonedUtcNow` -> `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (DuckDBMemberTranslator.cs:184-188).

String functions (`StringMemberTranslator`):
- `String.Join` / `Concat` -> `STRING_AGG(value, separator ORDER BY ...)` with full support for `DISTINCT`, `FILTER`, and `ORDER BY` (DuckDBMemberTranslator.cs:193-272).

Guid: `Guid.NewGuid()` -> `uuid()` non-pure function (DuckDBMemberTranslator.cs:20-24).

`SqlTypesTranslation`: `SqlTypes.Money` -> `DECIMAL(19,4)`, `SqlTypes.SmallMoney` -> `DECIMAL(10,4)`, DateTime/DateTime2 -> `DataType.DateTime2`.

### Public API layer

`DuckDBTools` (Source/LinqToDB/DataProvider/DuckDB/DuckDBTools.cs): `[PublicAPI]` entry points -- `GetDataProvider()`, `CreateDataConnection(string/DbConnection/DbTransaction)`, `ResolveDuckDB(string/Assembly)` for manual assembly resolution. Provider detection (`ProviderDetector`) checks `ProviderName` or `ConfigurationString` for the string "DuckDB" (case-insensitive).

`DuckDBOptions` (Source/LinqToDB/DataProvider/DuckDB/DuckDBOptions.cs): sealed record extending `DataProviderOptions<DuckDBOptions>`. Single option: `BulkCopyType` (default `ProviderSpecific`).

`DuckDBSpecificExtensions`: `AsDuckDB<T>()` on both `ITable<T>` and `IQueryable<T>` -- wraps into `IDuckDBSpecificTable<T>` / `IDuckDBSpecificQueryable<T>` respectively for provider-scoped extensions (currently marker interfaces, no DuckDB-specific query hints defined yet).

`DuckDBFactory`: `DataProviderFactoryBase` implementation for XML configuration; delegates entirely to `DuckDBTools.GetDataProvider()`.

## Key types

| Type | File | Role |
|---|---|---|
| `DuckDBDataProvider` | Internal/DataProvider/DuckDB/DuckDBDataProvider.cs | Core provider; reader expressions, parameter handling, bulk copy dispatch |
| `DuckDBSqlBuilder` | Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs | SQL generation; DDL, identity sequences, LATERAL joins |
| `DuckDBSqlOptimizer` | Internal/DataProvider/DuckDB/DuckDBSqlOptimizer.cs | Statement transforms; parameter inlining for RETURNING |
| `DuckDBSqlExpressionConvertVisitor` | Internal/DataProvider/DuckDB/DuckDBSqlExpressionConvertVisitor.cs | Expression-level rewrites; ILIKE, integer division, XOR |
| `DuckDBProviderAdapter` | Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs | Dynamic assembly binding; Appender factory; literal builders |
| `DuckDBMappingSchema` | Internal/DataProvider/DuckDB/DuckDBMappingSchema.cs | Type registrations; SQL literal converters |
| `DuckDBBulkCopy` | Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs | Appender path (provider-specific) + MultipleRows fallback |
| `DuckDBSchemaProvider` | Internal/DataProvider/DuckDB/DuckDBSchemaProvider.cs | Schema introspection via information_schema; `GetTableColumns` helper |
| `DuckDBMemberTranslator` | Internal/DataProvider/DuckDB/Translation/DuckDBMemberTranslator.cs | LINQ member translation: dates, strings, Guid.NewGuid |
| `DuckDBTools` | DataProvider/DuckDB/DuckDBTools.cs | Public API: data connection factory, assembly resolver |
| `DuckDBOptions` | DataProvider/DuckDB/DuckDBOptions.cs | Provider options record (BulkCopyType) |
| `DuckDBSpecificExtensions` | DataProvider/DuckDB/DuckDBSpecificExtensions.cs | `AsDuckDB()` extension for table/queryable specialization |
| `IDuckDBSpecificTable<T>` | DataProvider/DuckDB/IDuckDBSpecificTable.cs | Marker interface for DuckDB-scoped table queries |
| `IDuckDBSpecificQueryable<T>` | DataProvider/DuckDB/IDuckDBSpecificQueryable.cs | Marker interface for DuckDB-scoped queryable |
| `DuckDBSpecificTable<T>` | Internal/DataProvider/DuckDB/DuckDBSpecificTable.cs | Concrete `IDuckDBSpecificTable` implementation |
| `DuckDBSpecificQueryable<T>` | Internal/DataProvider/DuckDB/DuckDBSpecificQueryable.cs | Concrete `IDuckDBSpecificQueryable` implementation |
| `DuckDBFactory` | DataProvider/DuckDB/DuckDBFactory.cs | XML-config provider factory |

## Files (Tier 1 / Tier 2)

**Tier 1** (all read):
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBDataProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlOptimizer.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlExpressionConvertVisitor.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBMappingSchema.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSchemaProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/Translation/DuckDBMemberTranslator.cs`
- `Source/LinqToDB/DataProvider/DuckDB/DuckDBTools.cs`
- `Source/LinqToDB/DataProvider/DuckDB/DuckDBFactory.cs`
- `Source/LinqToDB/DataProvider/DuckDB/DuckDBOptions.cs`
- `Source/LinqToDB/DataProvider/DuckDB/DuckDBSpecificExtensions.cs`

**Tier 2** (all read):
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSpecificTable.cs`
- `Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSpecificQueryable.cs`
- `Source/LinqToDB/DataProvider/DuckDB/IDuckDBSpecificTable.cs`
- `Source/LinqToDB/DataProvider/DuckDB/IDuckDBSpecificQueryable.cs`

## Inbound / outbound dependencies

**Inbound:**
- `LinqToDBConnectionOptionsBuilder` / `DataOptions` extension methods (public surface) call into `DuckDBTools` for provider creation.
- `ProviderDetectorBase` calls `DuckDBTools.ProviderDetector` during auto-detection.
- `DataProviderFactoryBase` XML config resolution calls `DuckDBFactory`.

**Outbound:**
- `DuckDB.NET.Data` (assembly `DuckDB.NET.Data`) -- ADO.NET types, `DuckDBAppender`, `IDuckDBAppenderRow`.
- `DuckDB.NET.Bindings` (assembly `DuckDB.NET.Bindings`) -- `DuckDBDateOnly`, `DuckDBTimeOnly`, `DuckDBTimestamp`, `DuckDBInterval` provider-native types.
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `BasicBulkCopy`, `SchemaProviderBase`, `LockedMappingSchema`, `DynamicDataProviderBase<T>` -- core linq2db base classes.
- `GetAlternativeUpdatePostgreSqlite` (shared with `PROV-POSTGRESQL`, `PROV-SQLITE`) -- UPDATE rewriting shared across providers.

## Known issues / debt

- **Identity sequence orphan on TRUNCATE** (DuckDBSqlBuilder.cs:359-388): DuckDB's `ALTER SEQUENCE RESTART` is not implemented; the workaround creates a replacement sequence (`{seqname}_reset`) and switches the column default, leaving the old sequence permanently orphaned. This is a known DuckDB engine limitation as of PR #5451.
- **Appender AppendDefault incompatibility with nextval()** (DuckDBBulkCopy.cs:227-231): when any table column is unmapped, the Appender path falls back to MultipleRows. This means tables with identity columns not covered by entity properties always use the slower path.
- **DATE translation merge-race fix** (PR #5518, commit `f6d511cc3`): a post-#5451 merge-race caused incorrect date translation overrides; fixed two days after initial merge. No residual code debt -- the fix was a correction to translator overrides.
- **DuckDB-specific query hints not yet implemented**: `IDuckDBSpecificTable<T>` and `IDuckDBSpecificQueryable<T>` are marker interfaces with no provider-specific hint methods (no equivalent of ClickHouse's `WithTableHint` or PostgreSQL's `ForUpdate`).
- **`DuckDBInterval` and `DuckDBTimestamp` (precision > 6) are read-only** in the provider: writing via parameters is disabled; these types flow through literal generation only (DuckDBSqlOptimizer.cs:103-105). Documented in adapter source comments (DuckDBProviderAdapter.cs:38-42).

## See also

- [PROV-CLICKHOUSE](../PROV-CLICKHOUSE/INDEX.md) -- closest sibling (OLAP column-store, file-based, similar embedded-engine pattern).
- [PROV-SQLITE](../PROV-SQLITE/INDEX.md) -- closest embedded-engine analogue; `GetAlternativeUpdatePostgreSqlite` is shared.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 13 / 13 ✓
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBDataProvider.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlOptimizer.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlExpressionConvertVisitor.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBMappingSchema.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSchemaProvider.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/Translation/DuckDBMemberTranslator.cs
  - Source/LinqToDB/DataProvider/DuckDB/DuckDBTools.cs
  - Source/LinqToDB/DataProvider/DuckDB/DuckDBFactory.cs
  - Source/LinqToDB/DataProvider/DuckDB/DuckDBOptions.cs
  - Source/LinqToDB/DataProvider/DuckDB/DuckDBSpecificExtensions.cs
- Tier 2 (visited / total): 4 / 4 (100%) ✓
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSpecificTable.cs
  - Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSpecificQueryable.cs
  - Source/LinqToDB/DataProvider/DuckDB/IDuckDBSpecificTable.cs
  - Source/LinqToDB/DataProvider/DuckDB/IDuckDBSpecificQueryable.cs
- Tier 3 (skipped, logged): 0
</details>
