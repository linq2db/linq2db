---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Refactor provider configuration and versioning

## Context
Provider version selection (e.g. SqlServerVersion.v2019, FirebirdVersion.FB4) was scattered across provider factory methods, ProviderName string constants, and IDataProvider.Name-based dispatch. Adding a new provider version required changes in multiple places and was inconsistent across providers.

## Decision
Commit `4704ace` (2024, #4002) refactored provider configuration and version detection into a unified model. Each provider now has a typed version enum and a centralized ProviderOptions record. Version detection from connection strings was consolidated into provider-specific detectors.

## Why
The refactoring was a prerequisite for the 2025 UseOptions/UseMappingSchema API and for consistent provider configuration in the EFCore integration merged in 2024.

## Consequences
- ProviderName string constants retained for backward compatibility but provider-specific version enums are the canonical API.
- Provider factory methods delegate to options-based construction.
- Version detection results are cached per connection string.

## Sources
- Commit `4704ace` -- Refactor provider configuration and versioning (#4002) (MaceWindu, 2024)
