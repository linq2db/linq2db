---
area: EXPR-TRANS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 63/63
coverage_tier_2: 71/71
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
- `` `TranslationModifier` `` (`TranslationModifier.cs:8`) — immutable value type propagated through every `IBuildContext`. Carries `InlineParameters` and `IgnoreQueryFilters` (an array of entity types whose global filters are bypassed). Participates in structural equality so two queries with different filter-disable sets produce different cache entries.
- `` `ParametersContext` `` (`ParametersContext.cs:18`) — manages `SqlParameter` allocation and the `ExpressionCacheManager` for dynamic expression accessors. Holds `CurrentSqlParameters` (the ordered list of `ParameterAccessor` values emitted into the compiled query delegate) and `RegisterDynamicExpressionAccessor` for association-query lambdas.

## Files (Tier 1 / Tier 2)

### Tier 1 — pinned (63 files)

`ExpressionBuilder.cs` plus every `*Builder.cs` at the folder root.

### Tier 2 — implementation / contexts / helpers (71 files)

Examples: `BuildInfo.cs`, `BuildSequenceResult.cs`, `BuildContextBase.cs`, `SequenceContextBase.cs`, `IBuildContext.cs`, `ISequenceBuilder.cs`, `Attributes.cs`, `BuildFlags.cs`, `BuildPurpose.cs`, `ProjectFlags.cs`, `ProjectFlagExtensions.cs`, `ProjectionPathHelper.cs`, `SubQueryContext.cs`, `SelectContext.cs`, `AnchorContext.cs`, `AsSubqueryContext.cs`, `EagerContext.cs`, `EagerLoading.cs`, `ScopeContext.cs`, `PassThroughContext.cs`, `SingleExpressionContext.cs`, `EnumerableContext.cs`, `EnumerableContextDynamic.cs`, `CteContext.cs`, `CteTableContext.cs`, `CteAnnotationsContainer.cs`, `CteBuilderImpl.cs`, `TableLikeQueryContext.cs`, `TableLikeHelpers.cs`, `TableBuilder.TableContext.cs`, `TableBuilder.RawSqlContext.cs`, `TableBuilder.CteTableContext.cs`, `MergeBuilder.*.cs` partials (12), `MergeProjectionHelper.cs`, `AssociationHelper.cs`, `LoadWithEntity.cs`, `LoadWithMember.cs`, `IAnnotatableBuilderInternal.cs`, `IBuildProxy.cs`, `BuildProxyBase{TOwner}.cs`, `ILoadWithContext.cs`, `ITableContext.cs`, `EntityConstructorBase.cs`, `RecordReaderBuilder.cs` helpers, `SequenceHelper.cs`, `EvaluationHelper.cs`, `LambdaResolveVisitor.cs`, `ExpressionBuildVisitor.cs`, `BinaryExpressionAggregatorVisitor.cs`, `ExpressionTreeOptimizerVisitor.cs`, `ExpressionTreeOptimizationContext.cs`, `ExpressionTestGenerator.cs`, `ParametersContext.cs`, `TranslationModifier.cs`, `BuildContextDebuggingHelper.cs`, plus the two files under `Visitors/` (`ExposeExpressionVisitor.cs`, `CanBeEvaluatedOnClientCheckVisitorBase.cs`).

## Subsystems

1. **Dispatcher (source-generated).** `ExpressionBuilder.FindBuilderImpl` is generated by `BuildersGenerator` (`Source/CodeGenerators/BuildersGenerator.cs:14`) from `[BuildsExpression]`, `[BuildsMethodCall]`, `[BuildsAny]` markers on each `*Builder` class. The generator emits a single `switch (expr.NodeType)` over `ExpressionType`, with a nested `switch (call.Method.Name)` for the `Call` case, and finally falls through to the `[BuildsAny]` set (`ContextRefBuilder` is the only one). Each candidate calls a static `CanBuild` / `CanBuildMethod` predicate; the first that returns `true` wins, and the dispatcher returns the singleton instance from `Builder<T>.Instance` (line 184 of the template). Adding a new operator = drop a `[BuildsMethodCall("Foo")]`-decorated `FooBuilder.cs`; the generator picks it up next build.

2. **`BuildSequence` recursion.** `ExpressionBuilder.TryBuildSequence(BuildInfo)` (line 336) is the recursive workhorse: it `ExpandToRoot`s the expression (resolves macros / associations), runs `TryFindBuilder`, asks the chosen builder for a context, registers the originating expression on the context (`RegisterSequenceExpression`), and returns. Most builders themselves call back into `builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]))` to translate the source sequence (e.g. `WhereBuilder.cs:16`, `SelectBuilder.cs:31`, `OrderByBuilder.cs:38`, `JoinBuilder.cs:35`).

3. **Context graph.** Each builder produces an `IBuildContext`. Composite operators (`Where`, `Select`, `OrderBy`, joins) wrap the child context — typically in a `SubQueryContext` (`SubQueryContext.cs`) when the parent already has `Distinct` / `Take` / `Skip` / projection, e.g. `WhereBuilder.cs:25-30` and `SelectBuilder.cs:42`. Joins create `outer`/`inner` `SubQueryContext` pairs (`JoinBuilder.cs:46-47`). The resulting `IBuildContext` chain mirrors the LINQ method chain in reverse and drives later projection resolution via `MakeExpression`.

   Context specializations (all Tier 2, all read this run):
   - `` `SubQueryContext` `` — wraps a child context in a new outer `SelectQuery` with `FROM (SELECT ...)`. Adds the inner `SelectQuery` to the outer `FROM` when `addToSql = true`. `IsSelectWrapper` flag distinguishes a pure projection wrapper from a real sub-select. Delegates `SetRunQuery`, `GetContext`, and `Clone` to the child.
   - `` `AsSubqueryContext` `` — thin subclass of `SubQueryContext` that overrides `MakeExpression` to return `path` unchanged for root/aggregation-root flags, forcing the result to remain as a subquery reference rather than being inlined.
   - `` `SelectContext` `` — implements both scalar and member-projection selects. Holds `Body` (the lambda body expression post-parameter-substitution) and `InnerContext`. `MakeExpression` either delegates to `InnerContext` via `ContextRefExpression` body shortcuts or calls `Builder.Project` to walk nested member paths.
   - `` `ScopeContext` `` — bridges two contexts: `Context` (the inner scope) and `UpTo` (the parent scope). `MakeExpression` builds the expression in `Context` then re-resolves it against `UpTo.SelectQuery` for SQL output, preventing accidental reference escape across scope boundaries.
   - `` `PassThroughContext` `` (abstract) — forwards all `IBuildContext` operations to an inner `Context`. Base class for contexts that add no query logic but need to intercept a single operation (e.g., `AsSubqueryContext`).
   - `` `SingleExpressionContext` `` — wraps a single `ISqlExpression` as an `IBuildContext`. `MakeExpression` always returns a `SqlPlaceholderExpression` wrapping the held `ISqlExpression`. Used for scalar subqueries and function-call results that need context identity.
   - `` `AnchorContext` `` — wraps a child sequence and transforms every `SqlPlaceholderExpression` it produces (except those already wrapped in `SqlAnchor`) into `SqlAnchor(placeholder.Sql, AnchorKind)`. Implements provider hint anchoring for specific SQL constructs.
   - `` `EagerContext` `` — lightweight context that delegates `MakeExpression` to a corrected path in the wrapped `Context`, but returns `path` unchanged for root/subquery flags. Used to mark eager-load entry points without introducing a new `SelectQuery` level.
   - `` `EnumerableContext` `` — context backed by `SqlValuesTable`. Maps LINQ-side member paths to `SqlField` entries in the `VALUES(...)` table via `_fieldsMap`. Supports `IDataContext` data rows and `SqlParameter`-style placeholders.
   - `` `EnumerableContextDynamic` `` — variant of `EnumerableContext` for runtime expression rows (e.g. `db.GetTable<T>().InnerJoin(enumerable, ...)` where `enumerable` is not a constant). Tracks per-row expressions in `_expressionRows` / `_byPathExpressions` and supports a synthetic row-index property via `EnsureRowIndexCreated`.
   - `` `CteContext` `` — holds the `CteClause` and deferred-build state for a CTE. `InitQuery()` triggers a recursive `BuildSequence` call inside `Builder.PushRecursive` to construct the CTE body. Maps recursive-CTE forward references via `_recursiveMap`. Inner `SubQueryContext` is created after the body is built.
   - `` `CteTableContext` `` — implements `ITableContext` over a `SqlCteTable` (a `SqlTable` subclass that references a `CteClause`). Produces `SqlField` placeholders from the CTE's columns.
   - `` `TableBuilder.TableContext` `` — the primary table context. Constructed from `EntityDescriptor` + `SqlTable` via three constructors (from `BuildInfo`, from explicit `SqlTable`, or from a pre-existing `SelectQuery`). Registers query filters, manages inheritance type resolution (`GetObjectType`), and implements full `MakeExpression` / `GetContext` / `Clone` semantics.
   - `` `TableBuilder.RawSqlContext` `` — context for `FromSql<T>` / raw SQL fragments. The inner `SimpleSelectContext` returns `path` unchanged from `MakeExpression`; the outer code builds an `SqlRawSqlTable` and attaches parameters.
   - `` `TableBuilder.CteTableContext` `` — partial of `TableBuilder` that implements the `AsCte` / `AsCteInternal` method call handling, creating or looking up a `CteContext` and wrapping it in a `CteTableContext`.

4. **`SelectQuery` plumbing.** Every builder mutates the running `SelectQuery` (from `BuildInfo.SelectQuery`) — adding `Where`/`Having` clauses, columns, `OrderBy`, joins. The `SelectQuery` instance is allocated from `ExpressionBuilder.QueryPool` (line 44) at the entry point and threaded through `BuildInfo`. The final `SqlStatement` is constructed by terminal builders (`InsertBuilder`, `DeleteBuilder`, `UpdateBuilder`, `MergeBuilder`, etc.) — see `InsertBuilder.cs:36` (`new SqlInsertStatement(sequence.SelectQuery)`).

5. **Projection / materialization.** After `BuildSequence` returns the root context, `ExpressionBuilder.BuildQuery<T>` (line 162) calls `_buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), buildPurpose: BuildPurpose.Expression)` to produce the materialization mapper. This walks the context graph via `IBuildContext.MakeExpression(path, ProjectFlags)` — the same method is reused for SQL-side resolution by passing `ProjectFlags.SQL`. The `ExpressionBuildVisitor` is the central traversal driver; it maintains per-build caches (`_translationCache` as a `SnapshotDictionary<ExprCacheKey, Expression>`, `_columnCache` as a `SnapshotDictionary<ColumnCacheKey, SqlPlaceholderExpression>`), supports cloning for sub-query isolation via `Clone(CloningContext)`, and tracks `BuildPurpose`, `BuildFlags`, nullability context, and the current `ColumnDescriptor`.

   Supporting visitors (Tier 2, all read this run):
   - `` `ExpressionTreeOptimizationContext` `` — stateful container for pre-translation expression analysis. Exposes `IsServerSideOnly(Expression)`, constant-fold helpers, and the `CanBeEvaluatedOnClient` check; instantiated once per query from `ExpressionBuilder` and passed into `ParametersContext`.
   - `` `ExpressionTreeOptimizerVisitor` `` — pure expression rewriter. Folds `IIF(const, x, y)` → `x`/`y`, eliminates double-not (`!!x` → `x`), collapses `IIF(x == y, a, a)` → `a`, and simplifies boolean conditionals such as `IIF(b, true, false)` → `b`. Stateless; used as a pass inside `ExposeExpression`.
   - `` `BinaryExpressionAggregatorVisitor` `` — rebalances deeply right-skewed `AND`/`OR` binary trees with more than 3 leaves into a balanced tree. Prevents stack overflows in subsequent recursive expression visitors for queries with large `IN`-predicate conjunctions. Singleton (`Instance`).
   - `` `ExposeExpressionVisitor` `` (`Visitors/`) — the primary expression normalization pass run before `BuildSequence`. Evaluates client-side sub-expressions, replaces `SqlQueryRootExpression` nodes with the real `IDataContext`, expands `INotifyPropertyChanging` / closure captures, applies `IMemberConverter` translations, and optionally runs condition optimization and binary compaction. Implements `IExpressionEvaluator`.
   - `` `CanBeEvaluatedOnClientCheckVisitorBase` `` (`Visitors/`) — abstract base for the "can this expression be folded client-side?" check. Propagates `CanBeEvaluated = false` immediately on first `ContextRefExpression`, `SqlPlaceholderExpression`, `SqlErrorExpression`, or similar server-side node encounter. Tracks allowed lambda parameters to handle nested lambdas correctly.
   - `` `LambdaResolveVisitor` `` — resolves member and method-call expressions that contain `ContextRefExpression` nodes during lambda body traversal. Calls `Builder.BuildExpandExpression` or `Builder.BuildSqlExpression` for nodes that reference the context, leaving other nodes untouched.
   - `` `ExpressionTestGenerator` `` — diagnostic utility. Generates a self-contained NUnit test source fragment from a LINQ expression tree for bug reproduction. Serializes the expression as C# using a code template; not called in production paths.

6. **Method-chain extension hook (`MethodChainBuilder`).** Aggregate / window-function calls registered via `Sql.ExtensionAttribute` (e.g. `Sql.RowNumber()`, custom `[Sql.Function]` aggregates) are dispatched to `MethodChainBuilder` (`MethodChainBuilder.cs:13`, `[BuildsExpression(ExpressionType.Call)]`) — *not* a per-method `*Builder`. It walks `methodCall.SkipMethodChain` to find the queryable root, builds the source sequence, then asks the extension attribute to produce a `SqlPlaceholderExpression` that gets attached to a synthetic `ChainContext`.

7. **Finalization + handoff to SQL-PROVIDER.** `ExpressionBuilder.BuildQuery<T>` calls `query.SqlOptimizer.Finalize(...)` per query info (line 184), then `SqlProviderHelper.IsValidQuery(...)` (line 188); failure here surfaces as a `SqlErrorExpression` and returns `false`. The optimizer is provider-supplied — see [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md).

8. **MergeBuilder partials.** `MergeBuilder` is a `partial class` spread across 10+ files. The top-level `MergeBuilder.cs` (Tier 1) handles execution dispatch and OUTPUT clause handling. The partial files (all Tier 2) each contain one inner `MethodCallBuilder` subclass:
   - `` `MergeBuilder.Merge` `` / `` `MergeBuilder.MergeInto` `` — create the `SqlMergeStatement` and `MergeContext`. `Merge` takes `ITable<TTarget>` as the target; `MergeInto` takes the source queryable first, then the target, and wraps both in a `TableLikeQueryContext`.
   - `` `MergeBuilder.Using` `` / `` `MergeBuilder.UsingTarget` `` — attach a source queryable (or a clone of the target) as a `TableLikeQueryContext` on the `MergeContext`.
   - `` `MergeBuilder.On` `` — builds the `ON` match condition. Handles three overloads: `On(condition)`, `On(targetKey, sourceKey)`, and `OnTargetKey()` (PK-based auto-condition).
   - `` `MergeBuilder.InsertWhenNotMatched` `` — appends `MergeOperationType.Insert` operation; uses `UpdateBuilder.ParseSetter` + `InitializeSetExpressions` for explicit setters, or `BuildFullEntityExpression` for implicit all-columns insert.
   - `` `MergeBuilder.UpdateWhenMatched` `` — appends `MergeOperationType.Update`; similar setter handling; skips empty operation for implicit setters with no updatable non-key columns (issue #2843).
   - `` `MergeBuilder.UpdateWhenMatchedThenDelete` `` — appends `MergeOperationType.UpdateWithDelete` with a separate `WhereDelete` condition.
   - `` `MergeBuilder.DeleteWhenMatched` `` — appends `MergeOperationType.Delete`; sets `IsSourceOuter = true` for condition building.
   - `` `MergeBuilder.DeleteWhenNotMatchedBySource` `` / `` `MergeBuilder.UpdateWhenNotMatchedBySource` `` — "by source" variants that build against `TargetContext` rather than `SourceContext`.
   - `` `MergeBuilder.MergeContext` `` — the `SequenceContextBase` subclass that holds `SqlMergeStatement`, `TargetContext`, `SourceContext`, `Kind`, and optional `OutputContext`. `SetRunQuery` dispatches based on `MergeKind` — non-query for plain merge/output-into, mapper-based for output-with-result.
   - `` `MergeProjectionHelper` `` — helper for building the combined target+source projection expression during `MergeWithOutput`. Calls `Builder.BuildSqlExpression` with `ForceDefaultIfEmpty | ForSetProjection | ResetPrevious` flags and collects `SqlPlaceholderExpression` / `SqlEagerLoadExpression` fragments.
   - `` `TableLikeQueryContext` `` — dual-context holder used by merge source expressions. Owns `TargetContextRef`, `SourceContextRef`, a `SubQueryContext` over the source, and a `SqlTableLikeSource`. Provides `PrepareSourceBody`, `PrepareTargetSource`, `PrepareTargetLambda`, `PrepareSelfTargetLambda` helpers that substitute lambda parameters with the appropriate proxy expressions.
   - `` `TableLikeHelpers` `` — static utility for column-alias generation (`GenerateColumnAlias`) and field-list deduplication (`RegisterFieldMapping`) used by `EnumerableContext`, `TableLikeQueryContext`, and merge contexts.

9. **CTE subsystem.**
   - `` `CteBuilderImpl` `` — public implementation of `ICteBuilder` / `IAnnotatableBuilderInternal`. Captures `Name` and annotation bag set by fluent `.HasName()` / provider-extension `.SetAnnotation()` calls on the builder callback passed to `AsCte(b => ...)`.
   - `` `CteAnnotationsContainer` `` — immutable snapshot of the `CteBuilderImpl` state placed into the expression tree as a query-cache key. Implements `IExpressionCacheKey` / `IEquatable<CteAnnotationsContainer>` via a deterministic string `_cacheKey` built from name + sorted annotation key=value pairs.
   - `` `TableBuilder.CteTableContext` `` partial — builds `CteContext` (deferred) or looks up an existing one via `FindRegisteredCteContext`, then wraps it in a `CteTableContext`. Handles `AsCte(source)`, `AsCte(source, name)`, and `AsCteInternal(source, container)` call shapes.

10. **LoadWith / association subsystem.**
    - `` `AssociationHelper` `` — static factory for association query lambdas (`CreateAssociationQueryLambda`). Handles `HasQueryMethod()` associations (dynamic lambdas registered via `RegisterDynamicExpressionAccessor`), produces the `(ParentType p) => dc.GetTable<ObjectType>().Where(...)` pattern, and supports `DefaultIfEmpty` optional associations and `LoadWith` filter injection.
    - `` `LoadWithEntity` `` — tree node tracking one `LoadWith` configuration level. Holds a `List<LoadWithMember>` and a `Parent` pointer. Implements structural equality by recursively comparing `MembersToLoad`.
    - `` `LoadWithMember` `` — leaf node for a single eager-loaded member. Carries `MemberInfo`, optional `FilterExpression` / `FilterFunc`, and `ShouldLoad` flag. Equality based on `MemberInfo` only.
    - `` `ILoadWithContext` `` — interface added to `IBuildContext` implementations that support `LoadWith` root attachment (one property: `LoadWithRoot`).
    - `` `ITableContext` `` — extends `ILoadWithContext` with `ObjectType` and `SqlTable`; implemented by `TableContext`, `CteTableContext`, and `TableLikeQueryContext`-based targets.
    - `` `EagerLoading` `` — minimal static class with `GetEnumerableElementType(Type, MappingSchema)` that resolves `IGrouping<,>` and collection types to their element type. Used by `ExpressionBuilder.EagerLoad` partial.

11. **Proxy / build-proxy subsystem.**
    - `` `IBuildProxy` `` — interface for contexts that delegate to an `Owner` context. Three members: `Owner`, `InnerExpression`, `HandleTranslated(path, placeholder)`.
    - `` `BuildProxyBase<TOwner>` `` — generic abstract base for build proxies. Implements `IBuildContext.MakeExpression` by mapping member/`ContextRefExpression` paths through the inner `BuildContext`, then routing results to `HandleTranslated` (for `SqlPlaceholderExpression` leaves) or `ProcessTranslated` (for composite expressions including `SqlGenericConstructorExpression`, `SqlDefaultIfEmptyExpression`, `SqlAdjustTypeExpression`, `NewExpression`, `MethodCallExpression`). Contains a nested `BuildProxyVisitor` that recurses into `ContextRefExpression` nodes encountered during complex translation. Abstract factory `CreateProxy` allows subclasses to produce new instances of themselves.

12. **Entity construction / column mapping.**
    - `` `EntityConstructorBase` `` — abstract base shared by `TableContext` and other contexts that need to build full-entity `SqlGenericConstructorExpression` trees. `BuildGenericFromMembers` iterates `ColumnDescriptor` collections, respects `SkipOnInsert` / `SkipOnUpdate` / `SkipOnEntityFetch` flags, handles nested dotted member names, and filters to primary keys when `ProjectFlags.Keys` is set. Also handles the `FullEntityPurpose` enum (Default / Insert / Update).

13. **`SequenceHelper` utilities.** Static class with context-manipulation helpers used by every builder and context:
    - `PrepareBody` — replaces lambda parameters with `ContextRefExpression` nodes; handles `Convert` over `ContextRefExpression` by retaining type.
    - `IsSameContext` / `CreateRef` / `CorrectExpression` / `CorrectTrackingPath` / `ReplacePlaceholdersPathByTrackingPath` — path correction for context delegation chains.
    - `UnwrapProxy` — walks `IBuildProxy.Owner` until a non-proxy context is reached.
    - `FindError` — locates the first `SqlErrorExpression` in an expression tree.
    - `CreateSpecialProperty` — synthesizes a phantom property expression used by `EnumerableContextDynamic` for row-index tracking.

14. **Parameter binding (`ParametersContext`).** `ParametersContext` owns:
    - `CurrentSqlParameters` — the ordered `ParameterAccessor` list wired into the compiled delegate.
    - `BuildParameter` — the main SQL-parameter allocation entry point; handles `BuildParameterType.Bool` (wraps in `CASE WHEN`) and `BuildParameterType.InPredicate` (used for `IN` list parameters).
    - `RegisterDynamicExpressionAccessor` — delegates to `ExpressionCacheManager`; used by `AssociationHelper` to register association-query lambdas as dynamic cache discriminators.
    - `SimplifyConversion` — strips redundant double-convert / nullable `.Value` unwraps before parameter type resolution.

15. **Misc helpers.**
    - `` `ProjectFlagExtensions` `` — `[MethodImpl(AggressiveInlining)]` extension methods exposing each `ProjectFlags` bit as a named predicate (`IsRoot`, `IsSql`, `IsExpression`, `IsKeys`, etc.). Used pervasively in every `MakeExpression` override to avoid raw flag-mask arithmetic.
    - `` `ProjectionPathHelper` `` — walks a `SqlGenericConstructorExpression` tree applying a caller-supplied `TraverseProjection` delegate to each assignment/parameter path. Used during merge-output projection construction.
    - `` `EvaluationHelper` `` — static `EvaluateExpression(expression, dataContext, parameterValues)` that substitutes `SqlQueryRootExpression`/`DataContextParam`/array-index nodes and calls `.EvaluateExpression()`. Used to fold constant-ish sub-expressions at build time.
    - `` `BuildContextDebuggingHelper` `` — `#if DEBUG`-gated helpers: `GetContextInfo` emits `TypeName[ID](SourceID)` (including `SqlTable.SourceID` for `TableContext` and scope targets for `ScopeContext`); `GetPath` walks the `Parent` chain to produce a chain-of-context string. Not called in release builds.

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
- `MergeBuilder.UpdateWhenMatched` silently skips the entire Update operation when no updatable non-key columns are found with an implicit setter (workaround for issue #2843); this may surprise callers who expect an error when source and target have disjoint column sets.
- `TableLikeQueryContext`'s `ProjectionHelper<,>` helper type uses a `selft_target` property name (typo for `self_target`) — a cosmetic issue but visible in reflection-based diagnostics.
- `ExpressionBuildVisitor` carries both `_translationCache` and `_columnCache` as mutable `SnapshotDictionary` fields; the `Clone(CloningContext)` path copies only entries whose keys reference cloned contexts, which means stale entries from the parent build can leak into sub-build visitors if the cloning context is not exhaustive.
- `CteContext.InitQuery()` uses a `_isRecursiveCall` flag to guard against re-entrant initialization but does not hold a lock — concurrent compilation of the same CTE from multiple threads (if `ExpressionBuilder` were ever shared) would produce undefined behavior. Currently safe because `ExpressionBuilder` is not shared, but the guard is fragile.

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
- Merge pipeline entry: `MergeBuilder.Merge.BuildMethodCall` → `MergeBuilder.MergeContext`.
- CTE entry: `TableBuilder.CteTableContext.cs` partial → `CteContext.InitQuery()`.
- Parameter binding: `ParametersContext.BuildParameter` / `CurrentSqlParameters`.
- Expression normalization: `ExposeExpressionVisitor.ExposeExpression` (called by `ExpressionBuilder.ExposeExpression`).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 63 / 63
- Tier 2 (visited / total): 71 / 71

Read (prior run): ISequenceBuilder.cs, IBuildContext.cs, BuildContextBase.cs, SequenceContextBase.cs, BuildInfo.cs, BuildSequenceResult.cs, Attributes.cs, BuildFlags.cs, BuildPurpose.cs, ProjectFlags.cs, plus structural anchors SubQueryContext/SelectContext/EnumerableContext partial, plus Source/CodeGenerators/BuildersGenerator.cs.

Read (this run): all 56 files from the deferred queue — see body for per-file characterization.

- Tier 3 (skipped, logged): 0
</details>
