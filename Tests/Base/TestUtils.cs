using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LinqToDB.Data;
using System.Threading;
using Tests.Model;
using LinqToDB.SchemaProvider;

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

		public const string NO_SCHEMA_NAME = "UNUSED_SCHEMA";
		public const string NO_DATABASE_NAME = "UNUSED_DB";

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

		[Sql.Expression("current_schema", ServerSideOnly = true, Configuration = ProviderName.SapHana)]
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
		[Sql.Expression("current_user"  , ServerSideOnly = true, Configuration = ProviderName.SapHana)]
		[Sql.Expression("current schema", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_schema"  , ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("USER_NAME"       , ServerSideOnly = true, Configuration = ProviderName.Sybase)]
		[Sql.Function("SCHEMA_NAME"     , ServerSideOnly = true)]
		private static string SchemaName()
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
				case ProviderName.SapHana:
				case ProviderName.Informix:
				case ProviderName.Oracle:
				case ProviderName.OracleNative:
				case ProviderName.OracleManaged:
				case ProviderName.PostgreSQL:
				case ProviderName.DB2:
				case ProviderName.Sybase:
				case ProviderName.SybaseManaged:
				case ProviderName.SqlServer2000:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
				case TestProvName.SqlAzure:
					return db.GetTable<LinqDataTypes>().Select(_ => SchemaName()).First();
			}

			return NO_SCHEMA_NAME;
		}

		public static GetSchemaOptions GetDefaultSchemaOptions(string context, GetSchemaOptions baseOptions = null)
		{
			if (context.Contains("SapHana"))
			{
				// SAP HANA provider throws C++ assertions when we try to load schema for some functions
				var options = baseOptions ?? new GetSchemaOptions();

				var oldLoad = options.LoadProcedure;
				if (oldLoad != null)
					options.LoadProcedure = p => oldLoad(p) && loadCheck(p);
				else
					options.LoadProcedure = loadCheck;

				bool loadCheck(ProcedureSchema p)
				{
					return p.ProcedureName != "SERIES_GENERATE_TIME"
						&& p.ProcedureName != "SERIES_DISAGGREGATE_TIME"
						// just too slow
						&& p.ProcedureName != "GET_FULL_SYSTEM_INFO_DUMP"
						&& p.ProcedureName != "GET_FULL_SYSTEM_INFO_DUMP_WITH_PARAMETERS"
						&& p.ProcedureName != "FULL_SYSTEM_INFO_DUMP_CREATE";
				}

				return options;
			}

			return baseOptions;
		}

		private static string GetContextName(IDataContext db)
		{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
			if (db is TestServiceModelDataContext linqDb)
				return linqDb.Configuration;
#endif

			if (db is TestDataConnection testDb)
				return testDb.ConfigurationString;

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
				case ProviderName.SQLiteMS:
					return "main";
				case ProviderName.Access:
					return "Database\\TestData";
				case ProviderName.SapHana:
				case ProviderName.MySql:
				case TestProvName.MariaDB:
				case TestProvName.MySql57:
				case ProviderName.PostgreSQL:
				case ProviderName.DB2:
				case ProviderName.Sybase:
				case ProviderName.SybaseManaged:
				case ProviderName.SqlServer2000:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
				case TestProvName.SqlAzure:
					return db.GetTable<LinqDataTypes>().Select(_ => DbName()).First();
				case ProviderName.Informix:
					return db.GetTable<LinqDataTypes>().Select(_ => DbInfo("dbname")).First();
			}

			return NO_DATABASE_NAME;
		}

		public static bool ProviderNeedsTimeFix(this IDataContext db, string context)
		{
			if (context == "MySql" || context == "MySql.LinqService")
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

			return false;
		}

		// see ProviderNeedsTimeFix
		public static DateTime FixTime(DateTime value, bool fix)
		{
			return fix ? value.AddMilliseconds(-value.Millisecond) : value;
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, string tableName = null)
		{
			try
			{
				return new TempTable<T>(db, tableName);
			}
			catch
			{
				db.DropTable<T>(tableName);
				return new TempTable<T>(db, tableName);
			}
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, string tableName, IEnumerable<T> items)
		{
			var table = CreateLocalTable<T>(db, tableName);

			if (db is DataConnection)
				using (new DisableLogging())
					table.Copy(items
						, new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows }
						);
			else
				using (new DisableLogging())
					foreach (var item in items)
						db.Insert(item, table.TableName);


			return table;
		}

		public static TempTable<T> CreateLocalTable<T>(this IDataContext db, IEnumerable<T> items)
		{
			return CreateLocalTable(db, null, items);
		}
	}
}
