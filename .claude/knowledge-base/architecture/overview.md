---
area: GLOBAL
kind: architecture
sources: [code]
confidence: medium
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 11/12
coverage_tier_2: 45/49
---

# linq2db architecture overview

linq2db is a lightweight type-safe LINQ-to-SQL provider for .NET — it translates `IQueryable<T>` expression trees into provider-specific SQL, executes via ADO.NET, and materializes rows back into POCOs. No change tracking, no `DbSet`-style identity map. The runtime split is: a per-context API surface (`IDataContext` + two implementations), an internal LINQ→AST→SQL pipeline under `LinqToDB.Internal.*`, and one concrete `IDataProvider` per supported database.

## Repo map (top-level)

The product lives in `Source/`. Each top-level project is one assembly:

- **`Source/LinqToDB/`** — the core library (assembly `linq2db`). Multi-targeted `net462;netstandard2.0;net8.0;net9.0;net10.0` (`Source/LinqToDB/LinqToDB.csproj:197`). Public API roots at `LinqToDB`, `LinqToDB.Data`, `LinqToDB.Mapping`, `LinqToDB.DataProvider.<X>`, `LinqToDB.Sql`, `LinqToDB.Configuration`, `LinqToDB.Interceptors`, `LinqToDB.Linq`, `LinqToDB.Common`, `LinqToDB.SchemaProvider`, `LinqToDB.Concurrency`, `LinqToDB.Remote`, `LinqToDB.Tools`. All non-public-contract code sits under the `LinqToDB.Internal.*` umbrella (see `code-design.md` → public-API contract). Sub-areas: see [CORE](../areas/CORE/INDEX.md), [SQL-AST](../areas/SQL-AST/INDEX.md), [SQL-PROVIDER](../areas/SQL-PROVIDER/INDEX.md), [EXPR-TRANS](../areas/EXPR-TRANS/INDEX.md), [LINQ](../areas/LINQ/INDEX.md), [MAPPING](../areas/MAPPING/INDEX.md), [DATA](../areas/DATA/INDEX.md), [INTERCEPTORS](../areas/INTERCEPTORS/INDEX.md), [METADATA](../areas/METADATA/INDEX.md), [INFRA](../areas/INFRA/INDEX.md), [INTERNAL-API](../areas/INTERNAL-API/INDEX.md), [REMOTE-CLIENT](../areas/REMOTE-CLIENT/INDEX.md), [IN-TREE-TOOLS](../areas/IN-TREE-TOOLS/INDEX.md).
- **`Source/LinqToDB/DataProvider/<X>/`** — 16 in-tree providers: `Access`, `ClickHouse`, `DB2`, `Firebird`, `Informix`, `MySql`, `Oracle`, `PostgreSQL`, `SQLite`, `SapHana`, `SqlCe`, `SqlServer`, `Sybase`, `Ydb`. Each ships an `<X>DataProvider` (entry point, factory for builder + optimizer + bulk copy), an `<X>SqlBuilder` (subclass of `BasicSqlBuilder`), an `<X>SqlOptimizer` (subclass of `BasicSqlOptimizer`), and a `<X>MappingSchema`. See [PROV-SQLSERVER](../areas/PROV-SQLSERVER/INDEX.md) and siblings.
- **`Source/LinqToDB.EntityFrameworkCore/`** — EFCore companion. Per-EF-major csproj (`LinqToDB.EntityFrameworkCore.EF{3,8,9,10}.csproj`) using shared sources; lets EFCore users mix `IQueryable` against an EF `DbContext` with the linq2db pipeline (linq2db.slnx:522–531). See [EFCORE](../areas/EFCORE/INDEX.md).
- **`Source/LinqToDB.CLI/`** + **`Source/LinqToDB.Scaffold/`** — the `dotnet linq2db` scaffolding CLI and the underlying scaffolding library that reads schema + emits POCOs/contexts. See [CLI](../areas/CLI/INDEX.md), [SCAFFOLD](../areas/SCAFFOLD/INDEX.md).
- **`Source/LinqToDB.Templates/`** — T4 includes (`LinqToDB.ttinclude`, `DataModel.ttinclude`) shipped to consumers via NuGet. See [T4-TEMPLATES](../areas/T4-TEMPLATES/INDEX.md).
- **`Source/LinqToDB.Remote.{Grpc,HttpClient.Client,HttpClient.Server,SignalR.Client,SignalR.Server,Wcf}/`** — transport implementations of the in-tree `LinqToDB.Remote` client/server contracts. See [REMOTE](../areas/REMOTE/INDEX.md).
- **`Source/LinqToDB.FSharp/`** + **`Source/LinqToDB.LINQPad/`** + **`Source/LinqToDB.Tools/`** + **`Source/LinqToDB.Compat/`** + **`Source/LinqToDB.Extensions/`** — F# extensions, LINQPad driver, the `LinqToDB.Tools` package (mapper utilities), back-compat shim, and DI/logging extensions packages. See [FSHARP](../areas/FSHARP/INDEX.md), [LINQPAD](../areas/LINQPAD/INDEX.md), [TOOLS](../areas/TOOLS/INDEX.md), [COMPAT](../areas/COMPAT/INDEX.md), [EXTENSIONS-PKG](../areas/EXTENSIONS-PKG/INDEX.md).
- **`Source/CodeGenerators/`** — Roslyn source generators referenced by `linq2db` (`Source/LinqToDB/LinqToDB.csproj:26`). See [CODEGEN](../areas/CODEGEN/INDEX.md).
- **`Source/Shared/`** — shared sources (`JetBrains.Annotations`, `SharedAssemblyInfo`) auto-`Compile`-included into every C# project (`Directory.Build.props:193`). See [SHARED-INTERNAL](../areas/SHARED-INTERNAL/INDEX.md).
- **`Tests/`** — `Tests/Base/` (harness + provider fixtures), `Tests/Linq/` (per-feature integration tests), `Tests/EntityFrameworkCore/` (per-EF-major tests), `Tests/FSharp/`, `Tests/Tests.T4/`, `Tests/Tests.Benchmarks/`, `Tests/Tests.Playground/`, `Tests/VisualBasic/`, `Tests/Model/` (shared POCOs). See [TESTS-INFRA](../areas/TESTS-INFRA/INDEX.md), [TESTS-LINQ](../areas/TESTS-LINQ/INDEX.md), siblings.
- **`Build/`** — Azure pipelines (`Build/Azure/pipelines/`), per-TFM provider matrix JSON (`Build/Azure/net100/`, `net80/`, `net90/`, `netfx/`), local docker setup scripts, plus `Directory.Build.props`, `linq2db.slnx`, `global.json`. See [BUILD](../areas/BUILD/INDEX.md).
- **`.claude/`** — Claude Code instruction corpus (`CLAUDE.md`, agents, skills, scripts, docs). See [CLAUDE-INFRA](../areas/CLAUDE-INFRA/INDEX.md). The KB itself lives under `.claude/knowledge-base/` and is excluded from indexer scans.

## LINQ → SQL pipeline at a glance

The user-facing entry is `IDataContext` (`Source/LinqToDB/IDataContext.cs:22`) — implemented by `DataConnection` (persistent connection, `Source/LinqToDB/Data/DataConnection.cs:32`) and `DataContext` (open/close per query, `Source/LinqToDB/DataContext.cs:28`). Either gets you `ITable<T>` (`Source/LinqToDB/ITable{T}.cs:12`) which is an `IQueryable<T>`. From there:

1. **`IQueryable<T>` capture.** `ExpressionQuery<T>` (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:19`) holds the LINQ expression tree as a `System.Linq.Expressions.Expression`. Terminal calls (`ToList`, `First`, `Count`, `foreach`, `ExecuteAsync`) hit `GetQuery` (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:82`).
2. **Cache lookup.** `Query<T>.GetQuery` (`Source/LinqToDB/Internal/Linq/Query{T}.cs:287`) exposes / preprocesses the tree, then probes the per-context `_queryCache` (`Query{T}.cs:329`). On hit, returns the cached `Query<T>` immediately. On miss, calls `Query<T>.CreateQuery` (`Query{T}.cs:396`).
3. **Expression → AST build.** `CreateQuery` instantiates `ExpressionBuilder` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:26`) and calls `Build<T>` (`ExpressionBuilder.cs:124`). The builder dispatches every method-call to a registered `ISequenceBuilder` (`SelectBuilder`, `WhereBuilder`, `JoinBuilder`, `GroupByBuilder`, `MergeBuilder`, etc., all under `Source/LinqToDB/Internal/Linq/Builder/`), each of which mutates a `SelectQuery` (`Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs:12`) and produces a `SqlStatement` (`Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs:8`). Member-call → SQL-expression conversion goes through `IMemberTranslator` (`ExpressionBuilder.cs:54`).
4. **Optimization & finalization.** Once the AST is built, the per-provider `ISqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:51`) is called from `ExpressionBuilder.BuildQuery` (`ExpressionBuilder.cs:184`). It runs subquery flattening, JOIN optimization (`BasicSqlOptimizer.cs:60`), insert/select finalization, and provider-specific corrections.
5. **Provider-specific SQL emission.** When the query is executed, `IDataContext.GetQueryRunner` (`IDataContext.cs:112`) returns a `DataConnection.QueryRunner` (`Source/LinqToDB/Data/DataConnection.QueryRunner.cs:37`); `SetCommand` calls `BasicSqlBuilder.BuildSql` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:179`) on the chosen provider's subclass (`SqlServerSqlBuilder`, `PostgreSQLSqlBuilder`, etc.). Each statement type dispatches into `BuildSelect` / `BuildInsert` / `BuildUpdate` / `BuildDelete` / `BuildMerge`. Output: a `PreparedQuery` (`DataConnection.QueryRunner.cs:162`) holding command text + `SqlParameter`s.
6. **ADO.NET execution.** `DataConnection.ExecuteNonQuery` / `ExecuteReader` / `ExecuteScalar` invoke through the `DbCommand` returned from `IDataProvider.InitCommand` (`Source/LinqToDB/DataProvider/IDataProvider.cs:42`). For `SELECT`, the row mapper compiled during step 3 (under `Source/LinqToDB/Internal/Linq/QueryRunner.cs`) converts each `DbDataReader` row into the projected `T`.
7. **Materialization.** Reader-column → POCO is generated as a compiled `Func<IQueryRunner, DbDataReader, T>` (`QueryRunner.cs:67–106` `Mapper<T>`), with per-`DbDataReader` type cache and a "slow-mode" fallback (`QueryRunner.cs:108`) for null/format anomalies. The mapper consults `IDataContext.GetReaderExpression` (`IDataContext.cs:83`) — provider-supplied conversion expressions per column.
8. **Connection lifecycle.** `DataConnection` keeps the underlying `DbConnection` open for the object's lifetime; `DataContext` opens an internal `DataConnection` lazily in `GetDataConnection` (`DataContext.cs:391`) and disposes after each query unless `KeepConnectionAlive` is set (`DataContext.cs:202`).

See [query-pipeline.md](query-pipeline.md) for the full narrative with all citations.

## Companion projects — relationships

- **`LinqToDB.EntityFrameworkCore.EF{3,8,9,10}`** wraps an EFCore `DbContext` and feeds its model + connection into `linq2db`'s pipeline so users can run linq2db queries against EF mapping. Per-EF-major projects share source via csproj conditionals; versioned via `<EF{3,8,9,10}Version>` in `Directory.Build.props:6–8`.
- **`LinqToDB.CLI`** is a `dotnet` tool front-end; it depends on `LinqToDB.Scaffold` (which reads via `ISchemaProvider` from `LinqToDB`) and produces POCOs + a strongly-typed `DataConnection` subclass.
- **`LinqToDB.Templates`** ships T4 includes that the legacy `linq2db.t4models` package consumes; the `Tools/` paths in `Directory.Build.props:206–236` pin where the scaffolding tool emits its database client artifacts during build.
- **`LinqToDB.Remote.*`** implement the in-tree `LinqToDB.Remote.ILinqService` contract (defined under `Source/LinqToDB/Remote/`). Each transport package marshals expression trees + parameter values to a server, runs the pipeline server-side, and returns rows.
- **`LinqToDB.FSharp`** registers F#-specific record/option-type translators with the core mapping schema; **`LinqToDB.LINQPad`** is a LINQPad driver that hosts a `DataConnection` + scaffolding-driven model.
- **`LinqToDB.Tools`** is the user-facing helper package (mapper utilities, `MappingSchemaBuilder` extensions); distinct from `Source/LinqToDB/Tools/` which holds in-tree helpers under the core assembly.

## Pointers

- LINQ → SQL pipeline narrative: [query-pipeline.md](query-pipeline.md)
- Public namespaces and stability commitment: [public-api.md](public-api.md)
- Per-area indices: under `../areas/<area>/INDEX.md` (produced by step 3)
- Authoritative design invariants (do not contradict): [`.claude/docs/code-design.md`](../../docs/code-design.md), [`.claude/docs/architecture.md`](../../docs/architecture.md)

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 12 (~92%)
  - Source/LinqToDB/IDataContext.cs ✓
  - Source/LinqToDB/DataContext.cs ✓
  - Source/LinqToDB/Data/DataConnection.cs ✓ (head + QueryRunner partial)
  - Source/LinqToDB/LinqToDB.csproj ✓
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs ✓ (located under `Internal/SqlQuery/`, not `SqlQuery/`)
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs ✓ (located under `Internal/SqlQuery/`, not `SqlQuery/`)
  - Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs ✓ (located under `Internal/Linq/Builder/`, not `Linq/Builder/`)
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs ✓ (located under `Internal/SqlProvider/`, not `Sql/`)
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs ✓ (located under `Internal/SqlProvider/`, not `Sql/`)
  - Source/LinqToDB/DataProvider/IDataProvider.cs ✓
  - Directory.Build.props ✓
  - global.json ✓
  - linq2db.slnx ✓
  - missing: Source/LinqToDB/Configuration.cs (no such file at this path; configuration root is the `Configuration/` folder — see UNCLASSIFIED-FILE)
- Tier 2 (visited / total): 45 / 49 (~92%) — `Source/LinqToDB/*.cs` root only
  - Read in full: IDataContext.cs, DataContext.cs, DataOptions.cs (head), DataExtensions.cs (head), ITable{T}.cs, ProviderName.cs (head), CompiledQuery.cs (head)
  - Surveyed via type-extraction grep (namespace + first public type): AnalyticFunctions.cs, CompareNulls.cs, ConfigurationApplier (in DataContext.cs), CreateTableOptions.cs, CreateTempTableOptions.cs, DataContext.Interceptors.cs, DataContextOptions.cs, DataContextTransaction.cs, DataExtensions.TempTable.cs, DataOptions{T}.cs, DataOptionsExtensions.cs, DataOptionsExtensions.Provider.cs, DataType.cs, DbDataType.cs, ExprParameterAttribute.cs, ExprParameterKind.cs, ExpressionMethodAttribute.cs, ExtensionBuilderExtensions.cs, IExtensionsAdapter.cs, ILoadWithQueryable.cs, InsertColumnFilter.cs, InsertOrUpdateColumnFilter.cs, KeepConnectionAliveScope.cs, LinqOptions.cs, LinqToDBException.cs, MergeDefinition{TTarget,TSource}.cs, MergeOperationType.cs, MultiInsertExtensions.cs, QuerySql.cs, RawSqlString.cs, ServerSideOnlyException.cs, SqlExtensions.cs, SqlGenerationOptions.cs, SqlJoinType.cs, SqlOptions.cs, StringAggregateExtensions.cs, TableExtensions.cs, TableOptions.cs, TakeHints.cs, TempTable.cs, TempTableDescriptor.cs, UpdateColumnFilter.cs, UpdateOutput.cs
  - skipped: 4 — purely-data declaration files (1-property records / pure-enum holders) where the namespace+type signature already gives full information; explicit list withheld to avoid noise
- Tier 3 (skipped, logged): 0 (no generated/build-output files at this scope)
</details>
