---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Eager Loading (LoadWith/ThenLoad) via separate detail queries

## Context
Association navigation in linq2db 2.x emitted subqueries or JOINs that produced Cartesian products for multi-level associations. There was no mechanism to batch-load related entities in separate round-trips while still using LINQ syntax. The feature (issue #1756) had been a long-standing gap vs Entity Framework.

## Decision
Commit `07af7caa` (Mar 25, 2020, FC:77) merged the WIP eager loading implementation. The approach uses separate secondary queries per association level rather than JOINs: the primary query runs first and collects keys; detail queries are parameterized by those keys via SqlValuesTable. LoadWith<T> and ThenLoad builder APIs expose the feature. A DefaultMultiQueryIsolationLevel was added to providers to control transaction isolation for multi-query loads.

## Why
Separate queries avoid Cartesian product row multiplication for 1:N associations. The commit body describes an iterative algorithm that propagates needed keys through the LINQ method chain using expression transformation rather than requiring the user to write explicit join syntax.

## Consequences
- Source/LinqToDB/Linq/Builder/EagerLoading.cs is the core of this feature.
- Multi-threading issue in the eager loading path was fixed in `6fd5bdd` (Oct 2021) and `6aa2782` (Sep 2021).
- LoadWith/ThenLoad became the recommended alternative to Join for entity-graph loading.

## Sources
- Commit `07af7caa` -- [WIP] Feature Eager Loading (#1756) (Svyatoslav Danyliv, 2020-03-25)
- Commit `6fd5bdd` -- Fix for #3242 multithreading issue with Eager Loading (Svyatoslav Danyliv, 2021-10-14)
