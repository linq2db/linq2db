using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Data.RetryPolicy
{
	static class DefaultRetryPolicyFactory
	{
		public static IRetryPolicy? GetRetryPolicy(DataConnection dataContext)
		{
			if (dataContext.DataProvider is SqlServerDataProvider)
			{
				var retryOptions = dataContext.Options.RetryPolicyOptions;

				return new SqlServerRetryPolicy(
					retryOptions.MaxRetryCount,
					retryOptions.MaxDelay,
					retryOptions.RandomFactor,
					retryOptions.ExponentialBase,
					retryOptions.Coefficient,
					null);
			}

			if (dataContext.DataProvider is ClickHouseDataProvider { Name: ProviderName.ClickHouseOctonica } clickHouseDataProvider)
				return new ClickHouseRetryPolicy();

			return null;
		}
	}
}
