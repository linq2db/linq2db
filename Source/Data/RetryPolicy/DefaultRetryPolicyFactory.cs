using System;

namespace LinqToDB.Data.RetryPolicy
{
#if !SILVERLIGHT
	using DataProvider.SqlServer;
#endif

	static class DefaultRetryPolicyFactory
	{
		public static IRetryPolicy GetRetryPolicy(DataConnection dataContext)
		{
#if !SILVERLIGHT
			if (dataContext.DataProvider is SqlServerDataProvider)
				return new SqlServerRetryPolicy();
#endif

			return null;
		}
	}
}