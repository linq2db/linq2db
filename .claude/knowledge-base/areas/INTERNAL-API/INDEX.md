---
area: INTERNAL-API
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 23/23
coverage_tier_2: 199/199
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

Each file starts with `#nullable enable` and lists one public member signature per line in the format emitted by `dotnet format` / the RS0016 analyzer. `Shipped.txt` contains the stable committed surface; `Unshipped.txt` accumulates new additions awaiting the next milestone merge. As of `4a478ff14`, `PublicAPI.Unshipped.txt` (root) contains the following additions:

- `LinqToDB.Internal.Common.ErrorHelper.Oracle` constants added during the Oracle scalar-subquery fix (PR #5500).
- `LinqToDB.Linq.IAsQueryableBuilder<T>` and `IAsQueryableExceptBuilder<T>` -- new fluent-builder interfaces for the `AsQueryable` configured overload (PR #5495).
- `static LinqToDB.LinqExtensions.AsQueryable<TElement>(IEnumerable<TElement>, IDataContext, Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>>)` -- the new configure-delegate overload.
- `static readonly LinqToDB.Internal.Reflection.Methods.LinqToDB.AsQueryableConfigured -> MethodInfo` -- reflection constant for the new overload.

The per-TFM files exist because the `LinqToDB` project multi-targets and certain members are conditionally compiled. `net10.0/PublicAPI.Shipped.txt` carries TFM-specific additions such as `TransientRetryPolicy` and the `<Clone>$()` record members for provider options that appear on newer TFMs.

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

`ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull` is the addition tracked in current `PublicAPI.Unshipped.txt`.

### `Internal/Conversion/**` -- Value conversion machinery

Backs the public `Mapping/Converter` surface. Key types: `ConvertInfo`, `ConverterLambda`, `ConvertBuilder`, `ConvertReducer`, `ConvertUtils`.

Consumers: `MappingSchema`, `ColumnDescriptor` (owned by [MAPPING](../MAPPING/INDEX.md)).

### `Internal/DataProvider/**` (root + `Translation/`) -- Provider-shared internals

Base types consumed by *all* provider implementations. Provider-specific subdirs (`Access/`, `ClickHouse/`, `DB2/`, etc.) are owned by the respective `PROV-*` areas.

Key types at root: `DataProviderBase`, `DynamicDataProviderBase<TProviderMappings>`, `IDynamicProviderAdapter`, `ProviderDetectorBase<TProvider,TVersion>`, `BasicBulkCopy`, `BulkCopyReader<T>`, `IIdentifierService` / `IdentifierServiceBase` / `IdentifierServiceSimple`, `AliasesHelper`, `AssemblyResolver`, `DatabaseSpecificQueryable<T>` / `DatabaseSpecificTable<T>`, `DataProviderExtensions`, `DataProviderFactoryBase`, `DataProviderOptions<T>`, `DataTools`, `DmlServiceBase` / `IDmlService`, `IExecutionScope`, `InvariantCultureRegion`, `IQueryParametersNormalizer`, `IdentifierKind`, `IdentifiersHelper`, `NoopQueryParametersNormalizer`, `OdbcProviderAdapter`, `OleDbProviderAdapter`, `ReaderInfo`, `ReservedWords`, `SimpleServiceProvider`, `SqlProviderHelper`, `SqlTypes`, `TableSpecHintExtensionBuilder`, `UniqueParametersNormalizer`, `WrapParametersVisitor`.

- `MultipleRowsHelper` / `MultipleRowsHelper<T>` (`Source/LinqToDB/Internal/DataProvider/MultipleRowsHelper.cs`) -- builds column lists and parameter sets for multi-row `INSERT` batches. `BuildColumns` emits literal or parameter SQL per column, wrapping values in `CAST(... AS type)` when required. `Execute` / `ExecuteAsync` dispatches the accumulated SQL+parameters. `ExecuteCustom(Func<DataConnection, string, DataParameter[], int>)` (line 175, added PR #5467) allows callers to substitute a custom execution function instead of the default command dispatch.

Key types in `Translation/`:
- `MemberTranslatorBase` -- base class for per-provider member translators. Holds a `TranslationRegistration` (lookup table) and `CombinedMemberTranslator`.
- `ProviderMemberTranslatorDefault` -- abstract translator base providing default `String`, `Math`, `Date`, `Convert`, `Guid`, `Sql-functions`, and `Aggregate` sub-translators via abstract factory methods. Also exposes protected helpers added in PR #5467 / PR #5503: `ProcessHasFlag` (translates `Enum.HasFlag` to bitwise-AND after stripping abstract-type boxing via `UnwrapEnumBoxing`; guards against non-integer enum storage), `ProcessGetValueOrDefault` (translates `Nullable<T>.GetValueOrDefault([default])` to a SQL `CASE WHEN IS NULL THEN default ELSE value END`).
- Specialized bases: `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `GuidMemberTranslatorBase`, `AggregateFunctionsMemberTranslatorBase`, `SqlFunctionsMemberTranslatorBase`.
- `CombinedMemberTranslator` / `CombinedMemberConverter` -- fan-out translator/converter that delegates to a list of translators in order.

**Date/time "current time" virtuals (PR #5467 split):** `DateFunctionsTranslatorBase` registers `DateTime` and `DateTimeOffset` constructors, `AddXxx` / `DatePart` methods, and "current time" members. The "current time" registration was split into five distinct virtual methods in PR #5467 (previously collapsed into fewer overrides): `TranslateServerNow` (`Sql.CurrentTimestamp`, `Sql.CurrentTimestamp2` -- server-side timestamp, default: `CURRENT_TIMESTAMP`), `TranslateNow` (`DateTime.Now` -- local time, default: `CURRENT_TIMESTAMP`), `TranslateUtcNow` (`DateTime.UtcNow`, `Sql.CurrentTimestampUtc` -- UTC, default: returns `null`), `TranslateZonedNow` (`DateTimeOffset.Now` -- zone-aware local, default: returns `null`), `TranslateZonedUtcNow` (`DateTimeOffset.UtcNow` -- zone-aware UTC, default: returns `null`). Providers override only the methods whose semantics they can satisfy, returning `null` to fall through.

**`SqlExpressionFactoryExtensions` expanded surface (PR #5467):** extension methods on `ISqlExpressionFactory` substantially expanded. Core helpers: `Fragment`, `Expression` / `NotNullExpression` / `NonPureExpression` / `NonPureFunction`, `SearchCondition` (new `SqlSearchCondition`), `Coalesce` (new `SqlCoalesceExpression`), `Concat` (string/expression concatenation overloads via `SqlBinaryExpression` with `Precedence.Concatenate`), `NullValue` / `NotNull`, `Cast` (two overloads wrapping `SqlCastExpression`), arithmetic operators `Div`/`Multiply`/`BitNot`/`Negate`/`BitAnd`/`Sub`/`Add`/`Binary`/`Mod`/`Increment`/`Decrement`, `TypeExpression`/`EnsureType`/`SqlDataType`, `Function` (full `SqlExtendedFunction` overload), string helpers `ToLower`/`ToUpper`/`Length`/`Replace` (via `PseudoFunctions` constants), and comparison predicates `Equal`/`NotEqual`/`Greater`/`GreaterOrEqual`/`Less`/`LessOrEqual`/`IsNull`/`IsNullPredicate`/`LikePredicate`/`ExprPredicate`.

Other translation helpers: `AggregateFunctionsMemberTranslatorBase`, `ConvertMemberTranslatorDefault`, `GuidMemberTranslatorBase`, `IMemberConverter`, `LegacyMemberConverterBase`, `MathMemberTranslatorBase`, `SqlFunctionsMemberTranslatorBase`, `SqlTypesTranslationDefault`, `StringMemberTranslatorBase`, `TranslationContextExtensions`, `TranslationRegistration`, `TranslationRegistrationExtensions`.

Consumers: every `PROV-*` area's data provider and SQL builder, plus [EXPR-TRANS](../EXPR-TRANS/INDEX.md) for member-translation dispatch.

### `Internal/Expressions/**` -- Internal expression-tree types and visitors

Custom `Expression` subclasses used inside the LINQ->SQL translation pipeline, plus high-performance visitor infrastructure. Companions to the public `Expressions/` surface (owned by [EXPR](../EXPR/INDEX.md)).

Key types: `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `ContextRefExpression`, `ConvertFromDataReaderExpression`, `MarkerExpression`, `TagExpression`, `SqlQueryRootExpression`, `ExpressionVisitorBase`.

Node families: type-coercion (`ChangeTypeExpression`, `SqlAdjustTypeExpression`, `ConvertFromDataReaderExpression`, `DefaultValueExpression`); query-structure (`SqlQueryRootExpression`, `ContextRefExpression`, `SqlEagerLoadExpression`, `SqlDefaultIfEmptyExpression`, `SqlPathExpression`); access and validation (`SqlGenericParamAccessExpression`, `SqlReaderIsNullExpression`, `SqlValidateExpression`, `SqlErrorExpression`); placeholder and marker (`ConstantPlaceholderExpression`, `MarkerExpression`, `TagExpression`).

Utility passes: `ExpressionConstants`, `ExpressionInstances`, `ExpressionEqualityComparer`, `ExpressionEvaluator`, `ExpressionHelper`, `ExpressionHelpers`, `ExpressionGenerator`, `ExpressionPrinter`, `IPrintableExpression`, `InternalExtensions`, `SkipIfConstantAttribute`, `SqlQueryDependentAttributeHelper`.

Visitors framework (`ExpressionVisitors/`): `FindVisitorBase`, `FindVisitor`, `FindVisitor<TContext>`, `LegacyVisitorBase`, `TransformVisitorsBase`, `TransformVisitor`, `TransformVisitor<TContext>`, `TransformInfoVisitor`, `TransformInfoVisitor<TContext>`, `VisitActionVisitor`, `VisitActionVisitor<TContext>`, `VisitFuncVisitor`, `VisitFuncVisitor<TContext>`, `EqualsToVisitor`, `PathVisitor<TContext>`, `WritableContext`, `WritableContext<TWriteable,TStatic>`.

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
- `MappingSchemaInfo`, `LockedMappingSchema` -- mapping schema state and chained-schema base for providers.
- `AnnotatableBase` -- concrete annotation store for scaffolding/EFCore.
- `LinqServiceSerializer` -- remote SQL serialization facade.
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
| `WrapParametersVisitor` | `Internal/DataProvider/WrapParametersVisitor.cs` | Parameter CAST-wrapping SQL visitor |
| `OdbcProviderAdapter` / `OleDbProviderAdapter` | `Internal/DataProvider/OdbcProviderAdapter.cs` / `OleDbProviderAdapter.cs` | TypeMapper-based adapters for ODBC / OLE DB |
| `ReservedWords` | `Internal/DataProvider/ReservedWords.cs` | Provider-keyed reserved-word set |
| `UniqueParametersNormalizer` | `Internal/DataProvider/UniqueParametersNormalizer.cs` | Unique, valid parameter name policy |
| `DataProviderOptions<T>` | `Internal/DataProvider/DataProviderOptions.cs` | Abstract per-provider `IOptionSet` record |
| `DataProviderExtensions` | `Internal/DataProvider/DataProviderExtensions.cs` | Typed `SetFieldReaderExpression` overloads |
| `ReaderInfo` | `Internal/DataProvider/ReaderInfo.cs` | Key struct for `DataProviderBase.ReaderExpressions` |
| `TableSpecHintExtensionBuilder` | `Internal/DataProvider/TableSpecHintExtensionBuilder.cs` | SQL table hint rendering for `SqlQueryExtension` |
| `TranslationRegistration` | `Internal/DataProvider/Translation/TranslationRegistration.cs` | Member-to-translator lookup table |
| `ProviderMemberTranslatorDefault` | `Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs` | Translator composition root; `ProcessHasFlag` / `ProcessGetValueOrDefault` helpers (PR #5467 / #5503) |
| `DateFunctionsTranslatorBase` | `Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs` | Date/time registration; Now-virtual split into 5 methods (PR #5467) |
| `MemberTranslatorBase` | `Internal/DataProvider/Translation/MemberTranslatorBase.cs` | Base for per-provider member translators |
| `SqlExpressionFactoryExtensions` | `Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs` | Extension helpers for SQL AST nodes (Coalesce, Cast, arithmetic, predicates, expanded PR #5467) |
| `TranslationContextExtensions` | `Internal/DataProvider/Translation/TranslationContextExtensions.cs` | `ITranslationContext` helper extensions |
| `OptionsContainer<T>` | `Internal/Options/OptionsContainer.cs` | Immutable options composition root |
| `IOptionSet` | `Internal/Options/IOptionSet.cs` | Options group contract |
| `IConfigurationID` | `Internal/Common/IConfigurationID.cs` | Cache-key identity contract |
| `IdentifierBuilder` | `Internal/Common/IdentifierBuilder.cs` | Stable integer ID compositor |
| `MemoryCache<TKey,TEntry>` | `Internal/Cache/MemoryCache.cs` | In-process LRU cache |
| `IAsyncDbConnection` | `Internal/Async/IAsyncDbConnection.cs` | Async connection wrapper contract |
| `AsyncFactory` | `Internal/Async/AsyncFactory.cs` | Per-type async-wrapper factory registry |
| `ExpressionVisitorBase` | `Internal/Expressions/ExpressionVisitorBase.cs` | Visitor base with stack-guard and `SqlXxx` dispatch |
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
| `WindowFunctionHelpers` | `Internal/Expressions/WindowFunctionHelpers.cs` | Window function expression tree factory |
| `SchemaProviderBase` | `Internal/SchemaProvider/SchemaProviderBase.cs` | Abstract schema reader base |
| `MappingSchemaInfo` | `Internal/Mapping/MappingSchemaInfo.cs` | Per-configuration mapping state |
| `LockedMappingSchema` | `Internal/Mapping/LockedMappingSchema.cs` | Provider schemas with stable IDs |
| `IInfrastructure<T>` | `Internal/Infrastructure/IInfrastructure{T}.cs` | Hidden property marker interface |
| `AnnotatableBase` | `Internal/Infrastructure/AnnotatableBase.cs` | Concrete annotation store for scaffolding/EFCore |
| `Methods` | `Internal/Reflection/Methods.cs` | Pre-cached reflection constants; `LinqToDB.AsQueryableConfigured` added (PR #5495) |
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
- `Source/LinqToDB/Internal/Options/IOptionsContainer.cs`
- `Source/LinqToDB/Internal/Options/IApplicable.cs`
- `Source/LinqToDB/Internal/Common/IConfigurationID.cs`
- `Source/LinqToDB/Internal/Common/IdentifierBuilder.cs`
- `Source/LinqToDB/Internal/Common/ObjectPool.cs`
- `Source/LinqToDB/Internal/Cache/IMemoryCache.cs`
- `Source/LinqToDB/Internal/Cache/MemoryCache.cs`
- `Source/LinqToDB/Internal/Async/IAsyncDbConnection.cs`
- `Source/LinqToDB/Internal/Async/AsyncFactory.cs`
- `Source/LinqToDB/Internal/Expressions/ExpressionVisitorBase.cs`
- `Source/LinqToDB/Internal/Expressions/SqlPlaceholderExpression.cs`
- `Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs`
- `Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs`
- `Source/LinqToDB/Internal/Expressions/Types/TypeWrapperGenericArgsAttribute.cs` (added PR #5451)
- `Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitorBase.cs`
- `Source/LinqToDB/Internal/Mapping/MappingSchemaInfo.cs`
- `Source/LinqToDB/Internal/Mapping/LockedMappingSchema.cs`
- `Source/LinqToDB/Internal/SchemaProvider/SchemaProviderBase.cs`
- `Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs`
- `Source/LinqToDB/Internal/Interceptors/IInterceptable.cs`
- `Source/LinqToDB/Internal/Infrastructure/IInfrastructure{T}.cs`
- `Source/LinqToDB/Internal/Infrastructure/Annotatable.cs`
- `Source/LinqToDB/Internal/Infrastructure/IAnnotation.cs`
- `Source/LinqToDB/Internal/Reflection/Methods.cs`
- `Source/LinqToDB/Internal/Remote/LinqServiceSerializer.cs`
- `Source/LinqToDB/Internal/Logging/LoggingExtensions.cs`
- `Source/LinqToDB/Internal/Conversion/ConvertInfo.cs`
- `Source/LinqToDB/Internal/Common/Tools.cs`
- `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt`
- `Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt`
- `Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Shipped.txt`
- `Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Unshipped.txt`

**Tier-2 (199 / 199 visited):** in-scope `*.cs` files: Async(10) + Cache(18) + Common(26) + Conversion(5) + DataProvider root(36) + DataProvider/Translation(18) + Expressions(62) + Extensions(11) + Infrastructure(12) + Interceptors(14, cross-listed) + Logging(1) + Mapping(8) + Options(5) + Reflection(2) + Remote(4) + SchemaProvider(7) + Internal root(1) = 240 `.cs` files in scope. Many are read in batches across prior runs; this run's delta read covers the changed files (TypeWrapperGenericArgsAttribute new, DateFunctionsTranslatorBase, ProviderMemberTranslatorDefault, SqlExpressionFactoryExtensions, MultipleRowsHelper, Methods, PublicAPI.Unshipped.txt).

`CompatibilitySuppressions.xml` (`Source/LinqToDB/CompatibilitySuppressions.xml`) is managed by the `/api-baselines` skill and is NOT read or modified by this indexer.

---

## Inbound / outbound dependencies

**Sub-trees owned by other areas (read-only references from this document):**
- [SQL-AST](../SQL-AST/INDEX.md) -- `Internal/SqlQuery/**`: ISqlExpression, SelectQuery, SqlStatement consumed by `SqlPlaceholderExpression`, `LinqServiceSerializer`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `Internal/SqlProvider/**`: `ISqlBuilder`, `ISqlOptimizer`, `BasicSqlBuilder` extend `DataProviderBase` factories.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- `Internal/Linq/Builder/**`: consumes `ExpressionVisitorBase`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `SqlQueryRootExpression`.
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
- [conventions/public-api-discipline.md](../../conventions/public-api-discipline.md) -- rules on `LinqToDB.Internal.*` namespace placement and `PublicAPI.txt` maintenance.
- [LINQ/INDEX.md](../LINQ/INDEX.md) -- query cache; uses `IdentifierBuilder` IDs for cache-key composition.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- mapping schema; uses `MappingSchemaInfo`, `ConvertInfo`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 23 / 23 (+1 new file `TypeWrapperGenericArgsAttribute.cs` added Tier-1 by PR #5451)
- Tier 2 (visited / total): 199 / 199 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta, sha 4a478ff14):
- `Source/LinqToDB/Internal/Expressions/Types/TypeWrapperGenericArgsAttribute.cs` (new file -- PR #5451 DuckDB)
- `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs` (Now-virtual split into 5 methods)
- `Source/LinqToDB/Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs` (ProcessHasFlag, ProcessGetValueOrDefault, UnwrapEnumBoxing added)
- `Source/LinqToDB/Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs` (Coalesce, Concat, SearchCondition, NonPureFunction, NullValue, NotNull, Cast, arithmetic ops, TypeExpression, EnsureType, SqlDataType, Function, ToLower/ToUpper/Length/Replace, predicates)
- `Source/LinqToDB/Internal/DataProvider/MultipleRowsHelper.cs` (ExecuteCustom added at :175)
- `Source/LinqToDB/Internal/Reflection/Methods.cs` (AsQueryableConfigured added at :205)
- `Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt` (IAsQueryableBuilder<T>, IAsQueryableExceptBuilder<T>, AsQueryable overload, AsQueryableConfigured MethodInfo)
- `Source/LinqToDB/Internal/DataProvider/BasicBulkCopy.cs` (touched, no structural API change)
- `Source/LinqToDB/Internal/DataProvider/DynamicDataProviderBase.cs` (touched, no structural API change)
- `Source/LinqToDB/CompatibilitySuppressions.xml` -- not read this run (managed by /api-baselines skill)

Read (prior runs across batches 1-3): full coverage of all 199 in-scope Tier-2 files. See git history for per-file detail.

</details>
