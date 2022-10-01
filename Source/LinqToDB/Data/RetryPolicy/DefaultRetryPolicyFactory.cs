namespace LinqToDB.Data.RetryPolicy
{
	using DataProvider.SqlServer;
	using LinqToDB.DataProvider.ClickHouse;

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
