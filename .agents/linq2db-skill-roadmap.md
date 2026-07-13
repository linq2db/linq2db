# linq2db Skill Roadmap

This repository-local file tracks coverage work for the NuGet-shipped linq2db skill pack.
It is not package documentation, not Custom GPT instructions, and not a second source of truth.

Canonical maintenance and layout references:

- `Source/Skills/README.md` - shipped skill-pack layout and package rules.
- `Source/Knowledge/README.md` - generated Expert knowledge-pack layout and source-of-truth rules.
- `.agents/knowledge-pack-maintenance.md` - mechanical Expert pack rebuild and validation procedure.

Use this file only to track planned, partial, completed, or deferred skill documentation areas.
When an item becomes package guidance, write it under `Source/Skills/linq2db/` first and regenerate
derived artifacts when needed.

---
## Coverage Checklist

| Done | Area | Target doc | Priority | Status | Notes |
|---|---|---|---|---|---|
| [x] | Agent entry point | `Source/Skills/linq2db/SKILL.md` | Critical | Done | Mandatory package entry point for agents. |
| [x] | Agent skill entry point | `Source/Skills/linq2db/SKILL.md` | Critical | Done | Compatibility entry point for agent skill systems; routes to `SKILL.md`, task guides, generated API discovery, and raw XML-doc confirmation when needed. |
| [x] | API discovery | `Source/Skills/linq2db/docs/api.md` | Critical | Done | General rules for finding exact API members through the generated API index and raw XML-doc confirmation when needed. |
| [x] | Architecture overview | `Source/Skills/linq2db/docs/architecture.md` | Critical | Done | Core reference; already linked from `SKILL.md`. |
| [x] | Agent anti-patterns | `Source/Skills/linq2db/docs/agent-antipatterns.md` | Critical | Done | Quick symptom index and wrong/correct examples. |
| [x] | AI metadata governance | `Source/Skills/linq2db/docs/ai-tags.md` | High | Done | Selective coverage policy is documented; generated API and Expert pack generators validate `<ai-tags />` / `<ai-tags-defaults />` keys and controlled values. |
| [x] | Mapping | `Source/Skills/linq2db/docs/mapping.md` | High | Done | Conventions, attributes, fluent mapping, `MappingSchema`, DDL metadata, `DataType`, `DbType`, conversions, associations. |
| [ ] | Mapping `Configuration` selector | `Source/Skills/linq2db/docs/mapping-configuration.md` | High | Planned | Attribute/fluent metadata selected by mapping schema configuration names; underused provider- or environment-specific mapping mechanism. |
| [x] | Configuration and `DataOptions` | `Source/Skills/linq2db/docs/configuration.md` | High | Done | Audited against source: connection setup, tracing (`TraceLevel` behavior corrected), retry (`IRetryPolicy` method count corrected), external config files (JSON/`ILinqToDBSettings`, legacy `app.config`/`linq2db.Compat` added), interceptors/member-translator sections trimmed to `interceptors.md`/`extensions.md`, mapping schema, `UseOptions` scope rules (`LinqToDBException` on connection-identity changes; broken `UseCommandTimeout` example fixed), `DataProviderFactory`. |
| [x] | Provider setup | `Source/Skills/linq2db/docs/provider-setup.md` | High | Done | Audited against source: fixed non-compiling `UseYdb()`/`UseTransaction`/bulk-copy-callback examples, fabricated `UseAccess(AccessVersion, ...)` signature, added missing PostgreSQL 11/12/19 constants/dialects and Oracle `Devart.Data.Oracle` package, clarified SQL Server CE TFM-restriction source. |
| [x] | Provider capabilities | `Source/Skills/linq2db/docs/provider-capabilities.md` | High | Done | Audited against `SqlProviderFlags`/builders: fixed wrong DuckDB MERGE and SAP HANA Upsert cells, added missing MariaDB/Oracle APPLY version qualifiers, narrowed overstated Oracle OUTPUT/RETURNING claim, fixed fabricated `LinqExtensions.Output()`/`OutputInto()` (real API is `*WithOutput*` family) and wrong `InsertOrUpdate()` class attribution, footnoted Access/Informix Upsert emulation fallback. |
| [x] | CRUD overview | `Source/Skills/linq2db/docs/crud/crud.md` | High | Done | Router for CRUD guides; audited alongside the full crud/*.md family. |
| [x] | SELECT basics | `Source/Skills/linq2db/docs/crud/crud-select.md` | High | Done | Query materialization and async namespace checks; mostly generic LINQ, low API-signature risk, no defects found. |
| [x] | INSERT | `Source/Skills/linq2db/docs/crud/crud-insert.md` | High | Done | Insert routes to value/select variants; sub-router, no defects found. |
| [x] | INSERT values | `Source/Skills/linq2db/docs/crud/crud-insert-values.md` | High | Done | Entity and fluent value inserts; audited against source, fixed `InsertWithOutput` provider list (added DuckDB, YDB). |
| [x] | INSERT from SELECT | `Source/Skills/linq2db/docs/crud/crud-insert-select.md` | High | Done | Insert from query patterns; audited against source, fixed `InsertWithOutput` provider list and a non-compiling `MultiInsert` example (`.Into`/`.When` are mutually exclusive chains; added missing `InsertAll`/`InsertFirst`/`Else`). |
| [x] | UPDATE | `Source/Skills/linq2db/docs/crud/crud-update.md` | High | Done | Entity and fluent update patterns; audited against source, all claims confirmed accurate including the PostgreSQL/SQLite `UpdateWithOutput` nuance. |
| [x] | DELETE | `Source/Skills/linq2db/docs/crud/crud-delete.md` | High | Done | Delete patterns and safeguards; audited against source, all claims confirmed accurate. |
| [x] | Upsert | `Source/Skills/linq2db/docs/crud/crud-upsert.md` | High | Done | `InsertOrReplace` and related caveats; audited against source, all claims confirmed accurate. |
| [x] | MERGE | `Source/Skills/linq2db/docs/crud/crud-merge.md` | High | Done | Merge builder plus merge-specific hints; audited against source, fixed stale "SQL Server only" label for `WhenNotMatchedBySource*` (PostgreSQL 17+ also supports it) and imprecise throw-timing wording. |
| [x] | Bulk copy | `Source/Skills/linq2db/docs/crud/crud-bulkcopy.md` | High | Done | Bulk insert behavior and provider support; audited against source, added ClickHouse to the `BulkCopyTimeout` provider list. |
| [ ] | CTE | `Source/Skills/linq2db/docs/query-cte.md` | High | Existing / needs audit | `.AsCte()` and recursive CTE patterns. |
| [x] | Temporary tables | `Source/Skills/linq2db/docs/query-temp-tables.md` | High | Done | `TempTable<T>`, `CreateTempTable`, `DataConnection` requirement, `TableOptions`, and `setTable` mapping schema rules. |
| [ ] | Translatable methods | `Source/Skills/linq2db/docs/translatable-methods.md` | Medium | Existing / needs audit | String, Math, DateTime translation guidance. |
| [ ] | Extension mechanisms | `Source/Skills/linq2db/docs/extensions.md` | High | Existing / needs audit | SQL function/expression mapping plus `IMemberTranslator` member translators (moved here from `configuration.md`). |
| [ ] | Interceptors | `Source/Skills/linq2db/docs/interceptors.md` | Medium | Existing / needs audit | Callback choice and registration. |
| [x] | Hints | `Source/Skills/linq2db/docs/hints.md` | High | Done | General raw-text hints, provider-specific `AsXxx()` typed hint APIs, unsupported provider gaps, safe multi-provider branches, merge hints, generated provider hint `<ai-tags />` via T4 and handwritten provider hint `<ai-tags />` in XML docs. |
| [x] | Provider hints API map | `Source/Skills/linq2db/docs/hints-api-map.md` | High | Done | Reverse lookup from concrete provider SQL hint text to typed provider-specific helper APIs, generated from XML-doc-shaped source comments. |
| [x] | Provider hint gaps | `Source/Skills/linq2db/docs/hints.md` | Medium | Done | DB2 optimization guidelines, Firebird `PLAN`, Informix directives, SAP HANA `WITH HINT`, and Sybase dialect hints are documented as unsupported/gap areas; implementation is out of scope. |
| [ ] | Joins - translation gotchas | `Source/Skills/linq2db/docs/query-joins.md` | High | Planned | Not a LINQ-join syntax tutorial (agents already know that). Navigation-like/association joins, cases that fail to translate, and provider limitations only. |
| [ ] | Grouping and aggregation | `Source/Skills/linq2db/docs/query-grouping.md` | High | Planned | Translation restrictions and gotchas only (what fails to translate, HAVING/projection limits after `GroupBy`) - not `GroupBy` syntax itself. |
| [ ] | Ordering and paging | `Source/Skills/linq2db/docs/query-paging.md` | High | Planned | `TakeHints` and provider-specific paging gotchas only - not `OrderBy`/`Skip`/`Take` syntax itself. |
| [ ] | Set operations | `Source/Skills/linq2db/docs/query-set-operations.md` | Medium | Planned | `Concat`, `Union`, `Except`, `Intersect`, provider differences. |
| [ ] | Projections | `Source/Skills/linq2db/docs/query-projections.md` | High | Planned | DTO projections, computed values, nested projections, materialization traps. |
| [x] | Null semantics | `Source/Skills/linq2db/docs/null-semantics.md` | High | Done | `CompareNulls` (`LikeClr`/`LikeSql`/`LikeSqlExceptParameters`), literal-null and parameter-null handling, the both-operands-nullable condition for the `OR (... IS NULL AND ...)` expansion, and manual control (`AsNotNull`/`AsNotNullable`, `IsDistinctFrom`/`IsNotDistinctFrom`, `ToNullable`/`ToNotNull`/`ToNotNullable`/`AsNullable`). Coalesce (`??`) translation not covered - add if it comes up. |
| [x] | Associations and eager loading | `Source/Skills/linq2db/docs/associations.md` | High | Done | `[Association]`, fluent associations, `LoadWith`, `ThenLoad`, nullability, predicates/query expressions, eager-loading strategies, implicit collection loading guard, and no lazy loading. |
| [ ] | Inheritance mapping | `Source/Skills/linq2db/docs/inheritance-mapping.md` | Medium | Planned | Discriminators, inheritance attributes, query behavior. |
| [ ] | Advanced value converters | `Source/Skills/linq2db/docs/value-conversions.md` | Low | Deferred | Only if mapping guide becomes insufficient; would cover `IValueConverter`, null handling, provider types, and reusable converter patterns. |
| [ ] | Custom mapping metadata | `Source/Skills/linq2db/docs/custom-mapping-metadata.md` | Low | Deferred | Metadata readers and custom mapping attributes; document only if package users have a real extension scenario. |
| [x] | Raw SQL | `Source/Skills/linq2db/docs/raw-sql.md` | High | Done | `FromSql`, `FromSqlScalar`, `RawSqlString`, `SetCommand`, `CommandInfo`, `ToSqlQuery`, `QuerySql`, parameters, composability boundaries, and alias placement. |
| [x] | Parameters | `Source/Skills/linq2db/docs/parameters.md` | High | Done | `DataParameter` factories vs `Create()` type-inference mismatches, output/input-output parameters and the `QueryProc` deferred-execution gotcha, `Sql.Parameter`/`Sql.Constant`/`InlineParameters`/`ToSql`/`AsSql` for forcing parameter-vs-literal, and an explicit "do not invent" rule for automatic parameterization outside those APIs. |
| [ ] | Transactions | `Source/Skills/linq2db/docs/transactions.md` | High | Planned | `DataConnection` transactions, `TransactionScope`, async flow, common mistakes. |
| [ ] | Connection lifetime | `Source/Skills/linq2db/docs/connection-lifetime.md` | High | Planned | `DataConnection` vs `DataContext`, session state, disposal, pooling assumptions. |
| [ ] | Generated SQL inspection | `Source/Skills/linq2db/docs/generated-sql.md` | Medium | Planned | How to inspect SQL, logging hooks, `ToString()` caveats if applicable. |
| [ ] | Logging and diagnostics | `Source/Skills/linq2db/docs/diagnostics.md` | Medium | Planned | Tracing, errors, common exception interpretation. |
| [ ] | DDL and schema APIs | `Source/Skills/linq2db/docs/schema.md` | High | Planned | `CreateTable`, `DropTable`, table options, metadata required for generated DDL. |
| [ ] | Schema provider and scaffolding | `Source/Skills/linq2db/docs/schema-provider.md` | Medium | Planned | Reading database schema metadata, scaffolding-generated mappings, and T4 templates. |
| [ ] | Provider-specific APIs | `Source/Skills/linq2db/docs/provider-specific-apis.md` | Medium | Planned | `SqlServerTools`, provider namespaces, typed helper APIs. |
| [ ] | Provider guides overview | `Source/Skills/linq2db/docs/providers/providers.md` | Medium | Planned | Router for provider-specific setup, dialects, capabilities, and caveats. |
| [ ] | Access guide | `Source/Skills/linq2db/docs/providers/access.md` | Low | Planned | JET/ACE, OLE DB/ODBC, .mdb/.accdb, platform caveats. |
| [ ] | SQL Server guide | `Source/Skills/linq2db/docs/providers/sql-server.md` | Medium | Planned | SQL Server-specific setup, hints, OUTPUT, identity, collation. |
| [ ] | PostgreSQL guide | `Source/Skills/linq2db/docs/providers/postgresql.md` | Medium | Planned | Arrays, JSON, RETURNING, CTE/materialized hints. |
| [ ] | SQLite guide | `Source/Skills/linq2db/docs/providers/sqlite.md` | Medium | Planned | In-memory DB, limitations, type affinity. |
| [ ] | MySQL / MariaDB guide | `Source/Skills/linq2db/docs/providers/mysql.md` | Medium | Planned | Dialect, hints, bulk copy, generated SQL differences. |
| [ ] | Oracle guide | `Source/Skills/linq2db/docs/providers/oracle.md` | Medium | Planned | Sequences, hints, MERGE, identity, parameter details. |
| [ ] | ClickHouse guide | `Source/Skills/linq2db/docs/providers/clickhouse.md` | Low | Planned | Insert/query limitations, engine-specific behavior. |
| [ ] | DB2 guide | `Source/Skills/linq2db/docs/providers/db2.md` | Low | Planned | LUW/zOS selection, driver notes, SQL dialect caveats. |
| [ ] | Firebird guide | `Source/Skills/linq2db/docs/providers/firebird.md` | Low | Planned | Version selection, RETURNING, identity/sequence behavior. |
| [ ] | Informix guide | `Source/Skills/linq2db/docs/providers/informix.md` | Low | Planned | Native/DB2 provider choice and dialect limitations. |
| [ ] | SAP HANA guide | `Source/Skills/linq2db/docs/providers/sap-hana.md` | Low | Planned | Driver notes, SQL dialect, identity/sequence behavior. |
| [ ] | SQL Server CE guide | `Source/Skills/linq2db/docs/providers/sql-ce.md` | Low | Planned | .NET Framework-only constraints and limited SQL support. |
| [ ] | Sybase / SAP ASE guide | `Source/Skills/linq2db/docs/providers/sybase.md` | Low | Planned | ASE driver, identity, SQL dialect caveats. |
| [ ] | YDB guide | `Source/Skills/linq2db/docs/providers/ydb.md` | Low | Planned | `YdbTools.CreateDataConnection(...)`, no `UseYdb()`, YDB-specific hints. |
| [ ] | Async operations | `Source/Skills/linq2db/docs/async.md` | High | Planned | `LinqToDB.Async`, cancellation, streaming/materialization distinctions. |
| [ ] | Streaming and low-level reads | `Source/Skills/linq2db/docs/data-reader.md` | Medium | Planned | DataReader-style APIs and resource lifetime. |
| [ ] | Window functions | `Source/Skills/linq2db/docs/window-functions.md` | Low | Deferred | Current API is expected to be superseded by an alternative API; revisit after that API is available. |
| [ ] | Compiled queries | `Source/Skills/linq2db/docs/compiled-queries.md` | Medium | Planned | `CompiledQuery` hot-path query caching guidance; recognized in coverage but not yet documented in depth. |
| [ ] | Metrics and activity instrumentation | `Source/Skills/linq2db/docs/metrics.md` | Medium | Planned | `Metrics`, `ActivityService`, `IActivity`, and how this differs from basic SQL tracing/interceptors. |
| [ ] | Remote data contexts | `Source/Skills/linq2db/docs/remote.md` | Medium | Planned | `RemoteDataContextBase`, `ILinqService`, and `LinqToDB.Remote.*` package boundary guidance. |
| [ ] | SQL expressions | `Source/Skills/linq2db/docs/sql-expressions.md` | Medium | Planned | `Sql.*` helpers, server-side expressions, custom methods boundary. |
| [ ] | JSON and XML | `Source/Skills/linq2db/docs/json-xml.md` | Medium | Planned | Provider-specific JSON/XML mappings and functions. |
| [ ] | Temporal/date-time behavior | `Source/Skills/linq2db/docs/date-time.md` | Medium | Planned | Date/time translation, offsets, provider precision. |
| [ ] | Identity and sequences | `Source/Skills/linq2db/docs/identity-sequences.md` | Medium | Planned | Identity retrieval, sequences, provider differences. |
| [x] | Optimistic concurrency | `Source/Skills/linq2db/docs/concurrency.md` | Medium | Done | `UpdateOptimistic`, `DeleteOptimistic`, `WhereKeyOptimistic`, `OptimisticLockPropertyAttribute`, `OptimisticLockPropertyBaseAttribute`, `VersionBehavior`, primary-key requirement, and affected-row-count handling. |
| [ ] | Table expressions and views | `Source/Skills/linq2db/docs/table-expressions.md` | Medium | Planned | Views, table functions, expression-backed tables. |
| [ ] | Multi-table DML | `Source/Skills/linq2db/docs/multi-table-dml.md` | Medium | Planned | Provider-specific update/delete shapes. |
| [ ] | Extension package boundaries | `Source/Skills/linq2db/docs/extension-packages.md` | Low | Planned | Clarify scope for `LinqToDB.EntityFrameworkCore`, `LinqToDB.AspNet`, `LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `LinqToDB.CLI`, and `LinqToDB.FSharp`. |
| [ ] | Compatibility namespaces | `Source/Skills/linq2db/docs/compatibility.md` | Low | Deferred | `LinqToDB.Compatibility.*` legacy surface; document only if agents start using it incorrectly. |
| [ ] | Performance guidance | `Source/Skills/linq2db/docs/performance.md` | Medium | Planned | Query cache, mapping schema reuse, N+1 risks, bulk operations. |
| [ ] | Testing LinqToDB integrations | `Source/Skills/linq2db/docs/testing-integrations.md` | Low | Planned | Provider test strategy for package users. |
| [ ] | Troubleshooting | `Source/Skills/linq2db/docs/troubleshooting.md` | High | Planned | Symptom-to-guide index for common errors. |

---

## Documentation Inbox

Use this section for important AI-agent guidance that does not yet have an obvious final guide.
Do not leave items here forever: during documentation work, either move each item into a target
guide or turn it into a planned checklist row.

| Topic | Likely target | Note |
|---|---|---|
| Generated SQL inspection behavior | `Source/Skills/linq2db/docs/generated-sql.md` | Black-box tests show agents may invent claims such as "local variables are inlined" when discussing SQL Server plan/cache issues. The parameterization/inlining API side (`Sql.Parameter`, `Sql.Constant`, `InlineParameters`, `ToSql`, `AsSql`) is now covered in `docs/parameters.md`, including an explicit "do not invent" rule for behavior outside those APIs. This row is only about the still-missing generated-SQL-inspection guide (how to view/log the actual SQL text, `ToString()` caveats). |

---

## How To Add A New Area

1. Add a row to `Coverage Checklist` with `Done` unchecked and status `Planned`.
2. Create the guide under `Source/Skills/linq2db/docs`.
3. Add the standard opening block.
4. Add minimal examples and common mistakes.
5. Link related guides.
6. Add the guide to `Source/Skills/linq2db/SKILL.md` if package users should route through it.
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

