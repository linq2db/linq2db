---
area: TOOLS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 15/15
coverage_tier_2: 13/13
---

# TOOLS

Standalone `linq2db.Tools` NuGet package (`linq2db.Tools` assembly). Provides five opt-in utility subsystems — Activity instrumentation, entity comparers, SQL Server system-schema queries, entity identity map, and object-to-object mapping — that require the core `linq2db` package but are not shipped with it. No inbound runtime dependency from the core library; consumers add a `<PackageReference>` to `linq2db.Tools` alongside `linq2db`.

Source tree: `Source/LinqToDB.Tools/` (28 `.cs` files + `Schemas.tt` + `Schemas.generated.cs`). Assembly name: `linq2db.Tools` (`LinqToDB.Tools.csproj`). Single `<ProjectReference>` to `LinqToDB.csproj`.

## Subsystems

### Activity (`Activity/`)

Provides two concrete implementations of the core `LinqToDB.Metrics.IActivity` abstraction for use as `ActivityService.AddFactory(…)` callbacks. Neither type is needed unless the caller opts in via `ActivityService`.

- `ActivityStatistics` — static singleton registry. Holds one `StatActivity` (internal `IStatActivity`) per `ActivityID` enum value (~70 entries covering query compile, execute, ADO.NET, interceptor, DDL, bulk-copy paths). Each `StatActivity` accumulates `CallCount` (interlocked) and `ElapsedTicks` (interlocked). `ActivityStatistics.Factory(ActivityID)` is the factory delegate. `ActivityStatistics.GetReport()` formats a columnar ASCII table via `EnumerableExtensions.ToDiagnosticString`. `StatActivitySum` sums child metrics for rolled-up groups (`ExecuteTotal`, `ExecuteAdo`, `Interceptors` groups). Source: `Activity/ActivityStatistics.cs`, `Activity/StatActivity.cs`, `Activity/StatActivitySum.cs`.

- `ActivityHierarchy` — `IDisposable` tree-of-scopes recorder. Stored in an `AsyncLocal<ActivityHierarchy>` so parent-child nesting is async-safe. On root disposal (`_parent == null`), renders an indented call-tree string and invokes the user-supplied `Action<string> pushReport`. Consecutive sibling calls with the same `ActivityID` are coalesced to `(n)` suffix rather than expanding. Usage: `ActivityService.AddFactory(activityID => new ActivityHierarchy(activityID, s => …))`. Source: `Activity/ActivityHierarchy.cs`.

Both classes depend on `LinqToDB.Metrics.ActivityID`, `ActivityService`, and `ActivityBase` from the core `INTERCEPTORS` area.

### Comparers (`Comparers/`)

`ComparerBuilder` — static factory for expression-compiled `IEqualityComparer<T>` and equality/hash-code delegates. Entry points:

- `ComparerBuilder.GetEqualityComparer<T>()` — reflects all public members via `TypeAccessor`; skips members decorated with `IgnoreComparisonAttribute`. Cached in `Comparer<T>.DefaultInstance`.
- `ComparerBuilder.GetEqualityComparer<T>(params Expression<Func<T,object?>>[] members)` — member subset overload.
- `ComparerBuilder.GetEqualityComparer(Type)` — non-generic path via `MakeGenericMethod`.

`GetEqualityComparerExpression(Type)` dispatches to the correct per-element comparer: `BitArrayEqualityComparer` for `BitArray`, `EnumerableEqualityComparer<T>` for typed enumerables, `EnumerableEqualityComparer` for non-generic `IEnumerable`, recursive `ComparerBuilder.GetEqualityComparer<T>()` for classes without a custom `Equals`, and `EqualityComparer<T>.Default` otherwise. Source: `Comparers/ComparerBuilder.cs`.

`MappingSchemaExtensions` bridges this to the mapping layer: `GetKeyEqualityComparer<T>()`, `GetEntityEqualityComparer<T>()`, `GetEqualityComparer<T>(Func<ColumnDescriptor,bool>)` — all available on `MappingSchema`, `IDataContext`, and `ITable<T>`. Source: `MappingSchemaExtensions.cs`.

### DataProvider/SqlServer/Schemas (`DataProvider/SqlServer/Schemas/`)

Typed LINQ model over SQL Server `master` / system catalog views.

- `Schemas.generated.cs` — T4-generated (from `Schemas.tt`); declares ~30 typed schema models (`ObjectSchema`, `SecuritySchema`, `InformationSchema`, etc.) as nested `DataContext` properties on `SystemSchemaModel`. Generated against SQL Server 2022 (`16.00.1135`). This file is Tier 3 (auto-generated).
- `SystemSchemaModel` is the root wrapper populated by `SystemDB`.
- `SystemDB : DataConnection, ISystemSchemaData` — concrete `DataConnection` subclass; exposes `System` property (`SystemSchemaModel`). Three constructors cover `string configuration`, `DataOptions`, and `DataOptions<SystemDB>`. Source: `DataProvider/SqlServer/Schemas/SystemDB.cs`.
- `ISystemSchemaData : IDataContext` — interface requiring `System { get; }`. Allows testing against a fake implementation. Source: `DataProvider/SqlServer/Schemas/ISystemSchemaData.cs`.
- `SystemSchemaExtensions.GetTableRowCountInfo(ISystemSchemaData)` — returns an `IQueryable<TableRowCountInfo>` querying `sys.partitions` filtered to index types 0/1 and user tables (`Type == "U"`), grouped by `ObjectID`, summing `Rows`. Uses `SqlFn.ObjectSchemaName`/`SqlFn.ObjectName` and `.InlineParameters()`. Source: `DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs`.

### EntityServices (`EntityServices/`)

Opt-in identity map: two queries returning the same primary key hand back the same CLR instance.

- `IdentityMap : EntityServiceInterceptor, IDisposable` — public entry point. Constructor takes `IDataContext` and calls `_dataContext.AddInterceptor(this)`. Internally holds `ConcurrentDictionary<Type, IEntityMap>`. Overrides `EntityCreated(EntityCreatedEventData, object)` to route to `EntityMap<T>.MapEntity`. Lookup by key: `IdentityMap.GetEntity<T>(object key)` delegates to `EntityMap<T>.GetEntity`. Source: `EntityServices/IdentityMap.cs`.
- `EntityMap<T> : IEntityMap` — per-type store, keyed by `ConcurrentDictionary<T, EntityMapEntry<T>>` initialized with `dataContext.GetKeyEqualityComparer<T>()` (from `MappingSchemaExtensions`). `MapEntity` calls `GetOrAdd`; if the returned entry's entity differs from the incoming object, `args.Entity` is replaced with the cached instance. `GetEntity(IDataContext, object)` uses a per-key-type `KeyComparer<TK>` (inner class) that builds a mapper from `TK → T` via `Map.GetMapper<TK,T>` for complex keys, or direct scalar assignment for single-column PKs. Falls back to a live database query when the entity is not cached. Source: `EntityServices/EntityMap.cs`.
- `EntityMapEntry<T>` — tracks `DBCount` (incremented each DB load) and `CacheCount` (incremented each cache hit) with interlocked counters. Source: `EntityServices/EntityMapEntry.cs`.
- `EntityCreatedEventArgs` — re-used from core namespace `LinqToDB` (file `EntityServices/EntityCreatedEventArgs.cs`). Internal constructor; `Entity` is mutable so the interceptor can replace it.
- `IEntityMap` (internal interface) — `MapEntity(EntityCreatedEventArgs)` + `GetEntities()`.

### Mapper (`Mapper/`)

AutoMapper-style object-to-object conversion driven by expression compilation.

- `Map` (static helper) — `Map.GetMapper<TFrom,TTo>()`, `Map.GetMapper<TFrom,TTo>(Func<MapperBuilder<TFrom,TTo>, MapperBuilder<TFrom,TTo>>)`, `Map.DeepCopy<T>(this T obj)` (extension). `Map.DeepCopy` uses a `MapHolder<T>` static cache with `ProcessCrossReferences=true` and `DeepCopy=true`. Source: `Mapper/Map.cs`.
- `MapperBuilder<TFrom,TTo> : IMapperBuilder` — fluent configuration. Exposes `SetMappingSchema`, `FromMapping`/`ToMapping`/`Mapping` (member-rename dictionaries), `MapMember<T>` (explicit per-member setter lambda), `SetToMemberFilter`, `SetProcessCrossReferences`, `SetDeepCopy`. `GetMapper()` returns a `Mapper<TFrom,TTo>`. `GetExpressionMapper()` (internal) constructs an `ExpressionBuilder`. Source: `Mapper/MapperBuilder.cs`.
- `Mapper<TFrom,TTo>` — caches compiled delegates lazily: `GetMapper()` → `Func<TFrom,TTo>`, `GetMapperEx()` → `Func<TFrom,TTo,IDictionary<object,object>?,TTo>` (for in-place mapping with cross-reference tracking). `Map(TFrom)`, `Map(TFrom, TTo)`, `Map(TFrom, TTo, IDictionary)` call site overloads. Source: `Mapper/Mapper.cs`.
- `ExpressionBuilder` (internal) — core expression synthesis. Two paths: `GetExpression()` builds `Expression<Func<TFrom,TTo>>` (create-new); `GetExpressionEx()` builds `Expression<Func<TFrom,TTo,IDictionary<object,object>?,TTo>>` (in-place update). Handles collections (`IEnumerable<T>` → `List<T>`, `HashSet<T>`, array), cross-reference tracking via `IDictionary<object,object>` cache, recursive member mapping, and per-member `MappingSchema.GetConvertExpression` for scalar conversions. Cross-reference restart guard: if recursion exceeds 10 levels (`RestartCounter > 10`), sets `ProcessCrossReferences = true` and rebuilds with the extended path. Source: `Mapper/ExpressionBuilder.cs`.
- `MemberMapperInfo` — DTO: `ToMember: LambdaExpression`, `Setter: LambdaExpression`. Source: `Mapper/MemberMapperInfo.cs`.

### Root files

- `EnumerableExtensions.ToDiagnosticString<T>` — formats an `IEnumerable<T>` as an ASCII table. Numeric columns are right-aligned; date/decimal formatting uses `InvariantCulture`. Used by `ActivityStatistics.GetReport()`. Source: `EnumerableExtensions.cs`.
- `MappingSchemaExtensions` — `GetEqualityComparer<T>`, `GetEntityEqualityComparer<T>`, `GetKeyEqualityComparer<T>` on `MappingSchema`, `IDataContext`, and `ITable<T>`. `GetKeyEqualityComparer` falls back to all-columns comparer when no PK is defined. Source: `MappingSchemaExtensions.cs`.

## Key types

| Type | File | Role |
|---|---|---|
| `ActivityStatistics` | `Activity/ActivityStatistics.cs` | Static cumulative stats registry; factory delegate for `ActivityService` |
| `ActivityHierarchy` | `Activity/ActivityHierarchy.cs` | Async-local tree recorder; root Dispose emits indented call-tree string |
| `ComparerBuilder` | `Comparers/ComparerBuilder.cs` | Expression-compiled `IEqualityComparer<T>` factory |
| `IgnoreComparisonAttribute` | `Comparers/IgnoreComparisonAttribute.cs` | Opts a property/field out of `ComparerBuilder` reflection scan |
| `SystemDB` | `DataProvider/SqlServer/Schemas/SystemDB.cs` | `DataConnection` subclass exposing typed SQL Server system catalog |
| `ISystemSchemaData` | `DataProvider/SqlServer/Schemas/ISystemSchemaData.cs` | Interface contract for typed system catalog; testable |
| `SystemSchemaExtensions` | `DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs` | `GetTableRowCountInfo` LINQ query over `sys.partitions` |
| `IdentityMap` | `EntityServices/IdentityMap.cs` | Opt-in identity map; hooks `IEntityServiceInterceptor` |
| `EntityMap<T>` | `EntityServices/EntityMap.cs` | Per-type ConcurrentDictionary cache + DB fallback query |
| `EntityMapEntry<T>` | `EntityServices/EntityMapEntry.cs` | Cache entry with hit counters |
| `Map` | `Mapper/Map.cs` | Static entry point + `DeepCopy<T>` extension |
| `MapperBuilder<TFrom,TTo>` | `Mapper/MapperBuilder.cs` | Fluent mapper configuration |
| `Mapper<TFrom,TTo>` | `Mapper/Mapper.cs` | Caches compiled `Func<TFrom,TTo>` delegates |
| `ExpressionBuilder` | `Mapper/ExpressionBuilder.cs` | Expression-tree mapper synthesis (internal) |
| `MappingSchemaExtensions` | `MappingSchemaExtensions.cs` | Schema-aware equality comparer factory |
| `EnumerableExtensions` | `EnumerableExtensions.cs` | ASCII table formatter for diagnostics |

## Files (Tier 1 / Tier 2)

**Tier 1** (canonical anchors, all read):

| File | Role |
|---|---|
| `Activity/ActivityHierarchy.cs` | Async-local call-tree recorder |
| `Activity/ActivityStatistics.cs` | Cumulative stats registry + factory |
| `Activity/IStatActivity.cs` | Internal stat contract |
| `Comparers/ComparerBuilder.cs` | Main comparer factory |
| `Comparers/IgnoreComparisonAttribute.cs` | Opt-out attribute |
| `DataProvider/SqlServer/Schemas/ISystemSchemaData.cs` | Catalog interface |
| `DataProvider/SqlServer/Schemas/SystemDB.cs` | Catalog DataConnection |
| `DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs` | Catalog query helpers |
| `EntityServices/IEntityMap.cs` | Internal map interface |
| `EntityServices/IdentityMap.cs` | Public identity map |
| `EntityServices/EntityMap.cs` | Per-type entity store |
| `Mapper/IMapperBuilder.cs` | Mapper config interface |
| `Mapper/MapperBuilder.cs` | Fluent mapper builder |
| `Mapper/Map.cs` | Static entry point |
| `Mapper/ExpressionBuilder.cs` | Expression synthesis (internal) |

**Tier 2** (all read — area is small enough for full coverage):

| File | Notes |
|---|---|
| `Activity/StatActivity.cs` | Internal timing watcher (Stopwatch / low-res DateTime) |
| `Activity/StatActivitySum.cs` | Aggregate sum of child `IStatActivity` metrics |
| `Comparers/ArrayEqualityComparer.cs` | `T[]` comparer with recursive element dispatch |
| `Comparers/BitArrayEqualityComparer.cs` | `BitArray` bit-by-bit comparison |
| `Comparers/EnumerableEqualityComparer.cs` | Non-generic `IEnumerable` comparer |
| `Comparers/EnumerableEqualityComparer`1.cs` | Generic `IEnumerable<T>` comparer |
| `EntityServices/EntityCreatedEventArgs.cs` | Event args (mutable Entity slot) |
| `EntityServices/EntityMapEntry.cs` | Cache entry with interlocked hit counters |
| `Mapper/Mapper.cs` | Compiled delegate cache |
| `Mapper/MemberMapperInfo.cs` | DTO for explicit per-member mapper |
| `EnumerableExtensions.cs` | ASCII table formatter |
| `MappingSchemaExtensions.cs` | Schema-aware comparer factory on `MappingSchema`/`IDataContext`/`ITable<T>` |
| `LinqToDB.Tools.csproj` | Assembly name `linq2db.Tools`; single ProjectReference to LinqToDB |

**Tier 3** (generated, not read):

| File | Reason |
|---|---|
| `DataProvider/SqlServer/Schemas/Schemas.generated.cs` | T4 auto-generated from `Schemas.tt` (auto-generated marker in header) |

## Inbound / outbound dependencies

**Inbound** (packages / callers that reference `linq2db.Tools`):
- `LinqToDB.LINQPad` — no direct reference found in csproj; both reference core.
- End-user code that opts in to `IdentityMap`, `ActivityStatistics`, or `Map`.

**Outbound** (what `linq2db.Tools` depends on):
- `LinqToDB` (single `<ProjectReference>`) — uses `IDataContext`, `DataConnection`, `MappingSchema`, `EntityDescriptor`, `TypeAccessor`, `MemberAccessor`, `ActivityID`, `ActivityService`, `ActivityBase`, `EntityServiceInterceptor`, `EntityCreatedEventData`, `ITable<T>`, `SqlFn`.
- `LinqToDB.Metrics` namespace (core, `INTERCEPTORS` area): `IActivity`, `ActivityID`, `ActivityService`, `ActivityBase`.
- `LinqToDB.Interceptors` namespace (core, `INTERCEPTORS` area): `EntityServiceInterceptor`.

## Known issues / debt

- `StatActivity.Start()` branches on `Environment.OSVersion.Platform != PlatformID.Unix` to pick `Stopwatch` vs. `DateTime.Now` fallback. The platform check is a blunt instrument — `DateTime.Now` has ~15 ms resolution on Windows, making it unreliable for sub-millisecond activity tracking. Source: `Activity/StatActivity.cs:20`.
- `ActivityStatistics` is fully static — all statistics are process-global. Parallel test runs or multi-tenant server scenarios will see interleaved counts with no way to reset or scope per-call-site. No `Reset()` method exists.
- `ExpressionBuilder.ConvertCollection` throws `NotImplementedException` for non-List, non-HashSet, non-array `IEnumerable<T>` targets (`Mapper/ExpressionBuilder.cs:277`). Consumers mapping to custom collection types will hit this at runtime.
- `Schemas.generated.cs` was generated against SQL Server 2022 (`16.00.1135`). The `Schemas.tt` template must be re-run against a live SQL Server instance to refresh the catalog model — there is no automated regeneration step in CI.
- `EntityMap<T>.GetEntity` issues a live DB query (`context.GetTable<T>().Where(…).FirstOrDefault()`) when the entity is not in cache, silently bypassing the identity-map contract — callers may not expect a remote call from a cache-lookup method.

## See also

- [INTERCEPTORS area INDEX](../INTERCEPTORS/INDEX.md) — owns `IActivity`, `ActivityID`, `ActivityService`; `ActivityBase` base class for `StatActivity`/`ActivityHierarchy`.
- [IN-TREE-TOOLS area INDEX](../IN-TREE-TOOLS/INDEX.md) — the `DataExtensions.RetrieveIdentity<T>` helper inside the main `linq2db.dll`; distinct package.
- [MAPPING area INDEX](../MAPPING/INDEX.md) — `MappingSchema`, `EntityDescriptor`, `ColumnDescriptor` consumed by `MappingSchemaExtensions` and `EntityMap`.
- [PROV-SQLSERVER area INDEX](../PROV-SQLSERVER/INDEX.md) — `SqlFn` used by `SystemSchemaExtensions.GetTableRowCountInfo`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 15 / 15 ✓
  - Source/LinqToDB.Tools/Activity/ActivityHierarchy.cs
  - Source/LinqToDB.Tools/Activity/ActivityStatistics.cs
  - Source/LinqToDB.Tools/Activity/IStatActivity.cs
  - Source/LinqToDB.Tools/Comparers/ComparerBuilder.cs
  - Source/LinqToDB.Tools/Comparers/IgnoreComparisonAttribute.cs
  - Source/LinqToDB.Tools/DataProvider/SqlServer/Schemas/ISystemSchemaData.cs
  - Source/LinqToDB.Tools/DataProvider/SqlServer/Schemas/SystemDB.cs
  - Source/LinqToDB.Tools/DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs
  - Source/LinqToDB.Tools/EntityServices/IEntityMap.cs
  - Source/LinqToDB.Tools/EntityServices/IdentityMap.cs
  - Source/LinqToDB.Tools/EntityServices/EntityMap.cs
  - Source/LinqToDB.Tools/Mapper/IMapperBuilder.cs
  - Source/LinqToDB.Tools/Mapper/MapperBuilder.cs
  - Source/LinqToDB.Tools/Mapper/Map.cs
  - Source/LinqToDB.Tools/Mapper/ExpressionBuilder.cs
- Tier 2 (visited / total): 13 / 13 (100%) ✓
  - Source/LinqToDB.Tools/Activity/StatActivity.cs
  - Source/LinqToDB.Tools/Activity/StatActivitySum.cs
  - Source/LinqToDB.Tools/Comparers/ArrayEqualityComparer.cs
  - Source/LinqToDB.Tools/Comparers/BitArrayEqualityComparer.cs
  - Source/LinqToDB.Tools/Comparers/EnumerableEqualityComparer.cs
  - Source/LinqToDB.Tools/Comparers/EnumerableEqualityComparer`1.cs
  - Source/LinqToDB.Tools/EntityServices/EntityCreatedEventArgs.cs
  - Source/LinqToDB.Tools/EntityServices/EntityMapEntry.cs
  - Source/LinqToDB.Tools/Mapper/Mapper.cs
  - Source/LinqToDB.Tools/Mapper/MemberMapperInfo.cs
  - Source/LinqToDB.Tools/EnumerableExtensions.cs
  - Source/LinqToDB.Tools/MappingSchemaExtensions.cs
  - Source/LinqToDB.Tools/LinqToDB.Tools.csproj
- Tier 3 (skipped, logged): 1
  - Source/LinqToDB.Tools/DataProvider/SqlServer/Schemas/Schemas.generated.cs (T4 auto-generated)
</details>
