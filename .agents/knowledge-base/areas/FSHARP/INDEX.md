---
area: FSHARP
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 4/4
coverage_tier_2: 0/0
---

# FSHARP

Optional NuGet package (`linq2db.FSharp`, assembly `linq2db.FSharp`) that adds F#-specific mapping support to linq2db. Currently provides F# record type support in mappings and projections. Target frameworks inherit from the main `Directory.Build.props`; `net462` adds an explicit `FSharp.Core` package reference (other TFMs receive it transitively from the SDK).

## Subsystems

### Entry point: `DataOptions.UseFSharp()`

`DataOptionsExtensions.fs` defines a C#-visible extension method on `DataOptions` via `[<Extension>]`. Calling `.UseFSharp()` registers `FSharpEntityBindingInterceptor.Instance` via `options.UseInterceptor`. No expression interceptor is registered.

### `FSharpEntityBindingInterceptor`

`FSharpEntityBindingInterceptor.fs:18` -- inherits `EntityBindingInterceptor` (`Source/LinqToDB/Internal/Interceptors/EntityBindingInterceptor.cs`) and implements `IEntityBindingInterceptor`. Exposes a singleton via `Instance`.

**Record detection** (`isRecord`, line 29): returns `true` when the type carries `CompilationMappingAttribute` with `SourceConstructFlags.RecordType` and does **not** carry `[<CLIMutable>]`. CLIMutable records have property setters and need no special handling.

**Member-to-constructor mapping** (`TryMapMembersToConstructor`, line 37): walks `TypeAccessor.Members`, finds each member's `CompilationMappingAttribute` where `SourceConstructFlags = Field`, and builds a `Dictionary<int, MemberAccessor>` keyed by `SequenceNumber` (i.e., positional index in the F# record declaration). Result is cached in a `ConcurrentDictionary<Type, ...>`.

**`ConvertConstructorExpression`** (line 55): intercepts `SqlGenericConstructorExpression` at two points in the pipeline:

- `CreateType.New` -- the `Parameters` list is positional; for each position `i`, if `map` has entry `i`, the parameter is re-tagged with the concrete `MemberInfo` so downstream SQL generation can match column -> constructor arg by position. Handles linq2db-generated `new T(args...)` calls.
- `CreateType.Full` -- the expression has named `Assignments`. The interceptor locates the single constructor with `parameters.Length >= map.Count`, allocates an `Expression[]` of that length, fills each slot by looking up the assignment by `MemberInfo`, substitutes `DefaultValue`/`MappingSchema.GetDefaultValue` for any missing slot, builds `Expression.New(ctor, arguments)`, and wraps it in `Expression.MemberInit`. Remaining (non-positional) assignments are added as `MemberBinding`s. The result is wrapped in a new `SqlGenericConstructorExpression`.

The effect: F# records, whose generated constructors have no settable properties (unless `[<CLIMutable>]`), are materialized via their primary constructor rather than via object-initializer syntax.

## Key types

| Type | File | Role |
|---|---|---|
| `Methods` (extension class) | `DataOptionsExtensions.fs` | Exposes `UseFSharp()` on `DataOptions` |
| `FSharpEntityBindingInterceptor` | `FSharpEntityBindingInterceptor.fs` | `IEntityBindingInterceptor`; rewrites `SqlGenericConstructorExpression` for F# records |

## Files (Tier 1 / Tier 2)

**Tier 1** (all 4 files; area is small enough that all are pinned):

| File | Notes |
|---|---|
| `Source/LinqToDB.FSharp/LinqToDB.FSharp.fsproj` | Package identity, TFM, compile order |
| `Source/LinqToDB.FSharp/DataOptionsExtensions.fs` | Entry-point extension method |
| `Source/LinqToDB.FSharp/FSharpEntityBindingInterceptor.fs` | Core interceptor implementation |
| `Source/LinqToDB.FSharp/readme.md` | NuGet readme; documents `UseFSharp()` |

**Tier 2**: none -- all 4 on-disk files are Tier 1.

## Inbound / outbound dependencies

**Outbound (this package depends on):**
- `LinqToDB` core project (`LinqToDB.csproj`) -- `DataOptions`, `TypeAccessor`, `MemberAccessor`, `EntityBindingInterceptor`, `IEntityBindingInterceptor`, `SqlGenericConstructorExpression`, `MappingSchema`, `DefaultValue`.
- `FSharp.Core` (explicit package ref on `net462`; SDK-provided on netstandard2.0+).

**Inbound (depends on this package):**
- Consumer applications that call `.UseFSharp()` on `DataOptions`. Nothing in the main linq2db solution depends on this package.

**Cross-area links:**
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- provides `EntityBindingInterceptor` base class and `IEntityBindingInterceptor` interface.
- [CORE](../CORE/INDEX.md) -- provides `DataOptions`, `TypeAccessor`, `MappingSchema`.

## Known issues / debt

- Only record types are handled; discriminated unions, F# `option` types, and F# collection types (`list<'T>`, `seq<'T>`) are not addressed. The readme acknowledges this ("More features planned for future releases").
- `CLIMutable` detection (`isRecord`) uses `AttributesExtensions.HasAttribute<CLIMutableAttribute>` which returns `bool?` -- the `= true` comparison is explicit and intentional, treating `null` as false.

## See also

- [INTERCEPTORS area index](../INTERCEPTORS/INDEX.md)
- `Source/LinqToDB/Internal/Interceptors/IEntityBindingInterceptor.cs` -- interface contract
- `Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs` -- expression type being rewritten

<details><summary>Coverage</summary>

Tier 1 (4/4 read): `LinqToDB.FSharp.fsproj`, `DataOptionsExtensions.fs`, `FSharpEntityBindingInterceptor.fs`, `readme.md` -- read in full.

Tier 2: none. Tier 3: none.

Cross-area reads (dependency verification, not counted): `Internal/Interceptors/IEntityBindingInterceptor.cs`, `Internal/Interceptors/EntityBindingInterceptor.cs`.

Read (this run -- delta):
- `Source/LinqToDB.FSharp/FSharpExpressionInterceptor.fs` (DELETED) -- file is absent from disk and from any compile reference in the FSharp project; no replacement found via Glob or Grep; the prior INDEX.md did not track this file (it was not listed in the Tier-1 table or Key types). On-disk file count remains 4; coverage_tier_1 unchanged at 4/4.
</details>
