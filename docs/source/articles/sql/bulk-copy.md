---
uid: Bulk-Copy
---
# Bulk Copy (Bulk Insert)

Some database servers provide functionality to insert large amounts of data into a table in more effective way compared to conventional inserts. The downside of this method is that each server has its own view on how this functionality should work; there is no standard interface for it.

## Overview

To leverage the complexity of work with this operation, `LINQ To DB` provides a `BulkCopy` method. There are several overrides, but all of them do the same thing - take data and operation options, then perform inserts and return operation status. How insert operations are performed internally depends on the level of provider support and the provided options.

```cs
// DataConnectionExtensions.cs
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, IEnumerable<T> source)

BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, IEnumerable<T> source)
```

In addition, there are two asynchronous methods for each of the methods listed above; one accepting an `IEnumerable<T>`, and for .Net Standard, one accepting an `IAsyncEnumerable<T>`. Each method accepts an optional `CancellationToken` parameter to cancel the bulk copy operation. A few of the asynchronous signatures are listed below:

```cs
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken = default)
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataConnection dataConnection, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default) 
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, IEnumerable<T> source, CancellationToken cancellationToken = default)
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
```

## Insert methods and support by providers

`LINQ To DB` allows you to specify one of four insert methods (or three, as Default is not an actual method):

- `Default`. `LINQ To DB` will choose the method automatically, based on the provider used. Which method to use for a specific provider can be overriden using the `<PROVIDER_NAME>Tools.DefaultBulkCopyType` property.
- `RowByRow`. This method just iterates over a provided collection and inserts each record using separate SQL `INSERT` commands. This is the least effective method, but some providers support only this one.
- `MultipleRows`. Similar to `RowByRow`. Inserts multiple records at once using SQL `INSERT FROM SELECT` or similar batch insert commands. This one is faster than `RowByRow`, but is only available for providers that support such `INSERT` operations. If the method is not supported, LINQ To DB will silently fall back to the `RowByRow` implementation.
- `ProviderSpecific`. Most effective method, available only for a few providers. Uses provider specific functionality, usually not based on `SQL` and could have provider-specific limitations, like transactions support. If the method is not supported, LINQ To DB will silently fall back to the `MultipleRows` implementation.

Provider             | RowByRow | MultipleRows | ProviderSpecific | Default      | Notes
---------------------|----------|--------------|------------------|--------------|------
Microsoft Access     |   Yes    |      No      |        No        | MultipleRows | AccessTools.DefaultBulkCopyType
IBM DB2 (LUW, zOS)   |   Yes    |     Yes      |       Yes (will fallback to `MultipleRows` if called in transaction)        | MultipleRows | DB2Tools.DefaultBulkCopyType
Firebird             |   Yes    |     Yes      |        No        | MultipleRows | FirebirdTools.DefaultBulkCopyType
IBM Informix         |   Yes    |      No      |        Yes (when using IDS provider for DB2 or Informix. Will fallback to `MultipleRows` if called in transaction)        | ProviderSpecific | InformixTools.DefaultBulkCopyType
MySql / MariaDB      |   Yes    |     Yes      |        Yes       | MultipleRows | MySqlTools.DefaultBulkCopyType
Oracle               |   Yes    |     Yes      |       Yes        | MultipleRows | OracleTools.DefaultBulkCopyType
PostgreSQL           |   Yes    |     Yes      |       Yes (read important notes below)       | MultipleRows | PostgreSQLTools.DefaultBulkCopyType
SAP HANA             |   Yes    |      No      |       Yes        | MultipleRows | SapHanaTools.DefaultBulkCopyType
Microsoft SQL CE     |   Yes    |     Yes      |        No        | MultipleRows | SqlCeTools.DefaultBulkCopyType
SQLite               |   Yes    |     Yes      |        No        | MultipleRows | SQLiteTools.DefaultBulkCopyType
Microsoft SQL Server |   Yes    |     Yes      |       Yes        | ProviderSpecific | SqlServerTools.DefaultBulkCopyType
Sybase ASE           |   Yes    |     Yes      |        Yes (using native provider. Also [see](https://stackoverflow.com/questions/57675379))        | MultipleRows | SybaseTools.DefaultBulkCopyType
ClickHouse           |   Yes    |      Yes      |        Yes (except MySqlConnector)        | ProviderSpecific | ClickHouseTools.DefaultBulkCopyType

Note that when using the provider-specific insert method, only some providers support asynchronous operation; other providers will silently fall back to a synchronous operation.

Providers with async support:

- MySqlConnector
- Npgsql
- SAP HANA native provider
- System.Data.SqlClient and Microsoft.Data.SqlClient
- ClickHouse.Client and Octonica.ClickHouseClient

### PostgreSQL provider-specific bulk copy

For PostgreSQL, `BulkCopy` uses the `BINARY COPY` operation when the `ProviderSpecific` method specified. This operation is very sensitive to what types are used. You must always use the proper type that matches the type in target table, or you will receive an error from server (e.g. `"22P03: incorrect binary data format"`).

Below is a list of types that could result in error without an explicit type specification:

- `decimal`/`numeric` vs `money`. Those are two different types, mapped to `System.Decimal`. Default mappings will use the `numeric` type, so if your column is the `money` type, you should type it in mappings using `DataType = DataType.Money` or `DbType = "money"` hints.
- `time` vs `interval`. Those are two different types, mapped to `System.TimeSpan`. Default mappings will use the `time` type, so if your column is the `interval` type, you should type it in mappings using a `DbType = "interval"` hint. Or use the `NpgsqlTimeSpan` type for intervals.
- any text types/`json` vs `jsonb`. All those types are mapped to `System.String` (except `character` which is mapped to `System.Char`). Default mappings will not work for `jsonb` column and you should type it in mappings using `DataType = DataType.BinaryJson` or `DbType = "jsonb"` hints.
- `inet` vs `cidr`. If you use `NpgsqlInet` type for the mapping column, it could be mapped to both `inet` and 'cidr' types. There is no default mapping for this type, so you should explicitly specify it using `DbType = "inet"` or `DbType = "cidr"` hints. Also for `inet` you can use `IPAddress` which will be mapped to the `inet` type.
- `macaddr` vs `macaddr8`. Both types could be mapped to the same `PhysicalAddress`/`String` types, so you should explicitly specify the column type using `DbType = "macaddr"` or `DbType = "macaddr8"` hints. Even if you use a provider version without `macaddr8` support, you should specify the hint or it will break after the provider updates to a newer version.
- `date` type. You should use the `NpgsqlDate` type in mappings or specify `DataType = DataType.Date` or `DbType = "date"` hints.
- `time with time zone` type needs the `DbType = "time with time zone"` hint.

If you have issues with other types, feel free to create an issue.

## Options

See [BulkCopyOptions](xref:linq2db::LinqToDB.Data.BulkCopyOptions) properties and support per-provider. Some options explained below.

### `KeepIdentity` option (default : `false`)

This option allows you to insert provided values into the identity column. It is supported by limited set of providers and is not compatible with `RowByRow` mode. Hence, if the provider doesn't support any other insert mode, the `KeepIdentity` option is not supported.

This option is not supported for `RowByRow` because corresponding functionality is not implemented by `LINQ To DB`; it could be added upon request.

If you will set this option to `true` for an unsupported mode or provider, you will get a `LinqToDBException`.

Provider             | Support
---------------------|----------
Microsoft Access     |   No
IBM DB2 (LUW, zOS)   |   Only for GENERATED BY DEFAULT columns
Firebird             |   No (you need to disable triggers manually, if you use generators in triggers)
IBM Informix         |   No
MySql / MariaDB      |   Yes
Oracle               |   Partial. Starting from version 12c it will work for GENERATED BY DEFAULT columns (as DB2), for earlier versions you need to disable triggers with generators (as Firebird). Note that for versions prior to 12c, no exception will be thrown if you will try to use it with `KeepIdentity` set to `true` and generated values will be used silently as `LINQ To DB` don't have Oracle version detection right now. This could be changed in future.
PostgreSQL           |   Yes
SAP HANA             |   Depends on provider version (HANA 2 only?)
Microsoft SQL CE     |   Yes
SQLite               |   Yes
Microsoft SQL Server |   Yes
Sybase ASE           |   Yes
ClickHouse           |   No (ClickHouse doesn't have identity/sequence concept)

### `MaxDegreeOfParallelism` Option (default: `null`)

Supported only by `ClickHouse.Client` provider and defines number of parallel connections to use for insert. Note that behavior depends on provider implementation, which currently use 4 parallel connections by default and perform parallel insert only for batches larger than 100K records.

### `WithoutSession` Option (default: `false`)

Supported only by `ClickHouse.Client` provider. When enabled, provider will use session-less connection even if `linq2db` connection configured to use sessions. Note that:

- session-less connection requied for parallel inserts (see `MaxDegreeOfParallelism` option)
- session-less connections doesn't support temporary tables, so you cannot insert into temporary table with this option set to `true`

## See Also

As an alternative to `BulkCopy`, a [Merge](xref:Merge) operation could be used. It allows more flexibility but is not available for some providers and will be always slower than `BulkCopy` with native provider support.
