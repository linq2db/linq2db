# Architecture

## Core Query Pipeline

The main flow for translating LINQ to SQL:

1. **LINQ Expressions** (`Source/LinqToDB/Linq/`) — Entry point. LINQ method calls are captured as expression trees. `Translation/` contains the member translator interfaces (`IMemberTranslator`, `ISqlExpressionTranslator`) that convert .NET method calls to SQL expressions.

2. **SQL AST** (`Source/LinqToDB/SqlQuery/`, `Source/LinqToDB/Internal/SqlQuery`) — Internal SQL representation. Contains `SqlDataType`, `SqlObjectName`, window/frame definitions, and the precedence system for SQL operator ordering.

3. **SQL Generation** (`Source/LinqToDB/Sql/`) — `Sql.ExpressionAttribute` and `Sql.FunctionAttribute` map C# methods to SQL expressions. Provider-specific overloads allow different SQL per database.

4. **Data Providers** (`Source/LinqToDB/DataProvider/`, `Source/LinqToDB/Internal/DataProvider`) — One subfolder per database (Access, ClickHouse, DB2, DuckDB, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SapHana, SqlCe, SqlServer, Sybase, Ydb). Each implements `IDataProvider` with dialect-specific SQL generation, type mapping, and bulk copy.

5. **Data Access** (`Source/LinqToDB/Data/`) — `DataConnection` (holds connection open until dispose), `BulkCopy*` classes, `CommandInfo`, retry policies. `DataContext` (opens/closes per query) lives one level up at `Source/LinqToDB/DataContext.cs`, not under `Data/`.

### Async DML extension methods build the *sync* expression

The async DML extensions (`UpdateAsync` / `InsertAsync` / `DeleteAsync` / … in `Source/LinqToDB/LinqExtensions/`) build their expression tree from the **synchronous** method infos — e.g. `UpdateAsync(predicate, setter)` (`LinqExtensions.Update.cs`) emits `Methods.LinqToDB.Update.UpdatePredicateSetter` (the sync info) and then calls `currentSource.ExecuteAsync<int>(expr, token)`. Async-ness is a property of the *execution*, not of the expression node. The `*Async` method infos (`UpdateSetterAsync` / `UpdatePredicateSetterAsync`, `Methods.cs`) are defined and exported in `PublicAPI.Shipped.txt` but are **not** used to build any expression. Consequence: a query-expression interceptor or translator that matches the sync `Update`/`Insert`/`Delete` infos already covers the async overloads — there is no separate async expression shape to handle. (This was a real reviewer trap on PR #5627: a finding claimed the async F# record-copy update path was unhandled, reasoning from the existence of the `*Async` infos without checking the call site.)

## Other Key Directories

- `Source/LinqToDB/Mapping/` — Attribute-based and fluent mapping configuration (`[Table]`, `[Column]`, `FluentMappingBuilder`)
- `Source/LinqToDB/Metadata/` — Metadata readers for mapping discovery
- `Source/LinqToDB/Interceptors/` — Extension points for connection/command lifecycle
- `Source/LinqToDB/Remote/` — Remote context for client/server scenarios
- `Source/LinqToDB/SchemaProvider/` — Database schema introspection
- `Source/LinqToDB/Concurrency/` — Optimistic concurrency support
- `Source/LinqToDB/LinqExtensions/` — Extension methods for LINQ operations (joins, merge, CTE, etc.)
- `Source/Shared/` — Polyfills for missing runtime APIs across TFMs (auto-included by all projects)
- `Source/CodeGenerators/` — Internal Roslyn source generators

## Companion Projects

- `Source/LinqToDB.EntityFrameworkCore/` — EF Core integration
- `Source/LinqToDB.Extensions/` — DI and logging extensions
- `Source/LinqToDB.FSharp/` — F# support
- `Source/LinqToDB.Analyzers/` and `Source/LinqToDB.Analyzers.CodeFixes/` — shipping Roslyn analyzer package (e.g. LINQ2DB1001, legacy `Sql.Ext` window API → `Sql.Window`)
- `Source/LinqToDB.Tools/` — developer tooling (query metrics, mapping validation)
- `Source/LinqToDB.Compat/` — compatibility shims
- `Source/LinqToDB.LINQPad/` — LINQPad driver
- `Source/LinqToDB.Templates/` — T4 templates
- `Source/LinqToDB.CLI/` and `Source/LinqToDB.Scaffold/` — Database scaffolding CLI tool
- `Source/LinqToDB.Remote.*` — Remote context implementations (gRPC, SignalR, HttpClient, WCF)
