namespace LinqToDB.Data.RetryPolicy
{
	using DataProvider.ClickHouse;
	using DataProvider.SqlServer;

	static class DefaultRetryPolicyFactory
	{
		public static IRetryPolicy? GetRetryPolicy(DataConnection dataContext)
		{
			if (dataContext.DataProvider is SqlServerDataProvider)
				return new SqlServerRetryPolicy();
			if (dataContext.DataProvider is ClickHouseDataProvider { Name: ProviderName.ClickHouseOctonica } clickHouseDataProvider)
				return new ClickHouseRetryPolicy();

			return null;
		}
	}
}
