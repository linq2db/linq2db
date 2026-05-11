---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# DataOptions and options-class refactoring (BREAKING)

## Context
Linq2db configuration used a mix of global Configuration.* static properties and per-connection builder APIs with inconsistent naming. Adding new connection configuration required modifying LinqToDbConnectionOptionsBuilder and related types, causing API proliferation. The options model was not conducive to DI-based configuration.

## Decision
Commit `b6d5f78` (Jan 11, 2023, FC:411, #3530) rewrote options handling by distributing configuration across typed option classes (ConnectionOptions, MappingOptions, QueryOptions, LinqOptions, RetryPolicyOptions, BulkCopyOptions) grouped under a new DataOptions record. Follow-up commit `f90d762` (Jan, FC:66, #3942) introduced explicit breaking changes to the fluent configuration API; `738fd90` (Feb, FC:72, #3969) moved EntityDescriptorCreatedCallback to options.

## Why
Commit `b6d5f78` body cites 11,667 insertions / 6,165 deletions and describes a full rewrite of the configuration path. The record-based approach makes configuration immutable per-connection and composable in DI scenarios.

## Consequences
- DataOptions became the primary configuration surface (later extended by UseOptions/UseMappingSchema in 2025).
- Global Configuration.* properties were progressively obsoleted.
- The fluent builder changes were classified as breaking changes in the 5.x release notes.

## Sources
- Commit `b6d5f78` -- Refactoring options (#3530) (Svyatoslav Danyliv, 2023-01-11)
- Commit `f90d762` -- Resubmit fluent configuration breaking changes (#3942) (MaceWindu, 2023-01)
- Commit `738fd90` -- Move EntityDescriptorCreatedCallback to options (#3969) (MaceWindu, 2023-02)
