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
		public void TestOleDb([IncludeDataSources(TestProvName.AllAccessOleDb)] string context)
		{
			var cs = DataConnection.GetConnectionString(context).Replace("TestData", "issue_1164");

			using (var db = new DataConnection(AccessTools.GetDataProvider(provider: AccessProvider.OleDb), cs))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db);

				Assert.That(schema, Is.Not.Null);
			}
		}

		[Test]
		public void TestOdbc([IncludeDataSources(TestProvName.AllAccessOdbc)] string context)
		{
			var cs = DataConnection.GetConnectionString(context).Replace("TestData.ODBC", "issue_1164");
			using (var db = new DataConnection(AccessTools.GetDataProvider(provider: AccessProvider.ODBC), cs))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				var schema = schemaProvider.GetSchema(db);

				Assert.That(schema, Is.Not.Null);
			}
		}
	}
}
