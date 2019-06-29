using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
		static string AggregateStrings(string separator, IEnumerable<string> arguments)
		{
			var result = arguments.Aggregate((v1, v2) =>
			{
				if (v1 == null && v2 == null)
					return null;
				if (v1 == null)
					return v2;
				if (v2 == null)
					return v1;
				return v1 + separator + v2;
			});
			return result;
		}

		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value1 { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value2 { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value3 { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.VarChar)]
			                                        public string Value4 { get; set; }
		}

		public class StringTestSourcesAttribute : IncludeDataSourcesAttribute
		{
			public StringTestSourcesAttribute(bool includeLinqService = true) : base(includeLinqService,
				TestProvName.AllSqlServer2017Plus,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				ProviderName.SapHana,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				ProviderName.DB2,
				TestProvName.AllFirebird)
			{
			}
		}

		public class StringTestOrderSourcesAttribute : IncludeDataSourcesAttribute
		{
			public StringTestOrderSourcesAttribute(bool includeLinqService = true) : base(includeLinqService,
				TestProvName.AllSqlServer2017Plus,
				TestProvName.AllPostgreSQL,
				ProviderName.SapHana,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				ProviderName.DB2)
			{
			}
		}

		[Test]
		public void AggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationOrderTest([IncludeDataSources(ProviderName.SqlServer2017)] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").OrderBy(e => e).ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g.OrderBy(e => e)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationOrderDescTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").OrderByDescending(e => e).ToValue(),
					};


				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g.OrderByDescending(e => e)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = g.StringAggregate(" -> ", e => e.Value1).ToValue(),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = AggregateStrings(" -> ", g.Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationOrderedSelectorTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values     = g.StringAggregate(" -> ", e => e.Value1).OrderBy(e => e.Value3).ThenByDescending(e => e.Value1).ToValue(),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values     = AggregateStrings(" -> ", g.OrderBy(e => e.Value3).ThenByDescending(e => e.Value1).Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationOrderedDescSelectorTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values     = g.StringAggregate(" -> ", e => e.Value1).OrderByDescending(e => e.Value3).ThenBy(e => e.Value1).ToValue(),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values     = AggregateStrings(" -> ", g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value1).Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual   = table.Select(t => t.Value1).StringAggregate(" -> ").ToValue();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value1));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationOrderedTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actualAsc   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderBy().ToValue();
				var expectedAsc = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderBy(d => d));
				Assert.AreEqual(expectedAsc, actualAsc);

				var actualAscExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderBy(d => d).ToValue();
				var expectedAscExpr = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderBy(d => d));
				Assert.AreEqual(expectedAscExpr, actualAscExpr);

				var actualDesc   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending().ToValue();
				var expectedDesc = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.AreEqual(expectedDesc, actualDesc);

				var actualDescExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending(d => d).ToValue();
				var expectedDescExpr = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.AreEqual(expectedDescExpr, actualDescExpr);
			}
		}

		[Test]
		public void FinalAggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual   = table.AsQueryable().StringAggregate(" -> ", t => t.Value1).ToValue();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value1));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void ConcatStringsTest([
			IncludeDataSources(
				TestProvName.AllSqlServer,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSQLite
			)] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actualOne   = table.Select(t => Sql.ConcatStrings(" -> ", t.Value2));
				var expectedOne = data .Select(t => Sql.ConcatStrings(" -> ", t.Value2));

				Assert.AreEqual(expectedOne, actualOne);

				var actualOneNull   = table.Select(t => Sql.ConcatStrings(" -> ", t.Value3));
				var expectedOneNull = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3));

				Assert.AreEqual(expectedOneNull, actualOneNull);

				var actual   = table.Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value1, t.Value2));
				var expected = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value1, t.Value2));

				Assert.AreEqual(expected, actual);

				var actualAllEmpty   = table.Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value3));
				var expectedAllEmpty = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value3));

				Assert.AreEqual(expectedAllEmpty, actualAllEmpty);
			}
		}

		private static SampleClass[] GenerateData()
		{
			var data = new[]
			{
				new SampleClass { Id = 1, Value1 = "V1", Value2 = "V2", Value4 = "V4" },
				new SampleClass { Id = 2, Value1 = null, Value2 = "Z2", Value4 = null },
				new SampleClass { Id = 3, Value1 = "Z1", Value2 = null, Value4 = "Z4" }
			};
			return data;
		}

		[Test]
		public void Issue1765Test1([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = table.Select(t => t.Value4).StringAggregate(" -> ").ToValue();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value4));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void Issue1765Test2([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = table.AsQueryable().StringAggregate(" -> ", t => t.Value4).ToValue();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value4));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void Issue1765Test3([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
							 group t by new { t.Id, Value = t.Value4 }
					into g
							 select new
							 {
								 Values = g.StringAggregate(" -> ", e => e.Value4).OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).ToValue(),
							 };

				var expected = from t in data
							   group t by new { t.Id, Value = t.Value4 }
					into g
							   select new
							   {
								   Values = AggregateStrings(" -> ", g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
							   };

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void Issue1765Test4([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
							 group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							 select new
							 {
								 Max = g.Max(),
								 Values = g.StringAggregate(" -> ").ToValue(),
							 };

				var expected = from t in data
							   group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							   select new
							   {
								   Max = g.Max(),
								   Values = AggregateStrings(" -> ", g),
							   };

				AreEqual(expected, actual);
			}
		}
	}
}
