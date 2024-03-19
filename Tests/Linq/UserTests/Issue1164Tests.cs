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
			var cs = DataConnection.GetConnectionString(context).Replace("TestData", "issue_1164");

			using (var db = new DataConnection(new AccessOleDbDataProvider(), cs))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db);

				Assert.That(schema, Is.Not.Null);
			}
		}

		[Test]
		public void TestOdbc([IncludeDataSources(ProviderName.AccessOdbc)] string context)
		{
			var cs = DataConnection.GetConnectionString(context).Replace("TestData.ODBC", "issue_1164");
			using (var db = new DataConnection(new AccessODBCDataProvider(), cs))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db);

				Assert.That(schema, Is.Not.Null);
			}
		}
	}
}
