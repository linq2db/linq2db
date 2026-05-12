---
area: REMOTE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 18/18
coverage_tier_2: 6/6
---

# REMOTE

Six transport-binding NuGet packages that expose a linq2db data context over a network wire. Each package is a thin shim: it has no query logic, no mapping logic, and no wire-format ownership. All of those live in [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) (`ILinqService`, `RemoteDataContextBase`, `LinqServiceQuery/Result/Info`) and [INTERNAL-API](../INTERNAL-API/INDEX.md) (`LinqServiceSerializer`).

## Common pattern

Every transport follows the same three-type pattern:

- **`<X>DataContext`** — subclasses `RemoteDataContextBase`. Overrides `GetClient()` to construct a transport-specific `ILinqService` client. Overrides `ContextIDPrefix` to tag query cache buckets (e.g. `"GrpcRemoteLinqService"`, `"HttpRemoteLinqService"`, `"SignalRRemoteLinqService"`, `"WcfRemoteLinqService"`).
- **`<X>LinqServiceClient`** — implements `ILinqService` by delegating each of the five methods (`GetInfoAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`, `ExecuteReaderAsync`, `ExecuteBatchAsync`) to the underlying transport channel. Sets `ILinqService.RemoteClientTag` to a transport identifier string (used for diagnostics / interceptors).
- **Server surface** — receives the same five calls over the wire and delegates to an injected `ILinqService` (the concrete `LinqService` / `LinqService<T>` from REMOTE-CLIENT). Error propagation from server to client is uniform: a `bool transferInternalExceptionToClient` flag wraps exceptions in the transport's native error type (`RpcException` for gRPC, `FaultException` for WCF; HTTP relies on status codes).

## Subsystems

### gRPC (`LinqToDB.Remote.Grpc`)

TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0`. AOT-compatible on net8.0+.

gRPC (protobuf-net.Grpc) requires message types for every operation parameter and return value. `ILinqService` methods use `string?` and `int` primitives that cannot be gRPC message types directly, so four DTO wrappers live in `Dto/`:

- `GrpcConfiguration` — wraps `string? Configuration` (field 1). Used as the request for `GetInfoAsync`.
- `GrpcConfigurationQuery` — wraps `string? Configuration` + `string QueryData` (fields 1, 2). Used as the request for all four query operations.
- `GrpcInt` — wraps `int Value` (field 1) with bidirectional implicit operators. Return type for non-query and batch.
- `GrpcString` — wraps `string? Value` (field 1) with bidirectional implicit operators. Return type for scalar and reader.

All four carry `[DataContract]` / `[DataMember(Order = N)]` for protobuf-net field numbering.

`IGrpcLinqService` (`Source/LinqToDB.Remote.Grpc/IGrpcLinqService.cs`) is the service contract: annotated with `[Service]` (protobuf-net.Grpc) and each method with `[Operation]`. It is the gRPC analogue of `IWcfLinqService`.

`GrpcLinqService` (`Source/LinqToDB.Remote.Grpc/GrpcLinqService.cs`) is the server implementation: it implements `IGrpcLinqService`, holds an injected `ILinqService`, and unwraps `CallContext` for `CancellationToken` (`context.ServerCallContext?.CancellationToken`). On exception with `transferInternalExceptionToClient = true` it throws `RpcException(StatusCode.Unknown, exception.ToString())`.

`GrpcLinqServiceClient` (`Source/LinqToDB.Remote.Grpc/GrpcLinqServiceClient.cs`) implements `ILinqService` + `IDisposable`. Holds a `GrpcChannel`; constructs the channel proxy via `channel.CreateGrpcService<IGrpcLinqService>()` (protobuf-net.Grpc client factory). Wraps `GrpcInt` / `GrpcString` results back to `int` / `string?` via the implicit operators.

`GrpcDataContext` (`Source/LinqToDB.Remote.Grpc/GrpcDataContext.cs`) accepts `string address` + optional `GrpcChannelOptions`. `GetClient()` calls `GrpcChannel.ForAddress(Address[, ChannelOptions])` to create a fresh channel per logical query.

### HTTP (`LinqToDB.Remote.HttpClient.Client` + `LinqToDB.Remote.HttpClient.Server`)

HTTP is split into two NuGet packages. Client TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0`. Server TFMs: `net8.0;net9.0;net10.0` only (requires ASP.NET Core; uses `Microsoft.NET.Sdk.Web`).

**Client side:**

`HttpClientLinqServiceClient` (`Source/LinqToDB.Remote.HttpClient.Client/HttpClientLinqServiceClient.cs`) implements `ILinqService`. Uses `System.Net.Http.HttpClient` with `PostAsJsonAsync` / `ReadFromJsonAsync` / `ReadAsStringAsync`. Endpoint URL is `{requestUri}/{methodName}/{configuration?}`. `GetInfoAsync` deserializes `LinqServiceInfo` from JSON; other responses return raw `string` or parse `int` via `int.Parse(CultureInfo.InvariantCulture)`. `CancellationToken` is threaded through on net8.0+ (`#if NET8_0_OR_GREATER`).

`HttpClientDataContext` (`Source/LinqToDB.Remote.HttpClient.Client/HttpClientDataContext.cs`) accepts either a pre-built `HttpClientLinqServiceClient`, a `(HttpClient, string requestUri)` pair, or a `(Uri baseAddress, string requestUri)` pair.

`ServiceConfigurationExtensions` (client) registers `HttpClientLinqServiceClient` as a keyed-scoped service (key = serviceName) and `TContext` as transient. Also wires `IDataContextFactory<TContext>`. Default route: `"api/linq2db"`. Provides `InitHttpClientAsync(IDataContext)` extension to trigger `ConfigureAsync` (which calls `GetInfoAsync` to warm up the remote connection).

**Server side:**

`LinqToDBController` (`Source/LinqToDB.Remote.HttpClient.Server/LinqToDBController.cs`) is an ASP.NET Core `[ApiController]` `ControllerBase`. Exposes five `[HttpPost]` actions mapping to the five `ILinqService` methods. A protected virtual `LinqService` property lazily creates `LinqService { AllowUpdates = false, RemoteClientTag = "HttpClient" }`.

`LinqToDBController<T>` (`Source/LinqToDB.Remote.HttpClient.Server/LinqToDBController{T}.cs`) inherits from the base controller; receives `ILinqService<T>` via constructor injection.

`ServiceConfigurationExtensions` (server) provides `AddLinqToDBController(IMvcBuilder, route)` and `AddLinqToDBController<TContext>(IMvcBuilder, route, allowUpdate)`. The generic overload auto-registers `ILinqService<TContext>` backed by `LinqService<T>` (scoped) if not already registered. Both overloads inject the controller type via a custom `SpecificControllerFeatureProvider<T>` and set the route via `ControllerRouteConvention<T>`.

### SignalR (`LinqToDB.Remote.SignalR.Client` + `LinqToDB.Remote.SignalR.Server`)

Also split client/server. Both share the namespace `LinqToDB.Remote.SignalR`. Client TFMs: `net462;netstandard2.0;net8.0;net9.0;net10.0`. Server: same TFM set (no explicit override; uses `Microsoft.Net.Sdk.Web`; adds `Microsoft.AspNetCore.SignalR.Core` package on pre-net8.0 TFMs). Uses `Newtonsoft.Json` as a transitive pin on pre-net8.0 TFMs.

**Client side:**

`SignalRLinqServiceClient` (`Source/LinqToDB.Remote.SignalR.Client/SignalRLinqServiceClient.cs`) implements `ILinqService` + `IAsyncDisposable`. Delegates all five methods to `HubConnection.InvokeAsync<T>(methodName, args)`. Method names match `ILinqService` member names by string (no strong-typed contract; hub method names must match).

`SignalRDataContext` accepts either a pre-built `SignalRLinqServiceClient` or a raw `HubConnection` (in which case it owns the client's lifetime and disposes it via `DisposeAsync`). Overrides `DisposeAsync` to propagate.

`ServiceConfigurationExtensions` (client) registers `HubConnection` as a singleton wrapped in a `Container<HubConnection>` helper class (prevents DI from treating `HubConnection` itself as a keyed/unkeyed ambiguity). On net8.0+ enables `WithAutomaticReconnect()`. Default SignalR hub route: `"/hub/linq2db"`. Provides `InitSignalRAsync` extensions that call `HubConnection.StartAsync()` + `ConfigureAsync`.

**Server side:**

`LinqToDBHub` (`Source/LinqToDB.Remote.SignalR.Server/LinqToDBHub.cs`) extends `Microsoft.AspNetCore.SignalR.Hub`. Hub methods are plain named methods (no interface annotation needed); `SignalRLinqServiceClient` calls them by `nameof` string. Creates `LinqService { AllowUpdates = false, RemoteClientTag = "Signal/R" }` lazily.

`LinqToDBHub<T>` receives `ILinqService<T>` via constructor injection; overrides `LinqService`.

No `ServiceConfigurationExtensions` in the server package — the user adds hub routing via standard ASP.NET Core `MapHub<LinqToDBHub>()`.

### WCF (`LinqToDB.Remote.Wcf`)

TFM: **`net462` only**. Single combined package for both client and server. Uses `System.ServiceModel` (built-in on net462).

`IWcfLinqService` (`Source/LinqToDB.Remote.Wcf/IWcfLinqService.cs`) is the WCF service contract: `[ServiceContract]` on the interface, `[OperationContract(Name = nameof(...))]` on each of the five methods. Note: WCF `IWcfLinqService` lacks `CancellationToken` parameters because WCF does not support cancellation in the same way; `WcfLinqServiceClient` calls `cancellationToken.ThrowIfCancellationRequested()` before each call as a best-effort check.

`WcfLinqService` (`Source/LinqToDB.Remote.Wcf/WcfLinqService.cs`) is the server implementation: `[ServiceBehavior(InstanceContextMode.Single, ConcurrencyMode.Multiple)]`. Wraps exceptions as `FaultException(exception.ToString())`.

`WcfLinqServiceClient` (`Source/LinqToDB.Remote.Wcf/WcfLinqServiceClient.cs`) extends `ClientBase<IWcfLinqService>` and implements `ILinqService`. Four constructor overloads mirror WCF's `ClientBase` constructors: by endpoint config name, by config name + remote address string, by config name + `EndpointAddress`, and by `Binding` + `EndpointAddress`. `RemoteClientTag` is set to `"Wсf"` (note: contains a Cyrillic `с` — likely a typo, no functional impact).

`WcfDataContext` (`Source/LinqToDB.Remote.Wcf/WcfDataContext.cs`) mirrors the four `WcfLinqServiceClient` constructors. `GetClient()` selects the right `WcfLinqServiceClient` constructor based on which fields are populated.

## Key types

| Type | Package | Role |
|---|---|---|
| `IGrpcLinqService` | `linq2db.Remote.Grpc` | gRPC service contract (`[Service]` / `[Operation]`) |
| `GrpcLinqService` | `linq2db.Remote.Grpc` | Server: delegates to `ILinqService` |
| `GrpcLinqServiceClient` | `linq2db.Remote.Grpc` | Client: implements `ILinqService` over gRPC channel |
| `GrpcDataContext` | `linq2db.Remote.Grpc` | Client entry point (`RemoteDataContextBase`) |
| `GrpcConfiguration`, `GrpcConfigurationQuery`, `GrpcInt`, `GrpcString` | `linq2db.Remote.Grpc` | Protobuf message wrappers (gRPC cannot use primitives) |
| `HttpClientLinqServiceClient` | `linq2db.Remote.HttpClient.Client` | Client: implements `ILinqService` over `HttpClient` |
| `HttpClientDataContext` | `linq2db.Remote.HttpClient.Client` | Client entry point |
| `LinqToDBController` / `LinqToDBController<T>` | `linq2db.Remote.HttpClient.Server` | Server: ASP.NET Core MVC controller |
| `SignalRLinqServiceClient` | `linq2db.Remote.SignalR.Client` | Client: implements `ILinqService` over `HubConnection` |
| `SignalRDataContext` | `linq2db.Remote.SignalR.Client` | Client entry point |
| `LinqToDBHub` / `LinqToDBHub<T>` | `linq2db.Remote.SignalR.Server` | Server: ASP.NET Core SignalR hub |
| `IWcfLinqService` | `linq2db.Remote.Wcf` | WCF service contract (`[ServiceContract]`) |
| `WcfLinqService` | `linq2db.Remote.Wcf` | Server: `[ServiceBehavior]` implementation |
| `WcfLinqServiceClient` | `linq2db.Remote.Wcf` | Client: `ClientBase<IWcfLinqService>` + `ILinqService` |
| `WcfDataContext` | `linq2db.Remote.Wcf` | Client entry point |

## Files (Tier 1 / Tier 2)

**Tier 1** (18 files — all `.cs` files in the area; every file read):

| File | Role |
|---|---|
| `Source/LinqToDB.Remote.Grpc/GrpcDataContext.cs` | Client entry point |
| `Source/LinqToDB.Remote.Grpc/GrpcLinqService.cs` | Server implementation |
| `Source/LinqToDB.Remote.Grpc/GrpcLinqServiceClient.cs` | Client `ILinqService` |
| `Source/LinqToDB.Remote.Grpc/IGrpcLinqService.cs` | gRPC contract interface |
| `Source/LinqToDB.Remote.Grpc/Dto/GrpcConfiguration.cs` | Protobuf DTO |
| `Source/LinqToDB.Remote.Grpc/Dto/GrpcConfigurationQuery.cs` | Protobuf DTO |
| `Source/LinqToDB.Remote.Grpc/Dto/GrpcInt.cs` | Protobuf DTO |
| `Source/LinqToDB.Remote.Grpc/Dto/GrpcString.cs` | Protobuf DTO |
| `Source/LinqToDB.Remote.HttpClient.Client/HttpClientDataContext.cs` | Client entry point |
| `Source/LinqToDB.Remote.HttpClient.Client/HttpClientLinqServiceClient.cs` | Client `ILinqService` |
| `Source/LinqToDB.Remote.HttpClient.Client/ServiceConfigurationExtensions.cs` | DI wiring (client) |
| `Source/LinqToDB.Remote.HttpClient.Server/LinqToDBController.cs` | Server base controller |
| `Source/LinqToDB.Remote.HttpClient.Server/LinqToDBController{T}.cs` | Server generic controller |
| `Source/LinqToDB.Remote.HttpClient.Server/ServiceConfigurationExtensions.cs` | DI wiring (server) |
| `Source/LinqToDB.Remote.SignalR.Client/SignalRDataContext.cs` | Client entry point |
| `Source/LinqToDB.Remote.SignalR.Client/SignalRLinqServiceClient.cs` | Client `ILinqService` |
| `Source/LinqToDB.Remote.SignalR.Client/ServiceConfigurationExtensions.cs` | DI wiring (client) |
| `Source/LinqToDB.Remote.SignalR.Server/LinqToDBHub.cs` | Server base hub |
| `Source/LinqToDB.Remote.SignalR.Server/LinqToDBHub{T}.cs` | Server generic hub |
| `Source/LinqToDB.Remote.Wcf/WcfDataContext.cs` | Client entry point |
| `Source/LinqToDB.Remote.Wcf/WcfLinqService.cs` | Server implementation |
| `Source/LinqToDB.Remote.Wcf/WcfLinqServiceClient.cs` | Client `ILinqService` |
| `Source/LinqToDB.Remote.Wcf/IWcfLinqService.cs` | WCF service contract |

**Tier 2** (6 files — csproj files; read for TFM data):

| File | Read |
|---|---|
| `Source/LinqToDB.Remote.Grpc/LinqToDB.Remote.Grpc.csproj` | yes |
| `Source/LinqToDB.Remote.HttpClient.Client/LinqToDB.Remote.HttpClient.Client.csproj` | yes |
| `Source/LinqToDB.Remote.HttpClient.Server/LinqToDB.Remote.HttpClient.Server.csproj` | yes |
| `Source/LinqToDB.Remote.SignalR.Client/LinqToDB.Remote.SignalR.Client.csproj` | yes |
| `Source/LinqToDB.Remote.SignalR.Server/LinqToDB.Remote.SignalR.Server.csproj` | yes |
| `Source/LinqToDB.Remote.Wcf/LinqToDB.Remote.Wcf.csproj` | yes |

**Tier 3**: none (no `bin/`, `obj/`, or generated files matched).

## TFM matrix

| Package | TFMs |
|---|---|
| `linq2db.Remote.Grpc` | `net462;netstandard2.0;net8.0;net9.0;net10.0` (global default) |
| `linq2db.Remote.HttpClient.Client` | `net462;netstandard2.0;net8.0;net9.0;net10.0` (global default) |
| `linq2db.Remote.HttpClient.Server` | `net8.0;net9.0;net10.0` (ASP.NET Core only) |
| `linq2db.Remote.SignalR.Client` | `net462;netstandard2.0;net8.0;net9.0;net10.0` (global default) |
| `linq2db.Remote.SignalR.Server` | `net462;netstandard2.0;net8.0;net9.0;net10.0` (global default; needs `Microsoft.AspNetCore.SignalR.Core` pkg on pre-net8.0) |
| `linq2db.Remote.Wcf` | `net462` only |

## Inbound / outbound dependencies

**Inbound** (consumers of this area):
- User application code that installs one of these NuGet packages.

**Outbound** (dependencies this area requires):
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) — `ILinqService`, `ILinqService<T>`, `RemoteDataContextBase`, `LinqService`, `LinqService<T>`, `LinqServiceInfo`, `IDataContextFactory<T>`, `DataContextFactory<T>`. Every type in this area is defined against REMOTE-CLIENT contracts.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) — `LinqServiceSerializer` (owned by `Internal/Remote/`). Serializes `LinqServiceQuery` / `LinqServiceResult` to the string payload that all five `ILinqService` methods carry as `queryData` / return values. Transport packages pass the strings opaquely — they own none of the encoding.
- ASP.NET Core (`Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.SignalR`) — server packages only.
- `Grpc.Net.Client`, `protobuf-net.Grpc`, `protobuf-net` — gRPC package.
- `Microsoft.Extensions.Http` — HTTP client package.
- `System.ServiceModel` (framework ref) — WCF package (net462 BCL).

## Known issues / debt

- **`WcfLinqServiceClient.RemoteClientTag` Cyrillic typo.** `Source/LinqToDB.Remote.Wcf/WcfLinqServiceClient.cs:57`: the string `"Wсf"` contains a Cyrillic `с` (U+0441) instead of Latin `c`. No functional impact but breaks string equality if any code matches `"Wcf"`.
- **WCF cancellation is best-effort only.** `WcfLinqServiceClient` calls `cancellationToken.ThrowIfCancellationRequested()` before each method but cannot cancel in-flight WCF calls. This is a WCF protocol limitation.
- **`SignalRLinqServiceClient.DisposeAsync` is a no-op.** `Source/LinqToDB.Remote.SignalR.Client/SignalRLinqServiceClient.cs:58`: returns `default` (i.e. `ValueTask.CompletedTask`). The `HubConnection` lifetime is managed by `SignalRDataContext` (when it owns the client) or by the DI container.
- **SignalR server has no `ServiceConfigurationExtensions`.** Hub registration is left entirely to the user; there is no `AddLinqToDBHub` helper analogous to `AddLinqToDBController`. Inconsistency with HTTP transport.
- **gRPC creates a new channel per query.** `GrpcDataContext.GetClient()` (`Source/LinqToDB.Remote.Grpc/GrpcDataContext.cs:52-54`) opens a new `GrpcChannel` on every `GetClient()` call. `GrpcChannel` pooling at the transport level may mitigate this, but it is not explicit.
- **HTTP `CA2000` suppression.** `HttpClientDataContext.cs:46` suppresses `CA2000` (dispose before losing scope) for the `HttpClient` allocated when using the `(Uri, string)` constructor. The `HttpClient` is transferred to `HttpClientLinqServiceClient` but the analyzer cannot see that.

## See also

- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) — contracts and default implementations this area binds.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) — `LinqServiceSerializer` (wire format ownership).
- [EXTENSIONS-PKG](../EXTENSIONS-PKG/INDEX.md) — `AddLinqToDBService<TContext>` in `linq2db.Extensions` registers `ILinqService<TContext>` in DI for the server side.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 23 / 23 (all .cs files in area read in full)
- Tier 2 (visited / total): 6 / 6 (all csprojs read for TFM/package data)
- Tier 3 (skipped, logged): 0
</details>
