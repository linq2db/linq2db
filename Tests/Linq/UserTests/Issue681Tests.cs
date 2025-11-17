using System;
using System.Linq;
using System.Threading.Tasks;

#if NETFRAMEWORK
using System.ServiceModel;
#else
using Grpc.Core;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[Table("Issue681Table")]
		sealed class TestTable
		{
			[PrimaryKey]
			public int ID    { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Table("Issue681Table4")]
		sealed class TestTableWithIdentity
		{
			[PrimaryKey, Identity]
			public int ID { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test]
		public async Task TestITable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) => { t.ToList(); return Task.CompletedTask; });
		}

		[Test]
		public async Task TestInsert(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Insert(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestUpdate(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Update(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestDelete(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Delete(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestInsertOrReplace(
			[InsertOrUpdateDataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				var record = new TestTable() { ID = 5, Value = 10 };
				// insert
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				// replace
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestInsertWithIdentity(
			[DataSources(TestProvName.AllClickHouse)] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTableWithIdentity>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.InsertWithIdentity(new TestTableWithIdentity() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestCreate(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					db.CreateTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
				}

				return Task.CompletedTask;
				// not allowed for remote server
			}, $"{TestProvName.AllSqlServer},{TestProvName.AllOracle}", ddl: true);
		}

		[Test]
		public async Task TestCreateAsync(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, async (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					await db.CreateTableAsync<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					await db.DropTableAsync<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
				}
				// not allowed for remote server
			}, $"{TestProvName.AllSqlServer},{TestProvName.AllOracle}", ddl: true);
		}

		[Test]
		public async Task TestDrop(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					db.CreateTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}

				return Task.CompletedTask;
			}, TestProvName.AllSqlServer, ddl: true);
		}

		private async Task TestTableFQN<TTable>(
			string context,
			bool withServer, bool withDatabase, bool withSchema,
			Func<IDataContext, ITable<TTable>, string?, string?, string?, Task> operation,
			string? withServerThrows = null,
			bool ddl = false)
			where TTable: class
		{
			var throws              = false;
			var throwsSqlException  = false;
			var throwsOraException  = false;
			var throwsHanaException = false;

			string? serverName;
			string? schemaName;
			string? dbName;

			if (withServer && (!withDatabase || !withSchema || ddl) && context.IsAnyOf(TestProvName.AllSqlServer))
			{
				// 1. SQL Server FQN requires schema and db components for linked-server query
				// 2. DDL queries cannto be run against linked server
				throws             = true;
				throwsSqlException = ddl && withDatabase && withSchema;
			}

			if (withServer && ddl && context.IsAnyOf(TestProvName.AllSapHana))
			{
				// SAP HANA doesn't support DDL queries for linked servers (CREATE/DROP TABLE)
				throws              = true;
				throwsHanaException = withSchema;
			}

			if (withServerThrows != null && withServer && context.IsAnyOf(withServerThrows))
			{
				throws = true;
				if (context.IsAnyOf(TestProvName.AllSqlServer) && withDatabase && withSchema)
					throwsSqlException = true;
				if (context.IsAnyOf(TestProvName.AllOracle))
					throwsOraException = true;
			}

			if (withServer && withDatabase && withSchema && context.IsAnyOf(TestProvName.AllSqlAzure))
			{
				// linked servers not supported by Azure
				// "Reference to database and/or server name in '...' is not supported in this version of SQL Server."
				throws = true;
				throwsSqlException = true;
			}

			if (withServer && !withDatabase && context.IsAnyOf(TestProvName.AllInformix))
			{
				// Informix requires db name for linked server queries
				throws = true;
			}

			if (withServer && !withSchema && context.IsAnyOf(TestProvName.AllSapHana))
			{
				// SAP HANA requires schema name for linked server queries
				throws = true;
			}

			if (withDatabase && !withSchema && context.IsAnyOf(ProviderName.DB2))
			{
				throws = true;
			}

			using var _  = new DisableBaseline("Use instance name is SQL", context.IsAnyOf(TestProvName.AllSqlServer) && !context.IsAnyOf(TestProvName.AllSqlAzure) && withServer);
			using var db = GetDataContext(context, testLinqService : false);

			using (new DisableLogging())
			{
				serverName = withServer   ? TestUtils.GetServerName  (db, context) : null;
				dbName     = withDatabase ? TestUtils.GetDatabaseName(db, context) : null;
				schemaName = withSchema   ? TestUtils.GetSchemaName  (db, context) : null;
			}

			using var t  = db.CreateLocalTable<TTable>(databaseName: dbName, serverName: serverName, schemaName: schemaName);

			var table = db.GetTable<TTable>();

			if (withServer  ) table = table.ServerName  (serverName);
			if (withDatabase) table = table.DatabaseName(dbName);
			if (withSchema  ) table = table.SchemaName  (schemaName);

			if (throws && context.IsRemote())
			{
#if NETFRAMEWORK
				await Assert.ThatAsync(() => operation(db, table, schemaName, dbName, serverName), Throws.InstanceOf<FaultException>());
#else
				await Assert.ThatAsync(() => operation(db, table, schemaName, dbName, serverName), Throws.InstanceOf<RpcException>());
#endif
			}
			else if (throws)
			{
				if (throwsSqlException)
				{
					await Assert.ThatAsync(() => operation(db, table, schemaName, dbName, serverName), Throws.InstanceOf(((SqlServerDataProvider)((DataConnection)db).DataProvider).Adapter.SqlExceptionType));
				}
				else if (throwsHanaException)
				{
					try
					{
						await operation(db, table, schemaName, dbName, serverName);
						Assert.Fail("OracleException expected");
					}
					catch (Exception ex)
					{
						Assert.That(ex.GetType().Name, Is.EqualTo(context.IsAnyOf(ProviderName.SapHanaOdbc) ? "OdbcException" : "HanaException"));
					}
				}
				else if (throwsOraException)
				{
					try
					{
						await operation(db, table, schemaName, dbName, serverName);
						Assert.Fail("OracleException expected");
					}
					catch (Exception ex)
					{
						Assert.That(ex.GetType().Name, Is.EqualTo("OracleException"));
					}
				}
				else
				{
					await Assert.ThatAsync(() => operation(db, table, schemaName, dbName, serverName), Throws.InstanceOf<LinqToDBException>());
				}
			}
			else
			{
				await operation(db, table, schemaName, dbName, serverName);
			}
		}
	}
}
