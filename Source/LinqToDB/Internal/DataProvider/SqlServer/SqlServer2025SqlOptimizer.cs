using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2025SqlOptimizer : SqlServer2022SqlOptimizer
	{
		public SqlServer2025SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2025)
		{
		}

		protected SqlServer2025SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServer2025SqlExpressionConvertVisitor(allowModify, SQLVersion);
		}
	}
}
