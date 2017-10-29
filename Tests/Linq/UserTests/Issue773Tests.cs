using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue773Tests : TestBase
	{
		[Table("dataFTS")]
		public partial class DtaFts
		{
			[Column]
			public long Id { get; set; }
			[Column]
			public string FirstName { get; set; }
			[Column]
			public string LastName { get; set; }
			[Column]
			public string MidName { get; set; }
		}

		[Test, IncludeDataContextSource(true, ProviderName.SQLite)]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				db.Execute(@"CREATE VIRTUAL TABLE dataFTS USING fts4(`ID` INTEGER, `FirstName` TEXT, `LastName` TEXT, `MidName` TEXT )");

				try
				{
					var data = db.GetTable<DtaFts>()
						.Select(result =>
						new
						{
							result.FirstName,
							result.MidName,
							result.LastName,
						});

					var list = data.Where(arg => SqlLite.MatchFts(arg, "John*")).ToList(); // <=THROWS EXCEPTION

					Assert.AreEqual(0, list.Count);

					db.GetTable<DtaFts>()
						.Insert(() => new DtaFts()
						{
							FirstName = "JohnTheRipper"
						});
					db.GetTable<DtaFts>()
						.Insert(() => new DtaFts()
						{
							FirstName = "DoeJohn"
						});

					list = data.Where(arg => SqlLite.MatchFts(arg, "John*")).ToList(); // <=THROWS EXCEPTION

					Assert.AreEqual(1, list.Count);
					Assert.AreEqual("JohnTheRipper", list[0].FirstName);
				}
				finally
				{
					// cleanup
					db.Execute("DROP TABLE dataFTS");
				}
			}
		}

	}
}
