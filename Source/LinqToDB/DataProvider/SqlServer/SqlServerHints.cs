using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// https://docs.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql
	/// </summary>
	public static partial class SqlServerHints
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
				return string.Format(CultureInfo.InvariantCulture, "SPATIAL_WINDOW_MAX_CELLS={0}", value);
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
			public const string KeepPlan                           = "KEEP PLAN";
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
				return string.Format(CultureInfo.InvariantCulture, "FAST {0}", value);
			}

			[Sql.Expression("MAX_GRANT_PERCENT={0}")]
			public static string MaxGrantPercent(decimal value)
			{
				return string.Format(CultureInfo.InvariantCulture, "MAX_GRANT_PERCENT={0}", value);
			}

			[Sql.Expression("MIN_GRANT_PERCENT={0}")]
			public static string MinGrantPercent(decimal value)
			{
				return string.Format(CultureInfo.InvariantCulture, "MIN_GRANT_PERCENT={0}", value);
			}

			[Sql.Expression("MAXDOP {0}")]
			public static string MaxDop(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "MAXDOP {0}", value);
			}

			[Sql.Expression("MAXRECURSION {0}")]
			public static string MaxRecursion(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "MAXRECURSION {0}", value);
			}

			[Sql.Expression("OPTIMIZE FOR ({0})")]
			public static string OptimizeFor(string value)
			{
				return $"OPTIMIZE FOR ({value})";
			}

			[Sql.Expression("QUERYTRACEON {0}")]
			public static string QueryTraceOn(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "QUERYTRACEON {0}", value);
			}
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static class TemporalTable
		{
			public const string All         = "ALL";
			public const string AsOf        = "AS OF";
			public const string FromTo      = "FROM";
			public const string Between     = "BETWEEN";
			public const string ContainedIn = "CONTAINED IN (";
		}

		#region SqlServerSpecific Hints

		[ExpressionMethod(nameof(WithIndexImpl))]
		public static ISqlServerSpecificTable<TSource> WithIndex<TSource>(this ISqlServerSpecificTable<TSource> table, string indexName)
			where TSource : notnull
		{
			return table.TableHint(Table.Index, indexName);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,string,ISqlServerSpecificTable<TSource>>> WithIndexImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexName) => table.TableHint(Table.Index, indexName);
		}

		[ExpressionMethod(nameof(WithIndex2Impl))]
		public static ISqlServerSpecificTable<TSource> WithIndex<TSource>(this ISqlServerSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return table.TableHint(Table.Index, indexNames);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,string[],ISqlServerSpecificTable<TSource>>> WithIndex2Impl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => table.TableHint(Table.Index, indexNames);
		}

		sealed class WithForceSeekExtensionBuilder : ISqlQueryExtensionBuilder
		{
			public void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var value = (SqlValue)sqlQueryExtension.Arguments["indexName"];
				var count = (int)((SqlValue)sqlQueryExtension.Arguments["columns.Count"]).Value!;

				if (count == 0)
				{
					stringBuilder.Append(CultureInfo.InvariantCulture, $"ForceSeek, Index({value.Value})");
				}
				else
				{
					stringBuilder.Append(CultureInfo.InvariantCulture, $"ForceSeek({value.Value}(");

					for (var i = 0; i < count; i++)
					{
						sqlBuilder.BuildExpression(sqlBuilder.StringBuilder, sqlQueryExtension.Arguments[FormattableString.Invariant($"columns.{i}")], false);
						stringBuilder.Append(", ");
					}

					stringBuilder.Length -= 2;
					stringBuilder.Append("))");
				}
			}
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, typeof(WithForceSeekExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> WithForceSeek<TSource>(
			this ISqlServerSpecificTable<TSource>     table,
			[SqlQueryDependent] string                indexName,
			params Expression<Func<TSource,object>>[] columns)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(WithForceSeek, table, indexName, columns),
					table.Expression, Expression.Constant(indexName), Expression.NewArrayInit(
						typeof(Expression<Func<TSource, object>>),
						columns))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[ExpressionMethod(nameof(WithSpatialWindowMaxCellsImpl))]
		public static ISqlServerSpecificTable<TSource> WithSpatialWindowMaxCells<TSource>(this ISqlServerSpecificTable<TSource> table, int cells)
			where TSource : notnull
		{
			return table.TableHint2012Plus(Table.SpatialWindowMaxCells(cells));
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,int,ISqlServerSpecificTable<TSource>>> WithSpatialWindowMaxCellsImpl<TSource>()
			where TSource : notnull
		{
			return (table, cells) => table.TableHint2012Plus(Table.SpatialWindowMaxCells(cells));
		}

		sealed class ParamsExtensionBuilder : ISqlQueryExtensionBuilder
		{
			public void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var count = (int)((SqlValue)sqlQueryExtension.Arguments["values.Count"]).Value!;

				stringBuilder.Append(
					CultureInfo.InvariantCulture,
					$"{((SqlValue)sqlQueryExtension.Arguments[".ExtensionArguments.0"]).Value}(");

				for (var i = 0; i < count; i++)
				{
					if (i > 0)
						stringBuilder.Append(", ");
					var value = (SqlValue)sqlQueryExtension.Arguments[FormattableString.Invariant($"values.{i}")];
					stringBuilder.Append(CultureInfo.InvariantCulture, $"{value.Value}");
				}

				stringBuilder.Append(')');
			}
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.QueryHint, typeof(ParamsExtensionBuilder), "OPTIMIZE FOR")]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> OptionOptimizeFor<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] params string[]       values)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OptionOptimizeFor, source, values),
					currentSource.Expression,
					Expression.NewArrayInit(typeof(string), values.Select(Expression.Constant)))));
		}

		[LinqTunnel, Pure, IsQueryable]
//		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.QueryHint, typeof(ParamsExtensionBuilder), "USE HINT")]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.QueryHint, typeof(ParamsExtensionBuilder), "USE HINT")]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(ParamsExtensionBuilder), "USE HINT")]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(ParamsExtensionBuilder), "USE HINT")]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> OptionUseHint<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] params string[]       values)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OptionUseHint, source, values),
					currentSource.Expression,
					Expression.NewArrayInit(typeof(string), values.Select(Expression.Constant)))));
		}

		sealed class TableParamsExtensionBuilder : ISqlQueryExtensionBuilder
		{
			public void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var count = (int)((SqlValue)sqlQueryExtension.Arguments["values.Count"]).Value!;

				var id    = (Sql.SqlID)((SqlValue)sqlQueryExtension.Arguments["tableID"]).Value!;
				var alias = sqlBuilder.BuildSqlID(id);

				stringBuilder
					.Append("TABLE HINT(")
					.Append(alias)
					;

				for (var i = 0; i < count; i++)
				{
					stringBuilder.Append(", ");
					var value = (SqlValue)sqlQueryExtension.Arguments[FormattableString.Invariant($"values.{i}")];
					stringBuilder.Append(CultureInfo.InvariantCulture, $"{value.Value}");
				}

				stringBuilder.Append(')');
			}
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2008, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2012, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(TableParamsExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> OptionTableHint<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] Sql.SqlID             tableID,
			[SqlQueryDependent] params string[]       values)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OptionTableHint, source, tableID, values),
					currentSource.Expression,
					Expression.Constant(tableID),
					Expression.NewArrayInit(typeof(string), values.Select(Expression.Constant)))));
		}

		#endregion

		#region TableHint

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TableHint<TSource>(this ISqlServerSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TableHint<TSource,TParam>(
			this ISqlServerSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] TParam            hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TableHint<TSource,TParam>(
			this ISqlServerSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params TParam[]   hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameters),
					table.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2012, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TableHint2012Plus<TSource>(this ISqlServerSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint2012Plus, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		static ISqlServerSpecificTable<TSource> TableHint2014Plus<TSource>(this ISqlServerSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint2014Plus, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		#endregion

		#region TablesInScopeHint

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] TParam                hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameter),
					currentSource.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] params object[]       hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2012, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> TablesInScopeHint2012Plus<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint2012Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> TablesInScopeHint2014Plus<TSource>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint2014Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region JoinHint

		/// <summary>
		/// Adds a join hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.JoinHint, typeof(NoneExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,     typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> JoinHint<TSource>(this ISqlServerSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(JoinHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds a join hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.JoinHint, typeof(NoneExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,     typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> JoinHint<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(JoinHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region QueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Hint parameter type</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Hint parameter.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint<TSource,TParam>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameter),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.Constant(hintParameter))));
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint<TSource, TParam>(
			this ISqlServerSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))))));
		}

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                       Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint2019Plus<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint2019Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2008, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2012, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint2008Plus<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint2008Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2012, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2014, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint2012Plus<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint2012Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer2016, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2017, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2019, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.SqlServer2022, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> QueryHint2016Plus<TSource>(this ISqlServerSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint2016Plus, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region TemporalTable

		sealed class TemporalTableExtensionBuilder : ISqlQueryExtensionBuilder
		{
			public void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var expression = (string)((SqlValue)sqlQueryExtension.Arguments["expression"]).Value!;

				stringBuilder
					.Append(" FOR SYSTEM_TIME ")
					.Append(expression)
					.Append(' ')
					;

				var lastLength = stringBuilder.Length;

				if (expression == TemporalTable.ContainedIn)
					stringBuilder.Length--;

				var b2016 = sqlBuilder as SqlServer2016SqlBuilder;

				if (b2016 != null)
					b2016.ConvertDateTimeAsLiteral = true;

				if (sqlQueryExtension.Arguments.TryGetValue("dateTime", out var dt))
					sqlBuilder.BuildExpression(stringBuilder, dt, true, this);

				if (sqlQueryExtension.Arguments.TryGetValue("dateTime2", out dt))
				{
					switch (expression)
					{
						case TemporalTable.FromTo      : stringBuilder.Append(" TO ");  break;
						case TemporalTable.Between     : stringBuilder.Append(" AND "); break;
						case TemporalTable.ContainedIn : stringBuilder.Append(", ");    break;
					}

					sqlBuilder.BuildExpression(stringBuilder, dt, true, this);

					if (expression == TemporalTable.ContainedIn)
						stringBuilder.Append(')');
				}

				if (b2016 != null)
					b2016.ConvertDateTimeAsLiteral = true;

				if (lastLength == stringBuilder.Length)
				{
					--stringBuilder.Length;
				}
			}
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableNameHint, typeof(TemporalTableExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,          typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TemporalTableHint<TSource>(
			this                ISqlServerSpecificTable<TSource> table,
			[SqlQueryDependent] string                           expression)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TemporalTableHint, table, expression),
					table.Expression, Expression.Constant(expression))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableNameHint, typeof(TemporalTableExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,          typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TemporalTableHint<TSource>(
			this                ISqlServerSpecificTable<TSource> table,
			[SqlQueryDependent] string                           expression,
			                    DateTime                         dateTime)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TemporalTableHint, table, expression, dateTime),
					table.Expression, Expression.Constant(expression), Expression.Constant(dateTime))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableNameHint, typeof(TemporalTableExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,          typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> TemporalTableHint<TSource>(
			this                ISqlServerSpecificTable<TSource> table,
			[SqlQueryDependent] string                           expression,
			                    DateTime                         dateTime,
			                    DateTime                         dateTime2)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TemporalTableHint, table, expression, dateTime, dateTime2),
					table.Expression, Expression.Constant(expression), Expression.Constant(dateTime),
					Expression.Constant(dateTime2))
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// <para>
		/// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables">Temporal table</see>
		/// </para>
		/// <b>Expression</b><br/><term/>
		/// <b>ALL</b>
		/// <br/>
		/// <b>Qualifying Rows</b><br/><term/>
		/// All rows
		/// <br/>
		/// <b>Note</b><br/>
		/// Returns the union of rows that belong to the current and the history table.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns>Table-like query source with <b>FOR SYSTEM_TIME ALL</b> filter.</returns>
		[LinqTunnel, Pure]
		[ExpressionMethod(ProviderName.SqlServer, nameof(TemporalTableAllImpl))]
		public static ISqlServerSpecificTable<TSource> TemporalTableAll<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TemporalTableHint(TemporalTable.All);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> TemporalTableAllImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TemporalTableHint(TemporalTable.All);
		}

		/// <summary>
		/// <para>
		/// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables">Temporal table</see>
		/// </para>
		/// <b>Expression</b><br/><term/>
		/// <b>AS OF</b> <i>dateTime</i>
		/// <br/>
		/// <b>Qualifying Rows</b><br/><term/>
		/// <c>ValidFrom</c> &lt;= <i>dateTime</i> AND <c>ValidTo</c> &gt; <i>dateTime</i>
		/// <br/>
		/// <b>Note</b><br/>
		/// Returns a table with rows containing the values that were current at the specified point in time in the past.
		/// Internally, a union is performed between the temporal table and its history table and the results are filtered
		/// to return the values in the row that was valid at the point in time specified by the <i>dateTime</i> parameter.
		/// The value for a row is deemed valid if the <i>system_start_time_column_name</i> value is less than or equal to
		/// the <i>dateTime</i> parameter value and the <i>system_end_time_column_name</i> value is greater than the <i>dateTime</i> parameter value.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns>Table-like query source with <b>FOR SYSTEM_TIME AS OF</b> <i>dateTime</i> filter.</returns>
		[LinqTunnel, Pure]
		[ExpressionMethod(ProviderName.SqlServer, nameof(TemporalTableAsOfImpl))]
		public static ISqlServerSpecificTable<TSource> TemporalTableAsOf<TSource>(this ISqlServerSpecificTable<TSource> table, DateTime dateTime)
			where TSource : notnull
		{
			return table.TemporalTableHint(TemporalTable.AsOf, dateTime);
		}
		static Expression<Func<ISqlServerSpecificTable<TSource>,DateTime,ISqlServerSpecificTable<TSource>>> TemporalTableAsOfImpl<TSource>()
			where TSource : notnull
		{
			return (table, dateTime) => table.TemporalTableHint(TemporalTable.AsOf, dateTime);
		}

		/// <summary>
		/// <para>
		/// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables">Temporal table</see>
		/// </para>
		/// <b>Expression</b><br/><term/>
		/// <b>FROM</b> <i>dateTime</i> <b>TO</b> <i>dateTime2</i>
		/// <br/>
		/// <b>Qualifying Rows</b><br/><term/>
		/// <c>ValidFrom</c> &lt; <i>dateTime2</i> AND <c>ValidTo</c> &gt; <i>dateTime</i>
		/// <br/>
		/// <b>Note</b><br/>
		/// Returns a table with the values for all row versions that were active within the specified time range,
		/// regardless of whether they started being active before the <i>dateTime</i> parameter value for the <b>FROM</b> argument or
		/// ceased being active after the <i>dateTime2</i> parameter value for the <b>TO</b> argument.
		/// Internally, a union is performed between the temporal table and its history table and the results are filtered
		/// to return the values for all row versions that were active at any time during the time range specified.
		/// Rows that stopped being active exactly on the lower boundary defined by the <b>FROM</b> endpoint aren't included,
		/// and records that became active exactly on the upper boundary defined by the <b>TO</b> endpoint are also not included.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns>Table-like query source with <b>FOR SYSTEM_TIME FROM</b> <i>dateTime</i> <b>TO</b> <i>dateTime2</i> filter.</returns>
		[LinqTunnel, Pure]
		[ExpressionMethod(ProviderName.SqlServer, nameof(TemporalTableFromToImpl))]
		public static ISqlServerSpecificTable<TSource> TemporalTableFromTo<TSource>(this ISqlServerSpecificTable<TSource> table, DateTime dateTime, DateTime dateTime2)
			where TSource : notnull
		{
			return table.TemporalTableHint(TemporalTable.FromTo, dateTime, dateTime2);
		}
		static Expression<Func<ISqlServerSpecificTable<TSource>,DateTime,DateTime,ISqlServerSpecificTable<TSource>>> TemporalTableFromToImpl<TSource>()
			where TSource : notnull
		{
			return (table, dateTime, dateTime2) => table.TemporalTableHint(TemporalTable.FromTo, dateTime, dateTime2);
		}

		/// <summary>
		/// <para>
		/// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables">Temporal table</see>
		/// </para>
		/// <b>Expression</b><br/><term/>
		/// <b>BETWEEN</b> <i>dateTime</i> <b>AND</b> <i>dateTime2</i>
		/// <br/>
		/// <b>Qualifying Rows</b><br/><term/>
		/// <c>ValidFrom</c> &lt;= <i>dateTime2</i> AND <c>ValidTo</c> &gt; <i>dateTime</i>
		/// <br/>
		/// <b>Note</b><br/>
		/// Same as in the <b>FOR SYSTEM_TIME FROM</b> <i>dateTime</i> <b>TO</b> <i>dateTime2</i> description,
		/// except the table of rows returned includes rows that became active on the upper boundary defined by the <i>dateTime2</i> endpoint.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns>Table-like query source with <b>FOR SYSTEM_TIME BETWEEN</b> <i>dateTime</i> <b>AND</b> <i>dateTime2</i> filter.</returns>
		[LinqTunnel, Pure]
		[ExpressionMethod(ProviderName.SqlServer, nameof(TemporalTableBetweenImpl))]
		public static ISqlServerSpecificTable<TSource> TemporalTableBetween<TSource>(this ISqlServerSpecificTable<TSource> table, DateTime dateTime, DateTime dateTime2)
			where TSource : notnull
		{
			return table.TemporalTableHint(TemporalTable.Between, dateTime, dateTime2);
		}
		static Expression<Func<ISqlServerSpecificTable<TSource>,DateTime,DateTime,ISqlServerSpecificTable<TSource>>> TemporalTableBetweenImpl<TSource>()
			where TSource : notnull
		{
			return (table, dateTime, dateTime2) => table.TemporalTableHint(TemporalTable.Between, dateTime, dateTime2);
		}

		/// <summary>
		/// <para>
		/// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables">Temporal table</see>
		/// </para>
		/// <b>Expression</b><br/><term/>
		/// <b>CONTAINED IN</b> (<i>dateTime</i>, <i>dateTime2</i>)
		/// <br/>
		/// <b>Qualifying Rows</b><br/><term/>
		/// <c>ValidFrom</c> &gt;= <i>dateTime</i> AND <c>ValidTo</c> &lt;= <i>dateTime2</i>
		/// <br/>
		/// <b>Note</b><br/>
		/// Returns a table with the values for all row versions that were opened and closed within the specified time range
		/// defined by the two period values for the <b>CONTAINED IN</b> argument. Rows that became active exactly on the lower
		/// boundary or ceased being active exactly on the upper boundary are included.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns>Table-like query source with <b>FOR SYSTEM_TIME CONTAINED IN</b> (<i>dateTime</i>, <i>dateTime2</i>) filter.</returns>
		[LinqTunnel, Pure]
		[ExpressionMethod(ProviderName.SqlServer, nameof(TemporalTableContainedInImpl))]
		public static ISqlServerSpecificTable<TSource> TemporalTableContainedIn<TSource>(this ISqlServerSpecificTable<TSource> table, DateTime dateTime, DateTime dateTime2)
			where TSource : notnull
		{
			return table.TemporalTableHint(TemporalTable.ContainedIn, dateTime, dateTime2);
		}
		static Expression<Func<ISqlServerSpecificTable<TSource>,DateTime,DateTime,ISqlServerSpecificTable<TSource>>> TemporalTableContainedInImpl<TSource>()
			where TSource : notnull
		{
			return (table, dateTime, dateTime2) => table.TemporalTableHint(TemporalTable.ContainedIn, dateTime, dateTime2);
		}

		#endregion
	}
}
