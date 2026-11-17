---
uid: Interceptors
---

# Interceptors<!-- omit in toc -->

This API available since `Linq To DB` 4.0.0

- [Introduction](#introduction)
- [Interceptors](#interceptors-1)
  - [`IUnwrapDataObjectInterceptor`](#iunwrapdataobjectinterceptor)
  - [`IEntityServiceInterceptor`](#ientityserviceinterceptor)
  - [`IDataContextInterceptor`](#idatacontextinterceptor)
  - [`ICommandInterceptor`](#icommandinterceptor)
  - [`IConnectionInterceptor`](#iconnectioninterceptor)
  - [`IExceptionInterceptor`](#iexceptioninterceptor)
- [Interceptors Registration](#interceptors-registration)
  - [Interceptors support per context](#interceptors-support-per-context)
- [Migration](#migration)

## Introduction

Interceptors represent an generic exensibility mechanism that allows users to attach custom logic at different places of `Linq To DB` execution pipeline.

In prior versions `Linq To DB` used assorted set of events, properties, delegates, virtual methods and interfaces with multiple usability issues:

- it wasn't an easy task to find specific extension point due to scarse documentation, absense of single extensibility mechanism and lack of common approach
- some extension points were not synchronized between `IDataContext` implementations. E.g. you can have some extension point on `DataConnection`, but not on `DataContext` and vice versa.

New mechanim provides following exensibility infrastructure:

- set of interceptor interfaces which group extension points logically;
- base interceptor implementation classes without added functionality. They could be used by user for own interceptor implementation. Just inherit from those classes and override required interceptor methods (also it is possible to implement interceptor interface directly);
- single mechanism for interceptors registration in `IDataContext` implementations (`DataConnection`, `DataContext`, `RemoteContext`);
- interceptors registration using `DataOptions` conntection configuration object (including required fluent configuration APIs);
- single source of documentation (this document).

Note that interceptors replace old extensibility mechanims, which means you may need to migrate your existing code to interceptors if you used them. For migration notes check [migration notes](#migration) section below.

We can add more interceptors in future (e.g. on user request).

## Interceptors

List of implemented interceptor interfaces:

- [`IUnwrapDataObjectInterceptor`](#iunwrapdataobjectinterceptor) : provides compatibility layer for connection wrappers, e.g. [MiniProfiler](https://miniprofiler.com/dotnet/)
- [`IEntityServiceInterceptor`](#ientityserviceinterceptor) : interceptor for entity creation on query materialization
- [`IDataContextInterceptor`](#idatacontextinterceptor) : interceptor for data context services
- [`ICommandInterceptor`](#icommandinterceptor) : `DbCommand` methods calls interceptor
- [`IConnectionInterceptor`](#iconnectioninterceptor) : `DbConnection` methods calls interceptor


### `IUnwrapDataObjectInterceptor`

Base abstract class: `UnwrapDataObjectInterceptor`.

This interceptor used by `Linq To DB` to access underlying ADO.NET provider objects when they are wrapped by non-provider classes. Most know example of such wrapper is [MiniProfiler](https://miniprofiler.com/dotnet/). It wraps native ADO.NET objects with profiling classes and  `Linq To DB` uses this interceptor to access underlying objects to call provider-specific functionality, not awailable on wrapper directly.

Interceptor methods:

```cs
// access underlying connection object
DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection);
// access underlying transaction object
DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction);
// access underlying command object
DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command);
// access underlying data reader object
DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader);
```

Example of  `MiniProfiler`-based interceptor implementation:

```cs
// MiniProfiler unwrap interceptor
public class MiniProfilerInterceptor : UnwrapDataObjectInterceptor
{
    // as interceptor is thread-safe, we will create
    // and use single instance
    public static readonly IInterceptor Instance = new MiniProfilerInterceptor();

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
        return command is ProfiledDbCommand c ? c.WrappedCommand : command;
    }

    public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
    {
        return dataReader is ProfiledDbDataReader dr ? dr.WrappedReader : dataReader;
    }
}
```

### `IEntityServiceInterceptor`

Base abstract class: `UnwrapDataObjectInterceptor`.

This interceptor provides access to entity materialization event during query execution.

Interceptor methods:

```cs
// triggered for new entity object (only for imlicit record materialization).
//
// Example 1:
// table.ToList();
// No explicit entity constructor call: event will be triggered for each materialized table record
//
// Example 2:
// table.Select(r => new SomeEntity() { Field1 = r.Field1 }).ToList();
// Query contains explicit entity constructor call: no event will be triggered
object EntityCreated(EntityCreatedEventData eventData, object entity);

// event arguments
struct EntityCreatedEventData
{
    public IDataContext Context      { get; }
    public TableOptions TableOptions { get; }
    public string?      TableName    { get; }
    public string?      SchemaName   { get; }
    public string?      DatabaseName { get; }
    public string?      ServerName   { get; }
}
```

### `IDataContextInterceptor`

Base abstract class: `DataContextInterceptor`.

This interceptor provides access to events and operations associated with database context.

Interceptor methods:

```cs
// triggered before data context instance `Close/CloseAsync` method execution
void OnClosing(DataContextEventData eventData);
Task OnClosingAsync(DataContextEventData eventData);

// triggered after data context instance `Close/CloseAsync` method execution
void OnClosed(DataContextEventData eventData);
Task OnClosedAsync(DataContextEventData eventData);

struct DataContextEventData
{
    public IDataContext Context { get; }
}
```

### `ICommandInterceptor`

Base abstract class: `CommandInterceptor`.

This interceptor provides access to events and operations associated with database command.

```cs
// triggered after command initialization but before execution
// it provides access to prepared SQL command and parameters
DbCommand CommandInitialized(CommandEventData eventData, DbCommand command);

// triggered before `ExecuteScalar/ExecuteScalarAsync` call on command
// and could replace actual call by returning results from interceptor
Option<object?>       ExecuteScalar     (CommandEventData eventData,
                                         DbCommand        command,
                                         Option<object?>  result);
Task<Option<object?>> ExecuteScalarAsync(CommandEventData  eventData,
                                         DbCommand         command,
                                         Option<object?>   result,
                                         CancellationToken cancellationToken);

// triggered before `ExecuteNonQuery/ExecuteNonQueryAsync` call on command
// and could replace actual call by returning results from interceptor
Option<int>       ExecuteNonQuery     (CommandEventData eventData,
                                       DbCommand        command,
                                       Option<int>      result);
Task<Option<int>> ExecuteNonQueryAsync(CommandEventData  eventData,
                                       DbCommand         command,
                                       Option<int>       result,
                                       CancellationToken cancellationToken);

// triggered before `ExecuteReader/ExecuteReaderAsync` call on command
// and could replace actual call by returning results from interceptor
Option<DbDataReader>       ExecuteReader     (CommandEventData     eventData,
                                              DbCommand            command,
                                              CommandBehavior      commandBehavior,
                                              Option<DbDataReader> result);
Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData     eventData,
                                              DbCommand            command,
                                              CommandBehavior      commandBehavior,
                                              Option<DbDataReader> result,
                                              CancellationToken    cancellationToken);

// triggered after `ExecuteReader/ExecuteReaderAsync` call but before reader enumeration
// could be used to configure reader
void AfterExecuteReader(
    CommandEventData eventData,
    DbCommand        command,
    CommandBehavior  commandBehavior,
    DbDataReader     dataReader);

// triggered before DbDataReader dispose using Dispose method in synchronous APIs
void BeforeReaderDispose(
    CommandEventData eventData,
    DbCommand? command,
    DbDataReader dataReader);    

// triggered before DbDataReader dispose using DisposeAsync method in asynchronous APIs
Task BeforeReaderDisposeAsync(
    CommandEventData eventData,
    DbCommand? command,
    DbDataReader dataReader);    

struct CommandEventData
{
    public DataConnection DataConnection { get; }
}
```

### `IConnectionInterceptor`

Base abstract class: `ConnectionInterceptor`.

This interceptor provides access to events and operations associated with database connection.

```cs
// triggered before data connection `Open/OpenAsync` method execution
void ConnectionOpening(ConnectionEventData eventData, DbConnection connection);
Task ConnectionOpeningAsync(ConnectionEventData eventData,
                            DbConnection        connection,
                            CancellationToken   cancellationToken);

// triggered after data connection `Open/OpenAsync` method execution
void ConnectionOpened(ConnectionEventData eventData, DbConnection connection);
Task ConnectionOpenedAsync(ConnectionEventData eventData,
                           DbConnection        connection,
                           CancellationToken   cancellationToken);

struct ConnectionEventData
{
    public DataConnection DataConnection { get; }
}
```

### `IExceptionInterceptor`

Base abstract class: `ExceptionInterceptor`.

This interceptor provides access to query execution exception inspection/interception.

```cs
public interface IExceptionInterceptor : IInterceptor
{
    void ProcessException(ExceptionEventData eventData, Exception exception);
}

public readonly struct ExceptionEventData
{
    public IDataContext DataContext { get; }
}
```

## Interceptors Registration

Interceptors could be registred using multiple ways:

- add interceptor instance to existing connection/context object using `AddInterceptor` method
- add interceptor to `DataOptions`
- use single-time interceptor of `ICommandInterceptor.CommandInitialized` using `OnNextCommandInitialized` method of context

```cs
// registration in DataContext
using (var ctx = new DataContext(...))
{
    // add interceptor to current context
    ctx.AddInterceptor(interceptor);

    // one-time command prepared interceptor
    ctx.OnNextCommandInitialized((args, cmd) =>
    {
        // save next command parameters to external variable
        parameters = cmd.Parameters.Cast<DbParameter>().ToArray();
        return cmd;
    });
}

// registration in DataConnection
using (var ctx = new DataConnection(...))
{
    ctx.AddInterceptor(interceptor);

    // one-time command prepared interceptor
    ctx.OnNextCommandInitialized((args, cmd) =>
    {
        // set oracle-specific command option for next command
        ((OracleCommand)command).BindByName = false;
    });
}

// registration in DataConnection using fluent options configuration
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .WithInterceptor(interceptor);
var dc = new DataConnection(options);
```

### Interceptors support per context

- `RemoteDataContext`:
  - `IDataContextInterceptor`
  - `IEntityServiceInterceptor`
  - `IUnwrapDataObjectInterceptor`
  - `IExceptionInterceptor`
- `DataContext` and `DataConnection`:
  - `ICommandInterceptor`
  - `IConnectionInterceptor`
  - `IDataContextInterceptor`
  - `IEntityServiceInterceptor`
  - `IUnwrapDataObjectInterceptor`
  - `IExceptionInterceptor`

## Migration

To see which APIs were replaced with interceptors check [migration notes](https://github.com/linq2db/linq2db/wiki/Version-4-Migration#migration-to-interceptors).
