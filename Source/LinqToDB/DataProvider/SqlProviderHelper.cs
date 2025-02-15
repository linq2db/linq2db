﻿using LinqToDB.Common.Internal;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.DataProvider
{
	public static class SqlProviderHelper
	{
		internal static readonly ObjectPool<SqlQueryValidatorVisitor> ValidationVisitorPool = new(() => new SqlQueryValidatorVisitor(), v => v.Cleanup(), 100);

		public static bool IsValidQuery(SelectQuery selectQuery, SelectQuery? parentQuery, SqlJoinedTable? fakeJoin, int? columnSubqueryLevel, SqlProviderFlags providerFlags, out string? errorMessage)
		{
			using var visitor = ValidationVisitorPool.Allocate();

			return visitor.Value.IsValidQuery(selectQuery, parentQuery, fakeJoin, columnSubqueryLevel, providerFlags, out errorMessage);
		}
	}
}
