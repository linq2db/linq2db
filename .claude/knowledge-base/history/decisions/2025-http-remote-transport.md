---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# LinqToDB.Remote.Http.Client/Server transport

## Context
The linq2db remoting feature relied historically on WCF transport. WCF was not available on .NET Core/.NET 5+. The 2022 remoting refactor (`459fb64`) separated transport from serializer in preparation for alternatives.

## Decision
Commit `c28e167` (#4775) added linq2db.Remote.Http.Client and linq2db.Remote.Http.Server packages implementing an HTTP/REST-based transport for RemoteDataContextBase. The client uses HttpClient; the server exposes an ASP.NET Core minimal API endpoint. JSON serialization is used by default.

## Why
HTTP is universally available on all .NET targets including WASM and mobile. The 2022 transport-interface refactoring made adding this transport a contained change.

## Consequences
- Source/LinqToDB.Remote.Http.Client/ and Source/LinqToDB.Remote.Http.Server/ added.
- WCF transport retained for existing users.
- The HTTP transport is the recommended approach for new .NET 5+ deployments.

## Sources
- Commit `c28e167` -- LinqToDB.Remote.Http.Client/Server (#4775) (MaceWindu, 2025)
- Commit `459fb64` -- Refactored remoting / new transport layer (#3410) (MaceWindu, 2022)
