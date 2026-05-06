using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
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

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
						Values = Sql.ConcatStringsNullable(" -> ", g) ?? nullVal,
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void AggregationOrderTest([IncludeDataSources(TestProvName.AllSqlServer2017Plus, TestProvName.AllClickHouse)] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						g.Key.Id,
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").OrderBy(e => e).ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					orderby g.Key.Id
					select new
					{
						g.Key.Id,
						Max = g.Max(),
						Values = Sql.ConcatStringsNullable(" -> ", g.OrderBy(e => e)) ?? nullVal,
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void AggregationOrderDescTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max    = g.Max(),
						Values = g.StringAggregate(" -> ").OrderByDescending(e => e).ToValue(),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max    = g.Max(),
						Values = Sql.ConcatStringsNullable(" -> ", g.OrderByDescending(e => e)),
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void AggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
						Values = Sql.ConcatStringsNullable(" -> ", g.Select(e => e.Value1)) ?? nullVal,
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void AggregationOrderedSelectorTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
						Values     = Sql.ConcatStringsNullable(" -> ", g.OrderBy(e => e.Value3).ThenByDescending(e => e.Value1).Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void AggregationOrderedDescSelectorTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
						Values     = Sql.ConcatStringsNullable(" -> ", g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value1).Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}

		[Test]
		public void FinalAggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual    = table.Select(t => t.Value1).StringAggregate(" -> ").ToValue();
				var expected1 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1));
				var expected2 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}

		[Test]
		public void FinalAggregationOrderedTest([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actualAsc   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderBy().ToValue();
				var expectedAsc = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).OrderBy(d => d));
				Assert.That(actualAsc, Is.EqualTo(expectedAsc));

				var actualAscExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderBy(d => d).ToValue();
				var expectedAscExpr = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).OrderBy(d => d));
				Assert.That(actualAscExpr, Is.EqualTo(expectedAscExpr));

				var actualDesc   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending().ToValue();
				var expectedDesc = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.That(actualDesc, Is.EqualTo(expectedDesc));

				var actualDescExpr   = table.Select(t => t.Value1).StringAggregate(" -> ").OrderByDescending(d => d).ToValue();
				var expectedDescExpr = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).OrderByDescending(d => d));
				Assert.That(actualDescExpr, Is.EqualTo(expectedDescExpr));
			}

		[Test]
		public void FinalAggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual    = table.AsQueryable().StringAggregate(" -> ", t => t.Value1).ToValue();
				var expected1 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1));
				var expected2 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value1).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}

		[Test]
		public void FinalAggregationSubqueryTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Count, Is.EqualTo(2));
					Assert.That(result[1].Count, Is.EqualTo(2));
					Assert.That(result[2].Count, Is.EqualTo(2));

					Assert.That(result[0].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
					Assert.That(result[1].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
					Assert.That(result[2].Aggregated, Is.EqualTo("V1 -> Z1").Or.EqualTo("Z1 -> V1"));
				}
			}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4501")]
		public void Issue4501Test([StringTestSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = table
				.GroupBy(x => x.Id)
				.Select(g => new
				{
					Id = g.Key,
					AggregatedDescription = g.StringAggregate(", ", x => x.Value1).ToValue()
				});

			query.ToList();
		}

		[Test]
		public void ConcatStringsTest([DataSources(TestProvName.AllOracle, TestProvName.AllSybase)] string context)
		{
			var data = GenerateData().OrderBy(_ => _.Id);

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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

		[Test]
		public void StringPlusNullOperands([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			if (value1Nullable && value2Nullable && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase cannot represent C# empty string result for null + null string concatenation.");

			var data = new []
				{
					new { ID = 1, Value1 = (string?)"A1", Value2 = (string?)"A2" },
					new { ID = 2, Value1 = (string?)null, Value2 = (string?)"B2" },
					new { ID = 3, Value1 = (string?)"C1", Value2 = (string?)null },
					new { ID = 4, Value1 = (string?)null, Value2 = (string?)null }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || t.Value2 != null))
				.ToArray();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
				$"StringPlusNullOperands_{Guid.NewGuid():N}",
				data,
				mb => mb
					.Property(t => t.ID)
						.IsPrimaryKey()
					.Property(t => t.Value1)
						.HasLength(50)
						.IsNullable(value1Nullable)
					.Property(t => t.Value2)
						.HasLength(50)
						.IsNullable(value2Nullable));

			var query = table
				.OrderBy(t => t.ID)
				.Select(t => t.Value1 + t.Value2);

			AssertQuery(query);

			if (context is ProviderName.SQLiteMS or ProviderName.SQLiteClassic)
			{
				var expectedCoalesceCount = (value1Nullable ? 1 : 0) + (value2Nullable ? 1 : 0);
				var sql                   = ((TestDataConnection)db).LastQuery!;
				var coalesceCount         = sql.Split(["Coalesce("], StringSplitOptions.None).Length - 1;

				Assert.That(coalesceCount, Is.EqualTo(expectedCoalesceCount), sql);
			}
		}

		[Test]
		public void StringPlusIntNullOperands([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			if (value1Nullable && value2Nullable && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase cannot represent C# empty string result for null + null string concatenation.");

			var data = new []
				{
					new { ID = 1, Value1 = (string?)"A", Value2 = (int?)1 },
					new { ID = 2, Value1 = (string?)null, Value2 = (int?)2 },
					new { ID = 3, Value1 = (string?)"C", Value2 = (int?)null },
					new { ID = 4, Value1 = (string?)null, Value2 = (int?)null }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || t.Value2 != null))
				.ToArray();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
				$"StringPlusIntNullOperands_{Guid.NewGuid():N}",
				data,
				mb => mb
					.Property(t => t.ID)
						.IsPrimaryKey()
					.Property(t => t.Value1)
						.HasLength(50)
						.IsNullable(value1Nullable)
					.Property(t => t.Value2)
						.IsNullable(value2Nullable));

			var query = table
				.OrderBy(t => t.ID)
				.Select(t => t.Value1 + t.Value2);

			AssertQuery(query);
		}

		[Test]
		public void StringPlusNullOperandsCompareNulls(
			[IncludeDataSources(false, ProviderName.SQLiteMS, ProviderName.SqlServer2025)] string context,
			[Values] CompareNulls compareNulls)
		{
			var data = new []
			{
				new { ID = 1, Value1 = (string?)"A1", Value2 = (string?)"A2" },
				new { ID = 2, Value1 = (string?)null, Value2 = (string?)"B2" },
				new { ID = 3, Value1 = (string?)"C1", Value2 = (string?)null },
				new { ID = 4, Value1 = (string?)null, Value2 = (string?)null }
			};

			using var db    = GetDataContext(context, o => o.UseCompareNulls(compareNulls));
			using var table = db.CreateLocalTable(
				$"StringPlusNullOperandsCompareNulls_{Guid.NewGuid():N}",
				data,
				mb => mb
					.Property(t => t.ID)
						.IsPrimaryKey()
					.Property(t => t.Value1)
						.HasLength(50)
						.IsNullable()
					.Property(t => t.Value2)
						.HasLength(50)
						.IsNullable());

			var actual = table
				.OrderBy(t => t.ID)
				.Select(t => t.Value1 + t.Value2);

			var expected = data
				.OrderBy(t => t.ID)
				.Select(t => compareNulls == CompareNulls.LikeClr || t.Value1 != null && t.Value2 != null ? t.Value1 + t.Value2 : null);

			AreEqual(expected, actual);

			var expectedCoalesceCount = compareNulls == CompareNulls.LikeClr ? 2 : 0;
			var sql                   = ((TestDataConnection)db).LastQuery!;
			var coalesceCount         = sql.Split(["Coalesce("], StringSplitOptions.None).Length - 1;

			Assert.That(coalesceCount, Is.EqualTo(expectedCoalesceCount), sql);
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

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual    = table.Select(t => t.Value4).StringAggregate(" -> ").ToValue();
				var expected1 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value4));
				var expected2 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value4).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}

		[Test]
		public void Issue1765TestLiteral2([StringTestSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
				var actual    = table.AsQueryable().StringAggregate(" -> ", t => t.Value4).ToValue();
				var expected1 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value4));
				var expected2 = Sql.ConcatStringsNullable(" -> ", data.Select(t => t.Value4).Reverse());

				// as we don't order aggregation, we should expect unstable results
				Assert.That(expected1 == actual || expected2 == actual, Is.True, $"Expected '{expected1}' or '{expected2}' but got '{actual}'");
			}

		[Test]
		public void Issue1765TestLiteral3([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
								   Values = Sql.ConcatStringsNullable(" -> ", g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
							   };

				AreEqual(expected, actual);
			}

		[Test]
		public void Issue1765TestLiteral4([StringTestSources] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
								   Values = Sql.ConcatStringsNullable(" -> ", g) ?? nullVal,
							   };

				AreEqual(expected, actual);
			}

		[Test]
		public void Issue1765TestParameter3([StringTestOrderSources] string context, [Values(" -> ", " => ")] string separator)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
								   Values = Sql.ConcatStringsNullable(separator, g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
							   };

				AreEqual(expected, actual);
			}

		[Test]
		public void Issue1765TestParameter4([StringTestSources] string context, [Values(" -> ", " => ")] string separator)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
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
								   Values = Sql.ConcatStringsNullable(separator, g) ?? nullVal,
							   };

				AreEqual(expected, actual);
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

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Issue1916Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var cnt = db.Person.Where(p => string.Concat(p.FirstName, p.MiddleName) != null).Count();

			Assert.That(cnt, Is.EqualTo(4));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Issue1916Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var cnt = db.Person.Where(p => Sql.Concat(p.FirstName, p.MiddleName) != null).Count();

			Assert.That(cnt, Is.EqualTo(4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4597")]
		public void Issue4597Test([DataSources(
			ProviderName.SqlCe,
			ProviderName.Ydb,
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase,
			TestProvName.AllSqlServer2016Minus,
			TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			db.Parent
				.Select(s => s.Children.StringAggregate(", ", l => l.ChildID.ToString()).ToValue())
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5173")]
		public void Issue5173_ParameterLocation([StringTestSources] string provider)
		{
			using var db = GetDataContext(provider);

			const string separator = ";";
			List<int> channels = [11, 13];

			var query = from ti in db.Parent
						from ch in channels.AsQueryable()
						from m in db.Child
						.LeftJoin(i => i.ParentID == ti.ParentID && i.ChildID == ch)
						orderby ti.ParentID
						group new
						{
							ch,
						} by ch % 10
						into grp
						select new
						{
							Value = grp.StringAggregate(separator,i => Sql.Concat("test:", i.ch)).ToValue()
						};

			_ = query.ToArray();
		}
	}
}
