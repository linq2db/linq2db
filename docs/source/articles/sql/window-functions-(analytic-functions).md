# Window (Analytic) Functions

A window function computes a value for each row from a set of rows related to it - its *window* - without collapsing the rows the way `GROUP BY` does. Row numbers, rankings, running totals and "previous row" lookups are typical uses.

In `LINQ To DB` window functions are methods of the static `Sql.Window` property. Each method takes the function arguments followed by a lambda that describes the `OVER` clause.

> The legacy `Sql.Ext.[Function]().Over()...ToValue()` chain, declared in [AnalyticFunctions.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/AnalyticFunctions.cs), is superseded by `Sql.Window`. Both produce the same SQL, but some result types differ: `Sql.Ext.NTile(4)` is `int` where `Sql.Window.NTile` is `long`, and `Sql.Ext.Average<double>(...)` reads a `NULL` average as `0` where `Sql.Window.Average` over a nullable column returns `null`. The [L2DB1001](https://github.com/linq2db/linq2db/wiki/L2DB1001) analyzer reports it and offers a code fix that migrates it.

## A First Example

For every child row: its position among the children of the same parent, the running sum of `ChildID` up to that row, and the `ChildID` of the previous row.

```cs
var q =
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID
	select new
	{
		RowNumber = Sql.Window.RowNumber(w => w.PartitionBy(p.ParentID).OrderBy(c.ChildID)),
		Sum       = Sql.Window.Sum(c.ChildID, w => w.PartitionBy(p.ParentID).OrderBy(c.ChildID)),
		Lag       = Sql.Window.Lag(c.ChildID, w => w.PartitionBy(p.ParentID).OrderBy(c.ChildID)),
	};
```

```sql
SELECT
	ROW_NUMBER() OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID]),
	SUM([c_1].[ChildID]) OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID]),
	LAG([c_1].[ChildID]) OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID])
FROM
	[Parent] [p]
		INNER JOIN [Child] [c_1] ON [p].[ParentID] = [c_1].[ParentID]
```

`PartitionBy` splits the rows into independent groups, and `OrderBy` orders the rows inside each group. Unless stated otherwise, the SQL in this article is what `LINQ To DB` generates for SQLite.

## Call Syntax

```cs
Sql.Window.[Function]([Parameters,] w => w
	.[PartitionPart]
	.[OrderByPart]
	.[FramePart]);
```

The lambda only describes the `OVER` clause. It is translated to SQL, not executed, and a window function can only be used inside a query.

| Part      | Calls                                                                                     | SQL                                   |
| --------- | ----------------------------------------------------------------------------------------- | ------------------------------------- |
| Partition | `PartitionBy(expr1, expr2, ...)`                                                          | `PARTITION BY expr1, expr2, ...`      |
| Order     | `OrderBy(expr)`, `OrderByDesc(expr)`, then any number of `ThenBy(expr)`, `ThenByDesc(expr)` | `ORDER BY expr [DESC], ...`           |
| Frame     | `RowsBetween`, `RangeBetween`, `GroupsBetween` and their shortcuts, see [Frames](#frames) | `ROWS BETWEEN ... AND ...`, also `RANGE` and `GROUPS` |

The parts are written in this order, and which of them a function accepts depends on the function - see [What Each Function Accepts](#what-each-function-accepts). For the functions where every part is optional, an empty lambda is a valid window: `Sql.Window.Count(w => w)` is `COUNT(*) OVER ()`, a count of all rows of the query.

The following query shows several functions, multiple partition and sort keys, and a frame:

```cs
var q =
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID
	select new
	{
		Rank = Sql.Window.Rank(w => w
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)
			.ThenBy(c.ChildID)
			.ThenBy(c.ParentID)),

		RowNumber = Sql.Window.RowNumber(w => w
			.PartitionBy(p.Value1, c.ChildID)
			.OrderByDesc(p.Value1)
			.ThenBy(c.ChildID)
			.ThenByDesc(c.ParentID)),

		DenseRank = Sql.Window.DenseRank(w => w
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)),

		Sum = Sql.Window.Sum(p.Value1, w => w
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)),

		Avg = Sql.Window.Average(p.Value1, w => w
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)),

		Count = Sql.Window.Count(p.ParentID, w => w
			.PartitionBy(p.Value1)
			.OrderBy(p.Value1)
			.RangeBetween.Unbounded.And.CurrentRow),
	};

var res = q.ToArray();
```

```sql
SELECT
	RANK() OVER (PARTITION BY [p].[Value1], [c_1].[ChildID] ORDER BY [p].[Value1], [c_1].[ChildID], [c_1].[ParentID]),
	ROW_NUMBER() OVER (PARTITION BY [p].[Value1], [c_1].[ChildID] ORDER BY [p].[Value1] DESC, [c_1].[ChildID], [c_1].[ParentID] DESC),
	DENSE_RANK() OVER (PARTITION BY [p].[Value1], [c_1].[ChildID] ORDER BY [p].[Value1]),
	SUM([p].[Value1]) OVER (PARTITION BY [p].[Value1], [c_1].[ChildID] ORDER BY [p].[Value1]),
	AVG([p].[Value1]) OVER (PARTITION BY [p].[Value1], [c_1].[ChildID] ORDER BY [p].[Value1]),
	COUNT([p].[ParentID]) OVER (PARTITION BY [p].[Value1] ORDER BY [p].[Value1] RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
FROM
	[Parent] [p]
		INNER JOIN [Child] [c_1] ON [p].[ParentID] = [c_1].[ParentID]
```

### Result Types

The C# type of a window function is fixed by its declaration, not by the database:

- `RowNumber`, `Rank`, `DenseRank` and `NTile` return `long`; `PercentRank` and `CumeDist` return `double`.
- `Count` returns `int`, `LongCount` returns `long`.
- `Sum`, `Min` and `Max` return the type of their argument, nullable for a nullable argument.
- `Average` returns `double` for integer arguments (`double?` for nullable ones, so `Sql.Window.Average(p.Value1, ...)` with an `int?` column is `double?`), and the argument type for `decimal`, `double` and `float`.
- `Lead`, `Lag`, `FirstValue`, `LastValue` and `NthValue` return the type of the expression they read; `PercentileCont` and `PercentileDisc` return the type of their sort key.
- The statistical functions (`StdDev`, `Variance`, `Corr`, `Median`, `RatioToReport`, `REGR_*` and so on) return `double?`; `RegrCount` returns `long?`.

## Ordering

`OrderBy`, `OrderByDesc`, `ThenBy` and `ThenByDesc` have an overload that takes the position of `NULL` values, `Sql.NullsPosition.First` or `Sql.NullsPosition.Last`. Databases without `NULLS FIRST` / `NULLS LAST` get an extra sort key instead. On SQL Server:

```cs
var q =
	from p in db.Parent
	select Sql.Window.RowNumber(w => w.OrderBy(p.Value1, Sql.NullsPosition.Last));
```

```sql
SELECT
	ROW_NUMBER() OVER (ORDER BY IIF([p].[Value1] IS NULL, 1, 0), [p].[Value1])
FROM
	[Parent] [p]
```

A sort key that has the same value for every row - a literal, or a local variable, which reaches SQL as a parameter - orders nothing and is removed from the `ORDER BY`.

## Frames

A frame narrows the window to a range of rows around the current row. It is written after the ordering and starts with one of three properties that select the frame unit:

| Builder call                                                  | SQL                                             |
| ------------------------------------------------------------- | ----------------------------------------------- |
| `RowsBetween`, `RangeBetween`, `GroupsBetween`                | `ROWS BETWEEN`, `RANGE BETWEEN`, `GROUPS BETWEEN` |
| `.Unbounded` as the start / as the end                        | `UNBOUNDED PRECEDING` / `UNBOUNDED FOLLOWING`   |
| `.CurrentRow`                                                 | `CURRENT ROW`                                   |
| `.ValuePreceding(n)`, `.ValueFollowing(n)`                    | `n PRECEDING`, `n FOLLOWING`                    |
| `.And`                                                        | `AND`                                           |
| `RowsBetweenValues(p, f)`, `RangeBetweenValues(p, f)`, `GroupsBetweenValues(p, f)` | `ROWS BETWEEN p PRECEDING AND f FOLLOWING`, also `RANGE` and `GROUPS` |
| `.ExcludeCurrentRow()`, `.ExcludeGroup()`, `.ExcludeTies()` after the end boundary | `EXCLUDE CURRENT ROW`, `EXCLUDE GROUP`, `EXCLUDE TIES` |

```cs
var q =
	from c in db.Child
	select new
	{
		// from the first row of the partition to the current row
		RunningTotal = Sql.Window.Sum(c.ChildID, w => w
			.PartitionBy(c.ParentID)
			.OrderBy(c.ChildID)
			.RowsBetween.Unbounded.And.CurrentRow),

		// the previous row, the current row and the next row
		MovingSum = Sql.Window.Sum(c.ChildID, w => w
			.PartitionBy(c.ParentID)
			.OrderBy(c.ChildID)
			.RowsBetweenValues(1, 1)),

		// the whole partition
		PartitionTotal = Sql.Window.Sum(c.ChildID, w => w
			.PartitionBy(c.ParentID)
			.OrderBy(c.ChildID)
			.RowsBetween.Unbounded.And.Unbounded),
	};
```

```sql
SELECT
	SUM([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
	SUM([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID] ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING),
	SUM([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
FROM
	[Child] [c_1]
```

`ROWS` counts physical rows. `RANGE` and `GROUPS` work with the values of the sort key, so a `RANGE` or `GROUPS` frame with a value offset needs exactly one sort key:

```cs
var q =
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID
	select Sql.Window.Sum(c.ChildID, w => w
		.OrderBy(c.ChildID)
		.RangeBetween.ValuePreceding(1).And.CurrentRow);
```

```sql
SELECT
	SUM([c_1].[ChildID]) OVER (ORDER BY [c_1].[ChildID] RANGE BETWEEN 1 PRECEDING AND CURRENT ROW)
FROM
	[Parent] [p]
		INNER JOIN [Child] [c_1] ON [p].[ParentID] = [c_1].[ParentID]
```

Not every database has every frame kind; `GROUPS` frames and `EXCLUDE` are the rarest. See [Database Support](#database-support).

## Neighbouring Rows: Lead, Lag, FirstValue, LastValue, NthValue

`Lag` and `Lead` read a value from a row before or after the current one. Both have overloads with an offset, and with an offset and a default value that is returned when there is no such row. `FirstValue`, `LastValue` and `NthValue` read a value from a row of the frame.

```cs
var q =
	from c in db.Child
	select new
	{
		Previous       = Sql.Window.Lag(c.ChildID, w => w.PartitionBy(c.ParentID).OrderBy(c.ChildID)),
		PreviousOrZero = Sql.Window.Lag(c.ChildID, 1, 0, w => w.PartitionBy(c.ParentID).OrderBy(c.ChildID)),
		NextButOne     = Sql.Window.Lead(c.ChildID, 2, w => w.PartitionBy(c.ParentID).OrderBy(c.ChildID)),
		First          = Sql.Window.FirstValue(c.ChildID, w => w.PartitionBy(c.ParentID).OrderBy(c.ChildID)),
		Last           = Sql.Window.LastValue(c.ChildID, w => w
			.PartitionBy(c.ParentID)
			.OrderBy(c.ChildID)
			.RowsBetween.Unbounded.And.Unbounded),
	};
```

```sql
SELECT
	LAG([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID]),
	LAG([c_1].[ChildID], 1, 0) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID]),
	LEAD([c_1].[ChildID], 2) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID]),
	FIRST_VALUE([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID]),
	LAST_VALUE([c_1].[ChildID]) OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
FROM
	[Child] [c_1]
```

`LastValue` is given a frame that extends to the end of the partition; this is how to get the last value of the whole partition rather than of the rows up to the current one.

For a non-nullable column, a missing row reads as the default value of the type: `Previous` above is `0` for the first child of every parent. Cast the expression to a nullable type, as in `Sql.Window.Lag((int?)c.ChildID, ...)`, to get `null` instead, or pass the default value explicitly.

These functions take their modifiers at the start of the lambda, before the window itself: `IgnoreNulls()` / `RespectNulls()` for all five, and `FromFirst()` / `FromLast()` (before the null treatment) for `NthValue`. `IGNORE NULLS` and `FROM LAST` exist on few databases, see [Database Support](#database-support).

## Aggregate Modifiers: Filter and Distinct

Aggregate functions accept `Filter(condition)`, which limits the rows the aggregate sees, as the first call of the lambda. Where the database has no `FILTER (WHERE ...)` clause - everywhere but PostgreSQL, DuckDB and Access (LibRed) - the condition moves inside the aggregate:

```cs
var q =
	from c in db.Child
	select Sql.Window.Count(w => w.Filter(c.ChildID > 20).PartitionBy(c.ParentID));
```

```sql
SELECT
	COUNT(CASE
		WHEN [c_1].[ChildID] > 20 THEN 1
		ELSE NULL
	END) OVER (PARTITION BY [c_1].[ParentID])
FROM
	[Child] [c_1]
```

Single-argument aggregates also accept `Distinct()`, before `Filter`: `Sql.Window.Count(c.ChildID, w => w.Distinct().PartitionBy(c.ParentID))` is `COUNT(DISTINCT ...) OVER (...)`. Most databases do not allow `DISTINCT` in a window aggregate, see [Database Support](#database-support).

## Reusing a Window Definition

When several functions use the same window, define it once with `Sql.Window.DefineWindow` and pass it to each function with `UseWindow`. `DefineWindow` takes the same partitioning, ordering and frame calls as the function lambdas.

```cs
var q =
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID
	let wnd = Sql.Window.DefineWindow(w => w
		.PartitionBy(p.ParentID)
		.OrderBy(c.ChildID))
	select new
	{
		RowNumber = Sql.Window.RowNumber(w => w.UseWindow(wnd)),
		Sum       = Sql.Window.Sum(c.ChildID, w => w.UseWindow(wnd)),
		Lag       = Sql.Window.Lag(c.ChildID, w => w.UseWindow(wnd)),
	};
```

The definition is written out in every `OVER` clause - no `WINDOW` clause is generated - so this query produces exactly the SQL of [the first example](#a-first-example), which repeats the window in each function.

`UseWindow` takes the place of the partitioning, ordering and frame calls, so nothing can follow it. Function modifiers such as `Distinct()` or `IgnoreNulls()` go before it. On Oracle:

```cs
var q =
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID
	let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(p.ParentID).OrderBy(c.ChildID))
	select new
	{
		Lag = Sql.Window.Lag(p.Value1, w => w.IgnoreNulls().UseWindow(wnd)),
	};
```

```sql
SELECT
	LAG(p."Value1") IGNORE NULLS OVER (PARTITION BY p."ParentID" ORDER BY c_1."ChildID")
FROM
	"Parent" p
		INNER JOIN "Child" c_1 ON p."ParentID" = c_1."ParentID"
```

`Filter` and `KeepFirst` / `KeepLast` cannot be combined with `UseWindow`.

## Filtering by a Window Function

SQL does not allow a window function in `WHERE`, and `LINQ To DB` rejects such a query with `Window functions cannot be used in search condition.` Select the function in a subquery and filter the outer query instead. The first child of every parent:

```cs
var firstChildren =
	from x in
		from c in db.Child
		select new
		{
			Child     = c,
			RowNumber = Sql.Window.RowNumber(w => w.PartitionBy(c.ParentID).OrderBy(c.ChildID)),
		}
	where x.RowNumber == 1
	select x.Child;
```

```sql
SELECT
	[x].[ParentID],
	[x].[ChildID]
FROM
	(
		SELECT
			ROW_NUMBER() OVER (PARTITION BY [c_1].[ParentID] ORDER BY [c_1].[ChildID]) as [RowNumber],
			[c_1].[ParentID],
			[c_1].[ChildID]
		FROM
			[Child] [c_1]
	) [x]
WHERE
	[x].[RowNumber] = 1
```

## Compile-Time and Run-Time Checks

The builder lambdas are typed so that most invalid `OVER` clauses do not compile: ranking functions and `LEAD` / `LAG` need an ordering and take no frame, `KeepFirst()` / `KeepLast()` need an ordering, and nothing can follow `UseWindow`.

What the types cannot express is checked when the query is translated, before anything is sent to the database, and reported as a `LinqToDBException`. The reason is at the end of the exception message, after `Additional details:`.

- A function or clause the current database does not support, e.g. `NTH_VALUE is not supported by current provider.` on SQL Server, or `KEEP clause is not supported by current provider.` on any database but Oracle. Some clauses are emulated rather than rejected: `Filter(...)` becomes a `CASE WHEN` inside the aggregate, and `NULLS FIRST` / `NULLS LAST` become an extra sort key.
- A `RANGE` or `GROUPS` frame with a value offset, such as `RangeBetween.ValuePreceding(1)`, with more than one sort key: `A RANGE or GROUPS frame with a value offset requires exactly one ORDER BY expression.`
- A window function in a `where` condition: `Window functions cannot be used in search condition.` See [Filtering by a Window Function](#filtering-by-a-window-function).

Two shapes compile without an ordering and are not rejected: a frame given before `OrderBy`, as in `w.PartitionBy(x).RowsBetweenValues(1, 1)`, and a `DefineWindow` window without `OrderBy` passed to a ranking function. Databases that require an ordering there get a constant stand-in, such as `ORDER BY (SELECT 1)` on SQL Server, and the others run the window unordered. Either way the rows are taken in whatever order the database reads them, so add an `OrderBy` when the result depends on it.

## Reference

### What Each Function Accepts

Calls in brackets are optional; calls are written in the order shown. `UseWindow(wnd)` replaces the partition, order and frame parts.

| Functions | Lambda |
| --- | --- |
| `RowNumber`, `Rank`, `DenseRank`, `PercentRank`, `CumeDist`, `NTile` | `[PartitionBy]` `OrderBy` `[ThenBy...]`, or `UseWindow` |
| `Lead`, `Lag` | `[IgnoreNulls]` or `[RespectNulls]`, then `[PartitionBy]` `OrderBy` `[ThenBy...]`, or `UseWindow` |
| `FirstValue`, `LastValue` | `[IgnoreNulls]` or `[RespectNulls]`, then `[PartitionBy]` `[OrderBy...]` `[frame]`, or `UseWindow` |
| `NthValue` | `[FromFirst]` or `[FromLast]`, `[IgnoreNulls]` or `[RespectNulls]`, then `[PartitionBy]` `[OrderBy...]` `[frame]`, or `UseWindow` |
| `Count()`, `LongCount()` (`COUNT(*)`) | `[Filter]` `[PartitionBy]` `[OrderBy...]` `[frame]`, or `UseWindow`, or `KeepFirst` / `KeepLast` |
| `Count(expr)`, `LongCount(expr)`, `Sum`, `Average`, `Min`, `Max`, `StdDev`, `StdDevPop`, `StdDevSamp`, `Variance`, `VarPop`, `VarSamp` | `[Distinct]`, then as `Count()` |
| `Corr`, `CovarPop`, `CovarSamp`, `Regr*` | `[Filter]` `[PartitionBy]` `[OrderBy...]` `[frame]`, or `UseWindow` |
| `Median`, `RatioToReport` | `[PartitionBy]` |
| `PercentileCont` | `OrderBy` `[PartitionBy]` |
| `PercentileDisc` | `OrderBy` `[ThenBy...]` `[PartitionBy]` |

`KeepFirst()` / `KeepLast()` are followed by `OrderBy` `[ThenBy...]` `[PartitionBy]`.

### Supported Functions

The following table lists the supported window functions and their `LINQ To DB` representation. Function names link to the Oracle Database documentation, which covers every function in the table.

Function                                                                                                                                          | Linq To DB Extension
------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------
[AVG](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/AVG.html)                                                               | `Sql.Window.Average()`
[CORR](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/CORR.html)                                                             | `Sql.Window.Corr()`
[COUNT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/COUNT.html)                                                           | `Sql.Window.Count()`, `Sql.Window.LongCount()`
[COVAR_POP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/COVAR_POP.html)                                                   | `Sql.Window.CovarPop()`
[COVAR_SAMP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/COVAR_SAMP.html)                                                 | `Sql.Window.CovarSamp()`
[CUME_DIST](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/CUME_DIST.html)                                                   | `Sql.Window.CumeDist()`
[DENSE_RANK](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/DENSE_RANK.html)                                                 | `Sql.Window.DenseRank()`
[FIRST](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/FIRST.html)                                                           | `Sql.Window.[AggregateFunction](expr, w => w.KeepFirst().OrderBy(...))`
[FIRST_VALUE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/FIRST_VALUE.html)                                               | `Sql.Window.FirstValue()`
[LAG](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LAG.html)                                                               | `Sql.Window.Lag()`
[LAST](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LAST.html)                                                             | `Sql.Window.[AggregateFunction](expr, w => w.KeepLast().OrderBy(...))`
[LAST_VALUE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LAST_VALUE.html)                                                 | `Sql.Window.LastValue()`
[LEAD](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LEAD.html)                                                             | `Sql.Window.Lead()`
[MAX](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/MAX.html)                                                               | `Sql.Window.Max()`
[MEDIAN](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/MEDIAN.html)                                                         | `Sql.Window.Median()`
[MIN](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/MIN.html)                                                               | `Sql.Window.Min()`
[NTH_VALUE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/NTH_VALUE.html)                                                   | `Sql.Window.NthValue()`
[NTILE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/NTILE.html)                                                           | `Sql.Window.NTile()`
[PERCENT_RANK](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/PERCENT_RANK.html)                                             | `Sql.Window.PercentRank()`
[PERCENTILE_CONT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/PERCENTILE_CONT.html)                                       | `Sql.Window.PercentileCont()`
[PERCENTILE_DISC](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/PERCENTILE_DISC.html)                                       | `Sql.Window.PercentileDisc()`
[RANK](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/RANK.html)                                                             | `Sql.Window.Rank()`
[RATIO_TO_REPORT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/RATIO_TO_REPORT.html)                                       | `Sql.Window.RatioToReport()`
[REGR_ (Linear Regression) Functions](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html) |
[REGR_SLOPE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                          | `Sql.Window.RegrSlope()`
[REGR_INTERCEPT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                      | `Sql.Window.RegrIntercept()`
[REGR_COUNT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                          | `Sql.Window.RegrCount()`
[REGR_R2](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                             | `Sql.Window.RegrR2()`
[REGR_AVGX](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                           | `Sql.Window.RegrAvgX()`
[REGR_AVGY](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                           | `Sql.Window.RegrAvgY()`
[REGR_SXX](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                            | `Sql.Window.RegrSXX()`
[REGR_SYY](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                            | `Sql.Window.RegrSYY()`
[REGR_SXY](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/REGR_-Linear-Regression-Functions.html)                            | `Sql.Window.RegrSXY()`
[ROW_NUMBER](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/ROW_NUMBER.html)                                                 | `Sql.Window.RowNumber()`
[STDDEV](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/STDDEV.html)                                                         | `Sql.Window.StdDev()`
[STDDEV_POP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/STDDEV_POP.html)                                                 | `Sql.Window.StdDevPop()`
[STDDEV_SAMP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/STDDEV_SAMP.html)                                               | `Sql.Window.StdDevSamp()`
[SUM](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/SUM.html)                                                               | `Sql.Window.Sum()`
[VAR_POP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/VAR_POP.html)                                                       | `Sql.Window.VarPop()`
[VAR_SAMP](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/VAR_SAMP.html)                                                     | `Sql.Window.VarSamp()`
[VARIANCE](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/VARIANCE.html)                                                     | `Sql.Window.Variance()`

`FIRST` and `LAST` are Oracle's `KEEP (DENSE_RANK FIRST | LAST ORDER BY ...)` clause, and in `PERCENTILE_CONT` / `PERCENTILE_DISC` the `OrderBy` is the `WITHIN GROUP` ordering. Both are written with the ordering before the partition. These are expressions taken from one query and the Oracle SQL generated for them:

```cs
Sql.Window.Min(c.ChildID, w => w.KeepFirst().OrderBy(p.Value1).PartitionBy(p.ParentID))
Sql.Window.Max(c.ChildID, w => w.KeepLast().OrderBy(p.Value1))
Sql.Window.PercentileCont(0.5, w => w.OrderBy(c.ChildID).PartitionBy(p.ParentID))
```

```sql
MIN(c_1."ChildID") KEEP (DENSE_RANK FIRST ORDER BY p."Value1") OVER (PARTITION BY p."ParentID")
MAX(c_1."ChildID") KEEP (DENSE_RANK LAST ORDER BY p."Value1") OVER ()
PERCENTILE_CONT(0.5D) WITHIN GROUP (ORDER BY c_1."ChildID") OVER (PARTITION BY p."ParentID")
```

Some functions are not sent to every database under their own name: `RatioToReport` is `RATIO_TO_REPORT` on Oracle and DB2 and a division by `SUM(expr) OVER (...)` elsewhere, and `StdDev` / `Variance` always mean the *sample* standard deviation and variance, whichever function name the database uses for them.

>If you have found that your database supports function that is not listed in table above, please create a feature request or PR. The functions are declared in [Sql.Window.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/Sql.Window.cs), and [WindowFunctions.FeatureMatrix.md](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/WindowFunctions.FeatureMatrix.md) lists the functions and clauses each database supports.

### Aggregates over a Group

A few related functions are aggregates of a `group by` rather than window functions, and are called on the group.

`PercentileCont` and `PercentileDisc` have a group form, `PERCENTILE_CONT(...) WITHIN GROUP (ORDER BY ...)` without `OVER`, and `Rank`, `DenseRank`, `PercentRank` and `CumeDist` have a hypothetical-set form that returns the rank a given value would have in the group. On Oracle:

```cs
var q =
	from c in db.Child
	group c by c.ParentID into g
	select new
	{
		g.Key,
		Median = g.PercentileCont(0.5, (e, w) => w.OrderBy(e.ChildID)),
		Rank   = g.Rank(20, (e, w) => w.OrderBy(e.ChildID)),
	};
```

```sql
SELECT
	g_1."ParentID",
	PERCENTILE_CONT(0.5D) WITHIN GROUP (ORDER BY g_1."ChildID"),
	RANK(20) WITHIN GROUP (ORDER BY g_1."ChildID")
FROM
	"Child" g_1
GROUP BY
	g_1."ParentID"
```

[LISTAGG](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LISTAGG.html) has no `Sql.Window` equivalent. To concatenate strings, use `string.Join` over a group:

```cs
var q =
	from c in db.Child
	group c by c.ParentID into g
	select new
	{
		g.Key,
		Ids = string.Join(", ", g.OrderBy(x => x.ChildID).Select(x => x.ChildID.ToString())),
	};
```

```sql
SELECT
	g_1."ParentID",
	LISTAGG(CAST(g_1."ChildID" AS VarChar(255)), ', ') WITHIN GROUP (ORDER BY g_1."ChildID")
FROM
	"Child" g_1
GROUP BY
	g_1."ParentID"
```

### Database Support

`Sql.Window` is available for:

- [Oracle](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/Analytic-Functions.html)
- [SQL Server 2012+](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-over-clause-transact-sql?view=sql-server-ver16)
- [PostgreSQL](https://www.postgresql.org/docs/current/tutorial-window.html)
- [MySQL 8](https://dev.mysql.com/doc/refman/8.0/en/window-functions-usage.html)
- [MariaDB 10.3+](https://mariadb.com/kb/en/window-functions/)
- [SQLite](https://www.sqlite.org/windowfunctions.html)
- [ClickHouse](https://clickhouse.com/docs/en/sql-reference/window-functions/)
- [DuckDB](https://duckdb.org/docs/stable/sql/functions/window_functions)
- DB2 ([LUW](https://www.ibm.com/docs/en/db2/11.1?topic=expressions-olap-specification), [z/OS](https://www.ibm.com/docs/en/db2-for-zos/12?topic=expressions-olap-specification), [iSeries](https://www.ibm.com/docs/en/i/7.3?topic=statement-using-olap-specifications))
- [Informix](https://www.ibm.com/docs/en/informix-servers/12.10?topic=expressions-over-clause-olap-window)
- [SAP HANA](https://help.sap.com/docs/SAP_HANA_PLATFORM/4fe29514fd584807ac9f2a04f6754767/20a353327519101495dfd0a87060a0d3.html?version=2.0.00)
- [Firebird 3+](https://www.firebirdsql.org/file/documentation/release_notes/html/en/3_0/rnfb30-dml-windowfuncs.html)
- [YDB](https://github.com/ydb-platform/ydb/blob/main/ydb/docs/en/core/yql/reference/syntax/select/window.md)
- Access, only with the LibRed managed engine

Window functions are not available for SQL Server 2005 / 2008, MySQL 5.7, Firebird 2.5, SAP/Sybase ASE, SQL CE, and Access over OLE DB or ODBC.

Not every database in the list has every function and clause. The complete picture is in [WindowFunctions.FeatureMatrix.md](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/WindowFunctions.FeatureMatrix.md); the differences met most often are:

| Feature | Available on |
| --- | --- |
| `NthValue` | all but SQL Server and Informix |
| `Lead` / `Lag` with a default value | all but MariaDB and YDB |
| `NTile`, `PercentRank`, `CumeDist` | all but Firebird 3; `CumeDist` also not on ClickHouse |
| `ROWS` frames | all but Firebird 3 |
| `RANGE` frames | all but Firebird 3, SAP HANA and YDB |
| `GROUPS` frames, frame `EXCLUDE` | PostgreSQL, SQLite, DuckDB, Access (LibRed) |
| `Distinct()` | Oracle, ClickHouse, DuckDB, Access (LibRed) |
| `IgnoreNulls()` | Oracle, SQL Server 2022+, DuckDB, DB2, Informix, Access (LibRed); on YDB for `FirstValue` / `LastValue` / `NthValue` only |
| `FromLast()` | Oracle, DB2, Firebird 3+, Access (LibRed) |
| `KeepFirst()` / `KeepLast()` | Oracle |
| `Median` | Oracle, MariaDB, DuckDB, DB2, SAP HANA |
| `Sql.Window.PercentileCont` / `PercentileDisc` | Oracle, SQL Server, MariaDB, SAP HANA, Access (LibRed) |
| `g.PercentileCont` / `g.PercentileDisc` over a group | Oracle, PostgreSQL, DuckDB, DB2, Access (LibRed) |
| `g.Rank(value, ...)` and the other hypothetical-set forms | Oracle, PostgreSQL |
| `REGR_*` | Oracle, PostgreSQL, DuckDB, DB2, Access (LibRed) |
| `Filter(...)` | all; native on PostgreSQL, DuckDB and Access (LibRed), emulated elsewhere |
| `Sql.NullsPosition` | all; emulated where the database has no `NULLS FIRST` / `NULLS LAST` |
