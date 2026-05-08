---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Move AsyncExtensions to separate namespace LinqToDB.Async (BREAKING)

## Context
AsyncExtensions lived in the LinqToDB root namespace alongside synchronous extension methods. This cluttered the root namespace intellisense and made it harder to selectively import async APIs.

## Decision
Commit `5a7e062` (#4981) moved AsyncExtensions to the LinqToDB.Async namespace. The old location received [Obsolete] forwarding methods for one major version to ease migration.

## Why
Consistent with the broader internal-namespace cleanup. The LinqToDB.Async namespace makes the async extension methods discoverable via an explicit using directive.

## Consequences
- Users who used await query.ToListAsync() without explicitly importing LinqToDB needed to add using LinqToDB.Async.
- The breaking change was listed in 5.x migration notes.

## Sources
- Commit `5a7e062` -- Move AsyncExtensions to separate namespace LinqToDB.Async (#4981) (MaceWindu, 2025)
