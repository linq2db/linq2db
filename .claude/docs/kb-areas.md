# KB area registry

Single source of truth for the areas the knowledge base is organized around. Indexers, the issue detector, the detected-issues schema, and `/kb-issues` filtering all reference area codes from this file.

This file is **bootstrapped** by `/kb-build` step 1 (the skill proposes an initial table from a directory scan; the user confirms or edits) and **maintained by hand** thereafter. Each `/kb-refresh` flags new top-level dirs that don't map to any existing area and asks the user to extend this file.

## Conventions

- Area code is uppercase, alphanumeric, with optional `-` separator. Keep it short (≤ 12 chars).
- Path patterns use forward slashes. Glob support is the same as `Glob` tool patterns.
- Tier-1 files are the canonical anchors for the area. Tier-2 globs catch implementation files. Tier-3 is the global Tier-3 pattern set in [`kb-coverage-tiers.md`](kb-coverage-tiers.md) — not duplicated here.
- An area row with no Tier-1 / Tier-2 files (e.g. `GLOBAL`) is for cross-cutting docs only; indexers skip the codebase scan for those areas.

## Bootstrap proposal (initial scan output)

The table below is the proposal `/kb-build` step 1 will produce on first run. The user reviews and edits before step 1 is marked `done`. Every top-level dir under `Source/` and `Tests/` must appear in some area's path patterns or be explicitly excluded with a one-line reason.

| Area code | Path patterns | Tier-1 files (canonical anchors) | Tier-2 globs | Notes / owner |
|---|---|---|---|---|
| `CORE` | `Source/LinqToDB/*.cs` (root only), `Source/LinqToDB/LinqToDB.csproj`, `Source/LinqToDB/Configuration/**` | `IDataContext.cs`, `DataContext.cs`, `Data/DataConnection.cs`, `LinqToDB.csproj`, `Configuration/LinqToDBSection.cs`, `Configuration/ILinqToDBSettings.cs` | `Source/LinqToDB/*.cs`, `Configuration/*.cs` | Root-level public surface + configuration entry types (file `Configuration.cs` does not exist; the `Configuration/` folder is the entry surface). Everything else is a sub-area. |
| `SQL-AST` | `Source/LinqToDB/Internal/SqlQuery/**`, `Source/LinqToDB/SqlQuery/**` | `Internal/SqlQuery/SqlStatement.cs`, `Internal/SqlQuery/SelectQuery.cs`, `Internal/SqlQuery/SqlExpression.cs`, `Internal/SqlQuery/SqlField.cs`, `Internal/SqlQuery/SqlBinaryExpression.cs` | `**/Sql*.cs` under both folders | AST node types. New types must land in `LinqToDB.Internal.SqlQuery`; legacy public types under `Source/LinqToDB/SqlQuery/` are slated to move on next major (see `code-design.md`). |
| `SQL-PROVIDER` | `Source/LinqToDB/Internal/SqlProvider/**`, `Source/LinqToDB/Sql/**` | `Internal/SqlProvider/BasicSqlBuilder.cs`, `Internal/SqlProvider/BasicSqlOptimizer.cs`, `Internal/SqlProvider/ISqlBuilder.cs`, `Internal/SqlProvider/ISqlOptimizer.cs` | `**/*SqlBuilder.cs`, `**/*SqlOptimizer.cs` | Provider-agnostic SQL emission. Build/optimize logic is under `Internal/SqlProvider/`; the `Sql/` folder holds public `Sql.*` extension surface. |
| `EXPR-TRANS` | `Source/LinqToDB/Internal/Linq/Builder/**` | `Internal/Linq/Builder/ExpressionBuilder.cs`, `Internal/Linq/Builder/MethodCallBuilder.cs`, every `*Builder.cs` root | `**/*Builder.cs` | LINQ → SQL translation pipeline. Per-method dispatch is `MethodCallBuilder` + `[BuildsMethodCall]` markers + the `BuildersGenerator` source generator (no hand-written `MethodCallParser.cs`). |
| `LINQ` | `Source/LinqToDB/Internal/Linq/**` (excluding `Builder/`) | `Internal/Linq/Query.cs`, `Internal/Linq/Query{T}.cs`, `Internal/Linq/IQueryRunner.cs`, `Internal/Linq/QueryRunner.cs`, `Internal/Linq/ExpressionQuery.cs` | `Source/LinqToDB/Internal/Linq/*.cs` | Query execution + caching. |
| `MAPPING` | `Source/LinqToDB/Mapping/**` | `MappingSchema.cs`, `EntityDescriptor.cs`, `ColumnDescriptor.cs` | `**/*.cs` under Mapping | POCO ↔ table/column mapping. |
| `DATA` | `Source/LinqToDB/Data/**` | `DataConnection.cs` (cross-listed), `DataContextExtensions.cs`, `CommandInfo.cs` | `**/*.cs` under Data | Connection lifecycle + bulk operations. `DataContextExtensions.cs` carries the public raw-SQL + bulk-copy entry points (no standalone `BulkCopy.cs` exists). |
| `EXPR` | `Source/LinqToDB/Expressions/**`, `Source/LinqToDB/Extensions/**`, `Source/LinqToDB/LinqExtensions/**` | `Expressions/IExpressionEvaluator.cs`, `Expressions/ExpressionExtensions.cs`, `Expressions/MemberHelper.cs`, `LinqExtensions/LinqExtensions.cs`, `LinqExtensions/ICteBuilder.cs` | `**/*.cs` under those dirs | Expression-tree utilities and LINQ extension methods (separate from EXPR-TRANS, which is the LINQ→SQL pipeline). |
| `INFRA` | `Source/LinqToDB/Async/**`, `Source/LinqToDB/Common/**`, `Source/LinqToDB/Compatibility/**`, `Source/LinqToDB/Concurrency/**`, `Source/LinqToDB/Configuration/**`, `Source/LinqToDB/Reflection/**`, `Source/LinqToDB/Resources/**`, `Source/LinqToDB/Properties/**` | `Async/AsyncExtensions.cs`, `Common/Configuration.cs`, `Common/Converter.cs`, `Reflection/TypeAccessor.cs`, `Reflection/MemberAccessor.cs` | `**/*.cs` under those dirs | Cross-cutting plumbing: async helpers, common utils, concurrency, config, reflection, resources. `Configuration/**` files are cross-listed with CORE (CORE owns `LinqToDBSection.cs` + `ILinqToDBSettings.cs` as its Tier-1); INFRA treats them as Tier-2. The `Common/Configuration.cs` file is the `LinqToDB.Common.Configuration` static-flag class — distinct from the `Configuration/` namespace. |
| `INTERCEPTORS` | `Source/LinqToDB/Interceptors/**`, `Source/LinqToDB/Metrics/**` | (TBD on first read) | `**/*.cs` under those dirs | Pipeline interception + metrics. |
| `INTERNAL-API` | `Source/LinqToDB/Internal/**`, `Source/LinqToDB/PublicAPI/**` | (TBD on first read) | `**/*.cs` under those dirs | Internal-namespace types and public-API analyzer baselines. |
| `METADATA` | `Source/LinqToDB/Metadata/**`, `Source/LinqToDB/SchemaProvider/**` | (TBD on first read) | `**/*.cs` under those dirs | Attribute / fluent metadata + schema discovery. |
| `REMOTE-CLIENT` | `Source/LinqToDB/Remote/**` | (TBD on first read) | `**/*.cs` under Remote | In-tree remote client/server contracts. |
| `IN-TREE-TOOLS` | `Source/LinqToDB/Tools/**` | (TBD on first read) | `**/*.cs` under Source/LinqToDB/Tools | In-tree helpers under the LinqToDB assembly (distinct from the `LinqToDB.Tools` package). |
| `PROV-SQLSERVER` | `Source/LinqToDB/DataProvider/SqlServer/**` | `SqlServerDataProvider.cs`, `SqlServerSqlBuilder.cs`, `SqlServerSqlOptimizer.cs` | `**/SqlServer*.cs` | |
| `PROV-POSTGRES` | `Source/LinqToDB/DataProvider/PostgreSQL/**` | `PostgreSQLDataProvider.cs`, `PostgreSQLSqlBuilder.cs` | `**/PostgreSQL*.cs` | |
| `PROV-MYSQL` | `Source/LinqToDB/DataProvider/MySql/**` | `MySqlDataProvider.cs`, `MySqlSqlBuilder.cs` | `**/MySql*.cs` | |
| `PROV-ORACLE` | `Source/LinqToDB/DataProvider/Oracle/**` | `OracleDataProvider.cs`, `OracleSqlBuilder.cs` | `**/Oracle*.cs` | |
| `PROV-SQLITE` | `Source/LinqToDB/DataProvider/SQLite/**` | `SQLiteDataProvider.cs`, `SQLiteSqlBuilder.cs` | `**/SQLite*.cs` | |
| `PROV-FIREBIRD` | `Source/LinqToDB/DataProvider/Firebird/**` | `FirebirdDataProvider.cs`, `FirebirdSqlBuilder.cs` | `**/Firebird*.cs` | |
| `PROV-DB2` | `Source/LinqToDB/DataProvider/DB2/**` | `DB2DataProvider.cs`, `DB2SqlBuilder.cs` | `**/DB2*.cs` | |
| `PROV-ACCESS` | `Source/LinqToDB/DataProvider/Access/**` | `AccessDataProvider.cs`, `AccessSqlBuilder.cs` | `**/Access*.cs` | |
| `PROV-INFORMIX` | `Source/LinqToDB/DataProvider/Informix/**` | `InformixDataProvider.cs`, `InformixSqlBuilder.cs` | `**/Informix*.cs` | |
| `PROV-SYBASE` | `Source/LinqToDB/DataProvider/Sybase/**` | `SybaseDataProvider.cs`, `SybaseSqlBuilder.cs` | `**/Sybase*.cs` | SAP ASE. |
| `PROV-SAPHANA` | `Source/LinqToDB/DataProvider/SapHana/**` | `SapHanaDataProvider.cs`, `SapHanaSqlBuilder.cs` | `**/SapHana*.cs` | |
| `PROV-CLICKHOUSE` | `Source/LinqToDB/DataProvider/ClickHouse/**` | `ClickHouseDataProvider.cs`, `ClickHouseSqlBuilder.cs` | `**/ClickHouse*.cs` | |
| `PROV-SQLCE` | `Source/LinqToDB/DataProvider/SqlCe/**` | `SqlCeDataProvider.cs`, `SqlCeSqlBuilder.cs` | `**/SqlCe*.cs` | SQL Server Compact Edition. |
| `PROV-YDB` | `Source/LinqToDB/DataProvider/Ydb/**` | (TBD on first read) | `**/Ydb*.cs` | Yandex Database (YDB). |
| `EFCORE` | `Source/LinqToDB.EntityFrameworkCore/**` | `LinqToDBForEFTools.cs`, `LinqToDBForEFExtensions.cs` | `**/*.cs` | EFCore companion (per-EF version). |
| `CLI` | `Source/LinqToDB.CLI/**` | (TBD on first read) | `**/*.cs` | Scaffolding CLI. |
| `COMPAT` | `Source/LinqToDB.Compat/**` | (TBD on first read) | `**/*.cs` | Back-compat shim package. |
| `EXTENSIONS-PKG` | `Source/LinqToDB.Extensions/**` | (TBD on first read) | `**/*.cs` | Extension-method package. |
| `FSHARP` | `Source/LinqToDB.FSharp/**` | (TBD on first read) | `**/*.fs`, `**/*.fsproj` | F# extensions. |
| `LINQPAD` | `Source/LinqToDB.LINQPad/**` | (TBD on first read) | `**/*.cs` | LINQPad driver. |
| `REMOTE` | `Source/LinqToDB.Remote.Grpc/**`, `Source/LinqToDB.Remote.HttpClient.Client/**`, `Source/LinqToDB.Remote.HttpClient.Server/**`, `Source/LinqToDB.Remote.SignalR.Client/**`, `Source/LinqToDB.Remote.SignalR.Server/**`, `Source/LinqToDB.Remote.Wcf/**` | (TBD on first read) | `**/*.cs` | All remote-transport companion packages (gRPC, HTTP, SignalR, WCF). |
| `SCAFFOLD` | `Source/LinqToDB.Scaffold/**` | (TBD on first read) | `**/*.cs` | Model scaffolding library. |
| `T4-TEMPLATES` | `Source/LinqToDB.Templates/**` | `LinqToDB.ttinclude`, `DataModel.ttinclude`, `LinqToDB.Tools.ttinclude` | `**/*.ttinclude` | T4 template includes shipped to consumers. |
| `TOOLS` | `Source/LinqToDB.Tools/**` | (TBD on first read) | `**/*.cs` | `LinqToDB.Tools` package (separate from `IN-TREE-TOOLS`). |
| `CODEGEN` | `Source/CodeGenerators/**` | (TBD on first read) | `**/*.cs` | Source-generator support project. |
| `SHARED-INTERNAL` | `Source/Shared/**` | `JetBrains.Annotations.cs`, `SharedAssemblyInfo.cs` | `**/*.cs` | Tiny shared assembly bits used across multiple projects. |
| `TESTS-INFRA` | `Tests/Base/**`, `Tests/Linq/Tests.csproj`, `Tests/Tests.Playground/**` | `TestBase.cs`, every `*Attribute.cs` under `Tests/Base/` | `**/*.cs` under `Tests/Base/` | Test harness + fixtures. |
| `TESTS-LINQ` | `Tests/Linq/**` (excluding csproj) | (none — Tier-2 only area) | `**/*.cs` under Tests/Linq | Per-feature linq tests. |
| `TESTS-EFCORE` | `Tests/EntityFrameworkCore/**` | (none) | `**/*.cs` | EFCore tests across EF3/8/9/10. |
| `TESTS-FSHARP` | `Tests/FSharp/**`, `Tests/EntityFrameworkCore.FSharp/**` | (none) | `**/*.fs` | F# tests for both core and EFCore. |
| `TESTS-T4` | `Tests/Tests.T4/**`, `Tests/Tests.T4.Nugets/**` | (none) | `**/*.cs`, `**/*.tt` | T4 template tests. |
| `TESTS-VB` | `Tests/VisualBasic/**` | (none) | `**/*.vb` | VB.NET tests. |
| `TESTS-MODEL` | `Tests/Model/**` | (TBD on first read) | `**/*.cs` | Test model project (shared POCOs / mappings). |
| `TESTS-BENCHMARKS` | `Tests/Tests.Benchmarks/**` | (none) | `**/*.cs` | BenchmarkDotNet harness. |
| `BUILD` | `Build/**`, `.github/workflows/**`, `.github/ISSUE_TEMPLATE/**`, `Directory.Build.props`, `global.json`, `linq2db.slnx` | `Directory.Build.props`, `global.json`, `linq2db.slnx`, `BannedSymbols.txt` | `Build/**/*.ps1`, `Build/**/*.sh`, `.github/workflows/*.yml` | CI + build infra. |
| `CLAUDE-INFRA` | `.claude/agents/**`, `.claude/docs/**`, `.claude/hooks/**`, `.claude/scripts/**`, `.claude/skills/**`, `CLAUDE.md` | `CLAUDE.md`, every `.claude/skills/*/SKILL.md`, every `.claude/agents/*.md` | `.claude/**/*.md`, `.claude/scripts/*.ps1` | Claude Code instruction corpus. Excludes `.claude/knowledge-base/` (KB output, not source). |
| `GLOBAL` | (n/a) | (n/a) | (n/a) | Cross-cutting docs only — `glossary.md`, `architecture/public-api.md`. |

Areas with `(TBD on first read)` get their Tier-1 file lists filled in by `kb-architect` during step 2/3 — the agent reads the area's directories, identifies anchor types (interfaces, base classes, public-surface entry points), and emits an `=== AUDIT-NOTE ===` proposing the Tier-1 list. The user updates this file accordingly.

## Excluded paths (no area)

These do not feed any area's scope:

- `bin/`, `obj/`, `.vs/`, `.idea/`, `TestResults/` — build outputs.
- `Source/Default/**` — historical default-symbol stubs (deprecated).
- `Source/LinqToDB.LegacySnapshot/**` — frozen pre-v6 snapshot if present (deprecated).
- `Source/Logo/**` — image assets, not code.
- `.claude/knowledge-base/**` — KB output, not source. The KB is generated by `/kb-build` and read by `kb-research`; it doesn't feed any indexer's scan scope.

## Adding a new area

1. Edit this file: insert a new row in the table.
2. Update `kb-coverage-tiers.md` only if the new area introduces a new Tier classification rule (uncommon).
3. Run `/kb-refresh` — it will pick up the new area and run any indexer that produces area-scoped artifacts (`kb-architect`, `kb-issue-detector`, `kb-github-curator` themes).
4. The next time `/kb-status` runs, the new area appears with empty / pending counts.

## Renaming an area

Renaming an area code is destructive — all `detected-issues/index.json` entries that reference the old code, and every `area:` frontmatter field across the KB, must be updated. Do not rename casually. If a rename is required, run a search-and-replace across `.claude/knowledge-base/` after editing this file, then run `/kb-refresh` to regenerate area-scoped roll-ups.
