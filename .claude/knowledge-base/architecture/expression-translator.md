---
area: GLOBAL
kind: architecture
sources: [code]
confidence: medium
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 63/63
coverage_tier_2: 17/71
---

# Expression translator

How linq2db turns a `System.Linq.Expressions.Expression` rooted on `IQueryable<T>` / `ITable<T>` into an `IBuildContext` graph and finally a finalized `SqlStatement` ready for [`SQL-PROVIDER`](../areas/SQL-PROVIDER/INDEX.md) emission. Living in [`Source/LinqToDB/Internal/Linq/Builder/`](../areas/EXPR-TRANS/INDEX.md), this is the largest single subsystem in the library: **134 files**, **62 root-level `*Builder` classes**, one central `ExpressionBuilder`, and a Roslyn source generator that wires them together.

## Entry point

`ExpressionBuilder.Build<T>(ref IQueryExpressions expressions)` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:124`) is invoked by [`LINQ`](../areas/LINQ/INDEX.md) (`Internal/Linq/Query.cs`, `Internal/Linq/ExpressionQuery.cs`) on a query-cache miss. It opens an `ActivityID.Build` metric, creates a root `BuildInfo` with `parent=null`, the original `Expression`, and a fresh `SelectQuery` from `QueryPool`, and calls `BuildSequence(...)` to produce an `IBuildContext` rooted at the LINQ tree. The returned context is handed to `_query.Init(sequence)` and then to `BuildQuery<T>` for materialization-mapper construction (line 162).

`BuildQuery<T>` finalizes the `SqlStatement`(s) attached to each `Query.Queries[i]` by:

1. Building the result expression via `_buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), BuildPurpose.Expression)` — this is the projection-resolution + materialization pass (line 169).
2. Running per-provider `query.SqlOptimizer.Finalize(...)` on each statement (line 184) — handoff to [`SQL-PROVIDER`](../areas/SQL-PROVIDER/INDEX.md).
3. Validating with `SqlProviderHelper.IsValidQuery` (line 188); failures attach a `SqlErrorExpression` to `query.ErrorExpression`.
4. Threading parameter accessors back into the query via `ParametersContext.ApplyAccessors` and `FinalizeQueryCacheInformation` (line 197, 207).

## Dispatch — how a method call finds its builder

There is no `MethodCallParser` class in this codebase. Dispatch is **source-generated**.

Each `*Builder` class declares which expression nodes it handles via attribute markers from `Source/LinqToDB/Internal/Linq/Builder/Attributes.cs`:

```
[BuildsMethodCall("Where", "Having")]    sealed class WhereBuilder    : MethodCallBuilder { ... }
[BuildsMethodCall("Select")]             sealed class SelectBuilder   : MethodCallBuilder { ... }
[BuildsMethodCall("Skip", "Take")]       sealed class TakeSkipBuilder : MethodCallBuilder { ... }
[BuildsMethodCall("OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending")]
                                         sealed class OrderByBuilder  : MethodCallBuilder { ... }
[BuildsExpression(ExpressionType.Constant, ExpressionType.Call, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
                                         sealed class EnumerableBuilder : ISequenceBuilder { ... }
[BuildsAny]                              sealed class ContextRefBuilder : ISequenceBuilder { ... }
```

The `BuildersGenerator` (`Source/CodeGenerators/BuildersGenerator.cs:14`, an `IIncrementalGenerator`) collects every class decorated with `[BuildsAny]`, `[BuildsExpression]`, `[BuildsMethodCall]` and emits a generated partial method body for `ExpressionBuilder.FindBuilderImpl` into `ExpressionBuilder.g.cs` (line 214). The generated body has the shape:

```
private static partial ISequenceBuilder? FindBuilderImpl(BuildInfo info, ExpressionBuilder builder)
{
    var expr = info.Expression = info.Expression.Unwrap();
    switch (expr.NodeType)
    {
        case ExpressionType.Constant:
            if (EnumerableBuilder.CanBuild(expr, builder))
                return Builder<EnumerableBuilder>.Instance;
            break;
        case ExpressionType.Call:
        {
            var call = (MethodCallExpression)expr;
            switch (call.Method.Name)
            {
                case "Where":
                    if (WhereBuilder.CanBuildMethod(call))
                        return Builder<WhereBuilder>.Instance;
                    break;
                case "Select":
                    if (SelectBuilder.CanBuildMethod(call))
                        return Builder<SelectBuilder>.Instance;
            }
            if (MethodChainBuilder.CanBuild(expr, info, builder))
                return Builder<MethodChainBuilder>.Instance;
            break;
        }
    }
    if (ContextRefBuilder.CanBuild(info))
        return Builder<ContextRefBuilder>.Instance;
    return null;
}
```

A few mechanics worth noting:

- **Singleton instance per builder.** The dispatcher returns `Builder<T>.Instance`, a static field initialized by `Builder<T> where T : ISequenceBuilder, new()` (`BuildersGenerator.cs:184`). Every `*Builder` is therefore stateless — all per-call state lives in `BuildInfo` / the produced `IBuildContext`. This is why `WhereBuilder`, `SelectBuilder`, etc. are `sealed class` with no instance fields.
- **`CanBuild` predicate signatures vary.** The generator inspects the `CanBuild`/`CanBuildMethod` parameters and emits the matching call: `(Expression, BuildInfo, ExpressionBuilder)`, `(Expression, ExpressionBuilder)`, `(MethodCallExpression)`, or no parameters (see `RenderCallNode` at `BuildersGenerator.cs:283`). `MethodCallBuilder` subclasses get the no-arg or single-call form for free.
- **Ordering.** Within a `case ExpressionType.X:` block, candidates are emitted in metadata-collection order. Within a `case "MethodName":` switch, they are grouped by name and ordered by name. The first `CanBuild...` returning `true` wins. There is no priority annotation — name uniqueness is the convention.
- **`[BuildsAny]` is the catch-all.** Only `ContextRefBuilder` (`ContextRefBuilder.cs:6`) uses it: it unwraps a `ContextRefExpression` (an internal node that wraps an already-built `IBuildContext`) regardless of `NodeType`.
- **`MethodChainBuilder` is a generic `Call` handler.** It carries `[BuildsExpression(ExpressionType.Call)]` (no method-name list) and runs after the name switch. It captures any `MethodCallExpression` decorated with `Sql.ExtensionAttribute` whose target is `IsAggregate` or `IsWindowFunction` (see `MethodChainBuilder.cs:21-46`). This is how `Sql.Avg`, `Sql.RowNumber()`, custom `[Sql.Function]` aggregates, etc. reach the SQL side without each needing its own `*Builder`.
- **Dispatch is invoked by `ExpressionBuilder.TryFindBuilder`** (`ExpressionBuilder.cs:30`) called from `TryBuildSequence` (line 347) and `IsSequence` (line 399).

## How a builder builds — the recursion shape

Most builders follow the same skeleton. Looking at `WhereBuilder`:

```
[BuildsMethodCall("Where", "Having")]
sealed class WhereBuilder : MethodCallBuilder
{
    public static bool CanBuildMethod(MethodCallExpression call)
        => call.IsQueryable;

    protected override BuildSequenceResult BuildMethodCall(
        ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequenceResult = builder.TryBuildSequence(
            new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (sequenceResult.BuildContext == null) return sequenceResult;

        var sequence  = sequenceResult.BuildContext;
        var condition = methodCall.Arguments[1].UnwrapLambda();

        if (sequence.SelectQuery.Select.IsDistinct ||
            sequence.SelectQuery.Select.TakeValue != null ||
            sequence.SelectQuery.Select.SkipValue != null)
            sequence = new SubQueryContext(sequence);

        var result = builder.BuildWhere(sequence,
            condition: condition, enforceHaving: isHaving, out var error);
        return BuildSequenceResult.FromContext(result);
    }
}
```

The pattern repeats across the area:

1. Recursively build the source sequence from `methodCall.Arguments[0]` via `builder.TryBuildSequence(new BuildInfo(buildInfo, ...))`.
2. Inspect / wrap the returned `IBuildContext` (typically `new SubQueryContext(sequence)` when the parent already has constructs the new operator can't compose with — `Distinct`, `Take`, `Skip`, projection — see `SelectBuilder.cs:42`, `JoinBuilder.cs:46-47`).
3. Mutate the running `SelectQuery` (add a `Where` clause, push columns, configure joins, set `OrderBy`, etc.) via helpers on `ExpressionBuilder` (`BuildWhere`, `BuildExtractExpression`, `ConvertToExtensionSql`, `UpdateNesting`, …).
4. Construct a new `IBuildContext` (often `SequenceContextBase`-derived: `SelectContext`, `JoinBuilder.JoinContext`, `GroupByBuilder.GroupByContext`, `MethodChainBuilder.ChainContext`) that wraps the child + records the operator-specific state (selector lambda, join keys, group-by key, etc.).
5. Return `BuildSequenceResult.FromContext(newContext)`.

Terminal CRUD builders deviate: instead of producing a wrapping `SequenceContextBase`, they construct a dedicated `SqlStatement` and an `IBuildContext` whose `GetResultStatement()` returns it. See `InsertBuilder.cs:36` (`new SqlInsertStatement(sequence.SelectQuery)` inside `InsertContext`), `DeleteBuilder.cs`, `UpdateBuilder.cs`, `MergeBuilder.cs`. Deferred construction (e.g. for `MergeWithOutput*`) layers `SqlOutputClause` and `SingleExpressionContext` for synthetic output columns (`MergeBuilder.cs:56-65`).

## `BuildContext` ↔ `SqlStatement` ↔ AST

The relationship in three sentences:

- **`IBuildContext`** is a per-stage dossier carrying a (mutable, shared) `SelectQuery` plus enough state to resolve `MakeExpression(path, ProjectFlags)` calls — i.e. translate a path through the LINQ projection tree into either a SQL expression (`ProjectFlags.SQL`) or a materialization expression (`ProjectFlags.Expression`).
- **`SelectQuery`** is a [`SQL-AST`](../areas/SQL-AST/INDEX.md) node (lives in `LinqToDB.Internal.SqlQuery.SelectQuery`); the same instance threads through every builder in the chain via `BuildInfo.SelectQuery`. Builders mutate it; they don't replace it (subqueries get new `SelectQuery` instances inside `SubQueryContext`).
- **`SqlStatement`** is the top-level AST root. `IBuildContext.GetResultStatement()` returns it; for query expressions this is a `SqlSelectStatement` wrapping the root `SelectQuery`, for CRUD it's `SqlInsertStatement` / `SqlUpdateStatement` / `SqlDeleteStatement` / `SqlMergeStatement` / `SqlInsertOrUpdateStatement` / `SqlMultiInsertStatement` / `SqlTruncateTableStatement`.

The `MakeExpression(path, ProjectFlags)` virtual on `IBuildContext` (defined `IBuildContext.cs:28`) is the single most important method in the area. It is called twice per query in different modes:

- During **SQL build** (e.g. by `WhereBuilder.BuildWhere` → `ConvertToSql`), with `ProjectFlags.SQL` set, it returns a `SqlPlaceholderExpression` wrapping an `ISqlExpression` AST node — that placeholder is the bridge between the LINQ tree and the SQL tree.
- During **mapper build** (in `BuildQuery<T>`), with `ProjectFlags.Expression` set, it returns the materialization expression — typically a `SqlGenericConstructorExpression` that the `FinalizeExpressionVisitor` (`ExpressionBuilder.QueryBuilder.cs:22`) lowers into actual `Expression.New` / property-binding trees ultimately compiled into a `Func<IQueryRunner, IDataContext, DbDataReader, IQueryExpressions, object[], int, T>` mapper.

The shared `SelectQuery` mutation pattern means most builders compose correctly without rewriting AST — `WhereBuilder` adds to `sequence.SelectQuery.Where`, `OrderByBuilder` adds to `.OrderBy`, `JoinBuilder` adds a `SqlFromClause.Join` — but it also means **the order of builders matters at LINQ build time**, because each operator sees the already-mutated `SelectQuery` from its child. The `SubQueryContext` wrap is the standard safety net when a guard (Distinct / Take / projection) is already in place.

## Handoff to SQL-PROVIDER

After `BuildSequence` returns the root `IBuildContext`, the pipeline transitions in `BuildQuery<T>` (line 162):

```
foreach (var queryInfo in query.Queries)
{
    queryInfo.Statement = query.SqlOptimizer.Finalize(
        query.MappingSchema, queryInfo.Statement, query.DataOptions);

    if (queryInfo.Statement.SelectQuery != null)
    {
        if (!SqlProviderHelper.IsValidQuery(queryInfo.Statement, ...,
            DataContext.SqlProviderFlags, out var errorMessage))
        {
            query.ErrorExpression = new SqlErrorExpression(Expression, errorMessage, Expression.Type);
            return false;
        }
    }
}
```

`query.SqlOptimizer` is the provider-supplied `ISqlOptimizer` (per `IDataProvider`); see [`SQL-PROVIDER`](../areas/SQL-PROVIDER/INDEX.md). `Finalize` rewrites the AST — flattening unnecessary subqueries, hoisting / pushing parameters, applying provider-specific quirks (e.g. SQL Server CROSS APPLY transformations, MySQL `LIMIT` rewrites). `SqlProviderHelper.IsValidQuery` enforces provider-flag-driven invariants (e.g. "no correlated subquery in `SELECT` columns" for providers without `OUTER APPLY`).

Once finalized, `sequence.SetRunQuery(query, finalized)` (line 200) attaches the materialization mapper to the `Query`. Execution is then [`LINQ`](../areas/LINQ/INDEX.md)'s problem (`IQueryRunner` etc.) — this area's job is done.

## Cross-cutting concerns

- **`ParametersContext`** (`ParametersContext.cs`, Tier 2) tracks `SqlParameter` / `SqlValue` use across the build, supports `InlineParameters`, and produces the per-query parameter accessor table (`ParametersContext.ApplyAccessors`, `CacheManager.BuildQueryCacheCompareInfo`).
- **`ExpressionTreeOptimizationContext`** (Tier 2) hosts `ExposeExpression` — the pre-pass that inlines lambdas, evaluates client-evaluatable subtrees, and folds away `null`-conditional / `??` patterns into shapes the builders can recognize. `ExpressionBuilder.ExposeExpression` (line 422) is the static entry; it pools `ExposeExpressionVisitor` instances.
- **`TranslationModifier`** (`TranslationModifier.cs`, Tier 2) — bag of "translation modes" (inline parameters, filter overrides, etc.) propagated through the context graph.
- **`IMemberTranslator` / `CombinedMemberTranslator`** (lives in `LinqToDB.Internal.DataProvider.Translation`, [`EXPR`](../areas/EXPR/INDEX.md) territory) — extension point for `[Sql.Expression]`, `[Sql.Function]`, member-access mapping. `ExpressionBuilder` resolves it from the data context and optionally prepends `DataOptions.DataContextOptions.MemberTranslators` (`ExpressionBuilder.cs:87-97`). Custom `IMemberTranslator` registrations are the supported way to add provider-specific or user-defined SQL translations without touching this area.
- **`Activity` metrics** — every major step opens an `ActivityService.Start(ActivityID.Build / BuildSequence / BuildSequenceBuild / BuildQuery / FinalizeQuery)` scope (lines 126, 130, 180, 338, 350) for [`INTERCEPTORS`](../areas/INTERCEPTORS/INDEX.md) / `Metrics` consumption.

## Adding a new operator — checklist

1. Add a `XxxBuilder.cs` file under `Source/LinqToDB/Internal/Linq/Builder/`, decorated with `[BuildsMethodCall("Xxx")]` (or `[BuildsExpression(ExpressionType.Yyy)]` if it's not a method call). Inherit from `MethodCallBuilder` for the common shape.
2. Implement `static bool CanBuildMethod(MethodCallExpression call)` (or `CanBuild(Expression, ExpressionBuilder)` for `[BuildsExpression]`). Keep it cheap — it runs on every dispatch.
3. Implement `BuildMethodCall(...)`: recurse into the source via `builder.TryBuildSequence`, mutate the running `SelectQuery`, optionally wrap in a new `IBuildContext`-derived class (typically `SequenceContextBase`), and return `BuildSequenceResult.FromContext(...)`.
4. If the operator is a public-API LINQ extension method, wire it up in `LinqToDB.LinqExtensions`/`ExtensionMethods` ([`EXPR`](../areas/EXPR/INDEX.md)) so users have a callable entry point. If it's a SQL aggregate or window function instead, the right home is usually `Sql.ExtensionAttribute` + reuse of `MethodChainBuilder`.
5. Run `dotnet build`. The source generator regenerates `ExpressionBuilder.g.cs` automatically.

## Pointers

- Recursion / dispatch core: `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:30`, `:336`, `:371`.
- Source generator: `Source/CodeGenerators/BuildersGenerator.cs:165` (template) + `:283` (per-call rendering).
- Reference operator implementation: `WhereBuilder.cs`, `SelectBuilder.cs`, `OrderByBuilder.cs`.
- Reference root implementation: `TableBuilder.cs`, `EnumerableBuilder.cs`, `ContextRefBuilder.cs`.
- Reference extension-attribute path: `MethodChainBuilder.cs`.
- Per-area details + file inventory: [`areas/EXPR-TRANS/INDEX.md`](../areas/EXPR-TRANS/INDEX.md).
- Downstream consumer: [`areas/SQL-PROVIDER/INDEX.md`](../areas/SQL-PROVIDER/INDEX.md), [`areas/SQL-AST/INDEX.md`](../areas/SQL-AST/INDEX.md).
- Caller: [`areas/LINQ/INDEX.md`](../areas/LINQ/INDEX.md).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 63 / 63 ✓
- Tier 2 (visited / total): 17 / 71 (24%) ✗ — see [`areas/EXPR-TRANS/INDEX.md`](../areas/EXPR-TRANS/INDEX.md) coverage block.
- Tier 3 (skipped, logged): 0
</details>
