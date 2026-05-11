---
area: TESTS-LINQ
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 4/4
coverage_tier_2: 524/600
---

# TESTS-LINQ

Integration and regression test suite under `Tests/Linq/`. The largest area in the KB: ~602 files across 23 subdirectories. The area's KB value is taxonomic -- knowing *what feature surface each subdirectory exercises* and *which production area it validates*, not the content of individual fixtures.

All test classes extend `TestBase` (from `Tests.Tools`). Provider selection uses `[IncludeDataSources(...)]` / `[DataSources(...)]` NUnit parameterized-fixture attributes; `TestProvName.*` constants identify provider sets. The single assembly `[SetUpFixture]` is `TestsInitialization` (root, no namespace).

## Assembly setup

`Tests/Linq/TestsInitialization.cs` -- NUnit `[SetUpFixture]`, no namespace (intentional). `[OneTimeSetUp]` runs once per assembly:

- Pre-loads the SQLite native runtime (`e_sqlite3`) via `NativeLibrary.SetDllImportResolver` on .NET 8+, or forces a `SQLiteConnection` construction on .NET Framework to ensure SDS loads before other providers.
- Registers `ActivityHierarchyFactory` (debug) or `ActivityStatistics.Factory` (metrics) via `ActivityService`.
- Forces `ClickHouseOptions.Default` with `UseStandardCompatibleAggregates = true` -- required for test expectations.
- Registers `SqlCE` provider factory from a hardcoded path on non-NETFX.
- Sets `OracleConfiguration.SqlNetAllowedLogonVersionClient = Version11` (enables Oracle 11 protocol with v23 client).
- On NETFX (non-Azure): installs `AppDomain.AssemblyResolve` handler to resolve IBM.Data.DB2 and IBM.Data.Informix from GAC.
- Calls `TestNoopProvider.Init()`, `SQLiteMiniprofilerProvider.Init()`, `CustomizationSupport.Init()`.
- `[OneTimeTearDown]`: dumps `ActivityStatistics.GetReport()` and optionally writes metrics baselines via `BaselinesWriter.WriteMetrics`.

## Subsystems (subdirectory taxonomy)

| Subdirectory | File count | Purpose | Representative files | Production area(s) validated |
|---|---|---|---|---|
| `Linq/` | ~176 | Per-LINQ-operator/feature fixtures. Core of the suite. | `JoinTests.cs`, `GroupByTests.cs`, `CteTests.cs`, `EagerLoadingTests.cs`, `WindowFunctionsTests.cs` | EXPR-TRANS, SQL-PROVIDER, SQL-AST |
| `UserTests/` | ~256 | Issue-numbered regression repros (`Issue<N>Tests.cs`). | `Issue2296Tests.cs`, `Issue5302Tests.cs`, `Issue3586Tests.cs` | All areas (cross-cutting) |
| `Update/` | ~44 | DML operations: Insert, Update, Delete, Merge, BulkCopy, CreateTable, TempTable, TruncateTable, MultiInsert, OutputWithRows. Merge family has 18 partial files. InsertWithOutput/DeleteWithOutput/UpdateWithOutput cover SQL Server, Firebird, PostgreSQL, SQLite, Ydb output-clause support. | `MergeTests.cs` (+17 partials), `BulkCopyTests.cs`, `InsertTests.cs`, `UpdateTests.cs`, `UpdateWithOutputTests.cs` | EXPR-TRANS, SQL-PROVIDER, PROV-* |
| `DataProvider/` | ~33 | Provider-specific type-mapping and vendor feature tests. Root files cover Access, DB2, Informix, SqlCe, SqlServer, PostgreSQL (array + extensions), SQLite, Ydb. `Types/` subfolder has 8 per-vendor type-framework files (DuckDBTypeTests.cs added). | `SqlServerTests.cs`, `OracleTests.cs`, `PostgreSQLTests.cs`, `SqlServerVectorTypeTests.cs` | PROV-SQLSERVER, PROV-ORACLE, PROV-POSTGRES, PROV-DUCKDB, etc. |
| `Extensions/` | ~18 | Query hints, table aliases, vendor SQL extensions (`SqlServer`, `MySQL`, `Oracle`, `PostgreSQL`, `SQLite`, `Access`, `ClickHouse`). `.generated.cs` files are T4 output (see below). | `QueryHintsTests.cs`, `SqlServerTests.cs`, `PostgreSQLTests.cs`, `DocExampleTests.cs` | SQL-PROVIDER, PROV-* |
| `Mapping/` | ~13 | Fluent mapping, attribute mapping, `MappingSchema`, value converters, enum mapping, dynamic columns, column aliases, expression methods, `ConversionType`. | `FluentMappingTests.cs`, `MappingSchemaTests.cs`, `FluentMappingBuildTests.cs`, `FluentMappingExpressionMethodTests.cs` | METADATA |
| `Data/` | ~9 | `DataConnection`, transactions, retry policy, tracing, interceptors, stored procedures, `QueryMultipleResult`, MiniProfiler integration. | `InterceptorsTests.cs`, `DataConnectionTests.cs`, `TransactionTests.cs`, `MiniProfilerTests.cs` | INTERCEPTORS, INTERNAL-API |
| `Common/` | ~11 | Utility/runtime helpers: `ChangeType`, `Convert`, `Extensions`, `DefaultValue`, `ValueComparer`, reserved words, `EnumerableHelper`, `DataTools`, `ConnectionBuilder`, `SettingsReader`. | `ConvertTests.cs`, `ValueComparerTests.cs`, `ReservedWordTest.cs` | INTERNAL-API |
| `Exceptions/` | ~9 | Validates that unsupported operations throw expected exception types. Mirrors `Linq/` categories but tests error paths. Includes `StackUseTests.cs` for `ExpressionVisitorBase` / `QueryElementVisitor` stack-hop behavior. | `CommonTests.cs`, `JoinTests.cs`, `AggregationTests.cs`, `StackUseTests.cs` | EXPR-TRANS, SQL-AST |
| `Infrastructure/` | ~6 | Test infrastructure self-tests: `ActiveIssue` attribute behavior, `DataOptions` builder, `IdentifierBuilder`, nullability context, `Annotatable`. | `ActiveIssueConfigurationTests.cs`, `DataOptionsTests.cs`, `AnnotatableTests.cs`, `NullabilityContextTests.cs` | TESTS-INFRA, INTERNAL-API |
| `Metadata/` | ~3 | Attribute reader, XML reader, `System.Data.Linq` attribute reader (NETFX-only). | `AttributeReaderTests.cs`, `XmlReaderTests.cs`, `SystemDataLinqAttributeReaderTests.cs` | METADATA |
| `SchemaProvider/` | ~3 | Schema introspection: `SchemaProviderTests.cs`, `PostgreSQLSchemaProviderTests.cs`, `SqlServerTests.cs`. | `SchemaProviderTests.cs` | SCAFFOLD |
| `Scaffold/` | ~3 | Code generation name resolution and type-mapping: `NameGenerationTests.cs`, `SchemaProviderTests.cs`, `TypeParserTests.cs`. | `NameGenerationTests.cs` | SCAFFOLD, IN-TREE-TOOLS |
| `TypeMapping/` | ~2 | Dynamic type-mapping wrapper tests (Oracle and generic). `MappingTests.cs` exercises `ExpressionTypeMapper`, delegate mapping, event wrapping. | `OracleWrappingTests.cs`, `MappingTests.cs` | PROV-ORACLE, INTERNAL-API |
| `Reflection/` | ~2 | `TypeAccessor` and attribute reflection tests. `AttributesTests.cs` includes `DynamicColumnInfo` attribute API coverage. | `TypeAccessorTests.cs`, `AttributesTests.cs` | INTERNAL-API |
| `Tools/` | ~5 | `ComparerBuilder`, `DecimalHelper`, `ToDiagnosticString`, entity-service identity map, mapper tests. `MapperTests.cs` exercises `MapperBuilder<TFrom,TTo>` and `Map.GetMapper<T1,T2>()`. | `MapperTests.cs`, `IdentityMapTests.cs` | IN-TREE-TOOLS |
| `Samples/` | ~4 | Illustrative usage patterns: concurrency check, exception intercept, join operator, JSON conversion. `JsonConvertTests.cs` demonstrates `MappingSchema`-based JSON column converter via `Newtonsoft.Json`. | `ConcurrencyCheckTests.cs`, `JsonConvertTests.cs` | INTERNAL-API |
| `OrmBattle/` | ~3 | Tests ported from the [ORMBattle.NET](http://ormbattle.net) benchmark suite (originally by Alexis Kochetov, 2009; T4-generated, updated 2015). Uses `Northwind` test model. `Helper/` contains `ExpressionUtils` and `GenericEqualityComparer`. | `OrmBattleTests.cs`, `Helper/ExpressionUtils.cs`, `Helper/GenericEqualityComparer.cs` | EXPR-TRANS, SQL-PROVIDER |
| `Microsoft/` | ~1 | OData query-composition integration test against `Microsoft.AspNetCore.OData` (net8+) and `Microsoft.AspNet.OData` (net462). | `MicrosoftODataTests.cs` | INTERNAL-API, EXPR-TRANS |
| `ThirdParty/` | ~1 | Third-party LINQ extension compatibility (`LinqKit.Core` -- `PredicateBuilder`, `AsExpandable()`). | `LinqKitTests.cs` | EXPR-TRANS |
| `AST/` | ~1 | SQL AST unit tests: `SqlDataTypeTests.cs`. Single test: `SqlDataType.GetDataType(DataType.Boolean).SystemType`. | `SqlDataTypeTests.cs` | SQL-AST |
| `Create/` | ~1 | `CreateData.cs` -- utility class (no namespace, class `a_CreateData`) that populates test-database tables for all providers. Uses `BulkCopy` for seed rows and per-provider `DbConnection` action callbacks for binary/text data. `[Order(-1)]` ensures it runs first. | `CreateData.cs` | TESTS-INFRA |

## Delta since prior run (sha 7f972dbce → 4a478ff14)

~70 files changed across six cross-cutting PRs:

### PR #5451 -- DuckDB provider tests

New file: `DataProvider/Types/DuckDBTypeTests.cs` -- sealed `DuckDBTypeTests : TypeTestsBase` tagged `[TestFixture]` under `#if SUPPORTS_DATEONLY`. Exercises the full DuckDB type surface via `TypeTestsBase.TestType<TType,TNullableType>()`:

- **Boolean:** `bool`.
- **Integer:** `sbyte`/`byte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`, `BigInteger` (Int128 / UInt128 / variable-length HugeInt/BigNum). Provider bug with UHugeInt range commented out; BigNum (VARINT) uses literal-only mode (`expectedParamCount: 0`) due to provider read corruption.
- **Float/Double:** including `NaN`, `PositiveInfinity`, `NegativeInfinity`.
- **Decimal:** DECIMAL(18,3) default plus precision 1..38 with scale sweep.
- **String/Binary:** VARCHAR (string + char), BLOB (byte[] + `System.Data.Linq.Binary`), BITSTRING (BitArray + string + byte[] with `DataType.BitArray`; provider: `expectedParamCount: 0`, BulkCopy excluded).
- **UUID:** `Guid`.
- **Date/Time:** DATE (DateOnly + DateTime.Date + `DuckDBDateOnly` with infinity); TIME (TimeOnly + TimeSpan + precision 0/5/6; TIME_NS p=7/9 disabled due to provider `ArgumentException`); TIMETZ (TimeSpan + DateTimeOffset); INTERVAL (TimeSpan + `DuckDBInterval`; negative interval support missing); TIMESTAMP (DateTime × TIMESTAMP/TIMESTAMP_S/TIMESTAMP_MS/TIMESTAMP_NS precision ladder + `DuckDBTimestamp` native type); TIMESTAMPTZ (DateTimeOffset).
- **JSON:** string with `DataType.Json`.
- `TestBulkCopyType` local function skips `BulkCopyType.ProviderSpecific` for types the provider doesn't support in bulk copy.
- Known provider limitations documented in comments: UTINYINT range bug, `\0`/`\x1` char parameter issues, TIME_NS type code 39 unknown, negative INTERVAL, UHugeInt range, BigNum read corruption.

DuckDB was also added to existing fixtures (sampled from changed file list): `ConflictActionTests.cs` adds `TestProvName.AllDuckDB` to `[IncludeDataSources]` for `IgnoreConflictsTest` / `IgnoreConflictsTestAsync` -- validates `BulkCopyOptions { ConflictAction = ConflictAction.Ignore }` with `MultipleRows` mode ignores PK conflicts and inserts non-conflicting rows. Similar DuckDB additions are present in `BulkCopyTests.cs`, `DateTimeFunctionsTests.cs`, `DateTimeOffsetTests.cs`, `DataTypesTests.cs`, `MergeTests.*`, and other fixtures throughout the delta set.

### PR #5495 -- AsQueryable parameterization (issue #5424)

New file: `Linq/EnumerableSourceTests.AsQueryable.cs` -- partial of `EnumerableSourceTests`. Covers the new three-argument `IEnumerable<T>.AsQueryable(IDataContext, Action<IAsQueryableBuilder>)` overload added in PR #5495. Tests:

- `AsQueryable_Parameterize_AllParameters` -- verifies SQL contains no inlined literals when `.Parameterize()` is configured.
- `AsQueryable_Inline_AllInlined` -- verifies SQL contains inlined literals when `.Inline()` is configured.
- `AsQueryable_Parameterize_ExceptId_InlinesId` / `AsQueryable_Inline_ExceptData_ParameterisesData` -- `.Except(p => p.Id)` / `.Except(p => p.Data)` flip individual member between inline/parameter mode.
- Cache stability: `AsQueryable_Parameterize_CacheStable_AcrossDataChanges` -- same-shape but different-data second query hits cache (no `GetCacheMissCount()` increase).
- Cache hit across NUnit `[Values(1, 2)]` iterations for `Parameterize`, `Inline`, and `Parameterize().Except(p => p.Id)` modes.
- Scalar int list, inline array, inline-array-in-SelectMany (expects `LinqToDBException` with "AsQueryable configure" message).
- JOIN and CROSS APPLY patterns through parameterized enumerable source.
- Nested member (`p.Address!.Zip`) in `.Except()`.
- Error cases: non-member selector (`p.Id + 1`), bare parameter (`p => p`), captured external member (`other.Id`) all throw `LinqToDBException`.
- `CompiledQuery.Compile` integration -- static `_compiledConfiguredAsQueryable` field reused across two invocations with different row counts/seeds.
- Provider exclusions: Access and ClickHouse excluded from most tests (join/apply patterns further restrict to SQL Server 2008+, PostgreSQL 9.3+, Oracle 12+, MySqlWithApply).

### PR #5467 -- DateTime.Now translation

`DateTimeFunctionsTests.cs` and `DateTimeOffsetTests.cs` updated. Changes are at the summary level (DuckDB provider additions and per-provider now-translation behavior assertions). The precise test changes verify that `DateTime.Now` / `DateTimeOffset.Now` / `DateTime.UtcNow` emit the correct provider-specific SQL for DuckDB (and may adjust expectations for other providers). See PROV-DUCKDB area for translation specifics.

### PR #5517 -- DateTime.Date DbType preservation

`DateTimeFunctionsTests.cs` updated. Issue #5309: `DateTime.Date` truncation cast was dropping the column's original `DbType`, causing incorrect type on the generated SQL parameter. Tests updated to verify the `DbType` is preserved after `.Date` access on a typed column. Involves `SaveCommandInterceptor` pattern to inspect `DbCommand.Parameters` (similar to `Issue488Tests.cs` pattern).

### PR #5503 -- Enum.HasFlag translation

`ExpressionsTests.cs` updated -- the `Expressions.MapMember` for `Enum.HasFlag` section receives additional coverage or provider-specific assertions. ClickHouse uses `bitShiftLeft` extension; SQL Server and others use bitwise AND. The test validates the SQL shape via `ToSqlQuery()`.

### PR #5455 -- BulkCopy ConflictAction

`ConflictActionTests.cs` (new or substantially revised) -- validates `BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, ConflictAction = ConflictAction.Ignore }` against MySQL/PostgreSQL/SQLite/DuckDB. Sync and async variants. Verifies: conflicting PK rows (1, 2) retain original values; non-conflicting row (3) is inserted. Both `table.BulkCopy(...)` and `table.BulkCopyAsync(...)` paths exercised.

### Other changed files (cross-cutting)

The remaining ~55 changed files fall into these categories (not individually enumerated -- delta is additive, not structural):

- **`Data/`**: `DataConnectionTests.cs`, `DataExtensionsTests.cs`, `TransactionTests.cs` -- minor updates, likely DuckDB connection-lifecycle additions and/or async transaction coverage refinements.
- **`DataProvider/`**: `FirebirdTests.cs`, `PostgreSQLTests.cs`, `SqlServerTests.cs` -- provider-specific additions (DuckDB ripple or independent fixes).
- **`Extensions/ClickHouseTests.cs`**: ClickHouse-specific additions.
- **`Infrastructure/DataOptionsTests.cs`**: `DataOptions` builder coverage updates.
- **`Linq/`**: ~30 files -- primarily DuckDB provider additions to `[IncludeDataSources]` or `[DataSources]` attributes across `AnalyticTests`, `CharTypesTests`, `CteTests`, `CteMaterializedTests`, `DataTypesTests`, `DefaultIfEmptyTests`, `EnumerableSourceTests`, `ExceptByMethodTests`, `GroupByExtensionsTests`, `IdlTests`, `InterfaceTests`, `IntersectByMethodTests`, `JoinTests`, `MinByMaxByMethodTests`, `ParameterTests`, `PredicateTests`, `SetOperatorTests`, `SqlExtensionTests`, `StringFunctionTests`, `StringFunctionsTests`, `SubQueryTests`, `TableOptionsTests`, `TypesTests`, `UnionByMethodTests`, `WhereTests`. Also `ConvertExpressionTests`, `ConvertTests` -- expression/conversion coverage updates.
- **`Mapping/`**: `ConversionTypeTests.cs`, `FluentMappingBuildTests.cs` -- mapping coverage updates.
- **`Update/`**: `BulkCopyTests.cs`, `DeleteTests.cs`, `DeleteWithOutputTests.cs`, `InsertTests.cs`, `InsertWithOutputTests.cs`, `MergeTests.*` (10 files), `UpdateFromTests.cs`, `UpdateTests.cs`, `OldMergeTests.cs` -- DuckDB provider additions and possibly conflict-action coverage expansion.
- **`UserTests/`**: `Issue1238Tests.cs`, `Issue269Tests.cs`, `Issue3432Tests.cs`, `Issue356Tests.cs`, `Issue445Tests.cs`, `Issue792Tests.cs`, `LetTests.cs` -- regression additions or DuckDB provider additions.

## Naming patterns

- **`<Feature>Tests.cs`** -- primary fixture style in `Linq/`, `Exceptions/`, `Update/`, `Data/`, `Mapping/`, `Extensions/`. One class per file; class name matches file name.
- **`Issue<N>Tests.cs`** -- `UserTests/` pattern. `N` is the GitHub issue number. Issues start at 10 and run to 5458+ (as of HEAD). File-per-issue, one or few test methods. Some non-issue files in `UserTests/` document a reproducer without a tracked issue (e.g. `GroupBySubqueryTests.cs`, `SelectManyUpdateTests.cs`).
- **Partial-class spreads** -- used when a fixture family is too large or has provider-specific branches:
  - `Linq/WindowFunctionsTests.*.cs` (13 files: `Average`, `Cume`, `DenseRank`, `Frame`, `Max`, `Min`, `NTile`, `PercentRank`, `PercentileCont`, `Rank`, `RowNumber`, `Sum` + root). **All 13 are excluded from the project via `<Compile Remove>` in `Tests.csproj`** -- they are work-in-progress or gated tests, not compiled into the assembly.
  - `Linq/FullTextTests.*.cs` -- three provider-specific partials (`SqlServer`, `SQLite`, `MySql`); `[Category(TestCategory.FTS)]` gates FTS tests.
  - `Linq/ParameterTests.*.cs` -- base + `SqlServer` + `FSharp` provider partials.
  - `Linq/IsNullTests.SqlServer.cs` -- SQL Server-specific `IS NULL` optimization tests.
  - `Update/MergeTests.*.cs` -- 18 partial files (root `MergeTests.Issues.cs` + 17 operation/sub-API files). See **MergeTests family** below.
  - `Update/UpdateFromTests.Row.cs` -- row-constructor variant of update-from.
- **`.generated.cs` files** -- `Extensions/` has T4-generated files: `MySqlTests.generated.cs`, `PostgreSQLTests.generated.cs`, `OracleTests.generated.cs`, `SqlServerTests.generated.cs`, `SqlCeTests.generated.cs`. Each is a `partial class` extending its sibling handwritten fixture.

## Notable per-fixture findings

**AST:**
- `SqlDataTypeTests.cs` -- minimal single test: verifies `SqlDataType.GetDataType(DataType.Boolean).SystemType == typeof(bool)`. Validates the `DataType`-to-`SystemType` lookup table in `SqlDataType`.

**Common:**
- `ConvertTests.cs` -- exercises `Convert<TFrom,TTo>`, `ConvertTo<T>`, `ConvertBuilder.GetConverter`, `MappingSchema.GetConverter<T1,T2>()`, `LinqToDBConvertException` for ambiguous `[MapValue]`. Covers `Convert<int,string>.Lambda` / `.Expression` setters. Also tests nullable operator-parameter edge case in `ConvertBuilder.GetConverter`.
- `ConnectionBuilderTests.cs` -- tests `DataOptions.UseLoggerFactory()` / `UseDefaultLogging()` wiring; verifies `QueryTraceOptions.WriteTrace` is populated from `ILoggerFactory`. No database access except one SQL Server parameterized test.
- `DataToolsTests.cs` -- unit tests for `DataTools.ConvertStringToSql` (null-byte escaping, `chr(N)` emission for Access-style SQL) and `DataTools.EscapeUnterminatedBracket` (LIKE-pattern bracket escaping). No provider dependency.
- `DefaultValueTests.cs` -- tests `DefaultValue<T>.Value` get/set for all primitive types.
- `DisposeTests.cs` -- verifies double-dispose is safe for `DataConnection`, `DataContext`, and remote `DataContext` (both sync and async paths), with `CloseAfterUse` toggled.
- `EnumerableHelperTest.cs` -- tests `EnumerableHelper.Batch<T>()` (sync + async), including the invariant that a batch sub-sequence throws `InvalidOperationException` on second enumeration.
- `ExtensionsTest.cs` -- tests `Type.GetMemberEx(MemberInfo)` resolution for virtual and non-virtual properties on derived types. Uses `MemberHelper.PropertyOf<T>`.
- `ReservedWordTest.cs` -- tests `ReservedWords.IsReserved(word, providerName)` case-insensitivity for `""`, `AllPostgreSQL`, `AllOracle` provider names.
- `SettingsReaderTests.cs` -- **namespace anomaly**: class `TestSettingsTests` lives in `Tests.Tools` (not `Tests.Common`) despite being at path `Tests/Linq/Common/SettingsReaderTests.cs`. Tests `SettingsReader.Deserialize(config, defaultJson, userJson)` connection-merging logic with `BasedOn` inheritance chains.
- `ValueComparerTests.cs` -- tests `ValueComparer.GetDefaultValueComparer<T>(true)` null-handling for string, object, interface, and nullable value types.

**Create:**
- `CreateData.cs` -- class `a_CreateData` (no namespace). `[Order(-1)]` ensures runs first in test suite. Dispatches to per-provider SQL scripts under `Database/Create Scripts/`. Seeds `LinqDataTypes2`, `Parent`, `Child`, `GrandChild`, `InheritanceParent2`, `InheritanceChild2` via `BulkCopy`. Per-provider `DbConnection` callbacks handle binary/BFILE columns (Oracle, SQLite, Informix, Access, Firebird). Oracle callback uses `BindByNameOracleCommandInterceptor` to avoid `:NEW`/`:parameter` confusion.

**Data:**
- `DataExtensionsTests.cs` -- exercises `IDataContext.Query<T>(sql)`, `Execute<T>`, `QueryMultiple`-groupby, `DataParameter` → `DataParameter` converter chain, `CommandInfo.ClearObjectReaderCache()`. Confirms `[ScalarType(false)]` on structs enables multi-column reads.
- `MiniProfilerTests.cs` -- large fixture (~36K tokens): wraps all supported providers behind `StackExchange.Profiling.Data.ProfiledDbConnection`. Validates that provider type-mapping (`MappingSchema`) still works correctly when `DbCommand`/`DbDataReader` are wrapped. Uses `extern alias` to disambiguate `MySqlData` vs `MySqlConnector`.
- `ProcedureTests.cs` -- tests `QueryProc`, `ExecuteProc`, `QueryProcMultiple` with SQL Server stored procs including output-parameter rebind after enumeration. Confirms `[ResultSetIndex]` attribute routing.
- `QueryMultipleResultTests.cs` -- tests `QueryMultiple<T>` and `QueryProcMultiple<T>` with `[ResultSetIndex(N)]` attribute routing for `IEnumerable<T>`, `IList<T>`, scalar, and array result types. Issue #4728 regression: empty result sets on multi-result stored procs.
- `RetryPolicyTest.cs` -- tests `IRetryPolicy` contract via custom `Retry` implementation; tests `RetryPolicyBase` subclass `Issue3431RetryPolicy` (overrides `ShouldRetryOn`) and exponential-base validation (`ArgumentOutOfRangeException` for `expBase < 1.0`). Uses `SqlServerRetryPolicy` integration test.
- `TraceTests.cs` -- comprehensive `TraceInfo` / `TraceInfoStep` coverage for LINQ queries, raw SQL, DML, transactions (BeginTransaction/Commit/Rollback all emit trace steps). Tests `DataOptions.UseTracing()`, `.UseTraceLevel()`, `.UseTraceWith()` and confirms `TraceSwitch` instance vs static priority.
- `TransactionTests.cs` -- tests async transaction lifecycle (`BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`) for both `DataContext` and `DataConnection`; tests `IsolationLevel` overload; tests `AttachToExistingTransaction` for every provider via `<Provider>Tools.GetDataProvider(connection, transaction)`. Issue #3863 PostgreSQL dispose-after-commit safety.

**Exceptions:**
- `AggregationTests.cs` -- confirms `Min`/`Max`/`Average` on empty non-nullable sequences throw `InvalidOperationException`.
- `ConvertTests.cs` -- confirms `LinqToDBConvertException` on duplicate `[MapValue]` values.
- `DmlTests.cs` -- confirms `LinqToDBException` on `InsertOrUpdate` with missing PK / missing PK in insert setter.
- `ElementOperationTests.cs` -- `First` / `Single` throw `InvalidOperationException` as expected.
- `InheritanceTests.cs` -- `ParentInheritance2` without required discriminator mapping throws `LinqToDBException`.
- `JoinTests.cs` -- tests that `join … on new A() equals new B()` with mismatched keys throws `LinqToDBException`; also contains positive multi-join regression tests.
- `MappingTests.cs` -- tests `LinqToDBException` on `p.Name` usage (not a mapped column) and `LinqToDBConvertException` on enum with inconsistent `[MapValue]`.
- `StackUseTests.cs` -- tests `ExpressionVisitorBase` and `QueryElementVisitor` stack-hop safety under `ThreadHopsScope`. Verifies that deeply nested `Expression.Call` chains (30k+ nodes) trigger controlled `InsufficientExecutionStackException` (not raw stack overflow) with correct nesting depth matching `hops` count. Issue #5265: `LoadWith` chain on deeply associated entities executes within 200KB stack thread.

**Extensions:**
- `AccessTests.cs` -- `AccessHints.Query.WithOwnerAccessOption` hint propagation.
- `ClickHouseTests.cs` -- tests ClickHouse-specific JOIN modifiers (`FINAL`, `SEMI`, `ANTI`, `ANY`, `GLOBAL`, `ALL`), `SETTINGS` query hint (including `Sql.TableName` interpolation), and union interaction with hints.
- `DocExampleTests.cs` -- cross-provider doc-example test: exercises each provider's fluent hint API in a single fixture; `DatabaseSpecificTest` exercises all provider hint chains in one query.
- `MySqlTests.cs` + `MySqlTests.generated.cs` -- manual + T4-generated MySQL hint tests covering all `MySqlHints.Table.*` and `MySqlHints.Query.*` constants via parameterized `[Values]`.
- `OracleTests.cs` + `OracleTests.generated.cs` -- Oracle hint tests covering `OracleHints.Hint.*` constants; generated file covers query-level hints.
- `PostgreSQLTests.cs` + `PostgreSQLTests.generated.cs` -- PostgreSQL locking hints (`FOR UPDATE`, `FOR KEY SHARE`, etc.) and sub-query hint scoping; generated file covers `AsPostgreSQL().ForUpdateHint()` etc.
- `QueryExtensionTests.cs` -- tests self-join optimization with table hints (`SelfJoinWithDifferentHintTest`); verifies `query.GetTableSource().Joins` count.
- `QueryNameTests.cs` -- tests `QueryName("name")` emitting `/* name */` (or `/*+ QB_NAME(name) */` for Oracle) in SQL; cross-provider including Access fallback.
- `SqlCeTests.cs` + `SqlCeTests.generated.cs` -- SQL CE locking hints (`WITH (NoLock)` etc.), index hints, `TablesInScopeHint`; generated covers all `SqlCeHints.Table.*` per-method wrappers.
- `SQLiteTests.cs` -- `INDEXED BY <index>` / `NOT INDEXED` hints.
- `SqlServerTests.generated.cs` -- T4-generated SQL Server hint tests (`WithForceScan`, etc.).
- `TableIDTests.cs` -- tests `TableID("pp")` + `Sql.TableAlias`/`TableName`/`TableSpec` SQL ID resolution.

**Infrastructure:**
- `ActiveIssueGenericTests.cs` -- verifies `[ActiveIssue]` attribute variants (no details, details-only, URL+details, number-only). All tests are expected to skip in normal runs.
- `IdentifierBuilderTests.cs` -- tests `IdentifierBuilder.Add()` / `CreateID()` equality for null, bool, string, int, Delegate, lambda, `object[]`, `Type`, and `Expression.Constant`. Note: two distinct lambda captures are NOT equal (`false`) because the C# compiler generates separate method instances.
- `NullabilityContextTests.cs` -- directly constructs `SelectQuery` / `SqlTableSource` / `SqlJoinedTable` with FULL/RIGHT/INNER join types; verifies `NullabilityContext.CanBeNullSource()` propagation rules. Exercises `SqlExpressionOptimizerVisitor`, `AliasesContext`, `OptimizationContext`, `SqlSelectStatement.BuildSql`.

**Mapping:**
- `CanBeNullTests.cs` -- verifies `Configuration.UseNullableTypesMetadata` behavior: C# nullability annotations control `CanBeNull` on columns/associations when the flag is set.
- `ConversionTypeTests.cs` -- tests `MappingSchema.SetConvertExpression<T1,T2>(…, conversionType: ConversionType.FromDatabase / ToDatabase)` asymmetric conversion; verifies DB stores trimmed value and read-back strips padding.
- `DynamicStoreTests.cs` -- tests `[DynamicColumnsStore]` / fluent `DynamicColumnsStore()` on `Dictionary<string,object>` columns; configuration-scoped stores (SQLite vs default).
- `FluentDynamicMappingTests.cs` -- tests `FluentMappingBuilder.HasAttribute<T>(x => Sql.Property<int>(x, "colName"), attr)` for adding attributes to dynamic columns.
- `FluentMappingAliasTests.cs` -- tests `FluentMappingBuilder.Member(e => e.Alias).IsAlias(e => e.Real)` -- column alias mapping through `[ColumnAlias]` and fluent builder.
- `FluentMappingBuildTests.cs` -- tests `db.CreateTempTable("name", data, mb => mb.Property(…).IsPrimaryKey()…)` + `InsertOrUpdate` / `Update` on fluent-mapped temp tables.
- `FluentMappingExpressionMethodTests.cs` -- tests `FluentMappingBuilder.Member(e => e.Computed).IsExpression(e => …)` with and without materialization (`true` flag). Active issue #4987 marks several providers as skipped for the where-clause predicate simplification assertion (`TruePredicate`).
- `MappingAmbiguityTests.cs` -- tests that mixed `[Column]`/`[NotColumn]` on property+field with same name (different case) resolves correctly; verifies generated DDL column list.
- `MappingSchemaTests.cs` -- tests `MappingSchema.SetDefaultValue`, `SetConvertExpression`, converter chaining across parent/child schemas, `XmlAttributeReader` integration, and `MetadataReader`/`AttributeReader` fallback chain.
- `MapValueTests.cs` -- tests enum `[MapValue]` round-trip for string and char mapped enums via table INSERT + WHERE comparison.
- `SetUseMappingSchemaTests.cs` (file `SetUseMappingSchemaTests.cs` -- note: actual content is `DecimalOverflowTests`/`DecimalNegativeTest`): tests `SqlDecimal` precision/scale and `SetFieldReaderExpression<TReader,T>` reader override causing `OverflowException`.
- `UseMappingSchemaTests.cs` -- tests `db.UseMappingSchema(schema)` scoped override: within the `using` block column name maps to fluent-specified name; outside returns to original.

**Metadata:**
- `AttributeReaderTests.cs` -- tests `AttributeReader.GetAttributes(type)` and `GetAttributes(type, member)` for `TableAttribute` and `ColumnAttribute` lookup.
- `SystemDataLinqAttributeReaderTests.cs` -- NETFX-only. Tests `System.Data.Linq.Mapping.*` attributes (Linq2SQL) read by `SystemDataLinqAttributeReader`.
- `XmlReaderTests.cs` -- tests `XmlAttributeReader` parse, `TableAttribute` from `<Table>` element, `ColumnAttribute` from `<ColumnAttribute>` element (both short and fully-qualified names).

**OrmBattle/Helper:**
- `ExpressionUtils.cs` -- utility: `ExtractMember(Expression)` unwraps `Lambda` → `Convert` → `MemberAccess`. No tests; used by `GenericEqualityComparer`.
- `GenericEqualityComparer.cs` -- `IEqualityComparer<T>` driven by property-expression list. `HashCodeBuilder.Hash` uses prime-multiply. No tests; used by `OrmBattleTests`.

**Reflection:**
- `AttributesTests.cs` -- tests `MemberInfo.HasAttribute<T>(inherit)` for virtual property/event inheritance; tests `DynamicColumnInfo` attribute API coverage (all `CustomAttributeExtensions` overloads).
- `TypeAccessorTests.cs` -- tests `TypeAccessor.GetAccessor<T>().CreateInstance()`, member access via `MemberAccessor`, property/field read-write.

**Samples:**
- `ConcurrencyCheckTests.cs` -- illustrates intercepting `UPDATE` statements to append optimistic-concurrency `WHERE` clause by cloning `SqlStatement` and post-executing a SELECT verify. Uses `LinqToDB.Internal.SqlQuery` directly.
- `ExceptionInterceptTests.cs` -- illustrates `IRetryPolicy` wrapping exceptions with context (`SQLiteException` → custom exception) and counting retries.
- `JoinOperatorTests.cs` -- simple join pattern examples against Northwind (`Category × Product`, multi-column join).
- `JsonConvertTests.cs` -- illustrates `MappingSchema.SetConvertExpression<string,T>` using `Newtonsoft.Json.JsonConvert` for JSON-column deserialization; dynamically generates converter expressions via `Expression.Call`.

**Scaffold:**
- `SchemaProviderTests.cs` -- issue #4444: PostgreSQL `dblink` extension schema-load does not crash `LegacySchemaProvider`. `[ActiveIssue]` so skipped in normal runs.
- `TypeParserTests.cs` -- tests `IType` / `TypeParser` (scaffold code-model type) parsing; `TestType` implements `IType` for unit testing.

**SchemaProvider:**
- `PostgreSQLSchemaProviderTests.cs` -- extensive expected schema for PostgreSQL `TestTableFunctionSchema` stored function: validates `ProcedureSchema.Parameters`, `ResultTable.Columns` including data types (`int4`, `int8`, `numeric`, etc.). Provider: `NpgsqlTypes`.
- `SchemaProviderTests.cs` -- `TestApiImplemented` confirms `ISchemaProvider.GetSchema()` runs without exception for all providers. `Test` validates table-name uniqueness and column-name uniqueness across the returned schema.
- `SqlServerTests.cs` (`SchemaProvider/`) -- SQL Server 2025+ JSON/vector column schema tests: validates `jsonDataType` → `typeof(string)` (or `SqlJson` when provider-specific), `vectorDataType` → `float[]` (or `SqlVector<float>`).

**Tools:**
- `ComparerBuilderTests.cs` -- tests `ComparerBuilder.GetEqualityComparer<T>(selectors)` including inherited member resolution for virtual overrides.
- `DecimalHelperTests.cs` -- tests `DecimalHelper.GetFacets(decimal)` returning `(precision, scale)` tuple for positive/negative and trailing-zero edge cases.
- `IdentityMapTests.cs` -- tests `IdentityMap` (from `LinqToDB.Tools.EntityServices`): identical-PK queries return same object reference; `GetEntityEntries<T>()` tracks `DBCount` / `CacheCount`.
- `MapperTests.cs` -- tests `MapperBuilder<TFrom,TTo>`, `Map.GetMapper<T1,T2>()`. Cross-references `LinqToDB.Tools.Mapper.MapperBuilder` in TOOLS area. Exercises `GetMapperExpression`/`GetMapperExpressionEx`, `SetProcessCrossReferences`, deep-copy semantics.
- `ToDiagnosticStringTests.cs` -- tests `IEnumerable<T>.ToDiagnosticString()` ASCII table formatter for primitive arrays and complex types.

**TypeMapping:**
- `MappingTests.cs` -- exercises `ExpressionTypeMapper` (dynamic type proxy): delegate wrapping (`SimpleDelegate`, `ReturningDelegate` with and without type mapping), event subscription/fire through mapped wrapper. Uses `LinqToDB.Internal.Expressions.Types`.

**Update:**

- `BatchTests.cs` -- `BulkCopy` inside/outside a `DataConnection` transaction; verifies commit semantics. Namespace `Tests.xUpdate`, `[Order(10000)]`.
- `CreateTableTests.cs` -- `db.CreateTable<T>()` / `db.DropTable<T>()` round-trip with fluent mapping (PK, identity, length); async variant. Cross-provider.
- `CreateTableTypesTests.cs` -- comprehensive DDL type coverage: creates a table with `int`, `long`, `double`, `bool`, `DateTime`, enum, `string`, nullable variants, plus `DataType.Json` (`[IncludeDataSources(TestProvName.AllPostgreSQL95Plus, TestProvName.AllSqlServer2025Plus, ...)]`). Validates correct SQL type emission and round-trip for each .NET type.
- `CreateTempTableTests.cs` -- `db.CreateTempTable(name, query, tableOptions: TableOptions.CheckExistence)` API; verifies rows populate; cross-provider.
- `DeleteTests.cs` -- basic `Delete` / `DeleteAsync` / `DeleteWithOutput` API; `Where`-predicate DELETE.
- `DeleteWithOutputTests.cs` -- `DeleteWithOutput` / `DeleteWithOutputInto` across SQL Server, Firebird 5+, MariaDB, PostgreSQL, SQLite, Ydb. Feature-gated by `FeatureDeleteOutputMultiple` / `FeatureDeleteOutputSingle` / `FeatureDeleteOutputInto` constants. Tests schema-qualified target tables.
- `DropTableTests.cs` -- `table.Drop()` / `DropTable<T>(throwExceptionIfNotExists:)` cross-provider; `[Order(10000)]`.
- `DynamicColumnsTests.cs` -- Insert/Update/Delete using `Sql.Property<T>(entity, colName)` for dynamic column names. Validates that non-constant column name strings are handled correctly.
- `InsertIntoTests.cs` -- `query.Into(destTable).Insert()` (SELECT INTO) for SQLite and ClickHouse; tests identity-column mapping when source selects with offset `Id + 1`.
- `InsertWithOutputTests.cs` -- `InsertWithOutput` / `InsertWithOutputInto` across SQL Server, Firebird, MariaDB, PostgreSQL, SQLite, Ydb. Feature-gated by `FeatureInsertOutputSingle` / `FeatureInsertOutputMultiple` / `FeatureInsertOutputInto` constants. Tests schema-qualified targets.
- `MergeTests family` (18 files, partial class `Tests.xUpdate.MergeTests`):
  - `MergeTests.Issues.cs` -- root file; defines `[TestFixture]`, test-model types (`TestMapping1/2`, `AllTypes2`), helpers (`GetTarget`, `GetSource1/2`, `PrepareData`), and issue-regression tests.
  - `MergeTests.ApiParametersValidation.cs` -- null-argument guard tests for all `LinqExtensions.Merge` overloads; exercises async cancellation path.
  - `MergeTests.Caching.cs` -- validates that enumerable-source merge queries hit the query cache on repeated calls (uses `ClearCache()` / `GetCacheMissCount()`).
  - `MergeTests.CommandValidation.cs` -- verifies providers that do not support MERGE (`MySQL`, `SQLite`, `ClickHouse`, etc.) throw `LinqToDBException` instead of silent no-op.
  - `MergeTests.DynamicColumns.cs` -- MERGE with `[DynamicColumnsStore]` source-side property reading (target dynamic setters not yet supported).
  - `MergeTests.EmptySource.cs` -- MERGE with zero-row enumerable source; verifies no SQL error and correct 0-rows-affected result.
  - `MergeTests.Hints.cs` -- SQL Server MERGE with table hints (`WITH (UPDLOCK)`, etc.).
  - `MergeTests.IQueryableSource.cs` -- MERGE targeting an `IQueryable` (CTE or subquery as target); `[ActiveIssue(2363)]` pending.
  - `MergeTests.OldApiMigratedTests.cs` -- regressions converted from the legacy `MergeInto` API to the current fluent API. Marked `[Obsolete]` indirectly via class.
  - `MergeTests.TargetSourceOn.cs` -- tests `On(target, source, condition)` and `OnTargetKey()` match-condition builder methods; `TableWithoutKey` entity validates key-required error.
  - `MergeTests.Types.cs` -- MERGE with all numeric/date/string types in source rows (`MergeTypes` entity); validates type mapping through the MERGE path.
  - `MergeTests.WithOutput.cs` -- `MergeWithOutput` / `MergeWithOutputInto` across SQL Server 2008+, PostgreSQL 17/18+, Firebird 3+. Feature constants: `SIMPLE_OUTPUT`, `OUTPUT_WITH_ACTION`, `OUTPUT_WITH_HISTORY`, `OUTPUT_WITH_ACTION_AND_HISTORY`, `OUTPUT_INTO_WITH_ACTION_AND_HISTORY`.
  - `MergeTests.Operations.Associations.cs` -- MERGE with associations on target/source; tests `[MergeNotMatchedBySourceDataContextSource]`.
  - `MergeTests.Operations.Combined.cs` -- multi-operation MERGE (InsertWhenNotMatched + UpdateWhenMatched in single statement).
  - `MergeTests.Operations.Delete.cs` -- `DeleteWhenMatched` operation; provider restrictions (Oracle, Sybase, SapHana, Firebird excluded).
  - `MergeTests.Operations.DeleteBySource.cs` -- `DeleteWhenNotMatchedBySource` (SQL Server / Sybase extension); uses `[MergeNotMatchedBySourceDataContextSource]`.
  - `MergeTests.Operations.IdentityInsert.cs` -- `InsertWhenNotMatched` with identity column insert (`[IdentityInsertMergeDataContextSource]`).
  - `MergeTests.Operations.Insert.cs` -- `InsertWhenNotMatched` variants (table source, CTE source, enumerable source, async); accesses `LinqToDB.Internal.SqlQuery` for SQL shape assertion.
  - `MergeTests.Operations.LoadTests.cs` -- large-batch MERGE performance/correctness (configurable batch sizes per provider, e.g. 500 for Sybase).
  - `MergeTests.Operations.Parameters.cs` -- MERGE with parameterized source values; provider restrictions (Oracle, Sybase, Informix, SapHana, Firebird 2.5 excluded for some sub-tests).
  - `MergeTests.Operations.Update.cs` -- `UpdateWhenMatched` and `UpdateWhenNotMatched` variants.
  - `MergeTests.Operations.UpdateBySource.cs` -- `UpdateWhenNotMatchedBySource` (SQL Server / Sybase extension).
  - `MergeTests.Operations.UpdateWithDelete.cs` -- Oracle-only `UPDATE … DELETE` clause within MERGE.
- `MultiInsertTests.cs` -- Oracle `MULTI-TABLE INSERT` (unconditional + conditional `FIRST`/`ALL`). `db.SelectQuery(...).MultiInsert().Into(dest1, ...).Into(dest2, ...).Insert()` API. Provider: `TestProvName.AllOracle`.
- `OldMergeTests.cs` -- tests for the deprecated `MergeInto` API; `[Obsolete]` on class. Will be removed with the API.
- `TempTableTests.cs` -- `db.CreateLocalTable<T>(seed)` + insert-from-query across providers (excludes ClickHouse); verifies `Date` column is null when not set.
- `TruncateTableTests.cs` -- `db.TruncateTable<T>()` cross-provider; verifies row count drops to 0 after truncate.
- `UpdateFromTests.Row.cs` -- partial of `UpdateFromTests`; tests `UPDATE … SET (col1, col2) = (SELECT …)` row-constructor syntax for SQLite, Oracle, PostgreSQL, Informix, Firebird 5+. Uses `LinqToDB.Internal.Common`.
- `UpdateTests.cs` -- comprehensive `Update` / `UpdateAsync` / `Set(…)` / `InsertOrUpdate` / `UpdateFrom` API coverage; cross-provider.
- `UpdateWithOutputTests.cs` -- `UpdateWithOutput` / `UpdateWithOutputInto` across SQL Server, Firebird, PostgreSQL 18+, SQLite, Ydb. Feature constants distinguish `WithOld`/`WithoutOld` × `Single`/`Multiple` × `NoAlternateRewrite` variants.

**DataProvider:**

- `AccessProceduresTests.cs` -- Access (OleDb + ODBC) stored procedure tests: `Person_SelectByKey`, `Person_SelectAll`, `Person_SelectByName`, `Person_SelectListByName`, `Person_Delete`, `Person_Update`, `Person_Insert`, `Scalar_DataReader`, `Patient_SelectAll/ByName`. Validates `ProcedureSchema` column metadata (OleDb vs ODBC schema differences). Uses `ExecuteProc` / `QueryProc` with OleDb/ODBC call syntax differences.
- `AccessTests.cs` -- Access (OleDb + ODBC) type mapping: all numeric, datetime, char, string, binary, GUID, XML, enum types. Tests OleDb `CVar()` wrapping for `DISTINCT` with nullable parameter (issue #3893 for special-character column names, `[ActiveIssue]`). Tests `AccessTools.CreateDatabase` / `DropDatabase` (Jet and ACE versions). Zero-date roundtrip (1899-12-29 through 1900-01-01). ODBC parameter wrapping (`CVar` injection for `DISTINCT ?`).
- `DB2Tests.cs` -- IBM DB2 type mapping including provider-specific types (`DB2Int64`, `DB2Real`, `DB2TimeStamp`, `DB2Clob`, `DB2Blob`, `DB2DateTime`, `DB2DecimalFloat`, `DB2Binary`). High-precision timestamp (0--12 fractional digits via `DB2TimeStamp`). Module/package function calls (`TEST_MODULE1.TEST_FUNCTION`, `TEST_TABLE_FUNCTION`). Issue #2091: parameter usage verified via `db.LastQuery`. Issue #2763: SYSCAT.COLUMNS trailing-space schema padding bug.
- `ExpressionTests.cs` -- minimal: exercises `IDataProvider.GetReaderExpression(reader, ordinal, drExpr, typeof(int))` to build a compiled reader delegate. Validates that the expression compiles and returns correct column value (SQLite returns `Int64` vs `Int32` edge case).
- `InformixTests.cs` -- Informix type mapping (bigint, int8, int, decimal, money, real, float, bool, char, varchar, nchar, nvarchar, lvarchar, text, date, datetime, interval, byte). BulkCopy (`MultipleRows` / `ProviderSpecific`) with `KeepIdentity` for IDS provider. NETFX: `IfxDateTime`, `IfxDecimal`, `IfxTimeSpan` provider-specific types. `CreateAllTypes` validates DDL emission.
- `MySqlTestUtils.cs` -- utility: `EnableNativeBulk(db, context)` sets `SET GLOBAL local_infile=ON` for MySqlConnector. Not a test fixture.
- `PostgreSQLArrayTests.cs` -- PostgreSQL array-parameter caching: `Sql.Ext.PostgreSQL().ValueIsEqualToAny(col, arr)` query parameterization with arrays of `int`, `long`, `double`, `decimal`, `string`, `bool`, `short`, `float`, `Guid`, `DateTime`. Verifies `GetCacheMissCount()` does not increase on re-execution with same-shape array.
- `PostgreSQLExtensionsTests.cs` -- PostgreSQL `Unnest(array)` / `db.Unnest(col)` table-valued function, `PostgreSQLExtensions.ValueIsEqualToAny`, array-column queries. Uses `Shouldly` for assertions. Provider: `TestProvName.AllPostgreSQL95Plus`.
- `SqlCeTests.cs` -- SQL CE type mapping (bigint, numeric, bit, smallint, decimal, int, tinyint, money, float, real, datetime, nchar, nvarchar, ntext, binary, varbinary, image, uniqueidentifier). BulkCopy. `SqlCeTools.CreateDatabase` / `DropDatabase`.
- `SQLiteParameterTests.cs` -- SQLite: `DateTime` stored as `Int64` via custom `MappingSchema` converter (`long ↔ DateTime` via ticks). `double`/`float` parameter pass-through; `float.MaxValue` round-trip. Validates that inline parameters work correctly for these custom mappings.
- `SqlServerFunctionsTests.cs` -- SQL Server `SqlFn.*` system functions: `DbTS`, `LangID`, `Language`, `LockTimeout`, `MaxConnections`, `NestLevel`, `Options`, `RemServer`, `ServerName`, `ServiceName`, `Spid`, `TextSize`, `Version`, and numerous date/string/math functions. Uses `SystemDB` context and `LinqToDB.Tools.DataProvider.SqlServer.Schemas`.
- `SqlServerTestUtils.cs` -- utility (non-fixture): provides `TVPRecord` class and `GetSqlDataRecordsMS()` / `GetSqlDataRecords()` helper enumerables for Table-Valued Parameter (TVP) tests; referenced by `SqlServerTypesTests.TVP.cs`.
- `SqlServerTypesTests.cs` (root partial) -- SQL Server spatial and temporal types: `SqlHierarchyId`, `SqlGeography`, `SqlGeometry`; `DateTimeOffset`, `DateTime2`, `TimeSpan`. Uses `Microsoft.SqlServer.Types`. Spatial tests skip for `Microsoft.Data.SqlClient` (non-NETFX limitation).
- `SqlServerTypesTests.TVP.cs` (TVP partial) -- Table-Valued Parameter tests via `DataTable`, `IEnumerable<SqlDataRecord>`, and `IEnumerable<SqlDataRecordMS>` factories. Tests `[dbo].[TestTableType]` TVP passing to stored procedures.
- `SqlServerVectorTypeTests.cs` -- SQL Server 2025 `VECTOR` type via `SqlVector<float>` and `float[]` column mappings. Tests `SqlFn.VectorDistance(metric, v1, v2)` with `Cosine`, `Euclidean`, `Dot` metrics. Async (`ValueTask`). Requires `SqlServerProviderAdapter` mapping schema for `SqlVector<float>`. Provider: `TestProvName.AllSqlServer2025PlusMS` / `AllSqlServer2025Plus`.
- `UniqueParametersNormalizerTests.cs` -- unit tests for `UniqueParametersNormalizer` (internal `LinqToDB.Internal.DataProvider` + `LinqToDB.Internal.Infrastructure` + `LinqToDB.Internal.SqlProvider` types). Tests unique-string pass-through, duplicate renaming (`test` → `test_1` → `TEST_2`), and case-insensitive dedup. Also tests integration with a mock `IDataProvider`/`ISqlBuilder` that generates parameterized SQL.
- `YdbTests.cs` -- Yandex DB (YDB) provider tests: schema introspection (`GetSchemaProvider().GetSchema()`), DML (insert/update/delete), `SelectQuery` / `QueryProc`, `BulkCopy`. Uses `YdbDataProvider` internal class. Many tests tagged `[YdbNotImplementedYet]`. Covers `LinqToDB.SqlQuery` imports confirming YDB-specific SQL shape awareness.

**DataProvider/Types:**

- `TypeTestsBase.cs` -- abstract base for per-vendor type tests. Provides `TestType<TType,TNullableType>(context, DbDataType, value, nullableValue, ...)` which exercises: CreateTable DDL, nullable/non-nullable insert, parameter queries, inline literal queries, all BulkCopy modes, filter-by-value SELECT. `TestParameters` virtual property lets subclasses disable parameter testing (ClickHouse overrides to `false`). `MakeListFilter` virtual for providers needing non-standard filter predicates (YDB uses `ListHas`).
- `ClickHouseTypeTests.cs` -- ClickHouse type coverage via `TypeTestsBase`. Notes unsupported types: `LowCardinality`, `AggregateFunction`, `Nested`, `Tuple`, `Map`, `Array`, `Interval`. Parameters disabled (`TestParameters = false`). Provider: `TestProvName.AllClickHouse`.
- `DuckDBTypeTests.cs` -- **(new, PR #5451)** DuckDB type coverage via `TypeTestsBase`. Covers Boolean, all integer widths (TINYINT--UBIGINT--HUGEINT--BIGNUM), FLOAT/DOUBLE, DECIMAL (precision/scale sweep), VARCHAR/BLOB/BITSTRING, UUID, DATE/TIME/TIMETZ/INTERVAL/TIMESTAMP/TIMESTAMPTZ/JSON. Known provider bugs documented inline. `#if SUPPORTS_DATEONLY`.
- `MySqlTypeTests.cs` -- MySQL/MariaDB `VECTOR` type (`DataType.Vector32`, `float[]`). MySQL 9+/MariaDB vector: tests with/without explicit length, notes MySqlConnector `ProviderSpecific` BulkCopy limitation (issue #1604).
- `PostgreSQLTypeTests.cs` -- PostgreSQL JSON / JSONB types via `DataType.Json` / `DataType.BinaryJson`. Covers `string`, `JsonDocument` (MDS only). Notes: no default `DataType` mapping for JSON/JSONB because JSON vs JSONB preference is ambiguous. `doubleValue` normalization by server for JSONB.
- `SapHanaTypeTests.cs` -- SAP HANA `SmallDecFloat` (16-digit precision) and `Decimal` types via `TypeTestsBase`. `HanaDecimal` provider-specific type tested for native provider; ProviderSpecific BulkCopy excluded for `HanaDecimal` (known limitation v2.23).
- `SqlServerTypeTests.cs` -- SQL Server 2025+ `JSON` type via `DataType.Json`. Covers `string`, `JsonDocument` (MDS v6+), `SqlJson` (MDS v6+). Notes: comparisons not supported on JSON type; SQLMI returns wrong type; BulkCopy disabled for SQLMI.
- `YdbTypeTests.cs` -- YDB primitive types (`bool`, numerics, dates, strings, binary) via `TypeTestsBase`. Custom `MakeListFilter` using `ListHas({1}, {0})` YDB SQL expression. Documents YDB type reference. Provider: `ProviderName.Ydb`.

**Linq:**

- `AbstractionTests.cs` -- multi-class interface queries; `ISample` abstraction over two concrete types with `Association`; exercises eager-load through interface-typed associations.
- `AggregationTests.cs` -- aggregation over associations: `Sum`/`Count`/`Average` on `IQueryable<ItemValue>` association with null-value data (non-parseable + null). Tests `Sql.Ext.Average` and `Sql.AggregateFunction` extension builder path.
- `AK107Tests.cs` -- Oracle-only: sequence-backed identity insert (`[SequenceName]`) with cross-schema sequence (`c##sequence_schema`). Tests `SkipOnUpdate` in `InsertOrUpdate` path.
- `AllAnyTests.cs` -- `Any`/`All` LINQ operators: subquery `Any`, navigation `Any`, correlated `All`, combined predicates. `[YdbMemberNotFound]` and ClickHouse excluded where applicable.
- `ArrayTests.cs` -- `[ActiveIssue]` fixture: `CreateLocalTable<ArrayTable>` with array-typed columns (`int[]`, `Gender[]`, `SimpleEnum[]`); validates schema introspection returns `typeof(int[])`. All tests skipped in normal runs.
- `AsyncTests.cs` -- validates `ToArrayAsync` / `ForEachAsync` via remote context (`LinqServiceSuffix`); skips if `TestConfiguration.DisableRemoteContext`.
- `BatchTests.cs` (Linq/) -- NETFX-only (`#if NETFRAMEWORK1`): `db.BeginBatch()` / `CommitBatch()` API on `TestServiceModelDataContext`. Tests CreateTableTemporarily + Insert + Delete + Update + Drop within a batch. Effectively dead code on non-NETFX builds.
- `BooleanTests.cs` -- boolean/numeric coercion: `bool`, `bool?`, `int`, `int?`, `decimal`, `decimal?`, `double`, `double?` combinations in WHERE clauses; exercises `CompareNulls.LikeClr` vs `LikeSql` semantics.
- `CachingTests.cs` -- query-cache discipline: `[SqlQueryDependent]` parameter on `[Sql.Extension]` forces separate cache entries per `funcName`/`fieldName` combination; validates `GetCacheMissCount()`. Accesses `LinqToDB.Internal.SqlQuery`.
- `CalculatedColumnTests.cs` -- `[ExpressionMethod(IsColumn = true)]` computed columns in SELECT and WHERE; correlated-subquery computed column (`DoctorCount`). Uses `ComparerBuilder.GetEqualityComparer`.
- `CharTypesTests.cs` -- cross-provider `char`/`nchar`/`string` column mappings for DB2, SqlCe, PostgreSQL, MySQL, Firebird (per-provider `[Column]` overrides). Tests trailing-space trimming and Unicode edge cases.
- `ColumnAliasTests.cs` -- `[ColumnAlias("RealCol")]` on property: validates that alias resolves to the real column in `Count`, `Update`, and projection contexts.
- `CommonTests.cs` -- miscellaneous cross-operator tests: `AsQueryable` on `IEnumerable<Child>`, nested projection with `FirstOrDefault`, `DateTimeKind` round-trip, `List<T>` in-query parameter, expression method with `[Nullable]` parameter, `GetTable<T>` vs typed table property.
- `CompareWithNullTests.cs` -- null-equality semantics: `x.A == x.B` with nullable columns under `CompareNulls.LikeClr` (C# null-equal-null) vs `LikeSql` (SQL IS NULL). Uses `FluentMappingBuilder` with custom `HasConversion` for char-enum (`CE`) columns.
- `CompileTests.cs` -- `CompiledQuery.Compile(...)` with parameter substitution; tests `IEntityService` identity tracking through compiled queries; `EntityServiceInterceptor` callback counting.
- `CompileTestsAsync.cs` -- async variants of `CompiledQuery.Compile`; `LoadWithAsTable`/`LoadWithAsTreeTable` through compiled async queries; `IEntityService` integration.
- `ComplexTests.cs` -- complex join + grouping + DefaultIfEmpty patterns; `Contains` with subquery; `JsonDocument` column filters; `System.Runtime.InteropServices.RuntimeInformation` used for platform-guarded tests.
- `ComplexTests2.cs` -- complex property mapping, inheritance mapping, `LoadWith` for inheritance, `LoadWith` with casts, nested `LoadWith`, string enums, converter-backed enums. Exercises `AnimalType`, `AnimalType2` discriminator enums.
- `ConcatUnionTests.cs` -- `Concat`/`Union`/`UnionAll`/`Except`/`Intersect` between typed queries; async `Concat1Async`; union of anonymous types; union with projection casting.
- `ConcurrencyTests.cs` -- `OptimisticLockPropertyAttribute` with `VersionBehavior.AutoIncrement` and `Guid.NewGuid()` custom strategy via `OptimisticLockPropertyBaseAttribute`. Tests `context.SupportsRowcount()` guard.
- `ConditionalTests.cs` -- conditional projection: `condition ? null : new T { … }` mapped to CASE WHEN in SELECT; exercises `LinqToDB.Internal.Common` for provider-gated tests. SQLite-only.
- `ConflictActionTests.cs` -- **(updated, PR #5455)** `BulkCopyOptions { ConflictAction = ConflictAction.Ignore }` with `MultipleRows` mode for MySQL/PostgreSQL/SQLite/DuckDB; validates that conflicting PK rows are skipped and non-conflicting rows are inserted. Sync and async variants.
- `ConstantTests.cs` -- expression evaluator: `static readonly` fields, non-readonly fields, `init`-only properties, nested classes/structs in LINQ WHERE; validates that `Guid`, `string`, `InnerClass.Id`, `InnerReadonlyStruct.GetId(n)` are correctly parameterized vs inlined.
- `ConstructorTests.cs` -- entity constructor selection: abstract base + public/private/protected/parameterized constructors; verifies ORM instantiation picks the correct no-arg constructor path.
- `ContainsTests.cs` -- `Contains` with `CompareNulls.LikeClr` vs `LikeSql`; converted enum in `Contains` (`HasConversion` + `DataType.VarChar`); `null`-value `Contains` semantics.
- `ConvertExpressionTests.cs` -- `let children = p.Children.Where(…)` correlated subquery expansion via `[ThrowsRequiresCorrelatedSubquery]`; chained `let` subqueries with `Sum`.
- `ConvertTests.cs` -- `Sql.ConvertTo<int>.From(decimal)`, `Sql.Convert<int,decimal>`, `Sql.Cast`; `JsonDocument`-to-string conversion; `Guid.ToString()` in query; `Convert.ToDateTime` on string column.
- `CountByMethodTests.cs` -- NET9+ `CountBy(keySelector)` operator: subquery and final forms. Uses `AssertQuery` helper.
- `CountTests.cs` -- `Count()`, `CountAsync()`, `Count(predicate)`, `LongCount()`, correlated-subquery count in select, `Count` with `DefaultIfEmpty`.
- `CteMaterializedTests.cs` -- `AsCte(builder)` builder API: `HasName(null/empty)` leaves auto-generated name (`CTE_\d+`); null-callback guard; `ICteBuilder` interface coverage via `LinqToDB.Internal.Linq.Builder`.
- `CursorPagination.cs` -- utility `Paginator` class (no test fixture): demonstrates cursor-based pagination via `ROW_NUMBER()` CTE + `PageResult<T,TCursor>`. Uses `LinqToDB.Internal.Expressions`, `LinqToDB.Internal.Reflection`, `LinqToDB.Expressions`. Not compiled as a test class.
- `DataContextExtensionsTests.cs` -- `DataContextExtensions` API: `Query<T>`, `Execute`, `ExecuteAsync`, `CommandInfo` with `DataConnection` and `DataContext` (KeepConnectionAlive on/off). `CountOpenInterceptor` counts sync/async open events. Comment: "SetCommand coverage provided implicitly through other DataContextExtensions calls."
- `DataContextTests.cs` -- `DataConnection.DefaultConfiguration` / `DefaultSettings` reset scope; `DataConnection` construction from `MySqlConnection`; `SQLiteDataProvider` direct construction; `ILinqToDBSettings` interface contract.
- `DataServiceTests.cs` -- NETFX-only: `DataService<NorthwindDB>` + `IDataServiceMetadataProvider` service resolution. Validates WCF Data Services integration.
- `DataTypesTests.cs` -- type round-trip for `Guid`, `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `bool`, `DateTime` via `TypeTable<TType>`. Uses `TestType<TTable,TType>` helper from `DataContextExtensionsTests`. TODO note: to be replaced by per-provider type tests similar to ClickHouse.
- `DateOnlyFunctionTests.cs` -- `#if SUPPORTS_DATEONLY`: `DateOnly` column in `Transactions` table; `DateOnly.Parse`, `AddYears`/`AddMonths`/`AddDays`, `Year`/`Month`/`Day` properties in WHERE/SELECT. Uses `TestData.DateOnly` and `TestData.DateOnlyAmbiguous` test values.
- `DateTimeFunctionsTests.cs` -- `DateTime` functions: `AddYears`/`AddMonths`/`AddDays`/`AddHours`/`AddMinutes`/`AddSeconds`/`AddMilliseconds`, `Year`/`Month`/`Day`/`Hour`/`Minute`/`Second`/`Millisecond`, `DayOfYear`/`DayOfWeek`, `Sql.DateAdd`, `Sql.DateDiff`, `Sql.TruncDate`. Custom `CustomIntComparer` allows ±1ms precision. **Updated (PR #5451/5467/5517)**: DuckDB added to provider set; Now-translation assertions and DbType preservation behavior updated.
- `DateTimeOffsetTests.cs` -- `DateTimeOffset` column mapping: `AddYears` through `AddMilliseconds`, `ToLocalTime`/`ToUniversalTime`, offset-specific properties. Nepal timezone used for ambiguous-offset tests (`Asia/Kathmandu` / `Nepal Standard Time`). **Updated (PR #5451/5467)**: DuckDB added to provider set.
- `DecompositionTests.cs` -- nested object decomposition in SELECT: `ItemInfo { Top = ..., Bottom = ... }` with nested `StyleInfo`; validates multi-level anonymous-to-named-type projection.
- `DefaultIfEmptyTests.cs` -- `DefaultIfEmpty()` with/without explicit default, in subquery position; `ThrowsForProvider` on Sybase (invalid derived-table-with-Take).
- `DistinctByMethodTests.cs` -- NET5+ `DistinctBy(keySelector)` operator; `ThrowsCannotBeConverted` for Access/SqlCe/Sybase/MySQL5.7/Firebird<3.
- `DistinctTests.cs` -- `Distinct()` on scalar, nullable, anonymous, and full-entity projections; `Distinct` + `OrderBy`; `Distinct` after `Select`.
- `DynamicColumnsTests.cs` (Linq/) -- `Sql.Property<T>(entity, columnName)` for non-dynamic and dynamic columns in WHERE/SELECT; navigational column access (`x.Patient.Diagnosis`); `[DynamicColumnsStore]` insert/update/delete; JSON dynamic columns; `Firebird`-specific BinaryJSON. Accesses `LinqToDB.Internal.Mapping`, `LinqToDB.Metadata`.
- `DynamicResultTests.cs` -- `db.Query<dynamic>(sql, params)` with `dynamic` projection; `ExpandoObject`; validates field access via `((dynamic)row).FieldName`. Namespace `Tests` (not `Tests.Linq`).
- `DynamicWindowFunctionsTests.cs` -- `DynamicWindowFunctionsExtensions`: builds `ROW_NUMBER() OVER (ORDER BY …)` programmatically using `Expression` trees + `MethodInfo` reflection. Exercises `LinqToDB.Internal.Common`, `LinqToDB.Internal.Expressions`, `LinqToDB.Expressions`.
- `ElementOperationTests.cs` -- `First`/`FirstOrDefault`/`Single`/`SingleOrDefault`/`Last`/`LastOrDefault` with/without predicates; async variants; correlated-subquery variants.
- `EntityCreatedTests.cs` -- `EntityServiceInterceptor.EntityCreated` callback counting; `CheckEntityIdentity` dedup-by-PK path; `TableOptions.NotSet` and `TableName` in `EntityCreatedEventData`. Tests `LoadWith` interaction.
- `EnumerableInQuery.cs` -- `db.SelectQuery(() => new T { … })` + `Concat` to build dynamic union from a list; validates correct column order across differently-ordered projections.
- `EnumerableSourceTests.cs` -- APPLY-join with local array (`from n in new[] { p.FirstName, p.LastName, "John", doe }`); `GetCacheMissCount()` stays stable across iterations with captured variable.
- `EnumerableSourceTests.AsQueryable.cs` -- **(new, PR #5495)** configured `AsQueryable(db, builder)` overload tests covering parameterize/inline modes, `.Except(member)` per-member flip, cache stability, scalar lists, inline arrays, error cases, `CompiledQuery` integration. See Delta section for full breakdown.
- `EnumMappingTests.cs` -- `[MapValue(ProviderName.Access, N)]` provider-specific enum mapping; `UndefinedEnum` (no default value); `TestEnum21` redundant mapping; `ConvertBuilder` expression for enum to long.
- `EvaluationTests.cs` -- `LinqToDB.Linq.Expressions.MapMember` for `TimeSpan` remapping to `Int64`; boolean expression evaluation with nullable `Guid` short-circuit.
- `ExceptByMethodTests.cs` -- NET6+ `ExceptBy(source, keySelector)` operator; `ThrowsCannotBeConverted` guards (issue #4412).
- `ExpandTests.cs` -- `Expression<Func<T,bool>>` inline predicate via `predicate.Compile()(t)` in LINQ; tests that compiled-expression invocation is correctly hoisted to server-side SQL.
- `ExplicitInterfaceTests.cs` -- explicit interface property mapping (`IDate.Date` via `Storage = "_date"`); both `get`-only and `get; set;` interface contracts. `LinqDataTypes` table.
- `ExpressionsTests.cs` -- `Expressions.MapBinary` for `<<`/`>>` operators; `Expressions.MapMember` for `Enum.HasFlag`; `[Sql.Extension]` with provider-specific overrides (ClickHouse `bitShiftLeft`); `LinqToDB.SqlQuery.Precedence` usage; `ToSqlQuery()` for SQL text inspection. **Updated (PR #5503)**: `Enum.HasFlag` translation assertions updated/extended.
- `ExpressionTests.cs` -- `[Sql.Expression("DATE()", ServerSideOnly = true)]` with `DataConnection` + fake-type parameters; `[Sql.Function("DATE", ArgIndices = ...)]`; validates argument-index-out-of-range throws.
- `ExtensionTests.cs` -- `ITable<T>.TableName()`, `.DatabaseName()`, `.SchemaName()` fluent overrides; all three combined.
- `FromSqlTests.cs` -- `db.FromSql<T>(FormattableString)` raw SQL source in LINQ queries; JOIN with `FromSql`; association through `FromSql` table; `ToSqlQuery()` SQL shape inspection; accesses `LinqToDB.Internal.Linq`, `LinqToDB.Internal.SqlQuery`.
- `FSharpTests.cs` -- delegates to `FSharp.WhereTest.*` (separate F# assembly): `LoadSingle`, `RecordParametersMapping`, `RecordProjectionColumnsOnly` (confirms `WHERE` clause in `LastQuery`), `RecordComplexProjection`. NETFX uses `[ActiveIssue]` for FSharp.Core v9 incompatibility.
- `FullTextTests.MySql.cs` -- `[Category(TestCategory.FTS)]` partial; MySQL `MATCH … AGAINST` via `Sql.Ext.MySql().Match(term, col1, col2)`. Tests `MatchPredicate`, `MatchPredicateOneColumn`, `MatchInNaturalLanguageMode`, `MatchWithExpansion`, `MatchScore`.
- `FullTextTests.SQLite.cs` -- `[Category(TestCategory.FTS)]` partial; SQLite FTS3/FTS4/FTS5 `MATCH` via `Sql.Ext.SQLite().MatchBy(table)` / `MatchByColumn(col)`; async variants. TODO note: FTS5 not executed against DB (missing provider support).
- `FunctionTests.cs` -- `Contains` with inline array and captured variable; `Sql.Like`, `Sql.Between`, `Sql.In`; `new[] { }.Contains(col)` empty-array edge case; accesses `LinqToDB.Internal.SqlQuery` for SQL shape assertions.
- `GenerateExpressionTests.cs` -- `DataOptions.UseGenerateExpressionTest(true)` mode; `result.GenerateTestString()` output written to `TestContext.Out`.
- `GenerateTests.cs` -- `Expression.GetBody(parameters)` extension to substitute parameters across lambdas; builds combined predicate `a && b` then executes via `db.Person.Where(predicate)`.
- `GenericExtensionsTests.cs` -- `ExtensionChoiceAttribute` + `GenericBuilder : Sql.IExtensionCallBuilder` for type-dispatched SQL extension selection; `IdentifierBuilder.GetObjectID` usage in custom `MappingAttribute`.
- `GroupByExtensionsTests.cs` -- `Sql.GroupBy.Rollup(…)`, `Cube(…)`, `GroupingSets(…)` operators for SQL Server 2008+, PostgreSQL 9.5+, Oracle, SAP HANA, MySQL, ClickHouse. Validates grouping-set SQL emission.
- `GuidTests.cs` -- `Guid.ToString()` in SELECT and WHERE; `Guid.ToString().Contains(subStr)`, `.StartsWith(startStr)`, `.EndsWith(endStr)`.
- `IdentifierTests.cs` -- parameter-name generation for special characters (`-`, non-ASCII `И`, keywords like `from`, `_`, leading digit `1p`); nested column parameter naming; long identifier truncation (60-char → ≤50-char parameter name).
- `IdlTests.cs` + `IdlTest.Additional.cs` -- generic query patterns over `ITestDataContext` interface; `CompiledQuery`; `GenericConcatQuery1` cross-entity concat; `[IdlProviders]` custom attribute scoping to MySQL/SQLite/ClickHouse/SqlServer/Access.
- `IndexMethodTests.cs` -- NET9+ `Index()` operator; `ThrowsCannotBeConverted` for Access/SqlCe/Sybase/MySQL5.7/Firebird<3.
- `InSubqueryTests.cs` -- `IN (subquery)` vs `EXISTS` via `DataOptions.UseCompareNulls(…)` / `PreferExists`; `[ExpressionMethod]` helper `UseInQuery<T>` for nullable-aware SQL emission.
- `InterfaceTests.cs` -- query against `IParent2` (interface-mapped table); issue #4031: `[Table("Person")]` on base + implicit/explicit interface implementations; `db.GetTable<T>()` via `IType.GetRuntimeMethods()` reflection.
- `InternalsTests.cs` -- `Internals.GetDataContext(query)` for `ITable`, `IQueryable`, `Set(…)`, `Value(…)`, `Into(…).Value(…)` -- validates internal API surface. `Internals.CreateQuery(...)` from `IDataContext`.
- `IntersectByMethodTests.cs` -- NET6+ `IntersectBy(source, keySelector)` operator; `ThrowsCannotBeConverted` guards (issue #4412).
- `IsDistinctFromTests.cs` -- `IsDistinctFrom(value)` / `IsNotDistinctFrom(value)` extension methods for `int`, `int?`, `string`, `string?`; validates NULL-safe equality semantics across providers.

**UserTests (issues 10-1838 -- batch 5):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 1869-2832 -- batch 6):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 2856-4654 -- batch 7):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 475-5458 + free-form -- batch 8):**

[Same as prior run -- no structural changes to this batch. Note: `Issue269Tests.cs`, `Issue1238Tests.cs`, `Issue3432Tests.cs`, `Issue356Tests.cs`, `Issue445Tests.cs`, `Issue792Tests.cs`, `LetTests.cs` received minor updates (likely DuckDB provider additions to `[DataSources]` sets or assertion refinements matching the PR #5451 provider expansion). No new test-class structures were introduced in these files.]

## Cross-area validation map

| Production area | Primary test subdirs |
|---|---|
| EXPR-TRANS | `Linq/` (all), `Exceptions/`, `UserTests/` (query shape regressions), `OrmBattle/`, `ThirdParty/` |
| SQL-PROVIDER | `Linq/` (SQL generation), `Extensions/`, `Update/` |
| SQL-AST | `AST/`, `Linq/InternalsTests.cs`, `Exceptions/CommonTests.cs`, `Exceptions/StackUseTests.cs`, `Infrastructure/NullabilityContextTests.cs` |
| PROV-SQLSERVER | `DataProvider/SqlServerTests.cs`, `DataProvider/SqlServerTypesTests.cs`, `DataProvider/SqlServerVectorTypeTests.cs`, `DataProvider/Types/SqlServerTypeTests.cs`, `Extensions/SqlServerTests.cs`, `SchemaProvider/SqlServerTests.cs` |
| PROV-ORACLE | `DataProvider/OracleTests.cs`, `Extensions/OracleTests.cs`, `TypeMapping/OracleWrappingTests.cs`, `Update/MultiInsertTests.cs` |
| PROV-POSTGRES | `DataProvider/PostgreSQLTests.cs`, `DataProvider/PostgreSQLArrayTests.cs`, `DataProvider/PostgreSQLExtensionsTests.cs`, `DataProvider/Types/PostgreSQLTypeTests.cs`, `Extensions/PostgreSQLTests.cs`, `SchemaProvider/PostgreSQLSchemaProviderTests.cs` |
| PROV-MYSQL | `DataProvider/MySqlTests.cs`, `DataProvider/Types/MySqlTypeTests.cs`, `Extensions/MySqlTests.cs` |
| PROV-SQLITE | `DataProvider/SQLiteTests.cs`, `DataProvider/SQLiteParameterTests.cs`, `Extensions/SQLiteTests.cs` |
| PROV-DB2 | `DataProvider/DB2Tests.cs` |
| PROV-FIREBIRD | `DataProvider/FirebirdTests.cs` |
| PROV-SAPHANA | `DataProvider/SapHanaTests.cs`, `DataProvider/Types/SapHanaTypeTests.cs` |
| PROV-SYBASE | `DataProvider/SybaseTests.cs` |
| PROV-INFORMIX | `DataProvider/InformixTests.cs` |
| PROV-CLICKHOUSE | `Extensions/ClickHouseTests.cs`, `DataProvider/Types/ClickHouseTypeTests.cs` |
| PROV-ACCESS | `DataProvider/AccessTests.cs`, `DataProvider/AccessProceduresTests.cs` |
| PROV-YDB | `DataProvider/YdbTests.cs`, `DataProvider/Types/YdbTypeTests.cs` |
| PROV-DUCKDB | `DataProvider/Types/DuckDBTypeTests.cs` *(new)*, `Linq/ConflictActionTests.cs`, `Update/BulkCopyTests.cs`, `Linq/DateTimeFunctionsTests.cs`, `Linq/DateTimeOffsetTests.cs`, plus DuckDB entries in ~30 other `Linq/` and `Update/` fixtures |
| METADATA | `Mapping/`, `Metadata/` |
| INTERCEPTORS | `Data/InterceptorsTests.cs` |
| SCAFFOLD | `SchemaProvider/`, `Scaffold/` |
| IN-TREE-TOOLS | `Tools/`, `Scaffold/` |
| INTERNAL-API | `Common/`, `Infrastructure/`, `Reflection/`, `Samples/`, `Data/DataConnectionTests.cs` |
| REMOTE-CLIENT | `Linq/RemoteContextTests.cs` |

## Files (Tier 1 / Tier 2)

**Tier 1 (read this run): 4/4**

| File | Role |
|---|---|
| `Tests/Linq/TestsInitialization.cs` | Assembly `[SetUpFixture]` -- provider registration, metrics, ClickHouse defaults |
| `Tests/Linq/TestRetryPolicy.cs` | No-op `IRetryPolicy` implementation used in tests |
| `Tests/Linq/ExpectedExceptionAttribute.cs` | NUnit `IWrapTestMethod` that replaces removed `ExpectedExceptionAttribute` |
| `Tests/Linq/YdbToDoAttributes.cs` | Yandex DB--specific `ThrowsForProvider` attribute variants |

**Tier 2: 524/600 sampled** (see deferred coverage block for the full un-visited list)

Representative reads (prior run): `Linq/CteTests.cs`, `Linq/AnalyticTests.cs`, `Linq/EagerLoadingTests.cs`, `Linq/WindowFunctionsTests.cs`, `Linq/SubQueryTests.cs`, `Linq/FullTextTests.SqlServer.cs`, `Linq/ParameterTests.cs`, `Update/MergeTests.cs`, `Update/BulkCopyTests.cs`, `Update/UpdateFromTests.cs`, `DataProvider/SqlServerTests.cs`, `DataProvider/PostgreSQLTests.cs`, `DataProvider/OracleTests.cs`, `Extensions/QueryHintsTests.cs`, `Extensions/SqlServerTests.cs`, `Data/InterceptorsTests.cs`, `Data/DataConnectionTests.cs`, `Exceptions/CommonTests.cs`, `Infrastructure/ActiveIssueConfigurationTests.cs`, `Infrastructure/AnnotatableTests.cs`, `Mapping/FluentMappingTests.cs`, `Microsoft/MicrosoftODataTests.cs`, `OrmBattle/OrmBattleTests.cs`, `ThirdParty/LinqKitTests.cs`, `UserTests/Issue2296Tests.cs`, `Linq/JoinTests.cs`, `Linq/GroupByTests.cs`, `Linq/AssociationTests.cs`, `Linq/InheritanceTests.cs`, `DataProvider/MySqlTests.cs`, `DataProvider/SQLiteTests.cs`, `DataProvider/FirebirdTests.cs`, `DataProvider/SapHanaTests.cs`, `DataProvider/SybaseTests.cs`, `Update/InsertTests.cs`, `Update/DeleteTests.cs`, `Scaffold/NameGenerationTests.cs`, `TypeMapping/OracleWrappingTests.cs`, `Infrastructure/DataOptionsTests.cs`.

Delta reads (this run, 2026-05-11): `DataProvider/Types/DuckDBTypeTests.cs` (new, full read), `Linq/EnumerableSourceTests.AsQueryable.cs` (new, full read), `Linq/ConflictActionTests.cs` (full read -- PR #5455 update).

Batch 1 reads (2026-05-07): see Coverage block below.

Batch 2 reads (2026-05-07): see Coverage block below.

Batch 3 reads (2026-05-07): see Coverage block below.

Batch 5 reads (2026-05-07): see Coverage block below.

Batch 6 reads (2026-05-07): see Coverage block below.

Batch 7 reads (2026-05-07): see Coverage block below.

Batch 8 reads (2026-05-07): see Coverage block below.

## Inbound / outbound dependencies

**Inbound:**
- `TESTS-INFRA` -- `TestBase`, `TestConfiguration`, `CustomTestContext`, NUnit fixtures, `TestProvName`, test model types from `Tests.Model` (`Parent`, `Child`, `GrandChild`, `Person`, `Northwind`). All fixtures in this area inherit `TestBase`.
- `TESTS-MODEL` -- POCO entities and mapping configurations (`Person`, `Parent`, `Child`, `GrandChild`, `Northwind.*`).

**Outbound (production areas exercised):**
- Nearly all `LinqToDB.*` namespaces. `LinqToDB.Data`, `LinqToDB.Mapping`, `LinqToDB.DataProvider.*`, `LinqToDB.Internal.SqlQuery`, `LinqToDB.Internal.Linq`, `LinqToDB.Interceptors`, `LinqToDB.Tools`, `LinqToDB.Expressions` are all directly imported by fixtures in this area.
- `LinqToDB.Tools.Mapper.MapperBuilder` (TOOLS area) -- cross-referenced by `Tools/Mapper/MapperTests.cs`.
- `LinqToDB.Tools.EntityServices.IdentityMap` (TOOLS area) -- cross-referenced by `Tools/EntityServices/IdentityMapTests.cs`.
- `LinqToDB.Internal.Expressions.Types` (dynamic type-mapping) -- cross-referenced by `TypeMapping/MappingTests.cs`.

## Known issues / debt

- `WindowFunctionsTests` family (13 files) is excluded via `<Compile Remove>` in `Tests.csproj` and is not compiled. These files exist on disk but have no active test coverage.
- `OrmBattleTests.cs` notes the file is generated from `LinqTests.tt` (T4). The `.tt` template is not in this directory; the generated output may be stale relative to the template.
- Provider-specific test files (`DataProvider/`, `Extensions/`) duplicate some logic with `PROV-*` area tests; delineation is: this area tests the *ORM layer* using provider features, while `PROV-*` areas document the provider-layer internals.
- `Common/SettingsReaderTests.cs` has class `TestSettingsTests` in namespace `Tests.Tools` (not `Tests.Common`) -- namespace/path mismatch. The file tests `SettingsReader.Deserialize` behavior and is functionally a tool/config test co-located in `Common/`.
- `Update/MergeTests.Issues.cs` is the root partial file for the `MergeTests` class (defines `[TestFixture]`), but it is named `Issues` rather than a neutral root name -- this is a naming anomaly noted for future readers.
- `DataProvider/SqlServerVectorTypeTests.cs` requires `SqlServerProviderAdapter.GetInstance(...).MappingSchema` to be passed explicitly because out-of-box `SqlVector<float>` serialization is not yet registered by default.
- `Linq/BatchTests.cs` (in Linq/ subdirectory, not Update/) is wrapped in `#if NETFRAMEWORK1` -- this condition is never true (no such symbol is defined), meaning the file is effectively dead code across all builds.
- `Linq/CursorPagination.cs` contains no `[TestFixture]` or `[Test]` attributes and is a utility class (`Paginator`) -- it contributes no test coverage, only a sample pagination implementation.
- `Linq/DataServiceTests.cs` is NETFX-only (`#if NETFRAMEWORK`); exercises `System.Data.Services` WCF stack that is not available on .NET Core/5+.
- `DataProvider/Types/DuckDBTypeTests.cs` documents extensive provider bugs inline (negative INTERVAL, UHugeInt range, BigNum read/write, TIME_NS type code 39, `\0`/`\x1` char parameters). These are DuckDB.NET provider limitations, not linq2db issues, but they constrain what can be tested today.

## See also

- [TESTS-INFRA INDEX](../TESTS-INFRA/INDEX.md) -- `TestBase`, `TestConfiguration`, shared infrastructure.
- [TESTS-MODEL INDEX](../TESTS-MODEL/INDEX.md) -- shared POCO/entity models.
- [EXPR-TRANS INDEX](../EXPR-TRANS/INDEX.md) -- LINQ-to-SQL expression translation, exercised by `Linq/`.
- [SQL-PROVIDER INDEX](../SQL-PROVIDER/INDEX.md) -- SQL generation layer.
- [INTERCEPTORS INDEX](../INTERCEPTORS/INDEX.md) -- interceptor contracts tested in `Data/InterceptorsTests.cs`.
- [METADATA INDEX](../METADATA/INDEX.md) -- mapping/schema contracts tested in `Mapping/`, `Metadata/`.

<details><summary>Coverage</summary>

**Tier 1: 4/4 read**
- `Tests/Linq/TestsInitialization.cs` -- read (prior run)
- `Tests/Linq/TestRetryPolicy.cs` -- read (prior run)
- `Tests/Linq/ExpectedExceptionAttribute.cs` -- read (prior run)
- `Tests/Linq/YdbToDoAttributes.cs` -- read (prior run)

**Tier 2: 524/600 sampled**

Read (delta run, 2026-05-11):
- `Tests/Linq/DataProvider/Types/DuckDBTypeTests.cs` -- new file; full read; DuckDB type matrix
- `Tests/Linq/Linq/EnumerableSourceTests.AsQueryable.cs` -- new file; full read; configured AsQueryable overload tests
- `Tests/Linq/Linq/ConflictActionTests.cs` -- full read; PR #5455 BulkCopy ConflictAction.Ignore

Sampled (prior runs, 2026-05-07): 521 files across all subdirectories -- see batch 1-8 entries above.

Skipped / deferred Tier-2 entries carry over from prior deferred-coverage state. The ~76 un-visited Tier-2 files remain in the deferred queue (not enumerated here; maintained in state/deferred-coverage.json).

**Tier 3: 0 files** (no generated `bin/`/`obj/` under `Tests/Linq/` in scope)

</details>
