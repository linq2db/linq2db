using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class GroupByExtensionsTests : TestBase
	{
		[Table]
		sealed class GroupSampleClass
		{
			[Column] public int Id1    { get; set; }
			[Column] public int Id2    { get; set; }
			[Column] public int Value  { get; set; }

			public static GroupSampleClass[] TestData()
			{
				var result = Enumerable.Range(1, 10)
					.Select(i => new GroupSampleClass
					{
						Id1 = i,
						Id2 = i % 3,
						Value = i % 2
					})
					.ToArray();
				return result;
			}
		}

		[Test]
		public void GroupByRollup([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllMySql,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.Rollup(new { q.Id1, q.Id2 })
					into g
					select new
					{
						g.Key.Id1,
						Count = g.Count()
					};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByRollupGrouping([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.Rollup(new { q.Id1, q.Id2 })
					into g
					select new
					{
						IsGrouping = Sql.Grouping(g.Key.Id1) == 1,
						g.Key.Id1,
						Count = g.Count()
					};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByRollupGroupingMany([IncludeDataSources(true,
			TestProvName.AllMySql80, TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.Rollup(new { q.Id1, q.Id2 })
					into g
					select new
					{
						IsGrouping = Sql.Grouping(g.Key.Id1, g.Key.Id2) == 1,
						g.Key.Id1,
						Count = g.Count()
					};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByCube([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.Cube(new { q.Id1, q.Id2 })
					into g
					select new
					{
						IsGrouping = Sql.Grouping(g.Key.Id1),
						g.Key.Id1,
						Count = g.Count()
					};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByGroupingSets([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.GroupingSets(new { Set1 = new { q.Id1, q.Id2 }, Set2 = new { q.Id2 }, Set3 = new {}})
					into g
					select new
					{
						IsGrouping = Sql.Grouping(g.Key.Set1.Id1),
						g.Key.Set1.Id1,
						Count = g.Count()
					};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByGroupingSetsHaving([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = from q in query
					group q by Sql.GroupBy.GroupingSets(new
						{ Set1 = new { q.Id1, q.Id2 }, Set2 = new { q.Id2 }, Set3 = new { } })
					into g
					where g.Count() > 0 || Sql.Grouping(g.Key.Set1.Id1) == 1
					select
						new
						{
							g.Key.Set1.Id1,
							Count = g.Count()
						};

				var result = grouped.ToArray();
			}
		}

		[Test]
		public void GroupByGroupingSetsHaving2([IncludeDataSources(true,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL95Plus,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse)] string context)
		{
			var testData = GroupSampleClass.TestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.Distinct();

				var grouped = (from q in query
						group q by Sql.GroupBy.GroupingSets(new
							{ Set1 = new { q.Id1, q.Id2 }, Set2 = new { q.Id2 }, Set3 = new { } })
						into g
						select g)
					.Where(gg => gg.Count() > 0)
					.Select(g =>
						new
						{
							g.Key.Set1.Id1,
							Count = g.Count()
						});

				var result = grouped.ToArray();
			}
		}
	}
}
