using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1064Tests : TestBase
	{
		[Table]
		sealed class TableTest1064
		{
			[Column]
			public int Column1064 { get; set; }
		}

		[Table("TableTest1064")]
		sealed class TableTest1064Renamed
		{
			[Column("#Column1064")]
			public int Column1064 { get; set; }
		}


		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSybase)] string configuration)
		{
			using (var db = GetDataConnection(configuration))
			{
				using (db.CreateLocalTable<TableTest1064>())
				{
					db.Execute("sp_configure 'allow updates', 1");
					try
					{
						db.Execute("UPDATE syscolumns SET name = '#Column1064' where name = 'Column1064'");

						db.Insert(new TableTest1064Renamed() { Column1064 = 123 });

						var records = db.GetTable<TableTest1064Renamed>().ToList();

						Assert.That(records, Has.Count.EqualTo(1));
						Assert.That(records[0].Column1064, Is.EqualTo(123));
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
