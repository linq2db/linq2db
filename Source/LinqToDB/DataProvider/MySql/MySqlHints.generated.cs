#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.MySql
{
	public static partial class MySqlHints
	{
		/// <summary>
		/// Adds a MySQL <c>JOIN_FIXED_ORDER</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinFixedOrderTableHintImpl))]
		public static IMySqlSpecificTable<TSource> JoinFixedOrderHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.JoinFixedOrder);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> JoinFixedOrderTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.JoinFixedOrder);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_FIXED_ORDER</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinFixedOrderInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> JoinFixedOrderInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.JoinFixedOrder);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> JoinFixedOrderInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.JoinFixedOrder);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_FIXED_ORDER</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinFixedOrderHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> JoinFixedOrderHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.JoinFixedOrder, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> JoinFixedOrderHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.JoinFixedOrder, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_ORDER</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinOrderTableHintImpl))]
		public static IMySqlSpecificTable<TSource> JoinOrderHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.JoinOrder);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> JoinOrderTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.JoinOrder);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_ORDER</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinOrderInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> JoinOrderInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.JoinOrder);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> JoinOrderInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.JoinOrder);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_ORDER</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinOrderHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> JoinOrderHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.JoinOrder, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> JoinOrderHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.JoinOrder, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_PREFIX</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinPrefixTableHintImpl))]
		public static IMySqlSpecificTable<TSource> JoinPrefixHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.JoinPrefix);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> JoinPrefixTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.JoinPrefix);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_PREFIX</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinPrefixInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> JoinPrefixInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.JoinPrefix);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> JoinPrefixInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.JoinPrefix);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_PREFIX</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinPrefixHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> JoinPrefixHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.JoinPrefix, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> JoinPrefixHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.JoinPrefix, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_SUFFIX</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinSuffixTableHintImpl))]
		public static IMySqlSpecificTable<TSource> JoinSuffixHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.JoinSuffix);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> JoinSuffixTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.JoinSuffix);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_SUFFIX</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinSuffixInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> JoinSuffixInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.JoinSuffix);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> JoinSuffixInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.JoinSuffix);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_SUFFIX</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinSuffixHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> JoinSuffixHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.JoinSuffix, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> JoinSuffixHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.JoinSuffix, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BkaTableHintImpl))]
		public static IMySqlSpecificTable<TSource> BkaHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.Bka);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> BkaTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.Bka);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BkaInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> BkaInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.Bka);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> BkaInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.Bka);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(BkaHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> BkaHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.Bka, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> BkaHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.Bka, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BatchedKeyAccessTableHintImpl))]
		public static IMySqlSpecificTable<TSource> BatchedKeyAccessHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.BatchedKeyAccess);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> BatchedKeyAccessTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.BatchedKeyAccess);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BatchedKeyAccessInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> BatchedKeyAccessInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.BatchedKeyAccess);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> BatchedKeyAccessInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.BatchedKeyAccess);
		}

		/// <summary>
		/// Adds a MySQL <c>BKA</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(BatchedKeyAccessHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> BatchedKeyAccessHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.BatchedKeyAccess, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> BatchedKeyAccessHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.BatchedKeyAccess, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBkaTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoBkaHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoBka);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoBkaTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoBka);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBkaInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoBkaInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoBka);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoBkaInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoBka);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoBkaHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoBkaHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoBka, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoBkaHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoBka, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBatchedKeyAccessTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoBatchedKeyAccessHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoBatchedKeyAccess);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoBatchedKeyAccessTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoBatchedKeyAccess);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBatchedKeyAccessInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoBatchedKeyAccessInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoBatchedKeyAccess);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoBatchedKeyAccessInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoBatchedKeyAccess);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BKA</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoBatchedKeyAccessHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoBatchedKeyAccessHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoBatchedKeyAccess, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoBatchedKeyAccessHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoBatchedKeyAccess, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BnlTableHintImpl))]
		public static IMySqlSpecificTable<TSource> BnlHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.Bnl);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> BnlTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.Bnl);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BnlInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> BnlInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.Bnl);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> BnlInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.Bnl);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(BnlHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> BnlHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.Bnl, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> BnlHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.Bnl, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BlockNestedLoopTableHintImpl))]
		public static IMySqlSpecificTable<TSource> BlockNestedLoopHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.BlockNestedLoop);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> BlockNestedLoopTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.BlockNestedLoop);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(BlockNestedLoopInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> BlockNestedLoopInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.BlockNestedLoop);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> BlockNestedLoopInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.BlockNestedLoop);
		}

		/// <summary>
		/// Adds a MySQL <c>BNL</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(BlockNestedLoopHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> BlockNestedLoopHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.BlockNestedLoop, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> BlockNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.BlockNestedLoop, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBnlTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoBnlHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoBnl);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoBnlTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoBnl);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBnlInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoBnlInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoBnl);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoBnlInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoBnl);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoBnlHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoBnlHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoBnl, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoBnlHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoBnl, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBlockNestedLoopTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoBlockNestedLoopHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoBlockNestedLoop);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoBlockNestedLoopTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoBlockNestedLoop);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoBlockNestedLoopInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoBlockNestedLoopInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoBlockNestedLoop);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoBlockNestedLoopInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoBlockNestedLoop);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_BNL</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoBlockNestedLoopHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoBlockNestedLoopHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoBlockNestedLoop, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoBlockNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoBlockNestedLoop, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>DERIVED_CONDITION_PUSHDOWN</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(DerivedConditionPushDownTableHintImpl))]
		public static IMySqlSpecificTable<TSource> DerivedConditionPushDownHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.DerivedConditionPushDown);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> DerivedConditionPushDownTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.DerivedConditionPushDown);
		}

		/// <summary>
		/// Adds a MySQL <c>DERIVED_CONDITION_PUSHDOWN</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(DerivedConditionPushDownInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> DerivedConditionPushDownInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.DerivedConditionPushDown);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> DerivedConditionPushDownInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.DerivedConditionPushDown);
		}

		/// <summary>
		/// Adds a MySQL <c>DERIVED_CONDITION_PUSHDOWN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(DerivedConditionPushDownHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> DerivedConditionPushDownHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.DerivedConditionPushDown, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> DerivedConditionPushDownHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.DerivedConditionPushDown, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_DERIVED_CONDITION_PUSHDOWN</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoDerivedConditionPushDownTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoDerivedConditionPushDownHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoDerivedConditionPushDown);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoDerivedConditionPushDownTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoDerivedConditionPushDown);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_DERIVED_CONDITION_PUSHDOWN</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoDerivedConditionPushDownInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoDerivedConditionPushDownInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoDerivedConditionPushDown);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoDerivedConditionPushDownInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoDerivedConditionPushDown);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_DERIVED_CONDITION_PUSHDOWN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoDerivedConditionPushDownHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoDerivedConditionPushDownHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoDerivedConditionPushDown, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoDerivedConditionPushDownHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoDerivedConditionPushDown, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>HASH_JOIN</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(HashJoinTableHintImpl))]
		public static IMySqlSpecificTable<TSource> HashJoinHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.HashJoin);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> HashJoinTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.HashJoin);
		}

		/// <summary>
		/// Adds a MySQL <c>HASH_JOIN</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(HashJoinInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> HashJoinInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.HashJoin);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> HashJoinInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.HashJoin);
		}

		/// <summary>
		/// Adds a MySQL <c>HASH_JOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(HashJoinHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> HashJoinHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.HashJoin, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> HashJoinHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.HashJoin, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_HASH_JOIN</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoHashJoinTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoHashJoinHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoHashJoin);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoHashJoinTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoHashJoin);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_HASH_JOIN</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoHashJoinInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoHashJoinInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoHashJoin);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoHashJoinInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoHashJoin);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_HASH_JOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoHashJoinHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoHashJoinHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoHashJoin, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoHashJoinHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoHashJoin, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>MERGE</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(MergeTableHintImpl))]
		public static IMySqlSpecificTable<TSource> MergeHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.Merge);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> MergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.Merge);
		}

		/// <summary>
		/// Adds a MySQL <c>MERGE</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(MergeInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> MergeInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.Merge);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> MergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.Merge);
		}

		/// <summary>
		/// Adds a MySQL <c>MERGE</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(MergeHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> MergeHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.Merge, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> MergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.Merge, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_MERGE</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoMergeTableHintImpl))]
		public static IMySqlSpecificTable<TSource> NoMergeHint<TSource>(this IMySqlSpecificTable<TSource> table)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoMerge);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,IMySqlSpecificTable<TSource>>> NoMergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => MySqlHints.TableHint(table, Table.NoMerge);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_MERGE</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoMergeInScopeHintImpl))]
		public static IMySqlSpecificQueryable<TSource> NoMergeInScopeHint<TSource>(this IMySqlSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return MySqlHints.TablesInScopeHint(query, Table.NoMerge);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,IMySqlSpecificQueryable<TSource>>> NoMergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => MySqlHints.TablesInScopeHint(query, Table.NoMerge);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_MERGE</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoMergeHintImpl4))]
		public static IMySqlSpecificQueryable<TSource> NoMergeHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoMerge, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> NoMergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => MySqlHints.QueryHint(query, Query.NoMerge, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>GROUP_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(GroupIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> GroupIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.GroupIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> GroupIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.GroupIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_GROUP_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoGroupIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoGroupIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoGroupIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoGroupIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoGroupIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.Index, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.Index, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>INDEX_MERGE</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IndexMergeIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IndexMergeHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.IndexMerge, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IndexMergeIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.IndexMerge, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_INDEX_MERGE</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoIndexMergeIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoIndexMergeHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoIndexMerge, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoIndexMergeIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoIndexMerge, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>JOIN_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(JoinIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> JoinIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.JoinIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> JoinIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.JoinIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_JOIN_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoJoinIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoJoinIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoJoinIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoJoinIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoJoinIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>MRR</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(MrrIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> MrrHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.Mrr, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> MrrIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.Mrr, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_MRR</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoMrrIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoMrrHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoMrr, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoMrrIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoMrr, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_ICP</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoIcpIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoIcpHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoIcp, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoIcpIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoIcp, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_RANGE_OPTIMIZATION</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoRangeOptimizationIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoRangeOptimizationHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoRangeOptimization, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoRangeOptimizationIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoRangeOptimization, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>ORDER_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(OrderIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> OrderIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.OrderIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> OrderIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.OrderIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_ORDER_INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoOrderIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoOrderIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoOrderIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoOrderIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoOrderIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>SKIP_SCAN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(SkipScanIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> SkipScanHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.SkipScan, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> SkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.SkipScan, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_SKIP_SCAN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(NoSkipScanIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> NoSkipScanHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableHint(table, Table.NoSkipScan, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> NoSkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableHint(table, Table.NoSkipScan, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>SEMIJOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(SemiJoinHintImpl5))]
		public static IMySqlSpecificQueryable<TSource> SemiJoinHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params string[] values)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.SemiJoin, values);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string[],IMySqlSpecificQueryable<TSource>>> SemiJoinHintImpl5<TSource>()
			where TSource : notnull
		{
			return (query, values) => MySqlHints.QueryHint(query, Query.SemiJoin, values);
		}

		/// <summary>
		/// Adds a MySQL <c>NO_SEMIJOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoSemiJoinHintImpl5))]
		public static IMySqlSpecificQueryable<TSource> NoSemiJoinHint<TSource>(this IMySqlSpecificQueryable<TSource> query, params string[] values)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.NoSemiJoin, values);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string[],IMySqlSpecificQueryable<TSource>>> NoSemiJoinHintImpl5<TSource>()
			where TSource : notnull
		{
			return (query, values) => MySqlHints.QueryHint(query, Query.NoSemiJoin, values);
		}

		/// <summary>
		/// Adds a MySQL <c>MAX_EXECUTION_TIME(...)</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(MaxExecutionTimeHintImpl2))]
		public static IMySqlSpecificQueryable<TSource> MaxExecutionTimeHint<TSource>(this IMySqlSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.MaxExecutionTime(value));
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,int,IMySqlSpecificQueryable<TSource>>> MaxExecutionTimeHintImpl2<TSource>()
			where TSource : notnull
		{
			return (query, value) => MySqlHints.QueryHint(query, Query.MaxExecutionTime(value));
		}

		/// <summary>
		/// Adds a MySQL <c>SET_VAR</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(SetVarHintImpl3))]
		public static IMySqlSpecificQueryable<TSource> SetVarHint<TSource>(this IMySqlSpecificQueryable<TSource> query, string value)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.SetVar, value);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string,IMySqlSpecificQueryable<TSource>>> SetVarHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, value) => MySqlHints.QueryHint(query, Query.SetVar, value);
		}

		/// <summary>
		/// Adds a MySQL <c>RESOURCE_GROUP</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ResourceGroupHintImpl3))]
		public static IMySqlSpecificQueryable<TSource> ResourceGroupHint<TSource>(this IMySqlSpecificQueryable<TSource> query, string value)
			where TSource : notnull
		{
			return MySqlHints.QueryHint(query, Query.ResourceGroup, value);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,string,IMySqlSpecificQueryable<TSource>>> ResourceGroupHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, value) => MySqlHints.QueryHint(query, Query.ResourceGroup, value);
		}

		/// <summary>
		/// Adds a MySQL <c>USE INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE INDEX FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseIndexForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseIndexForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseIndexForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseIndexForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseIndexForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE INDEX FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseIndexForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseIndexForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseIndexForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseIndexForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseIndexForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE INDEX FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseIndexForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseIndexForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseIndexForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseIndexForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseIndexForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE KEY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseKeyIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseKeyHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseKey, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseKeyIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseKey, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE KEY FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseKeyForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseKeyForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseKeyForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseKeyForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseKeyForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE KEY FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseKeyForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseKeyForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseKeyForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseKeyForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseKeyForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>USE KEY FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(UseKeyForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> UseKeyForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.UseKeyForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> UseKeyForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.UseKeyForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE INDEX FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreIndexForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreIndexForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreIndexForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreIndexForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreIndexForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE INDEX FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreIndexForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreIndexForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreIndexForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreIndexForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreIndexForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE INDEX FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreIndexForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreIndexForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreIndexForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreIndexForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreIndexForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE KEY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreKeyIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreKeyHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreKey, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreKeyIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreKey, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE KEY FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreKeyForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreKeyForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreKeyForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreKeyForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreKeyForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE KEY FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreKeyForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreKeyForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreKeyForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreKeyForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreKeyForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>IGNORE KEY FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(IgnoreKeyForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> IgnoreKeyForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.IgnoreKeyForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> IgnoreKeyForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.IgnoreKeyForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE INDEX</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceIndexIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceIndexHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceIndex, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceIndex, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE INDEX FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceIndexForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceIndexForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceIndexForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceIndexForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceIndexForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE INDEX FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceIndexForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceIndexForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceIndexForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceIndexForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceIndexForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE INDEX FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceIndexForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceIndexForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceIndexForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceIndexForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceIndexForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE KEY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceKeyIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceKeyHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceKey, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceKeyIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceKey, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE KEY FOR JOIN</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceKeyForJoinIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceKeyForJoinHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceKeyForJoin, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceKeyForJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceKeyForJoin, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE KEY FOR ORDER BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceKeyForOrderByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceKeyForOrderByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceKeyForOrderBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceKeyForOrderByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceKeyForOrderBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FORCE KEY FOR GROUP BY</c> index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.MySql, nameof(ForceKeyForGroupByIndexHintImpl))]
		public static IMySqlSpecificTable<TSource> ForceKeyForGroupByHint<TSource>(this IMySqlSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return MySqlHints.TableIndexHint(table, Table.ForceKeyForGroupBy, indexNames);
		}
		static Expression<Func<IMySqlSpecificTable<TSource>,string[],IMySqlSpecificTable<TSource>>> ForceKeyForGroupByIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => MySqlHints.TableIndexHint(table, Table.ForceKeyForGroupBy, indexNames);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR UPDATE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForUpdateHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForUpdateHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForUpdate, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForUpdateHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForUpdate, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR UPDATE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForUpdateNoWaitHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForUpdateNoWaitHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForUpdate, SubQuery.NoWait, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForUpdateNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForUpdate, SubQuery.NoWait, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR UPDATE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForUpdateSkipLockedHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForUpdateSkipLockedHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForUpdate, SubQuery.SkipLocked, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForUpdateSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForUpdate, SubQuery.SkipLocked, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR SHARE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForShareHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForShareHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForShare, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForShareHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForShare, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR SHARE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForShareNoWaitHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForShareNoWaitHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForShare, SubQuery.NoWait, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForShareNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForShare, SubQuery.NoWait, tableIDs);
		}

		/// <summary>
		/// Adds a MySQL <c>FOR SHARE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ForShareSkipLockedHintImpl))]
		public static IMySqlSpecificQueryable<TSource> ForShareSkipLockedHint<TSource>(
			this IMySqlSpecificQueryable<TSource> query,
			params Sql.SqlID[]                    tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, SubQuery.ForShare, SubQuery.SkipLocked, tableIDs);
		}
		static Expression<Func<IMySqlSpecificQueryable<TSource>,Sql.SqlID[],IMySqlSpecificQueryable<TSource>>> ForShareSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, SubQuery.ForShare, SubQuery.SkipLocked, tableIDs);
		}

	}
}
