using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.Ydb;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Provides YDB-specific extension methods.
	/// </summary>
	public static class YdbSpecificExtensions
	{
		/// <summary>
		/// Marks the table as a YDB-specific table, enabling YDB table/query extension methods on it.
		/// </summary>
		/// <typeparam name="TSource">Table record type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <returns>YDB-specific table source.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IYdbSpecificTable<TSource> AsYdb<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var wrapped = new Table<TSource>(
				table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsYdb, table),
					table.Expression));

			return new YdbSpecificTable<TSource>(wrapped);
		}

		/// <summary>
		/// Marks the query as a YDB-specific query, enabling YDB query extension methods on it.
		/// </summary>
		/// <typeparam name="TSource">Query record type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <returns>YDB-specific query source.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IYdbSpecificQueryable<TSource> AsYdb<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var normal = source.ProcessIQueryable();

			return new YdbSpecificQueryable<TSource>(
				(IExpressionQuery<TSource>)normal.Provider.CreateQuery<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(AsYdb, source),
						normal.Expression)));
		}
	}
}
