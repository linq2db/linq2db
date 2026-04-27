# LinqToDB Interceptors

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - choose the correct interceptor type for a LinqToDB extension point
> - inspect or modify prepared commands, SQL text, or command parameters
> - run behavior around connection opening or context close
> - rewrite query expression trees before translation
> - post-process materialized entities, translate exceptions, or unwrap ADO.NET wrappers

Interceptors are extension points for cross-cutting behavior around supported LinqToDB operations.
Use them when the behavior must run at a specific operation boundary: query expression processing,
command preparation, command execution, connection opening, entity materialization, exception handling,
or ADO.NET object unwrapping.

Interceptors are not general query operators. For query composition, use LINQ and `LinqExtensions`.
For SQL expression/member translation, use translator APIs such as `IMemberTranslator`.

---

## Registration

Register interceptors through `DataOptions` when the behavior should be part of a reusable context configuration.

```csharp
static readonly DataOptions Options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseInterceptor(new MyCommandInterceptor());

using var db = new DataConnection(Options);
```

Register interceptors on an existing context instance when the behavior should apply to that context instance.

```csharp
using var db = new DataConnection(Options);
db.AddInterceptor(new MyCommandInterceptor());
```

`DataOptions` is immutable. Every `UseInterceptor` or `UseInterceptors` call returns a new `DataOptions`
instance. Calling those methods multiple times adds interceptors; it does not replace previously registered
interceptors.

A single interceptor object can implement multiple interceptor interfaces. LinqToDB registers it for every
interceptor interface it implements.

## Choosing An Interceptor

| Need | Use |
| --- | --- |
| Inspect generated SQL and parameters | `ICommandInterceptor.CommandInitialized` |
| Change command text, command timeout, or parameters | `ICommandInterceptor.CommandInitialized` |
| Suppress command execution and provide a result | `ICommandInterceptor.ExecuteScalar`, `ExecuteNonQuery`, or `ExecuteReader` |
| Collect command execution metrics | `ICommandInterceptor.ExecuteScalar`, `ExecuteNonQuery`, or `ExecuteReader` |
| Run session initialization when a physical connection opens | `IConnectionInterceptor.ConnectionOpened` |
| Provide access tokens or configure connection before open | `IConnectionInterceptor.ConnectionOpening` |
| Run behavior when a context is closed | `IDataContextInterceptor` |
| Rewrite LINQ expression trees before translation | `IQueryExpressionInterceptor` |
| Post-process mapped entities created by materialization | `IEntityServiceInterceptor` |
| Translate or enrich exceptions before they are rethrown | `IExceptionInterceptor` |
| Unwrap profiler/proxy ADO.NET objects | `IUnwrapDataObjectInterceptor` |
| Change only the next command for one context | `OnNextCommandInitialized` |

## Command Interception

`ICommandInterceptor` is called after a `DbCommand` is prepared and around `DbCommand.Execute*` calls.
Use it for SQL logging, command rewriting, metrics, and command execution suppression.

`CommandInitialized` is called after command text and parameters are assigned and before command execution.
Return the command instance LinqToDB should execute.

`ExecuteScalar`, `ExecuteNonQuery`, and `ExecuteReader` are called before the corresponding provider command
execution method. Return `Option<T>.None` to continue normal provider execution. Return an explicit
`Option<T>` value to suppress provider execution and use that value as the result.

`AfterExecuteReader` is called after `ExecuteReader` returns a data reader.
`BeforeReaderDispose` and `BeforeReaderDisposeAsync` are called after reader consumption and before reader disposal.

## Connection Interception

`IConnectionInterceptor` is called before and after physical `DbConnection.Open` or `OpenAsync` calls.
Use it for connection-scoped initialization and diagnostics.
It observes connection opening only. It is not called when a physical connection is closed.

Use `ConnectionOpening` or `ConnectionOpeningAsync` to configure the connection before opening.
Use `ConnectionOpened` or `ConnectionOpenedAsync` to run initialization that requires an open connection,
for example provider-specific session settings.

For simple before/after-open callbacks, `UseBeforeConnectionOpened` and `UseAfterConnectionOpened` are usually
sufficient.

## Data Context Lifecycle Interception

`IDataContextInterceptor` is called before and after `IDataContext.Close` or `CloseAsync`.
Use it for lifecycle diagnostics and cleanup that belongs to the context boundary rather than to a single command.
It observes context close events, not physical connection close events.

## Query Expression Interception

`IQueryExpressionInterceptor` is called before query expression translation.
Use it when a query shape must be rewritten at the LINQ expression-tree level.

The interceptor can be called for exposed query expressions, full query expressions, filters, or associations.
Use `QueryExpressionArgs.ExpressionKind` to distinguish those call sites.

This interceptor rewrites expression trees. It does not rewrite provider-specific SQL text.

## Entity Materialization Interception

`IEntityServiceInterceptor` is called after LinqToDB materializes a mapped entity instance and before that
instance is returned to user code. Return the entity instance LinqToDB should use.

This interceptor is not called for objects explicitly constructed by user projections, for example
`select new MyDto { ... }`.

## Exception Interception

`IExceptionInterceptor` is called after a command/query exception is thrown and before LinqToDB rethrows it.
Use it for exception translation, policy enforcement, or diagnostic enrichment.

If the interceptor throws a new exception, that exception replaces the original one.
If the interceptor returns normally, LinqToDB rethrows the original exception.

## ADO.NET Object Unwrapping

`IUnwrapDataObjectInterceptor` is called when LinqToDB needs provider objects from wrapped ADO.NET objects.
Use it with profilers, instrumentation wrappers, or proxy libraries that wrap `DbConnection`, `DbTransaction`,
`DbCommand`, or `DbDataReader`.

Each method must return the object LinqToDB should use for subsequent provider operations.

## One-Time Command Interception

`OnNextCommandInitialized` adds a one-time command-initialization interceptor to a `DataConnection` or
`DataContext`. Use it when only the next prepared command should be inspected or modified.

For reusable behavior, prefer `DataOptions.UseInterceptor` or `IDataContext.AddInterceptor`.
