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

All contexts of one registration share one `LinqToDBSignalRConnection`; each registered context type has a connection
of its own, so two context types can use two hubs. The first query starts it; a query after the connection was lost
(a failed start, reconnect attempts exhausted, or a dropped connection on .NET Framework / .NET Standard, whose client
does not reconnect) starts it again. `InitSignalRAsync<T>()` does the same up front and is optional.

With your own `HubConnection`:

```csharp
// .NET 10+; with the 1.x client (.NET Framework, .NET Standard, .NET 8/9) HubConnection is not IAsyncDisposable:
// call DisposeAsync() yourself.
await using var connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
await connection.StartAsync();

await using (var db = new SignalRDataContext(connection))
{
    // ...
}
```

`SignalRDataContext(HubConnection)` leaves the connection to the caller, so one connection can serve any number of
contexts. Use `SignalRDataContext(connection, disposeHubConnection: true)` to hand it over to the context.

On .NET Framework / .NET Standard, and in .NET 8 and .NET 9 applications (which get the .NET Standard build of this
package), it is built against the 1.x Signal/R client it depends on, and works with that version only: from 3.0 on,
`HubConnection` is not binary compatible with it (`DisposeAsync` returns `ValueTask`), so keep
`Microsoft.AspNetCore.SignalR.Client` at 1.x there. What this readme says about .NET Framework / .NET Standard applies
to .NET 8 and .NET 9 applications too: no size check before sending, no automatic reconnect.

Cancelling a query (the `CancellationToken` of `ToListAsync` and other async methods) cancels it on the server: a
running command is cancelled where the database provider supports it, and reading the result stops at the next row.
Calls are never re-sent after a lost connection. Synchronous APIs block a thread, which Blazor
WebAssembly does not support: use the async ones there.

Use the same linq2db version on the client and the server.

## Other Transports

We provide
[gRPC support](https://www.nuget.org/packages/linq2db.Remote.gRPC),
[Signal/R Client support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Client) and
[Signal/R Server support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Server),
[HttpClient.Client support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Client) and
[HttpClient.Server support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Server),
and [WCF support](https://www.nuget.org/packages/linq2db.Remote.Wcf) (.NET Framework only currently).

If you need Remote Context support over other types of transport, you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
