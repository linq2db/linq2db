using System.Linq.Expressions;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Linq;
	using SqlProvider;

	public interface IPostgreSQLSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class PostgreSQLSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IPostgreSQLSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public PostgreSQLSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IPostgreSQLSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class PostgreSQLSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IPostgreSQLSpecificQueryable<TSource>, ITable
	{
		public PostgreSQLSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class PostgreSQLTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IPostgreSQLSpecificQueryable<TSource> AsPostgreSQL<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new PostgreSQLSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsPostgreSQL, source),
					currentSource.Expression)));
		}
	}
}
