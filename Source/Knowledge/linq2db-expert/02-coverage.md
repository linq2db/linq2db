<!-- Generated from: Source/Skills/linq2db/docs/coverage.md -->

# AI Documentation Coverage

> Before using this guide:
> - apply global rules from [`SKILL.md`](01-skill.md);
> - for exact API names/signatures, search [`docs/api.md`](04-api-discovery-and-extract.md) first, then use
>   `lib/<TFM>/linq2db.xml` when the generated extract is not detailed enough.

This file records which LinqToDB topics have package-local AI guidance and which topics still
require generated API index lookup or raw XML-doc confirmation.

## Covered Topics

These areas have task-focused markdown guidance in this package:

- provider setup and required ADO.NET driver packages;
- architecture, translation pipeline, and execution model;
- configuration, `DataOptions`, logging, retry, interceptors, and member translators;
- mapping basics, `MappingSchema`, attributes/fluent mapping, and DDL-sensitive column metadata;
- associations, fluent relationship mapping, `LoadWith` / `ThenLoad`, and eager-loading strategies;
- CRUD routing, select/insert/update/delete/upsert/bulk copy/MERGE;
- temporary tables and `CreateTempTable*` overload selection;
- CTEs and recursive queries;
- query/table/index/join/subquery/provider-specific hints;
- SQL hint reverse lookup through `docs/hints-api-map.md`;
- custom SQL expressions and functions;
- built-in translatable .NET methods;
- common anti-patterns and symptom-based checks.

## Not Yet Covered In Depth

These areas are not yet covered by a dedicated AI guide or are covered only indirectly:

- stored procedures and functions in depth;
- schema provider APIs;
- scaffolding, T4 templates, and CLI workflows in depth;
- diagnostics and SQL logging beyond the basic tracing pattern;
- transactions beyond core lifetime and `TransactionScope` pitfalls;
- provider-specific advanced behavior outside documented setup/capability/hint surfaces;
- advanced expression builder and SQL builder internals.

## Recognized But Not Yet Guided

These public or package-adjacent areas exist and should not be treated as unknown, but they do not
yet have task-focused package-local AI guidance. For these topics, search `docs/api.md` and then
`lib/<TFM>/linq2db.xml` for exact version-matched API details before answering:

- compiled queries: `CompiledQuery`;
- metrics and activity instrumentation: `Metrics`, `ActivityService`, `IActivity`;
- remote data contexts and service contracts: `RemoteDataContextBase`, `ILinqService`,
  `LinqToDB.Remote.*` packages;
- optimistic concurrency helpers: `UpdateOptimistic`, `OptimisticLockPropertyAttribute`;
- raw SQL query APIs: `FromSql`, `QuerySql`, `RawSqlString`;
- compatibility namespaces: `LinqToDB.Compatibility.*` legacy surface;
- analytic/window functions and string aggregate helpers: recognized, but dedicated guidance is
  deferred while the API shape is expected to evolve.

## Package Scope

This skill pack covers the core `linq2db` package. It does not provide dedicated task guides for
extension packages such as `LinqToDB.EntityFrameworkCore`, `LinqToDB.AspNet`,
`LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `LinqToDB.CLI`, or `LinqToDB.FSharp`.

For those packages, use their own package-local docs and XML documentation when available. If they
are not available, state that this skill covers the core package and separate any best-effort
guidance from package-confirmed core `linq2db` facts.

## Rule For Uncovered Topics

If a topic is not covered by markdown guidance, do not infer LinqToDB API shape or behavior from
memory. Search `docs/api.md` by task terms, member names, provider names, receiver types, and
AI metadata. Use `lib/<TFM>/linq2db.xml` as the version-matched primary reference only when the
generated extract is inconclusive or exact signature/remarks detail is required.

Do not use `LinqToDB.Internal.*` APIs in application code while investigating uncovered topics.
They are implementation details even when visible as public members.
