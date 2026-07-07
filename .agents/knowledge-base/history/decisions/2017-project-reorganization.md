---
area: GLOBAL
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Project reorganization

## Context
linq2db source layout had grown organically since 2012. By 2017 the solution contained multiple projects with overlapping responsibilities, and the directory structure no longer reflected the logical separation between the core library, providers, and tooling.

## Decision
The project structure was reorganized. Commit subject: Project reorganization; 74 files changed, 3697 insertions, 1879 deletions across a broad set of paths. This is one of the highest file-count changes of the era.

## Why
No rationale is stated in the commit body. The scale (74 files, multiple project files affected) indicates a deliberate structural decision rather than a feature addition.

## Consequences
- Build system and project references updated across the solution.
- Directory layout changed, requiring contributor tooling updates (IDE project references, build scripts).
- Subsequent provider additions followed the new layout.

## Sources
- Commit `beb417c` -- Project reorganization (MaceWindu, 2017-01-19)
