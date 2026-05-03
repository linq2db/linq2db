namespace LinqToDB.DataProvider.MySql
{
	/// <summary>
	/// MySql ADO.NET provider.
	/// </summary>
	public enum MySqlProvider
	{
		/// <summary>
		/// Automatically detect available provider.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// MySql.Data provider.
		/// </summary>
		MySqlData,
		/// <summary>
		/// MySqlConnector provider.
		/// </summary>
		MySqlConnector,
	}
}
