---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove SQL Server 2000 provider

## Context
SQL Server 2000 reached end of support in 2013. The provider required separate DDL paths and SQL builder classes (SqlServer2000SqlBuilder, SqlServer2000SchemaProvider, SqlServer2000SqlOptimizer) that had not been exercised in CI for several releases.

## Decision
Commit `3ac156d` (Apr 3, 2021, FC:43) deleted the SQL Server 2000 provider, removing SqlServer2000SqlBuilder.cs, SqlServer2000SchemaProvider.cs, SqlServer2000SqlOptimizer.cs, the create script SqlServer2000.sql, and all test and configuration references. Over 1,200 lines of legacy DDL and SQL-builder code were deleted.

## Why
The commit message is direct: Remove SQL Server 2000 Support. The timing coincides with the 3.3.x branch preparing for release; the ADO.NET interface migration the same week was also a breaking change window, making this an appropriate moment to drop the legacy target.

## Consequences
- SqlServerVersion.v2000 was removed from the version enum.
- Users on SQL Server 2000 (effectively none in 2021) needed to stay on linq2db 3.2.x or older.
- SqlServerFactory and SqlServerTools were simplified to start from v2005.

## Sources
- Commit `3ac156d` -- Remove SQL Server 2000 Support (MaceWindu, 2021-04-03)
