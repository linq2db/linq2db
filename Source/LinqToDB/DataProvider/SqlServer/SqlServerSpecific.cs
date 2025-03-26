using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	public interface ISqlServerSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class SqlServerSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlServerSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SqlServerSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface ISqlServerSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class SqlServerSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlServerSpecificQueryable<TSource>, ITable
	{
		public SqlServerSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class SqlServerTools
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
