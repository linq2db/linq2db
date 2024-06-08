using System;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class AccessODBCSqlOptimizer : AccessSqlOptimizer
	{
		public AccessODBCSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
	}
}
