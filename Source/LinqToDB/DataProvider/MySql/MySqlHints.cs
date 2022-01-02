using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.MySql
{
	public static class MySqlHints
	{
		// https://dev.mysql.com/doc/refman/8.0/en/optimizer-hints.html#optimizer-hints-index-level
		//
		public static class TableHint
		{
			public const string Bka                        = "BKA";
			public const string NoBka                      = "NO_BKA";
			public const string Bnl                        = "BNL";
			public const string NoBnl                      = "NO_BNL";
			public const string DerivedConditionPushDown   = "DERIVED_CONDITION_PUSHDOWN";
			public const string NoDerivedConditionPushDown = "NO_DERIVED_CONDITION_PUSHDOWN";
			public const string HashJoin                   = "HASH_JOIN";
			public const string NoHashJoin                 = "NO_HASH_JOIN";
			public const string Merge                      = "MERGE";
			public const string NoMerge                    = "NO_MERGE";
		}

		public static class IndexHint
		{
			public const string GroupIndex          = "GROUP_INDEX";
			public const string NoGroupIndex        = "NO_GROUP_INDEX";
			public const string Index               = "INDEX";
			public const string NoIndex             = "NO_INDEX";
			public const string IndexMerge          = "INDEX_MERGE";
			public const string NoIndexMerge        = "NO_INDEX_MERGE";
			public const string JoinIndex           = "JOIN_INDEX";
			public const string NoJoinIndex         = "NO_JOIN_INDEX";
			public const string Mrr                 = "MRR";
			public const string NoMrr               = "NO_MRR";
			public const string NoIcp               = "NO_ICP";
			public const string NoRangeOptimization = "NO_RANGE_OPTIMIZATION";
			public const string OrderIndex          = "ORDER_INDEX";
			public const string NoOrderIndex        = "NO_ORDER_INDEX";
			public const string SkipScan            = "SKIP_SCAN";
			public const string NoSkipScan          = "NO_SKIP_SCAN";
		}

		public static class QueryHint
		{
			public const string Bka                        = "BKA";
			public const string NoBka                      = "NO_BKA";
			public const string Bnl                        = "BNL";
			public const string NoBnl                      = "NO_BNL";
			public const string DerivedConditionPushDown   = "DERIVED_CONDITION_PUSHDOWN";
			public const string NoDerivedConditionPushDown = "NO_DERIVED_CONDITION_PUSHDOWN";
			public const string HashJoin                   = "HASH_JOIN";
			public const string NoHashJoin                 = "NO_HASH_JOIN";
			public const string Merge                      = "MERGE";
			public const string NoMerge                    = "NO_MERGE";
			public const string SemiJoin                   = "SEMIJOIN";
			public const string NoSemiJoin                 = "NO_SEMIJOIN";
			public const string SetVar                     = "SET_VAR";
			public const string ResourceGroup              = "RESOURCE_GROUP";

			[Sql.Expression("MAX_EXECUTION_TIME({0})")]
			public static string MaxExecutionTime(int value)
			{
				return $"MAX_EXECUTION_TIME({value})";
			}
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
		[LinqTunnel, Pure]
		[CLSCompliant(false)]
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder), " ", ", ")]
		public static IQueryable<TSource> QueryBlockHint<TSource, TParam>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryBlockHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p)))));
		}
	}
}
