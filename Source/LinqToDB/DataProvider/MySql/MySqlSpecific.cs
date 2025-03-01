using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.MySql
{
	public interface IMySqlSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class MySqlSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IMySqlSpecificTable<TSource>
		where TSource : notnull
	{
		public MySqlSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IMySqlSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class MySqlSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IMySqlSpecificQueryable<TSource>
	{
		public MySqlSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static class MySqlSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificTable<TSource> AsMySql<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsMySql, table),
				table.Expression)
			);

			return new MySqlSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IMySqlSpecificQueryable<TSource> AsMySql<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new MySqlSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsMySql, source),
					currentSource.Expression)));
		}
	}
}
