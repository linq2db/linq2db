---
area: PROV-INFORMIX
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-INFORMIX -- GitHub themes

## Open themes

- **Schema support expansion** -- #1182, #122. User-requested schema provider extensions for Informix, specifically stored procedure metadata extraction, with initial schema support resolved but procedures remaining pending.
- **New provider version support** -- #3517. IBM released Informix.Net.Core, a modern .NET Core provider for Informix; feature request to add explicit support alongside the legacy/legacy provider.
- **SUM aggregate null-guard wrapping** -- #5531, #5568. Recent PR #5502 introduced null-guard wrapping for SUM aggregates that diverges on Informix, emitting `Nvl(x, NULL)` instead of the intended `Nvl(x, 0)`.

## Resolved themes

- **PK/FK metadata extraction** -- #125, #126. Schema provider duplicated PK columns and incorrectly mapped FK constraints; both resolved via vendor schema provider refinement.
- **String truncation on insert/replace** -- #1306, #1307. Data provider raised truncation errors on CHAR column overflow during bulk operations; fixed via data type handling enhancements.
- **Runtime initialization** -- #219. Type initializer errors in web application contexts (vs. console) resolved via provider adapter lifecycle improvements.
- **SQL dialect support gaps** -- #1778 (TOP in subqueries), #1870 (IBM.Data.DB2.Core package support). Closed via dialect-specific translator enhancements and package migration support.
- **DateTime format configuration** -- #1265. Additional configuration options for controlling DateTime serialization format in Informix (resolved, merged).
- **String predicates** -- #2824. SQL translation for string.IsNullOrWhitespace added (merged 2021-02-28).
- **Locale handling on Linux** -- #2295. DB2/Informix locale issues on Linux fixed (merged 2020-06-27).
- **Nullable<T> default value mapping** -- #3837. Fixed nullable generic type mapping on insert/update (merged 2022-12-06).

## Active discussions

- No active discussions.

## Stats

- Open issues: 2
- Closed issues: 8
- Open PRs: 1
- Total PRs: 11
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 23 (10 issues + 11 PRs + 0 discussions)
- Themes extracted: 8 (3 open, 5 resolved)

</details>
