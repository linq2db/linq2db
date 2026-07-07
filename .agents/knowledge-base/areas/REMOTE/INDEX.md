---
area: REMOTE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 18/18
coverage_tier_2: 6/6
---

# REMOTE

Six transport-binding NuGet packages that expose a linq2db data context over a network wire. Each package is a thin shim: it has no query logic, no mapping logic, and no wire-format ownership. All of those live in [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) (`ILinqService`, `RemoteDataContextBase`, `LinqServiceQuery/Result/Info`) and [INTERNAL-API](../INTERNAL-API/INDEX.md) (`LinqServiceSerializer`).

## Common pattern

Every transport follows the same three-type pattern:

- **`<X>DataContext`** -- subclasses `RemoteDataContextBase`. Overrides `GetClient()` to construct a transport-specific `ILinqService` client. Overrides `ContextIDPrefix` to tag query cache buckets (e.g. `"GrpcRemoteLinqService"`, `"HttpRemoteLinqService"`, `"SignalRRemoteLinqService"`, `"WcfRemoteLinqService"`).
- **`<X>LinqServiceClient`** -- implements `ILinqService` by delegating each of the five methods (`GetInfoAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`, `ExecuteReaderAsync`, `ExecuteBatchAsync`) to the underlying transport channel. Sets `ILinqService.RemoteClientTag` to a transport identifier string.
- **Server surface** -- receives the same five calls over the wire and delegates to an injected `ILinqService` (the concrete `LinqService` / `LinqService<T>` from REMOTE-CLIENT). Error propagation is uniform: a `bool transferInternalExceptionToClient` flag wraps exceptions in the transport's native error type (`RpcException` for gRPC, `FaultException` for WCF; HTTP relies on status codes).

## Subsystems

### gRPC (`LinqToDB.Remote.Grpc`)

TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0`. AOT-compatible on net8.0+.

gRPC (protobuf-net.Grpc) requires message types for every parameter and return value. Four DTO wrappers live in `Dto/`:
- `GrpcConfiguration` -- wraps `string? Configuration` (field 1). Request for `GetInfoAsync`.
- `GrpcConfigurationQuery` -- wraps `string? Configuration` + `string QueryData` (fields 1, 2). Request for all four query operations.
- `GrpcInt` -- wraps `int Value` (field 1) with bidirectional implicit operators.
- `GrpcString` -- wraps `string? Value` (field 1) with bidirectional implicit operators.

All four carry `[DataContract]` / `[DataMember(Order = N)]`.

`IGrpcLinqService` is the `[Service]` contract; `GrpcLinqService` is the server impl (unwraps `CallContext` for `CancellationToken`; throws `RpcException(StatusCode.Unknown, ...)` on transferred exceptions). `GrpcLinqServiceClient` implements `ILinqService` + `IDisposable` over a `GrpcChannel` proxy. `GrpcDataContext` accepts `string address` + optional `GrpcChannelOptions`; `GetClient()` calls `GrpcChannel.ForAddress(...)` per logical query.

### HTTP (`LinqToDB.Remote.HttpClient.Client` + `.Server`)

Client TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0`. Server: `net8.0;net9.0;net10.0` (ASP.NET Core, `Microsoft.NET.Sdk.Web`).

Client: `HttpClientLinqServiceClient` uses `HttpClient` with `PostAsJsonAsync`/`ReadFromJsonAsync`/`ReadAsStringAsync`; endpoint `{requestUri}/{methodName}/{configuration?}`. `HttpClientDataContext` accepts a pre-built client, `(HttpClient, requestUri)`, or `(Uri baseAddress, requestUri)`. Client `ServiceConfigurationExtensions` registers the client keyed-scoped + `TContext` transient + `IDataContextFactory<TContext>`; default route `"api/linq2db"`; `InitHttpClientAsync` warms up via `ConfigureAsync`.

Server: `LinqToDBController` is an `[ApiController]` exposing five `[HttpPost]` actions; lazily creates `LinqService { AllowUpdates = false, RemoteClientTag = "HttpClient" }`. `LinqToDBController<T>` receives `ILinqService<T>` via DI. Server `ServiceConfigurationExtensions` provides `AddLinqToDBController` overloads using `SpecificControllerFeatureProvider<T>` + `ControllerRouteConvention<T>`.

### SignalR (`LinqToDB.Remote.SignalR.Client` + `.Server`)

Shared namespace `LinqToDB.Remote.SignalR`. Client/Server TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0` (server adds `Microsoft.AspNetCore.SignalR.Core` on pre-net8.0).

Client: `SignalRLinqServiceClient` implements `ILinqService` + `IAsyncDisposable`, delegating to `HubConnection.InvokeAsync<T>(methodName, args)` (method names match `ILinqService` member names). `SignalRDataContext` accepts a pre-built client or a raw `HubConnection`. Client `ServiceConfigurationExtensions` registers `HubConnection` as a singleton wrapped in `Container<HubConnection>`; enables `WithAutomaticReconnect()` on net8.0+; default hub route `"/hub/linq2db"`; `InitSignalRAsync` calls `StartAsync()` + `ConfigureAsync`.

Server: `LinqToDBHub` extends `Hub`; hub methods are plain named methods. `LinqToDBHub<T>` receives `ILinqService<T>` via DI. No server-side `ServiceConfigurationExtensions` -- user adds `MapHub<LinqToDBHub>()`.

### WCF (`LinqToDB.Remote.Wcf`)

TFM: **`net462` only**. Single combined client+server package using `System.ServiceModel`.

`IWcfLinqService` is the `[ServiceContract]`; methods lack `CancellationToken` (WCF limitation -- client does best-effort `ThrowIfCancellationRequested()`). `WcfLinqService` is `[ServiceBehavior(InstanceContextMode.Single, ConcurrencyMode.Multiple)]`, wraps exceptions as `FaultException`. `WcfLinqServiceClient` extends `ClientBase<IWcfLinqService>` + `ILinqService` (four ctor overloads). `WcfDataContext` mirrors the four ctors; `GetClient()` selects the right one by populated fields.

## Key types

| Type | Package | Role |
|---|---|---|
| `IGrpcLinqService` | `linq2db.Remote.Grpc` | gRPC service contract (`[Service]`/`[Operation]`) |
| `GrpcLinqService` | `linq2db.Remote.Grpc` | Server: delegates to `ILinqService` |
| `GrpcLinqServiceClient` | `linq2db.Remote.Grpc` | Client: implements `ILinqService` over gRPC channel |
| `GrpcDataContext` | `linq2db.Remote.Grpc` | Client entry point (`RemoteDataContextBase`) |
| `GrpcConfiguration`/`GrpcConfigurationQuery`/`GrpcInt`/`GrpcString` | `linq2db.Remote.Grpc` | Protobuf message wrappers |
| `HttpClientLinqServiceClient` | `linq2db.Remote.HttpClient.Client` | Client over `HttpClient` |
| `HttpClientDataContext` | `linq2db.Remote.HttpClient.Client` | Client entry point |
| `LinqToDBController`/`LinqToDBController<T>` | `linq2db.Remote.HttpClient.Server` | ASP.NET Core MVC controller |
| `SignalRLinqServiceClient` | `linq2db.Remote.SignalR.Client` | Client over `HubConnection` |
| `SignalRDataContext` | `linq2db.Remote.SignalR.Client` | Client entry point |
| `LinqToDBHub`/`LinqToDBHub<T>` | `linq2db.Remote.SignalR.Server` | ASP.NET Core SignalR hub |
| `IWcfLinqService` | `linq2db.Remote.Wcf` | WCF service contract |
| `WcfLinqService` | `linq2db.Remote.Wcf` | Server `[ServiceBehavior]` impl |
| `WcfLinqServiceClient` | `linq2db.Remote.Wcf` | Client `ClientBase<IWcfLinqService>` + `ILinqService` |
| `WcfDataContext` | `linq2db.Remote.Wcf` | Client entry point |

## Files (Tier 1 / Tier 2)

**Tier 1** (all `.cs` files in the area; every file read): the gRPC `GrpcDataContext`/`GrpcLinqService`/`GrpcLinqServiceClient`/`IGrpcLinqService` + 4 `Dto/*`; HTTP client `HttpClientDataContext`/`HttpClientLinqServiceClient`/`ServiceConfigurationExtensions`; HTTP server `LinqToDBController`/`LinqToDBController{T}`/`ServiceConfigurationExtensions`; SignalR client `SignalRDataContext`/`SignalRLinqServiceClient`/`ServiceConfigurationExtensions`; SignalR server `LinqToDBHub`/`LinqToDBHub{T}`; WCF `WcfDataContext`/`WcfLinqService`/`WcfLinqServiceClient`/`IWcfLinqService`.

**Tier 2** (6 csproj files -- read for TFM data): `LinqToDB.Remote.Grpc.csproj`, `LinqToDB.Remote.HttpClient.Client.csproj`, `LinqToDB.Remote.HttpClient.Server.csproj`, `LinqToDB.Remote.SignalR.Client.csproj`, `LinqToDB.Remote.SignalR.Server.csproj`, `LinqToDB.Remote.Wcf.csproj`.

**Tier 3**: none.

## TFM matrix

| Package | TFMs |
|---|---|
| `linq2db.Remote.Grpc` | `net462;netstandard2.0;net8.0;net9.0;net10.0` |
| `linq2db.Remote.HttpClient.Client` | `net462;netstandard2.0;net8.0;net9.0;net10.0` |
| `linq2db.Remote.HttpClient.Server` | `net8.0;net9.0;net10.0` (ASP.NET Core only) |
| `linq2db.Remote.SignalR.Client` | `net462;netstandard2.0;net8.0;net9.0;net10.0` |
| `linq2db.Remote.SignalR.Server` | `net462;netstandard2.0;net8.0;net9.0;net10.0` (needs `Microsoft.AspNetCore.SignalR.Core` on pre-net8.0) |
| `linq2db.Remote.Wcf` | `net462` only |

## Inbound / outbound dependencies

**Inbound:** user application code that installs one of these NuGet packages.

**Outbound:**
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) -- `ILinqService`, `ILinqService<T>`, `RemoteDataContextBase`, `LinqService`, `LinqService<T>`, `LinqServiceInfo`, `IDataContextFactory<T>`, `DataContextFactory<T>`. Every type here is defined against REMOTE-CLIENT contracts.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `LinqServiceSerializer` (owned by `Internal/Remote/`). Transports pass the string payload opaquely; they own none of the encoding.
- ASP.NET Core (`Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.SignalR`) -- server packages only.
- `Grpc.Net.Client`, `protobuf-net.Grpc`, `protobuf-net` -- gRPC.
- `Microsoft.Extensions.Http` -- HTTP client.
- `System.ServiceModel` -- WCF (net462 BCL).

## Known issues / debt

- **`WcfLinqServiceClient.RemoteClientTag` Cyrillic typo** (`WcfLinqServiceClient.cs:57`): `"Wсf"` contains a Cyrillic `с` (U+0441). No functional impact but breaks equality vs Latin `"Wcf"`.
- **WCF cancellation is best-effort only** -- `ThrowIfCancellationRequested()` before each call; cannot cancel in-flight WCF calls.
- **`SignalRLinqServiceClient.DisposeAsync` is a no-op** (`SignalRLinqServiceClient.cs:58`); lifetime is managed by `SignalRDataContext` or DI.
- **SignalR server has no `ServiceConfigurationExtensions`** -- no `AddLinqToDBHub` analogue to `AddLinqToDBController` (inconsistency with HTTP).
- **gRPC creates a new channel per query** (`GrpcDataContext.GetClient()`); channel pooling not explicit.
- **HTTP `CA2000` suppression** (`HttpClientDataContext.cs:46`) for the `HttpClient` transferred to `HttpClientLinqServiceClient`.

## See also

- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) -- contracts and default implementations this area binds.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `LinqServiceSerializer` (wire format ownership).
- [EXTENSIONS-PKG](../EXTENSIONS-PKG/INDEX.md) -- `AddLinqToDBService<TContext>` registers `ILinqService<TContext>` for the server side.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 23 / 23 (all .cs files in area read in full)
- Tier 2 (visited / total): 6 / 6 (all csprojs read for TFM/package data)
- Tier 3 (skipped, logged): 0

Read (this run -- delta):
- `Source/LinqToDB.Remote.Grpc/PublicAPI.Shipped.txt` -- v6 release promotion: Unshipped entries moved to Shipped. No API surface changes; pure baseline churn.
- `Source/LinqToDB.Remote.Wcf/PublicAPI.Shipped.txt` -- v6 release promotion: Unshipped entries moved to Shipped. No API surface changes; pure baseline churn.
</details>
