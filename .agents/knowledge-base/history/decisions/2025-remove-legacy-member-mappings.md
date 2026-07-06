---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove old mappings for members with translations (BREAKING)

## Context
Linq2db maintained a dual mechanism for translating .NET member accesses to SQL: old-style Expressions.MapMember registrations and the newer MemberTranslator plugin approach. Over time the new approach covered all cases the old mappings handled, making the old registrations redundant and a source of confusion when both fired.

## Decision
Commit `a701bd2` (#5130) removed the legacy MapMember-based registrations for all members that had corresponding MemberTranslator implementations. This affected date/time, string, math, and collection members across all providers.

## Why
Removing the old registrations eliminates the dual-code-path maintenance burden and ensures MemberTranslator is the single authoritative implementation. The commit body notes this as a BREAKING change.

## Consequences
- Any custom MapMember override targeting the same members as a MemberTranslator would now take effect differently.
- Expressions.MapMember is still available for custom members not covered by a translator.
- Provider-specific SQL for date/time functions is now exclusively in MemberTranslator subclasses.

## Sources
- Commit `a701bd2` -- Remove old mappings for members with translations (#5130) (MaceWindu, 2025)
