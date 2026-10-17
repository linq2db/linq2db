# LINQ to DB Remote Data Context Over gRPC<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required server and client classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over gRPC transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\Grpc).

## Client: connections and channels

A `GrpcChannel` is thread-safe and meant to be long-lived. Create one per server, share it between all data contexts
and pass it to the `GrpcDataContext(GrpcChannel channel, ...)` constructor: every context created over it then reuses
the channel's connections. The channel stays yours - contexts never dispose it - so dispose it only when no context
uses it any more (usually at application shutdown).

```cs
// once, e.g. a singleton in your DI container
var channel = GrpcChannel.ForAddress("https://my-server:5001");

// per unit of work
await using var db = new GrpcDataContext(channel);
```

A context created from an address (`GrpcDataContext(string address, ...)`) creates its own channel on first use and
disposes it with the context, so every context opens its own connection (TCP and, over https, a TLS handshake).
That is fine for a few long-lived contexts; for many short-lived ones, share a channel as above.

Things to watch in `GrpcChannelOptions`:

- `HttpHandler = new SocketsHttpHandler()` does **not** share connections between channels, even when the same handler
  instance is passed to all of them: Grpc.Net.Client takes a `SocketsHttpHandler` over for its client-side load
  balancing and every channel opens its own connections through it. Connections are shared between channels only
  through a shared `HttpClient`, or a shared handler of another type (for example `HttpClientHandler`).
- `HttpClient.Timeout` limits every remote query made through that client, including long-running ones; its default
  is 100 seconds. To run longer queries, set it to `Timeout.InfiniteTimeSpan` and use cancellation tokens instead. A
  query ended by the timeout fails with `RpcException` (`StatusCode.Cancelled`), while a query cancelled through its
  own cancellation token throws `OperationCanceledException`.
- Do not set `DisposeHttpClient = true` in options used by several contexts: the first context to be disposed would
  dispose the shared `HttpClient` for all of them.

## Other Transports

We provide
[gRPC support](https://www.nuget.org/packages/linq2db.Remote.gRPC),
[Signal/R Client support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Client) and
[Signal/R Server support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Server),
[HttpClient.Client support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Client) and
[HttpClient.Server support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Server),
and [WCF support](https://www.nuget.org/packages/linq2db.Remote.Wcf) (.NET Framework only currently).

If you need Remote Context support over other types of transport, you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
