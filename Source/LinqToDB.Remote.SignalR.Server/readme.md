# LINQ to DB Remote Data Context Over Signal/R<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required server classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over Signal/R transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\SignalR).

## Usage

```csharp
builder.Services
    .AddLinqToDBService<IMyDataContext>()
    .AddSignalR()
    // The largest request the hub accepts (Signal/R default: 32 KB). A larger message closes the client's whole
    // connection, so size it to the largest expected query (long string parameters, large Contains lists, batches).
    // .NET 8+ servers report it to the client, which refuses larger requests before sending them.
    .AddHubOptions<LinqToDBHub<IMyDataContext>>(o => o.MaximumReceiveMessageSize = 1024 * 1024);

builder.Services.Configure<LinqToDBHubOptions>(o =>
{
    // Calls of one client connection running at the same time. By default it follows
    // HubOptions.MaximumParallelInvocationsPerClient (1 unless set). A client usually shares one connection
    // between all its contexts, so 8 is a good starting point.
    o.MaxConcurrentCallsPerConnection = 8;
    // Optional limit across all connections.
    // o.MaxConcurrentCalls = 64;
    // Send server error text to the client (.NET 8+ servers only; see below).
    // o.TransferInternalExceptionToClient = true;
});

app.MapHub<LinqToDBHub<IMyDataContext>>("/hub/linq2db");
```

Every call is a Signal/R streaming invocation:

- Cancelling a query on the client cancels it on the server: while the command executes through the provider's
  cancellation, while rows are read at the next row. Serializing and sending a result that was already read is not
  interrupted.
- A lost connection cancels its calls the same way. Calls are never replayed: a call whose connection was lost fails
  on the client.
- Large results are sent in chunks of 32K characters.

To validate or authorize calls, decorate the `ILinqService` the hub uses (`ILinqService<T>` in DI, or
`CreateLinqService()` in a derived hub). Class-level `[Authorize]` on a derived hub applies to all its methods.

A hub derived from `LinqToDBHub` or `LinqToDBHub<T>` that does not pass `IOptions<LinqToDBHubOptions>` to its base
constructor reads the registered options from the connection's request services. The legacy (.NET Framework /
.NET Standard) server has none for long polling connections, where such a hub uses the default options; pass the
options to the base constructor to have them on every transport.

`TransferInternalExceptionToClient` works on .NET 8+ servers. The legacy (.NET Framework / .NET Standard) Signal/R
server drops error text unless `HubOptions.EnableDetailedErrors` is set, which exposes the errors of all hub methods.

Client and server packages must both be 6.6.0 or later: the hub protocol changed in 6.6.0, and older clients
are not supported.

## Other Transports

We provide
[gRPC support](https://www.nuget.org/packages/linq2db.Remote.gRPC),
[Signal/R Client support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Client) and
[Signal/R Server support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Server),
[HttpClient.Client support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Client) and
[HttpClient.Server support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Server),
and [WCF support](https://www.nuget.org/packages/linq2db.Remote.Wcf) (.NET Framework only currently).

If you need Remote Context support over other types of transport, you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
