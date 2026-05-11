---
area: PROV-POSTGRES
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 11/11
coverage_tier_2: 16/16
---

# PROV-POSTGRES

PostgreSQL provider. Single ADO.NET dependency: **Npgsql** (loaded dynamically). Covers PostgreSQL 9.2 through 18+. All public API lives in `LinqToDB.DataProvider.PostgreSQL`; all implementation lives in `LinqToDB.Internal.DataProvider.PostgreSQL`.

## Subsystems

### Version matrix

Six concrete sealed subclasses of `PostgreSQLDataProvider` are defined in `PostgreSQLDataProvider.cs:23--28`, one per supported dialect:

| Class | ProviderName constant | Key capabilities unlocked |
|---|---|---|
| `PostgreSQLDataProvider92` | `PostgreSQL92` | baseline; no `APPLY` join, no upsert |
| `PostgreSQLDataProvider93` | `PostgreSQL93` | `APPLY` join; no upsert |
| `PostgreSQLDataProvider95` | `PostgreSQL95` | upsert (`ON CONFLICT`) |
| `PostgreSQLDataProvider13` | `PostgreSQL13` | `gen_random_uuid()`, v13 member translator |
| `PostgreSQLDataProvider15` | `PostgreSQL15` | `MERGE` statement (`PostgreSQLSql15Builder`) |
| `PostgreSQLDataProvider18` | `PostgreSQL18` | `OUTPUT`/`RETURNING` via special table (`OutputInsertUseSpecialTable` etc.) |

`PostgreSQLDataProvider` constructor sets `SqlProviderFlags` version-conditionally: `IsApplyJoinSupported` is false only for v92; `IsInsertOrUpdateSupported` is false for v92 and v93; `OutputDeleteUseSpecialTable` and siblings require v18+. `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs:43--57`.

### SQL builder hierarchy

Two classes -- much narrower than SqlServer's nine-version stack:

- `PostgreSQLSqlBuilder` -- base for all versions. Handles `RETURNING` for identity, `LIMIT`/`OFFSET`, `LATERAL` joins, identifier quoting, `SERIAL`/`SMALLSERIAL`/`BIGSERIAL` for `CREATE TABLE`, `ON CONFLICT` for upsert, PostgreSQL-flavored cast syntax (`::type`), `RECURSIVE` CTE keyword, sequence `nextval(...)` expressions. File: `PostgreSQLSqlBuilder.cs`.
- `PostgreSQLSql15Builder` -- v15+ only. Overrides `BuildInsertOrUpdateQuery` to emit `MERGE` via `BuildInsertOrUpdateQueryAsMerge` instead of `ON CONFLICT`. File: `PostgreSQLSql15Builder.cs:24--27`.

`PostgreSQLDataProvider.CreateSqlBuilder` always instantiates `PostgreSQLSqlBuilder` regardless of version (`PostgreSQLDataProvider.cs:261`); the v15 builder is **not** used from `CreateSqlBuilder` here -- the v15 `DataProvider` class would need to override `CreateSqlBuilder` to use it. Review note: `PostgreSQLSql15Builder` exists but `PostgreSQLDataProvider15` still inherits the base `CreateSqlBuilder` which returns the base builder. The Merge partial (`PostgreSQLSqlBuilder.Merge.cs`) contains `BuildMergeOperationDeleteBySource` and `BuildMergeOperationUpdateBySource` annotated "available since PGSQL17" -- these are enabled in the base builder class to allow per-query dialect negotiation without requiring a version-specific builder.

The `PostgreSQLSqlBuilder.Merge.cs` partial also defines `IsSqlValuesTableValueTypeRequired`, which forces explicit type annotation on the first row of a `VALUES` table for `long`, `float`, `double`, `decimal`, `NULL`-only columns, and JSON/JSONB columns (`PostgreSQLSqlBuilder.Merge.cs:16--37`).

### Identifier quoting

Controlled by `PostgreSQLIdentifierQuoteMode` (public enum, `PostgreSQLIdentifierQuoteMode.cs`):

- `None` -- never quote.
- `Quote` -- always quote.
- `Needed` -- quote only for reserved words and whitespace.
- `Auto` (default) -- quote for reserved words, prohibited characters, and **uppercase letters** (critical for case-folding behavior; PostgreSQL folds unquoted identifiers to lowercase).

Logic is in `PostgreSQLSqlBuilder.Convert` (`PostgreSQLSqlBuilder.cs:153--208`). Parameter names use `:name` syntax (colon prefix, not `@`).

### NpgsqlProviderAdapter

Singleton (`NpgsqlProviderAdapter.GetInstance()`, locked double-check, `NpgsqlProviderAdapter.cs:198--203`). Loads `Npgsql.dll` via `Common.Tools.TryLoadAssembly`. Wraps:

- `NpgsqlConnection`, `NpgsqlParameter`, `NpgsqlDataReader`, `NpgsqlCommand`, `NpgsqlTransaction`, `NpgsqlBinaryImporter`.
- `NpgsqlDbType` enum -- mirrored as an internal `[Wrapper]` enum because Npgsql changes numeric field values between major versions; `ApplyDbTypeFlags` (`NpgsqlProviderAdapter.cs:172--187`) reconstructs flag composites (Array | Range | Multirange) against the runtime numeric values.
- Provider-specific geometric and temporal CLR types: `NpgsqlPoint`, `NpgsqlBox`, `NpgsqlCircle`, `NpgsqlLSeg`, `NpgsqlPath`, `NpgsqlPolygon`, `NpgsqlLine`, `NpgsqlInet`, `NpgsqlRange<T>`, and conditionally `NpgsqlDate`, `NpgsqlDateTime`, `NpgsqlTimeSpan`, `NpgsqlInterval`, `NpgsqlCidr`, `NpgsqlCube`.
- `BigInteger` support conditional on Npgsql >= 6.0 (`NpgsqlProviderAdapter.cs:196`).
- `BeginBinaryImport` / `BeginBinaryImportAsync` delegates for bulk copy.

### Provider detection

`PostgreSQLProviderDetector` extends `ProviderDetectorBase<Provider, PostgreSQLVersion>` (`PostgreSQLProviderDetector.cs:10`). Six `Lazy<IDataProvider>` statics, one per version. Detection order:

1. Exact `ProviderName.*` string match.
2. Configuration string contains version number substring (e.g. `"15"`, `"16"`, `"17"` -> v15).
3. `AutoDetectProvider` = true -> `DetectServerVersion` reads `connection.PostgreSqlVersion` from the live connection wrapper and pattern-matches on `Major`/`Minor` (`PostgreSQLProviderDetector.cs:105--116`).
4. Fallback to `DefaultVersion` = `v92`.

### Mapping schema

`PostgreSQLMappingSchema` (`LockedMappingSchema`, `PostgreSQLMappingSchema.cs`) registers:

- Column name comparison: `OrdinalIgnoreCase` (matches PostgreSQL's case-insensitive catalog).
- Value-to-SQL converters for `bool` (native `TRUE`/`FALSE`), `string`, `char`, `byte[]` (hex-escaped `E'\\x...'::bytea`), `Guid` (`'...'::uuid`), `DateTime` (inline `'...'::timestamp` or `'...'::date`), `DateTimeOffset` (`'...'::timestamptz`, microsecond precision, added PR #5467), `BigInteger`.
- Float/double special values: `NaN`, `Infinity` quoted and cast (`'NaN'::float4`, `'NaN'::float8`).
- Unsigned integer workarounds: `ushort` -> `int`, `uint` -> `long`, `ulong` -> `decimal(20,0)`.
- Native array types registered as scalars (enables correct query cache keying and parameter detection): all primitive CLR arrays, `List<T>`, `IReadOnlyList<T>` for ~20 element types.
- Six per-version sealed subclasses (`PostgreSQL92MappingSchema` ... `PostgreSQL18MappingSchema`) extend from `NpgsqlProviderAdapter.GetInstance().MappingSchema` and the base `PostgreSQLMappingSchema.Instance` (`PostgreSQLMappingSchema.cs:194--205`).

The `TIMESTAMPTZ_FORMAT` constant (`'...'::timestamptz`, `PostgreSQLMappingSchema.cs:26/31`) formats `DateTimeOffset` values with microsecond precision and timezone offset. `BuildDateTimeOffset` (`PostgreSQLMappingSchema.cs:163--166`) applies it via `AppendFormat`. This was absent before PR #5467 -- `DateTimeOffset` had no registered converter and fell through to a default path.

### Parameter type resolution

`PostgreSQLDataProvider.SetParameter` handles two special cases before delegating to base (`PostgreSQLDataProvider.cs:303--315`):

1. `IDictionary` with `DataType.Undefined` -> promoted to `DataType.Dictionary` (maps to `hstore`).
2. `DateTime`/`DateTimeOffset` normalization: when `NormalizeTimestampData` is true, `DateTimeOffset` is converted to UTC, and `DateTime` gets `DateTimeKind` adjusted to match `timestamp` vs `timestamptz` expectations introduced in Npgsql 6 (`PostgreSQLDataProvider.cs:275--300`).

`SetParameterType` maps `DataType` -> `NpgsqlDbType` and calls `Adapter.SetDbType(param, type)`. If the provider parameter is not available (wrapped connection), falls back to `DbType`.

`GetNativeType(string? dbType)` (`PostgreSQLDataProvider.cs:444--598`) normalizes type name aliases (e.g. `int4` -> `integer`, `timestamptz` -> `timestamp with time zone`), detects array `[]` suffix and range type names, and returns the correct `NpgsqlDbType` with flags composed via `ApplyDbTypeFlags`.

`SetProviderField` for reading `DateTimeOffset` from `DateTime` reader columns is now scoped to `"timestamp with time zone"` columns only and uses `rd.GetFieldValue<DateTimeOffset>(i)` directly (`PostgreSQLDataProvider.cs:70`). The prior `ConvertDateTimeToDateTimeOffset` helper -- which clamped `DateTime.Min/Max` to avoid offset failures for +/-infinity values -- was removed in PR #5467. Agents verifying infinity handling in `timestamptz` columns should note this removal.

### Bulk copy

`PostgreSQLBulkCopy` (`BasicBulkCopy` subclass, `PostgreSQLBulkCopy.cs`). Two strategies:

- `ProviderSpecific` (default when `DataConnection` is available): opens `NpgsqlBinaryImporter` via `COPY {table} ({fields}) FROM STDIN (FORMAT BINARY)`. Writes rows via `writer.StartRow()` + `writer.Write(value, npgsqlDbType)`. Batch size defaults to 10,000; importer is recycled per batch. Async path uses `BeginBinaryImportAsync` when available; falls back to sync importer if async methods are absent (`PostgreSQLBulkCopy.cs:281--287`).
- `MultipleRows`: delegates to `MultipleRowsCopy1` (base class `INSERT INTO ... VALUES (...), (...)` pattern). `GetMultipleRowsSuffix` returns `ON CONFLICT DO NOTHING` when `ConflictAction.Ignore` is set (`PostgreSQLBulkCopy.cs:38--43`).

`ConfigureWriter` sets `writer.Timeout` if `SupportsTimeout` (Npgsql added this; `PostgreSQLBulkCopy.cs:497--501`). `BuildTypes` resolves `NpgsqlDbType` per column, throwing if neither npgsql type nor explicit `DbType` can be determined (`PostgreSQLBulkCopy.cs:139--143`).

### SQL optimizer

`PostgreSQLSqlOptimizer` (`BasicSqlOptimizer` subclass, `PostgreSQLSqlOptimizer.cs`). `TransformStatement` applies:

1. `GetAlternativeDelete` for DELETE with joins (rewrites to `DELETE ... USING ...`).
2. `GetAlternativeUpdatePostgreSqlite` for UPDATE with joins (rewrites to `UPDATE ... FROM ...`).
3. `CorrectPostgreSqlOutput`: for UPDATE/MERGE statements, validates that OUTPUT anchors (`Inserted`/`Deleted`) reference plain fields only -- PostgreSQL does not support expressions in `RETURNING` (`PostgreSQLSqlOptimizer.cs:48--66`).

`CreateConvertVisitor` returns `PostgreSQLSqlExpressionConvertVisitor`.

### Expression convert visitor

`PostgreSQLSqlExpressionConvertVisitor` (`PostgreSQLSqlExpressionConvertVisitor.cs`):

- `SupportsNullInColumn` = `false`.
- `ConvertSearchStringPredicate`: case-insensitive search rewrites `LIKE` -> `ILIKE`.
- `ConvertSqlBinaryExpression`: `^` -> `#` (XOR), `+` on strings -> `||`, `%` casts non-decimal operand to `decimal` (PostgreSQL modulo supports only decimal/numeric).
- `ConvertSqlFunction`: `CharIndex` -> `Position(... in ...)` with 2- and 3-argument forms.
- `VisitExprExprPredicate`: JSON/JSONB equality comparisons cast mixed `json`/`jsonb` operands to `jsonb` (`PostgreSQLSqlExpressionConvertVisitor.cs:109--128`).
- `ConvertConversion`: `bool` targets use `ConvertBooleanToCase` unless already boolean expression; applies `FloorBeforeConvert` for numeric narrowing.
- `WrapColumnExpression`: `uint`/`long`/`ulong`/`float`/`double`/`decimal` literal values and non-query parameters get mandatory cast to prevent Npgsql type inference failures.

### Member translators

Two classes in `Translation/`:

- `PostgreSQLMemberTranslator` -- baseline. Inner classes: `DateFunctionsTranslator` (`EXTRACT`-based date parts, `date_trunc`, interval arithmetic), `MathMemberTranslator` (custom `RoundAwayFromZero` without `ROUND` for non-bankers rounding), `StringMemberTranslator` (`STRING_AGG` for `string.Join`, supports `DISTINCT`, `ORDER BY`, `FILTER`), `GuidMemberTranslator` (cast to `VarChar(36)`), `PostgreSQLAggregateFunctionsMemberTranslator` (marks `IsFilterSupported = true`), `SqlTypesTranslation`.
- `PostgreSQL13MemberTranslator` -- extends base; overrides `TranslateNewGuidMethod` to use `gen_random_uuid()` (available v13+) instead of falling back to UUID v4 via extension (`PostgreSQL13MemberTranslator.cs:11--17`).

Selected via `CreateMemberTranslator` in `PostgreSQLDataProvider`: `>= v13` -> `PostgreSQL13MemberTranslator`, else `PostgreSQLMemberTranslator` (`PostgreSQLDataProvider.cs:88--94`).

#### DateTime/DateTimeOffset translation (updated PR #5467, PR #5517)

`DateFunctionsTranslator` overrides (`PostgreSQLMemberTranslator.cs:240--268`):

- `TranslateServerNow` (was `TranslateSqlCurrentTimestampUtc` before PR #5467) -- emits `CURRENT_TIMESTAMP` typed as `DateTime`.
- `TranslateNow` -- returns `null`; `LOCALTIMESTAMP` is not safe to use because Npgsql sessions do not set a session timezone by default.
- `TranslateUtcNow` -- emits `timezone('UTC', now())`. The inner `now()` call carries `DataType.DateTimeOffset` (i.e. `timestamptz`); wrapping with `timezone('UTC', ...)` converts to `timestamp without time zone`. Prior to PR #5467 the outer result type was incorrectly set to `DataType.DateTime2`; it is now plain `DateTime` dbType.
- `TranslateZonedUtcNow` -- emits `now()` directly, typed to the caller's requested `dbDataType`. Rationale: PostgreSQL does not store the original timezone; `Now` and `UtcNow` represent the same instant regardless of session offset.
- `TranslateDateTimeTruncationToDate` -- emits `Date_Trunc('day', dateExpression)`, returning the same dbType as the input (`PostgreSQLMemberTranslator.cs:144--153`).
- `TranslateDateTimeOffsetTruncationToDate` -- emits `Date_Trunc('day', dateExpression AT TIME ZONE 'UTC')::date` (`PostgreSQLMemberTranslator.cs:155--167`). The `AT TIME ZONE 'UTC'` step normalizes the `timestamptz` to UTC before truncation; the final `::date` cast (using `DataType.Date`) prevents the result from retaining timezone metadata. This addresses PR #5517 where the truncation returned a `timestamptz` with a preserved (incorrect) `DbType`.

### Schema provider

`PostgreSQLSchemaProvider` (`SchemaProviderBase` subclass, `PostgreSQLSchemaProvider.cs`). Notable behaviors:

- Queries `SHOW server_version_num` on connect to branch version-conditional SQL (v9.3 materialized views, v10 `IDENTITY` columns, v11 procedure `prokind`).
- Excludes `pg_catalog` and `information_schema` from schema lists automatically.
- `GetTables`: base `information_schema.tables` query plus `UNION ALL pg_matviews` for v9.3+; excludes partitioned child tables via `pg_inherits` left join.
- `GetColumns`: deep `pg_catalog` query detecting custom enums (`typtype = 'e'`) and custom ranges (`typtype = 'r'`); `IsIdentity` detected from `attidentity` (v10+) or `DEFAULT` containing `nextval`.
- `GetProcedures`: pre-v11 uses `proisagg`/`proretset`; v11+ uses `prokind` (`'f'`/`'p'`/`'a'`/`'w'`).
- `GetSystemType`: recurses for array types (`[]` suffix), maps built-in range/multirange type names to `NpgsqlRange<T>` and `List<NpgsqlRange<T>>`.

### Hints

`PostgreSQLHints` (partial class split between `PostgreSQLHints.cs` and `PostgreSQLHints.generated.cs`). Row-level locking hints:

- Lock modes: `FOR UPDATE`, `FOR NO KEY UPDATE`, `FOR SHARE`, `FOR KEY SHARE`.
- Modifiers: `NOWAIT`, `SKIP LOCKED`.
- `SubQueryTableHintExtensionBuilder`: emits the hint fragment; comments out `FOR NO KEY UPDATE` / `FOR KEY SHARE` on v92 (not supported, `PostgreSQLHints.cs:36--38`); suppresses `SKIP LOCKED` on v92/v93 (`PostgreSQLHints.cs:58--63`).
- `PostgreSQLHints.generated.cs`: T4-generated typed overloads (`ForUpdateHint`, `ForUpdateNoWaitHint`, `ForUpdateSkipLockedHint`, ...), one per hint x modifier combination (12 methods).

### Public extensions

`PostgreSQLExtensions.cs` provides server-side LINQ extensions accessible via `Sql.Ext.PostgreSQL()`:

- `ArrayAggregate<T>` -- `ARRAY_AGG(...)` with optional `DISTINCT`/`ORDER BY`.
- Array operators: `ConcatArrays` (`||`), `LessThan`/`GreaterThan`/comparisons, `Contains` (`@>`), `ContainedBy` (`<@`), `Overlaps` (`&&`).
- Array functions: `ArrayAppend`, `ArrayCat`, `ArrayNDims`, `ArrayDims`, `ArrayLength`, `ArrayPosition(s)`, `ArrayPrepend`, `ArrayRemove`, `ArrayReplace`, `ArrayUpper`, `Cardinality`, `ArrayToString`, `StringToArray`.
- Table-valued functions: `Unnest<T>` and `UnnestWithOrdinality<T>` via `FromSqlScalar`.
- `GenerateSeries` overloads (int and DateTime+interval).
- `GenerateSubscripts`.
- System functions: `VERSION()`, `CURRENT_CATALOG`, `CURRENT_DATABASE()`, `CURRENT_ROLE`, `CURRENT_SCHEMA`, `CURRENT_USER`, `SESSION_USER`.

### Registration

`PostgreSQLTools` (public static class, `PostgreSQLTools.cs`): `GetDataProvider`, `CreateDataConnection` (three overloads), `ResolvePostgreSQL` (path/assembly overloads). `PostgreSQLFactory` (internal, `PostgreSQLFactory.cs`): config-file `DataProviderFactoryBase` mapping version strings to `PostgreSQLVersion` enum values.

## Key types

| Type | File | Role |
|---|---|---|
| `PostgreSQLDataProvider` (abstract) | `PostgreSQLDataProvider.cs` | Core provider; 6 sealed subclasses |
| `PostgreSQLSqlBuilder` | `PostgreSQLSqlBuilder.cs` | SQL generation for all versions |
| `PostgreSQLSql15Builder` | `PostgreSQLSql15Builder.cs` | MERGE override for v15+ |
| `PostgreSQLSqlBuilder` (Merge partial) | `PostgreSQLSqlBuilder.Merge.cs` | MERGE operations, VALUES table typing |
| `PostgreSQLSqlOptimizer` | `PostgreSQLSqlOptimizer.cs` | DELETE/UPDATE rewrite, OUTPUT validation |
| `PostgreSQLSqlExpressionConvertVisitor` | `PostgreSQLSqlExpressionConvertVisitor.cs` | Operator/function mapping, type coercion |
| `NpgsqlProviderAdapter` | `NpgsqlProviderAdapter.cs` | Dynamic Npgsql wrapper (singleton) |
| `PostgreSQLProviderDetector` | `PostgreSQLProviderDetector.cs` | Auto-detect and lazy provider registry |
| `PostgreSQLMappingSchema` | `PostgreSQLMappingSchema.cs` | Type converters, native array registration |
| `PostgreSQLBulkCopy` | `PostgreSQLBulkCopy.cs` | Binary COPY or multi-row INSERT |
| `PostgreSQLSchemaProvider` | `PostgreSQLSchemaProvider.cs` | pg_catalog introspection |
| `PostgreSQLMemberTranslator` | `Translation/PostgreSQLMemberTranslator.cs` | LINQ->SQL function mapping |
| `PostgreSQL13MemberTranslator` | `Translation/PostgreSQL13MemberTranslator.cs` | `gen_random_uuid()` override |
| `PostgreSQLOptions` | `PostgreSQLOptions.cs` | BulkCopyType, NormalizeTimestampData, IdentifierQuoteMode |
| `PostgreSQLVersion` | `PostgreSQLVersion.cs` | Dialect enum: AutoDetect, v92--v18 |
| `PostgreSQLIdentifierQuoteMode` | `PostgreSQLIdentifierQuoteMode.cs` | None / Quote / Needed / Auto |
| `PostgreSQLHints` | `PostgreSQLHints.cs` + `.generated.cs` | Row-level locking hints |
| `PostgreSQLExtensions` | `PostgreSQLExtensions.cs` | Array ops, UNNEST, generate_series, system fns |
| `PostgreSQLTools` | `PostgreSQLTools.cs` | Public registration entry point |
| `PostgreSQLFactory` | `PostgreSQLFactory.cs` | Config-file factory |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full -- 11 files)

| File | Why Tier 1 |
|---|---|
| `Internal/.../PostgreSQLDataProvider.cs` | Core provider: version flags, type map, parameter routing |
| `Internal/.../PostgreSQLSqlBuilder.cs` | SQL generation: RETURNING, LATERAL, quoting, identity, sequences |
| `Internal/.../PostgreSQLSqlOptimizer.cs` | Statement rewrite: DELETE/UPDATE/OUTPUT |
| `DataProvider/.../PostgreSQLTools.cs` | Public registration entry point |
| `DataProvider/.../PostgreSQLVersion.cs` | Public version enum |
| `DataProvider/.../PostgreSQLOptions.cs` | Public provider options record |
| `DataProvider/.../PostgreSQLIdentifierQuoteMode.cs` | Public enum controlling SQL emission |
| `Internal/.../NpgsqlProviderAdapter.cs` | ADO.NET adapter; NpgsqlDbType mirror; bulk import wrappers |
| `Internal/.../PostgreSQLProviderDetector.cs` | Auto-detect and lazy provider registry |
| `Internal/.../PostgreSQLMappingSchema.cs` | Type converters, array registration, per-version subclasses |
| `Internal/.../PostgreSQLBulkCopy.cs` | Binary COPY strategy |

### Tier 2 (read in full -- 16 files)

| File | Notes |
|---|---|
| `Internal/.../PostgreSQLSql15Builder.cs` | v15 MERGE builder |
| `Internal/.../PostgreSQLSqlBuilder.Merge.cs` | MERGE operations partial |
| `Internal/.../PostgreSQLSqlExpressionConvertVisitor.cs` | Expression transforms |
| `Internal/.../PostgreSQLSchemaProvider.cs` | pg_catalog schema introspection |
| `Internal/.../Translation/PostgreSQLMemberTranslator.cs` | Baseline member translator |
| `Internal/.../Translation/PostgreSQL13MemberTranslator.cs` | v13 UUID override |
| `Internal/.../PostgreSQLSpecificQueryable.cs` | Marker wrapper |
| `Internal/.../PostgreSQLSpecificTable.cs` | Marker wrapper |
| `DataProvider/.../PostgreSQLHints.cs` | Locking hints + builder |
| `DataProvider/.../PostgreSQLHints.generated.cs` | T4-generated typed hint overloads |
| `DataProvider/.../PostgreSQLExtensions.cs` | Array ops, UNNEST, generate_series |
| `DataProvider/.../PostgreSQLSpecificExtensions.cs` | `AsPostgreSQL()` LINQ entry |
| `DataProvider/.../PostgreSQLFactory.cs` | Config-file factory |
| `DataProvider/.../IPostgreSQLExtensions.cs` | Marker interface for Sql.Ext pattern |
| `DataProvider/.../IPostgreSQLSpecificQueryable.cs` | Specific queryable marker |
| `DataProvider/.../IPostgreSQLSpecificTable.cs` | Specific table interface |

### Tier 3 (counted, not read -- 1 file)

| File | Reason |
|---|---|
| `DataProvider/.../PostgreSQLHints.tt` | T4 template -- code-generation source, not runtime logic |

## Inbound / outbound dependencies

**Outbound (this area uses):**
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md): `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`, `ProviderMemberTranslatorDefault`.
- [INTERNAL-API](../INTERNAL-API/INDEX.md): `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `TypeMapper`, `SchemaProviderBase`.
- [MAPPING](../MAPPING/INDEX.md): `LockedMappingSchema`, `MappingSchema`.

**Inbound (other areas use this):**
- Consumer code uses `PostgreSQLTools.GetDataProvider` / `CreateDataConnection`.
- `PostgreSQLOptions` is consumed via `DataOptions.FindOrDefault` in both `SetParameter` and bulk copy.

## Known issues / debt

- `PostgreSQLSql15Builder` exists but `PostgreSQLDataProvider.CreateSqlBuilder` always instantiates `PostgreSQLSqlBuilder` regardless of version (`PostgreSQLDataProvider.cs:261`). The v15 builder's `MERGE` override is therefore unreachable through the normal builder creation path unless a subclass overrides `CreateSqlBuilder`. The MERGE partial in the base builder (`PostgreSQLSqlBuilder.Merge.cs`) provides `MERGE` support directly from the base class -- this appears to be intentional (comment "we enable MERGE in base pgsql builder class intentionally"), but the v15 builder's role is ambiguous.
- `TODO` in `PostgreSQLSqlBuilder.Convert`: identifier quoting does not handle embedded double-quotes in identifiers or surrogate pairs (`PostgreSQLSqlBuilder.cs:155--157`).
- `float(N)` precision mapping in `GetNativeType` has a copy-paste error: both `precision 1--24` and `25--53` branches assign `"real"` instead of `"double precision"` for the second range (`PostgreSQLDataProvider.cs:556--560`).
- `NpgsqlCidr` type handling has TFM-conditional branches (`#if NET8_0_OR_GREATER`) with hardcoded assembly-qualified type strings for older TFMs (`PostgreSQLSchemaProvider.cs:128--134`).
- `ConvertDateTimeToDateTimeOffset` helper removed in PR #5467: the helper clamped `DateTime.Min/Max` to `DateTimeOffset.Min/Max` to avoid offset arithmetic failures when Npgsql returns +/-infinity as `DateTime.MinValue`/`MaxValue`. The replacement (`rd.GetFieldValue<DateTimeOffset>(i)` scoped to `"timestamp with time zone"`) may surface that edge case differently; watch for regressions on infinity-valued `timestamptz` columns.

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer` base implementations.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `TypeMapper`.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- `MappingSchema`, `LockedMappingSchema`.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- reference for PROV-* area structure and shared patterns (multi-version builder hierarchy, provider detector, bulk copy strategy).

<details><summary>Coverage</summary>

### Tier 1 (11/11 read this run)

- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs` -- core provider
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlBuilder.cs` -- SQL generation
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlOptimizer.cs` -- statement rewrite
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLTools.cs` -- public entry
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLVersion.cs` -- version enum
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLOptions.cs` -- provider options
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLIdentifierQuoteMode.cs` -- quote mode enum
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/NpgsqlProviderAdapter.cs` -- Npgsql adapter
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLProviderDetector.cs` -- auto-detect
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLMappingSchema.cs` -- type mapping
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLBulkCopy.cs` -- bulk copy

### Tier 2 (16/16 read this run)

- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSql15Builder.cs` -- v15 MERGE builder
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlBuilder.Merge.cs` -- MERGE partial
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlExpressionConvertVisitor.cs` -- expression visitor
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSchemaProvider.cs` -- schema provider
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs` -- member translator (updated PR #5467, PR #5517)
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQL13MemberTranslator.cs` -- v13 translator
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSpecificQueryable.cs` -- marker wrapper
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSpecificTable.cs` -- marker wrapper
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLHints.cs` -- locking hints
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLHints.generated.cs` -- generated overloads
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLExtensions.cs` -- array ops, UNNEST, generate_series
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLSpecificExtensions.cs` -- AsPostgreSQL()
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLFactory.cs` -- config factory
- `Source/LinqToDB/DataProvider/PostgreSQL/IPostgreSQLExtensions.cs` -- marker interface
- `Source/LinqToDB/DataProvider/PostgreSQL/IPostgreSQLSpecificQueryable.cs` -- specific queryable interface
- `Source/LinqToDB/DataProvider/PostgreSQL/IPostgreSQLSpecificTable.cs` -- specific table interface

### Tier 3 (1 file -- counted, not read)

- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLHints.tt` -- T4 code-generation template; runtime logic is entirely in `.generated.cs`

### Delta reads (this run)

Changed files verified against current SHA `4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7`:

- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs` -- read in full; PR #5467 now/utcnow rework + PR #5517 Date_Trunc truncation fix confirmed.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs` -- read lines 1--120; PR #5467 DateTimeOffset reader field change confirmed.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLMappingSchema.cs` -- read in full; PR #5467 TIMESTAMPTZ_FORMAT + BuildDateTimeOffset addition confirmed. PR #5451 (DuckDB) produced no PostgreSQL-visible changes in this file.

</details>
