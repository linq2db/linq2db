using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
		static string? AggregateStrings(string separator, IEnumerable<string?> arguments)
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
		sealed class SampleClass
		{
			[Column]                                                              public int     Id     { get; set; }
			[Column(Length = 50, CanBeNull = true)]                               public string? Value1 { get; set; }
			[Column(Length = 50, CanBeNull = true)]                               public string? Value2 { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.VarChar)]  public string? Value3 { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NVarChar)] public string? Value4 { get; set; }
		}

		public class StringTestSourcesAttribute : IncludeDataSourcesAttribute
		{
			public StringTestSourcesAttribute(bool includeLinqService = true) : base(includeLinqService,
				TestProvName.AllSqlServer2017Plus,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllClickHouse,
				TestProvName.AllSapHana,
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
				TestProvName.AllSapHana,
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

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g) ?? nullVal,
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationOrderTest([IncludeDataSources(TestProvName.AllSqlServer2017Plus, TestProvName.AllClickHouse)] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").OrderBy(e => e).ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g.OrderBy(e => e)) ?? nullVal,
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

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Values = g.StringAggregate(" -> ", e => e.Value1).ToValue(),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						Values = AggregateStrings(" -> ", g.Select(e => e.Value1)) ?? nullVal,
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
				var actual    = table.Select(t => t.Value1).StringAggregate(" -> ").ToValue();
				var expected1 = AggregateStrings(" -> ", data.Select(t => t.Value1));
				var expected2 = AggregateStrings(" -> ", data.Select(t => t.Value1).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
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
				Assert.That(actualAsc, Is.EqualTo(expectedAsc));

				var actualAscExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderBy(d => d).ToValue();
				var expectedAscExpr = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderBy(d => d));
				Assert.That(actualAscExpr, Is.EqualTo(expectedAscExpr));

				var actualDesc   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending().ToValue();
				var expectedDesc = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.That(actualDesc, Is.EqualTo(expectedDesc));

				var actualDescExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending(d => d).ToValue();
				var expectedDescExpr = AggregateStrings(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.That(actualDescExpr, Is.EqualTo(expectedDescExpr));
			}
		}

		[Test]
		public void FinalAggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual    = table.AsQueryable().StringAggregate(" -> ", t => t.Value1).ToValue();
				var expected1 = AggregateStrings(" -> ", data.Select(t => t.Value1));
				var expected2 = AggregateStrings(" -> ", data.Select(t => t.Value1).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}
		}

		[Test]
		public void FinalAggregationSubqueryTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = from t in table
					select new
					{
						Count      = table.CountExt(e => e.Value1, Sql.AggregateModifier.Distinct),
						Aggregated = table.StringAggregate(" -> ", x => x.Value1).ToValue()
					};

				var expected = from t in data
					select new
					{
						Count      = data.Count(x => x.Value1 != null),
						Aggregated = string.Join(" -> ", data.Where(x => x.Value1 != null).Select(x => x.Value1))
					};

				// not usable due to lack of aggreation order
				//AreEqual(expected, query);

				var result = query.ToArray();
				Assert.That(result, Has.Length.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].Count, Is.EqualTo(2));
					Assert.That(result[1].Count, Is.EqualTo(2));
					Assert.That(result[2].Count, Is.EqualTo(2));

					Assert.That(result[0].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
				});
				Assert.Multiple(() =>
				{
					Assert.That(result[1].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
					Assert.That(result[2].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
				});
			}
		}

		[Test]
		public void ConcatStringsTest([
			IncludeDataSources(
				TestProvName.AllSqlServer,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllClickHouse,
				TestProvName.AllSQLite
			)] string context)
		{
			var data = GenerateData().OrderBy(_ => _.Id);

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = table.OrderBy(_ => _.Id);

				var actualOne   = query.Select(t => Sql.ConcatStrings(" -> ", t.Value2));
				var expectedOne = data .Select(t => Sql.ConcatStrings(" -> ", t.Value2));

				Assert.That(actualOne, Is.EqualTo(expectedOne));

				var actualOneNull   = query.Select(t => Sql.ConcatStrings(" -> ", t.Value3));
				var expectedOneNull = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3));

				Assert.That(actualOneNull, Is.EqualTo(expectedOneNull));

				var actual   = query.Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value1, t.Value2));
				var expected = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value1, t.Value2));

				Assert.That(actual, Is.EqualTo(expected));

				var actualAllEmpty   = query.Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value3));
				var expectedAllEmpty = data .Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value3));

				Assert.That(actualAllEmpty, Is.EqualTo(expectedAllEmpty));
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
		public void Issue1765TestLiteral1([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual    = table.Select(t => t.Value4).StringAggregate(" -> ").ToValue();
				var expected1 = AggregateStrings(" -> ", data.Select(t => t.Value4));
				var expected2 = AggregateStrings(" -> ", data.Select(t => t.Value4).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}
		}

		[Test]
		public void Issue1765TestLiteral2([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual    = table.AsQueryable().StringAggregate(" -> ", t => t.Value4).ToValue();
				var expected1 = AggregateStrings(" -> ", data.Select(t => t.Value4));
				var expected2 = AggregateStrings(" -> ", data.Select(t => t.Value4).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}
		}

		[Test]
		public void Issue1765TestLiteral3([StringTestOrderSources] string context)
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
		public void Issue1765TestLiteral4([StringTestSources] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
							 group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							 orderby g.Key.Id
							 select new
							 {
								 Max = g.Max(),
								 Values = g.StringAggregate(" -> ").ToValue(),
							 };

				var expected = from t in data
							   group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							   orderby g.Key.Id
							   select new
							   {
								   Max = g.Max(),
								   Values = AggregateStrings(" -> ", g) ?? nullVal,
							   };

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void Issue1765TestParameter3([StringTestOrderSources] string context, [Values(" -> ", " => ")] string separator)
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
								 Values = g.StringAggregate(separator, e => e.Value4).OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).ToValue(),
							 };

				var expected = from t in data
							   group t by new { t.Id, Value = t.Value4 }
					into g
							   select new
							   {
								   Values = AggregateStrings(separator, g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
							   };

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void Issue1765TestParameter4([StringTestSources] string context, [Values(" -> ", " => ")] string separator)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
							 group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							 orderby g.Key.Id
							 select new
							 {
								 Max = g.Max(),
								 Values = g.StringAggregate(separator).ToValue(),
							 };

				var expected = from t in data
							   group t.Value4 by new { t.Id, Value = t.Value4 }
					into g
							   orderby g.Key.Id
							   select new
							   {
								   Max = g.Max(),
								   Values = AggregateStrings(separator, g) ?? nullVal,
							   };

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void MySqlConcatStringsTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			_ = (from p in db.Person where p.FirstName == ("A" + "B") select p).ToList();
			Assert.That(db.LastQuery, Contains.Substring("AB"));

			//var str = "C";

			_ = (from p in db.Person where p.FirstName == "A" + p.FirstName + "B" select p).ToList();
			Assert.That(db.LastQuery, Contains.Substring("Concat('A', `p`.`FirstName`, 'B')"));
		}
	}
}
