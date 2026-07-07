---
area: PROV-SQLCE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# PROV-SQLCE -- GitHub themes

## Open themes

- **BulkCopy exceptions** -- Four open issues report distinct exceptions when bulk-inserting into SQL Server Compact 4.0: ntext field null handling (#4436), large string overflow into ntext (#4581), Expression evaluation overflow (#4438), and StackOverflowException on large batches (#4574). Single-row inserts work correctly, suggesting the issue is in the bulk-copy path or ntext-specific logic.
- **Case-sensitive search** -- Incorrect test configuration meant SQLCE case-sensitive-search test never ran; #3444 tracks the regression (related to #2952).
- **CommitMode context option** -- Request to expose CommitMode as a context option for SQL Server Compact (#4686), pending decision on implementation approach.
- **Final-alias rewriting** -- By-name column mapping for raw-SQL queries breaks when final aliases are applied (#5599); affects SqlCe and YDB, requiring clarification on whether by-name mapping should resolve against final aliases or original column names.

## Resolved themes

- **Schema provider PK duplications** -- PR #1761 fixed duplicate primary-key issues in SQLCE schema provider and related Convert function regressions.
- **Conditional operator** -- Earlier issue #1840 (conditional operator exception) was resolved.

## Active discussions

(None currently open)

## Stats

- Open issues: 7
- Closed issues: 4
- Open PRs: 0
- Total PRs: 2
- Discussions: 0 (1 closed)
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 12 (7 open issues + 4 closed issues + 1 discussion)
- Themes extracted: 4
</details>
