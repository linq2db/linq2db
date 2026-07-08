---
area: PROV-SYBASE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-SYBASE -- GitHub themes

## Open themes

- **String concatenation and Unicode handling** -- Ongoing work to migrate Sybase ASE string concatenation from `+` operator to ANSI `||` and handle Unicode parameter length issues. Active drafts: #5566 (skip bogus IS NULL guards on concat operands), #5569 (emit ANSI || for string concatenation).
- **Parameter and type handling** -- Recurring issues with parameter length limits, float column reading as double, and DateTime conversion anomalies (1 Jan 1900 → 1 Jan 0001). Resolved via #3908 (parameter length), #1792 (time/datetime handling), #3183 (float as double).

## Resolved themes

- **Merge statement and InsertOrUpdate** -- Sybase MERGE syntax support and CanCombineParameters handling. Resolved: #811 (merge builder), #814 (CanCombineParameters in merge-based InsertOrUpdate).
- **DateTime and type conversions** -- Multiple closed issues around DateTime handling, type mismatches, and column reading. Sample resolved: #1707 (1900 conversion), #2026 (template timeout), #2038 (ObjectDisposedException on REST API).
- **ODBC, T4 templates, and provider connectivity** -- Earlier issues with ODBC support, T4 template execution, and Stored Procedure execution. Sample: #186 (SQL Anywhere support), #792 (stored procedure execution), #1060 (invalid cast in transformation), #1064 (query for # columns).
- **UPDATE FROM and SQL generation** -- Sybase-specific SQL generation quirks for UPDATE FROM and general operator precedence. Resolved: #2875 (UPDATE FROM generation), #1169 (fix for #1064).
- **CI and provider dependency management** -- Dependency updates and CI environment support. Resolved: #1159 (DataAction AseClient), #1408 (DataAction provider), #1692 (static provider parameter setters), #2951 (Fix Sybase on CI).

## Active discussions

- No active discussions.

## Stats

- Open issues: 0
- Closed issues: 16
- Open PRs: 2
- Total PRs: 15
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 31 (16 issues + 15 PRs + 0 discussions)
- Themes extracted: 5
</details>
