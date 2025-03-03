using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.ClickHouse
{
	public static class ClickHouseSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificTable<TSource> AsClickHouse<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsClickHouse, table),
				table.Expression)
			);

			return new ClickHouseSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificQueryable<TSource> AsClickHouse<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsClickHouse, source),
					currentSource.Expression)));
		}
	}
}
