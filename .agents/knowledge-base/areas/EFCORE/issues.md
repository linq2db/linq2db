---
area: EFCORE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# EFCORE -- GitHub themes

## Open themes

- **EF Core integration issues** -- Connection leaks and transaction handling gaps when bridging EF Core contexts to linq2db queries. Key issues: #5364 (connection leak in ToLinqToDB), #5296 (SocketException on upgrade), #4877 (TransactionScope and prepared transactions).

- **Mapping incompatibilities** -- EF Core model features not translating correctly through the linq2db bridge. Including: #4662 (HasConversion<string> ignored for enums), #4663 (BulkCopy with ComplexProperty fails), #4640 (JSON column properties null on merge).

- **CTE and recursive query limitations** -- Challenges integrating EF Core features (like recursive CTEs) with linq2db generated queries. Issue #4603 shows ThenInclude in recursive CTE scenarios.

- **Query generation and optimization** -- Inefficient SQL or missed optimization opportunities. Example: #4600 (Collection.Contains generates inline parameters instead of IN expressions).

- **EF Core complex type support** -- Mapping EF Core's complex/owned types to linq2db's property mapping model remains problematic.

- **Concurrency and versioning** -- Explicit concurrency checks and conflict detection patterns similar to EF Core's RowVersion semantics (issue #4405).

## Resolved themes

- **Include/navigation loading** -- Multiple closed issues (#61, #1149, #2172) show resolved patterns for navigation property loading.

- **Soft delete and query filters** -- Closed issues (#1626) demonstrate soft-delete integration patterns with EF Core's query filters.

- **Complex type mapping** -- Several resolved tickets (#1463, #2082) document workarounds and eventual support for complex/owned types.

- **OData interoperability** -- Closed issues (#371) show fixes for OData-over-linq2db query patterns.

## Active discussions

- [AsNoTracking in Linq2db](https://github.com/linq2db/linq2db/discussions/3116) -- [Q&A] How to use AsNoTracking patterns with linq2db queries.

- [EF Core 6 VS linq2db benchmarks](https://github.com/linq2db/linq2db/discussions/3393) -- [General] Performance comparison between direct EF Core and the linq2db bridge.

- [Dynamically generating nested queries](https://github.com/linq2db/linq2db/discussions/3490) -- [Q&A] Patterns for building nested/subquery expressions.

- [Usage of IDbContextFactory](https://github.com/linq2db/linq2db/discussions/3928) -- [Q&A] Dependency injection and context factory integration with linq2db.

- [Add values from a dictionary when doing CRUD operations](https://github.com/linq2db/linq2db/discussions/4148) -- [Ideas] Bulk insert/update from dictionary sources.

- [[Docs] Comparison with Entity Framework](https://github.com/linq2db/linq2db/discussions/4288) -- [General] Documentation request for EF Core vs linq2db differences.

- [Configure `Linq2DB.EntityFrameworkCore` to respect shadow `Discriminator` property](https://github.com/linq2db/linq2db/discussions/5477) -- [Q&A] Shadow properties in inheritance hierarchies not handled in Merge operations.

## Stats

- Open issues: 26
- Closed issues: 58
- Open PRs: 0
- Total PRs: 28
- Discussions: 10
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 122 (84 issues + 28 PRs + 10 discussions)
- Themes extracted: 6
</details>
