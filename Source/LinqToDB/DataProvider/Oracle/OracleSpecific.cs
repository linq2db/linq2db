using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Oracle
{
	using Linq;
	using SqlProvider;

	public interface IOracleSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class OracleSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IOracleSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public OracleSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IOracleSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class OracleSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IOracleSpecificQueryable<TSource>, ITable
	{
		public OracleSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class OracleTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IOracleSpecificTable<TSource> AsOracle<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsOracle, table),
				table.Expression);

			return new OracleSpecificTable<TSource>(table);
		}

		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IOracleSpecificQueryable<TSource> AsOracle<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new OracleSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsOracle, source),
					currentSource.Expression)));
		}
	}
}
