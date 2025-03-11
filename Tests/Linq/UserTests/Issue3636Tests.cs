using System.Linq;

using Shouldly;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3636Tests : TestBase
	{
		[Table]
		public class T1
		{
			[Column("id")] public int ID { get;   set; } // integer
			[Column("id2")] public int ID2 { get; set; } // integer
			[Column("id3")] public int ID3 { get; set; } // integer
		}

		[Table]
		public class T2
		{
			[Column("id")] public int ID { get;   set; } // integer
			[Column("id2")] public int ID2 { get; set; } // integer
		}

		[Test]
		public void CheckCacheIssue([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context, [Values(2, 85)] int myId)
		{
			var data1 = new[] { new T1 { ID = 1, ID2 = 2 }, new T1 { ID = 2, ID2 = 2 }, new T1 { ID = 2, ID2 = 85 } };
			var data2 = new[] { new T2 { ID = 1, ID2 = 2 }, new T2 { ID = 2, ID2 = 2 }, new T2 { ID = 2, ID2 = 85 } };

			using var db     = GetDataContext(context, o => o.UseGuardGrouping(false));
			using var table1 = db.CreateLocalTable(data1);
			using var table2 = db.CreateLocalTable(data2);

			var query =
				from s in db.GetTable<T1>().Where(x => x.ID2 == myId)
				join o in db.GetTable<T2>().Where(x => x.ID2 == myId) on s.ID equals o.ID into temp1
				from order in temp1.DefaultIfEmpty()
				group new { s, order } by s.ID
				into g
				select g;

			var result      = query.OrderBy(x => x.Key).First();
			var groupResult = result.ToArray();

			groupResult.Select(x => x.s.ID2).ShouldAllBe(id => id == myId);

			if (myId == 2)
			{
				Assert.Multiple(() =>
				{
					Assert.That(result.Key, Is.EqualTo(1));

					Assert.That(groupResult, Has.Length.EqualTo(1));
				});

				Assert.Multiple(() =>
				{
					Assert.That(groupResult[0].s.ID, Is.EqualTo(1));
					Assert.That(groupResult[0].s.ID2, Is.EqualTo(2));
					Assert.That(groupResult[0].s.ID3, Is.EqualTo(0));
					Assert.That(groupResult[0].order.ID, Is.EqualTo(1));
					Assert.That(groupResult[0].order.ID2, Is.EqualTo(2));
				});
			}
			else
			{
				Assert.Multiple(() =>
				{
					Assert.That(result.Key, Is.EqualTo(2));

					Assert.That(groupResult, Has.Length.EqualTo(1));
				});

				Assert.Multiple(() =>
				{
					Assert.That(groupResult[0].s.ID, Is.EqualTo(2));
					Assert.That(groupResult[0].s.ID2, Is.EqualTo(85));
					Assert.That(groupResult[0].s.ID3, Is.EqualTo(0));
					Assert.That(groupResult[0].order.ID, Is.EqualTo(2));
					Assert.That(groupResult[0].order.ID2, Is.EqualTo(85));
				});
			}
		}
	}
}
