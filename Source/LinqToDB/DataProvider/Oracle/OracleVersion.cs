namespace LinqToDB.DataProvider.Oracle
{
	/// <summary>
	/// Supported Oracle SQL dialects.
	/// </summary>
	public enum OracleVersion
	{
		/// <summary>
		/// Use automatic detection of dialect by asking Oracle server for version.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Oracle 11g dialect.
		/// </summary>
		v11 = 11,
		/// <summary>
		/// Oracle 12c+ dialect.
		/// </summary>
		v12 = 12,
	}
}
