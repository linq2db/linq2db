---
area: EFCORE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# EFCORE -- GitHub themes

## Open themes

- **Query logging and diagnostics** -- Gaps in visualizing generated SQL and excluding sensitive data from logs within EF Core integration contexts. Issues: #4645 (query logging visibility), #4651 (sensitive data exclusion in logs). Documentation and tooling improvements needed for debugging linq2db queries via EF Core.

- **Include and navigation loading** -- Challenges with eager loading of related entities and collections in entity hierarchies, especially with inheritance patterns. Examples: #4012 (eager include to CTE), #4628 (inherited navigation populations). Recurring pattern in complex entity graphs.

- **Mapping incompatibilities with inheritance** -- EF Core TPH (Table Per Hierarchy) discriminator handling and inherited property materialization not translating correctly. Issues: #4644 (inherited properties unnecessarily in SQL), #4666 (Merge into TPH table with discriminator), #4628 (inherited Include interaction). Entity construction and calculated column expansion also affected (#5578 PR).

- **EF Core integration lifecycle gaps** -- Connection leak and transaction/state management issues when bridging EF Core contexts to linq2db queries. Issues: #5364 (connection leak in ToLinqToDB), #4653 (transaction assignment on pre-created queryables), #4649 (Set() method tracking semantics). Core integration points breaking expected context lifecycle.

- **Provider support and documentation** -- Limited provider support documentation and configuration gaps for EF Core integration. Issue #4611 tasks review of supported providers list and testing matrix.

## Resolved themes

- **Include/navigation loading** -- Multiple closed issues (#61, #1149, #2172) demonstrate resolved patterns for navigation property loading and query filters.

- **Soft delete and query filters** -- Closed issues (#1626) show integration patterns with EF Core's query filters and soft-delete patterns.

- **Complex type mapping** -- Several resolved tickets (#1463, #2082) document support for complex/owned types with field-level mapping.

- **OData interoperability** -- Closed issues (#371) show fixes for OData-over-linq2db query patterns and LINQ expression translation.

- **Concurrency and versioning** -- Issue #553 resolved with optimistic concurrency support patterns documented.

## Active discussions

- [AsNoTracking in Linq2db](https://github.com/linq2db/linq2db/discussions/3116) -- [Q&A] Tracking and change-detection patterns with linq2db.

- [EF Core 6 VS linq2db benchmarks](https://github.com/linq2db/linq2db/discussions/3393) -- [General] Performance comparison analysis.

- [Dynamically generating nested queries](https://github.com/linq2db/linq2db/discussions/3490) -- [Q&A] Dynamic query composition patterns.

- [Usage of IDbContextFactory](https://github.com/linq2db/linq2db/discussions/3928) -- [Q&A] Context lifecycle and dependency injection.

- [Add values from a dictionary when doing CRUD operations](https://github.com/linq2db/linq2db/discussions/4148) -- [Ideas] Bulk operations from dynamic sources.

- [[Docs] Comparison with Entity Framework](https://github.com/linq2db/linq2db/discussions/4288) -- [General] Feature comparison and gap documentation.

- [How to set DateTimeKind per property?](https://github.com/linq2db/linq2db/discussions/4238) -- [Q&A] Type-specific property configuration.

- [How to use TIME ZONE AT?](https://github.com/linq2db/linq2db/discussions/4688) -- [Q&A] Temporal type handling in mapping schema.

- [Is there a way to provide a custom SQL query for an entity?](https://github.com/linq2db/linq2db/discussions/4692) -- [Ideas] ToSqlQuery equivalent for static entity projections.

- [Configure Linq2DB.EntityFrameworkCore to respect shadow Discriminator property](https://github.com/linq2db/linq2db/discussions/5477) -- [Q&A] Shadow properties in inheritance hierarchies.

## Stats

- Open issues: 10
- Closed issues: 60
- Open PRs: 1
- Total PRs: 1
- Discussions: 10
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 21 (10 issues + 1 PR + 10 discussions)
- Themes extracted: 5
</details>
