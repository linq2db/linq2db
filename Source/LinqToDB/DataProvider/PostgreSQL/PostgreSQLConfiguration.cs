namespace LinqToDB.DataProvider.PostgreSQL
{
	/// <summary>
	/// Custom configurations for postgreSql
	/// </summary>
	public static class PostgreSQLConfiguration
	{
		/// <summary>
		/// Ordering null values in postgreSql in last rows. https://www.postgresql.org/docs/current/sql-select.html#SQL-ORDERBY
		/// </summary>
		public static bool DescNullsLast;
	}
}
