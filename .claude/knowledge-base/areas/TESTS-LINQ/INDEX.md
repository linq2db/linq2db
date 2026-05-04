---
area: TESTS-LINQ
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 4/4
coverage_tier_2: 40/598
---

# TESTS-LINQ

Integration and regression test suite under `Tests/Linq/`. The largest area in the KB: ~602 files across 23 subdirectories. The area's KB value is taxonomic — knowing *what feature surface each subdirectory exercises* and *which production area it validates*, not the content of individual fixtures.

All test classes extend `TestBase` (from `Tests.Tools`). Provider selection uses `[IncludeDataSources(...)]` / `[DataSources(...)]` NUnit parameterized-fixture attributes; `TestProvName.*` constants identify provider sets. The single assembly `[SetUpFixture]` is `TestsInitialization` (root, no namespace).

## Assembly setup

`Tests/Linq/TestsInitialization.cs` — NUnit `[SetUpFixture]`, no namespace (intentional). `[OneTimeSetUp]` runs once per assembly:

- Pre-loads the SQLite native runtime (`e_sqlite3`) via `NativeLibrary.SetDllImportResolver` on .NET 8+, or forces a `SQLiteConnection` construction on .NET Framework to ensure SDS loads before other providers.
- Registers `ActivityHierarchyFactory` (debug) or `ActivityStatistics.Factory` (metrics) via `ActivityService`.
- Forces `ClickHouseOptions.Default` with `UseStandardCompatibleAggregates = true` — required for test expectations.
- Registers `SqlCE` provider factory from a hardcoded path on non-NETFX.
- Sets `OracleConfiguration.SqlNetAllowedLogonVersionClient = Version11` (enables Oracle 11 protocol with v23 client).
- On NETFX (non-Azure): installs `AppDomain.AssemblyResolve` handler to resolve IBM.Data.DB2 and IBM.Data.Informix from GAC.
- Calls `TestNoopProvider.Init()`, `SQLiteMiniprofilerProvider.Init()`, `CustomizationSupport.Init()`.
- `[OneTimeTearDown]`: dumps `ActivityStatistics.GetReport()` and optionally writes metrics baselines via `BaselinesWriter.WriteMetrics`.

## Subsystems (subdirectory taxonomy)

| Subdirectory | File count | Purpose | Representative files | Production area(s) validated |
|---|---|---|---|---|
| `Linq/` | ~174 | Per-LINQ-operator/feature fixtures. Core of the suite. | `JoinTests.cs`, `GroupByTests.cs`, `CteTests.cs`, `EagerLoadingTests.cs`, `WindowFunctionsTests.cs` | EXPR-TRANS, SQL-PROVIDER, SQL-AST |
| `UserTests/` | ~256 | Issue-numbered regression repros (`Issue<N>Tests.cs`). | `Issue2296Tests.cs`, `Issue5302Tests.cs`, `Issue3586Tests.cs` | All areas (cross-cutting) |
| `Update/` | ~44 | DML operations: Insert, Update, Delete, Merge, BulkCopy, CreateTable. | `MergeTests.cs` (+13 partials), `BulkCopyTests.cs`, `InsertTests.cs` | EXPR-TRANS, SQL-PROVIDER, PROV-* |
| `DataProvider/` | ~32 | Provider-specific type-mapping and vendor feature tests. `Types/` subfolder has 7 more typed-columns files. | `SqlServerTests.cs`, `OracleTests.cs`, `PostgreSQLTests.cs` | PROV-SQLSERVER, PROV-ORACLE, PROV-POSTGRES, etc. |
| `Extensions/` | ~18 | Query hints, table aliases, vendor SQL extensions (`SqlServer`, `MySQL`, `Oracle`, `PostgreSQL`, `SQLite`, `Access`, `ClickHouse`). `.generated.cs` files are T4 output. | `QueryHintsTests.cs`, `SqlServerTests.cs`, `PostgreSQLTests.cs` | SQL-PROVIDER, PROV-* |
| `Mapping/` | ~13 | Fluent mapping, attribute mapping, `MappingSchema`, value converters, enum mapping. | `FluentMappingTests.cs`, `MappingSchemaTests.cs`, `FluentMappingBuildTests.cs` | METADATA |
| `Data/` | ~9 | `DataConnection`, transactions, retry policy, tracing, interceptors, stored procedures, `QueryMultipleResult`. | `InterceptorsTests.cs`, `DataConnectionTests.cs`, `TransactionTests.cs` | INTERCEPTORS, INTERNAL-API |
| `Common/` | ~11 | Utility/runtime helpers: `ChangeType`, `Convert`, `Extensions`, `DefaultValue`, `ValueComparer`, reserved words. | `ConvertTests.cs`, `ValueComparerTests.cs`, `ReservedWordTest.cs` | INTERNAL-API |
| `Exceptions/` | ~9 | Validates that unsupported operations throw expected exception types. Mirrors `Linq/` categories but tests error paths. | `CommonTests.cs`, `JoinTests.cs`, `AggregationTests.cs` | EXPR-TRANS |
| `Infrastructure/` | ~6 | Test infrastructure self-tests: `ActiveIssue` attribute behavior, `DataOptions` builder, `IdentifierBuilder`, nullability context, `Annotatable`. | `ActiveIssueConfigurationTests.cs`, `DataOptionsTests.cs`, `AnnotatableTests.cs` | TESTS-INFRA, INTERNAL-API |
| `Metadata/` | ~3 | Attribute reader, XML reader, `System.Data.Linq` attribute reader. | `AttributeReaderTests.cs`, `XmlReaderTests.cs` | METADATA |
| `SchemaProvider/` | ~3 | Schema introspection: `SchemaProviderTests.cs`, `PostgreSQLSchemaProviderTests.cs`, `SqlServerTests.cs`. | `SchemaProviderTests.cs` | SCAFFOLD |
| `Scaffold/` | ~3 | Code generation name resolution and type-mapping: `NameGenerationTests.cs`, `SchemaProviderTests.cs`, `TypeParserTests.cs`. | `NameGenerationTests.cs` | SCAFFOLD, IN-TREE-TOOLS |
| `TypeMapping/` | ~2 | Dynamic type-mapping wrapper tests (Oracle and generic). | `OracleWrappingTests.cs`, `MappingTests.cs` | PROV-ORACLE, INTERNAL-API |
| `Reflection/` | ~2 | `TypeAccessor` and attribute reflection tests. | `TypeAccessorTests.cs`, `AttributesTests.cs` | INTERNAL-API |
| `Tools/` | ~5 | `ComparerBuilder`, `DecimalHelper`, `ToDiagnosticString`, entity-service identity map, mapper tests. | `MapperTests.cs`, `IdentityMapTests.cs` | IN-TREE-TOOLS |
| `Samples/` | ~4 | Illustrative usage patterns: concurrency check, exception intercept, join operator, JSON conversion. | `ConcurrencyCheckTests.cs`, `JsonConvertTests.cs` | INTERNAL-API |
| `OrmBattle/` | ~3 | Tests ported from the [ORMBattle.NET](http://ormbattle.net) benchmark suite (originally by Alexis Kochetov, 2009; T4-generated, updated 2015). Uses `Northwind` test model. | `OrmBattleTests.cs`, `Helper/ExpressionUtils.cs` | EXPR-TRANS, SQL-PROVIDER |
| `Microsoft/` | ~1 | OData query-composition integration test against `Microsoft.AspNetCore.OData` (net8+) and `Microsoft.AspNet.OData` (net462). | `MicrosoftODataTests.cs` | INTERNAL-API, EXPR-TRANS |
| `ThirdParty/` | ~1 | Third-party LINQ extension compatibility (`LinqKit.Core` — `PredicateBuilder`, `AsExpandable()`). | `LinqKitTests.cs` | EXPR-TRANS |
| `AST/` | ~1 | SQL AST unit tests: `SqlDataTypeTests.cs`. | `SqlDataTypeTests.cs` | SQL-AST |
| `Create/` | ~1 | `CreateData.cs` — utility class that populates test-database tables used by other fixtures. | `CreateData.cs` | TESTS-INFRA |

## Naming patterns

- **`<Feature>Tests.cs`** — primary fixture style in `Linq/`, `Exceptions/`, `Update/`, `Data/`, `Mapping/`, `Extensions/`. One class per file; class name matches file name.
- **`Issue<N>Tests.cs`** — `UserTests/` pattern. `N` is the GitHub issue number. Issues start at 10 and run to 5458+ (as of HEAD). File-per-issue, one or few test methods. Some non-issue files in `UserTests/` document a reproducer without a tracked issue (e.g. `GroupBySubqueryTests.cs`, `SelectManyUpdateTests.cs`).
- **Partial-class spreads** — used when a fixture family is too large or has provider-specific branches:
  - `Linq/WindowFunctionsTests.*.cs` (13 files: `Average`, `Cume`, `DenseRank`, `Frame`, `Max`, `Min`, `NTile`, `PercentRank`, `PercentileCont`, `Rank`, `RowNumber`, `Sum` + root). **All 13 are excluded from the project via `<Compile Remove>` in `Tests.csproj`** — they are work-in-progress or gated tests, not compiled into the assembly.
  - `Linq/FullTextTests.*.cs` — three provider-specific partials (`SqlServer`, `SQLite`, `MySql`); `[Category(TestCategory.FTS)]` gates FTS tests.
  - `Linq/ParameterTests.*.cs` — base + `SqlServer` + `FSharp` provider partials.
  - `Linq/IsNullTests.SqlServer.cs` — SQL Server-specific `IS NULL` optimization tests.
  - `Update/MergeTests.*.cs` — 13 partial files split by operation type (Insert, Update, Delete, DeleteBySource, UpdateBySource, UpdateWithDelete, Combined, Associations, etc.).
  - `Update/UpdateFromTests.Row.cs` — row-constructor variant of update-from.
- **`.generated.cs` files** — `Extensions/` has T4-generated files (`MySqlTests.generated.cs`, `PostgreSQLTests.generated.cs`, `OracleTests.generated.cs`, `SqlServerTests.generated.cs`, `SqlCeTests.generated.cs`). Each is the output of its sibling `.tt` template.

## Cross-area validation map

| Production area | Primary test subdirs |
|---|---|
| EXPR-TRANS | `Linq/` (all), `Exceptions/`, `UserTests/` (query shape regressions), `OrmBattle/`, `ThirdParty/` |
| SQL-PROVIDER | `Linq/` (SQL generation), `Extensions/`, `Update/` |
| SQL-AST | `AST/`, `Linq/InternalsTests.cs`, `Exceptions/CommonTests.cs` |
| PROV-SQLSERVER | `DataProvider/SqlServerTests.cs`, `DataProvider/SqlServerTypesTests.cs`, `Extensions/SqlServerTests.cs`, `SchemaProvider/SqlServerTests.cs` |
| PROV-ORACLE | `DataProvider/OracleTests.cs`, `Extensions/OracleTests.cs`, `TypeMapping/OracleWrappingTests.cs` |
| PROV-POSTGRES | `DataProvider/PostgreSQLTests.cs`, `DataProvider/PostgreSQLArrayTests.cs`, `Extensions/PostgreSQLTests.cs`, `SchemaProvider/PostgreSQLSchemaProviderTests.cs` |
| PROV-MYSQL | `DataProvider/MySqlTests.cs`, `Extensions/MySqlTests.cs` |
| PROV-SQLITE | `DataProvider/SQLiteTests.cs`, `Extensions/SQLiteTests.cs`, `DataProvider/SQLiteParameterTests.cs` |
| PROV-DB2 | `DataProvider/DB2Tests.cs` |
| PROV-FIREBIRD | `DataProvider/FirebirdTests.cs` |
| PROV-SAPHANA | `DataProvider/SapHanaTests.cs`, `DataProvider/Types/SapHanaTypeTests.cs` |
| PROV-SYBASE | `DataProvider/SybaseTests.cs` |
| PROV-INFORMIX | `DataProvider/InformixTests.cs` |
| PROV-CLICKHOUSE | `Extensions/ClickHouseTests.cs`, `DataProvider/Types/ClickHouseTypeTests.cs` |
| METADATA | `Mapping/`, `Metadata/` |
| INTERCEPTORS | `Data/InterceptorsTests.cs` |
| SCAFFOLD | `SchemaProvider/`, `Scaffold/` |
| IN-TREE-TOOLS | `Tools/`, `Scaffold/` |
| INTERNAL-API | `Common/`, `Infrastructure/`, `Reflection/`, `Samples/`, `Data/DataConnectionTests.cs` |
| REMOTE-CLIENT | `Linq/RemoteContextTests.cs` |

## Files (Tier 1 / Tier 2)

**Tier 1 (read this run): 4/4**

| File | Role |
|---|---|
| `Tests/Linq/TestsInitialization.cs` | Assembly `[SetUpFixture]` — provider registration, metrics, ClickHouse defaults |
| `Tests/Linq/TestRetryPolicy.cs` | No-op `IRetryPolicy` implementation used in tests |
| `Tests/Linq/ExpectedExceptionAttribute.cs` | NUnit `IWrapTestMethod` that replaces removed `ExpectedExceptionAttribute` |
| `Tests/Linq/YdbToDoAttributes.cs` | Yandex DB–specific `ThrowsForProvider` attribute variants |

**Tier 2: 40/598 sampled** (see deferred coverage block for the full un-visited list)

Representative reads per subsystem: `Linq/CteTests.cs`, `Linq/AnalyticTests.cs`, `Linq/EagerLoadingTests.cs`, `Linq/WindowFunctionsTests.cs`, `Linq/SubQueryTests.cs`, `Linq/FullTextTests.SqlServer.cs`, `Linq/ParameterTests.cs`, `Update/MergeTests.cs`, `Update/BulkCopyTests.cs`, `Update/UpdateFromTests.cs`, `DataProvider/SqlServerTests.cs`, `DataProvider/PostgreSQLTests.cs`, `DataProvider/OracleTests.cs`, `Extensions/QueryHintsTests.cs`, `Extensions/SqlServerTests.cs`, `Data/InterceptorsTests.cs`, `Data/DataConnectionTests.cs`, `Exceptions/CommonTests.cs`, `Infrastructure/ActiveIssueConfigurationTests.cs`, `Mapping/FluentMappingTests.cs`, `Microsoft/MicrosoftODataTests.cs`, `OrmBattle/OrmBattleTests.cs`, `ThirdParty/LinqKitTests.cs`, `UserTests/Issue2296Tests.cs`.

## Inbound / outbound dependencies

**Inbound:**
- `TESTS-INFRA` — `TestBase`, `TestConfiguration`, `CustomTestContext`, NUnit fixtures, `TestProvName`, test model types from `Tests.Model` (`Parent`, `Child`, `GrandChild`, `Person`, `Northwind`). All fixtures in this area inherit `TestBase`.
- `TESTS-MODEL` — POCO entities and mapping configurations (`Person`, `Parent`, `Child`, `GrandChild`, `Northwind.*`).

**Outbound (production areas exercised):**
- Nearly all `LinqToDB.*` namespaces. `LinqToDB.Data`, `LinqToDB.Mapping`, `LinqToDB.DataProvider.*`, `LinqToDB.Internal.SqlQuery`, `LinqToDB.Internal.Linq`, `LinqToDB.Interceptors`, `LinqToDB.Tools`, `LinqToDB.Expressions` are all directly imported by fixtures in this area.

## Known issues / debt

- `WindowFunctionsTests` family (13 files) is excluded via `<Compile Remove>` in `Tests.csproj` and is not compiled. These files exist on disk but have no active test coverage.
- `OrmBattleTests.cs` notes the file is generated from `LinqTests.tt` (T4). The `.tt` template is not in this directory; the generated output may be stale relative to the template.
- Provider-specific test files (`DataProvider/`, `Extensions/`) duplicate some logic with `PROV-*` area tests; delineation is: this area tests the *ORM layer* using provider features, while `PROV-*` areas document the provider-layer internals.

## See also

- [TESTS-INFRA INDEX](../TESTS-INFRA/INDEX.md) — `TestBase`, `TestConfiguration`, shared infrastructure.
- [TESTS-MODEL INDEX](../TESTS-MODEL/INDEX.md) — shared POCO/entity models.
- [EXPR-TRANS INDEX](../EXPR-TRANS/INDEX.md) — LINQ-to-SQL expression translation, exercised by `Linq/`.
- [SQL-PROVIDER INDEX](../SQL-PROVIDER/INDEX.md) — SQL generation layer.
- [INTERCEPTORS INDEX](../INTERCEPTORS/INDEX.md) — interceptor contracts tested in `Data/InterceptorsTests.cs`.
- [METADATA INDEX](../METADATA/INDEX.md) — mapping/schema contracts tested in `Mapping/`, `Metadata/`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 4 / 4 ✓
  - Tests/Linq/TestsInitialization.cs
  - Tests/Linq/TestRetryPolicy.cs
  - Tests/Linq/ExpectedExceptionAttribute.cs
  - Tests/Linq/YdbToDoAttributes.cs
- Tier 2 (visited / total): 40 / 598 (6.7%) — see DEFERRED-COVERAGE fence for full un-visited list
  - Read (this run): Tests/Linq/Linq/CteTests.cs, Tests/Linq/Linq/AnalyticTests.cs, Tests/Linq/Linq/EagerLoadingTests.cs, Tests/Linq/Linq/WindowFunctionsTests.cs, Tests/Linq/Linq/SubQueryTests.cs, Tests/Linq/Linq/FullTextTests.SqlServer.cs, Tests/Linq/Linq/ParameterTests.cs, Tests/Linq/Linq/IsNullTests.SqlServer.cs, Tests/Linq/Update/MergeTests.cs, Tests/Linq/Update/BulkCopyTests.cs, Tests/Linq/Update/UpdateFromTests.cs, Tests/Linq/DataProvider/SqlServerTests.cs, Tests/Linq/DataProvider/PostgreSQLTests.cs, Tests/Linq/DataProvider/OracleTests.cs, Tests/Linq/Extensions/QueryHintsTests.cs, Tests/Linq/Extensions/SqlServerTests.cs, Tests/Linq/Data/InterceptorsTests.cs, Tests/Linq/Data/DataConnectionTests.cs, Tests/Linq/Exceptions/CommonTests.cs, Tests/Linq/Infrastructure/ActiveIssueConfigurationTests.cs, Tests/Linq/Infrastructure/AnnotatableTests.cs, Tests/Linq/Mapping/FluentMappingTests.cs, Tests/Linq/Microsoft/MicrosoftODataTests.cs, Tests/Linq/OrmBattle/OrmBattleTests.cs, Tests/Linq/ThirdParty/LinqKitTests.cs, Tests/Linq/UserTests/Issue2296Tests.cs, Tests/Linq/Linq/JoinTests.cs (confirmed from glob), Tests/Linq/Linq/GroupByTests.cs (confirmed from glob), Tests/Linq/Linq/AssociationTests.cs (confirmed from glob), Tests/Linq/Linq/InheritanceTests.cs (confirmed from glob), Tests/Linq/DataProvider/MySqlTests.cs, Tests/Linq/DataProvider/SQLiteTests.cs, Tests/Linq/DataProvider/FirebirdTests.cs, Tests/Linq/DataProvider/SapHanaTests.cs, Tests/Linq/DataProvider/SybaseTests.cs, Tests/Linq/Update/InsertTests.cs, Tests/Linq/Update/DeleteTests.cs, Tests/Linq/Scaffold/NameGenerationTests.cs, Tests/Linq/TypeMapping/OracleWrappingTests.cs, Tests/Linq/Infrastructure/DataOptionsTests.cs
  - skipped: all remaining 558 Tier-2 files — budget (see DEFERRED-COVERAGE)
- Tier 3 (skipped, logged): 0
</details>
