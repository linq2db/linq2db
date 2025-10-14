using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	public static class SqlServerSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> AsSqlServer<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlServer, table),
					table.Expression)
			);

			return new SqlServerSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> AsSqlServer<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlServer, source),
					currentSource.Expression)));
		}
	}
}
