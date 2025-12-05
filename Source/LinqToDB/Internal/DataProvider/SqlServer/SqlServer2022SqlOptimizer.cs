using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2022SqlOptimizer : SqlServer2019SqlOptimizer
	{
		public SqlServer2022SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2022)
		{
		}

		protected SqlServer2022SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}
	}
}
