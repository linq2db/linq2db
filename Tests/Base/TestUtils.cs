using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;

using NUnit.Framework;

using Tests.Model;

#if NETFRAMEWORK
using Tests.Model.Remote.Wcf;
#else
using Tests.Model.Remote.Grpc;
#endif

namespace Tests
{
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

		public const string NO_SCHEMA_NAME   = "UNUSED_SCHEMA";
		public const string NO_DATABASE_NAME = "UNUSED_DB";

		[Sql.Function("DBINFO", ServerSideOnly = true)]
		private static string DbInfo(string property)
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("current server", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_database", ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("DATABASE"        , ServerSideOnly = true, Configuration = ProviderName.MySql)]
		[Sql.Function("currentDatabase" , ServerSideOnly = true, Configuration = ProviderName.ClickHouse)]
		[Sql.Function("DB_NAME"         , ServerSideOnly = true)]
		private static string DbName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("user"                                    , ServerSideOnly = true, Configuration = ProviderName.Informix)]
		[Sql.Expression("sys_context('userenv', 'current_schema')", ServerSideOnly = true, Configuration = ProviderName.Oracle)]
		[Sql.Expression("current schema"                          , ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_schema"                            , ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("USER_NAME"                                 , ServerSideOnly = true, Configuration = ProviderName.Sybase)]
		[Sql.Expression("current_schema"                          , ServerSideOnly = true, Configuration = ProviderName.SapHana)]
		[Sql.Function("SCHEMA_NAME"                               , ServerSideOnly = true)]
		private static string SchemaName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("sys_context('userenv','service_name')", ServerSideOnly = true, Configuration = ProviderName.Oracle)]
		[Sql.Expression("DBSERVERNAME"                         , ServerSideOnly = true, Configuration = ProviderName.Informix)]
		[Sql.Expression("@@SERVERNAME"                         , ServerSideOnly = true)]
		private static string ServerName()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Returns schema name for provided connection.
		/// Returns UNUSED_SCHEMA if fully-qualified table name doesn't support database name.
		/// </summary>
		public static string GetSchemaName(IDataContext db, string context)
		{
			switch (context)
			{
				case string when context.IsAnyOf(TestProvName.AllInformix)  :
				case string when context.IsAnyOf(TestProvName.AllOracle)    :
				case string when context.IsAnyOf(TestProvName.AllPostgreSQL):
				case string when context.IsAnyOf(TestProvName.AllSybase)    :
				case string when context.IsAnyOf(TestProvName.AllSqlServer) :
				case string when context.IsAnyOf(TestProvName.AllSapHana)   :
				case string when context.IsAnyOf(ProviderName.DB2)          :
					return db.GetTable<LinqDataTypes>().Select(_ => SchemaName()).First();
			}

			return NO_SCHEMA_NAME;
		}

		/// <summary>
		/// Returns server name for provided connection.
		/// Returns UNUSED_SERVER if fully-qualified table name doesn't support server name.
		/// </summary>
		public static string GetServerName(IDataContext db, string context)
		{
			switch (context)
			{
				case string when context.IsAnyOf(TestProvName.AllSybase)   :
				case string when context.IsAnyOf(TestProvName.AllSqlServer):
				case string when context.IsAnyOf(TestProvName.AllOracle)   :
				case string when context.IsAnyOf(TestProvName.AllInformix) :
					return db.Select(() => ServerName());
				case string when context.IsAnyOf(TestProvName.AllSapHana)  :
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

		/// <summary>
		/// Returns database name for provided connection.
		/// Returns UNUSED_DB if fully-qualified table name doesn't support database name.
		/// </summary>
		public static string GetDatabaseName(IDataContext db, string context)
		{
			return context switch
			{
				string when context.IsAnyOf(TestProvName.AllSQLite)      => "main",
				// Access adds extension automatically to database name, but if there are
				// dots in name, extension not added as dot treated as extension separator by Access
				string when context.IsAnyOf(TestProvName.AllAccessOleDb) => "Database\\TestData",
				string when context.IsAnyOf(TestProvName.AllAccessOdbc)  => "Database\\TestData.ODBC.mdb",
				string when context.IsAnyOf(
					TestProvName.AllMySql,
					TestProvName.AllClickHouse,
					TestProvName.AllPostgreSQL,
					ProviderName.DB2,
					TestProvName.AllSybase,
					TestProvName.AllSqlServer)                        => db.GetTable<LinqDataTypes>().Select(_ => DbName()).First(),
				string when context.IsAnyOf(TestProvName.AllInformix) => db.GetTable<LinqDataTypes>().Select(_ => DbInfo("dbname")).First(),
				_                                                     => NO_DATABASE_NAME
			};
		}

		public static bool ProviderNeedsTimeFix(this IDataContext db, string context)
		{
			if (context.IsAnyOf(TestProvName.AllAccessOdbc))
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

		sealed class FirebirdTempTable<T> : TestTempTable<T>
			where T : notnull
		{
			public FirebirdTempTable(IDataContext db, string? tableName = null, string? databaseName = null, string? schemaName = null, TableOptions tableOptions = TableOptions.NotSet)
				: base(db, tableName, databaseName, schemaName, tableOptions : tableOptions)
			{
			}

			public override void Dispose()
			{
				if (DataContext is DataConnection dc && dc.DataProvider.Name.Contains(ProviderName.Firebird))
				{
					FirebirdTools.ClearAllPools();
				}

				DataContext.Close();
				FirebirdTools.ClearAllPools();
				base.Dispose();
			}

			public override ValueTask DisposeAsync()
			{
				if (DataContext is DataConnection dc && dc.DataProvider.Name.Contains(ProviderName.Firebird))
				{
					FirebirdTools.ClearAllPools();
				}

				DataContext.CloseAsync();
				FirebirdTools.ClearAllPools();

				return base.DisposeAsync();
			}
		}
		class TestTempTable<T> : TempTable<T>
			where T : notnull
		{
			public TestTempTable(IDataContext db, string? tableName = null, string? databaseName = null, string? schemaName = null, TableOptions tableOptions = TableOptions.NotSet)
				: base(db, tableName, databaseName, schemaName, tableOptions: tableOptions)
			{
			}

			public override void Dispose()
			{
				using var _ = new DisableBaseline("Test setup");
				base.Dispose();
			}

			public override ValueTask DisposeAsync()
			{
				using var _ = new DisableBaseline("Test setup");
				return base.DisposeAsync();
			}
		}

		static TempTable<T> CreateTable<T>(IDataContext db, string? tableName, TableOptions tableOptions = TableOptions.NotSet)
			where T : notnull =>
			db.CreateSqlBuilder() is FirebirdSqlBuilder ?
				new FirebirdTempTable<T>(db, tableName, tableOptions : tableOptions) :
				new     TestTempTable<T>(db, tableName, tableOptions : tableOptions);

		static void ClearDataContext(IDataContext db)
		{
			if (db.CreateSqlBuilder() is FirebirdSqlBuilder)
			{
				db.Close();
				FirebirdTools.ClearAllPools();
			}
		}

		public static Version GetSqliteVersion(DataConnection db)
		{
			var version = db.Execute<string>("select sqlite_version();");
			return new Version(version);
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, string? tableName = null, TableOptions tableOptions = TableOptions.CheckExistence)
			where T : notnull
		{
			using var _ = new DisableBaseline("Test setup");
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
			where T : notnull
		{
			using var _ = new DisableBaseline("Test setup");
			using (new DisableLogging())
			{
				var table = CreateLocalTable<T>(db, tableName, TableOptions.CheckExistence);

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

				return table;
			}
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, IEnumerable<T> items, bool insertInTransaction = false)
			where T : notnull
		{
			using var _ = new DisableBaseline("Test setup");
			return CreateLocalTable(db, null, items, insertInTransaction);
		}

		public static string GetValidCollationName(string providerName)
		{
			return providerName switch
			{
				string when providerName.IsAnyOf(TestProvName.AllOracle12Plus) => "latin_AI",
				string when providerName.IsAnyOf(ProviderName.DB2)             => "SYSTEM_923_DE",
				string when providerName.IsAnyOf(TestProvName.AllPostgreSQL)   => "POSIX",
				string when providerName.IsAnyOf(TestProvName.AllSQLite)       => "NOCASE",
				string when providerName.IsAnyOf(TestProvName.AllFirebird)     => "UNICODE_FSS",
				string when providerName.IsAnyOf(TestProvName.AllMySql)        => "utf8_bin",
				string when providerName.IsAnyOf(TestProvName.AllSqlServer)    => "Albanian_CI_AS",
				_                                                              => "what-ever"
			};
		}

		[return: NotNullIfNotNull(nameof(s))]
		public static string? Clean(this string? s)
		{
			return s?
				.Replace(" ", "")
				.Replace("\t", "")
				.Replace("\r", "")
				.Replace("\n", "")
				;
		}

		internal static string GetConfigName()
		{
#if NETFRAMEWORK
			return "NETFX";
#elif NET8_0
			return "NET80";
#elif NET9_0
			return "NET90";
#else
#error Unknown framework
#endif
		}

		public static void Log(Exception ex)
		{
			Log($"Exception: {ex.Message}");
			Log(ex.StackTrace);
		}

		public static void Log(string? message)
		{
			var path = Path.GetTempPath();
			File.AppendAllText(path + "linq2db.Tests." + GetConfigName() + ".log", (message ?? "") + Environment.NewLine);
		}

		public static string GetTableName(string context, [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null)
		{
			var tableName  = "xxPerson";

			if (context.IsAnyOf(TestProvName.AllFirebird))
			{
				tableName += "_f";

				if (context.IsRemote())
					tableName += "l";

				tableName += "_" + methodName;
			}

			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				tableName += "_o";

				if (context.IsRemote())
					tableName += "l";
			}

			return tableName;
		}

		public static void DeleteTestCases()
		{
			var fileName = GetTestFilePath();

			if (File.Exists(fileName))
				File.Delete(fileName);
		}

		public static string? GetLastTestCase()
		{
			var fileName = GetTestFilePath();

			TestContext.Out.WriteLine($"Test expression file: {fileName} ({File.Exists(fileName)})");

			if (File.Exists(fileName))
				return File.ReadAllText(fileName);

			return null;
		}

		// sync logic with ExpressionTestGenerator.GetTestFilePath
		static string GetTestFilePath()
		{
			var dir = Path.Combine(Path.GetTempPath(), "linq2db");

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			return Path.Combine(dir, FormattableString.Invariant($"ExpressionTest.0.cs"));
		}
	}
}
