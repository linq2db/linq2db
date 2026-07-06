---
area: EXPR-TRANS
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# EXPR-TRANS -- GitHub themes

## Open themes

- **Member/Method translation refactoring** -- Migration of expression methods from the legacy `Sql.Extension` + expose-mapping system into the member-translator pipeline (`StringMemberTranslatorBase` and related). #5613 (Migrate string.CompareTo), #5577 (expand member/method mappings), #5541 / #5578 (ExpressionMethodAttribute.IsColumn handling). Six PRs landed; remaining translation consolidation (binary/unary from #3994) in #5212.
- **Projection computation & client-side evaluation** -- #5604 (PreferClientCalculation option) addresses the v6 behavior change that forces projection expressions through server-side SQL translation; allows opting back to v5's client-side evaluation for computed columns. Open for review.
- **Nullable type handling in subqueries & correlated contexts** -- #5586 (Nullable<T>.HasValue over unbound members), #5582 (null-safe IN/NOT IN emulation on providers without correlated-subquery support). Fixes for null propagation in complex query shapes.
- **Type translation & casting** -- #5605 (SqlServer decimal overflow fallback via SqlDecimal), #5466 (DateTimeOffset.DateTime as cast), #5581 (nullable DateTime subtraction). Ongoing coverage for edge cases in CLR↔SQL type bridging.
- **Window Functions API** -- #5468 (new Sql.Window fluent API for window functions) replaces the older `Sql.Ext().Over().ToValue()` pattern; includes legacy converter. Open for review.

## Resolved themes

- **String method translation consolidation** -- #5613 (string.CompareTo), #5544 (string.IsNullOrWhiteSpace), #5504 (string.Concat, string.TrimStart/End) — all moved from legacy `Sql.Extension` into the member-translator pipeline with per-provider overrides.
- **Expression optimization post-AST refactor** -- #5570 (collapse nested case-conversion wraps in Guid→string translation), #5567 (restore IS NULL pushdown through SqlConcatExpression), #5566 (skip bogus IS NULL guards on non-null Sybase concat operands), #5569 (migrate Sybase concatenation to ANSI ||). All post-#5504 string-concat AST refactor.
- **Correlated subquery detection & validation** -- #5574 (reject unsupported correlated subqueries in expression position on ClickHouse/YDB), #5558 (fix InvalidCastException in APPLY→JOIN conversion with correlated Contains). Fixes for providers with limited or no correlated-subquery support.
- **Binary/unary operator translation** -- #5212 (merge binary/unary translation fixes from #3994); #5573 (Meziantou analyzer rules).
- **Projection & materialization edge cases** -- #5587 (spurious [item] column on local-collection LEFT JOIN with decimal projection), #5577 (expand member/method mappings during initial expose instead of at build time), #5581 (nullable DateTime subtraction in final projection).

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
- Open PRs: 8 (EXPR-TRANS focused)
- Total PRs: 29 EXPR-TRANS (8 open, 21 merged)
- Discussions: 68
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 29 EXPR-TRANS PRs (since last theme refresh)
- Themes extracted: 5 open, 5 resolved
</details>
