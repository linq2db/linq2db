# LinqToDB Configuration and Extensibility

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - configure `DataOptions` for a provider
> - add SQL tracing, retry policy, or interceptors
> - register custom member translators
> - understand `DataOptions` lifetime and immutability

This document describes the supported `DataOptions` configuration patterns and extensibility
points available to package consumers.

`DataOptions` is immutable - every `UseXxx` method returns a new instance. Configure once,
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

### Standard setup - connection string

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

`TraceLevel` is `System.Diagnostics.TraceLevel` (not a LinqToDB-defined type). LinqToDB only
distinguishes `Off` from every other level: `Off` disables the `onTrace`/`WriteTrace` callback
entirely, while `Error`, `Warning`, `Info`, and `Verbose` all receive the same events - LinqToDB
itself only ever tags an event `Info` (normal execution steps) or `Error` (an exception occurred);
it never produces `Warning`- or `Verbose`-tagged events, and `TraceInfo.SqlText` always includes
the command text and parameter values regardless of the level passed to `UseTracing`. Pick `Info`
(or any non-`Off` level) to receive tracing; pick `Off` to disable it.

| Level | What is traced |
|---|---|
| `Off` | nothing |
| any other value (`Error`, `Warning`, `Info`, `Verbose`) | all SQL statements + parameter values, and exceptions - LinqToDB does not vary behavior by these four levels |

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

`IRetryPolicy` has four methods to implement: `Execute<TResult>(Func<TResult> operation)`,
`Execute(Action operation)`, `ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)`,
and `ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)`.

---

## Configuration via external files

### JSON (`appsettings.json`)

LinqToDB has no built-in JSON-specific configuration API. For a single connection, the standard
.NET hosting pattern is enough: read a connection string via `IConfiguration`, then pass it to
`UseConnectionString`.

```csharp
var connectionString = configuration.GetConnectionString("Default");

var options = new DataOptions()
    .UseSqlServer(connectionString);
```

For named-configuration semantics equivalent to `app.config` (multiple connections,
`DataConnection.DefaultSettings`, `new DataConnection("Name")` / `UseConfiguration("Name")`),
implement `ILinqToDBSettings` yourself against `IConfiguration` - this is the same
provider-agnostic seam `LinqToDBSection` implements for XML, and it does not require
`linq2db.Compat` (that package is specific to reading `System.Configuration.ConfigurationManager`
XML sections):

```csharp
public class JsonLinqToDBSettings(IConfiguration configuration) : ILinqToDBSettings
{
    public IEnumerable<IDataProviderSettings> DataProviders => [];
    public string? DefaultConfiguration => "Default";
    public string? DefaultDataProvider  => null;

    public IEnumerable<IConnectionStringSettings> ConnectionStrings =>
        configuration.GetSection("ConnectionStrings").GetChildren()
            .Select(s => new ConnectionStringSettings(s.Key, s.Value!, providerName: "SqlServer"));
}

DataConnection.DefaultSettings = new JsonLinqToDBSettings(configuration);

using var db = new DataConnection("Default");
```

Provider-name resolution per connection is application-specific - `appsettings.json`'s
`ConnectionStrings` section has no standard provider-name field, so map it however the
application's JSON shape represents it.

### Legacy XML (`app.config` / `web.config`)

`ILinqToDBSettings`, `IConnectionStringSettings`, and the named-configuration API on
`DataConnection` (`AddConfiguration`, `AddOrSetConfiguration`, `GetConnectionString`,
`DefaultConfiguration`, `DefaultDataProvider`) are core, provider-agnostic, and work on every TFM -
without any config file, entries can be registered programmatically:

```csharp
DataConnection.AddConfiguration("MyDb", "Server=...;...", SqlServerTools.GetDataProvider());
using var db = new DataConnection("MyDb");
```

Reading the classic `<linq2db>` / `<connectionStrings>` XML sections from `app.config`/`web.config`
is a separate concern, backed by `System.Configuration.ConfigurationManager`:

- On **`net462`**, this is already compiled into `linq2db.dll` - `DataConnection.DefaultSettings`
  lazily resolves to `LinqToDBSection.Instance` automatically, so `app.config` is read with no
  extra package or startup code.
- On **`netstandard2.0`/`net8.0`+**, add the **`linq2db.Compat`** NuGet package and opt in
  explicitly at startup:

```csharp
using LinqToDB.Configuration;
DataConnection.DefaultSettings = LinqToDBSection.Instance;
```

Select a named configuration into a `DataOptions` with `UseConfiguration(string?)`
(the older `UseConfigurationString` is obsolete, scheduled for removal in v7).

---

## Interceptors

Interceptors allow viewing, modifying, or suppressing operations performed by LinqToDB.
Register with `UseInterceptor(IInterceptor)` or `UseInterceptors(IEnumerable<IInterceptor>)`;
remove with `RemoveInterceptor(instance)`. Multiple calls accumulate; existing interceptors are
not replaced.

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseInterceptor(new LoggingInterceptor());
```

For the full list of interceptor interfaces, which events each one covers, and worked examples,
see [`docs/interceptors.md`](interceptors.md).

---

## Custom SQL translation (member translators)

`UseMemberTranslator(IMemberTranslator)` registers a `DataOptions`-level translator that extends
how LinqToDB translates .NET member expressions to SQL; remove one with
`RemoveTranslator(instance)`.

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMemberTranslator(new MyTranslator());
```

For `MyTranslator` implementation patterns, `[Sql.Expression]` / `[Sql.Function]` /
`[ExpressionMethod]` alternatives, and a caveat about the `IMemberTranslator` implementation
surface, see [`docs/extensions.md`](extensions.md).

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

## Temporary context options

Use `IDataContext.UseOptions(...)` when an already-created context needs a scoped, temporary
option override.

The returned `IDisposable` restores the previous options and related context state when disposed.
Always use a `using` scope.

```csharp
using var db = new DataConnection(options);

using (db.UseOptions<DataContextOptions>(o => o.WithCommandTimeout(30)))
{
    // Command timeout override is active only inside this block.
    db.GetTable<Product>().ToList();
}

// Previous options are restored here.
```

Typed helpers are available when only one option group should change:

```csharp
using var db = new DataConnection(options);

using (db.UseLinqOptions(o => o with { DisableQueryCache = true }))
{
    db.GetTable<Product>().Where(p => p.IsActive).ToList();
}
```

Use `UseOptions` for short-lived per-context overrides, not for normal application configuration.
For regular configuration, build a reusable `DataOptions` instance once and pass it to each
`DataConnection` / `DataContext` constructor.

Connection-related overrides are limited: `UseOptions`/`UseConnectionOptions` reapply only mapping
schema and the connection interceptor. Passing a different `ConnectionString`, `ProviderName`,
`DataProvider`, `DbConnection`, `DbTransaction`, `DisposeConnection`, `ConnectionFactory`,
`DataProviderFactory`, or `OnEntityDescriptorCreated` value than the context was created with
**throws `LinqToDBException`** - these are creation-time identity settings, not silently ignored
overrides.

`UseMappingSchema(mappingSchema)` is a convenience override for temporarily replacing the context
mapping schema. It follows the same disposable-scope rule.

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

- `docs/provider-setup.md` - provider-specific `UseXxx` methods and connection string formats
- `docs/agent-antipatterns.md` - common mistakes including `MappingSchema` reuse
- `docs/interceptors.md` - full interceptor interface list and worked examples
- `docs/extensions.md` - `[Sql.Expression]` / `[Sql.Function]` / `[ExpressionMethod]` and `IMemberTranslator`
- `docs/translatable-methods.md` - standard .NET methods translated to SQL
- [`DataOptions` API reference](https://linq2db.github.io/api/LinqToDB.DataOptions.html)
