# LINQ to DB Remote Data Context Over gRPC<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required server and client classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over gRPC transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\Grpc).

## Other Transports

We also provide [WCF support](https://www.nuget.org/packages/linq2db.Remote.Wcf) (.NET Framework only currently).

If you need Remote Context support over other types of transport (not WCF or gRPC), you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
