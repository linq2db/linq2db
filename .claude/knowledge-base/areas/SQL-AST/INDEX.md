---
area: SQL-AST
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 5/5
coverage_tier_2: 133/143
---

# SQL-AST

Internal abstract syntax tree representing every SQL query linq2db can build. Lives almost entirely in the `LinqToDB.Internal.SqlQuery` namespace under `Source/LinqToDB/Internal/SqlQuery/`. The AST is the *contract* between three subsystems: the [expression translator](../EXPR-TRANS/INDEX.md) (producer), the [SQL provider](../SQL-PROVIDER/INDEX.md) (consumer that emits dialect text), and the [query runner](../LINQ/INDEX.md) (which materialises results).

Every node implements `IQueryElement` (`Source/LinqToDB/Internal/SqlQuery/IQueryElement.cs:8`). Concrete nodes derive from `QueryElement` (`Source/LinqToDB/Internal/SqlQuery/QueryElement.cs:11`) or, when they participate as expressions, from `SqlExpressionBase` which adds `ISqlExpression` semantics (`Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs:5`). The single dispatch entry is `IQueryElement.Accept(QueryElementVisitor)`, which every node implements as one line -- `visitor.Visit<TypeName>(this)` (e.g. `Source/LinqToDB/Internal/SqlQuery/SqlField.cs:186`).

## Key types

- **`IQueryElement`** (`IQueryElement.cs:8`) -- root interface; exposes `ElementType` (a tagged `QueryElementType` enum), debug `ToString(QueryElementTextWriter)`, `Accept(QueryElementVisitor)` and `GetElementHashCode()`. The hash is *structural*, not identity-based.
- **`QueryElementType`** (`QueryElementType.cs:6`) -- tagged-union discriminator. Every concrete node returns its constant from `ElementType`.
- **`ISqlExpression`** -- the value-producing subset; adds `Precedence`, `SystemType`, `CanBeNullable(NullabilityContext)`, and a `comparer`-driven `Equals` overload.
- **`ISqlPredicate`** -- the boolean subset; adds `CanInvert` / `Invert(NullabilityContext)` and `CanBeUnknown` to express SQL three-valued logic.
- **`ISqlTableSource`** -- anything usable in `FROM` / join. Carries an integer `SourceID` (allocated from `SelectQuery.SourceIDCounter`). Implementations: `SqlTable`, `SelectQuery`, `SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`. There's a `// TODO: [sdanyliv] ISqlTableSource why it extends ISqlExpression?` at `ISqlTableSource.cs:5`.
- **`SqlStatement`** -- abstract root of any executable statement; exposes `QueryType` and the optional carried `SelectQuery`. Concrete `Sql<Verb>Statement` types extend either `SqlStatement` directly or `SqlStatementWithQueryBase`.
- **`SelectQuery`** -- the central composite node, aggregates the six clauses (`Select`/`From`/`Where`/`GroupBy`/`Having`/`OrderBy`) plus optional `SetOperators` and `UniqueKeys`. Implements `ISqlTableSource`.
- **`SqlField`** -- column reference. Constructed either bare or from a `ColumnDescriptor`. `SqlField.All(table)` produces the `*` placeholder.
- **Expression nodes** -- value-producing nodes that derive from `SqlExpressionBase`:
  - `SqlValue` (literal), `SqlParameter` (parameterised value with `IsQueryParameter`/`AccessorId`; `NeedsCast` triggers `$Cast$()` wrapper; `ValueConverter` chain supports `Take` offset injection),
  - `SqlFunction` / `SqlExpression` (both extend the shared `SqlParameterizedExpressionBase`),
  - `SqlFragment` -- an untyped format-string fragment (`SystemType = null`).
  - `SqlBinaryExpression`, `SqlUnaryExpression`,
  - `SqlCaseExpression`, `SqlConditionExpression` ($IIF$), `SqlCoalesceExpression`, `SqlCompareToExpression`, `SqlCastExpression`,
  - `SqlNullabilityExpression`, `SqlAnchor` (table-source / inserted / deleted markers used during merge/output translation), `SqlRowExpression`,
  - `SqlAliasPlaceholder` -- singleton (`SqlAliasPlaceholder.Instance`), renders as `%ts%`.
  - `SqlObjectExpression` -- bundles a `SqlGetValue[]` array.
  - `SqlGroupingSet` -- an `ISqlExpression` node for a single `()` set within `GROUP BY GROUPING SETS`.
  - `SqlInlinedBase` and its derivatives `SqlInlinedSqlExpression` / `SqlInlinedToSqlExpression`.
  - `SqlExtendedFunction` (`Source/LinqToDB/SqlQuery/SqlExtendedFunction.cs:13`) -- the window/analytic function node.
  - `SqlQueryExtension` -- provider extension node embedded in a query.
- **Predicate nodes** -- boolean nodes nested inside `SqlSearchCondition`. All defined as nested classes of `SqlPredicate`: `Not`, `TruePredicate`, `FalsePredicate`, `Expr`, `BaseNotExpr`, `ExprExpr`, `Like`, `SearchString`, `Between`, `IsNull`, `IsDistinct`, `IsTrue`, `InSubQuery`, `InList`, `Exists`, plus the `Operator` enum.
  - `ExprExpr.Reduce(NullabilityContext, EvaluationContext, bool isInsidePredicate, LinqOptions)` is where `CompareNulls.LikeClr` null-comparison rewrites are applied.
  - `SqlSearchCondition` -- the `AND`/`OR`-of-predicates container; `IsOr` toggles the connective, `CanReturnUnknown` carries the three-valued-logic flag.
- **Clause nodes** -- `SqlSelectClause`, `SqlFromClause`, `SqlWhereClause`, `SqlGroupByClause`, `SqlHavingClause`, `SqlOrderByClause`, `SqlInsertClause`, `SqlUpdateClause`, `SqlOutputClause`, `SqlConditionalInsertClause`, `SqlMergeOperationClause`. All extend `ClauseBase` which holds a back-pointer to the owning `SelectQuery`.
- **Table-shaped nodes** -- `SqlTable` (entity-mapped), `SqlCteTable` (CTE reference), `SqlRawSqlTable`, `SqlValuesTable` (`VALUES`-list), `SqlTableSource` (positional wrapper holding alias + joins), `SqlJoinedTable`, `SqlTableLikeSource` (used as MERGE/MultiInsert source).
- **SET clause helper** -- `SqlSetExpression` -- a `Column`/`Expression` pair used in UPDATE `SET` and `OUTPUT ... INTO`. Constructor calls `RefineDbParameter` to propagate column-descriptor type metadata.
- **SET operator** -- `SqlSetOperator` -- wraps a `SelectQuery` + `SetOperation` enum.
- **`SqlTableType`** enum -- `Table`, `SystemTable`, `Function`, `Expression`, `Cte`, `RawSql`, `MergeSource`, `Values`.
- **CTE infrastructure** -- `CteClause`, `SqlWithClause`, `SqlCteTable`. `CteClause` carries an open-ended `Annotations` bag and a static `CteIDCounter` separate from `SourceID`.
- **Visitor base classes** -- `QueryElementVisitor` is the abstract dispatcher with one `VisitX` method per node type. Subclasses:
  - `SqlQueryVisitor` -- adds replacement tracking via `IVisitorTransformationInfo`; `GetVisitMode` promotes already-replaced nodes from `Transform` -> `Modify`.
  - `SqlQueryCloneVisitorBase` / `SqlQueryCloneVisitor` -- deep clone with optional predicate filter.
  - `SqlQueryConvertVisitorBase` / `SqlQueryConvertVisitor<TContext>` -- tree rewriting; optionally maintains a `Stack<IQueryElement>` for parent access via `WithStack`/`ParentElement`. **`ParentElement`** returns `Stack[^2]` (the true parent, not `Stack[^1]` which is self) -- fixed in PR #5451 (DuckDB). The `Convert<T>(element, convertAction, withStack: true)` overload on `QueryVisitorExtensions` also previously threw `NotImplementedException`; that guard was removed in the same PR so stack-enabled convert is now fully operational.
  - `SqlQueryActionVisitor` / `SqlQueryActionVisitor<TContext>` -- bottom-up read-only walk.
  - `SqlQueryFindVisitor` / `SqlQueryFindVisitor<TContext>` -- first-match search.
  - `SqlQueryFindExceptVisitor<TContext>` -- first-match search excluding a specified subtree.
  - `SqlQueryParentFirstVisitor` -- top-down walk.
  - `QueryElementReplacingVisitor` -- `Modify`-mode replacement using an `IDictionary<IQueryElement,IQueryElement>` map.
  - `SelectQueryOptimizerVisitor` -- the pooled, stateful optimizer the SQL builder runs before emission. Embeds six sub-visitors as fields.
  - `SqlQueryColumnNestingCorrector` -- builds a private `QueryNesting` tree tracking query-containment depth.
  - `SqlQueryColumnOptimizerVisitor` -- two-pass: pass 1 collects used `SqlColumn` references, pass 2 removes unused columns.
  - `SqlQueryColumnUsageCollector` -- propagates column usage across `SetOperators` and CTE field-index boundaries.
  - `SqlQueryOrderByOptimizer` -- consults `SqlProviderFlags`; pulls ORDER BY items from inner queries to outer when provider flag forbids inner ORDER BY.
  - `ReduceIsNullExpressionVisitor` -- simplifies `IS NULL` predicates over compound expressions.
  - `SqlQueryValidatorVisitor` -- validates structural constraints.
- **`VisitMode`** -- `ReadOnly | Modify | Transform`. Each `VisitX` method has three branches.
- **`NullabilityContext`** -- query-scoped cache that decides whether a given `ISqlExpression` can produce NULL.
- **`EvaluationContext`** -- caches client-side and server-side evaluation results for `IQueryElement` subtrees.
- **`QueryHelper`** -- static facade for tree analysis and transformation: `IsDependsOnSource`, `EnumerateAccessibleSources`, `IsAggregationQuery`, `WrapQuery` (in `QueryHelper.WrapQuery.cs`), `TryEvaluateExpression` (in `QueryHelper.Evaluate.cs`), `GetDbDataType` / `SuggestDbDataType` / `GetColumnDescriptor`, `CollectUniqueKeys`, `CalcCanBeNull`. Exposes pooled `SelectQueryOptimizerVisitor` / `AggregationCheckVisitor` instances.
- **`QueryVisitorExtensions`** -- the public extension-method surface for visiting/finding/cloning AST nodes. All operations go through pooled visitors. Key methods: `Visit` / `VisitAll`, `VisitParentFirst`, `Find` / `FindExcept`, `Clone`, `Replace`, `Convert` / `ConvertAll`. Two pools exist for `SqlQueryConvertVisitor<TContext>`: `ConvertPool` (immutable/Transform mode) and `ConvertMutationPool` (`allowMutation: true` / Modify mode). PR #5451 removed the `NotImplementedException` guard on the `Convert<T>(element, convertAction, withStack: true)` overload, making stack-enabled conversion fully operational.
- **`PseudoFunctions`** -- well-known function-name constants like `$ToLower$`, `$Convert_Format$`, `$merge_action$`.
- **`QueryElementTextWriter`** -- debug renderer threaded through every node's `ToString(writer)`.
- **`SqlBinaryExpressionHelper`** -- compile-time static lookup table for C# numeric binary-operator result types for `+` and `-`.
- **`SqlFlags`** -- `[Flags]` enum: `IsAggregate=0x1`, `IsPure=0x4`, `IsPredicate=0x8`, `IsWindowFunction=0x10`.
- **`Precedence`** (legacy public) -- int constants ranking SQL operator precedence.
- **`SqlObjectName`** (public) -- `readonly record struct` with `Name`, `Server`, `Database`, `Schema`, `Package`.
- **`SqlDataType`** (public) -- `SqlExpressionBase` wrapping a `DbDataType`.
- **`SqlFunctionArgument`** / **`SqlWindowOrderItem`** / **`SqlFrameBoundary`** / **`SqlFrameClause`** -- window-function supporting nodes (all in public `LinqToDB.SqlQuery` namespace, all tagged `// TODO: v7 - move to internal`).

## Files (Tier 1 / Tier 2)

**Tier 1** (5 / 5 visited in full):

- `Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs`
- `Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlField.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs`

**Tier 2** (143 candidates: 131 under `Internal/SqlQuery/`, 12 under legacy `SqlQuery/`).

Visited exemplars and full files: 133/143 (93.0%). See coverage block.

## Subsystems

The folder breaks down into seven sub-areas:

1. **Statement roots** -- `Sql<Verb>Statement.cs`. Every supported top-level operation gets one type, all extending `SqlStatement` or `SqlStatementWithQueryBase`.
2. **Composite query** -- `SelectQuery` and the six `Sql<Clause>Clause` files. The clause objects are owned 1:1 by the `SelectQuery` and back-link via `ClauseBase.SelectQuery`. `SqlGroupByClause` carries a `GroupingType` enum (Default / GroupBySets / Rollup / Cube).
3. **Expression nodes** -- `Sql*Expression.cs` plus `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlColumn`. All implement `ISqlExpression`. `SqlParameterizedExpressionBase` is the shared abstract base for `SqlFunction` and `SqlExpression`.
4. **Predicate nodes** -- nested classes of `SqlPredicate`, glued together by `SqlSearchCondition`. `ExprExpr.Reduce` is the central null-comparison rewrite point for `CompareNulls.LikeClr`.
5. **Tables and sources** -- `SqlTable` (entity-mapped) and its specialised siblings, plus the positional wrappers `SqlTableSource` / `SqlJoinedTable`. `SqlTableType` enum (8 values) classifies each source kind.
6. **Visitors** -- under `Visitors/`. Six visitor base classes plus eleven specialised pass implementations. Anything that walks the AST goes through one of these. The public entry point is always `QueryVisitorExtensions` extension methods.
7. **Helpers and contexts** -- `QueryHelper`, `NullabilityContext`, `EvaluationContext`, `AliasesContext`, `PseudoFunctions`, `DebugStringExtensions` / `QueryElementTextWriter`.

## Interactions

- **Producer**: every `*Builder` under `Source/LinqToDB/Internal/Linq/Builder/` constructs AST nodes -- see [`EXPR-TRANS`](../EXPR-TRANS/INDEX.md). Handles like `MakeToLower`, `MakeCast` are the canonical entry points.
- **Consumer**: `BasicSqlBuilder` and provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/` walk the AST through `QueryElementVisitor` and emit dialect text. `BasicSqlOptimizer` runs the AST-rewriting passes before `BasicSqlBuilder` emits.
- **Cross-cutting**: `NullabilityContext` is threaded through both producer and consumer paths.
- **Debug rendering**: `SqlStatement.SqlText` and `SelectQuery.SqlText` call back into `QueryElementTextWriter`. This is *only* for `[DebuggerDisplay]` and test diagnostics -- production SQL emission is done by `BasicSqlBuilder`.
- **Identity**: `SelectQuery.SourceIDCounter` is a single static counter increment'd by every `ISqlTableSource` constructor. Source IDs are repo-global within a process and survive cloning.
- **Parameter type refinement**: `SqlSetExpression` constructor calls `RefineDbParameter` to copy column-descriptor type metadata onto any `SqlParameter`.

## Inbound / outbound dependencies

**Inbound** (who consumes types defined here):

- `SQL-PROVIDER` -- `BasicSqlBuilder`, `BasicSqlOptimizer`, every `<Provider>SqlBuilder.cs` / `<Provider>SqlOptimizer.cs`. Heaviest consumer.
- `EXPR-TRANS` -- every `*Builder.cs` under `Internal/Linq/Builder/` constructs AST nodes.
- `LINQ` -- `Query`, `QueryRunner` carry parsed `SqlStatement` instances and pass them to the provider.
- `Internal/SqlProvider/Tools` -- `SqlProviderHelper` and similar helpers.
- Per-provider AST extensions -- providers define `SqlExpression`-based subclasses for hints. The DuckDB provider (added in PR #5451) uses `QueryVisitorExtensions.Convert` with `withStack: true` for its `InlineOutputParametersVisitor` pass -- this previously threw `NotImplementedException` and is now fully functional.

**Outbound** (what this area depends on):

- `LinqToDB.Mapping.ColumnDescriptor` -- read by `SqlField(ColumnDescriptor)` and `SqlTable(EntityDescriptor)`.
- `LinqToDB.Common` -- `DbDataType`, `TakeHints`, plus `ObjectPool<T>`.
- `LinqToDB.Internal.Common` -- `Utils.ObjectReferenceEqualityComparer`, `StackGuard`, `Annotatable`.
- `LinqToDB.Sql` -- `Sql.AggregateModifier`, `Sql.NullsPosition`.
- `Internal.Infrastructure` -- `Annotatable` for CTE annotations.
- `LinqToDB.Internal.Linq.Builder` -- `IToSqlConverter` referenced by `SqlInlinedToSqlExpression`.
- `LinqToDB.Internal.SqlProvider` -- `SqlProviderFlags` referenced by `SqlQueryOrderByOptimizer` and `SqlQueryValidatorVisitor` (one-directional).

No dependency on `SQL-PROVIDER` builder/optimizer classes -- the AST is the *bottom* of the dependency stack within the query pipeline.

## Known issues / debt

- **Legacy public namespace.** Twelve types still live in `Source/LinqToDB/SqlQuery/` (`LinqToDB.SqlQuery` namespace). Eight of them carry an explicit `// TODO: v7 - move to internal namespace...` comment.
- **`ISqlTableSource` extends `ISqlExpression`.** Open TODO at `ISqlTableSource.cs:5`. `SqlTableLikeSource` makes this concrete: all `ISqlExpression` members on it throw `NotSupportedException`.
- **Three-valued-logic flag duplicated.** `SqlSearchCondition.CanReturnUnknown` and per-predicate `CanBeUnknown` are not consistently used.
- **`Precedence` is db-specific but lives globally.** TODO at `Precedence.cs:3`. The constant table assumes SQL Server-ish precedence.
- **`DefaultNullable` enum is publicly exposed but probably shouldn't be.** TODO at `DefaultNullable.cs:3`.
- **`ColumnDescriptor` set on every field, even non-column fields.** TODO at `SqlField.cs:96`.
- **`ISqlExpression.SystemType` flagged for v4 refactor.** TODO at `ISqlExpression.cs:12`. Still pending.
- **Visitor mode branches.** Each `VisitX` method switches on `VisitMode` and duplicates work three ways. Comment at `Visitors/QueryElementVisitor.cs:13` warns about de-sync risk.
- **`SqlBinaryExpressionHelper` type table is incomplete.** Some combinations are commented out.
- **`SqlQueryActionVisitor{TContext}.cs` has a duplicated attribute.** `[return: NotNullIfNotNull(nameof(element))]` appears twice.

## See also

- [`architecture/sql-ast.md`](../../architecture/sql-ast.md) -- cross-area narrative.
- [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md) -- where the AST sits in the LINQ -> SQL pipeline.
- [`architecture/public-api.md`](../../architecture/public-api.md) -- public-API discipline.
- [`areas/SQL-PROVIDER/INDEX.md`](../SQL-PROVIDER/INDEX.md) -- primary consumer.
- [`areas/EXPR-TRANS/INDEX.md`](../EXPR-TRANS/INDEX.md) -- primary producer.
- [`areas/LINQ/INDEX.md`](../LINQ/INDEX.md) -- query-execution layer.
- `.claude/docs/code-design.md` -> "SQL AST types live in `LinqToDB.Internal.SqlQuery`".

## Pointers

- Visitor entry: every node's `Accept(QueryElementVisitor)` method.
- Add a new node:
  1. Add a `QueryElementType` value (`QueryElementType.cs`).
  2. Create the class in `LinqToDB.Internal.SqlQuery` extending `QueryElement` or `SqlExpressionBase`.
  3. Add a `VisitX` method to `QueryElementVisitor` and override in clone/convert visitors.
  4. Implement `ToString(QueryElementTextWriter)`, `GetElementHashCode()`, `Equals(other, comparer)` if the type is an expression.
  5. If consumed by SQL emission, add a corresponding case to `BasicSqlBuilder`.
- Walk an AST: `element.Visit(state, (state, e) => ...)`. `VisitParentFirst` for top-down traversal.
- Clone an AST: `element.Clone()` / `element.Clone(predicate)` -> `SqlQueryCloneVisitor`.
- Evaluate a constant subtree: `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)`.
- Inspect nullability: `expr.CanBeNullable(NullabilityContext.GetContext(query))`.
- Wrap a sub-query: `QueryHelper.WrapQuery(context, statement, wrapTest, onWrap, allowMutation)`.
- Replace nodes in a tree: `element.Replace(replacements, toIgnore)` -> `QueryElementReplacingVisitor`.
- Null-comparison rewriting: `SqlPredicate.ExprExpr.Reduce(nullability, context, isInsidePredicate, options)`.
- Use `WithStack`/`ParentElement` on a convert visitor: call `Convert<TContext, T>(element, context, convertAction, withStack: true)` -- now fully operational after PR #5451 removed the `NotImplementedException` guard. Access `visitor.ParentElement` inside `convertAction` to get the true parent node (returns `Stack[^2]`).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlField.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs
- Tier 2 (visited / total): 133 / 143 (93.0%)
  - Visited in detail (~55 files), visited as near-identical-shape group (~75 files), legacy folder (12/12).
  - Read (this run, delta 2026-05-11):
    - Source/LinqToDB/Internal/SqlQuery/QueryVisitorExtensions.cs -- PR #5451 removed NotImplementedException guard on Convert(withStack: true)
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs -- whitespace cleanup only
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryConvertVisitorBase.cs -- PR #5451: ParentElement now returns Stack[^2] not Stack[^1]
- Tier 3 (skipped, logged): 0 -- no generated / bin/ / obj/ files under this scope.
</details>
