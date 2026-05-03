namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// SQL Server database provider.
	/// </summary>
	public enum SqlServerProvider
	{
		/// <summary>
		/// Automatically detect provider. If application has <c>Microsoft.Data.SqlClient</c> assembly deployed, then
		/// <see cref="MicrosoftDataSqlClient" /> provider used.
		/// Otherwise use <see cref="SystemDataSqlClient" /> provider.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// <c>System.Data.SqlClient</c> legacy provider.
		/// </summary>
		SystemDataSqlClient,
		/// <summary>
		/// <c>Microsoft.Data.SqlClient</c> provider.
		/// </summary>
		MicrosoftDataSqlClient,
	}
}
