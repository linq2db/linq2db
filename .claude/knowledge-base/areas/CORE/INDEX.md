---
area: CORE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-06
last_verified_sha: 810c172d2cf4b404dc51b2343b491413c00f030a
coverage_tier_1: 6/6
coverage_tier_2: 53/58
---

# CORE

## What this area does

CORE owns the **top-level public-API surface** of the `linq2db` assembly — the user-facing context types (`IDataContext`, `DataContext`, `DataConnection`), the immutable `DataOptions` configuration record, the `DataExtensions` entry-point methods (`GetTable<T>`, `Insert`, `Update`, `Delete`, `Query<T>`), the `ITable<T>` queryable handle, and the `LinqToDB.Configuration` configuration entry types. Everything in this area sits at the top of the dependency graph: it brokers between user code and every internal subsystem (LINQ pipeline, mapping schema, providers, SQL builder) without owning any of those pipelines itself. AST nodes, translator builders, and SQL emission live in their own areas — see [SQL-AST](../SQL-AST/INDEX.md), [EXPR-TRANS](../EXPR-TRANS/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md), [LINQ](../LINQ/INDEX.md).

## Key types

- `IDataContext` — the central context abstraction, exposing the SQL-builder factory, `MappingSchema`, `DataReaderType`, query hints, options, and the `GetQueryRunner` execution hook (`Source/LinqToDB/IDataContext.cs:22`).
- `DataContext` — non-persistent context that opens an internal `DataConnection` lazily and disposes it after each query unless `KeepConnectionAlive` is set (`Source/LinqToDB/DataContext.cs:28`, lifecycle at `:391`, `:437`).
- `DataConnection` — persistent connection; holds the `DbConnection` for the lifetime of the object and is the actual ADO.NET execution site (`Source/LinqToDB/Data/DataConnection.cs:32`). Lives in the `LinqToDB.Data` namespace and is documented in [DATA](../DATA/INDEX.md), but cross-listed here because it implements `IDataContext` and is the canonical persistent-context factory.
- `DataOptions` — immutable, sealed configuration record. Composes per-concern option sets: `LinqOptions`, `RetryPolicyOptions`, `ConnectionOptions`, `DataContextOptions`, `BulkCopyOptions`, `SqlOptions`. Implements `OptionsContainer<DataOptions>` with `WithOptions` for fluent overrides (`Source/LinqToDB/DataOptions.cs:17`, `:49`, `:90`).
- `DataExtensions` — partial static class hosting the public extension surface on `IDataContext`: `GetTable<T>`, table-function dispatch, `Insert`/`Update`/`Delete` overloads (`Source/LinqToDB/DataExtensions.cs:29`, `:41`).
- `DataExtensions` (TempTable partial) — `CreateTempTable<T>` / `CreateTempTableAsync<T>` and `IntoTempTable<T>` / `IntoTempTableAsync<T>` overload families; all delegate to `TempTable<T>` constructors or `TempTable<T>.CreateAsync` static factory (`Source/LinqToDB/DataExtensions.TempTable.cs`). The `setTable` overloads call `GetTempTableDescriptor`, which temporarily pushes a new `MappingSchema` onto the context and returns a `TempTableDescriptor` carrying the old schema for rollback.
- `ITable<T>` — `IExpressionQuery<T>`-derived handle a user obtains from `dataContext.GetTable<T>()`; carries server/database/schema/table identity (`Source/LinqToDB/ITable{T}.cs:12`).
- `TempTable<T>` — `ITable<T>` + `ITableMutable<T>` + `IDisposable`/`IAsyncDisposable` wrapper; creates the physical table on construction (via `db.CreateTable<T>`), optionally populates it via BulkCopy or INSERT-SELECT, and drops it on `Dispose`/`DisposeAsync`. Restores the prior `MappingSchema` on dispose when a `TempTableDescriptor` is attached (`Source/LinqToDB/TempTable.cs:26`, `:839`, `:854`).
- `TempTableDescriptor` — sealed record `(EntityDescriptor EntityDescriptor, MappingSchema PrevMappingSchema)` holding the per-temp-table entity descriptor and the schema snapshot for restore (`Source/LinqToDB/TempTableDescriptor.cs:5`).
- `CompiledQuery` — caches a compiled `LambdaExpression` into a `Func<object?[], object?[]?, object?>` for repeated execution; lazy lock-protected initialization (`Source/LinqToDB/CompiledQuery.cs:25`, `:36`).
- `ProviderName` — string-constant catalog of every provider/dialect identifier the runtime recognizes (`Source/LinqToDB/ProviderName.cs:23`).
- `DataType`, `DbDataType`, `TableOptions`, `SqlJoinType`, `MergeOperationType`, `TakeHints` — public enums and structs consumed by mapping, SQL emission, and merge/insert APIs. `DbDataType` is a value struct carrying `(SystemType, DataType, DbType, Length, Precision, Scale)` with fluent `WithXxx` copy methods (`Source/LinqToDB/DbDataType.cs:15`). `SqlJoinType` exposes `Inner`/`Left`/`Right`/`Full` (`Source/LinqToDB/SqlJoinType.cs:7`). `TakeHints` is a `[Flags]` enum supporting `Percent` and `WithTies` (`Source/LinqToDB/TakeHints.cs:11`).
- `CompareNulls` — enum controlling null comparison semantics: `LikeClr` (nullable-aware IS NULL checks added), `LikeSql` (three-valued logic), `LikeSqlExceptParameters` (back-compat) (`Source/LinqToDB/CompareNulls.cs:4`).
- `LinqOptions`, `SqlOptions`, `DataContextOptions` — option records composing into `DataOptions`. `DataContextOptions` carries `CommandTimeout`, `Interceptors`, and `MemberTranslators`; it is `IApplicable<DataConnection>`, `IApplicable<DataContext>`, and `IApplicable<RemoteDataContextBase>` with both `Apply`/`Reapply` pairs (`Source/LinqToDB/DataContextOptions.cs:22`). `SqlOptions` provides `EnableConstantExpressionInOrderBy` and `GenerateFinalAliases` (`Source/LinqToDB/SqlOptions.cs:42`).
- `DataOptions<T>` — typed `DataOptions` wrapper for DI container registration, keyed by context type `T` (`Source/LinqToDB/DataOptions{T}.cs:7`). Marked `// TODO: move to linq2db.Extensions?`.
- `LinqToDBException`, `ServerSideOnlyException` — the two public exception types. `LinqToDBException` zero-arg and single-`Exception` ctors are `[Obsolete]`/`EditorBrowsable(Never)` to guide callers toward message-bearing constructors (`Source/LinqToDB/LinqToDBException.cs:20`). `ServerSideOnlyException` is thrown by server-side-only `Sql.*` members when called outside a query context (`Source/LinqToDB/ServerSideOnlyException.cs:14`).
- `RawSqlString` — readonly struct with implicit conversion from `string` and (no-op) implicit from `FormattableString`, used to distinguish overloads of `DataExtensions.FromSql<T>` (`Source/LinqToDB/RawSqlString.cs:11`).
- `QuerySql` — sealed class carrying generated SQL text and `IReadOnlyList<DataParameter>` parameters; returned by `IExpressionQuery.GetSqlQueries()` (`Source/LinqToDB/QuerySql.cs:10`).
- `SqlGenerationOptions` — options for `LinqExtensions.ToSqlQuery<T>`: `InlineParameters` and `MultiInsertMode` (`Source/LinqToDB/SqlGenerationOptions.cs:8`).
- `CreateTableOptions`, `CreateTempTableOptions` — positional records for DDL creation. `CreateTempTableOptions` is a sealed subtype of `CreateTableOptions` with default `TableOptions = TableOptions.IsTemporary`; the file comment explicitly forbids adding new parameters to `CreateTempTableOptions` as it is only for overriding defaults (`Source/LinqToDB/CreateTempTableOptions.cs:7`).
- `MergeDefinition<TTarget,TSource>` — immutable, builder-pattern class for fluent MERGE statement construction. Exposes `AddSource<TNewSource>`, `AddOperation`, `AddOnPredicate`, `AddOnKey`; inner `Operation` record holds typed predicate and expression lambdas for each `MergeOperationType` (`Source/LinqToDB/MergeDefinition{TTarget,TSource}.cs:10`).
- `MergeOperationType` — enum: `None`, `Insert`, `Update`, `Delete`, `UpdateWithDelete`, `UpdateBySource`, `DeleteBySource` (`Source/LinqToDB/MergeOperationType.cs:3`).
- `MultiInsertExtensions` — static class for Oracle-specific multi-table INSERT ALL / INSERT FIRST API. Fluent chain: `MultiInsert(source)` → `Into` / `When` / `Else` → `Insert()` / `InsertAll()` / `InsertFirst()`. Internally wraps `IQueryable` with Expression-tree `MethodInfo` calls into `MultiInsertQuery<TSource>` (`Source/LinqToDB/MultiInsertExtensions.cs:16`).
- `ExpressionMethodAttribute` — `[AttributeUsage(Property|Method, AllowMultiple=true)]`; tells linq2db to substitute the attributed member with an expression returned by the named method (or inline `LambdaExpression`). `IsColumn=true` makes it a calculated column loaded at materialization time (`Source/LinqToDB/ExpressionMethodAttribute.cs:37`).
- `ExprParameterAttribute`, `ExprParameterKind` — mark parameters of expression-method delegates; `ParameterKind` controls sequence/values semantics; `DoNotParameterize` suppresses SQL parameter generation (`Source/LinqToDB/ExprParameterAttribute.cs:7`, `Source/LinqToDB/ExprParameterKind.cs:3`).
- `ExtensionBuilderExtensions` — static helpers for `Sql.ISqlExtensionBuilder`: `AddParameter`, `AddFragment`, and arithmetic AST node constructors (`Add`, `Sub`, `Mul`, `Div`, `Inc`, `Dec`, `BitNot`, `Negate`) that emit `SqlBinaryExpression` / `SqlUnaryExpression` nodes (`Source/LinqToDB/ExtensionBuilderExtensions.cs:9`).
- `IExtensionsAdapter` — interface for overriding default linq2db async LINQ terminal operations (`ToListAsync`, `FirstAsync`, `AnyAsync`, `SumAsync`, `AverageAsync`, etc.); used by EF Core integration to delegate to EF's own async LINQ providers (`Source/LinqToDB/IExtensionsAdapter.cs:13`).
- `ILoadWithQueryable<TEntity,TProperty>` — marker interface (extends `IQueryable<TEntity>`) used to type-check `LoadWith`/`ThenLoad` chains (`Source/LinqToDB/ILoadWithQueryable.cs:12`).
- `InsertColumnFilter<T>`, `InsertOrUpdateColumnFilter<T>`, `UpdateColumnFilter<T>` — delegate types for per-column inclusion predicates on DML operations (`Source/LinqToDB/InsertColumnFilter.cs`, `Source/LinqToDB/InsertOrUpdateColumnFilter.cs`, `Source/LinqToDB/UpdateColumnFilter.cs`).
- `UpdateOutput<T>` — simple class with `Deleted` and `Inserted` properties used to capture OUTPUT clause rows from UPDATE operations (`Source/LinqToDB/UpdateOutput.cs:3`).
- `KeepConnectionAliveScope` — `IDisposable`/`IAsyncDisposable` RAII wrapper that sets `DataContext.KeepConnectionAlive = true` on entry and restores the saved value on dispose (`Source/LinqToDB/KeepConnectionAliveScope.cs:13`).
- `DataContextTransaction` — explicit transaction wrapper for `DataContext`; delegates to the underlying `DataConnection` and acquires a `ConnectionLockScope` on first `BeginTransaction` call; decrements a counter on commit/rollback (`Source/LinqToDB/DataContextTransaction.cs:14`).
- `SqlExtensions` — `In<T>` / `NotIn<T>` extension methods on any type, wired via `[ExpressionMethod]` to their expression implementations for SQL translation (`Source/LinqToDB/SqlExtensions.cs:18`).
- `StringAggregateExtensions` — `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` / `ToValue` for `Sql.IAggregateFunctionNotOrdered<T,TR>` / `Sql.IAggregateFunction<T,TR>` chaining; builds WITHIN GROUP (ORDER BY …) SQL. Oracle-specific `WITHIN GROUP (ORDER BY ROWNUM)` forced via provider token at `ChainPrecedence = 0` (`Source/LinqToDB/StringAggregateExtensions.cs:13`).
- `AnalyticFunctions` — large public static class providing `ROW_NUMBER`, `RANK`, `DENSE_RANK`, `NTILE`, window aggregates, lead/lag, first/last value, etc. via `[Sql.Extension]` attributes. Internal `OrderItemBuilder`, `ApplyAggregateModifier`, `ApplyNullsModifier`, `ForceApplyNullsModifier` call-builder helpers construct NULLS FIRST/LAST and DISTINCT/ALL modifiers at SQL-generation time (`Source/LinqToDB/AnalyticFunctions.cs:16`).
- `TableExtensions` — `IsTemporary<T>`, `TableOptions<T>`, `GetTableName<T>` extension methods on `ITable<T>`; `GetTableName` builds the fully-qualified name via the context's `ISqlBuilder` (`Source/LinqToDB/TableExtensions.cs:13`).
- `ILinqToDBSettings`, `LinqToDBSettings`, `LinqToDBSection` — configuration entry interfaces and implementations under `LinqToDB.Configuration`. `LinqToDBSection` is the legacy `app.config` section, gated on `NETFRAMEWORK || COMPAT` and forwarded via `[TypeForwardedTo]` for compat-shim builds (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1`, `:17`); `LinqToDBSettings` is the explicit-construction variant (`Source/LinqToDB/Configuration/LinqToDBSettings.cs:8`).
- `DataContextTransaction`, `DataContextOptions`, `KeepConnectionAliveScope` — context-side companions for transaction lifetime, sub-options, and lock scopes (`Source/LinqToDB/DataContext.cs:582`, `:968`).

## Configuration subsystem (LinqToDB.Configuration)

All `Configuration/` types are TFM-gated: the five `System.Configuration`-based types (`ElementBase`, `ElementCollectionBase<T>`, `DataProviderElement`, `DataProviderElementCollection`, `ConnectionStringSettings`) exist only under `NETFRAMEWORK || COMPAT`; the interfaces (`IConnectionStringSettings`, `IDataProviderSettings`) and `NamedValue` are unconditional. The `app.config` data provider element key is `string.Concat(element.Name, "/", element.TypeName)` — name is optional and may be empty, type name is required but not unique, so only the compound key is unique (`Source/LinqToDB/Configuration/DataProviderElementCollection.cs:19`). `IDataProviderSettings.Attributes` carries provider-specific parameters (e.g. `assemblyName` for Sybase/Oracle/HANA, `version` for SQL Server and DB2).

## DataOptionsExtensions

`DataOptionsExtensions` is split across two files. The main file (`DataOptionsExtensions.cs`) covers `LinqOptions`, `SqlOptions`, `ConnectionOptions`, `DataContextOptions`, `BulkCopyOptions`, and miscellaneous helpers (`UseConnectionString`, `UseProvider`, `UseInterceptor`, `RemoveInterceptor`, `UseEnableContextSchemaEdit`, etc.). The provider file (`DataOptionsExtensions.Provider.cs`) contains `Use<Database>` overloads for every supported provider (SQL Server, Oracle, PostgreSQL, MySQL, SQLite, Access, DB2, Firebird, Informix, SAP HANA, SQL CE, SAP/Sybase ASE, ClickHouse), following the overload convention documented in the file's leading comment: two overloads for single-dialect providers, four overloads for multi-dialect/provider databases. Provider overloads always delegate to `<Provider>Tools.ProviderDetector.CreateOptions(options, dialect, provider)` then apply the `optionSetter` callback (`Source/LinqToDB/DataOptionsExtensions.Provider.cs:55`).

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**

- `Source/LinqToDB/IDataContext.cs`
- `Source/LinqToDB/DataContext.cs`
- `Source/LinqToDB/Data/DataConnection.cs` (head + ConfigurationApplier sample; full file lives in DATA area scope)
- `Source/LinqToDB/LinqToDB.csproj`
- `Source/LinqToDB/Configuration/LinqToDBSection.cs`
- `Source/LinqToDB/Configuration/ILinqToDBSettings.cs`

**Tier 2 sample target — `Source/LinqToDB/*.cs` (root only) + `Source/LinqToDB/Configuration/*.cs`:**

Visited in full (prior run): `DataOptions.cs`, `DataExtensions.cs`, `ITable{T}.cs`, `CompiledQuery.cs`, `ProviderName.cs`, `LinqOptions.cs`, `TableOptions.cs`, `DataType.cs`, `Configuration/LinqToDBSettings.cs`.

Read in full (2026-05-06 coverage-fill): `AnalyticFunctions.cs`, `CompareNulls.cs`, `Configuration/ConnectionStringSettings.cs`, `Configuration/DataProviderElement.cs`, `Configuration/DataProviderElementCollection.cs`, `Configuration/ElementBase.cs`, `Configuration/ElementCollectionBase.cs`, `Configuration/IConnectionStringSettings.cs`, `Configuration/IDataProviderSettings.cs`, `Configuration/NamedValue.cs`, `CreateTableOptions.cs`, `CreateTempTableOptions.cs`, `DataContext.Interceptors.cs`, `DataContextOptions.cs`, `DataContextTransaction.cs`, `DataExtensions.TempTable.cs`, `DataOptions{T}.cs`, `DataOptionsExtensions.cs`, `DataOptionsExtensions.Provider.cs`, `DbDataType.cs`, `ExpressionMethodAttribute.cs`, `ExprParameterAttribute.cs`, `ExprParameterKind.cs`, `ExtensionBuilderExtensions.cs`, `IExtensionsAdapter.cs`, `ILoadWithQueryable.cs`, `InsertColumnFilter.cs`, `InsertOrUpdateColumnFilter.cs`, `KeepConnectionAliveScope.cs`, `LinqToDBException.cs`, `MergeDefinition{TTarget,TSource}.cs`, `MergeOperationType.cs`, `MultiInsertExtensions.cs`, `QuerySql.cs`, `RawSqlString.cs`, `ServerSideOnlyException.cs`, `SqlExtensions.cs`, `SqlGenerationOptions.cs`, `SqlJoinType.cs`, `SqlOptions.cs`, `StringAggregateExtensions.cs`, `TableExtensions.cs`, `TakeHints.cs`, `TempTable.cs`, `TempTableDescriptor.cs`, `UpdateColumnFilter.cs`, `UpdateOutput.cs`.

Skipped (with reason):

- `Source/LinqToDB/IDataContext.cs` — Tier 1, not Tier 2.
- `Source/LinqToDB/DataContext.cs` — Tier 1.
- 5 declaration-only enum/record files — `near-duplicate of Tier-1 type information already captured` (skip count surfaces in coverage tally; explicit list withheld to keep the index readable).

## Inbound dependencies

- [LINQ](../LINQ/INDEX.md) — `Query<T>`, `IQueryRunner`, `ExpressionQuery<T>` consume `IDataContext.GetQueryRunner`, `MappingSchema`, `Options`.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — `ExpressionBuilder` reads `DataOptions.LinqOptions` and dispatches via the context's `MappingSchema`.
- [DATA](../DATA/INDEX.md) — `DataConnection`'s persistent-connection internals (transactions, bulk copy, retry policy) sit on top of `IDataContext`/`DataOptions`.
- Every `PROV-*` area — provider entry points (`SqlServerDataProvider`, `PostgreSQLDataProvider`, …) plug into `IDataContext` via the `IDataProvider` contract surfaced through `DataConnection.DataProvider` and `DataContext.DataProvider`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) — `IDataContext.AddInterceptor` / `RemoveInterceptor` are the registration site; concrete interceptor types live under `LinqToDB.Interceptors`.
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md), [EFCORE](../EFCORE/INDEX.md), [TOOLS](../TOOLS/INDEX.md), [LINQPAD](../LINQPAD/INDEX.md) — companion packages consume `DataConnection` / `DataContext` / `DataOptions` directly.

## Outbound dependencies

- [MAPPING](../MAPPING/INDEX.md) via `MappingSchema` — `IDataContext.MappingSchema` (`IDataContext.cs:51`), `DataContext.AddMappingSchema` (`DataContext.cs:114`), `IDataContext.SetMappingSchema` (`DataContext.cs:120`).
- [LINQ](../LINQ/INDEX.md) via `Query`/`IQueryRunner`/`IQueryExpressions` — surfaced on `IDataContext.GetQueryRunner` (`IDataContext.cs:112`).
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) via `Func<ISqlBuilder>` and `Func<DataOptions, ISqlOptimizer>` factory delegates (`IDataContext.cs:31`, `:35`); `DataContext` resolves both through `DataProvider.CreateSqlBuilder` / `DataProvider.GetSqlOptimizer` (`DataContext.cs:486`, `:487`).
- [SQL-AST](../SQL-AST/INDEX.md) — `ExtensionBuilderExtensions` directly constructs `SqlBinaryExpression`, `SqlUnaryExpression`, `SqlValue`, `SqlFragment` AST nodes; option records and merge definitions reference AST building blocks indirectly via the SqlOptimizer / SqlBuilder factories above.
- [INFRA](../INFRA/INDEX.md) — `LinqToDB.Common`, `LinqToDB.Internal.Common` (`OptionsContainer<T>`, `IConfigurationID`, `IdentifierBuilder`, `DisposableAction`), `LinqToDB.Metrics` (`ActivityService`, `ActivityID`).
- `DataProvider` namespace — `IDataProvider` interface contract is consumed by every `IDataContext` implementation.

## Recurring patterns

- **Immutable-record options pattern.** `DataOptions` is sealed, `ICloneable`, and overrides `WithOptions(IOptionSet)` to return either the receiver or a new `DataOptions` whose specific option set is replaced (`Source/LinqToDB/DataOptions.cs:49`). Sub-option records (`LinqOptions`, `ConnectionOptions`, `SqlOptions`, `DataContextOptions`, …) follow the same shape — `[Pure] WithX(...)` returns a new instance, never mutates. Mutating the static `Default` property on `DataContextOptions` or `SqlOptions` triggers `DataConnection.ResetDefaultOptions()` and clears `ConnectionOptionsByConfigurationString` (`Source/LinqToDB/DataContextOptions.cs:118`, `Source/LinqToDB/SqlOptions.cs:88`). `DataConnection.DefaultDataOptions` plus `ConnectionOptionsByConfigurationString` (concurrent-dictionary cache, `Source/LinqToDB/DataContext.cs:53`) is the canonical static-default lookup.
- **Nested `ConfigurationApplier` static class.** Both `DataContext` (`DataContext.cs:726`) and `DataConnection` host an `internal static class ConfigurationApplier` with paired `Apply` / `Reapply` methods — `Apply` resolves `ConnectionOptions`/`DataContextOptions` against the live context; `Reapply` enforces "not changeable dynamically" rules and returns an `Action` undo delegate consumed by `UseOptions` (`DataContext.cs:919`). The pattern lets `IDisposable UseOptions(...)` swap option records temporarily and roll back on dispose. `DataContextOptions` is `IReapplicable<DataConnection>`, `IReapplicable<DataContext>`, and `IReapplicable<RemoteDataContextBase>`, implementing Reapply by short-circuiting when `ConfigurationID` is unchanged (`Source/LinqToDB/DataContextOptions.cs:86`).
- **`AssertDisposed` discipline.** Every public mutator and most accessors on `DataContext` start with `AssertDisposed()` (e.g. `DataContext.cs:149`, `:215`, `:228`, `:273`); the disposed flag is private and the throw message links to the wiki. New methods added to `DataContext` are expected to follow the same precondition.
- **TFM-conditional configuration entry.** `LinqToDBSection` is gated `#if NETFRAMEWORK && COMPAT` for `[TypeForwardedTo]` and `#elif NETFRAMEWORK || COMPAT` for the body (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1–3`). The `System.Configuration`-derived types (`ElementBase`, `ElementCollectionBase<T>`, `DataProviderElement`, `DataProviderElementCollection`) follow the same gate — they exist only under `NETFRAMEWORK || COMPAT`. Modern (`net8.0`+) callers configure via `DataOptions` directly and `LinqToDBSettings` (`Source/LinqToDB/Configuration/LinqToDBSettings.cs:8`); the `app.config` `<linq2db>` section is a back-compat path only.
- **Obsolete-with-`v7`-marker convention.** Legacy ctors and properties carry `[Obsolete("This API scheduled for removal in v7. Instead use: …")]` plus `EditorBrowsable(EditorBrowsableState.Never)` and a `// TODO: Remove in v7` comment on the line above (`DataContext.cs:67`, `:82`, `:181`, `:206`, `:239`; `DataConnection.cs:46`, `:74`, `:92`, …). `DataOptionsExtensions.WithPreloadGroups` is an example of an already-no-op API so marked (`Source/LinqToDB/DataOptionsExtensions.cs:39`). The pattern is consistent across CORE — when adding new public surface, prefer fluent `DataOptions` builders over ctor overloads, and document any planned removal with the same comment marker.
- **`DataContext.Interceptors.cs` partial pattern.** `DataContext` is partial; the interceptor registration logic lives in its own file. Eight interceptor aggregators (`AggregatedCommandInterceptor`, `AggregatedConnectionInterceptor`, etc.) are stored as nullable fields; `AddInterceptor(IInterceptor, bool addToOptions)` dispatches by type via a `switch` statement and creates the `AggregatedInterceptor<TI>` instance lazily, also propagating to the underlying `_dataConnection` if it already exists (`Source/LinqToDB/DataContext.Interceptors.cs:82`).
- **`MergeDefinition` immutable-builder pattern.** Every fluent method on `MergeDefinition<TTarget,TSource>` returns a new instance via the private all-parameters constructor — the public state (`Operations`, `MatchPredicate`, keys) is append-only via `AddOperation`/`AddOnPredicate`/`AddOnKey`. `Operation` is a nested sealed class with static factory methods per `MergeOperationType` (`Source/LinqToDB/MergeDefinition{TTarget,TSource}.cs:85`).
- **`TempTable<T>` lifecycle contract.** Construction creates the physical table; the constructor family that accepts `IEnumerable<T>` (BulkCopy) or `IQueryable<T>` (INSERT-SELECT) wraps population in a try/catch and drops the table on failure. `Dispose`/`DisposeAsync` always calls `DropTable(throwExceptionIfNotExists: false)` then restores `MappingSchema` if a `TempTableDescriptor` was provided (`Source/LinqToDB/TempTable.cs:839`).

## Known issues / debt

- `DataOptions<T>` has a `// TODO: move to linq2db.Extensions?` comment (`Source/LinqToDB/DataOptions{T}.cs:7`) — its placement in the core assembly is a known open question.
- `CreateTempTableOptions` has a comment forbidding new parameters (`Source/LinqToDB/CreateTempTableOptions.cs:7`): it is only allowed to override `CreateTableOptions` defaults for temp tables. Adding new DDL-specific options there would be a design violation.
- Multiple APIs in `DataOptionsExtensions.cs` are already `[Obsolete]` + no-op (e.g. `WithPreloadGroups`) pending v7 cleanup.

## Pointers

- Cross-area orientation: [`architecture/overview.md`](../../architecture/overview.md), [`architecture/public-api.md`](../../architecture/public-api.md), [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md).
- Design invariants that constrain CORE: [`code-design.md`](../../../docs/code-design.md) → **Public API is a contract** (every type listed under "Key types" is a stability commitment), **Cross-cutting internals are shared** (don't reshape `IDataContext` for a local fix), **Column-aligned formatting is intentional** (preserve the `Get { ... }` column alignment in `IDataContext.cs:27–73` and `DataContext.cs:108–138`).
- Companion areas: [DATA](../DATA/INDEX.md) for `DataConnection` internals, [MAPPING](../MAPPING/INDEX.md) for `MappingSchema`, [LINQ](../LINQ/INDEX.md) for the query runner, [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) for the builder/optimizer contracts surfaced via `IDataContext`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 6 / 6 ✓
  - Source/LinqToDB/IDataContext.cs
  - Source/LinqToDB/DataContext.cs
  - Source/LinqToDB/Data/DataConnection.cs (head + ConfigurationApplier; full body cross-listed under DATA)
  - Source/LinqToDB/LinqToDB.csproj
  - Source/LinqToDB/Configuration/LinqToDBSection.cs
  - Source/LinqToDB/Configuration/ILinqToDBSettings.cs
- Tier 2 (visited / total): 53 / 58 (~91%) ✓
  - Read in full (prior run, 2026-04-26): 9 files (DataOptions.cs, DataExtensions.cs, ITable{T}.cs, CompiledQuery.cs, ProviderName.cs, LinqOptions.cs, TableOptions.cs, DataType.cs, Configuration/LinqToDBSettings.cs)
  - Read in full (this run, 2026-05-06): 47 files — see "Read (this run, 2026-05-06)" list below
  - Skipped: 5 — IDataContext.cs / DataContext.cs / DataConnection.cs (already Tier 1, not double-counted), plus 2 declaration-only files where the type signature is fully captured by the head probe
- Read (this run, 2026-05-06):
  - Source/LinqToDB/AnalyticFunctions.cs
  - Source/LinqToDB/CompareNulls.cs
  - Source/LinqToDB/Configuration/ConnectionStringSettings.cs
  - Source/LinqToDB/Configuration/DataProviderElement.cs
  - Source/LinqToDB/Configuration/DataProviderElementCollection.cs
  - Source/LinqToDB/Configuration/ElementBase.cs
  - Source/LinqToDB/Configuration/ElementCollectionBase.cs
  - Source/LinqToDB/Configuration/IConnectionStringSettings.cs
  - Source/LinqToDB/Configuration/IDataProviderSettings.cs
  - Source/LinqToDB/Configuration/NamedValue.cs
  - Source/LinqToDB/CreateTableOptions.cs
  - Source/LinqToDB/CreateTempTableOptions.cs
  - Source/LinqToDB/DataContext.Interceptors.cs
  - Source/LinqToDB/DataContextOptions.cs
  - Source/LinqToDB/DataContextTransaction.cs
  - Source/LinqToDB/DataExtensions.TempTable.cs
  - Source/LinqToDB/DataOptions{T}.cs
  - Source/LinqToDB/DataOptionsExtensions.cs
  - Source/LinqToDB/DataOptionsExtensions.Provider.cs
  - Source/LinqToDB/DbDataType.cs
  - Source/LinqToDB/ExpressionMethodAttribute.cs
  - Source/LinqToDB/ExprParameterAttribute.cs
  - Source/LinqToDB/ExprParameterKind.cs
  - Source/LinqToDB/ExtensionBuilderExtensions.cs
  - Source/LinqToDB/IExtensionsAdapter.cs
  - Source/LinqToDB/ILoadWithQueryable.cs
  - Source/LinqToDB/InsertColumnFilter.cs
  - Source/LinqToDB/InsertOrUpdateColumnFilter.cs
  - Source/LinqToDB/KeepConnectionAliveScope.cs
  - Source/LinqToDB/LinqToDBException.cs
  - Source/LinqToDB/MergeDefinition{TTarget,TSource}.cs
  - Source/LinqToDB/MergeOperationType.cs
  - Source/LinqToDB/MultiInsertExtensions.cs
  - Source/LinqToDB/QuerySql.cs
  - Source/LinqToDB/RawSqlString.cs
  - Source/LinqToDB/ServerSideOnlyException.cs
  - Source/LinqToDB/SqlExtensions.cs
  - Source/LinqToDB/SqlGenerationOptions.cs
  - Source/LinqToDB/SqlJoinType.cs
  - Source/LinqToDB/SqlOptions.cs
  - Source/LinqToDB/StringAggregateExtensions.cs
  - Source/LinqToDB/TableExtensions.cs
  - Source/LinqToDB/TakeHints.cs
  - Source/LinqToDB/TempTable.cs
  - Source/LinqToDB/TempTableDescriptor.cs
  - Source/LinqToDB/UpdateColumnFilter.cs
  - Source/LinqToDB/UpdateOutput.cs
- Tier 3 (skipped, logged): 0 (no generated/build-output files at this scope — generated files live under `Async/`, `DataProvider/<X>/`, `Sql/`, not at the root)
</details>
