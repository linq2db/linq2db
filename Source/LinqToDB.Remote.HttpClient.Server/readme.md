# LINQ to DB Remote Data Context Over HttpClient<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required server classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over HttpClient transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\HttpClient).

## Other Transports

We provide
[gRPC support](https://www.nuget.org/packages/linq2db.Remote.gRPC),
[Signal/R Client support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Client) and
[Signal/R Server support](https://www.nuget.org/packages/linq2db.Remote.SignalR.Server),
[HttpClient.Client support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Client) and
[HttpClient.Server support](https://www.nuget.org/packages/linq2db.Remote.HttpClient.Server),
and [WCF support](https://www.nuget.org/packages/linq2db.Remote.Wcf) (.NET Framework only currently).

If you need Remote Context support over other types of transport, you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
