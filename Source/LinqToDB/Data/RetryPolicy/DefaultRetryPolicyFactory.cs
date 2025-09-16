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
				var o = dataContext.Options.RetryPolicyOptions;
				return new YdbRetryPolicy(
					o.MaxRetryCount,
					o.MaxDelay,
					o.RandomFactor,
					o.ExponentialBase,
					o.Coefficient,
					treatAsIdempotent : true
				);
			}

			return null;
		}
	}
}
