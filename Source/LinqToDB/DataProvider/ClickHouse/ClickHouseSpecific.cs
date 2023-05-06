using System;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Linq;
	using SqlProvider;

	public interface IClickHouseSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class ClickHouseSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IClickHouseSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public ClickHouseSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IClickHouseSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class ClickHouseSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IClickHouseSpecificQueryable<TSource>, ITable
	{
		public ClickHouseSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class ClickHouseTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificTable<TSource> AsClickHouse<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsClickHouse, table),
				table.Expression)
			);

			return new ClickHouseSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificQueryable<TSource> AsClickHouse<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsClickHouse, source),
					currentSource.Expression)));
		}
	}
}
