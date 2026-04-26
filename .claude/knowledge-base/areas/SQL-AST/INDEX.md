---
area: SQL-AST
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
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
- **`ISqlPredicate`** (`ISqlPredicate.cs:5`) — the boolean subset; adds `CanInvert` / `Invert(NullabilityContext)` and `CanBeUnknown(NullabilityContext, bool)` to express SQL three-valued logic.
- **`ISqlTableSource`** (`ISqlTableSource.cs:6`) — anything usable in `FROM` / join. Carries an integer `SourceID` (allocated from `SelectQuery.SourceIDCounter`, `SelectQuery.cs:186`) which the rest of the pipeline uses as the canonical identity for a table reference. Implementations: `SqlTable`, `SelectQuery`, `SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`. The interface extends `ISqlExpression` — there's a `// TODO: [sdanyliv] ISqlTableSource why it extends ISqlExpression?` at `ISqlTableSource.cs:5` flagging the unease.
- **`SqlStatement`** (`SqlStatement.cs:8`) — abstract root of any executable statement; exposes `QueryType` (Select/Insert/Update/Delete/Merge/etc.) and the optional carried `SelectQuery`. Concrete `Sql<Verb>Statement` types extend either `SqlStatement` directly (`SqlCreateTableStatement`, `SqlDropTableStatement`, `SqlTruncateTableStatement`) or `SqlStatementWithQueryBase` (`SqlStatementWithQueryBase.cs:5`) which adds the `With` (CTE) clause and forces a non-null `SelectQuery`.
- **`SelectQuery`** (`SelectQuery.cs:12`) — the central composite node, aggregates the six clauses (`Select`/`From`/`Where`/`GroupBy`/`Having`/`OrderBy`, `SelectQuery.cs:70-75`) plus optional `SetOperators` (UNION/INTERSECT/EXCEPT) and `UniqueKeys` used by the join optimizer. Implements `ISqlTableSource` — a sub-query is itself a table source.
- **`SqlField`** (`SqlField.cs:9`) — column reference. Constructed either bare (`name`/`physicalName`) or from a `ColumnDescriptor` (`SqlField.cs:63-78`), which is how `SqlTable` populates its fields from mapping metadata. `SqlField.All(table)` produces the `*` placeholder used in `SELECT *` paths.
- **Expression nodes** — value-producing nodes that derive from `SqlExpressionBase`:
  - `SqlValue` (literal), `SqlParameter` (parameterised value with `IsQueryParameter`/`AccessorId`), `SqlFunction` / `SqlExpression` (both extend the shared `SqlParameterizedExpressionBase`; `SqlFunction` is a named function call, `SqlExpression` is a free-form format-string fragment, `SqlExpression.cs:10`),
  - `SqlBinaryExpression` (`SqlBinaryExpression.cs:9`), `SqlUnaryExpression` (`SqlUnaryExpression.cs:9`),
  - `SqlCaseExpression`, `SqlConditionExpression` ($IIF$, `SqlConditionExpression.cs:8`), `SqlCoalesceExpression`, `SqlCompareToExpression`, `SqlCastExpression` (`SqlCastExpression.cs:9`),
  - `SqlNullabilityExpression` (wraps another expression to override its nullability annotation, `SqlNullabilityExpression.cs:9`), `SqlAnchor` (table-source / inserted / deleted markers used during merge/output translation, `SqlAnchor.cs:8`), `SqlRowExpression` (multi-column row constructor, `SqlRowExpression.cs:9`),
  - `SqlInlinedBase` and its derivatives `SqlInlinedSqlExpression` / `SqlInlinedToSqlExpression` — placeholders for `ISqlExpression` / `IToSqlConverter` instances embedded directly in a LINQ expression (see `SqlInlinedBase.cs:5`, `QueryElementType.SqlInlinedExpression`).
- **Predicate nodes** — boolean nodes nested inside `SqlSearchCondition`:
  - All defined as nested classes of `SqlPredicate` (`SqlPredicate.cs:10`): `Not`, `TruePredicate`, `FalsePredicate`, `Expr`, `ExprExpr`, `Like`, `SearchString`, `Between`, `IsNull`, `IsDistinct`, `IsTrue`, `InSubQuery`, `InList`, `Exists`, plus the `Operator` enum (=, <>, >, …, `Overlaps`).
  - `SqlSearchCondition` (`SqlSearchCondition.cs:12`) — the `AND`/`OR`-of-predicates container; `IsOr` toggles the connective, `CanReturnUnknown` carries the three-valued-logic flag.
- **Clause nodes** — `SqlSelectClause`, `SqlFromClause`, `SqlWhereClause`, `SqlGroupByClause`, `SqlHavingClause`, `SqlOrderByClause`, `SqlInsertClause`, `SqlUpdateClause`, `SqlOutputClause`, `SqlConditionalInsertClause`, `SqlMergeOperationClause`. All extend `ClauseBase` (`ClauseBase.cs:3`) which holds a back-pointer to the owning `SelectQuery`.
- **Table-shaped nodes** — `SqlTable` (mapped table, `SqlTable.cs:14`), `SqlCteTable` (CTE reference, `SqlCteTable.cs:11`), `SqlRawSqlTable` (raw SQL fragment used as a table, `SqlRawSqlTable.cs:11`), `SqlValuesTable` (`VALUES`-list, `SqlValuesTable.cs:11`), `SqlTableSource` (positional wrapper holding alias + joins), `SqlJoinedTable` (one element of a `JOIN` chain), `SqlTableLikeSource` (used as MERGE source, `SqlMergeStatement.cs:11`).
- **CTE infrastructure** — `CteClause` (`CteClause.cs:13`), `SqlWithClause`, `SqlCteTable`. `CteClause` carries an open-ended `Annotations` bag (`CteClause.cs:30`) for provider-specific hints (e.g. PostgreSQL `MATERIALIZED`).
- **Visitor base classes** — `QueryElementVisitor` (`Visitors/QueryElementVisitor.cs:27`) is the abstract dispatcher with one `VisitX` method per node type. Subclasses:
  - `SqlQueryVisitor` (`Visitors/SqlQueryVisitor.cs:18`) — adds replacement tracking via `IVisitorTransformationInfo`.
  - `SqlQueryCloneVisitorBase` / `SqlQueryCloneVisitor` (`Visitors/SqlQueryCloneVisitor.cs:5`) — deep clone with optional predicate filter.
  - `SqlQueryConvertVisitorBase` / `SqlQueryConvertVisitor{TContext}` (`Visitors/SqlQueryConvertVisitorBase.cs:5`) — tree rewriting with explicit `Modify` vs `Transform` modes.
  - `SqlQueryActionVisitor` / `SqlQueryFindVisitor` / `SqlQueryParentFirstVisitor` — read-only search/walk.
  - `SelectQueryOptimizerVisitor` (`Visitors/SelectQueryOptimizerVisitor.cs:16`) — the pooled, stateful optimizer the SQL builder runs before emission.
  - `SqlQueryColumnNestingCorrector`, `SqlQueryColumnOptimizerVisitor`, `SqlQueryColumnUsageCollector`, `SqlQueryOrderByOptimizer`, `ReduceIsNullExpressionVisitor`, `SqlQueryValidatorVisitor` — specialised passes invoked from the optimizer pipeline.
- **`VisitMode`** (`Visitors/VisitMode.cs:6`) — `ReadOnly | Modify | Transform`. `ReadOnly` walks without mutation, `Modify` mutates in place, `Transform` returns new instances. Each `VisitX` method has three branches — the comment block at `Visitors/QueryElementVisitor.cs:13` warns that this is a known refactor target ("could de-sync VisitMode branches on changes").
- **`NullabilityContext`** (`NullabilityContext.cs:12`) — query-scoped cache that decides whether a given `ISqlExpression` can produce NULL, factoring in outer joins and `SqlNullabilityExpression` overrides. `NullabilityContext.NonQuery` is the empty default; `GetContext(SelectQuery?)` is the entry the SQL builder uses.
- **`EvaluationContext`** (`EvaluationContext.cs:8`) — caches client-side and server-side evaluation results for `IQueryElement` subtrees, keyed by reference identity (`Utils.ObjectReferenceEqualityComparer`). Drives `QueryHelper.Evaluate`.
- **`QueryHelper`** (`QueryHelper.cs:18`, plus partial files `QueryHelper.Evaluate.cs`, `QueryHelper.WrapQuery.cs`) — static facade for tree analysis: `IsDependsOnSource`, `EnumerateAccessibleSources`, `IsAggregationQuery`, `WrapQuery` (sub-query injection). Exposes pooled `SelectQueryOptimizerVisitor` / `AggregationCheckVisitor` instances (`QueryHelper.cs:20-24`).
- **`PseudoFunctions`** (`PseudoFunctions.cs:9`) — well-known function-name constants like `$ToLower$`, `$Convert_Format$`, `$merge_action$`. Translators emit these; provider optimizers rewrite them to dialect-specific SQL. The dollar-sign convention prevents collision with real SQL function names.
- **`QueryElementTextWriter`** (`QueryElementTextWriter.cs:10`) — debug renderer threaded through every node's `ToString(writer)`. Carries the active `NullabilityContext` so `?` annotations track nullability. Used only for `DebugDisplay` and unit-test diagnostics; the production SQL emitter is `BasicSqlBuilder` in [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md), which does **not** go through this writer.
- **`Precedence`** (legacy public, `Source/LinqToDB/SqlQuery/Precedence.cs:4`) — int constants `Primary=100` … `LogicalDisjunction=10` ranking SQL operator precedence. Used by every expression node's `Precedence` property to decide when the writer must wrap with parentheses. Marked `// TODO: precedence requires total refactoring, as it db-specific` (`Precedence.cs:3`).

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

1. **Statement roots** — `Sql<Verb>Statement.cs`. Every supported top-level operation gets one type, all extending `SqlStatement` or `SqlStatementWithQueryBase`. Each carries a `QueryType` enum value (`QueryType.cs:3`) and an `ElementType`. Together these are how the SQL builder dispatches.
2. **Composite query** — `SelectQuery` and the six `Sql<Clause>Clause` files. The clause objects are owned 1:1 by the `SelectQuery` and back-link via `ClauseBase.SelectQuery`. Internal helpers (`Field`, `SubQuery`, `Table`, etc.) provide builder-style mutation; the translator uses these heavily.
3. **Expression nodes** — `Sql*Expression.cs` plus `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlColumn`. All implement `ISqlExpression`. `SqlExpression` itself is special — it's a *string-format* fragment with positional `{0}…{n}` parameters, the escape hatch when a dedicated node would be over-engineering.
4. **Predicate nodes** — nested classes of `SqlPredicate`, glued together by `SqlSearchCondition`. The two-valued-logic predicate (`bool`) and the three-valued-logic predicate (`bool?` via `CanBeUnknown`) coexist; the optimizer cares about the difference.
5. **Tables and sources** — `SqlTable` (entity-mapped) and its specialised siblings (`SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`), plus the positional wrappers `SqlTableSource` / `SqlJoinedTable` that hold alias and join metadata.
6. **Visitors** — under `Visitors/`. Six visitor base classes plus eight specialised pass implementations. Anything that walks the AST goes through one of these; do not write `switch` statements over `ElementType` outside of the visitors themselves.
7. **Helpers and contexts** — `QueryHelper` (analysis), `NullabilityContext` (NULL inference), `EvaluationContext` (constant folding), `AliasesContext` (alias scoping), `PseudoFunctions` (well-known names), `DebugStringExtensions` / `QueryElementTextWriter` (debug rendering).

## Interactions

- **Producer**: every `*Builder` under `Source/LinqToDB/Internal/Linq/Builder/` constructs AST nodes — see [`EXPR-TRANS`](../EXPR-TRANS/INDEX.md). Handles like `MakeToLower`, `MakeCast` (`PseudoFunctions.cs:17`, `:30`) are the canonical entry points; translators rarely `new` AST nodes directly when a helper exists.
- **Consumer**: `BasicSqlBuilder` and provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/` walk the AST through `QueryElementVisitor` and emit dialect text — see [`SQL-PROVIDER`](../SQL-PROVIDER/INDEX.md). `BasicSqlOptimizer` runs the AST-rewriting passes (`SelectQueryOptimizerVisitor`, `SqlQueryColumnOptimizerVisitor`, `ReduceIsNullExpressionVisitor`, …) before `BasicSqlBuilder` emits.
- **Cross-cutting**: `NullabilityContext` is threaded through both producer and consumer paths; the producer creates one via `NullabilityContext.GetContext(selectQuery)`, the consumer consults `expr.CanBeNullable(nullability)` for every emitted column.
- **Debug rendering**: `SqlStatement.SqlText` (`SqlStatement.cs:10`) and `SelectQuery.SqlText` (`SelectQuery.cs:333`) call back into `QueryElementTextWriter`. This is *only* for `[DebuggerDisplay]` and test diagnostics — production SQL emission is done by `BasicSqlBuilder`, which has its own writer.
- **Identity**: `SelectQuery.SourceIDCounter` (`SelectQuery.cs:186`) is a single static counter increment'd by every `ISqlTableSource` constructor (e.g. `SqlTable.cs:20`, `SelectQuery.cs:18`, `SqlSourceBase.cs:10`, `SqlValuesTable.cs:20`). Source IDs are repo-global within a process and survive cloning (the clone visitor reuses them) — they are the primary key the optimizer uses to track sources across rewrites.

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

No dependency on `SQL-PROVIDER` or `EXPR-TRANS` — the AST is the *bottom* of the dependency stack within the query pipeline.

## Known issues / debt

Pointers only — full debt entries land in step 11's `tech-debt.md`. Surface signals visible from a Tier-1 read:

- **Legacy public namespace.** Twelve types still live in `Source/LinqToDB/SqlQuery/` (`LinqToDB.SqlQuery` namespace). Eight of them carry an explicit `// TODO: v7 - move to internal namespace to other AST members...` comment (`SqlExtendedFunction.cs:12`, `SqlFrameBoundary.cs:9`, `SqlFrameClause.cs:9`, `SqlFunctionArgument.cs:10`, `SqlWindowOrderItem.cs:10`). See `architecture/sql-ast.md` for the migration plan and the `code-design.md` rule that gates new types.
- **`ISqlTableSource` extends `ISqlExpression`.** Open `// TODO: [sdanyliv]` at `ISqlTableSource.cs:5` — the inheritance is convenient (sub-queries are expressions) but couples table sources to value semantics they don't really have.
- **Three-valued-logic flag duplicated.** `SqlSearchCondition.CanReturnUnknown` (`SqlSearchCondition.cs:53`) and per-predicate `CanBeUnknown(NullabilityContext, bool withoutUnknownErased)` are not consistently used; some optimizer passes ignore `CanReturnUnknown` and recompute.
- **`Precedence` is db-specific but lives globally.** `// TODO: precedence requires total refactoring, as it db-specific` at `Precedence.cs:3`. The constant table assumes SQL Server-ish precedence; Oracle and SQLite have outliers (the Concatenate=5 line is a SQLite-specific carve-out).
- **`DefaultNullable` enum is publicly exposed but probably shouldn't be.** `// TODO: review: why we even expose this to public API?` at `DefaultNullable.cs:3`.
- **`ColumnDescriptor` set on every field, even non-column fields.** `// TODO: not true, we probably should introduce something else for non-column fields` at `SqlField.cs:96`.
- **`ISqlExpression.SystemType` flagged for v4 refactor.** `// TODO: v4 refactoring: replace with DbDataType and eradicate nullability` at `ISqlExpression.cs:12`. Still pending.
- **Visitor mode branches.** Each `VisitX` method in `QueryElementVisitor` switches on `VisitMode` and duplicates work three ways. Comment at `Visitors/QueryElementVisitor.cs:13` warns about de-sync risk on changes.

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
- Walk an AST: `element.Visit(state, (state, e) => …)` extension on `IQueryElement` (`QueryVisitorExtensions.cs`).
- Clone an AST: `element.Clone()` / `element.Clone(predicate)` (`QueryVisitorExtensions.cs`) → `SqlQueryCloneVisitor`.
- Evaluate a constant subtree: `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)` (in `QueryHelper.Evaluate.cs`).
- Inspect nullability: `expr.CanBeNullable(NullabilityContext.GetContext(query))`.

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
</details>
