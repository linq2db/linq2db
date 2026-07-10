---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Lock MappingSchema for thread safety

## Context
MappingSchema was mutable and shared across threads. Users who modified a global schema after first use (e.g. adding converters in request handlers) caused data races in the converter caches, producing intermittent incorrect conversions. Issue #2583 tracked the race.

## Decision
Commit `098f1f3` (2022, #2583) made MappingSchema lockable: once a schema is used in a data connection or context it is sealed and further mutations throw an InvalidOperationException. A new MappingSchema.Clone() method was provided for callers that need a mutable derived schema.

## Why
The sealing approach was chosen over concurrent-safe collections because the schema composite cache is not amenable to fine-grained locking without significant overhead. Sealing at first use is the same pattern used by ASP.NET Core IOptions.

## Consequences
- Callers who registered converters after first use received runtime exceptions, forcing registration to startup.
- MappingSchema.IsLocked property was added.
- Follow-up: EntityDescriptorCreatedCallback moved to DataOptions (`738fd90`, Feb 2023) for the same reason.

## Sources
- Commit `098f1f3` -- Lock MappingSchema (#2583) (MaceWindu, 2022)
