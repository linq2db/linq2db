using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.DataProvider.MySql
{
	public static partial class MySqlHints
	{
		// https://dev.mysql.com/doc/refman/8.0/en/optimizer-hints.html#optimizer-hints-index-level
		//
		public static class Table
		{
			// Join-Order Optimizer Hints.
			//
			public const string JoinFixedOrder             = "JOIN_FIXED_ORDER";
			public const string JoinOrder                  = "JOIN_ORDER";
			public const string JoinPrefix                 = "JOIN_PREFIX";
			public const string JoinSuffix                 = "JOIN_SUFFIX";

			// Table-Level Optimizer Hints.
			//
			public const string Bka                        = "BKA";
			public const string BatchedKeyAccess           = "BKA";
			public const string NoBka                      = "NO_BKA";
			public const string NoBatchedKeyAccess         = "NO_BKA";
			public const string Bnl                        = "BNL";
			public const string BlockNestedLoop            = "BNL";
			public const string NoBnl                      = "NO_BNL";
			public const string NoBlockNestedLoop          = "NO_BNL";
			public const string DerivedConditionPushDown   = "DERIVED_CONDITION_PUSHDOWN";
			public const string NoDerivedConditionPushDown = "NO_DERIVED_CONDITION_PUSHDOWN";
			public const string HashJoin                   = "HASH_JOIN";
			public const string NoHashJoin                 = "NO_HASH_JOIN";
			public const string Merge                      = "MERGE";
			public const string NoMerge                    = "NO_MERGE";

			// Index-Level Optimizer Hints.
			//
			public const string GroupIndex                 = "GROUP_INDEX";
			public const string NoGroupIndex               = "NO_GROUP_INDEX";
			public const string Index                      = "INDEX";
			public const string NoIndex                    = "NO_INDEX";
			public const string IndexMerge                 = "INDEX_MERGE";
			public const string NoIndexMerge               = "NO_INDEX_MERGE";
			public const string JoinIndex                  = "JOIN_INDEX";
			public const string NoJoinIndex                = "NO_JOIN_INDEX";
			public const string Mrr                        = "MRR";
			public const string NoMrr                      = "NO_MRR";
			public const string NoIcp                      = "NO_ICP";
			public const string NoRangeOptimization        = "NO_RANGE_OPTIMIZATION";
			public const string OrderIndex                 = "ORDER_INDEX";
			public const string NoOrderIndex               = "NO_ORDER_INDEX";
			public const string SkipScan                   = "SKIP_SCAN";
			public const string NoSkipScan                 = "NO_SKIP_SCAN";

			// Index Hints.
			//
			public const string UseIndex              = "USE INDEX";
			public const string UseIndexForJoin       = "USE INDEX FOR JOIN";
			public const string UseIndexForOrderBy    = "USE INDEX FOR ORDER BY";
			public const string UseIndexForGroupBy    = "USE INDEX FOR GROUP BY";
			public const string UseKey                = "USE KEY";
			public const string UseKeyForJoin         = "USE KEY FOR JOIN";
			public const string UseKeyForOrderBy      = "USE KEY FOR ORDER BY";
			public const string UseKeyForGroupBy      = "USE KEY FOR GROUP BY";
			public const string IgnoreIndex           = "IGNORE INDEX";
			public const string IgnoreIndexForJoin    = "IGNORE INDEX FOR JOIN";
			public const string IgnoreIndexForOrderBy = "IGNORE INDEX FOR ORDER BY";
			public const string IgnoreIndexForGroupBy = "IGNORE INDEX FOR GROUP BY";
			public const string IgnoreKey             = "IGNORE KEY";
			public const string IgnoreKeyForJoin      = "IGNORE KEY FOR JOIN";
			public const string IgnoreKeyForOrderBy   = "IGNORE KEY FOR ORDER BY";
			public const string IgnoreKeyForGroupBy   = "IGNORE KEY FOR GROUP BY";
			public const string ForceIndex            = "FORCE INDEX";
			public const string ForceIndexForJoin     = "FORCE INDEX FOR JOIN";
			public const string ForceIndexForOrderBy  = "FORCE INDEX FOR ORDER BY";
			public const string ForceIndexForGroupBy  = "FORCE INDEX FOR GROUP BY";
			public const string ForceKey              = "FORCE KEY";
			public const string ForceKeyForJoin       = "FORCE KEY FOR JOIN";
			public const string ForceKeyForOrderBy    = "FORCE KEY FOR ORDER BY";
			public const string ForceKeyForGroupBy    = "FORCE KEY FOR GROUP BY";
		}

		public static class Query
		{
			// Join-Order Optimizer Hints.
			//
			public const string JoinFixedOrder             = "JOIN_FIXED_ORDER";
			public const string JoinOrder                  = "JOIN_ORDER";
			public const string JoinPrefix                 = "JOIN_PREFIX";
			public const string JoinSuffix                 = "JOIN_SUFFIX";

			// Table-Level Optimizer Hints.
			//
			public const string Bka                        = "BKA";
			public const string BatchedKeyAccess           = "BKA";
			public const string NoBka                      = "NO_BKA";
			public const string NoBatchedKeyAccess         = "NO_BKA";
			public const string Bnl                        = "BNL";
			public const string BlockNestedLoop            = "BNL";
			public const string NoBnl                      = "NO_BNL";
			public const string NoBlockNestedLoop          = "NO_BNL";
			public const string DerivedConditionPushDown   = "DERIVED_CONDITION_PUSHDOWN";
			public const string NoDerivedConditionPushDown = "NO_DERIVED_CONDITION_PUSHDOWN";
			public const string HashJoin                   = "HASH_JOIN";
			public const string NoHashJoin                 = "NO_HASH_JOIN";
			public const string Merge                      = "MERGE";
			public const string NoMerge                    = "NO_MERGE";

			// Subquery Optimizer Hints.
			//
			public const string SemiJoin                   = "SEMIJOIN";
			public const string NoSemiJoin                 = "NO_SEMIJOIN";

			// Variable-Setting Hint Syntax.
			//
			public const string SetVar                     = "SET_VAR";

			// Resource Group Hint Syntax.
			//
			public const string ResourceGroup              = "RESOURCE_GROUP";

			// Statement Execution Time Optimizer Hints.
			//
			[Sql.Expression("MAX_EXECUTION_TIME({0})")]
			public static string MaxExecutionTime(int value)
			{
				return string.Format(CultureInfo.InvariantCulture, "MAX_EXECUTION_TIME({0})", value);
			}
		}

		public static class SubQuery
		{
			public const string ForUpdate       = "FOR UPDATE";
			public const string ForShare        = "FOR SHARE";
			public const string LockInShareMode = "LOCK IN SHARE MODE";

			public const string NoWait          = "NOWAIT";
			public const string SkipLocked      = "SKIP LOCKED";
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> QueryBlockHint<TSource, TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params TParam[]   hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryBlockHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))))));
		}

		[ExpressionMethod(nameof(SemiJoinHintWithQueryBlockImpl))]
		public static IMySqlSpecificQueryable<TSource> SemiJoinHintWithQueryBlock<TSource>(this IMySqlSpecificQueryable<TSource> query, params string[] values)
			where TSource : notnull
		{
			return QueryBlockHint(query, Query.SemiJoin, values);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string[],IMySqlSpecificQueryable<TSource>>> SemiJoinHintWithQueryBlockImpl<TSource>()
			where TSource : notnull
		{
			return (query, values) => QueryBlockHint(query, Query.SemiJoin, values);
		}

		[ExpressionMethod(nameof(NoSemiJoinHintWithQueryBlockImpl))]
		public static IMySqlSpecificQueryable<TSource> NoSemiJoinHintWithQueryBlock<TSource>(this IMySqlSpecificQueryable<TSource> query, params string[] values)
			where TSource : notnull
		{
			return QueryBlockHint(query, Query.NoSemiJoin, values);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string[],IMySqlSpecificQueryable<TSource>>> NoSemiJoinHintWithQueryBlockImpl<TSource>()
			where TSource : notnull
		{
			return (query, values) => QueryBlockHint(query, Query.NoSemiJoin, values);
		}

		#region TableHint

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableHint<TSource>(this IMySqlSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new MySqlSpecificTable<TSource>(newTable);
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableHint<TSource,TParam>(
			this IMySqlSpecificTable<TSource> table,
			[SqlQueryDependent] string        hint,
			[SqlQueryDependent] TParam        hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
			);

			return new MySqlSpecificTable<TSource>(newTable);
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableHint<TSource,TParam>(
			this IMySqlSpecificTable<TSource>   table,
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

			return new MySqlSpecificTable<TSource>(newTable);
		}

		#endregion

		#region TablesInScopeHint

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> TablesInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] TParam            hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params object[]   hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)))));
		}

		#endregion

		#region TableIndexHint

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableIndexHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableIndexHint<TSource,TParam>(
			this IMySqlSpecificTable<TSource> table,
			[SqlQueryDependent] string        hint,
			[SqlQueryDependent] TParam        hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableIndexHint, table, hint, hintParameter),
					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> TableIndexHint<TSource,TParam>(
			this IMySqlSpecificTable<TSource>   table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableIndexHint, table, hint, hintParameters),
					table.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		#endregion

		#region SubQueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> SubQueryHint<TSource>(this IMySqlSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryHint, source, hint),
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
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> SubQueryHint<TSource,TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] TParam            hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryHint, source, hint, hintParameter),
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
		/// <returns>Table-like query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> SubQueryHint<TSource, TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params            TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))))));
		}

		#endregion

		#region QueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> QueryHint<TSource>(this IMySqlSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> QueryHint<TSource,TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
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
		/// <returns>Table-like query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> QueryHint<TSource, TParam>(
			this IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))))));
		}

		#endregion

		#region SubQueryTableHint

		sealed class SubQueryTableHintExtensionBuilder : ISqlQueryExtensionBuilder
		{
			void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var hint    = (string)((SqlValue)sqlQueryExtension.Arguments["hint"]).Value!;
				var idCount = (int)   ((SqlValue)sqlQueryExtension.Arguments["tableIDs.Count"]).Value!;

				if ((hint is SubQuery.ForShare || idCount > 0) && sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.MariaDB10))
					stringBuilder.Append("-- ");

				stringBuilder.Append(hint);

				for (var i = 0; i < idCount; i++)
				{
					if (i == 0)
						stringBuilder.Append(" OF ");
					else if (i > 0)
						stringBuilder.Append(", ");

					var id    = (Sql.SqlID)((SqlValue)sqlQueryExtension.Arguments[FormattableString.Invariant($"tableIDs.{i}")]).Value!;
					var alias = sqlBuilder.BuildSqlID(id);

					stringBuilder.Append(alias);
				}

				if (sqlQueryExtension.Arguments.TryGetValue("hint2", out var h) && h is SqlValue { Value: string value } && !string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.Append(' ');
					stringBuilder.Append(value);
				}
			}
		}

		/// <summary>
		/// Adds subquery hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> SubQueryTableHint<TSource>(
			this                       IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent]        string                           hint,
			[SqlQueryDependent] params Sql.SqlID[]                      tableIDs)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))));
		}

		/// <summary>
		/// Adds subquery hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="hint2">NOWAIT | SKIP LOCKED</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> SubQueryTableHint<TSource>(
			this                       IMySqlSpecificQueryable<TSource> source,
			[SqlQueryDependent]        string                           hint,
			[SqlQueryDependent]        string                           hint2,
			[SqlQueryDependent] params Sql.SqlID[]                      tableIDs)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, hint2, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.Constant(hint2),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))));
		}

		/// <summary>
		/// Adds subquery hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> SubQueryTableHint<TSource>(
			this                       IMySqlSpecificTable<TSource> table,
			[SqlQueryDependent]        string                       hint,
			[SqlQueryDependent] params Sql.SqlID[]                  tableIDs)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, table, hint, tableIDs),
					table.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds subquery hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="hint2">NOWAIT | SKIP LOCKED</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> SubQueryTableHint<TSource>(
			this                       IMySqlSpecificTable<TSource> table,
			[SqlQueryDependent]        string                       hint,
			[SqlQueryDependent]        string                       hint2,
			[SqlQueryDependent] params Sql.SqlID[]                  tableIDs)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, table, hint, hint2, tableIDs),
					table.Expression,
					Expression.Constant(hint),
					Expression.Constant(hint2),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		[ExpressionMethod(nameof(LockInShareModeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> LockInShareModeHint<TSource>(
			this   IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.LockInShareMode);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> LockInShareModeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => SubQueryTableHint(query, SubQuery.LockInShareMode);
		}

		#endregion
	}
}
