---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove .NET 6 support (BREAKING)

## Context
.NET 6 reached end of support in November 2024. Maintaining net6.0 as a TFM imposed ongoing test matrix cost and prevented using .NET 7/8 APIs unconditionally.

## Decision
Commit `ee83817` (#4937) removed the net6.0 TFM from all projects. The minimum supported TFM became net8.0 for modern .NET alongside the retained net462/netstandard2.0 for legacy scenarios.

## Why
Standard policy: drop TFMs at Microsoft end-of-life date. Commit subject explicitly states Remove .NET 6 support.

## Consequences
- net6.0-specific #if guards and test matrix entries were removed.
- Consumers on .NET 6 must upgrade or stay on the previous linq2db release.

## Sources
- Commit `ee83817` -- Remove .NET 6 support (#4937) (MaceWindu, 2025)
