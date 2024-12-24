using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internals.SqlProvider;

namespace LinqToDB.Internals.DataProviders.SqlServer
{
	sealed class SqlServer2016SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2016SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2016)
		{
		}
	}
}
