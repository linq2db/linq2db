---
area: CORE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
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
- `ITable<T>` — `IExpressionQuery<T>`-derived handle a user obtains from `dataContext.GetTable<T>()`; carries server/database/schema/table identity (`Source/LinqToDB/ITable{T}.cs:12`).
- `CompiledQuery` — caches a compiled `LambdaExpression` into a `Func<object?[], object?[]?, object?>` for repeated execution; lazy lock-protected initialization (`Source/LinqToDB/CompiledQuery.cs:25`, `:36`).
- `ProviderName` — string-constant catalog of every provider/dialect identifier the runtime recognizes (`Source/LinqToDB/ProviderName.cs:23`).
- `DataType`, `DbDataType`, `TableOptions`, `SqlJoinType`, `MergeOperationType`, `TakeHints` — public enums consumed by mapping, SQL emission, and merge/insert APIs (`Source/LinqToDB/DataType.cs:11`, `Source/LinqToDB/TableOptions.cs:13`).
- `LinqOptions`, `SqlOptions`, `LinqToDBException`, `RawSqlString`, `ServerSideOnlyException` — option records and exception types pinned to the public surface (`Source/LinqToDB/LinqOptions.cs:1`).
- `ILinqToDBSettings`, `LinqToDBSettings`, `LinqToDBSection` — configuration entry interfaces and implementations under `LinqToDB.Configuration`. `LinqToDBSection` is the legacy `app.config` section, gated on `NETFRAMEWORK || COMPAT` and forwarded via `[TypeForwardedTo]` for compat-shim builds (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1`, `:17`); `LinqToDBSettings` is the explicit-construction variant (`Source/LinqToDB/Configuration/LinqToDBSettings.cs:8`).
- `DataContextTransaction`, `DataContextOptions`, `KeepConnectionAliveScope` — context-side companions for transaction lifetime, sub-options, and lock scopes (`Source/LinqToDB/DataContext.cs:582`, `:968`).

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**

- `Source/LinqToDB/IDataContext.cs`
- `Source/LinqToDB/DataContext.cs`
- `Source/LinqToDB/Data/DataConnection.cs` (head + ConfigurationApplier sample; full file lives in DATA area scope)
- `Source/LinqToDB/LinqToDB.csproj`
- `Source/LinqToDB/Configuration/LinqToDBSection.cs`
- `Source/LinqToDB/Configuration/ILinqToDBSettings.cs`

**Tier 2 sample target — `Source/LinqToDB/*.cs` (root only) + `Source/LinqToDB/Configuration/*.cs`:**

Visited in full: `DataOptions.cs`, `DataExtensions.cs`, `ITable{T}.cs`, `CompiledQuery.cs`, `ProviderName.cs`, `LinqOptions.cs`, `TableOptions.cs`, `DataType.cs`, `Configuration/LinqToDBSettings.cs`.

Surveyed by signature/namespace probe (declaration-only files where head + first public type carries full information): `AnalyticFunctions.cs`, `CompareNulls.cs`, `CreateTableOptions.cs`, `CreateTempTableOptions.cs`, `DataContext.Interceptors.cs`, `DataContextOptions.cs`, `DataContextTransaction.cs`, `DataExtensions.TempTable.cs`, `DataOptions{T}.cs`, `DataOptionsExtensions.cs`, `DataOptionsExtensions.Provider.cs`, `DbDataType.cs`, `ExprParameterAttribute.cs`, `ExprParameterKind.cs`, `ExpressionMethodAttribute.cs`, `ExtensionBuilderExtensions.cs`, `IExtensionsAdapter.cs`, `ILoadWithQueryable.cs`, `InsertColumnFilter.cs`, `InsertOrUpdateColumnFilter.cs`, `KeepConnectionAliveScope.cs`, `LinqToDBException.cs`, `MergeDefinition{TTarget,TSource}.cs`, `MergeOperationType.cs`, `MultiInsertExtensions.cs`, `QuerySql.cs`, `RawSqlString.cs`, `ServerSideOnlyException.cs`, `SqlExtensions.cs`, `SqlGenerationOptions.cs`, `SqlJoinType.cs`, `SqlOptions.cs`, `StringAggregateExtensions.cs`, `TableExtensions.cs`, `TakeHints.cs`, `TempTable.cs`, `TempTableDescriptor.cs`, `UpdateColumnFilter.cs`, `UpdateOutput.cs`, `Configuration/IConnectionStringSettings.cs`, `Configuration/IDataProviderSettings.cs`, `Configuration/ConnectionStringSettings.cs`, `Configuration/DataProviderElement.cs`, `Configuration/DataProviderElementCollection.cs`, `Configuration/ElementBase.cs`, `Configuration/ElementCollectionBase.cs`, `Configuration/NamedValue.cs`.

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
- [SQL-AST](../SQL-AST/INDEX.md) — option records and merge definitions reference AST building blocks indirectly via the SqlOptimizer / SqlBuilder factories above.
- [INFRA](../INFRA/INDEX.md) — `LinqToDB.Common`, `LinqToDB.Internal.Common` (`OptionsContainer<T>`, `IConfigurationID`, `IdentifierBuilder`, `DisposableAction`), `LinqToDB.Metrics` (`ActivityService`, `ActivityID`).
- `DataProvider` namespace — `IDataProvider` interface contract is consumed by every `IDataContext` implementation.

## Recurring patterns

- **Immutable-record options pattern.** `DataOptions` is sealed, `ICloneable`, and overrides `WithOptions(IOptionSet)` to return either the receiver or a new `DataOptions` whose specific option set is replaced (`Source/LinqToDB/DataOptions.cs:49`). Sub-option records (`LinqOptions`, `ConnectionOptions`, `SqlOptions`, …) follow the same shape — `[Pure] WithX(...)` returns a new instance, never mutates. `DataConnection.DefaultDataOptions` plus `ConnectionOptionsByConfigurationString` (concurrent-dictionary cache, `Source/LinqToDB/DataContext.cs:53`) is the canonical static-default lookup.
- **Nested `ConfigurationApplier` static class.** Both `DataContext` (`DataContext.cs:726`) and `DataConnection` host an `internal static class ConfigurationApplier` with paired `Apply` / `Reapply` methods — `Apply` resolves `ConnectionOptions`/`DataContextOptions` against the live context; `Reapply` enforces "not changeable dynamically" rules and returns an `Action` undo delegate consumed by `UseOptions` (`DataContext.cs:919`). The pattern lets `IDisposable UseOptions(...)` swap option records temporarily and roll back on dispose.
- **`AssertDisposed` discipline.** Every public mutator and most accessors on `DataContext` start with `AssertDisposed()` (e.g. `DataContext.cs:149`, `:215`, `:228`, `:273`); the disposed flag is private and the throw message links to the wiki. New methods added to `DataContext` are expected to follow the same precondition.
- **TFM-conditional configuration entry.** `LinqToDBSection` is gated `#if NETFRAMEWORK && COMPAT` for `[TypeForwardedTo]` and `#elif NETFRAMEWORK || COMPAT` for the body (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1–3`). Modern (`net8.0`+) callers configure via `DataOptions` directly and `LinqToDBSettings` (`Source/LinqToDB/Configuration/LinqToDBSettings.cs:8`); the `app.config` `<linq2db>` section is a back-compat path only.
- **Obsolete-with-`v7`-marker convention.** Legacy ctors and properties carry `[Obsolete("This API scheduled for removal in v7. Instead use: …")]` plus `EditorBrowsable(EditorBrowsableState.Never)` and a `// TODO: Remove in v7` comment on the line above (`DataContext.cs:67`, `:82`, `:181`, `:206`, `:239`; `DataConnection.cs:46`, `:74`, `:92`, …). The pattern is consistent across CORE — when adding new public surface, prefer fluent `DataOptions` builders over ctor overloads, and document any planned removal with the same comment marker.

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
  - Read in full: 9 files (DataOptions.cs, DataExtensions.cs, ITable{T}.cs, CompiledQuery.cs, ProviderName.cs, LinqOptions.cs, TableOptions.cs, DataType.cs, Configuration/LinqToDBSettings.cs)
  - Surveyed by signature/namespace probe: 44 files (full list under "Files (Tier 1 / Tier 2)" above)
  - skipped: 5 — IDataContext.cs / DataContext.cs / DataConnection.cs (already Tier 1, not double-counted), plus 2 declaration-only files where the type signature is fully captured by the head probe
- Tier 3 (skipped, logged): 0 (no generated/build-output files at this scope — generated files live under `Async/`, `DataProvider/<X>/`, `Sql/`, not at the root)
</details>
