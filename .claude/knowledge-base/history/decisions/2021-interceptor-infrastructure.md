---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Replace event-hook callbacks with typed Interceptor interfaces

## Context
Linq2db 3.x exposed extensibility via ad-hoc events and the IDbCommandProcessor interface. This pattern made it difficult to compose multiple interceptors, lacked type safety for event payloads, and mixed unrelated concerns (command preparation, connection lifecycle, entity creation) into a single interface.

## Decision
Commit `5a9d8e6` (Apr 3, 2021, FC:26) introduced the interceptor infrastructure and replaced IDbCommandProcessor with ICommandInterceptor. Commit `4a2ae76` (Jun 9, 2021, FC:170) converted the bulk of remaining event hooks: IDbCommandProcessor to ICommandInterceptor, IEntityServices to IEntityServiceInterceptor, IDataContext close events to IDataContextInterceptor. The design: callers implement typed interceptor interfaces; multiple interceptors are aggregated via AggregatedInterceptor.

## Why
Typed interfaces allow individual interceptors to implement only the callbacks they care about. Aggregation replaces multicast delegates, giving callers full control over execution order and short-circuit logic.

## Consequences
- Source/LinqToDB/Interceptors/ is the canonical namespace.
- LinqToDbConnectionOptionsBuilder carries the AddInterceptor() entry point.
- All legacy event subscriptions remain but delegate to an internal interceptor.

## Sources
- Commit `5a9d8e6` -- interceptors infrastructure + replace command prepared event (MaceWindu, 2021-04-03)
- Commit `4a2ae76` -- Convert more code to interceptors (#2941) (MaceWindu, 2021-06-09)
