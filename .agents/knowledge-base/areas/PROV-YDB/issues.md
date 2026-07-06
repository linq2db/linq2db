---
area: PROV-YDB
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: df84c7784ce6c6d84d2356269d997edc3db8acfa
---

# PROV-YDB -- GitHub themes

## Open themes
- **YDB type system strictness** -- YDB's strict type semantics surface multiple gaps in linq2db's type inference and code generation. Decimal facet mismatches in COALESCE/CAST/arithmetic (#5591), SDK decimal unpacking overflow (#5592), missing Timestamp--Date and Interval--Int64 conversions (#5593), null parameters lacking concrete type (#5594), and non-nullable string expressions inferred nullable on output (#5595). These block test enablement on the YDB provider completion PR #5564.

- **SQL ordering and optimizer scope** -- Two separate issues affect query shape and optimization. CTE inner ORDER BY not preserved in outer SELECT (#5596) breaks result ordering. DDL-only [PrimaryKey] treated as unique key by optimizer (#5597) silently drops projected columns on GROUP BY.

## Resolved themes

- **F# record-copy update behavior** -- F# record-copy { record with ... } emitted every column including PK, causing YDB rejection -- resolved by fixing the F# update path to set only changed columns.

## Active discussions

- No active discussions.

## Stats

- Open issues: 7
- Closed issues: 1
- Open PRs: 2
- Total PRs: 15
- Discussions: 0
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 9 (7 issues + 2 PRs + 0 discussions)
- Themes extracted: 2
</details>
