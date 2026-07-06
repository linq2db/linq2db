---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# Named (multiple) query filters, mirroring EF Core 10's keyed-filter API

## Context

linq2db's query filters (2020, commit `0f19c27`) supported one anonymous filter lambda per entity type. EF Core 10 introduced named/keyed query filters (multiple filters per entity, each addressable and independently ignorable), and the EF Core companion (`LinqToDB.EntityFrameworkCore`) needed to translate that shape rather than collapse it.

## Decision

PR #5525 (commit `c3fee23`, 20 files, 1359 insertions/108 deletions) added a `filterKey`-keyed overload set to `EntityMappingBuilder.HasQueryFilter`. Multiple named filters on one entity type are AND-combined at query time; passing `null` as the filter for an existing key removes that entry. `IgnoreFilters` gained `(IEnumerable<string> filterKeys, params Type[] entityTypes)` overloads to disable filters by key and/or key+type intersection, matching EF Core's `IgnoreQueryFilters` no-op-on-empty-collection semantics. `EntityDescriptor.QueryFilters` exposes the full keyed collection while the legacy single-slot `QueryFilterLambda`/`QueryFilterFunc` accessors stay as the empty-key default slot for back-compat. `TranslationModifier` switched from a single ignore-flag to a `FilterIgnoreScope` list (normalized to a canonical sorted/distinct form so equivalent scopes hash identically for the query cache). On the EF Core 10 bridge, `EFCoreMetadataReader` forwards `IQueryFilter.Key` into `QueryFilterAttribute.FilterKey` and `TransformExpressionVisitor` remaps EF's `IgnoreQueryFilters(IReadOnlyCollection<string>)` onto the new keyed `IgnoreFilters`.

## Why

Mirroring EF Core's named-filter shape (rather than inventing a distinct linq2db model) lets `LinqToDB.EntityFrameworkCore` translate EF 10 query filters 1:1, including empty-collection and derived-type override semantics. `EntityDescriptor.InitQueryFilters` walks the type hierarchy base-to-derived so a derived class's filter for the same key overrides an inherited one, matching declaration-order expectations. `AssociationHelper`'s inline-association optional-result check was widened from `QueryFilterLambda != null` to `QueryFilters.Count > 0` so associations promote to LEFT JOIN + DefaultIfEmpty even when an entity carries only named filters (a soft-delete-via-named-filter setup would otherwise silently drop parent rows).

## Consequences

- New public API: `HasQueryFilter(string filterKey, ...)` overloads, keyed `IgnoreFilters`, `EntityDescriptor.QueryFilters`.
- `TranslationModifier` carries a `FilterIgnoreScope` list instead of a single boolean/type-set pair.
- Legacy anonymous-filter accessors remain the empty-key default slot -- no behavior change for callers that don't use keys.
- `QueryFilterAttribute.GetObjectID` length-prefixes the filter key so a key containing `.` can't collide with the id-segment encoding used for query-cache addressing.

## Sources

- Commit `c3fee23` -- Add named (multiple) query filters mirroring EF Core 10 API (#5525) (Svyatoslav Danyliv, 2026-07-02)
- PR #5525
- File anchors: `Source/LinqToDB/Mapping/EntityQueryFilter.cs`, `Source/LinqToDB/Mapping/QueryFilterAttribute.cs`, `Source/LinqToDB/Internal/Linq/Builder/FilterIgnoreScope.cs`
