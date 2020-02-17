using System;

namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;

	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		public Oracle12SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
	}
}
