using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.DuckDB;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.DuckDB
{
	public static class DuckDBSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IDuckDBSpecificTable<TSource> AsDuckDB<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var wrapped = new Table<TSource>(
				table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsDuckDB, table),
					table.Expression));

			return new DuckDBSpecificTable<TSource>(wrapped);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IDuckDBSpecificQueryable<TSource> AsDuckDB<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var normal = source.ProcessIQueryable();

			return new DuckDBSpecificQueryable<TSource>(
				(IExpressionQuery<TSource>)normal.Provider.CreateQuery<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(AsDuckDB, source),
						normal.Expression)));
		}
	}
}
