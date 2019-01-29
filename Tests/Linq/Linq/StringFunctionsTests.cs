using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column(Length = 50)] public string Value1 { get; set; }
			[Column(Length = 50)] public string Value2 { get; set; }
		}

		public class StringTestSourcesAttribute : IncludeDataSourcesAttribute
		{
			public StringTestSourcesAttribute(bool includeLinqService = true) : base(includeLinqService, 
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95,
				ProviderName.MySql
				)
			{
			}
		}

		[Test]
		public void AggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(context, "AggregationTest", data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> "),
					};

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> "),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(context, "AggregationSelectorTest", data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = g.StringAggregate(" -> ", e => e.Value1),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = g.StringAggregate(" -> ", e => e.Value1),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(context, "FinalAggregationTest", data))
			{
				var actual   = table.Select(t => t.Value1).StringAggregate(" -> ");
				var expected = data .Select(t => t.Value1).StringAggregate(" -> ");
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(context, "FinalAggregationSelectorTest", data))
			{
				var actual   = table.AsQueryable().StringAggregate(" -> ", t => t.Value1);
				var expected = data .StringAggregate(" -> ", t => t.Value1);
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void ConcatWSTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(context, "ConcatWSTest", data))
			{
				var actual   = table.Select(t => Sql.ConcatWS(" -> ", t.Value1, t.Value2));
				var expected = data .Select(t => Sql.ConcatWS(" -> ", t.Value1, t.Value2));
				Assert.AreEqual(expected, actual);
			}
		}
		private static SampleClass[] GenerateData()
		{
			var data = new[]
			{
				new SampleClass { Id = 1, Value1 = "V1", Value2 = "V2"},
				new SampleClass { Id = 2, Value1 = "Z1", Value2 = "Z2"}
			};
			return data;
		}
	}
}
