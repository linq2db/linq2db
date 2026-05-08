---
area: GLOBAL
kind: glossary
sources: [code, kb]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Glossary

Domain terms used across the linq2db knowledge base. Each term lists files where the term is defined / discussed; readers can navigate via the See-also links inside.

## Terms

### ADO {#ado}

ActiveX Data Objects — .NET data-access API (`System.Data.IDbConnection`, `IDbCommand`). linq2db wraps ADO via `IDataProvider` + `DataConnection`. See [areas/DATA/INDEX.md](areas/DATA/INDEX.md).

### AST {#ast}

Abstract Syntax Tree. The intermediate SQL representation under `LinqToDB.Internal.SqlQuery`. Built by `ExpressionBuilder`, consumed by `SqlBuilder` to emit provider-specific SQL.

### BasicSqlBuilder {#basicsqlbuilder}

Provider-agnostic SQL emitter (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs`). Each provider extends it with override methods for dialect-specific syntax. See [areas/SQL-PROVIDER/INDEX.md](areas/SQL-PROVIDER/INDEX.md).

### BasicSqlOptimizer {#basicsqloptimizer}

AST optimizer baseline (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs`). Folds constants, eliminates dead conditions, normalizes nullability. Per-provider subclasses add dialect-specific rewrites.

### BLToolkit {#bltoolkit}

Predecessor library that linq2db forked from in 2011. Most BLToolkit-era classes were removed during the 2012-2013 refactor wave.

### BulkCopy {#bulkcopy}

Provider-native bulk-insert API. Each provider exposes `IDataProvider.BulkCopyAsync(...)` wrapping ADO`.SqlBulkCopy` (SqlServer), `NpgsqlBinaryImporter` (PostgreSQL), etc. See `Source/LinqToDB/Internal/DataProvider/<Provider>/<Provider>BulkCopy.cs`.

### capability flag {#capability-flag}

Boolean field on [SqlProviderFlags](#sqlproviderflags) advertising whether a provider supports a SQL feature (CROSS APPLY, recursive CTE, `OUTPUT` clause, etc.). Builders/optimizers gate emission on these.

### CTE {#cte}

Common Table Expression — SQL `WITH` clause syntax for naming subqueries. linq2db supports CTEs via `IQueryable<T>.AsCte()` and recursive CTEs. See [areas/EXPR-TRANS/INDEX.md](areas/EXPR-TRANS/INDEX.md).

### DataConnection {#dataconnection}

Concrete `IDataContext` (`Source/LinqToDB/Data/DataConnection.cs`) — owns the live ADO `DbConnection`, command execution, retry/transaction handling. See [areas/DATA/INDEX.md](areas/DATA/INDEX.md).

### DataContext {#datacontext}

Higher-level `IDataContext` (`Source/LinqToDB/DataContext.cs`) — defers connection opening, used as the default LINQ entry point. Wraps `DataConnection` lazily.

### DataOptions {#dataoptions}

Builder-style options record (`Source/LinqToDB/DataOptions.cs`) — provider, connection string, mapping schema, retry policy, etc. Replaced legacy mutable `LinqToDBSection` config. See [history/decisions/2023-data-options-refactor.md](history/decisions/2023-data-options-refactor.md).

### DataProvider {#dataprovider}

See [IDataProvider](#idataprovider). Refers to the type or its concrete impls.

### DataType {#datatype}

linq2db type enum (`Source/LinqToDB/DataType.cs`) — abstract DB type independent of provider (e.g. `DataType.Decimal`, `DataType.NVarChar`, `DataType.Json`). Mapped to provider-specific types per `MappingSchema`.

### DbManager {#dbmanager}

Legacy BLToolkit-era data-access class. Removed in 2013 (see [history/decisions/2013-remove-dbmanager.md](history/decisions/2013-remove-dbmanager.md)). Modern equivalent: [DataConnection](#dataconnection) + [DataContext](#datacontext).

### DI {#di}

In KB context: Dependency Injection. In `detected-issues/`: prefix `DI-NNNN` for detected-issue IDs (unrelated). See [docs/kb-issue-categories.md](../../docs/kb-issue-categories.md).

### EF Core {#ef-core}

Entity Framework Core — Microsoft's ORM. linq2db integrates via `LinqToDB.EntityFrameworkCore` package, exposing `IQueryable.ToLinqToDB()`. See [areas/EFCORE/INDEX.md](areas/EFCORE/INDEX.md).

### EFCore {#efcore}

See [EF Core](#ef-core).

### expression tree {#expression-tree}

C# `Expression<TDelegate>` representation of source code. linq2db captures the LINQ query as an expression tree, traverses it via `ExpressionBuilder`, and emits SQL.

### ExpressionBuilder {#expressionbuilder}

LINQ-to-SQL translation root (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs`). Walks the C# expression tree and emits an `SqlStatement`. See [areas/EXPR-TRANS/INDEX.md](areas/EXPR-TRANS/INDEX.md).

### fluent mapping {#fluent-mapping}

Type-mapping configured imperatively via `FluentMappingBuilder` (vs `[Table]`/`[Column]` attributes). Applied to `MappingSchema`. See [conventions/legacy-patterns.md](conventions/legacy-patterns.md) for migration notes.

### IDataContext {#idatacontext}

Public abstraction over a database connection (`Source/LinqToDB/IDataContext.cs`). Two implementations: `DataConnection` (eager) and `DataContext` (lazy). Public-API surface — see [conventions/public-api-discipline.md](conventions/public-api-discipline.md).

### IDataProvider {#idataprovider}

Top-level provider interface (`Source/LinqToDB/DataProvider/IDataProvider.cs`). Owns `SqlBuilder`, `SqlOptimizer`, mapping schema, retry policy. One concrete impl per RDBMS family (`PostgreSQLDataProvider`, `OracleDataProvider`, etc.). See [areas/CORE/INDEX.md](areas/CORE/INDEX.md).

### INSERT WITH OUTPUT {#insert-with-output}

INSERT statement returning generated values (PostgreSQL `RETURNING`, SQL Server `OUTPUT`, Oracle `RETURNING INTO`). API: `IQueryable<T>.InsertWithOutput(...)`. See [history/decisions/2020-insert-with-output.md](history/decisions/2020-insert-with-output.md).

### Interceptor {#interceptor}

Cross-cutting hook (`IInterceptor` family in `Source/LinqToDB/Interceptors/`). Subscribed via `DataOptions.UseInterceptor()`. Notable: `IConnectionInterceptor`, `ICommandInterceptor`, `IExceptionInterceptor`, `IEntityServiceInterceptor`. See [history/decisions/2021-interceptor-infrastructure.md](history/decisions/2021-interceptor-infrastructure.md).

### LINQ {#linq}

.NET Language Integrated Query — the C# expression-tree based query language. linq2db translates LINQ expressions into SQL.

### MappingSchema {#mappingschema}

Per-context type-to-table-and-column registry (`Source/LinqToDB/Mapping/MappingSchema.cs`). Holds entity descriptors, fluent overrides, value converters, scalar types. Locked after first query in linq2db 6+. See [history/decisions/2022-lock-mappingschema.md](history/decisions/2022-lock-mappingschema.md).

### MERGE {#merge}

SQL Server-style MERGE statement. linq2db builds it via `IQueryable<T>.Merge().On(...).InsertWhenNotMatched()...`. See [history/decisions/2017-new-merge-api.md](history/decisions/2017-new-merge-api.md) and [history/decisions/2018-merge-api-to-query.md](history/decisions/2018-merge-api-to-query.md).

### MethodCallBuilder {#methodcallbuilder}

Per-method dispatcher in `ExpressionBuilder` (`Source/LinqToDB/Internal/Linq/Builder/MethodCallBuilder.cs`). Routes `Where`, `Select`, `Join`, etc. via `[BuildsMethodCall]` markers + `BuildersGenerator` source generator.

### Nemerle {#nemerle}

.NET language used for an early parallel implementation of the SQL builder layer (`Source/SqlBuilder/SqlExpr.n`). Removed in 2012 — see [history/decisions/2012-remove-nemerle.md](history/decisions/2012-remove-nemerle.md).

### NRT {#nrt}

Nullable Reference Types — C# 8+ language feature. linq2db has `<Nullable>enable</Nullable>` solution-wide via `Directory.Build.props`. See [conventions/nullable-handling.md](conventions/nullable-handling.md).

### NullabilityContext {#nullabilitycontext}

Per-query nullability tracker (`Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs`). Distinguishes C# nullable refs from SQL NULL semantics; resolves outer-join column nullability. See [conventions/nullable-handling.md](conventions/nullable-handling.md).

### ORM {#orm}

Object-Relational Mapper. linq2db is a lightweight, type-safe ORM and LINQ provider for .NET — translates LINQ to SQL without change tracking or heavy abstraction.

### ProviderName {#providername}

Static-string registry of provider identifiers (`Source/LinqToDB/ProviderName.cs`). Examples: `ProviderName.SqlServer2017`, `ProviderName.PostgreSQL15`. Used for capability lookups via `IDataProvider.Name`.

### remote context {#remote-context}

Client-only `IDataContext` that ships LINQ over the wire to a remote linq2db server (HTTP/gRPC). See [areas/REMOTE-CLIENT/INDEX.md](areas/REMOTE-CLIENT/INDEX.md).

### Scaffold {#scaffold}

Code-generation tool that reads a database schema and emits POCO classes + `IDataContext` setup. Modern impl is `Source/LinqToDB.CLI` + `Source/LinqToDB.Scaffold`. See [areas/SCAFFOLD/INDEX.md](areas/SCAFFOLD/INDEX.md).

### SelectQuery {#selectquery}

AST node for a SELECT (`Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs`). Contains `Select`, `From`, `Where`, `GroupBy`, `OrderBy` clauses plus `Take`/`Skip`. See [areas/SQL-AST/INDEX.md](areas/SQL-AST/INDEX.md).

### SQL AST {#sql-ast}

See [AST](#ast). Specifically the SQL-flavored AST used inside linq2db (`SqlStatement`, `SelectQuery`, `SqlExpression`, etc.). The canonical namespace is `LinqToDB.Internal.SqlQuery`. See [areas/SQL-AST/INDEX.md](areas/SQL-AST/INDEX.md).

### SqlBuilder {#sqlbuilder}

See [BasicSqlBuilder](#basicsqlbuilder). Generic name for the SQL emission layer; per-provider classes (`SqlServerSqlBuilder`, `PostgreSQLSqlBuilder`, etc.) extend the base.

### SqlExpression {#sqlexpression}

AST leaf node for inline SQL fragments (`Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs`). Used by `Sql.Property<T>`, `Sql.Expression(...)` extension surface.

### SqlOptimizer {#sqloptimizer}

See [BasicSqlOptimizer](#basicsqloptimizer).

### SqlProviderFlags {#sqlproviderflags}

Capability bitfield (`Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs`). Each provider sets which features it supports (e.g. `IsCommonTableExpressionsSupported`, `IsCrossApplySupported`). Used by `BasicSqlBuilder` / `BasicSqlOptimizer` to gate dialect-specific code paths.

### SqlStatement {#sqlstatement}

Top-level AST node (`Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs`). Subtypes: `SqlSelectStatement`, `SqlInsertStatement`, `SqlUpdateStatement`, `SqlMergeStatement`, etc.

### T4 {#t4}

Text Template Transformation Toolkit. Code-generation system used in the legacy scaffolding pipeline (.tt → .cs). Replaced by the in-tree `Source/LinqToDB.CLI` scaffold tool. See [areas/T4-TEMPLATES/INDEX.md](areas/T4-TEMPLATES/INDEX.md).

### TFM {#tfm}

Target Framework Moniker (e.g. `net8.0`, `net462`, `netstandard2.0`). linq2db targets `net462`, `netstandard2.0`, `net8.0`-`net10.0`. Feature-flag macros (`SUPPORTS_SPAN`, `ADO_ASYNC`) gate per-TFM behavior.

### tier-1 file {#tier-1-file}

Required-read file for an area (per [docs/kb-coverage-tiers.md](../../docs/kb-coverage-tiers.md)). Listed in `kb-areas.md` per area. Must be 100% covered for area `confidence: high`.

### tier-2 file {#tier-2-file}

Sampled-read file for an area. ≥90% coverage gate; below that, files go on the deferred-coverage queue and are drained via `/kb-refresh --source coverage`.

### TypeMapper {#typemapper}

Reflection-driven type accessor registry (`Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs`). Maps unmanaged provider-specific types (e.g. `NpgsqlConnection`) to abstract surfaces.

## Excluded from glossary (by convention)

Terms that appear in >=3 KB files but do not warrant a glossary entry, with rationale:

- **Provider product names** (PostgreSQL, MySQL, Oracle, SqlServer, ClickHouse, DB2, Firebird, Informix, SapHana, SQLite, SqlCe, Sybase, Ydb): these are external products. See `areas/PROV-<name>/INDEX.md` for linq2db-specific provider-layer docs.
- **Area codes** (CORE, EXPR-TRANS, SQL-AST, SCAFFOLD, etc.): defined in [docs/kb-areas.md](../../docs/kb-areas.md).
- **Generic .NET types** (`DateTime`, `DbConnection`, `Task`, etc.): documented by Microsoft, not linq2db-specific.
- **Author handles** (e.g. github usernames): not domain terms.
- **SQL keywords** (`SELECT`, `INSERT`, `UPDATE`, `FROM`, `BY`, `IN`, `NOT`, `NULL`, `CAST`, `OFFSET`): standard SQL.

## See also

- [docs/kb-areas.md](../../docs/kb-areas.md) — area registry
- [conventions/](conventions/) — coding + architectural conventions
- [history/decisions/](history/decisions/) — recorded decisions referenced inline above
