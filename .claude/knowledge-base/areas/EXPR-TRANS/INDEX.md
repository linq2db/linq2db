---
area: EXPR-TRANS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 63/63
coverage_tier_2: 71/71
---

# EXPR-TRANS -- LINQ expression -> SQL AST translation

The pipeline that consumes a `System.Linq.Expressions.Expression` (a LINQ method-call tree on `IQueryable<T>` / `ITable<T>`) and produces an `IBuildContext` graph whose `SelectQuery` / `SqlStatement` is then handed to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) for optimization and emission. Every translated LINQ operator (`Where`, `Select`, `OrderBy`, `Join`, `GroupBy`, `Insert`, `Update`, `Delete`, `Merge`, `Concat`, `Distinct`, `Take`/`Skip`, `Cast`, `OfType`, `AsCte`, `FromSql`, `AsQueryable`, ...) has a dedicated `*Builder` class; the central `ExpressionBuilder` + a Roslyn-generated dispatcher pick the right one per node.

## Key types

- `` `ExpressionBuilder` `` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:26`) -- the per-query orchestrator. Holds the `Query`, `MappingSchema`, `DataOptions`, `ParametersContext`, `ExpressionTreeOptimizationContext`, the `_buildVisitor`, the `IMemberTranslator`, and pools for `SelectQuery` / `ParentInfo` / `PlaceholderCollectVisitor`. Public entry: `Build<T>(ref IQueryExpressions)` (line 124), called by [`LINQ`](../LINQ/INDEX.md)'s query setup. Sequence dispatch entry: `TryFindBuilder` -> `FindBuilderImpl` (line 30, 38) -- `FindBuilderImpl` is a `partial` method whose body is generated.
- `` `ISequenceBuilder` `` -- the strategy interface every builder implements. Two methods: `BuildSequence` (produce `BuildSequenceResult`) and `IsSequence` (predicate).
- `` `MethodCallBuilder` `` -- abstract base for the common case "this LINQ node is a `MethodCallExpression`"; subclasses override `BuildMethodCall(builder, methodCall, buildInfo)`. Default `IsSequence` recurses into `Arguments[0]` when `IQueryable`.
- `` `IBuildContext` `` -- the per-stage context produced by a builder. Carries `Builder`, `MappingSchema`, `Expression`, `SelectQuery`, `Parent`, `TranslationModifier`, `ElementType`. Key method `MakeExpression(path, ProjectFlags)` is the projection-resolution hook used during materialization-mapper construction.
- `` `BuildContextBase` `` / `` `SequenceContextBase` `` -- abstract base classes; `SequenceContextBase` adds a `Sequences[]` array and `Body` lambda, used by builders that wrap a child sequence.
- `` `BuildInfo` `` -- the per-call invocation record. Mutable bag carrying `Parent`, `Expression`, `SelectQuery`, `CreateSubQuery`, `IsAssociation`, `JoinType`, `IgnoreOrderBy`, `IsAggregation`, `SourceCardinality`. Cloned + retargeted as recursion descends through nested method calls.
- `` `BuildSequenceResult` `` -- return value of `BuildSequence`. Encodes three states via factories: `FromContext(IBuildContext)`, `Error(Expression)`, `NotSupported()`.
- `` `BuildsAnyAttribute` `` / `` `BuildsExpressionAttribute` `` / `` `BuildsMethodCallAttribute` `` -- attribute markers consumed by the source generator.
- `` `ProjectFlags` `` / `` `BuildPurpose` `` / `` `BuildFlags` `` -- bitmasks that drive the `MakeExpression` / `BuildExpression` path.
- `` `ContextRefExpression` `` (in [`EXPR`](../EXPR/INDEX.md), `LinqToDB.Internal.Expressions`) -- the expression-tree node that wraps an `IBuildContext` once a sequence root has been resolved.
- `` `TranslationModifier` `` -- immutable value type propagated through every `IBuildContext`. Carries `InlineParameters` and `IgnoreQueryFilters`.
- `` `ParametersContext` `` -- manages `SqlParameter` allocation and the `ExpressionCacheManager` for dynamic expression accessors.
- `` `IAsQueryableBuilder<T>` `` (`Source/LinqToDB/Linq/IAsQueryableBuilder.cs`) -- **new public interface (PR #5495)**. Marker-only; methods (`Parameterize()`, `Inline()`) are never called at runtime -- they are captured as expression nodes by the 3-arg `LinqExtensions.AsQueryable` overload and interpreted at query-build time by `EnumerableBuilder.TryParseConfigure`.
- `` `IAsQueryableExceptBuilder<T>` `` (`Source/LinqToDB/Linq/IAsQueryableExceptBuilder.cs`) -- second stage of the same configuration chain. Exposes `Except(params Expression<Func<T, object?>>[] members)` to flip the default per-column mode for specific members.
- `` `EnumerableParameterizationConfig` `` (`Source/LinqToDB/Internal/Linq/Builder/EnumerableParameterizationConfig.cs`) -- sealed internal class produced by `EnumerableBuilder.BuildConfigured`. Carries `DefaultForceParameter` (bool), `Parameter` (shared `ParameterExpression` root), and `Excepted` (list of `MemberExpression` paths). `ShouldForceParameter(Expression)` re-roots the access expression onto `Parameter` and checks it against the `Excepted` list; returns `!DefaultForceParameter` on a match, `DefaultForceParameter` otherwise.

## Files (Tier 1 / Tier 2)

### Tier 1 -- pinned (63 files)

`ExpressionBuilder.cs` plus every `*Builder.cs` at the folder root.

### Tier 2 -- implementation / contexts / helpers (71 files)

Examples: `BuildInfo.cs`, `BuildSequenceResult.cs`, `BuildContextBase.cs`, `SequenceContextBase.cs`, `IBuildContext.cs`, `ISequenceBuilder.cs`, `Attributes.cs`, `BuildFlags.cs`, `BuildPurpose.cs`, `ProjectFlags.cs`, `ProjectFlagExtensions.cs`, `ProjectionPathHelper.cs`, `SubQueryContext.cs`, `SelectContext.cs`, `AnchorContext.cs`, `AsSubqueryContext.cs`, `EagerContext.cs`, `EagerLoading.cs`, `ScopeContext.cs`, `PassThroughContext.cs`, `SingleExpressionContext.cs`, `EnumerableContext.cs`, `EnumerableContextDynamic.cs`, `CteContext.cs`, `CteTableContext.cs`, `CteAnnotationsContainer.cs`, `CteBuilderImpl.cs`, `TableLikeQueryContext.cs`, `TableLikeHelpers.cs`, `TableBuilder.TableContext.cs`, `TableBuilder.RawSqlContext.cs`, `TableBuilder.CteTableContext.cs`, `MergeBuilder.*.cs` partials (12), `MergeProjectionHelper.cs`, `AssociationHelper.cs`, `LoadWithEntity.cs`, `LoadWithMember.cs`, `IAnnotatableBuilderInternal.cs`, `IBuildProxy.cs`, `BuildProxyBase{TOwner}.cs`, `ILoadWithContext.cs`, `ITableContext.cs`, `EntityConstructorBase.cs`, `RecordReaderBuilder.cs` helpers, `SequenceHelper.cs`, `EvaluationHelper.cs`, `LambdaResolveVisitor.cs`, `ExpressionBuildVisitor.cs`, `BinaryExpressionAggregatorVisitor.cs`, `ExpressionTreeOptimizerVisitor.cs`, `ExpressionTreeOptimizationContext.cs`, `ExpressionTestGenerator.cs`, `ParametersContext.cs`, `TranslationModifier.cs`, `BuildContextDebuggingHelper.cs`, plus the two files under `Visitors/` (`ExposeExpressionVisitor.cs`, `CanBeEvaluatedOnClientCheckVisitorBase.cs`).

New files added by PR #5495 (now Tier 2): `EnumerableParameterizationConfig.cs`; `IAsQueryableBuilder.cs` and `IAsQueryableExceptBuilder.cs` live in `Source/LinqToDB/Linq/` (public surface, Tier 2 for this area).

## Subsystems

1. **Dispatcher (source-generated).** `ExpressionBuilder.FindBuilderImpl` is generated by `BuildersGenerator` (`Source/CodeGenerators/BuildersGenerator.cs:14`) from `[BuildsExpression]`, `[BuildsMethodCall]`, `[BuildsAny]` markers on each `*Builder` class. The generator emits a single `switch (expr.NodeType)` over `ExpressionType`, with a nested `switch (call.Method.Name)` for the `Call` case, and finally falls through to the `[BuildsAny]` set. Each candidate calls a static `CanBuild` / `CanBuildMethod` predicate; the first that returns `true` wins, and the dispatcher returns the singleton instance from `Builder<T>.Instance`. Adding a new operator = drop a `[BuildsMethodCall("Foo")]`-decorated `FooBuilder.cs`; the generator picks it up next build.

2. **`BuildSequence` recursion.** `ExpressionBuilder.TryBuildSequence(BuildInfo)` is the recursive workhorse: it `ExpandToRoot`s the expression (resolves macros / associations), runs `TryFindBuilder`, asks the chosen builder for a context, registers the originating expression on the context (`RegisterSequenceExpression`), and returns. Most builders themselves call back into `builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]))` to translate the source sequence.

3. **Context graph.** Each builder produces an `IBuildContext`. Composite operators (`Where`, `Select`, `OrderBy`, joins) wrap the child context -- typically in a `SubQueryContext` when the parent already has `Distinct` / `Take` / `Skip` / projection. Joins create `outer`/`inner` `SubQueryContext` pairs.

   Context specializations include `SubQueryContext`, `AsSubqueryContext`, `SelectContext`, `ScopeContext`, `PassThroughContext`, `SingleExpressionContext`, `AnchorContext`, `EagerContext`, `EnumerableContext`, `EnumerableContextDynamic`, `CteContext`, `CteTableContext`, `TableBuilder.TableContext`, `TableBuilder.RawSqlContext`, `TableBuilder.CteTableContext`.

   - `EnumerableContext` -- context backed by `SqlValuesTable`. Maps LINQ-side member paths to `SqlField` entries in the `VALUES(...)` table. As of PR #5495 the constructor accepts an optional `EnumerableParameterizationConfig? parameterization` parameter; when non-null, `BuildValueGetter` calls `_parameterization.ShouldForceParameter(me)` to choose between emitting `new SqlParameter(...)` and `new SqlValue(...)` per column -- columns whose mapping returns `DataParameter` are always parameters regardless of this config (`EnumerableContext.cs:233-235`).

4. **`SelectQuery` plumbing.** Every builder mutates the running `SelectQuery` (from `BuildInfo.SelectQuery`) -- adding `Where`/`Having` clauses, columns, `OrderBy`, joins. The `SelectQuery` instance is allocated from `ExpressionBuilder.QueryPool` at the entry point and threaded through `BuildInfo`. The final `SqlStatement` is constructed by terminal builders (`InsertBuilder`, `DeleteBuilder`, `UpdateBuilder`, `MergeBuilder`, etc.).

5. **Projection / materialization.** After `BuildSequence` returns the root context, `ExpressionBuilder.BuildQuery<T>` calls `_buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), buildPurpose: BuildPurpose.Expression)` to produce the materialization mapper. This walks the context graph via `IBuildContext.MakeExpression(path, ProjectFlags)`.

   `ExpressionBuilder.Project` (in `ExpressionBuilder.SqlBuilder.cs`) handles `Convert(body)` wrapping: when `path` is a `Convert` / `ConvertChecked` node whose operand is a `ContextRefExpression`, `Project` re-types the `ContextRefExpression` to the conversion's target type and recurses -- this is the path that resolves interface-member accesses on concrete types during projection (`ExpressionBuilder.SqlBuilder.cs:1808-1815`).

6. **Method-chain extension hook (`MethodChainBuilder`).** Aggregate / window-function calls registered via `Sql.ExtensionAttribute` are dispatched to `MethodChainBuilder` (`[BuildsExpression(ExpressionType.Call)]`) -- *not* a per-method `*Builder`.

7. **Finalization + handoff to SQL-PROVIDER.** `ExpressionBuilder.BuildQuery<T>` calls `query.SqlOptimizer.Finalize(...)` per query info, then `SqlProviderHelper.IsValidQuery(...)`; failure here surfaces as a `SqlErrorExpression`.

8. **MergeBuilder partials.** `MergeBuilder` is a `partial class` spread across 10+ files. The top-level `MergeBuilder.cs` (Tier 1) handles execution dispatch and OUTPUT clause handling. Per-operation partials: `Merge`, `MergeInto`, `Using`, `UsingTarget`, `On`, `InsertWhenNotMatched`, `UpdateWhenMatched`, `UpdateWhenMatchedThenDelete`, `DeleteWhenMatched`, `DeleteWhenNotMatchedBySource`, `UpdateWhenNotMatchedBySource`, `MergeContext`, `MergeProjectionHelper`, `TableLikeQueryContext`, `TableLikeHelpers`.

9. **CTE subsystem.**
   - `CteBuilderImpl` -- public implementation of `ICteBuilder` / `IAnnotatableBuilderInternal`. Captures `Name` and annotation bag set by fluent `.HasName()` / provider-extension `.SetAnnotation()` calls.
   - `CteAnnotationsContainer` -- immutable snapshot of the `CteBuilderImpl` state placed into the expression tree as a query-cache key.
   - `TableBuilder.CteTableContext` partial -- builds `CteContext` (deferred) or looks up an existing one via `FindRegisteredCteContext`, then wraps it in a `CteTableContext`.

10. **LoadWith / association subsystem.**
    - `AssociationHelper` -- static factory for association query lambdas (`CreateAssociationQueryLambda`).
    - `LoadWithEntity` / `LoadWithMember` -- tree nodes tracking `LoadWith` configuration levels.
    - `ILoadWithContext` / `ITableContext` -- interfaces added to `IBuildContext` implementations that support `LoadWith` root attachment.
    - `EagerLoading` -- minimal static class with `GetEnumerableElementType(Type, MappingSchema)`.

    **Association via realized concrete type (PR #5511 / issue #5510):** `IsAssociationInRealization` (`ExpressionBuilder.Associations.cs:49`) has a new third branch (lines 73-93): when `member.ReflectedType != expression.Type`, it looks up the member on `expression.Type` (the runtime concrete type), then checks `MappingSchema.GetAttribute<AssociationAttribute>(expression.Type, newMember)` directly. The comment at lines 78-83 explains the motivation: `newMember.DeclaringType` may be an abstract base or interface not registered in the metadata reader (e.g. EF Core `IEntityTypeConfiguration`-based models), so the attribute lookup must use the realized `expression.Type` as the owner type. `AssociationToRealization` (`ExpressionBuilder.Associations.cs:19`) has a parallel fix: when a `MemberExpression` is on a `ContextRefExpression` whose `.Type` is an interface but `BuildContext.ElementType` is a concrete subtype, the member is re-looked-up on `elementType` and the `ContextRefExpression` is re-typed before returning.

11. **Proxy / build-proxy subsystem.**
    - `IBuildProxy` / `BuildProxyBase<TOwner>` -- interface and generic abstract base for build proxies. `BuildProxyBase` implements `IBuildContext.MakeExpression` by mapping member/`ContextRefExpression` paths through the inner `BuildContext`, then routing results to `HandleTranslated` (for `SqlPlaceholderExpression` leaves) or `ProcessTranslated` (for composite expressions).

12. **Entity construction / column mapping.**
    - `EntityConstructorBase` -- abstract base shared by `TableContext` and other contexts that need to build full-entity `SqlGenericConstructorExpression` trees. `BuildGenericFromMembers` iterates `ColumnDescriptor` collections, respects `SkipOnInsert` / `SkipOnUpdate` / `SkipOnEntityFetch` flags, handles nested dotted member names.

13. **`SequenceHelper` utilities.** Static class with context-manipulation helpers: `PrepareBody`, `IsSameContext` / `CreateRef` / `CorrectExpression` / `CorrectTrackingPath`, `UnwrapProxy`, `FindError`, `CreateSpecialProperty`.

14. **Parameter binding (`ParametersContext`).** Owns `CurrentSqlParameters` (the ordered `ParameterAccessor` list wired into the compiled delegate), `BuildParameter` (the main SQL-parameter allocation entry point), `RegisterDynamicExpressionAccessor`, `SimplifyConversion`.

15. **AsQueryable parameterization API (PR #5495).** New public API in `LinqToDB.Linq` namespace:
    - `LinqExtensions.AsQueryable<TElement>(IEnumerable<TElement> source, IDataContext dataContext, Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>> configure)` (`Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs:41`) -- the 3-arg overload. Wraps `source` in `Expression.Constant`, the context in `SqlQueryRootExpression.Create`, and `configure` in `Expression.Quote`; normal `ExpressionQueryImpl<T>` dispatch follows.
    - The `[BuildsMethodCall(nameof(LinqExtensions.AsQueryable))]` on `EnumerableBuilder` is already present; the dispatcher additionally requires `CanBuildMethod` to match `Methods.LinqToDB.AsQueryableConfigured` (registered in `Methods.cs:205` as the 3-arg overload via `MemberHelper.MethodOfGeneric`).
    - `EnumerableBuilder.BuildConfigured` (lines 103-141): validates the source can be evaluated on the client (rejects outer-query-referencing arrays with a clear error message), unwraps the configure lambda, calls `TryParseConfigure` to walk the method-call chain, allocates the `SqlParameter` via `BuildParameter(..., InPredicate)`, constructs `EnumerableParameterizationConfig`, and creates `EnumerableContext` with that config.
    - `EnumerableBuilder.TryParseConfigure` (lines 143-236): walks a `MethodCallExpression` chain on `configureLambda.Body`. Recognized calls: `Parameterize()` sets `defaultForceParameter = true`; `Inline()` sets it `false`; `Except(nae)` extracts each selector lambda, substitutes the shared `rowParameter`, strips boxing `Convert`, and appends to `exceptedList`. Any unrecognized method name or a non-`NewArrayExpression` `Except` argument returns `false` with a descriptive error.

16. **Misc helpers.**
    - `ProjectFlagExtensions` -- `[MethodImpl(AggressiveInlining)]` extension methods exposing each `ProjectFlags` bit as a named predicate.
    - `ProjectionPathHelper` -- walks a `SqlGenericConstructorExpression` tree applying a caller-supplied `TraverseProjection` delegate.
    - `EvaluationHelper` -- static `EvaluateExpression(expression, dataContext, parameterValues)`.
    - `BuildContextDebuggingHelper` -- `#if DEBUG`-gated helpers: `GetContextInfo`, `GetPath`.

## Interactions

- **Driven by [`LINQ`](../LINQ/INDEX.md):** `Query<T>.GetQuery` / `ExpressionQuery` create an `ExpressionBuilder` and call `Build<T>` once per cache miss.
- **Consumes [`SQL-AST`](../SQL-AST/INDEX.md):** every builder mutates / constructs `SelectQuery`, `SqlField`, `SqlBinaryExpression`, `SqlPlaceholderExpression`, `SqlInsertStatement`, etc.
- **Hands off to [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md):** finalized `SqlStatement` flows into `ISqlOptimizer.Finalize` then `ISqlBuilder` for emission.
- **Uses [`MAPPING`](../MAPPING/INDEX.md):** `EntityDescriptor` lookups for query filters, column mapping for materialization, association definitions.
- **Uses [`EXPR`](../EXPR/INDEX.md):** `ContextRefExpression`, `SqlPlaceholderExpression`, `SqlGenericConstructorExpression`, etc. live in `LinqToDB.Internal.Expressions`.
- **Uses [`INFRA`](../INFRA/INDEX.md):** `ObjectPool`, `ActivityService` metrics, common helpers.
- **Generated by [`CODEGEN`](../CODEGEN/INDEX.md):** `BuildersGenerator` emits `ExpressionBuilder.g.cs` containing `FindBuilderImpl`.
- **Cross-link [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md):** `Enum.HasFlag` translation -- `ProviderMemberTranslatorDefault.ProcessHasFlag` (a SQL-PROVIDER / INTERNAL-API concern) was added in the same delta batch as the AsQueryable parameterization API; `Source/LinqToDB/Linq/Expressions.cs` was touched in that PR for member-mapping registration of `HasFlag`.

## Inbound / outbound dependencies

- **Inbound:** `LINQ` (`Internal/Linq/Query.cs`, `Internal/Linq/ExpressionQuery.cs`) constructs `ExpressionBuilder`. `EXPR` types are referenced inbound via `ContextRefExpression.BuildContext`.
- **Outbound:** `LinqToDB.Internal.SqlQuery.*` (heavy), `LinqToDB.Internal.Expressions.*`, `LinqToDB.Internal.Conversion`, `LinqToDB.Internal.DataProvider.Translation` (`IMemberTranslator`), `LinqToDB.Mapping`, `LinqToDB.Metrics`, `LinqToDB.Internal.Reflection.Methods` (well-known method handles).

## Known issues / debt

- Source-generator dispatcher relies on every builder declaring its triggering attributes correctly; a missing attribute is a silent miss (the call falls through to `null` and surfaces as `BuildSequenceResult.NotSupported`).
- Many `*Builder.BuildMethodCall` methods carry `[BuildsMethodCall]` lists that include legacy method names alongside current ones (`OrderBy` + `ThenOrBy`, `Insert` + `InsertWithOutput*`); changes here can affect provider compatibility.
- `IBuildContext` has a `// TODO: probably not needed` on `Parent.set`.
- `BuildInfo` is a mutable bag with `set;` accessors; its `IsSubQuery` is derived from `Parent != null` rather than tracked explicitly.
- `MethodCallParser.cs` referenced in `kb-areas.md` does not exist -- the area registry should be updated to remove it.
- `MergeBuilder.UpdateWhenMatched` silently skips the entire Update operation when no updatable non-key columns are found with an implicit setter (workaround for issue #2843).
- `TableLikeQueryContext`'s `ProjectionHelper<,>` helper type uses a `selft_target` property name (typo for `self_target`) -- a cosmetic issue but visible in reflection-based diagnostics.
- `ExpressionBuildVisitor` carries both `_translationCache` and `_columnCache` as mutable `SnapshotDictionary` fields; the `Clone(CloningContext)` path copies only entries whose keys reference cloned contexts.
- `CteContext.InitQuery()` uses a `_isRecursiveCall` flag to guard against re-entrant initialization but does not hold a lock -- concurrent compilation of the same CTE from multiple threads would produce undefined behavior. Currently safe because `ExpressionBuilder` is not shared.
- `EnumerableBuilder.BuildConfigured` rejects sources that reference outer query state with a runtime error at query-build time rather than a compile-time diagnostic.
- `IAsQueryableBuilder<T>` and `IAsQueryableExceptBuilder<T>` methods are marker-only; calling them outside an expression context is undefined behaviour and the implementations are not provided (the interfaces have no concrete class backing them in the shipped assembly).

## See also

- [`SQL-AST`](../SQL-AST/INDEX.md) -- output target of every builder.
- [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md) -- receives the finalized `SqlStatement`.
- [`LINQ`](../LINQ/INDEX.md) -- caller; also owns query caching.
- [`EXPR`](../EXPR/INDEX.md) -- `ContextRefExpression`, `SqlPlaceholderExpression`, visitor infrastructure.
- [`CODEGEN`](../CODEGEN/INDEX.md) -- `BuildersGenerator` Roslyn source generator.
- [`../../architecture/expression-translator.md`](../../architecture/expression-translator.md) -- narrative walkthrough.
- `.claude/docs/code-design.md` -- public-API + AST namespace invariants.

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

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 63 / 63
- Tier 2 (visited / total): 71 / 71

Read (this run -- delta sha 4a478ff14):
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

- Tier 3 (skipped, logged): 0
</details>
