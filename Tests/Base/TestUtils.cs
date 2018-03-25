﻿using LinqToDB;
using System;
using System.Linq;
using Tests.Model;

namespace Tests
{
	public class TestUtils
	{
		public const string NO_SCHEMA_NAME = "UNUSED_SCHEMA";
		public const string NO_DATABASE_NAME = "UNUSED_DB";

		[Sql.Function("DBINFO", ServerSideOnly = true)]
		private static string DbInfo(string property)
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("current_schema", ServerSideOnly = true, Configuration = ProviderName.SapHana)]
		[Sql.Expression("current server", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_database", ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("DATABASE", ServerSideOnly = true, Configuration = ProviderName.MySql)]
		[Sql.Function("DB_NAME", ServerSideOnly = true)]
		private static string DbName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("user", ServerSideOnly = true, Configuration = ProviderName.Informix)]
		[Sql.Expression("user", ServerSideOnly = true, Configuration = ProviderName.OracleNative)]
		[Sql.Expression("user", ServerSideOnly = true, Configuration = ProviderName.OracleManaged)]
		[Sql.Expression("current_user", ServerSideOnly = true, Configuration = ProviderName.SapHana)]
		[Sql.Expression("current schema", ServerSideOnly = true, Configuration = ProviderName.DB2)]
		[Sql.Function("current_schema", ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("USER_NAME", ServerSideOnly = true, Configuration = ProviderName.Sybase)]
		[Sql.Function("SCHEMA_NAME", ServerSideOnly = true)]
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
	}
}
