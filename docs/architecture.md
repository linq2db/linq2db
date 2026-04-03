# LinqToDB Architecture

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
Setting `(this as IDataContext).CloseAfterUse = true` changes the behavior to open and close the connection per command, matching `DataContext` behavior.

DataContext
Alternative connection abstraction that opens and closes the physical connection per command.
Automatically enlists in ambient `TransactionScope` regardless of when the scope was created,
because each command opens a new connection that can enlist at that point.
Prefer `DataContext` when you need transparent `TransactionScope` support across multiple operations.
Note: for explicit transaction control with `BeginTransaction`, use `DataConnection` instead
(see anti-pattern #7 in `docs/agent-antipatterns.md`).

DataOptions
Configuration object used to construct connections.

ITable<T>
Typed table access.

MappingSchema
Mapping configuration and metadata.

Sql
Helper API for SQL constructs.

---

# Machine-Readable Documentation (AI-Tags)

Some XML documentation comments contain compact machine-readable metadata.

Format:

AI-Tags: Key=Value; Key2=Value2; ...

Keys are separated by semicolons; multiple values within a single key are comma-separated (e.g. `Affects=DdlStatement,Data`).

These tags describe:

* logical API grouping
* execution semantics
* composability
* SQL semantics affected
* provider behavior

AI-Tags are intended for tooling and AI agents.

They do not affect runtime behavior.

The specification for these tags is described in:

docs/ai-tags.md

---

# Additional Documentation

The following files are included in the NuGet package:

docs/ai-tags.md
Specification of AI-Tags format and semantics.

docs/architecture.md
This document.

docs/agent-antipatterns.md
Operational anti-patterns with code examples — common mistakes and how to avoid them.

docs/provider-capabilities.md
Provider capability matrix — which SQL features (MERGE, CTEs, window functions, OUTPUT/RETURNING, etc.) are supported per provider.

docs/translatable-methods.md
Reference list of standard .NET methods (String, Math, DateTime, Nullable, etc.) that LinqToDB translates to SQL, plus the Sql.* helper API.

docs/configuration.md
DataOptions configuration patterns and extensibility points: connection setup, tracing/logging, retry policies, interceptors, member translators.

These files are version-aligned with the NuGet package.

For the latest documentation, refer to the project repository.
