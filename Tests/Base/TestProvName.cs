using LinqToDB;

namespace Tests
{
	public static class TestProvName
	{
		public const string Default           = "SQLite.Default";

		public const string SqlAzure          = "SqlAzure";
		public const string MariaDB           = "MariaDB";
		/// <summary>
		/// MySQL 5.5
		/// Features:
		/// - supports year(2) type
		/// - fractional seconds not supported
		/// </summary>
		public const string MySql55           = "MySql55";
		public const string Firebird3         = "Firebird3";
		public const string Firebird4         = "Firebird4";
		public const string Northwind         = "Northwind";
		public const string NorthwindSQLite   = "Northwind.SQLite";
		public const string NorthwindSQLiteMS = "Northwind.SQLite.MS";
		public const string PostgreSQL10      = "PostgreSQL.10";
		public const string PostgreSQL11      = "PostgreSQL.11";
		public const string PostgreSQL12      = "PostgreSQL.12";
		public const string PostgreSQL13      = "PostgreSQL.13";
		public const string Oracle11Native    = "Oracle.11.Native";
		public const string Oracle11Managed   = "Oracle.11.Managed";
		public const string SqlServer2019     = "SqlServer.2019";

		public const string SqlServer2019SequentialAccess       = "SqlServer.2019.SA";
		public const string SqlServer2019FastExpressionCompiler = "SqlServer.2019.FEC";

		/// <summary>
		/// SQLite classic provider wrapped into MiniProfiler without mappings to provider types configured.
		/// Used to test general compatibility of linq2db with wrapped providers.
		/// </summary>
		public const string SQLiteClassicMiniProfilerUnmapped = "SQLite.Classic.MPU";
		/// <summary>
		/// SQLite classic provider wrapped into MiniProfiler with mappings to provider types configured.
		/// Used to test general compatibility of linq2db with wrapped providers.
		/// </summary>
		public const string SQLiteClassicMiniProfilerMapped   = "SQLite.Classic.MPM";


		/// <summary>
		/// Fake provider, which doesn't execute any real queries. Could be used for tests, that shouldn't be affected
		/// by real database access.
		/// </summary>
		public const string NoopProvider  = "TestNoopProvider";

		public const string AllMySql               = ProviderName.MySql + "," + ProviderName.MySqlConnector + ",MySql55,MariaDB";
		// MySql server providers (no mariaDB)
		public const string AllMySqlServer         = "MySql,MySqlConnector,MySql55";
		// MySql <5.7 has inadequate FTS behavior
		public const string AllMySqlFullText       = "MySql,MySqlConnector,MariaDB";
		public const string AllMySql57Plus         = "MySql,MySqlConnector,MariaDB";
		// MySql server providers (no mariaDB) without MySQL 5.5
		public const string AllMySqlServer57Plus   = "MySql,MySqlConnector";
		// MySql.Data server providers (no mysqlconnector)
		public const string AllMySqlData           = "MySql,MySql55,MariaDB";
		public const string AllMySqlWithCTE        = "MySql,MariaDB";
		public const string AllPostgreSQL          = "PostgreSQL,PostgreSQL.9.2,PostgreSQL.9.3,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.12,PostgreSQL.13";
		public const string AllPostgreSQLLess10    = "PostgreSQL.9.2,PostgreSQL.9.3,PostgreSQL.9.5";
		public const string AllPostgreSQL93Plus    = "PostgreSQL,PostgreSQL.9.3,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.12,PostgreSQL.13";
		public const string AllPostgreSQL95Plus    = "PostgreSQL,PostgreSQL.9.5,PostgreSQL.10,PostgreSQL.11,PostgreSQL.12,PostgreSQL.13";
		public const string AllPostgreSQL10Plus    = "PostgreSQL,PostgreSQL.10,PostgreSQL.11,PostgreSQL.12,PostgreSQL.13";
		public const string AllOracle              = "Oracle.Native,Oracle.Managed,Oracle.11.Native,Oracle.11.Managed";
		public const string AllOracleManaged       = "Oracle.Managed,Oracle.11.Managed";
		public const string AllOracleNative        = "Oracle.Native,Oracle.11.Native";
		public const string AllOracle11            = "Oracle.11.Native,Oracle.11.Managed";
		public const string AllOracle12            = "Oracle.Native,Oracle.Managed";
		public const string AllFirebird            = ProviderName.Firebird + "," + Firebird3 +"," + Firebird4;
		public const string AllSQLite              = "SQLite.Classic,SQLite.MS,SQLite.Classic.MPU,SQLite.Classic.MPM";
		public const string AllSQLiteClassic       = "SQLite.Classic,SQLite.Classic.MPU,SQLite.Classic.MPM";
		public const string AllSybase              = "Sybase,Sybase.Managed";
		public const string AllSqlServer           = "SqlServer.2000,SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSqlServer2005Minus  = "SqlServer.2000,SqlServer.2005";
		public const string AllSqlServer2008Minus  = "SqlServer.2000,SqlServer.2005,SqlServer.2008";
		public const string AllSqlServer2005Plus   = "SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSqlServer2008Plus   = "SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSqlServer2012Plus   = "SqlServer.2012,SqlServer.2014,SqlServer.2017,SqlAzure";
		public const string AllSqlServer2016Plus   = "SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSqlServer2017Plus   = "SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSQLiteNorthwind     = "Northwind.SQLite,Northwind.SQLite.MS";
		public const string AllSapHana             = "SapHana.Native,SapHana.Odbc";
		public const string AllInformix            = ProviderName.Informix + "," + ProviderName.InformixDB2;
		public const string AllAccess              = "Access,Access.Odbc";
	}
}
