---
area: PROV-CLICKHOUSE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-07-06
last_verified_sha: df84c7784
---

# PROV-CLICKHOUSE -- GitHub themes

## Open themes

- **Correlated subqueries** -- ClickHouse cannot run correlated subqueries; linq2db does not reliably detect them and emits wrong SQL. #5590 (open issue) tracks unreliable detection. Fixed by #5574 (merged), with #5555 (open PR) improving join hints. Gap remains for complex nested cases.
- **Type system gaps** -- ClickHouse Array and JSON column types are not supported. #4898 requests Array support (users work around via GetTypeMapping); #4972 is a blocker for JSON columns. Both require schema and type-mapping extensions.
- **Lightweight DELETE** -- ClickHouse supports DELETE FROM for lightweight deletions, but linq2db uses ALTER TABLE DELETE. #4725 requests native syntax support.

## Resolved themes

- **Octonica client library** -- The Octonica.ClickHouseClient NuGet package (v3.1.8) was incompatible with linq2db (broken in #5460, CI disabled). Updated to v4.1.4 and CI re-enabled in #5608 (merged 2026-06-12).
- **IN/NOT IN null handling** -- Non-correlated NOT IN / IN emulation was not null-safe for providers without correlated-subquery support (ClickHouse, YDB). Fixed in #5582 (merged 2026-06-04).

## Active discussions

- [Wrap s3Cluster ClickHouse function calls](https://github.com/linq2db/linq2db/discussions/5668) -- [Q&A] TableFunction wrapper for ClickHouse s3Cluster remote table function calls; users need to call native s3Cluster() within INSERT INTO .. SELECT.
- [Force * in insert into select](https://github.com/linq2db/linq2db/discussions/5671) -- [Q&A] INSERT INTO .. SELECT from s3Cluster generates explicit columns instead of * (ClickHouse requires * for certain remote sources).
- [Schema Provider -- Access levels](https://github.com/linq2db/linq2db/discussions/4441) -- [General] Determining which system tables/databases need permission grants to make schema provider work.

## Stats

- Open issues: 5
- Closed issues: 12
- Open PRs: 1
- Total PRs: 3 (1 open, 2 merged)
- Discussions: 4 (2 open, 2 closed)
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 26 (5 open issues + 1 open PR + 4 discussions + 12 closed issues + 2 merged PRs + 1 closed discussion)
- Themes extracted: 5 (3 open + 2 resolved)
</details>
