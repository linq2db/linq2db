---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Refactor mapping attribute resolution

## Context
Mapping attributes (ColumnAttribute, TableAttribute, etc.) were read by multiple code paths that each performed their own GetCustomAttributes calls, leading to inconsistent results when attributes were inherited, stacked, or overridden via fluent configuration. Performance was also a concern: repeated reflection scans per entity type.

## Decision
Commit `13fbe5f` (Jan 23, 2023, FC:218, #3900) consolidated all attribute reading into a single pipeline: attributes are resolved once per entity type through MappingSchema.GetAttributes<T>() which applies configuration inheritance, fluent overrides, and caching. The change touched 3,822 insertions / 3,459 deletions across the attribute resolution path.

## Why
The consolidation eliminated multiple competing attribute-read paths and ensured fluent mapping and attribute mapping are applied in a consistent, documented order. Caching moved to the centralized resolver.

## Consequences
- AttributeReader was refactored; MappingSchema.GetAttributes<T>() is the single entry point.
- Attribute resolution order (fluent > derived type > base type) became explicit and tested.
- The change fixed several edge cases where fluent-mapped columns were ignored when attributes were present.

## Sources
- Commit `13fbe5f` -- Refactor work with attributes (#3900) (MaceWindu, 2023-01-23)
