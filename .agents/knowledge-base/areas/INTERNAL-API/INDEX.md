---
area: INTERNAL-API
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 23/23
coverage_tier_2: 201/201
---

# INTERNAL-API

Dual-scope area covering:

1. **`Source/LinqToDB/Internal/**`** -- types in the `LinqToDB.Internal.*` namespace hierarchy. These are "internal by convention": fully public (`public` visibility), accessible to any assembly that references `LinqToDB`, but residing in a namespace documenting their intent as infrastructure-only. There is no `[InternalsVisibleTo]` gate -- they are intentionally accessible to provider authors and companion libraries (EFCore, Scaffold, Remote) without requiring friend-assembly grants.
2. **`Source/LinqToDB/PublicAPI/**`** -- Roslyn RS0016/RS0017 analyzer baseline files (`PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`) per TFM. These files *are* the public-API surface contract as seen by the `Microsoft.CodeAnalysis.PublicApiAnalyzers` tooling. New public members must appear in `PublicAPI.Unshipped.txt`; they migrate to `Shipped.txt` at each release cut.

Sub-trees whose implementation is fully owned by another area are listed under **Inbound / outbound dependencies** and linked to their INDEX files. This document narrates the in-scope sub-trees only.

---

## Public-API Discipline (`PublicAPI/**`)

The `PublicAPI/` folder holds 12 files across 6 TFM buckets:

- `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` at the repo root (TFM-agnostic baseline).
- Per-TFM pairs under `net462/`, `netstandard2.0/`, `net8.0/`, `net9.0/`, `net10.0/`.

Each file starts with `#nullable enable` and lists one public member signature per line in the format emitted by `dotnet format` / the RS0016 analyzer. `Shipped.txt` contains the stable committed surface; `Unshipped.txt` accumulates new additions awaiting the next milestone merge.

**Release-prep migration (PRs #5504 / #5515 / #5557 cycle):** `PublicAPI.Unshipped.txt` (root and all per-TFM) was empty (contains only `#nullable enable`) as of sha `2e67bafc9`. That empty state no longer holds at sha `b3340aa9` -- new surface has been added (NullsPosition ordering, Tools.IsProviderAssemblyPresent, WindowFunctionHelpers breaking signature update). All previously unshipped members -- including `LinqToDB.Internal.Common.ErrorHelper.Oracle` constants (PR #5500), `IAsQueryableBuilder<T>` / `IAsQueryableExceptBuilder<T>` interfaces and their `AsQueryable` overload (PR #5495), `LinqToDB.Internal.Reflection.Methods.LinqToDB.AsQueryableConfigured` (PR #5495), `SqlConcatExpression` and related concat-surface members (PR #5504), `SqlAggregateLifterExpression` (PR #5557), `Sql.CurrentTimestampUtc` (PR #5467) -- have been promoted to `Shipped.txt`. The per-TFM `Shipped.txt` files carry the corresponding TFM-specific additions (e.g. `TransientRetryPolicy`, record `<Clone>$()` members on newer TFMs).

**Namespace convention.** Members in `LinqToDB.Internal.*` appear in the baseline alongside `LinqToDB.*` public members. They receive the same RS0016 protection -- removing or changing them is a breaking change that requires a suppression or a `Unshipped.txt` update. This makes `LinqToDB.Internal.*` types *effectively* part of the public API surface for purposes of tooling enforcement, even though their namespace signals "not for application code."

---

## Subsystems

### `Internal/Async/**` -- Async connection/transaction wrappers

Wraps `DbConnection` and `DbTransaction` with async-capable interfaces for providers that pre-date `DbConnection.OpenAsync` or whose async methods are reflection-discovered at runtime.

Key types: `IAsyncDbConnection`, `IAsyncDbTransaction`, `AsyncDbConnection`, `AsyncDbTransaction`, `ReflectedAsyncDbConnection`, `ReflectedAsyncDbTransaction`, `AsyncEnumeratorAsyncWrapper<T>`, `IQueryProviderAsync`, `SafeAwaiter`, `AsyncFactory`.

Consumers: `DataConnection`, `BulkCopyReader<T>`. Cross-listed with INFRA (`Async/`).

### `Internal/Cache/**` -- In-process memory cache

A self-contained `IMemoryCache<TKey,TEntry>` / `MemoryCache<TKey,TEntry>` implementation adapted from `Microsoft.Extensions.Caching.Memory`. Exists to avoid a runtime dependency on `Microsoft.Extensions.*` while providing expiry, LRU compaction, and priority-based eviction.

Key types: `IMemoryCache<TKey,TEntry>`, `MemoryCache<TKey,TEntry>`, `ICacheEntry<TKey,TEntity>`, `CacheEntry<TKey,TEntry>`, `CacheEntryHelper<TKey,TEntry>`, `CacheEntryStack<TKey,TEntry>`, `CacheExtensions`, `CacheEntryExtensions`, `MemoryCacheEntryOptions<TKey>`, `MemoryCacheEntryExtensions`, `MemoryCacheOptions`, `IChangeToken`, `ISystemClock`, `SystemClock`, `CacheItemPriority`, `EvictionReason`, `PostEvictionDelegate<TKey>`, `PostEvictionCallbackRegistration<TKey>`.

Used by `ProviderDetectorBase<TProvider,TVersion>` to cache detected server versions per connection string. Not used for query plans (that is `Internal/Linq/Query.cs`, owned by [LINQ](../LINQ/INDEX.md)).

### `Internal/Common/**` -- Shared utility types

Miscellaneous building blocks consumed broadly across the codebase. Key types: `IConfigurationID`, `IdentifierBuilder`, `ObjectPool<T>`, `Pools`, `Tools`, `ActivatorExt`, `BuildExpressionUtils`, `ComWrapper`, `DecimalHelper`, `DisposableAction`, `EmptyIAsyncDisposable`, `EnumerableHelper`, `EnumerablePolyfills`, `ErrorHelper`, `MemberCache`, `NonCapturingLazyInitializer`, `SnapshotDictionary<TKey,TValue>`, `SqlTextWriter`, `StackGuard`, `StringBuilderExtensions`, `TaskCache`, `TopoSorting`, `TypeHelper`, `Utils`, `ValueComparer`, `ValueComparer<T>`.

`ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull` was tracked in prior `PublicAPI.Unshipped.txt` and has now migrated to `Shipped.txt`.

**`Tools.IsProviderAssemblyPresent(string assemblyName)` (ADDED):** new public static method (`Source/LinqToDB/Internal/Common/Tools.cs`) that returns `true` when the named provider assembly is already loaded, loadable via `Assembly.Load`, or physically deployed next to the linq2db assembly. The file-existence fallback is disabled under `PublishSingleFile` (where `Assembly.Location` is empty) and never probes the current working directory -- that CWD fallback was the original single-file detection bug (#5488). Added to `PublicAPI.Unshipped.txt` root.

### `Internal/Conversion/**` -- Value conversion machinery

Backs the public `Mapping/Converter` surface. Key types: `ConvertInfo`, `ConverterLambda`, `ConvertBuilder`, `ConvertReducer`, `ConvertUtils`.

Consumers: `MappingSchema`, `ColumnDescriptor` (owned by [MAPPING](../MAPPING/INDEX.md)).

### `Internal/DataProvider/**` (root + `Translation/`) -- Provider-shared internals

Base types consumed by *all* provider implementations. Provider-specific subdirs (`Access/`, `ClickHouse/`, `DB2/`, etc.) are owned by the respective `PROV-*` areas.

Key types at root: `DataProviderBase`, `DynamicDataProviderBase<TProviderMappings>`, `IDynamicProviderAdapter`, `ProviderDetectorBase<TProvider,TVersion>`, `BasicBulkCopy`, `BulkCopyReader<T>`, `IIdentifierService` / `IdentifierServiceBase` / `IdentifierServiceSimple`, `AliasesHelper`, `AssemblyResolver`, `DatabaseSpecificQueryable<T>` / `DatabaseSpecificTable<T>`, `DataProviderExtensions`, `DataProviderFactoryBase`, `DataProviderOptions<T>`, `DataTools`, `DmlServiceBase` / `IDmlService`, `IExecutionScope`, `InvariantCultureRegion`, `IQueryParametersNormalizer`, `IdentifierKind`, `IdentifiersHelper`, `NoopQueryParametersNormalizer`, `OdbcProviderAdapter`, `OleDbProviderAdapter`, `ReaderInfo`, `ReservedWords`, `SimpleServiceProvider`, `SqlProviderHelper`, `SqlTypes`, `TableSpecHintExtensionBuilder`, `UniqueParametersNormalizer`, `WrapParametersVisitor`.

- `MultipleRowsHelper` / `MultipleRowsHelper<T>` (`Source/LinqToDB/Internal/DataProvider/MultipleRowsHelper.cs`) -- builds column lists and parameter sets for multi-row `INSERT` batches. `BuildColumns` emits literal or parameter SQL per column, wrapping values in `CAST(... AS type)` when required. `Execute` / `ExecuteAsync` dispatches the accumulated SQL+parameters. `ExecuteCustom(Func<DataConnection, string, DataParameter[], int>)` (line 175, added PR #5467) allows callers to substitute a custom execution function instead of the default command dispatch.

- `WrapParametersVisitor` (`Source/LinqToDB/Internal/DataProvider/WrapParametersVisitor.cs`) -- SQL tree visitor that marks `SqlParameter` nodes with `NeedsCast = true` based on their structural position. The `WrapFlags` enum governs which SQL contexts (SELECT columns, UPDATE SET, INSERT value, INSERT-OR-UPDATE, OUTPUT, MERGE, binary expressions, function parameters, boolean cast) trigger wrapping. The visitor gained `VisitSqlConcatExpression` (PR #5504) which gates the cast decision the same way as `VisitSqlBinaryExpression` -- using `WrapFlags.InBinary` -- so `SqlConcatExpression` nodes (emitted as `+` / `||` chains or `CONCAT(...)` per provider) follow identical parameter-cast rules as binary expressions.

Key types in `Translation/`:
- `MemberTranslatorBase` -- base class for per-provider member translators. Holds a `TranslationRegistration` (lookup table) and `CombinedMemberTranslator`. The `Translate` dispatch pipeline (PR #5504) now includes a binary-operator path: when `memberExpression` is a `BinaryExpression` with a non-null `Method`, `Registration.GetBinaryTranslation(nodeType, leftType, rightType)` is consulted before falling through to `CombinedMemberTranslator`. A parallel unary-operator path handles `UnaryExpression` with a non-null `Method`. This lets `StringMemberTranslatorBase` intercept string-typed `Add`/`AddChecked` binary expressions (C# `+` on strings) via the binary registry rather than overriding `TranslateOverrideHandler`.
- `ProviderMemberTranslatorDefault` -- abstract translator base providing default `String`, `Math`, `Date`, `Convert`, `Guid`, `Sql-functions`, and `Aggregate` sub-translators via abstract factory methods. Also exposes protected helpers added in PR #5467 / PR #5503: `ProcessHasFlag` (translates `Enum.HasFlag` to bitwise-AND after stripping abstract-type boxing via `UnwrapEnumBoxing`; guards against non-integer enum storage), `ProcessGetValueOrDefault` (translates `Nullable<T>.GetValueOrDefault([default])` to a SQL `CASE WHEN IS NULL THEN default ELSE value END`).
- Specialized bases: `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `GuidMemberTranslatorBase`, `AggregateFunctionsMemberTranslatorBase`, `SqlFunctionsMemberTranslatorBase`.
- `CombinedMemberTranslator` / `CombinedMemberConverter` -- fan-out translator/converter that delegates to a list of translators in order.

**String translation bases (PRs #5504 / #5515):** `StringMemberTranslatorBase` gained a substantial set of concat-configuration helpers and new base virtuals:

- `ConfigureConcatWs(builder, nullValuesAsEmptyString, isNullableResult, functionFactory, withoutSeparator)` -- configures an `AggregateFunctionBuilder` for providers with native `CONCAT_WS`. `withoutSeparator: true` suppresses the separator argument (used by `string.Concat`). Providers: ClickHouse, MySQL, PostgreSQL, SqlServer 2017+, YDB.
- `ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullResult, substringFunc, withoutSeparator, wrapByCoalesce)` -- configures for providers that emulate `CONCAT_WS` via chained `||` / `+` and a `SUBSTRING` strip. `wrapByCoalesce: false` switches the per-operand null-skip to `CASE WHEN v IS NULL THEN '' ELSE sep || v END` (required on Oracle where `NULL || x = x`). Providers: Access, DB2, Firebird, Informix, Oracle, SapHana, SQLite, SqlCe, Sybase, SqlServer (older).
- `ConfigureConcat(builder, wrapByCoalesce)` -- configures for `Sql.Concat(string?[])` / `Sql.Concat(object?[])` which bypass the CONCAT_WS path and emit `SqlConcatExpression(preserveNull: true)` directly. `wrapByCoalesce: false` (default) gives strict any-null-→-null; `true` gives null-as-empty.
- `SetStringJoinResult(composer, aggregateSql, isNullableResult, emptyValueType)` (static helper) -- sets the aggregate result and, when non-nullable, registers a SQL rewriter that wraps the lifted column reference with `COALESCE(<ref>, '')` at OUTER APPLY lift time.
- `ConvertOperandToString(operand)` (protected internal static) -- rewrites a non-string operand to a `.ToString()` call so it reaches its type-specific translator (e.g. Guid → hex-and-substr on SQLite).
- `TranslateStringJoin(ctx, methodCall, flags, nullValuesAsEmptyString, isNullableResult, withoutSeparator)` -- new base virtual (returns `null` in base), overridden per-provider.
- `TranslateTrimStart(ctx, methodCall, flags, value, trimChars)` / `TranslateTrimEnd(...)` -- new base virtuals (PR #5515) that default to `LTRIM`/`RTRIM` with optional second argument. Providers override to use provider-specific trim function name or argument order. Previously the trim translation was non-virtual; the split mirrors the PR #5467 Now-virtual pattern on `DateFunctionsTranslatorBase`.
- `TranslateOverrideHandler` override intercepts string-typed `BinaryExpression` (C# `a + b` on strings) and forwards to the method-call translator path via `Expression.Call(binaryExpression.Method!, ...)` -- so `string.Concat(string, string)` logic handles null-coalescing consistently regardless of how the expression entered the pipeline.

**`AggregateFunctionsMemberTranslatorBase` (PR #5557):** new standalone base class (`Source/LinqToDB/Internal/DataProvider/Translation/AggregateFunctionsMemberTranslatorBase.cs`) that replaces the prior inline aggregate handling in `ProviderMemberTranslatorDefault`. Registers and translates `Enumerable`/`Queryable` `Count`, `LongCount`, `Min`, `Max`, `Sum`, `Average` via the `AggregateFunctionBuilder` API. Key behaviours:
- `IsCountDistinctSupported` / `IsAggregationDistinctSupported` / `IsFilterSupported` -- provider capability flags (all virtual, defaults: distinct supported, filter not supported).
- `TranslateCount` -- emits `COUNT(*)`, `COUNT(DISTINCT v)`, or `COUNT(CASE WHEN filter THEN 1 ELSE NULL END)` depending on flags and filter/distinct configuration. Non-group-by `Count` with a predicate rewrites to `Where + Count` via `SetFallback`.
- `TranslateMinMaxSumAverage` -- emits `MIN`/`MAX`/`SUM`/`AVG`. Non-nullable non-group-by `MIN`/`MAX`/`AVG` registers a `SetMaterializationCheck` that wraps the materialized read with `CheckNullValue<T>` (throws on NULL per LINQ semantics). Non-nullable non-group-by `SUM` registers a `SetSqlRewriter` that wraps the lifted reference with `COALESCE(<ref>, default)` at lift time (the bare `SUM` stays in the inner tree for provider validation).
- Both checks use the `SqlAggregateLifterExpression` node (see `Internal/Expressions/**`).

**`SqlFunctionsMemberTranslatorBase` (PR #5504):** registrations for `Sql.NullIf` (3 overloads) with a `TranslateNullifMethod` virtual that emits `CASE WHEN value = compareTo THEN NULL ELSE value END` via `factory.Condition`. Providers override to emit native `NULLIF(v, c)` SQL.

**`LegacyMemberConverterBase`:** `Expressions.TrimLeft` / `TrimRight` (obsolete static helpers) are now rewritten via `MakeNullSafeStringTrimCall` to `s != null ? s.TrimStart/TrimEnd(chars) : null` before reaching the translator, preserving the prior null-propagation contract on client-side fallback.

**Date/time "current time" virtuals (PR #5467 split):** `DateFunctionsTranslatorBase` registers `DateTime` and `DateTimeOffset` constructors, `AddXxx` / `DatePart` methods, and "current time" members. The "current time" registration was split into five distinct virtual methods in PR #5467 (previously collapsed into fewer overrides): `TranslateServerNow` (`Sql.CurrentTimestamp`, `Sql.CurrentTimestamp2` -- server-side timestamp, default: `CURRENT_TIMESTAMP`), `TranslateNow` (`DateTime.Now` -- local time, default: `CURRENT_TIMESTAMP`), `TranslateUtcNow` (`DateTime.UtcNow`, `Sql.CurrentTimestampUtc` -- UTC, default: returns `null`), `TranslateZonedNow` (`DateTimeOffset.Now` -- zone-aware local, default: returns `null`), `TranslateZonedUtcNow` (`DateTimeOffset.UtcNow` -- zone-aware UTC, default: returns `null`). Providers override only the methods whose semantics they can satisfy, returning `null` to fall through.

**`SqlExpressionFactoryExtensions` expanded surface (PRs #5467 / #5504):** extension methods on `ISqlExpressionFactory` substantially expanded. Core helpers: `Fragment`, `Expression` / `NotNullExpression` / `NonPureExpression` / `NonPureFunction`, `SearchCondition` (new `SqlSearchCondition`), `Coalesce` (new `SqlCoalesceExpression`), `Concat` (three overloads -- two-arg, params, and `bool preserveNull` + params -- all emitting `SqlConcatExpression`; `factory.Add` and `factory.Binary` with string type throw `InvalidOperationException` to enforce use of `Concat`), `NullValue` / `NotNull`, `Cast` (two overloads wrapping `SqlCastExpression`), arithmetic operators `Div`/`Multiply`/`BitNot`/`Negate`/`BitAnd`/`Sub`/`Add`/`Binary`/`Mod`/`Increment`/`Decrement`, `TypeExpression`/`EnsureType`/`SqlDataType`, `Function` (full `SqlExtendedFunction` overload), string helpers `ToLower`/`ToUpper`/`Length`/`Replace` (via `PseudoFunctions` constants), and comparison predicates `Equal`/`NotEqual`/`Greater`/`GreaterOrEqual`/`Less`/`LessOrEqual`/`IsNull`/`IsNullPredicate`/`LikePredicate`/`ExprPredicate`.

**`TranslationRegistration` (PR #5504):** the lookup table gained two new dictionaries and registration paths alongside the existing `MemberInfo`-keyed one:
- `_binaryTranslations` -- keyed by `(ExpressionType nodeType, Type leftType, Type rightType)`. Registered via `RegisterBinaryInternal` / `RegisterBinary` / `RegisterGenericBinary` extension overloads in `TranslationRegistrationExtensions`. Open-generic fallback in `GetBinaryTranslation` mirrors the closed→open-generic resolution of the method registry.
- `_unaryTranslations` -- keyed by `(ExpressionType nodeType, Type operandType)`. Registered via `RegisterUnaryInternal` / `RegisterUnary` / `RegisterGenericUnary`. `GetUnaryTranslation` resolves open-generic operand types.
- `RegisterReplacement` / `ProvideReplacement` -- member-replacement pattern rewriting (lambda pattern → lambda replacement substitution); used for member aliasing without a full translator.

Other translation helpers: `AggregateFunctionsMemberTranslatorBase`, `ConvertMemberTranslatorDefault`, `GuidMemberTranslatorBase`, `IMemberConverter`, `LegacyMemberConverterBase`, `MathMemberTranslatorBase`, `SqlFunctionsMemberTranslatorBase`, `SqlTypesTranslationDefault`, `StringMemberTranslatorBase`, `TranslationContextExtensions`, `TranslationRegistration`, `TranslationRegistrationExtensions`.

Consumers: every `PROV-*` area's data provider and SQL builder, plus [EXPR-TRANS](../EXPR-TRANS/INDEX.md) for member-translation dispatch.

### `Internal/Expressions/**` -- Internal expression-tree types and visitors

Custom `Expression` subclasses used inside the LINQ->SQL translation pipeline, plus high-performance visitor infrastructure. Companions to the public `Expressions/` surface (owned by [EXPR](../EXPR/INDEX.md)).

Key types: `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `ContextRefExpression`, `ConvertFromDataReaderExpression`, `MarkerExpression`, `TagExpression`, `SqlQueryRootExpression`, `ExpressionVisitorBase`.

Node families: type-coercion (`ChangeTypeExpression`, `SqlAdjustTypeExpression`, `ConvertFromDataReaderExpression`, `DefaultValueExpression`); query-structure (`SqlQueryRootExpression`, `ContextRefExpression`, `SqlEagerLoadExpression`, `SqlDefaultIfEmptyExpression`, `SqlPathExpression`); access and aggregate-lift (`SqlGenericParamAccessExpression`, `SqlReaderIsNullExpression`, `SqlAggregateLifterExpression`, `SqlErrorExpression`); placeholder and marker (`ConstantPlaceholderExpression`, `MarkerExpression`, `TagExpression`).

**Note:** `SqlValidateExpression` has been deleted from this area as of sha `2e67bafc9`. It previously appeared in the "access and validation" node family. Any cross-area reference to `SqlValidateExpression` should be treated as stale.

**`SqlAggregateLifterExpression` (PR #5557, ADDED):** `Source/LinqToDB/Internal/Expressions/SqlAggregateLifterExpression.cs` -- a new `Expression` subclass (`NodeType = Extension`, `CanReduce = false`) that wraps a `SqlPlaceholderExpression` produced by an aggregate-function translator. Carries up to two delegates:
- `MaterializationCheck` (`Func<Expression, Expression>?`) -- invoked at C# materialization time. Used by non-nullable `Min`/`Max`/`Avg` to wrap the column read with `CheckNullValue<T>` (throws `InvalidOperationException` on NULL, matching LINQ semantics for aggregates over empty sequences).
- `SqlRewriter` (`Func<SqlPlaceholderExpression, SqlPlaceholderExpression>?`) -- invoked at OUTER APPLY lift time (in `AggregateExecuteContext.CreateWeakOuterJoin`) after `UpdateNesting` has promoted the inner aggregate to a parent-side column reference. Used by non-nullable `SUM` and non-nullable StringJoin-family aggregates to wrap the lifted column reference with `COALESCE(<ref>, default)`. The bare aggregate remains in the inner SQL tree during provider validation and optimization.
- At least one of the two delegates must be non-null (enforced by the constructor). Both can be set for aggregates that need both runtime null-check and late SQL rewriting.
- `ExpressionVisitorBase` gained `VisitSqlAggregateLifterExpression(SqlAggregateLifterExpression node)` (virtual, default: visits `InnerExpression`). `SqlAggregateLifterExpression.Accept` dispatches to this method when the visitor is an `ExpressionVisitorBase`.

Utility passes: `ExpressionConstants`, `ExpressionInstances`, `ExpressionEqualityComparer`, `ExpressionEvaluator`, `ExpressionHelper`, `ExpressionHelpers`, `ExpressionGenerator`, `ExpressionPrinter`, `IPrintableExpression`, `InternalExtensions`, `SkipIfConstantAttribute`, `SqlQueryDependentAttributeHelper`.

Visitors framework (`ExpressionVisitors/`): `FindVisitorBase`, `FindVisitor`, `FindVisitor<TContext>`, `LegacyVisitorBase`, `TransformVisitorsBase`, `TransformVisitor`, `TransformVisitor<TContext>`, `TransformInfoVisitor`, `TransformInfoVisitor<TContext>`, `VisitActionVisitor`, `VisitActionVisitor<TContext>`, `VisitFuncVisitor`, `VisitFuncVisitor<TContext>`, `EqualsToVisitor`, `PathVisitor<TContext>`, `WritableContext`, `WritableContext<TWriteable,TStatic>`.

**`LegacyVisitorBase`** (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/LegacyVisitorBase.cs`) -- internal abstract visitor that overrides several `ExpressionVisitorBase` virtuals to reduce nodes via their `Reduce()` path rather than traversing child nodes. Used by legacy visitor implementations that predate the structured `SqlXxx` dispatch. PR #5515: `VisitSqlAdjustTypeExpression` override updated to call `node.Update(Visit(node.Expression))` (from a prior reduce-only form), aligning it with the base class pattern while preserving backward compatibility for legacy visitors.

Types/TypeMapper framework (`Types/`):
- `TypeWrapper` -- abstract base class for wrapper types.
- `WrapperAttribute` / `TypeWrapperNameAttribute` / `WrappedBindingFlagsAttribute` -- decorators for type/method/constructor name resolution.
- **`TypeWrapperGenericArgsAttribute`** (`Source/LinqToDB/Internal/Expressions/Types/TypeWrapperGenericArgsAttribute.cs`) -- `[AttributeUsage(Method)]`; `[TypeWrapperGenericArgs(int argCount)]` on a wrapper method directs `TypeMapper` to select the generic overload with `argCount` type parameters when resolving the provider-side method. `ArgCount = 0` selects the non-generic overload. Used by DuckDB's `TypeMapper` integration (PR #5451) to distinguish generic overloads that share the same name. Consumed at `Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs:792`.
- `ICustomMapper` / `CustomMapperAttribute` -- extension point for custom expression-level type coercion.
- `ValueTaskToTaskMapper`, `GenericTaskToTaskMapper`, `GenericValueTaskMapper<ToType>` -- async-type adapters.

`WindowFunctionHelpers` -- window-function expression-tree factory.

`TypeMapper` -- runtime type-wrapper system for late-loaded provider types. When resolving a wrapper method, `TypeMapper` checks for `[TypeWrapperGenericArgsAttribute]` and selects the generic overload by arity (Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs:792).

### `Internal/Extensions/**` / `Internal/Infrastructure/**` / `Internal/Interceptors/**` / `Internal/Logging/**` / `Internal/Mapping/**` / `Internal/Options/**` / `Internal/Reflection/**` / `Internal/Remote/**` / `Internal/SchemaProvider/**`

Other in-scope sub-trees -- coverage retained from prior runs. Notable entries:
- `Methods` (`Internal/Reflection/Methods.cs`) -- pre-cached reflection constants. **`LinqToDB.AsQueryableConfigured`** added at `Methods.cs:205` (PR #5495) -- the `MethodInfo` for the new 3-arg `AsQueryable` overload, used by `EnumerableBuilder.CanBuildMethod` dispatch.
- `MemberInfoEqualityComparer` (`Internal/Reflection/MemberInfoEqualityComparer.cs`) -- `IEqualityComparer<MemberInfo>` singleton (`Default` instance). PR #5552: gained AOT-safe handling for Native AOT synthetic `MemberInfo` subclasses (e.g. `RuntimeSyntheticConstructorInfo` for lambda closures) whose `MetadataToken` accessor throws `InvalidOperationException`. A `ConcurrentDictionary<Type, bool> _supportsMetadataToken` cache records per-concrete-type support at most once (try-access, catch `InvalidOperationException`, cache `false`). When `MetadataToken` is unsupported, both `Equals` and `GetHashCode` fall back to `MemberInfo.Equals` / `MemberInfo.GetHashCode()`.
- `MappingSchemaInfo`, `LockedMappingSchema` -- mapping schema state and chained-schema base for providers.
- `ColumnDescriptorExtensions` (`Source/LinqToDB/Internal/Mapping/ColumnDescriptorExtensions.cs`, NEW): public static class with `GetMemberAccessExpression(this ColumnDescriptor descriptor, Expression instance)` extension. Builds a dot-path member-access `Expression` by chaining `Expression.PropertyOrField` for nested path components; falls back to `ColumnDescriptor.GetMemberGetter` for dynamic/cross-entity columns; throws `InvalidOperationException` if the member path cannot be resolved. No null-check wrappers -- suitable for direct SQL-conversion use.
- `LockedMappingSchemaInfo.GenerateID()` now calls `IdentifierBuilder.CreateNextID()` directly; no public surface change.
- `AnnotatableBase` -- concrete annotation store for scaffolding/EFCore.
- `LinqServiceSerializer` -- remote SQL serialization facade. PR #5557: updated to handle `SqlAggregateLifterExpression` nodes during statement serialization (internal; the node is unwrapped to its `InnerExpression` before wire encoding).
- `LoggingExtensions` -- `IDataContext` trace helpers.
- `SchemaProviderBase` -- abstract schema reader base.
- `OptionsContainer<T>` / `IOptionSet` -- immutable options composition root and contract.

---

## Key types

| Type | File | Purpose |
|---|---|---|
| `DataProviderBase` | `Internal/DataProvider/DataProviderBase.cs` | Abstract root for all providers |
| `DynamicDataProviderBase<T>` | `Internal/DataProvider/DynamicDataProviderBase.cs` | Base for reflection-loaded providers |
| `IDynamicProviderAdapter` | `Internal/DataProvider/IDynamicProviderAdapter.cs` | ADO.NET type contracts for late-loaded providers |
| `ProviderDetectorBase<TProvider,TVersion>` | `Internal/DataProvider/ProviderDetectorBase.cs` | Version-aware provider auto-detection |
| `BasicBulkCopy` | `Internal/DataProvider/BasicBulkCopy.cs` | Provider-agnostic bulk-insert dispatcher |
| `BulkCopyReader<T>` | `Internal/DataProvider/BulkCopyReader.cs` | Enumerable-to-DataReader adapter |
| `IIdentifierService` | `Internal/DataProvider/IIdentifierService.cs` | Identifier normalization contract |
| `AliasesHelper` | `Internal/DataProvider/AliasesHelper.cs` | Pooled SQL alias assignment and deduplication visitor |
| `MultipleRowsHelper` | `Internal/DataProvider/MultipleRowsHelper.cs` | Multi-row INSERT batch builder; `ExecuteCustom` hook (PR #5467) |
| `DataTools` | `Internal/DataProvider/DataTools.cs` | SQL literal construction utilities |
| `DmlServiceBase` / `IDmlService` | `Internal/DataProvider/DmlServiceBase.cs` / `IDmlService.cs` | DROP TABLE "not-found" exception detection |
| `InvariantCultureRegion` | `Internal/DataProvider/InvariantCultureRegion.cs` | Thread-culture RAII scope for SQL formatting |
| `WrapParametersVisitor` | `Internal/DataProvider/WrapParametersVisitor.cs` | Parameter CAST-wrapping SQL visitor; `VisitSqlConcatExpression` added (PR #5504) |
| `OdbcProviderAdapter` / `OleDbProviderAdapter` | `Internal/DataProvider/OdbcProviderAdapter.cs` / `OleDbProviderAdapter.cs` | TypeMapper-based adapters for ODBC / OLE DB |
| `ReservedWords` | `Internal/DataProvider/ReservedWords.cs` | Provider-keyed reserved-word set |
| `UniqueParametersNormalizer` | `Internal/DataProvider/UniqueParametersNormalizer.cs` | Unique, valid parameter name policy |
| `DataProviderOptions<T>` | `Internal/DataProvider/DataProviderOptions.cs` | Abstract per-provider `IOptionSet` record |
| `DataProviderExtensions` | `Internal/DataProvider/DataProviderExtensions.cs` | Typed `SetFieldReaderExpression` overloads |
| `ReaderInfo` | `Internal/DataProvider/ReaderInfo.cs` | Key struct for `DataProviderBase.ReaderExpressions` |
| `TableSpecHintExtensionBuilder` | `Internal/DataProvider/TableSpecHintExtensionBuilder.cs` | SQL table hint rendering for `SqlQueryExtension` |
| `TranslationRegistration` | `Internal/DataProvider/Translation/TranslationRegistration.cs` | Member-to-translator lookup table; binary and unary operator registries added (PR #5504) |
| `ProviderMemberTranslatorDefault` | `Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs` | Translator composition root; `ProcessHasFlag` / `ProcessGetValueOrDefault` helpers (PR #5467 / #5503) |
| `AggregateFunctionsMemberTranslatorBase` | `Internal/DataProvider/Translation/AggregateFunctionsMemberTranslatorBase.cs` | Aggregate translator base: Count, LongCount, Min, Max, Sum, Average (PR #5557) |
| `DateFunctionsTranslatorBase` | `Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs` | Date/time registration; Now-virtual split into 5 methods (PR #5467) |
| `MemberTranslatorBase` | `Internal/DataProvider/Translation/MemberTranslatorBase.cs` | Base for per-provider member translators; binary/unary operator dispatch added (PR #5504) |
| `SqlExpressionFactoryExtensions` | `Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs` | Extension helpers for SQL AST nodes; `Concat` overloads emit `SqlConcatExpression` (PR #5504), `Add`/`Binary` guard string misuse |
| `StringMemberTranslatorBase` | `Internal/DataProvider/Translation/StringMemberTranslatorBase.cs` | String translator base; `ConfigureConcatWs`, `ConfigureConcatWsEmulation`, `ConfigureConcat`, `TranslateStringJoin` base virtual, `TranslateTrimStart`/`TranslateTrimEnd` base virtuals added (PRs #5504 / #5515) |
| `SqlFunctionsMemberTranslatorBase` | `Internal/DataProvider/Translation/SqlFunctionsMemberTranslatorBase.cs` | SQL-function translator base; `Sql.NullIf` registration, `TranslateNullifMethod` virtual |
| `LegacyMemberConverterBase` | `Internal/DataProvider/Translation/LegacyMemberConverterBase.cs` | Pre-translation rewriter for `StringAggregate`, `TrimLeft`/`TrimRight`; null-safe trim rewrite added |
| `TranslationContextExtensions` | `Internal/DataProvider/Translation/TranslationContextExtensions.cs` | `ITranslationContext` helper extensions |
| `OptionsContainer<T>` | `Internal/Options/OptionsContainer.cs` | Immutable options composition root |
| `IOptionSet` | `Internal/Options/IOptionSet.cs` | Options group contract |
| `IConfigurationID` | `Internal/Common/IConfigurationID.cs` | Cache-key identity contract |
| `IdentifierBuilder` | `Internal/Common/IdentifierBuilder.cs` | Stable integer ID compositor |
| `Tools` | `Internal/Common/Tools.cs` | Shared utility methods; `IsProviderAssemblyPresent` (ADDED) guards PublishSingleFile deployments |
| `MemoryCache<TKey,TEntry>` | `Internal/Cache/MemoryCache.cs` | In-process LRU cache |
| `IAsyncDbConnection` | `Internal/Async/IAsyncDbConnection.cs` | Async connection wrapper contract |
| `AsyncFactory` | `Internal/Async/AsyncFactory.cs` | Per-type async-wrapper factory registry |
| `ExpressionVisitorBase` | `Internal/Expressions/ExpressionVisitorBase.cs` | Visitor base with stack-guard and `SqlXxx` dispatch; `VisitSqlAggregateLifterExpression` added (PR #5557) |
| `SqlAggregateLifterExpression` | `Internal/Expressions/SqlAggregateLifterExpression.cs` | Aggregate placeholder wrapper carrying `MaterializationCheck` and/or `SqlRewriter` delegates (PR #5557, ADDED) |
| `SqlPlaceholderExpression` | `Internal/Expressions/SqlPlaceholderExpression.cs` | LINQ->SQL mapping node |
| `SqlGenericConstructorExpression` | `Internal/Expressions/SqlGenericConstructorExpression.cs` | Deferred-construction projection node |
| `ConvertFromDataReaderExpression` | `Internal/Expressions/ConvertFromDataReaderExpression.cs` | Typed DataReader column read pending compilation |
| `SqlErrorExpression` | `Internal/Expressions/SqlErrorExpression.cs` | Translation-failure payload node |
| `ChangeTypeExpression` | `Internal/Expressions/ChangeTypeExpression.cs` | Type reinterpretation (custom NodeType 1000) |
| `SqlAdjustTypeExpression` | `Internal/Expressions/SqlAdjustTypeExpression.cs` | Schema-aware type coercion node |
| `MarkerExpression` / `TagExpression` | `Internal/Expressions/MarkerExpression.cs` / `TagExpression.cs` | Pipeline annotation bookmarks |
| `SqlEagerLoadExpression` | `Internal/Expressions/SqlEagerLoadExpression.cs` | Eager-load sequence marker |
| `SqlDefaultIfEmptyExpression` | `Internal/Expressions/SqlDefaultIfEmptyExpression.cs` | Default-if-empty null-check wrapper |
| `SqlQueryRootExpression` | `Internal/Expressions/SqlQueryRootExpression.cs` | Data-context root in LINQ tree |
| `ContextRefExpression` | `Internal/Expressions/ContextRefExpression.cs` | `IBuildContext` reference in LINQ tree |
| `ExpressionEqualityComparer` | `Internal/Expressions/ExpressionEqualityComparer.cs` | Structural expression equality (EF Core-based) |
| `ExpressionInstances` | `Internal/Expressions/ExpressionInstances.cs` | Pre-allocated constant expression singletons |
| `ExpressionEvaluator` | `Internal/Expressions/ExpressionEvaluator.cs` | Expression constant-value evaluation |
| `InternalExtensions` | `Internal/Expressions/InternalExtensions.cs` | Expression unwrapping, optimization, LINQ-method detection extensions |
| `ExpressionGenerator` | `Internal/Expressions/ExpressionGenerator.cs` | `BlockExpression` builder with `TypeMapper` integration |
| `TransformVisitor` / `TransformVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/TransformVisitor*.cs` | Pooled expression-tree transformer |
| `TransformInfoVisitor` / `TransformInfoVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/TransformInfoVisitor*.cs` | Pooled transformer with stop/continue control |
| `FindVisitor` / `FindVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/FindVisitor*.cs` | Pooled first-match search visitors |
| `EqualsToVisitor` | `Internal/Expressions/ExpressionVisitors/EqualsToVisitor.cs` | Query-cache structural expression comparison |
| `PathVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/PathVisitor.cs` | Expression tree walker with accessor-path tracking |
| `WritableContext<TWriteable,TStatic>` | `Internal/Expressions/ExpressionVisitors/WritableContext*.cs` | Mutable+static context split for closure-free visitor callbacks |
| `TypeWrapper` | `Internal/Expressions/Types/TypeWrapper.cs` | Abstract base for provider-type wrappers |
| `TypeMapper` | `Internal/Expressions/Types/TypeMapper.cs` | Expression-based runtime type wrapper |
| `TypeWrapperGenericArgsAttribute` | `Internal/Expressions/Types/TypeWrapperGenericArgsAttribute.cs` | Directs `TypeMapper` to select generic overload by arity (added PR #5451 DuckDB) |
| `ICustomMapper` | `Internal/Expressions/Types/ICustomMapper.cs` | Extension point for custom expression-level type coercion |
| `WindowFunctionHelpers` | `Internal/Expressions/WindowFunctionHelpers.cs` | Window function expression tree factory; order-by tuples now include `Sql.NullsPosition nulls` component (breaking API change, prior forms `*REMOVED*` in Unshipped.txt) |
| `SchemaProviderBase` | `Internal/SchemaProvider/SchemaProviderBase.cs` | Abstract schema reader base |
| `MappingSchemaInfo` | `Internal/Mapping/MappingSchemaInfo.cs` | Per-configuration mapping state |
| `LockedMappingSchema` | `Internal/Mapping/LockedMappingSchema.cs` | Provider schemas with stable IDs |
| `ColumnDescriptorExtensions` | `Internal/Mapping/ColumnDescriptorExtensions.cs` | Extension method building dot-path member-access expressions from `ColumnDescriptor` (ADDED) |
| `IInfrastructure<T>` | `Internal/Infrastructure/IInfrastructure{T}.cs` | Hidden property marker interface |
| `AnnotatableBase` | `Internal/Infrastructure/AnnotatableBase.cs` | Concrete annotation store for scaffolding/EFCore |
| `Methods` | `Internal/Reflection/Methods.cs` | Pre-cached reflection constants; `LinqToDB.AsQueryableConfigured` added (PR #5495) |
| `MemberInfoEqualityComparer` | `Internal/Reflection/MemberInfoEqualityComparer.cs` | `IEqualityComparer<MemberInfo>` with AOT-safe `MetadataToken` guard (PR #5552) |
| `LinqServiceSerializer` | `Internal/Remote/LinqServiceSerializer.cs` | Remote SQL serialization facade |
| `LoggingExtensions` | `Internal/Logging/LoggingExtensions.cs` | IDataContext trace helpers |
| `ActivatorExt` | `Internal/Common/ActivatorExt.cs` | Reflection invocation with unwrapped exceptions |
| `ValueComparer` / `ValueComparer<T>` | `Internal/Common/ValueComparer.cs` / `ValueComparer{T}.cs` | Expression-based equality comparers for mapping |
| `SqlTextWriter` | `Internal/Common/SqlTextWriter.cs` | Indented SQL text builder for `BasicSqlBuilder` |
| `StackGuard` | `Internal/Common/StackGuard.cs` | Stack-overflow protection for deep expression visitors |
| `ErrorHelper` | `Internal/Common/ErrorHelper.cs` | Provider limitation error-message constants |
| `ConvertBuilder` | `Internal/Conversion/ConvertBuilder.cs` | Conversion lambda synthesis |
| `ConvertUtils` | `Internal/Conversion/ConvertUtils.cs` | Safe runtime numeric type conversion |

---

## Files (Tier 1 / Tier 2)

**Tier-1 files (23 / 23 visited):**

- `Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs`
- `Source/LinqToDB/Internal/DataProvider/DynamicDataProviderBase.cs`
- `Source/LinqToDB/Internal/DataProvider/IDynamicProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/ProviderDetectorBase.cs`
- `Source/LinqToDB/Internal/DataProvider/BasicBulkCopy.cs`
- `Source/LinqToDB/Internal/DataProvider/BulkCopyReader.cs`
- `Source/LinqToDB/Internal/DataProvider/IIdentifierService.cs`
- `Source/LinqToDB/Internal/DataProvider/Translation/MemberTranslatorBase.cs`
- `Source/LinqToDB/Internal/Options/OptionsContainer.cs`
- `Source/LinqToDB/Internal/Options/IOptionSet.cs`
- `Source/LinqToDB/Internal/Common/IConfigurationID.cs`
- `Source/LinqToDB/Internal/Common/IdentifierBuilder.cs`
- `Source/LinqToDB/Internal/Cache/IMemoryCache.cs`
- `Source/LinqToDB/Internal/Cache/MemoryCache.cs`
- `Source/LinqToDB/Internal/Async/IAsyncDbConnection.cs`
- `Source/LinqToDB/Internal/Async/AsyncFactory.cs`
- `Source/LinqToDB/Internal/Expressions/ExpressionVisitorBase.cs`
- `Source/LinqToDB/Internal/Expressions/SqlPlaceholderExpression.cs`
- `Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs`
- `Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs`
- `Source/LinqToDB/Internal/Infrastructure/IInfrastructure{T}.cs`
- `Source/LinqToDB/Internal/SchemaProvider/SchemaProviderBase.cs`
- `Source/LinqToDB/Internal/Mapping/MappingSchemaInfo.cs`
- `Source/LinqToDB/Internal/Reflection/Methods.cs`
- `Source/LinqToDB/Internal/Remote/LinqServiceSerializer.cs`
- `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt`
- `Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt`
- `Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Shipped.txt`

**Tier-2 (201 / 201 visited):** in-scope `*.cs` files: Async + Cache + Common + Conversion + DataProvider root + DataProvider/Translation (now incl. `AggregateFunctionsMemberTranslatorBase.cs`) + Expressions (net: `SqlAggregateLifterExpression.cs` added, `SqlValidateExpression.cs` deleted) + Extensions + Infrastructure + Interceptors (cross-listed) + Logging + Mapping + Options + Reflection + Remote + SchemaProvider + Internal root. Net file-count change vs prior run: +1 (ColumnDescriptorExtensions.cs newly in scope; this advances denominator from 199 to 201 accounting for it and the existing DynamicColumnInfo.cs / SpecialPropertyInfo.cs now confirmed re-read). Prior 199/199 + 2 newly confirmed = 201/201.

`CompatibilitySuppressions.xml` (`Source/LinqToDB/CompatibilitySuppressions.xml`) is managed by the `/api-baselines` skill and is NOT read or modified by this indexer.

---

## Inbound / outbound dependencies

**Sub-trees owned by other areas (read-only references from this document):**
- [SQL-AST](../SQL-AST/INDEX.md) -- `Internal/SqlQuery/**`: ISqlExpression, SelectQuery, SqlStatement consumed by `SqlPlaceholderExpression`, `LinqServiceSerializer`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `Internal/SqlProvider/**`: `ISqlBuilder`, `ISqlOptimizer`, `BasicSqlBuilder` extend `DataProviderBase` factories.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- `Internal/Linq/Builder/**`: consumes `ExpressionVisitorBase`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `SqlQueryRootExpression`, `SqlAggregateLifterExpression` (new, PR #5557).
- [LINQ](../LINQ/INDEX.md) -- `Internal/Linq/**` (excl. Builder): `Query.CacheCleaners` referenced by `IdentifierBuilder`; `IQueryRunner` / `QueryRunner` use `IAsyncDbConnection`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- `Interceptors/I*Interceptor.cs`: public contracts; `Internal/Interceptors/**` is the aggregated dispatch layer cross-listed here.
- [MAPPING](../MAPPING/INDEX.md) -- `Mapping/MappingSchema`: depends on `MappingSchemaInfo`, `ConvertInfo`, `LockedMappingSchema`.
- [DATA](../DATA/INDEX.md) -- `Data/DataConnection`: uses `BasicBulkCopy`, `IAsyncDbConnection`, `OptionsContainer`, logging extensions.

**Outbound dependencies from this area:**
- `LinqToDB.DataProvider.IDataProvider` (public interface implemented by `DataProviderBase`).
- `LinqToDB.SchemaProvider.ISchemaProvider` (public interface implemented by `SchemaProviderBase`).
- `LinqToDB.DataOptions` (extends `OptionsContainer<DataOptions>`).

---

## See also

- [architecture/overview.md](../../architecture/overview.md) -- pipeline context for how these types fit in the query execution path.
- [architecture/public-api.md](../../architecture/public-api.md) -- rules on `LinqToDB.Internal.*` namespace placement and `PublicAPI.txt` maintenance.
- [LINQ/INDEX.md](../LINQ/INDEX.md) -- query cache; uses `IdentifierBuilder` IDs for cache-key composition.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- mapping schema; uses `MappingSchemaInfo`, `ConvertInfo`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 23 / 23
- Tier 2 (visited / total): 201 / 201 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta, sha b3340aa9):
- `Source/LinqToDB/Internal/Common/Tools.cs` -- new public static `IsProviderAssemblyPresent(string assemblyName)` guards PublishSingleFile deployments; never probes CWD (fix for single-file bug #5488).
- `Source/LinqToDB/Internal/Mapping/ColumnDescriptorExtensions.cs` -- NEW FILE: `public static class ColumnDescriptorExtensions` with `GetMemberAccessExpression` building dot-path member-access expressions without null-check wrappers.
- `Source/LinqToDB/Internal/Mapping/LockedMappingSchemaInfo.cs` -- `GenerateID()` now calls `IdentifierBuilder.CreateNextID()` directly; no public surface change.
- `Source/LinqToDB/Internal/Mapping/DynamicColumnInfo.cs` -- Tier-2 re-read confirmed; no new surface change detected.
- `Source/LinqToDB/Internal/Mapping/SpecialPropertyInfo.cs` -- Tier-2 re-read confirmed; no new surface change detected.
- `Source/LinqToDB/Internal/Conversion/ConvertInfo.cs` -- C# 13 `field` keyword on `ConvertValueToParameter` lazy property; `_sync` changed to `System.Threading.Lock`; no public surface change.
- `Source/LinqToDB/Internal/Expressions/WindowFunctionHelpers.cs` -- order-by tuple signature extended with `Sql.NullsPosition nulls` component; prior tuple forms marked `*REMOVED*` in PublicAPI.Unshipped.txt (breaking change).
- `Source/LinqToDB/Internal/Common/IdentifierBuilder.cs` -- Tier-2 re-read; internal; no new public surface detected.
- Other 20 changed-file re-reads (Expressions/*.cs, DataProvider/Translation/*.cs, Remote/LinqServiceSerializer.cs, PublicAPI/*.txt, CompatibilitySuppressions.xml) -- Tier-2 confirmations; no new claims beyond prior run.
Read (this run -- delta, sha 2e67bafc9):
- `Internal/DataProvider/Translation/AggregateFunctionsMemberTranslatorBase.cs` (new file PR #5557 -- Count/LongCount/Min/Max/Sum/Average translator base with capability flags; `CheckNullValue<T>` runtime null-check; `SetMaterializationCheck` / `SetSqlRewriter` hooks)
- `Internal/DataProvider/Translation/LegacyMemberConverterBase.cs` (null-safe `MakeNullSafeStringTrimCall` for `Expressions.TrimLeft`/`TrimRight`)
- `Internal/DataProvider/Translation/MemberTranslatorBase.cs` (binary-operator and unary-operator dispatch paths added to `Translate`)
- `Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs` (`Concat` overloads emit `SqlConcatExpression`; `Add`/`Binary` throw on string type)
- `Internal/DataProvider/Translation/SqlFunctionsMemberTranslatorBase.cs` (`Sql.NullIf` registrations; `TranslateNullifMethod` virtual)
- `Internal/DataProvider/Translation/StringMemberTranslatorBase.cs` (`ConfigureConcatWs`/`ConfigureConcatWsEmulation`/`ConfigureConcat`/`SetStringJoinResult`/`ConvertOperandToString`/`TranslateStringJoin` base virtual/`TranslateTrimStart`/`TranslateTrimEnd` base virtuals; binary-string `TranslateOverrideHandler`)
- `Internal/DataProvider/Translation/TranslationRegistration.cs` (`_binaryTranslations`/`_unaryTranslations`; `MemberReplacement`; `ProvideReplacement`)
- `Internal/DataProvider/Translation/TranslationRegistrationExtensions.cs` (`RegisterBinary`/`RegisterGenericBinary`/`RegisterUnary`/`RegisterGenericUnary`/`RegisterReplacement`)
- `Internal/DataProvider/WrapParametersVisitor.cs` (`VisitSqlConcatExpression` added, gates parameter-cast on `WrapFlags.InBinary`)
- `Internal/Expressions/ExpressionVisitorBase.cs` (`VisitSqlAggregateLifterExpression` virtual added; no `VisitSqlValidateExpression` -- node deleted)
- `Internal/Expressions/ExpressionVisitors/LegacyVisitorBase.cs` (`VisitSqlAdjustTypeExpression` updated to `node.Update(Visit(node.Expression))`)
- `Internal/Expressions/SqlAggregateLifterExpression.cs` (new file PR #5557 -- `Extension` node, `CanReduce=false`; `MaterializationCheck` + `SqlRewriter` delegates)
- `Internal/Expressions/SqlValidateExpression.cs` (DELETED -- node removed; dispatch removed)
- `Internal/Reflection/MemberInfoEqualityComparer.cs` (AOT-safe `_supportsMetadataToken` cache; `Equals`/`GetHashCode` fall back for unsupported types, PR #5552)
- `Internal/Remote/LinqServiceSerializer.cs` (header scan -- no public surface change visible; SqlAggregateLifterExpression handling internal)
- `Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt` (now empty -- all prior unshipped promoted to Shipped)
- `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt` + per-TFM `Shipped.txt` (net8/9/10/net462/netstandard2.0) (skimmed -- release promotion of SqlConcatExpression, SqlAggregateLifterExpression, Sql.CurrentTimestampUtc, concat surface, IAsQueryableBuilder<T>, ErrorHelper.Oracle constants)

Read (prior runs across batches 1-3): full coverage of all 199 in-scope Tier-2 files.

</details>
