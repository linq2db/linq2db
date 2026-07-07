---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Remoting refactor and new transport layer

## Context
The LinqService / RemoteDataContextBase remoting path used WCF-style message serialization and a single transport interface. Adding new transports (gRPC, HTTP) required forking LinqService. The serializer was also a performance bottleneck for large result sets.

## Decision
Commit `459fb64` (2022, #3410) refactored the remoting stack to separate the transport interface from the serializer. A new ILinqService interface was introduced with async-first methods; the WCF/SOAP transport became one implementation. The serializer was made injectable.

## Why
The refactor was prerequisite for the 2025 HTTP transport addition (`c28e167`). Separating transport from serialization allows each to evolve independently.

## Consequences
- Source/LinqToDB/ServiceModel/ was reorganized.
- RemoteDataContextBase became transport-agnostic.
- The 2025 linq2db.Remote.Http.Client/Server package was built on this foundation.

## Sources
- Commit `459fb64` -- Refactored remoting / new transport layer (#3410) (MaceWindu, 2022)
