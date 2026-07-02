---
area: PROV-CLICKHOUSE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-CLICKHOUSE -- GitHub themes

## Open themes

- **Correlated subqueries** -- ClickHouse cannot run correlated subqueries, but linq2db emits wrong SQL instead of detecting the unsupported case (#5590). Fixed by #5574 (merged: detect and reject with clean error). #5555 is a follow-up improving join hints.
- **Lightweight DELETE** -- ClickHouse supports DELETE FROM syntax for lightweight deletions, but linq2db currently uses ALTER TABLE DELETE (#4725).

## Resolved themes

- **Octonica client library** -- The Octonica.ClickHouseClient NuGet package (v3.1.8) was incompatible with linq2db (broken in #5460, CI disabled). Updated to v4.1.4 and CI re-enabled in #5608 (merged 2026-06-12).
- **IN/NOT IN null handling** -- Non-correlated NOT IN / IN emulation was not null-safe for providers without correlated-subquery support (ClickHouse, YDB). Fixed in #5582 (merged 2026-06-04).

## Active discussions

No active discussions in this area.

## Stats

- Open issues: 2
- Closed issues: 0
- Open PRs: 1
- Total PRs: 4 (1 open, 3 merged)
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 6 (2 issues + 4 PRs + 0 discussions)
- Themes extracted: 4
</details>
