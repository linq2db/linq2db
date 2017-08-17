using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;
using LinqToDB.SchemaProvider;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue792Tests : TestBase
	{
		public class AllTypes
		{
			public string char20DataType;
		}

#if !NETSTANDARD
		[Test, IncludeDataContextSource(false, ProviderName.Sybase)]
		[Explicit("https://github.com/linq2db/linq2db/issues/792")]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var recordsBefore = db.GetTable<AllTypes>().Count();

				var sp = db.DataProvider.GetSchemaProvider();

				// on fail procedure AddIssue792Record will add 1 record to AllTypes
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
#endif
	}
}
