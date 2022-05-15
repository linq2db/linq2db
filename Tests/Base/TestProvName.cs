using LinqToDB;

namespace Tests
{
	public static class TestProvName
	{
		#region SQLite
		public const string Default           = "SQLite.Default";
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
		public const string MariaDBConnector  = "MariaDBConnector";
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
		public const string AllFirebird3Plus = $"{Firebird3},{Firebird4}";
		public const string AllFirebirdLess4 = $"{ProviderName.Firebird},{Firebird3}";
		public const string AllFirebird      = $"{AllFirebirdLess4},{Firebird4}";
		#endregion

		#region Sybase
		public const string AllSybase = $"{ProviderName.Sybase},{ProviderName.SybaseManaged}";
		#endregion

		#region SQL Server
		public const string Northwind                    = "SqlServer.Northwind";
		public const string NorthwindMS                  = "SqlServer.Northwind.MS";
		public const string AllNorthwind                 = $"{Northwind},{NorthwindMS}";

		public const string SqlAzure                     = "SqlServer.Azure";
		public const string SqlAzureMS                   = "SqlServer.Azure.MS";
		public const string AllSqlAzure                  = $"{SqlAzure},{SqlAzureMS}";

		public const string SqlServer2005MS              = "SqlServer.2005.MS";
		public const string SqlServer2008MS              = "SqlServer.2008.MS";
		public const string SqlServer2012MS              = "SqlServer.2012.MS";
		public const string SqlServer2014MS              = "SqlServer.2014.MS";
		public const string SqlServer2016MS              = "SqlServer.2016.MS";
		public const string SqlServer2017MS              = "SqlServer.2017.MS";
		public const string SqlServer2019MS              = "SqlServer.2019.MS";

		public const string SqlServerSequentialAccess    = "SqlServer.SA";
		public const string SqlServerSequentialAccessMS  = "SqlServer.SA.MS";
		public const string AllSqlServerSequentialAccess = $"{SqlServerSequentialAccess},{SqlServerSequentialAccessMS}";

		public const string SqlServerContained                  = "SqlServer.Contained";
		public const string SqlServerContainedMS         = "SqlServer.Contained.MS";
		public const string AllSqlServerContained        = $"{SqlServerContained},{SqlServerContainedMS}";

		/// <summary>
		/// Case-sensitive database.
		/// </summary>
		public const string AllSqlServerCS               = $"{ProviderName.SqlServer2019},{SqlServer2019MS}";
		public const string AllSqlServer2005             = $"{ProviderName.SqlServer2005},{SqlServer2005MS}";
		public const string AllSqlServer2008             = $"{ProviderName.SqlServer2008},{SqlServer2008MS}";
		public const string AllSqlServer2012             = $"{ProviderName.SqlServer2012},{SqlServer2012MS}";
		public const string AllSqlServer2014             = $"{ProviderName.SqlServer2014},{SqlServer2014MS}";
		public const string AllSqlServer2016             = $"{ProviderName.SqlServer2016},{SqlServer2016MS}";
		public const string AllSqlServer2017             = $"{ProviderName.SqlServer2017},{SqlServer2017MS}";
		public const string AllSqlServer2019             = $"{ProviderName.SqlServer2019},{SqlServer2019MS},{AllSqlServerSequentialAccess},{AllSqlServerContained}";
		public const string AllSqlServer2008Minus        = $"{AllSqlServer2005},{AllSqlServer2008}";
		public const string AllSqlServer2019Plus         = $"{AllSqlServer2019},{AllSqlAzure}";
		public const string AllSqlServer2017Plus         = $"{AllSqlServer2017},{AllSqlServer2019Plus}";
		public const string AllSqlServer2016Plus         = $"{AllSqlServer2016},{AllSqlServer2017Plus}";
		public const string AllSqlServer2012PlusNoAzure  = $"{AllSqlServer2012},{AllSqlServer2014},{AllSqlServer2016},{AllSqlServer2017},{AllSqlServer2019}";
		public const string AllSqlServer2012Plus         = $"{AllSqlServer2012},{AllSqlServer2014},{AllSqlServer2016Plus}";
		public const string AllSqlServer2008Plus         = $"{AllSqlServer2008},{AllSqlServer2012Plus}";
		public const string AllSqlServerNoAzure          = $"{AllSqlServer2005},{AllSqlServer2008},{AllSqlServer2012},{AllSqlServer2014},{AllSqlServer2016},{AllSqlServer2017},{AllSqlServer2019}";
		public const string AllSqlServer                 = $"{AllSqlServerNoAzure},{AllSqlAzure}";
		#endregion

		#region Access
		public const string AllAccess              = "Access,Access.Odbc";
		#endregion

		#region Oracle
		public const string Oracle11Native   = "Oracle.11.Native";
		public const string Oracle11Managed  = "Oracle.11.Managed";
		public const string AllOracleManaged = $"{Oracle11Managed},{ProviderName.OracleManaged}";
		public const string AllOracleNative  = $"{Oracle11Native},{ProviderName.OracleNative}";
		public const string AllOracle11      = $"{Oracle11Native},{Oracle11Managed}";
		public const string AllOracle12      = $"{ProviderName.OracleNative},{ProviderName.OracleManaged}";
		public const string AllOracle        = $"{AllOracle11},{AllOracle12}";
		#endregion

		/// <summary>
		/// Fake provider, which doesn't execute any real queries. Could be used for tests, that shouldn't be affected
		/// by real database access.
		/// </summary>
		public const string NoopProvider  = "TestNoopProvider";

		public const string AllSapHana  = $"{ProviderName.SapHanaNative},{ProviderName.SapHanaOdbc}";
		public const string AllInformix = $"{ProviderName.Informix},{ProviderName.InformixDB2}";
	}
}
