using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public interface IPostgreSQLSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class PostgreSQLSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IPostgreSQLSpecificTable<TSource>
		where TSource : notnull
	{
		public PostgreSQLSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IPostgreSQLSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class PostgreSQLSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IPostgreSQLSpecificQueryable<TSource>
	{
		public PostgreSQLSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static class PostgreSQLSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IPostgreSQLSpecificQueryable<TSource> AsPostgreSQL<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new PostgreSQLSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsPostgreSQL, source),
					currentSource.Expression)));
		}
	}
}
