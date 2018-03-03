using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using NUnit.Framework;
using System.Linq;

namespace Tests.UserTests
{
	[ActiveIssue(792, Details = "It cannot be tested as it is server/provider bug. See referenced issue for proper fix")]
	[TestFixture]
	public class Issue792Tests : TestBase
	{
		public class AllTypes
		{
			public string char20DataType;
		}

		[Test, IncludeDataContextSource(false, ProviderName.Sybase)]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				// because for some reason sybase runs procedures when we request schema for them
				// procedure AddIssue792Record will run and add 1 record to AllTypes table
				var schema = sp.GetSchema(db, new GetSchemaOptions()
				{
					GetTables = false
				});

				var recordsAfter = db.GetTable<AllTypes>().Count();

				// cleanup
				db.GetTable<AllTypes>().Delete(_ => _.char20DataType == "issue792");

				Assert.AreEqual(recordsBefore, recordsAfter);
			}
		}
	}
}
