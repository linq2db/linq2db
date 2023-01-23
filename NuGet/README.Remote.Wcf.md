# LINQ to DB Remote Data Context Over WCF<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

## About

This package provides required server and client classes to query database from remote client using [Linq To DB](https://github.com/linq2db/linq2db) library over WCF transport.

You can find working example [here](https://github.com/linq2db/linq2db/tree/master/Examples\Remote\Wcf).

## Support and other transports

Currently we support only .NET Framework client and server for WCF and recommend to use [linq2db.Remote.Grpc](https://www.nuget.org/packages/linq2db.Remote.Grpc) for modern code.

If you need Remote Context support over WCF in .NET Core or other type of transport (not WCF or gRPC), you can create [feature request](https://github.com/linq2db/linq2db/issues/new) or send PR with transport implementation.
