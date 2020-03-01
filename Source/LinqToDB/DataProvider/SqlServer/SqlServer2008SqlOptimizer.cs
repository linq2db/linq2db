using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2008SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2008SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2008)
		{
		}
	}
}
