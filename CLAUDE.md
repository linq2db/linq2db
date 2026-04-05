# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is linq2db

LINQ to DB is a lightweight, type-safe ORM and LINQ provider for .NET. It translates LINQ expressions into SQL — no change tracking, no heavy abstraction. Think "one step above Dapper" with compiler-checked queries.

## Build Commands

```bash
# Build the full solution
dotnet build linq2db.slnx

# Build just the core library
dotnet build Source/LinqToDB/LinqToDB.csproj

# Build in Release (enables Roslyn analyzers and banned API checks)
dotnet build linq2db.slnx -c Release

# Quick single-TFM build for development (fastest iteration)
dotnet build linq2db.slnx -c Testing
# Testing config targets only net10.0 with DEBUG defines
```

Batch scripts at repo root: `Build.cmd` (full), `Compile.cmd` (library only), `Clean.cmd`, `Test.cmd`.

## Running Tests

Tests use **NUnit3** with **Shouldly** assertions (not NUnit Assert). Test targets: `net462`, `net8.0`, `net9.0`, `net10.0`. **Always use Debug configuration for tests** — Release enables Roslyn analyzers and is much slower to build.

```bash
# Run all tests (Debug, all TFMs, HTML report)
./Test.cmd

# Run specific TFM only (e.g. net9.0 Debug with trx output)
# Format: test.cmd <Config> <net462:0|1> <net8:0|1> <net9:0|1> <net10:0|1> <logger>
./Test.cmd Debug 0 0 1 0 trx

# Run a single test class or method via dotnet test
dotnet test Tests/Linq/Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net9.0

# Run tests with the lightweight playground solution (faster load)
dotnet test linq2db.playground.slnf
```

### Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json`
2. This gives you SQLite-based testing out of the box
3. Add connection strings for other databases as needed

**Important**: `_CreateData.*` tests must run first — they create and populate test databases. If running a single test, ensure `_CreateData` has run. If your test modifies data, revert changes to avoid side effects.

### Test Patterns

```csharp
[TestFixture]
public class MyTest : TestBase
{
    [Test]
    public void TestSomething([DataSources] string context)
    {
        using var db = GetDataContext(context);
        // AssertQuery runs query against both DB and in-memory collections
        // and verifies matching results
        AssertQuery(db.Person.Where(_ => _.Name == "John"));
    }
}
```

- `[DataSources]` — runs test for each enabled database provider
- `TestBase` — base class providing `GetDataContext()`, `AssertQuery()`, etc.
- Tests live in `Tests/Linq/` (main tests), `Tests/Base/` (test framework), `Tests/Model/` (model classes)

### Debugging linq2db translators

When debugging query translation or provider issues,
use `Console.WriteLine` to output intermediate values or SQL fragments.
Do not introduce logging dependencies.

## Solution Structure

| Solution | Purpose |
|---|---|
| `linq2db.slnx` | Full solution |
| `linq2db.playground.slnf` | Lightweight — for working on specific tests without full load |
| `linq2db.Benchmarks.slnf` | Benchmarks only |

## Architecture

### Core Query Pipeline

The main flow for translating LINQ to SQL:

1. **LINQ Expressions** (`Source/LinqToDB/Linq/`) — Entry point. LINQ method calls are captured as expression trees. `Translation/` contains the member translator interfaces (`IMemberTranslator`, `ISqlExpressionTranslator`) that convert .NET method calls to SQL expressions.

2. **SQL AST** (`Source/LinqToDB/SqlQuery/`) — Internal SQL representation. Contains `SqlDataType`, `SqlObjectName`, window/frame definitions, and the precedence system for SQL operator ordering.

3. **SQL Generation** (`Source/LinqToDB/Sql/`) — `Sql.ExpressionAttribute` and `Sql.FunctionAttribute` map C# methods to SQL expressions. Provider-specific overloads allow different SQL per database.

4. **Data Providers** (`Source/LinqToDB/DataProvider/`) — One subfolder per database (Access, ClickHouse, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SapHana, SqlCe, SqlServer, Sybase, Ydb). Each implements `IDataProvider` with dialect-specific SQL generation, type mapping, and bulk copy.

5. **Data Access** (`Source/LinqToDB/Data/`) — `DataConnection` (holds connection open until dispose) and `DataContext` (opens/closes per query). `BulkCopy*` classes, `CommandInfo`, retry policies.

### Other Key Directories

- `Source/LinqToDB/Mapping/` — Attribute-based and fluent mapping configuration (`[Table]`, `[Column]`, `FluentMappingBuilder`)
- `Source/LinqToDB/Metadata/` — Metadata readers for mapping discovery
- `Source/LinqToDB/Interceptors/` — Extension points for connection/command lifecycle
- `Source/LinqToDB/Remote/` — Remote context for client/server scenarios
- `Source/LinqToDB/SchemaProvider/` — Database schema introspection
- `Source/LinqToDB/Concurrency/` — Optimistic concurrency support
- `Source/LinqToDB/LinqExtensions/` — Extension methods for LINQ operations (joins, merge, CTE, etc.)
- `Source/Shared/` — Polyfills for missing runtime APIs across TFMs (auto-included by all projects)
- `Source/CodeGenerators/` — Internal Roslyn source generators

### Companion Projects

- `Source/LinqToDB.EntityFrameworkCore/` — EF Core integration
- `Source/LinqToDB.Extensions/` — DI and logging extensions
- `Source/LinqToDB.FSharp/` — F# support
- `Source/LinqToDB.CLI/` and `Source/LinqToDB.Scaffold/` — Database scaffolding CLI tool
- `Source/LinqToDB.Remote.*` — Remote context implementations (gRPC, SignalR, HttpClient, WCF)

## Code Conventions

- **Indentation**: Tabs (not spaces) for C#/VB. Spaces for F#, YAML, shell scripts, markdown.
- **C# version**: 14 (`LangVersion` in Directory.Build.props)
- **Nullable**: Enabled globally
- **Warnings as errors**: `TreatWarningsAsErrors` is true. No compilation warnings allowed.
- **Analyzers**: Run during Release builds only (`RunAnalyzersDuringBuild`). Banned API list in `Build/BannedSymbols.txt`.
- **XML documentation**: Required on new public classes, properties, and methods.
- **Target frameworks**: `net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. Use conditional compilation (`#if`) for TFM-specific code. Feature flags like `SUPPORTS_COMPOSITE_FORMAT`, `SUPPORTS_SPAN`, `ADO_ASYNC` are defined in `Directory.Build.props`.
- **Code style**: Match existing code. The project uses column-aligned formatting intentionally — do not "fix" alignment spacing.
- **.NET SDK**: 10.0 (see `global.json`)

## Branch Conventions

- `master` — main development branch
- `release` — latest released version
- Bugfix branches: `issue/<issue_id>`
- Feature branches: `feature/<issue_id_or_feature_name>`

## Review Guidelines (from Copilot instructions)

- Ignore minor/intentional formatting differences (column alignment, qualified type spacing)
- Only flag formatting when clearly broken (3+ blank lines, mixed tabs/spaces causing misalignment, broken indentation)
- Use Shouldly for test assertions, not NUnit Assert
