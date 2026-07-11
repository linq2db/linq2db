# LinqToDB Architecture

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - understand how LinqToDB translates LINQ to SQL
> - choose between `DataConnection` and `DataContext`
> - reason about whether a given LINQ expression will translate or must be rewritten
> - understand caching, pipeline stages, or execution model

This document describes the architectural model of LinqToDB and the mental model required to use it correctly.

LinqToDB is a deterministic LINQ-to-SQL translator designed for explicit, SQL-oriented data access.

---

# Core Identity

LinqToDB translates LINQ expression trees into SQL statements.

The same LINQ expression under the same configuration produces the same SQL intent (semantically equivalent SQL).

LinqToDB is not a traditional ORM and intentionally avoids hidden runtime behavior.

When writing LINQ queries for LinqToDB, developers should think in terms of SQL intent and query structure.

Every LINQ construct should correspond to a SQL construct.

If a LINQ expression has no clear SQL representation, it must be rewritten, explicitly mapped, or executed after materialization.

---

# Translation Pipeline

Query processing follows a deterministic translation pipeline:

Expression Tree → SQL AST → SQL text → Execution

Stages:

1. Expression Tree
   LINQ queries are captured as expression trees.

2. SQL AST
   Expression trees are analyzed and transformed into a SQL abstract syntax tree.

3. SQL Text
   The SQL AST is converted into provider-specific SQL text.

4. Execution
   Generated SQL is executed by the selected database provider.

Each stage is deterministic under the same configuration.

Different providers may emit different SQL text while preserving equivalent SQL semantics.

---

# Execution Model

LinqToDB follows standard LINQ execution semantics.

Queries may be:

Deferred execution
The query is translated and executed when enumerated.

Immediate execution
Certain methods execute SQL immediately (typically DML operations).

Execution behavior is explicit and configuration-driven.

---

# Query Model

The primary query abstractions are:

ITable<T>
Represents a mapped database table.

IQueryable<T>
Represents a composable query source.

LINQ queries operate on these abstractions and are translated into SQL by the LinqToDB query provider.

---

# Mapping Model

Mapping metadata describes how CLR types correspond to database structures.

Mapping may be defined using:

Attributes
Fluent mapping configuration
MappingSchema configuration

Mapping metadata determines:

* table names
* column names
* primary keys
* associations

---

# Provider Model

SQL generation is provider-defined.

Different database providers may implement different SQL dialects and capabilities.

Supported providers include (but are not limited to):

* SQL Server
* PostgreSQL
* Oracle
* SQLite
* MySQL
* SAP HANA

Provider-specific behavior must always be considered when generating SQL.

AI agents and developers should not assume uniform SQL capabilities across providers.

---

# What LinqToDB Does Not Provide Implicitly

LinqToDB intentionally avoids several behaviors commonly found in full ORMs.

The library does not provide implicitly:

* change tracking
* identity map
* hidden entity lifecycle management
* automatic client/server query splitting
* implicit navigation loading
* transparent client-side evaluation of non-translatable expressions

All data operations are explicit.

---

# Data Access Entry Points

Typical entry points for working with LinqToDB:

DataConnection
Primary database connection abstraction.
Opens the physical connection on the first command and holds it open until the instance is disposed.
Use this as the default choice: it is efficient for multiple sequential operations and explicit transaction control.
If you need per-command connection open/close behavior, use `DataContext` instead.

DataContext
Alternative connection abstraction that opens and closes the physical connection per command.
Automatically enlists in ambient `TransactionScope` regardless of when the scope was created,
because each command opens a new connection that can enlist at that point.
Prefer `DataContext` when you need transparent `TransactionScope` support across multiple operations.
Note: for explicit transaction control with `BeginTransaction`, use `DataConnection` instead
(see anti-pattern #7 in `docs/agent-antipatterns.md`).

Session-bound features
Features that depend on a stable physical connection - temp tables (`CreateTempTable`),
session variables, provider-level `SET` statements, and explicit transactions - require `DataConnection`.
`DataContext` opens and closes the connection per command; any session state created in one command
is gone before the next command executes.
Rule: if your code calls `CreateTempTable`, `BeginTransaction`, or relies on connection-scoped state,
use `DataConnection`.

DataOptions
Configuration object used to construct connections.

ITable<T>
Typed table access.

MappingSchema
Mapping configuration and metadata.

Sql
Helper API for SQL constructs.

> **For AI agents:** Markdown docs provide orientation and scenario rules. `docs/api.md` is the
> generated search index for public APIs. Raw XML-doc is the version-matched primary reference for
> exact signatures, overloads, remarks, usage rules, lifetime constraints, and thread-safety details
> when the generated extract is not detailed enough.

---

# Machine-Readable Documentation (AI-Tags)

Some XML documentation comments contain compact machine-readable metadata in custom XML-doc
elements.

Format:

```xml
<ai-tags group="Hints" hint-type="Query" execution="Deferred" composability="Composable" />
```

Generated docs render these attributes as `AI-Tags` metadata. Multiple values within a single key
are comma-separated (e.g. `affects="DdlStatement,Data"`).

These tags describe:

* logical API grouping
* execution semantics
* composability
* SQL semantics affected
* provider behavior

AI metadata is intended for tooling and AI agents.

They do not affect runtime behavior.

The specification for these tags is described in:

docs/ai-tags.md

---

# Additional Documentation

The NuGet package contains the following skill documents. Treat this list as package navigation,
not as a completeness claim for every linq2db feature.

| File | Purpose |
|---|---|
| `SKILL.md` | Canonical agent entry point and routing rules. |
| `docs/coverage.md` | Covered and not-yet-covered topics. |
| `docs/api.md` | Generated public API search/discovery index. |
| `docs/architecture.md` | This architecture overview. |
| `docs/agent-antipatterns.md` | Operational anti-patterns with code examples. |
| `docs/ai-tags.md` | `<ai-tags />` schema and generated AI metadata semantics. |
| `docs/configuration.md` | `DataOptions`, tracing/logging, retry policies, interceptors, member translators. |
| `docs/mapping.md` | Mapping attributes, fluent mapping, generated DDL metadata, value converters. |
| `docs/provider-setup.md` | Provider setup methods, `ProviderName` constants, driver packages. |
| `docs/provider-capabilities.md` | Provider SQL feature support matrix. |
| `docs/crud/crud.md` | CRUD routing index. |
| `docs/crud/*.md` | SELECT, INSERT, UPDATE, DELETE, UPSERT, MERGE, and bulk-copy guides. |
| `docs/query-cte.md` | CTE query composition. |
| `docs/query-temp-tables.md` | Temporary table workflows. |
| `docs/hints.md` | Hint API rules and fallback order. |
| `docs/hints-api-map.md` | Reverse lookup from SQL hint/directive text to typed provider helper. |
| `docs/custom-sql.md` | Custom SQL translation via `[Sql.Expression]`, `[Sql.Function]`, and `[ExpressionMethod]`. |
| `docs/translatable-methods.md` | Standard .NET methods and `Sql.*` helpers translated to SQL. |
| `docs/interceptors.md` | Interceptor extension points and safe usage. |

These files are version-aligned with the NuGet package.

For the latest documentation, refer to the project repository.
