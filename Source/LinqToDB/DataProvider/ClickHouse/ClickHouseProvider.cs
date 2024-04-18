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
		///  DarkWanderer ClickHouse provider: https://github.com/DarkWanderer/ClickHouse.Client.
		/// </summary>
		ClickHouseClient,
		/// <summary>
		/// MySqlConnector provider: https://mysqlconnector.net/.
		/// </summary>
		MySqlConnector
	}
}
