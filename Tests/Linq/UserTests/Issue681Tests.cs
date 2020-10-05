﻿using System.Linq;

#if NET472
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[Table("LinqDataTypes")]
		class TestTable
		{
			[Column("ID")]
			public int ID { get; set; }
		}

		// for SAP HANA cross-server queries see comments how to configure SAP HANA in TestUtils.GetServerName() method
		[Test]
		public void TestTableFQN(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			var throws             = false;
			var throwsSqlException = false;

			string? serverName;
			string? schemaName;
			string? dbName;

			using (new DisableBaseline("Use instance name is SQL", context.Contains("SqlServer") && withServer))
			using (var db = GetDataContext(context))
			{
				if (withServer && (!withDatabase || !withSchema) && (context.Contains("SqlServer") || context.Contains("Azure")))
				{
					// SQL Server FQN requires schema and db components for linked-server query
					throws = true;
				}

				if (withServer && withDatabase && withSchema && context.Contains("Azure"))
				{
					// linked servers not supported by Azure
					// "Reference to database and/or server name in '...' is not supported in this version of SQL Server."
					throws             = true;
					throwsSqlException = true;
				}

				if (withServer && !withDatabase && context.Contains("Informix"))
				{
					// Informix requires db name for linked server queries
					throws = true;
				}

				if (withServer && !withSchema && context.Contains("SapHana"))
				{
					// SAP HANA requires schema name for linked server queries
					throws = true;
				}

				if (withDatabase && !withSchema && context.Contains("DB2") && !context.Contains("Informix"))
				{
					throws = true;
				}

				using (new DisableLogging())
				{
					serverName = withServer   ? TestUtils.GetServerName(db)   : null;
					dbName     = withDatabase ? TestUtils.GetDatabaseName(db) : null;
					schemaName = withSchema   ? TestUtils.GetSchemaName(db)   : null;
				}

				var table = db.GetTable<TestTable>();

				if (withServer)   table = table.ServerName  (serverName!);
				if (withDatabase) table = table.DatabaseName(dbName!);
				if (withSchema)   table = table.SchemaName  (schemaName!);

				if (throws && context.Contains(".LinqService"))
				{
#if NET472
					Assert.Throws<FaultException<ExceptionDetail>>(() => table.ToList());
#endif
				}
				else if (throws)
				{
					if (throwsSqlException)
						// https://www.youtube.com/watch?v=Qji5x8gBVX4
						Assert.Throws(
							((SqlServerDataProvider)((DataConnection)db).DataProvider).Adapter.SqlExceptionType,
							() => table.ToList());
					else
						Assert.Throws<LinqToDBException>(() => table.ToList());
				}
				else
					table.ToList();
			}
		}
	}
}
