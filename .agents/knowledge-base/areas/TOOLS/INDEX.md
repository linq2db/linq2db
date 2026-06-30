---
area: TOOLS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 15/15
coverage_tier_2: 13/13
---

# TOOLS

Standalone `linq2db.Tools` NuGet package (`linq2db.Tools` assembly). Provides five opt-in utility subsystems -- Activity instrumentation, entity comparers, SQL Server system-schema queries, entity identity map, and object-to-object mapping -- that require the core `linq2db` package but are not shipped with it. No inbound runtime dependency from the core library; consumers add a `<PackageReference>` to `linq2db.Tools` alongside `linq2db`.

Source tree: `Source/LinqToDB.Tools/` (28 `.cs` files + `Schemas.tt` + `Schemas.generated.cs`). Assembly name: `linq2db.Tools` (`LinqToDB.Tools.csproj`). Single `<ProjectReference>` to `LinqToDB.csproj`.

## Subsystems

### Activity (`Activity/`)

Two concrete implementations of the core `LinqToDB.Metrics.IActivity` abstraction for use as `ActivityService.AddFactory(...)` callbacks.

- `ActivityStatistics` -- static singleton registry. Holds one `StatActivity` per `ActivityID` enum value (~70 entries). Each accumulates `CallCount` + `ElapsedTicks` (interlocked). `ActivityStatistics.Factory(ActivityID)` is the factory delegate; `GetReport()` formats a columnar ASCII table via `EnumerableExtensions.ToDiagnosticString`. `StatActivitySum` sums child metrics for rolled-up groups (`ExecuteTotal`, `ExecuteAdo`, `Interceptors`). Source: `Activity/ActivityStatistics.cs`, `StatActivity.cs`, `StatActivitySum.cs`.
- `ActivityHierarchy` -- `IDisposable` tree-of-scopes recorder stored in `AsyncLocal<ActivityHierarchy>`. On root disposal renders an indented call-tree string and invokes `Action<string> pushReport`. Consecutive same-`ActivityID` siblings coalesce to `(n)`. Source: `Activity/ActivityHierarchy.cs`.

Both depend on `LinqToDB.Metrics.ActivityID`/`ActivityService`/`ActivityBase` (INTERCEPTORS area).

### Comparers (`Comparers/`)

`ComparerBuilder` -- static factory for expression-compiled `IEqualityComparer<T>`. Entry points: `GetEqualityComparer<T>()` (reflects public members via `TypeAccessor`, skips `IgnoreComparisonAttribute`, cached in `Comparer<T>.DefaultInstance`); `GetEqualityComparer<T>(params Expression<Func<T,object?>>[])` (subset); `GetEqualityComparer(Type)` (non-generic). `GetEqualityComparerExpression(Type)` dispatches to `BitArrayEqualityComparer`, `EnumerableEqualityComparer<T>`/`EnumerableEqualityComparer`, recursive comparer, or `EqualityComparer<T>.Default`. Source: `Comparers/ComparerBuilder.cs`.

`MappingSchemaExtensions` bridges to mapping: `GetKeyEqualityComparer<T>()`, `GetEntityEqualityComparer<T>()`, `GetEqualityComparer<T>(Func<ColumnDescriptor,bool>)` on `MappingSchema`, `IDataContext`, `ITable<T>`. Source: `MappingSchemaExtensions.cs`.

### DataProvider/SqlServer/Schemas (`DataProvider/SqlServer/Schemas/`)

Typed LINQ model over SQL Server system catalog views.
- `Schemas.generated.cs` -- T4-generated (from `Schemas.tt`); ~30 typed schema models on `SystemSchemaModel`. Generated against SQL Server 2022 (`16.00.1135`). Tier 3.
- `SystemDB : DataConnection, ISystemSchemaData` -- exposes `System` (`SystemSchemaModel`). Three ctors (`string`, `DataOptions`, `DataOptions<SystemDB>`).
- `ISystemSchemaData : IDataContext` -- requires `System { get; }`.
- `SystemSchemaExtensions.GetTableRowCountInfo(ISystemSchemaData)` -- `IQueryable<TableRowCountInfo>` over `sys.partitions` (index types 0/1, user tables), grouped by `ObjectID`, summing `Rows`; uses `SqlFn.ObjectSchemaName`/`ObjectName` + `.InlineParameters()`.

### EntityServices (`EntityServices/`)

Opt-in identity map.
- `IdentityMap : EntityServiceInterceptor, IDisposable` -- ctor takes `IDataContext`, calls `AddInterceptor(this)`; holds `ConcurrentDictionary<Type, IEntityMap>`; overrides `EntityCreated` to route to `EntityMap<T>.MapEntity`; `GetEntity<T>(object key)` delegates to `EntityMap<T>.GetEntity`.
- `EntityMap<T> : IEntityMap` -- per-type `ConcurrentDictionary<T, EntityMapEntry<T>>` keyed by `GetKeyEqualityComparer<T>()`; `MapEntity` GetOrAdd replaces `args.Entity` with the cached instance on hit; `GetEntity` builds a `TK -> T` mapper for complex keys (falls back to live DB query when uncached).
- `EntityMapEntry<T>` -- interlocked `DBCount`/`CacheCount`.
- `EntityCreatedEventArgs` (namespace `LinqToDB`); `IEntityMap` internal interface.

### Mapper (`Mapper/`)

Object-to-object conversion via expression compilation.
- `Map` -- `GetMapper<TFrom,TTo>()`, configured-overload, `DeepCopy<T>(this T)` (uses `MapHolder<T>` cache, `ProcessCrossReferences=true`, `DeepCopy=true`).
- `MapperBuilder<TFrom,TTo> : IMapperBuilder` -- fluent config (`SetMappingSchema`, `FromMapping`/`ToMapping`/`Mapping`, `MapMember<T>`, `SetToMemberFilter`, `SetProcessCrossReferences`, `SetDeepCopy`); `GetMapper()` -> `Mapper<TFrom,TTo>`.
- `Mapper<TFrom,TTo>` -- caches `Func<TFrom,TTo>` / `Func<TFrom,TTo,IDictionary<object,object>?,TTo>`.
- `ExpressionBuilder` (internal) -- `GetExpression()` (create-new) / `GetExpressionEx()` (in-place); handles collections, cross-reference tracking, recursive member mapping, per-member `GetConvertExpression`; restart guard at recursion depth > 10.
- `MemberMapperInfo` DTO.

### Root files

- `EnumerableExtensions.ToDiagnosticString<T>` -- ASCII table formatter (numeric right-aligned, InvariantCulture).
- `MappingSchemaExtensions` -- comparer factory on `MappingSchema`/`IDataContext`/`ITable<T>`; `GetKeyEqualityComparer` falls back to all-columns when no PK.

## Key types

| Type | File | Role |
|---|---|---|
| `ActivityStatistics` | `Activity/ActivityStatistics.cs` | Static cumulative stats registry; factory delegate |
| `ActivityHierarchy` | `Activity/ActivityHierarchy.cs` | Async-local tree recorder |
| `ComparerBuilder` | `Comparers/ComparerBuilder.cs` | Expression-compiled `IEqualityComparer<T>` factory |
| `IgnoreComparisonAttribute` | `Comparers/IgnoreComparisonAttribute.cs` | Opt-out of comparer scan |
| `SystemDB` | `DataProvider/SqlServer/Schemas/SystemDB.cs` | Typed SQL Server system catalog |
| `ISystemSchemaData` | `DataProvider/SqlServer/Schemas/ISystemSchemaData.cs` | Catalog interface (testable) |
| `SystemSchemaExtensions` | `DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs` | `GetTableRowCountInfo` query |
| `IdentityMap` | `EntityServices/IdentityMap.cs` | Opt-in identity map |
| `EntityMap<T>` | `EntityServices/EntityMap.cs` | Per-type cache + DB fallback |
| `EntityMapEntry<T>` | `EntityServices/EntityMapEntry.cs` | Cache entry with hit counters |
| `Map` | `Mapper/Map.cs` | Static entry + `DeepCopy<T>` |
| `MapperBuilder<TFrom,TTo>` | `Mapper/MapperBuilder.cs` | Fluent mapper config |
| `Mapper<TFrom,TTo>` | `Mapper/Mapper.cs` | Compiled delegate cache |
| `ExpressionBuilder` | `Mapper/ExpressionBuilder.cs` | Expression-tree mapper synthesis |
| `MappingSchemaExtensions` | `MappingSchemaExtensions.cs` | Schema-aware comparer factory |
| `EnumerableExtensions` | `EnumerableExtensions.cs` | ASCII table formatter |

## Files (Tier 1 / Tier 2)

**Tier 1** (15 files, all read): `Activity/ActivityHierarchy.cs`, `Activity/ActivityStatistics.cs`, `Activity/IStatActivity.cs`, `Comparers/ComparerBuilder.cs`, `Comparers/IgnoreComparisonAttribute.cs`, `DataProvider/SqlServer/Schemas/ISystemSchemaData.cs`, `.../SystemDB.cs`, `.../SystemSchemaExtensions.cs`, `EntityServices/IEntityMap.cs`, `EntityServices/IdentityMap.cs`, `EntityServices/EntityMap.cs`, `Mapper/IMapperBuilder.cs`, `Mapper/MapperBuilder.cs`, `Mapper/Map.cs`, `Mapper/ExpressionBuilder.cs`.

**Tier 2** (13 files, all read): `Activity/StatActivity.cs`, `Activity/StatActivitySum.cs`, `Comparers/ArrayEqualityComparer.cs`, `Comparers/BitArrayEqualityComparer.cs`, `Comparers/EnumerableEqualityComparer.cs`, `Comparers/EnumerableEqualityComparer\`1.cs`, `EntityServices/EntityCreatedEventArgs.cs`, `EntityServices/EntityMapEntry.cs`, `Mapper/Mapper.cs`, `Mapper/MemberMapperInfo.cs`, `EnumerableExtensions.cs`, `MappingSchemaExtensions.cs`, `LinqToDB.Tools.csproj`.

**Tier 3** (1, not read): `DataProvider/SqlServer/Schemas/Schemas.generated.cs` (T4 auto-generated).

## Inbound / outbound dependencies

**Inbound:** end-user code opting in to `IdentityMap`, `ActivityStatistics`, or `Map`.

**Outbound:**
- `LinqToDB` (single `<ProjectReference>`) -- `IDataContext`, `DataConnection`, `MappingSchema`, `EntityDescriptor`, `TypeAccessor`, `MemberAccessor`, `ActivityID`, `ActivityService`, `ActivityBase`, `EntityServiceInterceptor`, `EntityCreatedEventData`, `ITable<T>`, `SqlFn`.
- `LinqToDB.Metrics` (INTERCEPTORS): `IActivity`, `ActivityID`, `ActivityService`, `ActivityBase`.
- `LinqToDB.Interceptors` (INTERCEPTORS): `EntityServiceInterceptor`.

## Known issues / debt

- `StatActivity.Start()` branches on `Environment.OSVersion.Platform != PlatformID.Unix` for `Stopwatch` vs `DateTime.Now` fallback -- a blunt platform check; `DateTime.Now` has ~15 ms resolution on Windows (`Activity/StatActivity.cs:20`).
- `ActivityStatistics` is fully static -- process-global, no `Reset()`, interleaved counts under parallel/multi-tenant.
- `ExpressionBuilder.ConvertCollection` throws `NotImplementedException` for non-List/HashSet/array `IEnumerable<T>` targets (`Mapper/ExpressionBuilder.cs:277`).
- `Schemas.generated.cs` generated against SQL Server 2022; no automated regeneration in CI.
- `EntityMap<T>.GetEntity` issues a live DB query on cache miss -- a remote call from a cache-lookup method.

## See also

- [INTERCEPTORS area INDEX](../INTERCEPTORS/INDEX.md) -- owns `IActivity`, `ActivityID`, `ActivityService`, `ActivityBase`.
- [IN-TREE-TOOLS area INDEX](../IN-TREE-TOOLS/INDEX.md) -- `DataExtensions.RetrieveIdentity<T>` inside the main `linq2db.dll`; distinct package.
- [MAPPING area INDEX](../MAPPING/INDEX.md) -- `MappingSchema`, `EntityDescriptor`, `ColumnDescriptor`.
- [PROV-SQLSERVER area INDEX](../PROV-SQLSERVER/INDEX.md) -- `SqlFn`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 15 / 15
  - Activity/ActivityHierarchy.cs, Activity/ActivityStatistics.cs, Activity/IStatActivity.cs, Comparers/ComparerBuilder.cs, Comparers/IgnoreComparisonAttribute.cs, DataProvider/SqlServer/Schemas/ISystemSchemaData.cs, .../SystemDB.cs, .../SystemSchemaExtensions.cs, EntityServices/IEntityMap.cs, EntityServices/IdentityMap.cs, EntityServices/EntityMap.cs, Mapper/IMapperBuilder.cs, Mapper/MapperBuilder.cs, Mapper/Map.cs, Mapper/ExpressionBuilder.cs
- Tier 2 (visited / total): 13 / 13 (100%)
- Tier 3 (skipped, logged): 1 -- DataProvider/SqlServer/Schemas/Schemas.generated.cs (T4 auto-generated)
- Read (this run -- delta): Source/LinqToDB.Tools/PublicAPI/PublicAPI.Shipped.txt -- v6 release-promotion churn; Unshipped promoted to Shipped; no API surface changes.
</details>
