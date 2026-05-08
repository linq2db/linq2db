---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# UseOptions / UseMappingSchema configuration API

## Context
After the 2023 DataOptions refactoring, configuring a data connection in DI scenarios still required subclassing DataConnection or using LinqToDbConnectionOptionsBuilder. Callers could not layer configuration cleanly (e.g. apply a tenant-specific mapping schema on top of a shared base configuration).

## Decision
Commit `4bbfb81` (#4861) added UseOptions(DataOptions) and UseMappingSchema(MappingSchema) methods to DataConnection and DataContext as fluent configuration APIs. These methods return this and apply the given options/schema as a derived layer on the existing configuration without requiring subclassing.

## Why
The pattern mirrors ASP.NET Core IOptions<T> layering. It enables DI-registered services to pass a configured DataOptions instance directly to a connection without needing to know the connection full configuration.

## Consequences
- DataConnection.UseOptions and DataContext.UseOptions became the recommended DI-integration entry points.
- Combined with the 2025 DataContext-only API extensions (#5107), the public API for connection configuration became consistent across both context types.

## Sources
- Commit `4bbfb81` -- UseOptions / UseMappingSchema (#4861) (MaceWindu, 2025)
