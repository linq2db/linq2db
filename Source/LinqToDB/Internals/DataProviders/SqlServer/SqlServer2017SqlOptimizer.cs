using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internals.SqlProvider;

namespace LinqToDB.Internals.DataProviders.SqlServer
{
	sealed class SqlServer2017SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2017SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2017)
		{
		}
	}
}
