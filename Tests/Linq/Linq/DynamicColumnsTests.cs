using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DynamicColumnsTests : TestBase
	{
		// Introduced to ensure that we process not only constants in column names
		private static string IDColumn        = "ID";
		private static string DiagnosisColumn = "Diagnosis";
		private static string PatientColumn   = "Patient";

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<int>(x, IDColumn) == 1)
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("John", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNavigationalNonDynamicColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(x.Patient, DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociation(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociationViaObject1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => (string)Sql.Property<object>(Sql.Property<object>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociationViaObject2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
				var result = db.GetTable<Person>()
					.Select(x => Sql.Property<object>(Sql.Property<object>(x, PatientColumn), DiagnosisColumn))
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _ as string).SequenceEqual(expected.OrderBy(_ => _)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithDynamicColumn(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(1, result.Single().ID);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithDynamicAssociation(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(2, result.Single().ID);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectAll(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Select(p => p.FirstName).ToList();
				var result = db.GetTable<PersonWithDynamicStore>().ToList().Select(p => p.ExtendedProperties["FirstName"]).ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectOne(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Select(p => p.FirstName).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.Select(x => Sql.Property<string>(x, "FirstName"))
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectProject(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
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

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();

				var result = db.GetTable<PersonWithDynamicStore>()
					.Select(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWhere(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Where(p => p.FirstName == "John").Select(p => p.ID).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWhereAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Where(p => p.Patient?.Diagnosis != null).Select(p => p.ID).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn) != null)
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyOrderBy(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.OrderByDescending(p => p.FirstName).Select(p => p.ID).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.OrderByDescending(x => Sql.Property<string>(x, "FirstName"))
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyOrderByAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.OrderBy(p => p.Patient?.Diagnosis).Select(p => p.ID).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.OrderBy(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyGroupBy(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.GroupBy(p => p.FirstName).Select(p => new {p.Key, Count = p.Count()}).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.GroupBy(x => Sql.Property<string>(x, "FirstName"))
					.Select(p => new {p.Key, Count = p.Count()})
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _.Key).SequenceEqual(expected.OrderBy(_ => _.Key)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyGroupByAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.GroupBy(p => p.Patient?.Diagnosis).Select(p => new {p.Key, Count = p.Count()}).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.GroupBy(x => Sql.Property<string>(Sql.Property<Patient>(x, PatientColumn), DiagnosisColumn))
					.Select(p => new {p.Key, Count = p.Count()})
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _.Key).SequenceEqual(expected.OrderBy(_ => _.Key)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyJoin(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected =
					from p in Person
					join pa in Patient on p.FirstName equals pa.Diagnosis
					select p;

				var result =
					from p in db.Person
					join pa in db.Patient on Sql.Property<string>(p, "FirstName") equals Sql.Property<string>(pa, DiagnosisColumn)
					select p;

				Assert.IsTrue(result.ToList().SequenceEqual(expected.ToList()));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyLoadWith(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
				var result = db.GetTable<PersonWithDynamicStore>()
					.LoadWith(x => Sql.Property<Patient>(x, "Patient"))
					.ToList()
					.Select(p => ((Patient)p.ExtendedProperties[PatientColumn])?.Diagnosis)
					.ToList();

				Assert.IsTrue(result.OrderBy(_ => _).SequenceEqual(expected.OrderBy(_ => _)));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyNoStoreGrouping1(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicClass()))
			{
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
		}

		[Test, DataContextSource]
		public void SqlPropertyNoStoreGrouping2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		public void CreateTestTable<T>(IDataContext db, string tableName = null)
		{
			db.DropTable<T>(tableName, throwExceptionIfNotExists: false);
			db.CreateTable<T>(tableName);
		}

		[Test, DataContextSource]
		public void SqlPropertyNoStoreNonIdentifier(string context)
		{
			using (new FirebirdQuoteMode(FirebirdIdentifierQuoteMode.Auto))
			using (var db = GetDataContext(context))
			{
				CreateTestTable<DynamicTablePrototype>(db);
				try
				{
					db.Insert(new DynamicTablePrototype { NotIdentifier = 77 });

					var query =
						from d in db.GetTable<DynamicTable>()
						select new
						{
							NI = Sql.Property<int>(d, "Not Identifier")
						};

					var result = query.ToArray();

					Assert.AreEqual(77, result[0].NI);
				}
				finally
				{
					db.DropTable<DynamicTablePrototype>();
				}
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyNoStoreNonIdentifierGrouping(string context)
		{
			using (new FirebirdQuoteMode(FirebirdIdentifierQuoteMode.Auto))
			using (var db = GetDataContext(context))
			{
				CreateTestTable<DynamicTablePrototype>(db);
				try
				{
					db.Insert(new DynamicTablePrototype { NotIdentifier = 77, Value = 5 });
					db.Insert(new DynamicTablePrototype { NotIdentifier = 77, Value = 5 });

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

					Assert.AreEqual(77, result[0].NI);
					Assert.AreEqual(2,  result[0].Count);
					Assert.AreEqual(10, result[0].Sum);
				}
				finally
				{
					db.DropTable<DynamicTablePrototype>();
				}
			}
		}

		private MappingSchema ConfigureDynamicClass()
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<PersonWithDynamicStore>().HasTableName("Person")
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID"))
				.Property(x => Sql.Property<string>(x, "FirstName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "LastName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "MiddleName"))
				.Association(x => Sql.Property<Patient>(x, "Patient"), x => Sql.Property<int>(x, "ID"), x => x.PersonID);

			return ms;
		}

		public class PersonWithDynamicStore
		{
			[Column("PersonID"), Identity]
			public int ID { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; }
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
			public string Description { get; set; }

			private sealed class SomeClassEqualityComparer : IEqualityComparer<SomeClassWithDynamic>
			{
				public bool Equals(SomeClassWithDynamic x, SomeClassWithDynamic y)
				{
					if (ReferenceEquals(x, y)) return true;
					if (ReferenceEquals(x, null)) return false;
					if (ReferenceEquals(y, null)) return false;
					if (x.GetType() != y.GetType()) return false;
					if (!string.Equals(x.Description, y.Description))
						return false;

					if (x.ExtendedProperties == null && x.ExtendedProperties == null)
						return true;

					if (x.ExtendedProperties == null || x.ExtendedProperties == null)
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
			public IDictionary<string, object> ExtendedProperties { get; set; }
		}

		[Test]
		[Combinatorial]
		public void TestConcatWithDynamic([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var mappingSchema = new MappingSchema();
			var builder = mappingSchema.GetFluentMappingBuilder()
				.Entity<SomeClassWithDynamic>();

			builder.Property(x => x.Description).HasColumnName("F066_04");
			builder.Property(x => Sql.Property<string>(x, "F066_05"));
			builder.Property(x => Sql.Property<string>(x, "F066_00"));

			var testData1 = new SomeClassWithDynamic[]
			{
				new SomeClassWithDynamic{Description = "Desc1", ExtendedProperties = new Dictionary<string, object>{{"F066_05", "v1"}}},
				new SomeClassWithDynamic{Description = "Desc2", ExtendedProperties = new Dictionary<string, object>{{"F066_05", "v2"}}},
			};

			var testData2 = new SomeClassWithDynamic[]
			{
				new SomeClassWithDynamic{Description = "Desc3", ExtendedProperties = new Dictionary<string, object>{{"F066_00", "v3"}}},
				new SomeClassWithDynamic{Description = "Desc4", ExtendedProperties = new Dictionary<string, object>{{"F066_00", "v4"}}},
			};

			using (var dataContext = GetDataContext(context, mappingSchema))
			{
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
		}

	}
}
