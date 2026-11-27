using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[PrimaryKey]                                                          public int     Id     { get; set; }
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
				TestProvName.AllFirebird,
				TestProvName.AllDuckDB)
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
				ProviderName.DB2,
				TestProvName.AllDuckDB)
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
			var actual =
				from t in table
				group t.Value1 by new {t.Id, Value = t.Value1}
				into g
				orderby g.Key.Id
				select new
				{
					Max = g.Max(),
					Values = g.StringAggregate(" -> ").ToValue(),
				};

			var expected =
				from t in data
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
			var actual =
				from t in table
				group t.Value1 by new {t.Id, Value = t.Value1}
				into g
				orderby g.Key.Id
				select new
				{
					g.Key.Id,
					Max = g.Max(),
					Values = g.StringAggregate(" -> ").OrderBy(e => e).ToValue(),
				};

			var expected =
				from t in data
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
			var actual =
				from t in table
				group t.Value1 by new {t.Id, Value = t.Value1}
				into g
				select new
				{
					Max    = g.Max(),
					Values = g.StringAggregate(" -> ").OrderByDescending(e => e).ToValue(),
				};

			var expected =
				from t in data
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
			var actual =
				from t in table
				group t by new {t.Id, Value = t.Value1}
				into g
				orderby g.Key.Id
				select new
				{
					Values = g.StringAggregate(" -> ", e => e.Value1).ToValue(),
				};

			var expected =
				from t in data
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
			var actual =
				from t in table
				group t by new {t.Id, Value = t.Value1}
				into g
				select new
				{
					Values     = g.StringAggregate(" -> ", e => e.Value1).OrderBy(e => e.Value3).ThenByDescending(e => e.Value1).ToValue(),
				};

			var expected =
				from t in data
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
			var actual =
				from t in table
				group t by new {t.Id, Value = t.Value1}
				into g
				select new
				{
					Values     = g.StringAggregate(" -> ", e => e.Value1).OrderByDescending(e => e.Value3).ThenBy(e => e.Value1).ToValue(),
				};

			var expected =
				from t in data
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

		static SampleClass[] GenerateNullsOrderData() =>
		[
			new SampleClass { Id = 1, Value1 = "b",  Value2 = "B" },
			new SampleClass { Id = 2, Value1 = null, Value2 = "N" },
			new SampleClass { Id = 3, Value1 = "a",  Value2 = "A" },
		];

		[Test]
		public void AggregationOrderByNulls(
			[IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL, TestProvName.AllSqlServer2017Plus, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.DB2, TestProvName.AllDuckDB)] string context,
			[Values(Sql.NullsPosition.First, Sql.NullsPosition.Last)] Sql.NullsPosition nulls,
			[Values] bool descending)
		{
			var data = GenerateNullsOrderData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			// Order by a nullable column (Value1), aggregate a non-null column (Value2) so the NULLS placement is observable.
			var ordered = descending
				? table.OrderByDescending(x => x.Value1, nulls)
				: table.OrderBy          (x => x.Value1, nulls);

			var actual = ordered.Select(x => x.Value2).StringAggregate(" -> ").ToValue();

			var grouped = nulls == Sql.NullsPosition.First
				? data.OrderBy(d => d.Value1 != null)   // null group first
				: data.OrderBy(d => d.Value1 == null);  // null group last
			var expectedSeq = descending ? grouped.ThenByDescending(d => d.Value1) : grouped.ThenBy(d => d.Value1);
			var expected    = Sql.ConcatStringsNullable(" -> ", expectedSeq.Select(d => d.Value2));

			actual.ShouldBe(expected);
		}

		[Test]
		public void AggregationDefaultNullsPosition(
			[IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL, TestProvName.AllSqlServer2017Plus, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.DB2, TestProvName.AllDuckDB)] string context)
		{
			var data = GenerateNullsOrderData();

			// The configured default must apply to a plain (no explicit position) aggregate OrderBy.
			using var db    = GetDataContext(context, o => o.UseDefaultNullsPosition(Sql.NullsPosition.Last));
			using var table = db.CreateLocalTable(data);

			var byDefault  = table.OrderBy(x => x.Value1).Select(x => x.Value2).StringAggregate(" -> ").ToValue();
			var byExplicit = table.OrderBy(x => x.Value1, Sql.NullsPosition.Last).Select(x => x.Value2).StringAggregate(" -> ").ToValue();

			byDefault.ShouldBe(byExplicit);
		}

		[Test]
		public void AggregationNonNullableNullsNoop(
			[IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL, TestProvName.AllSqlServer2017Plus, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.DB2, TestProvName.AllDuckDB)] string context)
		{
			var data = GenerateNullsOrderData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			// Id is non-nullable, so the NULLS position has no effect on the result — First == Last. The nullability
			// gate drops the position to None for the SQL; some native providers (PostgreSQL, SapHana, DuckDB) still
			// render a harmless NULLS token because their aggregate ordering pins a default null placement, but with
			// no NULLs in a non-nullable key it cannot change the outcome.
			var asFirst = table.OrderBy(x => x.Id, Sql.NullsPosition.First).Select(x => x.Value2).StringAggregate(" -> ").ToValue();
			var asLast  = table.OrderBy(x => x.Id, Sql.NullsPosition.Last).Select(x => x.Value2).StringAggregate(" -> ").ToValue();

			asFirst.ShouldBe(asLast);
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
			var query =
				from t in table
				select new
				{
					Count      = table.CountExt(e => e.Value1, Sql.AggregateModifier.Distinct),
					Aggregated = table.StringAggregate(" -> ", x => x.Value1).ToValue()
				};

			var expected =
				from t in data
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
		public void StringAggregateInGroupBySelect([StringTestSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateLiteralSeparatorOverSelect([StringTestSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateLiteralSeparatorWithSelector([StringTestSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateLiteralSeparatorOrdered([StringTestOrderSources] string context)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual =
				from t in table
				group t by new { t.Id, Value = t.Value4 }
				into g
				select new
				{
					Values = g.StringAggregate(" -> ", e => e.Value4).OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).ToValue(),
				};

			var expected =
				from t in data
				group t by new { t.Id, Value = t.Value4 }
				into g
				select new
				{
					Values = Sql.ConcatStringsNullable(" -> ", g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
				};

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateLiteralSeparatorGroupedValues([StringTestSources] string context)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual =
				from t in table
				group t.Value4 by new { t.Id, Value = t.Value4 }
				into g
				orderby g.Key.Id
				select new
				{
					Max = g.Max(),
					Values = g.StringAggregate(" -> ").ToValue(),
				};

			var expected =
				from t in data
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateParameterSeparatorOrdered([StringTestOrderSources] string context, [Values(" -> ", " => ")] string separator)
		{
			var data = GenerateData();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual =
				from t in table
				group t by new { t.Id, Value = t.Value4 }
				into g
				select new
				{
					Values = g.StringAggregate(separator, e => e.Value4).OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).ToValue(),
				};

			var expected =
				from t in data
				group t by new { t.Id, Value = t.Value4 }
				into g
				select new
				{
					Values = Sql.ConcatStringsNullable(separator, g.OrderByDescending(e => e.Value3).ThenBy(e => e.Value4).Select(e => e.Value4)),
				};

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1765")]
		public void StringAggregateParameterSeparatorGroupedValues([StringTestSources] string context, [Values(" -> ", " => ")] string separator)
		{
			var data = GenerateData();

			// https://github.com/ClickHouse/ClickHouse/issues/29978
			// if it changes, CanBeNull = false should be removed from mappings
			var nullVal = context.IsAnyOf(TestProvName.AllClickHouse) ? string.Empty : null;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual =
				from t in table
				group t.Value4 by new { t.Id, Value = t.Value4 }
				into g
				orderby g.Key.Id
				select new
				{
					Max = g.Max(),
					Values = g.StringAggregate(separator).ToValue(),
				};

			var expected =
				from t in data
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
			Assert.That(db.LastQuery, Contains.Substring("CONCAT('A', `p`.`FirstName`, 'B')"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4597")]
		public void StringAggregateOverAssociationInSelect([DataSources(
			ProviderName.SqlCe,
			TestProvName.AllYdb,
			TestProvName.AllNativeAccess,
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
		public void StringAggregateSeparatorParameterLocation([StringTestSources] string provider)
		{
			using var db = GetDataContext(provider);

			const string separator = ";";
			List<int> channels = [11, 13];

			var query =
				from ti in db.Parent
				from ch in channels.AsQueryable()
				from m in db.Child.LeftJoin(i => i.ParentID == ti.ParentID && i.ChildID == ch)
				orderby ti.ParentID
				group new { ch } by ch % 10
				into grp
				select new
				{
					Value = grp.StringAggregate(separator, i => Sql.Concat("test:", i.ch)).ToValue()
				};

			_ = query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1977")]
		public void ConcatStringsWithGuidConversionMatchesOperatorConcat([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(GuidConcatRow.TestData);
			var itemsQuery = table
				.Select
				(
					f => new
					{
						oldStr = nameof(GuidConcatRow)
						 + "/" + f.firstField
						 + "/" + f.secondField,
						newStr = Sql.AsSql(Sql.ConcatStrings("/",
							nameof(GuidConcatRow),
							Sql.Convert<string, Guid>(f.firstField),
							Sql.Convert<string, Guid>(f.secondField)))
					}
				)
				.Select
				(
					f => new
					{
						equals = Sql.AsSql(f.oldStr == f.newStr)
					}
				);

			Assert.That(itemsQuery.ToArray().All(r => r.equals), Is.True);
		}

		public class GuidConcatRow
		{
			public Guid firstField;
			public Guid secondField;

			public static GuidConcatRow[] TestData { get; }
				= new[]
				{
					new GuidConcatRow()
					{
						firstField  = global::Tests.TestData.Guid1,
						secondField = global::Tests.TestData.Guid2
					}
				};
		}

		[Sql.Expression(ProviderName.SqlServer, "ISNULL({0}, {1})", ServerSideOnly = true)]
		private static T ServerSideIsNull<T>(T value, T defaultValue)
		{
			throw new ServerSideOnlyException(nameof(ServerSideIsNull));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3257")]
		public void StringAggregateInConditionalProjection([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = x.ChecklistTriggers.Any()
							? x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						: "None"
					});

				var good = query.ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3257")]
		public void StringAggregateInConditionalProjectionFilteredByContains([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = x.ChecklistTriggers.Any()
							? x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						: "None"
					});

				var bad = query.Where(x => x.Triggers.Contains("H")).ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3257")]
		public void StringAggregateWrappedInServerSideIsNull([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = ServerSideIsNull(
							x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						, "None")
					});

				query.ToArray();
			}
		}

		[Table]
		public class Checklist
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChecklistTrigger.ChecklistId))]
			public ICollection<ChecklistTrigger> ChecklistTriggers { get; set; } = null!;
		}

		[Table]
		public class ChecklistTrigger
		{
			[Column] public int         Id          { get; set; }
			[Column] public int         ChecklistId { get; set; }
			[Column] public TriggerType TriggerType { get; set; }
		}

		public enum TriggerType
		{
			Hired      = 1,
			PreHired   = 2,
			Terminated = 3
		}
	}
}
