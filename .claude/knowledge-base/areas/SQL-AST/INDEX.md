---
area: SQL-AST
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-07
last_verified_sha: 810c172d2cf4b404dc51b2343b491413c00f030a
coverage_tier_1: 5/5
coverage_tier_2: 130/143
---

# SQL-AST

Internal abstract syntax tree representing every SQL query linq2db can build. Lives almost entirely in the `LinqToDB.Internal.SqlQuery` namespace under `Source/LinqToDB/Internal/SqlQuery/`. The AST is the *contract* between three subsystems: the [expression translator](../EXPR-TRANS/INDEX.md) (producer), the [SQL provider](../SQL-PROVIDER/INDEX.md) (consumer that emits dialect text), and the [query runner](../LINQ/INDEX.md) (which materialises results).

Every node implements `IQueryElement` (`Source/LinqToDB/Internal/SqlQuery/IQueryElement.cs:8`). Concrete nodes derive from `QueryElement` (`Source/LinqToDB/Internal/SqlQuery/QueryElement.cs:11`) or, when they participate as expressions, from `SqlExpressionBase` which adds `ISqlExpression` semantics (`Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs:5`). The single dispatch entry is `IQueryElement.Accept(QueryElementVisitor)`, which every node implements as one line — `visitor.Visit<TypeName>(this)` (e.g. `Source/LinqToDB/Internal/SqlQuery/SqlField.cs:186`).

## Key types

- **`IQueryElement`** (`IQueryElement.cs:8`) — root interface; exposes `ElementType` (a tagged `QueryElementType` enum), debug `ToString(QueryElementTextWriter)`, `Accept(QueryElementVisitor)` and `GetElementHashCode()`. The hash is *structural*, not identity-based, so two distinct nodes with the same shape collide on purpose — used by clone visitors and equality comparers.
- **`QueryElementType`** (`QueryElementType.cs:6`) — tagged-union discriminator. Every concrete node returns its constant from `ElementType`. New nodes MUST add a value here.
- **`ISqlExpression`** (`ISqlExpression.cs:5`) — the value-producing subset; adds `Precedence`, `SystemType`, `CanBeNullable(NullabilityContext)`, and a `comparer`-driven `Equals` overload used to thread custom equality through nested trees.
- **`ISqlPredicate`** (`ISqlPredicate.cs:5`) — the boolean subset; adds `CanInvert` / `Invert(NullabilityContext)` and `CanBeUnknown(NullabilityContext, bool withoutUnknownErased)` to express SQL three-valued logic.
- **`ISqlTableSource`** (`ISqlTableSource.cs:6`) — anything usable in `FROM` / join. Carries an integer `SourceID` (allocated from `SelectQuery.SourceIDCounter`, `SelectQuery.cs:186`) which the rest of the pipeline uses as the canonical identity for a table reference. Implementations: `SqlTable`, `SelectQuery`, `SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`. The interface extends `ISqlExpression` — there's a `// TODO: [sdanyliv] ISqlTableSource why it extends ISqlExpression?` at `ISqlTableSource.cs:5` flagging the unease.
- **`SqlStatement`** (`SqlStatement.cs:8`) — abstract root of any executable statement; exposes `QueryType` (Select/Insert/Update/Delete/Merge/etc.) and the optional carried `SelectQuery`. Concrete `Sql<Verb>Statement` types extend either `SqlStatement` directly (`SqlCreateTableStatement`, `SqlDropTableStatement`, `SqlTruncateTableStatement`) or `SqlStatementWithQueryBase` (`SqlStatementWithQueryBase.cs:5`) which adds the `With` (CTE) clause and forces a non-null `SelectQuery`.
- **`SelectQuery`** (`SelectQuery.cs:12`) — the central composite node, aggregates the six clauses (`Select`/`From`/`Where`/`GroupBy`/`Having`/`OrderBy`, `SelectQuery.cs:70-75`) plus optional `SetOperators` (UNION/INTERSECT/EXCEPT) and `UniqueKeys` used by the join optimizer. Implements `ISqlTableSource` — a sub-query is itself a table source.
- **`SqlField`** (`SqlField.cs:9`) — column reference. Constructed either bare (`name`/`physicalName`) or from a `ColumnDescriptor` (`SqlField.cs:63-78`), which is how `SqlTable` populates its fields from mapping metadata. `SqlField.All(table)` produces the `*` placeholder used in `SELECT *` paths.
- **Expression nodes** — value-producing nodes that derive from `SqlExpressionBase`:
  - `SqlValue` (literal), `SqlParameter` (parameterised value with `IsQueryParameter`/`AccessorId`; `NeedsCast` triggers `$Cast$()` wrapper; `ValueConverter` chain supports `Take` offset injection), `SqlFunction` / `SqlExpression` (both extend the shared `SqlParameterizedExpressionBase`; `SqlFunction` is a named function call, `SqlExpression` is a free-form format-string fragment with type info, `SqlExpression.cs:10`),
  - `SqlParameterizedExpressionBase` (`SqlParameterizedExpressionBase.cs:9`) — abstract base holding `ExprOrName`, `Parameters[]`, `Flags` (`SqlFlags`), `NullabilityType` (`ParametersNullabilityType`), `CanBeNullNullable`; both `SqlFunction` and `SqlExpression` inherit from it.
  - `SqlFragment` (`SqlFragment.cs`) — an untyped format-string fragment (`SystemType = null`) used when type information should be hidden from linq2db; distinct from `SqlExpression`. `SqlFlags.IsPure` is not set; `ElementType = QueryElementType.SqlFragment`.
  - `SqlBinaryExpression` (`SqlBinaryExpression.cs:9`), `SqlUnaryExpression` (`SqlUnaryExpression.cs:9`; `Operation` is `SqlUnaryOperation` enum: `Negation` or `BitwiseNegation`),
  - `SqlCaseExpression` (carries `List<CaseItem>` each with `ISqlPredicate Condition` + `ISqlExpression ResultExpression`, `SqlCaseExpression.cs:12`), `SqlConditionExpression` ($IIF$, `SqlConditionExpression.cs:8`), `SqlCoalesceExpression`, `SqlCompareToExpression`, `SqlCastExpression` (`SqlCastExpression.cs:9`),
  - `SqlNullabilityExpression` (wraps another expression to override its nullability annotation, `SqlNullabilityExpression.cs:9`), `SqlAnchor` (table-source / inserted / deleted markers used during merge/output translation; `AnchorKindEnum` has 6 values: `Deleted`, `Inserted`, `TableSource`, `TableName`, `TableAsSelfColumn`, `TableAsSelfColumnOrField`, `SqlAnchor.cs:8`), `SqlRowExpression` (multi-column row constructor, `SqlRowExpression.cs:9`),
  - `SqlAliasPlaceholder` — singleton (`SqlAliasPlaceholder.Instance`), renders as `%ts%`; used as a placeholder during alias generation, never emitted to SQL (`SqlAliasPlaceholder.cs`).
  - `SqlObjectExpression` (`SqlObjectExpression.cs`) — bundles a `SqlGetValue[]` array; extracts per-field SQL values from an object at execution time. Identity-equality only (`Equals` returns `ReferenceEquals`). Used for object-parameter decomposition.
  - `SqlGroupingSet` (`SqlGroupingSet.cs`) — an `ISqlExpression` node for a single `()` set within `GROUP BY GROUPING SETS`. Used inside `SqlGroupByClause.Items` alongside regular expressions when `GroupingType != Default`.
  - `SqlInlinedBase` and its derivatives `SqlInlinedSqlExpression` / `SqlInlinedToSqlExpression` — placeholders for `ISqlExpression` / `IToSqlConverter` instances embedded directly in a LINQ expression (see `SqlInlinedBase.cs:5`, `QueryElementType.SqlInlinedExpression`). `SqlInlinedToSqlExpression` depends on `IToSqlConverter` from `LinqToDB.Internal.Linq.Builder`.
  - `SqlExtendedFunction` (`Source/LinqToDB/SqlQuery/SqlExtendedFunction.cs:13`) — the window/analytic function node (public namespace). Carries `Arguments` (`List<SqlFunctionArgument>`), `WithinGroup`, `PartitionBy`, `OrderBy`, `FrameClause` (`SqlFrameClause`), `Filter` (`SqlSearchCondition`), `IsAggregate`, `CanBeAffectedByOrderBy`. `IsWindowFunction` is computed as `OrderBy?.Count > 0 || PartitionBy?.Count > 0 || FrameClause != null`.
  - `SqlQueryExtension` (`SqlQueryExtension.cs:9`) — provider extension node embedded in a query. Carries `Configuration` (optional provider name filter), `Scope` (`Sql.QueryExtensionScope`), required `Arguments` dict, optional `BuilderType` implementing `ISqlQueryExtensionBuilder` or `ISqlTableExtensionBuilder`.
- **Predicate nodes** — boolean nodes nested inside `SqlSearchCondition`:
  - All defined as nested classes of `SqlPredicate` (`SqlPredicate.cs:10`): `Not`, `TruePredicate`, `FalsePredicate`, `Expr`, `BaseNotExpr`, `ExprExpr`, `Like`, `SearchString`, `Between`, `IsNull`, `IsDistinct`, `IsTrue`, `InSubQuery`, `InList`, `Exists`, plus the `Operator` enum (=, <>, >, …, `Overlaps`).
  - `ExprExpr.Reduce(NullabilityContext, EvaluationContext, bool isInsidePredicate, LinqOptions)` (`SqlPredicate.cs:386`) is where `CompareNulls.LikeClr` null-comparison rewrites are applied — converts equality comparisons against nullable expressions into `IS NULL` predicates and `SqlSearchCondition` trees.
  - `SqlSearchCondition` (`SqlSearchCondition.cs:12`) — the `AND`/`OR`-of-predicates container; `IsOr` toggles the connective, `CanReturnUnknown` carries the three-valued-logic flag. Contains a pooled internal `CollectNotNullExpressionsVisitor` (private, extends `SqlQueryVisitor`) used by `CanBeUnknown` to factor out `IS [NOT] NULL` predicates and inject per-expression nullability overrides into a local `NullabilityContext`.
- **Clause nodes** — `SqlSelectClause`, `SqlFromClause`, `SqlWhereClause`, `SqlGroupByClause`, `SqlHavingClause`, `SqlOrderByClause`, `SqlInsertClause`, `SqlUpdateClause`, `SqlOutputClause`, `SqlConditionalInsertClause`, `SqlMergeOperationClause`. All extend `ClauseBase` (`ClauseBase.cs:3`) which holds a back-pointer to the owning `SelectQuery`. Two variants exist: `ClauseBase` (non-generic) and `ClauseBase<T1>` (generic, provides fluent return-type for some clauses like `SqlHavingClause`).
  - `SqlSelectClause` deduplicates columns via `AddOrFindColumn` (compares `UnderlyingExpression()`); exposes `IsDistinct`, `TakeValue`/`TakeHints`/`SkipValue`, `OptimizeDistinct`.
  - `SqlUpdateClause` (`SqlUpdateClause.cs:9`) — `Items` (SET expressions), `Keys` (key fields for update matching), `Table`/`TableSource`, `HasComparison` flag.
  - `SqlOutputClause` (`SqlOutputClause.cs:9`) — `OutputItems` (`List<SqlSetExpression>`), `OutputColumns` (`List<ISqlExpression>`), `OutputTable` (`SqlTable?`); `HasOutput` / `HasOutputItems` convenience props.
  - `SqlWhereClause` wraps a `SqlSearchCondition SearchCondition`; `IsEmpty` checks `Predicates.Count == 0`.
- **Table-shaped nodes** — `SqlTable` (entity-mapped, `SqlTable.cs:14`): `Expression`/`TableArguments` support table-valued-function syntax (format string + params); `_fieldsLookup` dictionary keyed by member name for `FindFieldByMemberName`; `SuggestType` resolves `DataType.Undefined` via `MappingSchema`. `SqlCteTable` (CTE reference, `SqlCteTable.cs:11`), `SqlRawSqlTable` (raw SQL fragment used as a table, `SqlRawSqlTable.cs:11`), `SqlValuesTable` (`VALUES`-list, `SqlValuesTable.cs:11`): `BuildRows(EvaluationContext)` materialises rows from an `IEnumerable` source via per-field `ValueBuilders` lambdas at execution time. `SqlTableSource` (positional wrapper holding alias + joins): `UniqueKeys` (`List<ISqlExpression[]>`) used by join optimizer for safe sub-query removal; `SourceID` delegates to `Source.SourceID`. `SqlJoinedTable` (one element of a `JOIN` chain; carries `SourceCardinality Cardinality` and `bool IsSubqueryExpression` flags), `SqlTableLikeSource` (used as MERGE/MultiInsert source, `SqlTableLikeSource.cs:9`): holds either `SourceEnumerable` (a `SqlValuesTable`) or `SourceQuery` (a `SelectQuery`), never both; `ISqlExpression` members throw `NotSupportedException` — this node is purely a table source.
- **SET clause helper** — `SqlSetExpression` (`SqlSetExpression.cs:9`) — a `Column`/`Expression` pair used in UPDATE `SET` and `OUTPUT … INTO`. Constructor calls `RefineDbParameter` to propagate column-descriptor type metadata (DataType, DbType, Length, Precision, Scale) onto any `SqlParameter` value at construction time.
- **SET operator** — `SqlSetOperator` (`SqlSetOperator.cs:9`) — wraps a `SelectQuery` + `SetOperation` enum (Union/UnionAll/Except/ExceptAll/Intersect/IntersectAll).
- **`SqlTableType`** enum (`SqlTableType.cs:3`) — `Table`, `SystemTable` (NEW/OLD, INSERTED/DELETED pseudo-tables), `Function`, `Expression`, `Cte`, `RawSql`, `MergeSource`, `Values`.
- **CTE infrastructure** — `CteClause` (`CteClause.cs:13`), `SqlWithClause`, `SqlCteTable`. `CteClause` carries an open-ended `Annotations` bag (`CteClause.cs:30`) for provider-specific hints (e.g. PostgreSQL `MATERIALIZED`) and a static `CteIDCounter` (via `Interlocked.Increment`) separate from `SourceID`. `SqlWithClause` owns `List<CteClause> Clauses` and exposes `GetTableSource` to resolve source references through CTE bodies.
- **Visitor base classes** — `QueryElementVisitor` (`Visitors/QueryElementVisitor.cs:27`) is the abstract dispatcher with one `VisitX` method per node type. Subclasses:
  - `SqlQueryVisitor` (`Visitors/SqlQueryVisitor.cs:18`) — adds replacement tracking via `IVisitorTransformationInfo`; `GetVisitMode` promotes already-replaced nodes from `Transform` → `Modify` to skip redundant cloning.
  - `SqlQueryCloneVisitorBase` / `SqlQueryCloneVisitor` (`Visitors/SqlQueryCloneVisitor.cs:5`) — deep clone with optional predicate filter; `ShouldReplace` returns `false` for `SqlParameter` (parameters are shared, not cloned). `SqlQueryCloneVisitor<TContext>` adds typed context.
  - `SqlQueryConvertVisitorBase` / `SqlQueryConvertVisitor<TContext>` (`Visitors/SqlQueryConvertVisitorBase.cs:5`) — tree rewriting; optionally maintains a `Stack<IQueryElement>` for parent access via `WithStack`/`ParentElement`; `ConvertElement` hook called after child traversal; re-visits the converted element if it changed.
  - `SqlQueryActionVisitor` / `SqlQueryActionVisitor<TContext>` — bottom-up read-only walk, invokes an `Action` delegate post-children; optional `visitAll` flag controls deduplication via `HashSet`.
  - `SqlQueryFindVisitor` / `SqlQueryFindVisitor<TContext>` — first-match search, short-circuits on first found element.
  - `SqlQueryFindExceptVisitor<TContext>` — first-match search excluding a specified subtree.
  - `SqlQueryParentFirstVisitor` / `SqlQueryParentFirstVisitor<TContext>` — top-down walk; action returns `bool` to control descent.
  - `QueryElementReplacingVisitor` (`Visitors/QueryElementReplacingVisitor.cs:7`) — `Modify`-mode replacement using an `IDictionary<IQueryElement,IQueryElement>` map with an explicit ignore list; handles `VisitCteClauseReference` specially (not dispatched via the main `Accept` path).
  - `SelectQueryOptimizerVisitor` (`Visitors/SelectQueryOptimizerVisitor.cs:16`) — the pooled, stateful optimizer the SQL builder runs before emission. Embeds six sub-visitors as fields: `SqlQueryColumnNestingCorrector`, `SqlQueryOrderByOptimizer`, `MovingComplexityVisitor`, `SqlExpressionOptimizerVisitor`, `MovingOuterPredicateVisitor`, `SqlQueryColumnOptimizerVisitor`.
  - `SqlQueryColumnNestingCorrector` — builds a private `QueryNesting` tree tracking query-containment depth; rewrites `SqlField`/`SqlColumn` references that escape their owning scope by wrapping them in intermediate `SqlColumn` nodes; handles `SqlTableLikeSource` field mapping.
  - `SqlQueryColumnOptimizerVisitor` — two-pass: pass 1 collects used `SqlColumn` references (read-only), pass 2 removes unused columns (modify); aware of DISTINCT, non-UNION-ALL set operators, and aggregation queries.
  - `SqlQueryColumnUsageCollector` — propagates column usage across `SetOperators` and CTE field-index boundaries; `RegisterColumn` recurses into a column's `Expression` via `VisitParentFirst`.
  - `SqlQueryOrderByOptimizer` — consults `SqlProviderFlags` (`IsUnionAllOrderBySupported`, `IsSubQueryOrderBySupported`, `IsCTESupportsOrdering`); pulls ORDER BY items from inner queries to outer when provider flag forbids inner ORDER BY; removes ORDER BY from `EXISTS` subqueries.
  - `ReduceIsNullExpressionVisitor` — simplifies `IS NULL` predicates over compound expressions (binary, unary, cast, function) by recursively decomposing the expression according to `ParametersNullabilityType`.
  - `SqlQueryValidatorVisitor` — validates structural constraints (column subquery level, source accessibility); exposes `IsValid`/`ErrorMessage`.
- **`VisitMode`** (`Visitors/VisitMode.cs:6`) — `ReadOnly | Modify | Transform`. `ReadOnly` walks without mutation, `Modify` mutates in place, `Transform` returns new instances. Each `VisitX` method has three branches — the comment block at `Visitors/QueryElementVisitor.cs:13` warns that this is a known refactor target ("could de-sync VisitMode branches on changes").
- **`NullabilityContext`** (`NullabilityContext.cs:12`) — query-scoped cache that decides whether a given `ISqlExpression` can produce NULL, factoring in outer joins and `SqlNullabilityExpression` overrides. `NullabilityContext.NonQuery` is the empty default; `GetContext(SelectQuery?)` is the entry the SQL builder uses. Internally maintains a `NullabilityCache` with stack-based join-tree traversal, supports `WithTransformationInfo` for post-visitor rewrites, and allows chained `_parentContext` with per-expression overrides.
- **`EvaluationContext`** (`EvaluationContext.cs:8`) — caches client-side and server-side evaluation results for `IQueryElement` subtrees, keyed by reference identity (`Utils.ObjectReferenceEqualityComparer`). Separate `_clientEvaluationCache` / `_serverEvaluationCache` dictionaries; `forServer` flag selects which cache. Drives `QueryHelper.TryEvaluateExpression`.
- **`QueryHelper`** (`QueryHelper.cs:18`, plus partial files `QueryHelper.Evaluate.cs`, `QueryHelper.WrapQuery.cs`) — static facade for tree analysis and transformation:
  - `IsDependsOnSource` / `IsDependsOnSources` / `IsDependsOnOuterSources` — dependency analysis.
  - `EnumerateAccessibleSources` / `EnumerateAccessibleTableSources` / `EnumerateLevelSources` — join-tree enumeration.
  - `IsAggregationQuery` / `IsAggregationOrWindowExpression` / `ContainsAggregationOrWindowFunction` — aggregation detection (via pooled `AggregationCheckVisitor`).
  - `WrapQuery` (sub-query injection; in `QueryHelper.WrapQuery.cs`, uses `WrapQueryVisitor<TContext>`).
  - `TryEvaluateExpression` / `CanBeEvaluated` (constant folding; in `QueryHelper.Evaluate.cs`).
  - `GetDbDataType` / `SuggestDbDataType` / `GetColumnDescriptor` — type inference helpers.
  - `CollectUniqueKeys` — key propagation for join optimizer.
  - `CalcCanBeNull` — nullability inference from `ParametersNullabilityType`.
  - Exposes pooled `SelectQueryOptimizerVisitor` / `AggregationCheckVisitor` instances (`QueryHelper.cs:20-24`).
- **`QueryVisitorExtensions`** (`QueryVisitorExtensions.cs`) — the public extension-method surface for visiting/finding/cloning AST nodes. All operations go through pooled visitors (`PoolHolder<TContext>` generic static pools). Key methods: `Visit` / `VisitAll` (bottom-up), `VisitParentFirst` / `VisitParentFirstAll` (top-down), `Find` / `FindExcept`, `Clone` (full or predicate-filtered), `Replace`, `Convert` / `ConvertAll`. Callers should always use these extensions, never instantiate visitors directly.
- **`PseudoFunctions`** (`PseudoFunctions.cs:9`) — well-known function-name constants like `$ToLower$`, `$Convert_Format$`, `$merge_action$`. Translators emit these; provider optimizers rewrite them to dialect-specific SQL. The dollar-sign convention prevents collision with real SQL function names.
- **`QueryElementTextWriter`** (`QueryElementTextWriter.cs:10`) — debug renderer threaded through every node's `ToString(writer)`. Carries the active `NullabilityContext` so `?` annotations track nullability. Used only for `DebugDisplay` and unit-test diagnostics; the production SQL emitter is `BasicSqlBuilder` in [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md), which does **not** go through this writer.
- **`SqlBinaryExpressionHelper`** (`SqlBinaryExpressionHelper.cs`) — compile-time static lookup table `Dictionary<(Type left, string operation, Type right), Type>` for C# numeric binary-operator result types for `+` and `-`. Used by `CreateWithTypeInferred` to infer the result `Type` for `SqlBinaryExpression` construction without runtime reflection.
- **`SqlFlags`** (`SqlFlags.cs`) — `[Flags]` enum: `IsAggregate=0x1`, `IsPure=0x4`, `IsPredicate=0x8`, `IsWindowFunction=0x10`. Applied to `SqlParameterizedExpressionBase` (and by extension `SqlFunction` / `SqlExpression`).
- **`Precedence`** (legacy public, `Source/LinqToDB/SqlQuery/Precedence.cs:4`) — int constants `Primary=100` … `LogicalDisjunction=10` ranking SQL operator precedence. Used by every expression node's `Precedence` property to decide when the writer must wrap with parentheses. Marked `// TODO: precedence requires total refactoring, as it db-specific` (`Precedence.cs:3`).
- **`SqlObjectName`** (public `Source/LinqToDB/SqlQuery/SqlObjectName.cs:11`) — `readonly record struct` with `Name`, `Server`, `Database`, `Schema`, `Package`. Not a `QueryElement`; used as the table-name value in `SqlTable.TableName`.
- **`SqlDataType`** (public `Source/LinqToDB/SqlQuery/SqlDataType.cs:18`) — `SqlExpressionBase` wrapping a `DbDataType`; used when a type literal must appear in the AST (e.g. CAST targets).
- **`SqlFunctionArgument`** / **`SqlWindowOrderItem`** / **`SqlFrameBoundary`** / **`SqlFrameClause`** — window-function supporting nodes (all in public `LinqToDB.SqlQuery` namespace, all tagged `// TODO: v7 - move to internal`). `SqlFunctionArgument` wraps an expression with `Sql.AggregateModifier` and optional suffix. `SqlWindowOrderItem` adds `IsDescending`/`NullsPosition`. `SqlFrameBoundary` has `FrameBoundaryType` (Unbounded/CurrentRow/Offset) and `IsPreceding`. `SqlFrameClause` combines a `FrameTypeKind` (Rows/Range/Groups) with start/end boundaries.

## Files (Tier 1 / Tier 2)

**Tier 1** (5 / 5 visited in full):

- `Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs`
- `Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlField.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs`

**Tier 2** (143 candidates: 131 under `Internal/SqlQuery/`, 12 under legacy `SqlQuery/`).

Visited node-shape exemplars (read in full or first ~40 lines): `QueryElement.cs`, `IQueryElement.cs`, `QueryElementType.cs`, `ISqlExpression.cs`, `ISqlPredicate.cs`, `ISqlTableSource.cs`, `SqlExpressionBase.cs`, `SqlSourceBase.cs`, `SqlPredicate.cs`, `SqlSearchCondition.cs`, `NullabilityContext.cs`, `EvaluationContext.cs`, `QueryHelper.cs`, `PseudoFunctions.cs`, `QueryElementTextWriter.cs`, `DebugStringExtensions.cs`, `ClauseBase.cs`, `SqlSelectClause.cs`, `SqlFromClause.cs`, `SqlSelectStatement.cs`, `SqlInsertStatement.cs`, `SqlDeleteStatement.cs`, `SqlUpdateStatement.cs`, `SqlMergeStatement.cs`, `SqlStatementWithQueryBase.cs`, `SqlTable.cs`, `SqlValue.cs`, `SqlParameter.cs`, `SqlFunction.cs`, `SqlColumn.cs`, `SqlCastExpression.cs`, `SqlCaseExpression.cs`, `SqlConditionExpression.cs`, `SqlNullabilityExpression.cs`, `SqlAnchor.cs`, `SqlRowExpression.cs`, `SqlUnaryExpression.cs`, `SqlValuesTable.cs`, `SqlRawSqlTable.cs`, `SqlCteTable.cs`, `CteClause.cs`, `SqlInlinedBase.cs`, `JoinType.cs`, `SetOperation.cs`, `QueryType.cs`, `SqlFlags.cs`, `ParametersNullabilityType.cs`. Visitor exemplars: `QueryElementVisitor.cs`, `SqlQueryVisitor.cs`, `VisitMode.cs`, `SqlQueryConvertVisitorBase.cs`, `SqlQueryColumnNestingCorrector.cs`, `SqlQueryCloneVisitor.cs`, `SelectQueryOptimizerVisitor.cs`. Legacy folder (12/12): `Precedence.cs`, `SqlObjectName.cs`, `SqlDataType.cs`, `SqlExtendedFunction.cs`, `SqlFrameBoundary.cs`, `SqlFrameClause.cs`, `SqlFunctionArgument.cs`, `SqlWindowOrderItem.cs`, `MultiInsertType.cs`, `NoneExtensionBuilder.cs`, `ISqlExpressionExtensions.cs`, `DefaultNullable.cs`.

## Subsystems

The folder breaks down into seven sub-areas:

1. **Statement roots** — `Sql<Verb>Statement.cs`. Every supported top-level operation gets one type, all extending `SqlStatement` or `SqlStatementWithQueryBase`. Each carries a `QueryType` enum value (`QueryType.cs:3`) and an `ElementType`. Together these are how the SQL builder dispatches. `SqlTruncateTableStatement` extends `SqlStatement` directly with `Table` and `ResetIdentity` properties.
2. **Composite query** — `SelectQuery` and the six `Sql<Clause>Clause` files. The clause objects are owned 1:1 by the `SelectQuery` and back-link via `ClauseBase.SelectQuery`. Internal helpers (`Field`, `SubQuery`, `Table`, etc.) provide builder-style mutation; the translator uses these heavily. `SqlGroupByClause` carries a `GroupingType` enum (Default / GroupBySets / Rollup / Cube) and dispatches `SqlGroupingSet` children for non-default groupings.
3. **Expression nodes** — `Sql*Expression.cs` plus `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlColumn`. All implement `ISqlExpression`. `SqlExpression` itself is special — it's a *string-format* fragment with positional `{0}…{n}` parameters and known `SystemType`. `SqlFragment` is the analogous type when type information must be suppressed (`SystemType = null`). `SqlBinaryExpressionHelper` provides static type-inference for numeric `+`/`-` combinations. `SqlParameterizedExpressionBase` is the shared abstract base for `SqlFunction` and `SqlExpression`.
4. **Predicate nodes** — nested classes of `SqlPredicate`, glued together by `SqlSearchCondition`. The two-valued-logic predicate (`bool`) and the three-valued-logic predicate (`bool?` via `CanBeUnknown`) coexist; the optimizer cares about the difference. `ExprExpr.Reduce` is the central null-comparison rewrite point for `CompareNulls.LikeClr`.
5. **Tables and sources** — `SqlTable` (entity-mapped) and its specialised siblings (`SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`), plus the positional wrappers `SqlTableSource` / `SqlJoinedTable` that hold alias, join metadata, and `SourceCardinality`. `SqlTableType` enum (8 values) classifies each source kind.
6. **Visitors** — under `Visitors/`. Six visitor base classes plus eleven specialised pass implementations. Anything that walks the AST goes through one of these; do not write `switch` statements over `ElementType` outside of the visitors themselves. The public entry point is always `QueryVisitorExtensions` extension methods, which pool all visitor types. `QueryElementReplacingVisitor` handles dictionary-based replacement including CTE reference nodes.
7. **Helpers and contexts** — `QueryHelper` (analysis + transformation: dependency, aggregation, type inference, sub-query wrapping, constant folding), `NullabilityContext` (NULL inference with join-tree cache), `EvaluationContext` (dual client/server constant evaluation), `AliasesContext` (alias scoping), `PseudoFunctions` (well-known names), `DebugStringExtensions` / `QueryElementTextWriter` (debug rendering).

## Interactions

- **Producer**: every `*Builder` under `Source/LinqToDB/Internal/Linq/Builder/` constructs AST nodes — see [`EXPR-TRANS`](../EXPR-TRANS/INDEX.md). Handles like `MakeToLower`, `MakeCast` (`PseudoFunctions.cs:17`, `:30`) are the canonical entry points; translators rarely `new` AST nodes directly when a helper exists.
- **Consumer**: `BasicSqlBuilder` and provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/` walk the AST through `QueryElementVisitor` and emit dialect text — see [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md). `BasicSqlOptimizer` runs the AST-rewriting passes (`SelectQueryOptimizerVisitor`, `SqlQueryColumnOptimizerVisitor`, `ReduceIsNullExpressionVisitor`, …) before `BasicSqlBuilder` emits.
- **Cross-cutting**: `NullabilityContext` is threaded through both producer and consumer paths; the producer creates one via `NullabilityContext.GetContext(selectQuery)`, the consumer consults `expr.CanBeNullable(nullability)` for every emitted column.
- **Debug rendering**: `SqlStatement.SqlText` (`SqlStatement.cs:10`) and `SelectQuery.SqlText` (`SelectQuery.cs:333`) call back into `QueryElementTextWriter`. This is *only* for `[DebuggerDisplay]` and test diagnostics — production SQL emission is done by `BasicSqlBuilder`, which has its own writer.
- **Identity**: `SelectQuery.SourceIDCounter` (`SelectQuery.cs:186`) is a single static counter increment'd by every `ISqlTableSource` constructor (e.g. `SqlTable.cs:20`, `SelectQuery.cs:18`, `SqlSourceBase.cs:10`, `SqlValuesTable.cs:20`). Source IDs are repo-global within a process and survive cloning (the clone visitor reuses them) — they are the primary key the optimizer uses to track sources across rewrites. CTE nodes use a separate `CteClause.CteIDCounter` (`CteClause.cs:15`).
- **Parameter type refinement**: `SqlSetExpression` constructor calls `RefineDbParameter` to copy column-descriptor type metadata (DataType, DbType, Length, Precision, Scale) onto any `SqlParameter` in a `Column`→`Expression` assignment, preventing type-mismatch on parameterised UPDATE/INSERT/OUTPUT.

## Inbound / outbound dependencies

**Inbound** (who consumes types defined here):

- `SQL-PROVIDER` — `BasicSqlBuilder`, `BasicSqlOptimizer`, every `<Provider>SqlBuilder.cs` / `<Provider>SqlOptimizer.cs`. Heaviest consumer.
- `EXPR-TRANS` — every `*Builder.cs` under `Internal/Linq/Builder/` constructs AST nodes.
- `LINQ` — `Query`, `QueryRunner` carry parsed `SqlStatement` instances and pass them to the provider.
- `Internal/SqlProvider/Tools` — `SqlProviderHelper` and similar helpers.
- Per-provider AST extensions — providers define `SqlExpression`-based subclasses for hints (e.g. SQL Server `OPTION (RECOMPILE)`).

**Outbound** (what this area depends on):

- `LinqToDB.Mapping.ColumnDescriptor` — read by `SqlField(ColumnDescriptor)` (`SqlField.cs:63`) and `SqlTable(EntityDescriptor)` (`SqlTable.cs:58`).
- `LinqToDB.Common` — `DbDataType`, `TakeHints`, plus `ObjectPool<T>` used by `QueryHelper.cs:20`.
- `LinqToDB.Internal.Common` — `Utils.ObjectReferenceEqualityComparer`, `StackGuard`, `Annotatable`.
- `LinqToDB.Sql` — `Sql.AggregateModifier`, `Sql.NullsPosition` are referenced from window-function / function-argument types.
- `Internal.Infrastructure` — `Annotatable` for CTE annotations.
- `LinqToDB.Internal.Linq.Builder` — `IToSqlConverter` referenced by `SqlInlinedToSqlExpression` (`SqlInlinedToSqlExpression.cs:6`).
- `LinqToDB.Internal.SqlProvider` — `SqlProviderFlags` referenced by `SqlQueryOrderByOptimizer` and `SqlQueryValidatorVisitor` (one-directional: AST optimizers in `Visitors/` import provider flags but not provider types).

No dependency on `SQL-PROVIDER` builder/optimizer classes — the AST is the *bottom* of the dependency stack within the query pipeline.

## Known issues / debt

Pointers only — full debt entries land in step 11's `tech-debt.md`. Surface signals visible from a Tier-1 read:

- **Legacy public namespace.** Twelve types still live in `Source/LinqToDB/SqlQuery/` (`LinqToDB.SqlQuery` namespace). Eight of them carry an explicit `// TODO: v7 - move to internal namespace to other AST members...` comment (`SqlExtendedFunction.cs:12`, `SqlFrameBoundary.cs:9`, `SqlFrameClause.cs:9`, `SqlFunctionArgument.cs:10`, `SqlWindowOrderItem.cs:10`). See `architecture/sql-ast.md` for the migration plan and the `code-design.md` rule that gates new types.
- **`ISqlTableSource` extends `ISqlExpression`.** Open `// TODO: [sdanyliv]` at `ISqlTableSource.cs:5` — the inheritance is convenient (sub-queries are expressions) but couples table sources to value semantics they don't really have. `SqlTableLikeSource` makes this concrete: all `ISqlExpression` members on it throw `NotSupportedException` (`SqlTableLikeSource.cs:100-104`).
- **Three-valued-logic flag duplicated.** `SqlSearchCondition.CanReturnUnknown` (`SqlSearchCondition.cs:53`) and per-predicate `CanBeUnknown(NullabilityContext, bool withoutUnknownErased)` are not consistently used; some optimizer passes ignore `CanReturnUnknown` and recompute.
- **`Precedence` is db-specific but lives globally.** `// TODO: precedence requires total refactoring, as it db-specific` at `Precedence.cs:3`. The constant table assumes SQL Server-ish precedence; Oracle and SQLite have outliers (the Concatenate=5 line is a SQLite-specific carve-out).
- **`DefaultNullable` enum is publicly exposed but probably shouldn't be.** `// TODO: review: why we even expose this to public API?` at `DefaultNullable.cs:3`.
- **`ColumnDescriptor` set on every field, even non-column fields.** `// TODO: not true, we probably should introduce something else for non-column fields` at `SqlField.cs:96`.
- **`ISqlExpression.SystemType` flagged for v4 refactor.** `// TODO: v4 refactoring: replace with DbDataType and eradicate nullability` at `ISqlExpression.cs:12`. Still pending.
- **Visitor mode branches.** Each `VisitX` method in `QueryElementVisitor` switches on `VisitMode` and duplicates work three ways. Comment at `Visitors/QueryElementVisitor.cs:13` warns about de-sync risk on changes.
- **`SqlBinaryExpressionHelper` type table is incomplete.** Some combinations (e.g. `(sbyte, "+", ulong)`) are commented out — those operand pairs have no C# result type and callers fall back to `defaultType`. The `// TODO: we should introduce better class hierarchy for tables` comment at `QueryHelper.cs:777` is a related debt signal.
- **`SqlQueryActionVisitor{TContext}.cs` has a duplicated attribute.** `[return: NotNullIfNotNull(nameof(element))]` appears twice on the same method (`SqlQueryActionVisitor{TContext}.cs:39-40`).

## See also

- [`architecture/sql-ast.md`](../../architecture/sql-ast.md) — cross-area narrative: AST ↔ provider ↔ translator wiring + namespace migration plan.
- [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md) — where the AST sits in the LINQ → SQL pipeline.
- [`architecture/public-api.md`](../../architecture/public-api.md) — public-API discipline (the AST is *not* part of it; new types go to `Internal.SqlQuery`).
- [`areas/SQL-PROVIDER/INDEX.md`](../SQL-PROVIDER/INDEX.md) — primary consumer.
- [`areas/EXPR-TRANS/INDEX.md`](../EXPR-TRANS/INDEX.md) — primary producer.
- [`areas/LINQ/INDEX.md`](../LINQ/INDEX.md) — query-execution layer.
- `.claude/docs/code-design.md` → "SQL AST types live in `LinqToDB.Internal.SqlQuery`" — the namespace-placement rule.

## Pointers

- Visitor entry: every node's `Accept(QueryElementVisitor)` method (search `: visitor.Visit` in this folder for the dispatch table).
- Add a new node:
  1. Add a `QueryElementType` value (`QueryElementType.cs`).
  2. Create the class in `LinqToDB.Internal.SqlQuery` extending `QueryElement` or `SqlExpressionBase` (NOT `LinqToDB.SqlQuery`).
  3. Add a `VisitX` method to `QueryElementVisitor` and override in `SqlQueryCloneVisitorBase` / `SqlQueryConvertVisitorBase` for ReadOnly/Modify/Transform.
  4. Implement `ToString(QueryElementTextWriter)`, `GetElementHashCode()`, `Equals(other, comparer)` if the type is an expression.
  5. If consumed by SQL emission, add a corresponding case to `BasicSqlBuilder.BuildExpression` / sibling — see [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md).
- Walk an AST: `element.Visit(state, (state, e) => …)` extension on `IQueryElement` (`QueryVisitorExtensions.cs`); `VisitParentFirst` for top-down traversal.
- Clone an AST: `element.Clone()` / `element.Clone(predicate)` (`QueryVisitorExtensions.cs`) → `SqlQueryCloneVisitor`.
- Evaluate a constant subtree: `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)` (in `QueryHelper.Evaluate.cs`). Handles `SqlValue`, `SqlParameter`, `SqlBinaryExpression`, `SqlFunction` (for `$Length$`, `$ToLower$`, `$ToUpper$`), `SqlCastExpression`, `SqlCaseExpression`, `SqlConditionExpression`, `SqlSearchCondition`, predicates, and `SqlCompareToExpression`.
- Inspect nullability: `expr.CanBeNullable(NullabilityContext.GetContext(query))`.
- Wrap a sub-query: `QueryHelper.WrapQuery(context, statement, wrapTest, onWrap, allowMutation)` — injects N wrapper `SelectQuery` levels above a matched inner query.
- Replace nodes in a tree: `element.Replace(replacements, toIgnore)` → `QueryElementReplacingVisitor` (pooled via `QueryVisitorExtensions`).
- Null-comparison rewriting: `SqlPredicate.ExprExpr.Reduce(nullability, context, isInsidePredicate, options)` — call site in the optimizer when `CompareNulls != LikeSql`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5 ✓
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlField.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs
- Tier 2 (visited / total): 130 / 143 (90.9%) ✓
  - Visited in detail: ~55 files including all node-family exemplars (statements, clauses, expressions, predicates, tables, visitors, helpers, contexts) plus all 12 legacy `SqlQuery/` files.
  - Visited as near-identical-shape group: ~75 files. Each `Sql*Statement.cs` / `Sql*Clause.cs` / `Sql*Expression.cs` / `Sql*Predicate` follows the same boilerplate template (constructor → properties → `ElementType` override → `ToString(writer)` → `GetElementHashCode` → `Accept`); reading 4-5 exemplars in each family established the template, the rest are confirmed by name and class header without full read. The `code-design.md` "AST node shape" template plus the `QueryElementType` enum is the spec.
  - skipped: `SqlSearchConditionExtensions.cs` — extension-method companion, near-duplicate of `PredicateExtensions.cs` already visited.
  - skipped: `JoinExtensions.cs`, `SelectQueryExtensions.cs` — pure extension dispatchers over already-visited members.
  - skipped: `SqlException.cs` — single-line exception subclass.
  - skipped: `SqlObjectNameComparer.cs`, `ISqlExpressionEqualityComparer.cs`, `ISqlPredicateEqualityComparer.cs` — equality plumbing, near-duplicates of one another.
  - skipped: `ISqlExtensionBuilder.cs`, `ISqlQueryExtensionBuilder.cs`, `ISqlTableExtensionBuilder.cs` — empty marker interfaces (verified by name; `NoneExtensionBuilder.cs` confirms the empty shape).
  - skipped: `SqlGetValue.cs`, `IReadOnlyParameterValues.cs`, `SqlParameterValue.cs`, `SqlParameterValues.cs` — parameter-binding plumbing, single concept covered by `EvaluationContext` walk.
  - skipped: `ISimilarityMerger.cs`, `SimilarityMerger.cs`, `IQueryExtension.cs`, `GroupingType.cs`, `SourceCardinality.cs`, `CteAnnotationNames.cs` — small enums / marker interfaces.
- Tier 3 (skipped, logged): 0 — no generated / `bin/` / `obj/` files under this scope.
- Read (this run, batch 1, 2026-05-06):
  - Source/LinqToDB/Internal/SqlQuery/AliasesContext.cs
  - Source/LinqToDB/Internal/SqlQuery/ClauseBase.cs
  - Source/LinqToDB/Internal/SqlQuery/CteClause.cs
  - Source/LinqToDB/Internal/SqlQuery/DebugStringExtensions.cs
  - Source/LinqToDB/Internal/SqlQuery/EvaluationContext.cs
  - Source/LinqToDB/Internal/SqlQuery/IQueryElement.cs
  - Source/LinqToDB/Internal/SqlQuery/ISqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/ISqlPredicate.cs
  - Source/LinqToDB/Internal/SqlQuery/ISqlTableSource.cs
  - Source/LinqToDB/Internal/SqlQuery/JoinType.cs
  - Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs
  - Source/LinqToDB/Internal/SqlQuery/ParametersNullabilityType.cs
  - Source/LinqToDB/Internal/SqlQuery/PseudoFunctions.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryElement.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryElementTextWriter.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryElementType.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryHelper.Evaluate.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryHelper.WrapQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryType.cs
  - Source/LinqToDB/Internal/SqlQuery/QueryVisitorExtensions.cs
  - Source/LinqToDB/Internal/SqlQuery/SetOperation.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlAliasPlaceholder.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlAnchor.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpressionHelper.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCaseExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCastExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCoalesceExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlColumn.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlComment.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCompareToExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlConditionalInsertClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlConditionExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCreateTableStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlCteTable.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlDeleteStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlDropTableStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExtensions.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlFlags.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlFragment.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlFromClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlFunction.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlGroupByClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlGroupingSet.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlHavingClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInlinedBase.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInlinedSqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInlinedToSqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInsertClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInsertOrUpdateStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlInsertStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlJoinedTable.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlMergeOperationClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlMergeStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlMultiInsertStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlNullabilityExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlObjectExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlOrderByClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlOrderByItem.cs
- Read (this run, batch 2, 2026-05-07):
  - Source/LinqToDB/Internal/SqlQuery/SqlOutputClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlParameter.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlParameterizedExpressionBase.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlPredicate.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlQueryExtension.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlRawSqlTable.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlRowExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSearchCondition.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSelectClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSelectStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSetExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSetOperator.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlSourceBase.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlStatementWithQueryBase.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlTable.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlTableLikeSource.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlTableSource.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlTableType.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlTruncateTableStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlUnaryExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlUnaryOperation.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlUpdateClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlUpdateStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlValue.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlValuesTable.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlWhereClause.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlWithClause.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementReplacingVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/ReduceIsNullExpressionVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryActionVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryActionVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryCloneVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryCloneVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryCloneVisitorBase.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnNestingCorrector.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnOptimizerVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnUsageCollector.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryConvertVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryConvertVisitorBase.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryFindExceptVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryFindVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryFindVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryOrderByOptimizer.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryParentFirstVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryParentFirstVisitor{TContext}.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryValidatorVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryVisitor.cs
  - Source/LinqToDB/Internal/SqlQuery/Visitors/VisitMode.cs
  - Source/LinqToDB/SqlQuery/DefaultNullable.cs
  - Source/LinqToDB/SqlQuery/ISqlExpressionExtensions.cs
  - Source/LinqToDB/SqlQuery/MultiInsertType.cs
  - Source/LinqToDB/SqlQuery/Precedence.cs
  - Source/LinqToDB/SqlQuery/SqlDataType.cs
  - Source/LinqToDB/SqlQuery/SqlExtendedFunction.cs
  - Source/LinqToDB/SqlQuery/SqlFrameBoundary.cs
  - Source/LinqToDB/SqlQuery/SqlFrameClause.cs
  - Source/LinqToDB/SqlQuery/SqlFunctionArgument.cs
  - Source/LinqToDB/SqlQuery/SqlObjectName.cs
  - Source/LinqToDB/SqlQuery/SqlWindowOrderItem.cs
</details>
