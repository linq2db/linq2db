using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ValueConversionTests : TestBase
	{

		class ItemClass
		{
			public string? Value { get; set; }
		}

		enum EnumValue
		{
			Value1,
			Value2,
			Value3,
			Null
		}
		
		[Table("ValueConversion")]
		class MainClass
		{
			[PrimaryKey]
			public int Id    { get; set; }
			
			[Column(DataType = DataType.NVarChar, Length = 200, CanBeNull = true)] 
			public JToken? Value1 { get; set; }
			
			[Column(DataType = DataType.NVarChar, Length = 200, CanBeNull = true)] 
			public List<ItemClass>? Value2 { get; set; }


			[Column(DataType = DataType.NVarChar, Length = 50)] 
			public EnumValue Enum { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)] 
			public EnumValue? EnumNullable { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)] 
			public EnumValue EnumWithNull { get; set; }


			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)]
			[ValueConverter(ConverterType = typeof(WithNullConverter))]
			public EnumValue EnumWithNullDeclarative { get; set; }

			[Column(DataType = DataType.VarChar, Length = 1, CanBeNull = false)]
			public bool BoolValue { get; set; }

			public static MainClass[] TestData()
			{
				return Enumerable.Range(1, 10)
					.Select(i =>
						new MainClass
						{
							Id = i, Value1 = i == 10 ? null : JToken.Parse($"{{ some : \"str{i}\" }}"),
							Value2 = i == 10 ? null : new List<ItemClass> { new ItemClass { Value = "Value" + i } },
							Enum = i % 3 == 1 ? EnumValue.Value1 : i % 3 == 2 ? EnumValue.Value2 : EnumValue.Value3,
							EnumNullable = i % 4 == 1 ? EnumValue.Value1 : i % 4 == 2 ? EnumValue.Value2 : i % 4 == 3 ? EnumValue.Value3 : (EnumValue?)null,
							EnumWithNull = i % 4 == 1 ? EnumValue.Value1 : i % 4 == 2 ? EnumValue.Value2 : i % 4 == 3 ? EnumValue.Value3 : EnumValue.Null,
							EnumWithNullDeclarative = i % 4 == 1 ? EnumValue.Value1 : i % 4 == 2 ? EnumValue.Value2 : i % 4 == 3 ? EnumValue.Value3 : EnumValue.Null,
							BoolValue = i % 4 == 1
						}
					).ToArray();
			}
		}

		class WithNullConverter: ValueConverter<EnumValue, string?>
		{
			public WithNullConverter() : base(v => v == EnumValue.Null ? null : v.ToString(), p=> p == null ? EnumValue.Null : (EnumValue)Enum.Parse(typeof(EnumValue), p), true)
			{

			}
		}

		[Table("ValueConversion")]
		class MainClassRaw
		{
			[PrimaryKey]
			public int Id    { get; set; }
			
			[Column(DataType = DataType.NVarChar, Length = 200, CanBeNull = true)] 
			public string? Value1 { get; set; }
			
			[Column(DataType = DataType.NVarChar, Length = 200, CanBeNull = true)] 
			public string? Value2 { get; set; }


			[Column(DataType = DataType.NVarChar, Length = 50)]
			public string Enum { get; set; } = null!;

			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)] 
			public string? EnumNullable { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)] 
			public string? EnumWithNull { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50, CanBeNull = true)] 
			public string? EnumWithNullDeclarative { get; set; }

			[Column(DataType = DataType.VarChar, Length = 1, CanBeNull = false)]
			public char BoolValue { get; set; }
		}

		[Sql.Extension("{value1} = {value2}", ServerSideOnly = true, IsPredicate = true, Precedence = Precedence.Comparison)]
		static bool AnyEquality<T>([ExprParameter] T value1, [ExprParameter] T value2)
			=> throw new NotImplementedException();

		private static MappingSchema CreateMappingSchema()
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

			builder.Entity<MainClass>()
				.Property(e => e.Value1)
				.HasConversion(v => JsonConvert.SerializeObject(v), p => JsonConvert.DeserializeObject<JToken>(p))
				.Property(e => e.Value2)
				.HasConversionFunc(v => JsonConvert.SerializeObject(v),
					p => JsonConvert.DeserializeObject<List<ItemClass>>(p))
				.Property(e => e.Enum)
				.HasConversion(v => v.ToString(), p => (EnumValue)Enum.Parse(typeof(EnumValue), p))
				.Property(e => e.EnumNullable)
				.HasConversion(v => v.ToString()!, p => (EnumValue)Enum.Parse(typeof(EnumValue), p))
				.Property(e => e.EnumWithNull)
				.HasConversion(
					v => v == EnumValue.Null ? null : v.ToString(),
					p => p == null ? EnumValue.Null : (EnumValue)Enum.Parse(typeof(EnumValue), p), 
					true
				)
				.Property(e => e.BoolValue)
				.HasConversion(v => v ? 'Y' : 'N', p => p == 'Y');

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

				Assert.That(result[0].Enum, Is.EqualTo(EnumValue.Value1));
				Assert.That(result[1].Enum, Is.EqualTo(EnumValue.Value2));
				Assert.That(result[2].Enum, Is.EqualTo(EnumValue.Value3));

				Assert.That(result[0].EnumNullable, Is.EqualTo(EnumValue.Value1));
				Assert.That(result[1].EnumNullable, Is.EqualTo(EnumValue.Value2));
				Assert.That(result[2].EnumNullable, Is.EqualTo(EnumValue.Value3));
				Assert.That(result[3].EnumNullable, Is.Null);

				Assert.That(result[0].EnumWithNull, Is.EqualTo(EnumValue.Value1));
				Assert.That(result[1].EnumWithNull, Is.EqualTo(EnumValue.Value2));
				Assert.That(result[2].EnumWithNull, Is.EqualTo(EnumValue.Value3));
				Assert.That(result[3].EnumWithNull, Is.EqualTo(EnumValue.Null));

				Assert.That(result[0].EnumWithNullDeclarative, Is.EqualTo(EnumValue.Value1));
				Assert.That(result[1].EnumWithNullDeclarative, Is.EqualTo(EnumValue.Value2));
				Assert.That(result[2].EnumWithNullDeclarative, Is.EqualTo(EnumValue.Value3));
				Assert.That(result[3].EnumWithNullDeclarative, Is.EqualTo(EnumValue.Null));

				Assert.That(result[9].Value1, Is.Null);
				Assert.That(result[9].Value2, Is.Null);
		
				Assert.That(result[0].BoolValue, Is.EqualTo(true));
				Assert.That(result[1].BoolValue, Is.EqualTo(false));
				Assert.That(result[2].BoolValue, Is.EqualTo(false));
				Assert.That(result[3].BoolValue, Is.EqualTo(false));
				

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
		public void GroupByTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var testedList = testData[0].Value2;

				var query = from t in table
					where testedList == t.Value2
					group t by t.Id
					into g
					select g;


				query = query.DisableGuard();

				foreach (var item in query)
				{
					var elements = item.ToArray();
				}
			
			}
		}

		[Test]
		public void ExtensionTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var testedList = testData[0].Value2;

				var query = from t in table
					where AnyEquality(t.Value2, testedList)
					select t;

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(1));
			}
		}

		[Test]
		public void BoolTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					where t.BoolValue
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
						t.BoolValue
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(3));
			}
		}

		[Test]
		public void BoolNotTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t in table
					where !t.BoolValue
					select new
					{
						t.Id,
						t.Value1,
						t.Value2,
						t.BoolValue
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(7));
			}
		}

		[Test]
		public void BoolJoinTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from t1 in table
					from t2 in table.Where(t2 => t2.BoolValue && t1.BoolValue).AsSubQuery()
					select new
					{
						t1.Enum,
					};

				var selectResult = query.ToArray();
				
				Assert.That(selectResult.Length, Is.EqualTo(9));
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
				var rawTable = db.GetTable<MainClassRaw>();

				var updated = new List<ItemClass> { new ItemClass { Value = "updated" } };
				var affected = table.Where(e => e.Id == 1)
					.Set(e => e.Value1, p => p.Value1)
					.Set(e => e.Value2, updated)
					.Set(e => e.EnumWithNull, EnumValue.Null)
					.Set(e => e.EnumWithNullDeclarative, EnumValue.Null)
					.Update();

				var update1Check = rawTable.FirstOrDefault(e => e.Id == 1)!;

				Assert.That(update1Check.Value1, Is.EqualTo(JsonConvert.SerializeObject(testData[0].Value1)));
				Assert.That(update1Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(updated)));
				Assert.That(update1Check.EnumWithNull, Is.Null);
				Assert.That(update1Check.EnumWithNullDeclarative, Is.Null);


				var toUpdate2 = new MainClass
				{
					Id = 2, 
					Value1 = JToken.Parse("{ some: \"updated2}\" }"),
					Value2 = new List<ItemClass> { new ItemClass { Value = "updated2" } },
					EnumWithNull = EnumValue.Value2,
					EnumWithNullDeclarative = EnumValue.Value2
				};

				db.Update(toUpdate2);

				var update2Check = rawTable.FirstOrDefault(e => e.Id == 2)!;

				Assert.That(update2Check.Value1, Is.EqualTo("{\"some\":\"updated2}\"}"));
				Assert.That(update2Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(new List<ItemClass> { new ItemClass { Value = "updated2" } })));
				Assert.That(update2Check.EnumWithNull, Is.EqualTo("Value2"));
				Assert.That(update2Check.EnumWithNullDeclarative, Is.EqualTo("Value2"));

				
				var toUpdate3 = new MainClass
				{
					Id = 3, 
					Value1 = null,
					Value2 = null,
					EnumWithNull = EnumValue.Null, 
					EnumWithNullDeclarative = EnumValue.Null, 
				};
				db.Update(toUpdate3);

				var update3Check = rawTable.FirstOrDefault(e => e.Id == 3)!;

				Assert.That(update3Check.Value1, Is.Null);
				Assert.That(update3Check.Value2, Is.Null);
				Assert.That(update3Check.EnumWithNull, Is.Null);
				Assert.That(update3Check.EnumWithNullDeclarative, Is.Null);

			}
		}

		[Test]
		public void Insert([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<MainClass>())
			{
				var rawTable = db.GetTable<MainClassRaw>();

				var inserted = new List<ItemClass> { new ItemClass { Value = "inserted" } };
				table
					.Value(e => e.Id, 1)
					.Value(e => e.Value1, new JArray())
					.Value(e => e.Enum, EnumValue.Value1)
					.Value(e => e.Value2, inserted)
					.Value(e => e.BoolValue, true)
					.Insert();
				var insert1Check = rawTable.FirstOrDefault(e => e.Id == 1)!;

				Assert.That(insert1Check.Value1, Is.EqualTo(JsonConvert.SerializeObject(new JArray())));
				Assert.That(insert1Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(inserted)));
				Assert.That(insert1Check.Enum,   Is.EqualTo("Value1"));
				Assert.That(insert1Check.EnumWithNull, Is.Null);
				Assert.That(insert1Check.EnumWithNullDeclarative, Is.Null);
				Assert.That(insert1Check.BoolValue, Is.EqualTo('Y'));
				
				table
					.Value(e => e.Id, 2)
					.Value(e => e.Value1, (JToken?)null)
					.Value(e => e.Value2, (List<ItemClass>?)null)
					.Value(e => e.Enum, EnumValue.Value2)
					.Value(e => e.BoolValue, false)
					.Insert();

				var insert2Check = rawTable.FirstOrDefault(e => e.Id == 2)!;

				Assert.That(insert2Check.Value1, Is.Null);
				Assert.That(insert2Check.Value2, Is.Null);
				Assert.That(insert2Check.Enum,   Is.EqualTo("Value2"));
				Assert.That(insert2Check.EnumWithNull, Is.Null);
				Assert.That(insert2Check.EnumWithNullDeclarative, Is.Null);
				Assert.That(insert2Check.BoolValue, Is.EqualTo('N'));


				var toInsert = new MainClass
				{
					Id = 3, 
					Value1 = JToken.Parse("{ some: \"inserted3}\" }"),
					Value2 = new List<ItemClass> { new ItemClass { Value = "inserted3" } },
					Enum = EnumValue.Value3,
					BoolValue = true
				};

				db.Insert(toInsert);

				var insert3Check = rawTable.FirstOrDefault(e => e.Id == 3)!;

				Assert.That(insert3Check.Value1, Is.EqualTo("{\"some\":\"inserted3}\"}"));
				Assert.That(insert3Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(new List<ItemClass> { new ItemClass { Value = "inserted3" } })));
				Assert.That(insert3Check.Enum,   Is.EqualTo("Value3"));
				Assert.That(insert3Check.EnumNullable, Is.Null);
				Assert.That(insert3Check.EnumWithNull, Is.EqualTo("Value1"));
				Assert.That(insert3Check.EnumWithNullDeclarative, Is.EqualTo("Value1"));
				Assert.That(insert3Check.BoolValue, Is.EqualTo('Y'));

				Assert.That(table.Count(), Is.EqualTo(3));
			}
		}
		
	}
}
