namespace LinqToDB.DataProvider.Oracle
{
	public enum OracleVersion
	{
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
