---
area: EFCORE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 1/2
coverage_tier_2: 22/22
---

# EFCORE

EFCore companion library. Single source tree compiled to four NuGet packages (`linq2db.EntityFrameworkCore`) — one per EF major version (EF 3.1, 8, 9, 10). Provides linq2db's SQL generation, bulk-copy, DML, and LINQ extensions as a drop-in complement to an existing `DbContext`.

## Multi-EF compilation strategy

Four `.csproj` files share one `.props` file (`LinqToDB.EntityFrameworkCore.props`) and all source files. Each csproj defines a single `DefineConstants` symbol that gates per-EF API differences:

| csproj | Symbol | TargetFrameworks | EF version |
|---|---|---|---|
| `.EF3.csproj` | `EF31` | `netstandard2.0;net462` | EF Core 3.1 |
| `.EF8.csproj` | `EF8` | `net8.0` | EF Core 8 |
| `.EF9.csproj` | `EF9` | `net8.0` | EF Core 9 (pins `Microsoft.EntityFrameworkCore.Relational` 9.0.0) |
| `.EF10.csproj` | `EF10` | `net10.0` | EF Core 10 |

All four reference `LinqToDB.csproj` (project reference) plus `Microsoft.EntityFrameworkCore.Relational` (package reference, version from `Directory.Build.props` except EF9 which overrides to 9.0.0). The shared `.props` also wires up `PublicAPI/PublicAPI.*.txt` and per-TFM `PublicAPI/$(TargetFramework)/PublicAPI.*.txt` as `AdditionalFiles` for the Roslyn API-compat analyzer. `NoWarn=EF1001` suppresses warnings about use of EF internal APIs (intentional — e.g. accessing `RelationalQueryContextFactory._dependencies`/`.Dependencies` and `QueryCompiler._queryContextFactory` via reflection).

Per-EF branches appear throughout the source as `#if EF31 ... #else ... #endif` blocks. The most significant divergences:
- `ReflectionMethods.cs`: `FromSqlOnQueryableMethodInfo` (EF31 only), `GetServiceProviderHashCode` return type (`long` in EF31, `int` later), `AsSplitQueryMethodInfo`/`AsSingleQueryMethodInfo` absent in EF31, `ShouldUseSameServiceProvider` absent in EF31.
- `EFCoreMetadataReader.cs`: `GetAttributes(Type)` uses `et.GetTableName()` directly in EF31 vs `StoreObjectIdentifier.Create` in EF8+; `FindDiscriminatorProperty()` vs `GetDiscriminatorProperty()`; annotation provider type is `IMigrationsAnnotationProvider` (EF31) vs `IRelationalAnnotationProvider` (EF8+); `IDiagnosticsLogger` and `DatabaseDependencies` injected only in EF8+; multi-filter via `et.GetDeclaredQueryFilters()` used in EF10.
- `LinqToDBForEFToolsDataConnection.cs`: Change-tracker snap uses `Snapshot.Empty` (EF8+ post-EF31) vs `ValueBuffer.Empty` (EF31/EF8).
- `TransformExpressionVisitor.cs`: `QueryRootExpression`-based dispatch (EF8+) handles `FromSqlQueryRootExpression`, temporal table root expressions; EF31 falls through to `VisitConstant`/`FromSqlOnQueryable`.

## Key types

### Public entry-point: `LinqToDBForEFTools` (partial class, 5 files)

`Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.cs` — static partial class. Initialized once via `Lazy<bool> _initialized`. Initialization (`InitializeInternal`) does two things: calls `InitializeMapping()` (maps `DbFunctions.Like` → `Sql.Like`) and installs `LinqExtensions.ProcessSourceQueryable` — a delegate that intercepts any EF `IQueryable` that is not already a `IQueryProviderAsync` and reroutes it through a new linq2db `ExpressionQuery` attached to a fresh `LinqToDBForEFToolsDataConnection`. Also sets `LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter()`.

Static `Implementation` property (type `ILinqToDBForEFTools`) is the strategy object. Default is `LinqToDBForEFToolsImplDefault`. Callers who want to customize provider detection, metadata reading, or logging replace this singleton before first use.

Per-partial responsibilities:
- `LinqToDBForEFTools.cs` — core: `Initialize`, `Implementation`, `GetMetadataReader`, `GetEFProviderInfo`, `GetDataProvider`, `GetMappingSchema`, `TransformExpression`, `CreateLinqToDBConnection`, `CreateLinqToDBContext`, `CreateLinqToDBConnectionDetached`, `ToLinqToDB`, `GetCurrentContext`, `EnableChangeTracker`.
- `LinqToDBForEFTools.ContextExtensions.cs` — `BulkCopy`/`BulkCopyAsync` on `DbContext`, `Into<T>`, `GetTable<T>`, `GetLinqToDBOptions`, `GetDbContextOptions`.
- `LinqToDBForEFTools.ContextOptionsBuilderExtensions.cs` — `UseLinqToDB` on `DbContextOptionsBuilder` (installs `LinqToDBOptionsExtension`).
- `LinqToDBForEFTools.Extensions.cs` — `ToLinqToDBTable` on `DbSet<T>`.
- `LinqToDBForEFTools.Mapping.cs` — `InitializeMapping`: maps `DbFunctions.Like`.

### Interface: `ILinqToDBForEFTools`

Extensibility seam. Callers override `LinqToDBForEFToolsImplDefault` methods or provide a full replacement via `LinqToDBForEFTools.Implementation`. Key methods:
- `GetDataProvider(DataOptions, EFProviderInfo, EFConnectionInfo)` — resolves `IDataProvider`.
- `CreateMetadataReader(IModel?, IInfrastructure<IServiceProvider>?)` — returns `EFCoreMetadataReader`.
- `GetMappingSchema` / `CreateMappingSchema` — builds `MappingSchema` from EF model + converters.
- `TransformExpression` — delegates to `TransformExpressionVisitor`.
- `GetCurrentContext(IQueryable)` — reflection-based extraction of `DbContext` from `EntityQueryProvider`.
- `ExtractConnectionInfo`, `ExtractModel`, `CreateLogger`, `LogConnectionTrace`.

### Default implementation: `LinqToDBForEFToolsImplDefault`

`Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsImplDefault.cs` — `public class`, virtualizable throughout. Caches:
- `_knownProviders`: `ConcurrentDictionary<ProviderKey, IDataProvider>` keyed on `(providerName, connectionString)`.
- `_schemaCache`: `MemoryCache` (1-hour sliding expiration) keyed on `(DataOptions, IModel, IRelationalTypeMappingSource, IMetadataReader, IValueConverterSelector, EnableChangeTracker)`.

Provider detection chain: `GetDataProvider` checks `DataOptions.ConnectionOptions.DataProvider` first (explicit override), then `ProviderName` string, then calls `GetLinqToDBProviderInfo` which tries three sources in priority order: `RelationalOptionsExtension` type name, `DbConnection` type name, `DatabaseFacade.ProviderName` string. Results from all three are merged via `LinqToDBProviderInfo.Merge` (first non-null wins).

EF provider name → linq2db `ProviderName` mapping (canonical table, `GetLinqToDBProviderInfo(DatabaseFacade)`):

| EF provider string | linq2db ProviderName |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | `ProviderName.SqlServer` |
| `Pomelo.EntityFrameworkCore.MySql`, `Devart.Data.MySql.EFCore` | `ProviderName.MySql` |
| `Npgsql.EntityFrameworkCore.PostgreSQL`, `Devart.Data.PostgreSql.EFCore` | `ProviderName.PostgreSQL` |
| `Microsoft.EntityFrameworkCore.Sqlite`, `Devart.Data.SQLite.EFCore` | `ProviderName.SQLite` |
| `FirebirdSql.EntityFrameworkCore.Firebird`, `EntityFrameworkCore.FirebirdSql` | `ProviderName.Firebird` |
| `IBM.EntityFrameworkCore` (+ lnx/osx variants) | `ProviderName.DB2LUW` |
| `Devart.Data.Oracle.EFCore` | `ProviderName.Oracle` |
| `EntityFrameworkCore.Jet` | `ProviderName.Access` |
| `EntityFrameworkCore.SqlServerCompact40/35` | `ProviderName.SqlCe` |

Not supported (documented TODO in code): Informix, SAP HANA, Sybase, ClickHouse.

`CreateLinqToDBDataProvider` dispatches on `LinqToDBProviderInfo.ProviderName` to the appropriate `<Vendor>Tools.GetDataProvider(...)` call. SQL Server always uses `SqlServerProvider.MicrosoftDataSqlClient` (via `CreateSqlServerProvider`). SQLite always resolves to `SQLiteProvider.Microsoft`. Default SQL Server version is `SqlServerVersion.AutoDetect`; PostgreSQL is `PostgreSQLVersion.AutoDetect` — both settable via static properties.

`DefineConvertors` / `CreateMappingSchema`: iterates all EF entity CLR types, uses `IValueConverterSelector.Select(modelType)` to import EF value converters into `MappingSchema` as `ConvertExpression` + `SetValueToSqlConverter` entries. Enum types with Npgsql label mappings get special treatment: `NpgsqlEnumTypeMapping.Labels` → SQL literal as `'label'::pgtype`.

### Metadata bridge: `EFCoreMetadataReader`

`Source/LinqToDB.EntityFrameworkCore/EFCoreMetadataReader.cs` — `internal sealed class`, implements `IMetadataReader`. Bridges EF `IModel` into linq2db's `MappingAttribute` model.

`GetAttributes(Type)`:
- Finds `IEntityType` in `IModel`. If found: emits `TableAttribute` (name + schema), `QueryFilterAttribute` (EF `IEntityType.GetQueryFilter()` / `GetDeclaredQueryFilters()` for EF10+), `InheritanceMappingAttribute` entries (discriminator hierarchy).
- Query filter rewrite: replaces `DbContext`-typed sub-expressions with `Expression.Property(context, "Context")` on the `LinqToDBForEFToolsDataConnection`, adding an `IDataContext dc` parameter — so linq2db can evaluate the filter with access to the live `DbContext`.
- Falls back to `[Table]` data-annotation if entity not in model.

`GetAttributes(Type, MemberInfo)`:
- Emits `ColumnAttribute` (name, type, nullability, identity, PK, discriminator, skip-on-insert/update).
- Identity detection: scans property annotations for `:ValueGenerationStrategy` containing "Identity", `:Autoincrement = true`, or `Relational:DefaultValueSql` containing "nextval" or "NEXT VALUE FOR".
- Emits `ValueConverterAttribute` when property has a value converter.
- Emits `AssociationAttribute` from EF navigation foreign keys.
- Calls EF's `RelationalSqlTranslatingExpressionVisitorDependencies.MethodCallTranslatorProvider.Translate` / `MemberTranslatorProvider.Translate` to discover provider-specific functions and encode them as `Sql.ExpressionAttribute`. Results are cached in `_calculatedExtensions: ConcurrentDictionary<MemberInfo, Sql.ExpressionAttribute?>`. This is how Npgsql-specific operators (`@>`, `<@`, `&&`, `AT TIME ZONE`, etc.) and provider DB functions become available in linq2db queries without explicit attribute decoration.
- `ConvertToExpressionAttribute` serializes EF `SqlExpression` trees back to linq2db `{0}`, `{1}` placeholder format.
- Special Npgsql handling: `PgBinaryExpression` operator-type → SQL operator string mapping (EF8+ path uses `PgBinaryExpressionName = "PgBinaryExpression"`; EF31 path uses `Left`/`Right`/`Operator` reflection).

`GetObjectID()` returns a composite hash string of the injected service instances, so `MappingSchema` can use it as a cache key.

### Query interception: `LinqToDBForEFQueryProvider<T>`

`Source/LinqToDB.EntityFrameworkCore/Internal/LinqToDBForEFQueryProvider.cs` — implements `IAsyncQueryProvider` (EF interface), `IQueryProviderAsync` (linq2db), `IQueryable<T>`, `IAsyncEnumerable<T>`, `IExpressionQuery`. Acts as the bridge queryable returned by `ToLinqToDB<T>()`. Internally creates a linq2db `ExpressionQuery<T>` via `Internals.CreateExpressionQueryInstance<T>`, stores it as `QueryProvider`. All `IQueryProvider` / `IQueryProviderAsync` calls delegate to that inner provider. EF's `IAsyncQueryProvider.ExecuteAsync<TResult>` is bridged by reflecting `IQueryProviderAsync.ExecuteAsync<item>` with the unwrapped Task item type.

### Expression rewriting: `TransformExpressionVisitor`

`Source/LinqToDB.EntityFrameworkCore/Internal/TransformExpressionVisitor.cs` — extends `ExpressionVisitorBase`. Entry point: `Transform(IDataContext?, IModel?, Expression)`. Tracks `Tracking` and `IgnoreTracking` booleans as side-effects of traversal.

Key rewrite rules (applied in `VisitConstant`, `VisitMethodCall`, `VisitExtension`):
- `EntityQueryable<T>` / `DbSet<T>` constants → `DataExtensions.GetTable<T>(dc)` call.
- `QueryRootExpression` (EF8+): `FromSqlQueryRootExpression` → `IDataContext.FromSql<T>`; temporal root expressions (`TemporalAsOf/FromTo/Between/ContainedIn/All`) → corresponding `SqlServerHints.TemporalTable*` calls wrapping an `AsSqlServer(dc.GetTable<T>())` expression.
- `Include` / `ThenInclude` (typed + string) → linq2db `LoadWith` / `ThenLoadFromSingle` / `ThenLoadFromMany`.
- `IgnoreQueryFilters` → linq2db `IgnoreFilters`.
- `AsNoTracking` / `AsNoTrackingWithIdentityResolution` → sets `Tracking = false`, strips from tree.
- `AsTracking` → sets `Tracking = true`, strips from tree.
- `TagWith` → linq2db `TagQuery`.
- `AsSplitQuery` / `AsSingleQuery` (EF8+) → stripped (no linq2db equivalent).
- `EF.Property<T>` → linq2db `Sql.Ext.Property<T>`.
- `[NotParameterized]`-marked method parameters wrapped in `Sql.ToSql(...)`.
- External `IQueryable`-returning method calls (not from linq2db assembly) that can be evaluated → expression replaced with the evaluated queryable's expression.
- `LinqExtensions.RemoveOrderBy` → sets `IgnoreTracking = true` (signals eager-loading nested query context).

`CanBeValuatedVisitor` (thin wrapper over `CanBeEvaluatedOnClientCheckVisitorBase` from EXPR-TRANS) tests whether a sub-expression can be safely evaluated on the client without linq2db pipeline involvement.

### Data connection: `LinqToDBForEFToolsDataConnection`

`Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsDataConnection.cs` — extends `DataConnection`, implements `IEntityServiceInterceptor`. Registered as a query-expression interceptor (inner `ExpressionInterceptor`) that calls `TransformExpression` on each query expression before linq2db processes it. Also registered as `IEntityServiceInterceptor` when `EnableChangeTracker` is true: on `EntityCreated`, looks up the entity in EF's `IStateManager` by primary key (compiled expression cached in `_entityKeyGetterCache: IMemoryCache`) and either returns the existing tracked entry or starts tracking the new entity via `StartTrackingFromQuery`. Guards: skips temporary tables, cross-server tables, tables with mismatched names, and `NoTracking` contexts. `CopyDatabaseProperties()` copies EF's command timeout into `DataConnection.CommandTimeout`.

### Options integration: `LinqToDBOptionsExtension` / `LinqToDBContextOptionsBuilder`

`LinqToDBOptionsExtension` implements `IDbContextOptionsExtension`. Stores `DataOptions` (linq2db options, including interceptors and mapping schema). Applied via `UseLinqToDB(DbContextOptionsBuilder, ...)`. `LinqToDBContextOptionsBuilder` fluent builder that adds interceptors (`AddInterceptor`), mapping schemas (`AddMappingSchema`), or arbitrary `DataOptions` mutations (`AddCustomOptions`) by mutating `LinqToDBOptionsExtension.Options`.

`GetLinqToDBOptions(DbContext)` retrieves stored options: `context.Database` → `IInfrastructure<IServiceProvider>` → `IDbContextOptions` → `FindExtension<LinqToDBOptionsExtension>()?.Options`.

### `LinqToDBExtensionsAdapter`

`Source/LinqToDB.EntityFrameworkCore/LinqToDBExtensionsAdapter.cs` — implements `IExtensionsAdapter`. Plugged into `LinqExtensions.ExtensionsAdapter` during `Initialize()`. Routes all linq2db async extension calls (`ToListAsync`, `ForEachAsync`, etc.) through `EntityFrameworkQueryableExtensions` when the source is still an EF queryable — i.e., before `ToLinqToDB()` has been called. This ensures EF's own async pipeline handles unintercepted queries correctly.

### Async extension surfaces

Two parallel async extension classes:
- `LinqToDBForEFExtensions` (`LinqToDBForEFExtensions.Async.cs`) — `...LinqToDB` suffix. Calls `.ToLinqToDB()` on source before delegating to `AsyncExtensions`. For users who want linq2db execution explicitly.
- `EFForEFExtensions` (`EFForEFExtensions.Async.EF.cs`) — `...EF` suffix. Delegates directly to `EntityFrameworkQueryableExtensions`. For users who want EF's own execution without routing through linq2db.

Both are `static partial class` (single partial each in this area). The distinction matters when the same project imports both namespaces and needs to call each engine's async path without ambiguity.

### DTOs: `EFProviderInfo`, `EFConnectionInfo`, `LinqToDBProviderInfo`

- `EFProviderInfo`: `DbConnection?`, `DbTransaction?`, `DbContext?`, `IDbContextOptions?` — snapshot of EF's connectivity context.
- `EFConnectionInfo`: `DbConnection?`, `DbTransaction?`, `ConnectionString?` — extracted from `EFProviderInfo` (via `RelationalOptionsExtension`).
- `LinqToDBProviderInfo`: `ProviderName?`, `Version?` — merged from three detection sources; `Merge()` fills nulls.

### Compat shim: `Compat/Polyfills.cs`

Provides `ArgumentNullException.ThrowIfNull` for `NETSTANDARD2_0` and `NETFRAMEWORK` targets (absent in .NET Standard 2.0 / .NET 4.x BCL). Emitted only under `#if NETSTANDARD2_0 || NETFRAMEWORK` — no-op on modern runtimes.

### `ReflectionMethods`

Cache of reflected `MethodInfo` / `ConstructorInfo` for EF and linq2db methods used in expression rewriting: `Include`, `ThenInclude`, `IgnoreQueryFilters`, `AsNoTracking`, `AsTracking`, `TagWith`, `EF.Property`, `AsSplitQuery`, `AsSingleQuery`, `AsNoTrackingWithIdentityResolution` (EF8+), `IDataContext.FromSql`, `DataParameter..ctor`, `Sql.ToSql`, `DbSet<T>.ToLinqToDBTable`, temporal SQL Server extension methods (EF8+), and the reflection lambda for `RelationalQueryContextFactory.Dependencies`/`_dependencies`.

## Subsystems

1. **Initialization** — `LinqToDBForEFTools.InitializeInternal` hooks `LinqExtensions.ProcessSourceQueryable` and sets `ExtensionsAdapter`. Runs once per app domain via `Lazy<bool>`.
2. **Provider resolution** — `LinqToDBForEFToolsImplDefault.GetDataProvider` → `GetLinqToDBProviderInfo` (3-source merge) → `CreateLinqToDBDataProvider` switch.
3. **Metadata bridging** — `EFCoreMetadataReader` translates `IModel` into `MappingAttribute[]`, including EF `ValueConverter`, `AssociationAttribute`, `QueryFilterAttribute`, and provider DB-function discovery.
4. **Expression transformation** — `TransformExpressionVisitor` rewrites EF expression nodes to linq2db equivalents before the query pipeline.
5. **Change tracking** — `LinqToDBForEFToolsDataConnection` as `IEntityServiceInterceptor` attaches loaded entities to EF's `IStateManager`.
6. **Options DI** — `LinqToDBOptionsExtension` + `LinqToDBContextOptionsBuilder` store linq2db `DataOptions` inside EF's `DbContextOptions` chain.

## Files (Tier 1 / Tier 2)

### Tier 1

| Status | File |
|---|---|
| Visited (on disk) | `Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.cs` |
| Missing (pinned, not on disk) | `LinqToDBForEFExtensions.cs` — replaced by `LinqToDBForEFExtensions.Async.cs` on disk |

### Tier 2 (all visited — 22/22)

| File | Role |
|---|---|
| `EFConnectionInfo.cs` | DTO: extracted connection info |
| `EFCoreMetadataReader.cs` | `IMetadataReader` bridging EF `IModel` |
| `EFForEFExtensions.Async.EF.cs` | Async ext methods routing through EF engine |
| `EFProviderInfo.cs` | DTO: EF provider snapshot |
| `ILinqToDBForEFTools.cs` | Extensibility interface |
| `LinqToDB.EntityFrameworkCore.EF3.csproj` | EF3.1 package target |
| `LinqToDB.EntityFrameworkCore.EF8.csproj` | EF8 package target |
| `LinqToDB.EntityFrameworkCore.EF9.csproj` | EF9 package target |
| `LinqToDB.EntityFrameworkCore.EF10.csproj` | EF10 package target |
| `LinqToDB.EntityFrameworkCore.props` | Shared project props |
| `LinqToDBContextOptionsBuilder.cs` | Fluent options builder |
| `LinqToDBExtensionsAdapter.cs` | `IExtensionsAdapter` routing EF async |
| `LinqToDBForEFExtensions.Async.cs` | Async ext methods routing through linq2db |
| `LinqToDBForEFTools.ContextExtensions.cs` | BulkCopy, GetTable, options on `DbContext` |
| `LinqToDBForEFTools.ContextOptionsBuilderExtensions.cs` | `UseLinqToDB` on `DbContextOptionsBuilder` |
| `LinqToDBForEFTools.Extensions.cs` | `ToLinqToDBTable` on `DbSet<T>` |
| `LinqToDBForEFTools.Mapping.cs` | `DbFunctions.Like` → `Sql.Like` mapping |
| `LinqToDBForEFToolsDataConnection.cs` | `DataConnection` + change-tracker interceptor |
| `LinqToDBForEFToolsDataContext.cs` | `DataContext` variant (unused code path) |
| `LinqToDBForEFToolsException.cs` | Domain exception type |
| `LinqToDBForEFToolsImplDefault.cs` | Default bridge implementation |
| `LinqToDBProviderInfo.cs` | DTO: linq2db provider name + version |
| `Compat/Polyfills.cs` | `ArgumentNullException.ThrowIfNull` shim |
| `Internal/CanBeValuatedVisitor.cs` | Client-evaluability checker |
| `Internal/LinqToDBForEFQueryProvider.cs` | Bridging `IAsyncQueryProvider` + linq2db |
| `Internal/LinqToDBOptionsExtension.cs` | `IDbContextOptionsExtension` for linq2db options |
| `Internal/ReflectionMethods.cs` | Cached EF/linq2db `MethodInfo` constants |
| `Internal/TransformExpressionVisitor.cs` | EF → linq2db expression rewriter |

## Inbound / outbound dependencies

**Inbound:**
- `TESTS-EFCORE` — all EFCore tests exercise this area directly.
- Application code calling `context.ToLinqToDB()`, `context.BulkCopy()`, `UseLinqToDB()`.

**Outbound (linq2db core):**
- `MAPPING` — `MappingSchema`, `MappingAttribute` subclasses (`TableAttribute`, `ColumnAttribute`, `AssociationAttribute`, `QueryFilterAttribute`, `ValueConverterAttribute`, `InheritanceMappingAttribute`).
- `METADATA` — `IMetadataReader` (this area provides an implementation bridging EF).
- `DATA` — `DataConnection`, `DataContext`, `DataOptions`, `BulkCopyOptions`.
- `LINQ` — `Internals.CreateExpressionQueryInstance`, `IQueryProviderAsync`, `IExpressionQuery`, `LinqExtensions.ProcessSourceQueryable`, `LinqExtensions.ExtensionsAdapter`.
- `INTERCEPTORS` — `IEntityServiceInterceptor`, `IQueryExpressionInterceptor`.
- `EXPR` — `LinqExtensions`, `IExtensionsAdapter`.
- `PROV-*` — `CreateLinqToDBDataProvider` calls `SqlServerTools`, `MySqlTools`, `PostgreSQLTools`, `SQLiteTools`, `FirebirdTools`, `DB2Tools`, `OracleTools`, `SqlCeTools`.

## Known issues / debt

- `LinqToDBForEFToolsDataContext` (`DataContext` subclass) is constructed but the code paths that would use it instead of `LinqToDBForEFToolsDataConnection` are commented out (see `CreateLinqToDBContext` / `CreateLinqToDBConnection` — the "special case" block). Dead code path.
- `GetCurrentContext(IQueryable)` uses private reflection (`EntityQueryProvider._queryCompiler`, `QueryCompiler._queryContextFactory`, `RelationalQueryContextFactory.Dependencies/_dependencies`) with an explicit EF version fork. Fragile: can break on EF internals rename without a compilation error. Guarded by `LinqToDBForEFToolsException` with a descriptive message when the field is absent.
- Provider mapping gap: Informix, SAP HANA, Sybase, and ClickHouse providers are not handled in `CreateLinqToDBDataProvider`. A TODO comment in source confirms this. Attempting to use those EF providers with this bridge throws `LinqToDBForEFToolsException`.
- `EFForEFExtensions.Async.EF.cs` returns `Task<TSource>` (not nullable) for `MinAsync`/`MaxAsync`, while the EF extension returns `Task<TSource?>` (nullable); `#pragma disable CS8619` is present on the mismatch sites — a nullable annotation gap.
- `LinqToDBOptionsExtension.GetServiceProviderHashCode()` always returns 0 — means EF will reuse the same service provider across all `LinqToDBOptionsExtension` instances regardless of content. Acceptable for the current design (linq2db options are not EF service-provider inputs) but worth noting.
- `NpgsqlEnumTypeMapping` handling in `DefineConvertors` uses `string.Equals(mapping.GetType().Name, ...)` (reflection by name) rather than a hard interface type, because the Npgsql EFCore package is not a direct compile-time dependency.
- `PgBinaryExpression` handling in `EFCoreMetadataReader.ConvertToExpressionAttribute` similarly uses type-name strings. There is an unreachable duplicate block: after the exhaustive switch on `operand`, there is another redundant `switch(operand)` that only handles `Contains`, `ContainedBy`, and `Overlaps` with the same values. Dead code.

## See also

- [METADATA area INDEX](../METADATA/INDEX.md) — `IMetadataReader` contract that `EFCoreMetadataReader` implements.
- [MAPPING area INDEX](../MAPPING/INDEX.md) — `MappingSchema` that receives the EF-derived attributes.
- [INTERCEPTORS area INDEX](../INTERCEPTORS/INDEX.md) — `IEntityServiceInterceptor`, `IQueryExpressionInterceptor` contracts.
- [PROV-SQLSERVER area INDEX](../PROV-SQLSERVER/INDEX.md) — primary EF+linq2db provider; temporal table support originates here.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 1 / 2 (50%)
  - Visited: Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.cs
  - Missing (not on disk): LinqToDBForEFExtensions.cs — see UNCLASSIFIED-FILE block; `LinqToDBForEFExtensions.Async.cs` is the actual on-disk file and was read as Tier 2
- Tier 2 (visited / total): 22 / 22 ✓
  - Source/LinqToDB.EntityFrameworkCore/EFConnectionInfo.cs
  - Source/LinqToDB.EntityFrameworkCore/EFCoreMetadataReader.cs
  - Source/LinqToDB.EntityFrameworkCore/EFForEFExtensions.Async.EF.cs
  - Source/LinqToDB.EntityFrameworkCore/EFProviderInfo.cs
  - Source/LinqToDB.EntityFrameworkCore/ILinqToDBForEFTools.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDB.EntityFrameworkCore.EF3.csproj
  - Source/LinqToDB.EntityFrameworkCore/LinqToDB.EntityFrameworkCore.EF8.csproj
  - Source/LinqToDB.EntityFrameworkCore/LinqToDB.EntityFrameworkCore.EF9.csproj
  - Source/LinqToDB.EntityFrameworkCore/LinqToDB.EntityFrameworkCore.EF10.csproj
  - Source/LinqToDB.EntityFrameworkCore/LinqToDB.EntityFrameworkCore.props
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBContextOptionsBuilder.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBExtensionsAdapter.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFExtensions.Async.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.ContextExtensions.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.ContextOptionsBuilderExtensions.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.Extensions.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.Mapping.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsDataConnection.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsDataContext.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsException.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsImplDefault.cs
  - Source/LinqToDB.EntityFrameworkCore/LinqToDBProviderInfo.cs
  - Source/LinqToDB.EntityFrameworkCore/Compat/Polyfills.cs
  - Source/LinqToDB.EntityFrameworkCore/Internal/CanBeValuatedVisitor.cs
  - Source/LinqToDB.EntityFrameworkCore/Internal/LinqToDBForEFQueryProvider.cs
  - Source/LinqToDB.EntityFrameworkCore/Internal/LinqToDBOptionsExtension.cs
  - Source/LinqToDB.EntityFrameworkCore/Internal/ReflectionMethods.cs
  - Source/LinqToDB.EntityFrameworkCore/Internal/TransformExpressionVisitor.cs
- Tier 3 (skipped, logged): 0
- Note: PublicAPI/ txt files exist under Source/LinqToDB.EntityFrameworkCore/PublicAPI/ (5 TFM subdirs + root) but contain no code — classified as baseline data files, not Tier 2 .cs files; not counted.
</details>
