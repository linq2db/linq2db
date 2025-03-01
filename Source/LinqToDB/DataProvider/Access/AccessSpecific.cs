using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq;

namespace LinqToDB.DataProvider.Access
{
	public interface IAccessSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	sealed class AccessSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IAccessSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public AccessSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public interface IAccessSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	sealed class AccessSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IAccessSpecificQueryable<TSource>, ITable
	{
		public AccessSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static class AccessSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IAccessSpecificTable<TSource> AsAccess<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext, Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsAccess, table),
				table.Expression));

			return new AccessSpecificTable<TSource>(newTable);
		}

		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IAccessSpecificQueryable<TSource> AsAccess<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new AccessSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsAccess, source),
					currentSource.Expression)));
		}
	}
}
