using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SqlOptimizationTests : TestBase
	{
		[Table]
		sealed class DataClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int IntValue { get; set; }
			[Column] public string StringValue { get; set; } = null!;

			public static DataClass[] Seed()
			{
				return Enumerable.Range(1, 10).Select(idx => new DataClass
					{
						Id = idx, IntValue = idx, StringValue = "Str" + idx + "Value"
					})
					.ToArray();
			}
		}

		[Test]
		public void EpxprExprPredicatePlus([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = DataClass.Seed();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(items);

			var expected1 = items.Where(t => t.Id + 1 > 5).ToArray();
			var result1 = table.Where(t => t.Id + 1 > 5).ToArray();
			
			AreEqualWithComparer(expected1, result1);
			
			var expected2 = items.Where(t => 5 < t.Id + 1).ToArray();
			var result2 = table.Where(t => 5 < t.Id + 1).ToArray();
			
			AreEqualWithComparer(expected2, result2);
			
			
			var expected3 = items.Where(t => 1 + t.Id > 5).ToArray();
			var result3 = table.Where(t => 1 + t.Id > 5).ToArray();
			
			AreEqualWithComparer(expected3, result3);

			var expected4 = items.Where(t => 5 < 1 + t.Id).ToArray();
			var result4 = table.Where(t => 5 < 1 + t.Id).ToArray();

			AreEqualWithComparer(expected4, result4);
		}

		[Test]
		public void EpxprExprPredicateMinus([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = DataClass.Seed();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(items);

			var expected1 = items.Where(t => t.Id - 1 > 5).ToArray();
			var result1 = table.Where(t => t.Id - 1 > 5).ToArray();
			
			AreEqualWithComparer(expected1, result1);
			
			var expected2 = items.Where(t => 5 < t.Id - 1).ToArray();
			var result2 = table.Where(t => 5 < t.Id - 1).ToArray();
			
			AreEqualWithComparer(expected2, result2);
			
			
			var expected3 = items.Where(t => 7 - t.Id > 5).ToArray();
			var result3 = table.Where(t => 7 - t.Id > 5).ToArray();
			
			AreEqualWithComparer(expected3, result3);

			var expected4 = items.Where(t => 5 < 7 - t.Id).ToArray();
			var result4 = table.Where(t => 5 < 7 - t.Id).ToArray();

			AreEqualWithComparer(expected4, result4);
		}

		[Test]
		public void BinaryPlus([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = DataClass.Seed();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(items);

			var n = 2;

			var expected1 = items.Select(t => n > 0 ? (t.Id - n - n) + 1 : 0).ToArray();
			var result1 = table.Select(t => n > 0 ? Sql.ToSql((t.Id - n - n) + 1) : 0).ToArray();
			
			AreEqualWithComparer(expected1, result1);

			var expected2 = items.Select(t => n > 0 ? (t.Id - n - n) + 1 : 0).ToArray();
			var result2 = table.Select(t => n > 0 ? Sql.AsSql((t.Id - n - n) + 1) : 0).ToArray();
			
			AreEqualWithComparer(expected2, result2);
			
			/*
			var expected2 = items.Where(t => 5 < t.Id - 1).ToArray();
			var result2 = table.Where(t => 5 < t.Id - 1).ToArray();
			
			AreEqualWithComparer(expected2, result2);
			
			
			var expected3 = items.Where(t => 7 - t.Id > 5).ToArray();
			var result3 = table.Where(t => 7 - t.Id > 5).ToArray();
			
			AreEqualWithComparer(expected3, result3);

			var expected4 = items.Where(t => 5 < 7 - t.Id).ToArray();
			var result4 = table.Where(t => 5 < 7 - t.Id).ToArray();

			AreEqualWithComparer(expected4, result4);
		*/
		}

	}

}
