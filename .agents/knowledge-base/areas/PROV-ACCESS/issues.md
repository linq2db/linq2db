---
area: PROV-ACCESS
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: d3061c6d7315303a86dfdd67bb7728d4736f6506
---

# PROV-ACCESS -- GitHub themes

## Open themes

- **Schema generation and model quality** -- T4 scaffold issues affecting MS Access models. Context class names embed full file paths instead of derived names (#3191); association names default to random GUIDs instead of derived back-reference names (#2331); schema build errors on Refresh in LINQPad with ACE provider (#5235).

## Resolved themes

- **VS2022 64-bit incompatibility** -- OLE DB / ODBC drivers in 64-bit environment. Access requires 32-bit drivers on VS2022 (64-bit IDE) (#3344, #5311).
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

- Open issues: 3
- Closed issues: 22
- Open PRs: 2
- Total PRs: 13
- Discussions: 4 (all closed)
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 42 (25 issues + 13 PRs + 4 discussions)
- Themes extracted: 1 (open) + 8 (resolved)
</details>
