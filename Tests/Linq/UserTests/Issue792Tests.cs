using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
#if !NETSTANDARD1_6
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
	/// 2. SAP HANA 2 provider schema calls are slow and cannot be automated as provider shows c++ assert messagebox from time
	///    to time or hang. Clicking "No" in message box leads to successfull run.
	///    This issue exists for all SAPHANA2 schema tests.
	///    File 'd:\703\w\c3eyx5mf7a\src\interfaces\ado.net\impl\command_imp.cpp' at line #637
	/// 3. Following providers execute procedures for real: Sybase, MySQL (5.6, 5.7, MariaDB)
	/// 4. Following providers miss procedures schema load so wasn't really tested yet: PostgreSQL, Informix
	/// 5. Following providers doesn't support transactions during schema load:
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
			public string char20DataType;
		}

		[Test, DataContextSource(false,
			// those providers doesn't support stored procedures
			ProviderName.SqlCe, ProviderName.SQLite, ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
			// those providers miss procedure schema load implementation for now
			ProviderName.Informix, ProviderName.PostgreSQL)]
		public void TestWithoutTransaction(string context)
		{
			using (var db = new DataConnection(context))
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				try
				{
					var schema = sp.GetSchema(db, new GetSchemaOptions()
					{
						GetTables = false
					});

					var recordsAfter = db.GetTable<AllTypes>().Count();

					// schema request shouldn't execute procedure
					Assert.AreEqual(recordsBefore, recordsAfter);

					// schema provider should find our procedure for real
					Assert.AreEqual(1, schema.Procedures.Count(p => p.ProcedureName.ToUpper() == "ADDISSUE792RECORD"));
				}
				finally
				{
					// cleanup
					db.GetTable<AllTypes>().Delete(_ => _.char20DataType == "issue792");
				}
			}
		}

		[Test, DataContextSource(false,
			// those providers doesn't support stored procedures
			ProviderName.SqlCe, ProviderName.SQLite, ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
			// those providers miss procedure schema load implementation for now
			ProviderName.Informix, ProviderName.PostgreSQL,
			// those providers cannot load schema when in transaction
			ProviderName.DB2, ProviderName.Sybase,
			ProviderName.MySql, TestProvName.MySql57, TestProvName.MariaDB,
			ProviderName.SqlServer2000, ProviderName.SqlServer2005, TestProvName.SqlAzure,
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TestWithTransaction(string context)
		{
			using (var db = new DataConnection(context))
			using (var ts = db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var schema = sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				});

				var recordsAfter = db.GetTable<AllTypes>().Count();

				// schema request shouldn't execute procedure
				Assert.AreEqual(recordsBefore, recordsAfter);

				// schema provider should find our procedure for real
				Assert.AreEqual(1, schema.Procedures.Count(p => p.ProcedureName.ToUpper() == "ADDISSUE792RECORD"));
			}
		}

		[Test, IncludeDataContextSource(false,
			ProviderName.DB2,
			ProviderName.SqlServer2000, ProviderName.SqlServer2005, TestProvName.SqlAzure,
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TestWithTransactionThrowsFromProvider(string context)
		{
			using (var db = new DataConnection(context))
			using (var ts = db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var ex = Assert.Catch(() => sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				}));

				Assert.IsInstanceOf<InvalidOperationException>(ex);
				Assert.IsTrue(ex.Message.Contains("requires the command to have a transaction"));
			}
		}

		[Test, IncludeDataContextSource(false,
			ProviderName.Sybase,
			ProviderName.MySql, TestProvName.MySql57, TestProvName.MariaDB)]
		public void TestWithTransactionThrowsFromLinqToDB(string context)
		{
			using (var db = new DataConnection(context))
			using (var ts = db.BeginTransaction())
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				var ex = Assert.Catch(() => sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				}));

				Assert.IsInstanceOf<LinqToDBException>(ex);
				Assert.AreEqual("Cannot read schema with GetSchemaOptions.GetProcedures = true from transaction. Remove transaction or set GetSchemaOptions.GetProcedures to false", ex.Message);
			}
		}
	}
#endif
}
