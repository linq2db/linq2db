namespace LinqToDB.DataProvider.SqlServer;

using SqlProvider;

class SqlServer2014SqlOptimizer : SqlServer2012SqlOptimizer
{
	public SqlServer2014SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2016)
	{
	}
}
