// Generated.
//
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Oracle
{
	public static partial class OracleHints
	{
		[ExpressionMethod(ProviderName.Oracle, nameof(FullHintImpl))]
		public static IOracleSpecificTable<TSource> FullHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.Full);
		}

		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> FullHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.Full);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(FullInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> FullInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.Full);
		}

		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FullInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.Full);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterHintImpl))]
		public static IOracleSpecificTable<TSource> ClusterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.Cluster);
		}

		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> ClusterHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.Cluster);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> ClusterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.Cluster);
		}

		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ClusterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.Cluster);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(HashHintImpl))]
		public static IOracleSpecificTable<TSource> HashHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.Hash);
		}

		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> HashHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.Hash);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(HashInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> HashInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.Hash);
		}

		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> HashInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.Hash);
		}

		[ExpressionMethod(nameof(AllRowsHintImpl))]
		public static IOracleSpecificQueryable<TSource> AllRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.AllRows);
		}

		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AllRowsHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.AllRows);
		}

		[ExpressionMethod(nameof(FirstRowsHintImpl))]
		public static IOracleSpecificQueryable<TSource> FirstRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint(Query.FirstRows(value));
		}

		static Expression<Func<IOracleSpecificQueryable<TSource>,int,IOracleSpecificQueryable<TSource>>> FirstRowsHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint(Query.FirstRows(value));
		}

	}
}
