---
area: PROV-DB2
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-07-06
last_verified_sha: df84c7784
---

# PROV-DB2 -- GitHub themes

## Open themes

- **DB2 string type handling edge cases** -- #2362 describes rows being skipped when a VARCHAR column contains an empty string (mapped via value converter). The issue affects querying with value converters where empty strings need to be distinguished from NULL.

## Resolved themes

- **DB2/Informix native client loading on Linux/macOS** -- #5563 stabilized transient failures where the native `libdb2.so` shared library failed to load due to `LD_LIBRARY_PATH` not reliably propagating to the testhost subprocess. Added explicit native-library resolver in `Tests/Linq/TestsInitialization.cs`.

- **DB2 DECFLOAT type mapping for special values** -- #5663 fixed mapping of DB2 `DECFLOAT` columns that hold IEEE 754 special values (`Infinity`, `-Infinity`, `NaN`). These values arise naturally from expressions like `RATIO_TO_REPORT()` and require double/float mapping instead of decimal.

- **DB2 identity column support with OVERRIDING SYSTEM VALUE** -- #4601 implemented support for SELECT INTO/INSERT operations with identity columns using DB2's `OVERRIDING SYSTEM VALUE` clause.

- **DB2 UPDATE with tuple assignment (SET clause)** -- #4696 documented the syntax for DB2's tuple assignment in UPDATE statements (`SET (col1, col2) = (val1, val2)`).

## Active discussions

- [DB2 testing and licensing](https://github.com/linq2db/linq2db/discussions/5127) — [Q&A] @MaceWindu Can you tell me how you got support from IBM to regression test DB2? I would like to do similar for FluentMigrator using Testcontainers.Db2.

## Stats

- Open issues: 1
- Closed issues: 22
- Open PRs: 0
- Total PRs: 21
- Discussions: 2
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 46 (23 issues + 21 PRs + 2 discussions)
- Themes extracted: 4
</details>
