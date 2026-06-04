---
area: EXPR-TRANS
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Move Linq expression translation to dedicated namespace

## Context
Early linq2db had LINQ-to-SQL translation code co-located with broader infrastructure. As the query pipeline grew, the expression translation layer needed a cleaner namespace boundary to make the code navigable and to separate concerns from the SQL AST layer.

## Decision
Expression translation types were moved to a dedicated namespace. The commit subject is move linq with 66 files changed, 3938 insertions and 3802 deletions, indicating a wholesale relocation rather than incremental refactoring.

## Why
No rationale is stated in the commit body. The file-count and line-churn pattern is consistent with a namespace reorganization move.

## Consequences
- All callers referencing the old namespace required update.
- Established a stable namespace boundary for EXPR-TRANS that subsequent features built on.

## Sources
- Commit `dd1d412` -- move linq (MaceWindu, 2012-10-17)
