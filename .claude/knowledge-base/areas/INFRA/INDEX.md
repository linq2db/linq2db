---
area: INFRA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-30
last_verified_sha: 916d3589b9c9e02e9ef87f755d0861a7a14ac2d9
coverage_tier_1: 5/5
coverage_tier_2: 25/25
---

# INFRA

## What this area does

INFRA is the **cross-cutting plumbing** of the `linq2db` assembly: the public-facing global flag bag (`LinqToDB.Common.Configuration`), the type-conversion entry points (`Converter`, `Convert<TFrom,TTo>`, `ConvertTo<T>`), the runtime reflection accessors (`TypeAccessor`, `MemberAccessor`, `ObjectFactory<T>`), the public async query extensions (`AsyncExtensions`), the optimistic-concurrency helpers (`ConcurrencyExtensions`), the `Compatibility` namespace shims for non-.NET-Framework targets, the `LinqToDB.Configuration` `app.config` section types (cross-listed with [CORE](../CORE/INDEX.md)), and miscellaneous helpers (`Option<T>`, `Compilation`, embedded reserved-words resources, assembly attributes).

Nothing in INFRA owns a query pipeline of its own — every type here is consumed from elsewhere. `Converter` and `Convert<,>` are the conversion plumbing under [MAPPING](../MAPPING/INDEX.md)'s `MappingSchema`. `TypeAccessor` / `MemberAccessor` are the reflection cache used by [MAPPING](../MAPPING/INDEX.md) (`EntityDescriptor`, `ColumnDescriptor`) and the [EXPR-TRANS](../EXPR-TRANS/INDEX.md) builders to compose getter/setter `Expression` trees without resorting to `Reflection.Emit`. `AsyncExtensions` is the public-API counterpart of [LINQ](../LINQ/INDEX.md)'s `IQueryProviderAsync`. `Common.Configuration` aggregates the global default option records (`LinqOptions.Default`, `SqlOptions.Default`, `RetryPolicyOptions.Default`, …) defined in CORE and exposes them as static get/set sugar.

## Key types

- `LinqToDB.Common.Configuration` — public static class of global flags and option-record proxies. The flat properties (`MaxBinaryParameterLengthLogging`, `OptimizeForSequentialAccess`, `TraceMaterializationActivity`, `TranslationThreadMaxHopCount`) are independently mutable; nested `Linq` / `Sql` / `RetryPolicy` / `BulkCopy` / `DataContext` / `Connection` / `QueryTrace` classes wrap the corresponding `*.Default` `OptionsContainer<T>` (`Source/LinqToDB/Common/Configuration.cs:24`, `:147`, `:494`, `:582`, `:697`).
- `LinqToDB.Common.Converter` — central conversion entry point. Two parallel `ConcurrentDictionary` caches: `_expressions` for explicit user-set `LambdaExpression` converters (`Source/LinqToDB/Common/Converter.cs:27`) and `_converters` for the boxing-aware `Func<object,object>` form built by `ChangeType` (`:110`). Static ctor seeds primitive cross-conversions (`string`↔`Binary`, `string`↔`XmlDocument`, `bool`↔`decimal`, `DateTime`↔`TimeSpan`, `byte`↔`BitArray`, `DateTime`↔`DateOnly` under `SUPPORTS_DATEONLY`, `Source/LinqToDB/Common/Converter.cs:39`).
- `Convert<TFrom,TTo>` — generic type-pair converter cache. Static ctor calls `ConvertBuilder.GetConverter` (`Source/LinqToDB/Internal/Conversion/ConvertBuilder.cs`) to build an `Expression<Func<TFrom,TTo>>` and a compiled `Func<TFrom,TTo>` (`Source/LinqToDB/Common/Convert{TFrom,TTo}.cs:32`). Setting `Expression` or `Lambda` re-publishes the converter into `ConvertInfo.Default`, making it the new default for that type pair (`:67`, `:107`).
- `ConvertTo<TTo>` — sugar over `Convert<TFrom,TTo>.From(o)` for the `ConvertTo<int>.From("123")` ergonomic (`Source/LinqToDB/Common/ConvertTo.cs:21`).
- `LinqToDB.Reflection.TypeAccessor` — abstract base. Holds `Members: List<MemberAccessor>` plus a name-keyed `ConcurrentDictionary<string, MemberAccessor>` index (`Source/LinqToDB/Reflection/TypeAccessor.cs:80`). `GetAccessor(Type)` lazy-builds a `TypeAccessor<T>` per closed type into a static `_accessors` `ConcurrentDictionary` (`:119`, `:121`). `GetOrCreateMemberAccessor` lock-protects the on-demand creation of new entries to fix issue #5361 — the lock-protected branch *replaces* the public `Members` list rather than mutating in place (`:96`).
- `LinqToDB.Reflection.TypeAccessor<T>` — generic concrete subclass. Static `_members` is computed by `GetTypeMembers()`, which walks the type's interface map and, for explicitly-implemented interface properties, picks the most-derived implementation by walking up the inheritance hierarchy (`Source/LinqToDB/Reflection/TypeAccessor{T}.cs:17`–`:113`). The constructor populates the base `Members` list once per closed type (`:115`).
- `LinqToDB.Reflection.MemberAccessor` — per-member fast getter/setter. Two ctors: a *simple* one (single `MemberInfo`, `Source/LinqToDB/Reflection/MemberAccessor.cs:176`) and a *complex* dotted-path one that handles `Foo.Bar.Baz`-style nested member chains with null-guard expression generation for the getter (`:32`–`:96`) and lazy-init expression for the setter (`:108`–`:170`). The actual delegate is built lazily via `Lazy<Func<object,object?>>` / `Lazy<Action<object,object?>>` since compilation is expensive (`:272`–`:296`); the *expression-tree* form (`GetGetterExpression`/`GetSetterExpression`) is what `EntityDescriptor` and the LINQ builders actually inline into compiled query expressions.
- `IObjectFactory` / `ObjectFactoryAttribute` / `ObjectFactory<T>` — pluggable instantiation hook. `[ObjectFactoryAttribute(typeof(MyFactory))]` on a class swaps `TypeAccessor.CreateInstanceEx` over to the factory; `ObjectFactory<T>` is the default that compiles `Expression.Lambda<Func<T>>(Expression.New(ctor))` and falls back to throwing `LinqToDBException` for abstract or constructor-less types (`Source/LinqToDB/Reflection/ObjectFactory{T}.cs:23`).
- `LinqToDB.Async.AsyncExtensions` — partial class split between hand-written `AsyncExtensions.cs` (`AsAsyncEnumerable`, `ForEachAsync`, `ForEachUntilAsync`, `ToListAsync`, `ToArrayAsync`, `ToDictionaryAsync`, `ToLookupAsync`) and the T4-style generated `AsyncExtensions.generated.cs` (per-`Queryable` operator: `FirstAsync`, `SingleAsync`, `AnyAsync`, `CountAsync`, `SumAsync`, …). Every public method follows the same dispatch shape: `IQueryProviderAsync` first, then `LinqExtensions.ExtensionsAdapter` (the EFCore.LinqToDB bridge), then a `Task.Run(...)` synchronous fallback (`Source/LinqToDB/Async/AsyncExtensions.cs:211`–`:233`, `:277`–`:292`; `Source/LinqToDB/Async/AsyncExtensions.generated.cs:18`–`:38`).
- `LinqToDB.Concurrency.ConcurrencyExtensions` — public extension methods for optimistic-lock CRUD. `WhereKeyOptimistic<T>` / `UpdateOptimistic<T>` / `DeleteOptimistic<T>` consume `OptimisticLockPropertyBaseAttribute` columns (cross-listed under [MAPPING](../MAPPING/INDEX.md)) to build a `Where` filter on PK + lock columns and an `IUpdatable<T>` whose lock-column updates come from `attr.GetNextValue` (`Source/LinqToDB/Concurrency/ConcurrencyExtensions.cs:54`, `:75`–`:107`).
- `LinqToDB.Common.Compilation` — single seam for swapping the default `LambdaExpression.Compile()` for an alternative compiler (e.g. FastExpressionCompiler in third-party setups). `SetExpressionCompiler` writes a static field; `CompileExpression` uses the override or falls back to `Compile()` with `RS0030` (banned API) suppressed locally (`Source/LinqToDB/Common/Compilation.cs:21`–`:47`).
- `LinqToDB.Common.Option<T>` — value-type option monad (`HasValue` + `Value`); used internally by builders to distinguish "no value" from "null value" without nullable-reference contortions (`Source/LinqToDB/Common/Option.cs:13`).
- `LinqToDB.Common.LinqToDBConvertException` — typed exception thrown by conversion code paths; carries optional `ColumnName` for diagnostic chaining from materializer errors (`Source/LinqToDB/Common/LinqToDBConvertException.cs:14`, `:73`).
- `System.Data.Linq.Binary` (compatibility shim) — pre-`net8.0` only; reimplements the `System.Data.Linq.Binary` type for non-NETFX TFMs so the `Converter` static ctor's `string`↔`Binary` registrations link cleanly (`Source/LinqToDB/Compatibility/System/Data/Linq/Binary.cs:1`, `:12`). Gated `#if !NETFRAMEWORK`.
- `LinqToDB.Configuration.*` (cross-listed with CORE) — `app.config` `<linq2db>` section types. `LinqToDBSection`, `DataProviderElement(Collection)`, `ElementBase`, `ElementCollectionBase<T>` are gated `#if NETFRAMEWORK || COMPAT` and use `[TypeForwardedTo]` for the `COMPAT` shim build (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1`–`:3`, `Source/LinqToDB/Configuration/DataProviderElement.cs:1`–`:3`). `ILinqToDBSettings`, `LinqToDBSettings`, `IConnectionStringSettings`, `ConnectionStringSettings`, `IDataProviderSettings`, `NamedValue` are TFM-unconditional so `net8.0`+ users can construct settings programmatically.

## Subsystems

- **Async** (`Source/LinqToDB/Async/`) — public `Async`-suffixed `IQueryable<T>` extensions. The dispatch chain (`IQueryProviderAsync` → `ExtensionsAdapter` → `Task.Run`) is the conversation point with [LINQ](../LINQ/INDEX.md). The `AsAsyncEnumerable` adapter wraps a synchronous enumerator inside an `async IAsyncEnumerator<T>` body — note the `#pragma warning disable CS1998` at `AsyncExtensions.cs:186`, deliberately suppressing the "lacks await" warning because the adapter is for non-async providers.
- **Common** (`Source/LinqToDB/Common/`) — conversion plumbing (`Converter`, `Convert<,>`, `ConvertTo<>`), the global `Configuration` flag class, the `Compilation` seam, `Option<T>`, `LinqToDBConvertException`, `Array<T>` (legacy, `[Obsolete]` planned for v7).
- **Compatibility** (`Source/LinqToDB/Compatibility/`) — single-file shim providing `System.Data.Linq.Binary` outside NETFX. The `#pragma warning disable IDE0130` ack the deliberate cross-namespace declaration.
- **Concurrency** (`Source/LinqToDB/Concurrency/`) — public optimistic-lock CRUD extensions; ties [MAPPING](../MAPPING/INDEX.md)'s `OptimisticLockPropertyBaseAttribute` to the [LINQ](../LINQ/INDEX.md) `IUpdatable<T>` builder pipeline.
- **Configuration** (`Source/LinqToDB/Configuration/`) — `app.config` `<linq2db>` section types + their settings interfaces. Cross-listed with [CORE](../CORE/INDEX.md), which pins `LinqToDBSection.cs` and `ILinqToDBSettings.cs` as Tier 1.
- **Reflection** (`Source/LinqToDB/Reflection/`) — runtime type/member accessor cache + pluggable object-factory attribute. The hot-path consumer is [MAPPING](../MAPPING/INDEX.md)'s `EntityDescriptor` (`MemberAccessor.GetGetterExpression` is what gets baked into compiled materializers); [EXPR-TRANS](../EXPR-TRANS/INDEX.md) consumes `MemberAccessor` via `ColumnDescriptor.MemberAccessor` for `Where`/`Select`/`Update` translation.
- **Resources** (`Source/LinqToDB/Resources/`) — four embedded `.txt` files (`ReservedWords.txt`, `ReservedWordsFirebird.txt`, `ReservedWordsPostgres.txt`, `ReservedWordsOracle.txt`) consumed by SQL-builder identifier-quoting decisions. No `.cs` files in this directory; counted as Tier-3 non-code resources.
- **Properties** (`Source/LinqToDB/Properties/`) — `AssemblyInfo.cs` carries `[CLSCompliant(true)]`, `[ComVisible(false)]`, `[NeutralResourcesLanguage("en-US")]`. **No `[InternalsVisibleTo]` attributes are declared anywhere in `Source/LinqToDB`** (verified by repo-wide grep) — internal types under `LinqToDB.Internal.*` are public-but-namespace-segregated by convention rather than gated by IVT. See [`code-design.md`](../../../docs/code-design.md) → "Internal namespace policy" for the design rationale this enforces.

## Files (Tier 1 / Tier 2)

**Tier 1 (read in full):**

| Path | Role |
|---|---|
| `Source/LinqToDB/Async/AsyncExtensions.cs` | Public async-LINQ entry points (hand-written half) |
| `Source/LinqToDB/Common/Configuration.cs` | Global flag/option-default proxy class |
| `Source/LinqToDB/Common/Converter.cs` | Central type-conversion cache + seed registrations |
| `Source/LinqToDB/Reflection/TypeAccessor.cs` | Cached type-accessor base, member-by-name index |
| `Source/LinqToDB/Reflection/MemberAccessor.cs` | Per-member compiled getter/setter delegate |

**Tier 2 (read for context):**

| Path | Role |
|---|---|
| `Source/LinqToDB/Async/AsyncExtensions.generated.cs` | Generated per-operator async wrappers (First/Single/Any/Count/Sum/…) |
| `Source/LinqToDB/Common/ConvertTo.cs` | `ConvertTo<TTo>.From<TFrom>` sugar |
| `Source/LinqToDB/Common/Convert{TFrom,TTo}.cs` | Generic type-pair converter cache |
| `Source/LinqToDB/Common/Compilation.cs` | `LambdaExpression.Compile` indirection seam |
| `Source/LinqToDB/Common/Array{T}.cs` | Obsolete v7-removal helper (`Array<T>.Append`) |
| `Source/LinqToDB/Common/LinqToDBConvertException.cs` | Conversion-error exception type |
| `Source/LinqToDB/Common/Option.cs` | `Option<T>` value-type monad |
| `Source/LinqToDB/Compatibility/System/Data/Linq/Binary.cs` | `System.Data.Linq.Binary` shim (non-NETFX) |
| `Source/LinqToDB/Concurrency/ConcurrencyExtensions.cs` | `WhereKeyOptimistic` / `UpdateOptimistic` / `DeleteOptimistic` |
| `Source/LinqToDB/Reflection/TypeAccessor{T}.cs` | Generic `TypeAccessor<T>` with interface-property resolution |
| `Source/LinqToDB/Reflection/IObjectFactory.cs` | Object-factory contract |
| `Source/LinqToDB/Reflection/ObjectFactoryAttribute.cs` | `[ObjectFactory(typeof(F))]` opt-in |
| `Source/LinqToDB/Reflection/ObjectFactory{T}.cs` | Default `Expression.New` ctor instantiator |
| `Source/LinqToDB/Properties/AssemblyInfo.cs` | Assembly attributes (no IVT) |
| `Source/LinqToDB/Configuration/ConnectionStringSettings.cs` | (cross-listed CORE) Explicit connection-string settings |
| `Source/LinqToDB/Configuration/IConnectionStringSettings.cs` | (cross-listed CORE) Connection settings interface |
| `Source/LinqToDB/Configuration/DataProviderElement.cs` | (cross-listed CORE) NETFX `<dataProvider>` element |
| `Source/LinqToDB/Configuration/DataProviderElementCollection.cs` | (cross-listed CORE) NETFX provider collection |
| `Source/LinqToDB/Configuration/IDataProviderSettings.cs` | (cross-listed CORE) Provider-settings interface |
| `Source/LinqToDB/Configuration/ElementBase.cs` | (cross-listed CORE) NETFX `ConfigurationElement` base |
| `Source/LinqToDB/Configuration/ElementCollectionBase.cs` | (cross-listed CORE) NETFX collection base |
| `Source/LinqToDB/Configuration/LinqToDBSection.cs` | (cross-listed CORE) NETFX `<linq2db>` section |
| `Source/LinqToDB/Configuration/LinqToDBSettings.cs` | (cross-listed CORE) Programmatic settings |
| `Source/LinqToDB/Configuration/ILinqToDBSettings.cs` | (cross-listed CORE) Settings interface |
| `Source/LinqToDB/Configuration/NamedValue.cs` | (cross-listed CORE) `name`/`value` pair |

**Tier 3 (counted, not read):**

| Path | Reason |
|---|---|
| `Source/LinqToDB/Resources/ReservedWords.txt` | embedded resource, not C# |
| `Source/LinqToDB/Resources/ReservedWordsFirebird.txt` | embedded resource |
| `Source/LinqToDB/Resources/ReservedWordsPostgres.txt` | embedded resource |
| `Source/LinqToDB/Resources/ReservedWordsOracle.txt` | embedded resource |

## Inbound dependencies

- [MAPPING](../MAPPING/INDEX.md) — `MappingSchema.GetConverter`, `EntityDescriptor.Columns[*].MemberAccessor`, `ColumnDescriptor`'s `IsPrimaryKey` / `IsIdentity` / `SkipOnUpdate` flags consumed by `ConcurrencyExtensions.MakeUpdateOptimistic` (`Source/LinqToDB/Concurrency/ConcurrencyExtensions.cs:75`–`:107`). `Converter.ChangeType` / `ChangeTypeTo<T>` accept an optional `MappingSchema` to use schema-specific converters (`Source/LinqToDB/Common/Converter.cs:120`, `:182`).
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — `MemberAccessor.GetGetterExpression` / `GetSetterExpression` are the standard splice points used by `Linq.Builder.*` (e.g. `OptimisticLockPropertyBaseAttribute.GetNextValue` returns a `LambdaExpression` plugged in at `ConcurrencyExtensions.cs:98`). `Compilation.CompileExpression<TDelegate>` is invoked by every builder that produces a runtime delegate from an expression tree.
- [LINQ](../LINQ/INDEX.md) — `AsyncExtensions` dispatches to `ExpressionQuery<T>.GetForEachAsync` / `IQueryProviderAsync.ExecuteAsync` (`Source/LinqToDB/Async/AsyncExtensions.cs:215`, `:277`–`:292`). `Internals.GetDataContext(source)` is the bridge from `IQueryable<T>` back to `IDataContext` for `WhereKeyOptimistic` (`Source/LinqToDB/Concurrency/ConcurrencyExtensions.cs:262`).
- [CORE](../CORE/INDEX.md) — `LinqToDB.Common.Configuration.Linq.Options` etc. proxy to `LinqOptions.Default`, `SqlOptions.Default`, `RetryPolicyOptions.Default`, `BulkCopyOptions.Default`, `DataContextOptions.Default`, `ConnectionOptions.Default`, `QueryTraceOptions.Default` — every nested option-record container lives in CORE. The cross-listed `Configuration/` namespace files are covered authoritatively by the [CORE](../CORE/INDEX.md) INDEX.
- [DATA](../DATA/INDEX.md) — `Configuration.RetryPolicy.Factory` returns an `IRetryPolicy?` consumed by `DataConnection`. `Configuration.BulkCopy.Options` is the default for `DataConnection.BulkCopy` (`Source/LinqToDB/Common/Configuration.cs:497`–`:525`, `:702`–`:706`).

## Outbound dependencies

- `LinqToDB.Internal.Common.DefaultValue<T>` and `LinqToDB.Internal.Common.DefaultValue` — read by `Converter.ChangeType` for null-input handling and by `MemberAccessor` to build the no-op setter for read-only properties (`Source/LinqToDB/Common/Converter.cs:124`, `Source/LinqToDB/Reflection/MemberAccessor.cs:218`).
- `LinqToDB.Internal.Conversion.ConvertInfo` / `ConvertBuilder` / `ConvertReducer` — `Converter` and `Convert<,>` delegate the actual expression-tree construction here. Only the cache + entry point lives in INFRA; the build logic is internal-conversion plumbing.
- `LinqToDB.Internal.Common.ActivatorExt` — `TypeAccessor.GetAccessor` and `ObjectFactoryAttribute(Type)` use `ActivatorExt.CreateInstance<T>` for reflection-aware instantiation (`Source/LinqToDB/Reflection/TypeAccessor.cs:130`, `Source/LinqToDB/Reflection/ObjectFactoryAttribute.cs:14`).
- `LinqToDB.Internal.Linq.LinqExtensions.ExtensionsAdapter` — the EFCore-bridge plug-in point that `AsyncExtensions` checks at every operator dispatch.
- `System.Linq.Expressions.LambdaExpression.Compile()` — banned API (`RS0030`); only `Compilation.CompileExpression` is allowed to call it directly, and the call site is wrapped in `#pragma warning disable RS0030` (`Source/LinqToDB/Common/Compilation.cs:33`–`:35`, `:43`–`:45`).

## Recurring patterns / known conventions

- **Banned-`Compile()` discipline.** Every code path that needs to materialize an `Expression` into a delegate goes through `Compilation.CompileExpression` (`Source/LinqToDB/Common/Compilation.cs:29`, `:41`). Direct calls to `expr.Compile()` are flagged by analyzer `RS0030` (`Build/BannedSymbols.txt`). New compile sites in INFRA, MAPPING, EXPR-TRANS, and LINQ should follow the same indirection so that third-party hosts can swap the implementation. Candidate for `conventions/` in step 4.
- **`ConcurrentDictionary`-based two-tier cache for compiled delegates.** Recurring shape: per-type-pair static dictionary holding either a `LambdaExpression` (for export) or a `Func<...>` (compiled) — `Converter._expressions` + `_converters` (`Source/LinqToDB/Common/Converter.cs:27`, `:110`), `TypeAccessor._accessors` (`:119`), per-member name dict (`:80`), `ExprHolder<T>.Converters` (`:171`). New caches added in INFRA should use this shape rather than rolling locks around a `Dictionary<,>`.
- **Async dispatch triple-fallback.** Every public `*Async` method on `AsyncExtensions` checks `IQueryProviderAsync` → `LinqExtensions.ExtensionsAdapter` → `Task.Run` synchronous fallback (`Source/LinqToDB/Async/AsyncExtensions.cs:215`–`:233`, `:281`–`:292`; `Source/LinqToDB/Async/AsyncExtensions.generated.cs:22`–`:38`). The fallback uses `Task.Run` deliberately — INFRA does *not* enforce `ConfigureAwait(false)` inside the `Task.Run` body (the lambda is synchronous), but every `await` *outside* a `Task.Run` body in this area uses `.ConfigureAwait(false)` (`Source/LinqToDB/Async/AsyncExtensions.cs:284`, `:289`, `:317`, `:344`, `:498`). The same triple is the rule for any new public async query operator.
- **Lazy-init compiled delegate inside expression-tree-first accessor.** `MemberAccessor` exposes both an expression form (`GetGetterExpression`/`GetSetterExpression`, used by builders to splice into a larger compiled query) and a delegate form (`GetValue`/`SetValue`, used at runtime when an isolated property access is needed). The delegate is `Lazy<>` so closed types that are only ever accessed through expression splicing never pay the compilation cost (`Source/LinqToDB/Reflection/MemberAccessor.cs:272`–`:296`).
- **`Obsolete("…in v7")` marker on retired flags.** `Configuration.IsStructIsScalarType`, `Configuration.UseEnumValueNameForStringColumns`, `Configuration.ContinueOnCapturedContext`, `Configuration.Data.ThrowOnDisposed`, `Configuration.Linq.PreloadGroups`, `Configuration.Linq.CompareNullsAsValues`, `Configuration.Linq.PreferApply`, `Configuration.Linq.KeepDistinctOrdered`, `Configuration.Linq.DoNotClearOrderBys`, and `Common.Array<T>` all carry `[Obsolete("…version 7"), EditorBrowsable(EditorBrowsableState.Never)]` plus a `// TODO: Remove in v7` comment on the line above (`Source/LinqToDB/Common/Configuration.cs:31`–`:33`, `:38`–`:40`, `:45`–`:47`, `:128`–`:130`, `:164`–`:166`, `:337`–`:343`, `:407`–`:409`, `:417`–`:419`; `Source/LinqToDB/Common/Array{T}.cs:10`–`:12`). The convention is consistent with CORE (see [CORE](../CORE/INDEX.md) → "Recurring patterns").
- **TFM-conditional `[TypeForwardedTo]` shim.** Every `Configuration/*.cs` file backed by `System.Configuration` opens with `#if NETFRAMEWORK && COMPAT` → `[TypeForwardedTo(typeof(...))]` → `#elif NETFRAMEWORK || COMPAT` → body → `#endif` (`Source/LinqToDB/Configuration/LinqToDBSection.cs:1`–`:3`, `Source/LinqToDB/Configuration/DataProviderElement.cs:1`–`:3`, `Source/LinqToDB/Configuration/DataProviderElementCollection.cs:1`–`:3`, `Source/LinqToDB/Configuration/ElementBase.cs:1`–`:3`, `Source/LinqToDB/Configuration/ElementCollectionBase.cs:1`–`:3`). Modern (`net8.0`+) consumers do not see these types.

## Known issues / debt

- **Issue #5361 workaround in `TypeAccessor.GetOrCreateMemberAccessor`.** The lock-protected branch *replaces* `Members` (`Members = [.. Members, memberAccessor]`, `Source/LinqToDB/Reflection/TypeAccessor.cs:96`) instead of mutating it, because external readers may be enumerating the public `List<MemberAccessor>` on another thread. A direct `Members.Add` here would race; the `TODO` linked at `:93` references the upstream issue. Candidate detected-issue: surface in step 8.
- **`TypeAccessor<T>` flagged for v7 internalization.** The class header carries `// TODO: v7: move to internal namespace (including related types) and refactor — https://github.com/linq2db/linq2db/issues/5361` at `Source/LinqToDB/Reflection/TypeAccessor{T}.cs:10`. Pin for the v7 milestone roll-up.
- **Dynamic-column accessor "fail fast vs. lazy" comment.** `MemberAccessor.SetSimple` has two `// @mace_windu: why not throw it immediately? Fail fast` annotations (`Source/LinqToDB/Reflection/MemberAccessor.cs:223`, `:245`) on the dynamic-column-store-missing branches. Both currently defer the throw to `_throwOnDynamicStoreMissingMethod`. Candidate detected-issue: design decision worth documenting (or fixing).
- **Obsolete-but-still-honored converter-side flags.** `Configuration.UseEnumValueNameForStringColumns` (`:38`–`:40`) and `Configuration.ContinueOnCapturedContext` (`:45`–`:47`) are documented as "no effect anymore" — but the latter is misleading: with `Task.Run` fallbacks in `AsyncExtensions`, the property has never had a way to surface to user code. Candidate detected-issue: clarify the API doc or remove the property ahead of schedule.
- **`Configuration.IsStructIsScalarType` discoverability.** Marked `[Obsolete]` for v7 with redirect to `MappingSchema.SetScalarType` / `[ScalarTypeAttribute]` (`Source/LinqToDB/Common/Configuration.cs:31`–`:33`); existing user code that flips the flag silently keeps working. Roll-up candidate.
- **`Common/Array<T>` is dead code.** Marked `[Obsolete("…version 7")]` and unused outside the file itself (`Source/LinqToDB/Common/Array{T}.cs:10`–`:12`). Safe to remove with v7.

## Pointers

- `Common.Configuration` ↔ option-record defaults in CORE: see [CORE](../CORE/INDEX.md) → "Immutable-record options pattern" for the `OptionsContainer<T>` shape that the proxy properties wrap.
- `MemberAccessor` consumers: every `EntityDescriptor.Columns[*]` carries a `MemberAccessor` (covered by [MAPPING](../MAPPING/INDEX.md) once that area's INDEX is built).
- `Converter` participates in the materialization pipeline documented in [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md) (step 2).
- `AsyncExtensions` ↔ `IQueryProviderAsync` ↔ `ExpressionQuery<T>.GetForEachAsync` chain documented under [LINQ](../LINQ/INDEX.md) (when built).
- Cross-cutting design invariants the area must preserve: [`code-design.md`](../../../docs/code-design.md) → "Public API is a contract" (every type listed under "Key types" with `[PublicAPI]` is a stability commitment), "Cross-cutting internals are shared" (don't reshape `MemberAccessor` for a local fix).

## See also

- [CORE](../CORE/INDEX.md) — owns `LinqOptions`, `SqlOptions`, `DataOptions`, etc. that `Configuration.*.Options` proxy to; `Configuration/*.cs` files are cross-listed there.
- [MAPPING](../MAPPING/INDEX.md) — primary consumer of `Converter`, `TypeAccessor`, `MemberAccessor`.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — splices `MemberAccessor.GetGetterExpression` / `GetSetterExpression` into compiled query trees; consumes `Compilation.CompileExpression`.
- [LINQ](../LINQ/INDEX.md) — `AsyncExtensions` dispatches into `ExpressionQuery<T>` and `IQueryProviderAsync`.
- [DATA](../DATA/INDEX.md) — `RetryPolicy.Factory` and `BulkCopy.Options` flow into `DataConnection`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5 ✓
  - Source/LinqToDB/Async/AsyncExtensions.cs
  - Source/LinqToDB/Common/Configuration.cs
  - Source/LinqToDB/Common/Converter.cs
  - Source/LinqToDB/Reflection/TypeAccessor.cs
  - Source/LinqToDB/Reflection/MemberAccessor.cs
- Tier 2 (visited / total): 25 / 25 (100%) ✓
  - Read in full: Source/LinqToDB/Common/ConvertTo.cs
  - Read in full: Source/LinqToDB/Common/Convert{TFrom,TTo}.cs
  - Read in full: Source/LinqToDB/Common/Compilation.cs
  - Read in full: Source/LinqToDB/Common/Array{T}.cs
  - Read in full: Source/LinqToDB/Common/LinqToDBConvertException.cs
  - Read in full: Source/LinqToDB/Common/Option.cs
  - Read in full: Source/LinqToDB/Compatibility/System/Data/Linq/Binary.cs
  - Read in full: Source/LinqToDB/Concurrency/ConcurrencyExtensions.cs
  - Read in full: Source/LinqToDB/Reflection/TypeAccessor{T}.cs
  - Read in full: Source/LinqToDB/Reflection/IObjectFactory.cs
  - Read in full: Source/LinqToDB/Reflection/ObjectFactoryAttribute.cs
  - Read in full: Source/LinqToDB/Reflection/ObjectFactory{T}.cs
  - Read in full: Source/LinqToDB/Properties/AssemblyInfo.cs
  - Read in full: Source/LinqToDB/Configuration/ConnectionStringSettings.cs
  - Read in full: Source/LinqToDB/Configuration/IConnectionStringSettings.cs
  - Read in full: Source/LinqToDB/Configuration/DataProviderElement.cs
  - Read in full: Source/LinqToDB/Configuration/DataProviderElementCollection.cs
  - Read in full: Source/LinqToDB/Configuration/IDataProviderSettings.cs
  - Read in full: Source/LinqToDB/Configuration/ElementBase.cs
  - Read in full: Source/LinqToDB/Configuration/ElementCollectionBase.cs
  - Read in full: Source/LinqToDB/Configuration/LinqToDBSection.cs
  - Read in full: Source/LinqToDB/Configuration/LinqToDBSettings.cs
  - Read in full: Source/LinqToDB/Configuration/ILinqToDBSettings.cs
  - Read in full: Source/LinqToDB/Configuration/NamedValue.cs
  - Sampled (first ~180 lines + dispatch shape verified by grep across remaining operator regions): Source/LinqToDB/Async/AsyncExtensions.generated.cs (large generated file with strictly homogeneous per-operator shape verified at `:18`–`:38`, `:126`–`:146`, `:153`–`:173`)
- Tier 3 (skipped, logged): 4 (Resources/*.txt — embedded resources, not C#)
</details>
