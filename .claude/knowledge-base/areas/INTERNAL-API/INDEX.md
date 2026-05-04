---
area: INTERNAL-API
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 22/22
coverage_tier_2: 199/199
---

# INTERNAL-API

Dual-scope area covering:

1. **`Source/LinqToDB/Internal/**`** — types in the `LinqToDB.Internal.*` namespace hierarchy. These are "internal by convention": fully public (`public` visibility), accessible to any assembly that references `LinqToDB`, but residing in a namespace documenting their intent as infrastructure-only. There is no `[InternalsVisibleTo]` gate — they are intentionally accessible to provider authors and companion libraries (EFCore, Scaffold, Remote) without requiring friend-assembly grants.
2. **`Source/LinqToDB/PublicAPI/**`** — Roslyn RS0016/RS0017 analyzer baseline files (`PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`) per TFM. These files *are* the public-API surface contract as seen by the `Microsoft.CodeAnalysis.PublicApiAnalyzers` tooling. New public members must appear in `PublicAPI.Unshipped.txt`; they migrate to `Shipped.txt` at each release cut.

Sub-trees whose implementation is fully owned by another area are listed under **Inbound / outbound dependencies** and linked to their INDEX files. This document narrates the in-scope sub-trees only.

---

## Public-API Discipline (`PublicAPI/**`)

The `PublicAPI/` folder holds 12 files across 6 TFM buckets:

- `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` at the repo root (TFM-agnostic baseline).
- Per-TFM pairs under `net462/`, `netstandard2.0/`, `net8.0/`, `net9.0/`, `net10.0/`.

Each file starts with `#nullable enable` and lists one public member signature per line in the format emitted by `dotnet format` / the RS0016 analyzer. `Shipped.txt` contains the stable committed surface; `Unshipped.txt` accumulates new additions awaiting the next milestone merge. As of `d8650bb48`, `PublicAPI.Unshipped.txt` (root) contains a single addition: `LinqToDB.Internal.Common.ErrorHelper.Oracle` constants added during the Oracle scalar-subquery fix (PR #5500, commit `4f98a0233`).

The per-TFM files exist because the `LinqToDB` project multi-targets and certain members are conditionally compiled. `net10.0/PublicAPI.Shipped.txt` (`Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Shipped.txt`) carries TFM-specific additions such as `TransientRetryPolicy` and the `<Clone>$()` record members for provider options that appear on newer TFMs.

**Namespace convention.** Members in `LinqToDB.Internal.*` appear in the baseline alongside `LinqToDB.*` public members. They receive the same RS0016 protection — removing or changing them is a breaking change that requires a suppression or a `Unshipped.txt` update. This makes `LinqToDB.Internal.*` types *effectively* part of the public API surface for purposes of tooling enforcement, even though their namespace signals "not for application code."

---

## Subsystems

### `Internal/Async/**` — Async connection/transaction wrappers

Wraps `DbConnection` and `DbTransaction` with async-capable interfaces for providers that pre-date `DbConnection.OpenAsync` or whose async methods are reflection-discovered at runtime.

Key types:
- `IAsyncDbConnection` (`Source/LinqToDB/Internal/Async/IAsyncDbConnection.cs`) — contract wrapping a `DbConnection` with `OpenAsync`, `CloseAsync`, `BeginTransactionAsync`. Consumed by `DataConnection` for all connection lifecycle operations.
- `IAsyncDbTransaction` (`Source/LinqToDB/Internal/Async/IAsyncDbTransaction.cs`) — parallel contract for `DbTransaction`; exposes `CommitAsync` / `RollbackAsync` in addition to sync `Commit` / `Rollback`.
- `AsyncDbConnection` (`Source/LinqToDB/Internal/Async/AsyncDbConnection.cs`) — default `IAsyncDbConnection` implementation delegating to `DbConnection` virtual async methods; instruments every lifecycle call through `ActivityService` for tracing. `#if ADO_ASYNC` guards the newer `CloseAsync` / `BeginTransactionAsync` paths.
- `AsyncDbTransaction` (`Source/LinqToDB/Internal/Async/AsyncDbTransaction.cs`) — default `IAsyncDbTransaction` implementation with the same `ActivityService` instrumentation pattern; falls back to synchronous `Commit`/`Rollback` on pre-`ADO_ASYNC` TFMs.
- `ReflectedAsyncDbConnection` (`Source/LinqToDB/Internal/Async/ReflectedAsyncDbConnection.cs`) — sealed subclass of `AsyncDbConnection` that overrides `OpenAsync`, `CloseAsync`, `BeginTransactionAsync`, and `DisposeAsync` with reflection-discovered delegate fields. Constructed by `AsyncFactory` for providers whose driver does not derive from the standard `DbConnection` async virtuals.
- `ReflectedAsyncDbTransaction` (`Source/LinqToDB/Internal/Async/ReflectedAsyncDbTransaction.cs`) — sealed subclass of `AsyncDbTransaction` for reflection-discovered `CommitAsync` / `RollbackAsync` / `DisposeAsync`.
- `AsyncEnumeratorAsyncWrapper<T>` (`Source/LinqToDB/Internal/Async/AsyncEnumeratorAsyncWrapper.cs`) — `IAsyncEnumerator<T>` that defers obtaining the underlying enumerator and an optional `IAsyncDisposable` until `MoveNextAsync` is first called; used to bridge lazy async source acquisition.
- `IQueryProviderAsync` (`Source/LinqToDB/Internal/Async/IQueryProviderAsync.cs`) — public interface extending `IQueryProvider` with `ExecuteAsync<TResult>` and `ExecuteAsyncEnumerable<TResult>`; implemented by `ExpressionQueryImpl`.
- `SafeAwaiter` (`Source/LinqToDB/Internal/Async/SafeAwaiter.cs`) — static helper that synchronously runs a `ValueTask<T>` / `Task<T>` on the thread-pool to avoid deadlocks when the sync path must block. Dispatches via `Task.Run` + `GetAwaiter().GetResult()`.
- `AsyncFactory` (`Source/LinqToDB/Internal/Async/AsyncFactory.cs`) — per-type factory registry keyed by `ConcurrentDictionary<Type, Func<DbConnection, IAsyncDbConnection>>`. Providers or host apps can register custom wrappers via `AsyncFactory.RegisterConnectionFactory<TConnection>`.

Consumers: `DataConnection`, `BulkCopyReader<T>`. Cross-listed with INFRA (`Async/`), which provides the public `AsyncExtensions` layer above this one.

### `Internal/Cache/**` — In-process memory cache

A self-contained `IMemoryCache<TKey,TEntry>` / `MemoryCache<TKey,TEntry>` implementation adapted from the `Microsoft.Extensions.Caching.Memory` MIT-licensed source (all 16 files in this sub-folder carry the .NET Foundation copyright header). Exists to avoid a runtime dependency on `Microsoft.Extensions.*` while providing expiry, LRU compaction, and priority-based eviction.

Key types:
- `IMemoryCache<TKey,TEntry>` (`Source/LinqToDB/Internal/Cache/IMemoryCache.cs`) — generic cache contract (`TryGetValue`, `CreateEntry`, `Remove`).
- `MemoryCache<TKey,TEntry>` (`Source/LinqToDB/Internal/Cache/MemoryCache.cs`) — `ConcurrentDictionary`-backed implementation with absolute/sliding expiry and background scan. Used by `ProviderDetectorBase<TProvider,TVersion>` to cache detected server versions per connection string (Source/LinqToDB/Internal/DataProvider/ProviderDetectorBase.cs:56).
- `ICacheEntry<TKey,TEntity>` (`Source/LinqToDB/Internal/Cache/ICacheEntry.cs`) — entry interface carrying `Key`, `Value`, `AbsoluteExpiration`, `SlidingExpiration`, `ExpirationTokens`, `PostEvictionCallbacks`, `Priority`, and `Size`.
- `CacheEntry<TKey,TEntry>` (`Source/LinqToDB/Internal/Cache/CacheEntry.cs`) — sealed `ICacheEntry` implementation; uses `CacheEntryHelper<TKey,TEntry>` for ambient scope tracking during nested `CreateEntry` calls to propagate expiration tokens to parent entries. Eviction callbacks run on a background `Task`.
- `CacheEntryHelper<TKey,TEntry>` (`Source/LinqToDB/Internal/Cache/CacheEntryHelper.cs`) — uses `AsyncLocal<CacheEntryStack<TKey,TEntry>>` to maintain a per-async-flow stack of active `CacheEntry` instances, enabling `PropagateOptions` to copy expiration tokens from child to parent.
- `CacheEntryStack<TKey,TEntry>` (`Source/LinqToDB/Internal/Cache/CacheEntryStack.cs`) — immutable linked-list node for the scope stack used by `CacheEntryHelper`.
- `CacheExtensions` (`Source/LinqToDB/Internal/Cache/CacheExtensions.cs`) — `IMemoryCache` convenience overloads: `Get`, `Set` (with absolute/sliding/token expiry overloads), `GetOrCreate` (with and without context parameter to avoid closure allocations), `GetOrCreateAsync`.
- `CacheEntryExtensions` (`Source/LinqToDB/Internal/Cache/CacheEntryExtensions.cs`) — fluent builder extensions on `ICacheEntry<TKey,TEntry>`: `SetPriority`, `AddExpirationToken`, `SetAbsoluteExpiration`, `SetSlidingExpiration`, `RegisterPostEvictionCallback`, `SetValue`, `SetSize`, `SetOptions`.
- `MemoryCacheEntryOptions<TKey>` (`Source/LinqToDB/Internal/Cache/MemoryCacheEntryOptions.cs`) — POCO for batch-configuring a cache entry (mirrors `ICacheEntry` properties); used with `SetOptions` extension.
- `MemoryCacheEntryExtensions` (`Source/LinqToDB/Internal/Cache/MemoryCacheEntryExtensions.cs`) — fluent builder extensions on `MemoryCacheEntryOptions<TKey>` (parallel to `CacheEntryExtensions`).
- `MemoryCacheOptions` (`Source/LinqToDB/Internal/Cache/MemoryCacheOptions.cs`) — cache configuration: `Clock`, `ExpirationScanFrequency` (default 1 min), `SizeLimit`, `CompactionPercentage` (default 5%).
- `IChangeToken` (`Source/LinqToDB/Internal/Cache/IChangeToken.cs`) — change-notification contract for token-based expiry; mirrors `Microsoft.Extensions.Primitives.IChangeToken`.
- `ISystemClock` (`Source/LinqToDB/Internal/Cache/ISystemClock.cs`) — `DateTimeOffset UtcNow` abstraction for testable time; pragma suppresses MA0188 (`use TimeProvider`).
- `SystemClock` (`Source/LinqToDB/Internal/Cache/SystemClock.cs`) — `ISystemClock` implementation returning `DateTimeOffset.UtcNow`.
- `CacheItemPriority` (`Source/LinqToDB/Internal/Cache/CacheItemPriority.cs`) — `Low` / `Normal` / `High` / `NeverRemove` eviction priority enum.
- `EvictionReason` (`Source/LinqToDB/Internal/Cache/EvictionReason.cs`) — `None` / `Removed` / `Replaced` / `Expired` / `TokenExpired` / `Capacity` enum.
- `PostEvictionDelegate<TKey>` (`Source/LinqToDB/Internal/Cache/PostEvictionDelegate.cs`) — delegate type `(TKey key, object? value, EvictionReason reason, object? state)` for post-eviction callbacks.
- `PostEvictionCallbackRegistration<TKey>` (`Source/LinqToDB/Internal/Cache/PostEvictionCallbackRegistration.cs`) — pairs `PostEvictionDelegate<TKey>` with an arbitrary `State` object.

The cache is not used for query plans (that is `Internal/Linq/Query.cs`, owned by [LINQ](../LINQ/INDEX.md)); it is specifically the *provider detection* and any other short-lived key/value stores that need eviction.

### `Internal/Common/**` — Shared utility types

Miscellaneous building blocks consumed broadly across the codebase.

Key types:
- `IConfigurationID` (`Source/LinqToDB/Internal/Common/IConfigurationID.cs`) — single-property interface `int ConfigurationID { get; }`. Implemented by `MappingSchemaInfo`, `IOptionSet`, and any type participating in `IdentifierBuilder`-based cache-key composition.
- `IdentifierBuilder` (`Source/LinqToDB/Internal/Common/IdentifierBuilder.cs`) — `readonly struct IDisposable` that composes a stable `int` identifier from heterogeneous inputs (strings, types, expressions, delegates, `IConfigurationID` values). Uses `ObjectPool<StringBuilder>` for allocation-free building. Static concurrent dictionaries intern the generated IDs so equal inputs always produce the same `int`. Cache-invalidation hook: `Query.CacheCleaners.Enqueue(ClearCache)` registered at static init (Source/LinqToDB/Internal/Common/IdentifierBuilder.cs:42).
- `ObjectPool<T>` (`Source/LinqToDB/Internal/Common/ObjectPool.cs`) — bounded pool with `RentedElement : IDisposable` RAII rental. Adapted from `dotnet/runtime`.
- `Pools` (`Source/LinqToDB/Internal/Common/Pools.cs`) — static accessor for the shared `ObjectPool<StringBuilder>` (capacity 100) used by `SqlTextWriter`, `IdentifierBuilder`, and similar.
- `Tools` (`Source/LinqToDB/Internal/Common/Tools.cs`) — general-purpose extension methods: `IsNullOrEmpty`, assembly-path helpers, whitespace normalization.
- `ActivatorExt` (`Source/LinqToDB/Internal/Common/ActivatorExt.cs`) — public static wrapper around `Activator.CreateInstance` / `ConstructorInfo.Invoke` / `Delegate.DynamicInvoke` / `MethodBase.Invoke` that unwraps `TargetInvocationException` before re-throwing; used everywhere reflection-based construction occurs. All overloads suppress `RS0030` (banned API).
- `BuildExpressionUtils` (`Source/LinqToDB/Internal/Common/BuildExpressionUtils.cs`) — public static helpers for normalizing LINQ expression trees: `UnwrapEnumerableCasting` strips `AsQueryable`/`AsEnumerable` wrappings; `EnsureQueryable` / `EnsureEnumerableType` coerce sequence types for query-builder compatibility.
- `ComWrapper` (`Source/LinqToDB/Internal/Common/ComWrapper.cs`) — `DynamicObject`-based `IDisposable` wrapper for COM objects, used instead of `dynamic` which is unsupported on .NET Core < 5. Provides `TryGetMember`, `TrySetMember`, `TryInvokeMember` via `InvokeMember` reflection. Platform-guarded on non-Windows.
- `DecimalHelper` (`Source/LinqToDB/Internal/Common/DecimalHelper.cs`) — `GetFacets(decimal)` → `(precision, scale)` using `decimal.GetBits`; uses `SUPPORTS_SPAN` conditional for stack-allocated `Span<int>`.
- `DisposableAction` (`Source/LinqToDB/Internal/Common/DisposableAction.cs`) — sealed `IDisposable` wrapping an `Action`; primary pattern for scope-exit callbacks.
- `EmptyIAsyncDisposable` (`Source/LinqToDB/Internal/Common/EmptyIAsyncDisposable.cs`) — singleton `IAsyncDisposable` whose `DisposeAsync` is a no-op `ValueTask`; eliminates null checks on nullable async-disposable handles.
- `EnumerableHelper` (`Source/LinqToDB/Internal/Common/EnumerableHelper.cs`) — `Batch<T>(IEnumerable<T>, int)` and `Batch<T>(IAsyncEnumerable<T>, int)` for chunking sequences; `AsyncToSyncEnumerable` bridges `IAsyncEnumerator` to `IEnumerable` via `SafeAwaiter`; `SyncToAsyncEnumerable` wraps sync sources; `MapList` for reference-equal-aware list transformation.
- `EnumerablePolyfills` (`Source/LinqToDB/Internal/Common/EnumerablePolyfills.cs`) — `net462`-only `Append<T>` extension on `IEnumerable<T>` (polyfill for BCL method available on later TFMs).
- `ErrorHelper` (`Source/LinqToDB/Internal/Common/ErrorHelper.cs`) — public static class of provider-limitation error-message constants organized into nested static classes per provider (`Oracle`, `Sybase`, `ClickHouse`, `MySql`). `ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull` is the addition tracked in current `PublicAPI.Unshipped.txt`.
- `MemberCache` (`Source/LinqToDB/Internal/Common/MemberCache.cs`) — `ConcurrentDictionary<MemberInfo, Info>` caching `IsQueryable` detection for `MethodInfo`. Registers `Query.CacheCleaners.Enqueue(ClearCache)` at static init; `Info.IsQueryable` flags generic methods carrying `[IsQueryableAttribute]`.
- `NonCapturingLazyInitializer` (`Source/LinqToDB/Internal/Common/NonCapturingLazyInitializer.cs`) — allocation-free lazy initialization via `Volatile.Read` + `Interlocked.CompareExchange`; overloads for 1- and 2-parameter factory delegates avoid capturing `this` in lambdas. Used by `ValueComparer<T>` to compile `EqualsExpression` / `HashCodeExpression` on first use.
- `SnapshotDictionary<TKey,TValue>` (`Source/LinqToDB/Internal/Common/SnapshotDictionary.cs`) — `IDictionary<TKey,TValue>` with `TakeSnapshot` / `Rollback` / `Commit` support; tracks added keys per snapshot level using a `Stack<HashSet<TKey>>`. Remove and update operations throw when a snapshot is active.
- `SqlTextWriter` (`Source/LinqToDB/Internal/Common/SqlTextWriter.cs`) — `StringBuilder`-backed SQL text emitter used by `BasicSqlBuilder` (owned by [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)). Maintains indent level; `IndentScope : IDisposable` RAII helper increments/decrements `_indentValue`. All `Append` overloads call `AppendIndentIfNeeded()` on new-line.
- `StackGuard` (`Source/LinqToDB/Internal/Common/StackGuard.cs`) — recursion-depth guard for expression visitors. Checks `RuntimeHelpers.TryEnsureSufficientExecutionStack()` every 64 calls; if stack is near-exhausted and hop count is below `Configuration.TranslationThreadMaxHopCount`, re-dispatches the continuation onto a fresh thread-pool thread via `Task.Factory.StartNew`. Throws `InsufficientExecutionStackException` after too many hops.
- `StringBuilderExtensions` (`Source/LinqToDB/Internal/Common/StringBuilderExtensions.cs`) — `AppendByteArrayAsHexViaLookup32` / `AppendByteAsHexViaLookup32` for efficiently encoding byte arrays as hex strings using a pre-computed 256-entry lookup table.
- `TaskCache` (`Source/LinqToDB/Internal/Common/TaskCache.cs`) — static pre-completed `Task<bool>`, `Task<int>`, and `Task<DataConnectionTransaction?>` instances to avoid allocations in hot synchronous-result paths.
- `TopoSorting` (`Source/LinqToDB/Internal/Common/TopoSorting.cs`) — `TopoSort` / `GroupTopoSort` extension methods on `IEnumerable<T>` / `ICollection<T>`; context-threaded to avoid closures; throws `ArgumentException("Cycle detected.")` on circular dependencies. Adapted from the CodeJam library.
- `TypeHelper` (`Source/LinqToDB/Internal/Common/TypeHelper.cs`) — generic-argument remapping helpers (`RegisterTypeRemapping`, `EnumTypeRemapping`), `MakeMethodCall` / `MakeGenericMethod` for constructing `MethodCallExpression` with inferred type arguments, `GetEnumerableElementType`, and `IsEqualParameters`.
- `Utils` (`Source/LinqToDB/Internal/Common/Utils.cs`) — `MakeUniqueNames<T>` (ensures a set of objects receive unique string names with a numeric suffix strategy); `RemoveDuplicates` / `RemoveDuplicatesFromTail` list helpers; `ObjectReferenceEqualityComparer<T>`; C#14 extension `IsNullValue` (matches `null`, `DBNull`, and `INullable.IsNull`).
- `ValueComparer` (`Source/LinqToDB/Internal/Common/ValueComparer.cs`) — abstract base `IEqualityComparer` / `IEqualityComparer<object>` storing `EqualsExpression` / `HashCodeExpression` lambda pairs. `CreateDefault(Type, bool)` factory selects `DefaultDoubleValueComparer` / `DefaultFloatValueComparer` / `DefaultValueComparer<T>` by type category. Static `ConcurrentDictionary` cache avoids re-creation.
- `ValueComparer<T>` (`Source/LinqToDB/Internal/Common/ValueComparer{T}.cs`) — generic typed subclass; lazily compiles `EqualsExpression` / `HashCodeExpression` via `NonCapturingLazyInitializer`. `CreateDefaultEqualsExpression` selects `IStructuralEquatable`, `op_Equality`, typed `Equals`, or `object.Equals` in priority order. Used by mapping for value comparison.

### `Internal/Conversion/**` — Value conversion machinery

Backs the public `Mapping/Converter` surface. Stores and resolves lambdas that convert between CLR types and SQL parameter types.

Key types:
- `ConvertInfo` (`Source/LinqToDB/Internal/Conversion/ConvertInfo.cs`) — singleton registry (`ConvertInfo.Default`) mapping `(fromType, toType)` pairs to `LambdaInfo` wrappers. Each `LambdaInfo` carries the nullable-aware check lambda, the main conversion lambda, an optional pre-compiled `Delegate`, and a flag `IsSchemaSpecific` for conversions registered at schema scope.
- `ConverterLambda` (`Source/LinqToDB/Internal/Conversion/ConverterLambda.cs`) — `public record struct` holding `(LambdaExpression CheckNullLambda, LambdaExpression? Lambda, bool IsSchemaSpecific)`. Replaces the older class-based `LambdaInfo` for new conversion registrations.
- `ConvertBuilder` (`Source/LinqToDB/Internal/Conversion/ConvertBuilder.cs`) — public static class that synthesizes conversion lambdas from registered mappings; tries constructor injection, `GetValueOrDefault`, `.Value` property access, explicit/implicit operators, and falls back to `Convert.ChangeType` via `DefaultConverter`. Used by `MappingSchema` to fill `ConvertInfo.Default`.
- `ConvertReducer` (`Source/LinqToDB/Internal/Conversion/ConvertReducer.cs`) — sealed internal class with a single static `Reducer(Expression)` method that reduces `DefaultValueExpression` nodes in conversion chains; avoids double-wrapping when a direct conversion exists.
- `ConvertUtils` (`Source/LinqToDB/Internal/Conversion/ConvertUtils.cs`) — `TryConvert(object?, Type, out object?)` performs safe numeric widening and range-checked narrowing conversions at runtime; uses two static lookup tables (`_alwaysConvert` for lossless widening, `_rangedConvert` for range-checked narrowing) to avoid `try/catch` overhead.

Consumers: `MappingSchema`, `ColumnDescriptor` (owned by [MAPPING](../MAPPING/INDEX.md)).

### `Internal/DataProvider/**` (root + `Translation/`) — Provider-shared internals

Base types consumed by *all* provider implementations. Provider-specific subdirs (`Access/`, `ClickHouse/`, `DB2/`, etc.) are owned by the respective `PROV-*` areas.

Key types at root (`Source/LinqToDB/Internal/DataProvider/`):
- `DataProviderBase` (`Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs`) — abstract base class implementing `IDataProvider` and `IInfrastructure<IServiceProvider>`. Sets default `SqlProviderFlags`, registers `DbDataReader` field-accessor lambdas (`SetField<T>`), and exposes `ID` computed via `IdentifierBuilder(Name)`. All concrete providers (through `DynamicDataProviderBase<T>`) extend this.
- `DynamicDataProviderBase<TProviderMappings>` (`Source/LinqToDB/Internal/DataProvider/DynamicDataProviderBase.cs`) — extends `DataProviderBase` for providers loaded via late-binding (`Reflection.Emit`-generated adapters). Accepts an `IDynamicProviderAdapter` and delegates connection creation to it.
- `IDynamicProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/IDynamicProviderAdapter.cs`) — contract exposing ADO.NET type references (`ConnectionType`, `DataReaderType`, `ParameterType`, `CommandType`, `TransactionType`) plus a `CreateConnection` factory. All dynamic providers implement this (e.g. `DB2ProviderAdapter`, `MySqlProviderAdapter`).
- `ProviderDetectorBase<TProvider,TVersion>` (`Source/LinqToDB/Internal/DataProvider/ProviderDetectorBase.cs`) — abstract base for provider-version auto-detection. Uses `MemoryCache<string,TVersion?>` to cache detected versions per connection string. Concrete detectors override `DetectProvider` and `DetectServerVersion`. `ProviderDetectorBase<TProvider>` variant omits version detection.
- `BasicBulkCopy` (`Source/LinqToDB/Internal/DataProvider/BasicBulkCopy.cs`) — provider-agnostic bulk-insert dispatcher. Dispatches `BulkCopyType.MultipleRows` (multi-row `INSERT INTO … VALUES` batches), `RowByRow`, and `ProviderSpecificCopy`. Protected virtual methods `GetMultipleRowsSuffix`, `GetInsertInto`, `CastFirstRowLiteralOnUnionAll`, etc. allow provider-specific overrides. Called from `DataConnection` bulk-copy entry points (owned by [DATA](../DATA/INDEX.md)).
- `BulkCopyReader<T>` — `DbDataReader`-compatible adapter over `IEnumerable<T>` / `IAsyncEnumerable<T>` for the `MultipleRows` path.
- `IIdentifierService` / `IdentifierServiceBase` / `IdentifierServiceSimple` — normalize and validate SQL identifiers (column/table aliases) for a given provider's limits.
- `ProviderDetectorBase.CreateDataProvider<T>()` — `Lazy<IDataProvider>` factory that auto-registers the provider with `DataConnection` on first access.
- `DataProviderFactoryBase` — abstract XML-config-based provider factory used by the `Configuration/` system.
- `DatabaseSpecificQueryable<T>` / `DatabaseSpecificTable<T>` — `IQueryable<T>` wrappers carrying provider-specific hints to the query builder. Used by every provider's `SpecificQueryable` and `SpecificTable` types.
- `MultipleRowsHelper` — computes column lists and parameter sets for multi-row INSERT batches.
- `AssemblyResolver` / `OdbcProviderAdapter` / `OleDbProviderAdapter` — assembly-loading and ODBC/OLEDB reflection helpers.

#### DataProvider (root) — additional types (batch 2)

- `AliasesHelper` (`Source/LinqToDB/Internal/DataProvider/AliasesHelper.cs`) — pooled `SqlQueryVisitor` that assigns and deduplicates SQL aliases (table sources, columns, CTE fields, subquery fields) across the AST. Uses `Utils.MakeUniqueNames` and `IIdentifierService.CorrectAlias`; an incremental `prevAliasContext` input allows reuse of aliases across plan re-runs. Pool size 100.
- `AssemblyResolver` (`Source/LinqToDB/Internal/DataProvider/AssemblyResolver.cs`) — registers an `AppDomain.AssemblyResolve` handler that loads an assembly from a given file path on first demand. Used by providers that ship as separate NuGet assemblies (path or pre-loaded `Assembly` constructor overloads). Uses expression-based `Add` call to bypass CAS restrictions on `add_AssemblyResolve`.
- `DatabaseSpecificQueryable<T>` (`Source/LinqToDB/Internal/DataProvider/DatabaseSpecificQueryable.cs`) — abstract `IExpressionQuery<T>` decorator; all provider-specific `SpecificQueryable` subclasses carry additional hint-bearing state by extending this.
- `DatabaseSpecificTable<T>` (`Source/LinqToDB/Internal/DataProvider/DatabaseSpecificTable.cs`) — abstract `ITable<T>` decorator parallel to `DatabaseSpecificQueryable`; delegates all `IQueryProvider`, `ITable`, and `IQueryProviderAsync` members to the wrapped `_table`.
- `DataProviderExtensions` (`Source/LinqToDB/Internal/DataProvider/DataProviderExtensions.cs`) — `[PublicAPI]`-annotated static class providing typed overloads of `SetFieldReaderExpression<TDbDataReader,T>` for registering reader lambda expressions on a provider. Six overloads allow scoping by reader type, CLR type, `DbDataReader.GetFieldType` result, `GetDataTypeName` result, or combinations. All overloads ultimately write to `DataProviderBase.ReaderExpressions[ReaderInfo]` and call `Linq.Tools.ClearAllCaches()`.
- `DataProviderFactoryBase` (`Source/LinqToDB/Internal/DataProvider/DataProviderFactoryBase.cs`) — abstract `IDataProviderFactory` implementation extracting `version` and `assemblyName` `NamedValue` attributes from XML-config data; concrete factories (e.g. `SqlServerDataProviderFactory`) override `GetDataProvider`.
- `DataProviderOptions<T>` (`Source/LinqToDB/Internal/DataProvider/DataProviderOptions.cs`) — abstract record base for per-provider option bags, implementing `IOptionSet`. Carries `BulkCopyType`; subclasses call `CreateID(IdentifierBuilder)` to compose a stable `ConfigurationID`. The `Default` static property writes through to `DataConnection.ResetDefaultOptions()`.
- `DataTools` (`Source/LinqToDB/Internal/DataProvider/DataTools.cs`) — static utility methods for SQL literal construction: `EscapeUnterminatedBracket` (bracket-escape for `LIKE` patterns), `ConvertStringToSql` / `ConvertCharToSql` (quote and escape string/char literals with `\x0` and single-quote handling), `GetCharExpression` lambda for `CHAR` column reads, `CreateFileDatabase` / `DropFileDatabase` (file-based DB lifecycle), `ConvertToIso8601Interval` (ISO-8601 duration formatting for `TimeSpan`), `BuildHexString`.
- `DmlServiceBase` (`Source/LinqToDB/Internal/DataProvider/DmlServiceBase.cs`) — abstract `IDmlService` base. `IsTableNotFoundException` walks the inner-exception chain (including `AggregateException` flattening). `TypeOrMessageContains` and `HResultMatches` static helpers match remote-transport wrapped exceptions by type name or message text, needed because gRPC/HTTP transports stringify the original exception type.
- `IDmlService` (`Source/LinqToDB/Internal/DataProvider/IDmlService.cs`) — single-method contract (`IsTableNotFoundException`) resolved from the data context's service provider by `DropTable` DML to decide whether to suppress "table not found" errors.
- `IExecutionScope` (`Source/LinqToDB/Internal/DataProvider/IExecutionScope.cs`) — marker interface extending `IDisposable` + `IAsyncDisposable`; used as the return type from provider execution-scope entry points to ensure cleanup.
- `InvariantCultureRegion` (`Source/LinqToDB/Internal/DataProvider/InvariantCultureRegion.cs`) — internal `IExecutionScope` that sets `Thread.CurrentThread.CurrentCulture` to `InvariantCulture` on construction and restores it on disposal; chains to a parent `IExecutionScope`. Used by SQL-literal formatting paths that must be locale-independent.
- `IQueryParametersNormalizer` (`Source/LinqToDB/Internal/DataProvider/IQueryParametersNormalizer.cs`) — single-method interface (`Normalize(string?)`) for per-provider parameter name normalization policy.
- `IdentifierKind` (`Source/LinqToDB/Internal/DataProvider/IdentifierKind.cs`) — enum of identifier categories (`Table`, `Field`, `Index`, `ForeignKey`, `PrimaryKey`, `UniqueKey`, `Sequence`, `Trigger`, `StoredProcedure`, `Function`, `View`, `Database`, `Schema`, `Alias`, `Parameter`, `Variable`, `Keyword`, `DataType`, `Other`); consumed by `IIdentifierService.IsFit` and `IdentifiersHelper.TruncateIdentifier`.
- `IdentifierServiceBase` (`Source/LinqToDB/Internal/DataProvider/IdentifierServiceBase.cs`) — abstract `IIdentifierService` base; `CorrectAlias` strips leading underscores and replaces non-alphanumeric chars with spaces then collapses them; abstract `IsFit` must be overridden.
- `IdentifierServiceSimple` (`Source/LinqToDB/Internal/DataProvider/IdentifierServiceSimple.cs`) — sealed concrete `IdentifierServiceBase` with a single `MaxLength` constraint; `IsFit` returns `false` with `sizeDecrement = identifier.Length - MaxLength`.
- `IdentifiersHelper` (`Source/LinqToDB/Internal/DataProvider/IdentifiersHelper.cs`) — `TruncateIdentifier` calls `IsFit` and if needed subtracts `sizeDecrement + 4` from the end; the `+4` is acknowledged as a quick solution (TODO comment).
- `MultipleRowsHelper` / `MultipleRowsHelper<T>` (`Source/LinqToDB/Internal/DataProvider/MultipleRowsHelper.cs`) — builds column lists and parameter sets for multi-row `INSERT` batches. `MultipleRowsHelper<T>` resolves the table name via `BasicBulkCopy.GetTableName`. `BuildColumns` emits literal or parameter SQL per column, wrapping values in `CAST(... AS type)` when required by `castFirstRowLiteralOnUnionAll`. `Execute` / `ExecuteAsync` dispatches the accumulated SQL+parameters and resets the batch state.
- `NoopQueryParametersNormalizer` (`Source/LinqToDB/Internal/DataProvider/NoopQueryParametersNormalizer.cs`) — singleton `IQueryParametersNormalizer` returning the original name unchanged; used by providers with positional parameters or no normalization need.
- `OdbcProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/OdbcProviderAdapter.cs`) — `IDynamicProviderAdapter` singleton wrapping `System.Data.Odbc`. On non-Framework TFMs loads the assembly via `Tools.TryLoadAssembly`. Uses `TypeMapper` to build compiled expression delegates for `OdbcParameter.OdbcType` get/set and `OdbcConnection` construction. Exposes `SetDbType` / `GetDbType` actions and a `ConnectionWrapper` for unwrapping to `OdbcConnection`.
- `OleDbProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/OleDbProviderAdapter.cs`) — `IDynamicProviderAdapter` singleton wrapping `System.Data.OleDb`. Same `TypeMapper` pattern as `OdbcProviderAdapter`; additionally exposes `GetOleDbSchemaTable` delegate (`Func<DbConnection, Guid, object[]?, DataTable>`) for OLE DB schema-table retrieval.
- `ReaderInfo` (`Source/LinqToDB/Internal/DataProvider/ReaderInfo.cs`) — `readonly record struct` key for `DataProviderBase.ReaderExpressions`; fields: `ToType`, `FieldType`, `ProviderFieldType`, `DataTypeName` (normalized to lower-case on `init`), `DataReaderType`. Used by `DataProviderExtensions.SetFieldReaderExpression` overloads.
- `ReservedWords` (`Source/LinqToDB/Internal/DataProvider/ReservedWords.cs`) — static `IsReserved(string)` / `IsReserved(string, string providerName)` backed by provider-specific `HashSet<string>` (OrdinalIgnoreCase). Loads word lists from embedded resources (`ReservedWords.txt`, `ReservedWordsPostgres.txt`, `ReservedWordsOracle.txt`, `ReservedWordsFirebird.txt`); `ConcurrentDictionary<string, HashSet<string>>` keyed by provider name. Used by `AliasesHelper.IsValidAlias`.
- `SimpleServiceProvider` (`Source/LinqToDB/Internal/DataProvider/SimpleServiceProvider.cs`) — lightweight `IServiceProvider` backed by `Dictionary<Type,object>`; `AddService<T>` registers; `GetService` returns or null. Used by `DataProviderBase` to vend provider services without a DI container.
- `SqlProviderHelper` (`Source/LinqToDB/Internal/DataProvider/SqlProviderHelper.cs`) — thin static wrapper around a pooled `SqlQueryValidatorVisitor` (pool size 100); `IsValidQuery` allocates, validates, and returns.
- `SqlTypes` (`Source/LinqToDB/Internal/DataProvider/SqlTypes.cs`) — internal static class of string constants for `System.Data.SqlTypes` method names (`GetSqlDecimal`, `GetSqlGuid`, etc.) shared by SQL Server and SQL CE provider adapters.
- `TableSpecHintExtensionBuilder` (`Source/LinqToDB/Internal/DataProvider/TableSpecHintExtensionBuilder.cs`) — `ISqlTableExtensionBuilder` implementation that renders provider table hints of the form `HINT(alias)` or `HINT(alias, param)` from `SqlQueryExtension.Arguments`; handles optional `hintParameter` / `hintParameters` multi-value variants and `QueryName`-based block naming when `IsNamingQueryBlockSupported`.
- `UniqueParametersNormalizer` (`Source/LinqToDB/Internal/DataProvider/UniqueParametersNormalizer.cs`) — `IQueryParametersNormalizer` that enforces uniqueness (case-insensitive `_usedParameterNames` set), strips invalid characters, limits to 50 characters, and resolves duplicates by appending `_N` suffixes. Overridable `MakeValidName`, `IsValidFirstCharacter`, `IsValidCharacter`, `IsReserved`, `Comparer`, `DefaultName`, `CounterSeparator`, `MaxLength`.
- `WrapParametersVisitor` (`Source/LinqToDB/Internal/DataProvider/WrapParametersVisitor.cs`) — `SqlQueryVisitor` subclass that wraps untyped SQL parameters in `CAST(? AS type)` expressions when required in `SELECT`, `UPDATE SET`, `INSERT VALUES`, `OUTPUT`, `MERGE`, binary expressions, or function parameters. `WrapFlags` bitmask controls which clause contexts trigger wrapping.

Key types in `Translation/` (`Source/LinqToDB/Internal/DataProvider/Translation/`):
- `MemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/MemberTranslatorBase.cs`) — base class for per-provider member translators. Holds a `TranslationRegistration` (lookup table) and `CombinedMemberTranslator`. Implements `IMemberTranslator`.
- `ProviderMemberTranslatorDefault` — abstract translator base providing default `String`, `Math`, `Date`, `Convert`, `Guid`, `Sql-functions`, and `Aggregate` sub-translators via abstract factory methods.
- Specialized bases: `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `GuidMemberTranslatorBase`, `AggregateFunctionsMemberTranslatorBase`, `SqlFunctionsMemberTranslatorBase`.
- `CombinedMemberTranslator` / `CombinedMemberConverter` — fan-out translator/converter that delegates to a list of translators in order.

#### DataProvider / Translation — additional types (batch 2)

- `AggregateFunctionsMemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/AggregateFunctionsMemberTranslatorBase.cs`) — registers `Count`/`LongCount` (with and without predicate), and handles `Min`/`Max`/`Sum`/`Average` via `TranslateMinMaxSumAverage`. Overridable flags `IsCountDistinctSupported`, `IsAggregationDistinctSupported`, `IsFilterSupported` control dialect capability.
- `CombinedMemberConverter` (`Source/LinqToDB/Internal/DataProvider/Translation/CombinedMemberConverter.cs`) — `IMemberConverter` composite iterating a `IMemberConverter[]` list; returns on first `handled = true`.
- `CombinedMemberTranslator` (`Source/LinqToDB/Internal/DataProvider/Translation/CombinedMemberTranslator.cs`) — `IMemberTranslator` composite over `List<IMemberTranslator>`; returns on first non-null result. Has `Add(IMemberTranslator)` for post-construction registration.
- `ConvertMemberTranslatorDefault` (`Source/LinqToDB/Internal/DataProvider/Translation/ConvertMemberTranslatorDefault.cs`) — registers all `System.Convert.ToXxx` overloads for every primitive CLR type plus `Guid` handling; each registration delegates to a typed translate handler that emits provider-specific SQL CAST or function.
- `DateFunctionsTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs`) — registers `DateTime` and `DateTimeOffset` constructors, `AddXxx` / `DatePart` methods, `DateTime.Now` / `UtcNow` / `Sql.GetDate()` / `Sql.CurrentTimestamp` replacements, `DateOnly` (conditional on `SUPPORTS_DATEONLY`), `TimeOnly`, `Sql.MakeDateTime`. Abstract `TranslateDateTimeMember`, `TranslateDateTimeConstructor`, etc. must be overridden per provider.
- `GuidMemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/GuidMemberTranslatorBase.cs`) — registers `Guid.Empty.ToString()` and `Guid?.ToString()`, calls virtual `TranslateGuildToString` (returns `null` by default — providers override to emit `NEWID()`, `LOWER(HEX())`, etc.).
- `IMemberConverter` (`Source/LinqToDB/Internal/DataProvider/Translation/IMemberConverter.cs`) — contract for expression-tree rewriting: `Convert(Expression, out bool handled)`. Distinct from `IMemberTranslator` which targets the SQL translation context.
- `LegacyMemberConverterBase` (`Source/LinqToDB/Internal/DataProvider/Translation/LegacyMemberConverterBase.cs`) — `IMemberConverter` handling the `StringAggregate(...).ToValue()` chained-call pattern by rewriting it into `ConcatStrings` / `StringAggregate` expression forms with `OrderBy` propagation; bridges the legacy fluent API into the translation pipeline.
- `MathMemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/MathMemberTranslatorBase.cs`) — registers `Math.Max`, `Math.Min`, `Math.Abs`, `Math.Round` (and `Sql.Round` / `Sql.RoundToEven`), `Math.Pow` / `Sql.Pow` for all numeric overloads; abstract `TranslateMaxMethod`, `TranslateMinMethod`, etc. are implemented by each provider's translator.
- `ProviderMemberTranslatorDefault` (`Source/LinqToDB/Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs`) — abstract `MemberTranslatorBase` subclass that wires together the full set of domain-specific sub-translators via `CreateXxxTranslator()` factory methods (all returning default base-class implementations unless overridden). Also registers `Sql.NewGuid()` / `Guid.NewGuid()` via virtual `TranslateNewGuidMethod`.
- `SqlExpressionFactoryExtensions` (`Source/LinqToDB/Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs`) — extension methods on `ISqlExpressionFactory`; `Fragment` creates `SqlFragment` nodes; `Expression` / `NotNullExpression` / `NonPureExpression` create `SqlExpression` nodes with varying `SqlFlags` / nullability. Allows translator code to avoid direct `SqlExpression` constructor calls.
- `SqlFunctionsMemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/SqlFunctionsMemberTranslatorBase.cs`) — registers `Sql.NullIf<T,T>` / `Sql.NullIf<T?,T?>` / `Sql.NullIf<object,object>`; default implementation emits a conditional `CASE WHEN v = c THEN NULL ELSE v END` via `factory.Condition`.
- `SqlTypesTranslationDefault` (`Source/LinqToDB/Internal/DataProvider/Translation/SqlTypesTranslationDefault.cs`) — `IMemberTranslator` (not derived from `MemberTranslatorBase`) that registers all `Sql.Types.*` members and maps them to `DbDataType`-typed `SqlExpression` nodes via protected `ConvertXxx` virtuals (e.g. `ConvertBit` → `DataType.Boolean`, `ConvertNVarChar` → `DataType.NVarChar`).
- `StringMemberTranslatorBase` (`Source/LinqToDB/Internal/DataProvider/Translation/StringMemberTranslatorBase.cs`) — registers `Sql.Like`, `Sql.Replace`, `string.Length`, `string.Replace`, `string.PadLeft`, `string.Join`, `Sql.ConcatStrings`, `Sql.ConcatStringsNullable`. Base implementations call `translationContext.ExpressionFactory.LikePredicate`, `Function("REPLACE", ...)`, `Function("LEN", ...)` etc.; providers override for dialect spelling.
- `TranslationContextExtensions` (`Source/LinqToDB/Internal/DataProvider/Translation/TranslationContextExtensions.cs`) — extension helpers on `ITranslationContext`: `TryEvaluate<T>`, `GetDbDataType(ISqlExpression)`, `CreatePlaceholder(ISqlExpression, Expression)` (calls `translationContext.CurrentSelectQuery` overload), `TranslateToSqlExpression` (two overloads — one returning bool + nullable, one returning bool + `SqlErrorExpression`).
- `TranslationRegistration` (`Source/LinqToDB/Internal/DataProvider/Translation/TranslationRegistration.cs`) — sealed lookup table `Dictionary<MemberInfoWithType, TranslateFunc>` keyed by `MethodInfo` / `PropertyInfo` / `ConstructorInfo` (with optional owner-type scope). `RegisterMethodInternal` normalizes generic methods to their `GetGenericMethodDefinition` unless `isGenericTypeMatch`. Also manages `MemberReplacement` pairs for AST-level substitutions.
- `TranslationRegistrationExtensions` (`Source/LinqToDB/Internal/DataProvider/Translation/TranslationRegistrationExtensions.cs`) — extension methods over `TranslationRegistration` providing the public `RegisterMethod<…>` / `RegisterMember<…>` / `RegisterReplacement<…>` / `RegisterConstructor<…>` typed overloads with up to six generic parameters; all delegate to `RegisterMethodInternal` / `RegisterMemberInternal` / `RegisterMemberReplacement` / `RegisterConstructorInternal`.

Consumers: every `PROV-*` area's data provider and SQL builder, plus [EXPR-TRANS](../EXPR-TRANS/INDEX.md) for member-translation dispatch.

### `Internal/Expressions/**` — Internal expression-tree types and visitors

Custom `Expression` subclasses used inside the LINQ→SQL translation pipeline, plus high-performance visitor infrastructure. Companions to the public `Expressions/` surface (owned by [EXPR](../EXPR/INDEX.md)).

Key types:
- `SqlPlaceholderExpression` (`Source/LinqToDB/Internal/Expressions/SqlPlaceholderExpression.cs`) — wraps an `ISqlExpression sql` together with the LINQ `path` expression and the owning `SelectQuery`. The translation pipeline replaces LINQ sub-expressions with `SqlPlaceholderExpression` nodes as SQL AST is built; the final compilation phase unwraps them into reader calls. Carries `Index` (ordinal in the SELECT list), `Alias`, and optional `TrackingPath`.
- `SqlGenericConstructorExpression` (`Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs`) — represents a deferred object construction (new `T(…)` or member-init) in a form the pipeline can rewrite. Carries a `ReadOnlyCollection<Parameter>` and `ReadOnlyCollection<Assignment>`. Used for projection/materialization before the reader-access lambdas are emitted.
- `ContextRefExpression` — references the current query context (`IBuildContext`) during expression translation.
- `ConvertFromDataReaderExpression` — represents a `DataReader.GetXxx(i)` call being composed into the materialization lambda.
- `MarkerExpression` / `TagExpression` — pipeline bookmarks for tracking sub-expression identity through visitor passes.
- `SqlQueryRootExpression` — root of a sub-tree representing a complete SQL query embedded as a LINQ expression.
- `ExpressionVisitorBase` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitorBase.cs`) — extends `System.Linq.Expressions.ExpressionVisitor` with a `StackGuard` (depth limit) and virtual dispatch methods for all custom `SqlXxx` expression nodes. All pipeline visitors in `EXPR-TRANS` derive from this.
- `ExpressionVisitors/` subtree — high-performance generic visitors with context parameters:
  - `TransformVisitor<TContext>` / `TransformInfoVisitor<TContext>` — transforming visitors avoiding allocations via context threading.
  - `FindVisitor<TContext>` / `VisitActionVisitor<TContext>` / `VisitFuncVisitor<TContext>` — read-only searching visitors.
  - `WritableContext<TWriteable,TStatic>` — context struct splitting mutable from static state.
- `Types/TypeMapper` (`Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs`) — runtime type-wrapper system for late-loaded provider types. Registers `TypeWrapper` subclasses against dynamically-loaded `originalType`s and builds expression-based wrappers. Used by `DynamicDataProviderBase<T>` adapters to project provider-specific types through a compiled expression layer without requiring direct assembly references.

#### Expressions / nodes (batch 3)

The following custom `Expression` subclasses serve as pipeline intermediates. All override `NodeType` (to `ExpressionType.Extension` or a custom value), `Type`, and optionally `CanReduce`/`Reduce`. Each routes through `ExpressionVisitorBase` dispatch via `Accept`.

**Type-coercion and reading nodes:**
- `ChangeTypeExpression` (`Source/LinqToDB/Internal/Expressions/ChangeTypeExpression.cs`) — reinterprets an expression's CLR type without runtime conversion; `NodeType` is the custom constant `ChangeTypeType = (ExpressionType)1000`. Used where a type override is needed in the LINQ tree without emitting a `Convert` call.
- `SqlAdjustTypeExpression` (`Source/LinqToDB/Internal/Expressions/SqlAdjustTypeExpression.cs`) — wraps an expression with a target type and `MappingSchema`; `Reduce()` calls `ExpressionBuilder.AdjustType`. Used when the LINQ→SQL pipeline needs to coerce a projection expression to match a column's declared CLR type.
- `ConvertFromDataReaderExpression` (`Source/LinqToDB/Internal/Expressions/ConvertFromDataReaderExpression.cs`) — represents a typed `DataReader.GetXxx(idx)` call pending compilation into the materialization lambda. `Reduce()` dispatches to `ColumnReader.GetValue` / `GetValueSequential`; has three `Reduce(…)` overloads for slow/fast/sequential-access modes. Inner sealed class `ColumnReader` caches compiled `Func<DbDataReader, object>` delegates per provider field type in `ConcurrentDictionary` to avoid repeated compilation.
- `DefaultValueExpression` (`Source/LinqToDB/Internal/Expressions/DefaultValueExpression.cs`) — reduces to `Constant(MappingSchema.GetDefaultValue(Type))` or `DefaultValue.GetValue(Type)` when no schema is present. Carries `IsNull` flag for null-default distinction.

**Query-structure nodes:**
- `SqlQueryRootExpression` (`Source/LinqToDB/Internal/Expressions/SqlQueryRootExpression.cs`) — identifies a data-context root in the LINQ tree; carries `MappingSchema` and `ContextType`. Equality is based on `IConfigurationID.ConfigurationID` and `ContextType`, not reference identity.
- `ContextRefExpression` (`Source/LinqToDB/Internal/Expressions/ContextRefExpression.cs`) — references an `IBuildContext` inline in the LINQ tree during translation; carries `ElementType`, `BuildContext`, and optional `Alias`. `WithType`, `WithContext`, `WithAlias` return new instances (immutable update pattern).
- `SqlEagerLoadExpression` (`Source/LinqToDB/Internal/Expressions/SqlEagerLoadExpression.cs`) — marks a sequence sub-expression for eager loading; holds `SequenceExpression` and optional `Predicate`; `AppendPredicate` merges additional `AndAlso` conditions.
- `SqlDefaultIfEmptyExpression` (`Source/LinqToDB/Internal/Expressions/SqlDefaultIfEmptyExpression.cs`) — wraps an expression to be substituted with a default if empty; holds `InnerExpression` and `NotNullExpressions` collection used by null-check generation.
- `SqlPathExpression` (`Source/LinqToDB/Internal/Expressions/SqlPathExpression.cs`) — represents an ordered path of `Expression[]` steps for column-tracking through nested projections; `Path` array is mutable (exposed setter) for in-place updates during certain pipeline phases.

**Access and validation nodes:**
- `SqlGenericParamAccessExpression` (`Source/LinqToDB/Internal/Expressions/SqlGenericParamAccessExpression.cs`) — indexes into a `SqlGenericConstructorExpression` by constructor `ParameterInfo`; used to project individual constructor parameters during materialization planning.
- `SqlReaderIsNullExpression` (`Source/LinqToDB/Internal/Expressions/SqlReaderIsNullExpression.cs`) — boolean expression testing `IsDBNull` on a `SqlPlaceholderExpression`; carries `IsNot` for both `IS NULL` and `IS NOT NULL` forms.
- `SqlValidateExpression` (`Source/LinqToDB/Internal/Expressions/SqlValidateExpression.cs`) — wraps an expression with a `Func<Expression, Expression> Validator`; `Reduce()` calls the validator on the inner expression. Used to defer validation of sub-expressions until the compilation phase.
- `SqlErrorExpression` (`Source/LinqToDB/Internal/Expressions/SqlErrorExpression.cs`) — carries a translation-failure payload (`Expression?`, `Message`, `IsCritical`); `Reduce()` throws `LinqToDBException`. Static helpers `EnsureError`, `CreateException`, `ThrowError`, and `PrepareExpressionString` (using `ExpressionPrinter`) format human-readable error messages.

**Placeholder and marker nodes:**
- `ConstantPlaceholderExpression` (`Source/LinqToDB/Internal/Expressions/ConstantPlaceholderExpression.cs`) — represents a constant whose value will be filled in later; `Reduce()` returns `Default(ConstantType)`. Used in query-caching paths where a `[SqlQueryDependent]`-tagged argument must be omitted from the cache-key comparison.
- `MarkerExpression` (`Source/LinqToDB/Internal/Expressions/MarkerExpression.cs`) — wraps an inner expression with a `MarkerType` enum tag (`PreferClientSide`, `AggregationFallback`). `Reduce()` returns the inner expression. Static `PreferClientSide(inner)` factory skips wrapping if the inner is already a `SqlPlaceholderExpression`.
- `TagExpression` (`Source/LinqToDB/Internal/Expressions/TagExpression.cs`) — attaches an arbitrary `object Tag` to an expression; `Reduce()` returns the inner expression. Used to carry metadata through pipeline passes without changing the expression's semantics.

**Other node types:**
- `TransformInfo` (`Source/LinqToDB/Internal/Expressions/TransformInfo.cs`) — `readonly struct` returned by `TransformInfoVisitor` callbacks; carries `Expression`, `Stop` (halt traversal), and `Continue` (re-invoke on same node) flags.

#### Expressions / utility passes (batch 3)

- `ExpressionConstants` (`Source/LinqToDB/Internal/Expressions/ExpressionConstants.cs`) — single static field `DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx")` shared across the pipeline to avoid duplicate parameter allocations.
- `ExpressionInstances` (`Source/LinqToDB/Internal/Expressions/ExpressionInstances.cs`) — pre-allocated `ConstantExpression` singletons for `true`, `false`, `null`, `0`–`10` integers, `string.Empty`, and common boxing-avoidance constants. `Int32(int)` / `Int32Array(int)` return cached instances for the 0–10 range.
- `ExpressionEqualityComparer` (`Source/LinqToDB/Internal/Expressions/ExpressionEqualityComparer.cs`) — singleton `IEqualityComparer<Expression>` (adapted from EF Core) with full structural equality across all standard and custom node types. Inner `ExpressionComparer` uses a `ScopedDictionary<ParameterExpression, ParameterExpression>` to handle alpha-equivalent lambdas. `EnumerableQuery` values compare as non-equal; `IEnumerable` constants compare element-by-element. Used as the `IEqualityComparer` argument wherever expression-keyed dictionaries appear in the pipeline.
- `ExpressionEvaluator` (`Source/LinqToDB/Internal/Expressions/ExpressionEvaluator.cs`) — static `EvaluateExpression(Expression?)` tries a fast path (`IsSimpleEvaluatable`) for constant/member-access/call expressions, then falls back to `Lambda.CompileExpression().DynamicInvokeExt()`. Typed `EvaluateExpression<T>()` casts the result.
- `ExpressionHelper` (`Source/LinqToDB/Internal/Expressions/ExpressionHelper.cs`) — case-sensitive alternatives to `Expression.Field`, `Expression.Property`, `Expression.PropertyOrField` that search both public and non-public members and throw descriptive errors when not found; also `GetPropertyOrFieldMemberInfo` variant returning raw `MemberInfo`.
- `ExpressionHelpers` (`Source/LinqToDB/Internal/Expressions/ExpressionHelpers.cs`) — `EnsureObject(expr)` converts to `object` if not already; `CollectMembers(expr)` yields `NewExpression` arguments or the expression itself; `MakeCall<…>` typed overloads inline a lambda body by substituting parameters with actual argument expressions (no `Invoke` node created).
- `ExpressionGenerator` (`Source/LinqToDB/Internal/Expressions/ExpressionGenerator.cs`) — builder for `BlockExpression` with variable declarations; exposes `DeclareVariable`, `Assign`, `AssignToVariable`, `IfThen`, `IfThenElse`, `Condition`, `TryCatch`, `Throw`, `MemberAccess`, and `MapExpression`/`MapAction` overloads delegating to `TypeMapper`. `Build()` returns a single-expression result if there are no variables and exactly one expression.
- `ExpressionPrinter` (`Source/LinqToDB/Internal/Expressions/ExpressionPrinter.cs`) — `ExpressionVisitorBase` subclass (adapted from EF Core) that produces human-readable string representations of expression trees using `SqlTextWriter`. Used by `SqlErrorExpression.PrepareExpressionString` and debugging utilities.
- `IPrintableExpression` (`Source/LinqToDB/Internal/Expressions/IPrintableExpression.cs`) — interface `Print(ExpressionPrinter)` implemented by custom expression nodes that want custom `ExpressionPrinter` rendering.
- `InternalExtensions` (`Source/LinqToDB/Internal/Expressions/InternalExtensions.cs`) — large static extension class providing C#14 extension blocks on `Expression`, `Expression?`, and `MethodCallExpression`. Key members: `Unwrap`/`UnwrapLambda`/`UnwrapUnary`/`UnwrapConvert`/`UnwrapAdjustType` (strip conversion/quote/adjust wrappers), `GetExpressionAccessors` (builds a path→accessor dictionary using `PathVisitor`), `IsNullValue`, `IsQueryable`/`IsOrderByMethodName`/`IsSameGenericMethod` on `MethodCallExpression`, `ApplyLambdaToExpression` (substitutes lambda parameter avoiding double-evaluation), `OptimizeExpression` (constant-folding and boolean short-circuit via pooled `ExpressionOptimizerVisitor`).
- `SkipIfConstantAttribute` (`Source/LinqToDB/Internal/Expressions/SkipIfConstantAttribute.cs`) — `[AttributeUsage(Parameter)]`; signals `EqualsToVisitor` to skip comparison of a `[SqlQueryDependent]`-tagged method argument when it evaluates to a `ConstantPlaceholderExpression`.
- `SqlQueryDependentAttributeHelper` (`Source/LinqToDB/Internal/Expressions/SqlQueryDependentAttributeHelper.cs`) — `ConcurrentDictionary`-backed cache mapping `MethodInfo` to its `SqlQueryDependentAttribute?[]` parameter array; used by `EqualsToVisitor.EqualsToX(MethodCallExpression,…)` to dispatch per-argument equality logic.

#### Expressions / visitors framework (batch 3)

All visitors in `ExpressionVisitors/` derive from `ExpressionVisitorBase` (a `StackGuard`-equipped extension of `System.Linq.Expressions.ExpressionVisitor`). The hierarchy splits into two branches:

**`FindVisitorBase` branch (read-only search):**
- `FindVisitorBase` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitorBase.cs`) — abstract base overriding visit methods for `SqlDefaultIfEmptyExpression` (recurse into inner), `SqlReaderIsNullExpression` (skip), `SqlErrorExpression` (recurse into expression), `SqlPathExpression` (recurse all path items).
- `FindVisitor` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitor.cs`) — pooled (capacity 100) non-generic find; `Find(node, Func<Expression,bool>)` returns the first matching node. Short-circuits once `_found` is set.
- `FindVisitor<TContext>` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitor{TContext}.cs`) — pooled context-threaded variant; `Find(node, context, Func<TContext,Expression,bool>)`. Avoids closure allocations.

**`LegacyVisitorBase` → `TransformVisitorsBase` branch (transformation):**
- `LegacyVisitorBase` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/LegacyVisitorBase.cs`) — abstract base handling legacy node forms: `SqlValidateExpression` (visit inner + update), `SqlEagerLoadExpression` (no-op), `SqlErrorExpression` (reduce and revisit), `DefaultValueExpression` (reduce and revisit), `SqlGenericParamAccessExpression` (visit constructor), `ConvertFromDataReaderExpression` (reduce and revisit), `ConstantPlaceholderExpression` (reduce and revisit), `MarkerExpression` (reduce and revisit), `SqlAdjustTypeExpression` (visit inner + update).
- `TransformVisitorsBase` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitorsBase.cs`) — refines `LegacyVisitorBase` for transforming visitors: overrides `TagExpression` (no-op), `SqlErrorExpression` (no-op), `DefaultValueExpression` (no-op), `ConvertFromDataReaderExpression` (no-op), `ConstantPlaceholderExpression` (no-op), `MarkerExpression` (visit inner + update).
- `TransformVisitor` / `TransformVisitor<TContext>` — pooled transforming visitors; `Transform(expr, func)` replaces each node if `func` returns a different expression, then recurses into the result. Context-threaded `<TContext>` variant avoids closure allocations.
- `TransformInfoVisitor` / `TransformInfoVisitor<TContext>` — pooled transforming visitors using `TransformInfo` return value; support `Stop` (halt) and `Continue` (re-invoke on same node after mutation). All inherit pool size 100.
- `VisitActionVisitor` / `VisitActionVisitor<TContext>` — pooled visitors invoking a post-order action on every visited node. Context-threaded variant.
- `VisitFuncVisitor` / `VisitFuncVisitor<TContext>` — pooled visitors invoking a function that returns `bool`; traversal continues only when the function returns `true`, enabling early short-circuit.

**Utility visitors:**
- `EqualsToVisitor` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/EqualsToVisitor.cs`) — static class (not an `ExpressionVisitor` subclass) providing structural `EqualsTo(expr1, expr2, EqualsToInfo)` comparison. Handles `ConstantPlaceholderExpression`↔`ConstantExpression` equivalence (cache-hit path), dispatches `[SqlQueryDependent]` attribute logic via `SqlQueryDependentAttributeHelper`, and compares `IQueryableContainer` arguments by inner expression tree recursion.
- `PathVisitor<TContext>` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/PathVisitor.cs`) — non-pooled non-`ExpressionVisitor`-derived visitor that walks an expression tree while tracking the accessor path (as a parallel `Expression`). Fires `Action<TContext, Expression, Expression>` at each node with both the expression and its path. Handles `SqlGenericConstructorExpression` specially; for other `Extension` nodes falls back to `Reduce()` if `CanReduce`.
- `WritableContext` / `WritableContext<TWriteable,TStatic>` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/WritableContext*.cs`) — context holder splitting a mutable `WriteableValue` from an immutable `StaticValue`; passed as `TContext` to context-threaded visitors when the callback needs to write a result without capturing the variable in a closure.

#### Expressions / Types (TypeWrapper) (batch 3)

The `Types/` sub-folder provides the runtime reflection-binding framework used by all dynamic provider adapters.

- `TypeWrapper` (`Source/LinqToDB/Internal/Expressions/Types/TypeWrapper.cs`) — abstract base class for wrapper types. Holds `instance_` (the wrapped provider-specific object) and `CompiledWrappers` (compiled delegate array built by `TypeMapper`). The dual-constructor pattern (protected no-arg for wrapper class definition, protected `(object, Delegate[])` for actual construction) reflects that `TypeMapper` uses expression-based construction and never calls the wrapper constructors directly.
- `WrapperAttribute` (`Source/LinqToDB/Internal/Expressions/Types/WrapperAttribute.cs`) — `[Wrapper]` or `[Wrapper("TypeName")]` decorates a `TypeWrapper` subclass. When a `TypeName` is specified it overrides the CLR type name used for provider-side lookup; required when the provider type name differs from the wrapper class name.
- `TypeWrapperNameAttribute` (`Source/LinqToDB/Internal/Expressions/Types/TypeWrapperNameAttribute.cs`) — `[TypeWrapperName("name")]` on a method provides an alternate provider-side method name for the `TypeMapper` mapping lookup.
- `WrappedBindingFlagsAttribute` (`Source/LinqToDB/Internal/Expressions/Types/WrappedBindingFlagsAttribute.cs`) — `[WrappedBindingFlags(BindingFlags)]` on a constructor controls the `BindingFlags` used by `TypeMapper` when locating the matching provider constructor.
- `ICustomMapper` (`Source/LinqToDB/Internal/Expressions/Types/ICustomMapper.cs`) — two-method interface: `CanMap(Expression)` and `Map(TypeMapper, Expression)`. Registered with `TypeMapper` for expression-level type coercion not handled by wrapper class reflection.
- `CustomMapperAttribute` (`Source/LinqToDB/Internal/Expressions/Types/CustomMapperAttribute.cs`) — `[CustomMapper(typeof(T))]` on a return value annotation; `T` must implement `ICustomMapper`. Directs `TypeMapper` to apply `T.Map(…)` when building the wrapper expression for that return value.
- `ValueTaskToTaskMapper` (`Source/LinqToDB/Internal/Expressions/Types/ValueTaskToTaskMapper.cs`) — `ICustomMapper` converting non-generic `ValueTask` and `ValueTask<T>` to `Task` via `.AsTask()`. Registered by provider adapters that need to unify async return types.
- `GenericTaskToTaskMapper` (`Source/LinqToDB/Internal/Expressions/Types/GenericTaskToTaskMapper.cs`) — `ICustomMapper` converting `Task<T>` or `ValueTask<T>` to `Task`; for `Task<T>` uses a direct `Convert` expression, for `ValueTask<T>` calls `.AsTask()`.
- `GenericValueTaskMapper<ToType>` (`Source/LinqToDB/Internal/Expressions/Types/GenericValueTaskMapper.cs`) — `ICustomMapper` converting `ValueTask<TFrom>` to `ValueTask<ToType>` where `ToType : TypeWrapper`; uses an async bridge method `Convert<TFrom,TTo>` that awaits and re-wraps through `TypeMapper.Wrap<ToType>`.

#### Expressions / WindowFunctionHelpers (batch 3)

- `WindowFunctionHelpers` (`Source/LinqToDB/Internal/Expressions/WindowFunctionHelpers.cs`) — static factory for window-function expression trees. `BuildWindowDefinition(partitionBy, orderBy)` constructs a `WindowFunctionBuilder.DefineWindow(…)` call expression with inline `PartitionBy`/`OrderBy`/`ThenBy`/`ThenByDesc` chains. `BuildRowNumber(partitionBy, orderBy)` wraps the window definition with `Sql.Window.RowNumber(…)`. `ExtractOrderByPart(query)` walks a `Queryable.OrderBy`/`ThenBy` chain and returns `(LambdaExpression, bool isDescending)[]` plus the unordered base. `ApplyOrderBy` re-applies an extracted order to any `IQueryable<T>` or `IEnumerable<T>`. `BuildAggregateExecuteExpression` builds `LinqExtensions.AggregateExecute<TSource,TResult>` call nodes from either a typed source or a raw `MethodCallExpression`.

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
| `MultipleRowsHelper` | `Internal/DataProvider/MultipleRowsHelper.cs` | Multi-row INSERT batch column/parameter builder |
| `DataTools` | `Internal/DataProvider/DataTools.cs` | SQL literal construction utilities (string/char/interval/hex) |
| `DmlServiceBase` / `IDmlService` | `Internal/DataProvider/DmlServiceBase.cs` / `IDmlService.cs` | DROP TABLE "not-found" exception detection |
| `InvariantCultureRegion` | `Internal/DataProvider/InvariantCultureRegion.cs` | Thread-culture RAII scope for SQL formatting |
| `WrapParametersVisitor` | `Internal/DataProvider/WrapParametersVisitor.cs` | Parameter CAST-wrapping SQL visitor |
| `OdbcProviderAdapter` / `OleDbProviderAdapter` | `Internal/DataProvider/OdbcProviderAdapter.cs` / `OleDbProviderAdapter.cs` | TypeMapper-based adapters for ODBC / OLE DB |
| `ReservedWords` | `Internal/DataProvider/ReservedWords.cs` | Provider-keyed reserved-word set from embedded resources |
| `UniqueParametersNormalizer` | `Internal/DataProvider/UniqueParametersNormalizer.cs` | Unique, valid parameter name policy |
| `DataProviderOptions<T>` | `Internal/DataProvider/DataProviderOptions.cs` | Abstract per-provider `IOptionSet` record |
| `DataProviderExtensions` | `Internal/DataProvider/DataProviderExtensions.cs` | Typed `SetFieldReaderExpression` overloads |
| `ReaderInfo` | `Internal/DataProvider/ReaderInfo.cs` | Key struct for `DataProviderBase.ReaderExpressions` |
| `TableSpecHintExtensionBuilder` | `Internal/DataProvider/TableSpecHintExtensionBuilder.cs` | SQL table hint rendering for `SqlQueryExtension` |
| `TranslationRegistration` | `Internal/DataProvider/Translation/TranslationRegistration.cs` | Member-to-translator lookup table |
| `ProviderMemberTranslatorDefault` | `Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs` | Full default translator composition root |
| `MemberTranslatorBase` | `Internal/DataProvider/Translation/MemberTranslatorBase.cs` | Base for per-provider member translators |
| `SqlExpressionFactoryExtensions` | `Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs` | Extension helpers for building `SqlExpression`/`SqlFragment` nodes |
| `TranslationContextExtensions` | `Internal/DataProvider/Translation/TranslationContextExtensions.cs` | `ITranslationContext` helper extensions |
| `OptionsContainer<T>` | `Internal/Options/OptionsContainer.cs` | Immutable options composition root |
| `IOptionSet` | `Internal/Options/IOptionSet.cs` | Options group contract |
| `IConfigurationID` | `Internal/Common/IConfigurationID.cs` | Cache-key identity contract |
| `IdentifierBuilder` | `Internal/Common/IdentifierBuilder.cs` | Stable integer ID compositor |
| `MemoryCache<TKey,TEntry>` | `Internal/Cache/MemoryCache.cs` | In-process LRU cache |
| `IAsyncDbConnection` | `Internal/Async/IAsyncDbConnection.cs` | Async connection wrapper contract |
| `AsyncFactory` | `Internal/Async/AsyncFactory.cs` | Per-type async-wrapper factory registry |
| `ExpressionVisitorBase` | `Internal/Expressions/ExpressionVisitorBase.cs` | Visitor base with stack-guard and `SqlXxx` dispatch |
| `SqlPlaceholderExpression` | `Internal/Expressions/SqlPlaceholderExpression.cs` | LINQ→SQL mapping node |
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
| `InternalExtensions` | `Internal/Expressions/InternalExtensions.cs` | Expression unwrapping, optimization, and LINQ-method detection extensions |
| `ExpressionGenerator` | `Internal/Expressions/ExpressionGenerator.cs` | `BlockExpression` builder with `TypeMapper` integration |
| `TransformVisitor` / `TransformVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/TransformVisitor*.cs` | Pooled expression-tree transformer |
| `TransformInfoVisitor` / `TransformInfoVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/TransformInfoVisitor*.cs` | Pooled transformer with stop/continue control |
| `FindVisitor` / `FindVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/FindVisitor*.cs` | Pooled first-match search visitors |
| `EqualsToVisitor` | `Internal/Expressions/ExpressionVisitors/EqualsToVisitor.cs` | Query-cache structural expression comparison |
| `PathVisitor<TContext>` | `Internal/Expressions/ExpressionVisitors/PathVisitor.cs` | Expression tree walker with accessor-path tracking |
| `WritableContext<TWriteable,TStatic>` | `Internal/Expressions/ExpressionVisitors/WritableContext*.cs` | Mutable+static context split for closure-free visitor callbacks |
| `TypeWrapper` | `Internal/Expressions/Types/TypeWrapper.cs` | Abstract base for provider-type wrappers |
| `TypeMapper` | `Internal/Expressions/Types/TypeMapper.cs` | Expression-based runtime type wrapper |
| `ICustomMapper` | `Internal/Expressions/Types/ICustomMapper.cs` | Extension point for custom expression-level type coercion |
| `WindowFunctionHelpers` | `Internal/Expressions/WindowFunctionHelpers.cs` | Window function expression tree factory |
| `SchemaProviderBase` | `Internal/SchemaProvider/SchemaProviderBase.cs` | Abstract schema reader base |
| `MappingSchemaInfo` | `Internal/Mapping/MappingSchemaInfo.cs` | Per-configuration mapping state |
| `LockedMappingSchema` | `Internal/Mapping/LockedMappingSchema.cs` | Provider schemas with stable IDs |
| `IInfrastructure<T>` | `Internal/Infrastructure/IInfrastructure{T}.cs` | Hidden property marker interface |
| `AnnotatableBase` | `Internal/Infrastructure/AnnotatableBase.cs` | Concrete annotation store for scaffolding/EFCore |
| `Methods` | `Internal/Reflection/Methods.cs` | Pre-cached reflection constants |
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

**Tier-1 files read in full (22 / 22):**

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

**Tier-2 total (in-scope, excluding owned sub-trees):**

In-scope `*.cs` files: Async(10) + Cache(18) + Common(26) + Conversion(5) + DataProvider root(36) + DataProvider/Translation(18) + Expressions(62) + Extensions(11) + Infrastructure(12) + Interceptors(14, cross-listed) + Logging(1) + Mapping(8) + Options(5) + Reflection(2) + Remote(4) + SchemaProvider(7) + Internal root(1) = **240** `.cs` files in scope. PublicAPI: 12 `.txt` files = 12. Total denominator: **252**.

Tier-2 visited after batch 1: 69. Tier-2 visited after batch 2: 125. Tier-2 visited this run (batch 3, 57 files): +57. Total after batch 3: **182**.

---

## Inbound / outbound dependencies

**Sub-trees owned by other areas (read-only references from this document):**
- [SQL-AST](../SQL-AST/INDEX.md) — `Internal/SqlQuery/**`: ISqlExpression, SelectQuery, SqlStatement consumed by `SqlPlaceholderExpression`, `LinqServiceSerializer`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `Internal/SqlProvider/**`: `ISqlBuilder`, `ISqlOptimizer`, `BasicSqlBuilder` extend `DataProviderBase` factories.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — `Internal/Linq/Builder/**`: consumes `ExpressionVisitorBase`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `SqlQueryRootExpression`.
- [LINQ](../LINQ/INDEX.md) — `Internal/Linq/**` (excl. Builder): `Query.CacheCleaners` referenced by `IdentifierBuilder` (Source/LinqToDB/Internal/Common/IdentifierBuilder.cs:42); `IQueryRunner` / `QueryRunner` use `IAsyncDbConnection`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) — `Interceptors/I*Interceptor.cs`: public contracts; `Internal/Interceptors/**` is the aggregated dispatch layer cross-listed here.
- [MAPPING](../MAPPING/INDEX.md) — `Mapping/MappingSchema`: depends on `MappingSchemaInfo`, `ConvertInfo`, `LockedMappingSchema`.
- [DATA](../DATA/INDEX.md) — `Data/DataConnection`: uses `BasicBulkCopy`, `IAsyncDbConnection`, `OptionsContainer`, logging extensions.

**Outbound dependencies from this area:**
- `LinqToDB.DataProvider.IDataProvider` (public interface implemented by `DataProviderBase`).
- `LinqToDB.SchemaProvider.ISchemaProvider` (public interface implemented by `SchemaProviderBase`).
- `LinqToDB.DataOptions` (extends `OptionsContainer<DataOptions>`).

---

## See also

- [architecture/overview.md](../../architecture/overview.md) — pipeline context for how these types fit in the query execution path.
- [conventions/public-api-discipline.md](../../conventions/public-api-discipline.md) — rules on `LinqToDB.Internal.*` namespace placement and `PublicAPI.txt` maintenance.
- [LINQ/INDEX.md](../LINQ/INDEX.md) — query cache; uses `IdentifierBuilder` IDs for cache-key composition.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) — mapping schema; uses `MappingSchemaInfo`, `ConvertInfo`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 22 / 22 ✓
  - Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs
  - Source/LinqToDB/Internal/DataProvider/DynamicDataProviderBase.cs
  - Source/LinqToDB/Internal/DataProvider/IDynamicProviderAdapter.cs
  - Source/LinqToDB/Internal/DataProvider/ProviderDetectorBase.cs
  - Source/LinqToDB/Internal/DataProvider/BasicBulkCopy.cs
  - Source/LinqToDB/Internal/DataProvider/BulkCopyReader.cs
  - Source/LinqToDB/Internal/DataProvider/IIdentifierService.cs
  - Source/LinqToDB/Internal/DataProvider/Translation/MemberTranslatorBase.cs
  - Source/LinqToDB/Internal/Options/OptionsContainer.cs
  - Source/LinqToDB/Internal/Options/IOptionSet.cs
  - Source/LinqToDB/Internal/Options/IOptionsContainer.cs
  - Source/LinqToDB/Internal/Options/IApplicable.cs
  - Source/LinqToDB/Internal/Common/IConfigurationID.cs
  - Source/LinqToDB/Internal/Common/IdentifierBuilder.cs
  - Source/LinqToDB/Internal/Common/ObjectPool.cs
  - Source/LinqToDB/Internal/Cache/IMemoryCache.cs
  - Source/LinqToDB/Internal/Cache/MemoryCache.cs
  - Source/LinqToDB/Internal/Async/IAsyncDbConnection.cs
  - Source/LinqToDB/Internal/Async/AsyncFactory.cs
  - Source/LinqToDB/Internal/Expressions/ExpressionVisitorBase.cs
  - Source/LinqToDB/Internal/Expressions/SqlPlaceholderExpression.cs
  - Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs
  - Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs
  - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitorBase.cs
  - Source/LinqToDB/Internal/Mapping/MappingSchemaInfo.cs
  - Source/LinqToDB/Internal/Mapping/LockedMappingSchema.cs
  - Source/LinqToDB/Internal/SchemaProvider/SchemaProviderBase.cs
  - Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs
  - Source/LinqToDB/Internal/Interceptors/IInterceptable.cs
  - Source/LinqToDB/Internal/Infrastructure/IInfrastructure{T}.cs
  - Source/LinqToDB/Internal/Infrastructure/Annotatable.cs
  - Source/LinqToDB/Internal/Infrastructure/IAnnotation.cs
  - Source/LinqToDB/Internal/Reflection/Methods.cs
  - Source/LinqToDB/Internal/Remote/LinqServiceSerializer.cs
  - Source/LinqToDB/Internal/Logging/LoggingExtensions.cs
  - Source/LinqToDB/Internal/Conversion/ConvertInfo.cs
  - Source/LinqToDB/Internal/Common/Tools.cs
  - Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt
  - Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt
  - Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Shipped.txt
  - Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Unshipped.txt
- Tier 2 (visited / total): 182 / 199 (91%) — 1 batch remains
  - visited (prior run): Source/LinqToDB/Internal/Common/Tools.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/Conversion/ConvertInfo.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/Mapping/MappingSchemaInfo.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/Infrastructure/Annotatable.cs
  - visited (prior run): Source/LinqToDB/Internal/Infrastructure/IAnnotation.cs
  - visited (prior run): Source/LinqToDB/Internal/Interceptors/AggregatedInterceptor.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/Interceptors/IInterceptable.cs
  - visited (prior run): Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitorBase.cs
  - visited (prior run): Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/DataProvider/Translation/MemberTranslatorBase.cs (partial)
  - visited (prior run): Source/LinqToDB/Internal/PublicAPI/net10.0/PublicAPI.Shipped.txt (partial)
  - visited (prior run): Source/LinqToDB/Internal/PublicAPI/net10.0/PublicAPI.Unshipped.txt
  - skipped (prior run): see DEFERRED-COVERAGE (budget — internal subtree, sampled)
  - Read (this run, batch 1):
    - Source/LinqToDB/Internal/Async/AsyncDbConnection.cs
    - Source/LinqToDB/Internal/Async/AsyncDbTransaction.cs
    - Source/LinqToDB/Internal/Async/AsyncEnumeratorAsyncWrapper.cs
    - Source/LinqToDB/Internal/Async/IAsyncDbTransaction.cs
    - Source/LinqToDB/Internal/Async/IQueryProviderAsync.cs
    - Source/LinqToDB/Internal/Async/ReflectedAsyncDbConnection.cs
    - Source/LinqToDB/Internal/Async/ReflectedAsyncDbTransaction.cs
    - Source/LinqToDB/Internal/Async/SafeAwaiter.cs
    - Source/LinqToDB/Internal/Cache/CacheEntry.cs
    - Source/LinqToDB/Internal/Cache/CacheEntryExtensions.cs
    - Source/LinqToDB/Internal/Cache/CacheEntryHelper.cs
    - Source/LinqToDB/Internal/Cache/CacheEntryStack.cs
    - Source/LinqToDB/Internal/Cache/CacheExtensions.cs
    - Source/LinqToDB/Internal/Cache/CacheItemPriority.cs
    - Source/LinqToDB/Internal/Cache/EvictionReason.cs
    - Source/LinqToDB/Internal/Cache/ICacheEntry.cs
    - Source/LinqToDB/Internal/Cache/IChangeToken.cs
    - Source/LinqToDB/Internal/Cache/ISystemClock.cs
    - Source/LinqToDB/Internal/Cache/MemoryCacheEntryExtensions.cs
    - Source/LinqToDB/Internal/Cache/MemoryCacheEntryOptions.cs
    - Source/LinqToDB/Internal/Cache/MemoryCacheOptions.cs
    - Source/LinqToDB/Internal/Cache/PostEvictionCallbackRegistration.cs
    - Source/LinqToDB/Internal/Cache/PostEvictionDelegate.cs
    - Source/LinqToDB/Internal/Cache/SystemClock.cs
    - Source/LinqToDB/Internal/Common/ActivatorExt.cs
    - Source/LinqToDB/Internal/Common/BuildExpressionUtils.cs
    - Source/LinqToDB/Internal/Common/ComWrapper.cs
    - Source/LinqToDB/Internal/Common/DecimalHelper.cs
    - Source/LinqToDB/Internal/Common/DisposableAction.cs
    - Source/LinqToDB/Internal/Common/EmptyIAsyncDisposable.cs
    - Source/LinqToDB/Internal/Common/EnumerableHelper.cs
    - Source/LinqToDB/Internal/Common/EnumerablePolyfills.cs
    - Source/LinqToDB/Internal/Common/ErrorHelper.cs
    - Source/LinqToDB/Internal/Common/MemberCache.cs
    - Source/LinqToDB/Internal/Common/NonCapturingLazyInitializer.cs
    - Source/LinqToDB/Internal/Common/Pools.cs
    - Source/LinqToDB/Internal/Common/SnapshotDictionary.cs
    - Source/LinqToDB/Internal/Common/SqlTextWriter.cs
    - Source/LinqToDB/Internal/Common/StackGuard.cs
    - Source/LinqToDB/Internal/Common/StringBuilderExtensions.cs
    - Source/LinqToDB/Internal/Common/TaskCache.cs
    - Source/LinqToDB/Internal/Common/TopoSorting.cs
    - Source/LinqToDB/Internal/Common/TypeHelper.cs
    - Source/LinqToDB/Internal/Common/Utils.cs
    - Source/LinqToDB/Internal/Common/ValueComparer.cs
    - Source/LinqToDB/Internal/Common/ValueComparer{T}.cs
    - Source/LinqToDB/Internal/Conversion/ConvertBuilder.cs
    - Source/LinqToDB/Internal/Conversion/ConverterLambda.cs
    - Source/LinqToDB/Internal/Conversion/ConvertReducer.cs
    - Source/LinqToDB/Internal/Conversion/ConvertUtils.cs
    - Source/LinqToDB/Internal/DataContextExtensions.cs
  - Read (this run, batch 2):
    - Source/LinqToDB/Internal/DataProvider/AliasesHelper.cs
    - Source/LinqToDB/Internal/DataProvider/AssemblyResolver.cs
    - Source/LinqToDB/Internal/DataProvider/DatabaseSpecificQueryable.cs
    - Source/LinqToDB/Internal/DataProvider/DatabaseSpecificTable.cs
    - Source/LinqToDB/Internal/DataProvider/DataProviderExtensions.cs
    - Source/LinqToDB/Internal/DataProvider/DataProviderFactoryBase.cs
    - Source/LinqToDB/Internal/DataProvider/DataProviderOptions.cs
    - Source/LinqToDB/Internal/DataProvider/DataTools.cs
    - Source/LinqToDB/Internal/DataProvider/DmlServiceBase.cs
    - Source/LinqToDB/Internal/DataProvider/IdentifierKind.cs
    - Source/LinqToDB/Internal/DataProvider/IdentifierServiceBase.cs
    - Source/LinqToDB/Internal/DataProvider/IdentifierServiceSimple.cs
    - Source/LinqToDB/Internal/DataProvider/IdentifiersHelper.cs
    - Source/LinqToDB/Internal/DataProvider/IDmlService.cs
    - Source/LinqToDB/Internal/DataProvider/IExecutionScope.cs
    - Source/LinqToDB/Internal/DataProvider/InvariantCultureRegion.cs
    - Source/LinqToDB/Internal/DataProvider/IQueryParametersNormalizer.cs
    - Source/LinqToDB/Internal/DataProvider/MultipleRowsHelper.cs
    - Source/LinqToDB/Internal/DataProvider/NoopQueryParametersNormalizer.cs
    - Source/LinqToDB/Internal/DataProvider/OdbcProviderAdapter.cs
    - Source/LinqToDB/Internal/DataProvider/OleDbProviderAdapter.cs
    - Source/LinqToDB/Internal/DataProvider/ReaderInfo.cs
    - Source/LinqToDB/Internal/DataProvider/ReservedWords.cs
    - Source/LinqToDB/Internal/DataProvider/SimpleServiceProvider.cs
    - Source/LinqToDB/Internal/DataProvider/SqlProviderHelper.cs
    - Source/LinqToDB/Internal/DataProvider/SqlTypes.cs
    - Source/LinqToDB/Internal/DataProvider/TableSpecHintExtensionBuilder.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/AggregateFunctionsMemberTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/CombinedMemberConverter.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/CombinedMemberTranslator.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/ConvertMemberTranslatorDefault.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/GuidMemberTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/IMemberConverter.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/LegacyMemberConverterBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/MathMemberTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/SqlExpressionFactoryExtensions.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/SqlFunctionsMemberTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/SqlTypesTranslationDefault.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/StringMemberTranslatorBase.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/TranslationContextExtensions.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/TranslationRegistration.cs
    - Source/LinqToDB/Internal/DataProvider/Translation/TranslationRegistrationExtensions.cs
    - Source/LinqToDB/Internal/DataProvider/UniqueParametersNormalizer.cs
    - Source/LinqToDB/Internal/DataProvider/WrapParametersVisitor.cs
    - Source/LinqToDB/Internal/Infrastructure/AnnotatableBase.cs
    - Source/LinqToDB/Internal/Infrastructure/AnnotatableExtensions.cs
    - Source/LinqToDB/Internal/Infrastructure/Annotation.cs
    - Source/LinqToDB/Internal/Infrastructure/IMutableAnnotatable.cs
    - Source/LinqToDB/Internal/Infrastructure/IReadOnlyAnnotatable.cs
    - Source/LinqToDB/Internal/Infrastructure/IUniqueIdGenerator.cs
    - Source/LinqToDB/Internal/Infrastructure/ServiceProviderExtensions.cs
    - Source/LinqToDB/Internal/Infrastructure/TypeExtensions.cs
    - Source/LinqToDB/Internal/Infrastructure/UniqueIdGenerator.cs
  - Read (this run, batch 3):
    - Source/LinqToDB/Internal/Expressions/ChangeTypeExpression.cs
    - Source/LinqToDB/Internal/Expressions/ConstantPlaceholderExpression.cs
    - Source/LinqToDB/Internal/Expressions/ContextRefExpression.cs
    - Source/LinqToDB/Internal/Expressions/ConvertFromDataReaderExpression.cs
    - Source/LinqToDB/Internal/Expressions/DefaultValueExpression.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionConstants.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionEqualityComparer.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionEvaluator.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionGenerator.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionHelper.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionHelpers.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionInstances.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionPrinter.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/EqualsToVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitor{TContext}.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/FindVisitorBase.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/LegacyVisitorBase.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/PathVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformInfoVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformInfoVisitor{TContext}.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitor{TContext}.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/TransformVisitorsBase.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/VisitActionVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/VisitActionVisitor{TContext}.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/VisitFuncVisitor.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/VisitFuncVisitor{TContext}.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/WritableContext.cs
    - Source/LinqToDB/Internal/Expressions/ExpressionVisitors/WritableContext{TWriteable,TStatic}.cs
    - Source/LinqToDB/Internal/Expressions/InternalExtensions.cs
    - Source/LinqToDB/Internal/Expressions/IPrintableExpression.cs
    - Source/LinqToDB/Internal/Expressions/MarkerExpression.cs
    - Source/LinqToDB/Internal/Expressions/MarkerType.cs
    - Source/LinqToDB/Internal/Expressions/SkipIfConstantAttribute.cs
    - Source/LinqToDB/Internal/Expressions/SqlAdjustTypeExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlDefaultIfEmptyExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlEagerLoadExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlErrorExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlGenericParamAccessExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlPathExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlQueryDependentAttributeHelper.cs
    - Source/LinqToDB/Internal/Expressions/SqlQueryRootExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlReaderIsNullExpression.cs
    - Source/LinqToDB/Internal/Expressions/SqlValidateExpression.cs
    - Source/LinqToDB/Internal/Expressions/TagExpression.cs
    - Source/LinqToDB/Internal/Expressions/TransformInfo.cs
    - Source/LinqToDB/Internal/Expressions/Types/CustomMapperAttribute.cs
    - Source/LinqToDB/Internal/Expressions/Types/GenericTaskToTaskMapper.cs
    - Source/LinqToDB/Internal/Expressions/Types/GenericValueTaskMapper.cs
    - Source/LinqToDB/Internal/Expressions/Types/ICustomMapper.cs
    - Source/LinqToDB/Internal/Expressions/Types/TypeWrapper.cs
    - Source/LinqToDB/Internal/Expressions/Types/TypeWrapperNameAttribute.cs
    - Source/LinqToDB/Internal/Expressions/Types/ValueTaskToTaskMapper.cs
    - Source/LinqToDB/Internal/Expressions/Types/WrappedBindingFlagsAttribute.cs
    - Source/LinqToDB/Internal/Expressions/Types/WrapperAttribute.cs
    - Source/LinqToDB/Internal/Expressions/WindowFunctionHelpers.cs
- Tier 3 (skipped, logged): 0
</details>
