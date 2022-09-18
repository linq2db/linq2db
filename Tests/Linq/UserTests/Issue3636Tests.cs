using FluentAssertions;
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

			using var db     = GetDataContext(context);
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

			groupResult.Select(x => x.s.ID2).Should().AllBeEquivalentTo(myId);

			if (myId == 2)
			{
				Assert.AreEqual(1, result.Key);

				Assert.AreEqual(1, groupResult.Length);

				Assert.AreEqual(1, groupResult[0].s.ID);
				Assert.AreEqual(2, groupResult[0].s.ID2);
				Assert.AreEqual(0, groupResult[0].s.ID3);
				Assert.AreEqual(1, groupResult[0].order.ID);
				Assert.AreEqual(2, groupResult[0].order.ID2);
			}
			else
			{
				Assert.AreEqual(2, result.Key);

				Assert.AreEqual(1, groupResult.Length);

				Assert.AreEqual(2, groupResult[0].s.ID);
				Assert.AreEqual(85, groupResult[0].s.ID2);
				Assert.AreEqual(0, groupResult[0].s.ID3);
				Assert.AreEqual(2, groupResult[0].order.ID);
				Assert.AreEqual(85, groupResult[0].order.ID2);
			}
		}
	}
}
