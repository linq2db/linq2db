---
area: SQL-PROVIDER
kind: architecture
sources: [code]
confidence: high
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
---

# SQL provider — emission and statement optimization

linq2db's SQL emission lives in two folders. `Source/LinqToDB/Internal/SqlProvider/` holds the engine: the `ISqlBuilder` / `ISqlOptimizer` contracts, their large `BasicSqlBuilder` / `BasicSqlOptimizer` default implementations, the visitor pair that runs over every AST node before emission, the `SqlProviderFlags` capability bag, and the cross-cutting helpers (joins optimizer, optimization context, identifier-conversion enum, query-extension hint builders). `Source/LinqToDB/Sql/` holds the user-visible `Sql.*` static partial class — server-side helpers and the attribute hierarchy that user code uses to declare custom translations.

The two together make up the provider-agnostic half of the SQL story. The provider-specific half lives in [PROV-*](../areas/PROV-SQLSERVER/INDEX.md) areas under `Source/LinqToDB/Internal/DataProvider/<Provider>/`, where each in-tree database ships exactly one `<Provider>SqlBuilder : BasicSqlBuilder<TOptions>` and one `<Provider>SqlOptimizer : BasicSqlOptimizer`.

## Where this fits in the pipeline

By the time the SQL provider gets called, the LINQ pipeline has already produced an `SqlStatement` (see [areas/SQL-AST/INDEX.md](../areas/SQL-AST/INDEX.md)). The handoff is in two phases:

1. `BasicSqlOptimizer.Finalize(mappingSchema, statement, dataOptions)` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:51`) takes the statement, runs a fixed sequence of rewrite passes, and returns a finalized statement that the builder can emit verbatim. Called from `ExpressionBuilder.BuildQuery` (see [architecture/query-pipeline.md](query-pipeline.md)).
2. `BasicSqlBuilder.BuildSql(commandNumber, statement, sb, optimizationContext, aliases, nullabilityContext, startIndent)` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:179`) walks the finalized statement and writes provider-specific SQL into the `StringBuilder`. Called from the query runner during `SetCommand` (see [architecture/query-pipeline.md](query-pipeline.md)).

The statement that arrives at the optimizer is conceptually independent of any database; the statement that leaves it has already been adapted for the target dialect (skip/take rewritten, unsupported joins flattened, NULL/parameter casts inserted across UNION arms, etc.). The builder's job is to render — it doesn't make structural decisions any more.

## How `BasicSqlBuilder` walks the AST

`BasicSqlBuilder` is one ~4650-line `abstract partial class` (`BasicSqlBuilder.cs:26`, plus `BasicSqlBuilder.Merge.cs` for MERGE-specific code). The control flow is dispatch-by-`QueryType`. `BuildSqlImpl` (`BasicSqlBuilder.cs:367`) is a switch over `Statement.QueryType`:

```
case QueryType.Select        : BuildSelectQuery((SqlSelectStatement)Statement);
case QueryType.Delete        : BuildDeleteQuery((SqlDeleteStatement)Statement);
case QueryType.Update        : BuildUpdateQuery(...);
case QueryType.Insert        : BuildInsertQuery(...);
case QueryType.InsertOrUpdate: BuildInsertOrUpdateQuery(...);
case QueryType.CreateTable   : BuildCreateTableStatement(...);
case QueryType.DropTable     : BuildDropTableStatement(...);
case QueryType.TruncateTable : BuildTruncateTableStatement(...);
case QueryType.Merge         : BuildMergeStatement(...);
case QueryType.MultiInsert   : BuildMultiInsertQuery(...);
default                      : BuildUnknownQuery();
```

Each per-verb method emits clauses in dialect order, setting `BuildStep` (a value of the `Step` enum, `BasicSqlBuilder.cs:4048`) before each. `BuildSelectQuery` (`BasicSqlBuilder.cs:469`) is the canonical example:

```
BuildStep = Step.Tag;             BuildTag(...);
BuildStep = Step.WithClause;      BuildWithClause(selectStatement.With);
BuildStep = Step.SelectClause;    BuildSelectClause(selectStatement.SelectQuery);
BuildStep = Step.FromClause;      BuildFromClause(...);
BuildStep = Step.WhereClause;     BuildWhereClause(...);
BuildStep = Step.GroupByClause;   BuildGroupByClause(...);
BuildStep = Step.HavingClause;    BuildHavingClause(...);
BuildStep = Step.OrderByClause;   BuildOrderByClause(...);
BuildStep = Step.OffsetLimit;     BuildOffsetLimit(...);
BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(...);
```

Subclasses override individual `BuildXxx` methods to change order, prepend/append text, or skip a clause when the dialect doesn't support it. The reason for the explicit `BuildStep` field is that helper methods like `BuildExpression` use it to know which clause they're rendering inside (e.g. column aliases are valid in `Step.SelectClause` but not in `Step.WhereClause`).

Sub-tree emission goes through `BuildExpression(StringBuilder sb, ISqlExpression expr, …)` (`BasicSqlBuilder.cs:3036`) — a switch on `expr.ElementType` that dispatches to `BuildSqlCastExpression`, `BuildSqlConditionExpression` (the `IIF` shape, `BasicSqlBuilder.cs:3502`), `BuildSqlCaseExpression`, `BuildBinaryExpression`, `BuildUnaryExpression`, `BuildFunction`, `BuildParameter`, `BuildValue`, `BuildSqlRow`, `BuildSqlExtendedFunction`, `BuildAnchor`, etc. Predicate emission is parallel: `BuildLikePredicate`, `BuildInListPredicate`, `BuildInSubQueryPredicate`, etc.

Identifier escaping is centralised through `Convert(StringBuilder, string, ConvertType)` (`BasicSqlBuilder.cs:606`, default no-op). The `ConvertType` enum (`Source/LinqToDB/Internal/SqlProvider/ConvertType.cs:3`) tags the *kind* of identifier — `NameToQueryField`, `NameToQueryTable`, `NameToCteName`, `NameToDatabase`, `NameToServer`, `NameToQueryParameter`, `NameToSprocParameter`, etc. — so a provider can apply different rules to a parameter name (`@p1` for SQL Server) vs a CTE alias (`[my_cte]`) vs a stored procedure parameter (`p_name` after `SprocParameterToName` strips the `@`).

CTE emission is in `BuildWithClause` (`BasicSqlBuilder.cs:670`). Hooks: `IsRecursiveCteKeywordRequired`, `IsCteColumnListSupported`, `SupportsMaterializedCteHint`, `SupportsNotMaterializedCteHint`, `CteFirst`. The `Materialized` annotation pathway (`BasicSqlBuilder.cs:632`) lets PostgreSQL 12+, SQLite 3.35+, and ClickHouse 26.3+ emit `AS MATERIALIZED` / `AS NOT MATERIALIZED` from a `CteAnnotationNames.Materialized` annotation on the [SQL-AST](../areas/SQL-AST/INDEX.md) `CteClause`. Oracle's `/*+ MATERIALIZE */` hint is delivered by overriding `BuildCteHeaderHint` directly.

MERGE is split into a separate partial file (`BasicSqlBuilder.Merge.cs`, 504 lines) because `BuildMergeStatement` (`BasicSqlBuilder.Merge.cs:48`) has a different shape — it walks `merge.Operations` rather than emitting a fixed clause sequence. Hooks `IsValuesSyntaxSupported`, `FakeTable`, `FakeTableSchema`, `RequiresConstantColumnAliases`, `SupportsColumnAliasesInSource` accommodate dialects that don't accept inline `VALUES (…)` as a merge source.

Sub-queries always create a new same-typed builder via the abstract `CreateSqlBuilder()` (`BasicSqlBuilder.cs:302`). Every concrete provider implements this as `=> new XxxSqlBuilder(this)` (e.g. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseSqlBuilder.cs:29`). The new builder shares `MappingSchema`, `DataProvider`, `SqlOptimizer`, `SqlProviderFlags`, `TablePath`, `QueryName`, `TableIDs`, `NullabilityContext` with the parent (`BasicSqlBuilder.cs:40-51`) so accumulated state propagates into nested scopes.

## How `BasicSqlOptimizer` rewrites it before emission

`BasicSqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:51`) is a fixed sequence of rewrite passes:

1. **`FixEmptySelect`** (`BasicSqlOptimizer.cs:549`) — top-level `SELECT *` queries with zero columns are turned into `SELECT 1` to avoid unintended `*` traffic and to keep remote contexts working.
2. **`FinalizeCte`** — collects every CTE used by the statement and stitches them into the `With` clause if not already there.
3. **`OptimizeQueries`** (`BasicSqlOptimizer.cs:2135`) — allocates a pooled `SelectQueryOptimizerVisitor` (defined in [SQL-AST](../areas/SQL-AST/INDEX.md)) and runs the bulk of the algebraic simplification: subquery flattening, dead-column removal, constant-folding, predicate canonicalisation.
4. **Optional `JoinsOptimizer.Optimize`** (`Source/LinqToDB/Internal/SqlProvider/JoinsOptimizer.cs:15`) — only when `LinqOptions.OptimizeJoins` is set. Two passes: `RemoveUnusedLeftJoins` drops outer joins to tables not referenced anywhere outside the join condition (`JoinsOptimizer.cs:27`); `RemoveDuplicateJoins` (`JoinsOptimizer.RemoveDuplicates.cs:18`) merges joins to the same table on equal keys via `TryMergeSources`/`TryMergeSources2`. The static `UnnestJoins` helper (`JoinsOptimizer.cs:46`) flattens compatible nested joins to a single level. After joins optimization runs, `FinalizeCte` runs again because the join rewrite may have eliminated CTE usage.
5. **`FinalizeInsert`** (`BasicSqlOptimizer.cs:80`) — collapses redundant `INSERT … SELECT` shapes when the source query has no FROM tables (a common shape from VALUES-style inserts).
6. **`FinalizeSelect`** (`BasicSqlOptimizer.cs:400`) — runs `SqlRowExpandVisitor` to expand `SqlRowExpression` columns into individual columns. `(a,b)` is conceptually a row-valued column; most dialects don't accept that in a select list, so the visitor flattens it. The visitor also flips a `(SqlRow) op (SqlQuery)` predicate into `(SqlQuery) op-swapped (SqlRow)` so the row is on the right (`BasicSqlOptimizer.cs:443`).
7. **`FixSetOperationValues`** (`BasicSqlOptimizer.cs:513`) — walks every `SelectQuery` with set operators and inserts casts on parameters and NULLs across UNION arms when `RequiresCastingParametersForSetOperations` / `RequiresCastingNullValueForSetOperations` are set. SQL Server and PostgreSQL need this; SQLite does not.
8. **`FinalizeStatement`** (`BasicSqlOptimizer.cs:1924`) — provider hook. Default chains `TransformStatement` → `FinalizeUpdate` → `FinalizeInsertOrUpdate` and conditionally normalises parameter order when `IsParameterOrderDependent`. Most concrete optimizers override `FinalizeStatement` to bolt on dialect-specific corrections (Oracle's identity-via-sequence rewrite, MySQL's `LIMIT` placement after `UPDATE`, etc.).

The Update path is the most rewrite-heavy. `BasicCorrectUpdate` (`BasicSqlOptimizer.cs:253`) handles the gap between `UPDATE` semantics. When `SqlProviderFlags.IsUpdateFromSupported` is true, the optimizer leaves the join intact and concats the comparison into the `WHERE` clause; when it's false, it constructs an inner `JOIN` against a clone of the update target, comparing on the table's keys (`BasicSqlOptimizer.cs:301-333`). `RemoveUpdateTableIfPossible` (`BasicSqlOptimizer.cs:876`) is a complementary helper that elides the explicit update-target table source when it can be moved out of the FROM clause without changing semantics.

`ConvertSkipTake` (`BasicSqlOptimizer.cs:1939`) is the second `ISqlOptimizer` entry point. It produces dialect-specific SKIP/TAKE expressions, deciding whether to keep them as parameters or pre-evaluate to literals based on `SqlProviderFlags.GetAcceptsTakeAsParameterFlag(...)` and `LinqOptions.ParameterizeTakeSkip`. This is called from `BasicSqlBuilder.BuildSqlBuilder` (`BasicSqlBuilder.cs:288`) for nested sub-queries and from per-verb builders for the outer query — both before they emit OFFSET/LIMIT/TOP text.

The third entry point, `IsParameterDependent` (`BasicSqlOptimizer.cs:1918`), walks the statement looking for nodes whose shape changes based on parameter values. It feeds the [LINQ](../areas/LINQ/INDEX.md) query cache key — a parameter-dependent statement can't safely be cached by parameter value alone.

Inside `Finalize`, the actual element-by-element rewriting happens via the two visitors `OptimizationContext` carries: `SqlExpressionOptimizerVisitor` (`Source/LinqToDB/Internal/SqlProvider/SqlExpressionOptimizerVisitor.cs:17`, ~2185 lines, provider-agnostic algebraic simplification, NULL-folding, predicate canonicalisation) followed by `SqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/SqlProvider/SqlExpressionConvertVisitor.cs:19`, ~2105 lines, provider-specific dialect translation). The two-phase split means optimizer rules don't have to know about dialect quirks — `OptimizationContext.OptimizeAndConvertAll<T>` (`OptimizationContext.cs:122`) runs them in the canonical order (optimize first, convert second). `BasicSqlOptimizer.CreateOptimizerVisitor` and `CreateConvertVisitor` (`BasicSqlOptimizer.cs:41-49`) are the factories; provider optimizers override these to inject their `SqlExpressionConvertVisitor` subclass with the dialect-specific `Visit*` overrides.

## The dialect override mechanism

There is no plugin or registry. Each provider is one virtual-method-overriding subclass per type. `IDataProvider.CreateSqlBuilder(MappingSchema, DataOptions)` (`Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs:123`) is the factory the runtime calls; concrete data providers (`SqlServerDataProvider`, `MySqlDataProvider`, etc.) implement it as `new XxxSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(), SqlProviderFlags)`.

`<Provider>SqlBuilder` is typically `: BasicSqlBuilder<XxxOptions>` (e.g. `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19` `public abstract class SqlServerSqlBuilder : BasicSqlBuilder<SqlServerOptions>`). The typed base (`BasicSqlBuilder<T>`, `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder{T}.cs:8`) gives the subclass strongly-typed access to its options bag via `ProviderOptions` (lazy `DataOptions.FindOrDefault`).

Most providers further specialize per server version: `SqlServer2008SqlBuilder`, `SqlServer2012SqlBuilder`, …, `SqlServer2025SqlBuilder` all derive from `SqlServerSqlBuilder` and override only the deltas (e.g. `SqlServer2012SqlBuilder` adds `OFFSET … FETCH NEXT` support; older ones use `ROW_NUMBER()` rewrite via `BasicSqlOptimizer.ReplaceTakeSkipWithRowNumber`, see `BasicSqlOptimizer.cs:2076`). Same pattern for Oracle (11/12), Firebird (3/4), MySQL (5.7/8.0/MariaDB), DB2 (LUW/zOS), Access (OleDb/ODBC), SapHana (HDB/ODBC).

The provider-specific overrides cluster around well-known sites:

- **`Convert`** — to wrap identifiers in `[ ]` / `" "` / `` ` `` / `:`-prefixed parameter syntax / etc.
- **`BuildObjectName`** — when the dialect adds linked-server / package qualifiers.
- **`BuildSkipFirst` / `BuildOffsetLimit`** — for `LIMIT`/`OFFSET` vs `TOP`/`FETCH FIRST`/`SKIP` syntactic differences.
- **`BuildFunction` / `BuildSqlCastExpression`** — for `LEN` vs `LENGTH`, `CONVERT` vs `CAST`, etc.
- **`BuildDataTypeFromDataType`** — for the type-name to write inside CREATE TABLE and CAST.
- **`BuildInsertQuery` / `BuildOutputSubclause` / `BuildGetIdentity`** — for identity-column return-value shapes (e.g. `SqlServerSqlBuilder` switches to `OUTPUT INTO @tbl` when the `[Identity]` field is a `Guid` or when `SqlServerOptions.GenerateScopeIdentity` is off, `SqlServerSqlBuilder.cs:42`).
- **`GetReserveSequenceValuesSql` / `GetMaxValueSql`** — for sequence emission and identity-default queries.

The `SqlProviderFlags` capability bag (`Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19`) is a complementary mechanism: instead of overriding a virtual to disable a feature, the provider sets a flag and `BasicSqlBuilder`/`BasicSqlOptimizer` consult it. Examples: `IsSkipSupported`, `IsApplyJoinSupported`, `IsCommonTableExpressionsSupported`, `IsUpdateFromSupported`, `IsInsertOrUpdateSupported`, `OutputDeleteUseSpecialTable`, `IsWindowFunctionsSupported`, `RowConstructorSupport` (a `RowFeature` flag set, `Source/LinqToDB/Internal/SqlProvider/RowFeature.cs:12`), `MaxInListValuesCount`. The `[DataContract]` markings (`SqlProviderFlags.cs:14-17`) are mandatory because remoting (gRPC/WCF/HTTP) round-trips this object across the wire — field reordering would break the contract.

`CustomFlags` (`SqlProviderFlags.cs:25`) is a `HashSet<string>` escape hatch for behavioural toggles too narrow to deserve a typed property — provider extension code uses string keys like `"SqlServer.UseOuterApplyForJoin"`.

## Public `Sql.*` extension surface vs internal builders

`Source/LinqToDB/Sql/` is *not* the engine. It's the user-facing library: server-side helpers users call from inside LINQ queries, plus the attribute hierarchy that lets users declare new server-side translations.

The class is `public static partial class Sql` in namespace `LinqToDB` (note: not `LinqToDB.Sql`, see `Source/LinqToDB/Sql/Sql.cs:24` with a `// ReSharper disable CheckNamespace`). It's split across ~28 partial files by topic:

- **General-purpose markers.** `Sql.AsSql<T>(T)` / `Sql.ToSql<T>(T)` (`Sql.cs:48-63`) force server-side evaluation of an expression that the translator might otherwise compute client-side. `Sql.AsNullable` / `Sql.AsNotNull` / `Sql.AsNotNullable` / `Sql.ToNullable` annotate nullability at the call site. `Sql.AllColumns()` (`Sql.cs:36`) emits `*`. `Sql.Default<T>()` (`Sql.cs:42`) emits the `DEFAULT` keyword for inserts.
- **Type wrappers.** `Sql.Types.*` (`Sql.Types.cs:10`) — typed dummies for `BigInt`, `Decimal(precision,scale)`, `Money`, `DateTime2`, etc. Used as type hints in `[Sql.Function]` decorations.
- **Aggregate helpers.** `Sql.Grouping` (`Sql.GroupBy.cs:30`) and `Sql.GroupBy.{Rollup,Cube,GroupingSets}` (`Sql.GroupBy.cs:5-11`). `Sql.StringAggregate` (`Sql.Strings.cs:18`).
- **Window-function fluent ladder.** `Sql.Window` instance (`Sql.ExtensionAttribute.cs:31`) is the receiver for the `WindowFunctionBuilder` interface ladder declared in `Sql.Window.cs` — gated as "Hidden until fully implemented" (`Sql.Window.cs:15`).
- **Date/time helpers.** `Sql.DateAdd`, `Sql.DatePart`, `Sql.DateDiff`, `Sql.DateParts` enum, `MakeDateTime`, `Truncate`, etc. — split across `Sql.DateTime.cs`, `Sql.DateOnly.cs`, `Sql.DateTimeOffset.cs`. The latter two duplicate most of `Sql.DateTime.cs`'s API by data type.
- **Predicates/operators.** `Sql.Between`, `Sql.NotBetween` (`Sql.Between.cs:9-23`). `Sql.Collate` (`Sql.Collate.cs:24`). `Sql.Ordinal` (`Sql.Special.cs`).
- **Row constructor.** `Sql.Row(...)` and the 1009-line generated `Sql.Row.generated.cs` (T4-emitted overload set). The `RowBuilder` private class (`Sql.Row.cs:10`) is the `IExtensionCallBuilder` that turns the call into an `SqlRowExpression` AST node.

The attribute hierarchy is what binds user methods to server-side expressions. `Sql.ExpressionAttribute` (`Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32`) is the foundational `[Expression("LEN({0})")]`-style decoration. Subclasses:

- **`Sql.PropertyAttribute`** (`Sql.PropertyAttribute.cs:20`) — for properties / parameterless methods (`@@SPID`, `CURRENT_USER`).
- **`Sql.FunctionAttribute`** (`Sql.FunctionAttribute.cs:23`) — `[Sql.Function("LEN")]`-style for named functions; uses the method's name when no explicit name.
- **`Sql.ExtensionAttribute`** (`Sql.ExtensionAttribute.cs`, ~983 lines) — the most powerful: takes a `BuilderType` implementing `IExtensionCallBuilder` for full programmatic control over how the call lowers to the AST.
- **`Sql.TableFunctionAttribute`** (`Sql.TableFunctionAttribute.cs:18`) — for table-valued functions used in `FROM`.
- **`Sql.TableExpressionAttribute`** (`Sql.TableExpressionAttribute.cs:18`) — extends `TableFunctionAttribute`; lets the user inject a raw table-shaped fragment.
- **`Sql.QueryExtensionAttribute`** (`Sql.QueryExtensionAttribute.cs:21`) + **`Sql.QueryExtensionScope`** (`Sql.QueryExtensionScope.cs:10`) — declare query-shape extensions: `TableHint`, `IndexHint`, `JoinHint`, `SubQueryHint`, `QueryHint`, `TableNameHint`, `TablesInScopeHint`, plus `None` (used with `NoneExtensionBuilder` to skip a hint per provider).
- **`Sql.EnumAttribute`** (`Sql.EnumAttribute.cs:13`) — undocumented (`// TODO: write xml doc what it does (I have no idea)` at `Sql.EnumAttribute.cs:11`).

The four built-in `ISqlQueryExtensionBuilder` implementations are siblings of the engine code, sitting in `Source/LinqToDB/Internal/SqlProvider/`:

- `HintExtensionBuilder` (`HintExtensionBuilder.cs:7`) — emits a raw text hint.
- `HintWithParameterExtensionBuilder` (`HintWithParameterExtensionBuilder.cs:8`) — emits `hint(arg)`.
- `HintWithParametersExtensionBuilder` (`HintWithParametersExtensionBuilder.cs:9`) — emits `hint(arg1, arg2, …)` with custom delimiters.
- `HintWithFormatParametersExtensionBuilder` (`HintWithFormatParametersExtensionBuilder.cs:9`) — uses `String.Format` against a stored format template.

All four resolve `Sql.SqlID` placeholders (`Sql.TableID.cs:11`) by calling `ISqlBuilder.BuildSqlID(id)` (`ISqlBuilder.cs:46`). `Sql.SqlID` is a `(SqlIDType, string)` pair (`Sql.TableIDType.cs:5`) — `TableAlias`, `TableName`, or `TableSpec` — used so a hint can refer back to a tagged table by ID rather than by string concatenation that would have to know the alias the optimizer chose.

## Relationship to `SQL-AST`

The SQL provider is the consumer side of the AST contract. Every type the builder switches on (`SqlSelectStatement`, `SqlInsertStatement`, `SqlUpdateStatement`, `SqlDeleteStatement`, `SqlMergeStatement`, `SelectQuery`, `SqlField`, `SqlValue`, `SqlParameter`, `SqlFunction`, `SqlExpression`, `SqlBinaryExpression`, `SqlCastExpression`, `SqlRowExpression`, `SqlSearchCondition`, `SqlPredicate.*`, `CteClause`, `SqlJoinedTable`, `SqlTableSource`, …) is owned by [SQL-AST](../areas/SQL-AST/INDEX.md). The provider folder doesn't define any AST nodes — it only emits them.

There are three places where the boundary leaks:

1. **`PseudoFunctions`** ([SQL-AST](../areas/SQL-AST/INDEX.md), `Source/LinqToDB/Internal/SqlQuery/PseudoFunctions.cs:9`) — well-known `$ToLower$`, `$Convert_Format$`, `$merge_action$` constants. Translators emit these into `SqlFunction` nodes with the pseudo-name; provider `SqlExpressionConvertVisitor` subclasses rewrite them to dialect-specific real function calls (`LOWER`, `CONVERT`, `STRING_AGG`, etc.). The dollar-sign prefix prevents collision with real SQL function names.
2. **`SqlRowExpression`** — emitted by user `Sql.Row(...)` calls and by the translator for tuple comparisons. The optimizer's `SqlRowExpandVisitor` (`BasicSqlOptimizer.cs:408`) flattens row-valued *columns* into individual columns before emission, but row-valued *predicates* survive into the builder's `BuildSqlRow` (`BasicSqlBuilder.cs:3787`) which dispatches based on `SqlProviderFlags.RowConstructorSupport`.
3. **`SqlAnchor`** — `Inserted` / `Deleted` / `TableSource` markers used during MERGE / OUTPUT translation. The optimizer's `CorrectOutputTables` (`BasicSqlOptimizer.cs:571`) resolves them to the right source before the builder walks the output clause.

The optimizer also reads several [SQL-AST](../areas/SQL-AST/INDEX.md)-owned helpers heavily: `QueryHelper.IsDependsOnSource`, `QueryHelper.GetUnderlyingField`, `QueryHelper.UnwrapNullablity`, `QueryHelper.SuggestDbDataType`, `NullabilityContext.GetContext`, `EvaluationContext` for constant folding. The pooled `SelectQueryOptimizerVisitor` (`Source/LinqToDB/Internal/SqlQuery/Visitors/SelectQueryOptimizerVisitor.cs:16`) is allocated from the [SQL-AST](../areas/SQL-AST/INDEX.md) `QueryHelper.SelectOptimizer` pool (`BasicSqlOptimizer.cs:2137`). The optimizer doesn't *own* the visitor; it owns the orchestration that runs it at the right time.

## Pointers

- `Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:11` — emission contract.
- `Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:7` — optimizer contract.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:179` — main `BuildSql` entry.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:367` — `BuildSqlImpl` dispatch by `QueryType`.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:4048` — `Step` enum.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:51` — `Finalize` pipeline.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:1939` — `ConvertSkipTake`.
- `Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19` — `SqlProviderFlags`.
- `Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:13` — bag carried through optimization + builder.
- `Source/LinqToDB/Internal/SqlProvider/JoinsOptimizer.cs:15` — joins-optimizer entry.
- `Source/LinqToDB/Sql/Sql.cs:27` — `public static partial class Sql`.
- `Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32` — `[Expression]` foundation.
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19` — example provider override.
- [areas/SQL-PROVIDER/INDEX.md](../areas/SQL-PROVIDER/INDEX.md) — area index.
- [areas/SQL-AST/INDEX.md](../areas/SQL-AST/INDEX.md) — AST contract.
- [architecture/query-pipeline.md](query-pipeline.md) — full pipeline narrative.
- [architecture/overview.md](overview.md) — global architecture overview.
