using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OrderByDistinctTests : TestBase
	{
		sealed class OrderByDistinctData
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string DuplicateData { get; set; } = null!;

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

		[Test]
		public void OrderByDistinctTestOrdering([DataSources] string context)
		{
			var testData = GetUniqueTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

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

		// if this test fails for mysql, check that you have no ONLY_FULL_GROUP_BY option set
		[Test]
		public void OrderByDistinctTest([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

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
					OrderData1 = g.Max(i => i.OrderData1),
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

		[Test]
		public void OrderByDistinctNotFailTest([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.OrderBy(x => x.OrderData1)
				.Select(x => x.DuplicateData)
				.Distinct()
				.Skip(0)
				.Take(3)
				.ToArray();
		}

		[Test]
		public void OrderByExpressionDistinctTests([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

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
					OrderData1 = g.Max(i => i.OrderData1 % 3),
				})
				.OrderBy(x => x.OrderData1)
				.Select(x => x.DuplicateData)
				.Skip(0)
				.Take(3)
				.ToArray();

			AreEqual(expected, actual);
		}

		[Test]
		public void OrderByDistinctNoTransformTests([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

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

		[Test]
		public void OrderByDistinctPartialTransformTests([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

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
					OrderData1 = g.Max(i => i.OrderData1),
				})
				.OrderBy(x => x.OrderData1)
				.ThenByDescending(x => x.OrderData2)
				.Select(x => new { x.DuplicateData, x.OrderData2 })
				.Skip(0)
				.Take(3)
				.ToArray();

			AreEqual(expected, actual);
		}

		[Test]
		public void OrderByUnionOptimization([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actualQuery = table
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
				.Distinct();

			//TODO: There is issue with this distinct. It contain duplicate field. Looks like after call sequence.ConvertToIndex(null, 0, ConvertFlags.All) in DistinctBuilder we have introduced duplicate.

			//var selectQuery = actualQuery.GetSelectQuery();
			//if (selectQuery.Select.IsDistinct)
			//{
			//	Assert.That(selectQuery.Select.Columns.Count, Is.EqualTo(1));
			//}

			var actual = actualQuery.ToArray();

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

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void OrderBySubQuery([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var subQuery1 = table
				.OrderBy(t => t.OrderData1)
				.Select(c => new OrderByDistinctData
				{
					Id = c.Id * 1,
					DuplicateData = c.DuplicateData,
					OrderData1 = c.OrderData1,
					OrderData2 = c.OrderData2,
				});

			var subQuery2 = subQuery1
				.OrderBy(t => t.OrderData1);

			var query = 
				from t in table.Take(2)
				orderby t.Id descending
				select new
				{
					t.DuplicateData,
					Count = subQuery2.Where(s => s.DuplicateData == t.DuplicateData).Count(),
				};

			var result = query.ToArray();
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test]
		public void DoubleOrderBy([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var subQuery1 = table
				.OrderBy(t => t.OrderData1)
				.Select(c => new OrderByDistinctData
				{
					Id = c.Id * 1,
					DuplicateData = c.DuplicateData,
					OrderData1 = c.OrderData1,
					OrderData2 = c.OrderData2,
				});

			var subQuery2 = table.OrderBy(t => t.OrderData2).Take(3);

			var query = 
				from q2 in subQuery2
				from q1 in subQuery1.InnerJoin(q1 => q1.Id == q2.Id)
				select q1;

			var result = query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5103")]
		public void OrderByDistinctAll1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Person.OrderBy(p => p.FirstName)
				.Distinct()
				.Select(r => new { r.ID, r.LastName })
				.Skip(1)
				.Take(2);

			AssertQuery(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5103")]
		public void OrderByDistinctAll2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Person.OrderBy(p => p.FirstName)
				.Distinct()
				.Select(r => new { r.ID, r.LastName, r.FirstName })
				.Skip(1)
				.Take(2);

			AssertQuery(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5626")]
		public void OrderByDistinctTakeOrdering([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			// OrderBy by an expression (not a literal Distinct output column) followed by Distinct().Take(...):
			// the ORDER BY must survive because the Take observes which rows it keeps. With OrderData1 * 100 + Id
			// descending the unique top-3 rows are Ids 600 (1100), 500 (900) and 400 (700) - dropping the ORDER BY
			// makes the Take return arbitrary rows instead.
			// Asserted against explicit values rather than AssertQuery: AssertQuery's in-memory baseline is derived
			// from linq2db's own exposed expression, which drops the same ORDER BY and would mask the bug.
			var result = table
				.Concat(table)
				.OrderByDescending(x => x.OrderData1 * 100 + x.Id)
				.Distinct()
				.Take(3)
				.Select(x => x.Id)
				.ToArray();

			Assert.That(result, Is.EqualTo(new[] { 600, 500, 400 }));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5626")]
		public void OrderByGroupByTakeOrdering([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			// GROUP BY analog of the Distinct case: OrderBy by an expression built from the grouping keys, then
			// GroupBy (with an aggregate so the GROUP BY survives) + Take. The ORDER BY must survive above the
			// GROUP BY because the Take observes it. Distinct (OrderData1, OrderData2) pairs ordered descending by
			// OrderData1 * 100 + OrderData2 give 505, 404, 303 as the top three.
			var result = table
				.OrderByDescending(x => x.OrderData1 * 100 + x.OrderData2)
				.GroupBy(x => new { x.OrderData1, x.OrderData2 })
				.Select(g => new { Key = g.Key.OrderData1 * 100 + g.Key.OrderData2, Count = g.Count() })
				.Take(3)
				.ToArray();

			Assert.That(result.Select(x => x.Key).ToArray(), Is.EqualTo(new[] { 505, 404, 303 }));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5626")]
		public void OrderByGroupByTakeOrderingKeyNotProjected([DataSources] string context)
		{
			var testData = GetTestData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			// Variant of OrderByGroupByTakeOrdering where the ORDER BY expression (OrderData1 * 100 + OrderData2)
			// is NOT present verbatim in the SELECT projection - only the OrderData1 grouping key is projected.
			// This exercises the new grouping-key path (AllOrderColumnsAreGroupingKeys) unambiguously, since the
			// pre-existing exact-column disjunct cannot match an order expression absent from the projection.
			// Distinct (OrderData1, OrderData2) pairs ordered descending by OrderData1 * 100 + OrderData2 give
			// (5,5)=505, (4,4)=404, (3,3)=303 as the top three, so the projected OrderData1 values are 5, 4, 3.
			var result = table
				.OrderByDescending(x => x.OrderData1 * 100 + x.OrderData2)
				.GroupBy(x => new { x.OrderData1, x.OrderData2 })
				.Select(g => g.Key.OrderData1)
				.Take(3)
				.ToArray();

			Assert.That(result, Is.EqualTo(new[] { 5, 4, 3 }));
		}
	}
}
