---
uid: Bulk-Copy
---
# Bulk Copy (Bulk Insert)

Some database servers provide functionality to quickly insert large amount of data into table. Downside of this method is that each server has it's own view on how this functionality should work and there is no standard interface to it.

## Overview

To leverage complexity of work with this operation, `LINQ To DB` provides `BulkCopy` method. There are several overrides of it, but all they do the same - take data and operation options, perform insert and return operation status. How insert operation performed internally depends on provider support level and provided options.

```cs
// DataConnectionExtensions.cs
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, IEnumerable<T> source)

BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source)
BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, IEnumerable<T> source)
```

## Insert methods and support by providers

`LINQ To DB` allows you to specify one of four insert methods (or three, as Default is not an actual method):

- `Default`. `LINQ To DB` will choose method automatically, based on used provider. Which method to use for specific provider could be overriden using `<PROVIDER_NAME>Tools.DefaultBulkCopyType` property.
- `RowByRow`. This method just iterate over provided collection and insert each record using separate SQL `INSERT` command. Least effective method, but some providers support only this one.
- `MultipleRows`. Similar to `RowByRow`. Inserts multiple records at once using SQL `INSERT FROM SELECT` or similar batch insert command. This one is faster than `RowByRow`, but available only for providers that support such `INSERT` operation. If method is not supported, LINQ To DB silently fallback to `RowByRow` implementation.
- `ProviderSpecific`. Most effective method, available only for few providers. Uses provider specific functionality, usually not based on `SQL` and could have provider-specific limitations, like transactions support. If method is not supported, LINQ To DB silently fallback to `MultipleRows` implementation.

Provider             | RowByRow | MultipleRows | ProviderSpecific | Default      | Notes
---------------------|----------|--------------|------------------|--------------|------
Microsoft Access     |   Yes    |      No      |        No        | MultipleRows | AccessTools.DefaultBulkCopyType
IBM DB2 (LUW, zOS)   |   Yes    |     Yes      |       Yes (will fallback to `MultipleRows` if called in transaction)        | MultipleRows | DB2Tools.DefaultBulkCopyType
Firebird             |   Yes    |     Yes      |        No        | MultipleRows | FirebirdTools.DefaultBulkCopyType
IBM Informix         |   Yes    |      No      |        No        | MultipleRows | InformixTools.DefaultBulkCopyType
MySql / MariaDB      |   Yes    |     Yes      |        No        | MultipleRows | MySqlTools.DefaultBulkCopyType
Oracle               |   Yes    |     Yes      |       Yes (will fallback to `MultipleRows` if called in transaction)        | MultipleRows | OracleTools.DefaultBulkCopyType
PostgreSQL           |   Yes    |     Yes      |       Yes (read important notes below)       | MultipleRows | PostgreSQLTools.DefaultBulkCopyType
SAP HANA             |   Yes    |      No      |       Yes        | MultipleRows | SapHanaTools.DefaultBulkCopyType
Microsoft SQL CE     |   Yes    |     Yes      |        No        | MultipleRows | SqlCeTools.DefaultBulkCopyType
SQLite               |   Yes    |     Yes      |        No        | MultipleRows | SQLiteTools.DefaultBulkCopyType
Microsoft SQL Server |   Yes    |     Yes      |       Yes        | ProviderSpecific | SqlServerTools.DefaultBulkCopyType
Sybase ASE           |   Yes    |     Yes      |        No        | MultipleRows | SybaseTools.DefaultBulkCopyType

### PostgreSQL provider-specific bulk copy

For PostgreSQL `BulkCopy` use `BINARY COPY` operation when `ProviderSpecific` method specified. This operation is very sensitive to what types are used. You must always use proper type that match type in target table, or you will receive error from server (e.g. `"22P03: incorrect binary data format"`).

Below is a list of types, that could result in error without explicit type specification:

- `decimal`/`numeric` vs `money`. Those are two different types, mapped to `System.Decimal`. Default mappings will use `numeric` type, so if your column has `money` type, you should type it in mapping using `DataType = DataType.Money` or `DbType = "money"` hints.
- `time` vs `interval`. Those are two different types, mapped to `System.TimeSpan`. Default mappings will use `time` type, so if your column has `interval` type, you should type it in mapping using `DbType = "interval"` hint. Or use `NpgsqlTimeSpan` type for intervals.
- any text types/`json` vs `jsonb`. All those types mapped to `System.String` (except `character` which is mapped to `System.Char`). Default mappings will not work for `jsonb` column and you should type it in mapping using `DataType = DataType.BinaryJson` or `DbType = "jsonb"` hint.
- `inet` vs `cidr`. If you use `NpgsqlInet` type for mapping column, it could be mapped to both `inet` and 'cidr' types. There is no default mapping for this type, so you should explicitly specify it using `DbType = "inet"` or `DbType = "cidr"` hint. Also for `inet` you can use `IPAddress` which will be mapped to `inet` type.
- `macaddr` vs `macaddr8`. Both types could be mapped to the same `PhysicalAddress`/`String` types, so you should explicitly specify column type using `DbType = "macaddr"` or `DbType = "macaddr8"` hints. Even if you use provider version without `macaddr8` support, you should specify hint or it will break after provider update to newer version.
- `date` type. You should use `NpgsqlDate` type in mapping or specify `DataType = DataType.Date` or `DbType = "date"` hints.
- `time with time zone` type needs `DbType = "time with time zone"` hint.

If you have issues with other types, feel free to create an issue.

## Options

See [BulkCopyOptions](xref:LinqToDB.Data.BulkCopyOptions) properties and support per-provider

## `KeepIdentity` option (default : `false`)

This option allows to insert provided values into identity column. It is supported by limited set of providers and is not compatible with `RowByRow` mode. Latter means that if provider doesn't support any other insert mode, `KeepIdentity` option is not supported too.

This option is not supported for `RowByRow` because corresponding functionality is not implemented by `LINQ To DB` and could be added on request.

If you will set this option to `true` for unsupported mode or provider, you will get `LinqToDBException`.

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

## See Also

As an alternative to `BulkCopy`, [Merge](xref:Merge) operation could be used. It allows more flexibility but not available for some providers and will be always slower than `BulkCopy` with native provider support.