---
area: PROV-POSTGRES
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# PROV-POSTGRES -- GitHub themes

## Open themes

- **Schema handling & identifier quoting** -- Scaffold generation, case-sensitivity mismatches, quoted identifier tracking, and multi-schema edge cases remain high-friction areas. Issues span T4 template instructions, EF integration attribute support, and incorrect type resolution (bigserial, dblink function schema discovery). (#1864, #4671, #3501, #4695, #4708, #3203, #4444, #5086, #4707)

- **Type mapping for PostgreSQL-native types** -- JSON/JSONB, enums, custom scalar types (ltree, pgvector), and range types (tstzrange) lack complete mapping interceptor support. Users struggle with Contains operator translation, jsonpath queries, and ServerSideOnly function interaction. (#4044, #3869, #4915, #5505, #4915, #2796)

- **Bulk copy & upsert completeness** -- BulkCopy edge cases persist: CheckConstraints option ignored, KeepIdentity failing to update sequences, and syntax errors on constraint-free operations. MERGE/UPSERT API surface gaps remain. (#1140, #4615, #4702)

- **Array & UNNEST support** -- Array type mapping for enums/custom types, UNNEST translation completeness, and query performance regressions (6× slowdown on FromSqlScalar with arrays) are active blockers. (#341, #1660, #4643, #4915, #5480 *recently fixed*)

- **Npgsql version compatibility & driver upgrades** -- Prepared transaction disabling in EntityFrameworkCore + Npgsql 8+, NpgsqlDataSource integration gaps, and range type mapping to modern Npgsql APIs. (#4877, #2796, #5248)

- **FTS & PostgreSQL extensions** -- Full-text search translation and other PostgreSQL-specific extensions remain unimplemented. (#1811)

- **InsertWithOutput API for PostgreSQL** -- Users seeking UpdateWithOutput-style semantics on INSERT operations. CTE-based strategies may unlock this capability via materialization trade-off. (#5679)

## Resolved themes

- **FromSqlScalar performance regression** -- Mapper compiled per call (6× slowdown); fixed in recent optimization pass (#5480).
- **JSONB mapping with computed columns** -- ServerSideOnly functions on JSONB now work correctly (#5505).
- **F# option type nullability** -- Mapping F# int option None to 0 instead of NULL fixed (#4646).
- **Order by NULLS LAST** -- Full support for explicit NULL ordering (#2068).
- **CTE MATERIALIZED** -- PostgreSQL 12+ MATERIALIZED keyword now supported (#5323).
- **String-to-JSONB Contains** -- Operator translation now respects case behavior (#5347).

## Active discussions

- [Idea for adding Postgres InsertWithOutput support](https://github.com/linq2db/linq2db/discussions/5679) -- [Ideas] Using INSERT in CTE to enable InsertWithOutput for PostgreSQL despite materialization penalty.
- [Invalid SQL generated through UpdateWithOutput() for PostgreSQL and SQLite](https://github.com/linq2db/linq2db/discussions/4996) -- [Q&A] DELETED tuple leakage into RETURNING clause edge case.
- [Postgres naming -- lower case](https://github.com/linq2db/linq2db/discussions/4952) -- [Q&A] Case-sensitivity and automatic identifier folding during mapping.
- [Change the schema for all tables/entities in a connection](https://github.com/linq2db/linq2db/discussions/4845) -- [Q&A] Per-connection schema override patterns.

## Stats

- Open issues: 45
- Closed issues: 191
- Open PRs: 2
- Total PRs: 86
- Discussions: 30
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 363 (45 issues + 2 open PRs + 86 total PRs + 30 discussions)
- Themes extracted: 7
</details>
