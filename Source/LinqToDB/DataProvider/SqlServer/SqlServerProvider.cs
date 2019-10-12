namespace LinqToDB.DataProvider.SqlServer
{
	public enum SqlServerProvider
	{
#if NET45 || NET46
		Default = SystemData,
		SystemData = 0,
#else
		Default = SystemDataSqlClient,
#endif
		SystemDataSqlClient = 1,
		MicrosoftDataSqlClient = 2
	}
}
