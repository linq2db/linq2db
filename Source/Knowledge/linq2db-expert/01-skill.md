<!-- Generated from: Source/Skills/linq2db/SKILL.md -->

# linq2db Skill <!-- omit in toc -->

> **This file is the mandatory entry point for AI agents.**
> **Read this file in full before reading any other file from this package.**
> Do not use public APIs from this package until this file has been read.

This package includes a package-local AI agent skill:
- `SKILL.md` is the canonical AI entry point.
- `docs/*.md` contains task-specific skill references.
- `docs/api.md` provides API discovery and generated AI metadata extracted from XML-doc.
- `lib/<TFM>/linq2db.xml` is the version-matched primary reference for exact API facts when
  the generated extract or markdown guidance is not enough.

Navigation:
- [Core reference](#core-reference) - **required** before writing any code; re-read for non-trivial tasks
- [When adding LinqToDB to a project](#when-adding-linqtodb-to-a-project) - **required** once per integration
- [When writing queries and DML](#when-writing-queries-and-dml) - **required** for every implementation task

---

## Core reference

**Must read before writing any LinqToDB code.**
These files define global rules that apply to every operation. Keep them in mind for every change.

| File | Purpose |
|---|---|
| `docs/architecture.md` | Translation pipeline, entry points, connection model |
| `docs/agent-antipatterns.md` | Common mistakes with WRONG/CORRECT code examples; quick symptom index at the top |
| `docs/coverage.md` | Covered and not-yet-covered AI documentation areas; use it to decide when generated API lookup or raw XML-doc confirmation is required |
| `docs/associations.md` | `[Association]`, fluent associations, `LoadWith` / `ThenLoad`, eager-loading strategies, and no-lazy-loading rules |

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

## When adding LinqToDB to a project

Perform these steps once when integrating the library, not for every query or change.

### 1 - Verify provider runtime dependencies

`linq2db` does **NOT** bundle database drivers.
**A project that only references `linq2db` will compile successfully but fail at runtime.**

Every provider requires a separate ADO.NET driver NuGet package:

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

See `docs/provider-setup.md` for the complete list including version requirements and dialect options.

**Check the project file** (`.csproj`) for the required driver package before writing any code.
If it is missing, add it - do not assume it will be present at runtime.

### 2 - Use XML-doc as exact API reference when needed

Markdown documentation is sufficient for most code generation scenarios.
For exact public API facts, use `docs/api.md` first as a generated search index. Inspect raw
XML-doc only when the generated extract or markdown guidance does not contain enough detail.

The XML documentation file ships with the package assembly:
`lib/<TFM>/linq2db.xml`
Use it for version-matched signatures, overloads, parameter documentation, return types, remarks,
and custom AI metadata. Do not read it sequentially.

### 3 - Use generated API discovery before raw XML-doc

Do not invent APIs, overloads, options, XML-doc remarks, AI metadata, provider flags, or provider
capabilities. Also do not assume an API is missing just because markdown docs do not mention it.

Do not use `LinqToDB.Internal.*` APIs in application code. They are implementation details even
when visible as public members in XML documentation or generated extracts.

Use outside knowledge only for the parts of the task that are not specific to LinqToDB. This can
include database tuning, SQL concepts, .NET/C# behavior, business-domain reasoning, or any other
general knowledge needed to understand the user's problem. Do not treat this package as the source
of truth for those non-LinqToDB topics.

This package does not provide authoritative advice for non-LinqToDB topics. Do not cite package
docs as the reason to choose a database tuning strategy, indexing strategy, business decision, or
other non-LinqToDB approach. Use package docs only to explain how to implement the chosen approach
with LinqToDB correctly.

For LinqToDB-specific decisions, this package is the source of truth: public API names and
signatures, namespaces, receiver types, provider-specific helpers, fallback order, mapping rules,
query composition rules, connection lifetime rules, and architecture constraints must be grounded
in bundled markdown docs, `docs/api.md`, or `lib/<TFM>/linq2db.xml`. When outside knowledge suggests
a SQL feature or implementation strategy, map it to LinqToDB through this package before writing
code. If no package-confirmed LinqToDB API path is found, say that and only then discuss fallbacks.

For any API-level question, especially provider-specific APIs, hints, SQL extensions,
configuration, and DML/query extensions:

1. First read the relevant markdown guide for concepts, boundaries, and common mistakes.
2. Start with the narrowest applicable API surface: provider-specific guides, maps, namespaces,
   and typed helpers.
3. Use `docs/api.md` as the curated API discovery extract when available. Do not read it
   sequentially; it is a search index. Search its
   `Search anchors:` lines first by task words, provider names, SQL keywords, likely member names,
   receiver scope, and AI metadata.
4. Use headings, summaries, and AI metadata to confirm likely candidates.
5. When `docs/api.md` has a candidate entry, copy the `XML member` id from its table row and search
   `lib/<TFM>/linq2db.xml` by that exact id for exact public API names, signatures, overloads,
   parameters, return types, remarks, and AI metadata when those details are not clear from the
   generated extract.
6. Treat XML-doc as the version-matched primary reference for exact API facts on members that have
   XML comments, but use it through `docs/api.md` whenever possible.
7. Do not conclude that an API is unavailable until `docs/api.md` has been searched and raw
   XML-doc has been searched when the generated extract is inconclusive.
   A compact `docs/api.md` extract entry groups overload families; a missing overload in that
   extract is not proof that the overload is absent.
8. Prefer typed or provider-specific APIs found in XML-doc over generic string-based APIs.
9. Use generic APIs such as `QueryHint`, `TableHint`, `Sql.Expression`, or raw SQL only as
   fallbacks when no typed API exists or when the typed API does not cover the requested case.
10. Use custom SQL, raw SQL, and interceptors only after typed and generic APIs do not cover the
   requested case.
11. If markdown and XML-doc disagree, prefer XML-doc for exact API shape and state the discrepancy.

For SQL hint questions, use this mandatory lookup order before answering:

1. Read `docs/hints.md` for hint concepts and fallback rules.
2. Search `docs/hints-api-map.md` by provider name, SQL hint text, and likely helper-name fragments.
3. Treat map hits as candidate typed provider helpers, then verify the exact member in
   `lib/<TFM>/linq2db.xml`.
4. Typed provider helpers are not available directly on plain `ITable<T>` or `IQueryable<T>`.
   Before calling a typed helper, call the provider marker method from `docs/hints.md`
   (`AsSqlServer()`, `AsOracle()`, `AsClickHouse()`, etc.) to switch the receiver to the matching
   provider-specific table/query interface.
5. If a candidate is a table-local hint, also check whether the same provider and SQL hint has a
   tables-in-scope helper. Choose the table-local or scope-level helper based on whether the hint
   should affect one table source or all table references in the current query scope.
6. For user wording such as "several tables", "all tables", "whole query", or "scope", search for
   `HintType=TablesInScope` and provider helper names containing `InScope` before recommending
   generic `TablesInScopeHint(...)`. Apply a `TablesInScope` helper to the query/subquery that
   already contains the tables to affect; do not apply it to the first table before composing joins.
7. Use common name shapes only to guide search: `<Base>Hint` -> `<Base>InScopeHint` and
   `With<Base>` -> `With<Base>InScope`. Do not invent unverified scope-helper names by string
   concatenation; verify the exact API in the map and XML-doc.
8. Search the provider `*Hints` API entries by SQL hint text, candidate helper names,
   receiver types, and AI metadata such as `Group=Hints`.
9. Prefer typed/provider-specific helpers found in the map or XML-doc.
10. Recommend generic hint APIs (`QueryHint`, `TableHint`, `TablesInScopeHint`, etc.) only after
   map, generated API lookup, and raw XML-doc confirmation fail to find a typed helper for the
   installed package version.
11. Recommend `Sql.Expression`, raw SQL, or interceptors only after both typed and generic hint APIs
   do not cover the requested case.

When answering a concrete provider-specific hint question, ground the answer in the API lookup
result. If a typed helper is found, name the required provider marker, the typed helper, and its
receiver before showing fallback APIs. If no typed helper is found, say that the exact map and
generated API lookup and raw XML-doc confirmation did not find one before
recommending `QueryHint`, `TableHint`, `TablesInScopeHint`, `Sql.Expression`, raw SQL, or
interceptors.

Do not answer a provider-specific hint question from the generic hints model alone.
Do not claim that `docs/hints-api-map.md` lacks a typed helper unless you searched it by exact
provider and exact SQL/database term, then searched `docs/api.md` and raw XML-doc, when needed, for the provider
`*Hints` type, SQL term, likely helper fragments, and AI metadata such as `Group=Hints`.
Do not skip this lookup because the database feature is a table modifier, lock clause, query
directive, or provider-specific SQL extension rather than a classic optimizer hint.

For temporary table questions, read `docs/query-temp-tables.md` before answering.
Temporary tables are a common "known topic" where prior ORM knowledge easily selects a lower-level
pattern than the current LinqToDB API. Choose the `CreateTempTable*` overload by source shape:

1. Rows already available in C# memory -> prefer `CreateTempTable(items)` /
   `CreateTempTableAsync(items)`.
2. Rows come from an `IQueryable<T>` -> prefer `CreateTempTable(query)` /
   `CreateTempTableAsync(query)`; LinqToDB populates the table with server-side
   `INSERT ... SELECT`.
3. Empty table first -> use `CreateTempTable<T>()` only when rows are not available at creation
   time, load timing must be separated, or explicit post-create work is required.
4. Anonymous-type projection -> specify a table name; use the `setTable` fluent mapping parameter
   when anonymous `string` or `decimal` columns need length/precision metadata.
5. In the answer, name the selected overload and source shape before showing code. Mention
   `TempTable<T>` lifecycle (`using` / `await using` drops the backing table) when lifetime is
   relevant.

When a guide lists several possible implementation paths, the order is meaningful. Read and try
the most specific package-version path first. Generic APIs, custom SQL, raw SQL, and interceptors
are fallback paths unless the guide explicitly says otherwise.

Types where raw XML-doc is often useful when `docs/api.md` is not enough:

- `docs/architecture.md` - architecture overview, translation pipeline, and cross-references
- `DataOptions` - MUST be created once per application and shared; DO NOT recreate per operation or per request
- `DataConnection` - connection lifecycle, session semantics, when to use vs `DataContext`
- `DataContext` - per-command connection, `TransactionScope` auto-enlist behaviour
- `MappingSchema` - if custom mapping schema is needed, create it once; DO NOT recreate per connection - destroys internal caches
- Provider `UseXxx` extension methods (e.g., `UseSqlServer`, `UseSQLite`) - exact overloads, version flags, options

### 4 - Apply core configuration rules

Key rules that are easy to miss:

- `DataOptions` - create once (`static readonly`), pass to every `DataConnection` constructor
- `MappingSchema` - only create when custom mapping is needed; then create once at startup and attach to `DataOptions` via `.UseMappingSchema(...)`
- `DataConnection` - create per operation (scoped); dispose after use
- Temp tables, explicit transactions, session state - require `DataConnection`, not `DataContext`
- Entity columns used with any LinqToDB API or option that generates a `CREATE TABLE` statement - specify `Length`, `Precision`, `Scale` explicitly for every provider-sensitive type (`string`, `decimal`, etc.).
  If the task does not state exact limits, **both steps are required - not optional**:
  1. choose a bounded value guided by field semantics;
  2. add a `TODO` comment on the same line as the property, marking it as an AI agent assumption.
  A field with a self-chosen size but no `TODO` is an incomplete implementation.
  ```csharp
  [Column(Length = 256)] public string Email { get; set; } = null!; // TODO: Confirm column length. 256 is an AI agent assumption.
  ```

---

## When writing queries and DML

Use the relevant reference file for the operation you are implementing.
Each file begins with a **"You are here if"** block.

### Common namespace requirements

Every file that uses LinqToDB needs at minimum:

```csharp
using LinqToDB;
using LinqToDB.Data;
```

Async query and DML extension methods (`ToListAsync`, `FirstOrDefaultAsync`, `SingleAsync`,
`MaxAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `MergeAsync`, etc.) are defined in a
**separate namespace** and require an additional import:

```csharp
using LinqToDB.Async;
```

If an async method is not found by the compiler, the missing `using LinqToDB.Async` is the
most common cause. Each individual CRUD guide repeats this reminder in its async note.

---

**DO NOT use GitHub, online API docs, or memory of prior versions as primary sources.**
They may not match this package version. Always use the bundled files below:

| File | When to read |
|---|---|
| `docs/api.md` | API discovery rules and curated extract entries; read before concluding that an API does not exist or before using generic fallbacks |
| `docs/mapping.md` | Entity mapping, `MappingSchema`, attributes/fluent mapping, schema/DDL-sensitive columns |
| `docs/associations.md` | Relationship metadata and eager loading - `[Association]`, fluent `.Association(...)`, `LoadWith`, `ThenLoad`, eager-loading strategies |
| `docs/crud/crud.md` | All CRUD operations - SELECT, INSERT, UPDATE, DELETE, upsert, bulk copy, MERGE; routes to the right guide |
| `docs/concurrency.md` | Optimistic concurrency for entity update/delete - `UpdateOptimistic`, `DeleteOptimistic`, `WhereKeyOptimistic`, `OptimisticLockPropertyAttribute` |
| `docs/query-cte.md` | CTEs, recursive queries - when `.AsCte()` or `db.GetCte<T>()` is needed |
| `docs/query-temp-tables.md` | Temporary tables - `TempTable<T>`, `CreateTempTable`, `TableOptions`; requires `DataConnection` |
| `docs/null-semantics.md` | Why generated SQL for a null comparison looks more complex than expected - `CompareNulls`, `Sql.AsNotNull`, `IsDistinctFrom`, `Sql.ToNullable`/`Sql.AsNullable` |
| `docs/parameters.md` | `DataParameter` construction, output/input-output procedure parameters, and forcing a value to be a bound parameter vs a SQL literal - `Sql.Parameter`, `Sql.Constant`, `InlineParameters` |
| `docs/hints.md` | Query, table, index, join, subquery, provider-specific, and MERGE hints; before proposing raw SQL, `Sql.Expression`, or interceptors for a hint, check this guide, `docs/hints-api-map.md`, and generated provider `*Hints` API entries |
| `docs/hints-api-map.md` | Reverse lookup from concrete provider SQL hint text to typed provider-specific helper APIs; use it as a search aid, then verify signatures in `docs/api.md` or raw XML-doc when needed |
| `docs/translatable-methods.md` | `String` / `Math` / `DateTime` methods in LINQ queries |
| `docs/provider-capabilities.md` | MERGE, CTE, bulk copy, OUTPUT/RETURNING - check provider support first |
| `docs/raw-sql.md` | Raw SQL query roots and command execution - `FromSql`, `FromSqlScalar`, `RawSqlString`, `SetCommand`, `CommandInfo`, `ToSqlQuery`, `QuerySql` |
| `docs/extensions.md` | Extension mechanisms - `[Sql.Expression]`, `[Sql.Function]`, `[ExpressionMethod]`, and `IMemberTranslator` / `UseMemberTranslator` |
| `docs/interceptors.md` | Choosing and registering interceptors; callback timing and supported use cases |
| `docs/configuration.md` | Logging, retry, interceptors, `DataOptions` builder, `app.config`/`web.config`/JSON configuration |
| `docs/coverage.md` | Coverage status for package-local AI guides; if a topic is not covered, search `docs/api.md` and raw XML-doc when needed |

> For any non-trivial code, transaction handling, lifetime issues, or unexpected exceptions - consult `docs/agent-antipatterns.md` (quick symptom index at the top) and `docs/architecture.md`.

## Quick violation reference

Full WRONG/CORRECT code examples are in `docs/agent-antipatterns.md`.

| Violation | Consequence |
|---|---|
| `SKILL.md` not read before task-specific docs | Task-specific guidance interpreted without global rules; likely lifetime, namespace, or schema violations |
| `DataOptions` recreated per operation | Correctness violation; prohibited by package docs and `DataOptions` XML-doc |
| `MappingSchema` recreated per connection | Destroys internal caches; severe performance degradation under load |
| Provider driver package missing | Compiles; fails at runtime with assembly-not-found error |
| `DataConnection` opened before `TransactionScope` created | Transaction not applied; data committed outside scope |
| GitHub / online docs used as primary source | Version mismatch risk; bundled docs are the authoritative version-matched source |
| API assumed missing because it is not in markdown | `docs/api.md` and raw XML-doc are the version-matched API reference; search them before using generic fallbacks |
| Raw XML-doc not checked when generated docs are inconclusive | Exact signature, overload, lifetime, or remarks detail can be missed |
| `InsertOrReplace` / `InsertOrReplaceAsync` used with `[Identity]` PK | `LinqToDBException` at query build time - upsert requires a caller-supplied PK value; identity columns have none |
| `string` / `decimal` column without explicit `Length` / `Precision` / `Scale` | Provider fills in implicit defaults that differ across databases; schema becomes non-portable |
| Self-chosen `Length` / `Precision` / `Scale` with no `TODO` comment | Assumption is invisible; cannot distinguish confirmed values from guesses - treat as incomplete |
| `using LinqToDB.Async` missing | Async methods (`ToListAsync`, `InsertAsync`, `MergeAsync`, etc.) not found at compile time |
| Empty temporary table + separate `BulkCopy` recommended as the default for existing rows | Lower-level pattern used instead of the source-shaped `CreateTempTable(items)` / `CreateTempTable(query)` overloads |
