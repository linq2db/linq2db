using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.DataProvider
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
