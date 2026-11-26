# Window (Analytic) Functions

Window functions are implemented as extension methods for static `Sql.Window` property. Each method takes the function arguments followed by a lambda that builds the `OVER` clause (partitioning, ordering and frame) with fluent syntax.

#### Call Syntax

```cs
Sql.Window.[Function]([Parameters,] w => w
	.[PartitionPart]
	.[OrderByPart]
	.[FramePart]);
```

The lambda only describes the `OVER` clause and is translated to SQL, not executed. The calls available in it depend on the function, e.g. ranking functions and `LEAD` / `LAG` require an ordering and accept no frame.

> The legacy `Sql.Ext.[Function]().Over()...ToValue()` chain, declared in [AnalyticFunctions.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/AnalyticFunctions.cs), is superseded by `Sql.Window`. The [L2DB1001](https://github.com/linq2db/linq2db/wiki/L2DB1001) analyzer reports it and offers a code fix that migrates it.

##### Example

```c#
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

##### Resulting SQL

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

#### Reusing a Window Definition

When several functions use the same window, define it once with `Sql.Window.DefineWindow` and pass it to each function with `UseWindow`. `DefineWindow` takes the same partitioning, ordering and frame calls as the function lambdas.

```c#
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

`UseWindow` takes the place of the partitioning, ordering and frame calls, so nothing can follow it; function modifiers such as `Distinct()` or `IgnoreNulls()` go before it. The definition is written out in every `OVER` clause, so the query produces the same SQL as one that repeats the window in each function:

```sql
SELECT
	ROW_NUMBER() OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID]),
	SUM([c_1].[ChildID]) OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID]),
	LAG([c_1].[ChildID]) OVER (PARTITION BY [p].[ParentID] ORDER BY [c_1].[ChildID])
FROM
	[Parent] [p]
		INNER JOIN [Child] [c_1] ON [p].[ParentID] = [c_1].[ParentID]
```

#### Compile-Time and Run-Time Checks

The builder lambdas are typed so that most invalid `OVER` clauses do not compile: ranking functions and `LEAD` / `LAG` need an ordering and take no frame, `KeepFirst()` / `KeepLast()` need an ordering, and nothing can follow `UseWindow`. What the types cannot express is checked when the query is translated, before anything is sent to the database, and reported as a `LinqToDBException`:

- A function or clause the current database does not support, e.g. `NTH_VALUE is not supported by current provider.` on SQL Server, or `KEEP clause is not supported by current provider.` on any database but Oracle. [WindowFunctions.FeatureMatrix.md](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/WindowFunctions.FeatureMatrix.md) lists what each database supports. Some clauses are emulated rather than rejected: `Filter(...)` becomes a `CASE WHEN` inside the aggregate where `FILTER (WHERE ...)` is missing, and `NULLS FIRST` / `NULLS LAST` become an extra sort key.
- A `RANGE` or `GROUPS` frame with a value offset, such as `RangeBetween.ValuePreceding(1)`, with more than one ordering key: `A RANGE or GROUPS frame with a value offset requires exactly one ORDER BY expression.`
- A window function in a `where` condition: `Window functions cannot be used in search condition.` Select it in a subquery and filter the outer query instead:

```c#
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

Two shapes compile without an ordering and are not rejected: a frame given before `OrderBy`, as in `w.PartitionBy(x).RowsBetweenValues(1, 1)`, and a `DefineWindow` window without `OrderBy` passed to a ranking function. Databases that require an ordering there get a constant stand-in, such as `ORDER BY (SELECT 1)` on SQL Server, and the others run the window unordered. Either way the rows are taken in whatever order the database reads them, so add an `OrderBy` when the result depends on it.

#### Supported Functions (could be incomplete)

The following table contains list of supported Window Functions and `LINQ To DB` representation of these functions. Function names link to the Oracle Database documentation, which covers every function in the table.

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

[LISTAGG](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LISTAGG.html) has no `Sql.Window` equivalent. To concatenate strings, use `string.Join` over a group, e.g. `string.Join(", ", g.OrderBy(x => x.Id).Select(x => x.Name))`, which is translated to `LISTAGG(...) WITHIN GROUP (ORDER BY ...)` on Oracle.

>If you have found that your database supports function that is not listed in table above, please create a feature request or PR. The functions are declared in [Sql.Window.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/Sql.Window.cs), and [WindowFunctions.FeatureMatrix.md](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/WindowFunctions.FeatureMatrix.md) lists the functions and clauses each database supports.

#### Window Functions Support

- [Oracle](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/Analytic-Functions.html)
- [MSSQL](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-over-clause-transact-sql?view=sql-server-ver16)
- [SQLite](https://www.sqlite.org/windowfunctions.html)
- [PostreSQL](https://www.postgresql.org/docs/current/tutorial-window.html)
- [MariaDB](https://mariadb.com/kb/en/window-functions/)
- [MySQL 8](https://dev.mysql.com/doc/refman/8.0/en/window-functions-usage.html)
- [DB2 z/OS](https://www.ibm.com/docs/en/db2-for-zos/12?topic=expressions-olap-specification)
- [DB2 LUW](https://www.ibm.com/docs/en/db2/11.1?topic=expressions-olap-specification)
- [DB2 iSeries](https://www.ibm.com/docs/en/i/7.3?topic=statement-using-olap-specifications)
- [Informix](https://www.ibm.com/docs/en/informix-servers/12.10?topic=expressions-over-clause-olap-window)
- [SAP HANA](https://help.sap.com/docs/SAP_HANA_PLATFORM/4fe29514fd584807ac9f2a04f6754767/20a353327519101495dfd0a87060a0d3.html?version=2.0.00)
- [SAP/Sybase ASE](https://infocenter.sybase.com/help/index.jsp?topic=/com.sybase.infocenter.dc38151.1602/doc/html/san1278452950084.html)
- [Firebird 3+](https://www.firebirdsql.org/file/documentation/release_notes/html/en/3_0/rnfb30-dml-windowfuncs.html)
- [ClickHouse](https://clickhouse.com/docs/en/sql-reference/window-functions/)
