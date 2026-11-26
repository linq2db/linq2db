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

#### Supported Functions (could be incomplete)

The following table contains list of supported Window Functions and `LINQ To DB` representation of these functions. Function names link to the PostgreSQL documentation, or to the Oracle documentation for functions PostgreSQL does not have.

Function                                                                                                                                     | Linq To DB Extension
-------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------
[AVG](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-TABLE)                                            | `Sql.Window.Average()`
[CORR](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                                | `Sql.Window.Corr()`
[COUNT](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-TABLE)                                          | `Sql.Window.Count()`, `Sql.Window.LongCount()`
[COVAR_POP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                           | `Sql.Window.CovarPop()`
[COVAR_SAMP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                          | `Sql.Window.CovarSamp()`
[CUME_DIST](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                            | `Sql.Window.CumeDist()`
[DENSE_RANK](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                           | `Sql.Window.DenseRank()`
[FIRST](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/FIRST.html)                                                      | `Sql.Window.[AggregateFunction](expr, w => w.KeepFirst().OrderBy(...))`
[FIRST_VALUE](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                          | `Sql.Window.FirstValue()`
[LAG](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                                  | `Sql.Window.Lag()`
[LAST](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/LAST.html)                                                        | `Sql.Window.[AggregateFunction](expr, w => w.KeepLast().OrderBy(...))`
[LAST_VALUE](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                           | `Sql.Window.LastValue()`
[LEAD](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                                 | `Sql.Window.Lead()`
[MAX](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-TABLE)                                            | `Sql.Window.Max()`
[MEDIAN](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/MEDIAN.html)                                                    | `Sql.Window.Median()`
[MIN](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-TABLE)                                            | `Sql.Window.Min()`
[NTH_VALUE](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                            | `Sql.Window.NthValue()`
[NTILE](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                                | `Sql.Window.NTile()`
[PERCENT_RANK](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                         | `Sql.Window.PercentRank()`
[PERCENTILE_CONT](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-ORDEREDSET-TABLE)                               | `Sql.Window.PercentileCont()`
[PERCENTILE_DISC](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-ORDEREDSET-TABLE)                               | `Sql.Window.PercentileDisc()`
[RANK](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                                 | `Sql.Window.Rank()`
[RATIO_TO_REPORT](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/RATIO_TO_REPORT.html)                                  | `Sql.Window.RatioToReport()`
[REGR_ (Linear Regression) Functions](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE) |
REGR_SLOPE                                                                                                                                   | `Sql.Window.RegrSlope()`
REGR_INTERCEPT                                                                                                                               | `Sql.Window.RegrIntercept()`
REGR_COUNT                                                                                                                                   | `Sql.Window.RegrCount()`
REGR_R2                                                                                                                                      | `Sql.Window.RegrR2()`
REGR_AVGX                                                                                                                                    | `Sql.Window.RegrAvgX()`
REGR_AVGY                                                                                                                                    | `Sql.Window.RegrAvgY()`
REGR_SXX                                                                                                                                     | `Sql.Window.RegrSXX()`
REGR_SYY                                                                                                                                     | `Sql.Window.RegrSYY()`
REGR_SXY                                                                                                                                     | `Sql.Window.RegrSXY()`
[ROW_NUMBER](https://www.postgresql.org/docs/current/functions-window.html#FUNCTIONS-WINDOW-TABLE)                                           | `Sql.Window.RowNumber()`
[STDDEV](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                              | `Sql.Window.StdDev()`
[STDDEV_POP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                          | `Sql.Window.StdDevPop()`
[STDDEV_SAMP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                         | `Sql.Window.StdDevSamp()`
[SUM](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-TABLE)                                            | `Sql.Window.Sum()`
[VAR_POP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                             | `Sql.Window.VarPop()`
[VAR_SAMP](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                            | `Sql.Window.VarSamp()`
[VARIANCE](https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE)                            | `Sql.Window.Variance()`

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
