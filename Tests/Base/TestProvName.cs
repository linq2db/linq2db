using LinqToDB;

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

		public const string AllMySql             = "MySql,MySqlConnector,MySql57,MariaDB";
		public const string AllMySqlData         = "MySql,MySql57,MariaDB";
		public const string AllPostgreSQL        = "PostgreSQL,PostgreSQL.9.2,PostgreSQL.9.3,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.EDGE";
		public const string AllPostgreSQLv3      = "PostgreSQL,PostgreSQL.9.2,PostgreSQL.9.3,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11";
		public const string AllPostgreSQLLess10  = "PostgreSQL,PostgreSQL.9.2,PostgreSQL.9.3,PostgreSQL.9.5";
		public const string AllPostgreSQL93Plus  = "PostgreSQL,PostgreSQL.9.3,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.EDGE";
		public const string AllPostgreSQL95Plus  = "PostgreSQL,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.EDGE";
		public const string AllPostgreSQL10Plus  = "PostgreSQL.10,PostgreSQL.11,PostgreSQL.EDGE";
		public const string AllOracle            = "Oracle.Native,Oracle.Managed";
		public const string AllFirebird          = "Firebird,Firebird3";
		public const string AllSQLite            = "SQLite.Classic,SQLite.MS";
		public const string AllSybase            = "Sybase,Sybase.Managed";
		public const string AllSqlServer         = "SqlServer.2000,SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlAzure.2012";
		public const string AllSqlServer2005Plus = "SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlAzure.2012";
		public const string AllSqlServer2008Plus = "SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlAzure.2012";
		public const string AllSqlServer2012Plus = "SqlServer.2012,SqlServer.2014,SqlAzure.2012";
		public const string AllSqlServer2016Plus = "SqlAzure.2012";
		public const string AllSQLiteNorthwind   = "Northwind.SQLite,Northwind.SQLite.MS";
	}
}
