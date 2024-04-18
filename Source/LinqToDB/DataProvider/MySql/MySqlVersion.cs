namespace LinqToDB.DataProvider.MySql
{
	/// <summary>
	/// MySQL and MariaDB language dialects. Version specifies minimal version to use this dialect.
	/// </summary>
	public enum MySqlVersion
	{
		/// <summary>
		/// Use automatic detection of dialect by asking server for version.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// MySql 5.7 SQL dialect.
		/// </summary>
		MySql57,
		/// <summary>
		/// MySql 8.0.x SQL dialect.
		/// </summary>
		MySql80,
		/// <summary>
		/// MariaDB 10+ SQL dialect.
		/// </summary>
		MariaDB10,
	}
}
