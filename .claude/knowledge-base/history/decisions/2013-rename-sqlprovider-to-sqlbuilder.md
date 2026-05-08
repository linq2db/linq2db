---
area: SQL-PROVIDER
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Rename SqlProvider to SqlBuilder

## Context
The SQL generation layer (the component that walks the AST and emits SQL text) was originally named SqlProvider. That name collided conceptually with the data-provider abstraction (IDataProvider). Renaming to SqlBuilder reserved provider exclusively for the data-access layer.

## Decision
SqlProvider was renamed to SqlBuilder. Subject: rename SqlProvider -> SqlBuilder; 27 files changed, 1434 insertions, 1424 deletions.

## Why
No rationale in the commit body. The rename disambiguates provider (data access) from builder (SQL text generation).

## Consequences
- All SQL generation classes and their callers had updated names.
- The SQL-PROVIDER area now has a stable naming split: IDataProvider for data access, ISqlBuilder for SQL generation.

## Sources
- Commit `05f1194` -- rename SqlProvider -> SqlBuilder (MaceWindu, 2013-07-02)
