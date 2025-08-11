namespace LinqToDB.DataProvider.ClickHouse
{
	/// <summary>
	/// Defines supported ClickHouse ADO.NET provider implementation libraries.
	/// </summary>
	public enum ClickHouseProvider
	{
		/// <summary>
		/// Detect provider automatically.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Octonica ClickHouse provider: https://github.com/Octonica/ClickHouseClient.
		/// </summary>
		Octonica,
		/// <summary>
		///  Official ClickHouse provider: https://github.com/ClickHouse/clickhouse-cs.
		/// </summary>
		ClickHouseDriver,
		/// <summary>
		/// MySqlConnector provider: https://mysqlconnector.net/.
		/// </summary>
		MySqlConnector,
	}
}
