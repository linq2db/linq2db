﻿using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SQLite
{
	public static class SQLiteSpecificExtensions
	{
		[LinqTunnel, Pure, IsQueryable]
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
