namespace LinqToDB.DataProvider.PostgreSQL
{
	/// <summary>
	/// PostgreSQL language dialect. Version specifies minimal PostgreSQL version to use this dialect.
	/// </summary>
	public enum PostgreSQLVersion
	{
		/// <summary>
		/// Use automatic detection of dialect by asking PostgreSQL server for version.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// PostgreSQL 9.2+ SQL dialect.
		/// </summary>
		v92,
		/// <summary>
		/// PostgreSQL 9.3+ SQL dialect.
		/// </summary>
		v93,
		/// <summary>
		/// PostgreSQL 9.5+ SQL dialect.
		/// </summary>
		v95,
		/// <summary>
		/// PostgreSQL 11+ SQL dialect (window-frame GROUPS mode, frame EXCLUDE, RANGE value-offset).
		/// </summary>
		v11,
		/// <summary>
		/// PostgreSQL 12+ SQL dialect (CTE AS [NOT] MATERIALIZED).
		/// </summary>
		v12,
		/// <summary>
		/// PostgreSQL 13+ SQL dialect.
		/// </summary>
		v13,
		/// <summary>
		/// PostgreSQL 15+ SQL dialect.
		/// </summary>
		v15,
		/// <summary>
		/// PostgreSQL 18+ SQL dialect.
		/// </summary>
		v18,
		/// <summary>
		/// PostgreSQL 19+ SQL dialect.
		/// </summary>
		v19,
	}
}
