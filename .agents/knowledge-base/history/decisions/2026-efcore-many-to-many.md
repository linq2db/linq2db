---
area: EFC
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd242e6399daa081ca
---

# EF Core many-to-many (skip navigation) association translation

## Context

EF Core models many-to-many relationships via skip navigations backed by a hidden join entity (CLR type Dictionary). EFCoreMetadataReader did not map skip navigations, so linq2db queries using them either fell back to client evaluation or threw.

## Decision

EFCoreMetadataReader was extended to build association query expressions joining target entities through the implicit join table via an EfJoinTable marker type (commit bb5b975, Jun 10, #5588). A join-entity discriminator in the marker handles self-referencing many-to-many and multiple distinct relationships between the same entity pair. Composite FK keys are handled by the existing FK-column loop. Field-mapped and shadow principal keys are referenced by DB column name via Sql.Property rather than member access, because private backing fields cannot be referenced by CLR member access in linq2db.

## Why

Sql.Property provides the DB-column-name reference path that works for both field-mapped and shadow properties. The discriminator in the EfJoinTable marker is required because without it, self-referencing many-to-many and multiple relationships between the same pair would resolve ambiguously to the same join table.

## Consequences

- Skip navigation associations are now queryable, filterable, and eager-loadable via linq2db EF Core integration.
- Multiple implicit joins between the same entity pair throw a clear exception.
- File anchors: Source/LinqToDB.EntityFrameworkCore/EFCoreMetadataReader.cs

## Sources

- Commit bb5b975 -- EF Core: translate many-to-many (skip navigation) associations (#5588) (Svyatoslav Danyliv, 2026-06-10)
