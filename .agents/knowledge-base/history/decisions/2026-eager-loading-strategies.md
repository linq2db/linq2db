---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# KeyedQuery and CteUnion eager-loading strategies alongside Default

## Context

The 2020 eager-loading design (commit `07af7caa`) runs one JOIN-shaped detail query per association level, with the parent's keys inlined as a VALUES table. That approach is now called the `Default` strategy. Issue #5450 asked for eager loading shaped for faster retrieval on workloads where `Default`'s per-level JOIN isn't the fastest option.

## Decision

PR #5450 (commit `f3e12dc`, 122 files, 14823 insertions/1038 deletions) added the `EagerLoadingStrategy` enum with two new values alongside `Default`:

- **`KeyedQuery`** (developed under the working name `PostQuery`, renamed mid-PR): extracts parent keys via a key-only projection, materializes them as a local VALUES table, and joins the child table to that local table -- avoiding a re-query of the parent table at each nesting level. Works at arbitrary nesting depth; buffer materialization (single main-query pass, keys extracted client-side from a `ValueTuple` buffer via `BufferReconstructionVisitor`) eliminates the separate key-extraction query entirely.
- **`CteUnion`**: wraps the parent query in a CTE and combines multiple same-level child associations into one `UNION ALL` query via a wide carrier `ValueTuple` (with slot reuse for same-CLR-type columns across branches), splitting rows back into per-child results by a `setId` discriminator column. Falls back to `KeyedQuery` on providers without CTE support or that exceed `MaxColumnCount`.

Entry points: `.AsKeyedQuery()` / `.AsUnionQuery()` per-query markers, `WithSeparateLoadStrategy`/`WithKeyedLoadStrategy`/`WithUnionLoadStrategy`, and a global `LinqOptions.DefaultEagerLoadingStrategy`. Fallback order on failure is whole-strategy (not per-expression): `CteUnion -> KeyedQuery -> Default`.

## Why

`KeyedQuery`'s local-VALUES-table join avoids re-querying the parent table at every association depth, and its buffer-materialization path collapses what would otherwise be an extra SELECT DISTINCT per nesting level down to one query per table level. `CteUnion`'s UNION-ALL batching reduces query count further when a level has multiple sibling associations (e.g. 2 sibling children collapse to a single SELECT once CTE wrapping was completed, corrected from an earlier interim "3 -> 2" measurement). Both are opt-in (or globally selectable) because `Default`'s simpler JOIN shape remains preferable for workloads without deep nesting or many sibling associations.

## Consequences

- New public API: `EagerLoadingStrategy` enum (`Default`/`KeyedQuery`/`CteUnion`), `AsKeyedQuery()`/`AsUnionQuery()`, `LinqOptions.DefaultEagerLoadingStrategy`.
- `ExpressionBuilder.EagerLoad*.cs` split into per-strategy partial files (`EagerLoadDefault`, `EagerLoadKeyedQuery`, `EagerLoadUnion`, shared `EagerLoad`/`EagerLoadState`).
- `SqlEagerLoadExpression` no longer carries a per-expression `Strategy`; strategy comes from `IBuildContext.TranslationModifier` or the global option.
- Composite (8+ member) `ValueTuple` keys in `KeyedQuery` grouping had a truncation defect (the client key-carry projection read only the first 7 tuple members, mis-grouping children differing only in later members); fixed within this PR by carrying the key as an explicit tuple rebuilt from leaf accessors instead of the whole VALUES element.
- Non-deterministic `KeyedQuery` VALUES key ordering (direct-vs-remote SQL divergence) was hardened by a follow-up fix in the same delta window (commit `47f9544`, #5665: ordinal, culture-invariant key sort at the single `SetKeys` choke point).

## Sources

- Commit `f3e12dc` -- Introduced two eager loading strategies for fastest data retrieval (#5450) (Svyatoslav Danyliv, 2026-06-29)
- Commit `47f9544` -- Fix non-deterministic VALUES key order in keyed eager-load (#5665) (MaceWindu, 2026-07-02)
- PR #5450
- File anchors: `Source/LinqToDB/EagerLoadingStrategy.cs`, `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoadKeyedQuery.cs`, `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoadUnion.cs`
