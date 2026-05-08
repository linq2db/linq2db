- [Interceptors Migration](#migration-to-interceptors)
- [ADO.NET Interfaces Removal](#migration-from-adonet-interfaces)
- [Obsoleted code removal](#code-removals)
- [Provider-Specific changes](#provider-specific-changes)
  - [Firebird](#firebird)

If you cannot find answer to your migration-related question here, ask it [here](https://github.com/linq2db/linq2db/discussions) and we will update document with missing details.

### T4 Changes

Due to changes to `DataConnection` API existing T4 models could produce compilation errors if stored procedures were generated for model. To resolve it you need to re-generate model using T4 templates.

### Migration to Interceptors

In v4 release we replace multiple existing events, delegates, properties and static settings that piled up along time with iterceptors infrastructure. Interceptor is an instance of class, that implements one or multiple interceptor interfaces with events. It is registered in `Linq To DB` (usually in `DataConnection` or `DataContext` instance) and called when one of events, defined on interceptor is triggered.

Supporting interceptors on `DataContext` level also allows user to use events, previously available only on `DataConnection` level.

For basic information and list of implemented interceptors check [release notes](https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap#interceptors) frist.

#### Functionality, replaced by interceptors

Note that some functionality, provided by interceptors previously was available only on specific implementation (usually `DataConnection`). With interceptors applied to all types of contexts now you can use it also with other contexts like `DataConext`.

| Removed | Replacement | Notes |
|:-|:-|:-|
| `DataConnection.OnBeforeConnectionOpen` event | `IConnectionInterceptor.ConnectionOpening` interceptor ||
| `DataConnection.OnBeforeConnectionOpenAsync` event | `IConnectionInterceptor.ConnectionOpeningAsync` interceptor ||
| `DataConnection.OnConnectionOpened` event | `IConnectionInterceptor.ConnectionOpened` interceptor ||
| `DataConnection.OnConnectionOpenedAsync` event | `IConnectionInterceptor.ConnectionOpenedAsync` interceptor ||
|`DataConnection.OnClosed` event|`IDataContextInterceptor.OnClosed` interceptor<br/>`IDataContextInterceptor.OnClosedAsync` interceptor||
|`IDataContext.OnClosing` event|`IDataContextInterceptor.OnClosing` interceptor<br/>`IDataContextInterceptor.OnClosingAsync` interceptor||
| `DataConnection.LastParameters` field | `ICommandInterceptor.CommandInitialized` interceptor<br />or for one-time use<br/>`OnNextCommandInitialized` method |As this field used excessively by T4 for stored procedure wrappers, T4 model requires regeneration |
| `DataConnection.Command` field | `ICommandInterceptor.CommandInitialized` interceptor<br />or for one-time use<br/>`OnNextCommandInitialized` method ||
|`IDbCommandProcessor` interface<br/>+`DbCommandProcessorExtensions` class|`ICommandInterceptor` interceptor||
|`IEntityServices` interface<br/> +`EntityCreatedEventArgs` class|`IEntityServiceInterceptor.EntityCreated` interceptor||
|`MiniProfiler` (or other wrapper) converters registration in `MappingSchema`|`IUnwrapDataObjectInterceptor` interceptor||

### Migration from ADO.NET interfaces

`Linq To DB` replaced support of ADO.NET interfaces (listed below) with corresponding asbstract classes. Technically, even in v3 you can work with those interfaces only if they belong to class, inherited from corresponding abstract class.
- `IDataReader` and `IDataRecord` -> `DbDataReader` class
- `IDataRecord` -> `DbDataReader`
- `IDbCommand` -> `DbCommand`
- `IDbDataParameter` -> `DbParameter`
- `IDbConnection` -> `DbConnection`
- `IDbTransaction` -> `IDbTransaction`

In most of cases compiler will give you error if you try to pass interface instead of class, but there are several cases where you need to change your code.

##### Connection wrappers (e.g. MiniProfiler) configuration
If you used `MiniProfiler` or any other tool that use connection wrappers, you need to update your integration code.
Previously integration were done using convert expressions registration in mapping schema. Now you should use `IUnwrapDataObjectInterceptor` for it.

```cs
// V3: MiniProfiler unwrap converters registration
MappingSchema.SetConvertExpression<ProfiledDbConnection,  IDbConnection> (db => db.WrappedConnection);
MappingSchema.SetConvertExpression<ProfiledDbDataReader,  IDataReader>   (db => db.WrappedReader);
MappingSchema.SetConvertExpression<ProfiledDbTransaction, IDbTransaction>(db => db.WrappedTransaction);
MappingSchema.SetConvertExpression<ProfiledDbCommand,     IDbCommand>    (db => db.InternalCommand);

// V4: interceptor
public class UnwrapProfilerInterceptor : UnwrapDataObjectInterceptor
{
    // as interceptor is thread-safe, we will create
    // and use single instance of it
    public static readonly IInterceptor Instance = new UnwrapProfilerInterceptor();

	public override DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection)
	{
		return connection is ProfiledDbConnection c ? c.WrappedConnection : connection;
	}

	public override DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
	{
		return transaction is ProfiledDbTransaction t ? t.WrappedTransaction : transaction;
	}

	public override DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
	{
		return command is ProfiledDbCommand c ? c.InternalCommand : command;
	}

	public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
	{
		return dataReader is ProfiledDbDataReader dr ? dr.WrappedReader : dataReader;
	}
}

// check interceptors documentation to find out alternative ways to register interceptor
dataConnection.AddInterceptor(UnwrapProfilerInterceptor.Instance);
```

##### Custom data reader expressions
If you had custom data reader expressions configured on data provider, you need to replace uses of `IDataReader` with `DbDataReader`
```cs
// V3
provider.SetProviderField<IDataReader, TimeSpan,DateTime>(
    (r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
// V4
provider.SetProviderField<DbDataReader, TimeSpan,DateTime>(
    (r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));

// V3
provider.SetToType<IDataReader,sbyte,int>("INTEGER", (r, i) => unchecked((sbyte)r.GetInt32(i)));
// V4
provider.SetToType<DbDataReader,sbyte,int>("INTEGER", (r, i) => unchecked((sbyte)r.GetInt32(i)));

// V3
provider.SetField<IDataReader,long>((r,i) => r.GetInt64(i));
// V4
provider.SetField<DbDataReader,long>((r,i) => r.GetInt64(i));
```

### Code removals

- `bool Configuration.Linq.AllowMultipleQuery` setting removal: this setting had no effect in v3 already so you can just remove code that access it
- `Task DataConnection.CloseAsync(CancellationTask)` method removal: call parameter-less `CloseAsync()` method (close operation doesn't support cancellation)
- `Task DataConnection.DisposeAsync(CancellationTask)` method removal: call parameter-less `DisposeAsync()` method (dispose operation doesn't support cancellation)
- `DataTools.GetChar` field removal: use `Expression<>`-typed field `DataTools.GetCharExpression` instead (`GetChar` field wasn't used in v3 already)
- `OracleTools.DataReaderGetDecimal` field removal: this field wasn't used in v3 already so you can just remove code that access it
- `SqlServerTools.DataReaderGetMoney` field removal: this field wasn't used in v3 already so you can just remove code that access it
- `SqlServerTools.DataReaderGetDecimal` field removal: this field wasn't used in v3 already so you can just remove code that access it
- `DataContext.BeginTransaction` and `DataContext.BeginTransactionAsync()` methods got rid of never used `bool autoCommitOnDispose` parameter

#### Removal of `BulkCopy` helpers on `<DB>Tools` classes

Those methods were just opionated shortcuts to `DataConnection.BulkCopy` method and to fix your code you just need to call it instead.

<details> 
<summary>CLICK TO SEE MIGRATION CODE</summary>
<p>


```cs
// AccessTools.MultipleRowsCopy =>
// FirebirdTools.MultipleRowsCopy =>
// MySqlTools.MultipleRowsCopy =>
// OracleTools.MultipleRowsCopy =>
// PostgreSQLTools.MultipleRowsCopy =>
// SQLiteTools.MultipleRowsCopy =>
// SqlCeTools.MultipleRowsCopy =>
// SybaseTools.MultipleRowsCopy =>
dataConnection.BulkCopy(
  new BulkCopyOptions
  {
    BulkCopyType       = BulkCopyType.MultipleRows,
    MaxBatchSize       = maxBatchSize, // default was 1000
    RowsCopiedCallback = rowsCopiedCallback, // default was null
  }, source);

// DB2Tools.MultipleRowsCopy =>
// InformixTools.MultipleRowsCopy =>
dataConnection.BulkCopy(
  new BulkCopyOptions
  {
    BulkCopyType       = BulkCopyType.ProviderSpecific, // right, ProviderSpecific used
    MaxBatchSize       = maxBatchSize, // default was 1000
    RowsCopiedCallback = rowsCopiedCallback, // default was null
  }, source);

// DB2Tools.ProviderSpecificBulkCopy =>
dataConnection.BulkCopy(
  new BulkCopyOptions
  {
    BulkCopyType       = BulkCopyType.ProviderSpecific,
    BulkCopyTimeout    = bulkCopyTimeout, // default was null
    KeepIdentity       = keepIdentity, // default was false
    NotifyAfter        = notifyAfter, // default was 0
    RowsCopiedCallback = rowsCopiedCallback, // default was null
  }, source);

// OracleTools.ProviderSpecificBulkCopy =>
dataConnection.BulkCopy(
  new BulkCopyOptions
  {
    BulkCopyType       = BulkCopyType.ProviderSpecific,
    MaxBatchSize       = maxBatchSize, // default was null
    BulkCopyTimeout    = bulkCopyTimeout, // default was null
    NotifyAfter        = notifyAfter, // default was 0
    RowsCopiedCallback = rowsCopiedCallback, // default was null
  }, source);

// SqlServerTools.ProviderSpecificBulkCopy =>
dataConnection.BulkCopy(
  new BulkCopyOptions
  {
    BulkCopyType       = BulkCopyType.ProviderSpecific,
    MaxBatchSize       = maxBatchSize, // default was null
    BulkCopyTimeout    = bulkCopyTimeout, // default was null
    KeepIdentity       = keepIdentity, // default was false
    CheckConstraints   = checkConstraints, // default was false
    NotifyAfter        = notifyAfter, // default was 0
    RowsCopiedCallback = rowsCopiedCallback, // default was null
  }, source);
```

</p></details>

### Provider-Specific Changes

#### Firebird

##### Guid to UUID default mapping

V4 introduces default mapping of `Guid` type to `UUID` type (represented by `CHAR(16) CHARACTER SET OCTETS` type). It will affect existing applications that used `Guid`-typed columns, mapped to `CHAR(38)`, especially in queries with inlined parameters (as SQL literals), as it will start to generate binary literal instead.

To update your application you have several options

###### Specify `DataType` for string-types `Guid` columns

You need to specify `DataType = DataType.Char` or `DataType = DataType.NChar` in column mapping:

```cs
// specify explicit DataType using attributes
[Table]
public class MyTable
{
    // map to fixed-length text database type
    [Column(DataType = DataType.Char)]
    public Guid GuidAsStringColumn1;

    // map to fixed-length text database type
    [Column(DataType = DataType.NChar)]
    public Guid GuidAsStringColumn2;

    // explicit mapping to UUID
    [Column(DataType = DataType.Guid)]
    public Guid GuidAsUUID1;

    // default mapping to UUID
    [Column]
    public Guid GuidAsUUID2;
}

// or using fluent mapping
ms.GetFluentMappingBuilder()
.Entity<MyTable>()
    .Property(e => e.GuidAsStringColumn1).HasDataType(DataType.Char)
    .Property(e => e.GuidAsStringColumn2).HasDataType(DataType.NChar)
    .Property(e => e.GuidAsUUID1).HasDataType(DataType.Guid)
    .Property(e => e.GuidAsUUID2)
```

If your model generated using T4 templates and doesn't have `DataType` generated, you need to enable following T4 option and re-generate your model:
```cs
GenerateDataTypes = true;
```

###### Restore old behavior for all Guid columns by default

Another option is to change default `Guid` `DataType` using following code:

```cs
// remap Guid to Char (or NChar)
ms.SetDataType(typeof(Guid), DataType.Char);
ms.SetDataType(typeof(Guid?), DataType.Char);
```

With this change `Linq To DB` will not use UUID type for `Guid` if you not specify `DataType.Guid` for column explicitly.