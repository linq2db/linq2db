using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class PostgreSQLExtensionsTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int     Id           { get; set; }
			[Column] public string  StrValue     { get; set; } = null!;
			[Column] public int     IntValue     { get; set; }
			[Column] public long    LongValue    { get; set; }
			[Column] public double  DoubleValue  { get; set; }
			[Column] public decimal DecimalValue { get; set; }
			[Column(DbType = "text[]")]             public string[]  StrArray     { get; set; } = null!;
			[Column(DbType = "int[]")]              public int[]     IntArray     { get; set; } = null!;
			[Column(DbType = "bigint[]")]           public long[]    LongArray    { get; set; } = null!;
			[Column(DbType = "double precision[]")] public double[]  DoubleArray  { get; set; } = null!;
			[Column(DbType = "numeric[]")]          public decimal[] DecimalArray { get; set; } = null!;

			public static SampleClass[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(i => new SampleClass
					{
						Id = i,
						StrValue = "S" + i,
						IntValue = i,
						LongValue = i,
						DoubleValue = i,
						DecimalValue = i,
						StrArray = Enumerable.Range(i, i).Select(e => $"V{e:00}").ToArray(),
						IntArray = Enumerable.Range(i, i).ToArray(),
						LongArray = Enumerable.Range(i, i).Select(i => (long)i).ToArray(),
						DoubleArray = Enumerable.Range(i, i).Select(i => (double)i).ToArray(),
						DecimalArray = Enumerable.Range(i, i).Select(i => (decimal)i).ToArray(),
					})
					.ToArray();
			}
		}

		[Test]
		public void Unnest([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					from v in db.Unnest(t.StrArray)
					where v.StartsWith("V")
					select v;

				var actual = query.ToArray();

				var expected = from t in testData
					from v in t.StrArray
					where v.StartsWith("V")
					select v;

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void UnnestSubquery([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					select new
					{
						t.Id,
						First = db.Unnest(t.StrArray).FirstOrDefault()
					};

				var actual = query.ToArray();

				var expected = from t in testData
					select new
					{
						t.Id,
						First = t.StrArray.FirstOrDefault()
					};
				
				AreEqual(expected, actual);
			}
		}

		[Test]
		public void UnnestWithOrdinality([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					from v in db.UnnestWithOrdinality(t.StrArray)
					where v.Value.StartsWith("V")
					select v;

				var actual = query.ToArray();

				var expected = from t in testData
					from v in t.StrArray.Select((e, i) => new PostgreSQLExtensions.Ordinality<string>{Index = i + 1, Value = e})
					where v.Value.StartsWith("V")
					select v;

				AreEqualWithComparer(expected, actual);
			}
		}

		[Test]
		public void UnnestWithOrdinalitySubquery([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					select new
					{
						t.Id,
						First = db.UnnestWithOrdinality(t.StrArray).Skip(1).Select(e => e.Index + " - " + e.Value).FirstOrDefault()
					};

				var actual = query.ToArray();

				var expectedQuery = from t in testData
					select new
					{
						t.Id,
						First = t.StrArray.Skip(1).Select((e, i) => i + 2 + " - " + e).FirstOrDefault()
					};

				var expected = expectedQuery.ToArray();
				
				AreEqual(expected, actual);
			}
		}

		[Test]
		public void ArrayAggregateGrouping([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					from v in db.Unnest(t.StrArray)
					group new {v, t} by t.Id / 3 into g
					select new
					{
						Id = g.Key,
						Items = g.ArrayAggregate(e => e.v, Sql.AggregateModifier.All).OrderBy(e => e.t.Id).ToValue(),
						AllItems = g.ArrayAggregate(e => e.v, Sql.AggregateModifier.None).OrderByDescending(e => e.t.Id).ThenBy(e => e.v).ToValue(),
						DistinctItems = g.ArrayAggregate(e => e.v, Sql.AggregateModifier.None).ToValue()
					};
				
				var selectResult = query.ToArray();

				var query2 = from t in table
					from v in db.Unnest(t.StrArray)
					select new { t, v };

				var result1 = query2.ArrayAggregate(e => e.v).ToValue();
				var result2 = query2.ArrayAggregate(e => e.v, Sql.AggregateModifier.Distinct).ToValue();
				var result3 = query2.ArrayAggregate(e => e.v).OrderBy(e => e.v).ToValue();

			}
		}

		[Test]
		public void AnalyticFunctions([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var subQuery = from t in table
					from v in db.Unnest(t.StrArray)
					select new
					{
						Id = t.Id / 3,
						Items1 = Sql.Ext.ArrayAggregate(v).Over().PartitionBy(t.Id / 3, t.Id / 2).ToValue(),
						Items2 = Sql.Ext.ArrayAggregate(v, Sql.AggregateModifier.None).Over().PartitionBy(t.Id / 3, t.Id / 2).ToValue(),
						AllItems = Sql.Ext.ArrayAggregate(v, Sql.AggregateModifier.All).Over().PartitionBy(t.Id / 3).ToValue(),
						ItemsFiltered = Sql.Ext.ArrayAggregate(v, Sql.AggregateModifier.None).Filter(v.StartsWith("V0")).Over().PartitionBy(t.Id / 3).ToValue(),
						AllItemsFiltered = Sql.Ext.ArrayAggregate(v, Sql.AggregateModifier.All).Filter(v.StartsWith("V0")).Over().PartitionBy(t.Id / 3).ToValue(),
						Ordered = Sql.Ext.ArrayAggregate(v, Sql.AggregateModifier.All).Filter(v.StartsWith("V0")).Over().PartitionBy(t.Id / 3).OrderBy(t.Id).ThenBy(t.Id - 1).ToValue(),
						RN = Sql.Ext.RowNumber().Over().PartitionBy(t.Id / 3).OrderBy(t.Id).ToValue()
					};

				var result = subQuery.ToArray();
			}
		}

		[Test]
		public void ArrayFunctions([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)]
			string context)
		{
			var arr = new int [] { 1, 2, 3 };
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t1 in table
					from t2 in table.InnerJoin(t2 => t2.Id != t1.Id)
					where
						   Sql.Ext.PostgreSQL().GreaterThan(t1.StrArray, t2.StrArray)
						|| Sql.Ext.PostgreSQL().GreaterThanOrEqual(t1.StrArray, t2.StrArray)
						|| Sql.Ext.PostgreSQL().Contains(t1.StrArray, t2.StrArray)
						|| Sql.Ext.PostgreSQL().ContainedBy(t1.StrArray, t2.StrArray)
						|| Sql.Ext.PostgreSQL().Overlaps(t1.StrArray, t2.StrArray)

						|| Sql.Ext.PostgreSQL().ValueIsEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanOrEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanOrEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsNotEqualToAny(t1.IntValue, t2.IntArray)

						|| Sql.Ext.PostgreSQL().ValueIsEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanOrEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanOrEqualToAny(t1.IntValue, t2.IntArray)
						|| Sql.Ext.PostgreSQL().ValueIsNotEqualToAny(t1.IntValue, t2.IntArray)

						|| Sql.Ext.PostgreSQL().ValueIsEqualToAny(t1.IntValue, arr)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanAny(t1.IntValue, arr)
						|| Sql.Ext.PostgreSQL().ValueIsLessThanOrEqualToAny(t1.IntValue, arr)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanAny(t1.IntValue, arr)
						|| Sql.Ext.PostgreSQL().ValueIsGreaterThanOrEqualToAny(t1.IntValue, arr)
						|| Sql.Ext.PostgreSQL().ValueIsNotEqualToAny(t1.IntValue, arr)

					select new
					{
						TwoArrays          = Sql.Ext.PostgreSQL().ConcatArrays(t1.StrArray, t2.StrArray),
						ThreeArrays        = Sql.Ext.PostgreSQL().ConcatArrays(t1.StrArray, t2.StrArray, t1.StrArray),
						LessThen           = Sql.Ext.PostgreSQL().LessThan(t1.StrArray, t2.StrArray),
						LessThenOrEqual    = Sql.Ext.PostgreSQL().LessThanOrEqual(t1.StrArray, t2.StrArray),
						GreaterThen        = Sql.Ext.PostgreSQL().GreaterThan(t1.StrArray, t2.StrArray),
						GreaterThanOrEqual = Sql.Ext.PostgreSQL().GreaterThanOrEqual(t1.StrArray, t2.StrArray),
						Contains           = Sql.Ext.PostgreSQL().Contains(t1.StrArray, t2.StrArray),
						ContainedBy        = Sql.Ext.PostgreSQL().ContainedBy(t1.StrArray, t2.StrArray),
						Overlaps           = Sql.Ext.PostgreSQL().Overlaps(t1.StrArray, t2.StrArray),

						ValueEqualToAny              = Sql.Ext.PostgreSQL().ValueIsEqualToAny(t1.IntValue, t2.IntArray),
						ValueLessThanAny             = Sql.Ext.PostgreSQL().ValueIsLessThanAny(t1.IntValue, t2.IntArray),
						ValueLessThanOrEqualToAny    = Sql.Ext.PostgreSQL().ValueIsLessThanOrEqualToAny(t1.IntValue, t2.IntArray),
						ValueGreaterThanAny          = Sql.Ext.PostgreSQL().ValueIsGreaterThanAny(t1.IntValue, t2.IntArray),
						ValueGreaterThanOrEqualToAny = Sql.Ext.PostgreSQL().ValueIsGreaterThanOrEqualToAny(t1.IntValue, t2.IntArray),
						ValueNotEqualToAny           = Sql.Ext.PostgreSQL().ValueIsNotEqualToAny(t1.IntValue, t2.IntArray),

						//TODO: Other types
						ArrayAppendStr     = Sql.Ext.PostgreSQL().ArrayAppend(t1.StrArray, t2.StrValue),
						ArrayAppendInt     = Sql.Ext.PostgreSQL().ArrayAppend(t1.IntArray, t2.Id),
						ArrayAppendLong    = Sql.Ext.PostgreSQL().ArrayAppend(t1.LongArray, t2.LongValue),
						ArrayAppendLong2   = Sql.Ext.PostgreSQL().ArrayAppend(t1.LongArray, Sql.ConvertTo<long>.From(t2.IntValue + 2)),
						ArrayAppendDouble  = Sql.Ext.PostgreSQL().ArrayAppend(t1.DoubleArray, t2.DoubleValue),
						ArrayAppendDecimal = Sql.Ext.PostgreSQL().ArrayAppend(t1.DecimalArray, t2.DecimalValue),

						ArrayCat       = Sql.Ext.PostgreSQL().ArrayCat(t1.StrArray, t2.StrArray),
						ArrayNDims     = Sql.Ext.PostgreSQL().ArrayNDims(t1.StrArray),
						ArrayDims      = Sql.Ext.PostgreSQL().ArrayDims(t1.StrArray),
						Length         = Sql.Ext.PostgreSQL().ArrayLength(t1.StrArray, 1),
						ArrayLower     = Sql.Ext.PostgreSQL().ArrayLower(t1.StrArray, 1),
						ArrayPosition1 = Sql.Ext.PostgreSQL().ArrayPosition(t1.StrArray, t2.StrValue),
						ArrayPosition2 = Sql.Ext.PostgreSQL().ArrayPosition(t1.StrArray, t2.StrValue, 1),
						ArrayPositions = Sql.Ext.PostgreSQL().ArrayPositions(t1.StrArray, t2.StrValue),
						ArrayPrepend   = Sql.Ext.PostgreSQL().ArrayPrepend(t2.StrValue, t1.StrArray),
						ArrayRemove    = Sql.Ext.PostgreSQL().ArrayRemove(t1.StrArray, t2.StrValue),
						ArrayReplace   = Sql.Ext.PostgreSQL().ArrayReplace(t1.StrArray, t2.StrValue, "NN"),
						ArrayToString1 = Sql.Ext.PostgreSQL().ArrayToString(t1.StrArray, ","),
						ArrayToString2 = Sql.Ext.PostgreSQL().ArrayToString(t1.StrArray, ",", "*"),
						ArrayUpper     = Sql.Ext.PostgreSQL().ArrayUpper(t1.StrArray, 1),
						Cardinality    = Sql.Ext.PostgreSQL().Cardinality(t1.StrArray),
						StringToArray1 = Sql.Ext.PostgreSQL().StringToArray("T1,T2,T3", ","),
						StringToArray2 = Sql.Ext.PostgreSQL().StringToArray("T1,T2,T3", ",", "T2"),
					};

				var result = query.ToArray();
			}
		}

		#region 4562

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4562")]
		public void Issue4562Test([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4562Table>();

			// arrays support missing now
			db.Execute("INSERT INTO \"Issue4562Table\"(\"Id\", \"Statuses\") VALUES(1, '{1, 2, 1, 3, 4}')");
			db.Execute("INSERT INTO \"Issue4562Table\"(\"Id\", \"Statuses\") VALUES(2, '{1, 4}')");

			var notAcceptedStatuses = new StatusType[] { StatusType.Value2, StatusType.Value3 };

			var result = tb.Where(x => !Sql.Ext.PostgreSQL().Overlaps(x.Statuses!, notAcceptedStatuses)).ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(2));
				Assert.That(result[0].Statuses, Is.Not.Null);
			}

			Assert.That(result[0].Statuses, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Statuses![0], Is.EqualTo(StatusType.Value1));
				Assert.That(result[0].Statuses![1], Is.EqualTo(StatusType.Value4));
			}
		}

		[Table]
		sealed class Issue4562Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DbType = "integer[]"), ValueConverter(ConverterType = typeof(Issue4562Converter))] public StatusType[]? Statuses { get; set; }

			sealed class Issue4562Converter : IValueConverter
			{
				bool IValueConverter.HandlesNulls => true;

				LambdaExpression IValueConverter.FromProviderExpression => (StatusType[] x) => x.Select(y => (int)y).ToArray();

				LambdaExpression IValueConverter.ToProviderExpression => (int[] x) => x.Select(y => (StatusType)y).ToArray();
			}
		}

		enum StatusType
		{
			Value1,
			Value2,
			Value3,
			Value4,
		}
		#endregion

		[Test]
		public void GenerateSeries([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var series1 = db.GenerateSeries(1, 10).ToArray();
				var series2 = db.GenerateSeries(1, 10, 2).ToArray();
				
				var dateSeries = db.GenerateSeries(TestData.DateTime.AddDays(-10), TestData.DateTime, TimeSpan.FromHours(5)).ToArray();

				var allInQuery = from t1 in db.GenerateSeries(1, 10)
					from t2 in db.GenerateSeries(1, 10, 2).LeftJoin(t2 => t2 == t1)
					from d in db.GenerateSeries(TestData.DateTime - TimeSpan.FromDays(10), TestData.DateTime,
						TimeSpan.FromHours(1))
					select new
					{
						t1,
						t2,
						Date = d
					};

				var allResult = allInQuery.ToArray();
			}
		}

		[Test]
		public void SystemFunctions([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var allResult = db.Select(() => new
				{
					Version = Sql.Ext.PostgreSQL().Version(db),
					CurrentCatalog = Sql.Ext.PostgreSQL().CurrentCatalog(db),
					CurrentDatabase = Sql.Ext.PostgreSQL().CurrentDatabase(db),
					CurrentRole = Sql.Ext.PostgreSQL().CurrentRole(db),
					CurrentSchema = Sql.Ext.PostgreSQL().CurrentSchema(db),
					//CurrentSchemas = Sql.Ext.PostgreSQL().CurrentSchemas(db),
					CurrentUser = Sql.Ext.PostgreSQL().CurrentUser(db),
					SessionUser = Sql.Ext.PostgreSQL().SessionUser(db),
				});

				var separateResult = new
				{
					Version = Sql.Ext.PostgreSQL().Version(db),
					CurrentCatalog = Sql.Ext.PostgreSQL().CurrentCatalog(db),
					CurrentDatabase = Sql.Ext.PostgreSQL().CurrentDatabase(db),
					CurrentRole = Sql.Ext.PostgreSQL().CurrentRole(db),
					CurrentSchema = Sql.Ext.PostgreSQL().CurrentSchema(db),
					//CurrentSchemas = Sql.Ext.PostgreSQL().CurrentSchemas(db),
					CurrentUser = Sql.Ext.PostgreSQL().CurrentUser(db),
					SessionUser = Sql.Ext.PostgreSQL().SessionUser(db),
				};

				Assert.That(allResult, Is.EqualTo(separateResult));
			}
		}

	}
}
