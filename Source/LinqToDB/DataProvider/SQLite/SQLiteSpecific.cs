﻿using System;
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

	sealed class SQLiteSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISQLiteSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SQLiteSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public static partial class SQLiteTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISQLiteSpecificTable<TSource> AsSQLite<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSQLite, table),
					table.Expression)
			);

			return new SQLiteSpecificTable<TSource>(newTable);
		}
	}
}
