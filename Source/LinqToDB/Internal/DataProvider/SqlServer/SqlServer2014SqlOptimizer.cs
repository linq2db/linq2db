using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2014SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2014SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2016)
		{
		}
	}
}
