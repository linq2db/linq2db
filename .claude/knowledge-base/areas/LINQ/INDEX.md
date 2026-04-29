---
area: LINQ
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-27
last_verified_sha: a198cb803e80ae740cfc6afc7687536e78cd6cf2
coverage_tier_1: 5/5
coverage_tier_2: 46/46
---

# LINQ — query caching, executor lifecycle, and the `IQueryable` adapter

The execution and caching half of the LINQ pipeline. Owns the per-entity-type query cache, the compiled executor delegates (`GetElement`, `GetElementAsync`, `GetResultEnumerable`), the `IQueryable<T>` / `IQueryProvider` adapter exposed to user code, and the ADO.NET execution loop that runs `IQueryRunner` against a `DbDataReader` and drives the materialization mapper. Translation of the LINQ expression tree into `SqlStatement` lives in the EXPR-TRANS area (`Source/LinqToDB/Internal/Linq/Builder/**`) and is invoked from here exactly once per cache miss, via `ExpressionBuilder.Build<T>` in `Query<T>.CreateQuery` (`Source/LinqToDB/Internal/Linq/Query{T}.cs:396`).

## Key types

- **`Query`** (`Source/LinqToDB/Internal/Linq/Query.cs:22`) — abstract base. Holds the `QueryInfo[]` shards (one per `SqlStatement` to execute), the cached `ConfigurationID` / `ContextType` / `MappingSchema` / `SqlProviderFlags` / `DataOptions` snapshot taken at build time, the `QueryCacheCompareInfo` used for cache-key comparison, the `ParameterAccessors` list that binds runtime values into `SqlParameter`s, and the `Preamble[]` for eager-loading prefetches. `GetElement` / `GetElementAsync` are the executor delegates set by `QueryRunner.Set*Query` once per build.
- **`Query<T>`** (`Source/LinqToDB/Internal/Linq/Query{T}.cs:18`) — sealed generic. Owns the static per-type `QueryCache` (max 100 entries, copy-on-write reordering by hit priority — see `## Subsystems` below). `GetQuery` is the cache-lookup entry point: it runs `BinaryExpressionAggregatorVisitor`, `IExpressionPreprocessor`, the `IQueryExpressionInterceptor`, then probes the cache; on miss it runs `ExposeAndPrepareExpression` (which fixed-points the exposer + interceptor up to 10 iterations) and probes a second time with the `QueryFlags.ExpandedQuery` bit set before falling through to `CreateQuery`.
- **`ExpressionQuery<T>`** (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:19`) — abstract `IExpressionQuery<T>` / `IAsyncEnumerable<T>` adapter. This is the type the public `IQueryable<T>` surface materializes to: it implements `IQueryProvider.CreateQuery` / `Execute` / `IQueryProviderAsync.ExecuteAsync` / `ExecuteAsyncEnumerable` and the synchronous / async `IEnumerable<T>` / `IAsyncEnumerable<T>` traversals. Concrete subclasses are `Table<T>`, `CteTable<T>`, `ExpressionQueryImpl<T>`.
- **`IQueryRunner`** (`Source/LinqToDB/Internal/Linq/IQueryRunner.cs:11`) — the per-execution ADO.NET handle owned by `IDataContext`. Provides sync + async `ExecuteNonQuery` / `ExecuteScalar` / `ExecuteReader` and exposes `DataContext`, `Expressions`, `Parameters`, `Preambles`, `MapperExpression`, `RowsCount`, `QueryNumber`. Concrete implementation lives in DATA (`DataConnection.QueryRunner`, owned by `IDataContext.GetQueryRunner`).
- **`QueryRunnerBase`** (`Source/LinqToDB/Internal/Linq/QueryRunnerBase.cs:11`) — abstract default plumbing for `IQueryRunner`: holds the parameter / preamble snapshot, calls `QueryRunner.SetParameters` to bind a `SqlParameterValues`, defers the actual command shaping to `SetQuery(IReadOnlyParameterValues, bool forGetSqlText)`. `Dispose` closes `IDataContext` when `CloseAfterUse` is set.
- **`QueryRunner`** (`Source/LinqToDB/Internal/Linq/QueryRunner.cs:33`) — static partial. The hub between a built `Query` and the runtime executor: assembles `Mapper<T>` (a per-data-reader-type `Func<IQueryRunner, DbDataReader, T>` cache with slow-mode fallback on `FormatException` / `InvalidCastException` / `*NullValueException`), wires `query.GetResultEnumerable` / `query.GetElement` via `SetRunQuery` / `SetScalarQuery` / `SetNonQueryQuery` / `SetNonQueryQuery2` / `SetQueryQuery2`, and provides per-operation entry points (`Insert<T>`, `Update<T>`, `Delete<T>`, `InsertOrReplace<T>`, `InsertWithIdentity<T>`, `CreateTable<T>`, `DropTable<T>` — see partial files).
- **`QueryInfo`** (`Source/LinqToDB/Internal/Linq/QueryInfo.cs:11`) — one shard per `SqlStatement` produced for a single LINQ query. Most queries have one; `SetNonQueryQuery2` / `SetQueryQuery2` paths produce two (e.g. update-or-insert that may run a fallback statement). Implements `IQueryContext`.
- **`QueryCacheCompareInfo`** (`Source/LinqToDB/Internal/Linq/QueryCacheCompareInfo.cs:8`) — the key structure for cache equality. Holds the canonical `MainExpression`, optional `DynamicAccessors` (satellite expressions that need re-fetching from the live `IDataContext` to compare), and optional `ComparisionFunctions` (compiled value-equality probes). `IsFastComparable` is `true` when neither list is needed — `ExpressionQuery<T>` only memoizes a `Query<T>` reference into its `Info` field when this flag holds (`ExpressionQuery.cs:93`).
- **`ExpressionCacheManager`** (`Source/LinqToDB/Internal/Linq/ExpressionCacheManager.cs:21`) — the build-time companion to `QueryCacheCompareInfo`. Tracks accessor mappings (`AccessorsMapping`), parameter cache entries (`_parameterEntries`), duplicate-parameter checks, by-value comparisons, and `_bySqlValueCompare` registrations; `BuildQueryCacheCompareInfo` finalizes them into the `Query.CompareInfo` + `Query.ParameterAccessors` actually stored on the cached `Query` (`ExpressionCacheManager.cs:412`).
- **`IQueryExpressions`** / **`RuntimeExpressionsContainer`** (`Source/LinqToDB/Internal/Linq/IQueryExpressions.cs:5`, `RuntimeExpressionsContainer.cs:9`) — wrapper that decouples `MainExpression` (the user's tree) from satellite expressions referenced by id. Used everywhere a `ParameterAccessor` or `DynamicExpressionInfo` needs to retrieve "the current value of subtree N from the live request".
- **`ParameterAccessor`** / **`ParameterCacheEntry`** (`ParameterAccessor.cs:13`, `ParameterCacheEntry.cs:5`) — runtime side and build-time side of a `SqlParameter`'s value pipeline. `ParameterAccessor.ClientValueAccessor(IQueryExpressions, IDataContext?, object?[]?) -> object?` extracts the user-visible value from the expression tree; `ClientToProviderConverter` / `ItemAccessor` / `DbDataTypeAccessor` map it to the provider-shaped value + type.
- **`Preamble`** (`Source/LinqToDB/Internal/Linq/Preamble.cs:9`) — a deferred prefetch executed before the main query. Eager loading materializes one preamble per `LoadWith` group; `Query.InitPreambles[Async]` runs them in array order and threads the previous results in via the `object[]? preambles` argument so a later preamble can reference earlier ones.
- **`Table<T>`** (`Source/LinqToDB/Internal/Linq/Table{T}.cs:13`) — concrete `ITable<T>` implementation. Builds its own initial expression tree from `Methods.LinqToDB.GetTable.MakeGenericMethod(T)` against an `SqlQueryRootExpression`, then layers `TableName` / `SchemaName` / `ServerName` / `DatabaseName` / `TableID` / `TableOptions` / `UseTableDescriptor` calls on top via the `Apply*` helpers as user code mutates the table.
- **`CompiledTable<T>`** (`Source/LinqToDB/Internal/Linq/CompiledTable{T}.cs:16`) — backs `CompiledQuery`. Caches a `Query<T>` keyed by `(operation, configurationID, expression, queryFlags)` in the global `QueryRunner.Cache<T>.QueryCache` (`MemoryCache<IStructuralEquatable, Query<T>>`), rewriting `*Async` method calls in the lambda body to their sync counterparts so the compiled tree can be re-used for both modes (`CompiledTable{T}.cs:28`).
- **`IExpressionCacheKey`** (`Source/LinqToDB/Internal/Linq/IExpressionCacheKey.cs:13`) — opt-in marker for constants that should *participate* in the cache key (used by `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` to decide whether to replace the constant with a `ConstantPlaceholderExpression`, which would erase its value from the comparison).

## Files (Tier 1 / Tier 2)

### Tier 1 (5)

| Path | Purpose |
|---|---|
| `Source/LinqToDB/Internal/Linq/Query.cs` | abstract `Query`, cache-cleaner registry, preamble execution |
| `Source/LinqToDB/Internal/Linq/Query{T}.cs` | typed `Query<T>` + per-type `QueryCache` + `GetQuery` cache-lookup entry point |
| `Source/LinqToDB/Internal/Linq/IQueryRunner.cs` | runtime ADO.NET handle contract |
| `Source/LinqToDB/Internal/Linq/QueryRunner.cs` | mapper assembly, executor delegate wiring, `BasicResultEnumerable` |
| `Source/LinqToDB/Internal/Linq/ExpressionQuery.cs` | `IQueryable<T>` / `IQueryProvider` adapter |

### Tier 2 (46)

Cache + key:
- `ExpressionCacheManager.cs` — build-time accessor / parameter / value-equality registration
- `ExpressionCacheHelpers.cs` — `ShouldRemoveConstantFromCache` rule
- `IExpressionCacheKey.cs` — opt-in marker for value-keyed constants
- `QueryCacheCompareInfo.cs` — runtime comparison structure
- `ParameterAccessor.cs` / `ParameterCacheEntry.cs` — parameter binding
- `QueryFlags.cs` / `QueryFlagsHelper.cs` — `InlineParameters` / `ExpandedQuery` flag bits used as part of cache key

Executor + runtime:
- `QueryRunnerBase.cs` — abstract `IQueryRunner` plumbing for provider impls
- `QueryRunner.CreateTable.cs`, `QueryRunner.DropTable.cs`, `QueryRunner.Insert.cs`, `QueryRunner.InsertOrReplace.cs`, `QueryRunner.InsertWithIdentity.cs`, `QueryRunner.Update.cs`, `QueryRunner.Delete.cs` — per-CRUD-op `Query<int>` / `Query<object>` builders + `MemoryCache` wiring
- `QueryInfo.cs` / `IQueryContext.cs` — per-statement shard
- `IResultEnumerable.cs` — combined `IEnumerable<T>` / `IAsyncEnumerable<T>` returned by `Query<T>.GetResultEnumerable`
- `LimitResultEnumerable.cs` — fallback Skip/Take wrapper used when the provider lacks `GetIsSkipSupportedFlag`
- `IDataReaderAsync.cs` — provider-agnostic data-reader handle exposed by `IQueryRunner.ExecuteReader[Async]`
- `Preamble.cs` — eager-loading prefetch base
- `SequentialAccessHelper.cs` — `OptimizeMappingExpressionForSequentialAccess` rewrites the materialization expression to read each column once when `Configuration.OptimizeForSequentialAccess` is true
- `ColumnReaderAttribute.cs` / `EagerEvaluationAttribute.cs` — annotations consumed by translator + sequential-access optimizer

`IQueryable` adapter:
- `IExpressionQuery.cs` / `IExpressionQuery{T}.cs` — public surface for `Internals` consumers
- `ExpressionQueryImpl.cs` — default `ExpressionQuery<T>` instantiation produced by `IQueryProvider.CreateQuery`
- `Table{T}.cs` — `ITable<T>` implementation
- `CteTable{T}.cs` — `IQueryable<T>` shell for CTE bodies
- `PersistentTable{T}.cs` — adapter wrapping a non-linq2db `IQueryable<T>` as `ITable<T>` (delegates to underlying provider; SQL helpers throw)
- `CompiledTable{T}.cs` — `CompiledQuery` backing
- `ITableMutable.cs` — `Change*` mutators for `ITable<T>`
- `IExpressionPreprocessor.cs` — interceptor hook used in `Query<T>.GetQuery` before cache lookup
- `LinqInternalExtensions.cs` — internal queryable helpers (`UseTableDescriptor`, `DisableFilterInternal`, `ApplyModifierInternal`, `SelectDistinct`, `LoadWithInternal`, `AssociationRecord`, `AsCte`)
- `Internals.cs` — public bridge for `Internals.CreateExpressionQueryInstance` / `GetDataContext` / `ExposeQueryExpression`
- `QueryDebugView.cs` — debugger-display thunks for expression / SQL / SQL-no-params

Support:
- `RuntimeExpressionsContainer.cs` — `IQueryExpressions` impl with id-keyed satellite expressions
- `IQueryExpressions.cs` — interface above
- `CloningContext.cs` — deep-clones `IBuildContext` + `IQueryElement` graphs (used during translation; lives in this area because the cloner is shared across builders and isn't builder-internal)
- `AccessorMember.cs` — equality-aware `(MemberInfo, args)` key used as association-cache key
- `MemberInfoComparer.cs` — `IEqualityComparer<MemberInfo>` for accessor lookups
- `MethodHelper.cs` — type-safe `MethodInfo` lookups
- `ReflectionHelper.cs` — pre-resolved `PropertyInfo`s for common `Expression` shapes (used in expression-tree surgery)
- `TransactionScopeHelper.cs` — runtime probe of `System.Transactions.Transaction.Current` via reflection (used to skip implicit transactions when running inside a `TransactionScope`)
- `Exceptions.cs` — `DefaultInheritanceMappingException` thrown from generated discriminator switch
- `Internals.cs` — public bridge (listed above)

## Subsystems

### 1. Query cache (per-entity-type, copy-on-write)

`Query<T>` owns a `static readonly QueryCache _queryCache` (`Query{T}.cs:232`). The cache stores up to 100 entries (`CacheSize = 100`, `Query{T}.cs:98`); on add, the new entry is inserted at index 0 and the array is rebuilt under `_syncCache`. A separate `_syncPriority` lock — taken via `Monitor.TryEnter` so a hot read path never blocks — controls a swap-up reorder that promotes a hit one slot toward the front per lookup (`Query{T}.cs:203-205`). The cache is keyed by `ConfigurationID`, `InlineParameters`, `ContextType`, `IsEntityServiceProvided`, the `QueryFlags`, and `CompareInfo.MainExpression.EqualsTo(other, dataContext)` plus optional dynamic-accessor and value-comparison probes (`Query.cs:62-119`).

Two-phase lookup: `Query<T>.GetQuery` first probes with the user's raw expression and `QueryFlags.None`; on miss it runs `ExposeAndPrepareExpression` (which fixed-points `ExpressionBuilder.ExposeExpression` + `IQueryExpressionInterceptor.ProcessExpression` up to 10 iterations) and probes a second time with `QueryFlags.ExpandedQuery` set. Cache miss falls through to `CreateQuery`, which invokes EXPR-TRANS via `new ExpressionBuilder(...).Build<T>(...)` and, on `ErrorExpression` non-null, retries once with `validateSubqueries: true` before throwing. Cache miss is recorded in `CacheMissCount` (`Query{T}.cs:374`).

`Query.CacheCleaners` is a `ConcurrentQueue<Action>` (`Query.cs:142`) populated lazily from per-type static constructors (`Query<T>` itself, `QueryRunner.Cache<T>`, `QueryRunner.Cache<T,TR>`, `Update<T>.Cache`, etc.). `Query.ClearCaches()` walks the queue.

### 2. Cache-key normalization

Constants are not raw-compared. `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` (`ExpressionCacheHelpers.cs:10`) replaces non-scalar constants with `ConstantPlaceholderExpression` before they enter `CompareInfo.MainExpression`, *unless* the value is `null`, an `EntityDescriptor`, a `FormattableString`, a `RawSqlString`, or implements `IExpressionCacheKey`. Implementers of `IExpressionCacheKey` must be immutable and provide value-based equality — they get hashed/compared by their own `GetHashCode` / `Equals`, so distinct logical values produce distinct cache lines without explosion of cache size for incidental reference-different but value-equal keys.

`ExpressionCacheManager.RegisterParameterEntry` (`ExpressionCacheManager.cs:172`) deduplicates parameter entries: structural equality on `ClientValueGetter` / `ClientToProviderConverter` / `ItemAccessor` / `DbDataTypeAccessor` + `DbDataType` collapses two trees into one `ParameterId`; if structural equality fails but the parameters share a name and `EvaluatedValue`, a `RegisterDuplicateCheck` runtime probe is registered so cache hits still distinguish "same shape, different runtime value" later.

### 3. Executor delegate wiring

A built `Query<T>` carries up to three executor delegates: `GetElement` (sync scalar/aggregate), `GetElementAsync` (async scalar/aggregate), `GetResultEnumerable` (sync + async iteration). These are assigned by `QueryRunner.Set*Query` once at the end of `ExpressionBuilder.Build<T>`. Choice of setter encodes the result shape:

| Setter | Result | When |
|---|---|---|
| `SetRunQuery<T>(query, mapper)` | `IResultEnumerable<T>` for `Cast` / `Concat` / `Union` / `OfType` / `ScalarSelect` / `Select` / `SequenceContext` / `Table` | Default for `IQueryable<T>` materialization |
| `SetRunQuery<T>(query, scalarMapper)` (overload returning `object`) | First-row `GetElement`/`Async` for `Aggregation` / `All` / `Any` / `Contains` / `Count` | First-row materialization |
| `SetScalarQuery` | `runner.ExecuteScalar()` → `GetElement` | DB-side scalar (e.g. `COUNT(*)`) |
| `SetNonQueryQuery` | `runner.ExecuteNonQuery()` → `GetElement` returning `int` | Insert/Update/Delete affected-row counts |
| `SetNonQueryQuery2` | run query[0]; if 0 affected, run query[1] | `InsertOrReplace` fallback paths |
| `SetQueryQuery2` | run query[0] as scalar; if `null`, run query[1] as non-query | `InsertWithIdentity` shapes |

`SetRunQuery<T>` runs `FinalizeQuery(query)` first, which calls `query.SqlOptimizer.Finalize` on each `QueryInfo.Statement` (`QueryRunner.cs:270`). If the provider doesn't support skip with the current take value (`SqlProviderFlags.GetIsSkipSupportedFlag`), it folds `Skip` into a larger `Take` and wraps the result in a `LimitResultEnumerable<T>` that drops the first N rows client-side (`QueryRunner.cs:438-460`).

### 4. Materialization mapper (`Mapper<T>`)

`QueryRunner.Mapper<T>` (`QueryRunner.cs:67`) caches a compiled `Func<IQueryRunner, DbDataReader, T>` per concrete data-reader type — the runtime reader type matters because different providers expose different `Get*` overloads, and `MapperExpressionTransformer` rewrites the original expression's `ConvertFromDataReaderExpression` nodes against the actual reader instance (`QueryRunner.cs:210-216`). The transformer also rewrites `SqlQueryRootExpression` references to read the live `IDataContext` off the `IQueryRunner` (`QueryRunner.cs:195-208`).

On `FormatException` / `InvalidCastException` / `LinqToDBConvertException` / `*NullValueException` (Oracle, MySql), `ReMapOnException` switches the column to slow-mode (`slowMode: true` in `TransformMapperExpression`), which keeps the `ConvertFromDataReaderExpression.ColumnReader` in the tree instead of inlining the typed `Get*` call. The `IsFaulted` flag on `ReaderMapperInfo` prevents infinite retry on a column that genuinely can't be read.

When `Configuration.OptimizeForSequentialAccess` is true, `SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess` rewrites the mapper to read each column exactly once — required for `CommandBehavior.SequentialAccess` semantics where re-reading a column throws (`SequentialAccessHelper.cs:57`).

The mapper is invoked in two places: `BasicResultEnumerable<T>.GetEnumerator` / `GetAsyncEnumerable` for set-returning queries (`QueryRunner.cs:493-547`, `554-620`), and `ExecuteElement<T>` / `ExecuteElementAsync<T>` for first-row aggregates (`QueryRunner.cs:733-814`). Both paths apply `IUnwrapDataObjectInterceptor.UnwrapDataReader` before the first read so providers like MiniProfiler can hand back the underlying reader for mapper compilation.

### 5. `IQueryable<T>` adapter and execution entry

`ExpressionQuery<T>` is the type the public API surfaces as `IQueryable<T>`. The five execution paths it implements (`IEnumerable<T>.GetEnumerator`, `IEnumerable.GetEnumerator`, `IQueryProvider.Execute(T)`, `IQueryProviderAsync.ExecuteAsync<TResult>`, `GetForEachAsync` / `GetForEachUntilAsync` / `GetAsyncEnumerator`) all share the same skeleton: wrap `Expression` in a `RuntimeExpressionsContainer`, call `GetQuery` (which memoizes the `Query<T>` reference via `Info` only when `CompareInfo.IsFastComparable && !dependsOnParameters` — `ExpressionQuery.cs:93`), call `StartLoadTransaction[Async]` to start an implicit transaction *only* when `query.IsAnyPreambles()` and the data context isn't already in a `TransactionScope` or transaction, run preambles, then call the appropriate executor delegate.

The "fast comparable" memoization avoids the cache lookup entirely on subsequent calls to the same `IQueryable<T>` instance. It's only safe when the query has no dynamic accessors / value-equality probes (because those need to be re-evaluated against the live context every call) and when the exposer didn't mutate the tree (`dependsOnParameters` is true when the second-phase `ExpandedQuery` lookup was the one that hit / created the entry — `Query{T}.cs:354`).

## Interactions

```
user code
   ↓ IQueryable<T>.Execute / GetEnumerator
ExpressionQuery<T>            (IQueryable adapter)
   ↓ Query<T>.GetQuery
QueryCache  ← Query<T>.QueryCache (per-T, max 100)
   ↓ miss → ExpressionBuilder.Build<T>          ← EXPR-TRANS
                                ↓ produces SqlStatement[] + executor delegate
QueryRunner.Set*Query → query.GetElement / GetResultEnumerable
   ↓ executor delegate invoked
IDataContext.GetQueryRunner → IQueryRunner    ← DATA
   ↓ ExecuteReader / ExecuteScalar / ExecuteNonQuery
DbDataReader → Mapper<T>                       ← MAPPING (entity descriptors, type converters)
   ↓ materialized T
yield return / Task<T>
```

### Inbound

- Public surface (CORE / EXPR areas): `ITable<T>`, `LinqExtensions`, `LinqToDB.Internals` — instantiate `Table<T>` / `ExpressionQueryImpl<T>` and call `IQueryable<T>` members.
- `CompiledQuery` (CORE): wraps `CompiledTable<T>` here.
- DATA: `DataConnection.QueryRunner` derives from `QueryRunnerBase` to bridge `IQueryRunner` to a `DbCommand`.

### Outbound

- **EXPR-TRANS** (`Source/LinqToDB/Internal/Linq/Builder/**`): `ExpressionBuilder.Build<T>` is invoked from `Query<T>.CreateQuery` on cache miss; `ExpressionTreeOptimizationContext`, `ParametersContext`, and `ExpressionBuilder.ExposeExpression` are imported from there.
- **EXPR**: `ExpressionPrinter`, `ExpressionEqualityComparer`, `BinaryExpressionAggregatorVisitor`, `ContextRefExpression`, `SqlPlaceholderExpression`, `ConvertFromDataReaderExpression`, `SqlQueryRootExpression` — `RuntimeExpressionsContainer` and `MapperExpressionTransformer` consume these.
- **SQL-AST** (`Source/LinqToDB/Internal/SqlQuery/**`): every `QueryInfo.Statement` is an `SqlStatement`; `QueryRunner` mutates `SelectQuery.Select.SkipValue` / `TakeValue` directly when fixing up unsupported skip semantics.
- **SQL-PROVIDER** (`Source/LinqToDB/Internal/SqlProvider/**`): `query.SqlOptimizer.Finalize` (called by `FinalizeQuery`) and `query.SqlProviderFlags` are imported.
- **DATA**: `IDataContext.GetQueryRunner` produces the runtime handle; `DataConnection` / `DataContext` provide the implicit transaction support (`StartLoadTransaction[Async]`).
- **MAPPING**: `MappingSchema`, `EntityDescriptor`, `ColumnDescriptor.GetDbParamLambda` / `GetDbDataType` consumed by `QueryRunner.GetParameter`.
- **INFRA / async**: `AsyncEnumeratorAsyncWrapper`, `EmptyIAsyncDisposable`, `ActivityService` (metrics).

## Known issues / debt

- **Single-threaded cache reorder.** The hit-promotion swap in `QueryCache.Find` (`Query{T}.cs:203-205`) is gated on `Monitor.TryEnter(_syncPriority)`; under contention only one thread reorders at a time and the rest read the `_indexes` snapshot. Working as designed, but it means under heavy parallel load the cache effectively never reorders. There's a `// TODO : IT : check` (`Query{T}.cs:137`) on the related add path.
- **`TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)`** — repeated comment in `Mapper<T>.Map` (`QueryRunner.cs:96`) and both copies of `BasicResultEnumerable<T>` (`QueryRunner.cs:531`, `597`). Indicates the slow-mode fallback is hit in tests and the maintainers don't have a clean reproduction story.
- **`IBuildContext` reachable from `CloningContext`.** `CloningContext` (`CloningContext.cs:15`) lives in this area but transitively touches `IBuildContext` from EXPR-TRANS — the area boundary is fuzzy here. `CloningContext` is invoked from translator paths in `Builder/`, but its public API (`Clone*` / `Correct*`) is also reusable elsewhere.
- **Static state in `Query<T>`.** Per-type static `_queryCache` plus per-pair-type static `Cache<T>` / `Cache<T,TR>` plus `Update<T>.Cache` means cache-cleaner registration is scattered across `static` constructors. `Query.CacheCleaners` only fires when those statics have been initialised — types that have never been touched skip cleanup. Working as designed, but worth knowing when chasing memory growth.
- **`PersistentTable<T>.DataContext` returns `null!`.** (`PersistentTable{T}.cs:38`) Any code path that pulls a `DataContext` off a queryable wrapped in `PersistentTable` will NRE — the type exists for narrow scenarios where the underlying queryable isn't a linq2db one and SQL generation isn't expected.

## See also

- [EXPR-TRANS area](../EXPR-TRANS/INDEX.md) — `Source/LinqToDB/Internal/Linq/Builder/**`, the translator that `Query<T>.CreateQuery` calls into.
- [SQL-AST area](../SQL-AST/INDEX.md) — `SqlStatement`, `SelectQuery`, `SqlParameter`, `SqlField`.
- [DATA area](../DATA/INDEX.md) — `IDataContext.GetQueryRunner`, `DataConnection`, `DataContext`.
- [MAPPING area](../MAPPING/INDEX.md) — `EntityDescriptor`, `ColumnDescriptor`.
- `architecture.md` § Query pipeline — top-level narrative this area implements.

## Pointers

- Cache miss diagnosis: instrument `Query<T>.CacheMissCount` and `Query.CacheCleaners`. A miss with a tree that *should* match an existing entry usually means `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` is returning `false` for a non-scalar value that doesn't implement `IExpressionCacheKey`.
- Mapper crash with `FormatException` / `InvalidCastException`: check `Configuration.OptimizeForSequentialAccess` first — sequential-access mode rewrites the mapper aggressively and the failure may be in the rewrite, not the materialization.
- Implicit-transaction surprises (deadlocks under preambles): `StartLoadTransaction` only kicks in when `query.IsAnyPreambles()` is true. Eager-loading queries open one transaction across all preambles + main query.
- `CompiledQuery` correctness when mixing sync + async: `CompiledTable<T>.ReplaceAsyncWithSync` (`CompiledTable{T}.cs:28`) rewrites `*Async` calls in the compiled lambda to their sync counterparts; the cache key includes the original `expression`, so sync and async invocations of the same compiled query share a cache line.

<details><summary>Coverage</summary>

- Tier 1: 5/5 — all pinned files read in full.
- Tier 2: 46/46 — every `Source/LinqToDB/Internal/Linq/*.cs` file outside `Builder/` was read; the seven `QueryRunner.<op>.cs` partials (`CreateTable`, `Delete`, `Insert`, `InsertOrReplace`, `InsertWithIdentity`, `Update`, `DropTable`) were sampled to header + first ~50 lines as they all share the same shape: build an `SqlTable` + `Sql<Op>Statement` + `Query<int>` (or `Query<object>` for `InsertWithIdentity`), wire `SetNonQueryQuery` / `SetNonQueryQuery2` / `SetQueryQuery2`, and cache via `MemoryCache<IStructuralEquatable, Query<int>>`.
- Tier 3: not applicable (the area has no `bin/` / generated content under `Internal/Linq/`).
- Excluded: `Source/LinqToDB/Internal/Linq/Builder/**` belongs to the EXPR-TRANS area per the area registry.

</details>
