---
area: EFCORE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 1/2
coverage_tier_2: 24/24
---

# EFCORE

EFCore companion library. Single source tree compiled to four NuGet packages (`linq2db.EntityFrameworkCore`) -- one per EF major version (EF 3.1, 8, 9, 10). Provides linq2db SQL generation, bulk-copy, DML, and LINQ extensions as a drop-in complement to an existing `DbContext`.

## Multi-EF compilation strategy

Four `.csproj` files share one `.props` file (`LinqToDB.EntityFrameworkCore.props`) and all source files. Each csproj defines a single `DefineConstants` symbol that gates per-EF API differences:

| csproj | Symbol | TargetFrameworks | EF version |
|---|---|---|---|
| `.EF3.csproj` | `EF31` | `netstandard2.0;net462` | EF Core 3.1 |
| `.EF8.csproj` | `EF8` | `net8.0` | EF Core 8 |
| `.EF9.csproj` | `EF9` | `net8.0` | EF Core 9 (pins `Microsoft.EntityFrameworkCore.Relational` 9.0.0) |
| `.EF10.csproj` | `EF10` | `net10.0` | EF Core 10 |

All four reference `LinqToDB.csproj` (project reference) plus `Microsoft.EntityFrameworkCore.Relational` (package reference, version from `Directory.Build.props` except EF9 which overrides to 9.0.0). The shared `.props` also wires up `PublicAPI/PublicAPI.*.txt` as `AdditionalFiles` for the Roslyn API-compat analyzer. `NoWarn=EF1001` suppresses warnings about use of EF internal APIs.

Per-EF branches appear throughout the source as `#if EF31 ... #else ... #endif` blocks. The most significant divergences:
- `ReflectionMethods.cs`: `FromSqlOnQueryableMethodInfo` (EF31 only), `GetServiceProviderHashCode` return type (`long` in EF31, `int` later), `AsSplitQueryMethodInfo`/`AsSingleQueryMethodInfo` absent in EF31, `ShouldUseSameServiceProvider` absent in EF31.
- `EFCoreMetadataReader.cs`: `GetAttributes(Type)` uses `et.GetTableName()` directly in EF31 vs `StoreObjectIdentifier.Create` in EF8+; `FindDiscriminatorProperty()` vs `GetDiscriminatorProperty()`; annotation provider type is `IMigrationsAnnotationProvider` (EF31) vs `IRelationalAnnotationProvider` (EF8+); `IDiagnosticsLogger` and `DatabaseDependencies` injected only in EF8+; multi-filter via `et.GetDeclaredQueryFilters()` used in EF10; many-to-many skip navigation support (`#if !EF31` throughout `EFCoreMetadataReader.ManyToMany.cs`).
- `LinqToDBForEFToolsDataConnection.cs`: change-tracker snap uses `Snapshot.Empty` (EF8+) vs `ValueBuffer.Empty` (EF31).
- `TransformExpressionVisitor.cs`: `QueryRootExpression`-based dispatch (EF8+) handles `FromSqlQueryRootExpression`, temporal table root expressions; EF31 falls through to `VisitConstant`/`FromSqlOnQueryable`.

## Key types

### Public entry-point: `LinqToDBForEFTools` (partial class, 5 files)

`LinqToDBForEFTools.cs` -- static partial class. Initialized once via `Lazy<bool> _initialized`. Initialization (`InitializeInternal`) does two things: calls `InitializeMapping()` (maps `DbFunctions.Like` -> `Sql.Like`) and installs `LinqExtensions.ProcessSourceQueryable` -- a delegate that intercepts any EF `IQueryable` that is not already a `IQueryProviderAsync` and reroutes it through a new linq2db `ExpressionQuery` attached to a fresh `LinqToDBForEFToolsDataConnection`. Also sets `LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter()`.

Static `Implementation` property (type `ILinqToDBForEFTools`) is the strategy object. Default is `LinqToDBForEFToolsImplDefault`.

Two public `GetMappingSchema` overloads on `LinqToDBForEFTools`:
- `GetMappingSchema(IModel, IInfrastructure<IServiceProvider>?, DataOptions?)` -- extracts `IValueConverterSelector` and `IRelationalTypeMappingSource` from the accessor, then delegates to `Implementation.GetMappingSchema`. Used when the EF service provider is available (e.g. inside `CreateLinqToDBConnection`).
- `GetMappingSchema(IModel, IRelationalTypeMappingSource?, IValueConverterSelector?, DataOptions?)` -- takes explicit service instances; calls `GetMetadataReader(model, null)` (no accessor). Used in `EFCoreMetadataReader` itself and from test paths where services are pre-extracted.

Per-partial responsibilities:
- `LinqToDBForEFTools.cs` -- core: `Initialize`, `Implementation`, `GetMetadataReader`, `GetEFProviderInfo`, `GetDataProvider`, `GetMappingSchema`, `TransformExpression`, `CreateLinqToDBConnection`, `CreateLinqToDBContext`, `CreateLinqToDBConnectionDetached`, `ToLinqToDB`, `GetCurrentContext`, `EnableChangeTracker`.
- `LinqToDBForEFTools.ContextExtensions.cs` -- `BulkCopy`/`BulkCopyAsync` on `DbContext`, `Into<T>`, `GetTable<T>`, `GetLinqToDBOptions`, `GetDbContextOptions`.
- `LinqToDBForEFTools.ContextOptionsBuilderExtensions.cs` -- `UseLinqToDB` on `DbContextOptionsBuilder` (installs `LinqToDBOptionsExtension`).
- `LinqToDBForEFTools.Extensions.cs` -- `ToLinqToDBTable` on `DbSet<T>`.
- `LinqToDBForEFTools.Mapping.cs` -- `InitializeMapping`: maps `DbFunctions.Like`.

### Interface: `ILinqToDBForEFTools`

Extensibility seam. Key methods: `GetDataProvider`, `CreateMetadataReader`, `GetMappingSchema`/`CreateMappingSchema`, `TransformExpression`, `GetCurrentContext(IQueryable)`, `ExtractConnectionInfo`, `ExtractModel`, `CreateLogger`, `LogConnectionTrace`.

### Default implementation: `LinqToDBForEFToolsImplDefault`

`public class`, virtualizable throughout. Caches:
- `_knownProviders`: `ConcurrentDictionary<ProviderKey, IDataProvider>` keyed on `(providerName, connectionString)`.
- `_schemaCache`: `MemoryCache` (1-hour sliding expiration) keyed on `(DataOptions, IModel, IRelationalTypeMappingSource, IMetadataReader, IValueConverterSelector, EnableChangeTracker)`.

Provider detection chain: `GetDataProvider` checks `DataOptions.ConnectionOptions.DataProvider` first (explicit override), then `ProviderName` string, then `GetLinqToDBProviderInfo` (three sources merged via `LinqToDBProviderInfo.Merge`).

EF provider name -> linq2db `ProviderName` mapping: SqlServer, Pomelo/Devart MySql, Npgsql/Devart PostgreSQL, Sqlite/Devart, Firebird, IBM DB2LUW, Devart Oracle, Jet (Access), SqlServerCompact (SqlCe). Not supported (documented TODO): Informix, SAP HANA, Sybase, ClickHouse.

`CreateLinqToDBDataProvider` dispatches on `LinqToDBProviderInfo.ProviderName`. SQL Server always uses `Microsoft.Data.SqlClient`; SQLite resolves to `SQLiteProvider.Microsoft`. Default SQL Server / PostgreSQL versions are `AutoDetect`. Added version cases: `SqlServerVersion.v2025` (`ProviderName.SqlServer2025`), `PostgreSQLVersion.v18` (`ProviderName.PostgreSQL18`), `FirebirdVersion.v4`/`v5`, `MySqlVersion.MariaDB10` (routes to `MySqlProvider.MySqlConnector`).

`DefineConvertors` / `CreateMappingSchema`: iterates all EF entity CLR types, uses `IValueConverterSelector.Select` to import EF value converters into `MappingSchema`. Npgsql enum label mappings get special treatment.

`EnableChangeTracker` is a virtual instance property on `LinqToDBForEFToolsImplDefault` (default `true`); the static `LinqToDBForEFTools.EnableChangeTracker` delegates to `Implementation.EnableChangeTracker`.

### Metadata bridge: `EFCoreMetadataReader` (partial class -- main + ManyToMany)

`internal sealed partial class`, implements `IMetadataReader`. Bridges EF `IModel` into linq2db's `MappingAttribute` model.

`GetAttributes(Type)`: emits `TableAttribute`, `QueryFilterAttribute` (EF `GetQueryFilter()` / `GetDeclaredQueryFilters()` for EF10+), `InheritanceMappingAttribute`. For EF8+, when `type` is an `EfJoinTable<,,>` marker, returns only a `TableAttribute` for the join entity's store object -- bypasses normal entity lookup. Query filter rewrite replaces `DbContext`-typed sub-expressions. Falls back to `[Table]` annotation if entity not in model. EF8+: table-name resolution goes through private `GetStoreObjectIdentifier(IEntityType)` (`EFCoreMetadataReader.cs:644`), which tries `StoreObjectType.Table` then falls back to `StoreObjectType.View` (handles view-mapped entities).

`GetAttributes(Type, MemberInfo)`: emits `ColumnAttribute`, identity detection, `ValueConverterAttribute`, `AssociationAttribute` from EF navigation FKs. For EF8+, also emits `AssociationAttribute` with `QueryExpression` for many-to-many skip navigations (`GetSkipNavigations()`) via `BuildManyToManyQueryExpression`. For `EfJoinTable<,,>` marker types, delegates to `BuildJoinColumnAttribute` for `DynamicColumnInfo` members. `HasSpanTypes`/`IsSpan` guard skips `Span<T>`/`ReadOnlySpan<T>` members. `ConvertToExpressionAttribute` serializes EF `SqlExpression` trees to linq2db `{0}`/`{1}` placeholders; `UnwrapConverted` strips a top-level COALESCE null-guard wrapper before serialization. Special Npgsql `PgBinaryExpression` operator-type mapping. `Sql.FunctionAttribute` emission via `IModel.GetDbFunctions()` and `[DbFunctionAttribute]`.

`GetDynamicColumns(Type)`: for EF8+, when `type` is an `EfJoinTable<,,>` marker, returns `DynamicColumnInfo` entries for all FK properties of both sides; otherwise returns empty.

`GetObjectID()` returns a composite hash of injected service instances (used as `MappingSchema` cache key).

### Many-to-many support: `EfJoinTable<TThis, TOther, TJoin>` + `EFCoreMetadataReader.ManyToMany.cs`

EF8+ only (`#if !EF31`). EF models many-to-many relationships through a hidden join entity (often `Dictionary<string, object>` for implicit relationships). The `EfJoinTable<TThis, TOther, TJoin>` marker (`Internal/EfJoinTable.cs`) creates a distinct CLR type per relationship -- necessary because EF's implicit join entity shares a single CLR type across all implicit M:N relationships.

`EfJoinTable<TThis, TOther, TJoin>`: `internal sealed class` with a single `[DynamicColumnsStore] IDictionary<string, object> Values` property. The three generic parameters are: declaring-side entity, target-side entity, EF join CLR type (discriminator). No mapped members -- all columns come from `GetDynamicColumns`.

`EFCoreMetadataReader.ManyToMany.cs` partial adds:
- `ManyToManyJoinInfo` -- nested sealed class holding `JoinEntityType`, `ThisForeignKey`, `OtherForeignKey` from a resolved `ISkipNavigation`.
- `_manyToManyJoins`: `ConcurrentDictionary<Type, ManyToManyJoinInfo?>` cache keyed on the marker CLR type.
- `ResolveManyToManyJoin(Type)`: checks if `type` is `EfJoinTable<,,>`, extracts generic args, walks `GetSkipNavigations()` on the entity to find a matching `ISkipNavigation`. Throws `LinqToDBException` if two *different* join entities have the same CLR type (ambiguous implicit joins). Returns `null` if no match.
- `BuildJoinColumnAttribute(ManyToManyJoinInfo, string)`: looks up a property by name in `ThisForeignKey.Properties` then `OtherForeignKey.Properties`, resolves `DataType` from `RelationalTypeMapping`, emits `ColumnAttribute`.
- `BuildManyToManyQueryExpression(IEntityType, ISkipNavigation)`: builds the association lambda navigating from source through join table to target entities. Uses `Methods.LinqToDB.GetTable`, `Methods.Queryable.Where`, `Methods.Queryable.SelectManySimple`.
- `BuildJoinPredicate(ParameterExpression, ParameterExpression, IForeignKey)`: builds composite equality predicate matching principal key members against FK columns; uses `Sql.Property<T>` (via `BuildSqlProperty`) for shadow/field-mapped keys that have no CLR `PropertyInfo`.

### Query interception: `LinqToDBForEFQueryProvider<T>`

Implements `IAsyncQueryProvider` (EF), `IQueryProviderAsync` (linq2db), `IQueryable<T>`, `IAsyncEnumerable<T>`, `IExpressionQuery`. Bridge queryable returned by `ToLinqToDB<T>()`. Creates a linq2db `ExpressionQuery<T>` via `Internals.CreateExpressionQueryInstance<T>`; delegates `IQueryProvider`/`IQueryProviderAsync` calls to it.

### Expression rewriting: `TransformExpressionVisitor`

Extends `ExpressionVisitorBase`. Entry: `Transform(IDataContext?, IModel?, Expression)`. Key rewrites: `EntityQueryable<T>`/`DbSet<T>` -> `GetTable<T>`; `QueryRootExpression` (EF8+) -> `FromSql<T>` / temporal-table hints; `Include`/`ThenInclude` -> `LoadWith`/`ThenLoad*`; `IgnoreQueryFilters` -> `IgnoreFilters`; `AsNoTracking*`/`AsTracking` toggle `Tracking`; `TagWith` -> `TagQuery`; `AsSplitQuery`/`AsSingleQuery` (EF8+) stripped; `EF.Property<T>` -> `Sql.Ext.Property<T>`; `[NotParameterized]` params wrapped in `Sql.ToSql`. `CanBeValuatedVisitor` tests client-evaluability.

### Data connection: `LinqToDBForEFToolsDataConnection`

Extends `DataConnection`, implements `IEntityServiceInterceptor`. Registered as a query-expression interceptor (calls `TransformExpression`) and, when `EnableChangeTracker`, as `IEntityServiceInterceptor` (attaches loaded entities to EF's `IStateManager`). `CopyDatabaseProperties()` copies EF's command timeout.

### Options integration: `LinqToDBOptionsExtension` / `LinqToDBContextOptionsBuilder`

`LinqToDBOptionsExtension` implements `IDbContextOptionsExtension`, stores linq2db `DataOptions`. Applied via `UseLinqToDB`. `LinqToDBContextOptionsBuilder` fluent builder (`AddInterceptor`, `AddMappingSchema`, `AddCustomOptions`). `GetLinqToDBOptions(DbContext)` retrieves stored options. `LinqToDBExtensionInfo.LogFragment` uses a C# 13 `field` keyword-backed property (`LinqToDBOptionsExtension.cs:80`) -- informational, no behavioral impact.

### `LinqToDBExtensionsAdapter`

Implements `IExtensionsAdapter`. Plugged into `LinqExtensions.ExtensionsAdapter` during `Initialize()`. Routes linq2db async extension calls through `EntityFrameworkQueryableExtensions` when the source is still an EF queryable.

### Async extension surfaces

- `LinqToDBForEFExtensions` (`...LinqToDB` suffix) -- calls `.ToLinqToDB()` before delegating to `AsyncExtensions`.
- `EFForEFExtensions` (`...EF` suffix) -- delegates directly to `EntityFrameworkQueryableExtensions`.

### DTOs: `EFProviderInfo`, `EFConnectionInfo`, `LinqToDBProviderInfo`

- `EFProviderInfo`: `DbConnection?`, `DbTransaction?`, `DbContext?`, `IDbContextOptions?`.
- `EFConnectionInfo`: `DbConnection?`, `DbTransaction?`, `ConnectionString?`.
- `LinqToDBProviderInfo`: `ProviderName?`, `Version?`; `Merge()` fills nulls.

### Compat shim: `Compat/Polyfills.cs`

`ArgumentNullException.ThrowIfNull` for `NETSTANDARD2_0` / `NETFRAMEWORK`. No-op on modern runtimes.

### `ReflectionMethods`

Cache of reflected `MethodInfo`/`ConstructorInfo` for EF and linq2db methods used in expression rewriting.

## Subsystems

1. **Initialization** -- `LinqToDBForEFTools.InitializeInternal` hooks `LinqExtensions.ProcessSourceQueryable` and sets `ExtensionsAdapter`. Once per app domain via `Lazy<bool>`.
2. **Provider resolution** -- `GetDataProvider` -> `GetLinqToDBProviderInfo` (3-source merge) -> `CreateLinqToDBDataProvider`.
3. **Metadata bridging** -- `EFCoreMetadataReader` translates `IModel` into `MappingAttribute[]`. View-mapped entities via `GetStoreObjectIdentifier` (EF8+); span members skipped; COALESCE wrappers stripped by `UnwrapConverted`. Many-to-many skip navigations emit `AssociationAttribute` with `QueryExpression` via `BuildManyToManyQueryExpression`; join-table markers (`EfJoinTable<,,>`) are materialized as dynamic-column tables.
4. **Expression transformation** -- `TransformExpressionVisitor` rewrites EF nodes to linq2db equivalents.
5. **Change tracking** -- `LinqToDBForEFToolsDataConnection` attaches loaded entities to EF's `IStateManager`.
6. **Options DI** -- `LinqToDBOptionsExtension` + `LinqToDBContextOptionsBuilder` store `DataOptions` in EF's `DbContextOptions` chain.

## Files (Tier 1 / Tier 2)

### Tier 1

| Status | File |
|---|---|
| Visited (on disk) | `Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.cs` |
| Missing (pinned, not on disk) | `LinqToDBForEFExtensions.cs` -- replaced by `LinqToDBForEFExtensions.Async.cs` on disk |

### Tier 2 (all visited -- 24/24)

`EFConnectionInfo.cs`, `EFCoreMetadataReader.cs`, `EFCoreMetadataReader.ManyToMany.cs` (new), `EFForEFExtensions.Async.EF.cs`, `EFProviderInfo.cs`, `ILinqToDBForEFTools.cs`, the 4 `.csproj` + `.props`, `LinqToDBContextOptionsBuilder.cs`, `LinqToDBExtensionsAdapter.cs`, `LinqToDBForEFExtensions.Async.cs`, the 5 `LinqToDBForEFTools.*.cs` partials, `LinqToDBForEFToolsDataConnection.cs`, `LinqToDBForEFToolsDataContext.cs`, `LinqToDBForEFToolsException.cs`, `LinqToDBForEFToolsImplDefault.cs`, `LinqToDBProviderInfo.cs`, `Compat/Polyfills.cs`, `Internal/CanBeValuatedVisitor.cs`, `Internal/EfJoinTable.cs` (new), `Internal/LinqToDBForEFQueryProvider.cs`, `Internal/LinqToDBOptionsExtension.cs`, `Internal/ReflectionMethods.cs`, `Internal/TransformExpressionVisitor.cs`.

## Inbound / outbound dependencies

**Inbound:**
- `TESTS-EFCORE` -- all EFCore tests exercise this area directly.
- Application code calling `context.ToLinqToDB()`, `context.BulkCopy()`, `UseLinqToDB()`.

**Outbound (linq2db core):**
- `MAPPING` -- `MappingSchema`, `MappingAttribute` subclasses.
- `METADATA` -- `IMetadataReader` (this area provides an EF-bridging impl).
- `DATA` -- `DataConnection`, `DataContext`, `DataOptions`, `BulkCopyOptions`.
- `LINQ` -- `Internals.CreateExpressionQueryInstance`, `IQueryProviderAsync`, `IExpressionQuery`, `LinqExtensions.ProcessSourceQueryable`/`ExtensionsAdapter`.
- `INTERCEPTORS` -- `IEntityServiceInterceptor`, `IQueryExpressionInterceptor`.
- `EXPR` -- `LinqExtensions`, `IExtensionsAdapter`.
- `PROV-*` -- `CreateLinqToDBDataProvider` calls `SqlServerTools`, `MySqlTools`, `PostgreSQLTools`, `SQLiteTools`, `FirebirdTools`, `DB2Tools`, `OracleTools`, `SqlCeTools`.

## Known issues / debt

- `LinqToDBForEFToolsDataContext` (`DataContext` subclass) constructed but its use-path is commented out -- dead code path.
- `GetCurrentContext(IQueryable)` uses private reflection with explicit EF version fork; fragile across EF internals renames. Guarded by `LinqToDBForEFToolsException`.
- Provider mapping gap: Informix, SAP HANA, Sybase, ClickHouse not handled in `CreateLinqToDBDataProvider` (TODO in source).
- `EFForEFExtensions.Async.EF.cs` returns non-nullable `Task<TSource>` for `MinAsync`/`MaxAsync` while EF returns `Task<TSource?>`; `#pragma disable CS8619` present.
- `LinqToDBOptionsExtension.GetServiceProviderHashCode()` always returns 0 (DI-0308 area; acceptable for current design).
- `NpgsqlEnumTypeMapping` handling in `DefineConvertors` uses type-name strings (Npgsql EFCore not a compile-time dependency).
- `PgBinaryExpression` handling in `EFCoreMetadataReader.ConvertToExpressionAttribute` uses type-name strings; unreachable duplicate `switch(operand)` block at line 898 (dead code). DI-0763 tracks the type-name-string-match smell across lines 315/661/809 (cross-ref GH #4652).
- DI-0764: `LinqToDBOptionsExtension.ApplyServices` (`:44`) XML doc lacks a `<summary>` wrapper.
- Many-to-many: `ResolveManyToManyJoin` throws `LinqToDBException` for multiple implicit relationships between the same ordered pair that use *different* join entity CLR types -- explicit join entities required in that case.

## See also

- [METADATA area INDEX](../METADATA/INDEX.md) -- `IMetadataReader` contract.
- [MAPPING area INDEX](../MAPPING/INDEX.md) -- `MappingSchema` receiving EF-derived attributes.
- [INTERCEPTORS area INDEX](../INTERCEPTORS/INDEX.md) -- `IEntityServiceInterceptor`, `IQueryExpressionInterceptor`.
- [PROV-SQLSERVER area INDEX](../PROV-SQLSERVER/INDEX.md) -- temporal table support.

## Pointers

- Many-to-many association query expression entry: `EFCoreMetadataReader.ManyToMany.cs` -> `BuildManyToManyQueryExpression` -> emitted as `AssociationAttribute.QueryExpression` on skip navigation members.
- Join-table marker type: `Internal/EfJoinTable.cs` -- `EfJoinTable<TThis, TOther, TJoin>` -- `[DynamicColumnsStore]` on `Values`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 1 / 2 (50%)
  - Visited: Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFTools.cs
  - Missing (not on disk): LinqToDBForEFExtensions.cs -- `LinqToDBForEFExtensions.Async.cs` is the actual on-disk file, read as Tier 2 (pre-existing pinned-vs-disk mismatch, not introduced this delta)
- Tier 2 (visited / total): 24 / 24
- Tier 3 (skipped, logged): 0
- Note: PublicAPI/*.txt files (5 TFM subdirs + root) contain no code -- baseline data files, not counted.

Read (prior run):
  - `EFCoreMetadataReader.cs` -- added `GetStoreObjectIdentifier` view-fallback helper, `HasSpanTypes`/`IsSpan` span guard, `UnwrapConverted` COALESCE-strip helper, `Sql.FunctionAttribute` emission from `IModel.GetDbFunctions()` and `[DbFunctionAttribute]`; dead duplicate switch at line 851 confirmed still present.
  - `Internal/LinqToDBOptionsExtension.cs` -- `GetServiceProviderHashCode()` still returns 0; `LogFragment` uses C# 13 `field` keyword-backed property.
  - `LinqToDBForEFTools.cs` -- confirmed two distinct public `GetMappingSchema` overloads; no other structural changes.
  - `PublicAPI/PublicAPI.Shipped.txt` -- release-promotion churn; no structural changes to public surface.

Read (this run -- delta):
  - `EFCoreMetadataReader.ManyToMany.cs` (new) -- full many-to-many partial: `ManyToManyJoinInfo`, `ResolveManyToManyJoin`, `BuildJoinColumnAttribute`, `BuildManyToManyQueryExpression`, `BuildJoinPredicate`, `BuildSqlProperty`; EF8+ only (`#if !EF31`).
  - `EFCoreMetadataReader.cs` -- now a `partial class`; added `_manyToManyJoins` cache field; `GetAttributes(Type)` has early-return for `EfJoinTable<,,>` markers; `GetAttributes(Type, MemberInfo)` has skip-navigation `AssociationAttribute` emission and `DynamicColumnInfo` branch for marker types; `GetDynamicColumns` implemented for many-to-many; dead duplicate switch now at line 898.
  - `Internal/EfJoinTable.cs` (new) -- `EfJoinTable<TThis, TOther, TJoin>` marker with `[DynamicColumnsStore] IDictionary<string, object> Values`; EF8+ only.
  - `LinqToDBForEFToolsDataConnection.cs` -- no structural changes; existing INDEX.md description confirmed accurate.
  - `LinqToDBForEFToolsImplDefault.cs` -- added provider version cases: `SqlServer2025` -> `SqlServerVersion.v2025`; `PostgreSQL18` -> `PostgreSQLVersion.v18`; `Firebird4`/`Firebird5`; `MariaDB10` -> `MySqlVersion.MariaDB10` / `MySqlProvider.MySqlConnector`. `EnableChangeTracker` confirmed as virtual instance property (default `true`).
</details>
