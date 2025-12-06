using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

namespace Tests.UserTests
{
	/// <summary>
	/// Note that tests below do only following:
	/// - check if procedure schema works as it is or it executes procedures for real and should be wrapped in transaction and rolled back
	/// - check it it works in existing transaction or not
	/// - DB2 zOS not tested due to unavailability
	///
	/// What is not tested:
	/// - support for procedure schema read and correctness of received data. For this we need a separate per-provider
	/// tests like we have for MySQL already. If schema provider that lacks procedures support will add this support,
	/// tests below should be reexaminated for this provider.
	///
	/// Summary on tests below:
	/// 1. SQL CE and SQLite excluded, as they don't support stored procedures
	/// 2. Following providers execute procedures for real: Sybase, MySQL/MariaDB
	/// 3. Following providers miss procedures schema load so wasn't really tested yet: PostgreSQL, Informix
	/// 4. Following providers doesn't support transactions during schema load:
	///    Sybase (The 'CREATE TABLE' command is not allowed within a multi-statement transaction in the 'tempdb' database.)
	///    DB2, MSSQL (Execute requires the command to have a transaction object when the connection assigned to the command is in a pending local transaction.  The Transaction property of the command has not been initialized)
	///    see also: https://social.msdn.microsoft.com/Forums/en-US/b4a458d0-65bd-40fb-bc60-c7ed8e94517f/sqlconnectiongetschema-exceptions-when-in-a-transaction?forum=adodotnetdataproviders
	/// </summary>
	[TestFixture]
	public class Issue792Tests : TestBase
	{
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		public class AllTypes
		{
			[Column("CHAR20DATATYPE", Configuration = ProviderName.DB2)]
			public string? char20DataType;
		}

		[Test]
		[YdbNotImplementedYet]
		public void TestWithoutTransaction([DataSources(false,
			// those providers doesn't support stored procedures
			ProviderName.SqlCe,
			TestProvName.AllSQLite,
			TestProvName.AllClickHouse,
			// those providers miss procedure schema load implementation for now
			TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				try
				{
					var schemaName = TestUtils.GetSchemaName(db, context);
					var schema     = sp.GetSchema(db, new GetSchemaOptions()
					{
						GetTables       = false,
						IncludedSchemas = schemaName != TestUtils.NO_SCHEMA_NAME ? new[] { schemaName } : null
					});

					var recordsAfter = db.GetTable<AllTypes>().Count();
					using (Assert.EnterMultipleScope())
					{
						// schema request shouldn't execute procedure
						Assert.That(recordsAfter, Is.EqualTo(recordsBefore));

						// schema provider should find our procedure for real
						Assert.That(schema.Procedures.Count(p => p.ProcedureName.ToUpperInvariant() == "ADDISSUE792RECORD"), Is.EqualTo(1));
					}
				}
				finally
				{
					// cleanup
					db.GetTable<AllTypes>().Delete(_ => _.char20DataType == "issue792");
				}
			}
		}

		[Test]
		[YdbNotImplementedYet]
		public void TestWithTransaction([DataSources(false,
			// those providers doesn't support stored procedures
			ProviderName.SqlCe,
			TestProvName.AllSQLite,
			TestProvName.AllClickHouse,
			// those providers miss procedure schema load implementation for now
			TestProvName.AllInformix,
			// those providers cannot load schema when in transaction
			ProviderName.DB2,
			TestProvName.AllAccessOleDb,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllSqlServer)]
			string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var schemaName = TestUtils.GetSchemaName(db, context);
				var schema     = sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables       = false,
					IncludedSchemas = schemaName != TestUtils.NO_SCHEMA_NAME ? new[] { schemaName } : null
				});

				var recordsAfter = db.GetTable<AllTypes>().Count();
				using (Assert.EnterMultipleScope())
				{
					// schema request shouldn't execute procedure
					Assert.That(recordsAfter, Is.EqualTo(recordsBefore));

					// schema provider should find our procedure for real
					Assert.That(schema.Procedures.Count(p => p.ProcedureName.ToUpperInvariant() == "ADDISSUE792RECORD"), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void TestWithTransactionThrowsFromProvider([IncludeDataSources(
			ProviderName.DB2,
			TestProvName.AllSqlServer)]
			string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var ex = Assert.Catch(() => sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				}))!;

				Assert.That(ex, Is.InstanceOf<InvalidOperationException>());
				Assert.That(
					ex.Message.Contains("requires the command to have a transaction")
					|| ex.Message.Contains("команда имела транзакцию") //for those who accidentally installed a russian localization of Sql Server :)
, Is.True);
			}
		}

		[Test]
		public void TestWithTransactionThrowsFromLinqToDB([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var ex = Assert.Catch(() => sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				}))!;

				Assert.That(ex, Is.InstanceOf<LinqToDBException>());
				Assert.That(ex.Message, Is.EqualTo("Cannot read schema with GetSchemaOptions.GetProcedures = true from transaction. Remove transaction or set GetSchemaOptions.GetProcedures to false"));
			}
		}
	}
}
