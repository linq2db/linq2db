---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# LINQ Query parsing v6 refactoring

## Context
The ExpressionBuilder and associated LINQ builder classes had accumulated six years of incremental patches. Complex queries involving CTEs, set operators, subquery flattening, and multi-level joins had known correctness edge cases. The builder architecture used a shared mutable context that made adding new SQL features difficult without regression risk.

## Decision
Commit `7ec71d5` (2024, #3401) performed a large-scale refactoring of LINQ query parsing. The builder context was restructured to reduce shared mutable state; visitor patterns were regularized; the set-operator and CTE builder paths were rewritten.

## Why
The change addressed issue #3401 which accumulated over 70 comments over multiple years. The refactoring addressed the root cause of several related correctness bugs rather than patching each individually.

## Consequences
- Source/LinqToDB/Linq/Builder/ was substantially reorganized.
- Several previously [ActiveIssue] test cases were fixed and re-enabled.
- New SQL features (window functions, new LINQ methods) added in 2025 depend on the restructured builder.

## Sources
- Commit `7ec71d5` -- Refactoring LINQ Query parsing v6 (#3401) (MaceWindu, 2024)
