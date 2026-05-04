---
area: TESTS-INFRA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 14/14
coverage_tier_2: 69/70
---

# TESTS-INFRA

Test harness shared by all `Tests/` projects. Provides provider selection via parameterized NUnit attributes, configuration loading, per-test context management, SQL baseline recording, remote-transport containers, and extensibility hooks for downstream forks.

## Subsystems

### Provider selection (`Tests/Base/Attributes/`)

`DataSourcesBaseAttribute` (`Tests/Base/Attributes/DataSourcesBaseAttribute.cs`) is the root of the provider-selection tree. It implements `IParameterDataSource`, making it usable as an NUnit parameter source. `GetData()` returns the filtered provider list; if `IncludeLinqService` is true and `DisableRemoteContext` is false, each provider name is duplicated with the `LinqServiceSuffix` appended to drive remote variants of the same test.

`DataSourcesAttribute` — excludes a given set from all `TestConfiguration.UserProviders`. `IncludeDataSourcesAttribute` — intersects the supplied list with `UserProviders`. Both delegate to `DataSourcesBaseAttribute` and override `GetProviders()` only.

`CreateDatabaseSourcesAttribute` — used for DB-init fixtures; ensures the default provider runs even when not in `UserProviders`.

Feature-scoped attributes live under `Attributes/FeatureSources/` and extend `IncludeDataSourcesAttribute` (or `DataSourcesAttribute`) with hardcoded supported-provider lists. Examples:
- `AllJoinsSourceAttribute`: `AllSqlServer`, `AllOracle`, `AllFirebird`, `AllPostgreSQL`, `AllClickHouse`.
- `MergeDataContextSourceAttribute`: all providers minus those that lack `MERGE` (`AllAccess`, `SqlCe`, `Ydb`, `AllSQLite`, `AllSqlServer2005`, `AllClickHouse`, `AllPostgreSQL14Minus`, `AllMySql`).
- `RecursiveCteContextSourceAttribute`: wraps `CteContextSourceAttribute`, additionally excluding `Ydb` and `AllSapHana`.
- `CteContextSourceAttribute` (`Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs`) — exposes `CteSupportedProviders` (static array) covering `AllSqlServer`, `AllFirebird`, `AllPostgreSQL`, `DB2`, `Ydb`, `AllSQLite`, `AllOracle`, `AllClickHouse`, `AllMySqlWithCTE`, `AllInformix`, `AllSapHana`. Constructors allow full CTE list, a `bool includeLinqService` override, or an explicit exclusion list.
- `IdentityInsertMergeDataContextSourceAttribute` (`Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs`) — restricts to `AllSybase`, `AllSqlServer2008Plus`, `AllPostgreSQL15Plus`. Supports optional exclusion and `includeLinqService` flag.
- `MergeNotMatchedBySourceDataContextSourceAttribute` (`Tests/Base/Attributes/FeatureSources/MergeNotMatchedBySourceDataContextSourceAttribute.cs`) — restricts to `AllFirebird5Plus`, `AllSqlServer2008Plus`, `AllPostgreSQL17Plus`. The second constructor parameter is `excludeLinqService` (note: inverted convention vs. other attributes).
- `SupportsAnalyticFunctionsContextAttribute` (`Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs`) — restricts to `AllSqlServer`, `AllOracle`, `AllClickHouse`; supports optional exclusion list.

`InsertOrUpdateDataSourcesAttribute` (`Tests/Base/Attributes/InsertOrUpdateDataSourcesAttribute.cs`) — extends `DataSourcesAttribute`; statically excludes `AllClickHouse` and `Ydb` (which lack `INSERT OR UPDATE` / `MERGE`). Additional exclusions passed via constructor append to that set.

`NorthwindDataContextAttribute` (`Tests/Base/Attributes/NorthwindDataContextAttribute.cs`) — restricts to Northwind-schema providers (`AllNorthwind`, `AllSQLiteNorthwind`); constructor flags to exclude SQLite or SQLite-MS variants.

`SkipCategoryAttribute` (`Tests/Base/Attributes/SkipCategoryAttribute.cs`) — implements `IApplyToTest`; sets `RunState.Explicit` when `TestConfiguration.SkipCategories` contains the attribute's `Category`. Optional `ProviderName` property suppresses the skip for other providers (guard: non-null `ProviderName` always returns without applying).

`ThrowsForProviderAttribute` (`Tests/Base/Attributes/ThrowsForProviderAttribute.cs`) — extends `ThrowsWhenAttribute`; `ExpectsException` returns `true` when the test `context` parameter matches any provider in the `Providers` `HashSet<string>`. Automatically includes both base and `LinqServiceSuffix` variants.

`ThrowsRequiredOuterJoinsAttribute` (`Tests/Base/Attributes/ThrowsRequiredOuterJoinsAttribute.cs`) — concrete `ThrowsForProviderAttribute` that expects `LinqToDBException` with message `ErrorHelper.Error_OUTER_Joins`.

`ThrowsRequiresCorrelatedSubqueryAttribute` (`Tests/Base/Attributes/ThrowsRequiresCorrelatedSubqueryAttribute.cs`) — concrete `ThrowsForProviderAttribute` expecting `LinqToDBException` with `ErrorHelper.Error_Correlated_Subqueries`; hardcoded for `Ydb` and `AllClickHouse`.

`ActiveIssueAttribute` (`Tests/Base/Attributes/ActiveIssueAttribute.cs`) — implements `IApplyToTest`; marks specific provider configurations as `RunState.Explicit` (skipped unless run by name) with reason `"Issue https://github.com/linq2db/linq2db/issues/<n>"`. Supports `Configurations` (provider filter) and `SkipForLinqService`/`SkipForNonLinqService` flags.

`ThrowsWhenAttribute` — wraps a test command (`ThrowsWhenCommand : DelegatingTestCommand`) to assert that an exception of a given type is thrown when a parameter matches a specific value. `SkipCIAttribute` is `CategoryAttribute` with `TestCategory.SkipCI` used to exclude flaky tests from CI.

### Configuration loading (`TestConfiguration`, `SettingsReader`)

`TestConfiguration` (`Tests/Base/TestConfiguration.cs`) is a static class whose constructor runs once per test assembly. It:
1. Searches upward from the assembly directory for `DataProviders.json` (committed defaults) and `UserDataProviders.json` (local, gitignored overrides).
2. Deserializes both via `SettingsReader.Deserialize()` which merges them: user settings win; the `BasedOn` field allows inheritance between named config blocks; `++`/`---` shorthand expands/clears the provider list; `-` prefix removes individual entries; `[OtherName]` in a connection string references another connection string by name.
3. Populates `UserProviders` (`HashSet<string>`), `SkipCategories`, `DefaultProvider`, `BaselinesPath`, and registers all connection strings into `TxtSettings.Instance` (non-framework) or `DataConnection.AddOrSetConfiguration` (net462).
4. Exposes `Providers` — the master compile-time list of all recognizable provider names (`TestProvName.*` constants), filtered by `CustomizationSupport.Interceptor.GetSupportedProviders()`.

`TestSettings` / `TestConnection` are the deserialization POCOs in `Tests/Base/Tools/SettingsReader.cs`.

`TxtSettings` (`Tests/Base/TxtSettings.cs`) — implements `ILinqToDBSettings` (`DataConnection.DefaultSettings`) by storing named provider/connection-string pairs in `List<ConnectionStringSettings>` and `List<DataProviderSettings>`; queried at connection-open time on non-netfx TFMs. `AddConnectionString(name, providerName, connectionString)` is the only mutation method.

### Provider name registry (`TestProvName`)

`TestProvName` (`Tests/Base/TestProvName.cs`) — static class of `const string` fields. Each entry is a comma-separated list of provider configuration names for one logical group (e.g. `AllSqlServer`, `AllPostgreSQL17Plus`, `AllMySqlConnector`). Used as arguments to `IncludeDataSourcesAttribute` and `DataSourcesAttribute`. The `NoopProvider` constant names the in-memory fake provider.

### Base test class (`TestBase`)

`TestBase` (`Tests/Base/TestBase.cs`) is `abstract partial`. It is spread across:
- `TestBase.cs` — static constructor sets `DataConnection.WriteTraceLine` to capture SQL into `CustomTestContext`; calls `DatabaseUtils.CopyDatabases()`; loads SqlServer spatial types on netfx. `[SetUp]` enables `Configuration.OptimizeForSequentialAccess` for `AllSqlServerSequentialAccess` providers. `[TearDown]` dumps baselines, appends SQL trace to the NUnit failure message, clears `CustomTestContext`.
- `TestBase.Context.cs` — `GetDataContext(string, ...)` / `GetDataConnection(string, ...)` factory methods. Context dispatch: if the provider name is "remote" (ends with `LinqServiceSuffix`), calls `IServerContainer.CreateContext(...)`; otherwise returns a `TestDataConnection`. `_serverContainers` is an `IReadOnlyDictionary<RemoteTransport, IServerContainer>` populated at startup with the transports appropriate for the TFM (gRPC + Http + SignalR on .NET; WCF + SignalR on netfx).
- `TestBase.Asserts.cs` — `AreEqual<T>()` family; `CompareSql()`; `AssertState()` DB-consistency check.
- `TestBase.AssertQuery.cs` — `AssertQuery<T>(IQueryable<T>)`: executes the query against the DB, then re-evaluates the same LINQ expression tree in-memory against loaded table data (replacing `ITable<>` references with arrays), then calls `AreEqual`. Uses `ApplyNullPropagationVisitor` to rewrite null-guard checks for in-memory evaluation.
- `TestBase.Tables.cs` — lazy-loaded cached properties for every test model entity (`Person`, `Parent`, `Child`, `GrandChild`, `LinqDataTypes`, etc.) and `TestBaseNorthwind` wrapper. `DataCache<T>` is a static keyed cache.
- `TestBase.Concurrent.cs` (`Tests/Base/TestBase.Concurrent.cs`) — `ConcurrentRunner<TParam, TResult>` runs a query function in parallel across a thread pool capped at 10 via a `Semaphore`. Uses `SaveCommandInterceptor` per thread to capture `DbParameter[]` for failure diagnostics; dumps JSON-serialized results with `System.Text.Json` on assertion failure.
- `TestBase.Identity.cs` (`Tests/Base/TestBase.Identity.cs`) — `ResetPersonIdentity`, `ResetAllTypesIdentity`, `ResetTestSequence`: provider-specific DDL sequences to reseed identity columns. Delegates to `CustomizationSupport.Interceptor.InterceptReset*` before the built-in switch. Covers Access, DB2, Firebird, Informix, MySQL, Oracle, PostgreSQL, SAP HANA, SQL Server, SqlCe, Sybase, SQLite, Ydb.
- `TestBase.Utils.cs` (`Tests/Base/TestBase.Utils.cs`) — exposes `LinqServiceSuffix = ".LinqService"`, `GetProviderName`, `GetParameterToken` (provider-specific query parameter prefix character), `IsCaseSensitiveDB`, `IsCaseSensitiveComparison`, `IsCollatedTableConfigured`, `CreateTempTable`, `IsIDSProvider`, `AdjustExpectedData`, `GetCurrentBaselines`, `AsyncEnumerableToListAsync`.

### Custom test context (`CustomTestContext`)

`CustomTestContext` (`Tests/Base/CustomTestContext.cs`) — thread-safe singleton `ConcurrentDictionary<string,object?>` keyed by well-known constants (`BASELINE`, `TRACE`, `LIMITED`, `BASELINE_DISABLED`, `TRACE_DISABLED`). Accessed from `DataConnection.WriteTraceLine` (capture) and `[TearDown]` (flush/clear). The single instance is safe across threads because tests are not parallelized.

### Baseline management (`BaselinesManager`, `BaselinesWriter`)

`BaselinesManager` — accumulates SQL `BeforeExecute` traces into a per-test `StringBuilder` stored in `CustomTestContext`. `BaselinesManager.Dump()` delegates to `BaselinesWriter.Write()`.

`BaselinesWriter` — writes per-test `.sql` files under `BaselinesPath/<provider>/<FullyQualifiedClassName>/`. Strips noise (transaction markers, `BeforeExecute\n`, `(asynchronously)` suffixes). Validates that direct and remote runs produce identical baselines; throws if they diverge.

### Remote transports (`Tests/Base/Remote/`)

`IServerContainer` — two-member interface: `KeepSamePortBetweenThreads` and `CreateContext(...)`.

`ITestLinqService` (`Tests/Base/Remote/ITestLinqService.cs`) — minimal interface with a single `MappingSchema?` property; implemented by all concrete service wrappers to allow tests to inject a custom mapping schema into the remote endpoint.

`PortStatusRestorer` (`Tests/Base/Remote/ServerContainer/PortStatusRestorer.cs`) — `IDisposable` RAII guard that saves and restores `IServerContainer.KeepSamePortBetweenThreads`; used in async tests that need per-thread port isolation for the duration of a single test.

`TestLinqService` (`Tests/Base/Remote/TestLinqService.cs`) — concrete `LinqService` that accepts a `connectionFactory` delegate and sets `AllowUpdates = true`. Implements `ITestLinqService`.

Concrete transport containers each spin up an in-process ASP.NET Core (or WCF) host on a fixed port; port is offset by `Environment.CurrentManagedThreadId % 1000 + TestExternals.RunID` when `KeepSamePortBetweenThreads = false`:

- `GrpcServerContainer` (`Tests/Base/Remote/gRPC/GrpcServerContainer.cs`) — uses `ProtoBuf.Grpc.Server`; hosts `TestGrpcLinqService`. `#if !NETFRAMEWORK` only.
- `TestGrpcLinqService` (`Tests/Base/Remote/gRPC/TestGrpcLinqService.cs`) — extends `GrpcLinqService`, implements `ITestLinqService`; wraps a `LinqService` instance. `#if !NETFRAMEWORK` only.
- `HttpServerContainer` (`Tests/Base/Remote/HttpContext/HttpServerContainer.cs`) — base port 22655; hosts `TestHttpLinqServiceController` via ASP.NET Core MVC. Uses `app.UsePathBase("/remote/linq2db")`. `#if !NETFRAMEWORK` only.
- `TestHttpLinqServiceController` (`Tests/Base/Remote/HttpContext/TestHttpLinqServiceController.cs`) — extends `LinqToDBController`; `CreateLinqService()` returns the injected `ILinqService`. `#if !NETFRAMEWORK` only.
- `SignalRServerContainer` (`Tests/Base/Remote/SignalR/SignalRServerContainer.cs`) — base port 22656; supports both netfx (`WebHost`) and non-netfx (`Host.CreateDefaultBuilder`); maps `TestSignalRLinqService` hub at `/remote/linq2db`.
- `TestSignalRLinqService` (`Tests/Base/Remote/SignalR/TestSignalRLinqService.cs`) — extends `LinqToDBHub`; `CreateLinqService()` returns the injected `ILinqService`.
- `WcfServerContainer` (`Tests/Base/Remote/WCF/WcfServerContainer.cs`) — base port 22654; `#if NETFRAMEWORK` only. Configures `NetTcpBinding` with 10 MB message limits and 10-minute receive/send timeouts; adds a metadata exchange endpoint. Registers `Host_Faulted` handler (throws `NotImplementedException`).
- `TestWcfLinqService` (`Tests/Base/Remote/WCF/TestWcfLinqService.cs`) — extends `WcfLinqService`, implements `ITestLinqService`. `#if NETFRAMEWORK` only.

### Interceptors (`Tests/Base/Interceptors/`)

Test-only `IInterceptor` implementations:
- `SaveQueriesInterceptor` — `CommandInterceptor`; collects `CommandText` into a `List<string>`.
- `CountingConnectionInterceptor` — `ConnectionInterceptor`; counts open events.
- `CountingContextInterceptor` (`Tests/Base/Interceptors/CountingContextInterceptor.cs`) — extends `DataContextInterceptor`; tracks `OnClosed`, `OnClosedAsync`, `OnClosing`, `OnClosingAsync` with both boolean flags and integer counts per event.
- `SaveCommandInterceptor` (`Tests/Base/Interceptors/SaveCommandInterceptor.cs`) — saves the last `DbCommand` and its `DbParameter[]` snapshot on `CommandInitialized`; used in `ConcurrentRunner` for failure diagnostics.
- `SaveWrappedCommandInterceptor` (`Tests/Base/Interceptors/SaveWrappedCommandInterceptor.cs`) — like `SaveCommandInterceptor` but optionally unwraps MiniProfiler-wrapped commands via `((dynamic)command).WrappedCommand`; exposes an `OnCommandSet` event for test callbacks.
- `SequentialAccessCommandInterceptor` (`Tests/Base/Interceptors/SequentialAccessCommandInterceptor.cs`) — replaces `CommandBehavior.Default` with `CommandBehavior.SequentialAccess` on `ExecuteReader`/`ExecuteReaderAsync`; skips schema queries (non-Default behavior). Singleton `Instance`.
- `BindByNameOracleCommandInterceptor` (`Tests/Base/Interceptors/BindByNameOracleCommandInterceptor.cs`) — sets `BindByName = false` on Oracle commands via `dynamic` cast; prevents `:NEW` trigger tokens from being misinterpreted as parameters.
- `CustomizationSupportInterceptor` — the interceptor plugged into `CustomizationSupport`; no-op base class for forks to override.

### Customization support (`CustomizationSupport`)

`CustomizationSupport.Interceptor` is a public `CustomizationSupportInterceptor` instance. Forks replace it to intercept `GetSupportedProviders()`, `InterceptDataSources()`, `InterceptTestDataSources()`, `IsCaseSensitiveDB()`, identity-reset sequences, and the `CreateDataScript` used for schema init.

### TestProviders (`Tests/Base/TestProviders/`)

- `TestNoopProvider` — in-memory `DynamicDataProviderBase` that executes no SQL. Used for tests that only exercise LINQ expression translation. Uses `TestNoopSqlBuilder` (extends `BasicSqlBuilder`) and `TestNoopSqlOptimizer`.
- `SQLiteMiniprofilerProvider` (`Tests/Base/TestProviders/SQLiteMiniprofilerProvider.cs`) — extends `SQLiteDataProvider`; wraps connections with `ProfiledDbConnection`. Registered via `DataConnection.InsertProviderDetector` keyed on `SQLiteClassicMiniProfilerMapped` / `SQLiteClassicMiniProfilerUnmapped`. Installs `UnwrapProfilerInterceptor` on mapped variant via `InitContext`. Static `Init()` must be called before tests.
- `UnwrapProfilerInterceptor` (`Tests/Base/TestProviders/UnwrapProfilerInterceptor.cs`) — extends `UnwrapDataObjectInterceptor`; unwraps `ProfiledDbConnection`, `ProfiledDbTransaction`, `ProfiledDbCommand`, `ProfiledDbDataReader` to their inner objects. Singleton `Instance`.

### Asserts (`Tests/Base/Asserts/`)

Shouldly extension methods: `ShouldlyMissingExtensions.ShouldContain(string, ITimesConstraint)` counts substring occurrences; `ShouldNotContainAny`; `ShouldAllSatisfy`. Cardinality DSL:
- `ITimesConstraint` (`Tests/Base/Asserts/ITimesConstraint.cs`) — interface with `TimesType Type` and `int Times`.
- `TimesType` (`Tests/Base/Asserts/TimesType.cs`) — `enum { Exactly, AtLeast }`.
- `AtLeast` (`Tests/Base/Asserts/AtLeast.cs`) — sealed `ITimesConstraint` with cached `Once/Twice/Thrice` singletons and `Times(n)` factory.
- `Exactly` (`Tests/Base/Asserts/Exactly.cs`) — sealed `ITimesConstraint` with the same factory pattern.

`Should` (`Tests/Base/Should.cs`) — NUnit-based multi-substring constraint. `Should.Contain(params string[])` returns a `SubstringsConstraint` that verifies all strings appear in order (each search starts after the previous match position). `Should.Not.Contain(params string[])` negates it. Diagnostic message names the first unmatched substring.

### X86Stubs (`Tests/Base/X86Stubs/`)

`DB2Stubs.cs` — compiled only with `#if DB2STUBS`. Contains minimal stub types in the `IBM.Data.DB2`, `IBM.Data.Db2`, `IBM.Data.DB2.Core`, `IBM.Data.DB2Types` namespaces (connections, typed structs like `DB2TimeStamp`, `DB2Xml`, etc.) so the test assembly can reference DB2 provider types on platforms where the real IBM driver is not installed.

### DataProvider base (`Tests/Base/DataProvider/`)

`DataProviderTestBase : TestBase` — abstract base for per-provider type-mapping tests. Provides `TestType<T>()` helper that queries `AllTypes` table and compares null/value roundtrips.

`ALLTYPE` (`Tests/Base/DataProvider/ALLTYPE.cs`) — POCO mapped to the `ALLTYPES` table. Columns: `ID` (identity PK), plus nullable `BIGINT`, `INT`, `SMALLINT`, `DECIMAL`, `DECFLOAT`, `REAL`, `DOUBLE`, `CHAR`, `VARCHAR`, `CLOB`, `DBCLOB`, `BINARY`, `VARBINARY`, `BLOB`, `GRAPHIC`, `DATE`, `TIME`, `TIMESTAMP`, `XML`. Several columns carry `[NotColumn(Configuration = ProviderName.MySql)]` because MySQL lacks those types.

### Tools (`Tests/Base/Tools/`)

`SettingsReader` — JSON deserialization of `DataProviders.json` / `UserDataProviders.json`.

`TempTable` (`Tests/Base/Tools/TempTable.cs`) — static factory plus `TempTable<T>` wrapper around `db.CreateTable<T>(tableName)`. `Dispose()` calls `Table.DropTable()`. On construction failure, drops-then-recreates. Note: `TestUtils.CreateLocalTable` provides a richer variant (`FirebirdTempTable`, `TestTempTable` with `DisableBaseline`) — this simpler `TempTable<T>` is the RAII primitive.

### Miscellaneous utilities

`Helpers` (`Tests/Base/Helpers.cs`) — single extension method `ToInvariantString<T>`: formats a value with `CultureInfo.InvariantCulture` and trims trailing zeros and dots.

`DictionaryEqualityComparer<TKey,TValue>` (`Tests/Base/DictionaryEqualityComparer.cs`) — `IEqualityComparer<IDictionary<TKey,TValue>>`; compares by key-set intersection and value equality. `GetHashCode` sums key + value hash codes modulo `int.MaxValue`.

`DatabaseUtils` (`Tests/Base/DatabaseUtils.cs`) — `CopyDatabases()` called from `TestBase` static ctor. In parallel-run mode copies only the provider-specific DB file; otherwise wipes and recreates `Database/Data/` entirely.

`TestExternals` (`Tests/Base/TestExternals.cs`) — static bag of process-level state: `LogFilePath`, `IsParallelRun`, `RunID`, `Configuration`. `Log(string)` appends to a file if `LogFilePath` is set; used by server containers and `DatabaseUtils`.

`TestData` (`Tests/Base/TestData.cs`) — static constants for test values: `DateTimeOffset`, `DateTime` (with 0/3/4/6 precision variants), `DateOnly` (conditional on `SUPPORTS_DATEONLY`), `TimeSpan` interval/time-of-day variants, six `Guid` constants (read-only + non-readonly mutable copies for parameter-mutation tests). `Binary(int)` creates a patterned byte array. `SequentialGuid(int)` generates GUIDs in a deterministic sequence.

`TestDataExtensions` (`Tests/Base/TestDataExtensions.cs`) — extension methods for `DateTime?`, `DateTime`, `SqlDateTime?`, `SqlDateTime`, `TimeSpan?`, `TimeSpan`, `DateTimeOffset?`, `DateTimeOffset`: `TrimPrecision(int precision)` removes sub-precision ticks; `TrimSeconds(int addMinutes)` trims to-the-minute and adds offset. Used to normalize expected values when a provider stores fewer digits than .NET.

`TestExtensions` (`Tests/Base/TestExtensions.cs`) — single extension: `OmitUnsupportedCompareNulls(DataOptions, string context)` sets `CompareNulls.LikeSql` for ClickHouse (which cannot compare nulls like CLR), `CompareNulls.LikeClr` otherwise.

`TestCategory` (`Tests/Base/TestCategory.cs`) — constants `SkipCI = "SkipCI"`, `Create = "Create"`, `FTS = "FreeText"`. Used with `SkipCategoryAttribute` and `SkipCIAttribute`.

`TestUtils` (`Tests/Base/TestUtils.cs`) — static utility class: `GetNext()` (atomic counter for unique table names, especially for Firebird DDL isolation), `GetSchemaName` / `GetServerName` / `GetDatabaseName` (query live DB via provider-specific SQL functions), `ProviderNeedsTimeFix` (Access ODBC strips milliseconds), `GetValidCollationName` (per-provider valid collation name for collation tests), `CreateLocalTable<T>` / overloads (RAII create-or-drop-recreate with optional bulk insert), `Clean(string?)` (strip whitespace/tabs/newlines for SQL comparison), `GetConfigName` (TFM constant `NET80`/`NET90`/`NET100`/`NETFX`), `Log(string?)` (append to temp log file), `DeleteTestCases` / `GetTestFilePath` (manage `ExpressionTest.0.cs` in `%TEMP%/linq2db/`). `FirebirdTempTable<T>` extends `TestTempTable<T>` to call `FirebirdTools.ClearAllPools()` on dispose.

`ProviderNameHelpers` (`Tests/Base/ProviderNameHelpers.cs`) — extension methods on `string`: `IsAnyOf(params string[])` (matches context against comma-separated provider lists after stripping remote suffix), `SupportsRowcount`, `IsUseParameters`, `IsUsePositionalParameters`, `IsRemote`, `StripRemote`, `SplitAll` (flattens comma-separated provider strings to individual names).

`QueryUtils` (`Tests/Base/QueryUtils.cs`) — test-internal helpers for SQL AST introspection: `GetStatement<T>` / `GetSelectQuery<T>` / `GetStatement<T,TResult>` extract the `SqlStatement` from an `IQueryable<T>` by calling `Query<T>.GetQuery` and `GetQueryRunner(...).GetSqlText()`. `EnumQueries<T>` enumerates all `SelectQuery` nodes parent-first via a private `QueryInformation` tree builder. `CollectParameters` harvests `SqlParameter` nodes from a statement. `RequireInsertClause` / `RequireUpdateClause` assert presence of insert/update clauses.

`ScopedSettings` (`Tests/Base/ScopedSettings.cs`) — collection of `IDisposable` scope guards: `RestoreBaseTables` (deletes rows added by tests from `Person`, `Patient`, `Child`, `Parent`, `LinqDataTypes2`, `LinqDataTypes`, `AllTypes`), `CultureRegion` / `InvariantCultureRegion` (save/restore `Thread.CurrentThread.CurrentCulture`), `OptimizeForSequentialAccess` (save/restore `Configuration.OptimizeForSequentialAccess`), `ThreadHopsScope` (save/restore `Configuration.TranslationThreadMaxHopCount`), `DisableBaseline` (sets `CustomTestContext.BASELINE_DISABLED`), `DeletePerson` (compiled-query delete on `Dispose`), `SerializeAssemblyQualifiedName` (save/restore `Configuration.LinqService.SerializeAssemblyQualifiedName`), `DisableLogging` (sets `CustomTestContext.TRACE_DISABLED`).

`Loader` (`Tests/Base/Loader.cs`) — `SqlServerTypes.Utilities.LoadNativeAssemblies(string rootApplicationPath)` P/Invokes `LoadLibrary` to load `msvcr120.dll` and `SqlServerSpatial140.dll` from the x86/x64 subfolder. Called from `TestBase` static ctor on netfx to enable SqlServer spatial type support.

### Playground (`Tests/Tests.Playground/`)

`Tests.Playground.csproj` builds `linq2db.Tests.Playground.dll` via `linq2db.playground.slnf`. Links `TestsInitialization.cs` and `CreateData.cs` from `Tests/Linq/`. `TestTemplate.cs` is the copy-paste seed for a new ad-hoc test.

## Key types

| Type | File | Role |
|---|---|---|
| `TestBase` | `Tests/Base/TestBase.cs` (+ partials) | Abstract base for every test fixture |
| `DataSourcesBaseAttribute` | `Tests/Base/Attributes/DataSourcesBaseAttribute.cs` | NUnit `IParameterDataSource`; provider enumeration |
| `DataSourcesAttribute` | `Tests/Base/Attributes/DataSourcesAttribute.cs` | Excludes listed providers from all user providers |
| `IncludeDataSourcesAttribute` | `Tests/Base/Attributes/IncludeDataSourcesAttribute.cs` | Restricts to listed providers |
| `ActiveIssueAttribute` | `Tests/Base/Attributes/ActiveIssueAttribute.cs` | Marks known-failing provider/test combos as explicit |
| `ThrowsWhenAttribute` | `Tests/Base/Attributes/ThrowsWhenAttribute.cs` | Asserts exception thrown for specific parameter value |
| `ThrowsForProviderAttribute` | `Tests/Base/Attributes/ThrowsForProviderAttribute.cs` | `ThrowsWhenAttribute` specialized for provider-name parameter |
| `CteContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs` | CTE-capable providers; exposes `CteSupportedProviders` |
| `IdentityInsertMergeDataContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs` | Providers supporting identity-insert merge |
| `MergeNotMatchedBySourceDataContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/MergeNotMatchedBySourceDataContextSourceAttribute.cs` | Providers supporting MERGE NOT MATCHED BY SOURCE |
| `SupportsAnalyticFunctionsContextAttribute` | `Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs` | Providers supporting analytic/window functions |
| `InsertOrUpdateDataSourcesAttribute` | `Tests/Base/Attributes/InsertOrUpdateDataSourcesAttribute.cs` | Excludes providers lacking INSERT OR UPDATE |
| `NorthwindDataContextAttribute` | `Tests/Base/Attributes/NorthwindDataContextAttribute.cs` | Northwind-schema provider selector |
| `SkipCategoryAttribute` | `Tests/Base/Attributes/SkipCategoryAttribute.cs` | Skip tests when category is in `TestConfiguration.SkipCategories` |
| `TestConfiguration` | `Tests/Base/TestConfiguration.cs` | Loads `DataProviders.json` + `UserDataProviders.json` |
| `TestProvName` | `Tests/Base/TestProvName.cs` | Comma-list constants for every provider group |
| `SettingsReader` | `Tests/Base/Tools/SettingsReader.cs` | JSON merge + inheritance + provider-list expansion |
| `CustomTestContext` | `Tests/Base/CustomTestContext.cs` | Per-test SQL trace + baseline accumulator |
| `BaselinesManager` / `BaselinesWriter` | `Tests/Base/Baselines*.cs` | SQL-baseline capture and file write |
| `IServerContainer` | `Tests/Base/Remote/ServerContainer/IServerContainer.cs` | Contract for remote transport hosts |
| `ITestLinqService` | `Tests/Base/Remote/ITestLinqService.cs` | `MappingSchema` injection into remote service |
| `PortStatusRestorer` | `Tests/Base/Remote/ServerContainer/PortStatusRestorer.cs` | RAII guard for `KeepSamePortBetweenThreads` |
| `TestLinqService` | `Tests/Base/Remote/TestLinqService.cs` | Concrete `LinqService` with factory-delegate connection |
| `GrpcServerContainer` | `Tests/Base/Remote/gRPC/GrpcServerContainer.cs` | In-process gRPC host for remote tests |
| `HttpServerContainer` | `Tests/Base/Remote/HttpContext/HttpServerContainer.cs` | In-process HTTP host for remote tests |
| `SignalRServerContainer` | `Tests/Base/Remote/SignalR/SignalRServerContainer.cs` | In-process SignalR host (netfx + non-netfx) |
| `WcfServerContainer` | `Tests/Base/Remote/WCF/WcfServerContainer.cs` | In-process WCF host (netfx only) |
| `CountingContextInterceptor` | `Tests/Base/Interceptors/CountingContextInterceptor.cs` | Tracks `DataContextInterceptor` event counts |
| `SaveCommandInterceptor` | `Tests/Base/Interceptors/SaveCommandInterceptor.cs` | Captures last `DbCommand` + parameters |
| `SaveWrappedCommandInterceptor` | `Tests/Base/Interceptors/SaveWrappedCommandInterceptor.cs` | Like `SaveCommandInterceptor`; optionally unwraps profiler |
| `SequentialAccessCommandInterceptor` | `Tests/Base/Interceptors/SequentialAccessCommandInterceptor.cs` | Forces `CommandBehavior.SequentialAccess` |
| `BindByNameOracleCommandInterceptor` | `Tests/Base/Interceptors/BindByNameOracleCommandInterceptor.cs` | Oracle `BindByName=false` workaround |
| `CustomizationSupportInterceptor` | `Tests/Base/Interceptors/CustomizationSupportInterceptor.cs` | Extensibility hook for downstream forks |
| `TestNoopProvider` | `Tests/Base/TestProviders/TestNoopProvider.cs` | In-memory no-op provider for translation-only tests |
| `SQLiteMiniprofilerProvider` | `Tests/Base/TestProviders/SQLiteMiniprofilerProvider.cs` | SQLite + MiniProfiler wrapping |
| `UnwrapProfilerInterceptor` | `Tests/Base/TestProviders/UnwrapProfilerInterceptor.cs` | Unwraps `ProfiledDb*` objects for linq2db internals |
| `ITimesConstraint` / `AtLeast` / `Exactly` / `TimesType` | `Tests/Base/Asserts/` | Cardinality DSL for Shouldly occurrence assertions |
| `Should` | `Tests/Base/Should.cs` | NUnit multi-substring in-order constraint |
| `QueryUtils` | `Tests/Base/QueryUtils.cs` | SQL AST extraction from `IQueryable<T>` for unit testing |
| `TestUtils` | `Tests/Base/TestUtils.cs` | Schema/server/DB name query; temp table factory; Firebird pool management |
| `ScopedSettings` | `Tests/Base/ScopedSettings.cs` | IDisposable scope guards for config/data mutation |
| `TestData` | `Tests/Base/TestData.cs` | Canonical test value constants (dates, GUIDs, byte arrays) |
| `TestDataExtensions` | `Tests/Base/TestDataExtensions.cs` | Precision-trim extensions for date/time types |
| `ProviderNameHelpers` | `Tests/Base/ProviderNameHelpers.cs` | `IsAnyOf`, `StripRemote`, `IsRemote`, `SupportsRowcount` |
| `DictionaryEqualityComparer<K,V>` | `Tests/Base/DictionaryEqualityComparer.cs` | Dictionary structural equality comparer |
| `DatabaseUtils` | `Tests/Base/DatabaseUtils.cs` | Copies file-based DBs to `Database/Data/` at startup |
| `TestExternals` | `Tests/Base/TestExternals.cs` | Process-level state: `RunID`, `IsParallelRun`, log path |
| `TxtSettings` | `Tests/Base/TxtSettings.cs` | `ILinqToDBSettings` backed by in-memory lists |
| `TempTable<T>` | `Tests/Base/Tools/TempTable.cs` | RAII create/drop wrapper for ad-hoc test tables |
| `ALLTYPE` | `Tests/Base/DataProvider/ALLTYPE.cs` | POCO for `ALLTYPES` table used in provider type-mapping tests |
| `Loader` / `SqlServerTypes.Utilities` | `Tests/Base/Loader.cs` | Loads SqlServer spatial native DLLs on netfx |
| `Helpers` | `Tests/Base/Helpers.cs` | `ToInvariantString<T>` extension |
| `TestExtensions` | `Tests/Base/TestExtensions.cs` | `OmitUnsupportedCompareNulls` DataOptions extension |
| `TestCategory` | `Tests/Base/TestCategory.cs` | `SkipCI`, `Create`, `FTS` category name constants |

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**
- `Tests/Base/TestBase.cs`
- `Tests/Base/Attributes/DataSourcesAttribute.cs`
- `Tests/Base/Attributes/DataSourcesBaseAttribute.cs`
- `Tests/Base/Attributes/IncludeDataSourcesAttribute.cs`
- `Tests/Base/Attributes/ActiveIssueAttribute.cs`
- `Tests/Base/Attributes/ThrowsWhenAttribute.cs`
- `Tests/Base/Attributes/SkipCIAttribute.cs`
- `Tests/Base/Attributes/CreateDatabaseSourcesAttribute.cs`
- `Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs`
- `Tests/Base/Attributes/FeatureSources/MergeDataContextSourceAttribute.cs`
- `Tests/Base/Attributes/FeatureSources/RecursiveCteContextSourceAttribute.cs`
- `Tests/Base/TestConfiguration.cs`
- `Tests/Base/TestProvName.cs`
- `Tests/Linq/Tests.csproj`

**Tier 2 (sampled / filled):** see Coverage block.

## Inbound / outbound dependencies

**Inbound (who depends on TESTS-INFRA):**
- `TESTS-LINQ` — every `Tests/Linq/*.cs` fixture inherits `TestBase` and uses `DataSourcesAttribute` / `IncludeDataSourcesAttribute`.
- `TESTS-EFCORE`, `TESTS-FSHARP`, `TESTS-T4`, `TESTS-VB`, `TESTS-BENCHMARKS`, `TESTS-MODEL` — all share the attribute family and `TestConfiguration`.

**Outbound (what TESTS-INFRA depends on):**
- **CORE** (`LinqToDB.Data.DataConnection`, `LinqToDB.Data.DataOptions`, `LinqToDB.Common.Configuration`) — used to open connections and register settings.
- **All PROV-*** — `TestConfiguration.Providers` enumerates every provider name from `LinqToDB.ProviderName`; each provider's connection factory is exercised.
- **REMOTE-CLIENT** (`LinqToDB.Remote.LinqService`, `LinqToDB.Remote.Grpc`, `LinqToDB.Remote.Http`, `LinqToDB.Remote.SignalR`) — `IServerContainer` implementations wrap these.
- **TESTS-MODEL** — `TestBase` lazy-loads model entities (`Person`, `Parent`, `Child`, `LinqDataTypes`, etc.).
- **NUnit** (`NUnit.Framework`, `NUnit.Framework.Interfaces`, `NUnit.Framework.Internal`) — test lifecycle.
- **Shouldly** — assertion extensions in `Tests/Base/Asserts/`.
- **StackExchange.MiniProfiler** — `SQLiteMiniprofilerProvider` and `UnwrapProfilerInterceptor` depend on `StackExchange.Profiling.Data.ProfiledDbConnection/Transaction/Command/Reader`.

## Known issues / debt

- `CustomizationSupport.Interceptor` is a non-thread-safe mutable static field; forks must set it before any test assembly loads. No protection against concurrent assignment.
- `CustomTestContext` uses a **single global** `ConcurrentDictionary`; tests that start background threads touching the context risk cross-test contamination if a thread outlives its test's `[TearDown]`.
- `AssertState()` is gated behind `_assertStateEnabled = false` at all times — the DB-consistency assertion is dead code in CI. The `TODO` comment at line 248 (`TestBase.Asserts.cs`) flags `AllTypes` as unverified.
- `BaselinesWriter._baselines` is a static `Dictionary<string, BaselineType>` without any per-run reset; if the same test name runs in two assemblies in the same process, the guard fires spuriously.
- `TestBase.Context.cs:26` — `_serverContainers` is populated unconditionally even when `DisableRemoteContext = true`. Server hosts are not started until `CreateContext` is first called, so this is latent-only, but it registers gRPC/SignalR DI pipelines eagerly.
- `Tests.csproj` excludes the entire `WindowFunctionsTests.*` partial-class family via `<Compile Remove="...">` rather than a feature flag — any rename of those files silently re-enables them.
- `MergeNotMatchedBySourceDataContextSourceAttribute` second constructor parameter is named `excludeLinqService` and is passed as `!excludeLinqService` to `base` — inverted convention compared to all other feature-source attributes which use `includeLinqService`. This is a latent source of confusion.
- `WcfServerContainer.Host_Faulted` handler throws `NotImplementedException` unconditionally — a WCF channel fault will crash the test process rather than emit a diagnostic.
- `TestUtils.CreateLocalTable` has three overloads; the Firebird-specific pool-clearing path in `FirebirdTempTable.Dispose` calls `FirebirdTools.ClearAllPools()` twice (before and after `DataContext.Close()`), which is redundant but harmless.
- `SkipCategoryAttribute` returns early without applying when `ProviderName != null` — the `ProviderName` property is stored but never acted on, making provider-specific skip-category a no-op. Likely a planned but unfinished feature.

## See also

- [architecture/overview.md](../../architecture/overview.md) — core query pipeline
- [areas/REMOTE-CLIENT/INDEX.md](../REMOTE-CLIENT/INDEX.md) — remote linq-service transport
- [areas/TESTS-LINQ/INDEX.md](../TESTS-LINQ/INDEX.md) — per-feature test fixtures that consume this infrastructure
- [areas/TESTS-MODEL/INDEX.md](../TESTS-MODEL/INDEX.md) — test model POCOs loaded by `TestBase.Tables.cs`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 14 / 14
- Tier 2 (visited / total): 69 / 70 (99%)
- Tier 3 (skipped, logged): 0

Read (prior run): SettingsReader.cs, BaselinesManager.cs, BaselinesWriter.cs, CustomTestContext.cs, NUnitUtils.cs, TestBase.Context.cs, TestBase.AssertQuery.cs, TestBase.Asserts.cs, TestBase.Tables.cs, SaveQueriesInterceptor.cs, CountingConnectionInterceptor.cs, CustomizationSupportInterceptor.cs, IServerContainer.cs, GrpcServerContainer.cs, ShouldlyMissingExtensions.cs, TestNoopProvider.cs, DB2Stubs.cs, DataProviderTestBase.cs, CustomizationSupport.cs, Tests.Playground/TestTemplate.cs, Tests.Playground/Tests.Playground.csproj.

Read (this run): Asserts/AtLeast.cs, Asserts/Exactly.cs, Asserts/ITimesConstraint.cs, Asserts/TimesType.cs, Attributes/FeatureSources/CteContextSourceAttribute.cs, Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs, Attributes/FeatureSources/MergeNotMatchedBySourceDataContextSourceAttribute.cs, Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs, Attributes/InsertOrUpdateDataSourcesAttribute.cs, Attributes/NorthwindDataContextAttribute.cs, Attributes/SkipCategoryAttribute.cs, Attributes/ThrowsForProviderAttribute.cs, Attributes/ThrowsRequiredOuterJoinsAttribute.cs, Attributes/ThrowsRequiresCorrelatedSubqueryAttribute.cs, DatabaseUtils.cs, DataProvider/ALLTYPE.cs, DictionaryEqualityComparer.cs, Helpers.cs, Interceptors/BindByNameOracleCommandInterceptor.cs, Interceptors/CountingContextInterceptor.cs, Interceptors/SaveCommandInterceptor.cs, Interceptors/SaveWrappedCommandInterceptor.cs, Interceptors/SequentialAccessCommandInterceptor.cs, Loader.cs, ProviderNameHelpers.cs, QueryUtils.cs, Remote/gRPC/TestGrpcLinqService.cs, Remote/HttpContext/HttpServerContainer.cs, Remote/HttpContext/TestHttpLinqServiceController.cs, Remote/ITestLinqService.cs, Remote/ServerContainer/PortStatusRestorer.cs, Remote/SignalR/SignalRServerContainer.cs, Remote/SignalR/TestSignalRLinqService.cs, Remote/TestLinqService.cs, Remote/WCF/TestWcfLinqService.cs, Remote/WCF/WcfServerContainer.cs, ScopedSettings.cs, Should.cs, TestBase.Concurrent.cs, TestBase.Identity.cs, TestBase.Utils.cs, TestCategory.cs, TestData.cs, TestDataExtensions.cs, TestExtensions.cs, TestExternals.cs, TestProviders/SQLiteMiniprofilerProvider.cs, TestProviders/UnwrapProfilerInterceptor.cs, TestUtils.cs, Tools/TempTable.cs, TxtSettings.cs.

</details>
