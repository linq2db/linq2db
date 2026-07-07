---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Add MySqlConnector provider (PR #1470)

## Context
linq2db MySQL support was built on the official MySql.Data (Oracle) driver. MySqlConnector is a community-built, fully async MySQL driver with better performance characteristics and a more permissive license. Users operating in high-throughput scenarios requested first-class support for it as an alternative backend.

## Decision
A dedicated MySqlConnector data provider was added alongside the existing MySql.Data provider. Commit subject: Add MySqlConnector provider (#1470); 57 files changed, 8184 insertions, 7986 deletions. The high deletion count reflects refactoring the existing MySQL provider to share code with the new one.

## Why
The commit body (PR #1470) describes the addition and includes iterative test fixes. No architectural rationale beyond the addition itself.

## Consequences
- ProviderName.MySqlConnector added to the public provider registry.
- MySQL provider code split to share a base class between the two driver backends.
- Test infrastructure updated to select the correct provider for each test run configuration.
- Users can now choose between MySql.Data and MySqlConnector at connection-string level.

## Sources
- Commit `8f6de5f` -- Add MySqlConnector provider (#1470) (Mitchell Kutchuk, 2018-12-22)
- PR #1470
- File anchors: `Source/LinqToDB/DataProvider/MySql/MySqlDataProvider.cs`, `Source/LinqToDB/ProviderName.cs`
