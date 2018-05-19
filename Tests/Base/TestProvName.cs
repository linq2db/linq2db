using System;

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

		/// <summary>
		/// Fake provider, which doesn't execute any real queries. Could be used for tests, that shouldn't be affected
		/// by real database access.
		/// </summary>
		public const string NoopProvider  = "TestNoopProvider";
	}
}
