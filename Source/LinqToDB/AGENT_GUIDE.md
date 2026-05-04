# LinqToDB Agent Guide

> **This file is the mandatory entry point for AI agents.**
> **Read this file in full before reading any other file from this package.**
> Do not use public APIs from this package until this file has been read.

Navigation:
- [Core reference](#core-reference) — **required** before writing any code; re-read for non-trivial tasks
- [When adding LinqToDB to a project](#when-adding-linqtodb-to-a-project) — **required** once per integration
- [When writing queries and DML](#when-writing-queries-and-dml) — **required** for every implementation task

---

## Core reference

**Must read before writing any LinqToDB code.**
These files define global rules that apply to every operation. Keep them in mind for every change.

| File | Purpose |
|---|---|
| `docs/architecture.md` | Translation pipeline, entry points, connection model |
| `docs/agent-antipatterns.md` | Common mistakes with WRONG/CORRECT code examples; quick symptom index at the top |

---

## When adding LinqToDB to a project

Perform these steps once when integrating the library, not for every query or change.

### 1 — Verify provider runtime dependencies

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
| Firebird | `UseFirebird(...)` | `FirebirdSql.Data.FirebirdClient` |
| Sybase / SAP ASE | `UseAse(...)` | `AdoNetCore.AseClient` |

See `docs/provider-setup.md` for the complete list including version requirements and dialect options.

**Check the project file** (`.csproj`) for the required driver package before writing any code.
If it is missing, add it — do not assume it will be present at runtime.

### 2 — Inspect XML-doc for lifetime-sensitive types

Markdown documentation is sufficient for most code generation scenarios.
For lifetime-sensitive types, inspect XML-doc when available —
it contains constraints that markdown summarises but does not fully enumerate.

The XML documentation file ships with the package assembly:
`lib/<TFM>/linq2db.xml`
Use it directly — do not search online or in source repositories.

### 3 - Use XML-doc for exact API discovery

Do not invent APIs, overloads, options, XML-doc remarks, AI-Tags, provider flags, or provider
capabilities. Also do not assume an API is missing just because markdown docs do not mention it.

For any API-level question, especially provider-specific APIs, hints, SQL extensions,
configuration, and DML/query extensions:

1. First read the relevant markdown guide for concepts, boundaries, and common mistakes.
2. Start with the narrowest applicable API surface: provider-specific guides, maps, namespaces,
   and typed helpers.
3. Use `docs/api.md` as the curated API discovery extract when available. Search its
   `Search anchors:` lines first by task words, provider names, SQL keywords, likely member names,
   receiver scope, and AI-Tags.
4. Use headings, summaries, and AI-Tags to confirm likely candidates.
5. When `docs/api.md` has a candidate entry, copy the `XML member` id from its table row and search
   `lib/<TFM>/linq2db.xml` by that exact id for exact public API names, signatures, overloads,
   parameters, return types, remarks, and AI-Tags.
6. Treat XML-doc as the complete current-version public API surface for members that have XML
   comments.
7. Do not conclude that an API is unavailable until XML-doc has been searched.
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
4. If a candidate is a table-local hint, also check whether the same provider and SQL hint has a
   tables-in-scope helper. Choose the table-local or scope-level helper based on whether the hint
   should affect one table source or all table references in the current query scope.
5. For user wording such as "several tables", "all tables", "whole query", or "scope", search for
   `HintType=TablesInScope` and provider helper names containing `InScope` before recommending
   generic `TablesInScopeHint(...)`. Apply a `TablesInScope` helper to the query/subquery that
   already contains the tables to affect; do not apply it to the first table before composing joins.
6. Use common name shapes only to guide search: `<Base>Hint` -> `<Base>InScopeHint` and
   `With<Base>` -> `With<Base>InScope`. Do not invent unverified scope-helper names by string
   concatenation; verify the exact API in the map and XML-doc.
7. Search the provider `*Hints` XML-doc members by SQL hint text, candidate helper names,
   receiver types, and `AI-Tags: Group=Hints`.
8. Prefer typed/provider-specific helpers found in the map or XML-doc.
9. Recommend generic hint APIs (`QueryHint`, `TableHint`, `TablesInScopeHint`, etc.) only after
   map and XML-doc lookup fail to find a typed helper for the installed package version.
10. Recommend `Sql.Expression`, raw SQL, or interceptors only after both typed and generic hint APIs
   do not cover the requested case.

When answering a concrete provider-specific hint question, ground the answer in the API lookup
result. If a typed helper is found, name that helper and its receiver before showing fallback APIs.
If no typed helper is found, say that the exact map and XML-doc lookup did not find one before
recommending `QueryHint`, `TableHint`, `TablesInScopeHint`, `Sql.Expression`, raw SQL, or
interceptors.

Do not answer a provider-specific hint question from the generic hints model alone.
Do not claim that `docs/hints-api-map.md` lacks a typed helper unless you searched it by exact
provider and exact SQL/database term, then searched `docs/api.md` and XML-doc for the provider
`*Hints` type, SQL term, likely helper fragments, and `AI-Tags: Group=Hints`.
Do not skip this lookup because the database feature is a table modifier, lock clause, query
directive, or provider-specific SQL extension rather than a classic optimizer hint.

When a guide lists several possible implementation paths, the order is meaningful. Read and try
the most specific package-version path first. Generic APIs, custom SQL, raw SQL, and interceptors
are fallback paths unless the guide explicitly says otherwise.

Types that require XML-doc inspection:

- `LinqToDB.LinqToDBArchitecture` — architecture overview and cross-references; start here
- `DataOptions` — MUST be created once per application and shared; DO NOT recreate per operation or per request
- `DataConnection` — connection lifecycle, session semantics, when to use vs `DataContext`
- `DataContext` — per-command connection, `TransactionScope` auto-enlist behaviour
- `MappingSchema` — if custom mapping schema is needed, create it once; DO NOT recreate per connection — destroys internal caches
- Provider `UseXxx` extension methods (e.g., `UseSqlServer`, `UseSQLite`) — exact overloads, version flags, options

### 4 - Apply core configuration rules

Key rules that are easy to miss:

- `DataOptions` — create once (`static readonly`), pass to every `DataConnection` constructor
- `MappingSchema` — only create when custom mapping is needed; then create once at startup and attach to `DataOptions` via `.UseMappingSchema(...)`
- `DataConnection` — create per operation (scoped); dispose after use
- Temp tables, explicit transactions, session state — require `DataConnection`, not `DataContext`
- Entity columns used with any LinqToDB API or option that generates a `CREATE TABLE` statement — specify `Length`, `Precision`, `Scale` explicitly for every provider-sensitive type (`string`, `decimal`, etc.).
  If the task does not state exact limits, **both steps are required — not optional**:
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
| `docs/crud/crud.md` | All CRUD operations — SELECT, INSERT, UPDATE, DELETE, upsert, bulk copy, MERGE; routes to the right guide |
| `docs/query-cte.md` | CTEs, recursive queries — when `.AsCte()` or `db.GetCte<T>()` is needed |
| `docs/query-temp-tables.md` | Temporary tables — `TempTable<T>`, `CreateTempTable`, `TableOptions`; requires `DataConnection` |
| `docs/hints.md` | Query, table, index, join, subquery, provider-specific, and MERGE hints; before proposing raw SQL, `Sql.Expression`, or interceptors for a hint, check this guide, `docs/hints-api-map.md`, and the provider `*Hints` XML-doc members |
| `docs/hints-api-map.md` | Reverse lookup from concrete provider SQL hint text to typed provider-specific helper APIs; use it as a search aid, then verify signatures in XML-doc |
| `docs/translatable-methods.md` | `String` / `Math` / `DateTime` methods in LINQ queries |
| `docs/provider-capabilities.md` | MERGE, CTE, bulk copy, OUTPUT/RETURNING — check provider support first |
| `docs/custom-sql.md` | Mapping custom methods to SQL expressions |
| `docs/interceptors.md` | Choosing and registering interceptors; callback timing and supported use cases |
| `docs/configuration.md` | Logging, retry, interceptors, `DataOptions` builder |

> For any non-trivial code, transaction handling, lifetime issues, or unexpected exceptions — consult `docs/agent-antipatterns.md` (quick symptom index at the top) and `docs/architecture.md`.

## Quick violation reference

Full WRONG/CORRECT code examples are in `docs/agent-antipatterns.md`.

| Violation | Consequence |
|---|---|
| `AGENT_GUIDE.md` not read before task-specific docs | Task-specific guidance interpreted without global rules; likely lifetime, namespace, or schema violations |
| `DataOptions` recreated per operation | Correctness violation; prohibited by XML-doc for `DataOptions` |
| `MappingSchema` recreated per connection | Destroys internal caches; severe performance degradation under load |
| Provider driver package missing | Compiles; fails at runtime with assembly-not-found error |
| `DataConnection` opened before `TransactionScope` created | Transaction not applied; data committed outside scope |
| GitHub / online docs used as primary source | Version mismatch risk; bundled docs are the authoritative version-matched source |
| API assumed missing because it is not in markdown | XML-doc is the current-version public API surface for documented members; search it before using generic fallbacks |
| XML-doc not inspected for lifetime-sensitive types | Lifetime and usage rules silently violated |
| `InsertOrReplace` / `InsertOrReplaceAsync` used with `[Identity]` PK | `LinqToDBException` at query build time — upsert requires a caller-supplied PK value; identity columns have none |
| `string` / `decimal` column without explicit `Length` / `Precision` / `Scale` | Provider fills in implicit defaults that differ across databases; schema becomes non-portable |
| Self-chosen `Length` / `Precision` / `Scale` with no `TODO` comment | Assumption is invisible; cannot distinguish confirmed values from guesses — treat as incomplete |
| `using LinqToDB.Async` missing | Async methods (`ToListAsync`, `InsertAsync`, `MergeAsync`, etc.) not found at compile time |
