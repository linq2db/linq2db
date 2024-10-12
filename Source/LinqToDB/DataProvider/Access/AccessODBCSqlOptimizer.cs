namespace LinqToDB.DataProvider.Access
{
	using LinqToDB.Internal.SqlProvider;

	sealed class AccessODBCSqlOptimizer : AccessSqlOptimizer
	{
		public AccessODBCSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
	}
}
