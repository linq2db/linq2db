using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public interface IPostgreSQLSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class PostgreSQLSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IPostgreSQLSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public PostgreSQLSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IPostgreSQLSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class PostgreSQLSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IPostgreSQLSpecificQueryable<TSource>, ITable
	{
		public PostgreSQLSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class PostgreSQLTools
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
