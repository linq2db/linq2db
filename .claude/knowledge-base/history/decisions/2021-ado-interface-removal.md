---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Replace ADO.NET interfaces with concrete base classes (BREAKING)

## Context
Linq2db 3.x used IDbConnection, IDataReader, IDbDataParameter, and IDbCommand throughout its core types. Modern .NET provides DbConnection, DbDataReader, DbParameter, and DbCommand as abstract base classes with additional members (e.g. DbDataReader.GetColumnSchemaAsync, DbCommand.ExecuteReaderAsync). Keeping interfaces blocked ValueTask-returning async overloads and integration with diagnostic listeners.

## Decision
In April 2021, MaceWindu replaced all ADO.NET interface references with concrete base-class types: `6cd2ab9` (IDbConnection to DbConnection, FC:63), `b87ee2a` (IDataReader to DbDataReader, FC:46), `b53fa29` (IDbDataParameter to DbParameter, FC:49), `1a206fa` (IDbCommand to DbCommand, FC:15). Commit `d31af52` (Jun 2021, FC:23) added a build-time banned-API rule to BannedSymbols.txt to prevent reintroduction of the ADO.NET interfaces.

## Why
Commit body for `6cd2ab9` states this was a deliberate breaking change to align with modern .NET and enable ValueTask-based overloads. The IAsyncDbConnection/IAsyncDbTransaction interfaces were simultaneously refactored to no longer mix with ADO.NET interfaces.

## Consequences
- Callers who passed IDbConnection-only implementations without the abstract base received compile errors.
- BannedSymbols.txt enforces the policy at build time in Release configuration.
- The change was listed in the 3.5.0/3.6.0 breaking-change notes.

## Sources
- Commit `6cd2ab9` -- IDbConnection to DbConnection (MaceWindu, 2021-04-05)
- Commit `b87ee2a` -- IDataReader to DbDataReader (MaceWindu, 2021-04-05)
- Commit `b53fa29` -- IDbDataParameter to DbParameter (MaceWindu, 2021-04-05)
- Commit `d31af52` -- merge fixes + ban ADO.NET interfaces (MaceWindu, 2021-06-04)
