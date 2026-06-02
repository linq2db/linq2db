---
area: EXPR-TRANS
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
---

# EXPR-TRANS -- GitHub themes

## Open themes

- **Window functions & analytic queries** -- 6 open issues share support for window functions in GROUP BY, null conditions inside windows, and analytic syntax. Sample: #3127, #5123, #5015.
- **Type translation & conversion** -- 5 open issues covering SumAsync with BigInteger, DateTime handling in filters, type property resolution, and implicit conversions. Sample: #4040, #3993, #4199.
- **Computed properties & materialization** -- 4 open issues on selective column loading, composite property selection, entity service interceptors, and dynamic column mapping. Sample: #4568, #4365, #4992.
- **Query cache & performance** -- 3 open issues on cache size limits, compiled query async issues, and extension method caching. Sample: #3009, #3266, #4266.
- **Async query operators** -- 3 open issues: LastOrDefaultAsync for IOrderedQueryable, SumAsync overloads, async compiled updates. Sample: #4019, #4040.
- **String & SQL functions** -- 2 open issues: LISTAGG/StringAggregate extensions, TRANSLATE function, CONTAINS in multi-column scenarios. Sample: #4224.
- **Subquery & correlation handling** -- 2 open issues on dynamic filter combination with OR/subqueries and null conditions in window frames. Sample: #5015, #5123.

## Resolved themes

- **JSON column auto-serialization** -- #1661 (json serialization with objects) merged into core mapping infrastructure.
- **Implicit transaction handling** -- #4053 resolved through explicit transaction management refinements.
- **Compiled query with updates** -- #3266 addressed with async compiled query infrastructure improvements.

## Active discussions

- [Resolving base or generic properties](https://github.com/linq2db/linq2db/discussions/4604) -- [Q&A] Translation of inherited/interface properties in entity selects.
- [Extension method cannot be converted to SQL](https://github.com/linq2db/linq2db/discussions/4674) -- [Q&A] Custom method translation constraints.
- [Applying SQL functions to column by default](https://github.com/linq2db/linq2db/discussions/4765) -- [Q&A] Default function wrapping for column operations.
- [Selecting multiple dynamic columns into a custom property type](https://github.com/linq2db/linq2db/discussions/4992) -- [Q&A] Dynamic column mapping to custom types.
- [TRANSLATE function](https://github.com/linq2db/linq2db/discussions/5150) -- [Q&A] SQL TRANSLATE function support.
- [failed comparing datetime](https://github.com/linq2db/linq2db/discussions/5185) -- [Q&A] DateTime comparison across zones/formats.
- [Sql.AsSql and constant expressions](https://github.com/linq2db/linq2db/discussions/5153) -- [General] Design clarifications for SQL expression templates.
- [Version 6.2.* migration Q&A](https://github.com/linq2db/linq2db/discussions/5453) -- [Q&A] Translation breakage across versions.

## Stats

- Open issues: 36
- Closed issues: 730
- Open PRs: 13
- Total PRs: 332
- Discussions: 68
- Last fetched: 2026-06-01

<details><summary>Coverage</summary>

- Index entries scanned: 1166 (766 issues + 332 PRs + 68 discussions)
- Themes extracted: 7 open, 3 resolved
</details>
