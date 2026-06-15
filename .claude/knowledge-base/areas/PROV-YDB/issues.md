---
area: PROV-YDB
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-YDB -- GitHub themes

## Open themes

- **YDB provider completion** -- Ongoing work to finish the experimental YDB provider implementation. Main PR #5564 in progress with schema API, SDK version bump (0.31.0 → 0.33.0), and test enablement. Fixes sync-over-async deadlock in YDB SDK 0.33.0.

## Resolved themes

- **Correlated subquery handling for zero-correlated providers** -- Providers with no correlated-subquery support (ClickHouse, YDB) required special handling. #5574 rejected unsupported correlated subqueries in expression position. #5582 fixed null-safety in non-correlated IN/NOT IN emulation for these providers.

## Active discussions

- No active discussions.

## Stats

- Open issues: 0
- Closed issues: 0
- Open PRs: 1
- Total PRs: 3
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 3 (0 issues + 3 PRs + 0 discussions)
- Themes extracted: 2
</details>
