---
area: SQL-AST
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 5/5
coverage_tier_2: 148/150
---

# SQL-AST

Internal abstract syntax tree representing every SQL query linq2db can build. Lives almost entirely in the `LinqToDB.Internal.SqlQuery` namespace under `Source/LinqToDB/Internal/SqlQuery/`. The AST is the *contract* between three subsystems: the [expression translator](../EXPR-TRANS/INDEX.md) (producer), the [SQL provider](../SQL-PROVIDER/INDEX.md) (consumer that emits dialect text), and the [query runner](../LINQ/INDEX.md) (which materialises results).

Every node implements `IQueryElement` (`Source/LinqToDB/Internal/SqlQuery/IQueryElement.cs:8`). Concrete nodes derive from `QueryElement` (`Source/LinqToDB/Internal/SqlQuery/QueryElement.cs:11`) or, when they participate as expressions, from `SqlExpressionBase` which adds `ISqlExpression` semantics (`Source/LinqToDB/Internal/SqlQuery/SqlExpressionBase.cs:5`). The single dispatch entry is `IQueryElement.Accept(QueryElementVisitor)`, which every node implements as one line -- `visitor.Visit<TypeName>(this)` (e.g. `Source/LinqToDB/Internal/SqlQuery/SqlField.cs:186`).

## Key types

- **`IQueryElement`** (`IQueryElement.cs:8`) -- root interface; exposes `ElementType` (a tagged `QueryElementType` enum), debug `ToString(QueryElementTextWriter)`, `Accept(QueryElementVisitor)` and `GetElementHashCode()`. The hash is *structural*, not identity-based.
- **`QueryElementType`** (`QueryElementType.cs:6`) -- tagged-union discriminator. Every concrete node returns its constant from `ElementType`. `SqlConcat` was appended at the end of the enum (after `SqlFrameBoundary`) specifically to preserve v6.x `LinqService` wire-compatibility -- enum ordinals are serialised as `int` on the wire, so inserting mid-enum would shift all subsequent values. A later delta tail-appended three more members for the identical reason: `SqlCteField` / `SqlCteTableField` (logically belong next to `SqlCteTable`) and `SqlKeepClause` (logically belongs next to `SqlCast`/`SqlCoalesce`) -- `QueryElementType.cs:119-128`.
- **`ISqlExpression`** -- the value-producing subset; adds `Precedence`, `SystemType`, `CanBeNullable(NullabilityContext)`, and a `comparer`-driven `Equals` overload.
- **`ISqlPredicate`** -- the boolean subset; adds `CanInvert` / `Invert(NullabilityContext)` and `CanBeUnknown` to express SQL three-valued logic.
- **`ISqlTableSource`** -- anything usable in `FROM` / join. Carries an integer `SourceID` (allocated from `SelectQuery.SourceIDCounter`). Implementations: `SqlTable`, `SelectQuery`, `SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`. There's a `// TODO: [sdanyliv] ISqlTableSource why it extends ISqlExpression?` at `ISqlTableSource.cs:5`.
- **`ISqlNamedTable`** (`Source/LinqToDB/Internal/SqlQuery/ISqlNamedTable.cs`) -- NEW. `ISqlTableSource` + `TableName` (`SqlObjectName`). Implemented by `SqlTable` and `SqlCteTable`, replacing several statement/clause properties that used to be hard-typed to `SqlTable`: `SqlDeleteStatement.Table`, `SqlUpdateClause.Table`, `SqlOutputClause.OutputTable`, `SqlFromClause.FindTableSource`, and the `QueryHelper` helpers `HasTableInQuery`/`IsSingleTableInQuery`/`EnumerateAccessibleTables`/`ExtractSqlTable`/`IsEqualTables`. This decouples those sites from `SqlTable` now that `SqlCteTable` no longer subclasses it (see Table-shaped nodes below).
- **`SqlStatement`** -- abstract root of any executable statement; exposes `QueryType` and the optional carried `SelectQuery`. Concrete `Sql<Verb>Statement` types extend either `SqlStatement` directly or `SqlStatementWithQueryBase`.
- **`SelectQuery`** -- the central composite node, aggregates the six clauses (`Select`/`From`/`Where`/`GroupBy`/`Having`/`OrderBy`) plus optional `SetOperators` and `UniqueKeys`. Implements `ISqlTableSource`.
- **`SqlField`** -- column reference. Constructed either bare or from a `ColumnDescriptor`. `SqlField.All(table)` produces the `*` placeholder. Now extends the new `SqlFieldBase` (shared with `SqlCteTableField`) rather than `SqlExpressionBase` directly; the dead `Alias` property was removed (nothing read it except `SqlColumn.GetAlias`, which now uses `PhysicalName` directly). `GetElementHashCode` switched from a value-based hash (Name/PhysicalName/etc.) to an identity-based one (`RuntimeHelpers.GetHashCode`) -- Name/PhysicalName are mutated in place by aliasing passes, so a value-based hash could go stale for instances already stored in hash-based collections keyed by `ISqlExpressionEqualityComparer`.
- **`SqlFieldBase`** (`Source/LinqToDB/Internal/SqlQuery/SqlFieldBase.cs`) -- NEW abstract base shared by `SqlField` and `SqlCteTableField`: `Type`, `Name`, abstract `NamedTable` (the owning `ISqlNamedTable`, if any), and a reference-equality `Equals` override.
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
  - `SqlExtendedFunction` (`Source/LinqToDB/Internal/SqlQuery/SqlExtendedFunction.cs`) -- the window/analytic function node. Moved here from the public `LinqToDB.SqlQuery` namespace in this delta (partially resolving the long-standing v7-migration TODO). Gained `KeepClause` (`SqlKeepClause?`, Oracle `KEEP (DENSE_RANK FIRST/LAST ORDER BY ...)`), `NullTreatment` (`Sql.Nulls`, `RESPECT`/`IGNORE NULLS`), and `FromPosition` (`Sql.From`, `FROM FIRST`/`FROM LAST`) -- all three feed into `IsWindowFunction`/`ToString`/`Equals`/`GetElementHashCode` and the matching `With*` builder methods.
  - `SqlFunctionArgument` / `SqlWindowOrderItem` -- window-function argument/order-item nodes, still public in `LinqToDB.SqlQuery`.
  - `SqlQueryExtension` -- provider extension node embedded in a query.
- **`SqlKeepClause`** (`Source/LinqToDB/Internal/SqlQuery/SqlKeepClause.cs`) -- NEW. Oracle-style `KEEP (DENSE_RANK FIRST|LAST ORDER BY ...)` analytic-function modifier; born directly in `LinqToDB.Internal.SqlQuery` (not part of the legacy-namespace migration). Structural `Equals`/`GetHashCode` delegate to the comparer-based overload (mirrors `SqlExtendedFunction`'s child-clause comparisons) to avoid hash-collision false-equality.
- **Predicate nodes** -- nested classes of `SqlPredicate`: `Not`, `TruePredicate`, `FalsePredicate`, `Expr`, `BaseNotExpr`, `ExprExpr`, `Like`, `SearchString`, `Between`, `IsNull`, `IsDistinct`, `IsTrue`, `InSubQuery`, `InList`, `Exists`, plus the `Operator` enum.
  - `ExprExpr.Reduce(NullabilityContext, EvaluationContext, bool isInsidePredicate, LinqOptions)` is where `CompareNulls.LikeClr` null-comparison rewrites are applied.
  - `SqlSearchCondition` -- the `AND`/`OR`-of-predicates container; `IsOr` toggles the connective, `CanReturnUnknown` carries the three-valued-logic flag.
- **Clause nodes** -- `SqlSelectClause`, `SqlFromClause`, `SqlWhereClause`, `SqlGroupByClause`, `SqlHavingClause`, `SqlOrderByClause`, `SqlInsertClause`, `SqlUpdateClause`, `SqlOutputClause`, `SqlConditionalInsertClause`, `SqlMergeOperationClause`. All extend `ClauseBase` which holds a back-pointer to the owning `SelectQuery`.
  - **`SqlOrderByClause`** -- `Expr()` now has an overload accepting `Sql.NullsPosition nullsPosition`; the no-position convenience overloads default to `Sql.NullsPosition.None`.
  - **`SqlSelectClause`** -- gained `DistinctOn` (`List<ISqlExpression>?`) and `IsDistinctOn` for PostgreSQL/DuckDB `DISTINCT ON (...)` queries: the listed expressions form the distinct key and (per key) the row that sorts first under the query's `ORDER BY` (which must begin with these expressions) survives. Callers setting `DistinctOn` must also set `IsDistinct = true`. Threaded through `QueryElementVisitor.VisitSqlSelectClause` (all three `VisitMode` branches), `GetElementHashCode`, `Cleanup`, and debug `ToString` (renders `SELECT DISTINCT ON (...)`).
- **Table-shaped nodes** -- `SqlTable` (entity-mapped) and `SqlCteTable` (CTE reference) both implement the new `ISqlNamedTable` directly -- `SqlCteTable` no longer subclasses `SqlTable` (see below). Plus `SqlRawSqlTable`, `SqlValuesTable` (`VALUES`-list), `SqlTableSource` (positional wrapper holding alias + joins), `SqlJoinedTable`, `SqlTableLikeSource` (used as MERGE/MultiInsert source).
  - **`SqlTableSource`** -- implements `Deconstruct(out ISqlTableSource source)` for pattern-matching deconstruction.
  - **`SqlTable`**'s entity-descriptor constructor (`SqlTable(EntityDescriptor, string?)`) gained inheritance-sibling-column dedup: when two sibling types in a TPH/TPT hierarchy map distinct members to the same physical column, only one `SqlField` is emitted for it (matched by `PhysicalName`), keyed under a synthetic name (`ColumnName` or `ColumnName:MemberName` on collision) so both siblings' members can still resolve it via `TableContext.GetField`.
- **SET clause helper** -- `SqlSetExpression` -- a `Column`/`Expression` pair used in UPDATE `SET` and `OUTPUT ... INTO`. Constructor calls `RefineDbParameter` to propagate column-descriptor type metadata.
- **SET operator** -- `SqlSetOperator` -- wraps a `SelectQuery` + `SetOperation` enum.
- **`SqlTableType`** enum -- `Table`, `SystemTable`, `Function`, `Expression`, `Cte`, `RawSql`, `MergeSource`, `Values`.
- **CTE infrastructure** -- `CteClause`, `SqlWithClause`, `SqlCteTable`. `CteClause` carries an open-ended `Annotations` bag and a static `CteIDCounter` separate from `SourceID`. CTE field data is a two-sided reference pair, both new types extending `SqlFieldBase`: **`SqlCteField`** (`Source/LinqToDB/Internal/SqlQuery/SqlCteField.cs`) is the schema-level field descriptor owned by `CteClause.Fields` (`List<SqlCteField>`, was `List<SqlField>`) -- it holds a direct `Column` reference into `CteClause.Body.Select.Columns`, used during recursive-CTE construction before the body is fully built. **`SqlCteTableField`** (`Source/LinqToDB/Internal/SqlQuery/SqlCteTableField.cs`) is the reference-site field owned by `SqlCteTable.Fields` (`List<SqlCteTableField>`, was `List<SqlField>`) -- it delegates `Name`/`Type`/`CanBeNullable` to its `CteField` back-reference. `SqlCteTable.GetKeys` now matches fields by direct-reference identity (`ReferenceEquals(f.CteField, cteField)`) with a name-string fallback for construction paths that haven't wired the direct reference yet.
- **`SqlOrderByItem`** -- ORDER BY item node. Carries a `NullsPosition` property (`Sql.NullsPosition`); constructor overload `SqlOrderByItem(expr, isDescending, isPositioned, nullsPosition)`. `ToString` renders `NULLS FIRST`/`NULLS LAST` suffix when `NullsPosition != None`. `GetElementHashCode` includes `NullsPosition`.
- **`NullsDefaultOrdering`** (`Source/LinqToDB/Internal/SqlQuery/NullsDefaultOrdering.cs:8`) -- public enum describing how a provider places NULLs in ORDER BY when no `NULLS FIRST`/`NULLS LAST` is specified. Values: `Unknown` (always emit the requested position), `Smallest` (SQL Server, MySQL, SQLite, Firebird), `Largest` (Oracle, PostgreSQL, DB2), `AlwaysFirst`, `AlwaysLast` (ClickHouse). Used by `QueryHelper.MatchesNaturalNullsPosition` and `SqlNullsOrderingLoweringVisitor` to elide emulated CASE sort keys when the provider's natural ordering already matches the request.
- **Visitor base classes** -- `QueryElementVisitor` is the abstract dispatcher with one `VisitX` method per node type. This delta adds `VisitSqlCteField`/`VisitSqlCteTableField` (dispatched from `VisitCteClause`/`VisitSqlCteTable`) and `VisitSqlKeepClause`, plus protected `CopyCteFields`/`CopyCteTableFields` helpers alongside the pre-existing `CopyFields`. It also fixes three latent copy/paste bugs found while touching these paths: `VisitSqlDropTableStatement`'s `Transform` branch built a `SqlCreateTableStatement` instead of `SqlDropTableStatement`; `VisitSqlDeleteStatement`'s `ReadOnly` branch visited `element.Table` a second time instead of `element.Top`; `VisitSqlCastExpression`'s `ReadOnly` branch double-visited `element.FromType` (`Visit(Visit(element.FromType))`). Subclasses:
  - `SqlQueryVisitor` -- adds replacement tracking via `IVisitorTransformationInfo`; `GetVisitMode` promotes already-replaced nodes from `Transform` -> `Modify`.
  - `SqlQueryCloneVisitorBase` / `SqlQueryCloneVisitor` -- deep clone with optional predicate filter. `VisitSqlCteTable` now clones CTE fields via `CopyCteFields`/`CopyCteTableFields` and re-resolves the `SqlCteField.Column` / `SqlCteTableField.CteField` cross-references post-clone (via `Visit`) so the direct-reference graph stays internally consistent in the cloned tree -- a naive per-field clone would otherwise leave stale pointers into the pre-clone object graph.
  - `SqlQueryConvertVisitorBase` / `SqlQueryConvertVisitor<TContext>` -- tree rewriting; optionally maintains a `Stack<IQueryElement>` for parent access via `WithStack`/`ParentElement`. **`ParentElement`** returns `Stack[^2]` (the true parent, not `Stack[^1]` which is self) -- fixed in PR #5451 (DuckDB). The `Convert<T>(element, convertAction, withStack: true)` overload on `QueryVisitorExtensions` also previously threw `NotImplementedException`; that guard was removed in the same PR so stack-enabled convert is now fully operational.
  - `SqlQueryActionVisitor` / `SqlQueryActionVisitor<TContext>` -- bottom-up read-only walk.
  - `SqlQueryFindVisitor` / `SqlQueryFindVisitor<TContext>` -- first-match search.
  - `SqlQueryFindExceptVisitor<TContext>` -- first-match search excluding a specified subtree.
  - `SqlQueryParentFirstVisitor` -- top-down walk.
  - `QueryElementReplacingVisitor` -- `Modify`-mode replacement using an `IDictionary<IQueryElement,IQueryElement>` map.
  - `SelectQueryOptimizerVisitor` -- the pooled, stateful optimizer the SQL builder runs before emission. Embeds six sub-visitors as fields, plus `SqlNullsOrderingLoweringVisitor` for pre-optimizer NULLS lowering. PR #5504: overrides `VisitSqlConcatExpression` to push `_isSubqueryInsideCondition = true` across the operands (same treatment as coalesce). PR #5522: extended `IsColumnExpressionAllowedToMoveUp` to handle `SqlConcatExpression`. This delta threads `SqlSelectClause.IsDistinctOn` through nearly every subquery-merge decision (`OptimizeDistinct`, `OptimizeApplyJoin`, `MoveSubQueryUp`, `MoveTablesToSubQuery`, `JoinMoveSubQueryUp`, `IsMovingUpValid`, the EXISTS-predicate simplifier) so a `DISTINCT ON` query is never folded/merged the way a plain `DISTINCT` is, and migrates `DistinctOn` alongside `IsDistinct` whenever a SELECT's modifiers move between queries. Set-operator column reordering was reworked: `UpdateSetIndexes`/`CheckSetColumns` were replaced by `TryBuildOuterColumnIndexes`/`TryReorderSetColumns`, plus two new peer-coalescing passes, `TryCoalescePeerSetOperators`/`TryLiftSetOperatorsFromOperandFromSubquery`. `SqlCteTableField` now participates alongside `SqlField`/`SqlColumn` in the "cheap to duplicate/move" expression checks used by `MoveSubQueryUp`/`JoinMoveSubQueryUp`/`IsMovingUpValid`.
  - `SqlQueryColumnNestingCorrector` -- builds a private `QueryNesting` tree tracking query-containment depth. PR #5556: `GetVisitMode` returns `VisitMode.Transform` for all expression-shaped node types while leaving container-shaped nodes (statements, `SelectQuery`, clauses, table sources) at `VisitMode.Modify`. This delta adds `VisitSqlCteTableField`, routing CTE-table-field nesting correction through `ProcessNesting` the same way `VisitSqlFieldReference` handles `SqlField`.
  - `SqlQueryColumnOptimizerVisitor` -- two-pass: pass 1 collects used `SqlColumn` references, pass 2 removes unused columns. `OptimizeColumns` now takes a `MappingSchema mappingSchema` parameter, threaded into a rewritten `SynchronizeCteFields`: CTE fields are matched to surviving body columns by direct `SqlCteField.Column` reference (falling back to positional index only when `Column` is unset) instead of pure index matching, and when no field survives, a properly-typed placeholder is synthesized via `QueryHelper.GetDbDataType(firstColumn.Expression, mappingSchema)` rather than the previous hardcoded `new SqlField(new DbDataType(typeof(int)), "c1", false)` dummy.
  - `SqlQueryColumnUsageCollector` -- propagates column usage across `SetOperators` and CTE field-index boundaries. Adds `VisitSqlCteTableField`, registering the referenced `CteField.Column` directly rather than re-deriving it by scanning `cte.Cte.Fields` by index.
  - `SqlQueryOrderByOptimizer` -- consults `SqlProviderFlags`; pulls ORDER BY items from inner queries to outer when provider flag forbids inner ORDER BY. Fixes #5626: an ORDER BY item can now promote above a `DISTINCT`/`GROUP BY` parent when it is a *computed* expression built entirely from columns/fields the parent already projects (`AllOrderColumnsProducedByDistinct`) or groups by (`AllOrderColumnsAreGroupingKeys`, plain `GROUP BY` only -- not ROLLUP/CUBE/GROUPING SETS), not just an exact-expression match as before. `CanRemoveOrderBy` now also refuses to strip or redistribute the ORDER BY of a `DISTINCT ON` query -- its ordering is semantically load-bearing (it selects which row per key survives).
  - **`SqlNullsOrderingLoweringVisitor`** (`Source/LinqToDB/Internal/SqlQuery/Visitors/SqlNullsOrderingLoweringVisitor.cs:14`) -- lowers `Sql.NullsPosition` on regular ORDER BY items into an explicit `CASE WHEN expr IS NULL THEN 0/1 END` emulation sort key for providers lacking native `NULLS FIRST`/`NULLS LAST` support. Runs before the query optimizer. Window `OVER (ORDER BY ...)` ordering is emulated separately at SQL-build time and is intentionally not touched here.
  - `ReduceIsNullExpressionVisitor` -- simplifies `IS NULL` predicates over compound expressions. Handles `SqlConcatExpression`: null-propagating concats (`PreserveNull = true`) reduce each operand via `ReduceOrAdd`; non-propagating concats are not nullable and never reach this reducer.
  - `SqlQueryValidatorVisitor` -- validates structural constraints.
- **`VisitMode`** -- `ReadOnly | Modify | Transform`. Each `VisitX` method has three branches.
- **`NullabilityContext`** -- query-scoped cache that decides whether a given `ISqlExpression` can produce NULL. `CanBeNull` gained a `_visitedColumns` cycle guard (`HashSet<SqlColumn>`) against infinite recursion when a column's expression chain references itself, directly or through other columns -- non-nullable-typed self-references return `false`, everything else conservatively returns `true`. `NonQuery` is now a computed property returning a fresh instance per call instead of a shared static singleton, avoiding shared mutable cache state (`_nullabilityCache`/`_visitedColumns`) across threads. Also gained a `SqlCteTableField` case (delegates to `CteField.CanBeNullable`, or `true` if the table/field link is missing).
- **`EvaluationContext`** -- caches client-side and server-side evaluation results for `IQueryElement` subtrees.
- **`QueryHelper`** -- static facade for tree analysis and transformation: `IsDependsOnSource`, `EnumerateAccessibleSources`, `IsAggregationQuery`, `WrapQuery`, `TryEvaluateExpression`, `GetDbDataType` / `SuggestDbDataType` / `GetColumnDescriptor`, `CollectUniqueKeys`, `CalcCanBeNull`. `GetColumnDescriptor`, `SuggestDbDataType`, and `GetDbDataType` gained `HashSet`-based recursion guards (`alreadyVisitedElements`/`visited`) against infinite loops on self-referential AST shapes, mirroring the `NullabilityContext.CanBeNull` fix above. All three, plus the dependency/usage collectors (`IsDependsOnSources`, `IsDependsOnOuterSources`, `CollectDependencies`, `CollectUsedSources`, `GetUsedSources`) and `EnsureFindTables`, gained `SqlCteTableField`/`SqlCteField` dispatch cases. `HasTableInQuery`/`IsSingleTableInQuery`/`EnumerateAccessibleTables`/`ExtractSqlTable` widened from `SqlTable` to `ISqlNamedTable`; `IsEqualTables` gained an `ISqlNamedTable`-typed overload delegating to the existing `SqlTable`-typed one. `CollectUniqueKeys` no longer treats a `DISTINCT ON` query's full projection as a unique key -- `DISTINCT ON` only guarantees uniqueness on the ON tuple, not the whole SELECT list.
- **`QueryVisitorExtensions`** -- the public extension-method surface for visiting/finding/cloning AST nodes. All operations go through pooled visitors. Key methods: `Visit` / `VisitAll`, `VisitParentFirst`, `Find` / `FindExcept`, `Clone`, `Replace`, `Convert` / `ConvertAll`. Two pools exist for `SqlQueryConvertVisitor<TContext>`: `ConvertPool` (immutable/Transform mode) and `ConvertMutationPool` (`allowMutation: true` / Modify mode).
- **`PseudoFunctions`** -- well-known function-name constants like `$ToLower$`, `$Convert_Format$`, `$merge_action$`.
- **`QueryElementTextWriter`** -- debug renderer threaded through every node's `ToString(writer)`.
- **`SqlBinaryExpressionHelper`** -- compile-time static lookup table for C# numeric binary-operator result types for `+` and `-`.
- **`SqlFlags`** -- `[Flags]` enum: `IsAggregate=0x1`, `IsPure=0x4`, `IsPredicate=0x8`, `IsWindowFunction=0x10`.
- **`Precedence`** (legacy public) -- int constants ranking SQL operator precedence. `Concatenate = 5` (PR #5504) -- conservative low-binding value below all other operators.
- **`SqlObjectName`** (public) -- `readonly record struct` with `Name`, `Server`, `Database`, `Schema`, `Package`.
- **`SqlDataType`** (public) -- `SqlExpressionBase` wrapping a `DbDataType`. Static fields include `DbDecFloat`/`DbTimeTZ` for provider-specific decimal/time-with-timezone types.
- **`SqlFunctionArgument`** / **`SqlWindowOrderItem`** / **`SqlFrameBoundary`** -- window-function supporting nodes still in the public `LinqToDB.SqlQuery` namespace, each tagged `// TODO: v7 - move to internal`. **`SqlFrameBoundary`** gained a comparer-based structural `Equals(SqlFrameBoundary other, Func<ISqlExpression,ISqlExpression,bool> comparer)` overload. **`SqlFrameClause`** (`Source/LinqToDB/Internal/SqlQuery/SqlFrameClause.cs`) moved to `Internal.SqlQuery` in this delta (partially resolving the v7-migration TODO -- see Known issues) and gained `FrameExclusionKind` (`None | CurrentRow | Group | Ties`, rendered as `EXCLUDE CURRENT ROW`/`EXCLUDE GROUP`/`EXCLUDE TIES`) plus a comparer-based structural `Equals` overload; its `Modify(SqlFrameBoundary start, SqlFrameBoundary end)` mutator is unchanged.

## Files (Tier 1 / Tier 2)

**Tier 1** (5 / 5 visited in full):

- `Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs`
- `Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlField.cs`
- `Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs`

**Tier 2** (150 candidates: 140 under `Internal/SqlQuery/`, 10 under legacy `SqlQuery/`). This delta (2026-07-05) adds 5 new files under `Internal/SqlQuery/` (`ISqlNamedTable.cs`, `SqlCteField.cs`, `SqlCteTableField.cs`, `SqlFieldBase.cs`, `SqlKeepClause.cs`) and relocates `SqlExtendedFunction.cs`/`SqlFrameClause.cs` from the legacy `SqlQuery/` folder into `Internal/SqlQuery/` (namespace `LinqToDB.SqlQuery` -> `LinqToDB.Internal.SqlQuery`, same file count).

Visited exemplars and full files: 148/150 (98.7%). See coverage block.

## Subsystems

The folder breaks down into seven sub-areas:

1. **Statement roots** -- `Sql<Verb>Statement.cs`. Every supported top-level operation gets one type, all extending `SqlStatement` or `SqlStatementWithQueryBase`. `SqlInsertOrUpdateStatement` gained `UpdateWhere` (`SqlSearchCondition?`) -- the conditional-upsert predicate, e.g. the `WHERE` on `ON CONFLICT ... DO UPDATE SET ... WHERE <cond>` (PostgreSQL/SQLite) or `WHEN MATCHED AND <cond>` (a MERGE-based emitter), populated by `UpsertBuilder` from `.Update(v => v.When(...))`. `SqlMergeStatement` gained an `ISqlTableSource`-typed constructor overload alongside the pre-existing `SqlTable`-typed one.
2. **Composite query** -- `SelectQuery` and the six `Sql<Clause>Clause` files. The clause objects are owned 1:1 by the `SelectQuery` and back-link via `ClauseBase.SelectQuery`. `SqlGroupByClause` carries a `GroupingType` enum (Default / GroupBySets / Rollup / Cube). `SqlSelectClause` gained `DistinctOn`/`IsDistinctOn` for PostgreSQL/DuckDB `DISTINCT ON (...)`.
3. **Expression nodes** -- `Sql*Expression.cs` plus `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlColumn`. All implement `ISqlExpression`. `SqlParameterizedExpressionBase` is the shared abstract base for `SqlFunction` and `SqlExpression`. `SqlConcatExpression` (PR #5504) joins this group as the canonical string-concatenation node. `SqlExtendedFunction` moved here from the public `LinqToDB.SqlQuery` namespace and gained `KeepClause`/`NullTreatment`/`FromPosition` analytic modifiers (KEEP/IGNORE-RESPECT-NULLS/FROM-FIRST-LAST).
4. **Predicate nodes** -- nested classes of `SqlPredicate`, glued together by `SqlSearchCondition`. `ExprExpr.Reduce` is the central null-comparison rewrite point for `CompareNulls.LikeClr`.
5. **Tables and sources** -- `SqlTable` and `SqlCteTable` both implement the new `ISqlNamedTable` interface (`ISqlTableSource` + `TableName`) rather than `SqlCteTable` subclassing `SqlTable` as before. CTE field data splits into schema-level `SqlCteField` (owned by `CteClause.Fields`, direct `Column` reference into the CTE body's SELECT list) and reference-site `SqlCteTableField` (owned by `SqlCteTable.Fields`, direct `CteField` back-reference) -- both extend `SqlFieldBase`, the base now also shared with `SqlField`. Field lookup upgraded from `SqlField`-based name-string matching to reference-identity matching with a name fallback. `ISqlNamedTable` also widened several statement/clause `Table` properties away from a hard `SqlTable` dependency: `SqlDeleteStatement.Table`, `SqlUpdateClause.Table`, `SqlOutputClause.OutputTable`, `SqlFromClause.FindTableSource`. Plus the positional wrappers `SqlTableSource` / `SqlJoinedTable`. `SqlTableType` enum (8 values) classifies each source kind.
6. **Visitors** -- under `Visitors/`. Six visitor base classes plus twelve specialised pass implementations. Anything that walks the AST goes through one of these. The public entry point is always `QueryVisitorExtensions` extension methods. PR #5556 introduced a selective-`Transform`-mode policy in `SqlQueryColumnNestingCorrector.GetVisitMode`. This delta's CTE-field-reference redesign (see Key types / Tables and sources) touches most of them: `QueryElementVisitor`, `SqlQueryCloneVisitor`, `SqlQueryColumnNestingCorrector`, `SqlQueryColumnOptimizerVisitor`, and `SqlQueryColumnUsageCollector` all gained CTE-field-aware overrides, and `QueryElementVisitor` also picked up three latent copy/paste bug fixes (see Key types) while its Transform paths were being exercised for the new field types.
7. **Helpers and contexts** -- `QueryHelper`, `NullabilityContext`, `EvaluationContext`, `AliasesContext`, `PseudoFunctions`, `DebugStringExtensions` / `QueryElementTextWriter`. `QueryHelper.GetColumnDescriptor`/`SuggestDbDataType`/`GetDbDataType` gained `HashSet`-based recursion guards against infinite loops on self-referential AST shapes, mirroring the same fix applied to `NullabilityContext.CanBeNull` via `_visitedColumns`.

## Interactions

- **Producer**: every `*Builder` under `Source/LinqToDB/Internal/Linq/Builder/` constructs AST nodes -- see [`EXPR-TRANS`](../EXPR-TRANS/INDEX.md). Handles like `MakeToLower`, `MakeCast` are the canonical entry points. String-concatenation in LINQ now produces `SqlConcatExpression` nodes rather than `SqlBinaryExpression("+")` chains.
- **Consumer**: `BasicSqlBuilder` and provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/` walk the AST through `QueryElementVisitor` and emit dialect text. `BasicSqlOptimizer` runs the AST-rewriting passes before `BasicSqlBuilder` emits. Providers must handle `QueryElementType.SqlConcat` and `QueryElementType.SqlKeepClause` in their builder's dispatch, and `SqlSelectClause.DistinctOn`/`IsDistinctOn` when emitting `SELECT`. `SqlNullsOrderingLoweringVisitor` runs as a pre-optimizer pass when a provider lacks native `NULLS FIRST`/`NULLS LAST` support; the `NullsDefaultOrdering` value is supplied by the provider via `SqlProviderFlags`.
- **Cross-cutting**: `NullabilityContext` is threaded through both producer and consumer paths.
- **Debug rendering**: `SqlStatement.SqlText` and `SelectQuery.SqlText` call back into `QueryElementTextWriter`. This is *only* for `[DebuggerDisplay]` and test diagnostics -- production SQL emission is done by `BasicSqlBuilder`.
- **Identity**: `SelectQuery.SourceIDCounter` is a single static counter increment'd by every `ISqlTableSource` constructor. Source IDs are repo-global within a process and survive cloning.
- **Parameter type refinement**: `SqlSetExpression` constructor calls `RefineDbParameter` to propagate column-descriptor type metadata onto any `SqlParameter`.

## Inbound / outbound dependencies

**Inbound** (who consumes types defined here):

- `SQL-PROVIDER` -- `BasicSqlBuilder`, `BasicSqlOptimizer`, every `<Provider>SqlBuilder.cs` / `<Provider>SqlOptimizer.cs`. Heaviest consumer.
- `EXPR-TRANS` -- every `*Builder.cs` under `Internal/Linq/Builder/` constructs AST nodes.
- `LINQ` -- `Query`, `QueryRunner` carry parsed `SqlStatement` instances and pass them to the provider.
- `Internal/SqlProvider/Tools` -- `SqlProviderHelper` and similar helpers.
- Per-provider AST extensions -- providers define `SqlExpression`-based subclasses for hints. The DuckDB provider (added in PR #5451) uses `QueryVisitorExtensions.Convert` with `withStack: true` for its `InlineOutputParametersVisitor` pass.

**Outbound** (what this area depends on):

- `LinqToDB.Mapping.ColumnDescriptor` -- read by `SqlField(ColumnDescriptor)` and `SqlTable(EntityDescriptor)`. `LinqToDB.Mapping.MappingSchema` is now also an explicit dependency of `SqlQueryColumnOptimizerVisitor.OptimizeColumns`, which needs it to synthesize a properly-typed placeholder `SqlCteField` when the CTE-field-synchronization pass would otherwise leave a CTE with zero fields.
- `LinqToDB.Common` -- `DbDataType`, `TakeHints`, plus `ObjectPool<T>`.
- `LinqToDB.Internal.Common` -- `Utils.ObjectReferenceEqualityComparer`, `StackGuard`, `Annotatable`.
- `LinqToDB.Sql` -- `Sql.AggregateModifier`, `Sql.NullsPosition`, `Sql.Nulls`, `Sql.From` (the latter two now consumed by `SqlExtendedFunction.NullTreatment`/`FromPosition`).
- `Internal.Infrastructure` -- `Annotatable` for CTE annotations.
- `LinqToDB.Internal.Linq.Builder` -- `IToSqlConverter` referenced by `SqlInlinedToSqlExpression`.
- `LinqToDB.Internal.SqlProvider` -- `SqlProviderFlags` referenced by `SqlQueryOrderByOptimizer` and `SqlQueryValidatorVisitor` (one-directional).

No dependency on `SQL-PROVIDER` builder/optimizer classes -- the AST is the *bottom* of the dependency stack within the query pipeline.

## Known issues / debt

- **Legacy public namespace.** Ten types still live in `Source/LinqToDB/SqlQuery/` (`LinqToDB.SqlQuery` namespace) -- down from twelve: `SqlExtendedFunction` and `SqlFrameClause` moved to `Internal.SqlQuery` in this delta, partially resolving the migration. Three of the remaining ten carry an explicit `// TODO: v7 - move to internal namespace...` comment (`SqlFrameBoundary`, `SqlWindowOrderItem`, `SqlFunctionArgument`).
- **`ISqlTableSource` extends `ISqlExpression`.** Open TODO at `ISqlTableSource.cs:5`. `SqlTableLikeSource` makes this concrete: all `ISqlExpression` members on it throw `NotSupportedException`.
- **Three-valued-logic flag duplicated.** `SqlSearchCondition.CanReturnUnknown` and per-predicate `CanBeUnknown` are not consistently used.
- **`Precedence` is db-specific but lives globally.** TODO at `Precedence.cs:3`. `Concatenate = 5` (PR #5504) deliberately uses a conservative global value because `||` precedence varies per provider.
- **`DefaultNullable` enum is publicly exposed but probably shouldn't be.** TODO at `DefaultNullable.cs:3`.
- **`ColumnDescriptor` set on every field, even non-column fields.** TODO at `SqlField.cs:96` (still present after the `SqlFieldBase` extraction).
- **`ISqlExpression.SystemType` flagged for v4 refactor.** TODO at `ISqlExpression.cs:12`. Still pending.
- **Visitor mode branches.** Each `VisitX` method switches on `VisitMode` and duplicates work three ways. Comment at `Visitors/QueryElementVisitor.cs:13` warns about de-sync risk -- borne out concretely this delta: three of the duplicated branches (`VisitSqlDropTableStatement`, `VisitSqlDeleteStatement`, `VisitSqlCastExpression`) had been copy/paste-corrupted and are now fixed.
- **`SqlBinaryExpressionHelper` type table is incomplete.** Some combinations are commented out.
- **`SqlQueryActionVisitor{TContext}.cs` has a duplicated attribute.** `[return: NotNullIfNotNull(nameof(element))]` appears twice.
- **`SqlConcat` enum ordinal placement.** `QueryElementType.SqlConcat` is at the tail of the enum (after `SqlFrameBoundary`) with a comment noting it should logically live next to `SqlCast`/`SqlCoalesce` -- deferred to v7 to avoid breaking LinqService wire-compat. Tracked as DI-0673. Three more members were tail-appended for the identical reason in this delta: `SqlCteField`/`SqlCteTableField` (belong next to `SqlCteTable`) and `SqlKeepClause` (belongs next to `SqlCast`/`SqlCoalesce`) -- `QueryElementType.cs:119-128`.
- **CTE field lookup has two fallback paths.** `SqlCteTable.GetKeys` and `SqlQueryColumnOptimizerVisitor.SynchronizeCteFields` both do direct-reference-first / name-string-fallback matching between `SqlCteField` and `SqlCteTableField` -- the fallback exists for construction paths that don't yet populate the direct reference. A v7 cleanup could require the direct reference unconditionally and drop the string fallback.

## See also

- [`architecture/sql-ast.md`](../../architecture/sql-ast.md) -- cross-area narrative.
- [`architecture/query-pipeline.md`](../../architecture/query-pipeline.md) -- where the AST sits in the LINQ -> SQL pipeline.
- [`architecture/public-api.md`](../../architecture/public-api.md) -- public-API discipline.
- [`areas/SQL-PROVIDER/INDEX.md`](../SQL-PROVIDER/INDEX.md) -- primary consumer.
- [`areas/EXPR-TRANS/INDEX.md`](../EXPR-TRANS/INDEX.md) -- primary producer.
- [`areas/LINQ/INDEX.md`](../LINQ/INDEX.md) -- query-execution layer.
- `.agents/docs/code-design.md` -> "SQL AST types live in `LinqToDB.Internal.SqlQuery`".

## Pointers

- Visitor entry: every node's `Accept(QueryElementVisitor)` method.
- Add a new node:
  1. Add a `QueryElementType` value (`QueryElementType.cs`).
  2. Create the class in `LinqToDB.Internal.SqlQuery` extending `QueryElement` or `SqlExpressionBase`.
  3. Add a `VisitX` method to `QueryElementVisitor` and override in clone/convert visitors.
  4. Implement `ToString(QueryElementTextWriter)`, `GetElementHashCode()`, `Equals(other, comparer)` if the type is an expression.
  5. If consumed by SQL emission, add a corresponding case to `BasicSqlBuilder`.
  6. Note: for wire-compat (LinqService serialises `QueryElementType` ordinals as `int`), append new enum members at the tail and add a comment per `SqlConcat`/`SqlCteField`/`SqlKeepClause` precedent.
- Walk an AST: `element.Visit(state, (state, e) => ...)`. `VisitParentFirst` for top-down traversal.
- Clone an AST: `element.Clone()` / `element.Clone(predicate)` -> `SqlQueryCloneVisitor`.
- Evaluate a constant subtree: `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)`.
- Inspect nullability: `expr.CanBeNullable(NullabilityContext.GetContext(query))`.
- Wrap a sub-query: `QueryHelper.WrapQuery(context, statement, wrapTest, onWrap, allowMutation)`.
- Replace nodes in a tree: `element.Replace(replacements, toIgnore)` -> `QueryElementReplacingVisitor`.
- Null-comparison rewriting: `SqlPredicate.ExprExpr.Reduce(nullability, context, isInsidePredicate, options)`.
- Use `WithStack`/`ParentElement` on a convert visitor: call `Convert<TContext, T>(element, context, convertAction, withStack: true)`. Access `visitor.ParentElement` inside `convertAction` to get the true parent node (returns `Stack[^2]`).
- Build a string-concat node: `new SqlConcatExpression(preserveNull: true/false, expr1, expr2, ...)`. `preserveNull: true` for null-propagating (standard SQL `||`); `false` for null-to-empty-string coercion.
- ORDER BY with explicit NULL placement: set `Sql.NullsPosition` on `SqlOrderByItem` or via `SqlOrderByClause.Expr(..., nullsPosition)`. For providers without native support, `SqlNullsOrderingLoweringVisitor` lowers to a `CASE WHEN expr IS NULL THEN 0/1 END` emulation key pre-optimizer. Check `QueryHelper.MatchesNaturalNullsPosition(ordering, requested, descending)` first to avoid redundant emulation.
- Reference a CTE column: schema-side `SqlCteField` (`CteClause.Fields`) holds the direct `Column` link into the body; site-side `SqlCteTableField` (`SqlCteTable.Fields`) holds the direct `CteField` back-reference. Prefer following the reference over name matching when writing new CTE-aware code -- see `SqlCteTable.GetKeys`, `SqlQueryColumnUsageCollector.VisitSqlCteTableField`, `SqlQueryColumnOptimizerVisitor.SynchronizeCteFields` for the pattern.
- `DISTINCT ON`: set `SqlSelectClause.DistinctOn` (and `IsDistinct = true`); the query's `OrderBy` must begin with the same expressions. `SqlQueryOrderByOptimizer.CanRemoveOrderBy` and the optimizer's DISTINCT-merge paths already special-case `IsDistinctOn` -- don't add a separate `DISTINCT ON` bypass elsewhere without checking those first.
- Oracle-style analytic modifiers on a window/aggregate function: `SqlExtendedFunction.KeepClause` (`new SqlKeepClause(KeepType.First/Last, orderByItems)`), `.NullTreatment` (`Sql.Nulls.Ignore/Respect`), `.FromPosition` (`Sql.From.First/Last`).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlExpression.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlField.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlBinaryExpression.cs
- Tier 2 (visited / total): 148 / 150 (98.7%)
  - Visited in detail (~55 files), visited as near-identical-shape group (~75 files), legacy folder (10/10). This delta adds 5 new Tier-2 files (all visited) and relocates 2 existing ones (see Files section).
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
  - Read (this run -- delta 2026-07-05):
    - Source/LinqToDB/Internal/SqlQuery/CteClause.cs -- Fields retyped List<SqlField> -> List<SqlCteField>; ctors/Init updated to match.
    - Source/LinqToDB/Internal/SqlQuery/ISqlNamedTable.cs (ADDED) -- ISqlTableSource + TableName; implemented by SqlTable and SqlCteTable.
    - Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs -- CanBeNull gained a _visitedColumns cycle guard against self-referential columns; added SqlCteTableField nullability case; NonQuery now allocates a fresh instance per call instead of a shared singleton.
    - Source/LinqToDB/Internal/SqlQuery/QueryElementType.cs -- tail-appended SqlCteField, SqlCteTableField, SqlKeepClause (wire-compat, same rationale as SqlConcat).
    - Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs -- SqlCteTableField/SqlCteField dispatch added throughout (GetColumnDescriptor, SuggestDbDataType, GetDbDataType, dependency/usage collectors, EnsureFindTables); recursion guards added to GetColumnDescriptor/SuggestDbDataType/GetDbDataType; HasTableInQuery/IsSingleTableInQuery/EnumerateAccessibleTables/ExtractSqlTable/IsEqualTables widened to ISqlNamedTable; CollectUniqueKeys no longer treats DISTINCT ON as a full-projection unique key.
    - Source/LinqToDB/Internal/SqlQuery/SelectQueryExtensions.cs -- added SelectQuery.HasJoins extension property.
    - Source/LinqToDB/Internal/SqlQuery/SqlColumn.cs -- GetAlias no longer falls back to SqlField.Alias (removed); debug-string dispatch now also covers SqlCteTableField.
    - Source/LinqToDB/Internal/SqlQuery/SqlCteField.cs (ADDED) -- schema-level CTE field descriptor (Type, Name, direct Column reference into the CTE body).
    - Source/LinqToDB/Internal/SqlQuery/SqlCteTable.cs -- no longer extends SqlTable; implements ISqlNamedTable directly; Fields retyped to List<SqlCteTableField>; GetKeys upgraded to reference-identity field matching with name fallback.
    - Source/LinqToDB/Internal/SqlQuery/SqlCteTableField.cs (ADDED) -- reference-site CTE field (SqlFieldBase subclass) delegating Name/Type/CanBeNullable to its CteField back-reference.
    - Source/LinqToDB/Internal/SqlQuery/SqlDeleteStatement.cs -- Table widened SqlTable? -> ISqlNamedTable?.
    - Source/LinqToDB/Internal/SqlQuery/SqlExtendedFunction.cs (MOVED from Source/LinqToDB/SqlQuery/, namespace LinqToDB.SqlQuery -> LinqToDB.Internal.SqlQuery) -- gained KeepClause, NullTreatment (Sql.Nulls), FromPosition (Sql.From) analytic modifiers plus matching With* builders and Equals/hash coverage.
    - Source/LinqToDB/Internal/SqlQuery/SqlField.cs -- extracted common members into new SqlFieldBase; removed dead Alias property; GetElementHashCode switched from value-based to identity-based (RuntimeHelpers.GetHashCode) to avoid stale hashes after mutation.
    - Source/LinqToDB/Internal/SqlQuery/SqlFieldBase.cs (ADDED) -- abstract base shared by SqlField/SqlCteTableField (Type, Name, abstract NamedTable, reference-equality Equals).
    - Source/LinqToDB/Internal/SqlQuery/SqlFrameClause.cs (MOVED from Source/LinqToDB/SqlQuery/, namespace LinqToDB.SqlQuery -> LinqToDB.Internal.SqlQuery) -- gained FrameExclusionKind (EXCLUDE CURRENT ROW/GROUP/TIES) and a comparer-based structural Equals.
    - Source/LinqToDB/Internal/SqlQuery/SqlFromClause.cs -- FindTableSource widened SqlTable -> ISqlNamedTable.
    - Source/LinqToDB/Internal/SqlQuery/SqlInsertOrUpdateStatement.cs -- gained UpdateWhere (conditional upsert predicate).
    - Source/LinqToDB/Internal/SqlQuery/SqlKeepClause.cs (ADDED) -- Oracle KEEP (DENSE_RANK FIRST/LAST ORDER BY ...) clause node.
    - Source/LinqToDB/Internal/SqlQuery/SqlMergeStatement.cs -- added an ISqlTableSource-typed constructor overload.
    - Source/LinqToDB/Internal/SqlQuery/SqlOutputClause.cs -- OutputTable widened SqlTable? -> ISqlNamedTable?.
    - Source/LinqToDB/Internal/SqlQuery/SqlSelectClause.cs -- added DistinctOn/IsDistinctOn (DISTINCT ON (...), PostgreSQL/DuckDB).
    - Source/LinqToDB/Internal/SqlQuery/SqlTable.cs -- now implements ISqlNamedTable (was ISqlTableSource directly); entity-descriptor constructor gained inheritance-sibling-column dedup (TPH/TPT).
    - Source/LinqToDB/Internal/SqlQuery/SqlUpdateClause.cs -- Table widened SqlTable? -> ISqlNamedTable?.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs -- new VisitSqlCteField/VisitSqlCteTableField/VisitSqlKeepClause; new CopyCteFields/CopyCteTableFields helpers; fixed 3 copy/paste bugs (VisitSqlDropTableStatement built the wrong statement type; VisitSqlDeleteStatement ReadOnly visited Table instead of Top; VisitSqlCastExpression ReadOnly double-visited FromType).
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs -- IsDistinctOn threaded through subquery-merge decisions; set-operator column-reordering reworked (TryBuildOuterColumnIndexes/TryReorderSetColumns replace UpdateSetIndexes/CheckSetColumns; new peer-coalescing passes); SqlCteTableField added to cheap-expression checks.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlNullsOrderingLoweringVisitor.cs -- doc-comment spacing only, no logic change.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryCloneVisitor.cs -- VisitSqlCteTable clones via CopyCteFields/CopyCteTableFields and re-resolves Column/CteField cross-references post-clone.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnNestingCorrector.cs -- added VisitSqlCteTableField (nests via ProcessNesting, mirroring VisitSqlFieldReference).
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnOptimizerVisitor.cs -- OptimizeColumns takes a MappingSchema; SynchronizeCteFields matches by direct Column reference (index as fallback) and synthesizes a properly-typed placeholder field instead of a hardcoded int dummy.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryColumnUsageCollector.cs -- added VisitSqlCteTableField, registering CteField.Column directly.
    - Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryOrderByOptimizer.cs -- fixes #5626 (ORDER BY promotion past DISTINCT/GROUP BY for computed expressions built from projected/grouped columns); CanRemoveOrderBy protects DISTINCT ON ordering from being stripped.
    - Source/LinqToDB/SqlQuery/SqlFrameBoundary.cs -- added a comparer-based structural Equals(other, comparer) overload (stays public, still TODO v7).
- Tier 3 (skipped, logged): 0 -- no generated / bin/ / obj/ files under this scope.
</details>
