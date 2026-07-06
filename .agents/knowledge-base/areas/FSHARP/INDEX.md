---
area: FSHARP
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 6/6
coverage_tier_2: 0/0
---

# FSHARP

Optional NuGet package (`linq2db.FSharp`, assembly `linq2db.FSharp`) that adds F#-specific mapping support to linq2db. Provides F# record type support in mappings and projections, automatic mapping of F# `'T option` / `'T voption` columns, and rewriting of F#-specific expression-tree shapes (record-copy `Update`, record-construction blocks) that the core translator doesn't understand. Target frameworks inherit from the main `Directory.Build.props`; `net462` adds an explicit `FSharp.Core` package reference (other TFMs receive it transitively from the SDK).

## Subsystems

### Entry point: `DataOptions.UseFSharp()`

`DataOptionsExtensions.fs` defines a C#-visible extension method on `DataOptions` via `[<Extension>]`. Calling `.UseFSharp()` registers two interceptors via `options.UseInterceptor`: `FSharpEntityBindingInterceptor.Instance` and `FSharpQueryExpressionInterceptor.Instance` (`DataOptionsExtensions.fs:27-28`). It also combines a private `optionMappingSchema` (backed by `FSharpOptionMetadataReader`) into the caller's `MappingSchema` via `MappingSchema.CombineSchemas`, added as a *lower-priority* fallback so it only fills in members the caller hasn't explicitly mapped -- `UseAdditionalMappingSchema` was rejected because its default attribute reader would shadow explicit fluent column metadata (`DataOptionsExtensions.fs:29-37`).

### `FSharpEntityBindingInterceptor`

`FSharpEntityBindingInterceptor.fs:18` -- inherits `EntityBindingInterceptor` (`Source/LinqToDB/Internal/Interceptors/EntityBindingInterceptor.cs`) and implements `IEntityBindingInterceptor`. Exposes a singleton via `Instance`.

**Record detection** (`isRecord`, line 29): returns `true` when the type carries `CompilationMappingAttribute` with `SourceConstructFlags.RecordType` and does **not** carry `[<CLIMutable>]`. CLIMutable records have property setters and need no special handling.

**Member-to-constructor mapping** (`TryMapMembersToConstructor`, line 37): walks `TypeAccessor.Members`, finds each member's `CompilationMappingAttribute` where `SourceConstructFlags = Field`, and builds a `Dictionary<int, MemberAccessor>` keyed by `SequenceNumber` (i.e., positional index in the F# record declaration). Result is cached in a `ConcurrentDictionary<Type, ...>`.

**`ConvertConstructorExpression`** (line 55): intercepts `SqlGenericConstructorExpression` at two points in the pipeline:

- `CreateType.New` -- the `Parameters` list is positional; for each position `i`, if `map` has entry `i`, the parameter is re-tagged with the concrete `MemberInfo` so downstream SQL generation can match column -> constructor arg by position. Handles linq2db-generated `new T(args...)` calls.
- `CreateType.Full` -- the expression has named `Assignments`. The interceptor locates the single constructor with `parameters.Length >= map.Count`, allocates an `Expression[]` of that length, fills each slot by looking up the assignment by `MemberInfo`, substitutes `DefaultValue`/`MappingSchema.GetDefaultValue` for any missing slot, builds `Expression.New(ctor, arguments)`, and wraps it in `Expression.MemberInit`. Remaining (non-positional) assignments are added as `MemberBinding`s. The result is wrapped in a new `SqlGenericConstructorExpression`.

The effect: F# records, whose generated constructors have no settable properties (unless `[<CLIMutable>]`), are materialized via their primary constructor rather than via object-initializer syntax.

### `FSharpOptionSupport` / `FSharpOptionMetadataReader`

`FSharpOptionSupport.fs` -- automatic mapping for `'T option` / `'T voption` columns, so option-typed members round-trip without manual `MappingSchema` configuration.

`IsOption` (line 75) detects `FSharpOption<_>` / `FSharpValueOption<_>` by generic type definition; `IsScalarOption` (line 81) additionally requires the element type to satisfy `MappingSchema.Default.IsScalarType` -- an option over a complex/entity element is left untouched (not treated as a column).

`build` (line 26) constructs a bidirectional `ValueConverter<TOption, TProvider>` via explicit `Expression` trees, cached by `GetConverter` (line 85) in a `ConcurrentDictionary<Type, IValueConverter>` keyed by the closed option type. A non-nullable value-typed element (`int`, `decimal`, etc.) is wrapped in `Nullable<'a>` as the provider type, so `None`/`ValueNone` serializes as SQL `NULL` rather than `default('a)` -- fixes issue #4646 (`int option` storing `None` as `0`). Reference-typed and already-nullable elements pass through unwrapped.

`FSharpOptionMetadataReader` (line 90) implements `IMetadataReader` (`Source/LinqToDB/Metadata/IMetadataReader.cs`): `GetAttributes(Type)` returns `ScalarTypeAttribute` for a scalar option type; `GetAttributes(Type, MemberInfo)` returns `ColumnAttribute(CanBeNull = true)` plus a `ValueConverterAttribute` wrapping the cached converter for every scalar-option member. The DB type is intentionally left unset -- `ColumnDescriptor` derives it from the value converter's provider type against the active provider-inclusive schema, preserving provider-faithful facets (decimal precision/scale, string length) that deriving from context-free `MappingSchema.Default` would truncate (issue #5645, e.g. `decimal option` -> `decimal(18,0)`).

### `FSharpQueryExpressionInterceptor`

`FSharpQueryExpressionInterceptor.fs` -- an `IQueryExpressionInterceptor` (`Source/LinqToDB/Interceptors/IQueryExpressionInterceptor.cs`) (`Instance` singleton), registered by `.UseFSharp()` alongside the entity-binding interceptor. `ProcessExpression` rewrites only the pre-expose tree (`args.Kind = QueryExpressionArgs.ExpressionKind.Query`); linq2db custom nodes from the post-expose tree never carry the F# shapes this interceptor targets.

Rewriting is done by the private `FSharpRewriteVisitor(mappingSchema)` (an `ExpressionVisitor`):

- **Block inlining** (`VisitBlock`, line 97): F# emits a `BlockExpression` for record construction (`{ var x = expr1; new type(x, expr2) }`); when the block's variable-assignment statements are each single-use, non-self-referential, and reference one of the block's own variables, the visitor substitutes the value into the result and re-visits it, producing `new type(expr1, expr2)`.
- **Record-copy `Update` rewrite** (`RewriteUpdate`, line 26): turns `q.Update(p, fun r -> { r with Field = v })` into `q.Where(p).Set(x => x.Field, x => v).Update()`, via `Methods.LinqToDB.Update.SetQueryablePrev` / `SetUpdatablePrev` / `UpdateUpdatable` (`LinqToDB.Internal.Reflection`). Uses `FSharpEntityBindingInterceptor.isRecord` and `TryMapMembersToConstructor` to map each ctor argument back to its member. A ctor argument that is a self-copy (`r.SameField`) is excluded from the change set. When every argument is a self-copy (a literal no-op `{ r with Field = r.Field }`), every non-PK column is assigned to itself instead -- keeping the primary key out of `SET` (YDB rejects a PK in `SET`) rather than falling back to a full all-column update.
- **`VisitExtension`** (line 95) returns the node unchanged -- the base `ExpressionVisitor` would call `VisitChildren` on a non-reducible linq2db extension node and throw; F# constructs handled here only appear in the raw standard-node tree.
- Any shape `RewriteUpdate` doesn't recognize (non-record setter, no constructor map, multi-param setter) falls back to the original, unrewritten `Update` call with no diagnostic (`FSharpQueryExpressionInterceptor.fs:71-90`).

## Key types

| Type | File | Role |
|---|---|---|
| `Methods` (extension class) | `DataOptionsExtensions.fs` | Exposes `UseFSharp()` on `DataOptions` |
| `FSharpEntityBindingInterceptor` | `FSharpEntityBindingInterceptor.fs` | `IEntityBindingInterceptor`; rewrites `SqlGenericConstructorExpression` for F# records |
| `FSharpQueryExpressionInterceptor` | `FSharpQueryExpressionInterceptor.fs` | `IQueryExpressionInterceptor`; rewrites F# block/record-copy-update expression shapes |
| `FSharpOptionSupport` | `FSharpOptionSupport.fs` | Builds/caches `IValueConverter` for `'T option` / `'T voption` |
| `FSharpOptionMetadataReader` | `FSharpOptionSupport.fs` | `IMetadataReader`; supplies column + value-converter attributes for scalar-option members |

## Files (Tier 1 / Tier 2)

**Tier 1** (all 6 files; area is small enough that all are pinned):

| File | Notes |
|---|---|
| `Source/LinqToDB.FSharp/LinqToDB.FSharp.fsproj` | Package identity, TFM, compile order |
| `Source/LinqToDB.FSharp/DataOptionsExtensions.fs` | Entry-point extension method |
| `Source/LinqToDB.FSharp/FSharpEntityBindingInterceptor.fs` | Core interceptor implementation |
| `Source/LinqToDB.FSharp/FSharpQueryExpressionInterceptor.fs` | Expression-tree interceptor: block inlining + record-copy `Update` rewrite |
| `Source/LinqToDB.FSharp/FSharpOptionSupport.fs` | F# option/voption value-converter + metadata reader |
| `Source/LinqToDB.FSharp/readme.md` | NuGet readme; documents `UseFSharp()` |

**Tier 2**: none -- all 6 on-disk files are Tier 1.

## Inbound / outbound dependencies

**Outbound (this package depends on):**
- `LinqToDB` core project (`LinqToDB.csproj`) -- `DataOptions`, `TypeAccessor`, `MemberAccessor`, `EntityBindingInterceptor`, `IEntityBindingInterceptor`, `SqlGenericConstructorExpression`, `MappingSchema`, `DefaultValue`.
- `LinqToDB` core project, expression-interception and option-mapping surface -- `IQueryExpressionInterceptor`, `QueryExpressionArgs`, `IMetadataReader`, `IValueConverter`/`ValueConverter<,>`, `ScalarTypeAttribute`, `ColumnAttribute`, `ValueConverterAttribute`, `MappingSchema.CombineSchemas`, `Methods.LinqToDB.Update.*` (`LinqToDB.Internal.Reflection`).
- `FSharp.Core` (explicit package ref on `net462`; SDK-provided on netstandard2.0+).

**Inbound (depends on this package):**
- Consumer applications that call `.UseFSharp()` on `DataOptions`. Nothing in the main linq2db solution depends on this package.

**Cross-area links:**
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- provides `EntityBindingInterceptor` base class, `IEntityBindingInterceptor`, `IQueryExpressionInterceptor`, `QueryExpressionArgs`.
- [CORE](../CORE/INDEX.md) -- provides `DataOptions`, `TypeAccessor`, `MappingSchema`, `IMetadataReader`, `IValueConverter`.

## Known issues / debt

- Record types and F# `option`/`voption` columns are handled; discriminated unions and F# collection types (`list<'T>`, `seq<'T>`) are still not addressed. The readme acknowledges this ("More features planned for future releases").
- `CLIMutable` detection (`isRecord`) uses `AttributesExtensions.HasAttribute<CLIMutableAttribute>` which returns `bool?` -- the `= true` comparison is explicit and intentional, treating `null` as false.
- `FSharpRewriteVisitor.RewriteUpdate` silently falls back to the original, unrewritten `Update` call for any shape it doesn't recognize (non-record setter, no matching constructor map, multi-param setter) -- no diagnostic surfaces, so an F# record-copy `Update` outside the handled shape just runs as a full column-list update with no visible signal that the optimization didn't apply.

## See also

- [INTERCEPTORS area index](../INTERCEPTORS/INDEX.md)
- `Source/LinqToDB/Internal/Interceptors/IEntityBindingInterceptor.cs` -- interface contract
- `Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs` -- expression type being rewritten
- `Source/LinqToDB/Interceptors/IQueryExpressionInterceptor.cs` -- interface contract for `FSharpQueryExpressionInterceptor`
- `Source/LinqToDB/Metadata/IMetadataReader.cs` -- interface contract for `FSharpOptionMetadataReader`
- `Source/LinqToDB/Mapping/IValueConverter.cs`, `Source/LinqToDB/Mapping/ValueConverter.cs` -- converter types used by `FSharpOptionSupport`

<details><summary>Coverage</summary>

Tier 1 (4/4 read): `LinqToDB.FSharp.fsproj`, `DataOptionsExtensions.fs`, `FSharpEntityBindingInterceptor.fs`, `readme.md` -- read in full.

Tier 2: none. Tier 3: none.

Cross-area reads (dependency verification, not counted): `Internal/Interceptors/IEntityBindingInterceptor.cs`, `Internal/Interceptors/EntityBindingInterceptor.cs`.

Read (this run -- delta):
- `Source/LinqToDB.FSharp/FSharpExpressionInterceptor.fs` (DELETED) -- file is absent from disk and from any compile reference in the FSharp project; no replacement found via Glob or Grep; the prior INDEX.md did not track this file (it was not listed in the Tier-1 table or Key types). On-disk file count remains 4; coverage_tier_1 unchanged at 4/4.

Read (this run -- delta):
- `Source/LinqToDB.FSharp/LinqToDB.FSharp.fsproj` -- compile-item list grew from 2 to 4 entries: added `FSharpQueryExpressionInterceptor.fs` and `FSharpOptionSupport.fs` (both now Tier 1). Resolves the prior run's dangling reference: `FSharpQueryExpressionInterceptor.fs` is the replacement for the `FSharpExpressionInterceptor.fs` file noted deleted above (different name, expression-interceptor role retained).
- `Source/LinqToDB.FSharp/DataOptionsExtensions.fs` -- `UseFSharp()` now also registers `FSharpQueryExpressionInterceptor.Instance` and combines an option-mapping `MappingSchema` (`optionMappingSchema`) into the caller's schema via `MappingSchema.CombineSchemas`, added as a lower-priority fallback.
- `Source/LinqToDB.FSharp/FSharpOptionSupport.fs` (NEW) -- adds `FSharpOptionSupport` (builds/caches `IValueConverter` for `'T option`/`'T voption`, wrapping non-nullable value elements in `Nullable<'a>` so `None` stores as `NULL`, fixing issue #4646) and `FSharpOptionMetadataReader` (`IMetadataReader` supplying `ScalarTypeAttribute` + `ColumnAttribute(CanBeNull = true)` + `ValueConverterAttribute` for scalar-option members; DB type left unset to preserve provider facets, fixing issue #5645).
- `Source/LinqToDB.FSharp/FSharpQueryExpressionInterceptor.fs` (NEW) -- adds `FSharpQueryExpressionInterceptor` (`IQueryExpressionInterceptor`) and the private `FSharpRewriteVisitor`, which inlines F# record-construction blocks and rewrites F# record-copy `Update` calls into targeted `Where(...).Set(...).Update()` chains.
- `Source/LinqToDB.FSharp/readme.md` -- documents the new automatic F# `option`/`voption` column mapping alongside the existing record-type support.

coverage_tier_1 increased from 4/4 to 6/6 (2 new Tier-1 files: `FSharpQueryExpressionInterceptor.fs`, `FSharpOptionSupport.fs`). All 6 Tier-1 files read in full this run or a prior run (`FSharpEntityBindingInterceptor.fs` unchanged since the last run, previously read in full).
</details>
