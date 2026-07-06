---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# Fluent Upsert API and fluent entity Insert/Update, replacing InsertOrUpdate overloads

## Context

linq2db's `InsertOrUpdate` overloads (issue #2558) were awkward for expressing conditional MERGE-shaped operations (skip an insert/update branch, per-branch predicates, non-PK match columns), forcing users either into a narrow single-shape API or the lower-level `Merge()` builder.

## Decision

PR #5482 (commit `3c0785f`, 52 files, 4600 insertions/39 deletions) added a fluent `Upsert`/`UpsertAsync` API (`table.Upsert(item or items).Match(...).Insert(i => ...).Update(v => ...)`) plus sibling fluent entity `Insert`/`Update` builders, developed in phases:

- Phase 1: single-entity SQLite-only, PK-based `.Match`, whole-object defaults overlaid by per-branch `.Set`/`.Ignore`, native `ON CONFLICT` emission.
- Phase 2: `.SkipInsert`/`.SkipUpdate`/`.DoNothing()`/`.When(cond)` per-branch modifiers, plumbed through a new `SqlInsertOrUpdateStatement.UpdateWhere` search condition.
- Phase 3: when a configuration needs something `ON CONFLICT` can't express (non-PK match, `.SkipInsert`, `.When` on the insert branch), `UpsertBuilder` synthesizes an equivalent `table.Merge().Using(...).On(...).InsertWhenNotMatchedAnd(...).UpdateWhenMatchedAnd(...)` expression tree and hands it to the existing `MergeBuilder` dispatchers instead of throwing.
- Phase 4: bulk `IEnumerable<TSource>`/`IQueryable<TSource>` sources, unconditionally routed through MERGE synthesis (`ON CONFLICT` is single-row).
- Phase 5: per-provider MERGE `WHERE`-placement fixes (Oracle/DB2 want `WHEN MATCHED THEN UPDATE SET ... WHERE cond`; SQL Server/PG15+ want `WHEN MATCHED AND cond THEN UPDATE`), match-column exclusion from whole-object UPDATE defaults (Oracle rejects updating a column referenced in the MERGE ON clause), and a 3-query (guarded `SELECT 1` / guarded `UPDATE` / guarded `INSERT`) alternative path for `.When` on providers with neither native MERGE-with-predicate nor `ON CONFLICT` (SqlCe/Informix/Access/SAP HANA/PostgreSQL 9.2-9.3, later extended to MySQL/MariaDB/Sybase/SQL Server 2005), replacing a prior UPDATE-then-IF-ROWCOUNT-INSERT emulation that could double-INSERT when `.When` rejected a matched row.

The parallel fluent entity `Insert(setter)`/`Update(setter)` builders (`EntityInsertBuilder`/`EntityUpdateBuilder`/`EntitySetterBuilder`) share the same setter-parsing infrastructure and support entity types without a public parameterless constructor by building a `SqlGenericConstructorExpression` directly instead of `Expression.New`.

## Why

Native-path-first with MERGE-as-fallback keeps the common case (single-row PK-matched upsert) on the cheapest SQL shape per provider, while still supporting the full conditional-MERGE feature set for callers who need it, without requiring every caller to hand-write a `Merge()` chain. The 3-query alternative path for `.When` was needed because the naive UPDATE-then-check-affected-rows emulation cannot distinguish "predicate rejected the row" from "row doesn't exist," which would otherwise INSERT a duplicate.

## Consequences

- New public API: `IUpsertable<TTarget,TSource>`, `IUpsertInsertSpec`/`IUpsertUpdateSpec`, `LinqExtensions.Upsert`/`UpsertAsync`, `IEntityInsertSpec`/`IEntityUpdateSpec`, `LinqOptions.ThrowOnUpsertEmulation`, `UpsertEmulationPolicy`.
- `SqlInsertOrUpdateStatement.UpdateWhere` is new AST surface, serialized for remote execution.
- New `SqlProviderFlags`: `IsUpsertWithMergeLoweringSupported`, `IsUpsertMergeWithPredicateSupported`, `IsInsertOrUpdateWithPredicateSupported`, plus `BasicSqlBuilder.IsUpsertUpdateWhereAfterSet`.
- Known caveat: the synthesized-MERGE path inlines source values into the USING SELECT rather than parameterizing them, so repeated single-item upserts against the same shape each compile fresh SQL (a performance, not correctness, gap -- confirmed by dedicated cache-parameterization tests) queued as future work.
- Follow-on review items open at merge time (tracked separately): auto-deriving MERGE from a stored upsert config, nested-bind support in entity Insert/Update setters, SAP HANA UPSERT routing.

## Sources

- Commit `3c0785f` -- Fluent Upsert + Insert/Update entity API (#5482) (Svyatoslav Danyliv, 2026-06-14)
- PR #5482
- File anchors: `Source/LinqToDB/LinqExtensions/LinqExtensions.Upsert.cs`, `Source/LinqToDB/Internal/Linq/Builder/UpsertBuilder.cs`, `Source/LinqToDB/Internal/Linq/Builder/EntityInsertBuilder.cs`, `Source/LinqToDB/Internal/Linq/Builder/EntityUpdateBuilder.cs`
