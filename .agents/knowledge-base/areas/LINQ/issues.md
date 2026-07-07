---
area: LINQ
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# LINQ -- GitHub themes

## Open themes

- **Inheritance and polymorphic queries** -- #4585, #4620, #4773 show recurring exceptions when entity hierarchies (base/derived classes, interface-based associations) are used. Child-type associations fail (#4585), unions of interface-implementing types (#4620), and casting-to-interface in SQL generation (#4773) each block real-world patterns. 3 open issues.

- **Query projection optimization** -- #4568, #5303 indicate that composite property selection and field-wise projection choices can trigger performance regressions or incorrect result counts: selecting all columns when only a subset is needed (#4568), or different result counts based on which properties are projected (#5303). Both have tests; both are open.

- **Configuration and data-context APIs** -- #4039, #5195, #5253 span parameter logging (raw vs masked in `.UseDefaultLogging`), context replacement for reusable `IQueryable` pipelines, and LINQPad configuration. Users need more control over data-context lifetime and introspection. 3 open.

- **Version-specific regressions** -- #4624 (3.0.0-rc0 + EF Core 3.2.0 null-ref), #5560 (DateTime.Date under MSSQL extensions) are version-bound failures. #5303 also marks a 6.0.0 regression. 2--3 open issues.

- **Provider-specific features** -- #1014 is a voting thread for new RDBMS platforms, #5171 requests Clickhouse tuple support for Oracle-style `IN (tuple)` clauses. Niche but request-backed. 2 open.

- **Schema and infrastructure improvements** -- #5403 (migrate raw SQL in SchemaProvider to LINQ queries), #5455 (support new .NET 11 LINQ APIs) reflect ongoing platform and infrastructure work. 2 open.

- **Optimistic concurrency & write-back** -- #5650 proposes a `.UseConcurrencyField()` / `.Fetch()` builder pattern for Update operations with optimistic locking and read-back semantics. 1 open (candidate for next-version feature gate).

## Open PRs

- **#3367** (draft) -- Address multiple issues with SET queries. Support more than two set operations in sequence without wrapping them into subqueries when operations are identical.
- **#5614** (draft) -- Parallel test execution across database providers. Note: marked merged but state=open; likely awaiting validation fix.
- **#5376** (ready) -- Add package-local linq2db AI skill and Expert knowledge pack. Fixes #4437.
- **#5673** (draft) -- Unify combined-command execution and eager loading. Adds automatic mapping of F# `'T option` columns to the `linq2db.FSharp` package.

## Resolved themes

- **Union and set-operation associations** -- 11+ closed issues (sample: #2503, #2505, #2619, #2932, #2966, #3323, #3346, #3669). Unions, concats, and set operations with entities, calculated fields, and associations were historically broken or threw during aggregation; now mostly fixed.

- **Inheritance edge cases** -- 6+ closed (sample: #292, #1008, #1017, #2161, #3034, #5274). Base-class load queries, derived-class filters in `LoadWith`, discriminator ambiguity, and interface-inheritance mapping have been addressed.

- **Calculated fields and projection stability** -- #3150, #2461 were about nested projections losing constants or throwing on union+calculated-field; now resolved.

## Active discussions

- No active discussions in the LINQ area.

## Stats

- Open issues: 16
- Closed issues: 35+
- Open PRs: 4
- Total PRs: 19+
- Discussions: 0
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 20+ (16 open issues + 4 open PRs)
- Themes extracted: 7 open + 3 resolved

</details>
