---
area: TESTS-EFCORE
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 0/0
coverage_tier_2: 131/171
---

# TESTS-EFCORE

EFCore integration test suite. Four `.csproj` files (`Tests.EntityFrameworkCore.EF3/EF8/EF9/EF10.csproj`) share a single source tree under `Tests/EntityFrameworkCore/`, compiled against the matching production package (`LinqToDB.EntityFrameworkCore.EF{n}.csproj`). All csprojs also reference `Tests.Base.csproj` (the TESTS-INFRA harness).

## Multi-EF csproj layout

| csproj | TargetFramework | Production ref | Notes |
|---|---|---|---|
| `Tests.EntityFrameworkCore.EF3.csproj` | `net462` | `LinqToDB.EntityFrameworkCore.EF3.csproj` | Pins `Npgsql` 4.1.14 override for transitive vuln |
| `Tests.EntityFrameworkCore.EF8.csproj` | (see props) | `LinqToDB.EntityFrameworkCore.EF8.csproj` | |
| `Tests.EntityFrameworkCore.EF9.csproj` | (see props) | `LinqToDB.EntityFrameworkCore.EF9.csproj` | |
| `Tests.EntityFrameworkCore.EF10.csproj` | `net10.0` | `LinqToDB.EntityFrameworkCore.EF10.csproj` | Removes `Pomelo.EntityFrameworkCore.MySql` (unsupported on EF10) |

The shared `Tests.EntityFrameworkCore.props` (`Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.props`) sets:
- `AssemblyName` = `linq2db.EntityFrameworkCore.Tests`, `RootNamespace` = `LinqToDB.EntityFrameworkCore.Tests`
- References `Tests.Base.csproj` (TESTS-INFRA)
- Package references: `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.SqlServer`, `Pomelo.EntityFrameworkCore.MySql`, `Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime`, `Microsoft.Extensions.Logging.Console`

`#if EF8`-gated source file: `Tests/FSharpTests.cs` -- active only from EF8 onwards.

## Subsystems

### Utilities (`Tests/EntityFrameworkCore/Utilities/`)

EFCore-specific provider-selection attributes and test utilities.

- `EFDataSourcesAttribute` -- extends `DataSourcesBaseAttribute`. `GetProviders()` returns `TestConfiguration.UserProviders` filtered against `TestConfiguration.EFProviders` (exclusion mode). `[EFDataSources]` is the standard parameter attribute used across almost every test fixture.
- `EFIncludeDataSourcesAttribute` -- inclusion-mode variant. `GetProviders()` intersects user providers with `TestConfiguration.EFProviders`. Used for provider-specific tests (`[EFIncludeDataSources(TestProvName.AllSqlServer)]`).
- `TestConfiguration.EFProviders` (`Tests/Base/TestConfiguration.cs:200`) -- the EF-supported provider list: `SQLiteMS`, `AllSqlServer2016PlusMS`, `AllPostgreSQL13Plus`, and `AllMySqlConnector` (excluded on `NET10_0`). All EF test attributes filter through this list.
- `QueryableExtensions` -- two helpers: `AsLinqToDB(bool)` and `AsTracking(bool)`, convenience for conditional `ToLinqToDB()` / `AsNoTracking()` in test code.
- `AAA` -- Arrange/Act/Assert fluent DSL (`ArrangeResult<T,TMock>`, `ActResult<T,TMock>`) used by a subset of tests.
- `ExceptionExtensions` -- single helper `Throw(this Exception)` returning `Unit`; used with `AAA` DSL for act-throws patterns.
- `StringExtensions` -- `ToSnakeCase(string)` using compiled/generated regex; consumed by `ModelBuilderExtensions.UseSnakeCase()`.
- `TypeExtensions` -- `UnwrapNullable(Type?)` stripping `Nullable<T>` wrapper; used in `IdValueConverterSelector` and `ModelBuilderExtensions`.
- `Polyfills` -- `#if NETFRAMEWORK` shims: `HasDatabaseName` for `IndexBuilder`, `ArgumentNullException.ThrowIfNull`, `Enumerable.ToHashSet`.
- `Unit` -- empty sentinel value type returned by `ExceptionExtensions.Throw` to satisfy expression-body return types.

### Test base classes (`Tests/EntityFrameworkCore/`)

- `ContextTestBase<TContext>` (`Tests/EntityFrameworkCore/ContextTestBase.cs`) -- `abstract class` extending `TestBase` (TESTS-INFRA), parameterized on `TContext : DbContext`. Key method: `CreateContext(provider, optionsSetter?, optionsBuilderSetter?)` -- builds `DbContextOptionsBuilder<TContext>`, calls `ProviderSetup()` to wire the EF provider, calls `UseLinqToDB()` when `optionsSetter` is set, then constructs the context. On first call per `connectionString × TContext`, calls `EnsureDeleted()` + `EnsureCreated()` + `OnDatabaseCreated()` to initialize the schema. Uses `TestContextTracker.LastContexts` (static `Dictionary<string, Type>`) to avoid reinitializing databases across tests.

  `ProviderSetup()` dispatches on `provider` to `optionsBuilder.UseNpgsql()`, `.UseMySql()`, `.UseSqlite()`, or `.UseSqlServer()`. Always calls `UseLinqToDB()` for PostgreSQL with `UseMappingSchema(NodaTimeSupport)`. Pomelo (`AllMySql`) is excluded when `#if NET10_0`.

- `NorthwindContextTestBase` (`Tests/EntityFrameworkCore/NorthwindContextTestBase.cs`) -- concrete `ContextTestBase<NorthwindContextBase>`. `CreateProviderContext()` dispatches provider string to the matching per-provider `NorthwindContext` subclass. `OnDatabaseCreated()` calls `NorthwindData.Seed(context)`.

### Test fixtures (`Tests/EntityFrameworkCore/Tests/`)

13 fixtures. All inherit `ContextTestBase<T>` or `NorthwindContextTestBase`:

| Fixture | Base context | Key coverage |
|---|---|---|
| `ToolsTests` | `NorthwindContextTestBase` | Core bridge API: `ToLinqToDB()`, `CreateLinqToDBConnection()`, `Include`/`ThenInclude`, change tracker, `TagWith`, temporal tables, `FromSqlRaw/Interpolated`, `SetUpdate`, DML, async methods |
| `IssueTests` | `ContextTestBase<IssueContextBase>` | Regression tests for EFCore-specific GitHub issues (numbered: Issue73, 117, 321, 340, 4624...5388 etc.) |
| `InterceptorTests` | `NorthwindContextTestBase` | Tests all 5 interceptor surfaces: `ICommandInterceptor`, `IConnectionInterceptor`, `IDataContextInterceptor`, `IEntityServiceInterceptor`, dual EF+linq2db combo interceptor, plus `UseEfCoreRegisteredInterceptorsIfPossible` glue |
| `ForMappingTests` | `ContextTestBase<ForMappingContextBase>` | `EFCoreMetadataReader` mapping: identity detection, skip-on-insert/update, type mapping, bulk-copy, `MERGE`, `BulkCopyType.*`, `SkipModesTable`, `UIntTable` |
| `ConvertorTests` | `ContextTestBase<ConvertorContext>` | Custom `IValueConverterSelector`, strongly-typed `Id<T,U>` round-trips via EF value converters into linq2db |
| `IdTests` | `ContextTestBase<IdTestContext>` | `Models/Shared` entity set with EF-registered `IdValueConverter`; verifies converter import into `MappingSchema` |
| `InheritanceTests` | `ContextTestBase<InheritanceContext>` | Discriminator inheritance + bulk copy (`BulkCopyType.*`) across `Blog`/`RssBlog`/`ShadowBlog`/`ShadowRssBlog` hierarchy |
| `NpgSqlTests` | `ContextTestBase<NpgSqlEntitiesContext>` | Npgsql range functions, arrays, xmin, views, NodaTime, `AT TIME ZONE` |
| `PomeloMySqlTests` | `NorthwindContextTestBase` | MySQL-specific queries via Pomelo; excluded on EF10. Tests: `SimpleProviderTest` (basic `.ToLinqToDB()` query), `TestFunctionTranslation` and `TestFunctionTranslationParameter` (Pomelo issue #1801 -- `string.Contains` translation) |
| `SQLiteTests` | `NorthwindContextTestBase` | SQLite-specific provider selection (issue #343: `UseSQLite(SQLiteProvider.Microsoft)`) |
| `JsonConvertTests` | `ContextTestBase<JsonConvertContext>` | JSON-serialized column round-trips via EF value converters. Single test `TestJsonConvert` ([EFIncludeDataSources(AllSqlServer)]): inserts `EventScheduleItem` with JSON-serialized `LocalizedString` column, queries via `.ToLinqToDB()` projecting `JSON_VALUE` EF db-function, asserts deserialized field values. Has a `//TODO` to support sub-property projection from JSON columns. |
| `FSharpTests` | `ContextTestBase<FSharpContext.AppDbContext>` | F# entity mapping interop; `#if EF8` only; uses `EntityFrameworkCore.FSharp` |
| `CustomContextIssueTests` | `TestBase` (direct) | Tests that corrupt DB state; creates fresh contexts per test via `TestContextTracker` invalidation |

### Interceptors (`Tests/EntityFrameworkCore/Interceptors/`)

Test-only interceptor implementations (all extend `TestInterceptor` base with `HasInterceptorBeenInvoked` flag + `ResetInvocations()`):

- `TestCommandInterceptor` -- implements `ICommandInterceptor` (linq2db); tracks all 10 command lifecycle events.
- `TestConnectionInterceptor` -- implements `IConnectionInterceptor` (linq2db); tracks `ConnectionOpened`, `ConnectionOpenedAsync`, `ConnectionOpening`, `ConnectionOpeningAsync`.
- `TestDataContextInterceptor` -- implements `IDataContextInterceptor` (linq2db); tracks `OnClosed`, `OnClosedAsync`, `OnClosing`, `OnClosingAsync`.
- `TestEntityServiceInterceptor` -- implements `IEntityServiceInterceptor` (linq2db); single event `EntityCreated` -- sets flag and returns entity unchanged.
- `TestEfCoreAndLinqToDBComboInterceptor` -- implements both `ICommandInterceptor` (linq2db) and `IDbCommandInterceptor` (EF Core). Used to verify `UseEfCoreRegisteredInterceptorsIfPossible()` bridges EF-registered interceptors into linq2db.

`Interceptors/Extensions/LinqToDBContextOptionsBuilderExtensions.cs` -- static extension `UseEfCoreRegisteredInterceptorsIfPossible(LinqToDBContextOptionsBuilder)`. Reads `CoreOptionsExtension.Interceptors` from EF's `DbContextOptions` and registers any that are also `IInterceptor` (linq2db) via `builder.AddInterceptor(...)`.

### Logging (`Tests/EntityFrameworkCore/Logging/`)

`TestLoggerProvider` / `TestLogger` / `TestLoggerExtensions` / `LogMessageEntry` -- custom `ILoggerProvider` + `ILogger` that capture log output to `Tests.BaselinesManager` for baseline comparison. `TestLogger.LogBaselines()` routes messages through `BaselinesManager`. `NullExternalScopeProvider` / `NullScope` -- stubs for scope-less logging in tests.

- `LogMessageEntry` -- `readonly record struct` capturing a single log line: `Message`, optional `TimeStamp`, `LevelString`, `LevelBackground`/`LevelForeground`/`MessageColor` (console colors), `LogAsError` flag.
- `NullExternalScopeProvider` -- singleton `IExternalScopeProvider` implementation; `ForEachScope` and `Push` are no-ops (returns `NullScope.Instance`).
- `NullScope` -- singleton `IDisposable` with empty `Dispose()`; returned by `NullExternalScopeProvider.Push`.
- `TestLoggerExtensions` -- static `ILoggingBuilder.AddTestLogger()` extension; registers `TestLoggerProvider` as `ILoggerProvider` singleton via `TryAddEnumerable` and wires `ConsoleLoggerOptions`.

### Models (`Tests/EntityFrameworkCore/Models/`)

Six model subdirectories, each scoped to specific test scenarios:

| Subdir | Entities | Test fixture |
|---|---|---|
| `Northwind/` | Full Northwind schema (`Category`, `Customer`, `Order`, `OrderDetail`, `Product`, `Employee`, `Territory`, ...); `NorthwindContextBase` with `QueryFilter` + `ISoftDelete` support | `NorthwindContextTestBase` descendants |
| `Northwind/SQLServer/`, `/Pomelo/`, `/PostgreSQL/` | Per-provider `NorthwindContext` subclass + EF Fluent API mapping classes | Provider dispatch in `NorthwindContextTestBase` |
| `IssueModel/` | `IssueContextBase` + per-issue entity classes (Issue73, 117, 321, 4624...5388); per-provider context subclasses under `SQLServer/`, `SQLite/`, `PostgreSQL/`, `Pomelo/` | `IssueTests` |
| `ForMapping/` | `ForMappingContextBase` with identity/no-identity, `UIntTable`, `StringTypes`, `TypesTable`, `WithInheritance`, `SkipModesTable`, `WithDuplicateProperties`; per-provider subclasses | `ForMappingTests` |
| `Inheritance/` | `InheritanceContext` with `Blog/RssBlog` + `ShadowBlog/ShadowRssBlog` discriminator hierarchies | `InheritanceTests` |
| `NpgSqlEntities/` | `NpgSqlEntitiesContext`, `EntityWithArrays`, `EntityWithXmin`, `Event` (range type), `EventView`, `TimeStampEntity` | `NpgSqlTests` |
| `Shared/` | `IdTestContext`, `Entity`, `Item`, `Detail`, `SubDetail`, strongly-typed `Id<T,U>` + `IdValueConverter`, `ModelBuilderExtensions` | `IdTests` |
| `ValueConversion/` | `ConvertorContext`, `SubDivision`, `Id<T,U>`, `IEntity<T>`, `IdValueConverterSelector`, `IdValueConverter<T,U>` | `ConvertorTests` |
| `JsonConverter/` | `JsonConvertContext`, `EventScheduleItem`, `CrashEnum`, `LocalizedString` | `JsonConvertTests` |

#### Models/Northwind -- entity detail

All entities inherit `BaseEntity` (which implements `ISoftDelete` with `IsDeleted bool`). The full entity set:

| Entity | Key | Notable columns / relationships |
|---|---|---|
| `BaseEntity` | -- | `IsDeleted bool`; base for all Northwind entities; `ISoftDelete` global filter in `NorthwindContextBase` |
| `Category` | `CategoryId` | `CategoryName`, `Description`, `Picture byte[]`; nav `Products` |
| `Customer` | `CustomerId (string)` | Address fields; nav `Orders`, `CustomerCustomerDemo` |
| `CustomerCustomerDemo` | `CustomerId + CustomerTypeId` | M:M join between `Customer` and `CustomerDemographics` |
| `CustomerDemographics` | `CustomerTypeId (string)` | `CustomerDesc`; nav `CustomerCustomerDemo` |
| `CustomerOrderHistory` | -- | `ProductName`, `Total`; keyless stored-proc result type |
| `CustomerQuery` | -- | `CompanyName`, `OrderCount`, `SearchTerm`; keyless query result |
| `CustomerView` | -- | Read-only view projection; `IsLondon` computed (`[NotMapped]`) |
| `Employee` | `EmployeeId` | Full HR fields; self-referential `ReportsTo`/`InverseReportsToNavigation` |
| `EmployeeTerritory` | `EmployeeId + TerritoryId` | M:M join between `Employee` and `Territory` |
| `Order` | `OrderId` | Date/shipping fields; nav `Customer`, `Employee`, `ShipViaNavigation`, `OrderDetails` |
| `OrderDetail` | `OrderId + ProductId` | `UnitPrice`, `Quantity`, `Discount`; nav `Order`, `Product` |
| `Product` | `ProductId` | `ProductName`, `Discontinued`; nav `Category`, `Supplier`, `OrderDetails`; SQL Server map uses `IsTemporal()` |
| `Region` | `RegionId` | `RegionDescription`; nav `Territories` |

`NorthwindData` -- partial class seeding the DB: `Seed(DbContext)` / `SeedAsync(DbContext)` call `AddEntities()` which uses `EF1001`-suppressed internal shadow-property access for `Employee.Title`. Inner `AsyncEnumerable<T>` implements `IAsyncQueryProvider` to support in-memory EF query tests; rewrites `EF.Property` shadow-property access via `ShadowStateAccessRewriter : ExpressionVisitor`.

`NorthwindData.Objects.cs` -- oversized data-seeding file (`>1 MB`) containing `CreateCustomers()`, `CreateEmployees()`, `CreateOrders()`, `CreateProducts()`, `CreateOrderDetails()`, `CreateCategories()`, `CreateSupliers()`, `CreateShippers()` static factory arrays. Claimed `<auto-generated>` to suppress analyzer warnings.

#### Models/Northwind/SQLServer -- Fluent API maps

All map classes extend `BaseEntityMap<T> : IEntityTypeConfiguration<T>` which sets `IsDeleted.HasDefaultValue(false)`. Eleven concrete maps covering the full Northwind schema:

| Map class | Entity | Notable config |
|---|---|---|
| `CategoriesMap` | `Category` | `CategoryID` column rename; `CategoryName` index |
| `CustomersMap` | `Customer` | `CustomerID` rename; city/company/postal-code indexes |
| `CustomerCustomerDemoMap` | `CustomerCustomerDemo` | Composite PK non-clustered; FK constraint names |
| `CustomerDemographicsMap` | `CustomerDemographics` | PK non-clustered; `CustomerDesc` as `nvarchar(max)` |
| `EmployeesMap` | `Employee` | `EmployeeID` rename; `Photo`/`Notes` as `varbinary/nvarchar(max)`; self-FK `FK_Employees_Employees` |
| `EmployeeTerritoriesMap` | `EmployeeTerritory` | Composite PK non-clustered; `TerritoryID` max-length 20 |
| `OrderMap` | `Order` | `OrderID` rename; `Freight` as `money`; 6 indexes; 3 FKs |
| `OrderDetailsMap` | `OrderDetail` | `ToTable("Order Details")`; composite PK; `UnitPrice` as `money` |
| `ProductsMap` | `Product` | `IsTemporal()` on `#if !NETFRAMEWORK`; category/supplier FKs |
| `RegionMap` | `Region` | PK non-clustered |
| `ShippersMap` | `Shipper` | `ShipperID` rename |
| `SuppliersMap` | `Supplier` | `SupplierID` rename; company/postal indexes |
| `TerritoriesMap` | `Territory` | PK non-clustered; FK to `Region` |

#### Models/ForMapping -- entity detail

Entities in `ForMappingContextBase` used by `ForMappingTests` to exercise `EFCoreMetadataReader`:

| Entity | Key / columns | Purpose |
|---|---|---|
| `WithIdentity` | `Id int` (identity) | Tests identity-column detection per provider |
| `NoIdentity` | `Id Guid` | Tests non-identity primary key detection |
| `SkipModesTable` | `Id int` + `InsertOnly?`, `UpdateOnly?`, `ReadOnly?` | Tests skip-on-insert / skip-on-update / read-only column mapping |
| `UIntTable` | `ID int` + `Field16/32/64 u*`, nullable variants | Tests unsigned-integer type mapping |
| `StringTypes` | `Id int` + `AsciiString?`, `UnicodeString?` | Tests unicode vs. ASCII string mapping (SQL Server `IsUnicode`) |
| `TypesTable` | `Id int` + `DateTime?`, `String?` | Tests datetime / string column type mapping |
| `WithDuplicateProperties` / `WithDuplicatePropertiesBase` | `Id int` + `Value` (base `string?`, derived `int?`) | Tests `new` keyword property hiding in inheritance |
| `WithInheritance` / `WithInheritanceA` / `WithInheritanceA1` / `WithInheritanceA2` | `Id int` + `Discriminator string` | Tests EF discriminator-based TPH inheritance mapping |

Per-provider `ForMappingContext` subclasses (`Npgsql/`, `Pomelo/`, `SQLite/`, `SQLServer/`) override `OnModelCreating` to set provider-specific identity column configuration (`UseIdentityAlwaysColumn()` for Npgsql, `UseMySqlIdentityColumn()` for Pomelo gated `#if !NET10_0`, `UseIdentityColumn()` for SQL Server). All four configure `HasDiscriminator` on `WithInheritance`. The SQL Server variant additionally maps `StringTypes` unicode flags and `TypesTable` max-length.

#### Models/IssueModel -- entity detail

`IssueContextBase` (`Tests/EntityFrameworkCore/Models/IssueModel/IssueContextBase.cs`) holds `DbSet<>` registrations for all tracked issue entities and configures them in `OnModelCreating`. As of sha `4a478ff1`:

- `Issue73Entity` (self-referential tree: `Id`, `ParentId?`, `Name`, `Childs`); seeds two rows via `HasData`.
- `Patent`/`PatentAssessment` (1:1 with `DeleteBehavior.Restrict`).
- `Parent`/`Child`/`GrandChild` -- three-level hierarchy; `Parent.ParentsParent` self-ref; seeds rows via `HasData`.
- `IdentityTable` -- `[DatabaseGenerated(Identity)]` `Id`, `required string Name`.
- `Issue4624ItemTicketDate`, `Issue4624Item`, `Issue4624Entry` -- multi-nav ticket-date graph.
- `Master`/`Detail` -- 1:N; seeds one master + two details.
- `TypesTable` -- `DateTimeOffset` fields, two with `DateTimeOffsetToBinaryConverter`; seeds two rows.
- `Issue4627Container`/`Item`/`ChildItem` -- 3-entity container graph with `UseIdentityColumn()`.
- `Issue4628Other`/`Issue4628Inherited` -- inheritance test entities.
- `Issue4629Post`/`Issue4629Tag` -- post-tag M:N; seeds six tags.
- `Issue340Entity` -- abstract base with virtual `Guid Id`; derived class `new long Id`.
- `Issue4640Table` -- `Items List<Issue4640Items>?` stored as JSON via `ValueConverter`; `HasConversion` + `ValueComparer`.
- `Issue212Table` -- composite key `{Id, Value}`.
- `Issue4642Table1`/`Issue4642Table2` -- composite-key tables.
- `Issue4644Main`/`Issue4644BaseItem`/`Issue4644PricedItem` -- `#if !NETFRAMEWORK` gated; mapped to `Issue245Main/PricedDetails` tables.
- `Issue4649Table` -- `UseIdentityColumn()` PK.
- `Issue4662Table` -- `DayOfWeek Value` stored as `string` via `HasConversion<string>()`.
- `Issue4663Entity` -- `#if NET8_0_OR_GREATER`; EF8 complex-property mapping (`ComplexProperty`).
- `Issue4666BaseEntity`/`Issue4666Type1Entity`/`Issue4666Type2Entity` -- discriminator-based TPH; `Issue4666EntityType` enum discriminator.
- `Issue4668TableBase`/`Issue4668Table` -- base-type mapping with `HasBaseType((Type?)null)` override.
- `Issue4671Entity1` -- `[Table]`-attributed entity; registered via `modelBuilder.Entity<Issue4671Entity1>()`.
- `IssueEnumTable` -- `StatusEnum Value`.
- `Issue4783Record` -- C# record; enum properties with three storage strategies: raw, `HasConversion<string>()`, `EnumToStringConverter<>`.
- `Issue5177Table` -- `GuidValue? Value` (nested struct); custom `ValueConverter<GuidValue, Guid>`.
- `Issue5355LicenseProfile` -- `Id int` (never generated), `License string`; seeds two profiles via `HasData`.
- `Issue5355Customer` -- extends `Issue5355CustomerBase : IIssue5355Profile`; `Name string`; FK to `Issue5355LicenseProfile`; seeds three customers via `HasData`.
- `Issue5388Task` -- `Id int`, `IsArchived bool`; `IsArchived` stored as `smallint` via `HasConversion<short>()`. No `DbSet<>` -- registered model-only via `modelBuilder.Entity<Issue5388Task>()`.
- `BulkCopyIdentityTable` -- `Id int`, `Value int`; not registered in `IssueContextBase` model at all -- accessed via `db.GetTable<BulkCopyIdentityTable>()` through the linq2db bridge.

Per-provider `IssueContext` subclasses:
- `SQLite/IssueContext` and `Pomelo/IssueContext` -- extend `IssueContextBase`, override `Issue4640Table.Items` to `HasColumnType("text")`.
- `PostgreSQL/IssueEntities.cs` -- provider-specific entity classes: `PostgreTable` (with `NpgsqlTsVector SearchVector`), `Issue155Table` (`int[] Linked`, `[NotMapped] int[] LinkedFrom`), `Issue4641Table`, `Issue4643Table` (`DayOfWeek[]? Value`), `Issue4667Table` (`Dictionary<string,string> Headers`).
- `SQLServer/IssueEntities.cs` -- `Issue129Table` (`Id`, `Key` both private-set), `Issue4816Table` (`ValueVarChar?`, `ValueNVarChar?` both private-set).

#### Models/NpgSqlEntities -- entity detail

| Entity | Key columns | Purpose |
|---|---|---|
| `Event` | `Id`, `Name`, `Duration NpgsqlRange<DateTime>` | Tests `tsrange` column type via `HasColumnType("tsrange")` |
| `EventView` | `Name` | Keyless view mapped to `"EventsView"` schema `"views"` |
| `EntityWithArrays` | `Id`, `Guids Guid[]` | Tests Npgsql array column support |
| `EntityWithXmin` | `Id`, `xmin uint`, `Value` | Tests concurrency token via `IsRowVersion()` (EF8+) or `UseXminAsConcurrencyToken()` (older) |
| `TimeStampEntity` | `Id`, `Timestamp1 DateTime`, `Timestamp2 LocalDateTime`, `TimestampTZ1 DateTime`, `TimestampTZ2 DateTimeOffset`, `TimestampTZ3 Instant` | Tests NodaTime type mapping (`timestamp`, `timestamp with time zone`) |

`NpgSqlEntitiesContext` configures all five entities in `OnModelCreating`; `EventView` registered as keyless with `ToView(...)`.

#### Models/Shared -- entity detail

Strongly-typed ID pattern used by `IdTests`:

- `IHasId<T,TId>` / `IHasWriteableId<T,TId>` -- interfaces constraining entities to expose `Id<T,TId>`.
- `Id<T,TId>` (readonly struct) -- phantom-typed wrapper over `TId`; equality by `EqualityComparer<TId>.Default`; implicit `operator TId`.
- `IdValueConverter<TId,T>` -- EF `ValueConverter<Id<T,TId>, TId>`: identity conversion (stores raw value as-is).
- `IdValueConverterSelector` -- extends EF `ValueConverterSelector`; detects `Id<,>` generic properties and injects the appropriate `IdValueConverter` automatically.
- `DataContextExtensions` -- `InsertAsId<T>(IDataContext, T)` helper wrapping `InsertWithInt64Identity` + `AsId`.
- `ModelBuilderExtensions` -- four `ModelBuilder` extension methods: `UseIdAsKey` (auto-detects `Id<,>` properties -> FK/PK relationships), `UseSnakeCase` (applies snake_case to all table/column/index names; `#if NETFRAMEWORK`-gated API variants), `UseOneIdSequence<T>` (attaches a single DB sequence to all `Id<T,long>` PKs), `UsePermanentId` / `UseCode` (unique-index helpers).
- Entities: `Entity` (Id, Name, nav Details/Children/Items), `Detail` (Id, MasterId, nav SubDetails), `SubDetail` (Id, MasterId), `Item` (Id, Name), `Child` (Id, ParentId, nav Parent), `Entity2Item` (composite key EntityId+ItemId join table).
- `IdTestContext` -- `DbContext` with all 5 entity `DbSet`s; `OnModelCreating` calls `UseSnakeCase().UseIdAsKey().UseOneIdSequence<long>("test", ...)`.

#### Models/ValueConversion -- entity detail

Parallel to `Models/Shared` but for `ConvertorTests`; key difference is `IdValueConverter<TEntity>` (long-only specialization) applies an intentional `id + 1` / `key - 1` offset to verify round-trip through EF value conversion:

- `IEntity<TKey>` -- interface with read-only `Id TKey`.
- `Id<TEntity,TKey>` (readonly struct) -- same phantom-type pattern as `Shared.Id`; `Id.AsId<T,long>()` and `AsId<T,Guid>()` static helpers.
- `IdValueConverter<TKey,TEntity>` -- bidirectional EF converter, identity mapping.
- `IdValueConverter<TEntity>` (long specialization) -- stores `id.Value + 1`, restores `key - 1`; intentional offset verifies converter is active.
- `IdValueConverterSelector` -- same pattern as `Shared.IdValueConverterSelector`; handles both generic `IdValueConverter<,>` and long-specific `IdValueConverter<>` paths.
- `SubDivision` -- entity implementing `IEntity<long>` with `Id<SubDivision,long>`, `PermanentId Guid`, `Code`, `Name`, `IsDeleted?`.
- `ConvertorContext` -- minimal `DbContext` with single `DbSet<SubDivision> Subdivisions`.

#### Models/JsonConverter -- entity detail

- `LocalizedString` -- POCO with `English`, `German`, `Slovak` string properties; serialized to JSON via `JsonConvert.SerializeObject`.
- `EventScheduleItemBase` -- `Id int`, `NameLocalized LocalizedString` (JSON column `NameLocalized_JSON` via EF converter), `JsonColumn string?`.
- `EventScheduleItem` -- extends `EventScheduleItemBase`; adds `CrashEnum CrashEnum` (`tinyint`) and `GuidColumn Guid` (`uniqueidentifier`).
- `CrashEnum` -- `byte`-backed enum (`OneValue=0`, `OtherValue=1`); named for the bug it was created to reproduce.
- `JsonConvertContext` -- `DbContext` registering `EventScheduleItem` table; wires `NameLocalized_JSON` Newtonsoft converter; registers `JSON_VALUE` EF db-function with `#if !NETFRAMEWORK`-gated `SqlFunctionExpression` overload differences.

## Key types

| Type | File | Role |
|---|---|---|
| `ContextTestBase<TContext>` | `Tests/EntityFrameworkCore/ContextTestBase.cs` | Abstract base for all EFCore test fixtures; owns `CreateContext()`, DB-init, `ProviderSetup()` |
| `NorthwindContextTestBase` | `Tests/EntityFrameworkCore/NorthwindContextTestBase.cs` | Northwind-specific `ContextTestBase`; seeds DB via `NorthwindData.Seed()` |
| `EFDataSourcesAttribute` | `Tests/EntityFrameworkCore/Utilities/EFDataSourcesAttribute.cs` | NUnit parameter source: all EF-capable user providers (exclusion mode) |
| `EFIncludeDataSourcesAttribute` | `Tests/EntityFrameworkCore/Utilities/EFIncludeDataSourcesAttribute.cs` | NUnit parameter source: intersection of listed + EF-capable providers |
| `TestContextTracker` | `Tests/EntityFrameworkCore/ContextTestBase.cs` | Static `Dictionary<connectionString, Type>` -- DB-init idempotency guard |
| `NorthwindContextBase` | `Tests/EntityFrameworkCore/Models/Northwind/NorthwindContext.cs` | `DbContext` with full Northwind `DbSet`s, `QueryFilter` on `Product`, `ISoftDelete` global filter |
| `IssueContextBase` | `Tests/EntityFrameworkCore/Models/IssueModel/IssueContextBase.cs` | `DbContext` for regression tests; holds ~32 `DbSet`s across all tracked issues |
| `TestInterceptor` | `Tests/EntityFrameworkCore/Interceptors/TestInterceptor.cs` | Base class for all test interceptors; `HasInterceptorBeenInvoked` + `ResetInvocations()` |
| `TestEfCoreAndLinqToDBComboInterceptor` | `Tests/EntityFrameworkCore/Interceptors/TestEfCoreAndLinqToDBComboInterceptor.cs` | Implements both linq2db `ICommandInterceptor` and EF `IDbCommandInterceptor` |
| `LinqToDBContextOptionsBuilderExtensions` | `Tests/EntityFrameworkCore/Interceptors/Extensions/...cs` | `UseEfCoreRegisteredInterceptorsIfPossible()` -- bridges EF interceptors into linq2db |
| `TestLoggerProvider` | `Tests/EntityFrameworkCore/Logging/TestLoggerProvider.cs` | `ILoggerProvider` wiring test log output to `BaselinesManager` |
| `ModelBuilderExtensions` | `Tests/EntityFrameworkCore/Models/Shared/ModelBuilderExtensions.cs` | `UseIdAsKey`, `UseSnakeCase`, `UseOneIdSequence` -- configure strongly-typed ID model conventions |
| `IdValueConverterSelector` | `Tests/EntityFrameworkCore/Models/Shared/IdValueConverter.cs` | EF `ValueConverterSelector` that auto-injects `IdValueConverter` for `Id<,>` properties |
| `NorthwindData` | `Tests/EntityFrameworkCore/Models/Northwind/NorthwindData.cs` | DB seeder + in-memory `IAsyncQueryProvider` wrapper; `ShadowStateAccessRewriter` rewrites EF shadow-property expressions |
| `Issue5355LicenseProfile` / `Issue5355Customer` | `Tests/EntityFrameworkCore/Models/IssueModel/IssueEntities.cs` | Issue #5355 entities: `IIssue5355Profile` interface + abstract base + sealed derived |
| `Issue5388Task` | `Tests/EntityFrameworkCore/Models/IssueModel/IssueEntities.cs` | Issue #5388: `bool IsArchived` stored as `smallint` via `HasConversion<short>()` |
| `BulkCopyIdentityTable` | `Tests/EntityFrameworkCore/Models/IssueModel/IssueEntities.cs` | BulkCopy identity sequence test table; not a `DbSet<>` in `IssueContextBase` |
| `FilterIssue5355License<T>` | `Tests/EntityFrameworkCore/Tests/IssueTests.cs` | Static extension on `IQueryable<T> where T : IIssue5355Profile`; applies `licenseFilter.Contains(x.Profile.License)` predicate |

## Files (Tier 1 / Tier 2)

There are no declared Tier-1 files for this area (row says `(none)`). See AUDIT-NOTE for proposed anchors.

### Tier 2 (read this run -- 131 of 171)

#### Previously read (run 1 -- 36 files)

| File | Notes |
|---|---|
| `Tests.EntityFrameworkCore.EF10.csproj` | EF10 csproj -- anchor candidate |
| `Tests.EntityFrameworkCore.EF3.csproj` | EF3 csproj |
| `Tests.EntityFrameworkCore.props` | Shared props -- anchor candidate |
| `ContextTestBase.cs` | Core test base -- anchor candidate |
| `NorthwindContextTestBase.cs` | Northwind test base |
| `Tests/ToolsTests.cs` | Primary integration fixture -- anchor candidate |
| `Tests/InterceptorTests.cs` | Interceptor surface tests |
| `Tests/IssueTests.cs` (partial) | Regression fixture |
| `Tests/ForMappingTests.cs` (partial) | Mapping fixture |
| `Tests/ConvertorTests.cs` (partial) | Value-converter fixture |
| `Tests/IdTests.cs` (partial) | Typed-ID fixture |
| `Tests/InheritanceTests.cs` (partial) | Inheritance fixture |
| `Tests/NpgSqlTests.cs` (partial) | Npgsql-specific fixture |
| `Tests/SQLiteTests.cs` | SQLite provider fixture |
| `Tests/FSharpTests.cs` | F# interop fixture (`#if EF8`) |
| `Tests/CustomContextIssueTests.cs` (partial) | Issue tests with custom DB setup |
| `Interceptors/TestInterceptor.cs` | Interceptor base |
| `Interceptors/TestCommandInterceptor.cs` | Command interceptor |
| `Interceptors/TestEfCoreAndLinqToDBComboInterceptor.cs` | Combo interceptor |
| `Interceptors/Extensions/LinqToDBContextOptionsBuilderExtensions.cs` | EF->linq2db interceptor bridge |
| `Logging/TestLoggerProvider.cs` | Logger provider |
| `Logging/TestLogger.cs` (partial) | Logger impl |
| `Models/Northwind/NorthwindContext.cs` | Northwind base `DbContext` |
| `Models/Northwind/SQLServer/NorthwindContext.cs` | SQL Server `NorthwindContext` |
| `Models/IssueModel/IssueContextBase.cs` | Issue test `DbContext` |
| `Models/ForMapping/ForMappingContextBase.cs` | ForMapping test `DbContext` |
| `Models/Inheritance/InheritanceContext.cs` | Inheritance test `DbContext` |
| `Models/NpgSqlEntities/` (via NpgSqlTests imports) | Npgsql entity context |
| `Utilities/EFDataSourcesAttribute.cs` | Provider-selection attribute |
| `Utilities/EFIncludeDataSourcesAttribute.cs` | Provider-inclusion attribute |
| `Utilities/AAA.cs` | Arrange/Act/Assert DSL |
| `Utilities/QueryableExtensions.cs` | `AsLinqToDB`/`AsTracking` helpers |
| `Tests/Base/TestConfiguration.cs` (EFProviders field) | `EFProviders` list (cross-area read) |

#### Read (run 2 -- 92 files)

**Interceptors/**

| File | Notes |
|---|---|
| `Interceptors/TestConnectionInterceptor.cs` | `IConnectionInterceptor`: 4 events (opened/opening, sync+async) |
| `Interceptors/TestDataContextInterceptor.cs` | `IDataContextInterceptor`: 4 events (closed/closing, sync+async) |
| `Interceptors/TestEntityServiceInterceptor.cs` | `IEntityServiceInterceptor`: `EntityCreated` single event |

**Logging/**

| File | Notes |
|---|---|
| `Logging/LogMessageEntry.cs` | `readonly record struct` for a captured log line (message + console color metadata) |
| `Logging/NullExternalScopeProvider.cs` | Singleton no-op `IExternalScopeProvider` |
| `Logging/NullScope.cs` | Singleton no-op `IDisposable` scope |
| `Logging/TestLoggerExtensions.cs` | `ILoggingBuilder.AddTestLogger()` extension registering `TestLoggerProvider` |

**Models/ForMapping/**

| File | Notes |
|---|---|
| `Models/ForMapping/WithIdentity.cs` | `Id int`, `Name` -- identity-column test entity |
| `Models/ForMapping/NoIdentity.cs` | `Id Guid`, `Name` -- non-identity key test entity |
| `Models/ForMapping/SkipModesTable.cs` | `Id`, `InsertOnly?`, `UpdateOnly?`, `ReadOnly?` -- skip-mode column tests |
| `Models/ForMapping/UIntTable.cs` | `ID int` + 6 unsigned-int fields -- unsigned type-mapping tests |
| `Models/ForMapping/StringTypes.cs` | `Id`, `AsciiString?`, `UnicodeString?` -- unicode vs ASCII string tests |
| `Models/ForMapping/TypesTable.cs` | `Id`, `DateTime?`, `String?` -- datetime/string column type tests |
| `Models/ForMapping/WithDuplicateProperties.cs` | Base `Value string?` + derived `new Value int?` -- property-hiding tests |
| `Models/ForMapping/WithInheritance.cs` + `WithInheritanceA/A1/A2` | TPH hierarchy with `Discriminator` string |
| `Models/ForMapping/Npgsql/ForMappingContext.cs` | Npgsql subcontext: `UseIdentityAlwaysColumn()` for `WithIdentity` |
| `Models/ForMapping/Pomelo/ForMappingContext.cs` | Pomelo subcontext: `UseMySqlIdentityColumn()` gated `#if !NET10_0` |
| `Models/ForMapping/SQLite/ForMappingContext.cs` | SQLite subcontext: no provider-specific identity config |
| `Models/ForMapping/SQLServer/ForMappingContext.cs` | SQL Server subcontext: `UseIdentityColumn()` + `StringTypes` unicode + `TypesTable` max-length |

**Models/IssueModel/**

| File | Notes |
|---|---|
| `Models/IssueModel/Issue117Entities.cs` | `Patent` + `PatentAssessment` -- 1:1 with `DeleteBehavior.Restrict` |
| `Models/IssueModel/Issue73Entity.cs` | Self-referential tree entity (`Id`, `ParentId?`, `Childs`) |
| `Models/IssueModel/IssueContext.cs` | Provider-agnostic `DbContext` for Issue73 + Issue117 entities; seeds `Issue73Entity` data |
| `Models/IssueModel/Pomelo/IssueContext.cs` | Pomelo subcontext: `Issue4640Table.Items` -> `text` column type |
| `Models/IssueModel/SQLite/IssueContext.cs` | SQLite subcontext: `Issue4640Table.Items` -> `text` column type |
| `Models/IssueModel/PostgreSQL/IssueEntities.cs` | PostgreSQL-specific entities: `PostgreTable` (tsVector), `Issue155Table` (int[] arrays), `Issue4641/4643/4667Table` |
| `Models/IssueModel/SQLServer/IssueEntities.cs` | SQL Server-specific entities: `Issue129Table`, `Issue4816Table` (private-set properties) |

**Models/JsonConverter/**

| File | Notes |
|---|---|
| `Models/JsonConverter/LocalizedString.cs` | POCO: `English`, `German`, `Slovak` -- JSON serialization target |
| `Models/JsonConverter/EventScheduleItemBase.cs` | Base: `Id`, `NameLocalized LocalizedString` (JSON col), `JsonColumn string?` |
| `Models/JsonConverter/EventScheduleItem.cs` | Derived: adds `CrashEnum`, `GuidColumn Guid` |
| `Models/JsonConverter/CrashEnum.cs` | `byte`-backed enum (`OneValue=0`, `OtherValue=1`) |
| `Models/JsonConverter/JsonConvertContext.cs` | `DbContext` mapping `EventScheduleItem`; wires Newtonsoft JSON converter + `JSON_VALUE` db-function (with `#if !NETFRAMEWORK` overload) |

**Models/Northwind/**

| File | Notes |
|---|---|
| `Models/Northwind/BaseEntity.cs` | `IsDeleted bool`; `ISoftDelete` interface; base for all Northwind entities |
| `Models/Northwind/Category.cs` | `CategoryId`, `CategoryName`, `Description`, `Picture byte[]`; nav `Products` |
| `Models/Northwind/Customer.cs` | `CustomerId (string)`, address fields; nav `Orders`, `CustomerCustomerDemo` |
| `Models/Northwind/CustomerCustomerDemo.cs` | M:M join: `CustomerId + CustomerTypeId` |
| `Models/Northwind/CustomerDemographics.cs` | `CustomerTypeId (string)`, `CustomerDesc`; nav `CustomerCustomerDemo` |
| `Models/Northwind/CustomerOrderHistory.cs` | Keyless: `ProductName`, `Total` (stored-proc result) |
| `Models/Northwind/CustomerQuery.cs` | Keyless: `CompanyName`, `OrderCount`, `SearchTerm` |
| `Models/Northwind/CustomerView.cs` | Keyless view projection; `IsLondon` computed `[NotMapped]` |
| `Models/Northwind/Employee.cs` | `EmployeeId`, HR fields, self-ref `ReportsTo`; nav `EmployeeTerritories`, `Orders` |
| `Models/Northwind/EmployeeTerritory.cs` | M:M join: `EmployeeId + TerritoryId` |
| `Models/Northwind/Order.cs` | `OrderId`, shipping fields; nav `Customer`, `Employee`, `ShipViaNavigation`, `OrderDetails` |
| `Models/Northwind/OrderDetail.cs` | `OrderId + ProductId` composite PK; `UnitPrice`, `Quantity`, `Discount` |
| `Models/Northwind/Product.cs` | `ProductId`, `ProductName`, `Discontinued`; nav `Category`, `Supplier`, `OrderDetails` |
| `Models/Northwind/Region.cs` | `RegionId`, `RegionDescription`; nav `Territories` |
| `Models/Northwind/NorthwindData.cs` | DB seeder + in-memory `IAsyncQueryProvider`; `ShadowStateAccessRewriter` rewrites `EF.Property` calls |
| `Models/Northwind/NorthwindData.Objects.cs` | `<auto-generated>` large seed-data file with `CreateCustomers/Employees/Orders/Products/...` factory arrays (>1 MB) |

**Models/Northwind/SQLServer/**

| File | Notes |
|---|---|
| `SQLServer/BaseEntityMap.cs` | `BaseEntityMap<T> : IEntityTypeConfiguration<T>` -- sets `IsDeleted.HasDefaultValue(false)` |
| `SQLServer/CategoriesMap.cs` | `CategoryID` rename, index, `Description`/`Picture` column types |
| `SQLServer/CustomersMap.cs` | `CustomerID` rename, 4 indexes, field max-lengths |
| `SQLServer/CustomerCustomerDemoMap.cs` | Composite PK non-clustered; FK constraint names |
| `SQLServer/CustomerDemographicsMap.cs` | PK non-clustered; `CustomerDesc` as `nvarchar(max)` |
| `SQLServer/EmployeesMap.cs` | `EmployeeID` rename; `Photo`/`Notes` max types; self-FK |
| `SQLServer/EmployeeTerritoriesMap.cs` | Composite PK non-clustered; FK names |
| `SQLServer/OrderMap.cs` | `OrderID` rename; `Freight` as `money`; 6 indexes; 3 FKs |
| `SQLServer/OrderDetailsMap.cs` | `ToTable("Order Details")`; composite PK; `UnitPrice` as `money` |
| `SQLServer/ProductsMap.cs` | `IsTemporal()` `#if !NETFRAMEWORK`; category/supplier FKs |
| `SQLServer/RegionMap.cs` | PK non-clustered |
| `SQLServer/ShippersMap.cs` | `ShipperID` rename |
| `SQLServer/SuppliersMap.cs` | `SupplierID` rename; 2 indexes |
| `SQLServer/TerritoriesMap.cs` | PK non-clustered; FK to `Region` |

**Models/NpgSqlEntities/**

| File | Notes |
|---|---|
| `Models/NpgSqlEntities/Event.cs` | `Id`, `Name`, `Duration NpgsqlRange<DateTime>` -- `tsrange` column |
| `Models/NpgSqlEntities/EventView.cs` | Keyless `Name`; mapped to `"EventsView"` in schema `"views"` |
| `Models/NpgSqlEntities/EntityWithArrays.cs` | `Id`, `Guids Guid[]` -- Npgsql array support |
| `Models/NpgSqlEntities/EntityWithXmin.cs` | `Id`, `xmin uint`, `Value` -- concurrency token via `IsRowVersion`/`UseXminAsConcurrencyToken` |
| `Models/NpgSqlEntities/TimeStampEntity.cs` | 5 timestamp columns mixing `DateTime`, `LocalDateTime` (NodaTime), `DateTimeOffset`, `Instant` |
| `Models/NpgSqlEntities/NpgSqlEntitiesContext.cs` | `DbContext` registering all 5 entities; configures `tsrange`, `IsRowVersion`, NodaTime column types |

**Models/Shared/**

| File | Notes |
|---|---|
| `Models/Shared/IHasId.cs` | `IHasId<T,TId>` + `IHasWriteableId<T,TId>` interfaces |
| `Models/Shared/Id.cs` | `Id<T,TId>` phantom-typed readonly struct; `Id.AsId` static helpers |
| `Models/Shared/IdValueConverter.cs` | `IdValueConverter<TId,T>` (identity) + `IdValueConverterSelector` |
| `Models/Shared/ModelBuilderExtensions.cs` | `UseIdAsKey`, `UseSnakeCase`, `UseOneIdSequence`, `UsePermanentId`, `UseCode` |
| `Models/Shared/DataContextExtensions.cs` | `InsertAsId<T>` linq2db helper |
| `Models/Shared/IdTestContext.cs` | `DbContext`: 5 `DbSet`s; uses `UseSnakeCase().UseIdAsKey().UseOneIdSequence` |
| `Models/Shared/Entity.cs` | `Id<Entity,long>`, `Name`; nav `Details`, `Children`, `Items` |
| `Models/Shared/Detail.cs` | `Id<Detail,long>`, `MasterId`, `Name`; nav `SubDetails` |
| `Models/Shared/SubDetail.cs` | `Id<SubDetail,long>`, `MasterId`, `Name` |
| `Models/Shared/Item.cs` | `Id<Item,long>`, `Name` |
| `Models/Shared/Child.cs` | `Id<Child,long>`, `ParentId Id<Entity,long>`, `Name` |
| `Models/Shared/Entity2Item.cs` | Composite-key join: `EntityId + ItemId` |

**Models/ValueConversion/**

| File | Notes |
|---|---|
| `Models/ValueConversion/IEntity`1.cs` | `IEntity<TKey>` interface with read-only `Id TKey` |
| `Models/ValueConversion/Id`2.cs` | `Id<TEntity,TKey>` phantom struct; `Id.AsId` helpers for `long` and `Guid` |
| `Models/ValueConversion/IdValueConverter`2.cs` | `IdValueConverter<TKey,TEntity>` (identity) + `IdValueConverter<TEntity>` (long, applies `+-1` offset) |
| `Models/ValueConversion/IdValueConverterSelector.cs` | `ValueConverterSelector` detecting `Id<,>` properties; selects long vs generic converter path |
| `Models/ValueConversion/SubDivision.cs` | `IEntity<long>` entity: `Id<SubDivision,long>`, `PermanentId Guid`, `Code`, `Name`, `IsDeleted?` |
| `Models/ValueConversion/ConvertorContext.cs` | `DbContext` with `DbSet<SubDivision> Subdivisions` |

**Tests/**

| File | Notes |
|---|---|
| `Tests/JsonConvertTests.cs` | Single test `TestJsonConvert` ([AllSqlServer]): verifies JSON-serialized `LocalizedString` column + `JSON_VALUE` db-function via `.ToLinqToDB()` |
| `Tests/PomeloMySqlTests.cs` | 3 tests: `SimpleProviderTest` (basic query), `TestFunctionTranslation` / `TestFunctionTranslationParameter` (Pomelo `string.Contains` issue #1801) |

**Utilities/**

| File | Notes |
|---|---|
| `Utilities/ExceptionExtensions.cs` | `Throw(this Exception)` returning `Unit` |
| `Utilities/Unit.cs` | Empty sentinel type for `ExceptionExtensions.Throw` return |
| `Utilities/StringExtensions.cs` | `ToSnakeCase(string)` using compiled/generated regex; used by `ModelBuilderExtensions.UseSnakeCase` |
| `Utilities/TypeExtensions.cs` | `UnwrapNullable(Type?)` -- strips `Nullable<T>` |
| `Utilities/Polyfills.cs` | `#if NETFRAMEWORK` shims: `HasDatabaseName`, `ArgumentNullException.ThrowIfNull`, `Enumerable.ToHashSet` |

#### Read (run 3 -- delta, 3 files)

| File | Notes |
|---|---|
| `Models/IssueModel/IssueContextBase.cs` | Added `DbSet<Issue5355LicenseProfile>`, `DbSet<Issue5355Customer>`; added `OnModelCreating` for `Issue5388Task` (bool-as-smallint) and `Issue5355LicenseProfile`/`Issue5355Customer` (seeded data + FK) |
| `Models/IssueModel/IssueEntities.cs` | Added `IIssue5355Profile` interface, `Issue5355CustomerBase` abstract, `Issue5355LicenseProfile`, `Issue5355Customer`; added `Issue5388Task`; added `BulkCopyIdentityTable` |
| `Tests/IssueTests.cs` | Added `Issue5355_ContainsViaIEnumerableInGenericMethod`, `Issue5355_ContainsViaArrayInGenericMethod`, `ConstantAndValueConversion` (Issue5388), `BulkCopy_Sequence_AsIdentity` (`#if !NETFRAMEWORK`, SQL Server + PostgreSQL only); added `FilterIssue5355License<T>` static extension in `TestExtensions` |

## Inbound / outbound dependencies

**Inbound:**
- Nothing depends on TESTS-EFCORE; it is a leaf test assembly.

**Outbound:**
- **EFCORE** -- the primary production dependency. Every test fixture exercises `LinqToDBForEFTools`, `LinqToDBForEFToolsImplDefault`, `EFCoreMetadataReader`, `TransformExpressionVisitor`, `LinqToDBForEFToolsDataConnection`, `LinqToDBOptionsExtension`.
- **TESTS-INFRA** -- `ContextTestBase<T>` extends `TestBase`; `EFDataSourcesAttribute` / `EFIncludeDataSourcesAttribute` extend `DataSourcesBaseAttribute`; `TestConfiguration.EFProviders` is defined in `Tests/Base/TestConfiguration.cs`.
- **PROV-SQLSERVER** -- used by `ToolsTests.TestFunctions`, `TestCommandTimeout`, `TestCreateTempTable`, temporal-table tests; SQL Server NorthwindContext; `JsonConvertTests` (AllSqlServer only); SQL Server Fluent maps use `IsTemporal()`. Also used by `BulkCopy_Sequence_AsIdentity`.
- **PROV-SQLITE** -- used by `SQLiteTests`; default EF provider in many `[EFDataSources]` tests.
- **PROV-POSTGRES** -- used by `NpgSqlTests` (range, array, NodaTime); Northwind and IssueModel PostgreSQL context variants; `ForMapping/Npgsql` context. Also used by `BulkCopy_Sequence_AsIdentity`.
- **PROV-MYSQL** -- used by `PomeloMySqlTests` and multi-provider tests (excluded EF10); `ForMapping/Pomelo` and `IssueModel/Pomelo` contexts.
- **INTERCEPTORS** -- `TestCommandInterceptor` / `TestConnectionInterceptor` / `TestEntityServiceInterceptor` implement INTERCEPTORS area interfaces; `LinqToDBContextOptionsBuilderExtensions` bridges EF-registered interceptors.
- **DATA** -- `BulkCopyType`, `DataOptions`, `DataConnection` (`CreateLinqToDBConnection`), `CreateTempTable`.
- **MAPPING** -- `MappingSchema`, `ColumnAttribute`, `AssociationAttribute` assertions in `ToolsTests.TestKey` and `TestAssociations`.

## Known issues / debt

- The 4 csprojs share source but have no shared `DefineConstants` contract analogous to `EF31`/`EF8`/`EF9`/`EF10` in the production EFCORE area. Per-version test differences are handled by `#if NETFRAMEWORK` and `#if NET8_0_OR_GREATER` guards rather than EF-version symbols, making EF-version-specific test behaviour less obvious.
- `TestContextTracker.LastContexts` is a static `Dictionary<string, Type>` (not thread-safe, no locking). Multiple `TContext` types sharing a connection string will cause spurious re-initializations; the first test to run for a given `connectionString` wins.
- `CustomContextIssueTests` manually invalidates `TestContextTracker.LastContexts` by removing its connection string on every `GetConnectionString()` call, forcing schema recreation on each test. This is intentional but fragile if test ordering changes.
- `InheritanceTests.TestInheritanceBulkCopy` has a workaround `try { x = CreateContext(); } catch { x = CreateContext(); }` for an Npgsql EFCore bug (#3671). The bug may be resolved upstream; the workaround should be rechecked.
- `PomeloMySqlTests` is excluded from EF10 builds by removing the `Pomelo.EntityFrameworkCore.MySql` package reference in the EF10 csproj -- there is no explicit `[ActiveIssue]` annotation explaining the gap for readers.
- `FSharpTests` is gated `#if EF8` -- F# EFCore interop is not tested against EF3 or EF31.
- `NorthwindData.Objects.cs` is marked `<auto-generated>` to suppress analyzers but is a hand-maintained seed-data file exceeding 1 MB; file is not actually generated and the marker is a workaround, not a true generation artifact.
- `JsonConvertTests` has a `//TODO` comment noting that sub-property projection from a JSON column (e.g. `p.NameLocalized.English`) does not yet work through the linq2db bridge.
- `Models/Shared/IdValueConverter.cs` and `Models/ValueConversion/IdValueConverterSelector.cs` are near-duplicate implementations of the strongly-typed ID pattern (differing only in the `IHasId` vs `IEntity` interface constraint and the `+-1` test offset in `IdValueConverter<TEntity>`). No shared abstraction exists.
- `Models/ForMapping/Pomelo/ForMappingContext.cs` gates `UseMySqlIdentityColumn()` on `#if !NET10_0` with an inline comment but no `[ActiveIssue]` link, making the exclusion reason opaque in test output.
- `Issue5388Task` has no `DbSet<>` in `IssueContextBase` -- it is model-only (registered only via `modelBuilder.Entity<Issue5388Task>()`); test access is via `db.GetTable<Issue5388Task>()` through the linq2db bridge after EF `SaveChanges`. This asymmetry (EF writes, linq2db reads) is intentional to verify constant-value-conversion parity.
- `BulkCopyIdentityTable` is not registered in `IssueContextBase` model at all -- test calls `ctx.BulkCopy(...)` directly via the EFCore extension bridge, then reads back via `db.GetTable<BulkCopyIdentityTable>()`. The table schema must be created externally or via a migration; behavior on providers that auto-create via `EnsureCreated` is untested for this entity.

## See also

- [areas/EFCORE/INDEX.md](../EFCORE/INDEX.md) -- production EFCORE bridge; all test fixtures exercise this area.
- [areas/TESTS-INFRA/INDEX.md](../TESTS-INFRA/INDEX.md) -- `TestBase`, `DataSourcesBaseAttribute`, `TestConfiguration`; shared by all test projects.
- [areas/INTERCEPTORS/INDEX.md](../INTERCEPTORS/INDEX.md) -- `ICommandInterceptor`, `IConnectionInterceptor`, `IEntityServiceInterceptor` contracts tested here.
- [areas/PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- temporal table tests and SQL Server NorthwindContext.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 0 / 0 -- no Tier-1 anchors declared; see AUDIT-NOTE for proposed anchors
- Tier 2 (visited / total): 131 / 171 (76.6%)
- Tier 3 (skipped, logged): 4 (the 4 csproj files -- counted, not read as .cs)

Read (run 1):
- `Tests.EntityFrameworkCore.EF10.csproj`, `Tests.EntityFrameworkCore.EF3.csproj`, `Tests.EntityFrameworkCore.props`
- `ContextTestBase.cs`, `NorthwindContextTestBase.cs`
- `Tests/ToolsTests.cs`, `Tests/InterceptorTests.cs`, `Tests/IssueTests.cs`, `Tests/ForMappingTests.cs`, `Tests/ConvertorTests.cs`, `Tests/IdTests.cs`, `Tests/InheritanceTests.cs`, `Tests/NpgSqlTests.cs`, `Tests/SQLiteTests.cs`, `Tests/FSharpTests.cs`, `Tests/CustomContextIssueTests.cs`
- `Interceptors/TestInterceptor.cs`, `Interceptors/TestCommandInterceptor.cs`, `Interceptors/TestEfCoreAndLinqToDBComboInterceptor.cs`, `Interceptors/Extensions/LinqToDBContextOptionsBuilderExtensions.cs`
- `Logging/TestLoggerProvider.cs`, `Logging/TestLogger.cs`
- `Models/Northwind/NorthwindContext.cs`, `Models/Northwind/SQLServer/NorthwindContext.cs`
- `Models/IssueModel/IssueContextBase.cs`, `Models/ForMapping/ForMappingContextBase.cs`, `Models/Inheritance/InheritanceContext.cs`
- `Utilities/EFDataSourcesAttribute.cs`, `Utilities/EFIncludeDataSourcesAttribute.cs`, `Utilities/AAA.cs`, `Utilities/QueryableExtensions.cs`
- `Tests/Base/TestConfiguration.cs` (cross-area)

Read (run 2):
- `Interceptors/TestConnectionInterceptor.cs`, `Interceptors/TestDataContextInterceptor.cs`, `Interceptors/TestEntityServiceInterceptor.cs`
- `Logging/LogMessageEntry.cs`, `Logging/NullExternalScopeProvider.cs`, `Logging/NullScope.cs`, `Logging/TestLoggerExtensions.cs`
- `Models/ForMapping/NoIdentity.cs`, `Models/ForMapping/SkipModesTable.cs`, `Models/ForMapping/StringTypes.cs`, `Models/ForMapping/TypesTable.cs`, `Models/ForMapping/UIntTable.cs`, `Models/ForMapping/WithDuplicateProperties.cs`, `Models/ForMapping/WithIdentity.cs`, `Models/ForMapping/WithInheritance.cs`
- `Models/ForMapping/Npgsql/ForMappingContext.cs`, `Models/ForMapping/Pomelo/ForMappingContext.cs`, `Models/ForMapping/SQLite/ForMappingContext.cs`, `Models/ForMapping/SQLServer/ForMappingContext.cs`
- `Models/IssueModel/Issue117Entities.cs`, `Models/IssueModel/Issue73Entity.cs`, `Models/IssueModel/IssueContext.cs`, `Models/IssueModel/Pomelo/IssueContext.cs`, `Models/IssueModel/PostgreSQL/IssueEntities.cs`, `Models/IssueModel/SQLite/IssueContext.cs`, `Models/IssueModel/SQLServer/IssueEntities.cs`
- `Models/JsonConverter/CrashEnum.cs`, `Models/JsonConverter/EventScheduleItem.cs`, `Models/JsonConverter/EventScheduleItemBase.cs`, `Models/JsonConverter/JsonConvertContext.cs`, `Models/JsonConverter/LocalizedString.cs`
- `Models/Northwind/BaseEntity.cs`, `Models/Northwind/Category.cs`, `Models/Northwind/Customer.cs`, `Models/Northwind/CustomerCustomerDemo.cs`, `Models/Northwind/CustomerDemographics.cs`, `Models/Northwind/CustomerOrderHistory.cs`, `Models/Northwind/CustomerQuery.cs`, `Models/Northwind/CustomerView.cs`, `Models/Northwind/Employee.cs`, `Models/Northwind/EmployeeTerritory.cs`, `Models/Northwind/NorthwindData.cs`, `Models/Northwind/NorthwindData.Objects.cs`, `Models/Northwind/Order.cs`, `Models/Northwind/OrderDetail.cs`, `Models/Northwind/Product.cs`, `Models/Northwind/Region.cs`
- `Models/Northwind/SQLServer/BaseEntityMap.cs`, `Models/Northwind/SQLServer/CategoriesMap.cs`, `Models/Northwind/SQLServer/CustomerCustomerDemoMap.cs`, `Models/Northwind/SQLServer/CustomerDemographicsMap.cs`, `Models/Northwind/SQLServer/CustomersMap.cs`, `Models/Northwind/SQLServer/EmployeesMap.cs`, `Models/Northwind/SQLServer/EmployeeTerritoriesMap.cs`, `Models/Northwind/SQLServer/OrderDetailsMap.cs`, `Models/Northwind/SQLServer/OrderMap.cs`, `Models/Northwind/SQLServer/ProductsMap.cs`, `Models/Northwind/SQLServer/RegionMap.cs`, `Models/Northwind/SQLServer/ShippersMap.cs`, `Models/Northwind/SQLServer/SuppliersMap.cs`, `Models/Northwind/SQLServer/TerritoriesMap.cs`
- `Models/NpgSqlEntities/EntityWithArrays.cs`, `Models/NpgSqlEntities/EntityWithXmin.cs`, `Models/NpgSqlEntities/Event.cs`, `Models/NpgSqlEntities/EventView.cs`, `Models/NpgSqlEntities/NpgSqlEntitiesContext.cs`, `Models/NpgSqlEntities/TimeStampEntity.cs`
- `Models/Shared/Child.cs`, `Models/Shared/DataContextExtensions.cs`, `Models/Shared/Detail.cs`, `Models/Shared/Entity.cs`, `Models/Shared/Entity2Item.cs`, `Models/Shared/Id.cs`, `Models/Shared/IdTestContext.cs`, `Models/Shared/IdValueConverter.cs`, `Models/Shared/IHasId.cs`, `Models/Shared/Item.cs`, `Models/Shared/ModelBuilderExtensions.cs`, `Models/Shared/SubDetail.cs`
- `Models/ValueConversion/ConvertorContext.cs`, `Models/ValueConversion/Id`2.cs`, `Models/ValueConversion/IdValueConverter`2.cs`, `Models/ValueConversion/IdValueConverterSelector.cs`, `Models/ValueConversion/IEntity`1.cs`, `Models/ValueConversion/SubDivision.cs`
- `Tests/JsonConvertTests.cs`, `Tests/PomeloMySqlTests.cs`
- `Utilities/ExceptionExtensions.cs`, `Utilities/Polyfills.cs`, `Utilities/StringExtensions.cs`, `Utilities/TypeExtensions.cs`, `Utilities/Unit.cs`

Read (run 3 -- delta at sha 4a478ff1):
- `Models/IssueModel/IssueContextBase.cs` (re-read: Issue5355 DbSets + model config; Issue5388 bool-as-smallint config)
- `Models/IssueModel/IssueEntities.cs` (re-read: Issue5355LicenseProfile, Issue5355Customer, Issue5355CustomerBase, IIssue5355Profile, Issue5388Task, BulkCopyIdentityTable)
- `Tests/IssueTests.cs` (re-read: Issue5355_ContainsViaIEnumerableInGenericMethod, Issue5355_ContainsViaArrayInGenericMethod, ConstantAndValueConversion, BulkCopy_Sequence_AsIdentity, FilterIssue5355License extension)
</details>
