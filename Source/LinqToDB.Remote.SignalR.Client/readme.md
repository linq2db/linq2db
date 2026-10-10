# LINQ to DB Remote Data Context Over Signal/R<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required client classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over Signal/R transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\SignalR).

## Usage

With dependency injection (Blazor WebAssembly and other client applications):

```csharp
builder.Services.AddLinqToDBSignalRDataContext<IMyDataContext>(
    new Uri(new Uri(builder.HostEnvironment.BaseAddress), "/hub/linq2db"),
    client => new MyDataContext(client),
    options => options.ConfigureHttpConnection = http => http.AccessTokenProvider = GetAccessTokenAsync);
```

All contexts share one `LinqToDBSignalRConnection`. The first query starts it; a query after the connection was lost
(a failed start, reconnect attempts exhausted, or a dropped connection on .NET Framework / .NET Standard, whose client
does not reconnect) starts it again. `InitSignalRAsync<T>()` does the same up front and is optional.

With your own `HubConnection`:

```csharp
// .NET 8+; on .NET Framework / .NET Standard HubConnection is not IAsyncDisposable: call DisposeAsync() yourself.
await using var connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
await connection.StartAsync();

await using (var db = new SignalRDataContext(connection))
{
    // ...
}
```

`SignalRDataContext(HubConnection)` leaves the connection to the caller, so one connection can serve any number of
contexts. Use `SignalRDataContext(connection, disposeHubConnection: true)` to hand it over to the context.

Cancelling a query (the `CancellationToken` of `ToListAsync` and other async methods) cancels it on the server and in
the database. Calls are never re-sent after a lost connection. Synchronous APIs block a thread, which Blazor
WebAssembly does not support: use the async ones there.

Client and server packages must both be 6.6.0 or later: the hub protocol changed in 6.6.0, and older servers
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
