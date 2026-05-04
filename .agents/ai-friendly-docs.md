# AI-Friendly Documentation Maintenance

This file defines how to create, review, and update package-local documentation optimized for
AI coding agents and LLM-based tools.

AI-friendly documentation should let an agent write correct LinqToDB code without relying on
online documentation, old examples, or memory of prior versions.

The coverage checklist below is part of this process: keep it current when guides are added,
audited, split, merged, or intentionally deferred.

---

## Documentation Model

AI-friendly documentation is part of the LinqToDB package surface. It is written for code agents
that work inside a consuming project after the NuGet package has been installed.

The package should provide enough local context for an agent to:

- discover the right API without internet access;
- avoid version drift from GitHub, online docs, blog posts, or older examples;
- understand library-specific invariants before generating code;
- route from a broad task to the narrow guide that describes the relevant API;
- inspect XML documentation only when markdown intentionally points to the full API contract.

Entry points:

| File | Audience | Purpose |
|---|---|---|
| `Source/LinqToDB/readme.md` | Humans and package browsers | Short package overview and feature discovery. |
| `Source/LinqToDB/AGENT_GUIDE.md` | AI coding agents | Mandatory first file for agents before using public APIs. |
| `Source/LinqToDB/docs/*.md` | AI coding agents and humans | Task-focused guides routed from `AGENT_GUIDE.md`. |
| `lib/<TFM>/linq2db.xml` | AI coding agents and IDEs | Full XML documentation for exact overloads and lifetime-sensitive details. |

`readme.md`, `AGENT_GUIDE.md`, and `docs/*.md` are intended to ship with the NuGet package.
Write links as package-local relative paths, not repository-root assumptions or GitHub URLs.
For example, `docs/mapping.md` is correct from `AGENT_GUIDE.md`, and `crud/crud-update.md` is
correct from `docs/crud/crud.md`.

Do not make package-shipped documentation depend on files that are present only in the repository,
such as `.agents/*`, `.github/*`, test projects, or build scripts. This file is repository-local
maintenance guidance and is not itself a package entry point.

---

## Update Rules

When adding or changing AI-friendly documentation:

1. Keep authoritative documentation package-local under `Source/LinqToDB/docs`.
2. Add the standard opening block from `Standard Opening Block` to every task guide.
3. Add or update the link in `Source/LinqToDB/AGENT_GUIDE.md` when the guide is relevant to users
   of the package.
4. Add `Related documentation` links at the end of each guide.
5. Prefer installed package files and XML documentation over GitHub, online docs, or memory.
   Missing from markdown does not mean a public API is missing; agents must search XML-doc before
   documenting generic fallbacks for provider-specific APIs.
6. Document when to use an API and when not to use it. Agents need boundaries, not only examples.
7. Put the narrowest and most specific discovery path before broader fallback paths. Agents often
   follow the first plausible solution they find. Provider-specific maps, typed APIs, exact
   XML-doc lookup, and package-version APIs must appear before generic APIs, custom SQL, raw SQL,
   or interceptors.
8. Put provider-specific behavior in an explicit provider-specific note.
9. If an example contains an assumed value, add a `TODO` comment on the same line.
10. Keep examples small, compilable in shape, and focused on the documented API.
11. Mention required namespaces, especially `LinqToDB.Async` for async APIs.
12. For public APIs with AI-Tags, keep `docs/ai-tags.md` and XML-doc tags aligned.
13. If a public API is declared in `*.generated.cs`, do not treat that file as the source of truth.
    Find and update the generator/template first (for example the matching `.tt` file), then
    update the checked-in generated file as generated output.
    For generated hint helpers, pass the concrete SQL hint text into the generator method (for
    example `sqlHint`) and include it in the XML-doc summary.
14. `Source/LinqToDB/docs/api.md` contains a generated API extract derived from `linq2db.xml`.
    If XML-doc output changes, regenerate the extract in the same change. Do not hand-edit generated
    extract rows as the long-term fix.
15. The generated API extract must include only LinqToDB public API members. Exclude external or
    compatibility XML-doc members such as `System.*`, `Microsoft.*`, `JetBrains.*`, and
    `BitOperations`; they may appear only as parameter or return types in LinqToDB signatures.
16. Use CRLF line endings for all edited files.

Do not mark a row as done until:

- the guide exists or the existing guide was audited;
- it has the standard opening block when it is a task guide;
- it has examples for the common path and at least one common mistake;
- provider caveats are called out when applicable;
- `AGENT_GUIDE.md` routes agents to it when package users need it;
- related docs are linked.

---

## Standard Opening Block

Every task-focused guide under `Source/LinqToDB/docs` must start with the same entry protocol.
Use this template after the document title and before the guide body:

```md
> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - ...
> - ...
```

Rules:

- Keep the `Stop` text and `⚠️` marker identical across guides.
- Adjust only the relative link to `AGENT_GUIDE.md` when the guide is nested deeper, for example
  `../../AGENT_GUIDE.md` from `docs/crud/*.md`.
- Keep `You are here if you need to:` as the exact heading text.
- Use concise bullets that describe user intent, not API inventory.
- Do not add extra warnings before this block; put guide-specific caveats after it.

---

## Coverage Checklist

| Done | Area | Target doc | Priority | Status | Notes |
|---|---|---|---|---|---|
| [x] | Agent entry point | `Source/LinqToDB/AGENT_GUIDE.md` | Critical | Done | Mandatory package entry point for agents. |
| [x] | Agent skill entry point | `Source/LinqToDB/SKILL.md` | Critical | Done | Compatibility entry point for agent skill systems; routes to `AGENT_GUIDE.md`, task guides, and XML-doc API discovery. |
| [x] | API discovery | `Source/LinqToDB/docs/api.md` | Critical | Done | General rules for finding exact API members in XML-doc before using generic fallbacks. |
| [x] | Architecture overview | `Source/LinqToDB/docs/architecture.md` | Critical | Done | Core reference; already linked from `AGENT_GUIDE.md`. |
| [x] | Agent anti-patterns | `Source/LinqToDB/docs/agent-antipatterns.md` | Critical | Done | Quick symptom index and wrong/correct examples. |
| [ ] | AI-Tags governance | `Source/LinqToDB/docs/ai-tags.md` | High | Partial | Tag vocabulary exists, including hint `HintType`; validator is not planned yet. |
| [x] | Mapping | `Source/LinqToDB/docs/mapping.md` | High | Done | Conventions, attributes, fluent mapping, `MappingSchema`, DDL metadata, `DataType`, `DbType`, conversions, associations. |
| [ ] | Mapping `Configuration` selector | `Source/LinqToDB/docs/mapping-configuration.md` | High | Planned | Attribute/fluent metadata selected by mapping schema configuration names; underused provider- or environment-specific mapping mechanism. |
| [ ] | Configuration and `DataOptions` | `Source/LinqToDB/docs/configuration.md` | High | Existing / needs audit | Includes `UseOptions`; verify coverage against current `DataOptions` APIs later. |
| [ ] | Provider setup | `Source/LinqToDB/docs/provider-setup.md` | High | Existing / needs audit | Driver packages and provider bootstrap. |
| [ ] | Provider capabilities | `Source/LinqToDB/docs/provider-capabilities.md` | High | Existing / needs audit | MERGE, CTE, bulk copy, OUTPUT/RETURNING support. |
| [ ] | CRUD overview | `Source/LinqToDB/docs/crud/crud.md` | High | Existing / needs audit | Router for CRUD guides. |
| [ ] | SELECT basics | `Source/LinqToDB/docs/crud/crud-select.md` | High | Existing / needs audit | Query materialization and async namespace checks. |
| [ ] | INSERT | `Source/LinqToDB/docs/crud/crud-insert.md` | High | Existing / needs audit | Insert routes to value/select variants. |
| [ ] | INSERT values | `Source/LinqToDB/docs/crud/crud-insert-values.md` | High | Existing / needs audit | Entity and fluent value inserts. |
| [ ] | INSERT from SELECT | `Source/LinqToDB/docs/crud/crud-insert-select.md` | High | Existing / needs audit | Insert from query patterns. |
| [ ] | UPDATE | `Source/LinqToDB/docs/crud/crud-update.md` | High | Existing / needs audit | Entity and fluent update patterns. |
| [ ] | DELETE | `Source/LinqToDB/docs/crud/crud-delete.md` | High | Existing / needs audit | Delete patterns and safeguards. |
| [ ] | Upsert | `Source/LinqToDB/docs/crud/crud-upsert.md` | High | Existing / needs audit | `InsertOrReplace` and related caveats. |
| [ ] | MERGE | `Source/LinqToDB/docs/crud/crud-merge.md` | High | Existing / needs audit | Merge builder plus merge-specific hints. |
| [ ] | Bulk copy | `Source/LinqToDB/docs/crud/crud-bulkcopy.md` | High | Existing / needs audit | Bulk insert behavior and provider support. |
| [ ] | CTE | `Source/LinqToDB/docs/query-cte.md` | High | Existing / needs audit | `.AsCte()` and recursive CTE patterns. |
| [ ] | Temporary tables | `Source/LinqToDB/docs/query-temp-tables.md` | High | Existing / needs audit | `TempTable<T>`, `CreateTempTable`, `DataConnection` requirement. |
| [ ] | Translatable methods | `Source/LinqToDB/docs/translatable-methods.md` | Medium | Existing / needs audit | String, Math, DateTime translation guidance. |
| [ ] | Custom SQL mapping | `Source/LinqToDB/docs/custom-sql.md` | High | Existing / needs audit | SQL function/expression mapping. |
| [ ] | Interceptors | `Source/LinqToDB/docs/interceptors.md` | Medium | Existing / needs audit | Callback choice and registration. |
| [x] | Hints | `Source/LinqToDB/docs/hints.md` | High | Done | General raw-text hints, provider-specific `AsXxx()` typed hint APIs, unsupported provider gaps, safe multi-provider branches, merge hints, generated provider hint `AI-Tags` via T4, handwritten provider hint `AI-Tags` in XML docs. |
| [x] | Provider hints API map | `Source/LinqToDB/docs/hints-api-map.md` | High | Done | Reverse lookup from concrete provider SQL hint text to typed provider-specific helper APIs, generated from XML-doc-shaped source comments. |
| [x] | Provider hint gaps | `Source/LinqToDB/docs/hints.md` | Medium | Done | DB2 optimization guidelines, Firebird `PLAN`, Informix directives, SAP HANA `WITH HINT`, and Sybase dialect hints are documented as unsupported/gap areas; implementation is out of scope. |
| [ ] | Query composition basics | `Source/LinqToDB/docs/query-basics.md` | High | Planned | Deferred execution, `IQueryable`, materialization, client/server boundary. |
| [ ] | Joins | `Source/LinqToDB/docs/query-joins.md` | High | Planned | Inner/left/cross/apply joins, navigation-like joins, provider limitations. |
| [ ] | Grouping and aggregation | `Source/LinqToDB/docs/query-grouping.md` | High | Planned | `GroupBy`, aggregates, HAVING, projection rules. |
| [ ] | Ordering and paging | `Source/LinqToDB/docs/query-paging.md` | High | Planned | `OrderBy`, `Skip`, `Take`, deterministic paging, `TakeHints`. |
| [ ] | Set operations | `Source/LinqToDB/docs/query-set-operations.md` | Medium | Planned | `Concat`, `Union`, `Except`, `Intersect`, provider differences. |
| [ ] | Projections | `Source/LinqToDB/docs/query-projections.md` | High | Planned | DTO projections, computed values, nested projections, materialization traps. |
| [ ] | Null semantics | `Source/LinqToDB/docs/null-semantics.md` | High | Planned | SQL three-valued logic, nullable comparisons, coalesce, provider differences. |
| [ ] | Associations and eager loading | `Source/LinqToDB/docs/associations.md` | High | Next | Separate guide for `[Association]`, fluent associations, `LoadWith`, `ThenLoadWith`, nullability, predicates, and no lazy loading. |
| [ ] | Inheritance mapping | `Source/LinqToDB/docs/inheritance-mapping.md` | Medium | Planned | Discriminators, inheritance attributes, query behavior. |
| [ ] | Advanced value converters | `Source/LinqToDB/docs/value-conversions.md` | Low | Deferred | Only if mapping guide becomes insufficient; would cover `IValueConverter`, null handling, provider types, and reusable converter patterns. |
| [ ] | Custom mapping metadata | `Source/LinqToDB/docs/custom-mapping-metadata.md` | Low | Deferred | Metadata readers and custom mapping attributes; document only if package users have a real extension scenario. |
| [ ] | Raw SQL | `Source/LinqToDB/docs/raw-sql.md` | High | Planned | `FromSql`, SQL queries, parameters, composability boundaries. |
| [ ] | Parameters | `Source/LinqToDB/docs/parameters.md` | High | Planned | `DataParameter`, `DataType`, `DbType`, precision, output parameters. |
| [ ] | Transactions | `Source/LinqToDB/docs/transactions.md` | High | Planned | `DataConnection` transactions, `TransactionScope`, async flow, common mistakes. |
| [ ] | Connection lifetime | `Source/LinqToDB/docs/connection-lifetime.md` | High | Planned | `DataConnection` vs `DataContext`, session state, disposal, pooling assumptions. |
| [ ] | Generated SQL inspection | `Source/LinqToDB/docs/generated-sql.md` | Medium | Planned | How to inspect SQL, logging hooks, `ToString()` caveats if applicable. |
| [ ] | Logging and diagnostics | `Source/LinqToDB/docs/diagnostics.md` | Medium | Planned | Tracing, errors, common exception interpretation. |
| [ ] | DDL and schema APIs | `Source/LinqToDB/docs/schema.md` | High | Planned | `CreateTable`, `DropTable`, table options, metadata required for generated DDL. |
| [ ] | Schema provider and scaffolding | `Source/LinqToDB/docs/schema-provider.md` | Medium | Planned | Reading database schema metadata, scaffolding-generated mappings, and T4 templates. |
| [ ] | Provider-specific APIs | `Source/LinqToDB/docs/provider-specific-apis.md` | Medium | Planned | `SqlServerTools`, provider namespaces, typed helper APIs. |
| [ ] | Provider guides overview | `Source/LinqToDB/docs/providers/providers.md` | Medium | Planned | Router for provider-specific setup, dialects, capabilities, and caveats. |
| [ ] | Access guide | `Source/LinqToDB/docs/providers/access.md` | Low | Planned | JET/ACE, OLE DB/ODBC, .mdb/.accdb, platform caveats. |
| [ ] | SQL Server guide | `Source/LinqToDB/docs/providers/sql-server.md` | Medium | Planned | SQL Server-specific setup, hints, OUTPUT, identity, collation. |
| [ ] | PostgreSQL guide | `Source/LinqToDB/docs/providers/postgresql.md` | Medium | Planned | Arrays, JSON, RETURNING, CTE/materialized hints. |
| [ ] | SQLite guide | `Source/LinqToDB/docs/providers/sqlite.md` | Medium | Planned | In-memory DB, limitations, type affinity. |
| [ ] | MySQL / MariaDB guide | `Source/LinqToDB/docs/providers/mysql.md` | Medium | Planned | Dialect, hints, bulk copy, generated SQL differences. |
| [ ] | Oracle guide | `Source/LinqToDB/docs/providers/oracle.md` | Medium | Planned | Sequences, hints, MERGE, identity, parameter details. |
| [ ] | ClickHouse guide | `Source/LinqToDB/docs/providers/clickhouse.md` | Low | Planned | Insert/query limitations, engine-specific behavior. |
| [ ] | DB2 guide | `Source/LinqToDB/docs/providers/db2.md` | Low | Planned | LUW/zOS selection, driver notes, SQL dialect caveats. |
| [ ] | Firebird guide | `Source/LinqToDB/docs/providers/firebird.md` | Low | Planned | Version selection, RETURNING, identity/sequence behavior. |
| [ ] | Informix guide | `Source/LinqToDB/docs/providers/informix.md` | Low | Planned | Native/DB2 provider choice and dialect limitations. |
| [ ] | SAP HANA guide | `Source/LinqToDB/docs/providers/sap-hana.md` | Low | Planned | Driver notes, SQL dialect, identity/sequence behavior. |
| [ ] | SQL Server CE guide | `Source/LinqToDB/docs/providers/sql-ce.md` | Low | Planned | .NET Framework-only constraints and limited SQL support. |
| [ ] | Sybase / SAP ASE guide | `Source/LinqToDB/docs/providers/sybase.md` | Low | Planned | ASE driver, identity, SQL dialect caveats. |
| [ ] | YDB guide | `Source/LinqToDB/docs/providers/ydb.md` | Low | Planned | `YdbTools.CreateDataConnection(...)`, no `UseYdb()`, YDB-specific hints. |
| [ ] | Async operations | `Source/LinqToDB/docs/async.md` | High | Planned | `LinqToDB.Async`, cancellation, streaming/materialization distinctions. |
| [ ] | Streaming and low-level reads | `Source/LinqToDB/docs/data-reader.md` | Medium | Planned | DataReader-style APIs and resource lifetime. |
| [ ] | Window functions | `Source/LinqToDB/docs/window-functions.md` | Medium | Planned | Analytic functions and provider support. |
| [ ] | SQL expressions | `Source/LinqToDB/docs/sql-expressions.md` | Medium | Planned | `Sql.*` helpers, server-side expressions, custom methods boundary. |
| [ ] | JSON and XML | `Source/LinqToDB/docs/json-xml.md` | Medium | Planned | Provider-specific JSON/XML mappings and functions. |
| [ ] | Temporal/date-time behavior | `Source/LinqToDB/docs/date-time.md` | Medium | Planned | Date/time translation, offsets, provider precision. |
| [ ] | Identity and sequences | `Source/LinqToDB/docs/identity-sequences.md` | Medium | Planned | Identity retrieval, sequences, provider differences. |
| [ ] | Optimistic concurrency | `Source/LinqToDB/docs/concurrency.md` | Medium | Planned | Version columns, conditional updates, no change tracking. |
| [ ] | Table expressions and views | `Source/LinqToDB/docs/table-expressions.md` | Medium | Planned | Views, table functions, expression-backed tables. |
| [ ] | Multi-table DML | `Source/LinqToDB/docs/multi-table-dml.md` | Medium | Planned | Provider-specific update/delete shapes. |
| [ ] | Performance guidance | `Source/LinqToDB/docs/performance.md` | Medium | Planned | Query cache, mapping schema reuse, N+1 risks, bulk operations. |
| [ ] | Testing LinqToDB integrations | `Source/LinqToDB/docs/testing-integrations.md` | Low | Planned | Provider test strategy for package users. |
| [ ] | Troubleshooting | `Source/LinqToDB/docs/troubleshooting.md` | High | Planned | Symptom-to-guide index for common errors. |

---

## Documentation Inbox

Use this section for important AI-agent guidance that does not yet have an obvious final guide.
Do not leave items here forever: during documentation work, either move each item into a target
guide or turn it into a planned checklist row.

| Topic | Likely target | Note |
|---|---|---|
| SQL null comparison generation | `Source/LinqToDB/docs/null-semantics.md` or `Source/LinqToDB/docs/generated-sql.md` | `Field == @p` can generate `Field IS NULL` when the parameter value is `null`; this preserves SQL semantics but may surprise agents inspecting generated SQL. |
| C# null semantics for nullable object comparisons | `Source/LinqToDB/docs/null-semantics.md` | When both compared values can be `null`, generated SQL can include extra null checks to preserve C# equality semantics. Document why the SQL is intentionally more complex. |

---

## How To Add A New Area

1. Add a row to `Coverage Checklist` with `Done` unchecked and status `Planned`.
2. Create the guide under `Source/LinqToDB/docs`.
3. Add the standard opening block.
4. Add minimal examples and common mistakes.
5. Link related guides.
6. Add the guide to `Source/LinqToDB/AGENT_GUIDE.md` if package users should route through it.
7. Change the checklist row to `[x]` only after the definition of done is met.

---

## Status Values

| Status | Meaning |
|---|---|
| Planned | No dedicated AI-friendly guide yet. |
| Existing / needs audit | A guide exists, but it has not been reviewed against the current AI-friendly definition of done. |
| Partial | A guide exists and covers some important cases, but known gaps remain. |
| Done | The guide was reviewed and meets the definition of done for the current documentation layer. |
| Deferred | Intentionally postponed; keep a note explaining why. |

