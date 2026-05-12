---
area: REMOTE-CLIENT
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-03
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 9/9
coverage_tier_2: 4/4
---

# REMOTE-CLIENT

In-tree contracts and base implementations for the linq2db remote-execution facility. The area owns everything under `Source/LinqToDB/Remote/` — 13 files total. Transport wrappers (gRPC, HTTP, SignalR, WCF) live in the [REMOTE](../REMOTE/INDEX.md) area and depend on this area's types without contributing to it.

## Subsystems

### Server-side contract family

`ILinqService` (`Source/LinqToDB/Remote/ILinqService.cs`) is the central server-side interface. Five async operations form the wire surface:

| Method | Returns | Purpose |
|---|---|---|
| `GetInfoAsync` | `Task<LinqServiceInfo>` | Server capability discovery on first connect |
| `ExecuteNonQueryAsync` | `Task<int>` | DML or DDL that returns affected-row count |
| `ExecuteScalarAsync` | `Task<string?>` | Single-value query; result is serialized to wire string or `null` |
| `ExecuteReaderAsync` | `Task<string>` | Full result set; wire payload is a serialized `LinqServiceResult` |
| `ExecuteBatchAsync` | `Task<int>` | Multi-statement transactional batch |

`RemoteClientTag` (`string?`) passes an arbitrary tag from the client through `DataConnection.Tag` on the server for diagnostics.

`ILinqService<T>` (`Source/LinqToDB/Remote/ILinqService{T}.cs`) is a typed marker — extends `ILinqService` with a `where T : IDataContext` constraint and no additional members. Used by per-transport packages to bind to a specific context type at the DI/registration level.

### Default server implementations

`LinqService` (`Source/LinqToDB/Remote/LinqService.cs`) is the default `ILinqService` implementation. Key responsibilities:

- **Context creation.** `CreateDataContext(string? configuration)` (virtual, `Source/LinqToDB/Remote/LinqService.cs:64`) constructs a `DataConnection`, applies `RemoteClientTag`, and optionally overlays `MappingSchema`.
- **Policy guard.** `ValidateQuery(LinqServiceQuery query)` (virtual, `:73`) throws `LinqToDBException` if `AllowUpdates == false` and the statement isn't a `SELECT`. Transport packages or host applications override this to implement finer-grained ACL.
- **Deserialization.** Each handler deserializes the wire payload via `LinqServiceSerializer.Deserialize` (see [INTERNAL-API](../INTERNAL-API/INDEX.md)) into a `LinqServiceQuery`, executes against a fresh `DataConnection`, then re-serializes the result.
- **Batch execution.** `ExecuteBatchAsync` deserializes a string array of queries (`LinqServiceSerializer.DeserializeStringArray`), validates each, runs them inside a single transaction (`BeginTransactionAsync` / `CommitTransactionAsync`), and returns the byte-count of the raw input string as a sentinel (`:266`).
- **Scalar serialization.** `ProcessScalar` wraps a raw scalar in a single-row `LinqServiceResult` so `ExecuteScalarAsync` returns a uniform format (`:172–197`).
- **Data reader materialization.** `ProcessDataReaderWrapper` handles column-type inference for enums and value converters, resolving `FieldTypes` from the query's `SelectQuery.Columns` or DML `Output.OutputColumns` (`:277–388`).

`LinqService<T>` (`Source/LinqToDB/Remote/LinqService{T}.cs`) extends `LinqService` with a typed factory: `CreateDataContext` delegates to an injected `IDataContextFactory<T>` and casts the result to `DataConnection`, throwing `LinqToDBException` if the context isn't a `DataConnection` subtype.

### IDataContextFactory contract

`IDataContextFactory<out TContext>` (`Source/LinqToDB/Remote/IDataContextFactory.cs:7`) is a minimal covariant factory — one method: `TContext CreateDataContext(string? configuration = null)`. Server packages implement this interface so `LinqService<T>` can create per-request contexts without a static dependency on any particular DI container.

`DataContextFactory<TContext>` (`Source/LinqToDB/Remote/DataContextFactory.cs:5`) is a trivial concrete implementation: wraps a `Func<string?,TContext>` delegate. Allows lambda-based registration for simple host setups.

### Client-side IDataContext implementation

`RemoteDataContextBase` (`Source/LinqToDB/Remote/RemoteDataContextBase.cs`) is the client-side `IDataContext`. It is `abstract partial` split across three files:

- `RemoteDataContextBase.cs` — core state and lifecycle.
- `RemoteDataContextBase.Interceptors.cs` — interceptor slot declarations.
- `RemoteDataContextBase.QueryRunner.cs` — nested `QueryRunner` bridging `IQueryRunner` → `ILinqService`.

**Configuration bootstrap.** On first use the context calls `PreloadConfigurationInfoAsync` (`:246`), which calls `GetInfoAsync` on the server, resolves the assembly-qualified type names from `LinqServiceInfo`, and hydrates a `ConfigurationInfo` record (`:104`) held in a static `ConcurrentDictionary<string,ConfigurationInfo>` keyed by `ConfigurationString` (`:113`). Subsequent contexts with the same configuration string reuse the cached record — no second round-trip. Public entry point is `ConfigureAsync(CancellationToken)` (`:293`).

**Provider surface reconstruction.** The client reconstructs `ISqlBuilder` and `ISqlOptimizer` factories locally by expression-compiling constructors from the resolved types (`:465–537`), cached in static `ConcurrentDictionary` instances keyed by `(SqlProviderType, MappingSchema, ...)`. This gives the client the same SQL-generation capability as the server-side provider without holding a live `DataConnection`.

**Mapping schema chain.** `RemoteMappingSchema` (nested, `:115`) caches per-`(contextIDPrefix, mappingSchemaType)` schemas. Three providers — SQL Server, Firebird, Oracle — receive special `GetRemoteMappingSchema` treatment to inject provider-specific type converters (`:133–139`); all others use `ActivatorExt.CreateInstance`. `SerializationMappingSchema` (`:355`) combines `Internal.Remote.SerializationMappingSchema.Instance` with the resolved provider schema for wire serialization.

**Batch mode.** `BeginBatch()` / `CommitBatchAsync()` (`:542–578`) accumulate serialized query strings in `_queryBatch` and flush them all at once via `ExecuteBatchAsync`. The `QueryRunner.ExecuteNonQueryAsync` short-circuits into the batch queue (`.QueryRunner.cs:259–261`) rather than firing a request immediately.

**Obsolete API.** `ConfigurationString` setter (`:51`) and `MappingSchema` setter (`:338`) are marked `[Obsolete("...v7")]`. Both are wired through `ConfigurationApplier` (`:644`) which enforces immutability of connection-identity options and allows schema overlay changes.

**Disposal.** `Dispose` / `DisposeAsync` fire `IDataContextInterceptor.OnClosing/OnClosed` interceptor slots (`:604–641`) then set `Disposed = true`. `ThrowOnDisposed()` guards every public member.

### QueryRunner (partial)

`RemoteDataContextBase.QueryRunner` (nested `sealed class`, `.QueryRunner.cs:29`) extends `QueryRunnerBase` from [LINQ](../LINQ/INDEX.md). Its `ExecuteReaderAsync` / `ExecuteScalarAsync` / `ExecuteNonQueryAsync` methods:

1. Call `ISqlOptimizer.PrepareStatementForRemoting` to finalize the AST for wire encoding.
2. Serialize via `LinqServiceSerializer.Serialize` (see [INTERNAL-API](../INTERNAL-API/INDEX.md)).
3. Call the appropriate `ILinqService` method on a `_client` obtained from `RemoteDataContextBase.GetClient()` (abstract).
4. Deserialize the response via `LinqServiceSerializer.DeserializeResult` / `SerializationConverter.Deserialize`.
5. Wrap the result in a `RemoteDataReader` (internal, `Source/LinqToDB/Internal/Remote/RemoteDataReader.cs`), exposed as `IDataReaderAsync`.

Synchronous `ExecuteNonQuery` / `ExecuteScalar` / `ExecuteReader` are shims over `SafeAwaiter.Run(…Async)` (`.QueryRunner.cs:145–149`).

`GetSqlText()` reconstructs the full SQL string locally using the client-side `ISqlBuilder` / `ISqlOptimizer` without sending a request (`.QueryRunner.cs:51–125`), matching how `DataConnection.QueryRunner.GetSqlText` works.

### Interceptors (partial)

`RemoteDataContextBase.Interceptors.cs` declares six `IInterceptable<T>` slots (`:7–22`) — one per interceptor interface: `IDataContextInterceptor`, `IEntityServiceInterceptor`, `IUnwrapDataObjectInterceptor`, `IEntityBindingInterceptor`, `IQueryExpressionInterceptor`, `IExceptionInterceptor`. Dispatch mechanics (`AddInterceptorImpl` / `RemoveInterceptorImpl`) are inherited from the [INTERCEPTORS](../INTERCEPTORS/INDEX.md) extension methods. The inline comment (`:15`) notes that remote context support is limited: only `IDataContextInterceptor` is actively invoked (on `Close` / `CloseAsync`); other slots exist for API compatibility but are not dispatched by the remote execution path.

### Wire DTOs

| Type | File | Role |
|---|---|---|
| `LinqServiceQuery` | `LinqServiceQuery.cs` | Client→server: carries `SqlStatement`, optional `QueryHints`, and `DataOptions` |
| `LinqServiceResult` | `LinqServiceResult.cs` | Server→client: tabular result — `FieldCount`, `RowCount`, `FieldNames`, `FieldTypes`, `Data` as `List<string?[]>` |
| `LinqServiceInfo` | `LinqServiceInfo.cs` | Server→client on `GetInfoAsync`: assembly-qualified type names for `MappingSchema`, `SqlBuilder`, `SqlOptimizer`, `MemberTranslator`, `MemberConverter`, optional `DmlService`; plus `SqlProviderFlags` and `SupportedTableOptions` |

`LinqServiceInfo` carries `[DataContract]` / `[DataMember(Order=N)]` attributes (`LinqServiceInfo.cs:7–26`) because it was originally used with WCF serialization; the WCF transport in [REMOTE](../REMOTE/INDEX.md) still relies on this.

`LinqServiceQuery` and `LinqServiceResult` carry no serialization attributes — they are handled entirely by `LinqServiceSerializer` (see [INTERNAL-API](../INTERNAL-API/INDEX.md)).

### DataService (legacy, NETFRAMEWORK only)

`DataService<T>` (`Source/LinqToDB/Remote/DataService.cs`) is compiled only under `#if NETFRAMEWORK`. It extends `System.Data.Services.DataService<T>` (WCF Data Services / OData v3) and implements `IServiceProvider` to expose `IDataServiceMetadataProvider`, `IDataServiceQueryProvider`, and `IDataServiceUpdateProvider` (`:55–64`). `MetadataInfo` reflects the entity types reachable from `T`'s `ITable<>` properties and maps them to OData `ResourceType` / `ResourceSet` objects (`:77–162`). Update and open-property operations all `throw new NotSupportedException()`. This class is a legacy integration path; no new development targets it.

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
| `LinqServiceQuery` | wire DTO | `LinqServiceQuery.cs` |
| `LinqServiceResult` | wire DTO | `LinqServiceResult.cs` |
| `LinqServiceInfo` | wire DTO / capability descriptor | `LinqServiceInfo.cs` |
| `DataService<T>` | legacy WCF/OData host | `DataService.cs` (`#if NETFRAMEWORK`) |

## Files (Tier 1 / Tier 2)

**Tier 1** (9 files — all read in full):

- `Source/LinqToDB/Remote/ILinqService.cs`
- `Source/LinqToDB/Remote/ILinqService{T}.cs`
- `Source/LinqToDB/Remote/LinqService.cs`
- `Source/LinqToDB/Remote/LinqService{T}.cs`
- `Source/LinqToDB/Remote/IDataContextFactory.cs`
- `Source/LinqToDB/Remote/RemoteDataContextBase.cs`
- `Source/LinqToDB/Remote/LinqServiceQuery.cs`
- `Source/LinqToDB/Remote/LinqServiceResult.cs`
- `Source/LinqToDB/Remote/LinqServiceInfo.cs`

**Tier 2** (4 files — all read in full):

- `Source/LinqToDB/Remote/RemoteDataContextBase.Interceptors.cs`
- `Source/LinqToDB/Remote/RemoteDataContextBase.QueryRunner.cs`
- `Source/LinqToDB/Remote/DataContextFactory.cs`
- `Source/LinqToDB/Remote/DataService.cs`

**Tier 3**: 0 files (no `bin/`, `obj/`, or generated files under `Source/LinqToDB/Remote/`).

## Inbound / outbound dependencies

**Outbound (this area depends on):**

- [INTERNAL-API](../INTERNAL-API/INDEX.md) — `LinqServiceSerializer` (serialize/deserialize queries and results), `SerializationMappingSchema`, `SerializationConverter`, `RemoteDataReader`, `QueryRunnerBase`, `SafeAwaiter`, `SimpleServiceProvider`, `IdentifierBuilder`, `MemoryCache<K,V>`.
- [INTERCEPTORS](../INTERCEPTORS/INDEX.md) — `IInterceptable<T>` slot pattern, `AddInterceptorImpl` / `RemoveInterceptorImpl` extension methods; all six `I*Interceptor` interfaces.
- [LINQ](../LINQ/INDEX.md) — `QueryRunnerBase`, `IQueryRunner`, `IQueryExpressions`.
- [SQL-AST](../SQL-AST/INDEX.md) — `SqlStatement` (held by `LinqServiceQuery`), `SqlInsertStatement`, `SqlDeleteStatement`, `SqlUpdateStatement`, `SqlMergeStatement`, `QueryType`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `ISqlBuilder`, `ISqlOptimizer`, `SqlProviderFlags`, `OptimizationContext`.
- [MAPPING](../MAPPING/INDEX.md) — `MappingSchema`, `EntityDescriptor`.
- [DATA](../DATA/INDEX.md) — `DataConnection`, `DataOptions`.

**Inbound (depends on this area):**

- [REMOTE](../REMOTE/INDEX.md) — every transport package (`LinqToDB.Remote.Grpc`, `.HttpClient.Client`, `.HttpClient.Server`, `.SignalR.Client`, `.SignalR.Server`, `.Wcf`) implements `ILinqService` (server) and subclasses `RemoteDataContextBase` (client).

## Known issues / debt

- `ConfigurationString` setter and `MappingSchema` setter on `RemoteDataContextBase` are marked `[Obsolete("...v7")]` but still used internally to avoid breaking callers (`RemoteDataContextBase.cs:51,338`). The plan is to make them private in v7.
- `DataService<T>` is `#if NETFRAMEWORK` only and contains several `throw new NotSupportedException()` stubs (SaveChanges, SetReference, open properties). It is effectively frozen.
- The `ExecuteBatchAsync` return value is `queryData.Length` (raw byte-count of the concatenated serialized string, `LinqService.cs:266`) — a surprising sentinel that carries no meaningful per-statement count.
- Only `IDataContextInterceptor` is actively dispatched on the client (Close/CloseAsync). The other five interceptor slots exist on `RemoteDataContextBase` for API compatibility but are never invoked by the remote path, which means `IQueryExpressionInterceptor` and `IExceptionInterceptor` registered on a remote context are silently ignored.

## See also

- [INTERNAL-API area index](../INTERNAL-API/INDEX.md) — `LinqServiceSerializer`, `SerializationMappingSchema`, `SerializationConverter`, `RemoteDataReader`
- [REMOTE area index](../REMOTE/INDEX.md) — transport packages that implement `ILinqService` and subclass `RemoteDataContextBase`
- [INTERCEPTORS area index](../INTERCEPTORS/INDEX.md) — `IInterceptable<T>` slot pattern
- [LINQ area index](../LINQ/INDEX.md) — `QueryRunnerBase`, `IQueryRunner`
- [SQL-AST area index](../SQL-AST/INDEX.md) — `SqlStatement` and statement subtypes carried in `LinqServiceQuery`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 9 / 9 ✓
  - Source/LinqToDB/Remote/ILinqService.cs
  - Source/LinqToDB/Remote/ILinqService{T}.cs
  - Source/LinqToDB/Remote/LinqService.cs
  - Source/LinqToDB/Remote/LinqService{T}.cs
  - Source/LinqToDB/Remote/IDataContextFactory.cs
  - Source/LinqToDB/Remote/RemoteDataContextBase.cs
  - Source/LinqToDB/Remote/LinqServiceQuery.cs
  - Source/LinqToDB/Remote/LinqServiceResult.cs
  - Source/LinqToDB/Remote/LinqServiceInfo.cs
- Tier 2 (visited / total): 4 / 4 (100%) ✓
  - Source/LinqToDB/Remote/RemoteDataContextBase.Interceptors.cs
  - Source/LinqToDB/Remote/RemoteDataContextBase.QueryRunner.cs
  - Source/LinqToDB/Remote/DataContextFactory.cs
  - Source/LinqToDB/Remote/DataService.cs
- Tier 3 (skipped, logged): 0
</details>
