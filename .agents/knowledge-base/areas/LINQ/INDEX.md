---
area: LINQ
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 5/5
coverage_tier_2: 47/47
---

# LINQ -- query caching, executor lifecycle, and the `IQueryable` adapter

The execution and caching half of the LINQ pipeline. Owns the shared LINQ query cache (`QueryCache.Default`, global and bucketed -- see `## Subsystems` below), the compiled executor delegates (`GetElement`, `GetElementAsync`, `GetResultEnumerable`), the `IQueryable<T>` / `IQueryProvider` adapter exposed to user code, and the ADO.NET execution loop that runs `IQueryRunner` against a `DbDataReader` and drives the materialization mapper. Translation of the LINQ expression tree into `SqlStatement` lives in the EXPR-TRANS area (`Source/LinqToDB/Internal/Linq/Builder/**`) and is invoked from here exactly once per cache miss, via `ExpressionBuilder.Build<T>` in `Query<T>.CreateQuery` (`Source/LinqToDB/Internal/Linq/Query{T}.cs:213`).

## Key types

- **`Query`** (`Source/LinqToDB/Internal/Linq/Query.cs:20`) -- abstract base. Holds the `QueryInfo[]` shards (one per `SqlStatement` to execute), the cached `MappingSchema` / `SqlOptimizer` / `SqlProviderFlags` / `DataOptions` snapshot taken at build time, the `QueryCacheCompareInfo` used for cache-key comparison, the `ParameterAccessors` list that binds runtime values into `SqlParameter`s, and the `Preamble[]` for eager-loading prefetches. `GetElement` / `GetElementAsync` are the executor delegates set by `QueryRunner.Set*Query` once per build. `ConfigurationID` / `ContextType` / `InlineParameters` / `IsEntityServiceProvided` are no longer stored on `Query` itself -- they are pinned by the `QueryCache` bucket key (`ContextType` + `ConfigurationID` + `QueryFlags`, the latter encoding `InlineParameters` and `HasEntityServiceInterceptor`), so `Query.Compare` (`Query.cs:56`) only re-checks the expression tree plus optional dynamic-accessor / value-comparison probes.
- **`Query<T>`** (`Source/LinqToDB/Internal/Linq/Query{T}.cs:16`) -- sealed generic. `GetQuery` is the cache-lookup entry point: it runs `BinaryExpressionAggregatorVisitor`, `IExpressionPreprocessor`, the `IQueryExpressionInterceptor`, then probes the shared `QueryCache.Default` (see `## Subsystems` below -- the per-type copy-on-write cache this bullet used to describe was replaced by one global bucketed cache); on miss it runs `ExposeAndPrepareExpression` (which fixed-points the exposer + interceptor up to 10 iterations) and probes a second time with the `QueryFlags.ExpandedQuery` bit set before falling through to `CreateQuery`. `ClearCache()` / `CacheMissCount` now delegate to `QueryCache.Default.ClearForType(typeof(T))` / `GetMissCount(typeof(T))` (`Query{T}.cs:58-63`).
- **`ExpressionQuery<T>`** (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:19`) -- abstract `IExpressionQuery<T>` / `IAsyncEnumerable<T>` adapter. This is the type the public `IQueryable<T>` surface materializes to: it implements `IQueryProvider.CreateQuery` / `Execute` / `IQueryProviderAsync.ExecuteAsync` / `ExecuteAsyncEnumerable` and the synchronous / async `IEnumerable<T>` / `IAsyncEnumerable<T>` traversals. Concrete subclasses are `Table<T>`, `CteTable<T>`, `ExpressionQueryImpl<T>`.
- **`IQueryRunner`** (`Source/LinqToDB/Internal/Linq/IQueryRunner.cs:11`) -- the per-execution ADO.NET handle owned by `IDataContext`. Provides sync + async `ExecuteNonQuery` / `ExecuteScalar` / `ExecuteReader` and exposes `DataContext`, `Expressions`, `Parameters`, `Preambles`, `MapperExpression`, `RowsCount`, `QueryNumber`. Concrete implementation lives in DATA (`DataConnection.QueryRunner`, owned by `IDataContext.GetQueryRunner`).
- **`QueryRunnerBase`** (`Source/LinqToDB/Internal/Linq/QueryRunnerBase.cs:11`) -- abstract default plumbing for `IQueryRunner`: holds the parameter / preamble snapshot, calls `QueryRunner.SetParameters` to bind a `SqlParameterValues`, defers the actual command shaping to `SetQuery(IReadOnlyParameterValues, bool forGetSqlText)`. `Dispose` closes `IDataContext` when `CloseAfterUse` is set.
- **`QueryRunner`** (`Source/LinqToDB/Internal/Linq/QueryRunner.cs:33`) -- static partial. The hub between a built `Query` and the runtime executor: assembles `Mapper<T>` (a per-data-reader-type `Func<IQueryRunner, DbDataReader, T>` cache with slow-mode fallback on `FormatException` / `InvalidCastException` / `*NullValueException`), wires `query.GetResultEnumerable` / `query.GetElement` via `SetRunQuery` / `SetScalarQuery` / `SetNonQueryQuery` / `SetNonQueryQuery2` / `SetQueryQuery2` / `SetIfExistsUpdateElseInsert`, and provides per-operation entry points (`Insert<T>`, `Update<T>`, `Delete<T>`, `InsertOrReplace<T>`, `InsertWithIdentity<T>`, `CreateTable<T>`, `DropTable<T>` -- see partial files).
- **`QueryInfo`** (`Source/LinqToDB/Internal/Linq/QueryInfo.cs:11`) -- one shard per `SqlStatement` produced for a single LINQ query. Most queries have one; `SetNonQueryQuery2` / `SetQueryQuery2` / `SetIfExistsUpdateElseInsert` paths produce two or three (e.g. update-or-insert fallbacks, or the `Upsert(...).Update(v => v.When(cond))` three-query orchestration). Implements `IQueryContext`.
- **`QueryCacheCompareInfo`** (`Source/LinqToDB/Internal/Linq/QueryCacheCompareInfo.cs:8`) -- the key structure for cache equality. Holds the canonical `MainExpression`, optional `DynamicAccessors` (satellite expressions that need re-fetching from the live `IDataContext` to compare), and optional `ComparisionFunctions` (compiled value-equality probes). `IsFastComparable` is `true` when neither list is needed -- `ExpressionQuery<T>` only memoizes a `Query<T>` reference into its `Info` field when this flag holds (`ExpressionQuery.cs:93`).
- **`QueryCache`** (`Source/LinqToDB/Internal/Linq/QueryCache.cs:42`) -- public sealed, process-wide singleton (`QueryCache.Default`, `QueryCache.cs:45`) that replaced the old per-`Query<T>` copy-on-write array cache. Buckets entries by a `CacheKey` (`ResultType`, `ContextType`, `ConfigurationID`, `QueryFlags`, and an 8-level `ChainHash` of the LINQ method/member chain -- `QueryCache.cs:102-144`, `525-553`); inside a bucket, `TryFind` still verifies candidates with `Query.Compare`. Each entry tracks its own idle-timeout deadline with hit-rate-tiered extension (`EffectiveTimeoutTicks`, `QueryCache.cs:988-1005`); a single-flighted background sweep (`ThreadPool.UnsafeQueueUserWorkItem`, `QueryCache.cs:591-628`) expires stale entries and trims the global count toward `DefaultMaxEntries` (10,000) when over capacity. The static constructor of `QueryCache.Default` registers the only `Query.CacheCleaners` entry needed for the LINQ query cache (`QueryCache.cs:50-55`) -- `Query<T>` no longer registers a per-closed-generic-type cleaner.
- **`ExpressionCacheManager`** (`Source/LinqToDB/Internal/Linq/ExpressionCacheManager.cs:21`) -- the build-time companion to `QueryCacheCompareInfo`. Tracks accessor mappings (`AccessorsMapping`), parameter cache entries (`_parameterEntries`), duplicate-parameter checks, by-value comparisons, and `_bySqlValueCompare` registrations; `BuildQueryCacheCompareInfo` finalizes them into the `Query.CompareInfo` + `Query.ParameterAccessors` actually stored on the cached `Query` (`ExpressionCacheManager.cs:412`).
- **`IQueryExpressions`** / **`RuntimeExpressionsContainer`** (`Source/LinqToDB/Internal/Linq/IQueryExpressions.cs:5`, `RuntimeExpressionsContainer.cs:9`) -- wrapper that decouples `MainExpression` (the user tree) from satellite expressions referenced by id. Used everywhere a `ParameterAccessor` or `DynamicExpressionInfo` needs to retrieve the current value of subtree N from the live request.
- **`ParameterAccessor`** / **`ParameterCacheEntry`** (`ParameterAccessor.cs:13`, `ParameterCacheEntry.cs:5`) -- runtime side and build-time side of a `SqlParameter` value pipeline. `ParameterAccessor.ClientValueAccessor(IQueryExpressions, IDataContext?, object?[]?) -> object?` extracts the user-visible value from the expression tree; `ClientToProviderConverter` / `ItemAccessor` / `DbDataTypeAccessor` map it to the provider-shaped value + type.
- **`Preamble`** (`Source/LinqToDB/Internal/Linq/Preamble.cs:9`) -- a deferred prefetch executed before the main query. Eager loading materializes one preamble per `LoadWith` group; `Query.InitPreambles[Async]` runs them in array order and threads the previous results in via the `object[]? preambles` argument so a later preamble can reference earlier ones. `IsInlined` (virtual, default `false`, `Preamble.cs:21`) marks a preamble that resolves from the main query own result set rather than running a separate query -- used by CteUnion single-query mode; `Query.IsAnyPreambles()` (`Query.cs:155`) now treats an all-inlined preamble array as no preambles for the implicit-transaction decision.
- **`Table<T>`** (`Source/LinqToDB/Internal/Linq/Table{T}.cs:13`) -- concrete `ITable<T>` implementation. Builds its own initial expression tree from `Methods.LinqToDB.GetTable.MakeGenericMethod(T)` against an `SqlQueryRootExpression`, then layers `TableName` / `SchemaName` / `ServerName` / `DatabaseName` / `TableID` / `TableOptions` / `UseTableDescriptor` calls on top via the `Apply*` helpers as user code mutates the table.
- **`CompiledTable<T>`** (`Source/LinqToDB/Internal/Linq/CompiledTable{T}.cs:16`) -- backs `CompiledQuery`. Caches a `Query<T>` keyed by `(operation, configurationID, expression, queryFlags)` in the global `QueryRunner.Cache<T>.QueryCache` (`MemoryCache<IStructuralEquatable, Query<T>>`), rewriting `*Async` method calls in the lambda body to their sync counterparts so the compiled tree can be re-used for both modes (`CompiledTable{T}.cs:28`).
- **`IExpressionCacheKey`** (`Source/LinqToDB/Internal/Linq/IExpressionCacheKey.cs:13`) -- opt-in marker for constants that should *participate* in the cache key (used by `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` to decide whether to replace the constant with a `ConstantPlaceholderExpression`, which would erase its value from the comparison).

## Files (Tier 1 / Tier 2)

### Tier 1 (5)

| Path | Purpose |
|---|---|
| `Source/LinqToDB/Internal/Linq/Query.cs` | abstract `Query`, cache-cleaner registry, preamble execution |
| `Source/LinqToDB/Internal/Linq/Query{T}.cs` | typed `Query<T>` + cache-lookup entry point (`GetQuery`) against the shared `QueryCache.Default` |
| `Source/LinqToDB/Internal/Linq/IQueryRunner.cs` | runtime ADO.NET handle contract |
| `Source/LinqToDB/Internal/Linq/QueryRunner.cs` | mapper assembly, executor delegate wiring, `BasicResultEnumerable`, upsert-fallback orchestration |
| `Source/LinqToDB/Internal/Linq/ExpressionQuery.cs` | `IQueryable<T>` / `IQueryProvider` adapter |

### Tier 2 (47)

Cache + key:
- `QueryCache.cs` -- global bucketed `Query` cache (`QueryCache.Default`); bucket key, hit-rate-tiered expiration, sweep/trim (see `## Subsystems`)
- `ExpressionCacheManager.cs` -- build-time accessor / parameter / value-equality registration
- `ExpressionCacheHelpers.cs` -- `ShouldRemoveConstantFromCache` rule
- `IExpressionCacheKey.cs` -- opt-in marker for value-keyed constants
- `QueryCacheCompareInfo.cs` -- runtime comparison structure
- `ParameterAccessor.cs` / `ParameterCacheEntry.cs` -- parameter binding
- `QueryFlags.cs` / `QueryFlagsHelper.cs` -- public `InlineParameters` / `ExpandedQuery` / `HasEntityServiceInterceptor` flag bits, now part of `QueryCache.CacheKey` (previously compared as separate `Query` instance fields)

Executor + runtime:
- `QueryRunnerBase.cs` -- abstract `IQueryRunner` plumbing for provider impls
- `QueryRunner.CreateTable.cs`, `QueryRunner.DropTable.cs`, `QueryRunner.Insert.cs`, `QueryRunner.InsertOrReplace.cs`, `QueryRunner.InsertWithIdentity.cs`, `QueryRunner.Update.cs`, `QueryRunner.Delete.cs` -- per-CRUD-op `Query<int>` / `Query<object>` builders + `MemoryCache` wiring; `QueryRunner.InsertOrReplace.cs`'s `MakeAlternativeInsertOrUpdate` additionally emits a 3-query existence-check / conditional-UPDATE / INSERT orchestration (`SetIfExistsUpdateElseInsert`) when the fallback statement has an `UpdateWhere` predicate (`Upsert(...).Update(v => v.When(cond))` on a provider without MERGE/ON CONFLICT), and detects `SqlProviderFlags.IsInsertOrUpdateRequiresAlignedBranches` + `UpsertBuilder.HasDivergentInsertOrUpdateBranches` to route SAP HANA-style providers to the emulation path even when native `IsInsertOrUpdateSupported` is set
- `QueryInfo.cs` / `IQueryContext.cs` -- per-statement shard
- `IResultEnumerable.cs` -- combined `IEnumerable<T>` / `IAsyncEnumerable<T>` returned by `Query<T>.GetResultEnumerable`
- `LimitResultEnumerable.cs` -- fallback Skip/Take wrapper used when the provider lacks `GetIsSkipSupportedFlag`
- `IDataReaderAsync.cs` -- provider-agnostic data-reader handle exposed by `IQueryRunner.ExecuteReader[Async]`
- `Preamble.cs` -- eager-loading prefetch base (`IsInlined` opt-out for CteUnion single-query mode)
- `SequentialAccessHelper.cs` -- `OptimizeMappingExpressionForSequentialAccess` rewrites the materialization expression to read each column once when `Configuration.OptimizeForSequentialAccess` is true
- `ColumnReaderAttribute.cs` / `EagerEvaluationAttribute.cs` -- annotations consumed by translator + sequential-access optimizer

`IQueryable` adapter:
- `IExpressionQuery.cs` / `IExpressionQuery{T}.cs` -- public surface for `Internals` consumers
- `ExpressionQueryImpl.cs` -- default `ExpressionQuery<T>` instantiation produced by `IQueryProvider.CreateQuery`
- `Table{T}.cs` -- `ITable<T>` implementation
- `CteTable{T}.cs` -- `IQueryable<T>` shell for CTE bodies
- `PersistentTable{T}.cs` -- adapter wrapping a non-linq2db `IQueryable<T>` as `ITable<T>` (delegates to underlying provider; SQL helpers throw)
- `CompiledTable{T}.cs` -- `CompiledQuery` backing
- `ITableMutable.cs` -- `Change*` mutators for `ITable<T>`
- `IExpressionPreprocessor.cs` -- interceptor hook used in `Query<T>.GetQuery` before cache lookup
- `LinqInternalExtensions.cs` -- internal queryable helpers (`UseTableDescriptor`, `DisableFilterInternal`, `ApplyModifierInternal`, `SelectDistinct`, `LoadWithInternal`, `AssociationRecord`, `AsCte`)
- `Internals.cs` -- public bridge for `Internals.CreateExpressionQueryInstance` / `GetDataContext` / `ExposeQueryExpression`
- `QueryDebugView.cs` -- debugger-display thunks for expression / SQL / SQL-no-params

Support:
- `RuntimeExpressionsContainer.cs` -- `IQueryExpressions` impl with id-keyed satellite expressions
- `IQueryExpressions.cs` -- interface above
- `CloningContext.cs` -- deep-clones `IBuildContext` + `IQueryElement` graphs (used during translation; lives in this area because the cloner is shared across builders and is not builder-internal)
- `AccessorMember.cs` -- equality-aware `(MemberInfo, args)` key used as association-cache key; `GetHashCode` uses `HashCode.Combine(MemberInfo, Arguments)` with structural per-element equality on `Arguments`
- `MemberInfoComparer.cs` -- `IEqualityComparer<MemberInfo>` for accessor lookups
- `MethodHelper.cs` -- type-safe `MethodInfo` lookups
- `ReflectionHelper.cs` -- pre-resolved `PropertyInfo`s for common `Expression` shapes (used in expression-tree surgery), including `SqlGenericConstructor` (exposes the `Assignments` property for `SqlGenericConstructorExpression`) and `SqlGenericConstructorAssignment` (exposes the `Expression` property for `SqlGenericConstructorExpression.Assignment`)
- `TransactionScopeHelper.cs` -- runtime probe of `System.Transactions.Transaction.Current` via reflection (used to skip implicit transactions when running inside a `TransactionScope`)
- `Exceptions.cs` -- `DefaultInheritanceMappingException` thrown from generated discriminator switch
- `Internals.cs` -- public bridge (listed above)

## Subsystems

### 1. Query cache (global, bucketed, hit-rate-tiered expiration)

**Rewritten in this delta (was: per-`Query<T>` copy-on-write array, max 100 entries -- see AUDIT-NOTE).** `Query<T>` no longer owns a per-type cache. All typed queries share one process-wide `QueryCache.Default` singleton (`QueryCache.cs:45`), a `ConcurrentDictionary<CacheKey, Bucket>` (`QueryCache.cs:66`). `CacheKey` is `(ResultType, ContextType, ConfigurationID, QueryFlags, ChainHash)` (`QueryCache.cs:102-144`); `ChainHash` walks up to 8 levels of the LINQ method/member chain from the root expression, hashing `MethodCallExpression.Method` / `MemberExpression.Member` / node-type+type for anything else (`ComputeChainHash`, `QueryCache.cs:525-553`). `ConfigurationID`, `ContextType`, `InlineParameters`, and `IsEntityServiceProvided` used to be separate fields compared inside `Query.Compare`; they are now folded entirely into the bucket key (the latter two via `QueryFlags.InlineParameters` / `HasEntityServiceInterceptor`, set by `QueryFlagsHelper.GetQueryFlags`), so `Query.Compare` (`Query.cs:56`) only re-checks `CompareInfo.MainExpression.EqualsTo(...)` plus the optional dynamic-accessor / value-comparison probes -- same tail behavior as before.

Each bucket is a copy-on-write `Entry[]` capped at `BucketCap = 16` (`QueryCache.cs:57`); a full bucket evicts by earliest expiration deadline, then oldest access time, then lowest hit rate (`FindBucketVictimIndex` / `CompareForEviction`, `QueryCache.cs:1034-1069`). Each `Entry` carries its own `BaseTimeoutTicks` (captured from `LinqOptions.CacheSlidingExpirationOrDefault` at add time) and an `ExpiresAtTicks` deadline that only ever extends, never shortens; the extension multiplies the base timeout by a hit-rate tier -- under 5/hr keeps 1x, under 50/hr gets 4x, under 500/hr gets 12x, 500/hr or more gets 24x (`EffectiveTimeoutTicks`, `QueryCache.cs:988-1005`). Hot-path hits sample the heavyweight deadline-extension write (1-in-16 within a 100ms window, `HitSampleMask`, `QueryCache.cs:847-878`) to avoid hammering shared cache lines. A single-flighted global sweep (`ThreadPool.UnsafeQueueUserWorkItem`, debounced by `_sweepRunning`, `QueryCache.cs:591-628`) runs every `DefaultSweepIntervalMs` (5 min) or when the approximate global `_entryCount` exceeds `DefaultMaxEntries` (10,000, overridable via `MaxEntriesOverride`); it updates hit-rate EMAs, reaps expired entries per bucket, then trims any remaining overage globally, sorted ascending by expiry, then last-access, then hit-rate (`TrimGlobalToCapacity`, `QueryCache.cs:703-758`).

Two-phase lookup is unchanged in shape: `Query<T>.GetQuery` first probes `QueryCache.Default.TryFind` with the user raw expression and `QueryFlags.None`; on miss it runs `ExposeAndPrepareExpression` (fixed-points `ExpressionBuilder.ExposeExpression` + `IQueryExpressionInterceptor.ProcessExpression` up to 10 iterations) and probes a second time with `QueryFlags.ExpandedQuery` set. Cache miss falls through to `CreateQuery`, which invokes EXPR-TRANS via `new ExpressionBuilder(...).Build<T>(...)` and, on `ErrorExpression` non-null, retries once with `validateSubqueries: true` before throwing. Cache miss is now recorded per-result-type in `QueryCache._misses` (`ConcurrentDictionary<Type, CounterBox>`) via `IncrementMissCount`, surfaced through `Query<T>.CacheMissCount => QueryCache.Default.GetMissCount(typeof(T))` (`Query{T}.cs:63`, `QueryCache.cs:228-235`).

`Query.CacheCleaners` is still a `ConcurrentQueue<Action>` (`Query.cs:131`), but the static constructor of `QueryCache.Default` now registers the single cleaner needed for the LINQ query cache (`cache.ClearAll`, `QueryCache.cs:50-55`) instead of each closed generic `Query<T>` registering its own -- the per-CRUD-op caches (`QueryRunner.Cache<T>`, `QueryRunner.Cache<T,TR>`, `Update<T>.Cache`) still register individually, unaffected by this change. `Query.ClearCaches()` still walks the queue unchanged (`Query.cs:136-142`).

### 2. Cache-key normalization

Constants are not raw-compared. `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` (`ExpressionCacheHelpers.cs:10`) replaces non-scalar constants with `ConstantPlaceholderExpression` before they enter `CompareInfo.MainExpression`, *unless* the value is `null`, an `EntityDescriptor`, a `FormattableString`, a `RawSqlString`, or implements `IExpressionCacheKey`. Implementers of `IExpressionCacheKey` must be immutable and provide value-based equality -- they get hashed/compared by their own `GetHashCode` / `Equals`, so distinct logical values produce distinct cache lines without explosion of cache size for incidental reference-different but value-equal keys.

`ExpressionCacheManager.RegisterParameterEntry` (`ExpressionCacheManager.cs:172`) deduplicates parameter entries: structural equality on `ClientValueGetter` / `ClientToProviderConverter` / `ItemAccessor` / `DbDataTypeAccessor` + `DbDataType` collapses two trees into one `ParameterId`; if structural equality fails but the parameters share a name and `EvaluatedValue`, a `RegisterDuplicateCheck` runtime probe is registered so cache hits still distinguish "same shape, different runtime value" later.

### 3. Executor delegate wiring

A built `Query<T>` carries up to three executor delegates: `GetElement` (sync scalar/aggregate), `GetElementAsync` (async scalar/aggregate), `GetResultEnumerable` (sync + async iteration). These are assigned by `QueryRunner.Set*Query` once at the end of `ExpressionBuilder.Build<T>`. Choice of setter encodes the result shape:

| Setter | Result | When |
|---|---|---|
| `SetRunQuery<T>(query, mapper)` | `IResultEnumerable<T>` for `Cast` / `Concat` / `Union` / `OfType` / `ScalarSelect` / `Select` / `SequenceContext` / `Table` | Default for `IQueryable<T>` materialization |
| `SetRunQuery<T>(query, scalarMapper)` (overload returning `object`) | First-row `GetElement`/`Async` for `Aggregation` / `All` / `Any` / `Contains` / `Count` | First-row materialization |
| `SetScalarQuery` | `runner.ExecuteScalar()` -> `GetElement` | DB-side scalar (e.g. `COUNT(*)`) |
| `SetNonQueryQuery` | `runner.ExecuteNonQuery()` -> `GetElement` returning `int` | Insert/Update/Delete affected-row counts |
| `SetNonQueryQuery2` | run query[0]; if 0 affected, run query[1] | `InsertOrReplace` fallback paths |
| `SetQueryQuery2` | run query[0] as scalar; if `null`, run query[1] as non-query | `InsertWithIdentity` shapes |
| `SetIfExistsUpdateElseInsert` | run query[0] as scalar existence-check; run query[1] (UPDATE) if a row exists, else query[2] (INSERT) | `Upsert(...).Update(v => v.When(cond))` fallback on providers lacking MERGE/ON CONFLICT (`QueryRunner.InsertOrReplace.cs:1025`) |

`SetRunQuery<T>` runs `FinalizeQuery(query)` first, which calls `query.SqlOptimizer.Finalize` on each `QueryInfo.Statement` (`QueryRunner.cs:270`). If the provider does not support skip with the current take value (`SqlProviderFlags.GetIsSkipSupportedFlag`), it folds `Skip` into a larger `Take` and wraps the result in a `LimitResultEnumerable<T>` that drops the first N rows client-side (`QueryRunner.cs:438-460`).

### 4. Materialization mapper (`Mapper<T>`)

`QueryRunner.Mapper<T>` (`QueryRunner.cs:67`) caches a compiled `Func<IQueryRunner, DbDataReader, T>` per concrete data-reader type -- the runtime reader type matters because different providers expose different `Get*` overloads, and `MapperExpressionTransformer` rewrites the original expression `ConvertFromDataReaderExpression` nodes against the actual reader instance (`QueryRunner.cs:210-216`). The transformer also rewrites `SqlQueryRootExpression` references to read the live `IDataContext` off the `IQueryRunner` (`QueryRunner.cs:195-208`).

On `FormatException` / `InvalidCastException` / `LinqToDBConvertException` / `*NullValueException` (Oracle, MySql), `ReMapOnException` switches the column to slow-mode (`slowMode: true` in `TransformMapperExpression`), which keeps the `ConvertFromDataReaderExpression.ColumnReader` in the tree instead of inlining the typed `Get*` call. The `IsFaulted` flag on `ReaderMapperInfo` prevents infinite retry on a column that genuinely cannot be read.

When `Configuration.OptimizeForSequentialAccess` is true, `SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess` rewrites the mapper to read each column exactly once -- required for `CommandBehavior.SequentialAccess` semantics where re-reading a column throws (`SequentialAccessHelper.cs:57`).

The mapper is invoked in two places: `BasicResultEnumerable<T>.GetEnumerator` / `GetAsyncEnumerable` for set-returning queries (`QueryRunner.cs:493-547`, `554-620`), and `ExecuteElement<T>` / `ExecuteElementAsync<T>` for first-row aggregates (`QueryRunner.cs:733-814`). Both paths apply `IUnwrapDataObjectInterceptor.UnwrapDataReader` before the first read so providers like MiniProfiler can hand back the underlying reader for mapper compilation.

### 5. `IQueryable<T>` adapter and execution entry

`ExpressionQuery<T>` is the type the public API surfaces as `IQueryable<T>`. The five execution paths it implements (`IEnumerable<T>.GetEnumerator`, `IEnumerable.GetEnumerator`, `IQueryProvider.Execute(T)`, `IQueryProviderAsync.ExecuteAsync<TResult>`, `GetForEachAsync` / `GetForEachUntilAsync` / `GetAsyncEnumerator`) all share the same skeleton: wrap `Expression` in a `RuntimeExpressionsContainer`, call `GetQuery` (which memoizes the `Query<T>` reference via `Info` only when `CompareInfo.IsFastComparable && !dependsOnParameters` -- `ExpressionQuery.cs:93`), call `StartLoadTransaction[Async]` to start an implicit transaction *only* when `query.IsAnyPreambles()` and the data context is not already in a `TransactionScope` or transaction, run preambles, then call the appropriate executor delegate.

The "fast comparable" memoization avoids the cache lookup entirely on subsequent calls to the same `IQueryable<T>` instance. This is only safe when the query has no dynamic accessors / value-equality probes (because those need to be re-evaluated against the live context every call) and when the exposer did not mutate the tree (`dependsOnParameters` is true when the second-phase `ExpandedQuery` lookup was the one that hit / created the entry -- `Query{T}.cs:172`).

## Interactions

```
user code
   ↓ IQueryable<T>.Execute / GetEnumerator
ExpressionQuery<T>            (IQueryable adapter)
   ↓ Query<T>.GetQuery
QueryCache  ← QueryCache.Default (global, bucketed, hit-rate-tiered TTL)
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

- **EXPR-TRANS** (`Source/LinqToDB/Internal/Linq/Builder/**`): `ExpressionBuilder.Build<T>` is invoked from `Query<T>.CreateQuery` on cache miss; `ExpressionTreeOptimizationContext`, `ParametersContext`, and `ExpressionBuilder.ExposeExpression` are imported from there. `QueryRunner.InsertOrReplace.cs`'s `MakeAlternativeInsertOrUpdate` also calls `UpsertBuilder.HasDivergentInsertOrUpdateBranches` to detect providers that need aligned Insert/Update branches (SAP HANA UPSERT).
- **EXPR**: `ExpressionPrinter`, `ExpressionEqualityComparer`, `BinaryExpressionAggregatorVisitor`, `ContextRefExpression`, `SqlPlaceholderExpression`, `ConvertFromDataReaderExpression`, `SqlQueryRootExpression` — `RuntimeExpressionsContainer` and `MapperExpressionTransformer` consume these.
- **SQL-AST** (`Source/LinqToDB/Internal/SqlQuery/**`): every `QueryInfo.Statement` is an `SqlStatement`; `QueryRunner` mutates `SelectQuery.Select.SkipValue` / `TakeValue` directly when fixing up unsupported skip semantics.
- **SQL-PROVIDER** (`Source/LinqToDB/Internal/SqlProvider/**`): `query.SqlOptimizer.Finalize` (called by `FinalizeQuery`) and `query.SqlProviderFlags` are imported, including the newer `IsInsertOrUpdateRequiresAlignedBranches` flag consumed by `QueryRunner.InsertOrReplace.cs`.
- **DATA**: `IDataContext.GetQueryRunner` produces the runtime handle; `DataConnection` / `DataContext` provide the implicit transaction support (`StartLoadTransaction[Async]`).
- **MAPPING**: `MappingSchema`, `EntityDescriptor`, `ColumnDescriptor.GetDbParamLambda` / `GetDbDataType` consumed by `QueryRunner.GetParameter`.
- **INFRA / async**: `AsyncEnumeratorAsyncWrapper`, `EmptyIAsyncDisposable`, `ActivityService` (metrics).

## Known issues / debt

- **Global cache is now a single shared resource across all result types.** (Superseded item -- see AUDIT-NOTE.) The single-threaded per-type reorder debt this bullet used to describe no longer applies -- the old per-`Query<T>` copy-on-write array cache (bounded 100 entries, `Monitor.TryEnter` reorder) was replaced by one process-wide `QueryCache.Default` bucketed by `(ResultType, ContextType, ConfigurationID, QueryFlags, ChainHash)`. New watch point: the approximate `_entryCount` used for the `DefaultMaxEntries` (10,000) global trim is maintained via `Interlocked` adds/subtracts rather than a live count, so it can transiently drift under concurrent `TryAdd`/sweep races -- `ReconcileEntryCount` (`QueryCache.cs:291-306`) is the only path that recomputes it exactly, and it only runs from `ClearAll`. The drift only affects when the global sweep triggers, not correctness -- per-bucket eviction (`BucketCap = 16`) still bounds any single bucket exactly.
- **Static state, narrowed.** The per-type static `_queryCache` on `Query<T>` this bullet used to describe is gone (see `## Subsystems` § 1) -- the main LINQ query cache is now a single eagerly-created `QueryCache.Default` static field (`QueryCache.cs:45`), registered once. Per-pair-type static `QueryRunner.Cache<T>` / `Cache<T,TR>` plus `Update<T>.Cache` still exist for the CRUD-op caches (Insert/Update/Delete/InsertOrReplace/etc.) and still register their own cleaner in a `static` constructor each -- `Query.CacheCleaners` still only fires for types that have been touched. Worth knowing when chasing memory growth from those op-specific caches; the main query cache memory is now bounded globally (`DefaultMaxEntries`) regardless of how many result types are in play.
- **`TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)`** — repeated comment in `Mapper<T>.Map` (`QueryRunner.cs:96`) and both copies of `BasicResultEnumerable<T>` (`QueryRunner.cs:531`, `597`). Indicates the slow-mode fallback is hit in tests and the maintainers do not have a clean reproduction story.
- **`IBuildContext` reachable from `CloningContext`.** `CloningContext` (`CloningContext.cs:15`) lives in this area but transitively touches `IBuildContext` from EXPR-TRANS — the area boundary is fuzzy here. `CloningContext` is invoked from translator paths in `Builder/`, but its public API (`Clone*` / `Correct*`) is also reusable elsewhere.
- **`PersistentTable<T>.DataContext` returns `null!`.** (`PersistentTable{T}.cs:38`) Any code path that pulls a `DataContext` off a queryable wrapped in `PersistentTable` will NRE — the type exists for narrow scenarios where the underlying queryable is not a linq2db one and SQL generation is not expected.

## See also

- [EXPR-TRANS area](../EXPR-TRANS/INDEX.md) — `Source/LinqToDB/Internal/Linq/Builder/**`, the translator that `Query<T>.CreateQuery` calls into.
- [SQL-AST area](../SQL-AST/INDEX.md) — `SqlStatement`, `SelectQuery`, `SqlParameter`, `SqlField`.
- [DATA area](../DATA/INDEX.md) — `IDataContext.GetQueryRunner`, `DataConnection`, `DataContext`.
- [MAPPING area](../MAPPING/INDEX.md) — `EntityDescriptor`, `ColumnDescriptor`.
- `architecture.md` § Query pipeline — top-level narrative this area implements.

## Pointers

- Cache miss diagnosis: instrument `Query<T>.CacheMissCount` and `Query.CacheCleaners`. A miss with a tree that *should* match an existing entry usually means `ExpressionCacheHelpers.ShouldRemoveConstantFromCache` is returning `false` for a non-scalar value that does not implement `IExpressionCacheKey` (the new `QueryCache` buckets on `ChainHash` first, but a hash collision or divergence there only costs a bucket-list scan, never a false negative, since `Query.Compare` still does the real equality check).
- Mapper crash with `FormatException` / `InvalidCastException`: check `Configuration.OptimizeForSequentialAccess` first — sequential-access mode rewrites the mapper aggressively and the failure may be in the rewrite, not the materialization.
- Implicit-transaction surprises (deadlocks under preambles): `StartLoadTransaction` only kicks in when `query.IsAnyPreambles()` is true. Eager-loading queries open one transaction across all preambles + main query. `IsAnyPreambles()` now also returns `false` when every preamble is `IsInlined` (e.g. CteUnion single-query mode) even if the preamble array is non-empty.
- `CompiledQuery` correctness when mixing sync + async: `CompiledTable<T>.ReplaceAsyncWithSync` (`CompiledTable{T}.cs:28`) rewrites `*Async` calls in the compiled lambda to their sync counterparts; the cache key includes the original `expression`, so sync and async invocations of the same compiled query share a cache line.
- Upsert `.Update(v => v.When(cond))` on a provider without MERGE/ON CONFLICT: trace through `QueryRunner.InsertOrReplace.cs:1025` (`SetIfExistsUpdateElseInsert`) rather than the older two-query `SetNonQueryQuery2` path — the three-query orchestration is not atomic across sessions by design (see the XML doc on `SetIfExistsUpdateElseInsert`); callers needing atomicity must wrap the `Upsert` call in their own transaction.

<details><summary>Coverage</summary>

- Tier 1: 5/5 — all pinned files read in full.
- Tier 2: 47/47 — every `Source/LinqToDB/Internal/Linq/*.cs` file outside `Builder/` was read; the seven `QueryRunner.<op>.cs` partials (`CreateTable`, `Delete`, `Insert`, `InsertOrReplace`, `InsertWithIdentity`, `Update`, `DropTable`) were sampled to header + first ~50 lines as they all share the same shape: build an `SqlTable` + `Sql<Op>Statement` + `Query<int>` (or `Query<object>` for `InsertWithIdentity`), wire `SetNonQueryQuery` / `SetNonQueryQuery2` / `SetQueryQuery2`, and cache via `MemoryCache<IStructuralEquatable, Query<int>>`. `QueryCache.cs` (new this delta) and `QueryRunner.InsertOrReplace.cs` (changed this delta) were both read in full, not sampled.
- Tier 3: not applicable (the area has no `bin/` / generated content under `Internal/Linq/`).
- Excluded: `Source/LinqToDB/Internal/Linq/Builder/**` belongs to the EXPR-TRANS area per the area registry.


Read (this run -- delta):
- `Source/LinqToDB/Internal/Linq/AccessorMember.cs` -- no structural change; confirmed `GetHashCode` uses `HashCode.Combine(MemberInfo, Arguments)` with structural per-element equality on `Arguments`; description updated in Tier 2 list for precision.
- `Source/LinqToDB/Internal/Linq/ReflectionHelper.cs` -- two new inner classes added: `SqlGenericConstructor` (exposes `Assignments` `PropertyInfo` for `SqlGenericConstructorExpression`) and `SqlGenericConstructorAssignment` (exposes `Expression` `PropertyInfo` for `SqlGenericConstructorExpression.Assignment`); Tier 2 description updated to enumerate all nested classes including the new linq2db-specific ones.

Read (this run -- delta, sha 36ee4f82f):
- `Source/LinqToDB/Internal/Linq/Preamble.cs` -- added virtual `IsInlined` (default `false`); `Query.IsAnyPreambles()` now treats an all-inlined preamble array as "no preambles" for the implicit-transaction decision. Backs CteUnion single-query mode.
- `Source/LinqToDB/Internal/Linq/Query.cs` -- removed `ConfigurationID` / `ContextType` / `InlineParameters` / `IsEntityServiceProvided` instance fields; `Query.Compare` no longer checks them (now pinned by the `QueryCache` bucket key instead); `Compare` visibility widened `protected` -> `protected internal` so `QueryCache.TryFind`/`TryAdd` (a different type in the same namespace) can call it; `IsAnyPreambles()` now skips `IsInlined` preambles.
- `Source/LinqToDB/Internal/Linq/QueryCache.cs` -- new file (1138 lines). Global bucketed `QueryCache.Default` singleton replacing the old per-`Query<T>` copy-on-write array cache; see `## Subsystems` § 1 for the full rewrite.
- `Source/LinqToDB/Internal/Linq/QueryFlags.cs` -- enum visibility `internal` -> `public`; new `HasEntityServiceInterceptor = 0x08` bit.
- `Source/LinqToDB/Internal/Linq/QueryFlagsHelper.cs` -- `GetQueryFlags` now also sets `HasEntityServiceInterceptor` when the data context is `IInterceptable<IEntityServiceInterceptor>` with a non-null interceptor (moved here from `Query`'s constructor).
- `Source/LinqToDB/Internal/Linq/QueryRunner.InsertOrReplace.cs` -- `CreateQuery` now detects `SqlProviderFlags.IsInsertOrUpdateRequiresAlignedBranches` + `UpsertBuilder.HasDivergentInsertOrUpdateBranches` and routes to the UPDATE-then-INSERT emulation instead of native `IsInsertOrUpdateSupported` when branches diverge (SAP HANA UPSERT case); `MakeAlternativeInsertOrUpdate` gained a 3-query existence-check / conditional-UPDATE / INSERT orchestration (`SetIfExistsUpdateElseInsert`) for `Upsert(...).Update(v => v.When(cond))` on providers lacking MERGE/ON CONFLICT, replacing a `// TODO! looks not working solution` comment.
- `Source/LinqToDB/Internal/Linq/QueryRunner.cs` -- new `#region IfExistsUpdateElseInsert` with `SetIfExistsUpdateElseInsert` / `IfExistsUpdateElseInsert` / `IfExistsUpdateElseInsertAsync`, the executor wiring for the 3-query orchestration above.
- `Source/LinqToDB/Internal/Linq/Query{T}.cs` -- removed the entire nested `QueryCache` class (copy-on-write array, `Monitor.TryEnter` reorder, `CacheSize = 100`) and the `static readonly QueryCache _queryCache` field; `GetQuery` / `ClearCache` / `CacheMissCount` now delegate to `QueryCache.Default`; public ctor visibility widened `internal` -> `public`.

</details>
