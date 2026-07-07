---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
---

# AsQueryable parameterization configuration API

## Context

The existing `AsQueryable(IEnumerable<T>, IDataContext)` overload rendered in-memory sequences as a `VALUES` clause with all cells either always parameterized or always inlined, depending on internal defaults. There was no public API for callers to control whether individual columns within the VALUES rows should be emitted as SQL parameters or as inlined literals. This was a gap for use cases where, for example, primary-key columns need to be inlined for plan stability while nullable metadata columns need to be parameterized.

## Decision

Commit `bda0798a3` (PR #5495, 2026-05-10) added a configuration-chain API:

- `IAsQueryableBuilder<T>` (new public interface) -- entry point; exposes `Parameterize()` and `Inline()` to set the default rendering mode for all columns.
- `IAsQueryableExceptBuilder<T>` (new public interface) -- available after a mode is chosen; exposes `Except(params Expression<Func<T, object?>>[] members)` to flip the mode for named columns.
- `AsQueryable<TElement>(IEnumerable<TElement>, IDataContext, Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>> configure)` -- new three-argument overload on `LinqExtensions`.
- `EnumerableParameterizationConfig` (internal sealed class in `LinqToDB.Internal.Linq.Builder`) -- carries the resolved `DefaultForceParameter` flag and the list of excepted `MemberExpression` nodes; consumed by `EnumerableContext` during SQL generation.

The configure lambda is captured as an expression tree and interpreted at query-build time. Calling the builder methods outside an expression context is undefined behavior (they are marker-only at runtime).

## Why

The design was chosen to keep the configuration declarative and expression-tree-based, consistent with the existing LINQ-expression approach used throughout the library. An alternative of accepting a `bool` flag or a column-name list was not taken because member-access expressions provide compile-time safety and IDE rename support. `DataParameter`-mapped columns are always emitted as parameters regardless of the inline mode, because `DataParameter` carries provider metadata that cannot be inlined.

## Consequences

- `IAsQueryableBuilder<T>` and `IAsQueryableExceptBuilder<T>` are new public interfaces in `LinqToDB.Linq`.
- The new `AsQueryable` overload is added to `LinqToDB.LinqExtensions` alongside the two existing overloads.
- `EnumerableParameterizationConfig` is internal; the public contract is fully expressed through the two interfaces and the `LinqExtensions` overload.
- Passing a selector that is not a simple member access (e.g. `p => p.Id + 1`, `p => p`, a captured external member) throws `LinqToDBException` at query-build time.

## Sources

- Commit `bda0798a3` -- Add AsQueryable parameterization API (#5495) (MaceWindu, 2026-05-10)
- PR #5495
- File anchors: `Source/LinqToDB/Linq/IAsQueryableBuilder.cs`, `Source/LinqToDB/Linq/IAsQueryableExceptBuilder.cs`, `Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs`, `Source/LinqToDB/Internal/Linq/Builder/EnumerableParameterizationConfig.cs`
