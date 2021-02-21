using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using FirebirdSql.Data.FirebirdClient;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;

namespace Tests
{
	using Model;

	public static class TestUtils
	{
		private static int _cnt;

		/// <summary>
		/// Returns unique per-testrun sequence number.
		/// E.g. it can be used to generate unique table names for tests to workaround Firebird's
		/// issues with DDL operations.
		/// </summary>
		public static int GetNext()
		{
			// Firebird issue details:
			// https://stackoverflow.com/questions/44353607
			// another solution with pools cleanup doesn't work well with Firebird3 and
			// also breaks provider
			return Interlocked.Increment(ref _cnt);
		}

		public const string NO_SCHEMA_NAME = "UNUSED_SCHEMA";
		public const string NO_DATABASE_NAME = "UNUSED_DB";
		public const string NO_SERVER_NAME   = "UNUSED_SERVER";

		[Sql.Function("VERSION", ServerSideOnly = true)]
		private static string MySqlVersion()
		{
			throw new InvalidOperationException();
		}

		[Sql.Function("DBINFO", ServerSideOnly = true)]
		private static string DbInfo(string property)
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("current server", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_database", ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("DATABASE"        , ServerSideOnly = true, Configuration = ProviderName.MySql)]
		[Sql.Function("DB_NAME"         , ServerSideOnly = true)]
		private static string DbName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.Informix)]
		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.OracleNative)]
		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.OracleManaged)]
		[Sql.Expression("current schema", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_schema"  , ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("USER_NAME"       , ServerSideOnly = true, Configuration = ProviderName.Sybase)]
		[Sql.Expression("current_schema", ServerSideOnly = true, Configuration = ProviderName.SapHana)]
		[Sql.Function("SCHEMA_NAME"     , ServerSideOnly = true)]
		private static string SchemaName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("sys_context('userenv','service_name')", ServerSideOnly = true, Configuration = ProviderName.OracleNative)]
		[Sql.Expression("sys_context('userenv','service_name')", ServerSideOnly = true, Configuration = ProviderName.OracleManaged)]
		[Sql.Expression("DBSERVERNAME", ServerSideOnly = true, Configuration = ProviderName.Informix)]
		[Sql.Expression("@@SERVERNAME", ServerSideOnly = true)]
		private static string ServerName()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Returns schema name for provided connection.
		/// Returns UNUSED_SCHEMA if fully-qualified table name doesn't support database name.
		/// </summary>
		public static string GetSchemaName(IDataContext db)
		{
			switch (GetContextName(db))
			{
				case ProviderName.Informix:
				case ProviderName.InformixDB2:
				case ProviderName.Oracle:
				case ProviderName.OracleNative:
				case ProviderName.OracleManaged:
				case TestProvName.Oracle11Native:
				case TestProvName.Oracle11Managed:
				case ProviderName.PostgreSQL:
				case ProviderName.PostgreSQL92:
				case ProviderName.PostgreSQL93:
				case ProviderName.PostgreSQL95:
				case TestProvName.PostgreSQL10:
				case TestProvName.PostgreSQL11:
				case TestProvName.PostgreSQL12:
				case TestProvName.PostgreSQL13:
				case ProviderName.DB2:
				case ProviderName.Sybase:
				case ProviderName.SybaseManaged:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
				case TestProvName.SqlServer2016:
				case ProviderName.SqlServer2017:
				case TestProvName.SqlServer2019:
				case TestProvName.SqlServer2019SequentialAccess:
				case TestProvName.SqlServer2019FastExpressionCompiler:
				case TestProvName.SqlAzure:
				case ProviderName.SapHanaNative:
				case ProviderName.SapHanaOdbc:
					return db.GetTable<LinqDataTypes>().Select(_ => SchemaName()).First();
				case ProviderName.SqlServer2000:
					return db.FromSql<string>($"SELECT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {nameof(LinqDataTypes)}").First();
			}

			return NO_SCHEMA_NAME;
		}

		/// <summary>
		/// Returns server name for provided connection.
		/// Returns UNUSED_SERVER if fully-qualified table name doesn't support server name.
		/// </summary>
		public static string GetServerName(IDataContext db)
		{
			switch (GetContextName(db))
			{
				case ProviderName.SybaseManaged:
				case ProviderName.SqlServer2000:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
				case TestProvName.SqlServer2016:
				case ProviderName.SqlServer2017:
				case TestProvName.SqlServer2019:
				case TestProvName.SqlServer2019SequentialAccess:
				case TestProvName.SqlServer2019FastExpressionCompiler:
				case TestProvName.SqlAzure:
				case ProviderName.OracleManaged:
				case ProviderName.OracleNative:
				case TestProvName.Oracle11Native:
				case TestProvName.Oracle11Managed:
				case ProviderName.Informix:
				case ProviderName.InformixDB2:
					return db.Select(() => ServerName());
				case ProviderName.SapHanaNative:
				case ProviderName.SapHanaOdbc:
					/* SAP HANA should be configured for linked server queries
					 This will help to configure (especially second link):
					 https://www.linkedin.com/pulse/cross-database-queries-thing-past-how-use-sap-hana-your-nandan
					 https://blogs.sap.com/2017/04/12/introduction-to-the-sap-hana-smart-data-access-linked-database-feature/
					 https://blogs.sap.com/2014/12/19/step-by-step-tutorial-cross-database-queries-in-sap-hana-sps09/
					 SAMPLE CONFIGURATION SCRIPT:

			CREATE REMOTE SOURCE "LINKED_DB" ADAPTER "hanaodbc" CONFIGURATION 'DRIVER=libodbcHDB.so;ServerNode=192.168.56.101:39013;';

			// optional step
			GRANT LINKED DATABASE ON REMOTE SOURCE LINKED_DB TO SYSTEM;

			CREATE CREDENTIAL FOR USER SYSTEM COMPONENT 'SAPHANAFEDERATION' PURPOSE 'LINKED_DB' TYPE 'PASSWORD' USING 'user=SYSTEM;password=E15342GcbaFd';
					 */
					return "LINKED_DB";
			}

			return NO_SCHEMA_NAME;
		}

		private static string GetContextName(IDataContext db)
		{
#if NET472
			if (db is TestServiceModelDataContext linqDb)
				return linqDb.Configuration!;
#endif

			if (db is TestDataConnection testDb)
				return testDb.ConfigurationString!;

			return db.ContextID;
		}

		/// <summary>
		/// Returns database name for provided connection.
		/// Returns UNUSED_DB if fully-qualified table name doesn't support database name.
		/// </summary>
		public static string GetDatabaseName(IDataContext db)
		{
			switch (GetContextName(db))
			{
				case ProviderName.SQLiteClassic:
				case TestProvName.SQLiteClassicMiniProfilerMapped:
				case TestProvName.SQLiteClassicMiniProfilerUnmapped:
				case ProviderName.SQLiteMS:
					return "main";
				case ProviderName.Access:
				case ProviderName.AccessOdbc:
					return "Database\\TestData";
				case ProviderName.MySql:
				case ProviderName.MySqlConnector:
				case TestProvName.MariaDB:
				case TestProvName.MySql55:
				case ProviderName.PostgreSQL:
				case ProviderName.PostgreSQL92:
				case ProviderName.PostgreSQL93:
				case ProviderName.PostgreSQL95:
				case TestProvName.PostgreSQL10:
				case TestProvName.PostgreSQL11:
				case TestProvName.PostgreSQL12:
				case TestProvName.PostgreSQL13:
				case ProviderName.DB2:
				case ProviderName.Sybase:
				case ProviderName.SybaseManaged:
				case ProviderName.SqlServer2000:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
				case TestProvName.SqlServer2016:
				case ProviderName.SqlServer2017:
				case TestProvName.SqlServer2019:
				case TestProvName.SqlServer2019SequentialAccess:
				case TestProvName.SqlServer2019FastExpressionCompiler:
				case TestProvName.SqlAzure:
					return db.GetTable<LinqDataTypes>().Select(_ => DbName()).First();
				case ProviderName.Informix:
				case ProviderName.InformixDB2:
					return db.GetTable<LinqDataTypes>().Select(_ => DbInfo("dbname")).First();
			}

			return NO_DATABASE_NAME;
		}

		public static bool ProviderNeedsTimeFix(this IDataContext db, string context)
		{
			if (context.Replace(".LinqService", "") == TestProvName.MySql55)
			{
				// MySql versions prior to 5.6.4 do not store fractional seconds so we need to trim
				// them from expected data too
				var version = db.GetTable<LinqDataTypes>().Select(_ => MySqlVersion()).First();
				var match = new Regex(@"^\d+\.\d+.\d+").Match(version);
				if (match.Success)
				{
					var versionParts = match.Value.Split('.').Select(_ => int.Parse(_)).ToArray();

					return (versionParts[0] * 10000 + versionParts[1] * 100 + versionParts[2] < 50604);
				}
			}
			else if (context.Replace(".LinqService", "") == ProviderName.AccessOdbc)
			{
				// ODBC driver strips milliseconds from values on both save and load
				return true;
			}

			return false;
		}

		// see ProviderNeedsTimeFix
		public static DateTime StripMilliseconds(DateTime value, bool fix)
		{
			return fix ? new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second) : value;
		}

		class FirebirdTempTable<T> : TempTable<T>
		{
			public FirebirdTempTable(IDataContext db, string? tableName = null, string? databaseName = null, string? schemaName = null, TableOptions tableOptions = TableOptions.NotSet)
				: base(db, tableName, databaseName, schemaName, tableOptions : tableOptions)
			{
			}

			public override void Dispose()
			{
				if (DataContext is DataConnection dc && dc.Connection is FbConnection fbc )
				{
					FbConnection.ClearPool(fbc);
				}

				DataContext.Close();
				FirebirdTools.ClearAllPools();
				base.Dispose();
			}
		}

		static TempTable<T> CreateTable<T>(IDataContext db, string? tableName, TableOptions tableOptions = TableOptions.NotSet) =>
			db.CreateSqlProvider() is FirebirdSqlBuilder ?
				new FirebirdTempTable<T>(db, tableName, tableOptions : tableOptions) :
				new         TempTable<T>(db, tableName, tableOptions : tableOptions);

		static void ClearDataContext(IDataContext db)
		{
			if (db.CreateSqlProvider() is FirebirdSqlBuilder)
			{
				db.Close();
				FirebirdTools.ClearAllPools();
			}
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, string? tableName = null, TableOptions tableOptions = TableOptions.NotSet)
		{
			try
			{
				if ((tableOptions & TableOptions.CheckExistence) == TableOptions.CheckExistence)
					db.DropTable<T>(tableName, tableOptions:tableOptions);
				return CreateTable<T>(db, tableName, tableOptions);
			}
			catch
			{
				ClearDataContext(db);
				db.DropTable<T>(tableName, throwExceptionIfNotExists:false);
				return CreateTable<T>(db, tableName, tableOptions);
			}
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, string? tableName, IEnumerable<T> items, bool insertInTransaction = false)
		{
			var table = CreateLocalTable<T>(db, tableName, TableOptions.CheckExistence);

			using (new DisableLogging())
			{
				if (db is DataConnection dc)
				{
					// apply transaction only on insert, as not all dbs support DDL within transaction
					if (insertInTransaction)
						dc.BeginTransaction();

					table.Copy(items, new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows });

					if (insertInTransaction)
						dc.CommitTransaction();
				}
				else
					foreach (var item in items)
						db.Insert(item, table.TableName);
			}

			return table;
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, IEnumerable<T> items, bool insertInTransaction = false)
		{
			return CreateLocalTable(db, null, items, insertInTransaction);
		}

		public static string GetValidCollationName(string providerName)
		{
			switch (providerName)
			{
				case ProviderName.OracleNative                       :
				case ProviderName.OracleManaged                      :
					return "GENERAL_M";
				case ProviderName.DB2                                :
					return "SYSTEM_923_DE";
				case ProviderName.PostgreSQL                         :
				case ProviderName.PostgreSQL92                       :
				case ProviderName.PostgreSQL93                       :
				case ProviderName.PostgreSQL95                       :
				case TestProvName.PostgreSQL10                       :
				case TestProvName.PostgreSQL11                       :
				case TestProvName.PostgreSQL12                       :
				case TestProvName.PostgreSQL13                       :
					return "POSIX";
				case ProviderName.SQLiteClassic                      :
				case ProviderName.SQLiteMS                           :
				case TestProvName.SQLiteClassicMiniProfilerMapped    :
				case TestProvName.SQLiteClassicMiniProfilerUnmapped  :
					return "NOCASE";
				case ProviderName.Firebird                           :
				case TestProvName.Firebird3                          :
					return "UNICODE_FSS";
				case ProviderName.MySql                              :
				case ProviderName.MySqlConnector                     :
				case TestProvName.MySql55                            :
				case TestProvName.MariaDB                            :
					return "utf8_bin";
				case TestProvName.SqlAzure                           :
				case ProviderName.SqlServer2000                      :
				case ProviderName.SqlServer2005                      :
				case ProviderName.SqlServer2008                      :
				case ProviderName.SqlServer2012                      :
				case ProviderName.SqlServer2014                      :
				case TestProvName.SqlServer2016                      :
				case ProviderName.SqlServer2017                      :
				case TestProvName.SqlServer2019                      :
				case TestProvName.SqlServer2019SequentialAccess      :
				case TestProvName.SqlServer2019FastExpressionCompiler:
					return "Albanian_CI_AS";
				default                                              :
					return "whatever";
			}
		}
	}
}
