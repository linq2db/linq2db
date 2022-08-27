using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlCe
{
	using Linq;
	using SqlProvider;

	public interface ISqlCeSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class SqlCeSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlCeSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SqlCeSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface ISqlCeSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class SqlCeSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlCeSpecificQueryable<TSource>, ITable
	{
		public SqlCeSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class SqlServerTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificTable<TSource> AsSqlCe<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSqlCe, table),
				table.Expression);

			return new SqlCeSpecificTable<TSource>(table);
		}

		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificQueryable<TSource> AsSqlCe<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlCeSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlCe, source),
					currentSource.Expression)));
		}
	}
}
