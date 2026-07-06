---
area: PROV-MYSQL
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# PROV-MYSQL -- GitHub themes

## Open themes

- **Type Mapping & Schema Handling** -- Issues with mapping MySQL types (enum → varchar, bit(1) → bool, UUID support) and schema generation. Recurs across multiple MySQL versions and mappings libraries. (#3223, #3554, #86)
- **Bulk Operations Gap** -- Bulk INSERT works, but bulk UPDATE unsupported; BulkCopy has issues with custom GuidFormat in binary modes. (#1259, #4354)
- **UPSERT / ON DUPLICATE KEY UPDATE** -- MySQL lacks MERGE; bulk upsert must route through INSERT ... ON DUPLICATE KEY UPDATE. Explicit API support requested (#5278). Related: #1480.
- **Connection / Initialization** -- Connection reuse, retry logic, and T4 model generation show stress points. RetryingDataConnection rejects re-open (#3431); T4 scaffold timeouts on large schemas (#3313).
- **Critical Error: Unresolved Exception** -- #4669 hits UnreachableException in EFCore MySql translation path; may indicate cross-provider integration fragility.

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

- Open issues: 9
- Closed issues: 60
- Open PRs: 0
- Total PRs: 0
- Discussions: 6
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 69 (9 issues + 0 PRs + 6 discussions)
- Themes extracted: 5 open + 3 resolved

</details>
