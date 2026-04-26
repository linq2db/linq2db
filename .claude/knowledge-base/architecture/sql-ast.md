---
area: SQL-AST
kind: architecture
sources: [code]
confidence: high
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 5/5
coverage_tier_2: 12/143
---

# SQL AST

Cross-area narrative for the SQL abstract syntax tree. The full per-node catalog and dependency map is in [`areas/SQL-AST/INDEX.md`](../areas/SQL-AST/INDEX.md); this document covers the *wiring* between the AST and the rest of the pipeline plus the in-flight namespace migration.

## What the AST is, what it isn't

linq2db builds a strongly-typed object graph that mirrors the structure of a SQL statement — `SqlSelectStatement` owns a `SelectQuery` which owns six clauses, and each clause owns expression and predicate nodes that own further expressions. This graph is the AST. It is *not* a parse tree (linq2db never parses SQL — it builds the graph directly from C# expression trees) and it is *not* a relational-algebra IR (no normalization to a small primitive set; the AST has dedicated nodes for `IIF`, `BETWEEN`, `LIKE`, window frames, and so on).

The contract is: producer code constructs `IQueryElement` graphs, consumer code walks them via `QueryElementVisitor`. Identity matters — `SelectQuery.SourceIDCounter` (`Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs:186`) hands every `ISqlTableSource` a process-global integer ID that survives clone/transform operations. Optimizer passes use these IDs as keys for cross-tree bookkeeping; rewriting a graph without preserving them breaks join-removal logic.

## Position in the pipeline

```
LINQ expression tree
        │
        ▼
EXPR-TRANS (Internal/Linq/Builder/*.cs)              ◄─── producers
   constructs SqlStatement / SelectQuery / nodes
        │
        ▼
   ┌── SqlStatement ────────────────────────────┐
   │   ├── SelectQuery                          │   the AST
   │   │   ├── SqlSelectClause   (Columns)      │
   │   │   ├── SqlFromClause     (Tables)       │
   │   │   ├── SqlWhereClause    (Predicates)   │
   │   │   ├── SqlGroupByClause                 │
   │   │   ├── SqlHavingClause                  │
   │   │   └── SqlOrderByClause                 │
   │   └── SqlWithClause (CTEs)                 │
   └────────────────────────────────────────────┘
        │
        ▼
SQL-PROVIDER (Internal/SqlProvider/*.cs)             ◄─── consumers
   BasicSqlOptimizer rewrites the AST in place
   (multiple QueryElementVisitor passes)
        │
        ▼
   BasicSqlBuilder + provider override
   walks the AST and emits dialect SQL text
```

`EXPR-TRANS` builds the tree via per-method-call builders (`MethodCallParser`, `WhereBuilder`, `SelectBuilder`, …). `SQL-PROVIDER` does two things to it: optimizes (a chain of visitors that mutate the tree — sub-query collapsing, column trimming, predicate normalization, NULL handling) and then emits dialect-specific SQL. The AST is the only contract between these two halves; neither side imports the other.

`LINQ` (query execution) holds the resulting `Query` / `QueryRunner` that pairs the optimized AST with parameter binders and result materializers. The AST is reused across executions of the same compiled query — only `SqlParameter` nodes get fresh values per call.

## Node taxonomy at a glance

| Family | Root | Concrete count | Examples |
|---|---|---|---|
| Statements | `SqlStatement` | 9 | `SqlSelectStatement`, `SqlInsertStatement`, `SqlUpdateStatement`, `SqlDeleteStatement`, `SqlMergeStatement`, `SqlInsertOrUpdateStatement`, `SqlMultiInsertStatement`, `SqlCreateTableStatement`, `SqlDropTableStatement`, `SqlTruncateTableStatement` |
| Composite query | `SelectQuery` | 1 | (the central node) |
| Clauses | `ClauseBase` | 11 | `SqlSelectClause`, `SqlFromClause`, `SqlWhereClause`, `SqlGroupByClause`, `SqlHavingClause`, `SqlOrderByClause`, `SqlInsertClause`, `SqlUpdateClause`, `SqlOutputClause`, `SqlConditionalInsertClause`, `SqlMergeOperationClause` |
| Expressions | `SqlExpressionBase` | ~20 | `SqlValue`, `SqlParameter`, `SqlField`, `SqlColumn`, `SqlFunction`, `SqlExpression`, `SqlBinaryExpression`, `SqlUnaryExpression`, `SqlCaseExpression`, `SqlConditionExpression`, `SqlCastExpression`, `SqlCoalesceExpression`, `SqlCompareToExpression`, `SqlNullabilityExpression`, `SqlAnchor`, `SqlRowExpression`, `SqlInlinedSqlExpression`, `SqlInlinedToSqlExpression`, `SqlObjectExpression`, `SqlAliasPlaceholder`, `SqlFragment` |
| Predicates | nested in `SqlPredicate` | 14 | `Not`, `TruePredicate`, `FalsePredicate`, `Expr`, `ExprExpr`, `Like`, `SearchString`, `Between`, `IsNull`, `IsDistinct`, `IsTrue`, `InSubQuery`, `InList`, `Exists` |
| Predicate container | `SqlSearchCondition` | 1 | (`AND`/`OR` of predicates) |
| Tables / sources | various | 6 | `SqlTable`, `SqlCteTable`, `SqlRawSqlTable`, `SqlValuesTable`, `SqlTableLikeSource`, `SqlTableSource`, `SqlJoinedTable` |
| CTE infrastructure | `QueryElement` | 3 | `SqlWithClause`, `CteClause`, `SqlCteTable` |
| Visitor passes | `QueryElementVisitor` | ~16 | `SqlQueryCloneVisitor`, `SqlQueryConvertVisitor`, `SelectQueryOptimizerVisitor`, `SqlQueryColumnNestingCorrector`, `SqlQueryColumnOptimizerVisitor`, `SqlQueryColumnUsageCollector`, `SqlQueryFindVisitor`, `SqlQueryActionVisitor`, `SqlQueryParentFirstVisitor`, `SqlQueryValidatorVisitor`, `SqlQueryOrderByOptimizer`, `ReduceIsNullExpressionVisitor` |

`QueryElementType` (`Source/LinqToDB/Internal/SqlQuery/QueryElementType.cs:6`) is the closed enum that tags every node. New nodes MUST extend it.

## Visitor contract

Every node has a one-line `Accept(QueryElementVisitor)` that calls back into the visitor's typed `VisitX(this)`. `QueryElementVisitor` exposes one virtual `VisitX` per `QueryElementType`; subclasses override only what they care about. Three modes:

- `VisitMode.ReadOnly` — visitor walks the graph without mutating. Used by `SqlQueryFindVisitor`, dependency analysis (`QueryHelper.IsDependsOnSources`), and validators.
- `VisitMode.Modify` — visitor mutates nodes in place. Used by `SelectQueryOptimizerVisitor` after a node has already been replaced once.
- `VisitMode.Transform` — visitor returns new nodes; original is unchanged. Used by clone visitors and the first pass of `SqlQueryConvertVisitor`.

Each `VisitX` method has three branches (one per mode). A known refactor target — comment at `Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs:13` — is that branches drift apart over time. New nodes should follow the existing template exactly to avoid the drift.

The `SqlQueryVisitor` subclass (`Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryVisitor.cs:18`) adds a replacement-tracking layer via `IVisitorTransformationInfo` — it remembers the old→new mapping for every replaced element so that downstream references are kept consistent during a multi-pass optimization. Most rewriting passes derive from `SqlQueryConvertVisitorBase` rather than `QueryElementVisitor` directly; the latter is mainly used for read-only walks and the clone visitor.

## Nullability and three-valued logic

linq2db tracks NULL-ability for two reasons: (1) to emit correct `IS NULL` / `IS NOT NULL` checks where C# semantics require them, (2) to decide when a sub-query can be removed without changing results. Two types carry the state:

- `NullabilityContext` (`Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs:12`) — query-scoped; threaded through every node's `CanBeNullable(nullability)` call. Knows about outer-join semantics (a column from a `LEFT JOIN`-ed table is nullable in the outer query even if its underlying column is `NOT NULL`).
- `SqlNullabilityExpression` — wraps an expression to override its computed nullability. Used by the translator when it can prove a stricter or looser bound.

Predicates use a separate `bool? CanBeUnknown(NullabilityContext, bool withoutUnknownErased)` because SQL predicates are three-valued (TRUE / FALSE / UNKNOWN). `SqlSearchCondition.CanReturnUnknown` is an upfront flag set at construction; the per-predicate method is the precise computation. The two overlap and aren't always kept in sync — a known soft spot.

## Debug-only rendering vs production SQL

The AST has a built-in stringifier — every node implements `ToString(QueryElementTextWriter)` and `SqlStatement.SqlText` / `SelectQuery.SqlText` use it for `[DebuggerDisplay]`. This output is **not** valid SQL and **not** what gets sent to the database. It uses placeholders like `$IIF$(...)` (`Source/LinqToDB/Internal/SqlQuery/SqlConditionExpression.cs:29`) and per-source identifiers like `t5.col?` to be diagnosable, not executable.

The production SQL emitter is `BasicSqlBuilder` and its provider-specific subclasses under `Source/LinqToDB/Internal/SqlProvider/`. They walk the AST through their own dispatch (a giant switch in `BuildExpression` plus dedicated `BuildSelectClause` / `BuildFromClause` / etc.) and write to a `SqlTextWriter`. There are two separate writers because the dialect-aware emission (parameter prefixes, identifier escaping, quoted strings, `LIMIT`/`TOP`/`FETCH FIRST` choice) cannot be expressed at the AST level. New nodes that need debug output add a `ToString(writer)`; new nodes that need to be emitted add a case to `BasicSqlBuilder` (covered in [`SQL-PROVIDER`](../areas/SQL-PROVIDER/INDEX.md)).

## Namespace placement rule

The repo's convention — pinned in `.claude/docs/code-design.md` → "SQL AST types live in `LinqToDB.Internal.SqlQuery`" — is that every AST node belongs in the `LinqToDB.Internal.SqlQuery` namespace. This holds for everything used only during query construction, translation, and rendering: `SqlField`, `SqlSelectClause`, every `Sql*Expression`, every `Sql*Statement`, every visitor.

The reason: AST types are not part of the stable public API. linq2db users build queries via LINQ — they should never see a `SqlField` directly. Treating the AST as `internal` keeps the option to refactor it freely across major versions.

### Legacy public namespace and pending migration

Twelve types still live in `Source/LinqToDB/SqlQuery/` (`LinqToDB.SqlQuery` namespace), originally because they pre-date the `Internal.*` convention:

| Type | File | Status |
|---|---|---|
| `Precedence` | `SqlQuery/Precedence.cs` | Static const class; `// TODO: precedence requires total refactoring, as it db-specific` (`SqlQuery/Precedence.cs:3`). Used by every expression node. Move blocked on the refactor. |
| `SqlObjectName` | `SqlQuery/SqlObjectName.cs` | Public `record struct` describing `Server.Database.Schema.Package.Name`. Used by metadata APIs that *are* public — moving requires public-API churn. |
| `SqlDataType` | `SqlQuery/SqlDataType.cs` | Wraps `DbDataType` as an AST node. Move scheduled but not yet done. |
| `SqlExtendedFunction` | `SqlQuery/SqlExtendedFunction.cs` | `// TODO: v7 - move to internal namespace to other AST members...` (`SqlQuery/SqlExtendedFunction.cs:12`) |
| `SqlFrameBoundary` | `SqlQuery/SqlFrameBoundary.cs` | Same `// TODO: v7` comment (`SqlQuery/SqlFrameBoundary.cs:9`) |
| `SqlFrameClause` | `SqlQuery/SqlFrameClause.cs` | Same `// TODO: v7` comment (`SqlQuery/SqlFrameClause.cs:9`) |
| `SqlFunctionArgument` | `SqlQuery/SqlFunctionArgument.cs` | Same `// TODO: v7` comment (`SqlQuery/SqlFunctionArgument.cs:10`) |
| `SqlWindowOrderItem` | `SqlQuery/SqlWindowOrderItem.cs` | Same `// TODO: v7` comment (`SqlQuery/SqlWindowOrderItem.cs:10`) |
| `MultiInsertType` | `SqlQuery/MultiInsertType.cs` | Enum used by `SqlMultiInsertStatement`. |
| `NoneExtensionBuilder` | `SqlQuery/NoneExtensionBuilder.cs` | Empty marker — `internal sealed class` (`SqlQuery/NoneExtensionBuilder.cs:5`). Despite being already `internal`, the file is in the legacy folder. |
| `ISqlExpressionExtensions` | `SqlQuery/ISqlExpressionExtensions.cs` | Static extension methods over `ISqlExpression`. |
| `DefaultNullable` | `SqlQuery/DefaultNullable.cs` | Public enum; `// TODO: review: why we even expose this to public API?` (`SqlQuery/DefaultNullable.cs:3`). |

The `// TODO: v7` cluster (the five window-function / extended-function / frame types) is scheduled to move on the next major. Moving them earlier would break public-API consumers — the `api-baselines` skill catches such moves as `LinqToDB.Internal.*` namespace policy violations and gates them on milestone.

The rule for new code is unambiguous: every new AST type goes to `LinqToDB.Internal.SqlQuery`. The legacy-folder types do not establish precedent. Per `code-design.md`, a `/review-pr` finding that flags a new type in `LinqToDB.SqlQuery` is a namespace-placement issue, not a public-API-break issue — the fix is to move, not to keep.

## Construction patterns

Translators construct AST via two idioms:

1. **Direct `new`** — for nodes with a single canonical shape (`new SqlBinaryExpression(type, left, "+", right)`, `new SqlValue(systemType, value)`, `new SqlField(columnDescriptor)`).
2. **`PseudoFunctions` helpers** — for nodes that need a well-known name plus consistent metadata (`PseudoFunctions.MakeToLower(value, mappingSchema)`, `PseudoFunctions.MakeCast(value, toType)`). Source: `Source/LinqToDB/Internal/SqlQuery/PseudoFunctions.cs:9`. The `$ToLower$` / `$ToUpper$` / `$Convert_Format$` markers prevent collision with real SQL function names; provider optimizers look for these markers and rewrite to dialect-specific SQL (e.g. SQL Server `LOWER(x)`, Oracle `LOWER(x)`, but Firebird `LOWER(CAST(x AS BLOB SUB_TYPE TEXT))`).

`QueryHelper` (`Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs:18`) is the static facade for *analysis* — it does not construct nodes, it only walks them. `QueryHelper.IsDependsOnSource`, `QueryHelper.IsAggregationQuery`, `QueryHelper.WrapQuery` are the high-traffic entry points.

## What pipelines through `EvaluationContext`

`EvaluationContext` (`Source/LinqToDB/Internal/SqlQuery/EvaluationContext.cs:8`) is the parameter-binding cache. When the optimizer needs to know "would this subtree evaluate to a constant given the current parameter values?", it calls `QueryHelper.TryEvaluateExpression(expr, evaluationContext, out value)`. The context distinguishes client-side and server-side evaluation (separate caches) because some nodes can fold on the client (`1 + 2 == 3`) but not on the server, and vice versa.

This is how compiled queries get re-optimized cheaply for new parameter values — the AST is reused, only the `EvaluationContext` is fresh.

## Out of scope for this document

- Per-node SQL emission rules (those live in [`SQL-PROVIDER`](../areas/SQL-PROVIDER/INDEX.md)).
- Translator-side construction details (those live in [`EXPR-TRANS`](../areas/EXPR-TRANS/INDEX.md)).
- Public-facing query-builder APIs (`Linq.Expressions.LinqExtensions`, `Sql.*` static helpers — those are [`CORE`](../areas/CORE/INDEX.md) / `EXTENSIONS-PKG`).
- Detected debt items (those land in `areas/SQL-AST/tech-debt.md` from step 11).

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 5 / 5 ✓ — same anchors as the area INDEX.
- Tier 2 (visited / total): 12 / 143 (8.4%) — this document's claims rest on the area INDEX's full sweep; only the 12 legacy `Source/LinqToDB/SqlQuery/` files are revisited here for the namespace-migration table.
  - skipped: ~131 internal-namespace AST files — covered by `areas/SQL-AST/INDEX.md` and not re-claimed in this cross-area narrative.
- Tier 3 (skipped, logged): 0
</details>
