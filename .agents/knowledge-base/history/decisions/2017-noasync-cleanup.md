---
area: CORE
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remove NoAsync code paths

## Context
linq2db maintained two parallel execution paths: one using async ADO.NET APIs and one fallback path labeled NoAsync for environments or drivers that did not support async. As .NET async support matured and driver coverage improved, the NoAsync path became redundant overhead.

## Decision
The NoAsync code paths were removed. Commit subject: remove NoAsync; 41 files changed, 1044 insertions, 3163 deletions -- a net removal of approximately 2100 lines.

## Why
No rationale is in the commit body. The net-negative line count and scope across 41 core and provider files indicate a deliberate cleanup of a fallback mechanism whose purpose was obsoleted by platform progress.

## Consequences
- All callers that branched on a NoAsync flag had those branches removed.
- Reduced the async execution surface to a single code path, simplifying future maintenance.
- Drivers without async support would need alternative handling if they existed.

## Sources
- Commit `61706ff` -- remove NoAsync (MaceWindu, 2017-07-19)
