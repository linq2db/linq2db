---
area: SQL-PROVIDER
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# SQL-PROVIDER -- GitHub themes

## Open themes

- **SQL generation edge cases** -- Multiple issues around SQL generation in complex scenarios: grouped projections with instances (#4256), chained associations and expression methods (#3822), nested case-conversion wraps in Guid→string conversion (#5528), and redundant IS NULL optimizations through SqlConcatExpression (#5529). Several PRs address specific cases but broader pattern emerges of edge cases in optimizer. Issues: #4256, #3822, #5528, #5529, #5570.

- **Table/alias resolution failures** -- Intermittent failures to build queries with complex joins and associations. Users report 'Table not found' exceptions that appear random or depend on accessing specific navigation properties. #4919 shows aliasing can become corrupted under certain query patterns. #4773 involves casting to interfaces combined with inheritance. Issues: #4919, #4773, #1347, #1816.

- **DDL support gaps (ForeignKey, Enum creation)** -- Users cannot create foreign keys or enums directly via linq2db DDL API. Both feature requests (#4334, #4335) show demand for fuller DDL coverage on providers like PostgreSQL. Related to incomplete temporary table support (#2368 closed but likely open items remain). Issues: #4334, #4335, #2368.

- **Projection and order-by composition** -- Recent regression in 6.0.0-rc.1: projecting to a new class then ordering by a composite property (e.g., string interpolation) fails SQL generation (#5050). Also, associations to arbitrary projection queries from joined group-by no longer compile (#5049). Both represent composition/ordering breakdown post-v5. Issues: #5050, #5049.

## Resolved themes

- **Provider customization barriers (mostly addressed)** -- Long-standing issue: ISqlBuilder and related builder classes were internal, blocking custom provider extensions. Fixed by making classes public (#347, #144, #156, #182, #58). Later refactoring (#4948) removed ISqlBuilder interface entirely to simplify maintainability. Fixed via #4948.

- **Type mapping and conversion edge cases** -- Historical issues with TimeSpan/DateTimeOffset literal generation (#149), anonymous type filtering (#2769), string/nullable coercion (#2746), custom type mappings (#3821). Most resolved case-by-case via converter overrides or type-specific builder fixes. Issues: #149, #2769, #2746, #3821.

- **ASP.NET Core DI registration problems** -- Multiple context registration (#3527, #4077, #4326), scoped lifetime issues (#2507, #3623), and DataOptions type selection bugs (#4326, #5024, #5041). Fixed incrementally through RC cycles and preview versions, now stabilized. Fixed via #4406.

## Active discussions

No active discussions in this area.

## Stats

- Open issues: 7
- Closed issues: 37
- Open PRs: 1 (+ 2 draft)
- Total PRs: 22
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 66 (44 issues + 22 PRs + 0 discussions)
- Themes extracted: 7
</details>
