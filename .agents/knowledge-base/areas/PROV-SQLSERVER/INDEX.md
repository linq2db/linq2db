---
area: PROV-SQLSERVER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 11/11
coverage_tier_2: 47/47
---

# PROV-SQLSERVER

SQL Server provider for linq2db. Covers two ADO.NET client packages (`System.Data.SqlClient` and `Microsoft.Data.SqlClient`), nine dialect versions (2005--2025), provider detection, bulk-copy via native `SqlBulkCopy`, schema discovery, hints, FTS extensions, and SQL Server 2025 types (`JSON`, `VECTOR`).

## Subsystems

### Public entry points

`SqlServerTools` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerTools.cs:14`) is the sole public static entry point. Key members:

- `GetDataProvider(SqlServerVersion, SqlServerProvider, ...)` -- delegates to `SqlServerProviderDetector.GetDataProvider`.
- `CreateDataConnection(...)` -- convenience overloads returning `DataConnection`.
- `QuoteIdentifier(string)` / `QuoteIdentifier(StringBuilder, string)` -- bracket-escaping (`[name]`, with `]]` for embedded `]`).
- `ResolveSqlTypes(string|Assembly)` -- registers spatial types from `Microsoft.SqlServer.Types` at run-time; forwards to `SqlServerProviderDetector.ResolveSqlTypes`, which calls `SqlServerTypes.Configure` on all live provider instances.
- `AutoDetectProvider` -- delegates to `ProviderDetector.AutoDetectProvider`.

`SqlServerFactory` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerFactory.cs`) is the config-driven factory: reads `assemblyName` and `version` named values and calls `SqlServerTools.GetDataProvider`.

### Provider and version enums

`SqlServerProvider` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerProvider.cs`) -- three values: `AutoDetect`, `SystemDataSqlClient`, `MicrosoftDataSqlClient`.

`SqlServerVersion` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerVersion.cs`) -- `AutoDetect` + nine versioned values: `v2005`, `v2008`, `v2012`, `v2014`, `v2016`, `v2017`, `v2019`, `v2022`, `v2025`.

### Provider options

`SqlServerOptions` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs:19`) -- sealed record extending `DataProviderOptions<SqlServerOptions>`:
- `BulkCopyType` -- default `ProviderSpecific`.
- `GenerateScopeIdentity` -- when `true` (default), identity retrieval uses `SCOPE_IDENTITY()`; when `false` or when the identity field is a GUID, falls back to an `OUTPUT [INSERTED].<field> INTO @varTable` pattern (`SqlServerSqlBuilder.cs:43--116`).
### Provider detector

`SqlServerProviderDetector` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:17`) extends [`ProviderDetectorBase<SqlServerProvider,SqlServerVersion>`](../INTERNAL-API/INDEX.md). Behaviour:

- Holds 18 `Lazy<IDataProvider>` singletons -- one per (version x client-package) combination (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:22--40`).
- `DetectProvider(ConnectionOptions)` matches provider-name strings containing `SqlServer` or `.SqlClient`, then reads version from the config string if present, else calls `DetectServerVersion`.
- `DetectServerVersion(DbConnection, DbTransaction?)` -- executes `SELECT compatibility_level FROM sys.databases WHERE name = db_name()` and maps levels: >=170->v2025, >=160->v2022, >=150->v2019, >=140->v2017, >=130->v2016, >=120->v2014, >=110->v2012, >=100->v2008, >=90->v2005. When `compatibility_level` falls below 90 (matches none of the above), a secondary fallback uses the server major version number: 9->v2005, 10->v2008, 11->v2012, 12->v2014, 13->v2016, 14->v2017, 15->v2019, 16->v2022, _->v2025 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:151--193`).
- Default version when detection fails: `v2012`.
- `DetectProvider(options, provider)` -- prefers `Microsoft.Data.SqlClient` if `Microsoft.Data.SqlClient.dll` is found next to the assembly (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:200--218`).

### Provider adapter

`SqlServerProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:32`) -- singleton per client package (double-checked lock). Implements [`IDynamicProviderAdapter`](../INTERNAL-API/INDEX.md). Key responsibilities:

- Loads the SqlClient assembly at run-time (`Assembly.Load` on non-Framework; `typeof(SqlConnection).Assembly` on Framework).
- Builds `TypeMapper`-backed wrappers for `SqlBulkCopy`, `SqlBulkCopyColumnMapping`, `SqlConnectionStringBuilder`, `SqlException` (for error-number extraction used by `SqlServerTransientExceptionDetector`).
- Exposes `SqlDbType` setter/getter for `SqlParameter`, `UdtTypeName`, `TypeName`.
- For `MicrosoftDataSqlClient` only: loads `SqlJson` (`DataType.Json`), `SqlVector<float>` (`DataType.Vector32`), and `SqlVector<Half>` (`DataType.Vector16`, net8+ only) from the `Microsoft.Data.SqlTypes` namespace; builds `MappingSchema`-registered value-to-SQL converters and round-trip converters for each type (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:328--563`).
- `VectorDbType` hard-codes `(SqlDbType)36` (no named constant in the public API yet).
- `JsonDbType` hard-codes `(SqlDbType)35`.
- New in this delta: `SqlServer2005BulkCopyUnsupported` property (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:154--156`) -- returns `true` when provider is `MicrosoftDataSqlClient` and the loaded assembly major version is >= 7 (i.e. Microsoft.Data.SqlClient 7.0+, which dropped BulkCopy support for SQL Server 2005 targets). Callers use this to fall back to `MultipleRowsCopy` for v2005 targets.
### Core data provider

`SqlServerDataProvider` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:52`) -- `abstract`, extends [`DynamicDataProviderBase<SqlServerProviderAdapter>`](../INTERNAL-API/INDEX.md). 18 concrete `sealed` subclasses are in the same file, one per (version x client-package). Constructor responsibilities:

- Sets `SqlProviderFlags`: `IsApplyJoinSupported`, `IsCommonTableExpressionsSupported`, `OutputDeleteUseSpecialTable`, `OutputInsertUseSpecialTable`, `OutputUpdateUseSpecialTables`, `OutputMergeUseSpecialTables`, `TakeHintsSupported = Percent|WithTies`, `IsDistinctFromSupported = Version >= v2022`, `SupportsBooleanType = false`, `IsUpdateTakeSupported = true`, `IsRowNumberWithoutOrderBySupported = false`.
- Registers `char`/`nchar` trimming; `SqlChars`, `SqlBinary`, `SqlBoolean`, etc. via `SetProviderField` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:100--114`).
- When `SqlJsonType != null` (MicrosoftDataSqlClient + new enough version), registers JSON reader (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:116--127`).
- When `SqlVectorType != null`, registers `SqlVector<float>` and `float[]` readers. New in this delta: reader expressions are registered for both `FieldType = byte[]` and `FieldType = SqlVectorType` to handle the SqlClient 6.x-vs-7.0.1+ field-type change -- SqlClient 6.x reports vector columns as `FieldType=byte[]`; 7.0.1+ reports `SqlVector<T>` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:139--143`).
- Selects `_sqlOptimizer` via switch on `(version, provider)` -- v2025+MicrosoftDataSqlClient gets `SqlServer2025SqlOptimizer`; v2025+SystemDataSqlClient gets `SqlServerSystem2025SqlOptimizer`; all others get the version-named optimizer (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:84--96`).
- `CreateMemberTranslator()` dispatches by `Version`: >=v2022->SqlServer2022MemberTranslator, >=v2017->SqlServer2017MemberTranslator, >=v2016->SqlServer2016MemberTranslator, >=v2012->SqlServer2012MemberTranslator, >=v2008->SqlServer2008MemberTranslator, v2005->SqlServer2005MemberTranslator, else->SqlServerMemberTranslator (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:259--270`).
- `CreateSqlBuilder()` switches on `Version` to instantiate the per-version builder (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:273--288`).
- `SetParameter` handles `DateTimeOffset`->`DateTime` for SmallDateTime/DateTime, `DateTimeOffset` precision for DateTime2/DateTimeOffset/Date, `DateOnly`->`DateTime` (`SUPPORTS_DATEONLY`), decimal precision clamping, `DataType.Structured` for TVP, `DataType.Vector32`/`Vector16` conversion, JSON/`SqlJson` conversion for v2025+ (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:319--580`).
- UDT support: `AddUdtType(Type, string, ...)` registers name<->type mappings into `_udtTypeNames`/`_udtTypes`; `GetUdtTypeByName` resolves by name.
- BulkCopy: lazily instantiates `SqlServerBulkCopy` and delegates; reads `SqlServerOptions.Default.BulkCopyType` when options say `Default` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:688--732`).

`MappingSchemaInstance` inner class selects one of 18 `SqlServerMappingSchema` subclass instances based on (version, provider) (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:212--241`).

### Mapping schema

`SqlServerMappingSchema` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs`) -- `sealed LockedMappingSchema`. Defines literal-generation for all SQL Server date/time types: `TIME(p)`, `DATE`, `SMALLDATETIME`, `DATETIME`, `DATETIME2(p)`, `DATETIMEOFFSET(p)`. Uses `TIMEFROMPARTS`, `DATEFROMPARTS`, `DATETIMEFROMPARTS`, `DATETIME2FROMPARTS`, `DATETIMEOFFSETFROMPARTS` factory functions. Provides `ConvertDateTimeOffsetToString` and `ConvertTimeSpanToString` used by `SqlServerDataProvider.SetParameter`. 18 nested mapping-schema subclasses (one per version x provider combination) extend it, layering version-specific rules (e.g., `SqlServer2005MappingSchema` maps `DATE` -> `DateTime`; `SqlServer2025MappingSchema` adds VECTOR literal builders).

New in this delta (`SqlServer2025MappingSchema`): adds `ConvertStringToSql2025` -- a variant of the base string-literal emitter that uses `||` (ANSI concat) instead of `+` as the fragment joiner, matching the SQL Server 2025 `||` operator (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs:317--325`). The 2025 schema's `SetValueToSqlConverter(typeof(string), ...)` calls this variant, so string literals emitted by the 2025 mapping schema join continuation fragments with `||`. Also adds `float[]` and `Half[]` (net8+) scalar type registrations with `BuildVectorLiteral` / `BuildHalfVectorLiteral` helpers that emit `CAST('[f1, f2, ...]' AS VECTOR(n, float32|float16))` syntax (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs:769--865`).
### SQL builder hierarchy

`SqlServerSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19`) -- `public abstract`, extends [`BasicSqlBuilder<SqlServerOptions>`](../SQL-PROVIDER/INDEX.md). Key SQL-Server-specific overrides:

- `FirstFormat` -- emits `TOP ({0})` when no `SKIP` is present.
- `BuildInsertQuery`/`BuildOutputSubclause`/`BuildGetIdentity` -- implements identity retrieval via `@varOutput TABLE` + `OUTPUT [INSERTED].<field>` or `SCOPE_IDENTITY()`.
- `BuildDeleteClause` -- `DELETE <alias>` pattern (not `DELETE FROM <table>`).
- `BuildObjectName` -- handles four-part name (`server.database.schema.table`), temp-table `#`/`##` prefix, `tempdb.` injection.
- `Convert` -- brackets identifiers with `[...]`, strips `@` from sproc-param names, maps `$action` for MERGE.
- `BuildDropTableStatement` -- generates `IF OBJECT_ID(...) IS NOT NULL DROP TABLE ...` for <=2014; 2016+ uses `DROP TABLE IF EXISTS`.
- `BuildCreateTablePrimaryKey` -- emits `PRIMARY KEY CLUSTERED`.
- `BuildDataTypeFromDataType` -- maps `Guid`->`UniqueIdentifier`, `Variant`->`Sql_Variant`, `NVarChar`/`VarChar`/`VarBinary` with `(MAX)` fallback, `VECTOR(n, float32|float16)` for 2025+ vector types.
- `BuildTableExtensions` / `BuildTableNameExtensions` / `BuildJoinType` / `BuildQueryExtensions` -- routes hint extensions into `WITH (...)`, join hint syntax, and `OPTION (...)` clauses.
- `IsSqlValuesTableValueTypeRequired` -- forces explicit type for `uint`, `long`, `float`, `double`, `decimal`, `null` in row-0.

### Per-version builder matrix

| Version | Builder class | Key diff vs prior |
|---|---|---|
| v2005 | `SqlServer2005SqlBuilder` | `IsValuesSyntaxSupported = false`; JSON->`NVARCHAR(MAX)`; date types -> `DateTime` |
| v2008 | `SqlServer2008SqlBuilder` | `INSERT OR UPDATE` uses MERGE (`BuildInsertOrUpdateQueryAsMerge`); JSON->`NVARCHAR(MAX)` |
| v2012 | `SqlServer2012SqlBuilder` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY`; `IIF()` for `SqlConditionExpression`; MERGE `BY SOURCE`; full merge support with `HasIdentityInsert`; JSON->`NVARCHAR(MAX)` |
| v2014 | `SqlServer2014SqlBuilder` | No diff (inherits v2012) |
| v2016 | `SqlServer2016SqlBuilder` | `DROP TABLE IF EXISTS` via `BuildDropTableStatementIfExists`; `ConvertDateTimeAsLiteral` flag for `FOR SYSTEM_TIME` parameter workaround |
| v2017 | `SqlServer2017SqlBuilder` | No diff (inherits v2016) |
| v2019 | `SqlServer2019SqlBuilder` | No diff (inherits v2017) |
| v2022 | `SqlServer2022SqlBuilder` | Native `IS [NOT] DISTINCT FROM` predicate |
| v2025 | `SqlServer2025SqlBuilder` | JSON->`JSON` (native type); `ConcatStyle = ConcatBuildStyle.Pipes` (emits `||` for string concat instead of `+`); inherits v2022 |

`SqlServer2025SqlBuilder` override detail: `ConcatStyle` returns `ConcatBuildStyle.Pipes` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlBuilder.cs:29`), enabling the ANSI `||` concatenation operator introduced in SQL Server 2025 (strict null propagation, auto-coerce). `BuildDataTypeFromDataType` maps `DataType.Json` to the literal `JSON` keyword for v2025 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlBuilder.cs:33--39`).

Partial class files `SqlServer2008SqlBuilder.Merge.cs` and `SqlServer2012SqlBuilder.Merge.cs` provide MERGE builder overrides (`BuildMergeInto`, `BuildMergeOperationDeleteBySource`, `BuildMergeOperationUpdateBySource`, `BuildMergeTerminator`) for their respective versions.
### Per-version optimizer matrix

| Version | Optimizer class | Pagination strategy |
|---|---|---|
| v2005 | `SqlServer2005SqlOptimizer` | `ROW_NUMBER`; separates DISTINCT from pagination; wraps UPDATE/DELETE `TOP` |
| v2008 | `SqlServer2008SqlOptimizer` | Same as v2005 (no OFFSET/FETCH) |
| v2012 | `SqlServer2012SqlOptimizer` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY`; `AddOrderByForSkip` injects `ORDER BY (1)` when missing |
| v2014 | `SqlServer2014SqlOptimizer` | Delegates to v2012 (bug: constructor passes `SqlServerVersion.v2016` -- see Known issues) |
| v2016 | `SqlServer2016SqlOptimizer` | Delegates to v2012 |
| v2017 | `SqlServer2017SqlOptimizer` | Delegates to v2012 |
| v2019 | `SqlServer2019SqlOptimizer` | Delegates to v2012 |
| v2022 | `SqlServer2022SqlOptimizer` | Delegates to v2019 |
| v2025 (Microsoft) | `SqlServer2025SqlOptimizer` | Same as v2022; `CreateConvertVisitor` returns `SqlServer2025SqlExpressionConvertVisitor` |
| v2025 (System) | `SqlServerSystem2025SqlOptimizer` | Same as v2025 Microsoft + `Finalize` marks `float[]`/`Half[]` params as `IsQueryParameter = false` (inline vector literals, no parameterization) |

`SqlServer2025SqlOptimizer` is a new dedicated class (added in this delta) extending `SqlServer2022SqlOptimizer`. Its sole addition is `CreateConvertVisitor` returning `SqlServer2025SqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlOptimizer.cs:16--19`). Previously v2025+Microsoft shared `SqlServer2022SqlOptimizer` directly.

`SqlServerSystem2025SqlOptimizer` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSystem2025SqlOptimizer.cs`) now extends `SqlServer2025SqlOptimizer` (was `SqlServer2022SqlOptimizer`). The `SetQueryParameter` visitor marks any `SqlParameter` of type `float[]` or `Half[]` (net8+) as `IsQueryParameter = false`, forcing inline vector literal emission rather than ADO.NET parameterization -- required because `System.Data.SqlClient` has no native vector parameter support (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSystem2025SqlOptimizer.cs:11--31`).

All optimizer versions route through `SqlServerSqlOptimizer.CorrectSqlServerUpdate`, which handles SQL Server's `UPDATE <alias> SET ... FROM ...` form and the `UPDATE TOP n` single-table case (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs:63--163`).

### Expression convert visitors

`SqlServerSqlExpressionConvertVisitor` (base, all versions) -- extends [`SqlExpressionConvertVisitor`](../SQL-PROVIDER/INDEX.md):
- `ConvertConcat(SqlConcatExpression)` -- new override (PR #5504): before delegating to the base `SqlConcatExpression` lowering, casts any `NText`/`Text` operands to `NVarChar`/`VarChar` respectively, because SQL Server's `+` and `||` operators reject LOB text types. Uses `PseudoFunctions.MakeCast` for the per-operand cast (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlExpressionConvertVisitor.cs:21--53`).
- `ConvertSearchStringPredicate` -- wraps case-sensitive LIKE with a `CONVERT(VARBINARY, ...)` equality check for `StartsWith`/`EndsWith`/`Contains`.
- `ConvertSqlBinaryExpression` -- converts float modulo `%` via `CONVERT(INT, ...)`.
- `ConvertConversion` -- default `DECIMAL` precision 38,17; calls `FloorBeforeConvert`.
- `WrapColumnExpression` -- adds mandatory `CAST` for `uint`/`long`/`ulong`/`float`/`double`/`decimal` values and inline parameters.
- `ConvertSqlFunction(LENGTH)` -- `LEN(value + '.') - 1` pattern.

`SqlServer2025SqlExpressionConvertVisitor` -- new class (added in this delta). Extends `SqlServer2012SqlExpressionConvertVisitor`. Overrides `ConcatRequiresExplicitStringCast` to return `false` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlExpressionConvertVisitor.cs:15`): SQL Server 2025's `||` operator auto-coerces non-string operands, so explicit `CAST` on concat arguments is not needed.

Chain: v2005 removes `TIME` cast (unsupported); v2008 re-enables it; v2012 adds `TRY_CONVERT` pseudo-function -> native `TRY_CONVERT(type, expr)`; v2025 skips explicit-string-cast on concat. v2022 optimizer sets `SupportsDistinctAsExistsIntersect = (_sqlServerVersion < v2022)`.
### Member translator hierarchy

The translator chain follows the version-order inheritance: `SqlServerMemberTranslator` (base) <- `SqlServer2005MemberTranslator` <- `SqlServer2008MemberTranslator` <- `SqlServer2012MemberTranslator` <- `SqlServer2016MemberTranslator` <- `SqlServer2017MemberTranslator` <- `SqlServer2022MemberTranslator`.

`SqlServerMemberTranslator` (base) -- extends [`ProviderMemberTranslatorDefault`](../INTERNAL-API/INDEX.md):
- `SqlServerDateFunctionsTranslator` -- maps `DatePart` enum to T-SQL date-part strings; translates `DatePart`, `DateAdd`, `DateDiff`, `MakeDateTime` (string-concat cast fallback), truncate-to-date.
- `TranslateNow` returns `null` (cannot translate -- server doesn't know client's local timezone).
- `TranslateUtcNow` emits `GETUTCDATE()` returning `DateTime`.
- `TranslateDateTimeOffsetTruncationToDate` delegates to `TranslateDateTimeTruncationToDate`.
- `SqlServerMathMemberTranslator` -- standard math; rounds with explicit `0` precision when none supplied.
- `SqlServerStringMemberTranslator` -- `REPLICATE`-based `LPad`; pre-2017 `string.Join` emulation via `SUBSTRING` of concat (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs:237--260`). `TrimStart`/`TrimEnd` with `trimChars != null` returns `null` (not supported pre-2022) (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs:202--215`).
- `TranslateNewGuidMethod` emits `NewID()`.

New in this delta -- `SqlServerStringMemberTranslator.TranslateStringJoin` `withoutSeparator` path (PR #5504): when `withoutSeparator=true`, calls `ConfigureConcat` (wraps by coalesce); when `withoutSeparator=false`, calls `ConfigureConcatWsEmulation` with a `SUBSTRING`-based extractor that strips the leading separator. Previously the `withoutSeparator` distinction was absent (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs:237--260`).

| Translator | Extends | Key additions vs parent |
|---|---|---|
| `SqlServer2005MemberTranslator` | `SqlServerMemberTranslator` | `DATE` mapped to `DATETIME` in sql-types; truncate-to-date uses `DATEADD(dd, DATEDIFF(dd,0,x), 0)` (no native DATE cast) |
| `SqlServer2008MemberTranslator` | `SqlServer2005MemberTranslator` | `DATE` mapped to `DataType.Date`; truncate-to-date uses `CAST(x AS DATE)` with `preserveDbType=true` (PR #5517); `TranslateUtcNow` emits `SYSUTCDATETIME()`; `TranslateZonedUtcNow` emits `CAST(SYSUTCDATETIME() AS datetimeoffset)` (no AT TIME ZONE) |
| `SqlServer2012MemberTranslator` | `SqlServer2008MemberTranslator` | `TranslateMakeDateTime` uses `DATETIME2FROMPARTS`/`DATETIMEFROMPARTS` |
| `SqlServer2016MemberTranslator` | `SqlServer2012MemberTranslator` | `TranslateZonedUtcNow` emits `SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'` (first version supporting AT TIME ZONE) |
| `SqlServer2017MemberTranslator` | `SqlServer2016MemberTranslator` | `STRING_AGG` for aggregate string join; `REPLICATE + value` for `PadLeft`; `withoutSeparator` path uses `HasSequenceIndex(0)` to omit the separator argument from the `STRING_AGG` call (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs:49--51`) |
| `SqlServer2022MemberTranslator` | `SqlServer2017MemberTranslator` | `GREATEST`/`LEAST` for `Math.Max`/`Math.Min`; `LTRIM(value, trimChars)` / `RTRIM(value, trimChars)` for `TrimStart`/`TrimEnd` with explicit chars (PR #5515) |

New in this delta -- `SqlServer2017MemberTranslator.TranslateStringJoin` `withoutSeparator` correction (PR #5504): when `withoutSeparator=true`, the builder now calls `c.HasSequenceIndex(0)` (value only, no separator argument) rather than `c.HasSequenceIndex(1).TranslateArguments(0)`. The `STRING_AGG` call is built with `factory.Value(valueType, string.Empty)` as the separator placeholder (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs:48--55`).

New in this delta -- `SqlServer2022MemberTranslator` adds `SqlServer2022StringMemberTranslator` (PR #5515): overrides `TranslateTrimStart` and `TranslateTrimEnd`. When `trimChars == null`, falls back to the base (LTRIM/RTRIM without arguments). When `trimChars` is provided, emits `LTRIM(value, trimChars)` / `RTRIM(value, trimChars)` using the SQL Server 2022 two-argument forms (`Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2022MemberTranslator.cs:44--65`). Pre-2022 (base `SqlServerStringMemberTranslator`) returns `null` for any `trimChars != null` call.

The `preserveDbType=true` flag on the 2008 `DATE` cast (PR #5517) prevents the SQL generator from re-applying the column's original `DbType` over the `CAST(x AS DATE)` result -- without it, `DateTime.Date` on a `DateTime2` column would emit `CAST(x AS DATE)` but then have the `DATE` type overridden back to `DateTime2` by the column's metadata.
### Bulk copy

`SqlServerBulkCopy` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs:16`) extends [`BasicBulkCopy`](../INTERNAL-API/INDEX.md):
- `MaxParameters = 2099`, `MaxSqlLength = 327670`.
- `RequiresMultipleRowsFallback` -- new computed property (this delta): returns `true` when provider is v2005 and `Adapter.SqlServer2005BulkCopyUnsupported` is true (i.e. Microsoft.Data.SqlClient 7.0+ which dropped 2005 BulkCopy support). All three `ProviderSpecificCopy*` overloads check this first and fall back to `MultipleRowsCopy` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs:37--39`).
- `ProviderSpecificCopy*` -- tries to unwrap to a native `SqlConnection`/`SqlTransaction`; if successful, uses `SqlBulkCopy` (wrapper) via `ProviderSpecificCopyInternal(Async)`. Falls back to `MultipleRowsCopy`.
- `MultipleRowsCopy` -- v2005 uses `MultipleRowsCopy2`; all others use `MultipleRowsCopy1`. Wraps bulk with `SET IDENTITY_INSERT <table> ON/OFF` when `KeepIdentity = true`.
- `CreateRowsHelper` -- for `SystemDataSqlClient`, sets `ConvertToParameter` to skip parameterization for `float[]` and (net8+) `Half[]` columns; vector types must be sent inline, not as ADO.NET parameters (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs:386--398`). The `_convertToParameter` predicate additionally gates on `options.BulkCopyOptions.UseParameters` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs:386--391`).
- `ProviderSpecificCopyInternal` maps `BulkCopyOptions` flags to `SqlBulkCopyOptions` enum.

### Schema provider

`SqlServerSchemaProvider` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs`) extends [`SchemaProviderBase`](../INTERNAL-API/INDEX.md):
- `InitProvider` detects Azure (via `@@version` containing `Azure`) and reads `CompatibilityLevel`.
- `GetTables` uses two different SQL branches: Azure (no `sys.extended_properties`) vs on-prem (with description from `INFORMATION_SCHEMA.TABLES` + `sys.extended_properties`). Filters temporal history tables when `CompatibilityLevel >= 130` and `IgnoreSystemHistoryTables`.
- Additional overrides for columns, foreign keys, procedures, etc. (body not fully read; pattern follows `SchemaProviderBase` contract).

### Hints

`SqlServerHints` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerHints.cs`) -- `public static partial class` with three nested classes: `Table`, `Join`, `Query`. Constants are string literals matching T-SQL hint syntax. `Table.SpatialWindowMaxCells(int)` generates a `SPATIAL_WINDOW_MAX_CELLS=n` fragment via `[Sql.Expression]`.

`SqlServerHints.generated.cs` -- T4-generated extension methods wiring hint constants into the `WithTableHint`/`WithJoinHint`/`WithQueryHint` pipeline on `ISqlServerSpecificTable<T>` and `ISqlServerSpecificQueryable<T>` (`WITH (NOLOCK)` -> `.With(SqlServerHints.Table.NoLock)`, `OPTION (RECOMPILE)` -> `.Option(SqlServerHints.Query.Recompile)`, etc.).

`SqlServerSqlBuilder.BuildTableExtensions` emits table hints as `WITH (hint1, hint2)` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:489--491`). `BuildQueryExtensions` emits query-level hints as `OPTION (hint)` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:539--542`). Join hints are injected directly into `JOIN` keyword -- e.g., `INNER LOOP JOIN` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:521--534`).

### Extensions surface

`SqlServerExtensions` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerExtensions.cs`) -- FTS extensions: `FreeTextTable<TTable,TKey>` and `ContainsTable<TTable,TKey>` overloads map to `FREETEXTTABLE(...)` / `CONTAINSTABLE(...)` via `[ExpressionMethod]` + `FromSql` raw queries.

`SqlServerSpecificExtensions` -- `AsSqlServer<T>(ITable<T>)` and `AsSqlServer<T>(IQueryable<T>)` return `ISqlServerSpecificTable<T>` / `ISqlServerSpecificQueryable<T>` for hint-scoped extension chaining.

### SQL Server 2025 additions

`SqlFn` (`Source/LinqToDB/DataProvider/SqlServer/SqlFn.cs:19`) -- `[PublicAPI]` static class of server-side-only T-SQL function mappings via `[Sql.Expression(ProviderName.SqlServer, ...)]`. Covers configuration functions (`@@DBTS`, `@@LANGID`, etc.), cursor functions, date/time, JSON functions, string functions, and more. This is an older pattern (pre-member-translator); new SQL-2025-era functions should use the member translator instead. The class is large but functionally pure -- each member is a `ServerSideOnly` expression.

`SqlType` (`Source/LinqToDB/DataProvider/SqlServer/SqlType.cs:9`) -- `abstract` base; `SqlType<T>` generic subclass. Static factory properties/methods expose all T-SQL type literals as `[Sql.Expression]`-annotated `SqlType<T>` instances for use in `Cast<T>` expressions. Covers all type families including `VECTOR(n)`, `JSON`, `HIERARCHYID`.

### Spatial type support

`SqlServerTypes` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs`) -- lazily loads `Microsoft.SqlServer.Types` assembly, registers `SqlHierarchyId`, `SqlGeography`, `SqlGeometry` into the mapping schema. `Configure(SqlServerDataProvider)` calls `AddUdtType` on each found type.

`SystemDataSqlServerAttributeReader` (`Source/LinqToDB/DataProvider/SqlServer/SystemDataSqlServerAttributeReader.cs:26`) -- implements `IMetadataReader`. Reads `[SqlMethod]` and `[SqlUserDefinedType]` attributes from spatial types (three static instances: `SystemDataSqlClientProvider`, `MicrosoftDataSqlClientProvider`, `MicrosoftSqlServerServerProvider` for the three assembly sources). Converts them to `Sql.ExpressionAttribute` / `DataTypeAttribute(DataType.Udt, ...)` mappings at metadata-read time.

### Transient exception / retry

`SqlServerTransientExceptionDetector` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerTransientExceptionDetector.cs:17`) -- static, registration-based. `RegisterExceptionType` is called by `SqlServerProviderAdapter.CreateAdapter` to bind `SqlException`->error-number extractor. `ShouldRetryOn` matches ~22 known transient SQL error codes. `IsHandled` returns the error numbers for additional classification.

`SqlServerRetryPolicy` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerRetryPolicy.cs:13`) -- extends `RetryPolicyBase` (exponential backoff). `ShouldRetryOn` calls `SqlServerTransientExceptionDetector.ShouldRetryOn`; memory-optimized errors (41301--41839) use a shorter delay (`GetNextDelay` returns `TotalSeconds` not `TotalMilliseconds`).
## Key types

| Type | File | Role |
|---|---|---|
| `SqlServerDataProvider` | `Internal/DataProvider/SqlServer/SqlServerDataProvider.cs` | Abstract base; 18 concrete subclasses per version x provider |
| `SqlServerSqlBuilder` | `Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs` | Abstract SQL builder base; handles OUTPUT, IDENTITY, temp tables, hints |
| `SqlServer2025SqlBuilder` | `Internal/DataProvider/SqlServer/SqlServer2025SqlBuilder.cs` | v2025 builder; `ConcatBuildStyle.Pipes` (`||`), JSON->native type |
| `SqlServerSqlOptimizer` | `Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs` | Optimizer base; `CorrectSqlServerUpdate` handles UPDATE alias pattern |
| `SqlServer2025SqlOptimizer` | `Internal/DataProvider/SqlServer/SqlServer2025SqlOptimizer.cs` | v2025 optimizer; wires `SqlServer2025SqlExpressionConvertVisitor` |
| `SqlServer2025SqlExpressionConvertVisitor` | `Internal/DataProvider/SqlServer/SqlServer2025SqlExpressionConvertVisitor.cs` | Skips explicit-string-cast on concat (2025 `||` auto-coerces) |
| `SqlServerSystem2025SqlOptimizer` | `Internal/DataProvider/SqlServer/SqlServerSystem2025SqlOptimizer.cs` | System.Data.SqlClient v2025; inlines `float[]`/`Half[]` as literals |
| `SqlServerProviderAdapter` | `Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs` | Dynamic ADO.NET bridge; loads SqlClient, BulkCopy, JSON/Vector types; `SqlServer2005BulkCopyUnsupported` for MC 7.0+ |
| `SqlServerProviderDetector` | `Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs` | Provider auto-detection; version detection via `compatibility_level`; major-version fallback |
| `SqlServerMappingSchema` | `Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs` | Literal generation for all SQL Server date/time + v2025 vector/string types |
| `SqlServerBulkCopy` | `Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs` | BulkCopy via `SqlBulkCopy`; MC7.0+ v2005 fallback; `float[]`/`Half[]` inline |
| `SqlServerSchemaProvider` | `Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs` | Schema discovery; Azure vs on-prem branches |
| `SqlServerTools` | `DataProvider/SqlServer/SqlServerTools.cs` | Public static entry point |
| `SqlServerOptions` | `DataProvider/SqlServer/SqlServerOptions.cs` | Options record; `BulkCopyType`, `GenerateScopeIdentity` |
| `SqlServerHints` | `DataProvider/SqlServer/SqlServerHints.cs` | Hint constant catalog |
| `SqlFn` | `DataProvider/SqlServer/SqlFn.cs` | Server-side T-SQL function library |
| `SqlType` | `DataProvider/SqlServer/SqlType.cs` | Type-safe T-SQL type constructors for CAST |
| `SystemDataSqlServerAttributeReader` | `DataProvider/SqlServer/SystemDataSqlServerAttributeReader.cs` | Spatial-type metadata reader |
| `SqlServerTransientExceptionDetector` | `DataProvider/SqlServer/SqlServerTransientExceptionDetector.cs` | Transient SQL error classification |
| `SqlServerRetryPolicy` | `DataProvider/SqlServer/SqlServerRetryPolicy.cs` | Exponential-backoff retry policy |
| `SqlServerTypes` | `Internal/DataProvider/SqlServer/SqlServerTypes.cs` | Spatial type lazy-loader |
| `SqlServerSqlExpressionConvertVisitor` | `Internal/DataProvider/SqlServer/SqlServerSqlExpressionConvertVisitor.cs` | Expression conversion; NText/Text cast for concat; case-sensitive LIKE; float %; decimal precision |
| `SqlServer2008MemberTranslator` | `Internal/DataProvider/SqlServer/Translation/SqlServer2008MemberTranslator.cs` | DATE truncation via CAST; SYSUTCDATETIME(); ZonedUtcNow via CAST |
| `SqlServer2016MemberTranslator` | `Internal/DataProvider/SqlServer/Translation/SqlServer2016MemberTranslator.cs` | ZonedUtcNow via `SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'` |
| `SqlServer2022MemberTranslator` | `Internal/DataProvider/SqlServer/Translation/SqlServer2022MemberTranslator.cs` | GREATEST/LEAST; LTRIM/RTRIM with explicit trim chars (PR #5515) |
## Files (Tier 1 / Tier 2)

**Tier 1** (11 files, all visited):

| File | Role |
|---|---|
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs` | Core provider; 18 concrete subclasses |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs` | SQL builder base |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs` | Optimizer base |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerTools.cs` | Public entry point |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerProvider.cs` | Provider enum |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerVersion.cs` | Version enum |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs` | Options record |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs` | ADO.NET adapter |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs` | Detection logic |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs` | Mapping schema |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs` | Bulk copy |

**Tier 2** (47 files, 47 visited): per-version builders, optimizers, translators (including `SqlServer2008MemberTranslator.cs`, `SqlServer2016MemberTranslator.cs` from PR #5467; new `SqlServer2025SqlBuilder.cs`, `SqlServer2025SqlExpressionConvertVisitor.cs`, `SqlServer2025SqlOptimizer.cs` from this delta), expression-convert visitors, hints, extensions, schema provider, attribute reader, retry policy, factory, marker interfaces. Previously 2 files were deferred (T4 template + generated hints); those are Tier-3 and excluded.

## Inbound / outbound dependencies

**Inbound:**
- `DataConnection.UseSqlServer` (EFCore companion, `EFCORE` area) calls `SqlServerTools.GetDataProvider`.
- `SqlServerFactory` is registered via `DataProviderFactoryBase`; config-driven providers call it.
- `SqlServerRetryPolicy` is instantiated by user code; no internal callers.

**Outbound:**
- Extends [`BasicSqlBuilder`](../SQL-PROVIDER/INDEX.md), [`BasicSqlOptimizer`](../SQL-PROVIDER/INDEX.md) -- SQL generation and optimization pipeline.
- Extends [`DynamicDataProviderBase<SqlServerProviderAdapter>`](../INTERNAL-API/INDEX.md) -- ADO.NET lifecycle.
- Extends [`ProviderDetectorBase`](../INTERNAL-API/INDEX.md) -- provider auto-detect infrastructure.
- Extends [`BasicBulkCopy`](../INTERNAL-API/INDEX.md) -- bulk-copy base.
- Extends [`SchemaProviderBase`](../INTERNAL-API/INDEX.md) -- schema discovery base.
- Extends [`ProviderMemberTranslatorDefault`](../INTERNAL-API/INDEX.md) -- member translation.
- Uses [`TypeMapper`](../INTERNAL-API/INDEX.md) (`Internal/Expressions/Types/TypeMapper.cs`) for dynamic adapter wrappers.
- `SqlServerMappingSchema` extends [`LockedMappingSchema`](../MAPPING/INDEX.md).
- `SystemDataSqlServerAttributeReader` implements [`IMetadataReader`](../METADATA/INDEX.md).

## Known issues / debt

- `SqlServer2014SqlOptimizer` constructor erroneously passes `SqlServerVersion.v2016` instead of `v2014` to its `base` call (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2014SqlOptimizer.cs:8`). Functionally harmless (the version field is used only for visitor creation and v2014/v2016 visitors are the same), but misleading.
- `GetConnectionInfo(IsMarsEnabled)` is marked `[Obsolete]` for removal in v7 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:302`). The `_marsFlags` cache still exists.
- `SqlConnectionStringBuilder` wrapper is `[Obsolete]` for removal in v7 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:193`).
- TODO comment at `SqlServer2025SqlBuilder` VECTOR reader (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:150`): review implementation after SqlClient adds support for this type.
- TODO comment for `SqlHalfVectorType` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:188--190`): implementation may need adjustment when `SqlClient` adds official `float16` vector support.
- The inheritance chain for builders (2005->2008->2012->2014->2016->2017->2019->2022->2025) causes near-duplicate MERGE logic in both `SqlServer2008SqlBuilder.Merge.cs` and `SqlServer2012SqlBuilder.Merge.cs`. The `SqlServer2012SqlBuilder.Merge.cs` itself notes: TODO: both 2008 and 2012 builders inherit from same base class which leads to duplicate builder logic (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlBuilder.Merge.cs:8`).
- `SqlFn` covers many T-SQL functions with the older `[Sql.Expression]` pattern; new functions (STRING_AGG, GREATEST, LEAST, DATETRUNC) are now added via member translators, creating two parallel extension points.
- `SqlServer2025SqlExpressionConvertVisitor` and `SqlServer2025SqlOptimizer` were added as new files (this delta) but were previously not explicitly present -- prior to this delta, v2025+Microsoft directly used `SqlServer2022SqlOptimizer`. The new classes are minimal but provide the correct extension point for future 2025-specific expression transformations.

## See also

- [SQL-PROVIDER area](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`, `ISqlBuilder`, `ISqlOptimizer`
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) -- `DataProviderBase`, `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `MemberTranslatorBase`, `TypeMapper`, `SchemaProviderBase`
- [MAPPING area](../MAPPING/INDEX.md) -- `MappingSchema`, `LockedMappingSchema`
- [METADATA area](../METADATA/INDEX.md) -- `IMetadataReader`, `SchemaProviderBase`

## Pointers

- `SqlConcatExpression` AST node and `ConcatBuildStyle` enum live in the SQL-PROVIDER area; the v2025 `Pipes` style is the first SQL Server dialect to use non-`+` concat.
- `StringMemberTranslatorBase.ConfigureConcat` / `ConfigureConcatWsEmulation` (base-class helpers) control the `withoutSeparator` branching used in all `TranslateStringJoin` overrides.
<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11
- Tier 2 (visited / total): 47 / 47 (100%)
- Tier 3 (skipped, logged): 2 (T4 template + generated hints file -- unchanged, not re-read)

Read (this run -- new files added by PR #5467):
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2008MemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2016MemberTranslator.cs

Read (this run -- delta, prior):
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2005MemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2012MemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs (delta around CreateMemberTranslator)
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs (delta around _convertToParameter for Half[])

Read (this run -- delta):
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlBuilder.cs -- new class; ConcatBuildStyle.Pipes, JSON->JSON type, inherits v2022
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlExpressionConvertVisitor.cs -- new class; ConcatRequiresExplicitStringCast = false for 2025 || auto-coerce
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlOptimizer.cs -- new class; wires SqlServer2025SqlExpressionConvertVisitor
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs -- RequiresMultipleRowsFallback for MC 7.0+ v2005 BulkCopy drop; _convertToParameter gated on UseParameters
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs -- vector reader registered for both byte[] and SqlVectorType field types; optimizer switch updated for v2025 split
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs -- ConvertStringToSql2025 with || joiner; SqlServer2025MappingSchema registers float[]/Half[] with BuildVectorLiteral
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs -- SqlServer2005BulkCopyUnsupported property for MC 7.0+ detection
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlExpressionConvertVisitor.cs -- ConvertConcat override casting NText/Text operands before lowering
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSystem2025SqlOptimizer.cs -- now extends SqlServer2025SqlOptimizer (was SqlServer2022SqlOptimizer)
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs -- withoutSeparator path uses HasSequenceIndex(0), empty-string separator in STRING_AGG
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2022MemberTranslator.cs -- SqlServer2022StringMemberTranslator adds LTRIM/RTRIM with trim-chars (PR #5515)
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs -- SqlServerStringMemberTranslator: TrimStart/TrimEnd returns null for trimChars!=null; TranslateStringJoin withoutSeparator branching corrected

Read (this run -- delta):
- Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs -- unchanged; BulkCopyType default ProviderSpecific, GenerateScopeIdentity default true, no new members
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs -- confirmed: CreateMemberTranslator switch (>=v2022 through v2005); optimizer switch includes v2025+MDC->SqlServer2025SqlOptimizer, v2025+SDC->SqlServerSystem2025SqlOptimizer; MappingSchemaInstance.Get has 18 switch arms; SetParameter handles DataType.Json, Vector32, Vector16
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs -- confirmed: ConvertStringToSql2025 at line 317 uses || joiner; ConvertStringToSql at line 306 uses + joiner
- Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs -- confirmed: 18 lazy singletons (9 SDC + 9 MDC, including v2025); DetectServerVersion secondary fallback: when compatibility_level < 90, maps major version number (9->v2005 .. 16->v2022, _->v2025)
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs -- confirmed: withoutSeparator=true branch calls c.HasSequenceIndex(0), withoutSeparator=false calls c.HasSequenceIndex(1).TranslateArguments(0); separator uses factory.Value(valueType, string.Empty) when withoutSeparator
- Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs -- confirmed: SqlServerStringMemberTranslator.TranslateStringJoin branches on withoutSeparator; TranslateTrimStart/TranslateTrimEnd return null when trimChars != null

</details>
