using System.Linq;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[Test]
		public void TestTableName([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = db.GetTable<TestTable>().ToList();
			}
		}

		[Test]
		public void TestTableNameWithSchema([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				string schemaName;

				using (new DisableLogging())
					schemaName = TestUtils.GetSchemaName(db);

				var _ = db.GetTable<TestTable>().SchemaName(schemaName).ToList();
			}
		}

		[Test]
		public void TestTableNameWithDatabase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				string dbName;

				using (new DisableLogging())
					dbName = TestUtils.GetDatabaseName(db);

				if (   context == ProviderName.SapHana
					|| context == ProviderName.DB2)
					Assert.Throws<LinqToDBException>(() => db.GetTable<TestTable>().DatabaseName(dbName).ToList());
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
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
		[Test]
		public void TestTableNameWithDatabaseAndSchema([DataSources(ProviderName.SapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				string schemaName;
				string dbName;

				using (new DisableLogging())
				{
					schemaName = TestUtils.GetSchemaName(db);
					dbName = TestUtils.GetDatabaseName(db);
				}

				db.GetTable<TestTable>().SchemaName(schemaName).DatabaseName(dbName).ToList();
			}
		}

		[Table("LinqDataTypes")]
		class TestTable
		{
			[Column("ID")]
			public int ID { get; set; }
		}
	}
}
