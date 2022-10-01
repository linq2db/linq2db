using System;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Access
{
	using Linq;
	using SqlProvider;

	public interface IAccessSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class AccessSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IAccessSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public AccessSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IAccessSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class AccessSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IAccessSpecificQueryable<TSource>, ITable
	{
		public AccessSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class AccessTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IAccessSpecificTable<TSource> AsAccess<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsAccess, table),
				table.Expression);

			return new AccessSpecificTable<TSource>(table);
		}

		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IAccessSpecificQueryable<TSource> AsAccess<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new AccessSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsAccess, source),
					currentSource.Expression)));
		}
	}
}
