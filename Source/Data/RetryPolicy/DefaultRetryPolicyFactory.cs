using System;

namespace LinqToDB.Data.RetryPolicy
{
	using DataProvider.SqlServer;

	static class DefaultRetryPolicyFactory
	{
		public static IRetryPolicy GetRetryPolicy(DataConnection dataContext)
		{
			if (dataContext.DataProvider is SqlServerDataProvider)
				return new SqlServerRetryPolicy();

			return null;
		}
	}
}
