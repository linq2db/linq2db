# LinqToDB Agent Guide

> Use the section that matches your current task:
> - [Core reference](#core-reference) — always relevant; read first, revisit for any non-trivial code
> - [When adding LinqToDB to a project](#when-adding-linqtodb-to-a-project) — perform once per integration
> - [When writing queries and DML](#when-writing-queries-and-dml) — daily reference

---

## Core reference

These files define the global rules for all LinqToDB code. Read them before writing any LinqToDB code and keep them in mind for every change.

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

Types that require XML-doc inspection:

- `LinqToDB.LinqToDBArchitecture` — architecture overview and cross-references; start here
- `DataOptions` — MUST be created once per application and shared; DO NOT recreate per operation or per request
- `DataConnection` — connection lifecycle, session semantics, when to use vs `DataContext`
- `DataContext` — per-command connection, `TransactionScope` auto-enlist behaviour
- `MappingSchema` — MUST be created once; DO NOT recreate per connection — destroys internal caches
- Provider `UseXxx` extension methods (e.g., `UseSqlServer`, `UseSQLite`) — exact overloads, version flags, options

### 3 — Apply core configuration rules

Key rules that are easy to miss:

- `DataOptions` — create once (`static readonly`), pass to every `DataConnection` constructor
- `MappingSchema` — create once at startup, attach to `DataOptions` via `.UseMappingSchema(...)`
- `DataConnection` — create per operation (scoped); dispose after use
- Temp tables, explicit transactions, session state — require `DataConnection`, not `DataContext`
- Entity columns used with `CreateTable`, `TableOptions.CreateIfNotExists` / `CheckExistence`, or temp tables — specify `Length`, `Precision`, `Scale` explicitly for every provider-sensitive type (`string`, `decimal`, etc.); if the task does not state exact limits, choose a bounded value guided by field semantics and add a `TODO` comment — do not leave column types unconstrained

---

## When writing queries and DML

Use the relevant reference file for the operation you are implementing.
Each file begins with a **"You are here if"** block.

**DO NOT use GitHub, online API docs, or memory of prior versions as primary sources.**
They may not match this package version. Always use the bundled files below:

| File | When to read |
|---|---|
| `docs/crud/crud.md` | All CRUD operations — SELECT, INSERT, UPDATE, DELETE, upsert, bulk copy, MERGE; routes to the right guide |
| `docs/query-cte.md` | CTEs, recursive queries — when `.AsCte()` or `db.GetCte<T>()` is needed |
| `docs/translatable-methods.md` | `String` / `Math` / `DateTime` methods in LINQ queries |
| `docs/provider-capabilities.md` | MERGE, CTE, bulk copy, OUTPUT/RETURNING — check provider support first |
| `docs/custom-sql.md` | Mapping custom methods to SQL expressions |
| `docs/configuration.md` | Logging, retry, interceptors, `DataOptions` builder |

> For any non-trivial code, transaction handling, lifetime issues, or unexpected exceptions — consult `docs/agent-antipatterns.md` (quick symptom index at the top) and `docs/architecture.md`.

---

## Validate

1. **Compile.** Fix all errors and all warnings, including obsolete API warnings — they indicate the correct modern replacement.
2. **Run.** A green build does not guarantee correct lifetime behaviour or provider runtime dependencies.
3. **Inspect generated SQL** where possible — LinqToDB can log SQL via `DataOptions.UseTracing(...)`.

---

## Quick violation reference

Full WRONG/CORRECT code examples are in `docs/agent-antipatterns.md`.

| Violation | Consequence |
|---|---|
| `DataOptions` recreated per operation | Correctness violation; prohibited by XML-doc for `DataOptions` |
| `MappingSchema` recreated per connection | Destroys internal caches; severe performance degradation under load |
| Provider driver package missing | Compiles; fails at runtime with assembly-not-found error |
| `DataConnection` opened before `TransactionScope` created | Transaction not applied; data committed outside scope |
| GitHub / online docs used as primary source | Version mismatch risk; bundled docs are the authoritative version-matched source |
| XML-doc not inspected for lifetime-sensitive types | Lifetime and usage rules silently violated |
| `InsertOrReplace` / `InsertOrReplaceAsync` used with `[Identity]` PK | `LinqToDBException` at query build time — upsert requires a caller-supplied PK value; identity columns have none |
| `string` / `decimal` column without `Length` / `Precision` / `Scale` in a mapped entity | Provider fills in implicit defaults that differ across databases; schema becomes non-portable |
