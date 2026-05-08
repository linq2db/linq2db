---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Add SAP HANA provider

## Context
linq2db supported a growing list of relational databases. SAP HANA is an in-memory column-store database used in enterprise scenarios; adding it extended linq2db reach into that market segment.

## Decision
A full SAP HANA data provider was added, including SapHanaDataProvider, SapHanaMappingSchema, SapHanaSqlBuilder, and supporting SQL creation scripts. Commit subject: SAP HANA provider; 42 files changed, 5523 insertions, 18 deletions. A large net-additive change confined to a new provider area.

## Why
The commit body is empty beyond the subject line. The scale of the addition (5500+ insertions, dedicated mapping schema, SQL builder, and test data scripts) indicates this was a planned provider addition rather than an opportunistic contribution.

## Consequences
- ProviderName.SapHana added to the public provider registry.
- New dependency on the SAP HANA ADO.NET driver.
- Test suite extended with SAP HANA-specific data-provider tests and SQL creation scripts.
- Established the pattern for adding new enterprise providers that subsequent contributions followed.

## Sources
- Commit `f0c6b8c` -- SAP HANA provider (MaceWindu, 2014-03-28)
- File anchors: `Source/LinqToDB/DataProvider/SapHana/`, `Data/Create Scripts/SapHana.sql`
