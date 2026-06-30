---
area: REMOTE-CLIENT
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 9/9
coverage_tier_2: 4/4
---

# REMOTE-CLIENT

In-tree contracts and base implementations for the linq2db remote-execution facility. The area owns everything under `Source/LinqToDB/Remote/` -- 13 files total. Transport wrappers (gRPC, HTTP, SignalR, WCF) live in the [REMOTE](../REMOTE/INDEX.md) area and depend on this area's types without contributing to it.

## Subsystems

### Server-side contract family

`ILinqService` (`Source/LinqToDB/Remote/ILinqService.cs`) is the central server-side interface. Five async operations form the wire surface:

| Method | Returns | Purpose |
|---|---|---|
| `GetInfoAsync` | `Task<LinqServiceInfo>` | Server capability discovery on first connect |
| `ExecuteNonQueryAsync` | `Task<int>` | DML or DDL that returns affected-row count |
| `ExecuteScalarAsync` | `Task<string?>` | Single-value query; result serialized to wire string or `null` |
| `ExecuteReaderAsync` | `Task<string>` | Full result set; wire payload is a serialized `LinqServiceResult` |
| `ExecuteBatchAsync` | `Task<int>` | Multi-statement transactional batch |

`RemoteClientTag` (`string?`) passes an arbitrary tag from the client through `DataConnection.Tag` on the server for diagnostics.

`ILinqService<T>` (`Source/LinqToDB/Remote/ILinqService{T}.cs`) is a typed marker -- extends `ILinqService` with a `where T : IDataContext` constraint and no additional members.

### Default server implementations

`LinqService` (`Source/LinqToDB/Remote/LinqService.cs`) is the default `ILinqService` implementation:

- **Context creation.** `CreateDataContext(string? configuration)` (virtual, `:64`) constructs a `DataConnection`, applies `RemoteClientTag`, optionally overlays `MappingSchema`.
- **Policy guard.** `ValidateQuery(LinqServiceQuery query)` (virtual, `:73`) throws `LinqToDBException` if `AllowUpdates == false` and the statement isn't a `SELECT`.
- **Deserialization.** Each handler deserializes the wire payload via `LinqServiceSerializer.Deserialize` (see [INTERNAL-API](../INTERNAL-API/INDEX.md)) into a `LinqServiceQuery`, executes against a fresh `DataConnection`, re-serializes.
- **Batch execution.** `ExecuteBatchAsync` deserializes a string array (`LinqServiceSerializer.DeserializeStringArray`), validates each, runs them in a single transaction, returns the byte-count of the raw input string as a sentinel (`:266`).
- **Scalar serialization.** `ProcessScalar` wraps a raw scalar in a single-row `LinqServiceResult` (`:172--197`).
- **Data reader materialization.** `ProcessDataReaderWrapper` handles column-type inference for enums/value converters from `SelectQuery.Columns` or DML `Output.OutputColumns` (`:277--388`).

`LinqService<T>` (`Source/LinqToDB/Remote/LinqService{T}.cs`) extends `LinqService` with a typed factory: `CreateDataContext` delegates to an injected `IDataContextFactory<T>`, throwing `LinqToDBException` if the context isn't a `DataConnection`.

### IDataContextFactory contract

`IDataContextFactory<out TContext>` (`Source/LinqToDB/Remote/IDataContextFactory.cs:7`) -- covariant factory with one method: `TContext CreateDataContext(string? configuration = null)`.

`DataContextFactory<TContext>` (`Source/LinqToDB/Remote/DataContextFactory.cs:5`) -- trivial concrete implementation wrapping a `Func<string?,TContext>`.

### Client-side IDataContext implementation

`RemoteDataContextBase` (`Source/LinqToDB/Remote/RemoteDataContextBase.cs`) is the client-side `IDataContext`, `abstract partial` split across three files:

- `RemoteDataContextBase.cs` -- core state and lifecycle.
- `RemoteDataContextBase.Interceptors.cs` -- interceptor slot declarations.
- `RemoteDataContextBase.QueryRunner.cs` -- nested `QueryRunner` bridging `IQueryRunner` -> `ILinqService`.

**Configuration bootstrap.** On first use `PreloadConfigurationInfoAsync` (`:248`) calls `GetInfoAsync`, resolves assembly-qualified type names from `LinqServiceInfo`, hydrates a `ConfigurationInfo` record (`:106`) held in a static `ConcurrentDictionary<string,ConfigurationInfo>` keyed by `ConfigurationString` (`:115`). Subsequent contexts reuse the cached record. Public entry point: `ConfigureAsync(CancellationToken)` (`:295`).

**Service provider.** Implements `IInfrastructure<IServiceProvider>` (`:83--104`) via double-checked lock (`Lock _guard`, `:81`, `System.Threading.Lock`). The backing field is lazily initialized via `InitServiceProvider` (`:69--79`), populating a `SimpleServiceProvider` with the `IMemberTranslator`, `IMemberConverter`, and (if present) `IDmlService` from the resolved `ConfigurationInfo`.

**Remote wrapper types.** Three nested sealed classes wrap server-resolved service types with per-type `MemoryCache`-based caches, each keyed by the resolved `Type` and bound to `Common.Configuration.Linq.CacheSlidingExpiration`:

- `RemoteMemberTranslator` (`:151`) -- wraps `IMemberTranslator`; delegates `Translate`.
- `RemoteMemberConverter` (`:177`) -- wraps `IMemberConverter`; delegates `Convert`.
- `RemoteDmlService` (`:203`) -- wraps `IDmlService`; delegates `IsTableNotFoundException`.

All three are instantiated via `ActivatorExt.CreateInstance<T>(resolvedType)` and cached so instances sharing the same resolved type share one wrapper.

**Provider surface reconstruction.** The client reconstructs `ISqlBuilder` and `ISqlOptimizer` factories locally by expression-compiling constructors from the resolved types (`:467--537`), cached in static `ConcurrentDictionary` instances. `_sqlBuilders` key is `(SqlProviderType, MappingSchema, SqlOptimizerType, SqlProviderFlags, DataOptions)` (`:462--465`); `_sqlOptimizers` key is `(SqlOptimizerType, SqlProviderFlags)` (`:502`).

**Mapping schema chain.** `RemoteMappingSchema` (nested, `:117`) caches per-`(contextIDPrefix, mappingSchemaType)`. SQL Server, Firebird, Oracle receive special `GetRemoteMappingSchema` treatment to inject provider-specific type converters (`:138--140`); others use `ActivatorExt.CreateInstance`. `SerializationMappingSchema` (`:357`) combines `Internal.Remote.SerializationMappingSchema.Instance` with the resolved provider schema.

**Batch mode.** `BeginBatch()` / `CommitBatchAsync()` (`:544--580`) accumulate serialized query strings in `_queryBatch` and flush via `ExecuteBatchAsync`. `QueryRunner.ExecuteNonQueryAsync` short-circuits into the batch queue (`.QueryRunner.cs:259--261`).

**Scoped options / schema override.** `UseOptions(Func<DataOptions,DataOptions>)` (`:755`) and `UseMappingSchema(MappingSchema)` (`:780`) return `IDisposable?` tokens restoring prior state on disposal.

**Disposal.** `Dispose` / `DisposeAsync` fire `IDataContextInterceptor.OnClosing/OnClosed` (`:632--644`) then set `Disposed = true`. `ThrowOnDisposed()` guards every public member. `DisposeClient` / `DisposeClientAsync` (`:582--596`) prefer `IAsyncDisposable` in the async path, the reverse in sync.

**Obsolete API.** `ConfigurationString` setter (`:51`) and `MappingSchema` setter (`:340`) are `[Obsolete("...v7")]`, wired through `ConfigurationApplier` (`:647`).

### QueryRunner (partial)

`RemoteDataContextBase.QueryRunner` (nested `sealed class`, `.QueryRunner.cs:29`) extends `QueryRunnerBase` from [LINQ](../LINQ/INDEX.md). Its `Execute*Async` methods:

1. Call `ISqlOptimizer.PrepareStatementForRemoting` to finalize the AST.
2. Serialize via `LinqServiceSerializer.Serialize`.
3. Call the appropriate `ILinqService` method on a `_client` from `RemoteDataContextBase.GetClient()` (abstract).
4. Deserialize the response.
5. Wrap in a `RemoteDataReader` (internal), exposed as `IDataReaderAsync`.

Synchronous variants are shims over `SafeAwaiter.Run(…Async)` (`.QueryRunner.cs:145--149`). `GetSqlText()` reconstructs the full SQL locally without a request (`.QueryRunner.cs:51--125`).

### Interceptors (partial)

`RemoteDataContextBase.Interceptors.cs` declares six `IInterceptable<T>` slots (`:7--22`). The inline comment (`:15`) notes remote support is limited: only `IDataContextInterceptor` is actively invoked (on Close/CloseAsync); other slots exist for API compatibility but are not dispatched.

### Wire DTOs

| Type | File | Role |
|---|---|---|
| `LinqServiceQuery` | `LinqServiceQuery.cs` | Client->server: `SqlStatement`, optional `QueryHints`, `DataOptions` |
| `LinqServiceResult` | `LinqServiceResult.cs` | Server->client: tabular result -- `FieldCount`, `RowCount`, `FieldNames`, `FieldTypes`, `Data` as `List<string?[]>` |
| `LinqServiceInfo` | `LinqServiceInfo.cs` | Server->client on `GetInfoAsync`: assembly-qualified type names + `SqlProviderFlags` + `SupportedTableOptions` |

`LinqServiceInfo` carries `[DataContract]` / `[DataMember(Order=N)]` (`LinqServiceInfo.cs:7--26`) for WCF serialization compatibility. `LinqServiceQuery` / `LinqServiceResult` carry no serialization attributes -- handled by `LinqServiceSerializer`.

### DataService (legacy, NETFRAMEWORK only)

`DataService<T>` (`Source/LinqToDB/Remote/DataService.cs`) compiled only under `#if NETFRAMEWORK`. Extends `System.Data.Services.DataService<T>` (WCF Data Services / OData v3). Update/open-property operations all `throw new NotSupportedException()`. Legacy integration path; no new development targets it.

## Key types

| Type | Kind | File |
|---|---|---|
| `ILinqService` | interface | `ILinqService.cs` |
| `ILinqService<T>` | interface | `ILinqService{T}.cs` |
| `LinqService` | class | `LinqService.cs` |
| `LinqService<T>` | class | `LinqService{T}.cs` |
| `IDataContextFactory<TContext>` | interface | `IDataContextFactory.cs` |
| `DataContextFactory<TContext>` | class | `DataContextFactory.cs` |
| `RemoteDataContextBase` | abstract class | `RemoteDataContextBase.cs` + partials |
| `RemoteMemberTranslator` | nested sealed class | `RemoteDataContextBase.cs:151` |
| `RemoteMemberConverter` | nested sealed class | `RemoteDataContextBase.cs:177` |
| `RemoteDmlService` | nested sealed class | `RemoteDataContextBase.cs:203` |
| `LinqServiceQuery` | wire DTO | `LinqServiceQuery.cs` |
| `LinqServiceResult` | wire DTO | `LinqServiceResult.cs` |
| `LinqServiceInfo` | wire DTO / capability descriptor | `LinqServiceInfo.cs` |
| `DataService<T>` | legacy WCF/OData host | `DataService.cs` (`#if NETFRAMEWORK`) |

## Files (Tier 1 / Tier 2)

**Tier 1** (9 files -- all read in full): `ILinqService.cs`, `ILinqService{T}.cs`, `LinqService.cs`, `LinqService{T}.cs`, `IDataContextFactory.cs`, `RemoteDataContextBase.cs`, `LinqServiceQuery.cs`, `LinqServiceResult.cs`, `LinqServiceInfo.cs`.

**Tier 2** (4 files -- all read in full): `RemoteDataContextBase.Interceptors.cs`, `RemoteDataContextBase.QueryRunner.cs`, `DataContextFactory.cs`, `DataService.cs`.

**Tier 3**: 0 files.

## Inbound / outbound dependencies

**Outbound (this area depends on):**

- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `LinqServiceSerializer`, `SerializationMappingSchema`, `SerializationConverter`, `RemoteDataReader`, `QueryRunnerBase`, `SafeAwaiter`, `SimpleServiceProvider`, `IdentifierBuilder`, `MemoryCache<K,V>`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) -- `IInterceptable<T>` slot pattern, `AddInterceptorImpl` / `RemoveInterceptorImpl`; all six `I*Interceptor` interfaces.
- [LINQ](../LINQ/INDEX.md) -- `QueryRunnerBase`, `IQueryRunner`, `IQueryExpressions`.
- [SQL-AST](../SQL-AST/INDEX.md) -- `SqlStatement`, `SqlInsertStatement`, `SqlDeleteStatement`, `SqlUpdateStatement`, `SqlMergeStatement`, `QueryType`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `ISqlBuilder`, `ISqlOptimizer`, `SqlProviderFlags`, `OptimizationContext`.
- [MAPPING](../MAPPING/INDEX.md) -- `MappingSchema`, `EntityDescriptor`.
- [DATA](../DATA/INDEX.md) -- `DataConnection`, `DataOptions`.

**Inbound (depends on this area):**

- [REMOTE](../REMOTE/INDEX.md) -- every transport package implements `ILinqService` (server) and subclasses `RemoteDataContextBase` (client).

## Known issues / debt

- `ConfigurationString` setter and `MappingSchema` setter on `RemoteDataContextBase` are `[Obsolete("...v7")]` but still used internally (`:50`,`:337`). Plan is to make them private in v7.
- `DataService<T>` is `#if NETFRAMEWORK` only with several `throw new NotSupportedException()` stubs. Effectively frozen.
- `ExecuteBatchAsync` returns `queryData.Length` (raw byte-count of the concatenated serialized string, `LinqService.cs:266`) -- a surprising sentinel carrying no meaningful per-statement count.
- Only `IDataContextInterceptor` is actively dispatched on the client (Close/CloseAsync). The other five interceptor slots exist for API compatibility but are never invoked -- `IQueryExpressionInterceptor` / `IExceptionInterceptor` registered on a remote context are silently ignored.
- DI-0241 / DI-0242: the `[Obsolete]`-setter TODOs on `RemoteDataContextBase.cs:50` and `:337` (tracked detected-issues).
- DI-0733: six public members on `RemoteDataContextBase` (`AddMappingSchema`, `BeginBatch`, `CommitBatch`, `CommitBatchAsync`, `Dispose`, `DisposeAsync`) lack `<summary>` XML docs.

## See also

- [INTERNAL-API area index](../INTERNAL-API/INDEX.md) -- `LinqServiceSerializer`, `SerializationMappingSchema`, `SerializationConverter`, `RemoteDataReader`
- [REMOTE area index](../REMOTE/INDEX.md) -- transport packages
- [INTERCEPTORS area index](../INTERCEPTORS/INDEX.md) -- `IInterceptable<T>` slot pattern
- [LINQ area index](../LINQ/INDEX.md) -- `QueryRunnerBase`, `IQueryRunner`
- [SQL-AST area index](../SQL-AST/INDEX.md) -- `SqlStatement` and statement subtypes

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 9 / 9
- Tier 2 (visited / total): 4 / 4 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta):
  - `Source/LinqToDB/Remote/RemoteDataContextBase.cs` -- no structural changes relative to build-time read; delta-confirmed: `IInfrastructure<IServiceProvider>` / `InitServiceProvider` pattern (`:83--104`), `Lock _guard` (`:81`), `RemoteMemberTranslator` / `RemoteMemberConverter` / `RemoteDmlService` nested wrappers (`:151`,`:177`,`:203`), `_sqlBuilders` cache key precise form (`:462--465`), `_sqlOptimizers` cache key (`:502`), `UseOptions` / `UseMappingSchema` scoped-override methods (`:755`,`:780`), `DisposeClient` / `DisposeClientAsync` (`:582--596`), and line-number corrections throughout.
</details>
