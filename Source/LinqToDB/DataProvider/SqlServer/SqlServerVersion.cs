namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// SQL Server dialect for SQL generation.
	/// </summary>
	public enum SqlServerVersion
	{
		/// <summary>
		/// Use automatic detection of dialect by asking SQL Server for version (compatibility level information used).
		/// </summary>
		AutoDetect,
		/// <summary>
		/// SQL Server 2005 dialect.
		/// </summary>
		v2005,
		/// <summary>
		/// SQL Server 2008 dialect.
		/// </summary>
		v2008,
		/// <summary>
		/// SQL Server 2012 dialect.
		/// </summary>
		v2012,
		/// <summary>
		/// SQL Server 2014 dialect.
		/// </summary>
		v2014,
		/// <summary>
		/// SQL Server 2016 dialect.
		/// </summary>
		v2016,
		/// <summary>
		/// SQL Server 2017 dialect.
		/// </summary>
		v2017,
		/// <summary>
		/// SQL Server 2019 dialect.
		/// </summary>
		v2019,
		/// <summary>
		/// SQL Server 2022 dialect.
		/// </summary>
		v2022,
		/// <summary>
		/// SQL Server 2025 dialect.
		/// </summary>
		v2025,
	}
}
