---
area: SQL-AST
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Rename SqlBuilder to SqlQuery

## Context
The central SQL AST node type was originally named SqlBuilder. As the codebase matured it became clear that this name was confusing: builder implies a construction helper, not the AST representation itself. The type needed a name that described what it is (a query model) rather than how it is used.

## Decision
SqlBuilder was renamed to SqlQuery across the codebase. Subject: rename SqlBuilder -> SqlQuery; 48 files changed, 3193 insertions, 3173 deletions -- essentially a search-and-replace rename across the whole SQL layer.

## Why
No rationale is stated in the commit body. The rename aligns the type name with its role as the AST representation of a SELECT query.

## Consequences
- All provider, builder, and test code referencing SqlBuilder (as the query model) required update.
- Established the SqlQuery name that the SQL AST layer uses to this day.

## Sources
- Commit `1583a2b` -- rename SqlBuilder -> SqlQuery (MaceWindu, 2013-06-28)
