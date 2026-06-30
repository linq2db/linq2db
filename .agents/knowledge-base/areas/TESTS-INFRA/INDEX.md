---
area: TESTS-INFRA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 14/14
coverage_tier_2: 69/70
---

# TESTS-INFRA

Test harness shared by all `Tests/` projects. Provides provider selection via parameterized NUnit attributes, configuration loading, per-test context management, SQL baseline recording, remote-transport containers, and extensibility hooks for downstream forks.

## Subsystems

### Provider selection (`Tests/Base/Attributes/`)

`DataSourcesBaseAttribute` is the root of the provider-selection tree. It implements `IParameterDataSource`, making it usable as an NUnit parameter source. `GetData()` returns the filtered provider list; if `IncludeLinqService` is true, each provider name is duplicated with the `LinqServiceSuffix` appended.

`DataSourcesAttribute` -- excludes a given set from all `TestConfiguration.UserProviders`. `IncludeDataSourcesAttribute` -- intersects the supplied list with `UserProviders`.

Feature-scoped attributes live under `Attributes/FeatureSources/` and extend `IncludeDataSourcesAttribute`. Examples:
- `AllJoinsSourceAttribute`: `AllSqlServer`, `AllOracle`, `AllFirebird`, `AllPostgreSQL`, `AllClickHouse`, **`AllDuckDB`**.
- `CteContextSourceAttribute` -- exposes `CteSupportedProviders` covering `AllSqlServer`, `AllFirebird`, `AllPostgreSQL`, `DB2`, `Ydb`, `AllSQLite`, `AllOracle`, `AllClickHouse`, `AllMySqlWithCTE`, `AllInformix`, `AllSapHana`, **`AllDuckDB`**.
- `IdentityInsertMergeDataContextSourceAttribute` -- restricts to `AllSybase`, `AllSqlServer2008Plus`, `AllPostgreSQL15Plus`, **`AllDuckDB`**.
- `SupportsAnalyticFunctionsContextAttribute` -- restricts to `AllSqlServer`, `AllOracle`, `AllClickHouse`, **`AllDuckDB`**.

`ActiveIssueAttribute` -- marks specific provider configurations as `RunState.Explicit` (skipped unless run by name) with reason `"Issue https://github.com/linq2db/linq2db/issues/<n>"`.

`ThrowsWhenAttribute` -- wraps a test command to assert that an exception of a given type is thrown when a parameter matches a specific value. Two constructors: one accepting `Type expectedException`, one accepting `string expectedException` (full type name). Inner `ThrowsWhenCommand : DelegatingTestCommand` runs the test then checks the result message. Virtual `ExpectsException(object)` handles string-contains matching; virtual `ExpectsFirst(object)` controls whether the message must *start with* the exception type (true for non-LinqService provider variants, false for LinqService suffix -- the exception type appears later in the message). `SkipCIAttribute` is `CategoryAttribute` with `TestCategory.SkipCI`.

`ThrowsCannotBeConvertedAttribute` -- sealed subclass of `ThrowsForProviderAttribute`. Hardcodes exception type to `LinqToDBException` and matches error message fragment `"could not be converted to SQL."`, covering both multi-line (`"The LINQ expression could not be converted to SQL.\nExpression:\n..."`) and single-line (`"The LINQ expression '<expr>' could not be converted to SQL."`) formats produced by `SqlErrorExpression.CreateException`. `Tests/Base/Attributes/ThrowsCannotBeConvertedAttribute.cs`.

`ThrowsRequiresCorrelatedSubqueryAttribute` -- sealed subclass of `ThrowsForProviderAttribute`. Constructor takes `bool simple = false`. When `simple=false`, expects `LinqToDBException` with `ErrorHelper.Error_Correlated_Subqueries` from both `ProviderName.Ydb` and `TestProvName.AllClickHouse`. When `simple=true`, only `ProviderName.Ydb` is in the throws-list (ClickHouse supports simple correlated subqueries via `IsSupportedSimpleCorrelatedSubqueries`). Also adds NUnit category "CorrelatedSubquery" to the test via `ApplyToTest`. `Tests/Base/Attributes/ThrowsRequiresCorrelatedSubqueryAttribute.cs`.

### Configuration loading (`TestConfiguration`, `SettingsReader`)

`TestConfiguration` is a static class whose constructor runs once per test assembly:
1. Searches upward from the assembly directory for `DataProviders.json` and `UserDataProviders.json`.
2. Deserializes both via `SettingsReader.Deserialize()` which merges them: user settings win; the `BasedOn` field allows inheritance; `++`/`---` shorthand expands/clears; `-` prefix removes individual entries.
3. Populates `UserProviders`, `SkipCategories`, `DefaultProvider`, `BaselinesPath`, and registers all connection strings into `TxtSettings.Instance` (non-framework) or `DataConnection.AddOrSetConfiguration` (net462).
4. Exposes `Providers` -- the master compile-time list of all recognizable provider names. **As of PR #5451, `TestProvName.AllDuckDB` is included in this list.**

`TxtSettings` -- implements `ILinqToDBSettings`. Queried at connection-open time on non-netfx TFMs.

### Provider name registry (`TestProvName`)

`TestProvName` -- static class of `const string` fields. Each entry is a comma-separated list of provider configuration names for one logical group. As of PR #5451 (DuckDB): **`AllDuckDB = ProviderName.DuckDB`** was added. DuckDB is **not** in `WithWindowFunctions` or `WithApplyJoin`. DuckDB uses `$` as its query parameter prefix.

SQL Server coverage now extends to 2025: `AllSqlServer2025`, `AllSqlServer2025MS`, `AllSqlServer2025Plus` (includes `AllSqlAzure` + `AllSqlAzureMi`), and `AllSqlServer2022Plus` / `AllSqlServer2019Plus` / etc. ranges updated accordingly. PostgreSQL coverage extended to 18: `PostgreSQL17`, `AllPostgreSQL17Plus`, `AllPostgreSQL18Plus = ProviderName.PostgreSQL18`; the `AllPostgreSQL15Plus` / `AllPostgreSQL` ranges are updated to include these.

### Base test class (`TestBase`)

`TestBase` is `abstract partial`. Key partials:
- `TestBase.cs` -- static constructor sets `DataConnection.WriteTraceLine`; `[SetUp]`/`[TearDown]`.
- `TestBase.Context.cs` -- `GetDataContext` / `GetDataConnection` factory methods.
- `TestBase.AssertQuery.cs` -- executes query against DB, re-evaluates LINQ expression in-memory, calls `AreEqual`. Expression rewriting handles `SqlQueryRootExpression` (replaces with a `ConstantExpression` of the `DataContext`) in addition to `ExpressionConstants.DataContextParam`. `RemapNullsOrdering` translates `LinqExtensions.OrderBy/OrderByDescending/ThenBy/ThenByDescending` overloads with a `Sql.NullsPosition` argument into two-step standard LINQ ordering: a null-grouping key (`0`/`1` constant) followed by the actual value key, reproducing NULLS FIRST/LAST semantics in-memory.
- `TestBase.Tables.cs` -- lazy-loaded cached properties for every test model entity.
- `TestBase.Concurrent.cs` -- `ConcurrentRunner` thread-pool parallelization.
- **`TestBase.Identity.cs`** -- `ResetPersonIdentity`, `ResetAllTypesIdentity`, `ResetTestSequence`. Covers Access, DB2, Firebird, Informix, MySQL, Oracle, PostgreSQL, SAP HANA, SQL Server, SqlCe, Sybase, SQLite, Ydb, and **DuckDB**. DuckDB uses `DROP SEQUENCE IF EXISTS` + `CREATE SEQUENCE START N` because it lacks `ALTER SEQUENCE RESTART`.
- **`TestBase.Utils.cs`** -- `LinqServiceSuffix = ".LinqService"`, `GetProviderName`, `GetParameterToken` (DuckDB returns `'$'`), `IsCaseSensitiveDB`, `IsCaseSensitiveComparison` (includes `AllDuckDB` as case-sensitive).

### Custom test context (`CustomTestContext`)

Thread-safe singleton `ConcurrentDictionary<string,object?>` keyed by well-known constants (`BASELINE`, `TRACE`, `LIMITED`, `BASELINE_DISABLED`, `TRACE_DISABLED`).

### Baseline management (`BaselinesManager`, `BaselinesWriter`)

`BaselinesWriter` -- writes per-test `.sql` files. Strips noise: `BeforeExecute\n`, `(asynchronously)` suffixes, **all `BeginTransaction(*)` variants (including async `BeginTransactionAsync(*)`), `DisposeTransaction\n`, and `DisposeTransactionAsync\n`** (5 transaction markers stripped, expanded in PR #5451).

### Remote transports (`Tests/Base/Remote/`)

Four transport containers spin up in-process hosts. All four now extend `ServerContainerBase<TService>` which uses dynamic port allocation via `GetFreePort()` (probes `TcpListener(IPAddress.Loopback, 0)`, releases the ephemeral port, reuses the number) rather than fixed ports. A TOCTTOU race (another process claims the probed port between probe and actual bind) is handled by `StartHostWithRetry` with up to `MaxStartAttempts = 3` attempts. Thread slots use raw `Environment.CurrentManagedThreadId` as a key (0 = shared slot when `KeepSamePortBetweenThreads = true`). `Lock _syncRoot` (the .NET 9 `System.Threading.Lock` type).

- `GrpcServerContainer` -- non-netfx; wraps `TestGrpcLinqService`; HTTPS.
- `HttpServerContainer` -- non-netfx; wraps `TestLinqService`; HTTP with `UsePathBase("/remote/linq2db")`.
- `SignalRServerContainer` -- both TFMs (netfx uses `WebHost.CreateDefaultBuilder`; non-netfx uses `Host.CreateDefaultBuilder`); hub path `/remote/linq2db`.
- `WcfServerContainer` -- netfx only; `net.tcp` binding; `MaxReceivedMessageSize` = 10MB.

Previously documented fixed ports (22655, 22656, 22654) are **no longer correct** -- port is probed dynamically by the OS.

### Test progress reporting (`TestProgressReporter`, `TestProgressTracker`)

`TestProgressReporterAttribute` -- assembly-level NUnit `ITestAction`. Delegates `BeforeTest`/`AfterTest` to the static `TestProgressTracker`. Applied as `[assembly: TestProgressReporter]` in `Tests/Tests.Playground/AssemblyInfo.TestProgress.cs`.

`TestProgressTracker` -- writes a JSON heartbeat file (`test-progress.<tfm>.<pid>.json`) under `.build/.agents/` (or a user-specified path). Opt-in via `LINQ2DB_TEST_PROGRESS` env var; disabled when unset or set to a falsy token (`0`, `false`, `off`, `no`, `disable`, `disabled`). Throttled to ~1 write/second (`WriteThrottleMs = 1000`). File write is atomic via `File.Replace(tmp, target, null)`. JSON fields: `tfm`, `pid`, `startedUtc`, `updatedUtc`, `done`, `total`, `completed`, `started`, `passed`, `failed`, `skipped`, `currentTest`, `elapsedSec`, `testsPerSec`, `etaSec`, `recentFailures` (up to 20 entries). The `_current` field retains the most-recently-started test between writes (not cleared on AfterTest) to keep the snapshot useful during throttle gaps.

### Interceptors (`Tests/Base/Interceptors/`)

Test-only `IInterceptor` implementations: `SaveQueriesInterceptor`, `CountingConnectionInterceptor`, `CountingContextInterceptor`, `SaveCommandInterceptor`, `SaveWrappedCommandInterceptor`, `SequentialAccessCommandInterceptor`, `BindByNameOracleCommandInterceptor`, `CustomizationSupportInterceptor`.

### TestProviders (`Tests/Base/TestProviders/`)

- `TestNoopProvider` -- in-memory `DynamicDataProviderBase` that executes no SQL.
- `SQLiteMiniprofilerProvider` -- extends `SQLiteDataProvider`; wraps connections with `ProfiledDbConnection`.
- `UnwrapProfilerInterceptor` -- unwraps `ProfiledDb*` types.

### Test utilities

`TestUtils` -- `GetNext()` (atomic counter), `GetSchemaName`/`GetServerName`/`GetDatabaseName` (provider-specific SQL functions -- **DuckDB maps to `current_schema()` and `current_database()`**), `GetValidCollationName` (provider-specific collation name constants -- **DuckDB returns `"NOCASE"`**), `CreateLocalTable<T>`, `Clean(string?)`, `GetConfigName`.

`ScopedSettings` -- collection of `IDisposable` scope guards: `RestoreBaseTables`, `CultureRegion`, `InvariantCultureRegion`, `OptimizeForSequentialAccess`, `ThreadHopsScope`, `DisableBaseline`, `DeletePerson`, `SerializeAssemblyQualifiedName`, `DisableLogging`.

`TestData` -- static constants for test values: `DateTimeOffset`, `DateTime`, `DateOnly`, `TimeSpan`, six `Guid` constants. `Binary(int)` and `SequentialGuid(int)`.

## Key types

| Type | File | Role |
|---|---|---|
| `TestBase` | `Tests/Base/TestBase.cs` (+ partials) | Abstract base for every test fixture |
| `DataSourcesBaseAttribute` | `Tests/Base/Attributes/DataSourcesBaseAttribute.cs` | NUnit `IParameterDataSource` |
| `DataSourcesAttribute` | `Tests/Base/Attributes/DataSourcesAttribute.cs` | Excludes listed providers |
| `IncludeDataSourcesAttribute` | `Tests/Base/Attributes/IncludeDataSourcesAttribute.cs` | Restricts to listed providers |
| `ActiveIssueAttribute` | `Tests/Base/Attributes/ActiveIssueAttribute.cs` | Marks known-failing provider/test combos |
| `ThrowsCannotBeConvertedAttribute` | `Tests/Base/Attributes/ThrowsCannotBeConvertedAttribute.cs` | Asserts `LinqToDBException` "could not be converted to SQL." for given providers |
| `ThrowsRequiresCorrelatedSubqueryAttribute` | `Tests/Base/Attributes/ThrowsRequiresCorrelatedSubqueryAttribute.cs` | Asserts correlated-subquery error for Ydb (+ ClickHouse when `simple=false`) |
| `ThrowsWhenAttribute` | `Tests/Base/Attributes/ThrowsWhenAttribute.cs` | Wraps test command; asserts exception thrown when parameter matches expected value |
| `CteContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs` | CTE-capable providers (incl. DuckDB) |
| `IdentityInsertMergeDataContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs` | Providers supporting identity-insert merge (incl. DuckDB) |
| `SupportsAnalyticFunctionsContextAttribute` | `Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs` | Window/analytic function providers (incl. DuckDB) |
| `AllJoinsSourceAttribute` | `Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs` | All-joins-capable providers (incl. DuckDB) |
| `TestConfiguration` | `Tests/Base/TestConfiguration.cs` | Loads `DataProviders.json` + `UserDataProviders.json` |
| `TestProvName` | `Tests/Base/TestProvName.cs` | Comma-list constants for every provider group |
| `CustomTestContext` | `Tests/Base/CustomTestContext.cs` | Per-test SQL trace + baseline accumulator |
| `BaselinesManager` / `BaselinesWriter` | `Tests/Base/Baselines*.cs` | SQL-baseline capture and file write |
| `IServerContainer` | `Tests/Base/Remote/ServerContainer/IServerContainer.cs` | Contract for remote transport hosts |
| `ServerContainerBase<TService>` | `Tests/Base/Remote/ServerContainer/ServerContainerBase.cs` | Dynamic-port host lifecycle; probe-then-retry port allocation |
| `TestProgressReporterAttribute` | `Tests/Base/TestProgressReporter.cs` | Assembly NUnit action; delegates to `TestProgressTracker` |
| `TestProgressTracker` | `Tests/Base/TestProgressReporter.cs` | Throttled JSON heartbeat writer for long test runs |
| `TestData` | `Tests/Base/TestData.cs` | Canonical test value constants |
| `TestUtils` | `Tests/Base/TestUtils.cs` | Schema/server/DB name query; collation names; temp table factory |
| `ScopedSettings` | `Tests/Base/ScopedSettings.cs` | IDisposable scope guards |

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):** `TestBase.cs`, all primary `Attributes/*.cs` (DataSources, IncludeDataSources, ActiveIssue, ThrowsWhen, SkipCI, CreateDatabaseSources, FeatureSources/AllJoinsSource, FeatureSources/MergeDataContextSource, FeatureSources/RecursiveCteContextSource), `TestConfiguration.cs`, `TestProvName.cs`, `Tests/Linq/Tests.csproj`.

**Tier 2 (sampled / filled):** see Coverage block.

## Inbound / outbound dependencies

**Inbound:**
- `TESTS-LINQ`, `TESTS-EFCORE`, `TESTS-FSHARP`, `TESTS-T4`, `TESTS-VB`, `TESTS-BENCHMARKS`, `TESTS-MODEL` -- all share the attribute family and `TestConfiguration`.

**Outbound:**
- **CORE** -- `DataConnection`, `DataOptions`, `Configuration`.
- **All PROV-*** -- `TestConfiguration.Providers` enumerates every provider name; **PR #5451 adds `ProviderName.DuckDB`**.
- **REMOTE-CLIENT** -- `IServerContainer` implementations wrap `LinqService`.
- **TESTS-MODEL** -- `TestBase` lazy-loads model entities.
- **NUnit, Shouldly, StackExchange.MiniProfiler**.

## Known issues / debt

- `CustomizationSupport.Interceptor` is a non-thread-safe mutable static field.
- `AssertState()` is gated behind `_assertStateEnabled = false` -- dead code in CI.
- `BaselinesWriter._baselines` is a static `Dictionary` without per-run reset.
- `Tests.csproj` excludes the entire `WindowFunctionsTests.*` partial-class family via `<Compile Remove>`.
- `MergeNotMatchedBySourceDataContextSourceAttribute` second constructor parameter is `excludeLinqService` and passed as `!excludeLinqService` -- inverted convention.
- `WcfServerContainer.Host_Faulted` throws `NotImplementedException` unconditionally.
- `SkipCategoryAttribute` returns early without applying when `ProviderName != null` -- planned but unfinished.
- **DuckDB identity-reseed uses `DROP SEQUENCE IF EXISTS` + `CREATE SEQUENCE START N`** -- correct but structurally different from every other provider's pattern.
- **`WithWindowFunctions` and `WithApplyJoin` in `TestProvName` do NOT include `AllDuckDB`** -- DuckDB window-function and lateral-join tests may be silently excluded if they use those constants directly.
- `ServerContainerBase.GetFreePort()` has a TOCTTOU race (probe-then-bind): `StartHostWithRetry` retries up to 3 times but a persistent port-churn environment can still exceed that threshold.

## See also

- [architecture/overview.md](../../architecture/overview.md)
- [areas/REMOTE-CLIENT/INDEX.md](../REMOTE-CLIENT/INDEX.md)
- [areas/TESTS-LINQ/INDEX.md](../TESTS-LINQ/INDEX.md)
- [areas/TESTS-MODEL/INDEX.md](../TESTS-MODEL/INDEX.md)

<details><summary>Coverage</summary>

- Tier 1: 14/14
- Tier 2: 69/70 (99%)

**Read (prior delta run -- PR #5451 DuckDB additions):**
- `Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/BaselinesWriter.cs` -- 5 transaction noise markers stripped
- `Tests/Base/TestBase.Identity.cs` -- DuckDB cases added
- `Tests/Base/TestBase.Utils.cs` -- `GetParameterToken` `'$'` for DuckDB; `IsCaseSensitiveComparison` includes `AllDuckDB`
- `Tests/Base/TestConfiguration.cs` -- `AllDuckDB` added to `Providers`
- `Tests/Base/TestProvName.cs` -- `AllDuckDB = ProviderName.DuckDB`
- `Tests/Base/TestUtils.cs` -- DuckDB `current_database`/`current_schema` helpers

**Read (prior delta run -- ThrowsCannotBeConverted):**
- `Tests/Base/Attributes/ThrowsCannotBeConvertedAttribute.cs` -- new sealed attribute deriving from `ThrowsForProviderAttribute`; hardcodes `LinqToDBException` + message fragment `"could not be converted to SQL."` covering both `SqlErrorExpression.CreateException` output formats
- `Tests/Base/TestUtils.cs` -- `GetValidCollationName` confirmed with `AllDuckDB => "NOCASE"` branch (not previously documented); `GetSchemaName` DuckDB branch confirmed; no other new methods
- `Tests/Tests.Playground/Tests.Playground.csproj` -- structural only; two `<Compile Include>` links (`TestsInitialization.cs`, `CreateData.cs`); no test-infra API changes


**Read (this run -- delta):**
- `Tests/Base/Attributes/ThrowsRequiresCorrelatedSubqueryAttribute.cs` -- new sealed attribute; `bool simple` param; `simple=false` expects throw from Ydb + ClickHouse; `simple=true` expects throw from Ydb only; uses `ErrorHelper.Error_Correlated_Subqueries`; adds NUnit category "CorrelatedSubquery" via `ApplyToTest`
- `Tests/Base/Attributes/ThrowsWhenAttribute.cs` -- full implementation confirmed: two constructors (Type/string overloads for exception type), virtual `ExpectsException`/`ExpectsFirst` methods, inner `ThrowsWhenCommand : DelegatingTestCommand`; `ExpectsFirst` distinguishes non-LinqService vs LinqService-suffix variants
- `Tests/Base/Remote/HttpContext/HttpServerContainer.cs` -- extends `ServerContainerBase<ITestLinqService>`; dynamic port via base class; sets `RemoteClientTag = "HttpClient"`; `Startup` uses `UsePathBase("/remote/linq2db")` + `MapControllers()`
- `Tests/Base/Remote/ServerContainer/ServerContainerBase.cs` -- refactored to dynamic port allocation: `GetFreePort()` via `TcpListener(IPAddress.Loopback, 0)`; `StartHostWithRetry` with `MaxStartAttempts = 3`; slot key is raw `Environment.CurrentManagedThreadId`; uses .NET 9 `Lock` type; `_connectionFactory` refreshed on every `CreateContext` call; fixed ports 22655/22656/22654 no longer apply
- `Tests/Base/Remote/SignalR/SignalRServerContainer.cs` -- extends `ServerContainerBase<ITestLinqService>`; hub path `/remote/linq2db`; both TFM branches confirmed
- `Tests/Base/Remote/WCF/WcfServerContainer.cs` -- extends `ServerContainerBase<TestWcfLinqService>`; `net.tcp` binding; 10MB message limits; `Host_Faulted` still throws `NotImplementedException`
- `Tests/Base/Remote/gRPC/GrpcServerContainer.cs` -- extends `ServerContainerBase<TestGrpcLinqService>`; HTTPS; `Startup.GrpcLinqService` static handshake; `UseDeveloperExceptionPage`
- `Tests/Base/TestBase.AssertQuery.cs` -- `RemapNullsOrdering` added: translates `LinqExtensions` OrderBy/ThenBy overloads with `Sql.NullsPosition` to two-step standard LINQ ordering; `SqlQueryRootExpression` now also replaced with `ConstantExpression(dc)` alongside `ExpressionConstants.DataContextParam`
- `Tests/Base/TestProgressReporter.cs` -- new file: `TestProgressReporterAttribute` (assembly `ITestAction`) + `TestProgressTracker` (throttled JSON heartbeat to `.build/.agents/test-progress.<tfm>.<pid>.json`); opt-in via `LINQ2DB_TEST_PROGRESS` env var; atomic write via `File.Replace`; up to 20 recent failures captured
- `Tests/Base/TestProvName.cs` -- SQL Server 2025 entries added (`AllSqlServer2025`, `AllSqlServer2025MS`, `AllSqlServer2025Plus`, ranges updated); PostgreSQL 17/18 entries added (`PostgreSQL17`, `AllPostgreSQL17Plus`, `AllPostgreSQL18Plus = ProviderName.PostgreSQL18`)
- `Tests/Tests.Playground/AssemblyInfo.TestProgress.cs` -- applies `[assembly: TestProgressReporter]` to the Playground project; no other test-infra API changes
</details>
