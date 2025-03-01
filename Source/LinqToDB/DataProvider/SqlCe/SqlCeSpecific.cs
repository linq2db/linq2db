using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SqlCe
{
	public interface ISqlCeSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class SqlCeSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlCeSpecificTable<TSource>
		where TSource : notnull
	{
		public SqlCeSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface ISqlCeSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class SqlCeSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlCeSpecificQueryable<TSource>
	{
		public SqlCeSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static class SqlCeSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificTable<TSource> AsSqlCe<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlCe, table),
					table.Expression)
			);

			return new SqlCeSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificQueryable<TSource> AsSqlCe<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new SqlCeSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlCe, source),
					currentSource.Expression)));
		}
	}
}
