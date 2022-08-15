﻿namespace LinqToDB.DataProvider.PostgreSQL
{
	/// <summary>
	/// PostgreSQL language dialect. Version defines minimal PostgreSQL version to use this dialect.
	/// </summary>
	public enum PostgreSQLVersion
	{
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
		/// PostgreSQL 15+ SQL dialect.
		/// </summary>
		v15,
	}
}
