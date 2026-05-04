---
area: EXTENSIONS-PKG
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 3/3
coverage_tier_2: 0/0
---

# EXTENSIONS-PKG — Microsoft.Extensions Integration

NuGet package `linq2db.Extensions` (assembly `linq2db.Extensions`). Wires linq2db into ASP.NET Core / generic-host applications via `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions`. No `Microsoft.Extensions.Options` dependency — options integration is done directly through `DataOptions`.

## Subsystems

### DI registration (`ServiceConfigurationExtensions`)

`ServiceConfigurationExtensions` (`Source/LinqToDB.Extensions/ServiceConfigurationExtensions.cs:14`) exposes three families of extension methods on `IServiceCollection`:

**`AddLinqToDB`** — single-connection shorthand. Three overloads accept `Func<DataOptions,DataOptions>?`, `Func<IServiceProvider,DataOptions,DataOptions>`, or `Func<DataOptions>`. All delegate to `AddLinqToDBContext<IDataContext,DataConnection>` (`ServiceConfigurationExtensions.cs:62`).

**`AddLinqToDBContext<TContext>`** and **`AddLinqToDBContext<TContext,TContextImplementation>`** — typed-context overloads. Same three configure-delegate shapes. The canonical implementation (`ServiceConfigurationExtensions.cs:481`) uses reflection (`HasTypedContextConstructor`) to detect which `DataOptions` variant the implementation constructor accepts — `DataOptions<TContextImplementation>`, `DataOptions<TContext>`, or plain `DataOptions` — and registers the matching typed options descriptor. Also registers `IDataContextFactory<TContext>` (backed by `DataContextFactory<TContext>`) at the same lifetime.

**`AddLinqToDBContext<TContext>(Func<IServiceProvider,TContext>)`** and the `(Func<IServiceProvider,string?,TContext>)` overload — factory-delegate variants that bypass `DataOptions` reflection entirely and call the delegate directly (`ServiceConfigurationExtensions.cs:592`, `ServiceConfigurationExtensions.cs:650`). The `string?` variant passes `DataConnection.DefaultConfiguration` as the configuration name.

**`AddLinqToDBService<TContext>`** — registers `ILinqService<TContext>` (REMOTE-CLIENT area) as `Scoped`. Two overloads: one auto-creates `LinqService<TContext>` from the container's `IDataContextFactory<TContext>`, the other accepts a custom factory delegate.

Default lifetime for all methods: `ServiceLifetime.Scoped` (`ServiceConfigurationExtensions.cs:16`).

### Logging adapter (`Logging/`)

**`LinqToDBLoggerFactoryAdapter`** (`Source/LinqToDB.Extensions/Logging/LinqToDbLoggerFactoryAdapter.cs:10`) — wraps `ILoggerFactory`, creates `ILogger<DataConnection>` on construction, exposes `OnTrace(string?, string?, TraceLevel)`. Maps `System.Diagnostics.TraceLevel` → `Microsoft.Extensions.Logging.LogLevel`. The `category` parameter is captured but unused (suppressed with `IDE0060`).

**`OptionsBuilderExtensions`** (`Source/LinqToDB.Extensions/Logging/OptionsBuilderExtensions.cs:12`) — two `DataOptions` extension methods:
- `UseLoggerFactory(DataOptions, ILoggerFactory)` — instantiates `LinqToDBLoggerFactoryAdapter`, then calls `options.WithOptions<QueryTraceOptions>(o => o with { TraceLevel = TraceLevel.Verbose, WriteTrace = adapter.OnTrace })` (`OptionsBuilderExtensions.cs:34`). This wires the adapter into the `QueryTraceOptions` slot on `DataOptions`.
- `UseDefaultLogging(DataOptions, IServiceProvider)` — resolves `ILoggerFactory` from the container and delegates to `UseLoggerFactory`.

## Key types

| Type | File | Role |
|---|---|---|
| `ServiceConfigurationExtensions` | `ServiceConfigurationExtensions.cs` | `IServiceCollection` extension entry points |
| `LinqToDBLoggerFactoryAdapter` | `Logging/LinqToDbLoggerFactoryAdapter.cs` | M.E.Logging → `WriteTrace` bridge |
| `OptionsBuilderExtensions` | `Logging/OptionsBuilderExtensions.cs` | `DataOptions` extension for logging wiring |

## Files (Tier 1 / Tier 2)

**Tier 1** (all .cs files — all read in full):

| File | Summary |
|---|---|
| `ServiceConfigurationExtensions.cs` | DI registration for `IDataContext`, `IDataContextFactory<T>`, `ILinqService<T>` |
| `Logging/LinqToDbLoggerFactoryAdapter.cs` | `OnTrace` delegate adapter over `ILoggerFactory` |
| `Logging/OptionsBuilderExtensions.cs` | `UseLoggerFactory` / `UseDefaultLogging` on `DataOptions` |

**Non-code** (read for package identity, not Tier classification):
- `LinqToDB.Extensions.csproj` — assembly name, NuGet metadata, package references
- `PublicAPI.Shipped.txt` — confirmed shipped surface

**Tier 2**: none (area has no .cs files beyond the 3 Tier-1 files).

**Tier 3**: none.

## Inbound / outbound dependencies

**Outbound (this area consumes):**
- `LinqToDB` core project — `DataOptions`, `DataOptions<T>`, `DataConnection`, `IDataContext`, `IDataContextFactory<T>`, `QueryTraceOptions`, `ILinqService<T>`, `LinqService<T>`, `DataContextFactory<T>` (CORE + REMOTE-CLIENT areas)
- `Microsoft.Extensions.DependencyInjection` NuGet — `IServiceCollection`, `ServiceLifetime`, `ServiceDescriptor`, `TryAdd`
- `Microsoft.Extensions.Logging.Abstractions` NuGet — `ILoggerFactory`, `ILogger<T>`, `LogLevel`

**Inbound (who consumes this area):** ASP.NET Core / generic-host application startup code (`ConfigureServices` / `AddLinqToDB`). No other in-repo project references this project.

## Known issues / debt

- The `category` parameter in `LinqToDBLoggerFactoryAdapter.OnTrace` is unused (`IDE0060` suppressed). Category-per-logger routing (e.g. separate loggers for SQL vs. connection events) is deferred. (`Logging/LinqToDbLoggerFactoryAdapter.cs:14`)
- `HasTypedContextConstructor` uses runtime reflection and throws `ArgumentException` at DI registration time if no `DataOptions` constructor is found. No compile-time guard exists.
- No `IOptions<DataOptions>` integration: callers must wire `UseDefaultLogging` explicitly in the configure delegate rather than relying on `Microsoft.Extensions.Options` pipeline.

## See also

- CORE area — `DataOptions`, `QueryTraceOptions`, `IDataContext`, `DataConnection`
- REMOTE-CLIENT area — `ILinqService<T>`, `LinqService<T>`, `IDataContextFactory<T>`

<details><summary>Coverage</summary>

Tier 1 (3/3 read): `ServiceConfigurationExtensions.cs`, `Logging/LinqToDbLoggerFactoryAdapter.cs`, `Logging/OptionsBuilderExtensions.cs`. Tier 2: 0 files (none exist beyond Tier 1). Non-code files read for package identity: `LinqToDB.Extensions.csproj`, `PublicAPI.Shipped.txt`.
</details>
