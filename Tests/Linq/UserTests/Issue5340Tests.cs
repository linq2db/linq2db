using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
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

		[ThrowsRequiredOuterJoins(TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_Join_Without_Condition)]
		[Test]
		public void UpdateWithOuterApplyInSubquery([DataSources(TestProvName.AllClickHouse, TestProvName.AllSapHana, TestProvName.AllSqlCe)] string context)
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
				var affected = db.GetTable<OuterTable>()
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

				affected.ShouldBe(2);

				var result = db.GetTable<OuterTable>().OrderBy(x => x.Id).ToArray();

				result[0].Field1.ShouldBe(1000);
				result[1].Field1.ShouldBe(2000);
			}
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllSybase)]
		[Test]
		public void UpdateWithSubquery([DataSources(TestProvName.AllClickHouse, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllSqlCe)] string context)
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

				var affected = db.GetTable<OuterTable>()
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

				affected.ShouldBe(2);

				var result = db.GetTable<OuterTable>().OrderBy(x => x.Id).ToArray();

				result[0].Field1.ShouldBe(1000);
				result[1].Field1.ShouldBe(2000);
			}
		}

	}
}
