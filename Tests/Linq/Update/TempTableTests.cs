using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class TempTableTests : TestBase
	{
		class Table1
		{
			[PrimaryKey]
			public int ID;
		}

		class Table2
		{
			[PrimaryKey]
			public int       ID;
			public DateTime? Date;
		}

		[Test]
		public void InsertTest([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Table1>([new Table1 { ID = 10 }]);
			using var t2 = db.CreateLocalTable<Table2>();

			t2.Insert(from t in t1 select new Table2 { ID = t.ID });

			var result = t2.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result,         Has.Count.EqualTo(1));
				Assert.That(result[0].ID,   Is.EqualTo(10));
				Assert.That(result[0].Date, Is.Null);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4389")]
		public async Task ClickHouseTempTableNotNullColumnBulkCopy([IncludeDataSources(ProviderName.ClickHouseDriver)] string context)
		{
			await using var db  = GetDataContext(context);

			await using var tmp = db.CreateTempTable("UniqueIdTmp",
				[ new { ID = 1 } ],
				fm => fm.Property(p => p.ID).IsNotNull());

			await tmp.BulkCopyAsync(new[] { new { ID = 2 } });

			await tmp.CountAsync();
		}
	}
}
