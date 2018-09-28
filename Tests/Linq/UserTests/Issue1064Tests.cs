using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;
	using System.Linq;

	public class Issue1064Tests : TestBase
	{
		[Table]
		class TableTest1064
		{
			[Column]
			public int Column1064 { get; set; }
		}

		[Table("TableTest1064")]
		class TableTest1064Renamed
		{
			[Column("#Column1064")]
			public int Column1064 { get; set; }
		}


		[Test, IncludeDataContextSource(false, ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void Test(string configuration)
		{
			using (var db = new DataConnection(configuration))
			{
				using (db.CreateLocalTable<TableTest1064>())
				{
					db.Execute("sp_configure 'allow updates', 1");
					try
					{
						db.Execute("UPDATE syscolumns SET name = '#Column1064' where name = 'Column1064'");

						db.Insert(new TableTest1064Renamed() { Column1064 = 123 });

						var records = db.GetTable<TableTest1064Renamed>().ToList();

						Assert.AreEqual(1, records.Count);
						Assert.AreEqual(123, records[0].Column1064);
					}
					finally
					{
						db.Execute("sp_configure 'allow updates', 0");
					}
				}
			}
		}
	}
}
