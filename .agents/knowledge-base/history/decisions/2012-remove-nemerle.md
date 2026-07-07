---
area: GLOBAL
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove Nemerle support

## Context
linq2db originally shipped with T4 template support for multiple languages including Nemerle. Nemerle is a statically-typed functional/OOP language for .NET that had limited adoption; maintaining a separate code-generation path for it added surface area without a proportional user base.

## Decision
The Nemerle T4 templates and related support files were removed from the repository. The commit subject is remove nemerle.

## Why
No rationale is given in the commit body. The removal is consistent with a pattern of dropping low-adoption language targets to reduce template-maintenance overhead.

## Consequences
- Nemerle users lost generated scaffolding support; no migration path was documented in the commit.
- T4 template surface reduced by one language variant.

## Sources
- Commit `9d104c5` -- remove nemerle (MaceWindu, 2012-10-19)
- File anchors: `T4Models/` (affected directory)
