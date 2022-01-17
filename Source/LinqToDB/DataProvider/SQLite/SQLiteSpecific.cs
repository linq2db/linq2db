using System;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SQLite
{
	using Linq;
	using SqlProvider;

	public interface ISQLiteSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class SQLiteSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISQLiteSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SQLiteSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	/*
	public interface ISQLiteSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class SQLiteSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISQLiteSpecificQueryable<TSource>, ITable
	{
		public SQLiteSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
	*/

	public static partial class SQLiteTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISQLiteSpecificTable<TSource> AsSQLite<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSQLite, table),
				table.Expression);

			return new SQLiteSpecificTable<TSource>(table);
		}

		/*
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISQLiteSpecificQueryable<TSource> AsSQLite<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SQLiteSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSQLite, source),
					currentSource.Expression)));
		}
		*/
	}
}
