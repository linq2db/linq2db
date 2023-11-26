namespace LinqToDB.DataProvider
{
	using Common.Internal;
	using SqlQuery;
	using SqlQuery.Visitors;
	using SqlProvider;

	public static class SqlProviderHelper
	{
		internal static readonly ObjectPool<SqlQueryValidatorVisitor> ValidationVisitorPool = new(() => new SqlQueryValidatorVisitor(), v => v.Cleanup(), 100);

		public static bool IsValidQuery(SelectQuery selectQuery, SelectQuery? parentQuery, bool forColumn, SqlProviderFlags providerFlags)
		{
			using var visitor = ValidationVisitorPool.Allocate();

			return visitor.Value.IsValidQuery(selectQuery, parentQuery, forColumn, providerFlags);
		}
	}
}
