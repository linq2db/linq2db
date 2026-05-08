---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# ClickHouse provider addition

## Context
ClickHouse gained traction as an analytical database. ClickHouse has a non-standard SQL dialect (no EXISTS, limited DML) and uses its own client libraries (ClickHouse.Client, Octonica.ClickHouseClient, HTTP transport). Several linq2db users requested first-class support.

## Decision
Commit `5c75264` (2022, #3627) added the ClickHouse provider covering all three driver variants. The provider implements ClickHouseSqlBuilder, ClickHouseMappingSchema, and ClickHouseDataProvider. Because ClickHouse does not support EXISTS, commit `08dd7eb` (Feb 2023) switched correlated subquery rewrites to use IN instead. Hint extensions were added in `99511fe` (May 2023).

## Why
ClickHouse was the first column-store/analytical provider added to linq2db. The multi-driver design (HTTP, TCP via Octonica, TCP via ClickHouse.Client) avoids forcing a single client dependency.

## Consequences
- Source/LinqToDB/DataProvider/ClickHouse/ added.
- EXISTS subquery rewrites required a ClickHouse-specific optimizer override.
- Added to the NuGet package set as linq2db.ClickHouse.

## Sources
- Commit `5c75264` -- ClickHouse provider (#3627) (MaceWindu, 2022)
- Commit `08dd7eb` -- ClickHouse: replace EXISTS with IN (Igor Tkachev, 2023-02)
