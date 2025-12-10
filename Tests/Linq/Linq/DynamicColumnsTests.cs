using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DynamicColumnsTests : TestBase
	{
		// Introduced to ensure that we process not only constants in column names
		static string IDColumn        = "ID";
		static string DiagnosisColumn = "Diagnosis";
		static string PatientColumn   = "Patient";

		[Test]
		public void SqlPropertyWithNonDynamicColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>()
					.Where(x => Sql.Property<int>(x, IDColumn) == 1)
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().FirstName, Is.EqualTo("John"));
		}

		[Test]
		public void SqlPropertyWithNavigationalNonDynamicColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(x.Patient, DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().FirstName, Is.EqualTo("Tester"));
		}

		[Test]
		public void SqlPropertyWithNonDynamicAssociation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().FirstName, Is.EqualTo("Tester"));
		}

		[Test]
		public void SqlPropertyWithNonDynamicAssociationViaObject1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>()
					.Where(x => (string)Sql.Property<object>(Sql.Property<object>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().FirstName, Is.EqualTo("Tester"));
		}

		[Test]
		public void SqlPropertyWithNonDynamicAssociationViaObject2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
			var result = db.GetTable<Person>()
					.Select(x => Sql.Property<object>(Sql.Property<object>(x, PatientColumn), DiagnosisColumn))
					.ToList();

			Assert.That(result.OrderBy(_ => _ as string).SequenceEqual(expected.OrderBy(_ => _)), Is.True);
		}

		[Test]
		public void SqlPropertyWithDynamicColumn([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().ID, Is.EqualTo(1));
		}

		[Test]
		public void SqlPropertyWithDynamicAssociation([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.Single().ID, Is.EqualTo(2));
		}

		[Test]
		public void SqlPropertySelectAll([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Select(p => p.FirstName).ToList();
			var result = db.GetTable<PersonWithDynamicStore>().ToList().Select(p => p.ExtendedProperties["FirstName"]).ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertySelectOne([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Select(p => p.FirstName).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.Select(x => Sql.Property<string>(x, "FirstName"))
					.ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertySelectProject([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Select(p => new
			{
				PersonId = p.ID,
				Name = p.FirstName
			}).ToList();

			var result = db.GetTable<PersonWithDynamicStore>()
					.Select(x => new
					{
						PersonId = x.ID,
						Name = Sql.Property<string>(x, "FirstName")
					})
					.ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertySelectAssociated([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();

			var result = db.GetTable<PersonWithDynamicStore>()
					.Select(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.ToList();

			Assert.That(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)), Is.True);
		}

		[Test]
		public void SqlPropertyWhere([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Where(p => p.FirstName == "John").Select(p => p.ID).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.Select(x => x.ID)
					.ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertyWhereAssociated([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Where(p => p.Patient?.Diagnosis != null).Select(p => p.ID).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) != null)
					.Select(x => x.ID)
					.ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertyOrderBy([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.OrderByDescending(p => p.FirstName).Select(p => p.ID).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.OrderByDescending(x => Sql.Property<string>(x, "FirstName"))
					.Select(x => x.ID)
					.ToList();

			Assert.That(result.SequenceEqual(expected), Is.True);
		}

		[Test]
		public void SqlPropertyOrderByAssociated([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.OrderBy(p => p.Patient?.Diagnosis).Select(p => p.ID).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.OrderBy(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.Select(x => x.ID)
					.ToList();

			Assert.That(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)), Is.True);
		}

		[Test]
		public void SqlPropertyGroupBy([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.GroupBy(p => p.FirstName).Select(p => new {p.Key, Count = p.Count()}).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.GroupBy(x => Sql.Property<string>(x, "FirstName"))
					.Select(p => new {p.Key, Count = p.Count()})
					.ToList();

			Assert.That(result.OrderBy(_ => _.Key).SequenceEqual(expected.OrderBy(_ => _.Key)), Is.True);
		}

		[Test]
		public void SqlPropertyGroupByAssociated([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.GroupBy(p => p.Patient?.Diagnosis).Select(p => new {p.Key, Count = p.Count()}).ToList();
			var result   = db.GetTable<PersonWithDynamicStore>()
					.GroupBy(x => Sql.Property<string?>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.Select(p => new { p.Key, Count = p.Count() })
					.ToList();

			Assert.That(result.OrderBy(_ => _.Key).SequenceEqual(expected.OrderBy(_ => _.Key)), Is.True);
		}

		[Test]
		public void SqlPropertyJoin([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected =
					from p in Person
					join pa in Patient on p.FirstName equals pa.Diagnosis
					select p;

			var result =
					from p in db.Person
					join pa in db.Patient on Sql.Property<string>(p, "FirstName") equals Sql.Property<string>(pa, DiagnosisColumn)
					select p;

			Assert.That(result.ToList().SequenceEqual(expected.ToList()), Is.True);
		}

		[Test]
		public void SqlPropertyLoadWith([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
			var result = db.GetTable<PersonWithDynamicStore>()
					.LoadWith(x => Sql.Property<Patient>(x, "Patient"))
					.ToList()
					.Select(p => ((Patient)p.ExtendedProperties[PatientColumn])?.Diagnosis)
					.ToList();

			Assert.That(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)), Is.True);
		}

		[Test]
		public void SqlPropertyNoStoreGrouping1([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureDynamicClass());
			var expected =
					from p in Person
					group p by p.FirstName into g
					select new
					{
						FirstName = g.Key,
						Count = g.Count()
					};

			var result =
					from p in db.GetTable<PersonWithDynamicStore>()
					group p by Sql.Property<string>(p, "FirstName") into g
					select new
					{
						FirstName = g.Key,
						Count = g.Count()
					};

			AreEqual(result.OrderBy(_ => _.FirstName), expected.OrderBy(_ => _.FirstName));
		}

		[Test]
		public void SqlPropertyNoStoreGrouping2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected =
					from p in Person
					group p by new { p.FirstName, p.LastName } into g
					select new
					{
						g.Key.FirstName,
						g.Key.LastName,
						Count = g.Count()
					};

			var result =
					from p in db.GetTable<PersonWithoutDynamicStore>()
					group p by new { FirstName = Sql.Property<string>(p, "FirstName"), LastName = Sql.Property<string>(p, "LastName")} into g
					select new
					{
						g.Key.FirstName,
						g.Key.LastName,
						Count = g.Count()
					};

			AreEqual(result.OrderBy(_ => _.FirstName), expected.OrderBy(_ => _.FirstName));
		}

		[Test]
		public void SqlPropertyNoStoreNonIdentifier([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context, o => o.UseFirebird(o => o with { IdentifierQuoteMode = FirebirdIdentifierQuoteMode.Auto })))
			using (db.CreateLocalTable(new []
			{
				new DynamicTablePrototype { NotIdentifier = 77 }
			}))
			{
				var query =
					from d in db.GetTable<DynamicTable>()
					select new
					{
						NI = Sql.Property<int>(d, "Not Identifier")
					};

				var result = query.ToArray();

				Assert.That(result[0].NI, Is.EqualTo(77));
			}
		}

		[Test]
		public void SqlPropertyNoStoreNonIdentifierGrouping([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context, o => o.UseFirebird(o => o with { IdentifierQuoteMode = FirebirdIdentifierQuoteMode.Auto })))
			using (db.CreateLocalTable(new []
			{
				new DynamicTablePrototype { NotIdentifier = 77, Value = 5 },
				new DynamicTablePrototype { NotIdentifier = 77, Value = 5 }
			}))
			{
				var query =
					from d in db.GetTable<DynamicTable>()
					group d by new { NI = Sql.Property<int>(d, "Not Identifier") }
					into g
					select new
					{
						g.Key.NI,
						Count = g.Count(),
						Sum = g.Sum(i => Sql.Property<int>(i, "Some Value"))
					};

				var result = query.ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].NI, Is.EqualTo(77));
					Assert.That(result[0].Count, Is.EqualTo(2));
				}

				Assert.That(result[0].Sum, Is.EqualTo(10));
			}
		}

		private MappingSchema ConfigureDynamicClass()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<PersonWithDynamicStore>().HasTableName("Person")
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID"))
				.Property(x => Sql.Property<string>(x, "FirstName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "LastName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "MiddleName"))
				.Association(x => Sql.Property<Patient>(x, "Patient"), x => Sql.Property<int>(x, "ID"), x => x.PersonID)
				.Build();

			return ms;
		}

		public class PersonWithDynamicStore
		{
			[Column("PersonID"), Identity]
			public int ID { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		[Table("Person")]
		public class PersonWithoutDynamicStore
		{
			[Column("PersonID"), Identity, PrimaryKey]
			public int ID { get; set; }
		}

		[Table("DynamicTable")]
		public class DynamicTablePrototype
		{
			[Column, Identity, PrimaryKey]
			public int ID { get; set; }

			[Column("Not Identifier")]
			public int NotIdentifier { get; set; }

			[Column("Some Value")]
			public int Value { get; set; }
		}

		[Table("DynamicTable")]
		public class DynamicTable
		{
			[Column, Identity, PrimaryKey]
			public int ID { get; set; }
		}

		public class SomeClassWithDynamic
		{
			public string? Description { get; set; }

			private sealed class SomeClassEqualityComparer : IEqualityComparer<SomeClassWithDynamic>
			{
				public bool Equals(SomeClassWithDynamic? x, SomeClassWithDynamic? y)
				{
					if (ReferenceEquals(x, y))      return true;
					if (ReferenceEquals(x, null))   return false;
					if (ReferenceEquals(y, null))   return false;
					if (x.GetType() != y.GetType()) return false;
					if (!string.Equals(x.Description, y.Description))
						return false;

					if (x.ExtendedProperties == null && y.ExtendedProperties == null)
						return true;

					if (x.ExtendedProperties == null || y.ExtendedProperties == null)
						return false;

					bool CompareValues(IDictionary<string, object> values1, IDictionary<string, object> values2)
					{
						foreach (var property in values1)
						{
							var value1 = property.Value as string ?? string.Empty;
							values2.TryGetValue(property.Key, out var value);
							var value2 = value as string ?? string.Empty;
							if (!string.Equals(value1, value2))
								return false;
						}

						return true;
					}

					return CompareValues(x.ExtendedProperties, y.ExtendedProperties) &&
						   CompareValues(y.ExtendedProperties, x.ExtendedProperties);
				}

				public int GetHashCode(SomeClassWithDynamic obj)
				{
					return (obj.Description != null ? obj.Description.GetHashCode() : 0);
				}
			}

			public static IEqualityComparer<SomeClassWithDynamic> SomeClassComparer { get; } = new SomeClassEqualityComparer();

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		[Test]
		public void TestConcatWithDynamic([IncludeDataSources(true, TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
		{
			var mappingSchema = new MappingSchema();
			var builder = new FluentMappingBuilder(mappingSchema)
				.Entity<SomeClassWithDynamic>();

			builder.Property(x => x.Description).HasColumnName("F066_04");
			builder.Property(x => Sql.Property<string>(x, "F066_05"));
			builder.Property(x => Sql.Property<string>(x, "F066_00"));

			builder.Build();

			var testData1 = new[]
			{
				new SomeClassWithDynamic{Description = "Desc1", ExtendedProperties = new Dictionary<string, object>{{"F066_05", "v1"}}},
				new SomeClassWithDynamic{Description = "Desc2", ExtendedProperties = new Dictionary<string, object>{{"F066_05", "v2"}}},
			};

			var testData2 = new[]
			{
				new SomeClassWithDynamic{Description = "Desc3", ExtendedProperties = new Dictionary<string, object>{{"F066_00", "v3"}}},
				new SomeClassWithDynamic{Description = "Desc4", ExtendedProperties = new Dictionary<string, object>{{"F066_00", "v4"}}},
			};

			using var dataContext = GetDataContext(context, mappingSchema);
			using (dataContext.CreateLocalTable("M998_T066", testData1))
			using (dataContext.CreateLocalTable("M998_T000", testData2))
			{
				var expected = testData1.Concat(testData2);
				var result =
						dataContext.GetTable<SomeClassWithDynamic>().TableName("M998_T066")
							.Concat(dataContext.GetTable<SomeClassWithDynamic>().TableName("M998_T000"))
							.ToList();

				AreEqual(expected, result, SomeClassWithDynamic.SomeClassComparer);
			}
		}

		sealed class BananaTable
		{
			public int Id { get; set; }
			public string? Property { get; set; }
		}

		[Test(Description = "https://stackoverflow.com/questions/61081571: Expression 't.Id' is not a Field.")]
		public void DynamicGoesBanana1([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BananaTable>())
			{
				db.GetTable<BananaTable>().Insert(() => new BananaTable() { Id = 1, Property = "test1" });

				var res = db.GetTable<BananaTable>().ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].Property, Is.EqualTo("test1"));

				Test(nameof(BananaTable), nameof(BananaTable.Id), nameof(BananaTable.Property), 1, "banana");

				res = db.GetTable<BananaTable>().ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].Property, Is.EqualTo("banana"));

				void Test(string entity, string filterProperty, string changedProperty, object filter, object value)
				{
					db.GetTable<object>()
						.TableName(entity)
						.Where(t => Sql.Property<object>(t, filterProperty).Equals(filter))
						.Set(t => Sql.Property<object>(t, changedProperty), value)
						.Update();
				}
			}
		}

		[Test]
		public void DynamicGoesBanana2([IncludeDataSources(true, TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BananaTable>())
			{
				db.GetTable<BananaTable>().Insert(() => new BananaTable() { Id = 1, Property = "test1" });

				var res = db.GetTable<BananaTable>().ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].Property, Is.EqualTo("test1"));

				Test<BananaTable>(nameof(BananaTable), nameof(BananaTable.Id), nameof(BananaTable.Property), 1, "banana");

				res = db.GetTable<BananaTable>().ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].Property, Is.EqualTo("banana"));

				void Test<TEntity>(string entity, string filterProperty, string changedProperty, object filter, object value)
					where TEntity : class
				{
					db.GetTable<TEntity>()
						.TableName(entity)
						.Where(t => Sql.Property<TEntity>(t, filterProperty)!.Equals(filter))
						.Set(t => Sql.Property<TEntity>(t, changedProperty)!, value)
						.Update();
				}
			}
		}

		[Test]
		public void Issue3158([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			Assert.DoesNotThrow(() =>
			{
				(from p in db.GetTable<PersonWithoutDynamicStore>()
				 join d in db.Doctor on p.ID equals d.PersonID
				 from pa in db.Patient.LeftJoin(pa => pa.Diagnosis == Sql.Property<string>(p, "FirstName"))
				 select new { p.ID, pa.Diagnosis })
				.ToList();
			});
		}

		#region Issue 4483
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4483")]
		public void Issue4483Test1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			var ms = new MappingSchema();
			var fm = new FluentMappingBuilder(ms);

			const int readColCount = 5;
			foreach (var col in Enumerable.Range(0, readColCount))
			{
				var colExpr = $"JSON_VALUE({nameof(TestJsonWrite.JsonData)}, '$.\"{col}\"') AS '{col}'";
				fm.Entity<TestJsonRead>()
					.Property(x => Sql.Property<float?>(x, col.ToString()))
					.IsExpression(row => Sql.Expr<float?>(colExpr), isColumn: true)
					;
			}

			fm.Build();

			var id = 0;

			var testData = Enumerable.Range(0, 100)
					.Select
					(
						f =>
						{
							var map = Enumerable.Range(0, 1000)
								.Select(p => new KeyValuePair<string, int>(p.ToString(), p))
								.ToDictionary(f => f.Key, f => f.Value);
							return new TestJsonWrite
							{
								Id = id++,
								JsonData = JsonSerializer.Serialize(map)
							};
						}
					)
					.ToDictionary(f => f.Id);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(testData.Values);

			var testRows = db.GetTable<TestJsonRead>().ToArray();
			foreach (var testRow in testRows)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(testData, Does.ContainKey(testRow.Id));
					Assert.That(testRow.Values, Has.Count.EqualTo(readColCount));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4483")]
		public void Issue4483Test2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			var ms = new MappingSchema();
			var fm = new FluentMappingBuilder(ms);

			const int readColCount = 5;
			foreach (var col in Enumerable.Range(0, readColCount))
			{
				var colExpr = $"JSON_VALUE({nameof(TestJsonWrite.JsonData)}, '$.\"{col}\"') AS '{col}'";
				fm.Entity<TestJsonRead>()
					.Property(x => Sql.Property<string?>(x, col.ToString()))
					.IsExpression(row => Sql.Expr<string?>(colExpr), isColumn: true)
					;
			}

			fm.Build();

			var id = 0;

			var testData = Enumerable.Range(0, 100)
					.Select
					(
						f =>
						{
							var map = Enumerable.Range(0, 1000)
								.Select(p => new KeyValuePair<string, int>(p.ToString(), p))
								.ToDictionary(f => f.Key, f => f.Value);
							return new TestJsonWrite
							{
								Id = id++,
								JsonData = JsonSerializer.Serialize(map)
							};
						}
					)
					.ToDictionary(f => f.Id);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(testData.Values);

			var testRows = db.GetTable<TestJsonRead>().ToArray();
			foreach (var testRow in testRows)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(testData, Does.ContainKey(testRow.Id));
					Assert.That(testRow.Values, Has.Count.EqualTo(readColCount));
				}
			}
		}

		[Table]
		class TestJsonWrite
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(DbType = "NVARCHAR(MAX)")]
			public string? JsonData { get; set; }
		}

		[Table(Name = nameof(TestJsonWrite))]
		class TestJsonRead
		{
			[PrimaryKey]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object?> Values { get; set; } = new();
		}
		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2817")]
		public void Issue2817Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<Person> query = from p in db.Person
				   orderby p.LastName
				   select p;

			query = ApplyFilterToGeneric(query);

			query.ToList();

			static IQueryable<T> ApplyFilterToGeneric<T>(IQueryable<T> query) => query.Where(p => Sql.Property<string>(p, "LastName") == "ministra");
		}

		[Test]
		public void Issue4602([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.AddMetadataReader(new CustomMetadataReader());

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicParent>())
			using (db.CreateLocalTable<DynamicChild>())
			{
				Assert.DoesNotThrowAsync(async () =>
				{
					await db.GetTable<DynamicParent>()
						.Where(it => it.Child!.ID == 123)
						.ToArrayAsync();
				});
			}
		}

		public class DynamicParent
		{
			[Column, PrimaryKey, Identity]
			public int ID { get; set; }

			[Association(ThisKey = "ID", OtherKey = "ParentID")]
			public DynamicChild? Child { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		public class DynamicChild
		{
			[Column, PrimaryKey, Identity]
			public int ID { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ID")]
			public DynamicParent Parent { get; set; } = null!;

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}
		
		class CustomMetadataReader: IMetadataReader
		{
			public MappingAttribute[] GetAttributes(Type type)
			{
				return Array.Empty<MappingAttribute>();
			}

			public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
			{
				if (type != typeof(DynamicChild) || memberInfo.Name != "ParentID")
					return Array.Empty<MappingAttribute>();

				return new[]
				{
					new ColumnAttribute("ParentID")
				};
			}

			public MemberInfo[] GetDynamicColumns(Type type)
			{
				if (type != typeof(DynamicChild))
					return Array.Empty<MemberInfo>();

				return new[]
				{
					new DynamicColumnInfo(typeof(DynamicChild), typeof(int), "ParentID"),
				};
			}

			public string GetObjectID()
			{
				return $".{nameof(CustomMetadataReader)}";
			}
		}

		#region Issue 4770

		sealed class Issue4770Person
		{
			public int Id { get; set; }
			public Issue4770Address? Address { get; set; }
			public string ?TestPostcode { get; set; }
		}

		sealed class Issue4770Address
		{
			public string? Postcode { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4770")]
		public void Issue4770([DataSources] string context)
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);
			fb.Entity<Issue4770Person>()
				.Property(c => c.Id).IsPrimaryKey()
				.Property(c => c.Address!.Postcode).IsExpression(c => Sql.Upper(Sql.Property<string>(c, "Postcode")), true).IsColumn()
				.Property(c => c.TestPostcode).IsExpression(c => Sql.Upper(Sql.Property<string>(c, "Postcode")), true);

			fb.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Issue4770Person>();

			tb.ToArray();
		}
		#endregion
	}
}
