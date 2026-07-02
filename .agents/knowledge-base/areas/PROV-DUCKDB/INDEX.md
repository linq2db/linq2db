---
area: PROV-DUCKDB
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 13/13
coverage_tier_2: 4/4
---

# PROV-DUCKDB

DuckDB provider added by PR #5451. DuckDB is an embedded, in-process analytical database (OLAP-oriented, file-based or in-memory). The linq2db integration follows the standard `DynamicDataProviderBase<TAdapter>` pattern with a native Appender bulk-copy path.

## Subsystems

### Provider core (`DuckDBDataProvider`)

`DuckDBDataProvider` extends `DynamicDataProviderBase<DuckDBProviderAdapter>` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBDataProvider.cs:23). Key `SqlProviderFlags` set at construction:

- CTE, sub-query ORDER BY, all set operations, APPLY joins (CROSS + OUTER), INSERT-OR-UPDATE, `DISTINCT FROM`, predicate comparison -- all enabled.
- `IsNullsOrderingSupported = true`; `DefaultNullsOrdering = NullsDefaultOrdering.AlwaysLast` (DuckDBDataProvider.cs:31-32): DuckDB places NULL last regardless of sort direction.
- `IsCrossApplyJoinSupportsCondition = true`; `IsOuterApplyJoinSupportsCondition = true` (DuckDBDataProvider.cs:37-38).
- `DefaultMultiQueryIsolationLevel` = `Snapshot` (DuckDB default).
- `RowConstructorSupport` = `CompareToSelect | Update | UpdateLiteral`.

Reader mappings handle DuckDB.NET non-standard return types:

- `BLOB` columns arrive as `UnmanagedMemoryStream`; converted to `byte[]` via `ReadStreamToBytes` (DuckDBDataProvider.cs:52-54).
- `TIME` columns arrive as `TimeOnly`; converted to `TimeSpan` or `DateTime` as needed (DuckDBDataProvider.cs:58-62).
- `TIMESTAMPTZ` arrives as `DateTime(Kind=Utc)`; converted to `DateTimeOffset` via `GetFieldValue<DateTimeOffset>` (DuckDBDataProvider.cs:64-66).
- `BITSTRING` default reader is `string`; `BitArray` reads use `GetFieldValue<BitArray>` (DuckDBDataProvider.cs:68-70).
- `"Bit"` field type mapped to `byte[]` via `ParseBitString` (DuckDBDataProvider.cs:74): parses a binary-string representation (`"0"`/`"1"` chars) into a `byte[]` array, LSB-first per byte.

`SetParameter` strips the `$` prefix from parameter names (DuckDB.NET expects unprefixed names but BulkCopy may pass prefixed ones) and converts `TimeSpan` -> `DateTimeOffset` for `TimeTZ`, `TimeSpan` -> `TimeOnly` for `Time` (net8+), `Binary` -> `byte[]` (DuckDBDataProvider.cs:127-165). `DateTimeOffset` values with `DataType.DateTime` are unwrapped to `dto.DateTime` (DuckDBDataProvider.cs:138-141). The base `SetParameter` is deliberately NOT called (comment at line 154) to avoid DbType being reset to string after value assignment.

`SetParameterType` override (DuckDBDataProvider.cs:170-178): maps `DataType.VarNumeric` -> `DbType.Decimal`; delegates all other types to the base.
### SQL builder (`DuckDBSqlBuilder`)

`DuckDBSqlBuilder` extends `BasicSqlBuilder<DuckDBOptions>` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs:19).

Notable overrides:

- `ConcatStyle` = `ConcatBuildStyle.Pipes` (DuckDBSqlBuilder.cs:35): delegates `||`-concatenation to the base builder. This replaces the prior visitor-level `+` -> `||` rewrite (PR #5504). `ConcatRequiresExplicitStringCast` is `false` in the visitor (DuckDBSqlExpressionConvertVisitor.cs:14), so no CAST wrapping is emitted for string operands.
- `IsRecursiveCteKeywordRequired` = `true`; `SupportsMaterializedCteHint` = `true` (DuckDBSqlBuilder.cs:32-33).
- `BuildDataTypeFromDataType` maps all linq2db `DataType` values to DuckDB SQL types. Includes DuckDB-specific types: `HUGEINT`/`UHUGEINT` (Int128/UInt128), `UHUGEINT`, `BITSTRING`, `INTERVAL`, `BIGNUM`, `TIMETZ`, `TIME_NS`, `TIMESTAMP_NS`/`_MS`/`_S` (precision-dispatched) (DuckDBSqlBuilder.cs:49-111).
- `Convert` (identifier quoting): standard double-quote escaping; parameters rendered as `$name`; `merge_action` pseudo-function maps to lowercase `merge_action` (DuckDBSqlBuilder.cs:118-160).
- `BuildGetIdentity`: emits `RETURNING <identity-field>` instead of a post-insert select (DuckDBSqlBuilder.cs:38-47).
- Identity via sequences: DuckDB does not support `GENERATED AS IDENTITY` with `PRIMARY KEY`. `BuildCreateTableStatement` prepends `CREATE SEQUENCE IF NOT EXISTS {table}_{field}_seq START 1` before the `CREATE TABLE`; `BuildCreateTableFieldType` appends `DEFAULT NEXTVAL('"<seqname>"')` (DuckDBSqlBuilder.cs:189-220). Sequence name formula: `{tableName}_{fieldName}_seq` (DuckDBSqlBuilder.cs:421).
- `BuildTruncateTableStatement`: DuckDB `ALTER SEQUENCE RESTART` is not implemented; `TRUNCATE` does not reset sequences. Workaround: create a replacement `{seqname}_reset` sequence and `ALTER TABLE ... SET DEFAULT nextval(...)` to it. The old sequence becomes orphaned (DuckDBSqlBuilder.cs:345-388). On `DROP TABLE`, both the primary and reset sequences are dropped (DuckDBSqlBuilder.cs:262-296).
- `BuildJoinType`: `CrossApply` -> `INNER JOIN LATERAL`, `OuterApply` -> `LEFT JOIN LATERAL` (DuckDBSqlBuilder.cs:223-232).
- `BuildObjectName`: strips schema/db for temp tables; supports `[database.][schema.]name` three-part form (DuckDBSqlBuilder.cs:234-260).
- `BuildParameter`: emits explicit `CAST` for `INTERVAL` and `DECIMAL` parameters in binary expressions to guide DuckDB operator overload resolution (DuckDBSqlBuilder.cs:390-414).
- `BuildCreateTableNullAttribute`: emits `NOT NULL` only for non-PK non-nullable fields; PK nullability is implicit (DuckDBSqlBuilder.cs:298-301).
### SQL optimizer (`DuckDBSqlOptimizer`)

`DuckDBSqlOptimizer` extends `BasicSqlOptimizer` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlOptimizer.cs:8). Notable overrides:

- `ConvertExpressionImpl`: passes all conversions to the base; DuckDB-specific rewrites live in `DuckDBSqlExpressionConvertVisitor`.
- `OptimizeQueryRoot`: no custom logic; inherits base CTE/limit/order normalization.
- No custom join rewriting beyond what the flags control (`IsApplyJoinSupported`, `IsCrossApplyJoinSupportsCondition`, `IsOuterApplyJoinSupportsCondition`).

### Provider adapter (`DuckDBProviderAdapter`)

Singleton that loads DuckDB.NET dynamically (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBProviderAdapter.cs). Key surface:

- `CreateConnection(connectionString)`: factory via reflection-loaded DuckDB.NET assembly.
- DuckDB-native types: `DuckDBDateOnly`, `DuckDBTimeOnly`, `DuckDBTimestamp`, `DuckDBInterval` -- exposed as `Type?` properties (DuckDBProviderAdapter.cs:50-53), registered via `SetGetFieldValueReader` in `DuckDBDataProvider` ctor.
- `CreateAppender(connection, table)`: creates a DuckDB native Appender object used by `DuckDBBulkCopy` for the Appender path.
- `AppendRow` / `EndRow` / `Flush`: Appender write cycle wrappers.

### Mapping schema (`DuckDBMappingSchema`)

`DuckDBMappingSchema` is a singleton that extends `LockedMappingSchema` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBMappingSchema.cs:9). It registers:

- Default converters for DuckDB-specific scalar types: `Int128`, `UInt128` (HUGEINT/UHUGEINT), `BitArray` (BITSTRING).
- DuckDB-specific column type annotations (e.g. `TIMESTAMP_NS`, `HUGEINT`, `UHUGEINT`) via `SetDataType`.
- Scalar `string` -> `BitArray` converter using `BitArray(bool[])` ctor.

### Bulk copy (`DuckDBBulkCopy`)

`DuckDBBulkCopy` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBBulkCopy.cs) handles two paths dispatched by `DuckDBOptions.BulkCopyType`:

- `BulkCopyType.ProviderSpecific` (= Appender path): uses DuckDB native Appender API for best performance. Auto-falls back to `MultipleRows` when the table has unmapped columns or identity columns with `nextval()` defaults (as documented by the XML doc on `DuckDBOptions.BulkCopyType`).
- `BulkCopyType.MultipleRows`: standard parametrized multi-row INSERT.
- Default `BulkCopyType` is `ProviderSpecific` (DuckDBOptions.cs).
### Schema provider (`DuckDBSchemaProvider`)

`DuckDBSchemaProvider` extends `SchemaProviderBase` (Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSchemaProvider.cs). Queries `information_schema` views for tables, columns, procedures, and foreign keys. Key details:

- Column type mapping: reads `DATA_TYPE` + `NUMERIC_PRECISION`/`SCALE` and maps to linq2db `DataType` including DuckDB-specific types (`HUGEINT`, `UHUGEINT`, `BITSTRING`, `INTERVAL`, `TIMETZ`, `TIME WITH TIME ZONE`).
- `GetProcedures`: returns empty by default (DuckDB functions are handled separately, not as stored procedures in the schema provider).
- Temp table filtering: tables in `information_schema.tables` with `TABLE_SCHEMA = 'temp'` are excluded.

### Member translator (`DuckDBMemberTranslator`)

`DuckDBMemberTranslator` extends `ProviderMemberTranslatorDefault` (DuckDBMemberTranslator.cs:14) and composes three sub-translators:

- `SqlTypesTranslation`: overrides `ConvertMoney` (DECIMAL(19,4)), `ConvertSmallMoney` (DECIMAL(10,4)), `ConvertDateTime`/`ConvertDateTime2` (both -> `DataType.DateTime2`).
- `DateFunctionsTranslator`: translates `Sql.DatePart` to `EXTRACT(part FROM x)` (DuckDBMemberTranslator.cs:43-80); `Millisecond` special-cased to `EXTRACT(millisecond FROM x) % 1000` (line 63-68); `DateAdd` via typed INTERVAL arithmetic (lines 87-116); `MakeDateTime` -> `make_timestamp(y,m,d,h,mi,s)` (lines 118-145); date/time truncation via CAST (lines 147-161); now-functions: `CURRENT_TIMESTAMP`, `LOCALTIMESTAMP`, `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (lines 163-188).
- `StringMemberTranslator`: `String.Join` -> `STRING_AGG(value, separator [ORDER BY ...])` (DuckDBMemberTranslator.cs:193-260); uses `AggregateFunctionBuilder` pattern; `withoutSeparator` path passes empty string; DISTINCT+ORDER BY is validated (must order by the aggregated value itself, else falls back via `SetFallback`); NULLs ordering via `BuildAggregateNullsOrderBy` with `NullsDefaultOrdering.AlwaysLast`.
- `TranslateNewGuidMethod`: `Guid.NewGuid()` -> non-pure `uuid()` function (DuckDBMemberTranslator.cs:20-24).
### Public API layer (`DuckDB` namespace)

`Source/LinqToDB/DataProvider/DuckDB/` contains the public-facing surface:

- `DuckDBTools` (DuckDBTools.cs): `UseDuckDB()` extension on `DataOptions` with connection-string overloads; `GetDuckDBConnection()` extension on `IDataContext`.
- `DuckDBOptions` (DuckDBOptions.cs): single option -- `BulkCopyType` (default `ProviderSpecific`). Uses native DuckDB Appender for best performance with automatic fallback to MultipleRows when the table has unmapped columns or identity columns with `nextval()` defaults.
- `DuckDBFactory` (DuckDBFactory.cs): `IDataProviderFactory` implementation used by connection-string-based provider resolution.
- `ProviderName.DuckDB` = 'DuckDB'.

## Key types

| Type | File | Role |
|---|---|---|
| `DuckDBDataProvider` | Internal/.../DuckDB/DuckDBDataProvider.cs | Main provider: flags, readers, parameter handling |
| `DuckDBSqlBuilder` | Internal/.../DuckDB/DuckDBSqlBuilder.cs | SQL emitter |
| `DuckDBSqlOptimizer` | Internal/.../DuckDB/DuckDBSqlOptimizer.cs | Query optimizer |
| `DuckDBSqlExpressionConvertVisitor` | Internal/.../DuckDB/DuckDBSqlExpressionConvertVisitor.cs | Expression converter |
| `DuckDBProviderAdapter` | Internal/.../DuckDB/DuckDBProviderAdapter.cs | Dynamic-load adapter for DuckDB.NET |
| `DuckDBMappingSchema` | Internal/.../DuckDB/DuckDBMappingSchema.cs | Type mappings + converters |
| `DuckDBBulkCopy` | Internal/.../DuckDB/DuckDBBulkCopy.cs | Appender + multi-row bulk-copy |
| `DuckDBSchemaProvider` | Internal/.../DuckDB/DuckDBSchemaProvider.cs | Schema introspection |
| `DuckDBMemberTranslator` | Internal/.../DuckDB/Translation/DuckDBMemberTranslator.cs | LINQ -> SQL date/string/type translation |
| `DuckDBOptions` | DataProvider/DuckDB/DuckDBOptions.cs | Public option: BulkCopyType |
| `DuckDBTools` | DataProvider/DuckDB/DuckDBTools.cs | Public: UseDuckDB(), GetDuckDBConnection() |
| `DuckDBFactory` | DataProvider/DuckDB/DuckDBFactory.cs | IDataProviderFactory |
## Files (Tier 1 / Tier 2)

**Tier 1 (13/13 read)**

| File | Notes |
|---|---|
| Internal/.../DuckDB/DuckDBDataProvider.cs | Provider core, flags, readers, parameter handling |
| Internal/.../DuckDB/DuckDBSqlBuilder.cs | SQL emitter |
| Internal/.../DuckDB/DuckDBSqlOptimizer.cs | Query optimizer |
| Internal/.../DuckDB/DuckDBSqlExpressionConvertVisitor.cs | Expression converter |
| Internal/.../DuckDB/DuckDBProviderAdapter.cs | Dynamic adapter |
| Internal/.../DuckDB/DuckDBMappingSchema.cs | Mapping schema |
| Internal/.../DuckDB/DuckDBBulkCopy.cs | Bulk copy |
| Internal/.../DuckDB/DuckDBSchemaProvider.cs | Schema provider |
| Internal/.../DuckDB/Translation/DuckDBMemberTranslator.cs | Member translator |
| DataProvider/DuckDB/DuckDBOptions.cs | Public options |
| DataProvider/DuckDB/DuckDBTools.cs | Public tools |
| DataProvider/DuckDB/DuckDBFactory.cs | Provider factory |
| DataProvider/DuckDB/DuckDBExtensions.cs | Public extension methods |

**Tier 2 (4/4 read)**

| File | Notes |
|---|---|
| Tests/Linq/DuckDB/*.cs (test fixtures) | Sampled for feature coverage |
| Tests/Linq/DuckDB/DuckDBTests.cs | Core integration tests |
| Tests/Linq/DuckDB/DuckDBBulkCopyTests.cs | Bulk-copy tests |
| Tests/Linq/DuckDB/DuckDBSpecificTests.cs | DuckDB-specific dialect tests |

## Inbound / outbound dependencies

**Inbound:** `DataConnection`, `DataOptions`, bulk-copy infrastructure call into `DuckDBDataProvider` / `DuckDBBulkCopy`. `DuckDBFactory` is registered by the DI layer.

**Outbound:**
- `DuckDB.NET` (NuGet, loaded dynamically via `DuckDBProviderAdapter`)
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `DynamicDataProviderBase`, `LockedMappingSchema` -- all from the shared linq2db engine
- `SchemaProviderBase` -- schema infrastructure
- `ProviderMemberTranslatorDefault`, `AggregateFunctionBuilder`, `DateFunctionsTranslatorBase` -- translator infrastructure
## Known issues / debt

- **TRUNCATE does not reset sequences** (DuckDBSqlBuilder.cs:345-388): the workaround creates a replacement `{seqname}_reset` sequence and re-points the column default, leaving the original sequence orphaned. A clean fix requires DuckDB to implement `ALTER SEQUENCE RESTART`.
- **Bitstring parsing** (`ParseBitString`, DuckDBDataProvider.cs:91-109): parses `"0"`/`"1"` chars into `byte[]` LSB-first per byte. This is a private method on the provider; any future change to DuckDB.NET bitstring wire format would require updating it.
- **No stored procedure support**: `GetProcedures` returns empty (DuckDB has macro/scalar functions but no traditional stored procedures accessible via the schema provider).
- **T4/NuGet DuckDB package skips netfx TFM** (from MEMORY.md): DuckDB.NET has no `net462` TFM, so the T4 NuGet package and LINQPad NuGet driver are unsupported. CLI scaffold and LINQPad driver are supported via netstandard2.0.

## See also

- [architecture/overview.md](../../architecture/overview.md) -- query pipeline end-to-end
- [architecture/public-api.md](../../architecture/public-api.md) -- DataOptions / provider registration
- [areas/SQL-BUILDER/INDEX.md](../SQL-BUILDER/INDEX.md) -- BasicSqlBuilder base
- [areas/TRANSLATE/INDEX.md](../TRANSLATE/INDEX.md) -- member translator infrastructure
- [areas/BULK-COPY/INDEX.md](../BULK-COPY/INDEX.md) -- bulk-copy infrastructure

## Pointers

- DuckDB.NET repo: https://github.com/Giorgi/DuckDB.NET -- upstream provider; data-reader types documented under `DuckDB.NET.Data/DataChunk/Reader`.
- PR #5451: initial DuckDB provider addition.
- PR #5504: `ConcatBuildStyle.Pipes` -- replaced visitor-level `+` -> `||` rewrite.
<details><summary>Coverage</summary>

**Tier 1 (13/13):** All 13 Tier-1 files read in full during initial build run.

**Tier 2 (4/4):** All 4 Tier-2 test files sampled.

**Read (prior delta run):**
- Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlBuilder.cs -- ConcatBuildStyle.Pipes, BuildJoinType LATERAL, BuildObjectName three-part form verified
- Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBSqlExpressionConvertVisitor.cs -- ConcatRequiresExplicitStringCast=false, no visitor-level + -> rewrite

**Read (this run -- delta):**
- Source/LinqToDB/DataProvider/DuckDB/DuckDBOptions.cs -- XML doc update on BulkCopyType parameter documenting Appender fallback conditions; no structural change.
- Source/LinqToDB/Internal/DataProvider/DuckDB/DuckDBDataProvider.cs -- Added IsNullsOrderingSupported=true, DefaultNullsOrdering=AlwaysLast (lines 31-32); IsCrossApplyJoinSupportsCondition=true, IsOuterApplyJoinSupportsCondition=true (lines 37-38); Bit field type -> byte[] via ParseBitString reader (line 74); DateTimeOffset+DataType.DateTime -> dto.DateTime unwrap (lines 138-141); SetParameterType override for DataType.VarNumeric -> DbType.Decimal (lines 170-178).
- Source/LinqToDB/Internal/DataProvider/DuckDB/Translation/DuckDBMemberTranslator.cs -- No structural changes; withoutSeparator path and AggregateFunctionBuilder pattern confirmed accurate per prior delta.

</details>
