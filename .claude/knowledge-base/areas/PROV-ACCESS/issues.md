---
area: PROV-ACCESS
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-ACCESS -- GitHub themes

## Open themes

- **VS2022 64-bit incompatibility** -- OLE DB / ODBC drivers in 64-bit environment. Access requires 32-bit drivers on VS2022 (64-bit IDE). #3344, #5311.

## Resolved themes

- **Boolean / string type mapping** -- Access CHAR/STRING columns mapped to C# bool via ValueConverter; parameter wrapping losses on UPDATE SET (#5519, #5520). Schema read ACE database fixes (#1119).
- **Schema discovery and FK relationships** -- AccessSchemaProvider issues with foreign keys, multiple association names without user-defined names (#593, #2331). Fixed via schema option DisableForeignKeySchemaLoad (#1907).
- **Query translation specifics** -- LIKE wildcard escaping differences, Single() producing TOP 2, association-alias resolution (#1925, #2022, #2557), parameter limits (64KB SQL, 767 max params).
- **ODBC vs OLE DB driver support** -- Feature request #333, netcore provider path additions (#1917, #1920).
- **Data access and connection lifecycle** -- Connection pool exhaustion after repeated access (#3608), INSERT statement missing PK inclusion (#3299), foreign key constraint on INSERT (#1909).
- **T4 template and scaffolding** -- Model generation failures (#1164), VS2022 64-bit T4 support, exclude-schemas option regression (#3709).
- **Type mapping** -- Long Integer field mapping (should be long, not int #289), boolean type mapping (#409), custom type conversions.

## Active discussions

- No active discussions.

## Stats

- Open issues: 1
- Closed issues: 22
- Open PRs: 0
- Total PRs: 5
- Discussions: 4
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 32 (23 issues + 5 PRs + 4 discussions)
- Themes extracted: 7
</details>
