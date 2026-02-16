using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{

	[TestFixture]
	public class Issue5340Tests : TestBase
	{
		[Table]
		public class OuterTable
		{
			[PrimaryKey] public int Id     { get; set; }
			[Column]     public int Field1 { get; set; }
			[Column]     public int Field2 { get; set; }
		}

		[Table]
		public class InnerTable
		{
			[PrimaryKey] public int Id     { get; set; }
			[Column]     public int Field3 { get; set; }
			[Column]     public int Field4 { get; set; }
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllMariaDB, TestProvName.AllFirebirdLess4, TestProvName.AllDB2, TestProvName.AllMySql57, TestProvName.AllOracle11)]
		[Test]
		public void UpdateWithOuterApplyInSubquery([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var outerData = new[]
			{
				new OuterTable { Id = 1, Field1 = 10, Field2 = 100 },
				new OuterTable { Id = 2, Field1 = 20, Field2 = 200 }
			};

			var innerData = new[]
			{
				new InnerTable { Id = 1, Field3 = 100, Field4 = 1000 },
				new InnerTable { Id = 2, Field3 = 200, Field4 = 2000 }
			};

			using var db = GetDataContext(context);

			using (db.CreateLocalTable(outerData))
			using (db.CreateLocalTable(innerData))
			{
				var query = db.GetTable<OuterTable>()
					.Set(
						x => x.Field1,
						x => (
							from a in db.SelectQuery(() => 1)
							from b in db.GetTable<InnerTable>()
								.Where(y => x.Field2 == y.Field3)
								.OrderBy(c => c.Field4)
								.Select(c => c.Field4)
								.Take(1)
							select b
						).Single())
					.Update();

				var result = db.GetTable<OuterTable>().OrderBy(x => x.Id).ToArray();

				result[0].Field1.ShouldBe(1000);
				result[1].Field1.ShouldBe(2000);
			}
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllMariaDB, TestProvName.AllFirebirdLess4, TestProvName.AllDB2, TestProvName.AllMySql57, TestProvName.AllOracle11)]
		[Test]
		public void UpdateWithSubquery([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var outerData = new[]
			{
				new OuterTable { Id = 1, Field1 = 10, Field2 = 100 },
				new OuterTable { Id = 2, Field1 = 20, Field2 = 200 }
			};

			var innerData = new[]
			{
				new InnerTable { Id = 1, Field3 = 100, Field4 = 1000 },
				new InnerTable { Id = 2, Field3 = 200, Field4 = 2000 }
			};

			using var db = GetDataContext(context);

			using (db.CreateLocalTable(outerData))
			using (db.CreateLocalTable(innerData))
			{

				var query = db.GetTable<OuterTable>()
					.Set(
						x => x.Field1,
						x => (
							from b in db.GetTable<InnerTable>()
								.Where(y => x.Field2 == y.Field3)
								.OrderBy(c => c.Field4)
								.Select(c => c.Field4)
								.Take(1)
							select b
						).Single())
					.Update();

				var result = db.GetTable<OuterTable>().OrderBy(x => x.Id).ToArray();

				result[0].Field1.ShouldBe(1000);
				result[1].Field1.ShouldBe(2000);
			}
		}

	}
}
