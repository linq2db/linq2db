using System.Linq;
using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class PostgreSQLExtensionsTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int    Id  { get; set; }
			[Column] public string Str { get; set; } = null!;
			[Column(DbType = "text[]")] public string[] StrArray { get; set; } = null!;

			public static SampleClass[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(i => new SampleClass
					{
						Id = i,
						Str = "S" + i,
						StrArray = Enumerable.Range(i, i).Select(e => $"V{e:00}").ToArray()
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
			var testData = SampleClass.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t1 in table
					from t2 in table.InnerJoin(t2 => t2.Id != t1.Id)
					select new
					{
						TwoArrays = Sql.Ext.PostgreSQL().ConcatArrays(t1.StrArray, t2.StrArray),
						ThreeArrays = Sql.Ext.PostgreSQL().ConcatArrays(t1.StrArray, t2.StrArray, t1.StrArray),
						LessThen = Sql.Ext.PostgreSQL().LessThan(t1.StrArray, t2.StrArray),
						LessThenOrEqual = Sql.Ext.PostgreSQL().LessThanOrEqual(t1.StrArray, t2.StrArray),
						GreaterThen = Sql.Ext.PostgreSQL().GreaterThan(t1.StrArray, t2.StrArray),
						GreaterThanOrEqual = Sql.Ext.PostgreSQL().GreaterThanOrEqual(t1.StrArray, t2.StrArray),
						Contains = Sql.Ext.PostgreSQL().Contains(t1.StrArray, t2.StrArray),
						ContainedBy = Sql.Ext.PostgreSQL().ContainedBy(t1.StrArray, t2.StrArray),
						Overlaps = Sql.Ext.PostgreSQL().Overlaps(t1.StrArray, t2.StrArray),
						//TODO: Other types
						ArrayAppendStr = Sql.Ext.PostgreSQL().ArrayAppend(t1.StrArray, t2.Str),
						ArrayCat = Sql.Ext.PostgreSQL().ArrayCat(t1.StrArray, t2.StrArray),
						ArrayNDims = Sql.Ext.PostgreSQL().ArrayNDims(t1.StrArray),
						ArrayDims = Sql.Ext.PostgreSQL().ArrayDims(t1.StrArray),
						Length = Sql.Ext.PostgreSQL().ArrayLength(t1.StrArray, 1),
						ArrayLower = Sql.Ext.PostgreSQL().ArrayLower(t1.StrArray, 1),
						//TODO:
						// ArrayPosition1 = Sql.Ext.PostgreSQL().ArrayPosition(t1.StrArray, t2.Str),
						// ArrayPosition2 = Sql.Ext.PostgreSQL().ArrayPosition(t1.StrArray, t2.Str, 1),
						// ArrayPositions = Sql.Ext.PostgreSQL().ArrayPositions(t1.StrArray, t2.Str),
						// ArrayPrepend = Sql.Ext.PostgreSQL().ArrayPrepend(t2.Str, t1.StrArray),
						// ArrayRemove = Sql.Ext.PostgreSQL().ArrayRemove(t1.StrArray, t2.Str),
						// ArrayReplace = Sql.Ext.PostgreSQL().ArrayReplace(t1.StrArray, t2.Str, "NN"),
						ArrayToString1 = Sql.Ext.PostgreSQL().ArrayToString(t1.StrArray, ","),
						ArrayToString2 = Sql.Ext.PostgreSQL().ArrayToString(t1.StrArray, ",", "*"),
						ArrayUpper = Sql.Ext.PostgreSQL().ArrayUpper(t1.StrArray, 1),
						Cardinality = Sql.Ext.PostgreSQL().Cardinality(t1.StrArray),
						StringToArray1 = Sql.Ext.PostgreSQL().StringToArray("T1,T2,T3", ","),
						StringToArray2 = Sql.Ext.PostgreSQL().StringToArray("T1,T2,T3", ",", "T2"),
					};

				var result = query.ToArray();
			}

		}


	}
}
