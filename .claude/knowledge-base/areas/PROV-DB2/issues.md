---
area: PROV-DB2
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-DB2 -- GitHub themes

## Open themes

- No clustered themes (insufficient open issue volume or shared keywords).

## Resolved themes

- **DB2/Informix native client loading on Linux/macOS** -- #5563 stabilized the transient failures in CI where the native `libdb2.so` shared library failed to load due to `LD_LIBRARY_PATH` not reliably propagating to the testhost subprocess. Added an explicit native-library resolver in `Tests/Linq/TestsInitialization.cs` to pre-load the driver before any DB2 ADO.NET calls.

## Active discussions

- [DB2 testing and licensing](https://github.com/linq2db/linq2db/discussions/5127) — [Q&A] @MaceWindu Can you tell me how you got support from IBM to regression test DB2? I would like to do similar for FluentMigrator using Testcontainers.Db2 Or do you not test it at all? I couldn't find any integration tests for Linq2Db. I found a few issues were users wrote inte...

## Stats

- Open issues: 1
- Closed issues: 22
- Open PRs: 0
- Total PRs: 21
- Discussions: 2
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 46 (23 issues + 21 PRs + 2 discussions)
- Themes extracted: 1
</details>
