---
area: DATA
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-07-06
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# DATA -- GitHub themes

## Open themes

No clustered themes among open issues. Single open item: #4660 (BulkCopyOptions configuration context scope).

## Resolved themes

- **Merge API** -- Multiple bugs and edge cases fixed across versions. Recent: #5476, #5181, #4971 (entity inheritance), #4338 (joins).
- **BulkCopy operations** -- Error handling, type mapping, async support, and options scoping. Examples: #4672 (NodaTime Period), #4461 (CLOB), #4534 (async cancellation).
- **InsertWithOutput variants** -- Correctness issues with aggregates, joins, and output routing. Examples: #5192, #3983, #3834 (column name mismatches).

## Active discussions

- [How to get records which were failed while bulk copy insert](https://github.com/linq2db/linq2db/discussions/3081) — [Q&A] Handling failed rows during bulk operations.
- [Is it ok to open another dataconnection inside ProcessQuery?](https://github.com/linq2db/linq2db/discussions/3388) — [Q&A] Connection lifecycle and interceptor scope.
- [[Update|Insert|Delete]WithOutputIntoOutput](https://github.com/linq2db/linq2db/discussions/3831) — [Ideas] Chaining output operations.
- [How to write Merge which only updates some columns?](https://github.com/linq2db/linq2db/discussions/4279) — [Q&A] Selective column updates in MERGE.
- [AsQueryable Paramterization?](https://github.com/linq2db/linq2db/discussions/4421) — [Q&A] Using InsertWithOutput with AsQueryable client datasets.
- [A way to not inline Merge parameters](https://github.com/linq2db/linq2db/discussions/4582) — [Q&A] Parameter handling in MERGE with custom checks.
- [Options as record type in version 6 is bad idea](https://github.com/linq2db/linq2db/discussions/4704) — [General] DataOptions record vs class initialization.
- [Tracking the changes](https://github.com/linq2db/linq2db/discussions/4834) — [Q&A] Identity tracking post-BulkCopy.
- [Still necessary to have two SetConverter](https://github.com/linq2db/linq2db/discussions/5165) — [Q&A] C# type vs DataParameter converter redundancy.

## Stats

- Open issues: 1
- Closed issues: 73
- Open PRs: 1
- Total PRs: 83
- Discussions: 9
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 166 (74 issues + 83 PRs + 9 discussions)
- Themes extracted: 3
</details>
