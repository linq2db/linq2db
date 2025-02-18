using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	sealed class SqlServer2014SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2014SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2016)
		{
		}
	}
}
