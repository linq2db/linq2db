using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.ClickHouse;
using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.Ydb;

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

			if (dataContext.DataProvider is YdbDataProvider)
			{
				var o           = dataContext.Options.RetryPolicyOptions;
				var maxAttempts = o.MaxRetryCount > 0 ? o.MaxRetryCount : 10;
				// YdbRetryPolicyConfig.Default
				return new YdbRetryPolicy(
					maxAttempts,
					fastBackoffBaseMs: 5,
					slowBackoffBaseMs: 50,
					fastCapBackoffMs: 500,
					slowCapBackoffMs: 5000,
					enableRetryIdempotence: false
				);
			}

			return null;
		}
	}
}
