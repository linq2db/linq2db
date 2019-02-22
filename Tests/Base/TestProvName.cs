namespace Tests
{
	public static class TestProvName
	{
		public const string SqlAzure          = "SqlAzure.2012";
		public const string MariaDB           = "MariaDB";
		public const string MySql57           = "MySql57";
		public const string Firebird3         = "Firebird3";
		public const string Northwind         = "Northwind";
		public const string NorthwindSQLite   = "Northwind.SQLite";
		public const string NorthwindSQLiteMS = "Northwind.SQLite.MS";
		public const string PostgreSQL10      = "PostgreSQL.10";
		public const string PostgreSQL11      = "PostgreSQL.11";
		/// <summary>
		/// Latest PostgreSQL and npgsql versions.
		/// </summary>
		public const string PostgreSQLLatest  = "PostgreSQL.EDGE";

		/// <summary>
		/// Fake provider, which doesn't execute any real queries. Could be used for tests, that shouldn't be affected
		/// by real database access.
		/// </summary>
		public const string NoopProvider  = "TestNoopProvider";
	}
}
