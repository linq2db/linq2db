using LinqToDB;

namespace Tests
{
	public static class TestProvName
	{
		public const string SqlAzure          = "SqlAzure";

		#region SQLite
		public const string Default                           = "SQLite.Default";
		public const string NorthwindSQLite                   = "Northwind.SQLite";
		public const string NorthwindSQLiteMS                 = "Northwind.SQLite.MS";
		public const string AllSQLiteNorthwind                = $"{NorthwindSQLite},{NorthwindSQLiteMS}";
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
		public const string AllSQLiteBase                     = $"{ProviderName.SQLiteClassic},{ProviderName.SQLiteMS}";
		public const string AllSQLiteMP                       = "SQLite.Classic.MPU,SQLite.Classic.MPM";
		public const string AllSQLite                         = $"{AllSQLiteBase},{AllSQLiteMP}";
		public const string AllSQLiteClassic                  = $"{ProviderName.SQLiteClassic},{AllSQLiteMP}";
		#endregion

		#region MySQL/MariaDB
		/// <summary>
		/// MariaDB over MySql.Data.
		/// </summary>
		public const string MariaDB           = "MariaDB";
		/// <summary>
		/// MariaDB over MySqlConnector.
		/// </summary>
		public const string MariaDBConnector  = "MariaDBMariaDBConnector";
		/// <summary>
		/// MySQL 5.5 over MySql.Data.
		/// </summary>
		public const string MySql55           = "MySql55";
		/// <summary>
		/// MySQL 5.5 over MySqlConnector.
		/// </summary>
		public const string MySql55Connector  = "MySql55Connector";

		/// <summary>
		/// All MySql.Data providers.
		/// </summary>
		public const string AllMySqlData      = $"{MySql55},{ProviderName.MySql},{MariaDB}";
		/// <summary>
		/// All MySqlConnector providers.
		/// </summary>
		public const string AllMySqlConnector = $"{MySql55Connector},{ProviderName.MySqlConnector},{MariaDBConnector}";
		/// <summary>
		/// All mysql/mariadb test providers.
		/// </summary>
		public const string AllMySql          = $"{AllMySqlData},{AllMySqlConnector}";
		/// <summary>
		/// All mysql test providers (no mariadb).
		/// </summary>
		public const string AllMySqlServer    = $"{MySql55},{MySql55Connector},{ProviderName.MySql},{ProviderName.MySqlConnector}";
		/// <summary>
		/// All mariadb test providers.
		/// </summary>
		public const string AllMariaDB        = $"{MariaDB},{MariaDBConnector}";
		/// <summary>
		/// MySQL 5.5.
		/// </summary>
		public const string AllMySql55        = $"{MySql55},{MySql55Connector}";
		/// <summary>
		/// MySQL > 5.7 (8.0 currently) and MariaDB.
		/// </summary>
		public const string AllMySql57Plus    = $"{ProviderName.MySql},{ProviderName.MySqlConnector},{MariaDB},{MariaDBConnector}";
		/// <summary>
		/// MySQL/MariaDB with supported FTS support.
		/// MySql less than 5.7 excluded due to inadequate FTS behavior.
		/// </summary>
		public const string AllMySqlFullText  = AllMySql57Plus;
		/// <summary>
		/// MySQL > 5.7 (8.0 currently). No MariaDB.
		/// </summary>
		public const string AllMySqlServer57Plus = $"{ProviderName.MySql},{ProviderName.MySqlConnector}";
		/// <summary>
		/// MySQL/MariaDB with CTE support.
		/// </summary>
		public const string AllMySqlWithCTE   = AllMySql57Plus;
		#endregion

		#region PostgreSQL
		public const string PostgreSQL10      = "PostgreSQL.10";
		public const string PostgreSQL11      = "PostgreSQL.11";
		public const string PostgreSQL12      = "PostgreSQL.12";
		public const string PostgreSQL13      = "PostgreSQL.13";
		public const string PostgreSQL14      = "PostgreSQL.14";

		public const string AllPostgreSQL9      = $"{ProviderName.PostgreSQL92},{ProviderName.PostgreSQL93},{ProviderName.PostgreSQL95}";
		public const string AllPostgreSQL10Plus = $"{PostgreSQL10},{PostgreSQL11},{PostgreSQL12},{PostgreSQL13},{PostgreSQL14}";
		public const string AllPostgreSQL95Plus = $"{ProviderName.PostgreSQL95},{AllPostgreSQL10Plus}";
		public const string AllPostgreSQL93Plus = $"{ProviderName.PostgreSQL93},{AllPostgreSQL95Plus}";
		public const string AllPostgreSQL       = $"{AllPostgreSQL9},{AllPostgreSQL10Plus}";
		#endregion

		#region Firebird
		public const string Firebird3        = "Firebird3";
		public const string Firebird4        = "Firebird4";
		public const string AllFirebirdLess4 = $"{ProviderName.Firebird},{Firebird3}";
		public const string AllFirebird      = $"{AllFirebirdLess4},{Firebird4}";
		#endregion

		#region Sybase
		public const string AllSybase = $"{ProviderName.Sybase},{ProviderName.SybaseManaged}";
		#endregion

		public const string Northwind         = "Northwind";
		public const string Oracle11Native    = "Oracle.11.Native";
		public const string Oracle11Managed   = "Oracle.11.Managed";
		public const string SqlServer2019     = "SqlServer.2019";

		public const string SqlServer2019SequentialAccess       = "SqlServer.2019.SA";
		public const string SqlServer2019FastExpressionCompiler = "SqlServer.2019.FEC";
		public const string SqlServerContained                  = "SqlServer.Contained";


		/// <summary>
		/// Fake provider, which doesn't execute any real queries. Could be used for tests, that shouldn't be affected
		/// by real database access.
		/// </summary>
		public const string NoopProvider  = "TestNoopProvider";

		public const string AllOracle              = "Oracle.Native,Oracle.Managed,Oracle.11.Native,Oracle.11.Managed";
		public const string AllOracleManaged       = "Oracle.Managed,Oracle.11.Managed";
		public const string AllOracleNative        = "Oracle.Native,Oracle.11.Native";
		public const string AllOracle11            = "Oracle.11.Native,Oracle.11.Managed";
		public const string AllOracle12            = "Oracle.Native,Oracle.Managed";
		public const string AllSqlServer           = "SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2005Minus  = "SqlServer.2005";
		public const string AllSqlServer2008Minus  = "SqlServer.2005,SqlServer.2008";
		public const string AllSqlServer2005Plus   = "SqlServer.2005,SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2008Plus   = "SqlServer.2008,SqlServer.2012,SqlServer.2014,SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2012Plus   = "SqlServer.2012,SqlServer.2014,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2016Plus   = "SqlServer.2016,SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2017Plus   = "SqlServer.2017,SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlServer.Contained,SqlAzure";
		public const string AllSqlServer2019Plus   = "SqlServer.2019,SqlServer.2019.SA,SqlServer.2019.FEC,SqlAzure";
		public const string AllSapHana             = "SapHana.Native,SapHana.Odbc";
		public const string AllInformix            = ProviderName.Informix + "," + ProviderName.InformixDB2;
		public const string AllAccess              = "Access,Access.Odbc";
	}
}
