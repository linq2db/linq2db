# Window (Analytic) Functions

Window functions are implemented as extension methods for static `Sql.Ext` property. For extensions generation (e.g. partitioning or ordering) fluent syntax is used.

#### Call Syntax

```cs
Sql.Ext.[Function]([Parameters])
	.Over()
	.[PartitionPart]
	.[OrderByPart]
	.[WindowingPart]
	.ToValue();
```

Last function in method chain **must** be function `ToValue()` - it is a mark that method chain is finished.

##### Example

```c#
var q = 
	from p in db.Parent
	join c in db.Child on p.ParentID equals c.ParentID 
	select new
	{
		Rank = Sql.Ext.Rank()
			.Over()
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)
			.ThenBy(c.ChildID)
			.ThenBy(c.ParentID)
			.ToValue(),

		RowNumber = Sql.Ext.RowNumber()
			.Over()
			.PartitionBy(p.Value1, c.ChildID)
			.OrderByDesc(p.Value1)
			.ThenBy(c.ChildID)
			.ThenByDesc(c.ParentID)
			.ToValue(),

		DenseRank = Sql.Ext.DenseRank()
			.Over()
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)
			.ToValue(),

		Sum = Sql.Ext.Sum(p.Value1)
			.Over()
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)
			.ToValue(),

		Avg = Sql.Ext.Average<double>(p.Value1)
			.Over()
			.PartitionBy(p.Value1, c.ChildID)
			.OrderBy(p.Value1)
			.ToValue(),

		Count = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All)
			.Over()
			.PartitionBy(p.Value1)
			.OrderBy(p.Value1)
			.Range.Between.UnboundedPreceding.And.CurrentRow
			.ToValue(),
	};

var res = q.ToArray();
```

##### Resulting SQL

```sql
SELECT
	RANK() OVER(PARTITION BY [p].[Value1], [c7].[ChildID] ORDER BY [p].[Value1], [c7].[ChildID], [c7].[ParentID]) as [c1],
	ROW_NUMBER() OVER(PARTITION BY [p].[Value1], [c7].[ChildID] ORDER BY [p].[Value1] DESC, [c7].[ChildID], [c7].[ParentID] DESC) as [c2],
	DENSE_RANK() OVER(PARTITION BY [p].[Value1], [c7].[ChildID] ORDER BY [p].[Value1]) as [c3],
	SUM([p].[Value1]) OVER(PARTITION BY [p].[Value1], [c7].[ChildID] ORDER BY [p].[Value1]) as [c4],
	AVG([p].[Value1]) OVER(PARTITION BY [p].[Value1], [c7].[ChildID] ORDER BY [p].[Value1]) as [c5],
	COUNT(ALL [p].[ParentID]) OVER(PARTITION BY [p].[Value1] ORDER BY [p].[Value1] RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as [c6]
FROM
	[Parent] [p]
		INNER JOIN [Child] [c7] ON [p].[ParentID] = [c7].[ParentID]
```

#### Supported Functions (could be incomplete)

The following table contains list of supported Window Functions and `LINQ To DB` representation of these functions.

Function                                                                                              | Linq To DB Extension
------------------------------------                                                                           |----------------------
[AVG](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions018.htm)                                 | `Sql.Ext.Average()`
[CORR](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions035.htm)                                | `Sql.Ext.Corr()`
[COUNT](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions039.htm)                               | `Sql.Ext.Count()`
[COVAR_POP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions040.htm)                           | `Sql.Ext.CovarPop()`
[COVAR_SAMP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions041.htm)                          | `Sql.Ext.CovarSamp()`
[CUME_DIST](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions043.htm)                           | `Sql.Ext.CumeDist()`
[DENSE_RANK](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions052.htm)                          | `Sql.Ext.DenseRank()`
[FIRST](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions065.htm)                               | `Sql.Ext.[AggregateFunction].KeepFirst()`
[FIRST_VALUE](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions066.htm)                         | `Sql.Ext.FirstValue()`
[LAG](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions082.htm)                                 | `Sql.Ext.Lag()` 
[LAST](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions083.htm)                                | `Sql.Ext.[AggregateFunction].KeepLast()`
[LAST_VALUE](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions085.htm)                          | `Sql.Ext.LastValue()`
[LEAD](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions086.htm)                                | `Sql.Ext.Lead()`
[LISTAGG](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions089.htm)                             | `Sql.Ext.ListAgg()`
[MAX](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions098.htm)                                 | `Sql.Ext.Max()`
[MEDIAN](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions099.htm)                              | `Sql.Ext.Median()`
[MIN](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions100.htm)                                 | `Sql.Ext.Min()`
[NTH_VALUE](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions114.htm)                           | `Sql.Ext.NthValue()`
[NTILE](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions115.htm)                               | `Sql.Ext.NTile()`
[PERCENT_RANK](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions126.htm)                        | `Sql.Ext.PercentRank()`
[PERCENTILE_CONT](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions127.htm)                     | `Sql.Ext.PercentileCont()`
[PERCENTILE_DISC](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions128.htm)                     | `Sql.Ext.PercentileDisc()`
[RANK](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions141.htm)                                | `Sql.Ext.Rank()`
[RATIO_TO_REPORT](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions142.htm)                     | `Sql.Ext.RatioToReport()`
[REGR_ (Linear Regression) Functions](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions151.htm) | 
REGR_SLOPE                                                                                                     | `Sql.Ext.RegrSlope()`
REGR_INTERCEPT                                                                                                 | `Sql.Ext.RegrIntercept()`
REGR_COUNT                                                                                                     | `Sql.Ext.RegrCount()`
REGR_R2                                                                                                        | `Sql.Ext.RegrR2()`
REGR_AVGX                                                                                                      | `Sql.Ext.RegrAvgX()`
REGR_AVGY                                                                                                      | `Sql.Ext.RegrAvgY()`
REGR_SXX                                                                                                       | `Sql.Ext.RegrSXX()`
REGR_SYY                                                                                                       | `Sql.Ext.RegrSYY()`
REGR_SXY                                                                                                       | `Sql.Ext.RegrSXY()`
[ROW_NUMBER](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions156.htm)                          | `Sql.Ext.RowNumber()`
[STDDEV](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions178.htm)                              | `Sql.Ext.StdDev()`
[STDDEV_POP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions179.htm)                          | `Sql.Ext.StdDevPop()`
[STDDEV_SAMP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions180.htm)                         | `Sql.Ext.StdDevSamp()`
[SUM](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions182.htm)                                 | `Sql.Ext.Sum()`
[VAR_POP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions230.htm)                             | `Sql.Ext.VarPop()`
[VAR_SAMP](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions231.htm)                            | `Sql.Ext.VarSamp()`
[VARIANCE](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions232.htm)                            | `Sql.Ext.Variance()`

>If you have found that your database supports function that is not listed in table above, you can easily create your own extension (but it will be better to create feature request or PR). Code samples are located in [Sql.Analytic.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Sql/Sql.Analytic.cs)

#### Window Functions Support

- [Oracle](https://docs.oracle.com/cd/E11882_01/server.112/e41084/functions004.htm#SQLRF06174)
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
