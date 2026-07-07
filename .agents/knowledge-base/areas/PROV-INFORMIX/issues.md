---
area: PROV-INFORMIX
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-07-06
last_verified_sha: df84c7784
---

# PROV-INFORMIX -- GitHub themes

## Open themes

- **Procedures schema support** -- #1182. Stored procedure metadata extraction remains incomplete for Informix; schema provider already supports this for MySQL and PostgreSQL but the Informix vendor provider still needs implementation.
- **New provider version support** -- #3517. IBM released a modern Informix.Net.Core provider for .NET Core; feature request to add explicit support for this provider alongside the legacy alternatives (SQLi, IDS, and multiple DB2 providers).

## Resolved themes

- **PK/FK metadata extraction** -- #125, #126. Schema provider duplicated PK columns and incorrectly mapped FK constraints; both resolved via vendor schema provider refinement.
- **String truncation on insert/replace** -- #1306, #1307. Data provider raised truncation errors on CHAR column overflow during bulk operations; fixed via data type handling enhancements.
- **Runtime initialization** -- #219. Type initializer errors in web application contexts (vs. console) resolved via provider adapter lifecycle improvements.
- **SQL dialect support gaps** -- #1778 (TOP in subqueries), #1870 (IBM.Data.DB2.Core package support). Closed via dialect-specific translator enhancements and package migration support.

## Active discussions

- No active discussions.

## Stats

- Open issues: 2
- Closed issues: 8
- Open PRs: 0
- Total PRs: 0
- Discussions: 0
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 10 (2 issues + 8 closed issues)
- Themes extracted: 2 (2 open, 4 resolved)

</details>
