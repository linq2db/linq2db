---
area: PROV-SQLSERVER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 11/11
coverage_tier_2: 43/45
---

# PROV-SQLSERVER

SQL Server provider for linq2db. Covers two ADO.NET client packages (`System.Data.SqlClient` and `Microsoft.Data.SqlClient`), nine dialect versions (2005–2025), provider detection, bulk-copy via native `SqlBulkCopy`, schema discovery, hints, FTS extensions, and SQL Server 2025 types (`JSON`, `VECTOR`).

## Subsystems

### Public entry points

`SqlServerTools` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerTools.cs:14`) is the sole public static entry point. Key members:

- `GetDataProvider(SqlServerVersion, SqlServerProvider, ...)` — delegates to `SqlServerProviderDetector.GetDataProvider`.
- `CreateDataConnection(...)` — convenience overloads returning `DataConnection`.
- `QuoteIdentifier(string)` / `QuoteIdentifier(StringBuilder, string)` — bracket-escaping (`[name]`, with `]]` for embedded `]`).
- `ResolveSqlTypes(string|Assembly)` — registers spatial types from `Microsoft.SqlServer.Types` at run-time; forwards to `SqlServerProviderDetector.ResolveSqlTypes`, which calls `SqlServerTypes.Configure` on all live provider instances.
- `AutoDetectProvider` — delegates to `ProviderDetector.AutoDetectProvider`.

`SqlServerFactory` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerFactory.cs`) is the config-driven factory: reads `assemblyName` and `version` named values and calls `SqlServerTools.GetDataProvider`.

### Provider and version enums

`SqlServerProvider` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerProvider.cs`) — three values: `AutoDetect`, `SystemDataSqlClient`, `MicrosoftDataSqlClient`.

`SqlServerVersion` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerVersion.cs`) — `AutoDetect` + nine versioned values: `v2005`, `v2008`, `v2012`, `v2014`, `v2016`, `v2017`, `v2019`, `v2022`, `v2025`.

### Provider options

`SqlServerOptions` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs:19`) — sealed record extending `DataProviderOptions<SqlServerOptions>`:
- `BulkCopyType` — default `ProviderSpecific`.
- `GenerateScopeIdentity` — when `true` (default), identity retrieval uses `SCOPE_IDENTITY()`; when `false` or when the identity field is a GUID, falls back to an `OUTPUT [INSERTED].<field> INTO @varTable` pattern (`SqlServerSqlBuilder.cs:43–116`).

### Provider detector

`SqlServerProviderDetector` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:17`) extends [`ProviderDetectorBase<SqlServerProvider,SqlServerVersion>`](../INTERNAL-API/INDEX.md). Behaviour:

- Holds 18 `Lazy<IDataProvider>` singletons — one per (version × client-package) combination (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:22–40`).
- `DetectProvider(ConnectionOptions)` matches provider-name strings containing `"SqlServer"` or `".SqlClient"`, then reads version from the config string if present, else calls `DetectServerVersion`.
- `DetectServerVersion(DbConnection, DbTransaction?)` — executes `SELECT compatibility_level FROM sys.databases WHERE name = db_name()` and maps levels: ≥170→v2025, ≥160→v2022, ≥150→v2019, ≥140→v2017, ≥130→v2016, ≥120→v2014, ≥110→v2012, ≥100→v2008, ≥90→v2005 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:151–193`).
- Default version when detection fails: `v2012`.
- `DetectProvider(options, provider)` — prefers `Microsoft.Data.SqlClient` if `Microsoft.Data.SqlClient.dll` is found next to the assembly (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:200–218`).

### Provider adapter

`SqlServerProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:32`) — singleton per client package (double-checked lock). Implements [`IDynamicProviderAdapter`](../INTERNAL-API/INDEX.md). Key responsibilities:

- Loads the SqlClient assembly at run-time (`Assembly.Load` on non-Framework; `typeof(SqlConnection).Assembly` on Framework).
- Builds `TypeMapper`-backed wrappers for `SqlBulkCopy`, `SqlBulkCopyColumnMapping`, `SqlConnectionStringBuilder`, `SqlException` (for error-number extraction used by `SqlServerTransientExceptionDetector`).
- Exposes `SqlDbType` setter/getter for `SqlParameter`, `UdtTypeName`, `TypeName`.
- For `MicrosoftDataSqlClient` only: loads `SqlJson` (`DataType.Json`), `SqlVector<float>` (`DataType.Vector32`), and `SqlVector<Half>` (`DataType.Vector16`, net8+ only) from the `Microsoft.Data.SqlTypes` namespace; builds `MappingSchema`-registered value-to-SQL converters and round-trip converters for each type (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:328–563`).
- `VectorDbType` hard-codes `(SqlDbType)36` (no named constant in the public API yet).
- `JsonDbType` hard-codes `(SqlDbType)35`.

### Core data provider

`SqlServerDataProvider` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:52`) — `abstract`, extends [`DynamicDataProviderBase<SqlServerProviderAdapter>`](../INTERNAL-API/INDEX.md). 18 concrete `sealed` subclasses are in the same file, one per (version × client-package). Constructor responsibilities:

- Sets `SqlProviderFlags`: `IsApplyJoinSupported`, `IsCommonTableExpressionsSupported`, `OutputDeleteUseSpecialTable`, `OutputInsertUseSpecialTable`, `OutputUpdateUseSpecialTables`, `OutputMergeUseSpecialTables`, `TakeHintsSupported = Percent|WithTies`, `IsDistinctFromSupported = Version >= v2022`, `SupportsBooleanType = false`, `IsUpdateTakeSupported = true`, `IsRowNumberWithoutOrderBySupported = false`.
- Registers `char`/`nchar` trimming; `SqlChars`, `SqlBinary`, `SqlBoolean`, etc. via `SetProviderField` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:100–114`).
- When `SqlJsonType != null` (MicrosoftDataSqlClient + new enough version), registers JSON reader (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:116–127`).
- When `SqlVectorType != null`, registers `SqlVector<float>` and `float[]` readers via dynamic expression build (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:129–142`).
- Selects `_sqlOptimizer` via switch on `(version, provider)` — v2025+SystemDataSqlClient gets `SqlServerSystem2025SqlOptimizer`; all others get the version-named optimizer (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:84–96`).
- `CreateMemberTranslator()` dispatches by `Version`: ≥v2022→`SqlServer2022MemberTranslator`, ≥v2017→`SqlServer2017MemberTranslator`, ≥v2012→`SqlServer2012MemberTranslator`, v2005→`SqlServer2005MemberTranslator`, else→`SqlServerMemberTranslator` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:252–261`).
- `CreateSqlBuilder()` switches on `Version` to instantiate the per-version builder (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:264–279`).
- `SetParameter` handles `DateTimeOffset`→`DateTime` for SmallDateTime/DateTime, `DateTimeOffset` precision for DateTime2/DateTimeOffset/Date, `DateOnly`→`DateTime` (`SUPPORTS_DATEONLY`), decimal precision clamping, `DataType.Structured` for TVP, `DataType.Vector32`/`Vector16` conversion, JSON/`SqlJson` conversion for v2025+ (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:319–580`).
- UDT support: `AddUdtType(Type, string, ...)` registers name↔type mappings into `_udtTypeNames`/`_udtTypes`; `GetUdtTypeByName` resolves by name.
- BulkCopy: lazily instantiates `SqlServerBulkCopy` and delegates; reads `SqlServerOptions.Default.BulkCopyType` when options say `Default` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:688–732`).

`MappingSchemaInstance` inner class selects one of 18 `SqlServerMappingSchema` subclass instances based on (version, provider) (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:212–241`).

### Mapping schema

`SqlServerMappingSchema` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs`) — `sealed LockedMappingSchema`. Defines literal-generation for all SQL Server date/time types: `TIME(p)`, `DATE`, `SMALLDATETIME`, `DATETIME`, `DATETIME2(p)`, `DATETIMEOFFSET(p)`. Uses `TIMEFROMPARTS`, `DATEFROMPARTS`, `DATETIMEFROMPARTS`, `DATETIME2FROMPARTS`, `DATETIMEOFFSETFROMPARTS` factory functions. Provides `ConvertDateTimeOffsetToString` and `ConvertTimeSpanToString` used by `SqlServerDataProvider.SetParameter`. 18 nested mapping-schema subclasses (one per version×provider combination) extend it, layering version-specific rules (e.g., `SqlServer2005MappingSchema` maps `DATE` → `DateTime`; `SqlServer2025MappingSchema` adds VECTOR literal builders).

### SQL builder hierarchy

`SqlServerSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19`) — `public abstract`, extends [`BasicSqlBuilder<SqlServerOptions>`](../SQL-PROVIDER/INDEX.md). Key SQL-Server-specific overrides:

- `FirstFormat` — emits `TOP ({0})` when no `SKIP` is present.
- `BuildInsertQuery`/`BuildOutputSubclause`/`BuildGetIdentity` — implements identity retrieval via `@varOutput TABLE` + `OUTPUT [INSERTED].<field>` or `SCOPE_IDENTITY()`.
- `BuildDeleteClause` — `DELETE <alias>` pattern (not `DELETE FROM <table>`).
- `BuildObjectName` — handles four-part name (`server.database.schema.table`), temp-table `#`/`##` prefix, `tempdb.` injection.
- `Convert` — brackets identifiers with `[…]`, strips `@` from sproc-param names, maps `$action` for MERGE.
- `BuildDropTableStatement` — generates `IF OBJECT_ID(…) IS NOT NULL DROP TABLE …` for ≤2014; 2016+ uses `DROP TABLE IF EXISTS`.
- `BuildCreateTablePrimaryKey` — emits `PRIMARY KEY CLUSTERED`.
- `BuildDataTypeFromDataType` — maps `Guid`→`UniqueIdentifier`, `Variant`→`Sql_Variant`, `NVarChar`/`VarChar`/`VarBinary` with `(MAX)` fallback, `VECTOR(n, float32|float16)` for 2025+ vector types.
- `BuildTableExtensions` / `BuildTableNameExtensions` / `BuildJoinType` / `BuildQueryExtensions` — routes hint extensions into `WITH (…)`, join hint syntax, and `OPTION (…)` clauses.
- `IsSqlValuesTableValueTypeRequired` — forces explicit type for `uint`, `long`, `float`, `double`, `decimal`, `null` in row-0.

### Per-version builder matrix

| Version | Builder class | Key diff vs prior |
|---|---|---|
| v2005 | `SqlServer2005SqlBuilder` | `IsValuesSyntaxSupported = false`; JSON→`NVARCHAR(MAX)`; date types → `DateTime` |
| v2008 | `SqlServer2008SqlBuilder` | `INSERT OR UPDATE` uses MERGE (`BuildInsertOrUpdateQueryAsMerge`); JSON→`NVARCHAR(MAX)` |
| v2012 | `SqlServer2012SqlBuilder` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY`; `IIF()` for `SqlConditionExpression`; MERGE `BY SOURCE`; full merge support with `HasIdentityInsert`; JSON→`NVARCHAR(MAX)` |
| v2014 | `SqlServer2014SqlBuilder` | No diff (inherits v2012) |
| v2016 | `SqlServer2016SqlBuilder` | `DROP TABLE IF EXISTS` via `BuildDropTableStatementIfExists`; `ConvertDateTimeAsLiteral` flag for `FOR SYSTEM_TIME` parameter workaround |
| v2017 | `SqlServer2017SqlBuilder` | No diff (inherits v2016) |
| v2019 | `SqlServer2019SqlBuilder` | No diff (inherits v2017) |
| v2022 | `SqlServer2022SqlBuilder` | Native `IS [NOT] DISTINCT FROM` predicate |
| v2025 | `SqlServer2025SqlBuilder` | JSON→`JSON` (native type); inherits v2022 |

Partial class files `SqlServer2008SqlBuilder.Merge.cs` and `SqlServer2012SqlBuilder.Merge.cs` provide MERGE builder overrides (`BuildMergeInto`, `BuildMergeOperationDeleteBySource`, `BuildMergeOperationUpdateBySource`, `BuildMergeTerminator`) for their respective versions.

### Per-version optimizer matrix

| Version | Optimizer class | Pagination strategy |
|---|---|---|
| v2005 | `SqlServer2005SqlOptimizer` | `ROW_NUMBER`; separates DISTINCT from pagination; wraps UPDATE/DELETE `TOP` |
| v2008 | `SqlServer2008SqlOptimizer` | Same as v2005 (no OFFSET/FETCH) |
| v2012 | `SqlServer2012SqlOptimizer` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY`; `AddOrderByForSkip` injects `ORDER BY (1)` when missing |
| v2014 | `SqlServer2014SqlOptimizer` | Delegates to v2012 (bug: constructor passes `SqlServerVersion.v2016` — see Known issues) |
| v2016 | `SqlServer2016SqlOptimizer` | Delegates to v2012 |
| v2017 | `SqlServer2017SqlOptimizer` | Delegates to v2012 |
| v2019 | `SqlServer2019SqlOptimizer` | Delegates to v2012 |
| v2022 | `SqlServer2022SqlOptimizer` | Delegates to v2019 |
| v2025 (Microsoft) | `SqlServer2022SqlOptimizer` | Same as v2022 |
| v2025 (System) | `SqlServerSystem2025SqlOptimizer` | Same as v2022 + marks `float[]`/`Half[]` params as `IsQueryParameter = false` (no parameterization for vector literals) |

All optimizer versions route through `SqlServerSqlOptimizer.CorrectSqlServerUpdate`, which handles SQL Server's `UPDATE <alias> SET … FROM …` form and the `UPDATE TOP n` single-table case (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs:63–163`).

### Expression convert visitors

`SqlServerSqlExpressionConvertVisitor` (base, all versions) — extends [`SqlExpressionConvertVisitor`](../SQL-PROVIDER/INDEX.md):
- `ConvertSearchStringPredicate` — wraps case-sensitive LIKE with a `CONVERT(VARBINARY, …)` equality check for `StartsWith`/`EndsWith`/`Contains`.
- `ConvertSqlBinaryExpression` — converts float modulo `%` via `CONVERT(INT, …)`.
- `ConvertConversion` — default `DECIMAL` precision 38,17; calls `FloorBeforeConvert`.
- `WrapColumnExpression` — adds mandatory `CAST` for `uint`/`long`/`ulong`/`float`/`double`/`decimal` values and inline parameters.
- `ConvertSqlFunction(LENGTH)` — `LEN(value + '.') - 1` pattern.

Chain: v2005 removes `TIME` cast (unsupported); v2008 re-enables it; v2012 adds `TRY_CONVERT` pseudo-function → native `TRY_CONVERT(type, expr)`. v2022 optimizer sets `SupportsDistinctAsExistsIntersect = (_sqlServerVersion < v2022)`.

### Member translator hierarchy

`SqlServerMemberTranslator` (base, v2008+2009+2010+2011 range) — extends [`ProviderMemberTranslatorDefault`](../INTERNAL-API/INDEX.md):
- `SqlServerDateFunctionsTranslator` — maps `DatePart` enum to T-SQL date-part strings; translates `DatePart`, `DateAdd`, `DateDiff`, truncate-to-date via `DATEADD(dd, DATEDIFF(dd,0,x), 0)`.
- `SqlServerMathMemberTranslator` — standard math.
- `SqlServerStringMemberTranslator` — string functions.

| Translator | Key additions |
|---|---|
| `SqlServer2005MemberTranslator` | `DATE` → `DATETIME`; truncate-to-date uses DATEADD/DATEDIFF pattern |
| `SqlServer2012MemberTranslator` | `DATETIME2FROMPARTS`/`DATETIMEFROMPARTS` for `MakeDateTime` |
| `SqlServer2017MemberTranslator` | `STRING_AGG` for `string.Join`; `REPLICATE + value` for `PadLeft` |
| `SqlServer2022MemberTranslator` | `GREATEST`/`LEAST` for `Math.Max`/`Math.Min` |

### Bulk copy

`SqlServerBulkCopy` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs:16`) extends [`BasicBulkCopy`](../INTERNAL-API/INDEX.md):
- `MaxParameters = 2099`, `MaxSqlLength = 327670`.
- `ProviderSpecificCopy*` — tries to unwrap to a native `SqlConnection`/`SqlTransaction`; if successful, uses `SqlBulkCopy` (wrapper) via `ProviderSpecificCopyInternal(Async)`. Falls back to `MultipleRowsCopy`.
- `MultipleRowsCopy` — v2005 uses `MultipleRowsCopy2`; all others use `MultipleRowsCopy1`. Wraps bulk with `SET IDENTITY_INSERT <table> ON/OFF` when `KeepIdentity = true`.
- `CreateRowsHelper` — for `SystemDataSqlClient`, sets `ConvertToParameter` to skip parameterization for `float[]`/`Half[]` columns (vector types must be sent inline).
- `ProviderSpecificCopyInternal` maps `BulkCopyOptions` flags to `SqlBulkCopyOptions` enum.

### Schema provider

`SqlServerSchemaProvider` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs`) extends [`SchemaProviderBase`](../INTERNAL-API/INDEX.md):
- `InitProvider` detects Azure (via `@@version` containing `"Azure"`) and reads `CompatibilityLevel`.
- `GetTables` uses two different SQL branches: Azure (no `sys.extended_properties`) vs on-prem (with description from `INFORMATION_SCHEMA.TABLES` + `sys.extended_properties`). Filters temporal history tables when `CompatibilityLevel >= 130` and `IgnoreSystemHistoryTables`.
- Additional overrides for columns, foreign keys, procedures, etc. (body not fully read; pattern follows `SchemaProviderBase` contract).

### Hints

`SqlServerHints` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerHints.cs`) — `public static partial class` with three nested classes: `Table`, `Join`, `Query`. Constants are string literals matching T-SQL hint syntax. `Table.SpatialWindowMaxCells(int)` generates a `SPATIAL_WINDOW_MAX_CELLS=n` fragment via `[Sql.Expression]`.

`SqlServerHints.generated.cs` — T4-generated extension methods wiring hint constants into the `WithTableHint`/`WithJoinHint`/`WithQueryHint` pipeline on `ISqlServerSpecificTable<T>` and `ISqlServerSpecificQueryable<T>` (`WITH (NOLOCK)` → `.With(SqlServerHints.Table.NoLock)`, `OPTION (RECOMPILE)` → `.Option(SqlServerHints.Query.Recompile)`, etc.).

`SqlServerSqlBuilder.BuildTableExtensions` emits table hints as `WITH (hint1, hint2)` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:489–491`). `BuildQueryExtensions` emits query-level hints as `OPTION (hint)` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:539–542`). Join hints are injected directly into `JOIN` keyword — e.g., `INNER LOOP JOIN` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:521–534`).

### Extensions surface

`SqlServerExtensions` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerExtensions.cs`) — FTS extensions: `FreeTextTable<TTable,TKey>` and `ContainsTable<TTable,TKey>` overloads map to `FREETEXTTABLE(…)` / `CONTAINSTABLE(…)` via `[ExpressionMethod]` + `FromSql` raw queries.

`SqlServerSpecificExtensions` — `AsSqlServer<T>(ITable<T>)` and `AsSqlServer<T>(IQueryable<T>)` return `ISqlServerSpecificTable<T>` / `ISqlServerSpecificQueryable<T>` for hint-scoped extension chaining.

### SQL Server 2025 additions

`SqlFn` (`Source/LinqToDB/DataProvider/SqlServer/SqlFn.cs:19`) — `[PublicAPI]` static class of server-side-only T-SQL function mappings via `[Sql.Expression(ProviderName.SqlServer, …)]`. Covers configuration functions (`@@DBTS`, `@@LANGID`, etc.), cursor functions, date/time, JSON functions, string functions, and more. This is an older pattern (pre-member-translator); new SQL-2025-era functions should use the member translator instead. The class is large but functionally pure — each member is a `ServerSideOnly` expression.

`SqlType` (`Source/LinqToDB/DataProvider/SqlServer/SqlType.cs:9`) — `abstract` base; `SqlType<T>` generic subclass. Static factory properties/methods expose all T-SQL type literals as `[Sql.Expression]`-annotated `SqlType<T>` instances for use in `Cast<T>` expressions. Covers all type families including `VECTOR(n)`, `JSON`, `HIERARCHYID`.

### Spatial type support

`SqlServerTypes` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs`) — lazily loads `Microsoft.SqlServer.Types` assembly, registers `SqlHierarchyId`, `SqlGeography`, `SqlGeometry` into the mapping schema. `Configure(SqlServerDataProvider)` calls `AddUdtType` on each found type.

`SystemDataSqlServerAttributeReader` (`Source/LinqToDB/DataProvider/SqlServer/SystemDataSqlServerAttributeReader.cs:26`) — implements `IMetadataReader`. Reads `[SqlMethod]` and `[SqlUserDefinedType]` attributes from spatial types (three static instances: `SystemDataSqlClientProvider`, `MicrosoftDataSqlClientProvider`, `MicrosoftSqlServerServerProvider` for the three assembly sources). Converts them to `Sql.ExpressionAttribute` / `DataTypeAttribute(DataType.Udt, …)` mappings at metadata-read time.

### Transient exception / retry

`SqlServerTransientExceptionDetector` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerTransientExceptionDetector.cs:17`) — static, registration-based. `RegisterExceptionType` is called by `SqlServerProviderAdapter.CreateAdapter` to bind `SqlException`→error-number extractor. `ShouldRetryOn` matches ~22 known transient SQL error codes. `IsHandled` returns the error numbers for additional classification.

`SqlServerRetryPolicy` (`Source/LinqToDB/DataProvider/SqlServer/SqlServerRetryPolicy.cs:13`) — extends `RetryPolicyBase` (exponential backoff). `ShouldRetryOn` calls `SqlServerTransientExceptionDetector.ShouldRetryOn`; memory-optimized errors (41301–41839) use a shorter delay (`GetNextDelay` returns `TotalSeconds` not `TotalMilliseconds`).

## Key types

| Type | File | Role |
|---|---|---|
| `SqlServerDataProvider` | `Internal/DataProvider/SqlServer/SqlServerDataProvider.cs` | Abstract base; 18 concrete subclasses per version×provider |
| `SqlServerSqlBuilder` | `Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs` | Abstract SQL builder base; handles OUTPUT, IDENTITY, temp tables, hints |
| `SqlServerSqlOptimizer` | `Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs` | Optimizer base; `CorrectSqlServerUpdate` handles UPDATE alias pattern |
| `SqlServerProviderAdapter` | `Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs` | Dynamic ADO.NET bridge; loads SqlClient, BulkCopy, JSON/Vector types |
| `SqlServerProviderDetector` | `Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs` | Provider auto-detection; version detection via `compatibility_level` |
| `SqlServerMappingSchema` | `Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs` | Literal generation for all SQL Server date/time types |
| `SqlServerBulkCopy` | `Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs` | BulkCopy via `SqlBulkCopy`; fallback `MultipleRowsCopy` |
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
| `SqlServerSqlExpressionConvertVisitor` | `Internal/DataProvider/SqlServer/SqlServerSqlExpressionConvertVisitor.cs` | Expression conversion; case-sensitive LIKE, float %, decimal precision |

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

**Tier 2** (45 files, 43 visited):

| File | Notes |
|---|---|
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2005SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2005SqlExpressionConvertVisitor.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2005SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2008SqlBuilder.Merge.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2008SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2008SqlExpressionConvertVisitor.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2008SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlBuilder.Merge.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlExpressionConvertVisitor.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2014SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2014SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2016SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2016SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2017SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2017SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2019SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2019SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2022SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2022SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2025SqlBuilder.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlExpressionConvertVisitor.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSystem2025SqlOptimizer.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs` | Read (first 80 lines; pattern clear) |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSpecificQueryable.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSpecificTable.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs` | Read (first 80 lines; pattern clear — skipped detail query methods) |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServerMemberTranslator.cs` | Read (first 80 lines; complete picture of hierarchy) |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2005MemberTranslator.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2012MemberTranslator.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2017MemberTranslator.cs` | Read |
| `Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2022MemberTranslator.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/ISqlServerExtensions.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/ISqlServerSpecificQueryable.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/ISqlServerSpecificTable.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SqlFn.cs` | Read (first 80 lines; pattern documented) |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerExtensions.cs` | Read (first 80 lines; FTS pattern documented) |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerFactory.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerHints.cs` | Read (first 80 lines; hint catalog structure documented) |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerRetryPolicy.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerSpecificExtensions.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SqlServerTransientExceptionDetector.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SystemDataSqlServerAttributeReader.cs` | Read |
| `Source/LinqToDB/DataProvider/SqlServer/SqlType.cs` | Read (first 60 lines; type-factory pattern documented) |
| skipped: `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs` detail query methods | body > first 80 lines; column/FK/proc query methods follow `SchemaProviderBase` contract; pattern clear from header |
| skipped: `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs` tail | body > 150 lines; format-string definitions + mapping registrations; pattern clear from header |

## Inbound / outbound dependencies

**Inbound:**
- `DataConnection.UseSqlServer` (EFCore companion, `EFCORE` area) calls `SqlServerTools.GetDataProvider`.
- `SqlServerFactory` is registered via `DataProviderFactoryBase`; config-driven providers call it.
- `SqlServerRetryPolicy` is instantiated by user code; no internal callers.

**Outbound:**
- Extends [`BasicSqlBuilder`](../SQL-PROVIDER/INDEX.md), [`BasicSqlOptimizer`](../SQL-PROVIDER/INDEX.md) — SQL generation and optimization pipeline.
- Extends [`DynamicDataProviderBase<SqlServerProviderAdapter>`](../INTERNAL-API/INDEX.md) — ADO.NET lifecycle.
- Extends [`ProviderDetectorBase`](../INTERNAL-API/INDEX.md) — provider auto-detect infrastructure.
- Extends [`BasicBulkCopy`](../INTERNAL-API/INDEX.md) — bulk-copy base.
- Extends [`SchemaProviderBase`](../INTERNAL-API/INDEX.md) — schema discovery base.
- Extends [`ProviderMemberTranslatorDefault`](../INTERNAL-API/INDEX.md) — member translation.
- Uses [`TypeMapper`](../INTERNAL-API/INDEX.md) (`Internal/Expressions/Types/TypeMapper.cs`) for dynamic adapter wrappers.
- `SqlServerMappingSchema` extends [`LockedMappingSchema`](../MAPPING/INDEX.md).
- `SystemDataSqlServerAttributeReader` implements [`IMetadataReader`](../METADATA/INDEX.md).

## Known issues / debt

- `SqlServer2014SqlOptimizer` constructor erroneously passes `SqlServerVersion.v2016` instead of `v2014` to its `base` call (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2014SqlOptimizer.cs:8`). Functionally harmless (the version field is used only for visitor creation and v2014/v2016 visitors are the same), but misleading.
- `GetConnectionInfo("IsMarsEnabled")` is marked `[Obsolete]` for removal in v7 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:293`). The `_marsFlags` cache still exists.
- `SqlConnectionStringBuilder` wrapper is `[Obsolete]` for removal in v7 (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:183`).
- TODO comment at `SqlServer2025SqlBuilder` VECTOR reader (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:146`): "review implementation after SqlClient adds support for this type".
- TODO comment for `SqlHalfVectorType` (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:179`): implementation may need adjustment when `SqlClient` adds official `float16` vector support.
- The inheritance chain for builders (2005→2008→2012→2014→2016→2017→2019→2022→2025) causes near-duplicate MERGE logic in both `SqlServer2008SqlBuilder.Merge.cs` and `SqlServer2012SqlBuilder.Merge.cs`. The `SqlServer2012SqlBuilder.Merge.cs` itself notes: "TODO: both 2008 and 2012 builders inherit from same base class which leads to duplicate builder logic" (`Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlBuilder.Merge.cs:8`).
- `SqlFn` covers many T-SQL functions with the older `[Sql.Expression]` pattern; new functions (STRING_AGG, GREATEST, LEAST, DATETRUNC) are now added via member translators, creating two parallel extension points.

## See also

- [SQL-PROVIDER area](../SQL-PROVIDER/INDEX.md) — `BasicSqlBuilder`, `BasicSqlOptimizer`, `ISqlBuilder`, `ISqlOptimizer`
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) — `DataProviderBase`, `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `MemberTranslatorBase`, `TypeMapper`, `SchemaProviderBase`
- [MAPPING area](../MAPPING/INDEX.md) — `MappingSchema`, `LockedMappingSchema`
- [METADATA area](../METADATA/INDEX.md) — `IMetadataReader`, `SchemaProviderBase`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11 ✓
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerTools.cs
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerProvider.cs
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerVersion.cs
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs
  - Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs
- Tier 2 (visited / total): 43 / 45 (95.6%) ✓
  - skipped (partial read, pattern clear): Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs — column/FK/procedure query methods follow SchemaProviderBase contract; first 80 lines read
  - skipped (partial read, pattern clear): Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs — format-string body and mapping registrations; first 150 lines read; full schema pattern documented
- Tier 3 (skipped, logged): 2
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerHints.generated.cs (T4-generated)
  - Source/LinqToDB/DataProvider/SqlServer/SqlServerHints.tt (T4 template source)
</details>
