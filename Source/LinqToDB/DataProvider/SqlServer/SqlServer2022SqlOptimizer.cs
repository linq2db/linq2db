namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	sealed class SqlServer2022SqlOptimizer : SqlServer2019SqlOptimizer
	{
		public SqlServer2022SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2022)
		{
		}
	}
}
