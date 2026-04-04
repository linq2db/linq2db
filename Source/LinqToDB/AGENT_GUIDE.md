# LinqToDB Agent Guide

> **AI/LLM agents MUST read this file completely before writing any code against this package.**

**Required workflow — do not skip steps:**

1. Read `docs/architecture.md` and `docs/agent-antipatterns.md`.
2. Inspect XML-doc for every LinqToDB symbol you will use.
3. Only then write code.
4. Validate by compile + runtime.

Details for each step follow below.

---

## Step 1 — Read bundled reference docs FIRST

This package includes version-matched machine-readable docs in the `docs/` directory,
co-located with this file in the NuGet package.

**DO NOT use GitHub, online API docs, or memory of prior versions as primary sources.**
They may not match this package version. Always use the bundled files below:

| File | When to read |
|---|---|
| `docs/architecture.md` | **Always** — translation pipeline, entry points, connection model |
| `docs/agent-antipatterns.md` | **Always** — common mistakes with WRONG/CORRECT code examples |
| `docs/provider-setup.md` | When configuring any provider — `UseXxx` method signatures, required packages |
| `docs/provider-capabilities.md` | When using MERGE, CTE, bulk copy, OUTPUT/RETURNING — check provider support first |
| `docs/configuration.md` | When configuring DataOptions, logging, retry, interceptors |
| `docs/translatable-methods.md` | When using `String`/`Math`/`DateTime` methods in LINQ queries |
| `docs/custom-sql.md` | When mapping custom methods to SQL expressions |

---

## Step 2 — Inspect XML-doc for every type you use

XML-doc is the **authoritative source** for lifetime rules, usage constraints, and required patterns.
Markdown docs provide orientation. XML-doc provides the per-symbol rules that markdown summarises but does not fully enumerate.

YOU MUST inspect XML-doc for:

- XML-doc for symbol `LinqToDB.LinqToDBArchitecture` — architecture overview and cross-references; start here before inspecting other symbols
- `DataOptions` — MUST be created once per application and shared; DO NOT recreate per operation or per request
- `DataConnection` — connection lifecycle, session semantics, when to use vs `DataContext`
- `DataContext` — per-command connection, `TransactionScope` auto-enlist behaviour
- `MappingSchema` — MUST be created once; DO NOT recreate per connection — destroys internal caches
- Provider `UseXxx` extension methods (e.g., `UseSqlServer`, `UseSQLite`) — exact overloads, version flags, options

For each symbol, extract:
- lifetime requirements (when to create, how long to keep, when to dispose)
- caching and thread-safety constraints
- required construction pattern (e.g., must share a single instance)
- provider-specific limitations or required companion packages

Skipping this step produces code that compiles and runs but violates critical lifetime rules.
See anti-pattern #8 in `docs/agent-antipatterns.md`.

---

## Step 3 — Verify provider runtime dependencies BEFORE writing code

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

---

## Step 4 — Generate code

Patterns in `readme.md` and bundled docs are correct but not exhaustive.
Apply all rules discovered in steps 1–3 before writing code.

Key rules that are easy to miss:

- `DataOptions` — create once (`static readonly`), pass to every `DataConnection` constructor
- `MappingSchema` — create once at startup, attach to `DataOptions` via `.UseMappingSchema(...)`
- `DataConnection` — create per operation (scoped); dispose after use
- Temp tables, explicit transactions, session state — require `DataConnection`, not `DataContext`

---

## Step 5 — Validate

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
| XML-doc not inspected before code generation | Lifetime and usage rules silently violated |
| XML-doc inspected only after code was written | Code may compile but still violate required lifetime and caching rules — inspection must precede the first line of code |
| `InsertOrReplace` / `InsertOrReplaceAsync` used with `[Identity]` PK | `LinqToDBException` at query build time — upsert requires a caller-supplied PK value; identity columns have none |
