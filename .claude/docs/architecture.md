# Architecture

## Core Query Pipeline

The main flow for translating LINQ to SQL:

1. **LINQ Expressions** (`Source/LinqToDB/Linq/`) — Entry point. LINQ method calls are captured as expression trees. `Translation/` contains the member translator interfaces (`IMemberTranslator`, `ISqlExpressionTranslator`) that convert .NET method calls to SQL expressions.

2. **SQL AST** (`Source/LinqToDB/SqlQuery/`, `Source/LinqToDB/Internal/SqlQuery`) — Internal SQL representation. Contains `SqlDataType`, `SqlObjectName`, window/frame definitions, and the precedence system for SQL operator ordering.

3. **SQL Generation** (`Source/LinqToDB/Sql/`) — `Sql.ExpressionAttribute` and `Sql.FunctionAttribute` map C# methods to SQL expressions. Provider-specific overloads allow different SQL per database.

4. **Data Providers** (`Source/LinqToDB/DataProvider/`, `Source/LinqToDB/Internal/DataProvider`) — One subfolder per database (Access, ClickHouse, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SapHana, SqlCe, SqlServer, Sybase, Ydb). Each implements `IDataProvider` with dialect-specific SQL generation, type mapping, and bulk copy.

5. **Data Access** (`Source/LinqToDB/Data/`) — `DataConnection` (holds connection open until dispose) and `DataContext` (opens/closes per query). `BulkCopy*` classes, `CommandInfo`, retry policies.

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
- `Source/LinqToDB.CLI/` and `Source/LinqToDB.Scaffold/` — Database scaffolding CLI tool
- `Source/LinqToDB.Remote.*` — Remote context implementations (gRPC, SignalR, HttpClient, WCF)
