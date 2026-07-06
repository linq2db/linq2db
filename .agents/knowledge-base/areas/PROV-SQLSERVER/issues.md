---
area: PROV-SQLSERVER
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# PROV-SQLSERVER -- GitHub themes

## Open themes

- **Scaffolding and schema generation gaps** -- Four open requests (#449, #4195, #4606, #4831) surface scaffolding limitations: table-valued functions (TVFs/UDFs) not discovered (#449), Azure SQL Edge CLR feature gap blocking schema import (#4195), database-name parameter not applied (#4606), and reserved-word-named views/tables causing code-generation errors (#4831). Core gap: the T4 schema provider needs extended coverage for TVF discovery, conditional CLR feature gating for Azure SQL Edge, and generated-code identifier sanitization.

- **EFCore integration gaps** -- Multiple open issues (#3174, #4640, #4656, #4665) surface feature-parity gaps: EFCore shadow properties not carried to temp tables (#3174), JSON columns returning null in MERGE operations (#4640), temporal query JOINs unsupported (#4656), and NetTopologySuite spatial-type mapping incomplete (#4665). All involve EFCore-to-linq2db mapping translation gaps or provider-feature exposure.

- **BulkCopy limitations** -- Two open issues (#1178, #4663) request BulkCopy improvements: #1178 requests API enhancements for performance tuning, and #4663 reports BulkCopy failure with EFCore complex properties (structured type mapping not propagated to bulk-copy column inference).

- **Temporary table type safety** -- #3174 and #4659 surface temp table gaps: shadow property omission and incorrect type inference for primitive-typed temp tables (TempTable<string> not recognized as a string type by IN predicate).

- **SQL string optimization** -- #1916 requests native CONCAT() function mapping (available since SQL Server 2012) instead of the current `+` operator concatenation.

- **Recursive CTE and projection type stability** -- #2451 and #5680 surface CTE issues: #2451 reports type-mismatch errors when recursive CTE branches emit different precisions (NVARCHAR(50) vs NVARCHAR(MAX)), and #5680 (open PR) fixes a silent column-drop when projection members carry object-typed references.

- **Type handling (decimal, string)** -- #4177 (open PR) addresses string.Length mapping for SQL CE, and #5605 (open draft PR) adds SqlServer decimal overflow fallback via SqlDecimal.

## Resolved themes

- **Fabric Datawarehouse scaffolding** -- #4536 resolved: ASSEMBLYPROPERTY exclusion in schema enumeration unblocks Microsoft Fabric datawarehouse scaffold.

## Active discussions

No active discussions for PROV-SQLSERVER.

## Stats

- Open issues: 15
- Closed issues: 1
- Open PRs: 4
- Total PRs: 4
- Discussions: 0
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 20 (15 issues + 4 PRs + 0 discussions)
- Themes extracted: 7 open + 1 resolved

</details>
