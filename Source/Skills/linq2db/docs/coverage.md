# AI Documentation Coverage

> Before using this guide:
> - apply global rules from [`SKILL.md`](../SKILL.md);
> - for exact API names/signatures, verify against [`docs/api.md`](api.md) and `lib/<TFM>/linq2db.xml`.

This file records which LinqToDB topics have package-local AI guidance and which topics still
require direct API/XML-doc lookup.

## Covered Topics

These areas have task-focused markdown guidance in this package:

- provider setup and required ADO.NET driver packages;
- architecture, translation pipeline, and execution model;
- configuration, `DataOptions`, logging, retry, interceptors, and member translators;
- mapping basics, `MappingSchema`, attributes/fluent mapping, and DDL-sensitive column metadata;
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

- associations in depth;
- eager loading and graph materialization in depth;
- stored procedures and functions in depth;
- schema provider APIs;
- scaffolding, T4 templates, and CLI workflows in depth;
- diagnostics and SQL logging beyond the basic tracing pattern;
- transactions beyond core lifetime and `TransactionScope` pitfalls;
- provider-specific advanced behavior outside documented setup/capability/hint surfaces;
- advanced expression builder and SQL builder internals.

## Rule For Uncovered Topics

If a topic is not covered by markdown guidance, do not infer LinqToDB API shape or behavior from
memory. Search `docs/api.md` by task terms, member names, provider names, receiver types, and
AI-Tags, then verify exact members in `lib/<TFM>/linq2db.xml`.

Do not use `LinqToDB.Internal.*` APIs in application code while investigating uncovered topics.
They are implementation details even when visible as public members.
