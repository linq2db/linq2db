using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;
#if !NETFX_CORE
using System.ServiceModel;
#endif

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[DataContextSource]
		public void TestTableName(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<TestTable>().ToList();
			}
		}

		[DataContextSource]
		public void TestTableNameWithSchema(string context)
		{
			using (var db = GetDataContext(context))
			{
				var ctx = context;
				if (ctx.EndsWith(".LinqService"))
					ctx = ctx.Substring(0, ctx.Length - ".LinqService".Length);

				string schemaName;

				using (new DisableLogging())
					schemaName = GetSchemaName(ctx, db);

				db.GetTable<TestTable>().SchemaName(schemaName).ToList();
			}
		}

		[DataContextSource]
		public void TestTableNameWithDatabase(string context)
		{
			using (var db = GetDataContext(context))
			{
				var ctx = context;
				if (ctx.EndsWith(".LinqService"))
					ctx = ctx.Substring(0, ctx.Length - ".LinqService".Length);

				string dbName;

				using (new DisableLogging())
					dbName = GetDatabaseName(ctx, db);

				if (   context == ProviderName.SapHana
					|| context == ProviderName.DB2)
					Assert.Throws<LinqToDBException>(() => db.GetTable<TestTable>().DatabaseName(dbName).ToList());
#if !NETFX_CORE
				else if (context == ProviderName.SapHana + ".LinqService"
					||   context == ProviderName.DB2     + ".LinqService")
					Assert.Throws<FaultException<ExceptionDetail>>(() => db.GetTable<TestTable>().DatabaseName(dbName).ToList());
#endif
				else
					db.GetTable<TestTable>().DatabaseName(dbName).ToList();
			}
		}

		// SAP HANA should be configured for such queries (I failed)
		// Maybe this will help:
		// https://www.linkedin.com/pulse/cross-database-queries-thing-past-how-use-sap-hana-your-nandan
		// https://blogs.sap.com/2017/04/12/introduction-to-the-sap-hana-smart-data-access-linked-database-feature/
		// https://blogs.sap.com/2014/12/19/step-by-step-tutorial-cross-database-queries-in-sap-hana-sps09/
		[DataContextSource(ProviderName.SapHana)]
		public void TestTableNameWithDatabaseAndSchema(string context)
		{
			using (var db = GetDataContext(context))
			{
				var ctx = context;
				if (ctx.EndsWith(".LinqService"))
					ctx = ctx.Substring(0, ctx.Length - ".LinqService".Length);

				string schemaName;
				string dbName;

				using (new DisableLogging())
				{
					schemaName = GetSchemaName(ctx, db);
					dbName = GetDatabaseName(ctx, db);
				}

				db.GetTable<TestTable>().SchemaName(schemaName).DatabaseName(dbName).ToList();
			}
		}

		private static string GetSchemaName(string context, ITestDataContext db)
		{
			switch (context)
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
					return db.Types.Select(_ => SchemaName()).First();
			}

			return "UNUSED_SCHEMA";
		}

		private static string GetDatabaseName(string context, ITestDataContext db)
		{
			switch (context)
			{
				case ProviderName.SQLite:
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
					return db.Types.Select(_ => DbName()).First();
				case ProviderName.Informix:
					return db.Types.Select(_ => DbInfo("dbname")).First();
			}

			return "UNUSED_DB";
		}

		[Sql.Function("DBINFO", ServerSideOnly = true)]
		static string DbInfo(string property)
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("current_schema", ServerSideOnly = true, Configuration = ProviderName.SapHana   )]
		[Sql.Expression("current server", ServerSideOnly = true, Configuration = ProviderName.DB2       )]
		[Sql.Function("current_database", ServerSideOnly = true, Configuration = ProviderName.PostgreSQL)]
		[Sql.Function("DATABASE"        , ServerSideOnly = true, Configuration = ProviderName.MySql     )]
		[Sql.Function("DB_NAME"         , ServerSideOnly = true                                         )]
		static string DbName()
		{
			throw new InvalidOperationException();
		}

		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.Informix     )]
		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.OracleNative )]
		[Sql.Expression("user"          , ServerSideOnly = true, Configuration = ProviderName.OracleManaged)]
		[Sql.Expression("current_user"  , ServerSideOnly = true, Configuration = ProviderName.SapHana      )]
		[Sql.Expression("current schema", ServerSideOnly = true, Configuration = ProviderName.DB2          )]
		[Sql.Function("current_schema"  , ServerSideOnly = true, Configuration = ProviderName.PostgreSQL   )]
		[Sql.Function("USER_NAME"       , ServerSideOnly = true, Configuration = ProviderName.Sybase       )]
		[Sql.Function("SCHEMA_NAME"     , ServerSideOnly = true                                            )]
		static string SchemaName()
		{
			throw new InvalidOperationException();
		}

		[Table("LinqDataTypes")]
		class TestTable
		{
			[Column("ID")]
			public int ID { get; set; }
		}
	}
}
