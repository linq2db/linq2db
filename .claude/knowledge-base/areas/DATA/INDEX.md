---
area: DATA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 7/7
coverage_tier_2: 24/24
---

# DATA

## What this area does

`Source/LinqToDB/Data/**` is the **public `LinqToDB.Data` namespace** -- the persistent-connection facade (`DataConnection`), raw-SQL command surface (`CommandInfo` + `DataContextExtensions.Query`/`Execute`/`ExecuteReader` overloads), bulk-copy entry points (`DataContextExtensions.BulkCopy`/`BulkCopyAsync`), tracing primitives (`TraceInfo`, `TraceInfoStep`, `TraceOperation`), the retry-policy framework (`IRetryPolicy`, `RetryPolicyBase`, `RetryingDbConnection/Command`), and the option records that feed `DataOptions` from this layer (`ConnectionOptions`, `BulkCopyOptions`, `QueryTraceOptions`, `RetryPolicyOptions`). Compared to [CORE](../CORE/INDEX.md), DATA is the *ADO.NET execution site*: it owns `DbConnection`/`DbCommand` lifecycle, parameter binding, reader materialization, transactions, and the `IQueryRunner` implementation that hands prepared SQL off to the ADO provider. AST construction, builder dispatch, and SQL emission are out of scope -- see [SQL-AST](../SQL-AST/INDEX.md), [EXPR-TRANS](../EXPR-TRANS/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md).

## Subsystems

- **Persistent connection (`DataConnection` partial class).** Six partial files compose one type. `DataConnection.cs` carries ctors (16 obsoleted overloads forwarding to `DataConnection(DataOptions)`, `Source/LinqToDB/Data/DataConnection.cs:39--501`), `DbConnection` lifecycle (`OpenDbConnection`/`OpenConnection`/`Close`, `:790--927`), command lifecycle (`InitCommand`/`CommitCommandInit`/`DisposeCommand`, `:968--1072`), the synchronous `ExecuteNonQuery`/`ExecuteScalar`/`ExecuteReader` triplet wrapped by interceptor + tracing (`:1076--1454`), transaction methods (`:1495--1659`), `MappingSchema` slot (`:1722--1779`), `IDisposable` (`:1801`), and the `UseOptions`/`UseMappingSchema` scoped-override pair (`:1824--1866`). `DataConnection.Async.cs` mirrors every execution path with `ValueTask`/`Task` returns and `ConfigureAwait(false)` discipline (`Source/LinqToDB/Data/DataConnection.Async.cs:54--777`). `DataConnection.Configuration.cs` hosts the static config registry (`ConfigurationInfo`, `_dataProviders`, `AddProviderDetector`, `Source/LinqToDB/Data/DataConnection.Configuration.cs:18--456`) plus the static ctor that chains every embedded provider's `ProviderDetector.DetectProvider` (`:194--212`), and the nested `ConfigurationApplier` static class with `Apply`/`Reapply` for each option set (`ConnectionOptions`/`RetryPolicyOptions`/`DataContextOptions`/`QueryTraceOptions`, `:502--854`). `DataConnection.Interceptors.cs` declares the `IInterceptable<...>` partial implementations for all eight interceptor kinds (`Source/LinqToDB/Data/DataConnection.Interceptors.cs:8--25`). `DataConnection.Linq.cs` wires `IDataContext.SqlProviderFlags`/`DataReaderType`/`CreateSqlBuilder`/`GetSqlOptimizer` through to the `DataProvider` (`Source/LinqToDB/Data/DataConnection.Linq.cs:21--43`).
- **Provider-detector registry.** The static ctor at `DataConnection.Configuration.cs:194--212` registers 15 embedded provider detectors in order: `Access`, `DB2`, `Firebird`, `Informix`, `MySql`, `Oracle`, `PostgreSQL`, `SapHana`, `SqlCe`, `SQLite`, `SqlServer`, `Sybase`, `ClickHouse`, `Ydb`, `DuckDB` (added in PR #5451, `:211`). Each detector is a `Func<ConnectionOptions, IDataProvider?>` added via `AddProviderDetector`; `InsertProviderDetector` prepends at index 0 for callers that need highest priority. `ConfigurationInfo.GetDataProvider` runs all detectors via `_providerDetectors.Select(d => d(options)).FirstOrDefault(dp => dp != null)` (`:143`). `DuckDBTools.ProviderDetector` follows the same static-field pattern as `YdbTools.ProviderDetector` (no `.DetectProvider` suffix -- the field itself is the `Func<ConnectionOptions, IDataProvider?>`).
- **Query runner (`DataConnection.QueryRunner`).** Nested `internal sealed class QueryRunner : QueryRunnerBase` is the `IQueryRunner` implementation `IDataContext.GetQueryRunner` returns for any LINQ query executed against a `DataConnection` (`Source/LinqToDB/Data/DataConnection.QueryRunner.cs:24--34`). It builds a `PreparedQuery` (per-statement command-list + `SqlStatement`) by running the optimizer/convert visitors, applying aliases, and rendering each command via `sqlBuilder.BuildSql` (`:180--292`). Parameter binding goes through `CreateParameter` which reconciles `DbDataType` against `MappingSchema` and lets the `IDataProvider` set provider-specific parameter shape (`:323--347`). Multi-command statements (e.g. an INSERT followed by `SELECT @@IDENTITY`, or a DROP-then-CREATE batch) are handled by per-index `InitCommand` calls and a "swallow exceptions on intermediate `DROP`" rule (`:419--443`, `:707--728`). The runner caches its prepared command list onto `query.Context` so repeated runs reuse the optimized SQL when the statement is not parameter-dependent (`:274--283`).
- **Raw-SQL command surface (`CommandInfo` + `DataContextExtensions`).** `CommandInfo` is the entry point for raw SQL: ctors accept `(IDataContext, sql)`, `(..., sql, params DataParameter[])`, `(..., sql, DataParameter)`, or `(..., sql, object parameters)` (the last reflects mapped columns into `DataParameter[]` via a compiled lambda, `Source/LinqToDB/Data/CommandInfo.cs:72--123`, `:1657--1764`). The class enforces that `_dataContext` is `DataConnection` or `DataContext` (`:74--79`), routing to `GetDataConnection()` for execution. Public surface: `Query<T>(Func<DbDataReader,T>)`, `Query<T>()`, `QueryProc<T>(...)`, `QueryAsync*`, `QueryToListAsync`/`QueryToArrayAsync`/`QueryForEachAsync`/`QueryToAsyncEnumerable`, `QueryMultiple<T>`/`QueryProcMultiple<T>` (multi-resultset materialization keyed by `[ResultSetIndex]` attributes or member declaration order, `:779--877`), `Execute`/`Execute<T>`/`ExecuteAsync*`, `ExecuteReader`/`ExecuteReaderAsync`. Object readers are cached in a static `MemoryCache<QueryKey,Delegate>` keyed by `(targetType, readerType, configId, sql, extraKey, isScalar, scalarSourceType)` and cleared via `CommandInfo.ClearObjectReaderCache()` (`:1782--1793`). `DataContextExtensions` is a flat extension wrapper that creates a `CommandInfo` and forwards -- every overload variant (parameter-form, async, template-typed, proc-typed) gets its own static method (`Source/LinqToDB/Data/DataContextExtensions.cs:31--2371`).
- **Bulk copy.** Public surface lives entirely on `DataContextExtensions` (`BulkCopy<T>` and `BulkCopyAsync<T>` with overloads accepting `BulkCopyOptions` / `int maxBatchSize` / nothing on either `IDataContext` or `ITable<T>`, `Source/LinqToDB/Data/DataContextExtensions.cs:2387--2823`). Each entry point delegates to `IDataProvider.BulkCopy(...)` after merging local options into `DataOptions` via `DataOptions.WithOptions(options)` and starts an `ActivityID.BulkCopy` activity (`:2392`, `:2415`, `:2435`). The actual bulk-copy strategies (`RowByRowBulkCopy`, `MultipleRowsBulkCopy`, provider-specific paths) live under `Source/LinqToDB/Internal/DataProvider/**` and are out of DATA scope. The DATA-side option bag is `BulkCopyOptions` -- a sealed `record` over 21 parameters. `BulkCopyType` is the four-way enum `Default`/`RowByRow`/`MultipleRows`/`ProviderSpecific`. `BulkCopyRowsCopied` is the callback DTO (`Abort`, `RowsCopied`, `StartTime`). `ConflictAction` is the new `Default`/`Ignore` flag added for MySQL/Postgres/SQLite `INSERT ... ON CONFLICT` lowering.
- **Tracing.** `TraceInfo` carries every event (`DataConnection`, `Command`, `StartTime`, `ExecutionTime`, `RecordsAffected`, `Exception`, `MapperExpression`, `IsAsync`, lazy `SqlText` formatter, `Source/LinqToDB/Data/TraceInfo.cs:14--158`). Step is one of `BeforeExecute`/`AfterExecute`/`Error`/`MapperCreated`/`Completed`. Operation is one of `ExecuteNonQuery`/`ExecuteReader`/`ExecuteScalar`/`BulkCopy`/`Open`/`BuildMapping`/`DisposeQuery`/`BeginTransaction`/`CommitTransaction`/`RollbackTransaction`/`DisposeTransaction`. The trace pipeline is invoked from `DataConnection.OnTraceConnection` (default `OnTraceInternal` formats via `Pools.StringBuilder`, `:593--695`) and from `TraceAction`/`TraceActionAsync` wrappers around transaction calls (`:1663--1718`, `Source/LinqToDB/Data/DataConnection.Async.cs:399--455`). Static `DataConnection.TraceSwitch` defaults to `Warning` in DEBUG and `Off` in Release.
- **Transactions.** `DataConnectionTransaction` is the disposable controller returned from `BeginTransaction[Async]` -- wraps `DataConnection` and tracks a `_disposeTransaction` flag so `Commit`/`Rollback` short-circuit dispose (`Source/LinqToDB/Data/DataConnectionTransaction.cs:10--77`). Begin/Commit/Rollback/Dispose all go through `TraceAction[Async]` so each transaction op produces a paired trace.
- **Retry policy.** `IRetryPolicy` exposes `Execute<T>(Func<T>)`, `Execute(Action)`, `ExecuteAsync<T>`, `ExecuteAsync` (`Source/LinqToDB/Data/RetryPolicy/IRetryPolicy.cs:7--46`). `RetryPolicyBase` is EF Core-derived with exponential backoff, jitter via `RandomFactor`, and an `AsyncLocal<bool> _suspended` flag to prevent recursive retries. Concrete provider policies live under their `PROV-*` areas; `DefaultRetryPolicyFactory.GetRetryPolicy` is the single dispatch site. `TransientRetryPolicy` and `DbExceptionTransientExceptionDetector` are gated `#if ADO_IS_TRANSIENT` (.NET 6+).
- **Reader materialization.** `DataReaderWrapper` is the disposable that owns the `DbDataReader` + `DbCommand` returned from `ExecuteReader` and chains `BeforeReaderDispose` interceptor + `OnBeforeCommandDispose` + `_dataConnection.DataProvider.DisposeCommand(Command)` + optional `Close` when `IDataContext.CloseAfterUse` is set. `DataReaderAsync` is the higher-level `IDisposable`/`IAsyncDisposable` returned from `DataContextExtensions.ExecuteReader[Async]` -- wraps `DataReaderWrapper`, tracks `ReadNumber` for multi-resultset navigation, exposes `Query<T>`/`QueryToListAsync`/`Execute<T>` etc. and emits a `TraceInfoStep.Completed` on dispose.
- **`DataParameter`.** Public DTO for ADO parameters. Stores `Name`, `Value`, `DbDataType` (lazy), `Direction`, `IsArray`, `Output` (filled by `CommandInfo.RebindParameters` for Output/InputOutput/ReturnValue parameters). Has a flat catalog of static factories per `DataType` (`Char`, `VarChar`, `Int32`, `DateTime2`, `DateTimeOffset`, `Json`, `BinaryJson`, ...) plus a `Create`-suffixed overload set keyed on .NET source type.
- **Configuration applier surface.** `ConnectionOptions` is the central record holding everything `DataConnection` needs to bind to a real `DbConnection`: `ConfigurationString`, `ConnectionString`, `IDataProvider`, `ProviderName`, `MappingSchema`, an existing `DbConnection` or `DbTransaction`, `DisposeConnection` flag, `ConnectionFactory` lambda, `DataProviderFactory` lambda, `ConnectionInterceptor`, `OnEntityDescriptorCreated` callback. It implements `IApplicable<DataConnection>`/`IApplicable<DataContext>`/`IApplicable<RemoteDataContextBase>` so the same record drives all three context kinds. `ConfigurationApplier.Apply(DataConnection, ConnectionOptions)` is a 7-tuple switch over `(ConfigurationString, ConnectionString, dataProvider, ProviderName, DbConnection, DbTransaction, ConnectionFactory)` that resolves the right binding path. `ConfigurationApplier.Reapply` enforces "not changeable dynamically" rules -- only `MappingSchema` and `ConnectionInterceptor` can swap on a live context; everything else throws `LinqToDBException`.

## Key types

- `DataConnection` -- the persistent ADO.NET context (`Source/LinqToDB/Data/DataConnection.cs:32`). `partial`, six files. Implements `IDataContext` + `IInfrastructure<IServiceProvider>`. `Options` is the immutable `DataOptions` snapshot; `DataProvider` is the resolved `IDataProvider`; `_connection` is an `IAsyncDbConnection` (potentially wrapped in `RetryingDbConnection`); `_command` is a single shared `DbCommand` reused across executions and disposed via `DataProvider.DisposeCommand`.
- `CommandInfo` -- `[PublicAPI]` raw-SQL command wrapper. Mutable public fields `CommandText`, `Parameters`, `CommandType`, `CommandBehavior`. The static `MemoryCache<QueryKey,Delegate> _objectReaders` is the single cache point for all reader-to-object materialization in DATA.
- `DataConnectionTransaction` -- `IDisposable`/`IAsyncDisposable` controller for transactions started via `DataConnection.BeginTransaction[Async]`.
- `DataContextExtensions` -- `[PublicAPI] public static class` hosting **all** raw-SQL and bulk-copy public methods. Receivers: `IDataContext` (most) and `ITable<T>` (bulk-copy).
- `DataParameter` -- `[ScalarType]`-marked public DTO for ADO parameters.
- `DataReaderWrapper` -- internal-construction disposable wrapping `DbDataReader` + `DbCommand` + before-dispose hook + optional `CloseAfterUse` close.
- `DataReaderAsync` -- public `IDisposable`/`IAsyncDisposable` returned from `ExecuteReader[Async]`.
- `BulkCopyOptions`, `BulkCopyType`, `BulkCopyRowsCopied`, `ConflictAction` -- the public bulk-copy option surface.
- `ConnectionOptions`, `RetryPolicyOptions`, `QueryTraceOptions` -- sealed `record` option types implementing `IOptionSet` + `IApplicable<DataConnection>` + `IReapplicable<...>`.
- `IRetryPolicy`, `RetryPolicyBase`, `TransientRetryPolicy`, `RetryingDbConnection`, `RetryingDbCommand`, `RetryLimitExceededException`, `DefaultRetryPolicyFactory`, `DbExceptionTransientExceptionDetector` -- the retry-policy framework.
- `LegacyMergeExtensions` -- `[Obsolete]`-marked extension class for the pre-fluent merge API; whole class is deprecated and slated for removal.
- `TraceInfo`, `TraceInfoStep`, `TraceOperation` -- public tracing primitives.

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**

- `Source/LinqToDB/Data/DataConnection.cs` (partial, head)
- `Source/LinqToDB/Data/DataConnection.Async.cs` (partial)
- `Source/LinqToDB/Data/DataConnection.Configuration.cs` (partial)
- `Source/LinqToDB/Data/DataConnection.Linq.cs` (partial)
- `Source/LinqToDB/Data/DataConnection.QueryRunner.cs` (partial)
- `Source/LinqToDB/Data/DataConnection.Interceptors.cs` (partial)
- `Source/LinqToDB/Data/CommandInfo.cs`

**Tier 2 (24 files, all visited):**

Read in full: `BulkCopyOptions.cs`, `BulkCopyType.cs`, `BulkCopyRowsCopied.cs`, `ConflictAction.cs`, `ConnectionOptions.cs`, `DataConnectionTransaction.cs`, `DataParameter.cs`, `DataReaderAsync.cs`, `DataReaderWrapper.cs`, `QueryTraceOptions.cs`, `TraceInfo.cs`, `TraceInfoStep.cs`, `TraceOperation.cs`, `RetryPolicy/IRetryPolicy.cs`, `RetryPolicy/RetryPolicyBase.cs`, `RetryPolicy/RetryingDbCommand.cs`, `RetryPolicy/RetryingDbConnection.cs`, `RetryPolicy/TransientRetryPolicy.cs`, `RetryPolicy/DbExceptionTransientExceptionDetector.cs`, `RetryPolicy/DefaultRetryPolicyFactory.cs`, `RetryPolicy/RetryLimitExceededException.cs`, `RetryPolicy/RetryPolicyOptions.cs`.

Surveyed by signature/grep: `DataContextExtensions.cs`, `LegacyMergeExtensions.cs`.

## Inbound dependencies

- [CORE](../CORE/INDEX.md) -- `IDataContext`, `DataContext`, `DataOptions`, `DataExtensions` all depend on `DataConnection` directly.
- [LINQ](../LINQ/INDEX.md) -- `Query`/`Query<T>` materialize results through `IDataContext.GetQueryRunner` which on `DataConnection` returns the nested `QueryRunner`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- `DataConnection` implements eight `IInterceptable<...>` slots and invokes them at every command-execute / connection-open / transaction / reader-dispose boundary.
- Every `PROV-*` area -- `IDataProvider.BulkCopy`, `IDataProvider.SetParameter`, `IDataProvider.InitCommand`, `IDataProvider.DisposeCommand`, `IDataProvider.ExecuteScope`, `IDataProvider.GetCommandBehavior`, `IDataProvider.GetReaderExpression`, `IDataProvider.GetSqlOptimizer`, `IDataProvider.CreateSqlBuilder` are all called from this area's execution paths.
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md), [EFCORE](../EFCORE/INDEX.md), [TOOLS](../TOOLS/INDEX.md), [LINQPAD](../LINQPAD/INDEX.md) -- companion projects construct `DataConnection` and consume `DataContextExtensions` directly.

## Outbound dependencies

- `LinqToDB.Internal.Async` (`AsyncFactory.CreateAndSetDataContext`, `IAsyncDbConnection`, `IAsyncDbTransaction`, `EmptyIAsyncDisposable`) -- see [INFRA](../INFRA/INDEX.md).
- `LinqToDB.Internal.Common` (`Pools.StringBuilder`, `IdentifierBuilder`, `OptionsContainer<T>`/`IConfigurationID`, `IExecutionScope`, `DisposableAction`, `Option<T>`).
- `LinqToDB.Metrics` (`ActivityService.Start` / `StartAndConfigureAwait`, `ActivityID.*`).
- [SQL-AST](../SQL-AST/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `DataConnection.QueryRunner.GetCommand` runs `sqlOptimizer.CreateOptimizerVisitor`/`CreateConvertVisitor`, `OptimizationContext.OptimizeAndConvertAll`, `AliasesHelper.PrepareQueryAndAliases`, and `sqlBuilder.BuildSql`.
- [MAPPING](../MAPPING/INDEX.md) -- `MappingSchema`, `EntityDescriptor`, `TypeAccessor<T>`.
- `LinqToDB.Internal.Cache` (`MemoryCache<TKey,TValue>`) -- used for `_objectReaders`, `_dataReaderConverter`, `_parameterReaders` in `CommandInfo`.
- `LinqToDB.Configuration` -- `ILinqToDBSettings`, `LinqToDBSection` consumed at static-init time.

## Recurring patterns

- **Sync/async sibling methods.** Every execution method has a paired sync method in `DataConnection.cs` and async method in `DataConnection.Async.cs`. The async version is hand-written and structurally identical, with `await ... .ConfigureAwait(false)` everywhere and `ActivityService.StartAndConfigureAwait` for activity spans. The source carries explicit `// In case of change the logic of this method, DO NOT FORGET to change the sibling method.` comments at every pair.
- **Trace wrapper triplet.** Every traceable op runs `BeforeExecute` -> action -> `AfterExecute`/`Error` via `TraceAction`/`TraceActionAsync`. The wrapper formats a SQL string lazily via `commandText?.Invoke(context)` only when `TraceSwitchConnection.TraceInfo` is set.
- **Interceptor-then-fallback.** Inside command-execute methods the pattern is "if `IInterceptable<ICommandInterceptor>.Interceptor` is set, ask it first, return its `Option<T>` if `HasValue`, else fall through to the raw provider call".
- **`CheckAndThrowOnDisposed` discipline.** Every public/internal method that mutates or reads connection-bound state starts with `CheckAndThrowOnDisposed()`.
- **Obsolete-with-`v7`-marker convention** (matches CORE). The 16 ctor overloads at the top of `DataConnection.cs:43--482` are all obsoleted in favor of `DataConnection(DataOptions)`.
- **`record` option types with `ConfigurationID` cache.** Every option type is a sealed `record`, implements `IConfigurationID.ConfigurationID` via a one-shot `IdentifierBuilder` that hashes every parameter, and overrides `Equals`/`GetHashCode` to compare on `ConfigurationID`.
- **`ConfigurationApplier` Apply/Reapply pair.** Same pattern as CORE's `DataContext.ConfigurationApplier`. Lockstep changes to `DataConnection.Configuration.cs` and the option's `IReapplicable<DataConnection>.Apply` are required when adding option fields.

## Known issues / debt

- **Pinned anchor `BulkCopy.cs` does not exist.** `kb-areas.md` row for DATA lists `BulkCopy.cs` as a Tier-1 anchor; the file is not on disk. The actual bulk-copy public surface is split between `DataContextExtensions.BulkCopy*` overloads (DATA, Tier 2) and the strategy implementations under `Source/LinqToDB/Internal/DataProvider/` (out of DATA scope).
- **Sixteen obsoleted `DataConnection` ctors slated for v7 removal.** Lines 39--482 of `DataConnection.cs` are almost entirely obsoleted ctor overloads. They forward to `DataConnection(DataOptions)`.
- **Several public properties retain obsoleted setters that are scheduled to become private in v7.** `RetryPolicy`, `OnTraceConnection`, `TraceSwitchConnection`, `WriteTraceLineConnection`, `IsMarsEnabled`, `Connection`, `CreateCommand`, `DisposeCommand`, `DisposeCommandAsync`, `EnsureConnectionAsync`, `ClearObjectReaderCache`, `ThrowOnDisposed`.
- **`LegacyMergeExtensions` is class-level `[Obsolete]`.** The whole 745-line file is deprecated.
- **`CommandInfo.ClearObjectReaderCache` is a globally-shared cache point.** The static `_objectReaders`, `_dataReaderConverter`, `_parameterReaders` `MemoryCache` instances are process-wide and not partitioned by `DataConnection` instance.
- **Single shared `_command` field reused across executions.** After `ExecuteReader`, the command is detached (`_command = null`) and ownership transfers to `DataReaderWrapper`.
- **Multi-statement DROP-then-X loop swallows DROP exceptions silently.** Intentional (drop-if-exists semantics) but obscures genuine permission/state errors.
- **`TransientRetryPolicy` is gated behind `ADO_IS_TRANSIENT` (.NET 6+ only).** Means net462/netstandard2.0 callers cannot use the cross-provider transient-retry policy.

## Pointers

- Cross-area orientation: [`architecture/overview.md`](../../architecture/overview.md), [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md), [`architecture/public-api.md`](../../architecture/public-api.md).
- Design invariants that constrain DATA: [`code-design.md`](../../../docs/code-design.md) -> **Public API is a contract** (`DataConnection`, `CommandInfo`, `DataParameter`, `DataContextExtensions`, `BulkCopyOptions`, all four trace types, every `IRetryPolicy`-related public type, and all eight `IInterceptable<...>` slot exposures are stability commitments), **Cross-cutting internals are shared** (any change to `IDataProvider.BulkCopy` / `IDataProvider.InitCommand` / `IDataProvider.SetParameter` lights up every `PROV-*` area).
- Companion areas: [CORE](../CORE/INDEX.md) (`IDataContext`, `DataOptions`, `DataContext`), [INTERCEPTORS](../INTERCEPTORS/INDEX.md), [LINQ](../LINQ/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md), [SQL-AST](../SQL-AST/INDEX.md), [MAPPING](../MAPPING/INDEX.md).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 7 / 7 -- delta re-read: DataConnection.Configuration.cs at sha 4a478ff1
  - Source/LinqToDB/Data/DataConnection.cs (partial root)
  - Source/LinqToDB/Data/DataConnection.Async.cs (partial)
  - Source/LinqToDB/Data/DataConnection.Configuration.cs (partial) -- delta re-read 2026-05-11; added DuckDB detector at :211
  - Source/LinqToDB/Data/DataConnection.Linq.cs (partial)
  - Source/LinqToDB/Data/DataConnection.QueryRunner.cs (partial)
  - Source/LinqToDB/Data/DataConnection.Interceptors.cs (partial)
  - Source/LinqToDB/Data/CommandInfo.cs
- Tier 2 (visited / total): 24 / 24 (100%)
- Tier 3 (skipped, logged): 0
</details>
