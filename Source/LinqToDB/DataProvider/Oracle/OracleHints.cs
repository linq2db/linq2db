using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Oracle
{
	public static partial class OracleHints
	{
		// https://docs.oracle.com/cd/B19306_01/server.102/b14200/sql_elements006.htm
		// https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E
		//
		public static class Hint
		{
			// Optimization Goals and Approaches.
			//
			public const string AllRows = "ALL_ROWS";

			[Sql.Expression("FIRST_ROWS({0})")]
			public static string FirstRows(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "FIRST_ROWS({0})", value);
			}

			// Access Path Hints.
			//
			public const string Cluster               = "CLUSTER";
			public const string Clustering            = "CLUSTERING";
			public const string NoClustering          = "NO_CLUSTERING";
			public const string Full                  = "FULL";
			public const string Hash                  = "HASH";
			public const string Index                 = "INDEX";
			public const string NoIndex               = "NO_INDEX";
			public const string IndexAsc              = "INDEX_ASC";
			public const string IndexDesc             = "INDEX_DESC";
			public const string IndexCombine          = "INDEX_COMBINE";
			public const string IndexJoin             = "INDEX_JOIN";
			public const string IndexFFS              = "INDEX_FFS";
			public const string IndexFastFullScan     = "INDEX_FFS";
			public const string IndexSS               = "INDEX_SS";
			public const string IndexSkipScan         = "INDEX_SS";
			public const string IndexSSAsc            = "INDEX_SS_ASC";
			public const string IndexSkipScanAsc      = "INDEX_SS_ASC";
			public const string IndexSSDesc           = "INDEX_SS_DESC";
			public const string IndexSkipScanDesc     = "INDEX_SS_DESC";
			public const string NativeFullOuterJoin   = "NATIVE_FULL_OUTER_JOIN";
			public const string NoIndexFFS            = "NO_INDEX_FFS";
			public const string NoIndexFastFullScan   = "NO_INDEX_FFS";
			public const string NoIndexSS             = "NO_INDEX_SS";
			public const string NoIndexSkipScan       = "NO_INDEX_SS";
			public const string NoNativeFullOuterJoin = "NO_NATIVE_FULL_OUTER_JOIN";

			// In-Memory Column Store Hints.
			//
			public const string InMemory          = "NMEMORY";
			public const string NoInMemory        = "NO_INMEMORY";
			public const string InMemoryPruning   = "INMEMORY_PRUNING";
			public const string NoInMemoryPruning = "NO_INMEMORY_PRUNING";

			// Join Order Hints.
			//
			public const string Leading = "LEADING";
			public const string Ordered = "ORDERED";

			// Join Operation Hints.
			//
			public const string UseBand                = "USE_BAND";
			public const string NoUseBand              = "NO_USE_BAND";
			public const string UseCube                = "USE_CUBE";
			public const string NoUseCube              = "NO_USE_CUBE";
			public const string UseHash                = "USE_HASH";
			public const string NoUseHash              = "NO_USE_HASH";
			public const string UseMerge               = "USE_MERGE";
			public const string NoUseMerge             = "NO_USE_MERGE";
			public const string UseNL                  = "USE_NL";
			public const string UseNestedLoop          = "USE_NL";
			public const string NoUseNL                = "NO_USE_NL";
			public const string NoUseNestedLoop        = "NO_USE_NL";
			public const string UseNLWithIndex         = "USE_NL_WITH_INDEX";
			public const string UseNestedLoopWithIndex = "USE_NL_WITH_INDEX";

			// Parallel Execution Hints.
			//
			public const string EnableParallelDml   = "ENABLE_PARALLEL_DML";
			public const string DisableParallelDml  = "DISABLE_PARALLEL_DML";
			public const string Parallel            = "PARALLEL";
			public const string NoParallel          = "NO_PARALLEL";
			public const string ParallelIndex       = "PARALLEL_INDEX";
			public const string NoParallelIndex     = "NO_PARALLEL_INDEX";
			public const string PQConcurrentUnion   = "PQ_CONCURRENT_UNION";
			public const string NoPQConcurrentUnion = "NO_PQ_CONCURRENT_UNION";
			public const string PQDistribute        = "PQ_DISTRIBUTE";
			public const string PQFilterSerial      = "PQ_FILTER(SERIAL)";
			public const string PQFilterNone        = "PQ_FILTER(NONE)";
			public const string PQFilterHash        = "PQ_FILTER(HASH)";
			public const string PQFilterRandom      = "PQ_FILTER(RANDOM)";
			public const string PQSkew              = "PQ_SKEW";
			public const string NoPQSkew            = "NO_PQ_SKEW";

			// Query Transformation Hints.
			//
			public const string Fact                  = "FACT";
			public const string NoFact                = "NO_FACT";
			public const string Merge                 = "MERGE";
			public const string NoMerge               = "NO_MERGE";
			public const string NoExpand              = "NO_EXPAND";
			public const string UseConcat             = "USE_CONCAT";
			public const string Rewrite               = "REWRITE";
			public const string NoRewrite             = "NO_REWRITE";
			public const string Unnest                = "UNNEST";
			public const string NoUnnest              = "NO_UNNEST";
			public const string StarTransformation    = "STAR_TRANSFORMATION";
			public const string NoStarTransformation  = "NO_STAR_TRANSFORMATION";
			public const string NoQueryTransformation = "NO_QUERY_TRANSFORMATION";

			// XML Hints.
			//
			public const string NoXmlIndexRewrite = "NO_XMLINDEX_REWRITE";
			public const string NoXmlQueryRewrite = "NO_XML_QUERY_REWRITE";

			// Other Hints.
			//
			public const string Append                = "APPEND";
			public const string AppendValues          = "APPEND_VALUES";
			public const string NoAppend              = "NOAPPEND";
			public const string Cache                 = "CACHE";
			public const string NoCache               = "NOCACHE";
			public const string CursorSharingExact    = "CURSOR_SHARING_EXACT";
			public const string DrivingSite           = "DRIVING_SITE";
			public const string DynamicSampling       = "DYNAMIC_SAMPLING"; // 0..10
			public const string FreshMV               = "FRESH_MV";
			public const string FreshMaterializedView = "FRESH_MV";
			public const string Grouping              = "GROUPING";
			public const string ModelMinAnalysis      = "MODEL_MIN_ANALYSIS";
			public const string Monitor               = "MONITOR";
			public const string NoMonitor             = "NO_MONITOR";
			public const string OptParam              = "OPT_PARAM";
			public const string PushPredicate         = "PUSH_PRED";
			public const string NoPushPredicate       = "PUSH_PRED";
			public const string PushSubQueries        = "PUSH_SUBQ";
			public const string NoPushSubQueries      = "NO_PUSH_SUBQ";
			public const string PxJoinFilter          = "PX_JOIN_FILTER";
			public const string NoPxJoinFilter        = "NO_PX_JOIN_FILTER";

			[Sql.Expression("CONTAINERS(DEFAULT_PDB_HINT='{0}')")]
			public static string Containers(string hint)
			{
				return string.Format(CultureInfo.InvariantCulture, "CONTAINERS(DEFAULT_PDB_HINT='{0}')", hint);
			}
		}

		#region OracleSpecific Hints

		[ExpressionMethod(nameof(OptParamHintImpl))]
		public static IOracleSpecificQueryable<TSource> OptParamHint<TSource>(this IOracleSpecificQueryable<TSource> query, params string[] parameters)
			where TSource : notnull
		{
			return QueryHint(query, Hint.OptParam, parameters);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string[],IOracleSpecificQueryable<TSource>>> OptParamHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, parameters) => QueryHint(query, Hint.OptParam, parameters);
		}

		[ExpressionMethod(nameof(ContainersHintImpl))]
		public static IOracleSpecificQueryable<TSource> ContainersHint<TSource>(this IOracleSpecificQueryable<TSource> query, string hint)
			where TSource : notnull
		{
			return QueryHint(query, Hint.Containers(hint));
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> ContainersHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, hint) => QueryHint(query, Hint.Containers(hint));
		}

		[ExpressionMethod(nameof(ParallelDefaultHintImpl))]
		public static IOracleSpecificQueryable<TSource> ParallelDefaultHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return QueryHint(query, $"{Hint.Parallel}(DEFAULT)");
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ParallelDefaultHintImpl<TSource>()
			where TSource : notnull
		{
			return query => QueryHint(query, $"{Hint.Parallel}(DEFAULT)");
		}

		[ExpressionMethod(nameof(ParallelAutoHintImpl))]
		public static IOracleSpecificQueryable<TSource> ParallelAutoHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return QueryHint(query, $"{Hint.Parallel}(AUTO)");
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ParallelAutoHintImpl<TSource>()
			where TSource : notnull
		{
			return query => QueryHint(query, $"{Hint.Parallel}(AUTO)");
		}

		[ExpressionMethod(nameof(ParallelManualHintImpl))]
		public static IOracleSpecificQueryable<TSource> ParallelManualHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return QueryHint(query, $"{Hint.Parallel}(MANUAL)");
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ParallelManualHintImpl<TSource>()
			where TSource : notnull
		{
			return query => QueryHint(query, $"{Hint.Parallel}(MANUAL)");
		}

		[ExpressionMethod(nameof(ParallelHintImpl2))]
		public static IOracleSpecificQueryable<TSource> ParallelHint<TSource>(this IOracleSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return QueryHint(query, string.Format(CultureInfo.InvariantCulture, Hint.Parallel + "({0})", value));
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,int,IOracleSpecificQueryable<TSource>>> ParallelHintImpl2<TSource>()
			where TSource : notnull
		{
			return (query, value) => QueryHint(query, string.Format(CultureInfo.InvariantCulture, Hint.Parallel + "({0})", value));
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), ", ", ", ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		static IOracleSpecificTable<TSource> TableHintWithComma<TSource,TParam>(
			this IOracleSpecificTable<TSource> table,
			[SqlQueryDependent] string         hint,
			[SqlQueryDependent] TParam         hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHintWithComma, table, hint, hintParameter),
				table.Expression,
				Expression.Constant(hint),
				Expression.Constant(hintParameter))
			);

			return new OracleSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), ", ", ", ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		static IOracleSpecificTable<TSource> TableHintWithComma<TSource,TParam>(
			this IOracleSpecificTable<TSource>  table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHintWithComma, table, hint, hintParameters),
				table.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam),
					hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return new OracleSpecificTable<TSource>(newTable);
		}

		[ExpressionMethod(nameof(ParallelHintImpl3))]
		public static IOracleSpecificTable<TSource> ParallelHint<TSource>(this IOracleSpecificTable<TSource> table, int value)
			where TSource : notnull
		{
			return TableHintWithComma(table, Hint.Parallel, value);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,int,IOracleSpecificTable<TSource>>> ParallelHintImpl3<TSource>()
			where TSource : notnull
		{
			return (table, value) => TableHintWithComma(table, Hint.Parallel, value);
		}

		[ExpressionMethod(nameof(ParallelDefaultHintImpl3))]
		public static IOracleSpecificTable<TSource> ParallelDefaultHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHintWithComma(table, Hint.Parallel, "DEFAULT");
		}
		static Expression<Func<IOracleSpecificTable<TSource>,int,IOracleSpecificTable<TSource>>> ParallelDefaultHintImpl3<TSource>()
			where TSource : notnull
		{
			return (table, value) => TableHintWithComma(table, Hint.Parallel, "DEFAULT");
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		static IOracleSpecificTable<TSource> TableHintWithComma2<TSource,TParam>(
			this IOracleSpecificTable<TSource>  table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHintWithComma2, table, hint, hintParameters),
				table.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return new OracleSpecificTable<TSource>(newTable);
		}

		[ExpressionMethod(nameof(PQDistributeHintImpl))]
		public static IOracleSpecificTable<TSource> PQDistributeHint<TSource>(this IOracleSpecificTable<TSource> table, string outerDistribution, string innerDistribution)
			where TSource : notnull
		{
			return TableHintWithComma2(table, Hint.PQDistribute, outerDistribution,innerDistribution);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string,string,IOracleSpecificTable<TSource>>> PQDistributeHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, outerDistribution, innerDistribution) => TableHintWithComma2(table, Hint.PQDistribute, outerDistribution, innerDistribution);
		}

		[ExpressionMethod(nameof(ParallelIndexHintImpl))]
		public static IOracleSpecificTable<TSource> ParallelIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params object[] values)
			where TSource : notnull
		{
			return TableHintWithComma(table, Hint.ParallelIndex, values);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,object[],IOracleSpecificTable<TSource>>> ParallelIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, values) => TableHintWithComma(table, Hint.ParallelIndex, values);
		}

		[ExpressionMethod(nameof(NoParallelIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoParallelIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params object[] values)
			where TSource : notnull
		{
			return TableHintWithComma(table, Hint.NoParallelIndex, values);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,object[],IOracleSpecificTable<TSource>>> NoParallelIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, values) => TableHintWithComma(table, Hint.NoParallelIndex, values);
		}

		[ExpressionMethod(nameof(DynamicSamplingHintImpl))]
		public static IOracleSpecificTable<TSource> DynamicSamplingHint<TSource>(this IOracleSpecificTable<TSource> table, int value)
			where TSource : notnull
		{
			return TableHint(table, Hint.DynamicSampling, value);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,int,IOracleSpecificTable<TSource>>> DynamicSamplingHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, value) => TableHint(table, Hint.DynamicSampling, value);
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource>(this IOracleSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new OracleSpecificTable<TSource>(newTable);
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource,TParam>(
			this IOracleSpecificTable<TSource> table,
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

			return new OracleSpecificTable<TSource>(newTable);
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource,TParam>(
			this IOracleSpecificTable<TSource>  table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
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

			return new OracleSpecificTable<TSource>(newTable);
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] TParam                hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] params object[]       hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)))));
		}

		#endregion

		#region QueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// <code>
		/// // will produce following SQL code in generated query: INNER LOOP JOIN
		/// var tableWithHint = db.Table.JoinHint("LOOP");
		/// </code>
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource>(this IOracleSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource,TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder), " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource, TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))))));
		}

		#endregion
	}
}
