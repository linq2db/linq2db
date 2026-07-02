---
area: PROV-SQLSERVER
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-SQLSERVER — GitHub themes

## Open themes

- **Schema generation / scaffolding** -- Two open requests (#4195, #449) surface scaffolding limitations: Azure SQL Edge lacks CLR support (required by ASSEMBLYPROPERTY enum), and table-valued functions (UDFs) require manual T4 template work. Sample: #4195, #449.
- **SQL Server-specific optimizations** -- #3314 requests ISNULL() optimization; ISNULL calculates parameters only once, while COALESCE expands to CASE and risks double evaluation on complex subqueries.
- **String concatenation (Sql.Concat)** -- #1916 requests native SQL.Concat() function mapping to SQL Server's CONCAT() (available since 2012); currently translates to string concatenation operator or CASE-based COALESCE.
- **Bulk copy enhancements** -- #1178 suggests BulkCopy API improvements for performance use cases.

## Resolved themes

- **Fabric Datawarehouse scaffolding** -- #4536 resolved: ASSEMBLYPROPERTY exclusion in schema enumeration unblocks Microsoft Fabric datawarehouse scaffold.

## Active discussions

No active discussions for PROV-SQLSERVER.

## Stats

- Open issues: 5
- Closed issues: 1
- Open PRs: 0
- Total PRs: 0
- Discussions: 0
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 6 (6 issues + 0 PRs + 0 discussions)
- Themes extracted: 4 open + 1 resolved

</details>
