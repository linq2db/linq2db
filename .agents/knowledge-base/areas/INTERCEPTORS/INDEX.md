---
area: INTERCEPTORS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-03
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 12/12
coverage_tier_2: 15/15
---

# INTERCEPTORS

Pipeline interception hooks and metrics activity tracing. Public contracts live in `Source/LinqToDB/Interceptors/` and `Source/LinqToDB/Metrics/`; dispatch machinery lives in `Source/LinqToDB/Internal/Interceptors/` (owned by [INTERNAL-API](../INTERNAL-API/INDEX.md) by path, but architecturally part of this area).

## Subsystems

### 1 — Public interceptor interfaces (8 interfaces)

All derive from `IInterceptor` (`Source/LinqToDB/Interceptors/IInterceptor.cs:3`) — a marker interface with no members.

| Interface | Scope | Key pattern |
|---|---|---|
| `ICommandInterceptor` | ADO command lifecycle | sync+async pairs; `Option<T>` return allows short-circuiting |
| `IConnectionInterceptor` | Connection open (before/after) | void + `Task` pairs |
| `IDataContextInterceptor` | Context close (before/after) | void + `Task` pairs |
| `IEntityServiceInterceptor` | Query materialization | sync only; returns modified entity |
| `IExceptionInterceptor` | DB exception catch | sync only; throw to replace exception |
| `IQueryExpressionInterceptor` | LINQ expression tree before translation | sync only; returns `Expression` |
| `IUnwrapDataObjectInterceptor` | Proxy/wrapper unwrapping | sync only; returns unwrapped ADO object |
| `IEntityBindingInterceptor` | Constructor-expression rewriting | internal-only (`LinqToDB.Internal.Interceptors`); takes/returns `SqlGenericConstructorExpression` |

`IEntityBindingInterceptor` (`Source/LinqToDB/Internal/Interceptors/IEntityBindingInterceptor.cs:5`) is not in the public `Interceptors/` namespace — it is internal, registered via the same `AddInterceptorImpl` dispatch, but not meant for end-users.

### 2 — Abstract base classes (convenience)

Each public interceptor interface has a paired abstract class with no-op virtual implementations:

- `CommandInterceptor` (`Source/LinqToDB/Interceptors/CommandInterceptor.cs`) — all methods delegate or return `result` / `Task.CompletedTask`.
- `ConnectionInterceptor`, `DataContextInterceptor`, `EntityServiceInterceptor`, `ExceptionInterceptor`, `UnwrapDataObjectInterceptor` — follow the same pattern.
- `ConnectionOptionsConnectionInterceptor` (`Source/LinqToDB/Interceptors/ConnectionOptionsConnectionInterceptor.cs`) — sealed concrete subclass of `ConnectionInterceptor`; bridges `DataOptions.ConnectionOpening`/`ConnectionOpened` callback delegates into the interceptor interface. Used internally when options carry connection callbacks.

### 3 — Event data structs

Each interceptor receives a typed `readonly struct` event data argument carrying the current `DataConnection` or `IDataContext`:

- `CommandEventData` — holds `DataConnection` (non-nullable).
- `ConnectionEventData` — holds `DataConnection?` (nullable; absent for non-`DataConnection`-originating connections, e.g. bulk-copy paths).
- `DataContextEventData` — holds `IDataContext`.
- `EntityCreatedEventData` — holds `IDataContext` + table qualification (`TableName`, `SchemaName`, `DatabaseName`, `ServerName`, `TableOptions`).
- `ExceptionEventData` — holds `IDataContext`.
- `QueryExpressionArgs` — holds `IDataContext`, the `Expression`, and `ExpressionKind` (`Query`, `ExposedQuery`, `AssociationExpression`, `QueryFilter`).

### 4 — Aggregated dispatch layer (Internal)

`AggregatedInterceptor<TInterceptor>` (`Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs`) is the fan-out base:

- Holds `List<TInterceptor> Interceptors`.
- Tracks an `_enumerating` flag to allow safe removal mid-iteration (deferred via `_removeList`).
- Each method loops all registered interceptors in registration order, passing the output of one as the input to the next (chain pattern for `Option<T>` and `Expression` results; broadcast for void events).
- Every call is wrapped in `ActivityService.Start(ActivityID.*)` so each interceptor invocation is individually measurable.

Concrete sealed subclasses: `AggregatedCommandInterceptor`, `AggregatedConnectionInterceptor`, `AggregatedDataContextInterceptor`, `AggregatedEntityServiceInterceptor`, `AggregatedExceptionInterceptor`, `AggregatedQueryExpressionInterceptor`, `AggregatedUnwrapDataObjectInterceptor`, `AggregatedEntityBindingInterceptor`.

### 5 — Registration model

`IInterceptable<T>` (`Source/LinqToDB/Internal/Interceptors/IInterceptable.cs`) is implemented by `DataConnection` and `DataContext`, one typed property slot per interceptor type. `InterceptorInternalExtensions.AddInterceptorImpl` (`Source/LinqToDB/Internal/Interceptors/InterceptorInternalExtensions.cs:12`) type-switches on the concrete `IInterceptor` and either:

1. Sets the typed slot directly (first interceptor of that type), or
2. Creates an `Aggregated*` wrapper holding the existing + new interceptor (second interceptor), or
3. Appends to the existing `Aggregated*` wrapper's `Interceptors` list (third+).

`DataConnection.AddInterceptor` delegates to `AddInterceptorImpl` directly (`Source/LinqToDB/Data/DataConnection.Interceptors.cs:28`).

`DataContext.AddInterceptor` additionally stores the interceptor in `DataOptions` (via `Options.UseInterceptor`) so it survives context cloning/reconfiguration. On `DataContext`, the typed slots are pre-typed as `Aggregated*` (not a bare interface), so that reconnect after re-open re-adds all interceptors from options to a fresh underlying `DataConnection` (`Source/LinqToDB/DataContext.Interceptors.cs:99`).

### 6 — One-time interceptors

`InterceptorExtensions.OnNextCommandInitialized` (`Source/LinqToDB/Interceptors/InterceptorExtensions.cs:23`) is the public entry for single-command interception. It registers a `OneTimeCommandInterceptor` (`Source/LinqToDB/Internal/Interceptors/OneTimeCommandInterceptor.cs`) which calls `dataConnection.RemoveInterceptor(this)` inside `CommandInitialized` after firing once.

### 7 — Metrics / ActivityService

`ActivityService` (`Source/LinqToDB/Metrics/ActivityService.cs:17`) is a static registry of `Func<ActivityID, IActivity?>` factories:

- Default `Start` delegate returns `null` (zero-cost when no factory is registered).
- `AddFactory(factory)` registers a delegate; first call makes `Start` a direct delegate invoke; second+ call switches `Start` to `StartImpl` which fans out across all registered factories via `MultiActivity`.
- `MultiActivity` (nested sealed class) implements `IActivity` and broadcasts all tag/dispose calls across its array of `IActivity?` instances.
- `AsyncDisposableWrapper` (nested sealed class) adapts `IActivity` to `ConfiguredValueTaskAwaitable` for use in `await using` blocks inside the aggregated interceptors.

`ActivityID` (`Source/LinqToDB/Metrics/ActivityID.cs`) is the full enumeration: covers query pipeline stages (`QueryProviderExecuteT` through `GetIEnumerable`), execution paths (`ExecuteQuery`, `ExecuteScalar`, `ExecuteNonQuery`, etc.), connection/transaction lifecycle, each interceptor call site, and internal spans (`Materialization`, `OnTraceInternal`). Total ~80 values.

`IActivity` (`Source/LinqToDB/Metrics/IActivity.cs:14`) extends `IDisposable` + `IAsyncDisposable`. Methods: `AddTag(ActivityTagID, object?)` and `AddQueryInfo(DataConnection?, DbConnection?, DbCommand?)`.

`ActivityBase` (`Source/LinqToDB/Metrics/ActivityBase.cs`) provides a default `AddQueryInfo` implementation that calls `AddTag` for `ConfigurationString`, `DataProviderName`, `DataSourceName`, `DatabaseName`, `CommandText`; wraps `DataSource` and `Database` reads in try/catch (some providers throw on failed initialization).

`ActivityTagID` (`Source/LinqToDB/Metrics/ActivityTagID.cs`) — 5 values: `ConfigurationString`, `DataProviderName`, `DataSourceName`, `DatabaseName`, `CommandText`.

## Key types

| Type | File | Role |
|---|---|---|
| `IInterceptor` | `Source/LinqToDB/Interceptors/IInterceptor.cs` | Marker base for all interceptors |
| `ICommandInterceptor` | `Source/LinqToDB/Interceptors/ICommandInterceptor.cs` | Command lifecycle hooks |
| `IConnectionInterceptor` | `Source/LinqToDB/Interceptors/IConnectionInterceptor.cs` | Connection-open hooks |
| `IDataContextInterceptor` | `Source/LinqToDB/Interceptors/IDataContextInterceptor.cs` | Context-close hooks |
| `IEntityServiceInterceptor` | `Source/LinqToDB/Interceptors/IEntityServiceInterceptor.cs` | Materialization hook |
| `IExceptionInterceptor` | `Source/LinqToDB/Interceptors/IExceptionInterceptor.cs` | Exception replacement hook |
| `IQueryExpressionInterceptor` | `Source/LinqToDB/Interceptors/IQueryExpressionInterceptor.cs` | Expression-tree rewrite hook |
| `IUnwrapDataObjectInterceptor` | `Source/LinqToDB/Interceptors/IUnwrapDataObjectInterceptor.cs` | ADO proxy-unwrap hook |
| `InterceptorExtensions` | `Source/LinqToDB/Interceptors/InterceptorExtensions.cs` | Public registration helpers |
| `AggregatedInterceptor<T>` | `Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs` | Fan-out base |
| `IInterceptable<T>` | `Source/LinqToDB/Internal/Interceptors/IInterceptable.cs` | Per-type slot on context |
| `InterceptorInternalExtensions` | `Source/LinqToDB/Internal/Interceptors/InterceptorInternalExtensions.cs` | Type-switch dispatch |
| `OneTimeCommandInterceptor` | `Source/LinqToDB/Internal/Interceptors/OneTimeCommandInterceptor.cs` | Self-removing one-shot |
| `ActivityService` | `Source/LinqToDB/Metrics/ActivityService.cs` | Metrics factory registry |
| `IActivity` | `Source/LinqToDB/Metrics/IActivity.cs` | Metrics span contract |
| `ActivityID` | `Source/LinqToDB/Metrics/ActivityID.cs` | Span identifier enum |
| `ActivityBase` | `Source/LinqToDB/Metrics/ActivityBase.cs` | Default `AddQueryInfo` impl |

## Files (Tier 1 / Tier 2)

### Tier 1
- `Source/LinqToDB/Interceptors/IInterceptor.cs`
- `Source/LinqToDB/Interceptors/ICommandInterceptor.cs`
- `Source/LinqToDB/Interceptors/IConnectionInterceptor.cs`
- `Source/LinqToDB/Interceptors/IDataContextInterceptor.cs`
- `Source/LinqToDB/Interceptors/IEntityServiceInterceptor.cs`
- `Source/LinqToDB/Interceptors/IExceptionInterceptor.cs`
- `Source/LinqToDB/Interceptors/IQueryExpressionInterceptor.cs`
- `Source/LinqToDB/Interceptors/IUnwrapDataObjectInterceptor.cs`
- `Source/LinqToDB/Interceptors/InterceptorExtensions.cs`
- `Source/LinqToDB/Metrics/IActivity.cs`
- `Source/LinqToDB/Metrics/ActivityService.cs`
- `Source/LinqToDB/Metrics/ActivityID.cs`

### Tier 2 (all visited)
- `Source/LinqToDB/Interceptors/CommandEventData.cs`
- `Source/LinqToDB/Interceptors/CommandInterceptor.cs`
- `Source/LinqToDB/Interceptors/ConnectionEventData.cs`
- `Source/LinqToDB/Interceptors/ConnectionInterceptor.cs`
- `Source/LinqToDB/Interceptors/ConnectionOptionsConnectionInterceptor.cs`
- `Source/LinqToDB/Interceptors/DataContextEventData.cs`
- `Source/LinqToDB/Interceptors/DataContextInterceptor.cs`
- `Source/LinqToDB/Interceptors/EntityCreatedEventData.cs`
- `Source/LinqToDB/Interceptors/EntityServiceInterceptor.cs`
- `Source/LinqToDB/Interceptors/ExceptionEventData.cs`
- `Source/LinqToDB/Interceptors/ExceptionInterceptor.cs`
- `Source/LinqToDB/Interceptors/QueryExpressionArgs.cs`
- `Source/LinqToDB/Interceptors/UnwrapDataObjectInterceptor.cs`
- `Source/LinqToDB/Metrics/ActivityBase.cs`
- `Source/LinqToDB/Metrics/ActivityTagID.cs`

## Inbound / outbound dependencies

**Inbound** (callers of the interceptor hooks):
- [DATA](../DATA/INDEX.md) — `DataConnection` implements all 8 `IInterceptable<T>` slots; calls interceptors at command execution, connection open, context close, exception catch sites (`Source/LinqToDB/Data/DataConnection.cs`).
- [CORE](../CORE/INDEX.md) — `DataContext` implements all 8 `IInterceptable<T>` slots; additionally syncs interceptors with `DataOptions` for persistence across reconnects (`Source/LinqToDB/DataContext.Interceptors.cs`).
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — calls `IQueryExpressionInterceptor.ProcessExpression` before LINQ→SQL translation and `IEntityBindingInterceptor.ConvertConstructorExpression` during entity binding.

**Outbound** (what this area uses):
- `LinqToDB.Common.Option<T>` ([INFRA](../INFRA/INDEX.md)) — return type for command interceptors allowing short-circuiting.
- `SqlGenericConstructorExpression` ([EXPR-TRANS](../EXPR-TRANS/INDEX.md)) — parameter and return type of `IEntityBindingInterceptor`.
- `DataConnection`, `IDataContext` ([CORE](../CORE/INDEX.md)/[DATA](../DATA/INDEX.md)) — carried in event-data structs.

## See also

- [architecture/interceptors.md](../../architecture/interceptors.md) — cross-area narrative: dispatch order, async/sync pairing, metrics integration.
- [CORE INDEX.md](../CORE/INDEX.md) — `DataContext` interceptor persistence via `DataOptions`.
- [DATA INDEX.md](../DATA/INDEX.md) — `DataConnection` interceptor call sites.
- [INTERNAL-API INDEX.md](../INTERNAL-API/INDEX.md) — `AggregatedInterceptor<T>` and `IInterceptable<T>` live under `Internal/Interceptors/`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 12 / 12 ✓
  - Source/LinqToDB/Interceptors/IInterceptor.cs
  - Source/LinqToDB/Interceptors/ICommandInterceptor.cs
  - Source/LinqToDB/Interceptors/IConnectionInterceptor.cs
  - Source/LinqToDB/Interceptors/IDataContextInterceptor.cs
  - Source/LinqToDB/Interceptors/IEntityServiceInterceptor.cs
  - Source/LinqToDB/Interceptors/IExceptionInterceptor.cs
  - Source/LinqToDB/Interceptors/IQueryExpressionInterceptor.cs
  - Source/LinqToDB/Interceptors/IUnwrapDataObjectInterceptor.cs
  - Source/LinqToDB/Interceptors/InterceptorExtensions.cs
  - Source/LinqToDB/Metrics/IActivity.cs
  - Source/LinqToDB/Metrics/ActivityService.cs
  - Source/LinqToDB/Metrics/ActivityID.cs
- Tier 2 (visited / total): 15 / 15 (100%) ✓
  - Source/LinqToDB/Interceptors/CommandEventData.cs
  - Source/LinqToDB/Interceptors/CommandInterceptor.cs
  - Source/LinqToDB/Interceptors/ConnectionEventData.cs
  - Source/LinqToDB/Interceptors/ConnectionInterceptor.cs
  - Source/LinqToDB/Interceptors/ConnectionOptionsConnectionInterceptor.cs
  - Source/LinqToDB/Interceptors/DataContextEventData.cs
  - Source/LinqToDB/Interceptors/DataContextInterceptor.cs
  - Source/LinqToDB/Interceptors/EntityCreatedEventData.cs
  - Source/LinqToDB/Interceptors/EntityServiceInterceptor.cs
  - Source/LinqToDB/Interceptors/ExceptionEventData.cs
  - Source/LinqToDB/Interceptors/ExceptionInterceptor.cs
  - Source/LinqToDB/Interceptors/QueryExpressionArgs.cs
  - Source/LinqToDB/Interceptors/UnwrapDataObjectInterceptor.cs
  - Source/LinqToDB/Metrics/ActivityBase.cs
  - Source/LinqToDB/Metrics/ActivityTagID.cs
- Tier 3 (skipped, logged): 0
- Cross-area reads (context only, not counted in Tier totals):
  - Source/LinqToDB/Internal/Interceptors/ (14 files — INTERNAL-API area, read for dispatch narrative)
  - Source/LinqToDB/Data/DataConnection.Interceptors.cs (DATA area)
  - Source/LinqToDB/DataContext.Interceptors.cs (CORE area)
</details>
