---
area: GLOBAL
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Move Merge API to SqlQuery

## Context
After the 2017 Merge API addition, the MERGE statement was represented outside the core SqlQuery AST hierarchy. Integrating it into SqlQuery would allow the optimizer and SQL builder infrastructure to handle MERGE nodes uniformly alongside SELECT, INSERT, UPDATE, and DELETE.

## Decision
The Merge API representation was moved into SqlQuery. Commit subject: move merge api to SqlQuery; 58 files changed, 2416 insertions, 1999 deletions.

## Why
No rationale in the commit body. The move is a natural follow-on to the 2017 SelectQuery refactor, which had restructured the AST to better support complex statement types.

## Consequences
- MERGE nodes became first-class members of the SqlQuery type hierarchy.
- SQL builders for all providers updated to emit MERGE SQL from the new AST position.
- Unified query visitor and optimizer could now traverse MERGE nodes without special-casing.

## Sources
- Commit `459fa8a` -- move merge api to SqlQuery (MaceWindu, 2018-01-14)
