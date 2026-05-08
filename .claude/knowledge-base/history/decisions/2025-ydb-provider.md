---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# YDB provider preview

## Context
YDB (Yandex Database) is a distributed SQL database with a PostgreSQL-compatible wire protocol and a native .NET SDK. Users from the Yandex ecosystem requested linq2db support.

## Decision
Commit `91954d9` (#5218) added a YDB provider preview. The provider implements YDBDataProvider, YDBMappingSchema, and YDBSqlBuilder using the TypeMapper pattern. It was shipped as a preview package (linq2db.YDB) to gather feedback before stabilizing the API.

## Why
YDB native .NET SDK (Ydb.Sdk) is compatible with the TypeMapper approach established in 2020. The preview designation allows the provider interface to change in the next minor release without a semver major bump.

## Consequences
- Source/LinqToDB/DataProvider/YDB/ added.
- linq2db.YDB NuGet published as preview.
- The provider is not included in the main test matrix until it exits preview.

## Sources
- Commit `91954d9` -- YDB provider preview (#5218) (MaceWindu, 2025)
