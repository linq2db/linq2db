---
area: EXPR-TRANS
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 63/63
coverage_tier_2: 17/71
---

# EXPR-TRANS — LINQ expression → SQL AST translation

The pipeline that consumes a `System.Linq.Expressions.Expression` (a LINQ method-call tree on `IQueryable<T>` / `ITable<T>`) and produces an `IBuildContext` graph whose `SelectQuery` / `SqlStatement` is then handed to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) for optimization and emission. Every translated LINQ operator (`Where`, `Select`, `OrderBy`, `Join`, `GroupBy`, `Insert`, `Update`, `Delete`, `Merge`, `Concat`, `Distinct`, `Take`/`Skip`, `Cast`, `OfType`, `AsCte`, `FromSql`, …) has a dedicated `*Builder` class; the central `ExpressionBuilder` + a Roslyn-generated dispatcher pick the right one per node.

## Key types

- `` `ExpressionBuilder` `` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:26`) — the per-query orchestrator. Holds the `Query`, `MappingSchema`, `DataOptions`, `ParametersContext`, `ExpressionTreeOptimizationContext`, the `_buildVisitor`, the `IMemberTranslator`, and pools for `SelectQuery` / `ParentInfo` / `PlaceholderCollectVisitor`. Public entry: `Build<T>(ref IQueryExpressions)` (line 124), called by [`LINQ`](../LINQ/INDEX.md)'s query setup. Sequence dispatch entry: `TryFindBuilder` → `FindBuilderImpl` (line 30, 38) — `FindBuilderImpl` is a `partial` method whose body is generated.
- `` `ISequenceBuilder` `` (`Source/LinqToDB/Internal/Linq/Builder/ISequenceBuilder.cs:3`) — the strategy interface every builder implements. Two methods: `BuildSequence` (produce `BuildSequenceResult`) and `IsSequence` (predicate).
- `` `MethodCallBuilder` `` (`Source/LinqToDB/Internal/Linq/Builder/MethodCallBuilder.cs:7`) — abstract base for the common case "this LINQ node is a `MethodCallExpression`"; subclasses override `BuildMethodCall(builder, methodCall, buildInfo)`. Default `IsSequence` recurses into `Arguments[0]` when `IQueryable`.
- `` `IBuildContext` `` (`Source/LinqToDB/Internal/Linq/Builder/IBuildContext.cs:9`) — the per-stage context produced by a builder. Carries `Builder`, `MappingSchema`, `Expression`, `SelectQuery`, `Parent`, `TranslationModifier`, `ElementType`. Key method `MakeExpression(path, ProjectFlags)` is the projection-resolution hook used during materialization-mapper construction.
- `` `BuildContextBase` `` / `` `SequenceContextBase` `` (`BuildContextBase.cs:11`, `SequenceContextBase.cs:11`) — abstract base classes; `SequenceContextBase` adds a `Sequences[]` array and `Body` lambda, used by builders that wrap a child sequence (Where, Select, OrderBy, …).
- `` `BuildInfo` `` (`Source/LinqToDB/Internal/Linq/Builder/BuildInfo.cs:7`) — the per-call invocation record. Mutable bag carrying `Parent`, `Expression`, `SelectQuery`, `CreateSubQuery`, `IsAssociation`, `JoinType`, `IgnoreOrderBy`, `IsAggregation`, `SourceCardinality`. Cloned + retargeted as recursion descends through nested method calls.
- `` `BuildSequenceResult` `` (`BuildSequenceResult.cs:5`) — return value of `BuildSequence`. Encodes three states via factories: `FromContext(IBuildContext)`, `Error(Expression)`, `NotSupported()`. `IsSequence` is true when context or error is set — distinguishes "I declined" from "I tried and failed".
- `` `BuildsAnyAttribute` `` / `` `BuildsExpressionAttribute` `` / `` `BuildsMethodCallAttribute` `` (`Attributes.cs:20-47`) — attribute markers consumed by the source generator. They have no runtime state; the generator emits the dispatcher.
- `` `ProjectFlags` `` (`ProjectFlags.cs:6`), `` `BuildPurpose` `` (`BuildPurpose.cs:6`), `` `BuildFlags` `` (`BuildFlags.cs:6`) — bitmasks that drive the `MakeExpression` / `BuildExpression` path: `SQL` vs `Expression`, `Root`, `Subquery`, `Table`, `Keys`, `Traverse`, `AssociationRoot`, `AggregationRoot`, etc.
- `` `ContextRefExpression` `` (in [`EXPR`](../EXPR/INDEX.md), `LinqToDB.Internal.Expressions`) — the expression-tree node that wraps an `IBuildContext` once a sequence root has been resolved. `ContextRefBuilder` (with `[BuildsAny]`) unwraps it back into the wrapped context (`ContextRefBuilder.cs:6`).

## Files (Tier 1 / Tier 2)

### Tier 1 — pinned (63 files)

`ExpressionBuilder.cs` plus every `*Builder.cs` at the folder root. Categorised:

- **Dispatch / infrastructure (3):** `ExpressionBuilder.cs`, `MethodCallBuilder.cs`, `MethodChainBuilder.cs`.
- **Sequence root (5):** `TableBuilder.cs`, `EnumerableBuilder.cs`, `ContextRefBuilder.cs`, `RecordReaderBuilder.cs`, `SelectQueryBuilder.cs`.
- **Restriction / projection / set ops (10):** `WhereBuilder.cs`, `SelectBuilder.cs`, `SelectManyBuilder.cs`, `OrderByBuilder.cs`, `GroupByBuilder.cs`, `JoinBuilder.cs`, `GroupJoinBuilder.cs`, `AllJoinsBuilder.cs`, `AllJoinsLinqBuilder.cs`, `DistinctBuilder.cs`.
- **Quantifiers / scalar / aggregate (10):** `AllAnyBuilder.cs`, `ContainsBuilder.cs`, `FirstSingleBuilder.cs`, `ElementAtBuilder.cs`, `MinByMaxByBuilder.cs`, `CountByBuilder.cs`, `DistinctByBuilder.cs`, `AggregationBuilder.cs`, `AggregateExecuteBuilder.cs`, `ScalarSelectBuilder.cs`.
- **Set + windowing (3):** `SetOperationBuilder.cs`, `SetOperationByBuilder.cs`, `TakeSkipBuilder.cs`.
- **CRUD (8):** `InsertBuilder.cs`, `InsertOrUpdateBuilder.cs`, `MultiInsertBuilder.cs`, `UpdateBuilder.cs`, `DeleteBuilder.cs`, `TruncateBuilder.cs`, `DropBuilder.cs`, `MergeBuilder.cs` (+ partials `MergeBuilder.*.cs` are Tier 2).
- **Casts / type tricks (3):** `CastBuilder.cs`, `OfTypeBuilder.cs`, `DefaultIfEmptyBuilder.cs`.
- **Hints / modifiers / extensions (10):** `AsSubQueryBuilder.cs`, `AsUpdatableBuilder.cs`, `AsValueInsertableBuilder.cs`, `ApplyModifierBuilder.cs`, `RemoveOrderByBuilder.cs`, `IgnoreFiltersBuilder.cs`, `DisableFiltersBuilder.cs`, `DisableGroupingGuardBuilder.cs`, `InlineParametersBuilder.cs`, `WithTableExpressionBuilder.cs`.
- **Schema / metadata / extension methods (8):** `LoadWithBuilder.cs`, `TableAttributeBuilder.cs`, `TagQueryBuilder.cs`, `QueryNameBuilder.cs`, `QueryExtensionBuilder.cs`, `IndexBuilder.cs`, `HasUniqueKeyBuilder.cs`, `PassThroughBuilder.cs`.
- **Partials of `ExpressionBuilder` (5):** `ExpressionBuilder.QueryBuilder.cs`, `ExpressionBuilder.SqlBuilder.cs`, `ExpressionBuilder.Aggregation.cs`, `ExpressionBuilder.Associations.cs`, `ExpressionBuilder.EagerLoad.cs`, `ExpressionBuilder.Expressions.cs`, `ExpressionBuilder.Generation.cs` (these are pinned because they extend the central type even though their filenames carry a dotted suffix).

### Tier 2 — implementation / contexts / helpers (71 files)

Examples: `BuildInfo.cs`, `BuildSequenceResult.cs`, `BuildContextBase.cs`, `SequenceContextBase.cs`, `IBuildContext.cs`, `ISequenceBuilder.cs`, `Attributes.cs`, `BuildFlags.cs`, `BuildPurpose.cs`, `ProjectFlags.cs`, `ProjectFlagExtensions.cs`, `ProjectionPathHelper.cs`, `SubQueryContext.cs`, `SelectContext.cs`, `AnchorContext.cs`, `AsSubqueryContext.cs`, `EagerContext.cs`, `EagerLoading.cs`, `ScopeContext.cs`, `PassThroughContext.cs`, `SingleExpressionContext.cs`, `EnumerableContext.cs`, `EnumerableContextDynamic.cs`, `CteContext.cs`, `CteTableContext.cs`, `CteAnnotationsContainer.cs`, `CteBuilderImpl.cs`, `TableLikeQueryContext.cs`, `TableLikeHelpers.cs`, `TableBuilder.TableContext.cs`, `TableBuilder.RawSqlContext.cs`, `TableBuilder.CteTableContext.cs`, `MergeBuilder.*.cs` partials (12), `MergeProjectionHelper.cs`, `AssociationHelper.cs`, `LoadWithEntity.cs`, `LoadWithMember.cs`, `IAnnotatableBuilderInternal.cs`, `IBuildProxy.cs`, `BuildProxyBase{TOwner}.cs`, `ILoadWithContext.cs`, `ITableContext.cs`, `EntityConstructorBase.cs`, `RecordReaderBuilder.cs` helpers, `SequenceHelper.cs`, `EvaluationHelper.cs`, `LambdaResolveVisitor.cs`, `ExpressionBuildVisitor.cs`, `BinaryExpressionAggregatorVisitor.cs`, `ExpressionTreeOptimizerVisitor.cs`, `ExpressionTreeOptimizationContext.cs`, `ExpressionTestGenerator.cs`, `ParametersContext.cs`, `TranslationModifier.cs`, `BuildContextDebuggingHelper.cs`, plus the two files under `Visitors/` (`ExposeExpressionVisitor.cs`, `CanBeEvaluatedOnClientCheckVisitorBase.cs`).

## Subsystems

1. **Dispatcher (source-generated).** `ExpressionBuilder.FindBuilderImpl` is generated by `BuildersGenerator` (`Source/CodeGenerators/BuildersGenerator.cs:14`) from `[BuildsExpression]`, `[BuildsMethodCall]`, `[BuildsAny]` markers on each `*Builder` class. The generator emits a single `switch (expr.NodeType)` over `ExpressionType`, with a nested `switch (call.Method.Name)` for the `Call` case, and finally falls through to the `[BuildsAny]` set (`ContextRefBuilder` is the only one). Each candidate calls a static `CanBuild` / `CanBuildMethod` predicate; the first that returns `true` wins, and the dispatcher returns the singleton instance from `Builder<T>.Instance` (line 184 of the template). Adding a new operator = drop a `[BuildsMethodCall("Foo")]`-decorated `FooBuilder.cs`; the generator picks it up next build.

2. **`BuildSequence` recursion.** `ExpressionBuilder.TryBuildSequence(BuildInfo)` (line 336) is the recursive workhorse: it `ExpandToRoot`s the expression (resolves macros / associations), runs `TryFindBuilder`, asks the chosen builder for a context, registers the originating expression on the context (`RegisterSequenceExpression`), and returns. Most builders themselves call back into `builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]))` to translate the source sequence (e.g. `WhereBuilder.cs:16`, `SelectBuilder.cs:31`, `OrderByBuilder.cs:38`, `JoinBuilder.cs:35`).

3. **Context graph.** Each builder produces an `IBuildContext`. Composite operators (`Where`, `Select`, `OrderBy`, joins) wrap the child context — typically in a `SubQueryContext` (`SubQueryContext.cs`) when the parent already has `Distinct` / `Take` / `Skip` / projection, e.g. `WhereBuilder.cs:25-30` and `SelectBuilder.cs:42`. Joins create `outer`/`inner` `SubQueryContext` pairs (`JoinBuilder.cs:46-47`). The resulting `IBuildContext` chain mirrors the LINQ method chain in reverse and drives later projection resolution via `MakeExpression`.

4. **`SelectQuery` plumbing.** Every builder mutates the running `SelectQuery` (from `BuildInfo.SelectQuery`) — adding `Where`/`Having` clauses, columns, `OrderBy`, joins. The `SelectQuery` instance is allocated from `ExpressionBuilder.QueryPool` (line 44) at the entry point and threaded through `BuildInfo`. The final `SqlStatement` is constructed by terminal builders (`InsertBuilder`, `DeleteBuilder`, `UpdateBuilder`, `MergeBuilder`, etc.) — see `InsertBuilder.cs:36` (`new SqlInsertStatement(sequence.SelectQuery)`).

5. **Projection / materialization.** After `BuildSequence` returns the root context, `ExpressionBuilder.BuildQuery<T>` (line 162) calls `_buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), buildPurpose: BuildPurpose.Expression)` to produce the materialization mapper. This walks the context graph via `IBuildContext.MakeExpression(path, ProjectFlags)` — the same method is reused for SQL-side resolution by passing `ProjectFlags.SQL`. The `ExpressionBuildVisitor` (`ExpressionBuildVisitor.cs`, Tier 2) is the central traversal.

6. **Method-chain extension hook (`MethodChainBuilder`).** Aggregate / window-function calls registered via `Sql.ExtensionAttribute` (e.g. `Sql.RowNumber()`, custom `[Sql.Function]` aggregates) are dispatched to `MethodChainBuilder` (`MethodChainBuilder.cs:13`, `[BuildsExpression(ExpressionType.Call)]`) — *not* a per-method `*Builder`. It walks `methodCall.SkipMethodChain` to find the queryable root, builds the source sequence, then asks the extension attribute to produce a `SqlPlaceholderExpression` that gets attached to a synthetic `ChainContext`.

7. **Finalization + handoff to SQL-PROVIDER.** `ExpressionBuilder.BuildQuery<T>` calls `query.SqlOptimizer.Finalize(...)` per query info (line 184), then `SqlProviderHelper.IsValidQuery(...)` (line 188); failure here surfaces as a `SqlErrorExpression` and returns `false`. The optimizer is provider-supplied — see [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md).

## Interactions

- **Driven by [`LINQ`](../LINQ/INDEX.md):** `Query<T>.GetQuery` / `ExpressionQuery` create an `ExpressionBuilder` and call `Build<T>` once per cache miss; the resulting `Query` carries the compiled mapper + the `SqlStatement`(s) for execution.
- **Consumes [`SQL-AST`](../SQL-AST/INDEX.md):** every builder mutates / constructs `SelectQuery`, `SqlField`, `SqlBinaryExpression`, `SqlPlaceholderExpression`, `SqlInsertStatement`, etc. New AST nodes must land in `LinqToDB.Internal.SqlQuery` (see `code-design.md`).
- **Hands off to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md):** finalized `SqlStatement` flows into `ISqlOptimizer.Finalize` then `ISqlBuilder` for emission.
- **Uses [`MAPPING`](../MAPPING/INDEX.md):** `EntityDescriptor` lookups for query filters (`TableBuilder.cs:99` → `mappingSchema.GetEntityDescriptor`), column mapping for materialization, association definitions.
- **Uses [`EXPR`](../EXPR/INDEX.md):** `ContextRefExpression`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `SqlEagerLoadExpression`, `SqlReaderIsNullExpression`, `SqlErrorExpression`, `SqlDefaultIfEmptyExpression`, and the visitor base classes live in `LinqToDB.Internal.Expressions`.
- **Uses [`INFRA`](../INFRA/INDEX.md):** `ObjectPool`, `ActivityService` metrics, common helpers.
- **Generated by [`CODEGEN`](../CODEGEN/INDEX.md):** `BuildersGenerator` emits `ExpressionBuilder.g.cs` containing `FindBuilderImpl`.

## Inbound / outbound dependencies

- **Inbound:** `LINQ` (`Internal/Linq/Query.cs`, `Internal/Linq/ExpressionQuery.cs`) constructs `ExpressionBuilder`. `EXPR` types are referenced inbound via `ContextRefExpression.BuildContext`.
- **Outbound:** `LinqToDB.Internal.SqlQuery.*` (heavy), `LinqToDB.Internal.Expressions.*`, `LinqToDB.Internal.Conversion`, `LinqToDB.Internal.DataProvider.Translation` (`IMemberTranslator`), `LinqToDB.Mapping`, `LinqToDB.Metrics`, `LinqToDB.Internal.Reflection.Methods` (well-known method handles).

## Known issues / debt

Pointer-only — full entries live in [`tech-debt.md`](tech-debt.md) (populated in step 11).

- Source-generator dispatcher relies on every builder declaring its triggering attributes correctly; a missing attribute is a silent miss (the call falls through to `null` and surfaces as `BuildSequenceResult.NotSupported`).
- Many `*Builder.BuildMethodCall` methods carry `[BuildsMethodCall]` lists that include legacy method names alongside current ones (`OrderBy` + `ThenOrBy`, `Insert` + `InsertWithOutput*`); changes here can affect provider compatibility.
- `IBuildContext` has a `// TODO: probably not needed` on `Parent.set` (`IBuildContext.cs:21`).
- `BuildInfo` is a mutable bag with `set;` accessors (`BuildInfo.cs:32-62`); its `IsSubQuery` is derived from `Parent != null` rather than tracked explicitly.
- `MethodCallParser.cs` referenced in `kb-areas.md` does not exist — the area registry should be updated to remove it (see audit note).

## See also

- [`SQL-AST`](../SQL-AST/INDEX.md) — output target of every builder.
- [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) — receives the finalized `SqlStatement`.
- [`LINQ`](../LINQ/INDEX.md) — caller; also owns query caching.
- [`EXPR`](../EXPR/INDEX.md) — `ContextRefExpression`, `SqlPlaceholderExpression`, visitor infrastructure.
- [`CODEGEN`](../CODEGEN/INDEX.md) — `BuildersGenerator` Roslyn source generator.
- [`../../architecture/expression-translator.md`](../../architecture/expression-translator.md) — narrative walkthrough.
- `.claude/docs/code-design.md` — public-API + AST namespace invariants.

## Pointers

- Dispatcher template: `Source/CodeGenerators/BuildersGenerator.cs:165`.
- Per-query entry: `ExpressionBuilder.Build<T>` at `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:124`.
- Recursion core: `TryBuildSequence` at `ExpressionBuilder.cs:336`.
- Reference operator implementations: `WhereBuilder.cs`, `SelectBuilder.cs`, `OrderByBuilder.cs`, `JoinBuilder.cs`, `GroupByBuilder.cs`, `InsertBuilder.cs`.
- Sequence root: `TableBuilder.cs`, `ContextRefBuilder.cs`, `EnumerableBuilder.cs`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 63 / 63 ✓
  - Read in full: `ExpressionBuilder.cs`, `MethodCallBuilder.cs`, `MethodChainBuilder.cs`, `WhereBuilder.cs`, `SelectBuilder.cs` (entry), `JoinBuilder.cs` (entry), `GroupByBuilder.cs` (entry), `InsertBuilder.cs` (entry), `OrderByBuilder.cs` (entry), `MergeBuilder.cs` (entry), `TableBuilder.cs` (entry), `TakeSkipBuilder.cs`, `ContextRefBuilder.cs`, `EnumerableBuilder.cs` (entry), `SelectQueryBuilder.cs`, `DeleteBuilder.cs`, `ExpressionBuilder.QueryBuilder.cs` (entry).
  - Sampled (read entry + attribute, follow same `MethodCallBuilder` pattern as the above — class has `[BuildsMethodCall("...")] sealed class XxxBuilder : MethodCallBuilder` with a static `CanBuildMethod` and an override `BuildMethodCall` that recurses on `Arguments[0]`): the remaining 46 root `*Builder.cs` files.
- Tier 2 (visited / total): 17 / 71 (24%) ✗
  - Read: `ISequenceBuilder.cs`, `IBuildContext.cs`, `BuildContextBase.cs`, `SequenceContextBase.cs`, `BuildInfo.cs`, `BuildSequenceResult.cs`, `Attributes.cs`, `BuildFlags.cs`, `BuildPurpose.cs`, `ProjectFlags.cs`, plus the structural anchors cited in MethodChainBuilder/SelectBuilder bodies (`SubQueryContext`, `SelectContext`, `EnumerableContext` partial), and read of source generator (`Source/CodeGenerators/BuildersGenerator.cs`) for dispatcher semantics.
  - Skipped: 54 Tier-2 files including the `MergeBuilder.*.cs` partials (12), per-builder `*Context.cs` types, helpers, visitors, parameter/translation helpers.
  - skip reason: budget — these helper / context / visitor types support the per-builder pattern but do not change the overall dispatch + recursion architecture documented above. Below 90% gate; emitted with `confidence: medium` to surface that. Re-run with focused budget recommended (see audit note).
- Tier 3 (skipped, logged): 0
</details>
