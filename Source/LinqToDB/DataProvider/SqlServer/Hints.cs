using System;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// https://docs.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql
	/// </summary>
	public static class Hints
	{
		public static class Table
		{
			public const string Index             = "Index";
			public const string ForceScan         = "ForceScan";
			public const string ForceSeek         = "ForceSeek";
			public const string HoldLock          = "HoldLock";
			public const string NoLock            = "NoLock";
			public const string NoWait            = "NoWait";
			public const string PagLock           = "PagLock";
			public const string ReadCommitted     = "ReadCommitted";
			public const string ReadCommittedLock = "ReadCommittedLock";
			public const string ReadPast          = "ReadPast";
			public const string ReadUncommitted   = "ReadUncommitted";
			public const string RepeatableRead    = "RepeatableRead";
			public const string RowLock           = "RowLock";
			public const string Serializable      = "Serializable";
			public const string Snapshot          = "Snapshot";
			public const string TabLock           = "TabLock";
			public const string TabLockX          = "TabLockX";
			public const string UpdLock           = "UpdLock";
			public const string XLock             = "XLock";

			[Sql.Expression("SPATIAL_WINDOW_MAX_CELLS={0}")]
			public static string SpatialWindowMaxCells(int value)
			{
				return "SPATIAL_WINDOW_MAX_CELLS=" + value;
			}
		}

		public static class Join
		{
			public const string Loop   = "LOOP";
			public const string Hash   = "HASH";
			public const string Merge  = "MERGE";
			public const string Remote = "REMOTE";
		}

		public static class Query
		{
			public const string HashGroup                          = "HASH GROUP";
			public const string OrderGroup                         = "ORDER GROUP";
			public const string ConcatUnion                        = "CONCAT UNION";
			public const string HashUnion                          = "HASH UNION";
			public const string MergeUnion                         = "MERGE UNION";
			public const string LoopJoin                           = "LOOP JOIN";
			public const string HashJoin                           = "HASH JOIN";
			public const string MergeJoin                          = "MERGE JOIN";
			public const string ExpandViews                        = "EXPAND VIEWS";
			public const string ForceOrder                         = "FORCE ORDER";
			public const string ForceExternalPushDown              = "FORCE EXTERNALPUSHDOWN";
			public const string DisableExternalPushDown            = "DISABLE EXTERNALPUSHDOWN";
			public const string ForceScaleOutExecution             = "FORCE SCALEOUTEXECUTION";
			public const string DisableScaleOutExecution           = "DISABLE SCALEOUTEXECUTION";
			public const string IgnoreNonClusteredColumnStoreIndex = "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX";
			public const string KeepPlan                           = "KEEP PLAN   ";
			public const string KeepFixedPlan                      = "KEEPFIXED PLAN";
			public const string NoPerformanceSpool                 = "NO_PERFORMANCE_SPOOL";
			public const string OptimizeForUnknown                 = "OPTIMIZE FOR UNKNOWN";
			public const string ParameterizationSimple             = "PARAMETERIZATION SIMPLE";
			public const string ParameterizationForced             = "PARAMETERIZATION FORCED";
			public const string Recompile                          = "RECOMPILE";
			public const string RobustPlan                         = "ROBUST PLAN";

			[Sql.Expression("FAST {0}")]
			public static string Fast(int value)
			{
				return $"FAST {value}";
			}

			[Sql.Expression("MAX_GRANT_PERCENT={0}")]
			public static string MaxGrantPercent(decimal value)
			{
				return $"MAX_GRANT_PERCENT={value}";
			}

			[Sql.Expression("MIN_GRANT_PERCENT={0}")]
			public static string MinGrantPercent(decimal value)
			{
				return $"MIN_GRANT_PERCENT={value}";
			}

			[Sql.Expression("MAXDOP {0}")]
			public static string MaxDop(int value)
			{
				return $"MAXDOP {value}";
			}

			[Sql.Expression("MAXRECURSION {0}")]
			public static string MaxRecursion(int value)
			{
				return $"MAXRECURSION {value}";
			}

			[Sql.Expression("OPTIMIZE FOR ({0})")]
			public static string OptimizeFor(string value)
			{
				return $"OPTIMIZE FOR ({value})";
			}

			[Sql.Expression("QUERYTRACEON {0}")]
			public static string QueryTraceOn(int value)
			{
				return $"QUERYTRACEON {value}";
			}

			[Sql.Expression("USE HINT ({0})")]
			public static string UseHint(string value)
			{
				return $"USE HINT ({value})";
			}

			[Sql.Expression("USE PLAN ({0})")]
			public static string UsePlan(string value)
			{
				return $"USE PLAN ({value})";
			}
		}

		#region QueryExtensions

		class WithForceSeekExtensionBuilder : ISqlExtensionBuilder
		{
			public void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var value = (SqlValue)sqlQueryExtension.Arguments["indexName"];
				var count = (int)((SqlValue)sqlQueryExtension.Arguments["columns.Count"]).Value!;

				if (count == 0)
				{
					stringBuilder
						.Append("ForceSeek, Index(")
						.Append(value.Value)
						.Append(')')
						;
				}
				else
				{
					stringBuilder
						.Append("ForceSeek (")
						.Append(value.Value)
						.Append(" (")
						;

					for (var i = 0; i < count; i++)
					{
						sqlBuilder.BuildExpression(sqlQueryExtension.Arguments[$"columns.{i}"], false, false, false);
						stringBuilder.Append(", ");
					}

					stringBuilder.Length -= 2;
					stringBuilder.Append("))");
				}
			}
		}

		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, ExtensionBuilderType = typeof(WithForceSeekExtensionBuilder))]
		public static ITable<TSource> WithForceSeek<TSource>(
			this ITable<TSource>                      table,
			[SqlQueryDependent] string                indexName,
			params Expression<Func<TSource,object>>[] columns)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(WithForceSeek, table, indexName, columns),
				table.Expression, Expression.Constant(indexName), Expression.NewArrayInit(
					typeof(Expression<Func<TSource,object>>),
					columns));

			return table;
		}

		#endregion

		#region ExpressionMethods

		[ExpressionMethod(nameof(WithIndexImpl))]
		public static ITable<TSource> WithIndex<TSource>(this ITable<TSource> table, string indexName)
			where TSource : notnull
		{
			return table.TableHint(Table.Index, indexName);
		}

		static Expression<Func<ITable<TSource>,string,ITable<TSource>>> WithIndexImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexName) => table.TableHint(Table.Index, indexName);
		}

		[ExpressionMethod(nameof(WithIndex2Impl))]
		public static ITable<TSource> WithIndex<TSource>(this ITable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return table.TableHint(Table.Index, indexNames);
		}

		static Expression<Func<ITable<TSource>,string[],ITable<TSource>>> WithIndex2Impl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => table.TableHint(Table.Index, indexNames);
		}

		#endregion
	}
}
