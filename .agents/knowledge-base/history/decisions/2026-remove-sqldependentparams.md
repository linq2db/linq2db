---
area: EXPR-TRANS
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
---

# Remove SqlQueryDependentParamsAttribute from Sql.Expr and deprecate for v7 removal

## Context

`[SqlQueryDependentParams]` was applied to the `parameters` argument of `Sql.Expr<T>` to trigger per-parameter `Expression.Compile()` on the query-cache comparison path. Issue #5154 showed this is unsafe when the applied parameter captures outer-scope transparent identifiers: the compiled delegate binds to the outer scope at compile time, causing incorrect cache hits when the captured value changes. The default structural comparison covers the cases the attribute was intended to handle.

## Decision

PR #5526 (commit `d455997`) removed `[SqlQueryDependentParams]` from the `Sql.Expr<T>.parameters` declaration -- the only first-party use inside the product. `SqlQueryDependentParamsAttribute` was then marked `[Obsolete]` with a message directing external users to migrate before v7, where the attribute will be physically removed. A revert of an unrelated `this.Expression` mutation in `ExpressionQuery.GetSqlQueries` was included (the removal had broken `BinaryExpressionAggregatorVisitor` balancing for 4+-leaf `AndAlso` chains).

## Why

The attribute's `Expression.Compile()` on the cache-compare path is incorrect when the parameter captures a transparent identifier from a surrounding LINQ scope. The structural comparison that is the default covers the same use cases safely. No first-party code used the attribute after the `Sql.Expr` removal, so deprecation rather than immediate removal was chosen to give external consumers a migration window.

## Consequences

- External extension methods applying `[SqlQueryDependentParams]` now receive an `[Obsolete]` warning at compile time.
- The attribute will be physically removed in v7.
- `Sql.Expr<T>` parameters are compared structurally by default; per-parameter compilation on cache lookup no longer occurs for built-in Sql.Expr overloads.
- Issue #5154 (incorrect query-cache hits for closures in `Sql.Expr` parameters) is resolved.

## Sources

- Commit `d455997` -- Fix #5154: remove [SqlQueryDependentParams] from Sql.Expr params (MaceWindu, 2026-05-xx)
- PR #5526
- File anchors: `Source/LinqToDB/Mapping/SqlQueryDependentParamsAttribute.cs`, `Source/LinqToDB/Sql/Sql.Expressions.cs`
