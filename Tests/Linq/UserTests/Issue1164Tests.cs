using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1164Tests : TestBase
	{
		[Test]
		public void TestOleDb([IncludeDataSources(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(new AccessOleDbDataProvider(), "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Database\\issue_1164.mdb;"))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				
				Assert.IsNotNull(schema);
			}
		}

		[Test]
		public void TestOdbc([IncludeDataSources(ProviderName.AccessOdbc)] string context)
		{
			using (var db = new DataConnection(new AccessODBCDataProvider(), "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=Database\\issue_1164.mdb;"))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				Assert.IsNotNull(schema);
			}
		}
	}
}
