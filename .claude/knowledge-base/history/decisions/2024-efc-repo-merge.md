---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Move linq2db.EntityFrameworkCore to main repository

## Context
linq2db.EntityFrameworkCore was maintained in a separate GitHub repository. This split caused release synchronization friction: EFCore package releases lagged linq2db core releases, and cross-repo PRs were needed for changes that touched both. The EFCore integration test coverage also lagged the main repo.

## Decision
Commit `0c99d98` (2024, #4595) merged the EntityFrameworkCore project into the main linq2db repository. The Source/LinqToDB.EntityFrameworkCore/ directory was added; CI pipelines were updated to include EFCore testing; the standalone repository was archived.

## Why
Commit body (#4595) cites improved release synchronization and unified CI as primary motivations.

## Consequences
- EFCore integration releases now track linq2db core releases exactly.
- Source/LinqToDB.EntityFrameworkCore/ and Tests/Tests.EntityFrameworkCore/ were added to the main solution.
- The linq2db.EntityFrameworkCore NuGet is now published from the main CI pipeline.

## Sources
- Commit `0c99d98` -- Move linq2db.EntityFrameworkCore to main repo (#4595) (MaceWindu, 2024)
