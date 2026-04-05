# LinqToDB Configuration and Extensibility

> You are here if you need to:
> - configure `DataOptions` for a provider
> - add SQL tracing, retry policy, or interceptors
> - register custom member translators
> - understand `DataOptions` lifetime and immutability

This document describes the supported `DataOptions` configuration patterns and extensibility
points available to package consumers.

`DataOptions` is immutable — every `UseXxx` method returns a new instance. Configure once,
store as a singleton, and pass to every `DataConnection` / `DataContext` constructor.

---

## Quick pattern

```csharp
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer("connection string")   // provider + connection string
    .UseTracing(TraceLevel.Info, t =>    // optional: SQL logging
        Console.WriteLine(t.SqlText))
    .UseRetryPolicy(new TransientRetryPolicy()); // optional: auto-retry

using var db = new DataConnection(_options);
```

---

## Connection configuration

### Standard setup — connection string

```csharp
// Use a provider-specific UseXxx method (recommended)
var options = new DataOptions()
    .UseSqlServer("Server=.;Database=MyDb;Trusted_Connection=True;");
```

See `docs/provider-setup.md` for the full list of `UseXxx` methods per provider.

### Reuse an existing DbConnection

```csharp
// DataConnection does NOT dispose the connection when disposeConnection is false
var options = new DataOptions()
    .UseConnection(dataProvider, existingConnection, disposeConnection: false);
```

Use this pattern with connection pooling wrappers (e.g. MiniProfiler) or when you need
to share a connection across multiple `DataConnection` instances.

### Connection factory

```csharp
// Called once per DataConnection instance
var options = new DataOptions()
    .UseConnectionFactory(dataProvider, opts =>
        new SqlConnection(opts.ConnectionOptions.ConnectionString));
```

Prefer `UseConnectionFactory` over `UseConnection` when you need to create a new physical
connection for each `DataConnection` instance but still need to customize the `DbConnection`
object before use (e.g. to set access tokens).

### Before / after connection open hooks

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseBeforeConnectionOpened(cn =>
    {
        ((SqlConnection)cn).AccessToken = GetToken();
    })
    .UseAfterConnectionOpened(cn =>
    {
        // executed after Open() completes
    });
```

Async overloads are available: `.UseBeforeConnectionOpened(sync, async)`.

---

## Tracing and logging

`UseTracing` receives a `TraceInfo` object that exposes the generated SQL text, parameters,
execution time, and exception (if any).

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseTracing(TraceLevel.Info, traceInfo =>
    {
        if (traceInfo.TraceInfoStep == TraceInfoStep.BeforeExecute)
            logger.LogDebug(traceInfo.SqlText);
        else if (traceInfo.Exception != null)
            logger.LogError(traceInfo.Exception, "Query failed");
    });
```

`TraceLevel` values:

| Level | What is traced |
|---|---|
| `Off` | nothing |
| `Error` | exceptions only |
| `Warning` | slow queries and exceptions |
| `Info` | all SQL statements (default recommendation) |
| `Verbose` | all SQL + parameter values |

String-callback overload (for legacy `TraceSwitch`-based setups):

```csharp
options.UseTraceWith((message, category, level) =>
    Trace.WriteLine(message, category));
```

---

## Retry policies

LinqToDB does not retry by default. Use `UseRetryPolicy` to enable automatic retries for
transient failures (e.g. network blips, connection pool exhaustion).

```csharp
// Built-in exponential back-off with defaults (5 retries, max 30 s delay)
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseDefaultRetryPolicyFactory();

// Built-in policy with custom parameters
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMaxRetryCount(3)
    .UseMaxDelay(TimeSpan.FromSeconds(10));

// Custom IRetryPolicy implementation
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseRetryPolicy(new MyRetryPolicy());
```

`IRetryPolicy` has a single method: `Execute<TResult>(Func<TResult> operation)`.

---

## Interceptors

Interceptors allow viewing, modifying, or suppressing operations performed by LinqToDB.
Register with `UseInterceptor(IInterceptor)` or `UseInterceptors(IEnumerable<IInterceptor>)`.
Multiple calls accumulate; existing interceptors are not replaced.

Available interceptor interfaces:

| Interface | Events |
|---|---|
| `ICommandInterceptor` | before/after command creation, before/after execution |
| `IConnectionInterceptor` | before/after connection open/close |
| `IDataContextInterceptor` | before/after `DataContext` close, entity created |
| `IEntityServiceInterceptor` | entity created during materialization |
| `IExceptionInterceptor` | exception thrown by a command |
| `IQueryExpressionInterceptor` | LINQ expression tree before translation |
| `IUnwrapDataObjectInterceptor` | unwrap profiler-wrapped ADO.NET objects |

```csharp
// Example: log every executed command
public class LoggingInterceptor : CommandInterceptor
{
    public override void AfterExecuteReader(
        CommandEventData eventData, DbCommand command,
        CommandBehavior commandBehavior, DbDataReader dataReader)
    {
        Console.WriteLine($"Executed: {command.CommandText}");
    }
}

var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseInterceptor(new LoggingInterceptor());
```

To remove an interceptor registered in a base `DataOptions`, call `RemoveInterceptor(instance)`.

---

## Custom SQL translation (member translators)

`UseMemberTranslator` registers a custom `IMemberTranslator` that extends how LinqToDB
translates .NET member expressions to SQL. This is the integration point for adding
provider-specific SQL functions that are not in the standard `Sql.*` API.

```csharp
public class MyTranslator : MemberTranslatorBase
{
    public MyTranslator()
    {
        Registration.RegisterMethod(
            (string s) => s.MyCustomMethod(),
            TranslateMyCustomMethod);
    }

    Expression? TranslateMyCustomMethod(
        ITranslationContext ctx, MethodCallExpression call, TranslationFlags flags)
    {
        if (!ctx.TranslateToSqlExpression(call.Object!, out var arg))
            return null;
        return ctx.CreatePlaceholder(
            ctx.ExpressionFactory.Function(
                ctx.ExpressionFactory.GetDbDataType(call.Type),
                "MY_FUNCTION", arg),
            call);
    }
}

var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMemberTranslator(new MyTranslator());
```

For simpler cases (no context access needed) use `[Sql.Expression]` or `[Sql.Function]`
attributes on a static method — see `docs/translatable-methods.md`.

To remove a previously registered translator, call `RemoveTranslator(instance)`.

---

## Mapping schema

```csharp
// Replace active schema
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(mySchema);

// Combine with existing schema (additive)
var options = baseOptions.UseAdditionalMappingSchema(extraSchema);
```

> **Note:** Create `MappingSchema` instances once and reuse them. See anti-pattern #1 in
> `docs/agent-antipatterns.md`.

---

## DataProviderFactory

For advanced scenarios where the provider itself must be chosen at runtime:

```csharp
var options = new DataOptions()
    .UseConnectionString("connection string")
    .UseDataProviderFactory(connOptions =>
        DetectProvider(connOptions.ConnectionString!));
```

---

## Chaining summary

All `UseXxx` methods return `DataOptions` and can be chained. The result is a new immutable
instance at each step:

```csharp
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(myMappingSchema)
    .UseTracing(TraceLevel.Info, t => logger.LogDebug(t.SqlText))
    .UseDefaultRetryPolicyFactory()
    .UseInterceptor(new LoggingInterceptor());
```

---

## See also

- `docs/provider-setup.md` — provider-specific `UseXxx` methods and connection string formats
- `docs/agent-antipatterns.md` — common mistakes including `MappingSchema` reuse
- `docs/translatable-methods.md` — standard .NET methods translated to SQL; custom extension via `[Sql.Expression]`
- [`DataOptions` API reference](https://linq2db.github.io/api/LinqToDB.DataOptions.html)
