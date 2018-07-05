using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OrderByDistinctTests : TestBase
	{
		class OrderByDistinctData
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string DuplicateData { get; set; }

			[Column]
			public int OrderData1 { get; set; }

			[Column]
			public int OrderData2 { get; set; }
		}

		static OrderByDistinctData[] GetTestData()
		{
			return new[]
			{
				new OrderByDistinctData { Id = 1,  DuplicateData = "One",    OrderData1 = 1, OrderData2 = 1 },
				new OrderByDistinctData { Id = 2,  DuplicateData = "One",    OrderData1 = 1, OrderData2 = 10 },
				new OrderByDistinctData { Id = 3,  DuplicateData = "One",    OrderData1 = 2, OrderData2 = 2 },
				new OrderByDistinctData { Id = 4,  DuplicateData = "One",    OrderData1 = 3, OrderData2 = 3 },
				new OrderByDistinctData { Id = 5,  DuplicateData = "One",    OrderData1 = 4, OrderData2 = 4 },
				new OrderByDistinctData { Id = 6,  DuplicateData = "One",    OrderData1 = 5, OrderData2 = 5 },

				new OrderByDistinctData { Id = 10, DuplicateData = "Two",    OrderData1 = 1, OrderData2 = 1 },
				new OrderByDistinctData { Id = 20, DuplicateData = "Two",    OrderData1 = 1, OrderData2 = 10 },
				new OrderByDistinctData { Id = 30, DuplicateData = "Two",    OrderData1 = 2, OrderData2 = 2 },
				new OrderByDistinctData { Id = 40, DuplicateData = "Two",    OrderData1 = 3, OrderData2 = 3 },
				new OrderByDistinctData { Id = 50, DuplicateData = "Two",    OrderData1 = 4, OrderData2 = 4 },
				new OrderByDistinctData { Id = 60, DuplicateData = "Two",    OrderData1 = 5, OrderData2 = 5 },

				new OrderByDistinctData { Id = 100, DuplicateData = "Three", OrderData1 = 1, OrderData2 = 1 },
				new OrderByDistinctData { Id = 200, DuplicateData = "Three", OrderData1 = 1, OrderData2 = 10 },
				new OrderByDistinctData { Id = 300, DuplicateData = "Three", OrderData1 = 2, OrderData2 = 2 },
				new OrderByDistinctData { Id = 400, DuplicateData = "Three", OrderData1 = 3, OrderData2 = 3 },
				new OrderByDistinctData { Id = 500, DuplicateData = "Three", OrderData1 = 4, OrderData2 = 4 },
				new OrderByDistinctData { Id = 600, DuplicateData = "Three", OrderData1 = 5, OrderData2 = 5 },

			};
		}

		static OrderByDistinctData[] GetUniqueTestData()
		{
			return GetTestData().Where(t => t.Id == 1 || t.Id == 10 || t.Id == 100).ToArray();
		}

		[Test, Combinatorial]
		public void OrderByDistinctTestOrdering([DataSources] string context)
		{
			var testData = GetUniqueTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.OrderBy(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				var expected = table
					.OrderBy(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderByDescending(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.OrderByDescending(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderBy(x => x.OrderData1)
					.ThenBy(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.OrderBy(x => x.OrderData1)
					.ThenBy(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderByDescending(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.OrderByDescending(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderByDistinctTest([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.OrderBy(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				var expected = table
					.GroupBy(x => x.DuplicateData)
					.Select(g => new
					{
						DuplicateData = g.Key,
						OrderData1 = g.Max(i => i.OrderData1)
					})
					.OrderBy(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);

				actual = table
					.OrderByDescending(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				expected = table
					.GroupBy(x => x.DuplicateData)
					.Select(g => new
					{
						DuplicateData = g.Key,
						OrderData1 = g.Min(i => i.OrderData1)
					})
					.OrderByDescending(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderByExpressionDistinctTests([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.OrderBy(x => x.OrderData1 % 3)
					.Select(x => x.DuplicateData)
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				var expected = table
					.GroupBy(x => x.DuplicateData)
					.Select(g => new
					{
						DuplicateData = g.Key,
						OrderData1 = g.Max(i => i.OrderData1 % 3)
					})
					.OrderBy(x => x.OrderData1)
					.Select(x => x.DuplicateData)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderByDistinctNoTransformTests([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.OrderBy(x => x.OrderData1)
					.Select(x => new { x.DuplicateData, x.OrderData1 })
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				var expected = testData
					.Select(x => new { x.DuplicateData, x.OrderData1 })
					.Distinct()
					.OrderBy(x => x.OrderData1)
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderByDistinctPartialTransformTests([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => new { x.DuplicateData, x.OrderData2 })
					.Distinct()
					.Skip(0)
					.Take(3)
					.ToArray();

				var expected = testData
					.GroupBy(x => new {x.DuplicateData, x.OrderData2})
					.Select(g => new
					{
						g.Key.DuplicateData,
						g.Key.OrderData2,
						OrderData1 = g.Max(i => i.OrderData1)
					})
					.OrderBy(x => x.OrderData1)
					.ThenByDescending(x => x.OrderData2)
					.Select(x => new { x.DuplicateData, x.OrderData2 })
					.Skip(0)
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderByUnionOptimization([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var actual = table
					.Where(x => x.Id.Between(1, 9))
					.OrderBy(x => x.OrderData1)
					.Concat(table
						.Where(x => x.Id.Between(10, 90))
						.OrderBy(x => x.OrderData2))
					.Union(table
						.Where(x => x.Id.Between(100, 900))
						.OrderBy(x => x.DuplicateData))
					.OrderBy(x => x.DuplicateData)
					.Select(x => x.Id)
					.Distinct()
					.ToArray();

				var expected = testData
					.Where(x => x.Id.Between(1, 9))
					.OrderBy(x => x.OrderData1)
					.Concat(testData
						.Where(x => x.Id.Between(10, 90))
						.OrderBy(x => x.OrderData2))
					.Union(testData
						.Where(x => x.Id.Between(100, 900))
						.OrderBy(x => x.DuplicateData))
					.OrderBy(x => x.DuplicateData)
					.Select(x => x.Id)
					.Distinct()
					.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test, Combinatorial]
		public void OrderBySubQuery([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var subQuery1 =
					table.OrderBy(t => t.OrderData1)
						.Select(c => new OrderByDistinctData
						{
							Id = c.Id * 1,
							DuplicateData = c.DuplicateData,
							OrderData1 = c.OrderData1,
							OrderData2 = c.OrderData2
						});

				var subQuery2 =
					subQuery1.OrderBy(t => t.OrderData1);

				var query = from t in table.Take(2)
					orderby t.Id descending
					select new
					{
						t.DuplicateData,
						Count = subQuery2.Where(s => s.DuplicateData == t.DuplicateData).Count()
					};

				var selectQuery = query.GetSelectQuery();
				var info = new QueryInformation(selectQuery);
				info.GetParentQuery(selectQuery);

				var result = query.ToArray();
			}

		}

		
		[Test, Combinatorial]
		public void DoubleOrderBy([DataSources] string context)
		{
			var testData = GetTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var subQuery1 =
					table.OrderBy(t => t.OrderData1)
						.Select(c => new OrderByDistinctData
						{
							Id = c.Id * 1,
							DuplicateData = c.DuplicateData,
							OrderData1 = c.OrderData1,
							OrderData2 = c.OrderData2
						});

				var subQuery2 = table.OrderBy(t => t.OrderData2).Take(3);

				var query = from q2 in subQuery2
					from q1 in subQuery1.InnerJoin(q1 => q1.Id == q2.Id)
					select q1;

				var result = query.ToArray();
			}

		}
	}
}
