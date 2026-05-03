using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.DataProvider
{
	static class SqlProviderHelper
	{
		internal static readonly ObjectPool<SqlQueryValidatorVisitor> ValidationVisitorPool = new(() => new SqlQueryValidatorVisitor(), v => v.Cleanup(), 100);

		public static bool IsValidQuery(IQueryElement element, SelectQuery? parentQuery, SqlJoinedTable? fakeJoin, int? columnSubqueryLevel, SqlProviderFlags providerFlags, out string? errorMessage)
		{
			using var visitor = ValidationVisitorPool.Allocate();

			return visitor.Value.IsValidQuery(element, parentQuery, fakeJoin, columnSubqueryLevel, providerFlags, out errorMessage);
		}
	}
}
