---
area: PROV-POSTGRES
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 11/11
coverage_tier_2: 19/19
---

# PROV-POSTGRES

PostgreSQL provider. Single ADO.NET dependency: **Npgsql** (loaded dynamically). Covers PostgreSQL 9.2 through 19+. All public API lives in `LinqToDB.DataProvider.PostgreSQL`; all implementation lives in `LinqToDB.Internal.DataProvider.PostgreSQL`.

## Subsystems

### Version matrix

Seven concrete sealed subclasses of `PostgreSQLDataProvider` are defined in `PostgreSQLDataProvider.cs:24--30`, one per supported dialect:

| Class | ProviderName constant | Key capabilities unlocked |
|---|---|---|
| `PostgreSQLDataProvider92` | `PostgreSQL92` | baseline; no `APPLY` join, no upsert |
| `PostgreSQLDataProvider93` | `PostgreSQL93` | `APPLY` join; no upsert |
| `PostgreSQLDataProvider95` | `PostgreSQL95` | upsert (`ON CONFLICT`) |
| `PostgreSQLDataProvider13` | `PostgreSQL13` | `gen_random_uuid()`, v13 member translator, `AS [NOT] MATERIALIZED` CTE hint (`PostgreSQL13SqlBuilder`) |
| `PostgreSQLDataProvider15` | `PostgreSQL15` | `MERGE` statement (`PostgreSQLSql15Builder`); `IsUpsertWithMergeLoweringSupported` |
| `PostgreSQLDataProvider18` | `PostgreSQL18` | `OUTPUT`/`RETURNING` via special table; native `uuidv7()` (`PostgreSQL18MemberTranslator`) |
| `PostgreSQLDataProvider19` | `PostgreSQL19` | Window `IGNORE`/`RESPECT NULLS` on value/offset functions (`PostgreSQL19MemberTranslator`) |

`PostgreSQLDataProvider` constructor sets `SqlProviderFlags` version-conditionally: `IsApplyJoinSupported` is false only for v92; `IsInsertOrUpdateSupported` is false for v92 and v93; `OutputDeleteUseSpecialTable` and siblings require v18+; `IsUpsertWithMergeLoweringSupported` requires v15+ (below v15, Upsert configurations that need MERGE lowering fail with `Error_Upsert_MergeLowering_NotSupported`). `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs:45--72`.

### SQL builder hierarchy

Three classes:

- `PostgreSQLSqlBuilder` -- base for all versions. Handles `RETURNING` for identity, `LIMIT`/`OFFSET`, `LATERAL` joins, identifier quoting, `SERIAL`/`SMALLSERIAL`/`BIGSERIAL` for `CREATE TABLE`, `ON CONFLICT` for upsert, PostgreSQL-flavored cast syntax (`::type`), `RECURSIVE` CTE keyword, sequence `nextval(...)` expressions. File: `PostgreSQLSqlBuilder.cs`. As of PR #5504, the builder declares `ConcatStyle => ConcatBuildStyle.Pipes` (`PostgreSQLSqlBuilder.cs:43`), delegating string concatenation to the base `BasicSqlBuilder` `SqlConcatExpression` path rather than handling `+` -> `||` conversion in the expression visitor. `SupportsMaterializedCteHint` defaults to `false` here (`PostgreSQLSqlBuilder.cs:41`) -- `AS [NOT] MATERIALIZED` requires PostgreSQL 12+, which pre-v13 providers do not have.
- `PostgreSQL13SqlBuilder` -- v13+ only, selected by `PostgreSQLDataProvider.CreateSqlBuilder`. Overrides `SupportsMaterializedCteHint => true` (`PostgreSQL13SqlBuilder.cs:24`), enabling the `AS [NOT] MATERIALIZED` CTE hint. `v13` is the lowest `PostgreSQLVersion` enum entry `>= 12` (PostgreSQL added the hint in 12), per the file header comment (`PostgreSQL13SqlBuilder.cs:7--10`); support lives in the builder *type* rather than a runtime `Version` check so it stays consistent when SQL is built remotely via LinqService. File: `PostgreSQL13SqlBuilder.cs`.
- `PostgreSQLSql15Builder` -- v15+ only. Overrides `BuildInsertOrUpdateQuery` to emit `MERGE` via `BuildInsertOrUpdateQueryAsMerge` instead of `ON CONFLICT`. File: `PostgreSQLSql15Builder.cs:24--27`.

**Correction (this delta):** `PostgreSQLDataProvider.CreateSqlBuilder` (`PostgreSQLDataProvider.cs:260--265`) now branches on `Version >= PostgreSQLVersion.v13`, returning `PostgreSQL13SqlBuilder` for v13+ and the base `PostgreSQLSqlBuilder` below it -- the prior claim here that `CreateSqlBuilder` *always* returns the base builder regardless of version is no longer accurate. The branch exists for the CTE-materialization-hint builder above, not for `PostgreSQLSql15Builder`: `PostgreSQL13SqlBuilder` extends `PostgreSQLSqlBuilder` directly, not `PostgreSQLSql15Builder`. So the underlying known issue still holds -- `PostgreSQLSql15Builder`'s `MERGE` override remains unreachable from `CreateSqlBuilder` for any version, v15 included; `PostgreSQLDataProvider15` still does not override `CreateSqlBuilder` itself, so it inherits the (now version-branching) base implementation, which never returns `PostgreSQLSql15Builder`. The Merge partial (`PostgreSQLSqlBuilder.Merge.cs`) contains `BuildMergeOperationDeleteBySource` and `BuildMergeOperationUpdateBySource` annotated "available since PGSQL17" -- these are enabled in the base builder class to allow per-query dialect negotiation without requiring a version-specific builder.

The `PostgreSQLSqlBuilder.Merge.cs` partial also defines `IsSqlValuesTableValueTypeRequired`, which forces explicit type annotation on the first row of a `VALUES` table for `long`, `float`, `double`, `decimal`, `NULL`-only columns, and JSON/JSONB columns (`PostgreSQLSqlBuilder.Merge.cs:16--37`).

### Identifier quoting

Controlled by `PostgreSQLIdentifierQuoteMode` (public enum, `PostgreSQLIdentifierQuoteMode.cs`):

- `None` -- never quote.
- `Quote` -- always quote.
- `Needed` -- quote only for reserved words and whitespace.
- `Auto` (default) -- quote for reserved words, prohibited characters, and **uppercase letters** (critical for case-folding behavior; PostgreSQL folds unquoted identifiers to lowercase).

Logic is in `PostgreSQLSqlBuilder.Convert` (`PostgreSQLSqlBuilder.cs:163--218`). Parameter names use `:name` syntax (colon prefix, not `@`).

### NpgsqlProviderAdapter

Singleton (`NpgsqlProviderAdapter.GetInstance()`, locked double-check, `NpgsqlProviderAdapter.cs:198--203`). Loads `Npgsql.dll` via `Common.Tools.TryLoadAssembly`. Wraps:

- `NpgsqlConnection`, `NpgsqlParameter`, `NpgsqlDataReader`, `NpgsqlCommand`, `NpgsqlTransaction`, `NpgsqlBinaryImporter`.
- `NpgsqlDbType` enum -- mirrored as an internal `[Wrapper]` enum because Npgsql changes numeric field values between major versions; `ApplyDbTypeFlags` (`NpgsqlProviderAdapter.cs:172--187`) reconstructs flag composites (Array | Range | Multirange) against the runtime numeric values.
- Provider-specific geometric and temporal CLR types: `NpgsqlPoint`, `NpgsqlBox`, `NpgsqlCircle`, `NpgsqlLSeg`, `NpgsqlPath`, `NpgsqlPolygon`, `NpgsqlLine`, `NpgsqlInet`, `NpgsqlRange<T>`, and conditionally `NpgsqlDate`, `NpgsqlDateTime`, `NpgsqlTimeSpan`, `NpgsqlInterval`, `NpgsqlCidr`, `NpgsqlCube`.
- `BigInteger` support conditional on Npgsql >= 6.0 (`NpgsqlProviderAdapter.cs:196`).
- `BeginBinaryImport` / `BeginBinaryImportAsync` delegates for bulk copy.

### Provider detection

`PostgreSQLProviderDetector` extends `ProviderDetectorBase<Provider, PostgreSQLVersion>` (`PostgreSQLProviderDetector.cs:10`). Seven `Lazy<IDataProvider>` statics, one per version (`_postgreSQLDataProvider19` added this delta). Detection order:

1. Exact `ProviderName.*` string match.
2. Configuration string contains version number substring (e.g. `"15"`, `"16"`, `"17"` -> v15; `"18"` -> v18; `"19"` -> v19).
3. `AutoDetectProvider` = true -> `DetectServerVersion` reads `connection.PostgreSqlVersion` from the live connection wrapper and pattern-matches on `Major`/`Minor`, now leading with `{ Major: >= 19 } => PostgreSQLVersion.v19` (`PostgreSQLProviderDetector.cs:111--123`).
4. Fallback to `DefaultVersion` = `v92`.

### Mapping schema

`PostgreSQLMappingSchema` (`LockedMappingSchema`, `PostgreSQLMappingSchema.cs`) registers:

- Column name comparison: `OrdinalIgnoreCase` (matches PostgreSQL's case-insensitive catalog).
- Value-to-SQL converters for `bool` (native `TRUE`/`FALSE`), `string`, `char`, `byte[]` (hex-escaped `E'\x...'::bytea`), `Guid` (`'...'::uuid`), `DateTime` (inline `'...'::timestamp` or `'...'::date`), `DateTimeOffset` (`'...'::timestamptz`, microsecond precision, added PR #5467), `BigInteger`.
- Float/double special values: `NaN`, `Infinity` quoted and cast (`'NaN'::float4`, `'NaN'::float8`).
- Unsigned integer workarounds: `ushort` -> `int`, `uint` -> `long`, `ulong` -> `decimal(20,0)`.
- Native array types registered as scalars (enables correct query cache keying and parameter detection): all primitive CLR arrays, `List<T>`, `IReadOnlyList<T>` for ~20 element types.
- Seven per-version sealed subclasses (`PostgreSQL92MappingSchema` ... `PostgreSQL19MappingSchema`) extend from `NpgsqlProviderAdapter.GetInstance().MappingSchema` and the base `PostgreSQLMappingSchema.Instance` (`PostgreSQLMappingSchema.cs:202--214`; `PostgreSQL19MappingSchema` added this delta).

**Note:** `PostgreSQLDataProvider.GetMappingSchema` (`PostgreSQLDataProvider.cs:602--613`) has no explicit `PostgreSQLVersion.v13` arm -- v13 providers fall through to `_` and receive `PostgreSQL95MappingSchema`, not the dedicated `PostgreSQL13MappingSchema` class defined above. See Known issues.

The `TIMESTAMPTZ_FORMAT` constant (`'...'::timestamptz`, `PostgreSQLMappingSchema.cs:26/31`) formats `DateTimeOffset` values with microsecond precision and timezone offset. `BuildDateTimeOffset` (`PostgreSQLMappingSchema.cs:163--166`) applies it via `AppendFormat`. This was absent before PR #5467 -- `DateTimeOffset` had no registered converter and fell through to a default path.

### Parameter type resolution

`PostgreSQLDataProvider.SetParameter` handles two special cases before delegating to base (`PostgreSQLDataProvider.cs:305--317`):

1. `IDictionary` with `DataType.Undefined` -> promoted to `DataType.Dictionary` (maps to `hstore`).
2. `DateTime`/`DateTimeOffset` normalization: when `NormalizeTimestampData` is true, `DateTimeOffset` is converted to UTC, and `DateTime` gets `DateTimeKind` adjusted to match `timestamp` vs `timestamptz` expectations introduced in Npgsql 6 (`PostgreSQLDataProvider.cs:277--303`).

`SetParameterType` maps `DataType` -> `NpgsqlDbType` and calls `Adapter.SetDbType(param, type)`. If the provider parameter is not available (wrapped connection), falls back to `DbType`.

`GetNativeType(string? dbType)` (`PostgreSQLDataProvider.cs:446--600`) normalizes type name aliases (e.g. `int4` -> `integer`, `timestamptz` -> `timestamp with time zone`), detects array `[]` suffix and range type names, and returns the correct `NpgsqlDbType` with flags composed via `ApplyDbTypeFlags`.

`SetProviderField` for reading `DateTimeOffset` from `DateTime` reader columns is scoped to `"timestamp with time zone"` columns only and uses `rd.GetFieldValue<DateTimeOffset>(i)` directly (`PostgreSQLDataProvider.cs:80`). The prior `ConvertDateTimeToDateTimeOffset` helper -- which clamped `DateTime.Min/Max` to avoid offset failures for +/-infinity values -- was removed in PR #5467. Agents verifying infinity handling in `timestamptz` columns should note this removal.

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
- `ConcatRequiresExplicitStringCast` = `false` (PR #5504: string concatenation is now handled by `BasicSqlBuilder` via `ConcatBuildStyle.Pipes`; explicit casts are not required for the `||` operator in PostgreSQL).
- `ConvertSearchStringPredicate`: case-insensitive search rewrites `LIKE` -> `ILIKE`.
- `ConvertSqlBinaryExpression`: `^` -> `#` (XOR), `%` casts non-decimal operand to `decimal` (PostgreSQL modulo supports only decimal/numeric). The prior `+` on strings -> `||` case has been removed -- string concat is handled at the builder level via `ConcatStyle.Pipes` since PR #5504.
- `ConvertSqlFunction`: `CharIndex` -> `Position(... in ...)` with 2- and 3-argument forms.
- `VisitExprExprPredicate`: JSON/JSONB equality comparisons cast mixed `json`/`jsonb` operands to `jsonb` (`PostgreSQLSqlExpressionConvertVisitor.cs:109--128`).
- `ConvertConversion`: `bool` targets use `ConvertBooleanToCase` unless already boolean expression; applies `FloorBeforeConvert` for numeric narrowing.
- `WrapColumnExpression`: `uint`/`long`/`ulong`/`float`/`double`/`decimal` literal values and non-query parameters get mandatory cast to prevent Npgsql type inference failures.

### Member translators

Four classes in `Translation/`, forming a linear inheritance chain `PostgreSQLMemberTranslator` -> `PostgreSQL13MemberTranslator` -> `PostgreSQL18MemberTranslator` -> `PostgreSQL19MemberTranslator`:

- `PostgreSQLMemberTranslator` -- baseline. Inner classes: `DateFunctionsTranslator` (`EXTRACT`-based date parts, `date_trunc`, interval arithmetic), `MathMemberTranslator` (custom `RoundAwayFromZero` without `ROUND` for non-bankers rounding), `StringMemberTranslator` (`STRING_AGG` for `string.Join`, supports `DISTINCT`, `ORDER BY`, `FILTER`), `GuidMemberTranslator` (cast to `VarChar(36)`), `PostgreSQLAggregateFunctionsMemberTranslator` (marks `IsFilterSupported = true`), `SqlTypesTranslation`, and `PostgreSQLWindowFunctionsMemberTranslator` (`PostgreSQLMemberTranslator.cs:450--461`) -- sets `IsWindowFilterSupported`, `IsOrderedSetFilterSupported`, `IsHypotheticalSetSupported`, `IsVarianceSupported`, `IsVarianceBareSupported`, `IsCorrelationSupported`, and `IsLinearRegressionSupported` all `true` (PostgreSQL supports the full statistical/regression window-function set under standard SQL names), selected via `CreateWindowFunctionsMemberTranslator` override (`PostgreSQLMemberTranslator.cs:463--466`).
- `PostgreSQL13MemberTranslator` -- extends base; overrides `TranslateNewGuidMethod` to use `gen_random_uuid()` (available v13+) instead of falling back to UUID v4 via extension (`PostgreSQL13MemberTranslator.cs:11--15`).
- `PostgreSQL18MemberTranslator` -- extends `PostgreSQL13MemberTranslator`; overrides `TranslateNewGuid7Method` to emit the built-in `uuidv7()` server function (available since PostgreSQL 18), backing `Guid` v7 generation (`PostgreSQL18MemberTranslator.cs:12--16`).
- `PostgreSQL19MemberTranslator` -- extends `PostgreSQL18MemberTranslator`; overrides `CreateWindowFunctionsMemberTranslator` to return a nested `PostgreSQL19WindowFunctionsMemberTranslator` (itself extending `PostgreSQLWindowFunctionsMemberTranslator`) with `IsLeadLagNullTreatmentSupported = true` and `IsValueNullTreatmentSupported = true` (`PostgreSQL19MemberTranslator.cs:11--20`). This enables SQL-standard `RESPECT`/`IGNORE NULLS` on value/offset window functions (`FIRST_VALUE`/`LAST_VALUE`/`NTH_VALUE`, `LEAD`/`LAG`), emitted after the argument list per `BasicSqlBuilder`'s default `WindowNullsPlacement.AfterClose`.

Selected via `CreateMemberTranslator` in `PostgreSQLDataProvider`: `>= v19` -> `PostgreSQL19MemberTranslator`, `>= v18` -> `PostgreSQL18MemberTranslator`, `>= v13` -> `PostgreSQL13MemberTranslator`, else -> `PostgreSQLMemberTranslator` (`PostgreSQLDataProvider.cs:98--107`).

#### DateTime/DateTimeOffset translation (updated PR #5467, PR #5517)

`DateFunctionsTranslator` overrides (`PostgreSQLMemberTranslator.cs:240--268`):

- `TranslateServerNow` (was `TranslateSqlCurrentTimestampUtc` before PR #5467) -- emits `CURRENT_TIMESTAMP` typed as `DateTime`.
- `TranslateNow` -- returns `null`; `LOCALTIMESTAMP` is not safe to use because Npgsql sessions do not set a session timezone by default.
- `TranslateUtcNow` -- emits `timezone('UTC', now())`. The inner `now()` call carries `DataType.DateTimeOffset` (i.e. `timestamptz`); wrapping with `timezone('UTC', ...)` converts to `timestamp without time zone`. Prior to PR #5467 the outer result type was incorrectly set to `DataType.DateTime2`; it is now plain `DateTime` dbType.
- `TranslateZonedUtcNow` -- emits `now()` directly, typed to the caller's requested `dbDataType`. Rationale: PostgreSQL does not store the original timezone; `Now` and `UtcNow` represent the same instant regardless of session offset.
- `TranslateDateTimeTruncationToDate` -- emits `Date_Trunc('day', dateExpression)`, returning the same dbType as the input (`PostgreSQLMemberTranslator.cs:144--153`).
- `TranslateDateTimeOffsetTruncationToDate` -- emits `Date_Trunc('day', dateExpression AT TIME ZONE 'UTC')::date` (`PostgreSQLMemberTranslator.cs:155--167`). The `AT TIME ZONE 'UTC'` step normalizes the `timestamptz` to UTC before truncation; the final `::date` cast (using `DataType.Date`) prevents the result from retaining timezone metadata. This addresses PR #5517 where the truncation returned a `timestamptz` with a preserved (incorrect) `DbType`.

#### String member translation (updated PR #5504, PR #5515)

`StringMemberTranslator.TranslateStringJoin` (`PostgreSQLMemberTranslator.cs:375--469`) emits `STRING_AGG` for `string.Join` / `string.Concat`. As of PR #5504 the `withoutSeparator` path (used when translating `string.Concat` via the new `SqlConcatExpression`) is corrected:

- `withoutSeparator = false` (normal `string.Join` with separator): `c.HasSequenceIndex(1).TranslateArguments(0)` -- value is at sequence index 1, separator argument at index 0.
- `withoutSeparator = true` (`string.Concat` / no explicit separator): `c.HasSequenceIndex(0)` -- value is at sequence index 0 (no argument translation); the separator passed to `STRING_AGG` is `factory.Value(valueType, string.Empty)` (empty string literal). The prior wiring incorrectly placed the sequence values in the separator slot of `STRING_AGG`.

`TrimStart` and `TrimEnd` translation for PostgreSQL is handled by `StringMemberTranslatorBase` (the base class); no PostgreSQL-specific override is required -- PR #5515 added the base translations that the PostgreSQL provider inherits.

### Schema provider

`PostgreSQLSchemaProvider` (`SchemaProviderBase` subclass, `PostgreSQLSchemaProvider.cs`). Notable behaviors:

- Queries `SHOW server_version_num` on connect to branch version-conditional SQL (v9.3 materialized views, v10 `IDENTITY` columns, v11 procedure `prokind`).
- Excludes `pg_catalog` and `information_schema` from schema lists automatically.
- `GetTables`: base `information_schema.tables` query plus `UNION ALL pg_matviews` for v9.3+; excludes partitioned child tables via `pg_inherits` left join.
- `GetColumns`: deep `pg_catalog` query detecting custom enums (`typtype = 'e'`) and custom ranges (`typtype = 'r'`); `IsIdentity` is `true` when native `attidentity` (v10+) reports `'a'`/`'d'`, OR (no native identity on the table AND the column is the table's chosen serial-style fallback). The fallback picks one `DEFAULT nextval(...)` column per table via a windowed `MIN(...) OVER (PARTITION BY TableID)`, preferring a primary-key column when one of the `nextval(...)` defaults is on the PK, otherwise the first such column by ordinal (`PostgreSQLSchemaProvider.cs:288--436`). This intentionally reports at most one linq2db-identity candidate per table even when several columns have `nextval(...)` defaults.
- `GetProcedures`: pre-v11 uses `proisagg`/`proretset`; v11+ uses `prokind` (`'f'`/`'p'`/`'a'`/`'w'`).
- `GetSystemType`: recurses for array types (`[]` suffix), maps built-in range/multirange type names to `NpgsqlRange<T>` and `List<NpgsqlRange<T>>`.

### Hints

`PostgreSQLHints` (partial class split between `PostgreSQLHints.cs` and `PostgreSQLHints.generated.cs`). Row-level locking hints:

- Lock modes: `FOR UPDATE`, `FOR NO KEY UPDATE`, `FOR SHARE`, `FOR KEY SHARE`.
- Modifiers: `NOWAIT`, `SKIP LOCKED`.
- `SubQueryTableHintExtensionBuilder`: emits the hint fragment; comments out `FOR NO KEY UPDATE` / `FOR KEY SHARE` on v92 (not supported, `PostgreSQLHints.cs:36--37`); suppresses `SKIP LOCKED` unless the mapping schema's `ConfigurationList` reports v95/v13/v15/v18/v19 (`PostgreSQLHints.cs:56--63`; the `PostgreSQL19` arm was added this delta).
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

`PostgreSQLTools` (public static class, `PostgreSQLTools.cs`): `GetDataProvider`, `CreateDataConnection` (three overloads), `ResolvePostgreSQL` (path/assembly overloads). `PostgreSQLFactory` (internal, `PostgreSQLFactory.cs`): config-file `DataProviderFactoryBase` mapping version strings to `PostgreSQLVersion` enum values (includes `"18" => v18` and `"19" => v19` as of this delta).

## Key types

| Type | File | Role |
|---|---|---|
| `PostgreSQLDataProvider` (abstract) | `PostgreSQLDataProvider.cs` | Core provider; 7 sealed subclasses |
| `PostgreSQLSqlBuilder` | `PostgreSQLSqlBuilder.cs` | SQL generation for all versions |
| `PostgreSQL13SqlBuilder` | `PostgreSQL13SqlBuilder.cs` | v13+ CTE `AS [NOT] MATERIALIZED` hint builder |
| `PostgreSQLSql15Builder` | `PostgreSQLSql15Builder.cs` | MERGE override for v15+ (unreachable, see Known issues) |
| `PostgreSQLSqlBuilder` (Merge partial) | `PostgreSQLSqlBuilder.Merge.cs` | MERGE operations, VALUES table typing |
| `PostgreSQLSqlOptimizer` | `PostgreSQLSqlOptimizer.cs` | Statement rewrite: DELETE/UPDATE/OUTPUT |
| `PostgreSQLSqlExpressionConvertVisitor` | `PostgreSQLSqlExpressionConvertVisitor.cs` | Operator/function mapping, type coercion |
| `NpgsqlProviderAdapter` | `NpgsqlProviderAdapter.cs` | Dynamic Npgsql wrapper (singleton) |
| `PostgreSQLProviderDetector` | `PostgreSQLProviderDetector.cs` | Auto-detect and lazy provider registry |
| `PostgreSQLMappingSchema` | `PostgreSQLMappingSchema.cs` | Type converters, native array registration |
| `PostgreSQLBulkCopy` | `PostgreSQLBulkCopy.cs` | Binary COPY or multi-row INSERT |
| `PostgreSQLSchemaProvider` | `PostgreSQLSchemaProvider.cs` | pg_catalog introspection |
| `PostgreSQLMemberTranslator` | `Translation/PostgreSQLMemberTranslator.cs` | LINQ->SQL function mapping |
| `PostgreSQL13MemberTranslator` | `Translation/PostgreSQL13MemberTranslator.cs` | `gen_random_uuid()` override |
| `PostgreSQL18MemberTranslator` | `Translation/PostgreSQL18MemberTranslator.cs` | `uuidv7()` override |
| `PostgreSQL19MemberTranslator` | `Translation/PostgreSQL19MemberTranslator.cs` | Window `RESPECT`/`IGNORE NULLS` override |
| `PostgreSQLOptions` | `PostgreSQLOptions.cs` | BulkCopyType, NormalizeTimestampData, IdentifierQuoteMode |
| `PostgreSQLVersion` | `PostgreSQLVersion.cs` | Dialect enum: AutoDetect, v92--v19 |
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

### Tier 2 (read in full -- 19 files)

| File | Notes |
|---|---|
| `Internal/.../PostgreSQL13SqlBuilder.cs` | v13+ CTE materialization hint (new file this delta) |
| `Internal/.../PostgreSQLSql15Builder.cs` | v15 MERGE builder (unreachable, see Known issues) |
| `Internal/.../PostgreSQLSqlBuilder.Merge.cs` | MERGE operations partial |
| `Internal/.../PostgreSQLSqlExpressionConvertVisitor.cs` | Expression transforms (updated PR #5504) |
| `Internal/.../PostgreSQLSchemaProvider.cs` | pg_catalog schema introspection |
| `Internal/.../Translation/PostgreSQLMemberTranslator.cs` | Baseline member translator (updated PR #5467, PR #5517, PR #5504, PR #5515; window-fn translator confirmed this delta) |
| `Internal/.../Translation/PostgreSQL13MemberTranslator.cs` | v13 UUID override |
| `Internal/.../Translation/PostgreSQL18MemberTranslator.cs` | v18 `uuidv7()` override (new file this delta) |
| `Internal/.../Translation/PostgreSQL19MemberTranslator.cs` | v19 window null-treatment override (new file this delta) |
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

- `PostgreSQLSql15Builder` exists but is unreachable through the normal builder creation path: `PostgreSQLDataProvider.CreateSqlBuilder` (`PostgreSQLDataProvider.cs:260--265`) now branches by version (added this delta, for the unrelated `PostgreSQL13SqlBuilder` CTE-hint concern -- see SQL builder hierarchy above), but neither branch constructs `PostgreSQLSql15Builder`, and `PostgreSQLDataProvider15` does not override `CreateSqlBuilder` itself. The v15 builder's `MERGE` override (`BuildInsertOrUpdateQuery` via `BuildInsertOrUpdateQueryAsMerge`) is therefore still dead code, same as before this delta -- only the previously-documented *reason* ("always instantiates the base builder regardless of version") is now stale and has been corrected above. The MERGE partial in the base builder (`PostgreSQLSqlBuilder.Merge.cs`) provides `MERGE` support directly from the base class -- this appears to be intentional (comment "we enable MERGE in base pgsql builder class intentionally"), but the v15 builder's role remains ambiguous.
- `PostgreSQLDataProvider.GetMappingSchema` (`PostgreSQLDataProvider.cs:602--613`) has no `PostgreSQLVersion.v13` arm in its switch expression -- v13 providers fall through to the `_` default and receive `PostgreSQL95MappingSchema`, not the dedicated `PostgreSQL13MappingSchema` sealed class defined alongside the other six per-version mapping schemas (`PostgreSQLMappingSchema.cs:208`). Same shape as the `PostgreSQLSql15Builder` issue above: a version-specific type exists but the dispatch point does not select it. Practical consequence: a v13-configured connection's `MappingSchema.ConfigurationList` reports `ProviderName.PostgreSQL95` instead of `ProviderName.PostgreSQL13` -- code that branches on `ConfigurationList.Contains(ProviderName.PostgreSQL13, ...)` (e.g. the `SubQueryTableHintExtensionBuilder` SkipLocked check in `PostgreSQLHints.cs`) would miss a v13 connection on that specific check, though in the observed `PostgreSQLHints.cs` case the same `||` chain also checks `ProviderName.PostgreSQL95`, so the visible behavior happens to still be correct there by coincidence. (Found this delta.)
- `TODO` in `PostgreSQLSqlBuilder.Convert`: identifier quoting does not handle embedded double-quotes in identifiers or surrogate pairs (`PostgreSQLSqlBuilder.cs:165--167`).
- `float(N)` precision mapping in `GetNativeType` has a copy-paste error: both `precision 1--24` and `25--53` branches assign `"real"` instead of `"double precision"` for the second range (`PostgreSQLDataProvider.cs:558--561`).
- `NpgsqlCidr` type handling has TFM-conditional branches (`#if NET8_0_OR_GREATER`) with hardcoded assembly-qualified type strings for older TFMs (`PostgreSQLSchemaProvider.cs:127--134`).
- `ConvertDateTimeToDateTimeOffset` helper removed in PR #5467: the helper clamped `DateTime.Min/Max` to `DateTimeOffset.Min/Max` to avoid offset arithmetic failures when Npgsql returns +/-infinity as `DateTime.MinValue`/`MaxValue`. The replacement (`rd.GetFieldValue<DateTimeOffset>(i)` scoped to `"timestamp with time zone"`) may surface that edge case differently; watch for regressions on infinity-valued `timestamptz` columns.

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer` base implementations.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `TypeMapper`.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- `MappingSchema`, `LockedMappingSchema`.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- reference for PROV-* area structure and shared patterns (multi-version builder hierarchy, provider detector, bulk copy strategy).

<details><summary>Coverage</summary>

- Tier 1 (11/11 read this run)
- Tier 2 (19/19 read this run)
- Tier 3 (1 file -- counted, not read)

### Delta reads (this run -- PostgreSQL 19 support + CTE materialization hint)

Changed files verified against current SHA `36ee4f82f06eaf242b052ade8c87121d251a6165`:

- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLVersion.cs` -- read in full; added `v19` enum value ("PostgreSQL 19+ SQL dialect").
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs` -- read in full; added sealed `PostgreSQLDataProvider19` (line 30); `CreateMemberTranslator` gained `>= v19`/`>= v18` arms ahead of the existing `>= v13` arm (lines 98--107); `CreateSqlBuilder` (lines 260--265) now branches `Version >= v13` between `PostgreSQL13SqlBuilder` and the base `PostgreSQLSqlBuilder` -- corrects the prior INDEX claim that it always returned the base builder (see SQL builder hierarchy + Known issues); `GetProviderName` and `GetMappingSchema` both gained `v19` arms; confirmed `GetMappingSchema` still has no `v13` arm (new known issue, see below); float(N) copy-paste bug at lines 558--561 still present.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlBuilder.cs` -- read in full; added `SupportsMaterializedCteHint => false` override (line 41) with a comment pointing at the new `PostgreSQL13SqlBuilder` for v13+; `ConcatStyle` shifted to line 43.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQL13SqlBuilder.cs` -- **new file**; read in full. `PostgreSQL13SqlBuilder : PostgreSQLSqlBuilder`, overrides `SupportsMaterializedCteHint => true` for the `AS [NOT] MATERIALIZED` CTE hint (PostgreSQL 12+). New Tier-2 file.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLMappingSchema.cs` -- read in full; added sealed `PostgreSQL19MappingSchema` (line 214). Confirmed `PostgreSQL13MappingSchema` (line 208) is a pre-existing sealed class not selected by `PostgreSQLDataProvider.GetMappingSchema`'s switch (new known issue).
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLProviderDetector.cs` -- read in full; added `_postgreSQLDataProvider19` static, a `ConfigurationString.Contains("19")` branch, a `PostgreSQLVersion.v19` arm in `GetDataProvider`, and a `{ Major: >= 19 }` arm (checked first) in `DetectServerVersion`.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSchemaProvider.cs` -- read in full; `GetColumns`'s identity-detection query is more elaborate than previously documented (windowed sequence-default fallback with primary-key tie-break) -- documentation updated in Schema provider subsystem; no v19-specific changes found.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs` -- read in full; contains `PostgreSQLWindowFunctionsMemberTranslator` inner class + `CreateWindowFunctionsMemberTranslator` override (lines 450--466), not previously documented in this INDEX. `DateFunctionsTranslator`/`StringMemberTranslator` bodies confirmed unchanged from prior delta reads.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQL13MemberTranslator.cs` -- read in full; unchanged (`gen_random_uuid()` override, matches prior documentation).
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQL18MemberTranslator.cs` -- **new file**; read in full. `PostgreSQL18MemberTranslator : PostgreSQL13MemberTranslator`, overrides `TranslateNewGuid7Method` to emit `uuidv7()`. New Tier-2 file.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQL19MemberTranslator.cs` -- **new file**; read in full. `PostgreSQL19MemberTranslator : PostgreSQL18MemberTranslator`, overrides `CreateWindowFunctionsMemberTranslator` to enable `IsLeadLagNullTreatmentSupported`/`IsValueNullTreatmentSupported` for window `IGNORE`/`RESPECT NULLS`. New Tier-2 file.
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLFactory.cs` -- read in full; version-string switch gained `"18" => v18` and `"19" => v19` arms.
- `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLHints.cs` -- read in full; the `SKIP LOCKED` suppression check's `ConfigurationList.Contains(...)` OR-chain gained a `ProviderName.PostgreSQL19` arm.

Cross-checked but not in `changedFiles` (read to verify the `CreateSqlBuilder` correction above): `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSql15Builder.cs` -- unchanged; still a sibling of `PostgreSQL13SqlBuilder` (both extend `PostgreSQLSqlBuilder` directly), still not selected by `CreateSqlBuilder` for any version.

Tier count change: Tier 2 grew from 16/16 to 19/19 (three new files: `PostgreSQL13SqlBuilder.cs`, `Translation/PostgreSQL18MemberTranslator.cs`, `Translation/PostgreSQL19MemberTranslator.cs`). Tier 1 unchanged at 11/11 (no new files match the `kb-areas.md` Tier-1 anchor list).

### Delta reads (previous run -- delta)

Changed files verified against current SHA b3340aa9ded15ffc626983fd202e6399daa081ca:

- Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLOptions.cs -- read in full; PostgreSQLOptions sealed record confirmed: three parameters (BulkCopyType, NormalizeTimestampData, IdentifierQuoteMode), copy constructor, CreateID includes both non-bulk fields. No structural change vs. existing INDEX.md documentation.
- Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs -- read in full; constructor flags, NormalizeTimeStamp, SetParameter, SetParameterType, GetNativeType (lines 434--588) all confirmed. float(N) copy-paste bug at lines 547--549 still present (both precision branches return real). CreateMemberTranslator at lines 91--98 confirmed. No structural change vs. existing INDEX.md documentation.
- Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs -- read in full; DateFunctionsTranslator, MathMemberTranslator, StringMemberTranslator, GuidMemberTranslator, PostgreSQLAggregateFunctionsMemberTranslator, SqlTypesTranslation inner classes all confirmed. TranslateStringJoin withoutSeparator logic at lines 375--447 confirmed. No structural change vs. existing INDEX.md documentation.
### Delta reads (this run)

Changed files verified against current SHA `2e67bafc9bfc8ae8ba573b93bde8671d9920c95d` (PR #5504, PR #5515):

- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlBuilder.cs` -- read in full; PR #5504 added `ConcatStyle => ConcatBuildStyle.Pipes` (line 41), delegating string concatenation to base builder via `SqlConcatExpression` path.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlExpressionConvertVisitor.cs` -- read in full; PR #5504 removed the `+` on strings -> `||` case from `ConvertSqlBinaryExpression`; added `ConcatRequiresExplicitStringCast = false`.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs` -- read in full; PR #5504 corrected `TranslateStringJoin` `withoutSeparator` path: `c.HasSequenceIndex(0)` + empty-string separator to `STRING_AGG` (prior wiring placed sequence values in the separator slot); PR #5515 `TrimStart`/`TrimEnd` inherited from `StringMemberTranslatorBase` without PostgreSQL-specific override.

### Delta reads (previous run -- PR #5467, PR #5517)

- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/Translation/PostgreSQLMemberTranslator.cs` -- PR #5467 now/utcnow rework + PR #5517 Date_Trunc truncation fix confirmed.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs` -- PR #5467 DateTimeOffset reader field change confirmed.
- `Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLMappingSchema.cs` -- PR #5467 TIMESTAMPTZ_FORMAT + BuildDateTimeOffset addition confirmed.

</details>
