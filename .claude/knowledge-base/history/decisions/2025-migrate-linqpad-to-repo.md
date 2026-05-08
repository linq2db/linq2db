---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Migrate LINQPad driver to main repository

## Context
The linq2db LINQPad driver was maintained in a separate repository. As with the EFCore integration, this caused release synchronization issues and split the CI/test burden.

## Decision
Commit `60f9a0d` (#5104) merged the LINQPad driver into the main repository. The Source/LinqToDB.LINQPad/ project was added; CI was updated.

## Why
Same motivation as the 2024 EFCore repo merge: unified CI, synchronized releases, reduced cross-repo friction.

## Consequences
- Source/LinqToDB.LINQPad/ added.
- The LINQPad driver now releases in lockstep with the core package.
- The standalone driver repository was archived.

## Sources
- Commit `60f9a0d` -- Migrate LINQPad driver to main repo (#5104) (MaceWindu, 2025)
