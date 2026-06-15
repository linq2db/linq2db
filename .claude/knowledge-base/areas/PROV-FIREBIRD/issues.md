---
area: PROV-FIREBIRD
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-FIREBIRD -- GitHub themes

## Open themes

- **Firebird 6 native syntax support** -- Firebird 6 added native `IF [NOT] EXISTS` clauses for CREATE/DROP TABLE operations; linq2db currently uses legacy `EXECUTE BLOCK` + metadata lookups. Tracked in #5483 (issue) and #5485 (draft PR).
- **Type mapping edge cases** -- UInt32 cast incorrectly to BigInt (#1318); CHAR(x) CHARACTER SET OCTETS not recognized (#755); substring field selection without mapping the base field (#3465).

## Resolved themes

- **firebird** -- 45 closed issues covering the full history of Firebird provider support. Sample: #92, #682, #806, #868, #966, #967, #1043, #1035.

## Active discussions

- [Create POCO with only some fields](https://github.com/linq2db/linq2db/discussions/3229) -- [Q&A] Template-based POCO generation filtering: how to exclude unwanted table columns from the `.tt` T4 output.
- [Select a substring of a memo field without having the base field on the entity](https://github.com/linq2db/linq2db/discussions/3465) -- [Q&A] Field property mapping to a SQL substring without requiring the full base memo field.
- [How is the query that Linq2DB generates for Firebird supposed to be executed?](https://github.com/linq2db/linq2db/discussions/4418) -- [Q&A] Parametrized SELECT query execution and dialect-specific SQL generation verification.
- [Linq2Db not all stored procedures scaffolding](https://github.com/linq2db/linq2db/discussions/4442) -- [Q&A] CLI-based database scaffolding coverage for Firebird stored procedures.

## Stats

- Open issues: 3
- Closed issues: 45
- Open PRs: 2
- Total PRs: 23
- Discussions: 4
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 75 (48 issues + 23 PRs + 4 discussions)
- Themes extracted: 2
</details>
