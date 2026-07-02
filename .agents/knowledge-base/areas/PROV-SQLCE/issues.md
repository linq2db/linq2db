---
area: PROV-SQLCE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-SQLCE -- GitHub themes

## Open themes

- **BulkCopy exceptions** -- Three open issues report distinct exceptions when bulk-inserting into SQL Server Compact 4.0: ntext field null handling (#4436), Expression evaluation overflow (#4438), and StackOverflowException on large batches (#4574). Single-row inserts work correctly, suggesting the issue is in the bulk-copy path logic.
- **Case-sensitive search** -- Incorrect test configuration meant SQLCE case-sensitive-search test never ran; #3444 tracks the regression.
- **CommitMode context option** -- Request to expose CommitMode as a context option for SQL Server Compact (#4686), pending decision on implementation approach.

## Resolved themes

- **Schema provider PK duplications** -- PR #1761 fixed duplicate primary-key issues in SQLCE schema provider and related Convert function regressions.
- **Conditional operator** -- Earlier issue #1840 (conditional operator exception) was resolved.

## Active discussions

- [How to use transactions properly?](https://github.com/linq2db/linq2db/discussions/4636) -- [question] Transaction usage clarification for SQL Server Compact context.

## Stats

- Open issues: 5
- Closed issues: 4
- Open PRs: 0
- Total PRs: 2
- Discussions: 1
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 12 (9 issues + 2 PRs + 1 discussions)
- Themes extracted: 3
</details>
