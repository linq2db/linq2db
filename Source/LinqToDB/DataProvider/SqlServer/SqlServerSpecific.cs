using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	using Linq;
	using SqlProvider;

	public interface ISqlServerSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class SqlServerSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlServerSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SqlServerSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface ISqlServerSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class SqlServerSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlServerSpecificQueryable<TSource>, ITable
	{
		public SqlServerSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class SqlServerTools
	{
		[LinqTunnel, Pure]
		[LinqToDB.Sql.QueryExtension(null, LinqToDB.Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> AsSqlServer<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSqlServer, table),
				table.Expression);

			return new SqlServerSpecificTable<TSource>(table);
		}

		[LinqTunnel, Pure]
		[LinqToDB.Sql.QueryExtension(null, LinqToDB.Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> AsSqlServer<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlServer, source),
					currentSource.Expression)));
		}
	}
}
