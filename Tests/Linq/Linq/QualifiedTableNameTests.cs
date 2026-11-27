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

namespace Tests.Linq
{
	[TestFixture]
	public class QualifiedTableNameTests : TestBase
	{
		[Table("QualifiedNameTable")]
		sealed class QualifiedNameTable
		{
			[PrimaryKey]
			public int ID    { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Table("QualifiedIdTable")]
		sealed class QualifiedIdTable
		{
			[PrimaryKey, Identity]
			public int ID { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task QueryQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) => { t.ToList(); return Task.CompletedTask; });
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task InsertIntoQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Insert(new QualifiedNameTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task UpdateQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Update(new QualifiedNameTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task DeleteFromQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Delete(new QualifiedNameTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task InsertOrReplaceQualifiedTable(
			[InsertOrUpdateDataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				var record = new QualifiedNameTable() { ID = 5, Value = 10 };
				// insert
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				// replace
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task InsertWithIdentityIntoQualifiedTable(
			[DataSources(TestProvName.AllClickHouse)] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedIdTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.InsertWithIdentity(new QualifiedIdTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task CreateQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					if (context.IsAnyOf(TestProvName.AllYdb))
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					else
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", throwExceptionIfNotExists: false);

					db.CreateTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					if (context.IsAnyOf(TestProvName.AllYdb))
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					else
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", throwExceptionIfNotExists: false);
				}

				return Task.CompletedTask;
				// not allowed for remote server
			}, $"{TestProvName.AllSqlServer},{TestProvName.AllOracle}", ddl: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task CreateQualifiedTableAsync(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, async (db, t, u, d, s) =>
			{
				try
				{
					if (context.IsAnyOf(TestProvName.AllYdb))
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					else
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", throwExceptionIfNotExists: false);

					await db.CreateTableAsync<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					if (context.IsAnyOf(TestProvName.AllYdb))
						await db.DropTableAsync<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
					else
						await db.DropTableAsync<QualifiedNameTable>(tableName: "QualifiedNameTable2", throwExceptionIfNotExists: false);
				}
				// not allowed for remote server
			}, $"{TestProvName.AllSqlServer},{TestProvName.AllOracle}", ddl: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/681")]
		public async Task DropQualifiedTable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await RunQualifiedTableOperation<QualifiedNameTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					if (context.IsAnyOf(TestProvName.AllYdb))
					{
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u, throwExceptionIfNotExists: false);
						db.CreateTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u);
					}
					else
					{
						db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", throwExceptionIfNotExists: false);
						db.CreateTable<QualifiedNameTable>(tableName: "QualifiedNameTable2");
					}
				}
				finally
				{
					db.DropTable<QualifiedNameTable>(tableName: "QualifiedNameTable2", databaseName: d, serverName: s, schemaName: u);
				}

				return Task.CompletedTask;
			}, TestProvName.AllSqlServer, ddl: true);
		}

		private async Task RunQualifiedTableOperation<TTable>(
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

			using var t  = context.IsAnyOf(TestProvName.AllYdb)
				? db.CreateLocalTable<TTable>(databaseName: dbName, serverName: serverName, schemaName: schemaName)
				: db.CreateLocalTable<TTable>();

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
