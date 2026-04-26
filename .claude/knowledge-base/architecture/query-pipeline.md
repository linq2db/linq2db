---
area: GLOBAL
kind: architecture
sources: [code]
confidence: medium
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 8/8
coverage_tier_2: 5/5
---

# Query pipeline

The full path of an `IQueryable<T>` from `db.Customers.Where(...).ToList()` to `SELECT ... FROM ...` text on the wire and back to a `List<Customer>`. Every numbered step cites the file:line where the corresponding code lives.

## 1. Capture: `IQueryable<T>` → `Expression`

The user obtains an `ITable<T>` from `IDataContext` via `dataContext.GetTable<T>()` (`Source/LinqToDB/DataExtensions.cs:29` and below). `ITable<T>` extends `IExpressionQuery<T>` (`Source/LinqToDB/ITable{T}.cs:12`), whose runtime implementation is `ExpressionQueryImpl<T>` → `ExpressionQuery<T>` (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:19`). Each LINQ method (`Where`, `Select`, `Join`, etc.) calls `IQueryProvider.CreateQuery<TElement>` (`ExpressionQuery.cs:287`), which builds a fresh `ExpressionQueryImpl<TElement>` carrying the appended `MethodCallExpression`. No SQL exists yet — only an in-memory expression tree.

## 2. Termination: `ToList` / `First` / `foreach` / `ExecuteAsync`

Terminal LINQ calls go through `IQueryProvider.Execute` / `IAsyncEnumerable.GetAsyncEnumerator` / `IQueryProviderAsync.ExecuteAsync`. They all funnel into the local `ExpressionQuery<T>.GetQuery` (`Source/LinqToDB/Internal/Linq/ExpressionQuery.cs:82`):

```
Query<T> GetQuery(ref IQueryExpressions expression, bool cache, out bool dependsOnParameters)
{
    if (cache && Info != null) return Info;
    var info = Query<T>.GetQuery(DataContext, ref expression, out dependsOnParameters);
    if (cache && info.CompareInfo?.IsFastComparable == true && !dependsOnParameters) Info = info;
    return info;
}
```

`Info` is the per-instance fast cache; the slow path delegates to the static `Query<T>.GetQuery`.

## 3. Cache lookup

`Query<T>.GetQuery` (`Source/LinqToDB/Internal/Linq/Query{T}.cs:287`) does:

1. Run `BinaryExpressionAggregatorVisitor` on the tree (`Query{T}.cs:307`) — flattens unbalanced `Or`/`AndAlso` chains for stable hashing.
2. Apply `IExpressionPreprocessor.ProcessExpression` (`Query{T}.cs:312`) and `IQueryExpressionInterceptor.ProcessExpression` (`Query{T}.cs:315`) so user code can rewrite the tree before caching.
3. Probe `_queryCache.Find(dataContext, runtimeExpressions, queryFlags, false)` (`Query{T}.cs:329`). The cache key is the configuration ID + flag set + a structural-equality hash of the tree (`Query{T}.cs:62`). A hit returns immediately, skipping all of step 4 onwards.
4. On miss, call `ExposeAndPrepareExpression` (`Query{T}.cs:343`) — expands `Expression.Constant` boxing of `IQueryable` instances, captures `ExpressionMethod` lambdas, and substitutes parameter accessors. Re-probe the cache one more time using the exposed tree (`Query{T}.cs:361`).
5. Final miss → `CreateQuery` (`Query{T}.cs:396`).

## 4. AST build: `Expression` → `SqlStatement`

`Query<T>.CreateQuery` (`Query{T}.cs:396`) instantiates `ExpressionBuilder` and calls `Build<T>`:

```
query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, dataContext, expressions.MainExpression, null).Build<T>(ref expressions);
```

`ExpressionBuilder.Build<T>` (`Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs:124`) opens a fresh `SelectQuery` (`Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs:12`), calls `BuildSequence` to walk the expression tree, then `BuildQuery` to project the final result. The walk is a dispatcher: each `MethodCallExpression` tries every registered `ISequenceBuilder` until one accepts it (`ExpressionBuilder.cs:336` `TryBuildSequence`). The registered builders live as siblings under `Source/LinqToDB/Internal/Linq/Builder/` — `SelectBuilder`, `WhereBuilder`, `JoinBuilder`, `GroupByBuilder`, `OrderByBuilder`, `TakeSkipBuilder`, `SelectManyBuilder`, `MergeBuilder`, `MultiInsertBuilder`, `DeleteBuilder`, etc. Each pushes constructs into the active `SelectQuery`'s `Select` / `From` / `Where` / `GroupBy` / `Having` / `OrderBy` clauses (`SelectQuery.cs:70–75`).

A `SelectQuery` is itself an `ISqlTableSource` (`SelectQuery.cs:12`) — sub-queries are first-class. Statements are `SqlStatement` subclasses (`Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs:8`): `SqlSelectStatement`, `SqlInsertStatement`, `SqlUpdateStatement`, `SqlDeleteStatement`, `SqlMergeStatement`, `SqlMultiInsertStatement`, etc., each wrapping a `SelectQuery`.

Member-call → SQL-expression conversion goes through `IMemberTranslator` (`ExpressionBuilder.cs:54`), constructed from the data provider's translator chain plus user-registered ones (`ExpressionBuilder.cs:96`). This is how `string.StartsWith`, `DateTime.Year`, etc., become provider-specific SQL fragments.

## 5. AST optimization: `Finalize`

After build, `BuildQuery` runs `query.SqlOptimizer.Finalize` per `QueryInfo.Statement` (`ExpressionBuilder.cs:184`). The implementation in `BasicSqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:51`) does:

1. `FixEmptySelect` and `FinalizeCte` (`BasicSqlOptimizer.cs:53–54`) — fixes selects with no projection and resolves recursive CTE references.
2. `OptimizeQueries` (`BasicSqlOptimizer.cs:58`) — provider-agnostic subquery flattening, redundant-join removal, predicate hoisting.
3. Conditionally `JoinsOptimizer().Optimize` (`BasicSqlOptimizer.cs:62`) when `LinqOptions.OptimizeJoins` is on.
4. `FinalizeInsert`, `FinalizeSelect`, `FixSetOperationValues` (`BasicSqlOptimizer.cs:68–70`) — INSERT-from-SELECT simplification, distinct/UNION conversion fixups.
5. Provider-specific `FinalizeStatement` hook (`BasicSqlOptimizer.cs:73`) — overridden in each `<X>SqlOptimizer` to apply dialect-specific corrections (e.g. SQL Server identity insert, Oracle hint placement, MySQL `LIMIT` translation).

The optimizer also validates the resulting tree via `SqlProviderHelper.IsValidQuery` (`ExpressionBuilder.cs:188`) against `SqlProviderFlags` — if the provider can't express the query (no `OFFSET`, no `INTERSECT`, etc.), an `SqlErrorExpression` is attached and execution will throw a meaningful error instead of generating broken SQL.

## 6. SQL emission: `SqlStatement` → `string`

When the query is finally executed, `IDataContext.GetQueryRunner` is called (`Source/LinqToDB/IDataContext.cs:112`). For `DataConnection`, this returns `DataConnection.QueryRunner` (`Source/LinqToDB/Data/DataConnection.QueryRunner.cs:37`). `SetCommand(false)` (`DataConnection.QueryRunner.cs:354`) calls `GetCommand` (`DataConnection.QueryRunner.cs:180`), which calls the provider-supplied `ISqlBuilder.BuildSql` to materialize each `SqlStatement` into a `PreparedQuery` (`DataConnection.QueryRunner.cs:162`).

The shared base is `BasicSqlBuilder.BuildSql` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:179`). It dispatches by `statement.QueryType` to `BuildSelectQuery` / `BuildInsertQuery` / `BuildUpdateQuery` / `BuildDeleteQuery` / `BuildMergeStatement` (siblings in the same file). Each provider's `<X>SqlBuilder` subclasses `BasicSqlBuilder` and overrides hooks like `LikeWildcardCharacters`, `BuildLimitClause`, quote-style, identifier escaping, and dialect-specific function rewrites. Set operations (`UNION`, `EXCEPT`, `INTERSECT`) are emitted by recursing into `BuildSql` on each branch (`BasicSqlBuilder.cs:240`) using a fresh per-branch `SqlBuilder` instance and merging the parameter list.

The result is a `PreparedQuery` carrying `CommandWithParameters[] Commands` — multi-statement queries (e.g. INSERT-with-identity) emit more than one entry, hence `CommandCount` is virtual on the builder (`BasicSqlBuilder.cs:114`).

## 7. ADO.NET execution

`QueryRunner.ExecuteNonQueryImpl` (`DataConnection.QueryRunner.cs:408`) iterates `executionQuery.PreparedQuery.Commands`, calling `dataConnection.ExecuteNonQuery()` for each one. The actual `DbCommand` is constructed via `IDataProvider.InitCommand` (`Source/LinqToDB/DataProvider/IDataProvider.cs:42`), which lets the provider customise the command (e.g. set `BindByName` on Oracle, configure parameter prefixes). Parameters set via `IDataProvider.SetParameter` (`IDataProvider.cs:51`) — the provider does any DB-specific value-conversion and `DbType` mapping. Read paths use `DbCommand.ExecuteReader`, returning `DbDataReader` wrapped in `DataReaderWrapper` for tracing.

Around execution, `IExecutionScope` (`IDataProvider.cs:63`) is the per-provider escape hatch: returned from `IDataProvider.ExecuteScope` and disposed when the runner finishes, used today on Oracle to switch thread culture for binary parameter encoding.

## 8. Materialization: `DbDataReader` → `T`

The mapper that turns reader rows into `T` is built during step 4 (`ExpressionBuilder.BuildQuery` → `FinalizeProjection<T>` → `sequence.SetRunQuery(query, finalized)`, `ExpressionBuilder.cs:200`). It is a compiled `Expression<Func<IQueryRunner, DbDataReader, T>>` cached as `Mapper<T>` (`Source/LinqToDB/Internal/Linq/QueryRunner.cs:67`). Per-row mapping calls `mapperInfo.Mapper(queryRunner, dataReader)` (`QueryRunner.cs:90`) inside an `ActivityID.Materialization` scope.

If a row throws `FormatException`, `InvalidCastException`, `LinqToDBConvertException`, or any provider-specific `*NullValueException`, the runner falls back to "slow mode" via `ReMapOnException` (`QueryRunner.cs:108`) — rebuilds the mapper expression with extra `IsDBNull` guards and per-column null tolerance. This is why the first malformed row is slow but subsequent rows in the same query stay fast.

The expressions referenced by the mapper come from `IDataContext.GetReaderExpression` (`IDataContext.cs:83`) — provider supplies the column-read code (`DbDataReader.GetInt32(idx)`, `GetFieldValue<DateTime>(idx)`, etc.); per-column nullability comes from `IsDBNullAllowed` (`IDataContext.cs:90`).

## 9. Connection lifecycle wrap-up

`DataConnection`'s `QueryRunner.Dispose` releases the active `DbCommand` via `IDataProvider.DisposeCommand` (`IDataProvider.cs:43`). `DataContext`'s `QueryRunner` (`DataContext.cs:654`) wraps the inner `DataConnection.QueryRunner` and calls `_dataContext.ReleaseQuery()` after disposal — which closes the underlying connection unless `KeepConnectionAlive` or a `ConnectionLockScope` is held (`DataContext.cs:447`, `DataContext.cs:966`).

For the cache-hit case (step 3), the entire pipeline below is skipped: only step 7 + 8 + 9 run, with the cached `Query<T>` providing the `PreparedQuery` and the compiled mapper.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 8 / 8 ✓
  - Source/LinqToDB/IDataContext.cs
  - Source/LinqToDB/DataContext.cs
  - Source/LinqToDB/Data/DataConnection.cs (head, GetDataConnection, lifecycle)
  - Source/LinqToDB/Data/DataConnection.QueryRunner.cs
  - Source/LinqToDB/Internal/SqlQuery/SelectQuery.cs
  - Source/LinqToDB/Internal/SqlQuery/SqlStatement.cs
  - Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs (BuildSql + dispatch)
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs (Finalize)
  - Source/LinqToDB/DataProvider/IDataProvider.cs
- Tier 2 (visited / total): 5 / 5 ✓
  - Source/LinqToDB/Internal/Linq/Query.cs
  - Source/LinqToDB/Internal/Linq/Query{T}.cs
  - Source/LinqToDB/Internal/Linq/QueryRunner.cs (head, Mapper<T>)
  - Source/LinqToDB/Internal/Linq/ExpressionQuery.cs
  - Source/LinqToDB/Internal/Linq/QueryRunnerBase.cs (referenced via DataConnection.QueryRunner)
- Tier 3 (skipped, logged): 0
</details>
