---
area: TESTS-INFRA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
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

`ThrowsWhenAttribute` -- wraps a test command to assert that an exception of a given type is thrown when a parameter matches a specific value. `SkipCIAttribute` is `CategoryAttribute` with `TestCategory.SkipCI`.

### Configuration loading (`TestConfiguration`, `SettingsReader`)

`TestConfiguration` is a static class whose constructor runs once per test assembly:
1. Searches upward from the assembly directory for `DataProviders.json` and `UserDataProviders.json`.
2. Deserializes both via `SettingsReader.Deserialize()` which merges them: user settings win; the `BasedOn` field allows inheritance; `++`/`---` shorthand expands/clears; `-` prefix removes individual entries.
3. Populates `UserProviders`, `SkipCategories`, `DefaultProvider`, `BaselinesPath`, and registers all connection strings into `TxtSettings.Instance` (non-framework) or `DataConnection.AddOrSetConfiguration` (net462).
4. Exposes `Providers` -- the master compile-time list of all recognizable provider names. **As of PR #5451, `TestProvName.AllDuckDB` is included in this list.**

`TxtSettings` -- implements `ILinqToDBSettings`. Queried at connection-open time on non-netfx TFMs.

### Provider name registry (`TestProvName`)

`TestProvName` -- static class of `const string` fields. Each entry is a comma-separated list of provider configuration names for one logical group. As of PR #5451 (DuckDB): **`AllDuckDB = ProviderName.DuckDB`** was added. DuckDB is **not** in `WithWindowFunctions` or `WithApplyJoin`. DuckDB uses `$` as its query parameter prefix.

### Base test class (`TestBase`)

`TestBase` is `abstract partial`. Key partials:
- `TestBase.cs` -- static constructor sets `DataConnection.WriteTraceLine`; `[SetUp]`/`[TearDown]`.
- `TestBase.Context.cs` -- `GetDataContext` / `GetDataConnection` factory methods.
- `TestBase.AssertQuery.cs` -- executes query against DB, re-evaluates LINQ expression in-memory, calls `AreEqual`.
- `TestBase.Tables.cs` -- lazy-loaded cached properties for every test model entity.
- `TestBase.Concurrent.cs` -- `ConcurrentRunner` thread-pool parallelization.
- **`TestBase.Identity.cs`** -- `ResetPersonIdentity`, `ResetAllTypesIdentity`, `ResetTestSequence`. Covers Access, DB2, Firebird, Informix, MySQL, Oracle, PostgreSQL, SAP HANA, SQL Server, SqlCe, Sybase, SQLite, Ydb, and **DuckDB**. DuckDB uses `DROP SEQUENCE IF EXISTS` + `CREATE SEQUENCE START N` because it lacks `ALTER SEQUENCE RESTART`.
- **`TestBase.Utils.cs`** -- `LinqServiceSuffix = ".LinqService"`, `GetProviderName`, `GetParameterToken` (DuckDB returns `'$'`), `IsCaseSensitiveDB`, `IsCaseSensitiveComparison` (includes `AllDuckDB` as case-sensitive).

### Custom test context (`CustomTestContext`)

Thread-safe singleton `ConcurrentDictionary<string,object?>` keyed by well-known constants (`BASELINE`, `TRACE`, `LIMITED`, `BASELINE_DISABLED`, `TRACE_DISABLED`).

### Baseline management (`BaselinesManager`, `BaselinesWriter`)

`BaselinesWriter` -- writes per-test `.sql` files. Strips noise: `BeforeExecute\n`, `(asynchronously)` suffixes, **all `BeginTransaction(*)` variants (including async `BeginTransactionAsync(*)`), `DisposeTransaction\n`, and `DisposeTransactionAsync\n`** (5 transaction markers stripped, expanded in PR #5451).

### Remote transports (`Tests/Base/Remote/`)

Four transport containers spinning up in-process hosts on fixed ports: `GrpcServerContainer` (non-netfx), `HttpServerContainer` (base port 22655, non-netfx), `SignalRServerContainer` (base port 22656, both TFMs), `WcfServerContainer` (base port 22654, netfx only).

### Interceptors (`Tests/Base/Interceptors/`)

Test-only `IInterceptor` implementations: `SaveQueriesInterceptor`, `CountingConnectionInterceptor`, `CountingContextInterceptor`, `SaveCommandInterceptor`, `SaveWrappedCommandInterceptor`, `SequentialAccessCommandInterceptor`, `BindByNameOracleCommandInterceptor`, `CustomizationSupportInterceptor`.

### TestProviders (`Tests/Base/TestProviders/`)

- `TestNoopProvider` -- in-memory `DynamicDataProviderBase` that executes no SQL.
- `SQLiteMiniprofilerProvider` -- extends `SQLiteDataProvider`; wraps connections with `ProfiledDbConnection`.
- `UnwrapProfilerInterceptor` -- unwraps `ProfiledDb*` types.

### Test utilities

`TestUtils` -- `GetNext()` (atomic counter), `GetSchemaName`/`GetServerName`/`GetDatabaseName` (provider-specific SQL functions -- **DuckDB maps to `current_schema()` and `current_database()`**), `CreateLocalTable<T>`, `Clean(string?)`, `GetConfigName`.

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
| `CteContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs` | CTE-capable providers (incl. DuckDB) |
| `IdentityInsertMergeDataContextSourceAttribute` | `Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs` | Providers supporting identity-insert merge (incl. DuckDB) |
| `SupportsAnalyticFunctionsContextAttribute` | `Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs` | Window/analytic function providers (incl. DuckDB) |
| `AllJoinsSourceAttribute` | `Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs` | All-joins-capable providers (incl. DuckDB) |
| `TestConfiguration` | `Tests/Base/TestConfiguration.cs` | Loads `DataProviders.json` + `UserDataProviders.json` |
| `TestProvName` | `Tests/Base/TestProvName.cs` | Comma-list constants for every provider group |
| `CustomTestContext` | `Tests/Base/CustomTestContext.cs` | Per-test SQL trace + baseline accumulator |
| `BaselinesManager` / `BaselinesWriter` | `Tests/Base/Baselines*.cs` | SQL-baseline capture and file write |
| `IServerContainer` | `Tests/Base/Remote/ServerContainer/IServerContainer.cs` | Contract for remote transport hosts |
| `TestData` | `Tests/Base/TestData.cs` | Canonical test value constants |
| `TestUtils` | `Tests/Base/TestUtils.cs` | Schema/server/DB name query; temp table factory |
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

## See also

- [architecture/overview.md](../../architecture/overview.md)
- [areas/REMOTE-CLIENT/INDEX.md](../REMOTE-CLIENT/INDEX.md)
- [areas/TESTS-LINQ/INDEX.md](../TESTS-LINQ/INDEX.md)
- [areas/TESTS-MODEL/INDEX.md](../TESTS-MODEL/INDEX.md)

<details><summary>Coverage</summary>

- Tier 1: 14/14
- Tier 2: 69/70 (99%)

**Read (this delta run -- PR #5451 DuckDB additions):**
- `Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs` -- `AllDuckDB` added to `SupportedProviders`
- `Tests/Base/Attributes/FeatureSources/CteContextSourceAttribute.cs` -- `AllDuckDB` added to `CteSupportedProviders`
- `Tests/Base/Attributes/FeatureSources/IdentityInsertMergeDataContextSourceAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/Attributes/FeatureSources/SupportsAnalyticFunctionsContextAttribute.cs` -- `AllDuckDB` added
- `Tests/Base/BaselinesWriter.cs` -- 5 transaction noise markers stripped (added `BeginTransactionAsync(*)`, `DisposeTransaction`, `DisposeTransactionAsync`)
- `Tests/Base/TestBase.Identity.cs` -- DuckDB cases added; uses `DROP SEQUENCE IF EXISTS` + `CREATE SEQUENCE START N`
- `Tests/Base/TestBase.Utils.cs` -- `GetParameterToken` returns `'$'` for DuckDB; `IsCaseSensitiveComparison` includes `AllDuckDB`
- `Tests/Base/TestConfiguration.cs` -- `TestProvName.AllDuckDB` added to `Providers` list
- `Tests/Base/TestData.cs` -- no functional changes
- `Tests/Base/TestProvName.cs` -- `AllDuckDB = ProviderName.DuckDB` constant added
- `Tests/Base/TestUtils.cs` -- `[Sql.Function("current_database", Configuration = ProviderName.DuckDB)]` and `[Sql.Function("current_schema", Configuration = ProviderName.DuckDB)]` added to `DbName`/`SchemaName` helpers

</details>
