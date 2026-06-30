---
area: EXPR-TRANS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 63/63
coverage_tier_2: 71/71
---

# EXPR-TRANS -- LINQ expression -> SQL AST translation

The pipeline that consumes a `System.Linq.Expressions.Expression` (a LINQ method-call tree on `IQueryable<T>` / `ITable<T>`) and produces an `IBuildContext` graph whose `SelectQuery` / `SqlStatement` is then handed to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) for optimization and emission. Every translated LINQ operator (`Where`, `Select`, `OrderBy`, `Join`, `GroupBy`, `Insert`, `Update`, `Delete`, `Merge`, `Concat`, `Distinct`, `Take`/`Skip`, `Cast`, `OfType`, `AsCte`, `FromSql`, `AsQueryable`, `DistinctBy`, `MinBy`/`MaxBy`, `ExceptBy`/`UnionBy`/`IntersectBy`, ...) has a dedicated `*Builder` class; the central `ExpressionBuilder` + a Roslyn-generated dispatcher pick the right one per node.

## Key types

- ``ExpressionBuilder`` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:26`) -- the per-query orchestrator. Holds the `Query`, `MappingSchema`, `DataOptions`, `ParametersContext`, `ExpressionTreeOptimizationContext`, the `_buildVisitor`, the `IMemberTranslator`, and pools for `SelectQuery` / `ParentInfo` / `PlaceholderCollectVisitor`. Public entry: `Build<T>(ref IQueryExpressions)` (line 124), called by [`LINQ`](../LINQ/INDEX.md)'s query setup. Sequence dispatch entry: `TryFindBuilder` -> `FindBuilderImpl` (line 30, 38) -- `FindBuilderImpl` is a `partial` method whose body is generated.
- ``ISequenceBuilder`` -- the strategy interface every builder implements. Two methods: `BuildSequence` (produce `BuildSequenceResult`) and `IsSequence` (predicate).
- ``MethodCallBuilder`` -- abstract base for the common case "this LINQ node is a `MethodCallExpression`"; subclasses override `BuildMethodCall(builder, methodCall, buildInfo)`. Default `IsSequence` recurses into `Arguments[0]` when `IQueryable`.
- ``IBuildContext`` -- the per-stage context produced by a builder. Carries `Builder`, `MappingSchema`, `Expression`, `SelectQuery`, `Parent`, `TranslationModifier`, `ElementType`. Key method `MakeExpression(path, ProjectFlags)` is the projection-resolution hook used during materialization-mapper construction.
- ``BuildContextBase`` / ``SequenceContextBase`` -- abstract base classes; `SequenceContextBase` adds a `Sequences[]` array and `Body` lambda, used by builders that wrap a child sequence.
- ``BuildInfo`` -- the per-call invocation record. Mutable bag carrying `Parent`, `Expression`, `SelectQuery`, `CreateSubQuery`, `IsAssociation`, `JoinType`, `IgnoreOrderBy`, `IsAggregation`, `SourceCardinality`. Cloned + retargeted as recursion descends through nested method calls.
- ``BuildSequenceResult`` -- return value of `BuildSequence`. Encodes three states via factories: `FromContext(IBuildContext)`, `Error(Expression)`, `NotSupported()`.
- ``BuildsAnyAttribute`` / ``BuildsExpressionAttribute`` / ``BuildsMethodCallAttribute`` -- attribute markers consumed by the source generator.
- ``ProjectFlags`` / ``BuildPurpose`` / ``BuildFlags`` -- bitmasks that drive the `MakeExpression` / `BuildExpression` path.
- ``ContextRefExpression`` (in [`EXPR`](../EXPR/INDEX.md), `LinqToDB.Internal.Expressions`) -- the expression-tree node that wraps an `IBuildContext` once a sequence root has been resolved.
- ``TranslationModifier`` -- sealed immutable class propagated through every `IBuildContext`. Carries `InlineParameters` and `IgnoreQueryFilters`. `WithIgnoreQueryFilters` merges type arrays via union when both sides are non-null/non-empty. `IsFilterDisabled(Type)` returns `true` when `IgnoreQueryFilters` is empty (all filters off) or when the type is in the array.
- ``ParametersContext`` -- manages `SqlParameter` allocation and the `ExpressionCacheManager` for dynamic expression accessors.
- ``IAsQueryableBuilder<T>`` (`Source/LinqToDB/Linq/IAsQueryableBuilder.cs`) -- **new public interface (PR #5495)**. Marker-only; methods (`Parameterize()`, `Inline()`) are never called at runtime -- they are captured as expression nodes by the 3-arg `LinqExtensions.AsQueryable` overload and interpreted at query-build time by `EnumerableBuilder.TryParseConfigure`.
- ``IAsQueryableExceptBuilder<T>`` (`Source/LinqToDB/Linq/IAsQueryableExceptBuilder.cs`) -- second stage of the same configuration chain. Exposes `Except(params Expression<Func<T, object?>>[] members)` to flip the default per-column mode for specific members.
- ``EnumerableParameterizationConfig`` (`Source/LinqToDB/Internal/Linq/Builder/EnumerableParameterizationConfig.cs`) -- sealed internal class produced by `EnumerableBuilder.BuildConfigured`. Carries `DefaultForceParameter` (bool), `Parameter` (shared `ParameterExpression` root), and `Excepted` (list of `MemberExpression` paths). `ShouldForceParameter(Expression)` re-roots the access expression onto `Parameter` and checks it against the `Excepted` list; returns `!DefaultForceParameter` on a match, `DefaultForceParameter` otherwise.
- ``SqlAggregateLifterExpression`` (`Source/LinqToDB/Internal/Expressions/SqlAggregateLifterExpression.cs`) -- **new expression node (PR #5502/5557)** in the [`EXPR`](../EXPR/INDEX.md) area. Wraps a `SqlPlaceholderExpression` and carries up to two delegates: `MaterializationCheck` (C# runtime null-throw for non-nullable Min/Max/Avg on empty input) and `SqlRewriter` (SQL-side COALESCE wrap applied at OUTER APPLY lift time for non-nullable Sum/StringJoin, or eagerly for grouped aggregates). At least one delegate is always set; `CanReduce` is false -- visitors do not auto-reduce it.
- ``BuildAggregationFunctionResult`` (`Source/LinqToDB/Linq/Translation/BuildAggregationFunctionResult.cs`) -- record returned from every `functionFactory` delegate passed to `BuildAggregationFunction` / `BuildArrayAggregationFunction`. Carries `SqlExpression`, `MaterializationCheck`, `SqlRewriter`, `ErrorExpression`, `FallbackExpression`, and (added in this delta) `IsSkipped` bool. `Skipped()` factory: sentinel that signals the builder declined translation because an arg could not be translated AND the surrounding visitor is in Expression mode AND `IsServerSideOnly` is false -- causes `AggregateFunctionBuilder.Build` to return `null` so the dispatch chain cascades.
- ``AggregateFunctionBuilder`` (`Source/LinqToDB/Linq/Translation/AggregateFunctionBuilder.cs`) -- public fluent builder for aggregate/window-function translation. `ConfigureAggregate` / `ConfigurePlain` accept `Action<AggregateModeBuilder>` to set up the mode. `Build(ITranslationContext, MethodCallExpression, bool isExpression)` dispatches to `BuildArrayAggregationFunction` (plain path) or `BuildAggregationFunction` (aggregate path). New properties on `ModeConfig`: `IsServerSideOnly` (bool, default false -- controls lenient vs strict error on translation failure), `ItemTransform` (plain-mode per-item LINQ rewrite hook), `ValueTransform` (aggregate-mode per-row-value LINQ rewrite hook). Instance field `_skipped` (bool) is reset at the top of each `Build` call; set inside `Combine` when `BuildAggregationFunctionResult.Skipped()` is returned.
- ``AggregateExecuteBuilder`` (`Source/LinqToDB/Internal/Linq/Builder/AggregateExecuteBuilder.cs`) -- `[BuildsMethodCall(nameof(LinqExtensions.AggregateExecute))]` builder. Inner `AggregateExecuteContext.MakeExpression` handles `SqlAggregateLifterExpression` at lift time: if `translated is SqlAggregateLifterExpression sqlAggregateLifter`, unpacks `InnerExpression` as `translatedPlaceholder` and captures `MaterializationCheck` / `SqlRewriter`. `CreateWeakOuterJoin` applies `SqlRewriter(Placeholder)` after `UpdateNesting`, then nulls `SqlRewriter` so it is applied exactly once.
- ``CteAnnotationsContainer`` (`Source/LinqToDB/Internal/Linq/Builder/CteAnnotationsContainer.cs`) -- immutable snapshot of `ICteBuilder` state placed into the expression tree as a query-cache key. Implements `IExpressionCacheKey` and `IEquatable<CteAnnotationsContainer>`. `BuildCacheKey` iterates annotations ordered by ordinal name (stable hashing): `[name]|key1=val1|key2=val2`. `GetHashCode` uses `StringComparer.Ordinal` on the cache key string.

## Files (Tier 1 / Tier 2)

### Tier 1 -- pinned (63 files)

`ExpressionBuilder.cs` plus every `*Builder.cs` at the folder root. `AggregateExecuteBuilder.cs` and `UpdateBuilder.cs` are included here.

Net-8-only builders (conditional `#if NET8_0_OR_GREATER`): `DistinctByBuilder.cs`, `MinByMaxByBuilder.cs`, `SetOperationByBuilder.cs`.

### Tier 2 -- implementation / contexts / helpers (71 files)

Examples: `BuildInfo.cs`, `BuildSequenceResult.cs`, `BuildContextBase.cs`, `SequenceContextBase.cs`, `IBuildContext.cs`, `ISequenceBuilder.cs`, `Attributes.cs`, `BuildFlags.cs`, `BuildPurpose.cs`, `ProjectFlags.cs`, `ProjectFlagExtensions.cs`, `ProjectionPathHelper.cs`, `SubQueryContext.cs`, `SelectContext.cs`, `AnchorContext.cs`, `AsSubqueryContext.cs`, `EagerContext.cs`, `EagerLoading.cs`, `ScopeContext.cs`, `PassThroughContext.cs`, `SingleExpressionContext.cs`, `EnumerableContext.cs`, `EnumerableContextDynamic.cs`, `CteContext.cs`, `CteTableContext.cs`, `CteAnnotationsContainer.cs`, `CteBuilderImpl.cs`, `TableLikeQueryContext.cs`, `TableLikeHelpers.cs`, `TableBuilder.TableContext.cs`, `TableBuilder.RawSqlContext.cs`, `TableBuilder.CteTableContext.cs`, `MergeBuilder.*.cs` partials (12), `MergeProjectionHelper.cs`, `AssociationHelper.cs`, `LoadWithEntity.cs`, `LoadWithMember.cs`, `IAnnotatableBuilderInternal.cs`, `IBuildProxy.cs`, `BuildProxyBase{TOwner}.cs`, `ILoadWithContext.cs`, `ITableContext.cs`, `EntityConstructorBase.cs`, `RecordReaderBuilder.cs` helpers, `SequenceHelper.cs`, `EvaluationHelper.cs`, `LambdaResolveVisitor.cs`, `ExpressionBuildVisitor.cs`, `BinaryExpressionAggregatorVisitor.cs`, `ExpressionTreeOptimizerVisitor.cs`, `ExpressionTreeOptimizationContext.cs`, `ExpressionTestGenerator.cs`, `ParametersContext.cs`, `TranslationModifier.cs`, `BuildContextDebuggingHelper.cs`, plus the two files under `Visitors/` (`ExposeExpressionVisitor.cs`, `CanBeEvaluatedOnClientCheckVisitorBase.cs`).

New files added by PR #5495 (now Tier 2): `EnumerableParameterizationConfig.cs`; `IAsQueryableBuilder.cs` and `IAsQueryableExceptBuilder.cs` live in `Source/LinqToDB/Linq/` (public surface, Tier 2 for this area).

Public translation-layer files (outside area globs, integrated here for completeness -- see UNCLASSIFIED-FILE blocks below): `Source/LinqToDB/Linq/Translation/AggregateFunctionBuilder.cs`, `Source/LinqToDB/Linq/Translation/BuildAggregationFunctionResult.cs`, `Source/LinqToDB/Linq/Translation/WindowFunctionsMemberTranslator.cs`, `Source/LinqToDB/Linq/Expressions.cs`.

## Subsystems

1. **Dispatcher (source-generated).** `ExpressionBuilder.FindBuilderImpl` is generated by `BuildersGenerator` (`Source/CodeGenerators/BuildersGenerator.cs:14`) from `[BuildsExpression]`, `[BuildsMethodCall]`, `[BuildsAny]` markers on each `*Builder` class. The generator emits a single `switch (expr.NodeType)` over `ExpressionType`, with a nested `switch (call.Method.Name)` for the `Call` case, and finally falls through to the `[BuildsAny]` set. Each candidate calls a static `CanBuild` / `CanBuildMethod` predicate; the first that returns `true` wins, and the dispatcher returns the singleton instance from `Builder<T>.Instance`. Adding a new operator = drop a `[BuildsMethodCall("Foo")]`-decorated `FooBuilder.cs`; the generator picks it up next build.

2. **`BuildSequence` recursion.** `ExpressionBuilder.TryBuildSequence(BuildInfo)` is the recursive workhorse: it `ExpandToRoot`s the expression (resolves macros / associations), runs `TryFindBuilder`, asks the chosen builder for a context, registers the originating expression on the context (`RegisterSequenceExpression`), and returns. Most builders themselves call back into `builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]))` to translate the source sequence.

3. **Context graph.** Each builder produces an `IBuildContext`. Composite operators (`Where`, `Select`, `OrderBy`, joins) wrap the child context -- typically in a `SubQueryContext` when the parent already has `Distinct` / `Take` / `Skip` / projection. Joins create `outer`/`inner` `SubQueryContext` pairs.

   Context specializations include `SubQueryContext`, `AsSubqueryContext`, `SelectContext`, `ScopeContext`, `PassThroughContext`, `SingleExpressionContext`, `AnchorContext`, `EagerContext`, `EnumerableContext`, `EnumerableContextDynamic`, `CteContext`, `CteTableContext`, `TableBuilder.TableContext`, `TableBuilder.RawSqlContext`, `TableBuilder.CteTableContext`.

   - `ScopeContext` -- carries a `Context` (inner) and an `UpTo` (parent boundary). `Clone(CloningContext)` delegates to `context.CloneContext` for both fields. `MakeExpression` re-routes SQL-mode requests through `Builder.BuildSqlExpression(UpTo, ...)` after building on the inner context.
   - `EnumerableContext` -- context backed by `SqlValuesTable`. Maps LINQ-side member paths to `SqlField` entries in the `VALUES(...)` table. As of PR #5495 the constructor accepts an optional `EnumerableParameterizationConfig? parameterization` parameter; when non-null, `BuildValueGetter` calls `_parameterization.ShouldForceParameter(me)` to choose between emitting `new SqlParameter(...)` and `new SqlValue(...)` per column -- columns whose mapping returns `DataParameter` are always parameters regardless of this config (`EnumerableContext.cs:233-235`).

4. **`SelectQuery` plumbing.** Every builder mutates the running `SelectQuery` (from `BuildInfo.SelectQuery`) -- adding `Where`/`Having` clauses, columns, `OrderBy`, joins. The `SelectQuery` instance is allocated from `ExpressionBuilder.QueryPool` at the entry point and threaded through `BuildInfo`. The final `SqlStatement` is constructed by terminal builders (`InsertBuilder`, `DeleteBuilder`, `UpdateBuilder`, `MergeBuilder`, etc.).

5. **Projection / materialization.** After `BuildSequence` returns the root context, `ExpressionBuilder.BuildQuery<T>` calls `_buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), buildPurpose: BuildPurpose.Expression)` to produce the materialization mapper. This walks the context graph via `IBuildContext.MakeExpression(path, ProjectFlags)`.

   `ExpressionBuilder.Project` (in `ExpressionBuilder.SqlBuilder.cs`) handles `Convert(body)` wrapping: when `path` is a `Convert` / `ConvertChecked` node whose operand is a `ContextRefExpression`, `Project` re-types the `ContextRefExpression` to the conversion's target type and recurses -- this is the path that resolves interface-member accesses on concrete types during projection (`ExpressionBuilder.SqlBuilder.cs:1808-1815`).

6. **Method-chain extension hook (`MethodChainBuilder`).** Aggregate / window-function calls registered via `Sql.ExtensionAttribute` are dispatched to `MethodChainBuilder` (`[BuildsExpression(ExpressionType.Call)]`) -- *not* a per-method `*Builder`.

7. **Finalization + handoff to SQL-PROVIDER.** `ExpressionBuilder.BuildQuery<T>` calls `query.SqlOptimizer.Finalize(...)` per query info, then `SqlProviderHelper.IsValidQuery(...)`; failure here surfaces as a `SqlErrorExpression`.

8. **MergeBuilder partials.** `MergeBuilder` is a `partial class` spread across 10+ files. The top-level `MergeBuilder.cs` (Tier 1) handles execution dispatch and OUTPUT clause handling. Per-operation partials: `Merge`, `MergeInto`, `Using`, `UsingTarget`, `On`, `InsertWhenNotMatched`, `UpdateWhenMatched`, `UpdateWhenMatchedThenDelete`, `DeleteWhenMatched`, `DeleteWhenNotMatchedBySource`, `UpdateWhenNotMatchedBySource`, `MergeContext`, `MergeProjectionHelper`, `TableLikeQueryContext`, `TableLikeHelpers`.

9. **CTE subsystem.**
   - `CteBuilderImpl` -- public implementation of `ICteBuilder` / `IAnnotatableBuilderInternal`. Captures `Name` and annotation bag set by fluent `.HasName()` / provider-extension `.SetAnnotation()` calls.
   - `CteAnnotationsContainer` -- immutable snapshot of the `CteBuilderImpl` state placed into the expression tree as a query-cache key. Implements `IExpressionCacheKey` and `IEquatable<CteAnnotationsContainer>`; equality is string-ordinal on the `_cacheKey` built from name + sorted annotations. This ensures distinct `AsCte(b => ...)` configurations produce distinct cache entries even when delegate equality is reference-equal.
   - `TableBuilder.CteTableContext` partial -- builds `CteContext` (deferred) or looks up an existing one via `FindRegisteredCteContext`, then wraps it in a `CteTableContext`.

10. **LoadWith / association subsystem.**
    - `AssociationHelper` -- static factory for association query lambdas (`CreateAssociationQueryLambda`).
    - `LoadWithEntity` / `LoadWithMember` -- tree nodes tracking `LoadWith` configuration levels. Both implement `Equals`/`GetHashCode`: `LoadWithEntity.Equals` recursively compares `MembersToLoad` lists; `LoadWithMember.Equals` compares by `MemberInfo` identity only (filter expressions are not part of equality).
    - `ILoadWithContext` / `ITableContext` -- interfaces added to `IBuildContext` implementations that support `LoadWith` root attachment.
    - `EagerLoading` -- minimal static class with `GetEnumerableElementType(Type, MappingSchema)`.

    **Association via realized concrete type (PR #5511 / issue #5510 -- prior delta):** `IsAssociationInRealization` (`ExpressionBuilder.Associations.cs:49`) has three branches. The third branch (lines 73-93, updated further in this delta -- PR #5548) handles the case where `member.ReflectedType != expression.Type` OR `member.DeclaringType != expression.Type`: it looks up the member on `expression.Type`, then calls `MappingSchema.GetAttribute<AssociationAttribute>(expression.Type, newMember)` directly using the realized concrete type as the owner type. The comment at lines 78-85 explains the motivation: after a projection such as `Select(x => x.Customer)` the member's `ReflectedType` may already match `expression.Type` while `DeclaringType` still points at an unregistered abstract base (e.g. an EF Core `IEntityTypeConfiguration`-based base class). Using `expression.Type` directly avoids `EFCoreMetadataReader` returning nothing for the unregistered base. The method also recurses `IsAssociationInRealization(null, newMember, ...)` as a further fallback.

11. **Proxy / build-proxy subsystem.**
    - `IBuildProxy` / `BuildProxyBase<TOwner>` -- interface and generic abstract base for build proxies. `BuildProxyBase` implements `IBuildContext.MakeExpression` by mapping member/`ContextRefExpression` paths through the inner `BuildContext`, then routing results to `HandleTranslated` (for `SqlPlaceholderExpression` leaves) or `ProcessTranslated` (for composite expressions). `ProcessTranslated` handles these expression shapes: `ContextRefExpression` (creates a new proxy), `MemberExpression`, `MethodCallExpression` (via `ParseGenericConstructor`), `SqlGenericConstructorExpression` (recursively processes assignments and parameters), `NewExpression`/`MemberInitExpression` (via `ParseGenericConstructor`), `SqlPlaceholderExpression` (calls `HandleTranslated`), `Convert`/`ConvertChecked` unary nodes, `SqlAdjustTypeExpression`, and `SqlDefaultIfEmptyExpression`. `BuildProxyBase` now implements `Equals(object?)` and `GetHashCode()` comparing `OwnerContext`, `BuildContext`, `OwnerContextRef`, `CurrentPath`, and `InnerExpression`.

12. **Entity construction / column mapping.**
    - `EntityConstructorBase` -- abstract base shared by `TableContext` and other contexts that need to build full-entity `SqlGenericConstructorExpression` trees. `BuildGenericFromMembers` iterates `ColumnDescriptor` collections, respects `SkipOnInsert` / `SkipOnUpdate` / `SkipOnEntityFetch` flags, handles nested dotted member names.

13. **`SequenceHelper` utilities.** Static class with context-manipulation helpers: `PrepareBody`, `IsSameContext` / `CreateRef` / `CorrectExpression` / `CorrectTrackingPath`, `UnwrapProxy`, `FindError`, `CreateSpecialProperty`.

14. **Parameter binding (`ParametersContext`).** Owns `CurrentSqlParameters` (the ordered `ParameterAccessor` list wired into the compiled delegate), `BuildParameter` (the main SQL-parameter allocation entry point), `RegisterDynamicExpressionAccessor`, `SimplifyConversion`.

15. **AsQueryable parameterization API (PR #5495).** New public API in `LinqToDB.Linq` namespace:
    - `LinqExtensions.AsQueryable<TElement>(IEnumerable<TElement> source, IDataContext dataContext, Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>> configure)` (`Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs:41`) -- the 3-arg overload. Wraps `source` in `Expression.Constant`, the context in `SqlQueryRootExpression.Create`, and `configure` in `Expression.Quote`; normal `ExpressionQueryImpl<T>` dispatch follows.
    - The `[BuildsMethodCall(nameof(LinqExtensions.AsQueryable))]` on `EnumerableBuilder` is already present; the dispatcher additionally requires `CanBuildMethod` to match `Methods.LinqToDB.AsQueryableConfigured` (registered in `Methods.cs:205` as the 3-arg overload via `MemberHelper.MethodOfGeneric`).
    - `EnumerableBuilder.BuildConfigured` (lines 103-141): validates the source can be evaluated on the client (rejects outer-query-referencing arrays with a clear error message), unwraps the configure lambda, calls `TryParseConfigure` to walk the method-call chain, allocates the `SqlParameter` via `BuildParameter(..., InPredicate)`, constructs `EnumerableParameterizationConfig`, and creates `EnumerableContext` with that config.
    - `EnumerableBuilder.TryParseConfigure` (lines 143-236): walks a `MethodCallExpression` chain on `configureLambda.Body`. Recognized calls: `Parameterize()` sets `defaultForceParameter = true`; `Inline()` sets it `false`; `Except(nae)` extracts each selector lambda, substitutes the shared `rowParameter`, strips boxing `Convert`, and appends to `exceptedList`. Any unrecognized method name or a non-`NewArrayExpression` `Except` argument returns `false` with a descriptive error.

16. **Aggregation subsystem (PR #5502 + PR #5557 -- this delta).** The aggregation pipeline spans `ExpressionBuilder.Aggregation.cs`, `AggregateExecuteBuilder.cs`, and the public `LinqToDB.Linq.Translation` layer:

    - `ExpressionBuilder.AggregationContext` nested sealed class -- implements `IAggregationContext`. Properties: `RootContext` (`ContextRefExpression?`), `ValueParameter` (`ParameterExpression?`), `FilterExpressions` (`Expression[]?`), `ValueExpression` (`Expression?`), `Items` (`Expression[]?` -- used in captured-collection path), `OrderBy` (`ITranslationContext.OrderByInformation[]`, default `[]`), `IsDistinct`, `IsGroupBy`, `IsEmptyGroupBy`, `SqlContext` (`ContextRefExpression?`). `TranslateExpression` builds a `SqlPlaceholderExpression` from any expression via the current `SqlContext`; for `GroupByContext` sources it reroutes through the group element. `SimplifyEntityLambda` substitutes the lambda parameter with the group's element ref.
    - `ExpressionBuilder.BuildAggregationFunction` (aggregate path, `ExpressionBuilder.Aggregation.cs:483`) -- handles LINQ method calls whose source resolves to a `ContextRefExpression`. Builds an `AggregationContext` from the chain (Where/Select/Distinct/OrderBy), calls `functionFactory(aggregationInfo)`, wraps the result via `WrapAggregateResult`. The fallback chain (`BuildAggregateExecuteExpression`) is triggered when the source isn't a simple group-by context or an `AggregateRootContext`.
    - `ExpressionBuilder.BuildArrayAggregationFunction` (`ExpressionBuilder.Aggregation.cs:129`) -- **new method (PR #5557)**. Handles the "plain array" path: the aggregate's sequence argument is a `NewArrayExpression` (captured collection literal). Unpacks the `NewArrayExpression` after traversal, iterates `chain` to collect `Distinct`/`Select`/`Where`/`OrderBy` operators that modify it, builds an `AggregationContext` with `Items` populated (not `ValueExpression`), and calls `functionFactory`. The `valueParameter` is `Expression.Parameter(elementType, "e")`.
    - `WrapAggregateResult` static helper (`ExpressionBuilder.Aggregation.cs:313`) -- given a `SqlPlaceholderExpression` from `functionFactory`, creates a `SqlAggregateLifterExpression` when either delegate is non-null. Critically: for `info.IsGroupBy`, applies `sqlRewriter` **eagerly** (wraps the placeholder immediately) and passes `sqlRewriter = null` to the lifter so the OUTER APPLY hook is not invoked again; for non-grouped aggregates, the rewriter is deferred to lift time inside `AggregateExecuteContext.CreateWeakOuterJoin`.
    - `AggregateExecuteBuilder.BuildMethodCall`: after obtaining `translated = builder.BuildSqlExpression(placeholderSequence, aggregateBody)`, branches on `SqlAggregateLifterExpression` to extract `translatedPlaceholder`, `materializationCheck`, and `sqlRewriter` -- these are stored on the `AggregateExecuteContext`.
    - `AggregateExecuteContext.CreateWeakOuterJoin` (`AggregateExecuteBuilder.cs:204`): after `UpdateNesting` promotes the inner aggregate to a parent-side column reference, calls `SqlRewriter(Placeholder)` then sets `SqlRewriter = null` -- ensures COALESCE is applied to the lifted reference, not the inner aggregate SQL, preserving the bare aggregate for provider validation/optimization.
    - `BuildAggregationFunctionResult.Skipped()` -- sentinel factory (PR #5557). `AggregateFunctionBuilder.Build(isExpression: true)` returns `null` when `_skipped` is set, allowing callers in Expression mode to fall through to partial-translation rather than receiving an error.
    - `AggregateFunctionBuilder.ModeConfig` additions: `IsServerSideOnly` (strict error on translation failure regardless of `isExpression`), `ItemTransform` (per-item rewrite for plain mode), `ValueTransform` (per-value rewrite for aggregate mode). Fluent accessors via `AggregateModeBuilder.IsServerSideOnly(bool)`, `.TransformItems(...)`, `.TransformValue(...)`.

17. **Update / SET subsystem (PR #5506 -- this delta).**
    - `UpdateBuilder.InitializeSetExpressions.ApplyConversions.NeedsConversion` (`UpdateBuilder.cs:536`) -- new static helper. Returns `false` for `SqlParameter`, `SqlValue`, `SqlColumn`, `SqlField` (these already carry the stored value and need no converter re-application), `SqlAnchor` (delegates to inner expression). For `SqlExpression` / `SqlFunction` (both implementing `SqlParameterizedExpressionBase`): scans `Parameters` to detect whether any parameter subtree references the target column (by `ReferenceEquals` on the `ColumnDescriptor`); if found, the expression is a server-side transform on the stored value and returns `false`; otherwise returns `true`. Falls through to `true` for all other cases. This prevents `ValueConverter.ToProviderExpression` from being applied a second time when the SET right-hand side is already a server-side field expression (e.g. a `jsonb_set(...)` call that receives and returns the same storage type).

18. **`ExpressionBuildVisitor` CTE-context cloning fix (this delta).**
    - `ExpressionBuildVisitor.Clone(CloningContext)` (`ExpressionBuildVisitor.cs:79`) now also copies `_cteContexts` into the cloned visitor by correcting its expression keys with `cloningContext.CorrectExpression` and cloning its context values with `cloningContext.CloneContext`. Previously `_cteContexts` was left null in the clone, meaning that CTE lookups via `FindRegisteredCteContext` would miss entries in the cloned visitor during subquery reuse scenarios. The `_cteContexts` field is typed `Dictionary<Expression, CteContext>?` with `ExpressionEqualityComparer.Instance` as comparer.

19. **Misc helpers.**
    - `ProjectFlagExtensions` -- `[MethodImpl(AggressiveInlining)]` extension methods exposing each `ProjectFlags` bit as a named predicate.
    - `ProjectionPathHelper` -- walks a `SqlGenericConstructorExpression` tree applying a caller-supplied `TraverseProjection` delegate.
    - `EvaluationHelper` -- static `EvaluateExpression(expression, dataContext, parameterValues)`.
    - `BuildContextDebuggingHelper` -- `GetContextInfo` includes non-DEBUG branches for `TableContext` (table source ID), `ScopeContext` (context/upTo query IDs), and `SubQueryContext` (SC suffix). `ContextId` field is `#if DEBUG` only. `GetPath` walks the parent chain building a diagnostic string.
    - `ExposeExpressionVisitor` -- carries `_isSingleConvert` bool state (cleared in `Cleanup`); controls single-convert fast-path logic inside the expose step.

20. **.NET 8+ only builders.**
    - `DistinctByBuilder` (`#if NET8_0_OR_GREATER`) -- `[BuildsMethodCall(nameof(Queryable.DistinctBy))]`. Requires 2-argument form. Extracts a preceding `OrderBy` from the source (via `WindowFunctionHelpers.ExtractOrderByPart`) and falls through via one of two strategies: when `IsWindowFunctionsSupported`, rewrites as `Select(e => new { Entity = e, RowNumber = ROW_NUMBER() OVER (PARTITION BY ...) }).Where(e => e.RowNumber == 1).Select(e => e.Entity)` (`BuildDistinctByViaRowNumber<T>`). When `IsOuterApplyJoinSupportsCondition` and not a subquery, rewrites as a CROSS APPLY / OUTER APPLY with an inner `Where + Take(1)` (`BuildDistinctByViaOuterApply<T,TSelector>`). Falls through to `NotSupported()` otherwise. Applies the configured default `NullsPosition` to extracted `OrderBy` keys.
    - `MinByMaxByBuilder` (`#if NET8_0_OR_GREATER`) -- `[BuildsMethodCall(nameof(Queryable.MinBy), nameof(Queryable.MaxBy))]`. Requires 2-argument form. Transforms: `MinBy(selector)` -> `OrderBy(selector).First[OrDefault]()`, `MaxBy(selector)` -> `OrderByDescending(selector).First[OrDefault]()`. Preserves any preceding `OrderBy` via `WindowFunctionHelpers.ExtractOrderByPart`; re-emits those as `ThenBy`/`ThenByDescending` NULLS-aware calls (with resolved default `NullsPosition`). Chooses `FirstOrDefault` for nullable/reference element types or subquery builds; `First` otherwise.
    - `SetOperationByBuilder` (`#if NET8_0_OR_GREATER`) -- `[BuildsMethodCall(nameof(Queryable.ExceptBy), nameof(Queryable.UnionBy), nameof(Queryable.IntersectBy))]`. Requires 3-argument form. Requires `IsWindowFunctionsSupported`; returns `Error(ErrorHelper.Error_RowNumber)` otherwise. Dispatches to `BuildExceptBy`, `BuildIntersectBy`, `BuildUnionBy` static helpers that rewrite set-membership tests via `ROW_NUMBER() OVER (PARTITION BY keySelector)` subqueries.

21. **`OrderByBuilder` IComparer guard.**
    - `OrderByBuilder.CanBuildMethod` now explicitly rejects BCL `OrderBy`/`ThenBy` overloads that take an `IComparer<TKey>` argument (`parameters.Length > 2 && parameters[2].ParameterType != typeof(Sql.NullsPosition)`). Previously these would silently be accepted and the extra argument ignored; now they are declined so the call falls through to client-side evaluation.

## Interactions

- **Driven by [`LINQ`](../LINQ/INDEX.md):** `Query<T>.GetQuery` / `ExpressionQuery` create an `ExpressionBuilder` and call `Build<T>` once per cache miss.
- **Consumes [`SQL-AST`](../SQL-AST/INDEX.md):** every builder mutates / constructs `SelectQuery`, `SqlField`, `SqlBinaryExpression`, `SqlPlaceholderExpression`, `SqlInsertStatement`, etc.
- **Hands off to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md):** finalized `SqlStatement` flows into `ISqlOptimizer.Finalize` then `ISqlBuilder` for emission.
- **Uses [`MAPPING`](../MAPPING/INDEX.md):** `EntityDescriptor` lookups for query filters, column mapping for materialization, association definitions.
- **Uses [`EXPR`](../EXPR/INDEX.md):** `ContextRefExpression`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, `SqlAggregateLifterExpression` etc. live in `LinqToDB.Internal.Expressions`.
- **Uses [`INFRA`](../INFRA/INDEX.md):** `ObjectPool`, `ActivityService` metrics, common helpers.
- **Generated by [`CODEGEN`](../CODEGEN/INDEX.md):** `BuildersGenerator` emits `ExpressionBuilder.g.cs` containing `FindBuilderImpl`.
- **Cross-link [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md):** `Enum.HasFlag` translation -- `ProviderMemberTranslatorDefault.ProcessHasFlag` (a SQL-PROVIDER / INTERNAL-API concern) was added in the same delta batch as the AsQueryable parameterization API; `Source/LinqToDB/Linq/Expressions.cs` was touched in that PR for member-mapping registration of `HasFlag`.

## Inbound / outbound dependencies

- **Inbound:** `LINQ` (`Internal/Linq/Query.cs`, `Internal/Linq/ExpressionQuery.cs`) constructs `ExpressionBuilder`. `EXPR` types are referenced inbound via `ContextRefExpression.BuildContext`.
- **Outbound:** `LinqToDB.Internal.SqlQuery.*` (heavy), `LinqToDB.Internal.Expressions.*` (including `SqlAggregateLifterExpression`), `LinqToDB.Internal.Conversion`, `LinqToDB.Internal.DataProvider.Translation` (`IMemberTranslator`), `LinqToDB.Mapping`, `LinqToDB.Metrics`, `LinqToDB.Internal.Reflection.Methods` (well-known method handles). Public translation layer: `LinqToDB.Linq.Translation.AggregateFunctionBuilder`, `BuildAggregationFunctionResult`.

## Known issues / debt

- Source-generator dispatcher relies on every builder declaring its triggering attributes correctly; a missing attribute is a silent miss (the call falls through to `null` and surfaces as `BuildSequenceResult.NotSupported`).
- Many `*Builder.BuildMethodCall` methods carry `[BuildsMethodCall]` lists that include legacy method names alongside current ones (`OrderBy` + `ThenOrBy`, `Insert` + `InsertWithOutput*`); changes here can affect provider compatibility.
- `IBuildContext` has a `// TODO: probably not needed` on `Parent.set`.
- `BuildInfo` is a mutable bag with `set;` accessors; its `IsSubQuery` is derived from `Parent != null` rather than tracked explicitly.
- `MethodCallParser.cs` referenced in `kb-areas.md` does not exist -- the area registry should be updated to remove it.
- `MergeBuilder.UpdateWhenMatched` silently skips the entire Update operation when no updatable non-key columns are found with an implicit setter (workaround for issue #2843).
- `TableLikeQueryContext`'s `ProjectionHelper<,>` helper type uses a `selft_target` property name (typo for `self_target`) -- a cosmetic issue but visible in reflection-based diagnostics.
- `ExpressionBuildVisitor` carries both `_translationCache` and `_columnCache` as mutable `SnapshotDictionary` fields; the `Clone(CloningContext)` path copies only entries whose keys reference cloned contexts. `_cteContexts` cloning was missing until this delta (PR #5557 batch).
- `CteContext.InitQuery()` uses a `_isRecursiveCall` flag to guard against re-entrant initialization but does not hold a lock -- concurrent compilation of the same CTE from multiple threads would produce undefined behavior. Currently safe because `ExpressionBuilder` is not shared.
- `EnumerableBuilder.BuildConfigured` rejects sources that reference outer query state with a runtime error at query-build time rather than a compile-time diagnostic.
- `IAsQueryableBuilder<T>` and `IAsQueryableExceptBuilder<T>` methods are marker-only; calling them outside an expression context is undefined behaviour and the implementations are not provided (the interfaces have no concrete class backing them in the shipped assembly).
- `UpdateBuilder.NeedsConversion` applies `ValueConverter` wrapping only when `NeedsConversion` returns true. The `SqlExpression`/`SqlFunction` column-reference check uses `ReferenceEquals` on `ColumnDescriptor` objects -- if a descriptor is not the identical instance (e.g. after mapping schema rebuild), the check may produce a false positive, re-applying the converter.
- `AggregateFunctionBuilder.Build` with `isExpression: true` and `!IsServerSideOnly` can return `null` (lenient bail-out) -- callers that do not handle `null` will throw a `NullReferenceException` rather than getting a SQL error. This is intentional design but undocumented in the public surface XML docs.
- `DistinctByBuilder`, `MinByMaxByBuilder`, `SetOperationByBuilder` are all `#if NET8_0_OR_GREATER` conditional -- on `net462`/`netstandard2.0` builds these operators are silently unavailable (fall through to `NotSupported` in the dispatcher because the class is not compiled). No runtime error surface for callers on older TFMs.
- `OrderByBuilder` now rejects `IComparer<TKey>` overloads -- callers that previously relied on silent acceptance (ignoring the comparer) will get a `NotSupported` result and likely a runtime exception.
- `LoadWithMember.Equals` compares only `MemberInfo` -- two `LoadWithMember` instances for the same member with different `FilterExpression` or `FilterFunc` compare as equal. This can cause unexpected deduplication in `LoadWithEntity.MembersToLoad` comparisons.

## See also

- [`SQL-AST`](../SQL-AST/INDEX.md) -- output target of every builder.
- [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) -- receives the finalized `SqlStatement`.
- [`LINQ`](../LINQ/INDEX.md) -- caller; also owns query caching.
- [`EXPR`](../EXPR/INDEX.md) -- `ContextRefExpression`, `SqlPlaceholderExpression`, `SqlAggregateLifterExpression`, visitor infrastructure.
- [`CODEGEN`](../CODEGEN/INDEX.md) -- `BuildersGenerator` Roslyn source generator.
- [`../../architecture/expression-translator.md`](../../architecture/expression-translator.md) -- narrative walkthrough.
- `.agents/docs/code-design.md` -- public-API + AST namespace invariants.

## Pointers

- Dispatcher template: `Source/CodeGenerators/BuildersGenerator.cs:165`.
- Per-query entry: `ExpressionBuilder.Build<T>` at `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:124`.
- Recursion core: `TryBuildSequence` at `ExpressionBuilder.cs:336`.
- Reference operator implementations: `WhereBuilder.cs`, `SelectBuilder.cs`, `OrderByBuilder.cs`, `JoinBuilder.cs`, `GroupByBuilder.cs`, `InsertBuilder.cs`.
- Sequence root: `TableBuilder.cs`, `ContextRefBuilder.cs`, `EnumerableBuilder.cs`.
- Merge pipeline entry: `MergeBuilder.Merge.BuildMethodCall` -> `MergeBuilder.MergeContext`.
- CTE entry: `TableBuilder.CteTableContext.cs` partial -> `CteContext.InitQuery()`.
- Parameter binding: `ParametersContext.BuildParameter` / `CurrentSqlParameters`.
- Expression normalization: `ExposeExpressionVisitor.ExposeExpression` (called by `ExpressionBuilder.ExposeExpression`).
- AsQueryable parameterization entry: `EnumerableBuilder.BuildConfigured` / `TryParseConfigure` (`EnumerableBuilder.cs:103`).
- Association via concrete type: `ExpressionBuilder.AssociationToRealization` + `IsAssociationInRealization` (`ExpressionBuilder.Associations.cs:19`, `ExpressionBuilder.Associations.cs:49`).
- Per-column parameterization decision: `EnumerableParameterizationConfig.ShouldForceParameter` (`EnumerableParameterizationConfig.cs:35`).
- Aggregate pipeline (non-grouped): `ExpressionBuilder.BuildAggregationFunction` (`ExpressionBuilder.Aggregation.cs:483`) -> `AggregateExecuteBuilder.AggregateExecuteContext.CreateWeakOuterJoin` -> `SqlRewriter` hook.
- Aggregate pipeline (grouped): `WrapAggregateResult` (`ExpressionBuilder.Aggregation.cs:313`) applies `sqlRewriter` eagerly.
- Captured-collection aggregate: `ExpressionBuilder.BuildArrayAggregationFunction` (`ExpressionBuilder.Aggregation.cs:129`).
- COALESCE / null-check wrapper: `SqlAggregateLifterExpression` (`Source/LinqToDB/Internal/Expressions/SqlAggregateLifterExpression.cs`).
- ValueConverter skip-for-field: `UpdateBuilder.NeedsConversion` (`UpdateBuilder.cs:536`).
- CTE-context clone fix: `ExpressionBuildVisitor.Clone` (`ExpressionBuildVisitor.cs:119`).
- DistinctBy rewrite entry: `DistinctByBuilder.BuildMethodCall` -> `BuildDistinctByViaRowNumber` / `BuildDistinctByViaOuterApply`.
- MinBy/MaxBy rewrite entry: `MinByMaxByBuilder.BuildMethodCall` (`MinByMaxByBuilder.cs:19`).
- Set-operation-by rewrite entry: `SetOperationByBuilder.BuildMethodCall` -> `BuildExceptBy` / `BuildIntersectBy` / `BuildUnionBy`.
- CteAnnotationsContainer cache-key: `CteAnnotationsContainer.BuildCacheKey` (`CteAnnotationsContainer.cs:45`).
- OrderByBuilder IComparer guard: `OrderByBuilder.CanBuildMethod` (`OrderByBuilder.cs:24-26`).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 63 / 63
- Tier 2 (visited / total): 71 / 71

Read (prior runs -- build sha 4a478ff14):
- `EnumerableBuilder.cs` -- BuildConfigured + TryParseConfigure (PR #5495)
- `EnumerableContext.cs` -- _parameterization optional ctor param
- `EnumerableParameterizationConfig.cs` (new file -- PR #5495)
- `ExpressionBuilder.Associations.cs` -- AssociationToRealization + IsAssociationInRealization (PR #5511 / #5510)
- `ExpressionBuilder.SqlBuilder.cs` -- Project handling of Convert(body) wrapping
- `Source/LinqToDB/Linq/Expressions.cs` (LinqToDB.Linq, sampled for AsQueryable registration)
- `Source/LinqToDB/Linq/IAsQueryableBuilder.cs` (new -- PR #5495)
- `Source/LinqToDB/Linq/IAsQueryableExceptBuilder.cs` (new -- PR #5495)
- `Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs` -- 3-arg overload
- `Source/LinqToDB/Internal/Reflection/Methods.cs` -- AsQueryableConfigured registration at :205

Read (this run -- delta sha 2e67bafc9):
- `Source/LinqToDB/Internal/Linq/Builder/AggregateExecuteBuilder.cs` -- AggregateExecuteContext now handles SqlAggregateLifterExpression (MaterializationCheck + SqlRewriter); CreateWeakOuterJoin applies SqlRewriter after UpdateNesting at OUTER APPLY lift time (PR #5502/#5557)
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs` (lines 79-133) -- Clone now copies _cteContexts alongside _associations/_translationCache/_columnCache (PR #5557 correctness fix)
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.Aggregation.cs` -- BuildArrayAggregationFunction (new method for captured-collection aggregates, PR #5557); WrapAggregateResult static helper (eager SqlRewriter for grouped, deferred for non-grouped); AggregationContext nested class
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.Associations.cs` -- IsAssociationInRealization third branch extended (PR #5548): guard now also fires when member.DeclaringType != expression.Type
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs` (lines 1-200) -- no net-new structural changes; confirmed Build<T> / FindBuilderImpl unchanged
- `Source/LinqToDB/Internal/Linq/Builder/UpdateBuilder.cs` -- NeedsConversion static helper (PR #5506): skips ValueConverter wrap for SqlParameter/SqlValue/SqlColumn/SqlField/SqlAnchor
- `Source/LinqToDB/Linq/Expressions.cs` (lines 1-80) -- MapMember overload surface; no aggregation-relevant changes in scanned range
- `Source/LinqToDB/Linq/Translation/AggregateFunctionBuilder.cs` -- IsServerSideOnly/ItemTransform/ValueTransform on ModeConfig; Build(isExpression) lenient bail-out path via Skipped()
- `Source/LinqToDB/Linq/Translation/BuildAggregationFunctionResult.cs` -- Skipped() sentinel factory and IsSkipped bool added (PR #5557)
- `Source/LinqToDB/Linq/Translation/WindowFunctionsMemberTranslator.cs` (lines 1-120) -- registration surface; no aggregate-pipeline changes in scanned range

Read (this run -- delta sha b3340aa9):
- `Source/LinqToDB/Internal/Linq/Builder/BuildContextDebuggingHelper.cs` -- GetContextInfo handles ScopeContext and SubQueryContext; ContextId is #if DEBUG only
- `Source/LinqToDB/Internal/Linq/Builder/BuildProxyBase{TOwner}.cs` -- ProcessTranslated handles SqlAdjustTypeExpression and SqlDefaultIfEmptyExpression; Equals/GetHashCode added
- `Source/LinqToDB/Internal/Linq/Builder/CteAnnotationsContainer.cs` -- implements IExpressionCacheKey and IEquatable<CteAnnotationsContainer>; ordinal-sorted BuildCacheKey
- `Source/LinqToDB/Internal/Linq/Builder/DistinctByBuilder.cs` -- new #if NET8_0_OR_GREATER builder for Queryable.DistinctBy; ROW_NUMBER or OUTER APPLY rewrite
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs` -- _cteContexts is Dictionary<Expression, CteContext>? with ExpressionEqualityComparer.Instance; Clone copies it
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.Aggregation.cs` -- AggregationContext properties confirmed; TranslateExpression/SimplifyEntityLambda visible
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.SqlBuilder.cs` -- no structural changes (BuildWhere entry confirmed)
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs` -- no structural changes; FindBuilderImpl partial declaration confirmed
- `Source/LinqToDB/Internal/Linq/Builder/LoadWithEntity.cs` -- Equals/GetHashCode added; ToDebugString helpers
- `Source/LinqToDB/Internal/Linq/Builder/LoadWithMember.cs` -- Equals compares by MemberInfo only; ToDebugString FF/FE suffixes
- `Source/LinqToDB/Internal/Linq/Builder/MergeBuilder.On.cs` -- no structural changes
- `Source/LinqToDB/Internal/Linq/Builder/MergeBuilder.UpdateWhenMatched.cs` -- no structural changes (scanned first 60 lines)
- `Source/LinqToDB/Internal/Linq/Builder/MergeBuilder.UpdateWhenMatchedThenDelete.cs` -- no structural changes; predicate + deleteCondition pair
- `Source/LinqToDB/Internal/Linq/Builder/MinByMaxByBuilder.cs` -- new #if NET8_0_OR_GREATER builder for Queryable.MinBy/MaxBy; OrderBy+First[OrDefault] rewrite
- `Source/LinqToDB/Internal/Linq/Builder/OrderByBuilder.cs` -- CanBuildMethod rejects IComparer<TKey> overloads
- `Source/LinqToDB/Internal/Linq/Builder/ScopeContext.cs` -- Clone delegates to cloningContext.CloneContext for both Context and UpTo
- `Source/LinqToDB/Internal/Linq/Builder/SelectBuilder.cs` -- CounterContext inner class for 2-param lambda index-selector
- `Source/LinqToDB/Internal/Linq/Builder/SetOperationByBuilder.cs` -- new #if NET8_0_OR_GREATER builder for ExceptBy/UnionBy/IntersectBy; requires IsWindowFunctionsSupported
- `Source/LinqToDB/Internal/Linq/Builder/TranslationModifier.cs` -- WithIgnoreQueryFilters merges via union; sealed confirmed
- `Source/LinqToDB/Internal/Linq/Builder/Visitors/ExposeExpressionVisitor.cs` -- _isSingleConvert bool field added; cleared in Cleanup
- `Source/LinqToDB/Linq/Expressions.cs` (lines 1-80) -- NormalizeMemeberInfo helper (note: typo in source); MapMember overloads
- `Source/LinqToDB/Linq/Translation/AggregateFunctionBuilder.cs` (lines 1-80) -- _skipped bool instance field; reset at top of Build

- Tier 3 (skipped, logged): 0
</details>
