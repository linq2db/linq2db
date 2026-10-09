---
name: linq2db
description: Usage guide for LINQ to DB (linq2db), the .NET LINQ-to-SQL data access library; it ships inside the linq2db NuGet package and matches that package version. Use when writing, reviewing or debugging C# code that uses linq2db or the LinqToDB namespaces - DataConnection, DataContext, DataOptions and provider setup (UseSqlServer, UsePostgreSQL, UseSQLite and others), entity mapping (Table and Column attributes, fluent mapping, MappingSchema), ITable<T> LINQ queries, associations and LoadWith, Insert, Update, Delete, InsertOrReplace, Merge, BulkCopy, temporary tables, CTEs, query and table hints, raw SQL, interceptors, and exact API lookup in the package XML documentation.
---

# linq2db Skill <!-- omit in toc -->

This skill helps write correct code against LINQ to DB (`linq2db` package, `LinqToDB.*`
namespaces). It covers the core `linq2db` package of the version it ships with:

- `SKILL.md` (this file) - general rules and the guide index;
- `docs/*.md` - task guides; open the ones the task needs (see [Guide index](#guide-index));
- `linq2db.xml` - the package's XML documentation, the exact reference for public API names,
  signatures and remarks (see [Exact API lookup](#exact-api-lookup-linq2dbxml)).

Navigation:
- [Exact API lookup (`linq2db.xml`)](#exact-api-lookup-linq2dbxml)
- [When adding LinqToDB to a project](#when-adding-linqtodb-to-a-project)
- [When writing queries and DML](#when-writing-queries-and-dml)
- [Guide index](#guide-index)
- [Quick violation reference](#quick-violation-reference)

---

## Package scope

This skill covers the core `linq2db` package. It does not provide dedicated task guides for
extension packages such as `LinqToDB.EntityFrameworkCore`, `LinqToDB.AspNet`,
`LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `LinqToDB.CLI`, or `LinqToDB.FSharp`.

For extension-package questions, use package-local docs/XML-doc from that package when available.
If this skill is the only available documentation, state that the package-specific guidance is not
covered here and separate best-effort extension-package guidance from package-confirmed core
`linq2db` facts.

---

## Exact API lookup (`linq2db.xml`)

`linq2db.xml` is the XML documentation file of the `linq2db` assembly. It has the summary,
parameters, return value and remarks of every documented public member, for exactly this package
version. It lies next to the assembly in the restored NuGet package:

```text
<global-packages>/linq2db/<version>/lib/<tfm>/linq2db.xml
```

- `<global-packages>` - printed by `dotnet nuget locals global-packages --list`; by default
  `~/.nuget/packages` on Linux and macOS, `%UserProfile%\.nuget\packages` on Windows
  (the `NUGET_PACKAGES` environment variable overrides it).
- `<version>` - the `linq2db` version the project references (its `PackageReference`, or the
  resolved version in the project's `project.assets.json`).
- `<tfm>` - the folder closest to the project's target framework: `net10.0`, `net9.0`, `net8.0`,
  `netstandard2.0` or `net462`.

When this skill is read from the package itself rather than from a copy in the project, the same
file is `../../lib/<tfm>/linq2db.xml` relative to this file.

Search it by member, type, provider name or SQL keyword; do not read it sequentially. Member ids
have the form `M:Namespace.Type.Member(...)` (`T:` for types, `P:` for properties, `F:` for
fields); generic types and methods carry a backtick arity suffix (``ITable`1``, ``TableHint``1``),
so search by member name rather than by full signature to find every overload. Members without
XML comments are absent from the file, so a missing entry does not prove that an API is absent.

---

## When adding LinqToDB to a project

These steps apply once per integration, not to every query or change.

### 1 - Add the provider's ADO.NET driver

`linq2db` does not bundle database drivers. A project that references only `linq2db` compiles
but fails at runtime when it opens a connection.

Every provider needs a separate ADO.NET driver NuGet package:

| Provider | `DataOptions` method | Required NuGet package |
|---|---|---|
| SQL Server | `UseSqlServer(...)` | `Microsoft.Data.SqlClient` |
| SQLite | `UseSQLite(...)` | `Microsoft.Data.Sqlite` |
| PostgreSQL | `UsePostgreSQL(...)` | `Npgsql` |
| MySQL / MariaDB | `UseMySql(...)` | `MySqlConnector` |
| Oracle | `UseOracle(...)` | `Oracle.ManagedDataAccess.Core` |
| ClickHouse | `UseClickHouse(...)` | `ClickHouse.Driver` |
| DB2 | `UseDB2(...)` | `Net.IBM.Data.Db2` |
| DuckDB | `UseDuckDB(...)` | `DuckDB.NET.Data.Full` |
| Firebird | `UseFirebird(...)` | `FirebirdSql.Data.FirebirdClient` |
| Sybase / SAP ASE | `UseAse(...)` | `AdoNetCore.AseClient` |

See [`docs/provider-setup.md`](docs/provider-setup.md) for the complete list including version
requirements and dialect options. Check the project file for the driver package and add it if it
is missing.

### 2 - Apply core configuration rules

Rules that are easy to miss:

- `DataOptions` - create once (`static readonly` or a singleton) and pass it to every
  `DataConnection`; it is immutable, and building it does initialization work.
- `MappingSchema` - create one only when custom mapping is needed; then create it once at startup
  and attach it with `.UseMappingSchema(...)`. A new schema per connection destroys its caches.
- `DataConnection` - create per unit of work (scoped) and dispose after use.
- Temp tables, explicit transactions, session state - need a connection kept open across commands:
  prefer `DataConnection` (a `DataContext` keeps temp tables and session state only with
  `SetKeepConnectionAlive(true)`).
- Entity columns used with any API or option that generates `CREATE TABLE` - specify `Length`,
  `Precision` and `Scale` explicitly for provider-sensitive types (`string`, `decimal`, etc.).
  When the task gives no limits, choose a bounded value from the field's meaning and flag it for
  review (a neutral `// TODO: confirm max length` or a note to the user); see
  [`docs/mapping.md`](docs/mapping.md#5-ddl-sensitive-column-metadata).

XML-doc remarks on `DataOptions`, `DataConnection`, `DataContext`, `MappingSchema` and the provider
`UseXxx` methods carry further lifetime and caching details.

---

## When writing queries and DML

Each guide begins with a "You are here if" block that says what it covers.

### Namespaces

Every file that uses LinqToDB needs at minimum:

```csharp
using LinqToDB;
using LinqToDB.Data;
```

Async query and DML extension methods (`ToListAsync`, `FirstOrDefaultAsync`, `SingleAsync`,
`MaxAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `MergeAsync`, etc.) are in a separate
namespace:

```csharp
using LinqToDB.Async;
```

If the compiler does not find an async method, the missing `using LinqToDB.Async` is the most
common cause.

### Verify APIs before using them

The guides and `linq2db.xml` describe this package version. Prefer them over online docs or memory
of other versions, which may not match. Do not invent APIs, overloads, options, provider flags or
provider capabilities, and do not assume an API is missing only because no guide mentions it.

Do not use `LinqToDB.Internal.*` APIs in application code. They are implementation details even
when visible as public members in XML documentation.

Outside knowledge is fine for whatever is not specific to LinqToDB (database tuning, SQL concepts,
.NET/C# behavior, the user's domain); this skill gives no advice on those. For the LinqToDB side -
API names and signatures, namespaces, receiver types, provider-specific helpers, mapping and query
composition rules, connection lifetime - ground the code in the guides or `linq2db.xml`. If no
LinqToDB API path is found, say so before discussing fallbacks.

For an API-level question (provider-specific APIs, hints, SQL extensions, configuration, DML and
query extensions):

1. Read the relevant guide for concepts, boundaries and common mistakes.
2. Start with the narrowest API surface: provider-specific guides, maps, namespaces, typed helpers.
3. Search `linq2db.xml` by likely member names, type names, provider names and SQL keywords, and
   confirm candidates by their summaries, parameters and remarks.
4. Prefer typed or provider-specific APIs over generic string-based ones (`QueryHint`,
   `TableHint`, `Sql.Expression`, raw SQL); use those when no typed API covers the case.
5. Use custom SQL, raw SQL and interceptors only when typed and generic APIs do not cover the case.
6. If a guide and `linq2db.xml` disagree, follow `linq2db.xml` for the exact API shape and mention
   the discrepancy.

When a guide lists several implementation paths, the order is meaningful: the most specific path
comes first, and generic APIs, custom SQL, raw SQL and interceptors are fallbacks unless the guide
says otherwise.

### SQL hints

Hints have the most typed, provider-specific API surface. The lookup order, with details in
[`docs/hints.md`](docs/hints.md#hint-lookup-order):

1. Search [`docs/hints-api-map.md`](docs/hints-api-map.md) by provider name, SQL hint text and
   likely helper-name fragments; verify a hit in `linq2db.xml`.
2. Typed provider helpers are not available on plain `ITable<T>` or `IQueryable<T>`: call the
   provider marker first (`AsSqlServer()`, `AsOracle()`, `AsClickHouse()`, etc.).
3. For a table hint, also look for the tables-in-scope form (`<Base>InScopeHint` /
   `With<Base>InScope`) and pick by whether the hint should affect one table or every table in the
   query scope. Apply a scope helper to the query that already contains the tables; it does not
   reach tables joined through association properties
   ([#4321](https://github.com/linq2db/linq2db/issues/4321)).
4. Do not build helper names by string concatenation; use only names found in the map or XML-doc.
5. Generic hint APIs (`QueryHint`, `TableHint`, `TablesInScopeHint`) come after the typed lookup
   finds nothing; `Sql.Expression`, raw SQL and interceptors come last.

When answering a provider-specific hint question, name the provider marker, the typed helper and
its receiver before showing code; if none was found, say that the map and XML-doc lookup found
none before showing a fallback.

### Temporary tables

Read [`docs/query-temp-tables.md`](docs/query-temp-tables.md). Prior ORM knowledge tends to pick a
lower-level pattern than the current API. Choose the `CreateTempTable*` overload by source shape:

1. Rows already in C# memory -> `CreateTempTable(items)` / `CreateTempTableAsync(items)`.
2. Rows from an `IQueryable<T>` -> `CreateTempTable(query)` / `CreateTempTableAsync(query)`;
   LinqToDB fills the table with server-side `INSERT ... SELECT`.
3. Empty table first -> `CreateTempTable<T>()` only when rows are not available at creation time,
   load timing must be separated, or explicit post-create work is required.
4. Anonymous-type projection -> specify a table name; use the `setTable` fluent mapping parameter
   when anonymous `string` or `decimal` columns need length/precision metadata.

`TempTable<T>` drops the backing table on dispose (`using` / `await using`).

---

## Guide index

Open a guide when the task touches its topic.

| File | When to read |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | Translation pipeline, entry points, `DataConnection` vs `DataContext`, why an expression does or does not translate |
| [`docs/agent-antipatterns.md`](docs/agent-antipatterns.md) | Diagnosing unexpected behavior (symptom index at the top) or reviewing code: common mistakes with wrong/correct examples |
| [`docs/mapping.md`](docs/mapping.md) | Entity mapping, `MappingSchema`, attributes/fluent mapping, schema/DDL-sensitive columns |
| [`docs/associations.md`](docs/associations.md) | Relationship metadata and eager loading - `[Association]`, fluent `.Association(...)`, `LoadWith`, `ThenLoad`, eager-loading strategies; there is no lazy loading |
| [`docs/crud/crud.md`](docs/crud/crud.md) | All CRUD operations - SELECT, INSERT, UPDATE, DELETE, upsert, bulk copy, MERGE; routes to `docs/crud/<operation>.md` |
| [`docs/concurrency.md`](docs/concurrency.md) | Optimistic concurrency for entity update/delete - `UpdateOptimistic`, `DeleteOptimistic`, `WhereKeyOptimistic`, `OptimisticLockPropertyAttribute` |
| [`docs/query-cte.md`](docs/query-cte.md) | CTEs, recursive queries - when `.AsCte()` or `db.GetCte<T>()` is needed |
| [`docs/query-joins.md`](docs/query-joins.md) | Fluent `InnerJoin`/`LeftJoin`/`RightJoin`/`FullJoin`/`CrossJoin`, association-driven joins, join translation failures, `RightJoin`/`FullJoin`/`APPLY` provider limitations |
| [`docs/query-temp-tables.md`](docs/query-temp-tables.md) | Temporary tables - `TempTable<T>`, `CreateTempTable`, `TableOptions`; session-scoped tables need a kept-open connection |
| [`docs/null-semantics.md`](docs/null-semantics.md) | Why generated SQL for a null comparison looks more complex than expected - `CompareNulls`, `Sql.AsNotNull`, `IsDistinctFrom`, `Sql.ToNullable`/`Sql.AsNullable` |
| [`docs/parameters.md`](docs/parameters.md) | `DataParameter` construction, output/input-output procedure parameters, bound parameter vs SQL literal - `Sql.Parameter`, `Sql.Constant`, `InlineParameters` |
| [`docs/hints.md`](docs/hints.md) | Query, table, index, join, subquery, provider-specific and MERGE hints |
| [`docs/hints-api-map.md`](docs/hints-api-map.md) | Reverse lookup from provider SQL hint text to typed provider-specific helper APIs |
| [`docs/translatable-methods.md`](docs/translatable-methods.md) | `String` / `Math` / `DateTime` methods and `Sql.*` helpers in LINQ queries |
| [`docs/provider-setup.md`](docs/provider-setup.md) | `UseXxx` methods, `ProviderName` constants, driver packages, dialect/version options per provider |
| [`docs/provider-capabilities.md`](docs/provider-capabilities.md) | MERGE, CTE, bulk copy, OUTPUT/RETURNING support per provider - check before using them |
| [`docs/raw-sql.md`](docs/raw-sql.md) | Raw SQL query roots and command execution - `FromSql`, `FromSqlScalar`, `RawSqlString`, `SetCommand`, `CommandInfo`, `ToSqlQuery`, `QuerySql` |
| [`docs/extensions.md`](docs/extensions.md) | Extension mechanisms - `[Sql.Expression]`, `[Sql.Function]`, `[ExpressionMethod]`, `IMemberTranslator` / `UseMemberTranslator` |
| [`docs/interceptors.md`](docs/interceptors.md) | Choosing and registering interceptors; callback timing and supported use cases |
| [`docs/configuration.md`](docs/configuration.md) | Logging, retry, interceptors, `DataOptions` builder, `app.config`/`web.config`/JSON configuration |
| [`docs/coverage.md`](docs/coverage.md) | Which topics have a guide; for the rest, search `linq2db.xml` |

## Quick violation reference

Full wrong/correct code examples are in [`docs/agent-antipatterns.md`](docs/agent-antipatterns.md).

| Violation | Consequence |
|---|---|
| `DataOptions` recreated per operation | Option initialization repeated per call; the `DataOptions` XML-doc asks for one shared instance |
| `MappingSchema` recreated per connection | Destroys internal caches; severe performance degradation under load |
| Provider driver package missing | Compiles; fails at runtime with assembly-not-found error |
| `DataConnection` opened before `TransactionScope` created | Transaction not applied; data committed outside scope |
| Online docs or memory of another version used for API shape | Version mismatch; the guides and `linq2db.xml` match this package |
| API assumed missing because no guide mentions it | `linq2db.xml` is the version-matched API reference; search it before using generic fallbacks |
| `InsertOrReplace` / `InsertOrReplaceAsync` used with `[Identity]` PK | `LinqToDBException` at query build time - upsert requires a caller-supplied PK value; identity columns have none |
| `string` / `decimal` column without explicit `Length` / `Precision` / `Scale` | Provider fills in implicit defaults that differ across databases; schema becomes non-portable |
| `using LinqToDB.Async` missing | Async methods (`ToListAsync`, `InsertAsync`, `MergeAsync`, etc.) not found at compile time |
| Empty temporary table + separate `BulkCopy` as the default for existing rows | Lower-level pattern used instead of the source-shaped `CreateTempTable(items)` / `CreateTempTable(query)` overloads |
| Scope hint expected to cover tables joined through associations | Those tables get no hint ([#4321](https://github.com/linq2db/linq2db/issues/4321)); join them explicitly |
