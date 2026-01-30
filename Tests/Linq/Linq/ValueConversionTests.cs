using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ValueConversionTests : TestBase
	{

		sealed class ItemClass
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
		sealed class MainClass
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

			[Column(DataType = DataType.VarChar, Length = 1, CanBeNull = false)]
			public bool AnotherBoolValue { get; set; }

			[Column]
			public DateTime? DateTimeNullable { get; set; }

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
							BoolValue = i % 4 == 1,
							DateTimeNullable = i % 3 == 1 ? (DateTime?)null : Tests.TestData.Date
						}
					).ToArray();
			}
		}

		sealed class WithNullConverter : ValueConverter<EnumValue, string?>
		{
#pragma warning disable CA2263 // Prefer generic overload when type is known
			public WithNullConverter() : base(v => v == EnumValue.Null ? null : v.ToString(), p=> p == null ? EnumValue.Null : (EnumValue)Enum.Parse(typeof(EnumValue), p), true)
#pragma warning restore CA2263 // Prefer generic overload when type is known
			{

			}
		}

		[Table("ValueConversion")]
		sealed class MainClassRaw
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

			[Column(DataType = DataType.VarChar, Length = 1, CanBeNull = false)]
			public char AnotherBoolValue { get; set; }

			[Column]
			public DateTime? DateTimeNullable { get; set; }
		}

		[Sql.Extension("{value1} = {value2}", ServerSideOnly = true, IsPredicate = true, Precedence = Precedence.Comparison)]
		static bool AnyEquality<T>([ExprParameter] T value1, [ExprParameter] T value2)
			=> throw new NotImplementedException();

		private static MappingSchema CreateMappingSchema()
		{
			var ms = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

#pragma warning disable CA2263 // Prefer generic overload when type is known
			builder.Entity<MainClass>()
				.Property(e => e.Value1)
				.HasConversion(v => JsonConvert.SerializeObject(v), p => JsonConvert.DeserializeObject<JToken>(p))
				.Property(e => e.Value2)
				.HasConversionFunc(JsonConvert.SerializeObject, JsonConvert.DeserializeObject<List<ItemClass>>)
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
				.HasConversion(v => v ? 'Y' : 'N', p => p == 'Y')
				.Property(e => e.AnotherBoolValue)
				.HasConversion(v => v ? 'T' : 'F', p => p == 'T')
				.Property(e => e.DateTimeNullable)
				.HasConversion(
					_ => _.HasValue ? _.Value.ToLocalTime() : new DateTime?(),
					_ => _.HasValue ? new DateTime(_.Value.Ticks, DateTimeKind.Local) : new DateTime?()
				)
				.Build();
#pragma warning restore CA2263 // Prefer generic overload when type is known

			return ms;
		}

		[Test]
		public void Select([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			// Table Materialization
			var result = table.OrderBy(_ => _.Id).ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Value1, Is.Not.Null);
				Assert.That(result[0].Value2!, Is.Not.Empty);

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

				Assert.That(result[0].BoolValue, Is.True);
				Assert.That(result[1].BoolValue, Is.False);
				Assert.That(result[2].BoolValue, Is.False);
				Assert.That(result[3].BoolValue, Is.False);
			}

			var query = from t in table
						select new
						{
							t.Id,
							t.Value1,
							t.Value2,
						};

			var selectResult = query.OrderBy(_ => _.Id).ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(selectResult[0].Value1, Is.Not.Null);
				Assert.That(selectResult[0].Value2!, Is.Not.Empty);
			}

			var subqueryResult = query.AsSubQuery().OrderBy(_ => _.Id).ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(subqueryResult[0].Value1, Is.Not.Null);
				Assert.That(subqueryResult[0].Value2!, Is.Not.Empty);
			}

			var unionResult = query.Concat(query.AsSubQuery()).OrderBy(_ => _.Id).ToArray();

			var firstItem = unionResult.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(firstItem.Value1, Is.Not.Null);
				Assert.That(firstItem.Value2!, Is.Not.Empty);
			}

			var lastItem = unionResult.Last();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(lastItem.Value1, Is.Null);
				Assert.That(lastItem.Value2, Is.Null);
			}

			var firstList = query.AsSubQuery().OrderBy(e => e.Id).Skip(1).Select(q => q.Value2).FirstOrDefault();
			Assert.That(firstList![0].Value, Is.EqualTo("Value2"));
		}

		[Test]
		public void ParameterTests([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
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

			Assert.That(selectResult, Has.Length.EqualTo(1));
		}

		[Test]
		public void ParameterTestsNullable([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var testDate = TestData.Date;

			var query1 = from t in table
						 where testDate == t.DateTimeNullable!.Value
						 select t.DateTimeNullable;

			var query2 = from t in table
						 where t.DateTimeNullable!.Value == testDate
						 select t.DateTimeNullable;

			var result1 = query1.ToArray();
			var result2 = query2.ToArray();

			AreEqual(result1, result2);
		}

		[Test]
		public void GroupByTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
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

		[Test]
		public void ExtensionTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var testedList = testData[0].Value2;

			var query = from t in table
						where AnyEquality(t.Value2, testedList)
						select t;

			var selectResult = query.ToArray();

			Assert.That(selectResult, Has.Length.EqualTo(1));
		}

		[Test]
		public void BoolTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
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

			Assert.That(selectResult, Has.Length.EqualTo(3));
		}

		[Test]
		public void BoolNotTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
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

			Assert.That(selectResult, Has.Length.EqualTo(7));
		}

		[Test]
		public void CoalesceTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var query = from t1 in table
						select new
						{
							Converted = t1.EnumNullable ?? t1.Enum,
						};

			var selectResult = query.ToArray();

			selectResult.Length.ShouldBe(10);
		}

		[Test]
		public void ConditionUnionTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var query = from t1 in table
						select new
						{
							Converted = t1.EnumNullable != null ? t1.EnumNullable : t1.Enum,
						};

			var selectResult = query.Concat(query).ToArray();

			selectResult.Length.ShouldBe(20);
		}

		[Test]
		public void CoalesceConcatTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var query = from t1 in table
						select new
						{
							Converted1 = t1.EnumNullable ?? t1.Enum,
							Converted2 = t1.Value1,
							Converted3 = t1.EnumNullable ?? t1.Enum,
						};

			var selectResult = query.Union(query).ToArray();

			selectResult.Length.ShouldBe(10);
		}

		[Test]
		public void BoolJoinTest([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var query = from t1 in table
						from t2 in table.Where(t2 => t2.BoolValue && t1.BoolValue).AsSubQuery()
						select new
						{
							t1.Enum,
						};

			var selectResult = query.ToArray();

			Assert.That(selectResult, Has.Length.EqualTo(9));
		}

		[Test]
		public void NullTest([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var query = from t in table
						where null == t.Value2
						select new
						{
							t.Id,
							t.Value1,
							t.Value2,
						};

			var selectResult = query.ToArray();

			Assert.That(selectResult, Has.Length.EqualTo(1));
		}

		[Test]
		public void NullParameterTests([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
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

			Assert.That(selectResult, Has.Length.EqualTo(1));
		}

		[Test]
		public void Update([DataSources(false)] string context)
		{
			var ms = CreateMappingSchema();

			var testData = MainClass.TestData();
			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(testData);
			var rawTable = db.GetTable<MainClassRaw>();

			var updated = new List<ItemClass> { new ItemClass { Value = "updated" } };
			var affected = table.Where(e => e.Id == 1)
					.Set(e => e.Value1, p => p.Value1)
					.Set(e => e.Value2, updated)
					.Set(e => e.EnumWithNull, EnumValue.Null)
					.Set(e => e.EnumWithNullDeclarative, EnumValue.Null)
					.Update();

			var update1Check = rawTable.First(e => e.Id == 1);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(update1Check.Value1, Is.EqualTo(JsonConvert.SerializeObject(testData[0].Value1)));
				Assert.That(update1Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(updated)));
				Assert.That(update1Check.EnumWithNull, Is.Null);
				Assert.That(update1Check.EnumWithNullDeclarative, Is.Null);
			}

			var toUpdate2 = new MainClass
			{
				Id = 2,
				Value1 = JToken.Parse("{ some: \"updated2}\" }"),
				Value2 = new List<ItemClass> { new ItemClass { Value = "updated2" } },
				EnumWithNull = EnumValue.Value2,
				EnumWithNullDeclarative = EnumValue.Value2
			};

			db.Update(toUpdate2);

			var update2Check = rawTable.First(e => e.Id == 2);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(update2Check.Value1, Is.EqualTo(/*lang=json,strict*/ "{\"some\":\"updated2}\"}"));
				Assert.That(update2Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(new List<ItemClass> { new ItemClass { Value = "updated2" } })));
				Assert.That(update2Check.EnumWithNull, Is.EqualTo("Value2"));
				Assert.That(update2Check.EnumWithNullDeclarative, Is.EqualTo("Value2"));
			}

			var toUpdate3 = new MainClass
			{
				Id = 3,
				Value1 = null,
				Value2 = null,
				EnumWithNull = EnumValue.Null,
				EnumWithNullDeclarative = EnumValue.Null,
			};
			db.Update(toUpdate3);

			var update3Check = rawTable.First(e => e.Id == 3)!;
			using (Assert.EnterMultipleScope())
			{
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

			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<MainClass>();
			var rawTable = db.GetTable<MainClassRaw>();

			var inserted = new List<ItemClass> { new ItemClass { Value = "inserted" } };
			table
				.Value(e => e.Id, 1)
				.Value(e => e.Value1, new JArray())
				.Value(e => e.Enum, EnumValue.Value1)
				.Value(e => e.Value2, inserted)
				.Value(e => e.BoolValue, true)
				.Value(e => e.AnotherBoolValue, true)
				.Insert();
			var insert1Check = rawTable.First(e => e.Id == 1);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(insert1Check.Value1, Is.EqualTo(JsonConvert.SerializeObject(new JArray())));
				Assert.That(insert1Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(inserted)));
				Assert.That(insert1Check.Enum, Is.EqualTo("Value1"));
				Assert.That(insert1Check.EnumWithNull, Is.Null);
				Assert.That(insert1Check.EnumWithNullDeclarative, Is.Null);
				Assert.That(insert1Check.BoolValue, Is.EqualTo('Y'));
				Assert.That(insert1Check.AnotherBoolValue, Is.EqualTo('T'));
			}

			table
				.Value(e => e.Id, 2)
				.Value(e => e.Value1, (JToken?)null)
				.Value(e => e.Value2, (List<ItemClass>?)null)
				.Value(e => e.Enum, EnumValue.Value2)
				.Value(e => e.BoolValue, false)
				.Value(e => e.AnotherBoolValue, false)
				.Insert();

			var insert2Check = rawTable.First(e => e.Id == 2);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(insert2Check.Value1, Is.Null);
				Assert.That(insert2Check.Value2, Is.Null);
				Assert.That(insert2Check.Enum, Is.EqualTo("Value2"));
				Assert.That(insert2Check.EnumWithNull, Is.Null);
				Assert.That(insert2Check.EnumWithNullDeclarative, Is.Null);
				Assert.That(insert2Check.BoolValue, Is.EqualTo('N'));
				Assert.That(insert2Check.AnotherBoolValue, Is.EqualTo('F'));
			}

			var toInsert = new MainClass
			{
				Id = 3,
				Value1 = JToken.Parse("{ some: \"inserted3}\" }"),
				Value2 = new List<ItemClass> { new ItemClass { Value = "inserted3" } },
				Enum = EnumValue.Value3,
				BoolValue = true,
				AnotherBoolValue = true,
			};

			db.Insert(toInsert);

			var insert3Check = rawTable.First(e => e.Id == 3);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(insert3Check.Value1, Is.EqualTo(/*lang=json,strict*/ "{\"some\":\"inserted3}\"}"));
				Assert.That(insert3Check.Value2, Is.EqualTo(JsonConvert.SerializeObject(new List<ItemClass> { new ItemClass { Value = "inserted3" } })));
				Assert.That(insert3Check.Enum, Is.EqualTo("Value3"));
				Assert.That(insert3Check.EnumNullable, Is.Null);
				Assert.That(insert3Check.EnumWithNull, Is.EqualTo("Value1"));
				Assert.That(insert3Check.EnumWithNullDeclarative, Is.EqualTo("Value1"));
				Assert.That(insert3Check.BoolValue, Is.EqualTo('Y'));
				Assert.That(insert3Check.AnotherBoolValue, Is.EqualTo('T'));

				Assert.That(table.Count(), Is.EqualTo(3));
			}
		}

		[Test]
		public void InsertExpression([DataSources(false)] string context, [Values(1, 2)] int iteration)
		{
			var ms = CreateMappingSchema();

			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<MainClass>();
			var rawTable = db.GetTable<MainClassRaw>();

			var inserted  = new List<ItemClass> { new ItemClass { Value = "inserted" } };
			var boolValue = iteration % 2 == 0;
			table.Insert(() => new MainClass
			{
				Id = iteration,
				Value1 = new JArray(),
				Enum = EnumValue.Value1,
				Value2 = inserted,
				BoolValue = boolValue,
				AnotherBoolValue = boolValue
			});

			var record = rawTable.Single(e => e.Id == iteration);

			record.Id.ShouldBe(iteration);
			record.Value1.ShouldBe(JsonConvert.SerializeObject(new JArray()));
			record.Value2.ShouldBe(JsonConvert.SerializeObject(inserted));
			record.Enum.ShouldBe("Value1");
			record.EnumWithNull.ShouldBeNull();
			record.EnumWithNullDeclarative.ShouldBeNull();
			record.BoolValue.ShouldBe(boolValue ? 'Y' : 'N');
			record.AnotherBoolValue.ShouldBe(boolValue ? 'T' : 'F');
		}

		public class Issue3684DateTimeNullConverter : ValueConverterFunc<DateTime?, System.Data.SqlTypes.SqlDateTime>
		{
			public Issue3684DateTimeNullConverter() : base(
				model =>
				{
					if (model == null)
						return SqlDateTime.Null;

					return new SqlDateTime(model.Value);
				},
				provider =>
				{
					if (provider.IsNull)
						return null;

					return provider.Value;
				}, true)
			{
			}
		}

		[Table]
		public class Issue3684Table
		{
			[PrimaryKey, Identity                                                  ] public int       Id                   { get; set; }
			[ValueConverter(ConverterType = typeof(Issue3684DateTimeNullConverter))]
			[Column(DataType = DataType.DateTime2, Precision = 0)                  ] public DateTime? FirstAppointmentTime { get; set; }
			[ValueConverter(ConverterType = typeof(Issue3684DateTimeNullConverter))]
			[Column(DataType = DataType.DateTime)                                  ] public DateTime? PassportDateOfIssue  { get; set; }
		}

		[Test]
		public void Issue3684Test([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			// remote context serialization for SqlDateTime
			var ms = new MappingSchema();
			ms.SetConvertExpression<SqlDateTime, string>(value => ((DateTime)value).ToBinary().ToString(CultureInfo.InvariantCulture));
			ms.SetConvertExpression<string, SqlDateTime>(value => DateTime.FromBinary(long.Parse(value, CultureInfo.InvariantCulture)));

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<Issue3684Table>();

			table.Insert(() => new Issue3684Table());
			table.Insert(() => new Issue3684Table() { FirstAppointmentTime = TestData.DateTime0, PassportDateOfIssue = TestData.DateTime3 });

			var data = table.OrderBy(_ => _.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(1));
				Assert.That(data[0].FirstAppointmentTime, Is.Null);
				Assert.That(data[0].PassportDateOfIssue, Is.Null);

				Assert.That(data[1].Id, Is.EqualTo(2));
				Assert.That(data[1].FirstAppointmentTime, Is.EqualTo(TestData.DateTime0));
				Assert.That(data[1].PassportDateOfIssue, Is.EqualTo(TestData.DateTime3));
			}
		}

		sealed class BoolConverterAttribute : ValueConverterAttribute
		{
			public BoolConverterAttribute()
			{
				ValueConverter = new ValueConverter<bool, string>(b => b ? "Y" : "N", m => m == "Y", false);
			}
		}

		sealed class BoolConverterNullableAttribute : ValueConverterAttribute
		{
			public BoolConverterNullableAttribute()
			{
				ValueConverter = new ValueConverter<bool?, string>(b => b == true ? "Y" : "N", m => m == "Y", false);
			}
		}

		sealed class BoolConverterNullsAttribute : ValueConverterAttribute
		{
			public BoolConverterNullsAttribute()
			{
				ValueConverter = new ValueConverter<bool, string?>(b => b ? "Y" : null, m => m == "Y", true);
			}
		}

		[Table]
		public class Issue3830TestTable
		{
			[PrimaryKey                                                                        ] public int   Id    { get; set; }
			[Column(DataType = DataType.Char, Length = 1), BoolConverter                       ] public bool  Bool1 { get; set; }
			[Column(DataType = DataType.Char, Length = 1), BoolConverterNullable               ] public bool? Bool2 { get; set; }
			[Column(DataType = DataType.Char, Length = 1, CanBeNull = true), BoolConverterNulls] public bool  Bool3 { get; set; }

			public static readonly Issue3830TestTable[] TestData = new Issue3830TestTable[]
			{
				new Issue3830TestTable() { Id = 1, Bool1 = true,  Bool2 = null,  Bool3 = false },
				new Issue3830TestTable() { Id = 2, Bool1 = false, Bool2 = null,  Bool3 = true  },
				new Issue3830TestTable() { Id = 3, Bool1 = false, Bool2 = true,  Bool3 = false },
				new Issue3830TestTable() { Id = 4, Bool1 = true,  Bool2 = false, Bool3 = true  },
			};
		}

		[Test]
		public void Issue3830Test([DataSources] string context, [Values] bool inline)
		{
			using var db        = GetDataContext(context);
			db.InlineParameters = inline;
			using var table     = db.CreateLocalTable(Issue3830TestTable.TestData);

			foreach (var record in Issue3830TestTable.TestData)
			{
				// bool_field=value
				AssertRecord(record, table.Where(r => r.Bool1 == record.Bool1 && r.Bool2 == record.Bool2 && r.Bool3 == record.Bool3).ToArray());
				// bool_field
				if (record.Bool1 == true ) AssertRecord(record, table.Where(r => r.Bool1 && r.Bool2 == record.Bool2 && r.Bool3 == record.Bool3).ToArray());
				if (record.Bool3 == true ) AssertRecord(record, table.Where(r => r.Bool3 && r.Bool1 == record.Bool1 && r.Bool2 == record.Bool2).ToArray());
				// !bool_field
				if (record.Bool1 == false) AssertRecord(record, table.Where(r => !r.Bool1 && r.Bool2 == record.Bool2 && r.Bool3 == record.Bool3).ToArray());
				if (record.Bool3 == false) AssertRecord(record, table.Where(r => !r.Bool3 && r.Bool1 == record.Bool1 && r.Bool2 == record.Bool2).ToArray());
				// bool_field is null
				if (record.Bool2 == null ) AssertRecord(record, table.Where(r => r.Bool2 == null && r.Bool1 == record.Bool1 && r.Bool3 == record.Bool3).ToArray());
				// bool_field is not null
				if (record.Bool2 != null ) AssertRecord(record, table.Where(r => r.Bool2 != null && r.Bool1 == record.Bool1 && r.Bool3 == record.Bool3).ToArray());
			}

			static void AssertRecord(Issue3830TestTable record, Issue3830TestTable[] result)
			{
				Assert.That(result, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Id, Is.EqualTo(record.Id));
					Assert.That(result[0].Bool1, Is.EqualTo(record.Bool1));
					Assert.That(result[0].Bool2, Is.EqualTo(record.Bool2));
					Assert.That(result[0].Bool3, Is.EqualTo(record.Bool3));
				}
			}
		}

		[Test]
		public void ConditionNullTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from p in Parent
				from i in new[] { 0, 1 }
				select new
				{
					ID    = i == 0 ? null : (int?)p.ParentID,
					Value = p.Value1
				} into p
				where p.ID == p.Value
				select p
				,
				from p in db.Parent
				from i in new[] { 0, 1 }
				select new
				{
					ID    = i == 0 ? null : (int?)p.ParentID,
					Value = p.Value1
				} into p
				where p.ID == p.Value
				select p);
		}

		static class Issue5075
		{
			public sealed class Table
			{
				public required int Id { get; init; }

				[ValueConverter(ConverterType = typeof(EnumConverter))]
				[Column(DataType = DataType.NVarChar)]
				public required EnumValue EnumValue { get; init; }

				[ValueConverter(ConverterType = typeof(EnumConverter))]
				[Column(DataType = DataType.NVarChar, CanBeNull = true)]
				public required EnumValue EnumValueNullable { get; init; }

				[ValueConverter(ConverterType = typeof(EnumConverter))]
				[Column(DataType = DataType.NVarChar)]
				public required EnumValue? EnumValueNull { get; init; }

				sealed class EnumConverter() : ValueConverter<EnumValue, string>(
					v => v.ToString(),
#pragma warning disable CA2263 // Prefer generic overload when type is known
					v => (EnumValue)Enum.Parse(typeof(EnumValue), v),
#pragma warning restore CA2263 // Prefer generic overload when type is known
					false)
				{
				}

				public static readonly Table[] Data =
				[
					new (){ Id = 1, EnumValue = EnumValue.Admin, EnumValueNullable = EnumValue.Admin, EnumValueNull = null },
					new (){ Id = 2, EnumValue = EnumValue.User, EnumValueNullable = EnumValue.User, EnumValueNull = EnumValue.Admin },
					new (){ Id = 3, EnumValue = EnumValue.User, EnumValueNullable = EnumValue.User, EnumValueNull = EnumValue.User },
				];
			}

			public enum EnumValue
			{
				Admin,
				User
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5075")]
		public void Issue5075Test([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue5075.Table.Data);

			Issue5075.EnumValue? value = Issue5075.EnumValue.User;

			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(t => t.EnumValue == value.Value).Count(), Is.EqualTo(2));
				Assert.That(tb.Where(t => t.EnumValue == value).Count(), Is.EqualTo(2));

				Assert.That(tb.Where(t => t.EnumValueNullable == value.Value).Count(), Is.EqualTo(2));
				Assert.That(tb.Where(t => t.EnumValueNullable == value).Count(), Is.EqualTo(2));

				Assert.That(tb.Where(t => t.EnumValueNull == value.Value).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(t => t.EnumValueNull == value).Count(), Is.EqualTo(1));
			}

			value = null;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(t => t.EnumValue == value).Count(), Is.Zero);
				Assert.That(tb.Where(t => t.EnumValue == value!.Value).Count(), Is.Zero);

				Assert.That(tb.Where(t => t.EnumValueNullable == value).Count(), Is.Zero);
				Assert.That(tb.Where(t => t.EnumValueNullable == value!.Value).Count(), Is.Zero);

				Assert.That(tb.Where(t => t.EnumValueNull == value!.Value).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(t => t.EnumValueNull == value).Count(), Is.EqualTo(1));
			}
		}

		[Table]
		class Issue5310Table
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(DbType = "smallint[]"), ValueConverter(ConverterType = typeof(CustomConverter))]
			public PaymentType[]? Types { get; set; }

			sealed class CustomConverter() : ValueConverter<PaymentType[], short[]>(
				types => types.Select(t => (short)t).ToArray(),
				values => values.Select(v => (PaymentType)v).ToArray(),
				false);

			public enum PaymentType : short
			{
				One = 1,
				Two = 2
			}

			public static readonly Issue5310Table[] Data =
			[
				new () { Id = 1 }
			];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5310")]
		public void Issue5310Test([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue5310Table.Data);

			var data = tb
				.Where(s => s.Id == 1)
				.Set(s => s.Types, [Issue5310Table.PaymentType.One])
				.UpdateWithOutput((_, inserted) => inserted)
				.ToArray();

			Assert.That(data, Has.Length.EqualTo(1));
			Assert.That(data[0].Types, Is.Not.Null.And.Length.EqualTo(1));
			Assert.That(data[0].Types[0], Is.EqualTo(Issue5310Table.PaymentType.One));
		}
	}
}
