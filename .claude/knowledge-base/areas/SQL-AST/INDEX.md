---
area: SQL-AST
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 5/5
coverage_tier_2: 143/145
---

# SQL-AST

Internal abstract syntax tree representing every SQL query linq2db can build. Lives almost entirely in the `LinqToDB.Internal.SqlQuery` namespace under `Source/LinqToDB/Internal/SqlQuery/`. The AST is the *contract* between three subsystems: the [expression translator](../EXPR-TRANS/INDEX.md) (producer), the [SQL provider](../SQL-PROVIDER/INDEX.md) (consumer that emits dialect text), and the [query runner](../LINQ/INDEX.md) (which materialises results).

Every node implements `IQueryElement` (`Source/LinqToDB/Internal/SqlQuery/IQueryElement.cs:8`). Concrete nodes derive from `QueryElement` (`Source/LinqToDB/Internal/SqlQuery/QueryElement.cs:11`) or, when they participate as expressions, from `SqlExpressionBase` which adds `ISqlExpression` semantics (`Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs:5`). The single dispatch entry is `IQueryElement.Accept(QueryElementVisitor)`, which every node implements as one line -- `visitor.Visit<TypeName>(this)` (e.g. `Source/LinqToDB/Internal/SqlQuery/SqlField.cs:186`).

## Key types

- **`IQueryElement`** (`IQueryElement.cs:8`) -- root interface; exposes `ElementType` (a tagged `QueryElementType` enum), debug `ToString(QueryElementTextWriter)`, `Accept(QueryElementVisitor)` and `GetElementHashCode()`. The hash is *structural*, not identity-based.
- **`QueryElementType`** (`QueryElementType.cs:6`) -- tagged-union discriminator. Every concrete node returns its constant from `ElementType`. `SqlConcat` was appended at the end of the enum (after `SqlFrameBoundary`) specifically to preserve v6.x `LinqService` wire-compatibility -- enum ordinals are serialised as `int` on the wire, so inserting mid-enum would shift all subsequent values.
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
  - **`SqlConcatExpression`** (PR #5504) -- string-concatenation node. Constructor: `SqlConcatExpression(bool preserveNull, params ISqlExpression[] expressions)`. `PreserveNull = true` propagates NULL if any operand is NULL (mirrors prior `+`-chain semantics); `false` replaces NULL operands with empty string. `SystemType` is always `typeof(string)` regardless of operand types -- non-string operands are rewritten to `.ToString()` upstream. `Precedence` is `Precedence.Concatenate` (value 5, below all arithmetic operators -- defensive against per-provider `||` precedence variance). `CanBeNullable` returns `true` iff `PreserveNull && Expressions.Any(e => e.CanBeNullable(nullability))`. Debug rendering: `$CONCAT$(expr1, expr2, ...)`. `Modify(expressions[])` mutates the `Expressions` array in place (used by `VisitMode.Modify`). `ElementType = QueryElementType.SqlConcat`. `GetElementHashCode` hashes `PreserveNull` then each operand's structural hash.
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
  - **`SqlOrderByClause`** -- `Expr()` now has an overload accepting `Sql.NullsPosition nullsPosition`; the no-position convenience overloads default to `Sql.NullsPosition.None`.
- **Table-shaped nodes** -- `SqlTable` (entity-mapped), `SqlCteTable` (CTE reference), `SqlRawSqlTable`, `SqlValuesTable` (`VALUES`-list), `SqlTableSource` (positional wrapper holding alias + joins), `SqlJoinedTable`, `SqlTableLikeSource` (used as MERGE/MultiInsert source).
  - **`SqlTableSource`** -- now implements `Deconstruct(out ISqlTableSource source)` for pattern-matching deconstruction.
- **SET clause helper** -- `SqlSetExpression` -- a `Column`/`Expression` pair used in UPDATE `SET` and `OUTPUT ... INTO`. Constructor calls `RefineDbParameter` to propagate column-descriptor type metadata.
- **SET operator** -- `SqlSetOperator` -- wraps a `SelectQuery` + `SetOperation` enum.
- **`SqlTableType`** enum -- `Table`, `SystemTable`, `Function`, `Expression`, `Cte`, `RawSql`, `MergeSource`, `Values`.
- **CTE infrastructure** -- `CteClause`, `SqlWithClause`, `SqlCteTable`. `CteClause` carries an open-ended `Annotations` bag and a static `CteIDCounter` separate from `SourceID`.
- **`SqlOrderByItem`** -- ORDER BY item node. Now carries a `NullsPosition` property (`Sql.NullsPosition`); constructor overload `SqlOrderByItem(expr, isDescending, isPositioned, nullsPosition)`. `ToString` renders `NULLS FIRST`/`NULLS LAST` suffix when `NullsPosition != None`. `GetElementHashCode` includes `NullsPosition`.
- **`NullsDefaultOrdering`** (`Source/LinqToDB/Internal/SqlQuery/NullsDefaultOrdering.cs:8`) -- NEW public enum. Describes how a provider places NULLs in ORDER BY when no `NULLS FIRST`/`NULLS LAST` is specified. Values: `Unknown` (always emit the requested position), `Smallest` (NULL sorts as smallest -- SQL Server, MySQL, SQLite, Firebird: ASC => first, DESC => last), `Largest` (NULL sorts as largest -- Oracle, PostgreSQL, DB2: ASC => last, DESC => first), `AlwaysFirst` (NULL always first), `AlwaysLast` (NULL always last -- ClickHouse). Used by `QueryHelper.MatchesNaturalNullsPosition` and `SqlNullsOrderingLoweringVisitor` to elide emulated CASE sort keys when the provider's natural ordering already matches the request.
- **Visitor base classes** -- `QueryElementVisitor` is the abstract dispatcher with one `VisitX` method per node type. Subclasses:
  - `SqlQueryVisitor` -- adds replacement tracking via `IVisitorTransformationInfo`; `GetVisitMode` promotes already-replaced nodes from `Transform` -> `Modify`.
  - `SqlQueryCloneVisitorBase` / `SqlQueryCloneVisitor` -- deep clone with optional predicate filter.
  - `SqlQueryConvertVisitorBase` / `SqlQueryConvertVisitor<TContext>` -- tree rewriting; optionally maintains a `Stack<IQueryElement>` for parent access via `WithStack`/`ParentElement`. **`ParentElement`** returns `Stack[^2]` (the true parent, not `Stack[^1]` which is self) -- fixed in PR #5451 (DuckDB). The `Convert<T>(element, convertAction, withStack: true)` overload on `QueryVisitorExtensions` also previously threw `NotImplementedException`; that guard was removed in the same PR so stack-enabled convert is now fully operational.
  - `SqlQueryActionVisitor` / `SqlQueryActionVisitor<TContext>` -- bottom-up read-only walk.
  - `SqlQueryFindVisitor` / `SqlQueryFindVisitor<TContext>` -- first-match search.
  - `SqlQueryFindExceptVisitor<TContext>` -- first-match search excluding a specified subtree.
  - `SqlQueryParentFirstVisitor` -- top-down walk.
  - `QueryElementReplacingVisitor` -- `Modify`-mode replacement using an `IDictionary<IQueryElement,IQueryElement>` map.
  - `SelectQueryOptimizerVisitor` -- the pooled, stateful optimizer the SQL builder runs before emission. Embeds six sub-visitors as fields. PR #5504: overrides `VisitSqlConcatExpression` to push `_isSubqueryInsideCondition = true` across the operands (same treatment as coalesce). PR #5522: extended `IsColumnExpressionAllowedToMoveUp` to handle `SqlConcatExpression` -- when all but one operand is a constant, recurse on the unique non-constant operand (covers `"prefix-" || col || "-suffix"` patterns with N >= 2 operands). Also embeds `SqlNullsOrderingLoweringVisitor` for pre-optimizer NULLS lowering.
  - `SqlQueryColumnNestingCorrector` -- builds a private `QueryNesting` tree tracking query-containment depth. PR #5556: `GetVisitMode` now returns `VisitMode.Transform` for all expression-shaped node types (including `SqlConcat`, `SqlCast`, `SqlCondition`, `SqlRow`, `CompareTo`, all predicate types, etc.) while leaving container-shaped nodes (statements, `SelectQuery`, clauses, table sources) at `VisitMode.Modify`. Rationale: expression-shaped nodes may be shared across query scopes; Transform mode produces a fresh instance on child modification rather than mutating the shared slot in place, preventing corruption of sibling scopes. Container-shaped nodes stay in Modify so side-effects (`AddColumn`/`AddField` on subquery clauses) remain visible to the eight callers that ignore the visitor's return value.
  - `SqlQueryColumnOptimizerVisitor` -- two-pass: pass 1 collects used `SqlColumn` references, pass 2 removes unused columns.
  - `SqlQueryColumnUsageCollector` -- propagates column usage across `SetOperators` and CTE field-index boundaries.
  - `SqlQueryOrderByOptimizer` -- consults `SqlProviderFlags`; pulls ORDER BY items from inner queries to outer when provider flag forbids inner ORDER BY. PR #5556: uses `_columnNestingCorrector.CorrectColumnNesting(selectQuery)` after ORDER BY item promotion when `needsNestingUpdate` is set -- delegates nesting repair to the corrector rather than doing it inline. `OptimizeOrderBy` now accepts a `SqlQueryColumnNestingCorrector columnNestingCorrector` parameter (passed from `SelectQueryOptimizerVisitor`). `SqlExpression`/`SqlFragment` ORDER BY items are pushed directly to the parent ORDER BY without wrapping in `AddColumn` -- raw-template AST nodes may carry trailing direction modifiers (e.g. `{0} NULLS FIRST`) that must remain in the outer clause position.
  - **`SqlNullsOrderingLoweringVisitor`** (`Source/LinqToDB/Internal/SqlQuery/Visitors/SqlNullsOrderingLoweringVisitor.cs:14`) -- NEW. Lowers `Sql.NullsPosition` on regular ORDER BY items into an explicit `CASE WHEN expr IS NULL THEN 0/1 END` emulation sort key for providers lacking native `NULLS FIRST`/`NULLS LAST` support. Runs before the query optimizer so the emulation key is treated as an ordinary derived expression. Window `OVER(ORDER BY ...)` ordering is emulated separately at SQL-build time and is intentionally not touched here. Uses `QueryHelper.MatchesNaturalNullsPosition` and `item.Expression.CanBeNullable(nullability)` to elide no-op emulations. Constructed with `NullsDefaultOrdering nullsOrdering`; entry point `LowerNullsOrdering(IQueryElement)`.
  - `ReduceIsNullExpressionVisitor` -- simplifies `IS NULL` predicates over compound expressions. Now handles `SqlConcatExpression`: null-propagating concats (`PreserveNull = true`) reduce each operand via `ReduceOrAdd`; non-propagating concats are not nullable and never reach this reducer.
  - `SqlQueryValidatorVisitor` -- validates structural constraints.
- **`VisitMode`** -- `ReadOnly | Modify | Transform`. Each `VisitX` method has three branches.
- **`NullabilityContext`** -- query-scoped cache that decides whether a given `ISqlExpression` can produce NULL.
- **`EvaluationContext`** -- caches client-side and server-side evaluation results for `IQueryElement` subtrees.
- **`QueryHelper`** -- static facade for tree analysis and transformation: `IsDependsOnSource`, `EnumerateAccessibleSources`, `IsAggregationQuery`, `WrapQuery` (in `QueryHelper.WrapQuery.cs`), `TryEvaluateExpression` (in `QueryHelper.Evaluate.cs`), `GetDbDataType` / `SuggestDbDataType` / `GetColumnDescriptor`, `CollectUniqueKeys`, `CalcCanBeNull`. Exposes pooled `SelectQueryOptimizerVisitor` / `AggregationCheckVisitor` instances. `GetColumnDescriptor` now handles `QueryElementType.SqlConcat` by iterating operands and returning the first operand that carries a descriptor. The string-format decomposer helper (previously emitting `SqlBinaryExpression` `+` chains) now emits `new SqlConcatExpression(preserveNull: true, parts.ToArray())` when the part list has >= 2 entries. Added: `GetNaturalNullsPosition(NullsDefaultOrdering, bool descending)` -- resolves the provider's natural NULL placement as a `Sql.NullsPosition?` (null when `Unknown`); `MatchesNaturalNullsPosition(NullsDefaultOrdering, Sql.NullsPosition requested, bool descending)` -- returns true when the requested position already matches the provider's natural position and can be elided.
- **`QueryVisitorExtensions`** -- the public extension-method surface for visiting/finding/cloning AST nodes. All operations go through pooled visitors. Key methods: `Visit` / `VisitAll`, `VisitParentFirst`, `Find` / `FindExcept`, `Clone`, `Replace`, `Convert` / `ConvertAll`. Two pools exist for `SqlQueryConvertVisitor<TContext>`: `ConvertPool` (immutable/Transform mode) and `ConvertMutationPool` (`allowMutation: true` / Modify mode). PR #5451 removed the `NotImplementedException` guard on the `Convert<T>(element, convertAction, withStack: true)` overload, making stack-enabled conversion fully operational.
- **`PseudoFunctions`** -- well-known function-name constants like `$ToLower$`, `$Convert_Format$`, `$merge_action$`.
- **`QueryElementTextWriter`** -- debug renderer threaded through every node's `ToString(writer)`.
- **`SqlBinaryExpressionHelper`** -- compile-time static lookup table for C# numeric binary-operator result types for `+` and `-`.
- **`SqlFlags`** -- `[Flags]` enum: `IsAggregate=0x1`, `IsPure=0x4`, `IsPredicate=0x8`, `IsWindowFunction=0x10`.
- **`Precedence`** (legacy public) -- int constants ranking SQL operator precedence. Added: `Concatenate = 5` (PR #5504) -- conservative low-binding value below all other operators, forcing parentheses around concat chains when nested inside another operator. Rationale: `||` precedence varies per provider (SQLite: between unary and multiplicative; Oracle: additive level).
- **`SqlObjectName`** (public) -- `readonly record struct` with `Name`, `Server`, `Database`, `Schema`, `Package`.
- **`SqlDataType`** (public) -- `SqlExpressionBase` wrapping a `DbDataType`. Added static fields: `DbDecFloat` (`DataType.DecFloat`, `typeof(object)`) and `DbTimeTZ` (`DataType.TimeTZ`, `typeof(object)`) for provider-specific floating-point decimal and time-with-timezone types. `GetDataType` switch extended with `DecFloat` and `TimeTZ` cases.
- **`SqlFunctionArgument`** / **`SqlWindowOrderItem`** / **`SqlFrameBoundary`** / **`SqlFrameClause`** -- window-function supporting nodes (all in public `LinqToDB.SqlQuery` namespace, all tagged `// TODO: v7 - move to internal`). **`SqlFrameClause`** now exposes a `Modify(SqlFrameBoundary start, SqlFrameBoundary end)` mutator used by `VisitMode.Modify` visitor passes.

## Files (Tier 1 / Tier 2)

**Tier 1** (5 / 5 visited in full):

- `Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs`
- `Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlField.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs`

**Tier 2** (145 candidates: 133 under `Internal/SqlQuery/`, 12 under legacy `SqlQuery/`). Delta adds 2 new files (`NullsDefaultOrdering.cs`, `SqlNullsOrderingLoweringVisitor.cs`, both visited this run).

Visited exemplars and full files: 143/145 (98.6%). See coverage block.

## Subsystems

The folder breaks down into seven sub-areas:

1. **Statement roots** -- `Sql<Verb>Statement.cs`. Every supported top-level operation gets one type, all extending `SqlStatement` or `SqlStatementWithQueryBase`.
2. **Composite query** -- `SelectQuery` and the six `Sql<Clause>Clause` files. The clause objects are owned 1:1 by the `SelectQuery` and back-link via `ClauseBase.SelectQuery`. `SqlGroupByClause` carries a `GroupingType` enum (Default / GroupBySets / Rollup / Cube).
3. **Expression nodes** -- `Sql*Expression.cs` plus `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlColumn`. All implement `ISqlExpression`. `SqlParameterizedExpressionBase` is the shared abstract base for `SqlFunction` and `SqlExpression`. **`SqlConcatExpression`** (PR #5504) joins this group as the canonical string-concatenation node, replacing ad-hoc `SqlBinaryExpression("+")` chains emitted by the format-string decomposer.
4. **Predicate nodes** -- nested classes of `SqlPredicate`, glued together by `SqlSearchCondition`. `ExprExpr.Reduce` is the central null-comparison rewrite point for `CompareNulls.LikeClr`.
5. **Tables and sources** -- `SqlTable` (entity-mapped) and its specialised siblings, plus the positional wrappers `SqlTableSource` / `SqlJoinedTable`. `SqlTableType` enum (8 values) classifies each source kind.
6. **Visitors** -- under `Visitors/`. Six visitor base classes plus twelve specialised pass implementations (added `SqlNullsOrderingLoweringVisitor`). Anything that walks the AST goes through one of these. The public entry point is always `QueryVisitorExtensions` extension methods. PR #5556 introduced a selective-`Transform`-mode policy in `SqlQueryColumnNestingCorrector.GetVisitMode`: expression-shaped nodes (expressions, predicates) get `VisitMode.Transform` to avoid mutating shared AST subtrees; container-shaped nodes (queries, clauses, table sources) stay at `VisitMode.Modify` to preserve side-effect visibility for callers that ignore return values.
7. **Helpers and contexts** -- `QueryHelper`, `NullabilityContext`, `EvaluationContext`, `AliasesContext`, `PseudoFunctions`, `DebugStringExtensions` / `QueryElementTextWriter`.

## Interactions

- **Producer**: every `*Builder` under `Source/LinqToDB/Internal/Linq/Builder/` constructs AST nodes -- see [`EXPR-TRANS`](../EXPR-TRANS/INDEX.md). Handles like `MakeToLower`, `MakeCast` are the canonical entry points. String-concatenation in LINQ now produces `SqlConcatExpression` nodes rather than `SqlBinaryExpression("+")` chains.
- **Consumer**: `BasicSqlBuilder` and provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/` walk the AST through `QueryElementVisitor` and emit dialect text. `BasicSqlOptimizer` runs the AST-rewriting passes before `BasicSqlBuilder` emits. Providers must handle `QueryElementType.SqlConcat` in their builder's dispatch. `SqlNullsOrderingLoweringVisitor` runs as a pre-optimizer pass when a provider lacks native `NULLS FIRST`/`NULLS LAST` support; the `NullsDefaultOrdering` value is supplied by the provider via `SqlProviderFlags`.
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
- **`Precedence` is db-specific but lives globally.** TODO at `Precedence.cs:3`. The constant table assumes SQL Server-ish precedence. `Concatenate = 5` (PR #5504) deliberately uses a conservative global value because `||` precedence varies per provider.
- **`DefaultNullable` enum is publicly exposed but probably shouldn't be.** TODO at `DefaultNullable.cs:3`.
- **`ColumnDescriptor` set on every field, even non-column fields.** TODO at `SqlField.cs:96`.
- **`ISqlExpression.SystemType` flagged for v4 refactor.** TODO at `ISqlExpression.cs:12`. Still pending.
- **Visitor mode branches.** Each `VisitX` method switches on `VisitMode` and duplicates work three ways. Comment at `Visitors/QueryElementVisitor.cs:13` warns about de-sync risk.
- **`SqlBinaryExpressionHelper` type table is incomplete.** Some combinations are commented out.
- **`SqlQueryActionVisitor{TContext}.cs` has a duplicated attribute.** `[return: NotNullIfNotNull(nameof(element))]` appears twice.
- **`SqlConcat` enum ordinal placement.** `QueryElementType.SqlConcat` is at the tail of the enum (after `SqlFrameBoundary`) with a comment noting it should logically live next to `SqlCast`/`SqlCoalesce` -- deferred to v7 to avoid breaking LinqService wire-compat (`QueryElementType.cs:116`). Tracked as DI-0673.

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
  6. Note: for wire-compat (LinqService serialises `QueryElementType` ordinals as `int`), append new enum members at the tail and add a comment per `SqlConcat` precedent.
- Walk an AST: `element.Visit(state, (state, e) => ...)`. `VisitParentFirst` for top-down traversal.
- Clone an AST: `element.Clone()` / `element.Clone(predicate)` -> `SqlQueryCloneVisitor`.
- Evaluate a constant subtree: `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)`.
- Inspect nullability: `expr.CanBeNullable(NullabilityContext.GetContext(query))`.
- Wrap a sub-query: `QueryHelper.WrapQuery(context, statement, wrapTest, onWrap, allowMutation)`.
- Replace nodes in a tree: `element.Replace(replacements, toIgnore)` -> `QueryElementReplacingVisitor`.
- Null-comparison rewriting: `SqlPredicate.ExprExpr.Reduce(nullability, context, isInsidePredicate, options)`.
- Use `WithStack`/`ParentElement` on a convert visitor: call `Convert<TContext, T>(element, context, convertAction, withStack: true)` -- now fully operational after PR #5451 removed the `NotImplementedException` guard. Access `visitor.ParentElement` inside `convertAction` to get the true parent node (returns `Stack[^2]`).
- Build a string-concat node: `new SqlConcatExpression(preserveNull: true/false, expr1, expr2, ...)`. `preserveNull: true` for null-propagating (standard SQL `||`); `false` for null-to-empty-string coercion. `QueryHelper` format-string decomposer uses `preserveNull: true`.
- ORDER BY with explicit NULL placement: set `Sql.NullsPosition` on `SqlOrderByItem` or via `SqlOrderByClause.Expr(..., nullsPosition)`. For providers without native support, `SqlNullsOrderingLoweringVisitor` lowers to a `CASE WHEN expr IS NULL THEN 0/1 END` emulation key pre-optimizer. Check `QueryHelper.MatchesNaturalNullsPosition(ordering, requested, descending)` first to avoid redundant emulation.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlField.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs
- Tier 2 (visited / total): 143 / 145 (98.6%)
  - Visited in detail (~55 files), visited as near-identical-shape group (~75 files), legacy folder (12/12). Delta adds 2 new Tier-2 files.
  - Read (this run, delta 2026-05-11):
    - Source/LinqToDB/Internal/SqlQuery/QueryVisitorExtensions.cs -- PR #5451 removed NotImplementedException guard on Convert(withStack: true)
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs -- whitespace cleanup only
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryConvertVisitorBase.cs -- PR #5451: ParentElement now returns Stack[^2] not Stack[^1]
  - Read (this run -- delta 2026-06-01):
    - Source/LinqToDB/Internal/SqlQuery/SqlConcatExpression.cs (ADDED, PR #5504) -- new string-concat AST node; `PreserveNull`, `Expressions[]`, `Precedence.Concatenate`, `SystemType = typeof(string)`, `CanBeNullable` null-propagation semantics, `Modify()` mutator for Modify-mode visitor, debug rendering `$CONCAT$(...)`.
    - Source/LinqToDB/Internal/SqlQuery/QueryElementType.cs -- `SqlConcat` appended at tail (after `SqlFrameBoundary`) with wire-compat comment; logically belongs near `SqlCast`/`SqlCoalesce`, deferred to v7.
    - Source/LinqToDB/SqlQuery/Precedence.cs -- added `Concatenate = 5`; conservative low-binding value with per-provider variance rationale in XML doc.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs -- `VisitSqlConcatExpression` added at line 3292; standard three-branch (ReadOnly/Modify/Transform) implementation; Transform branch allocates new `SqlConcatExpression` preserving `PreserveNull` when any child changed.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs -- (PR #5504) `VisitSqlConcatExpression` override sets `_isSubqueryInsideCondition = true` across operands; (PR #5522) `IsColumnExpressionAllowedToMoveUp` extended for `SqlConcatExpression` with all-but-one-constant-operand recursion.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnNestingCorrector.cs -- (PR #5556) `GetVisitMode` returns `VisitMode.Transform` for expression-shaped and predicate-shaped `QueryElementType` values (including `SqlConcat`); container-shaped stay at `VisitMode.Modify`; detailed rationale in comment block at line 153.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryOrderByOptimizer.cs -- (PR #5556) delegates nesting-correction after ORDER BY promotion to `_columnNestingCorrector.CorrectColumnNesting(selectQuery)` when `needsNestingUpdate` flag is set.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryValidatorVisitor.cs -- no `SqlConcat`-specific handling found; no changes attributable to #5502/5522 in this file.
    - Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs -- `GetColumnDescriptor` handles `QueryElementType.SqlConcat` by scanning operands; format-string decomposer now emits `SqlConcatExpression(preserveNull: true, ...)` for >= 2 parts.
  - Read (this run -- delta 2026-06-14):
    - Source/LinqToDB/Internal/SqlQuery/NullsDefaultOrdering.cs (ADDED) -- new public enum; 5 values describing provider NULL sort placement; used by SqlNullsOrderingLoweringVisitor and QueryHelper.MatchesNaturalNullsPosition.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlNullsOrderingLoweringVisitor.cs (ADDED) -- new visitor; lowers Sql.NullsPosition ORDER BY items to CASE WHEN IS NULL emulation for providers without native NULLS support; elides no-op positions via MatchesNaturalNullsPosition / CanBeNullable.
    - Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs -- added GetNaturalNullsPosition and MatchesNaturalNullsPosition helpers for NullsDefaultOrdering.
    - Source/LinqToDB/Internal/SqlQuery/SqlOrderByItem.cs -- added NullsPosition property; new constructor overload; ToString renders NULLS suffix; GetElementHashCode includes NullsPosition.
    - Source/LinqToDB/Internal/SqlQuery/SqlOrderByClause.cs -- Expr() overload now accepts Sql.NullsPosition; convenience overloads default to Sql.NullsPosition.None.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryOrderByOptimizer.cs -- OptimizeOrderBy now receives SqlQueryColumnNestingCorrector parameter; SqlExpression/SqlFragment items pushed directly to parent ORDER BY.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/ReduceIsNullExpressionVisitor.cs -- VisitSqlConcatExpression added; null-propagating concat (PreserveNull=true) reduces each operand via ReduceOrAdd.
    - Source/LinqToDB/Internal/SqlQuery/SqlTableSource.cs -- Deconstruct(out ISqlTableSource source) added; no logic changes.
    - Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs -- no substantive changes.
    - Source/LinqToDB/Internal/SqlQuery/SqlValuesTable.cs -- no substantive changes.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs -- class declaration read; no interface changes.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryValidatorVisitor.cs -- full read; no NULLS-ordering-specific logic added.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs -- embeds SqlNullsOrderingLoweringVisitor for pre-optimizer lowering pass.
    - Source/LinqToDB/SqlQuery/SqlDataType.cs -- added DbDecFloat and DbTimeTZ static fields; GetDataType switch extended.
    - Source/LinqToDB/SqlQuery/SqlFrameClause.cs -- Modify(SqlFrameBoundary start, SqlFrameBoundary end) mutator added.
- Tier 3 (skipped, logged): 0 -- no generated / bin/ / obj/ files under this scope.
</details>
