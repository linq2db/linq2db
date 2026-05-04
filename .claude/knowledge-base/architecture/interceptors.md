---
area: INTERCEPTORS
kind: architecture
sources: [code]
confidence: high
last_verified: 2026-05-03
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 12/12
coverage_tier_2: 15/15
---

# Interceptors and Metrics — Architecture

Cross-area narrative for pipeline interception and activity tracing. For the per-type API reference, see [areas/INTERCEPTORS/INDEX.md](../areas/INTERCEPTORS/INDEX.md).

## Registration model

Both `DataConnection` and `DataContext` implement `IInterceptable<T>` for all 8 interceptor types (7 public + `IEntityBindingInterceptor`). Each implementation holds one typed slot per interceptor type.

`InterceptorInternalExtensions.AddInterceptorImpl` (`Source/LinqToDB/Internal/Interceptors/InterceptorInternalExtensions.cs:12`) is the single write path. It type-switches on the `IInterceptor` concrete type and:

1. **Empty slot** — stores the interceptor directly in the typed property.
2. **Single interceptor already present** — wraps both in a new `Aggregated*` instance.
3. **`Aggregated*` already present** — appends to its `Interceptors` list.

Removal reverses this: if the slot holds an `Aggregated*`, it calls `AggregatedInterceptor<T>.Remove` (deferred when the list is being enumerated); if the slot holds the exact instance, it nulls the slot.

**`DataConnection` vs `DataContext` difference.** `DataConnection` uses untyped interface slots (the `IInterceptable<T>.Interceptor` property is the plain interface type). `DataContext` uses concrete `Aggregated*` fields (e.g. `AggregatedCommandInterceptor? _commandInterceptor`) cast via the interface property. Additionally, `DataContext.AddInterceptor` stores every interceptor into `DataOptions` via `Options.UseInterceptor(interceptor)` so it is re-applied when `DataContext` creates a new underlying `DataConnection` after reconnect (`Source/LinqToDB/DataContext.Interceptors.cs:82`).

## Dispatch order

Within one interceptor type, the execution order is **registration order** — the order `AddInterceptor` was called. `AggregatedInterceptor<T>` iterates `Interceptors` from index 0 upward.

For interceptors that return a result (all `ICommandInterceptor` non-void methods, `IEntityServiceInterceptor.EntityCreated`, `IQueryExpressionInterceptor.ProcessExpression`, `IUnwrapDataObjectInterceptor.*`), each interceptor in the chain receives the output of its predecessor as input. The first interceptor receives `Option<T>.None` (for command interceptors) or the base value (for others). Returning `Option<T>.None` from a command interceptor signals "do not short-circuit" — the framework will execute the command normally. Returning `Option<T>.Some(value)` short-circuits execution.

For void events (`IConnectionInterceptor`, `IDataContextInterceptor`, `IExceptionInterceptor`), all registered interceptors are called unconditionally.

**Safe removal during dispatch.** `AggregatedInterceptor<T>` sets `_enumerating = true` before the foreach loop and calls `RemoveDelayed()` in the `finally` block. Calls to `Remove` during enumeration are queued in `_removeList` and applied afterward (`Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs:34`).

## Async/sync pairing

Every interceptor interface that has synchronous methods also has async counterparts. The pattern is strict:

- Sync: `void` or `Option<T>` / `DbDataReader` / `object` return.
- Async: `Task` or `Task<Option<T>>` return; extra `CancellationToken cancellationToken` parameter.
- `AfterExecuteReader` has only a sync variant (`// no async version for now` comment at `Source/LinqToDB/Interceptors/ICommandInterceptor.cs:92`).
- `IEntityServiceInterceptor`, `IExceptionInterceptor`, `IUnwrapDataObjectInterceptor`, `IQueryExpressionInterceptor`, `IEntityBindingInterceptor` are sync-only throughout.

Abstract base classes provide no-op implementations for all methods, allowing implementors to override only the methods they care about.

## Metrics integration

`ActivityService.Start(ActivityID)` is called at every interceptor dispatch site inside `Aggregated*` classes. This means each individual interceptor invocation is wrapped in an activity span when a metrics factory is registered. When no factory is registered, `Start` returns `null` immediately (zero allocation, zero overhead).

The double-wrapping pattern for async interceptors:
```
await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteScalarAsync))
    result = await interceptor.ExecuteScalarAsync(...).ConfigureAwait(false);
```
`StartAndConfigureAwait` returns an `AsyncDisposableWrapper?` (null when no factory), so the `await using` is a no-op when metrics are off (`Source/LinqToDB/Metrics/ActivityService.cs:54`).

`ActivityID` values for interceptor calls follow the naming pattern `<InterceptorType>Interceptor<MethodName>`, e.g. `CommandInterceptorExecuteScalar`, `ConnectionInterceptorConnectionOpening`. The enum also covers non-interceptor spans for the full query pipeline (query building, SQL emission, bulk copy, transaction management) — these are emitted directly by [DATA](../areas/DATA/INDEX.md) and [LINQ](../areas/LINQ/INDEX.md) without going through the interceptor layer.

## Multiple metrics factories

`ActivityService.AddFactory` supports multiple registered factories via multicast. With two+ factories, `Start` calls `StartImpl` which allocates a `MultiActivity` that fans out `AddTag`/`Dispose`/`DisposeAsync` to all underlying activities. First-factory path avoids this allocation (`Source/LinqToDB/Metrics/ActivityService.cs:29`).

## IEntityBindingInterceptor — internal-only type

`IEntityBindingInterceptor` (`Source/LinqToDB/Internal/Interceptors/IEntityBindingInterceptor.cs`) sits in the `LinqToDB.Internal.Interceptors` namespace and is not part of the public API surface. It is dispatched by the LINQ→SQL builder during entity materialization to allow `SqlGenericConstructorExpression` rewriting. It is registered and removed through the same `AddInterceptorImpl`/`RemoveInterceptorImpl` paths as public interceptors.

## IQueryExpressionInterceptor — pipeline entry

`IQueryExpressionInterceptor.ProcessExpression` is called by the [EXPR-TRANS](../areas/EXPR-TRANS/INDEX.md) pipeline before translation begins. `QueryExpressionArgs.Kind` distinguishes four call sites:

- `Query` — main query expression.
- `ExposedQuery` — expression exposed for diagnostic/tooling.
- `AssociationExpression` — association sub-expression.
- `QueryFilter` — filter expression applied via global filters.

## ConnectionOptionsConnectionInterceptor — bridge from DataOptions

`ConnectionOptionsConnectionInterceptor` bridges `DataOptions.ConnectionOpening` / `ConnectionOpened` callback properties into the `IConnectionInterceptor` interface. It is registered automatically when those options properties are set. The async callback is invoked when present; otherwise the sync callback fires (`Source/LinqToDB/Interceptors/ConnectionOptionsConnectionInterceptor.cs:32`).

## Pointers

- [areas/INTERCEPTORS/INDEX.md](../areas/INTERCEPTORS/INDEX.md) — per-type API reference, file lists.
- [areas/DATA/INDEX.md](../areas/DATA/INDEX.md) — `DataConnection` call sites.
- [areas/CORE/INDEX.md](../areas/CORE/INDEX.md) — `DataContext` options-persistence flow.
- [areas/EXPR-TRANS/INDEX.md](../areas/EXPR-TRANS/INDEX.md) — `IQueryExpressionInterceptor` and `IEntityBindingInterceptor` call sites.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 12 / 12 ✓
  - (same list as areas/INTERCEPTORS/INDEX.md — this artifact covers the same scope)
- Tier 2 (visited / total): 15 / 15 (100%) ✓
- Tier 3 (skipped, logged): 0
</details>
