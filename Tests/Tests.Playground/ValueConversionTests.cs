using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class ValueConversionTests : TestBase
	{

		class ItemClass
		{
			public string? Value { get; set; }
		}
		
		[Table]
		class MainClass
		{
			[PrimaryKey]
			public int Id    { get; set; }
			
			[Column(DataType = DataType.NVarChar)] 
			public JToken? Value1 { get; set; }
			
			[Column(DataType = DataType.NVarChar)] 
			public List<ItemClass>? Value2 { get; set; }

			// [Column(DataType = DataType.NVarChar)] public List<>  Values { get; set; }
			
			public static MainClass[] TestData()
			{
				return Enumerable.Range(1, 10)
					.Select(i =>
						new MainClass
						{
							Id = i, Value1 = i == 10 ? null : JToken.Parse($"{{ some : \"str{i}\" }}"),
							Value2 = i == 10 ? null : new List<ItemClass> { new ItemClass { Value = "Value" + i } }
						}
					).ToArray();
			}
		}

		private static MappingSchema CreateMappingSchema()
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

			builder.Entity<MainClass>()
				.Property(e => e.Value1)
				.HasConversion(v => JsonConvert.SerializeObject(v), p => JsonConvert.DeserializeObject<JToken>(p))
				.Property(e => e.Value2)
				.HasConversionFunc(v => JsonConvert.SerializeObject(v), p => JsonConvert.DeserializeObject<List<ItemClass>>(p));
			return ms;
		}

		[Test]
		public void Select([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				// Table Materialization
				var result = table.ToArray();

				Assert.That(result[0].Value1, Is.Not.Null);
				Assert.That(result[0].Value2!.Count, Is.GreaterThan(0));
				
				Assert.That(result[9].Value1, Is.Null);
				Assert.That(result[9].Value2, Is.Null);
		

				var query = from t in table
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
					};

				var selectResult = query.ToArray();

				Assert.That(selectResult[0].Value1, Is.Not.Null);
				Assert.That(selectResult[0].Value2!.Count, Is.GreaterThan(0));
				
				var subqueryResult = query.AsSubQuery().ToArray();
				
				Assert.That(subqueryResult[0].Value1, Is.Not.Null);
				Assert.That(subqueryResult[0].Value2!.Count, Is.GreaterThan(0));

				var unionResult = query.Concat(query.AsSubQuery()).ToArray();

				var firstItem = unionResult.First();
				Assert.That(firstItem.Value1, Is.Not.Null);
				Assert.That(firstItem.Value2!.Count, Is.GreaterThan(0));

				var lastItem = unionResult.Last();
				Assert.That(lastItem.Value1, Is.Null);
				Assert.That(lastItem.Value2, Is.Null);

				var firstList = query.AsSubQuery().OrderBy(e => e.Id).Skip(1).Select(q => q.Value2).FirstOrDefault();
				Assert.That(firstList![0].Value, Is.EqualTo("Value2"));
			}
		}

		[Test]
		public void ParameterTests([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var testedList = testData[0].Value2;

				var query = from t in table
					where testedList == t.Value2 
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(1));
			}
		}

		[Test]
		public void NullTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					where null == t.Value2 
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(1));
			}
		}

		[Test]
		public void NullParameterTests([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				List<ItemClass>? testedList = null;

				var query = from t in table
					where testedList == t.Value2 
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(1));
			}
		}

		[Test]
		public void Update([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var updated = new List<ItemClass> { new ItemClass { Value = "updated" } };
				var affected = table.Where(e => e.Id == 1)
					.Set(e => e.Value1, p => p.Value1)
					.Set(e => e.Value2, updated)
					.Update();


				var toUpdate = new MainClass
				{
					Id = 2, 
					Value1 = JToken.Parse("{ some: \"updated2}\" }"),
					Value2 = new List<ItemClass> { new ItemClass { Value = "updated2" } }
				};

				db.Update(toUpdate);
				
				var toUpdate2 = new MainClass
				{
					Id = 3, 
					Value1 = null,
					Value2 = null 
				};

				db.Update(toUpdate2);
			}
		}

		[Test]
		public void Insert([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<MainClass>())
			{
				var inserted = new List<ItemClass> { new ItemClass { Value = "inserted" } };
				table
					.Value(e => e.Id, 1)
					.Value(e => e.Value1, new JArray())
					.Value(e => e.Value2, inserted)
					.Insert();
				
				table
					.Value(e => e.Id, 2)
					.Value(e => e.Value1, (JToken?)null)
					.Value(e => e.Value2, (List<ItemClass>?)null)
					.Insert();
				
				var toInsert = new MainClass
				{
					Id = 3, 
					Value1 = JToken.Parse("{ some: \"inserted3}\" }"),
					Value2 = new List<ItemClass> { new ItemClass { Value = "inserted3" } }
				};

				db.Insert(toInsert);
				
				Assert.That(table.Count(), Is.EqualTo(3));
			}
		}
		
	}
}
