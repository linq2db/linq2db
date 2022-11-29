namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	sealed class SqlServer2016SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2016SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2016)
		{
		}
	}
}
