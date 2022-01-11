using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	using Expressions;
	using Linq;
	using SqlProvider;

	public static partial class OracleHints
	{
		// https://docs.oracle.com/cd/B19306_01/server.102/b14200/sql_elements006.htm
		//
		// Not implemented:
		//
		// MERGE
		// NO_MERGE
		// NO_PUSH_PRED
		// NO_USE_MERGE
		// NO_USE_NL
		// PQ_DISTRIBUTE
		// PUSH_PRED
		// QB_NAME
		// REWRITE
		// USE_HASH
		// USE_MERGE
		// USE_NL
		//
		public static class Table
		{
			// Hints for Access Paths
			public const string Full = "FULL";

			public const string Cache               = "CACHE";
			public const string Cluster             = "CLUSTER";
			public const string DrivingSite         = "DRIVING_SITE";
			public const string DynamicSampling     = "DYNAMIC_SAMPLING"; // 0..10
			public const string Fact                = "FACT";
			public const string Hash                = "HASH";
			public const string NoCache             = "NOCACHE";
			public const string NoFact              = "NO_FACT";
			public const string NoParallel          = "NO_PARALLEL";
			public const string NoPxJoinFilter      = "NO_PX_JOIN_FILTER";
			public const string NoUseHash           = "NO_USE_HASH";
			public const string Parallel            = "PARALLEL";
			public const string PxJoinFilter        = "PX_JOIN_FILTER";
			public const string Index               = "INDEX";
			public const string IndexAsc            = "INDEX_ASC";
			public const string IndexCombine        = "INDEX_COMBINE";
			public const string IndexDesc           = "INDEX_DESC";
			public const string IndexFFS            = "INDEX_FFS";
			public const string IndexFastFullScan   = "INDEX_FFS";
			public const string IndexJoin           = "INDEX_JOIN";
			public const string IndexSS             = "INDEX_SS";
			public const string IndexSkipScan       = "INDEX_SS";
			public const string IndexSSAsc          = "INDEX_SS_ASC";
			public const string IndexSkipScanAsc    = "INDEX_SS_ASC";
			public const string IndexSSDesc         = "INDEX_SS_DESC";
			public const string IndexSkipScanDesc   = "INDEX_SS_DESC";
			public const string NoIndex             = "NO_INDEX";
			public const string NoIndexFFS          = "NO_INDEX_FFS";
			public const string NoIndexFastFullScan = "NO_INDEX_FFS";
			public const string NoIndexSS           = "NO_INDEX_SS";
			public const string NoIndexSkipScan     = "NO_INDEX_SS";
			public const string NoParallelIndex     = "NO_PARALLEL_INDEX";
			public const string ParallelIndex       = "PARALLEL_INDEX";
			public const string UseNlWithIndex      = "USE_NL_WITH_INDEX";
		}

		public static class Query
		{
			// Hints for Optimization Approaches and Goals
			public const string AllRows               = "ALL_ROWS";

			[Sql.Expression("FIRST_ROWS({0})")]
			public static string FirstRows(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "FIRST_ROWS({0})", value);
			}

			public const string Append                = "APPEND";
			public const string CursorSharingExact    = "CURSOR_SHARING_EXACT";
			public const string Leading               = "LEADING";
			public const string ModelMinAnalysis      = "MODEL_MIN_ANALYSIS ";
			public const string NoAppend              = "NOAPPEND";
			public const string NoExpand              = "NO_EXPAND";
			public const string NoPushSubQuery        = "NO_PUSH_SUBQ";
			public const string NoRewrite             = "NO_REWRITE";
			public const string NoQueryTransformation = "NO_QUERY_TRANSFORMATION";
			public const string NoStarTransformation  = "NO_STAR_TRANSFORMATION";
			public const string NoUnnest              = "NO_UNNEST";
			public const string NoXmlQueryRewrite     = "NO_XML_QUERY_REWRITE";
			public const string Ordered               = "ORDERED";
			public const string PushSubQueries        = "PUSH_SUBQ";
			public const string StarTransformation    = "STAR_TRANSFORMATION";
			public const string Rule                  = "RULE";
			public const string Unnest                = "UNNEST";
			public const string UseConcat             = "USE_CONCAT";
		}

		#region OracleSpecific Hints

		#endregion

		#region TableHint

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(PathableTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource>(this IOracleSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint),
				table.Expression, Expression.Constant(hint));

			return table;
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(PathableTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource,TParam>(
			this IOracleSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] TParam            hintParameter)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
				table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter));

			return table;
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(PathableTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> TableHint<TSource,TParam>(
			this IOracleSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params TParam[]   hintParameters)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameters),
				table.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))));

			return table;
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(PathableTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(PathableTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] TParam                hintParameter)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(PathableTableHintExtensionBuilder), " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] params object[]       hintParameters)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource>(this IOracleSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource,TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder), " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> QueryHint<TSource, TParam>(
			this IOracleSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

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
