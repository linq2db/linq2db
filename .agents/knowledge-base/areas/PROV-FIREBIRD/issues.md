---
area: PROV-FIREBIRD
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: df84c7784
---

# PROV-FIREBIRD -- GitHub themes

## Open themes

- **Type mapping edge cases** -- Firebird's type system diverges from standard SQL in subtle ways: UInt32 constants incorrectly cast to BigInt unsupported in SQL Dialect 1 (#1318); CHAR(x) CHARACTER SET OCTETS not recognized for binary data (#755); numeric constants vs variables producing different SQL and result types (#4469).

- **Stored procedure scaffolding coverage** -- CLI scaffold skips stored procedures with output parameters (#4447) and generates incorrect signatures with redundant output parameters in the method signature (#4718).

- **Firebird 6 DDL syntax modernization** -- Firebird 6 introduces native `IF [NOT] EXISTS` clauses for CREATE/DROP TABLE, but linq2db continues using legacy `EXECUTE BLOCK` + metadata-lookup workaround. Tracked in #5483.

## Resolved themes

- **Firebird provider foundation** -- 45 closed issues covering the full history of Firebird provider support. Sample: #92, #682, #806, #868, #966, #967, #1043, #1035.

## Active discussions

- [Create POCO with only some fields](https://github.com/linq2db/linq2db/discussions/3229) -- [Q&A] Template-based POCO generation filtering: how to exclude unwanted table columns from the `.tt` T4 output.

- [Select a substring of a memo field without having the base field on the entity](https://github.com/linq2db/linq2db/discussions/3465) -- [Q&A] Field property mapping to a SQL substring without requiring the full base memo field.

- [How is the query that Linq2DB generates for Firebird supposed to be executed?](https://github.com/linq2db/linq2db/discussions/4418) -- [Q&A] Parametrized SELECT query execution and dialect-specific SQL generation verification.

- [Linq2Db not all stored procedures scaffolding](https://github.com/linq2db/linq2db/discussions/4442) -- [Q&A] CLI-based database scaffolding coverage for Firebird stored procedures.

## Stats

- Open issues: 6
- Closed issues: 45
- Open PRs: 1
- Total PRs: 1
- Discussions: 4
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 52 (51 issues + 1 PR + 4 discussions)
- Themes extracted: 3

</details>
