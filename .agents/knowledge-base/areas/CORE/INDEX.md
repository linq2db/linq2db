---
area: CORE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 6/6
coverage_tier_2: 57/58
---

# CORE

## What this area does

CORE owns the **top-level public-API surface** of the `linq2db` assembly -- the user-facing context types (`IDataContext`, `DataContext`, `DataConnection`), the immutable `DataOptions` configuration record, the `DataExtensions` entry-point methods (`GetTable<T>`, `Insert`, `Update`, `Delete`, `Query<T>`), the `ITable<T>` queryable handle, and the `LinqToDB.Configuration` configuration entry types. Everything in this area sits at the top of the dependency graph: it brokers between user code and every internal subsystem (LINQ pipeline, mapping schema, providers, SQL builder) without owning any of those pipelines itself. AST nodes, translator builders, and SQL emission live in their own areas -- see [SQL-AST](../SQL-AST/INDEX.md), [EXPR-TRANS](../EXPR-TRANS/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md), [LINQ](../LINQ/INDEX.md).

## Key types

- `IDataContext` -- the central context abstraction, exposing the SQL-builder factory, `MappingSchema`, `DataReaderType`, query hints, options, and the `GetQueryRunner` execution hook (`Source/LinqToDB/IDataContext.cs:22`).
- `DataContext` -- non-persistent context that opens an internal `DataConnection` lazily and disposes it after each query unless `KeepConnectionAlive` is set (`Source/LinqToDB/DataContext.cs:28`, lifecycle at `:391`, `:437`).
- `DataConnection` -- persistent connection; holds the `DbConnection` for the lifetime of the object and is the actual ADO.NET execution site (`Source/LinqToDB/Data/DataConnection.cs:32`). Lives in the `LinqToDB.Data` namespace and is documented in [DATA](../DATA/INDEX.md), but cross-listed here because it implements `IDataContext`.
- `DataOptions` -- immutable, sealed configuration record. Composes per-concern option sets: `LinqOptions`, `RetryPolicyOptions`, `ConnectionOptions`, `DataContextOptions`, `BulkCopyOptions`, `SqlOptions`. Implements `OptionsContainer<DataOptions>` with `WithOptions` for fluent overrides (`Source/LinqToDB/DataOptions.cs:17`, `:49`, `:90`). `Reapply` methods dispatch to `IReapplicable<T>` on each sub-option set or fall back to `Default` when `previousOptions` was non-null (`Source/LinqToDB/DataOptions.cs:118--147`).
- `DataExtensions` -- partial static class hosting the public extension surface on `IDataContext`: `GetTable<T>`, table-function dispatch, `Insert`/`Update`/`Delete` overloads (`Source/LinqToDB/DataExtensions.cs:29`, `:41`).
- `DataExtensions` (TempTable partial) -- `CreateTempTable<T>` / `CreateTempTableAsync<T>` and `IntoTempTable<T>` / `IntoTempTableAsync<T>` families; delegate to `TempTable<T>` ctors or `TempTable<T>.CreateAsync`. The `setTable` overloads call `GetTempTableDescriptor`, which temporarily pushes a new `MappingSchema` and returns a `TempTableDescriptor` carrying the old schema for rollback.
- `ITable<T>` -- `IExpressionQuery<T>`-derived handle from `dataContext.GetTable<T>()`; carries server/database/schema/table identity (`Source/LinqToDB/ITable{T}.cs:12`).
- `TempTable<T>` -- `ITable<T>` + `ITableMutable<T>` + `IDisposable`/`IAsyncDisposable`; creates the physical table on construction, optionally populates via BulkCopy or INSERT-SELECT, drops on `Dispose`/`DisposeAsync`. Restores the prior `MappingSchema` when a `TempTableDescriptor` is attached (`Source/LinqToDB/TempTable.cs:26`, `:839`, `:854`).
- `TempTableDescriptor` -- sealed record `(EntityDescriptor EntityDescriptor, MappingSchema PrevMappingSchema)` (`Source/LinqToDB/TempTableDescriptor.cs:5`).
- `CompiledQuery` -- caches a compiled `LambdaExpression` into a `Func<object?[], object?[]?, object?>`; lazy lock-protected init (`Source/LinqToDB/CompiledQuery.cs:25`, `:36`).
- `ProviderName` -- string-constant catalog of every provider/dialect identifier (`Source/LinqToDB/ProviderName.cs:23`). As of PR #5451 includes `DuckDB = "DuckDB"` (`:341`). The file-level TODO at `:18` notes provider-specific entries are scheduled for obsolescence in v6; `ProviderName` is intended for dialect names only going forward.
- `DataType`, `DbDataType`, `TableOptions`, `SqlJoinType`, `MergeOperationType`, `TakeHints` -- public enums/structs. `DbDataType` is a value struct `(SystemType, DataType, DbType, Length, Precision, Scale)` with fluent `WithXxx` copies (`Source/LinqToDB/DbDataType.cs:15`). Added `EqualsDbOnly(DbDataType other)` method that compares all fields except `SystemType` -- used when only DB-side type attributes are relevant (`Source/LinqToDB/DbDataType.cs:119--126`). `SqlJoinType` exposes Inner/Left/Right/Full. `TakeHints` is `[Flags]` supporting `Percent`/`WithTies`. `DataType` carries an `Array` flag bit and composite `Vector32`/`Vector16`; `[Flags]` is noted with `// ??? TODO: remove Flags in v7` (`Source/LinqToDB/DataType.cs:11`).
- `CompareNulls` -- enum: `LikeClr`, `LikeSql`, `LikeSqlExceptParameters` (`Source/LinqToDB/CompareNulls.cs:4`).
- `LinqOptions`, `SqlOptions`, `DataContextOptions` -- option records composing into `DataOptions`. `DataContextOptions` carries `CommandTimeout`, `Interceptors`, `MemberTranslators`; now implements both `IApplicable<T>` and `IReapplicable<T>` for `DataConnection`, `DataContext`, and `RemoteDataContextBase` (`Source/LinqToDB/DataContextOptions.cs:33--106`). `SqlOptions` provides `EnableConstantExpressionInOrderBy`, `GenerateFinalAliases`, and `DefaultNullsPosition` (`Source/LinqToDB/SqlOptions.cs:44--71`). `LinqOptions` carries `PreloadGroups`, `PreferApply`, and `KeepDistinctOrdered` as `[Obsolete]` no-op parameters (not included in `ConfigurationID` or copy constructor) (`Source/LinqToDB/LinqOptions.cs:158--201`).
- `DataOptions<T>` -- typed wrapper for DI registration, keyed by context type `T` (`Source/LinqToDB/DataOptions{T}.cs:7`). Marked `// TODO: move to linq2db.Extensions?`.
- `LinqToDBException`, `ServerSideOnlyException` -- the two public exception types. `LinqToDBException` zero-arg/single-`Exception` ctors are `[Obsolete]`/`EditorBrowsable(Never)` (`Source/LinqToDB/LinqToDBException.cs:20`). `ServerSideOnlyException` is thrown by server-side-only `Sql.*` members called outside a query context (`Source/LinqToDB/ServerSideOnlyException.cs:14`).
- `RawSqlString` -- readonly struct with implicit conversion from `string` and (no-op) from `FormattableString`, used to distinguish overloads of `DataExtensions.FromSql<T>` (`Source/LinqToDB/RawSqlString.cs:11`).
- `QuerySql` -- sealed class carrying generated SQL text and `IReadOnlyList<DataParameter>`; returned by `IExpressionQuery.GetSqlQueries()` (`Source/LinqToDB/QuerySql.cs:10`).
- `SqlGenerationOptions` -- options for `LinqExtensions.ToSqlQuery<T>`: `InlineParameters`, `MultiInsertMode` (`Source/LinqToDB/SqlGenerationOptions.cs:8`).
- `CreateTableOptions`, `CreateTempTableOptions` -- positional records for DDL creation. `CreateTempTableOptions` is a sealed subtype with default `TableOptions = TableOptions.IsTemporary`; the file comment forbids adding new parameters (`Source/LinqToDB/CreateTempTableOptions.cs:7`).
- `MergeDefinition<TTarget,TSource>` -- immutable builder for fluent MERGE. `AddSource<TNewSource>`, `AddOperation`, `AddOnPredicate`, `AddOnKey`; inner `Operation` record holds typed predicate/expression lambdas per `MergeOperationType` (`Source/LinqToDB/MergeDefinition{TTarget,TSource}.cs:10`).
- `MergeOperationType` -- enum: `None`, `Insert`, `Update`, `Delete`, `UpdateWithDelete`, `UpdateBySource`, `DeleteBySource`.
- `MultiInsertExtensions` -- static class for Oracle-specific multi-table INSERT ALL / INSERT FIRST. `MultiInsert(source)` -> `Into`/`When`/`Else` -> `Insert()`/`InsertAll()`/`InsertFirst()` (`Source/LinqToDB/MultiInsertExtensions.cs:16`).
- `ExpressionMethodAttribute` -- `[AttributeUsage(Property|Method, AllowMultiple=true)]`; substitutes the attributed member with an expression. `IsColumn=true` makes it a calculated column (`Source/LinqToDB/ExpressionMethodAttribute.cs:37`).
- `ExprParameterAttribute`, `ExprParameterKind` -- mark expression-method delegate params; `DoNotParameterize` suppresses SQL parameter generation.
- `ExtensionBuilderExtensions` -- static helpers for `Sql.ISqlExtensionBuilder`: `AddParameter`, `AddFragment`, and arithmetic AST node constructors (`Add`, `Sub`, `Mul`, `Div`, `Inc`, `Dec`, `BitNot`, `Negate`) emitting `SqlBinaryExpression` / `SqlUnaryExpression` (`Source/LinqToDB/ExtensionBuilderExtensions.cs:9`). PR #5504 added three `Concat` overloads producing `SqlConcatExpression`, and hardened `Add` to throw `InvalidOperationException` when `type == typeof(string)` -- string concatenation must go through `Concat(...)` (`:24--62`).
- `IExtensionsAdapter` -- interface for overriding default async LINQ terminals; used by EF Core integration (`Source/LinqToDB/IExtensionsAdapter.cs:13`).
- `ILoadWithQueryable<TEntity,TProperty>` -- marker interface for `LoadWith`/`ThenLoad` chains.
- `InsertColumnFilter<T>`, `InsertOrUpdateColumnFilter<T>`, `UpdateColumnFilter<T>` -- delegate types for per-column inclusion predicates.
- `UpdateOutput<T>` -- `Deleted`/`Inserted` properties capturing OUTPUT clause rows.
- `KeepConnectionAliveScope` -- `IDisposable`/`IAsyncDisposable` RAII wrapper toggling `DataContext.KeepConnectionAlive`.
- `DataContextTransaction` -- explicit transaction wrapper for `DataContext`.
- `SqlExtensions` -- `In<T>` / `NotIn<T>` wired via `[ExpressionMethod]`.
- `StringAggregateExtensions` -- `OrderBy`/`ThenBy`/`ToValue` for `Sql.IAggregateFunction*` chaining; builds `WITHIN GROUP (ORDER BY ...)` (`Source/LinqToDB/StringAggregateExtensions.cs:13`).
- `AnalyticFunctions` -- large public static class: `ROW_NUMBER`, `RANK`, window aggregates, lead/lag, etc. via `[Sql.Extension]` (`Source/LinqToDB/AnalyticFunctions.cs:16`).
- `TableExtensions` -- `IsTemporary<T>`, `TableOptions<T>`, `GetTableName<T>` on `ITable<T>`.
- `ILinqToDBSettings`, `LinqToDBSettings`, `LinqToDBSection` -- configuration entry types under `LinqToDB.Configuration`. `LinqToDBSection` is the legacy `app.config` section, gated `NETFRAMEWORK || COMPAT` (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1`).
- `IAsQueryableBuilder<T>`, `IAsQueryableExceptBuilder<T>` -- new public interfaces (PR #5495) in `LinqToDB.Linq` parameterizing the `LinqExtensions.AsQueryable<T>(source, dataContext, configure)` overload. `IAsQueryableBuilder<T>` requires `Parameterize()` or `Inline()`; `IAsQueryableExceptBuilder<T>` exposes `Except(...)` (`Source/LinqToDB/Linq/IAsQueryableBuilder.cs:15`, `Source/LinqToDB/Linq/IAsQueryableExceptBuilder.cs:12`).

## Sql class -- timestamp and date/time functions

`Sql.cs` exposes server-side date/time properties (`Source/LinqToDB/Sql/Sql.cs:1166`--`:1198`):

- `Sql.GetDate()` -- emits `CURRENT_TIMESTAMP` (provider equivalents: `GetDate()` Sybase/SQL CE, `Now` Access, `CURRENT` Informix); client returns `DateTime.Now`.
- `Sql.CurrentTimestamp` -- server-side-only; throws `ServerSideOnlyException` outside a query.
- `Sql.CurrentTimestampUtc` -- evaluates `DateTime.UtcNow` client-side; server SQL is provider-dispatched via translator virtuals from PR #5467 (`TranslateServerNow`, `TranslateNow`, `TranslateUtcNow`, `TranslateZonedNow`, `TranslateZonedUtcNow`).
- `Sql.CurrentTimestamp2` -- dual-mode: server SQL as `CurrentTimestamp`, in-process `DateTime.Now`.
- `Sql.CurrentTzTimestamp` -- `DateTimeOffset.Now` client-side; provider-specific zoned timestamp server-side.

PR #5517 fixed `DateTime.Date` truncation: the cast to `date` preserves the source column's `DbType`.

## Configuration subsystem (LinqToDB.Configuration)

All `Configuration/` types are TFM-gated: the five `System.Configuration`-based types exist only under `NETFRAMEWORK || COMPAT`; the interfaces and `NamedValue` are unconditional. The `app.config` provider key is `string.Concat(element.Name, "/", element.TypeName)` (`Source/LinqToDB/Configuration/DataProviderElementCollection.cs:19`).

## DataOptionsExtensions

Split across two files. The main file covers `LinqOptions`, `SqlOptions`, `ConnectionOptions`, `DataContextOptions`, `BulkCopyOptions`, and helpers (`UseConnectionString`, `UseProvider`, `UseInterceptor`, etc.). The provider file (`DataOptionsExtensions.Provider.cs`) contains `Use<Database>` overloads for every supported provider, two overloads for single-dialect providers and four for multi-dialect. Provider overloads delegate to `<Provider>Tools.ProviderDetector.CreateOptions(...)` then apply the `optionSetter`. DuckDB is single-dialect (two overloads), delegating to `DuckDBTools.GetDataProvider()` directly (no `ProviderDetector`) (`Source/LinqToDB/DataOptionsExtensions.Provider.cs:999`). Added `WithDefaultNullsPosition(this SqlOptions, Sql.NullsPosition)` and `UseDefaultNullsPosition(this DataOptions, Sql.NullsPosition)` extension methods (`Source/LinqToDB/DataOptionsExtensions.cs:1965`, `:2024`).

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**

- `Source/LinqToDB/IDataContext.cs`
- `Source/LinqToDB/DataContext.cs`
- `Source/LinqToDB/Data/DataConnection.cs` (head + ConfigurationApplier sample; full file lives in DATA scope)
- `Source/LinqToDB/LinqToDB.csproj`
- `Source/LinqToDB/Configuration/LinqToDBSection.cs`
- `Source/LinqToDB/Configuration/ILinqToDBSettings.cs`

**Tier 2 target -- `Source/LinqToDB/*.cs` (root) + `Source/LinqToDB/Configuration/*.cs`:** see Coverage block for the full read list across prior runs + this delta.

## Inbound dependencies

- [LINQ](../LINQ/INDEX.md) -- `Query<T>`, `IQueryRunner`, `ExpressionQuery<T>` consume `IDataContext.GetQueryRunner`, `MappingSchema`, `Options`.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- `ExpressionBuilder` reads `DataOptions.LinqOptions`, dispatches via the context's `MappingSchema`.
- [DATA](../DATA/INDEX.md) -- `DataConnection` internals sit on top of `IDataContext`/`DataOptions`.
- Every `PROV-*` area -- provider entry points plug into `IDataContext` via the `IDataProvider` contract.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- `IDataContext.AddInterceptor` / `RemoveInterceptor`.
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md), [EFCORE](../EFCORE/INDEX.md), [TOOLS](../TOOLS/INDEX.md), [LINQPAD](../LINQPAD/INDEX.md) -- consume `DataConnection` / `DataContext` / `DataOptions`.

## Outbound dependencies

- [MAPPING](../MAPPING/INDEX.md) via `MappingSchema` -- `IDataContext.MappingSchema`, `DataContext.AddMappingSchema`, `IDataContext.SetMappingSchema`.
- [LINQ](../LINQ/INDEX.md) via `Query`/`IQueryRunner`/`IQueryExpressions`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) via `Func<ISqlBuilder>` / `Func<DataOptions, ISqlOptimizer>` factory delegates (`IDataContext.cs:31`,`:35`); `DataContext` resolves both through `DataProvider.CreateSqlBuilder` / `DataProvider.GetSqlOptimizer`.
- [SQL-AST](../SQL-AST/INDEX.md) -- `ExtensionBuilderExtensions` directly constructs `SqlBinaryExpression`, `SqlUnaryExpression`, `SqlValue`, `SqlFragment`, and now `SqlConcatExpression` nodes.
- [INFRA](../INFRA/INDEX.md) -- `LinqToDB.Common`, `LinqToDB.Internal.Common`, `LinqToDB.Metrics`.
- `DataProvider` namespace -- `IDataProvider` interface contract.

## Recurring patterns

- **Immutable-record options pattern.** `DataOptions` is sealed, `ICloneable`, overrides `WithOptions(IOptionSet)`. Sub-option records follow the same `[Pure] WithX(...)` shape. Mutating static `Default` on `DataContextOptions`/`SqlOptions` triggers `DataConnection.ResetDefaultOptions()`.
- **Nested `ConfigurationApplier` static class.** Both `DataContext` (`DataContext.cs:726`) and `DataConnection` host an `internal static class ConfigurationApplier` with paired `Apply`/`Reapply`; `Reapply` enforces "not changeable dynamically" rules and returns an undo `Action` consumed by `UseOptions`.
- **`AssertDisposed` discipline.** Every public mutator on `DataContext` starts with `AssertDisposed()`.
- **TFM-conditional configuration entry.** `LinqToDBSection` gated `#if NETFRAMEWORK && COMPAT` (`[TypeForwardedTo]`) / `NETFRAMEWORK || COMPAT` (body); modern callers use `DataOptions` directly.
- **Obsolete-with-`v7`-marker convention.** Legacy ctors/properties carry `[Obsolete("...removal in v7...")]` + `EditorBrowsable(Never)` + a `// TODO: Remove in v7` comment.
- **`DataContext.Interceptors.cs` partial pattern.** Eight interceptor aggregators stored as nullable fields; `AddInterceptor` dispatches by type, creating `AggregatedInterceptor<TI>` lazily.
- **`MergeDefinition` immutable-builder pattern.** Every fluent method returns a new instance via the private all-parameters constructor.
- **`TempTable<T>` lifecycle contract.** Construction creates the physical table; population is wrapped in try/catch with drop-on-failure; `Dispose`/`DisposeAsync` always `DropTable(throwExceptionIfNotExists: false)` then restores `MappingSchema`.
- **`AsQueryable` expression-tree configuration pattern.** `AsQueryable<T>(source, dataContext, configure)` (PR #5495) takes `configure` as `Expression<Func<IAsQueryableBuilder<T>, IAsQueryableExceptBuilder<T>>>`; the lambda is captured as a tree, interpreted at build time by `Methods.LinqToDB.AsQueryableConfigured`.
- **`ExtensionBuilderExtensions` concat/arithmetic split.** PR #5504 enforced a hard separation: numeric arithmetic (`Add`/`Sub`/`Mul`/`Div`/`Inc`/`Dec` -> `SqlBinaryExpression`) vs string concatenation (`Concat` -> `SqlConcatExpression`). `Add(builder, left, right, type)` throws `InvalidOperationException` when `type == typeof(string)` (`:24--25`). The three `Concat` overloads delegate to `new SqlConcatExpression(preserveNull, ...)` with `preserveNull = true` unless explicitly `false` (`:46--77`).
- **`IReapplicable<T>` on sub-option records.** `DataContextOptions` implements both `IApplicable<T>` and `IReapplicable<T>` for all three context types; `DataOptions.Reapply` dispatches through `IReapplicable<T>` and falls back to `DataContextOptions.Default` when previous was non-null but new record does not implement the interface (`Source/LinqToDB/DataOptions.cs:118--147`).

## Known issues / debt

- `DataOptions<T>` has `// TODO: move to linq2db.Extensions?` (`Source/LinqToDB/DataOptions{T}.cs:7`).
- `CreateTempTableOptions` forbids new parameters (`Source/LinqToDB/CreateTempTableOptions.cs:7`).
- Multiple `DataOptionsExtensions.cs` APIs are `[Obsolete]` + no-op (e.g. `WithPreloadGroups`, `WithPreferApply`, `WithKeepDistinctOrdered`) pending v7 cleanup.
- `DataType` carries `[Flags]` with `// ??? TODO: remove Flags in v7` (`Source/LinqToDB/DataType.cs:11`).
- `ProviderName` has `// TODO: v6: obsolete/remove all provider-specific entries` (`Source/LinqToDB/ProviderName.cs:18`).
- DI-0743: 18 public extension methods on `ExtensionBuilderExtensions` (the arithmetic/parameter helpers) lack `<summary>` XML docs; the three `Concat` overloads do carry docs.

## Pointers

- Cross-area orientation: [`architecture/overview.md`](../../architecture/overview.md), [`architecture/public-api.md`](../../architecture/public-api.md), [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md).
- Design invariants: [`code-design.md`](../../code-design.md) -> **Public API is a contract**, **Cross-cutting internals are shared**, **Column-aligned formatting is intentional** (preserve alignment in `IDataContext.cs:27--73`, `DataContext.cs:108--138`).
- Companion areas: [DATA](../DATA/INDEX.md), [MAPPING](../MAPPING/INDEX.md), [LINQ](../LINQ/INDEX.md), [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 6 / 6 done
  - Source/LinqToDB/IDataContext.cs
  - Source/LinqToDB/DataContext.cs
  - Source/LinqToDB/Data/DataConnection.cs (head + ConfigurationApplier; full body cross-listed under DATA)
  - Source/LinqToDB/LinqToDB.csproj
  - Source/LinqToDB/Configuration/LinqToDBSection.cs
  - Source/LinqToDB/Configuration/ILinqToDBSettings.cs
- Tier 2 (visited / total): 57 / 58 (~98%) done
  - Read in full (prior runs 2026-04-26 / 2026-05-06): DataOptions.cs, DataExtensions.cs, ITable{T}.cs, CompiledQuery.cs, ProviderName.cs, LinqOptions.cs, TableOptions.cs, DataType.cs, Configuration/LinqToDBSettings.cs, AnalyticFunctions.cs, CompareNulls.cs, Configuration/*.cs, CreateTableOptions.cs, CreateTempTableOptions.cs, DataContext.Interceptors.cs, DataContextOptions.cs, DataContextTransaction.cs, DataExtensions.TempTable.cs, DataOptions{T}.cs, DataOptionsExtensions.cs, DataOptionsExtensions.Provider.cs, DbDataType.cs, ExpressionMethodAttribute.cs, ExprParameterAttribute.cs, ExprParameterKind.cs, ExtensionBuilderExtensions.cs, IExtensionsAdapter.cs, ILoadWithQueryable.cs, InsertColumnFilter.cs, InsertOrUpdateColumnFilter.cs, KeepConnectionAliveScope.cs, LinqToDBException.cs, MergeDefinition{TTarget,TSource}.cs, MergeOperationType.cs, MultiInsertExtensions.cs, QuerySql.cs, RawSqlString.cs, ServerSideOnlyException.cs, SqlExtensions.cs, SqlGenerationOptions.cs, SqlJoinType.cs, SqlOptions.cs, StringAggregateExtensions.cs, TableExtensions.cs, TakeHints.cs, TempTable.cs, TempTableDescriptor.cs, UpdateColumnFilter.cs, UpdateOutput.cs
  - Read (2026-05-11 delta): AnalyticFunctions.cs, DataOptionsExtensions.Provider.cs (DuckDB overloads), DataType.cs, ProviderName.cs (DuckDB constant), Sql/Sql.cs, Sql/Sql.DateTime.cs (DateTime.Date DbType preservation), Sql/Sql.DateOnly.cs, Sql/Sql.DateTimeOffset.cs, Linq/IAsQueryableBuilder.cs (new), Linq/IAsQueryableExceptBuilder.cs (new), Linq/Expressions.cs, CompatibilitySuppressions.xml, PublicAPI/PublicAPI.Unshipped.txt
  - Read (this run -- delta, sha 2e67bafc9):
    - Source/LinqToDB/ExtensionBuilderExtensions.cs -- PR #5504 added `Concat(builder, x, y)`, `Concat(builder, params ISqlExpression[])`, `Concat(builder, bool preserveNull, params ISqlExpression[])` producing `SqlConcatExpression(preserveNull, ...)`; `Add(builder, left, right, type)` now throws when `type == typeof(string)` to prevent accidental `SqlBinaryExpression` for string concat.
    - Source/LinqToDB/CompatibilitySuppressions.xml -- regenerated for release; header probe only (generated file, content not detailed per indexer policy).
  - Read (this run -- delta, sha b3340aa9):
    - Source/LinqToDB/SqlOptions.cs -- added `DefaultNullsPosition` property (`Sql.NullsPosition`, default `None`); included in `ConfigurationID` computation and copy constructor; `WithDefaultNullsPosition` extension added in DataOptionsExtensions.
    - Source/LinqToDB/DataOptionsExtensions.cs -- added `WithDefaultNullsPosition(this SqlOptions, Sql.NullsPosition)` (~line 1965) and `UseDefaultNullsPosition(this DataOptions, Sql.NullsPosition)` (~line 2024) extension methods.
    - Source/LinqToDB/DataContextOptions.cs -- now implements `IReapplicable<DataConnection>`, `IReapplicable<DataContext>`, `IReapplicable<RemoteDataContextBase>` in addition to the existing `IApplicable<T>` variants; each `IReapplicable.Apply` short-circuits when `ConfigurationID` matches prior object.
    - Source/LinqToDB/DataOptions.cs -- `Reapply(DataConnection/DataContext/RemoteDataContextBase, DataOptions)` now dispatches through `IReapplicable<T>` on `DataContextOptions`; falls back to `DataContextOptions.Default` when previous options was non-null but the new record does not implement the interface.
    - Source/LinqToDB/DbDataType.cs -- added `EqualsDbOnly(DbDataType other)` method comparing `DataType`, `Length`, `Precision`, `Scale`, and `DbType` fields only, excluding `SystemType`; used when only DB-side type attributes are relevant.
    - Source/LinqToDB/LinqOptions.cs -- `PreloadGroups`, `PreferApply`, and `KeepDistinctOrdered` parameters are now `[Obsolete]` no-ops; they are excluded from the copy constructor and `ConfigurationID` computation (only non-obsolete fields hashed).
  - Skipped: 1 -- declaration-only file fully captured by the head probe
- Tier 3 (skipped, logged): 0
</details>
