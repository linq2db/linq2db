---
area: PROV-MYSQL
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-MYSQL -- GitHub themes

## Open themes

- **UPSERT / ON DUPLICATE KEY UPDATE** -- MySQL lacks native MERGE; bulk upsert must route through `INSERT ... ON DUPLICATE KEY UPDATE`. Two requests (#5278, #1480) ask for explicit Merge API support or multi-row INSERT variant. Workaround: inline row values or iterate inserts.
- **Bulk UPDATE** -- #1259 requests bulk update for MySQL. Current pattern is iterated updates; no batch-by-batch equivalent to bulk insert.
- **Connection / transaction lifecycle** -- #3431 (RetryingDataConnection rejects re-open), #5118 (LoadWith starts transaction on read-only query). Indicates tension between connection reuse, implicit transaction scope, and provider quirks.
- **Connection string / initialization** -- #3313 (T4 timeout on scaffold), #3422 (MySql.Data.KeyNotFoundException), #4123 (MariaDB vs MySQL dialect). Suggests fragile provider detection or dialect-selection UX when mixing MySql.Data / MySqlConnector and MySQL 5.7 / 8.0 / MariaDB versions.

## Resolved themes

- **mysql** -- 32 closed issues share this keyword. Sample: #45, #121, #168, #218, #350, #220, #558, #443.
- **update** -- 7 closed issues share this keyword. Sample: #443, #2573, #2556, #3284, #997, #3905, #4164.
- **connection** -- 6 closed issues share this keyword. Sample: #1686, #1772, #2567, #3390, #1991, #4457.

## Active discussions

- [How can we trace BeginTransaction(), RollbackTransaction() and CommitTransaction?()](https://github.com/linq2db/linq2db/discussions/3073) -- [Q&A] Tracing infrastructure doesn't log transaction boundaries; verbose trace shows only queries.
- [NET 6 with MySql connection](https://github.com/linq2db/linq2db/discussions/3422) -- [Q&A] MySql.Data v8.0.25 raises KeyNotFoundException on column lookup during scaffold.
- [How can I let linq2db work with MAUI](https://github.com/linq2db/linq2db/discussions/3828) -- [Q&A] MAUI/Xamarin UWP compatibility with MySQL provider.
- [How to connect with MariaDB](https://github.com/linq2db/linq2db/discussions/4123) -- [Q&A] Provider selection between MySQL and MariaDB; no explicit MariaDB dialect enum in public API.
- [linq2db CLI, Json scaffolding ignore table back references on indexes?](https://github.com/linq2db/linq2db/discussions/4432) -- [Q&A] Scaffold config for suppressing back-reference properties on indexed columns.
- [Why does using a method similar to LoadWith start a transaction when it's only a query?](https://github.com/linq2db/linq2db/discussions/5118) -- [General] LoadWith on MySQL auto-opens transaction even for read-only query; unexpected behavior.

## Stats

- Open issues: 6
- Closed issues: 60
- Open PRs: 0
- Total PRs: 44
- Discussions: 6
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 116 (66 issues + 44 PRs + 6 discussions)
- Themes extracted: 4 open + 3 resolved

</details>
