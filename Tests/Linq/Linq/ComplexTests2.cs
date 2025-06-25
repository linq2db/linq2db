using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	/// <summary>
	/// Tests:
	///
	/// Complex Property Mapping
	/// Inheritance Mapping
	/// LoadWith for Inheritance
	/// LoadWith with Casts
	/// Nested LoadWith
	/// String Enums
	/// Converters for Enums
	/// </summary>
	[TestFixture]
	public class ComplexTests2 : TestBase
	{
		public enum AnimalType
		{
			Small,
			Big
		}

		public enum AnimalType2
		{
			Small,
			Big
		}

		public class Eye
		{
			public int     Id { get; set; }
			public string? Xy { get; set; }
		}

		public class SauronsEye : Eye
		{
			public int Power { get; set; }
		}

		public class Name
		{
			public string? First  { get; set; }
			public string? Second { get; set; }
		}

		public class Animal
		{
			public AnimalType  AnimalType    { get; set; }
			public AnimalType2 AnimalType2   { get; set; }
			public int         Id            { get; set; }
			public string?     Name          { get; set; }
			public string?     Discriminator { get; set; }
		}

		public class WildAnimal : Animal
		{
			public WildAnimal()
			{
				Discriminator = "WildAnimal";
			}
		}

		public class SuperWildAnimal : WildAnimal
		{
			public SuperWildAnimal()
			{
				Discriminator = "SuperWildAnimal";
			}
		}

		public class Dog : SuperWildAnimal
		{
			public Dog()
			{
				Discriminator = "Dog";
			}

			public Eye?  Bla     { get; set; }
			public int?  EyeId   { get; set; }
			public Name? DogName { get; set; }
		}

		public class BadDog : Dog
		{
		}

		public class SuperBadDog : BadDog
		{
		}

		public class Test
		{
			public int     Id           { get; set; }
			public int?    TestAnimalId { get; set; }
			public Animal? TestAnimal   { get; set; }
		}

		private void InsertData(ITestDataContext db)
		{
			var eye = new Eye
			{
				Id = 1,
				Xy = "Hallo"
			};

			var dog = new Dog
			{
				Id = 1,
				Discriminator = "Dog",
				EyeId         = 1,
				Name          = "FirstDog",
				DogName       = new Name { First = "a", Second = "b" },
				AnimalType    = AnimalType.Big,
				AnimalType2   = AnimalType2.Big
			};

			var wildAnimal = new WildAnimal
			{
				Id = 2,
				Discriminator = "WildAnimal",
				Name          = "WildAnimal",
				AnimalType    = AnimalType.Big,
				AnimalType2   = AnimalType2.Big
			};

			var test = new Test
			{
				Id           = 1,
				TestAnimalId = null
			};

			var test2 = new Test
			{
				Id           = 2,
				TestAnimalId = 1
			};

			using (new DisableLogging())
			{
				db.Insert(eye);
				db.Insert(dog);
				db.Insert(wildAnimal);
				db.Insert(test);
				db.Insert(test2);
			}
		}

		MappingSchema SetMappings()
		{
			var cnt = TestUtils.GetNext().ToString();

			var ms = new MappingSchema(cnt);

			ms.SetConverter<AnimalType, string>       (obj => obj.ToString());
			ms.SetConverter<AnimalType, DataParameter>(obj => new DataParameter { Value = obj.ToString() });
#pragma warning disable CA2263 // Prefer generic overload when type is known
			ms.SetConverter<string, AnimalType>       (txt => (AnimalType)Enum.Parse(typeof(AnimalType), txt, true));
#pragma warning restore CA2263 // Prefer generic overload when type is known

			ms.SetDefaultFromEnumType(typeof(AnimalType2), typeof(string));

			var animalsTableName = "Animals" + cnt;
			var eyeTableName     = "Eyes"    + cnt;

			var mappingBuilder = new FluentMappingBuilder(ms);

			mappingBuilder.Entity<Animal>()
				.HasTableName(animalsTableName)
				.Inheritance(x => x.Discriminator, "Dog",             typeof(Dog))
				.Inheritance(x => x.Discriminator, "WildAnimal",      typeof(WildAnimal))
				.Inheritance(x => x.Discriminator, "SuperWildAnimal", typeof(SuperWildAnimal))

				.Property(x => x.Name         ).IsColumn().IsNullable().HasColumnName("Name")
				.Property(x => x.AnimalType   ).IsColumn().HasColumnName("AnimalType").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.AnimalType2  ).IsColumn().HasColumnName("AnimalType2").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.Id           ).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

			mappingBuilder.Entity<Dog>()
				.HasTableName(animalsTableName)
				.Property(x => x.Bla           ).IsNotColumn()
				.Property(x => x.EyeId         ).IsColumn().IsNullable().HasColumnName("EyeId")
				.Property(x => x.DogName!.Second).HasColumnName("Second").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.DogName!.First ).HasColumnName("First").HasDataType(DataType.NVarChar).HasLength(40)
				.Association(x => x.Bla, x => x.EyeId, x => x!.Id);

			mappingBuilder.Entity<WildAnimal>()
				.HasTableName(animalsTableName);

			mappingBuilder.Entity<SuperWildAnimal>()
				.HasTableName(animalsTableName);

			mappingBuilder.Entity<Eye>()
				.HasTableName(eyeTableName)
				.Property(x => x.Id).IsColumn().HasColumnName("Id").IsPrimaryKey()
				.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy").HasDataType(DataType.NVarChar).HasLength(40);

			mappingBuilder.Entity<SauronsEye>()
				.Property(x => x.Power).IsColumn().HasColumnName("power");

			mappingBuilder.Entity<Test>()
				.HasTableName("TestAnimalTable")
				.Association(x => x.TestAnimal, x => x.TestAnimalId, x => x!.Id)
				.Property(x => x.TestAnimalId).IsColumn().IsNullable().HasColumnName("TestAnimalId")
				.Property(x => x.TestAnimal  ).IsNotColumn();

			mappingBuilder.Build();

			return ms;
		}

		[Test]
		public void TestQueryForBaseType([DataSources] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);
				var data =  db.GetTable<Animal>().OrderBy(_ => _.Id).ToList();
				Assert.That(((Dog)data.First()).Bla, Is.Null);
			}
		}

		[Test]
		public void TestLoadWithWithCast([DataSources(false)] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, o => o.UseMappingSchema(ms)))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);
				var data = db.GetTable<Animal>().LoadWith(x => ((Dog)x).Bla).OrderBy(_ => _.Id).ToList();
				Assert.That(((Dog)data.First()).Bla, Is.Not.Null);
			}
		}

		[Test]
		public void TestNestedLoadWithWithCast([DataSources(false)] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, o => o.UseMappingSchema(ms)))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);
				var data = db.GetTable<Test>()
					.LoadWith(x => ((Dog)x.TestAnimal!).Bla)
					.OrderBy(x => x.Id)
					.ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data.First().TestAnimal, Is.Null);
					Assert.That(((Dog)data.Skip(1).First().TestAnimal!).Bla, Is.Not.Null);
					Assert.That(((Dog)data.Skip(1).First().TestAnimal!).DogName!.First, Is.Not.Null);
					Assert.That(((Dog)data.Skip(1).First().TestAnimal!).DogName!.Second, Is.Not.Null);
				}
			}
		}

		[Test]
		public void TestComplexPropertyLoading([DataSources] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);
				var data = db.GetTable<Dog>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].DogName!.First, Is.Not.Null);
					Assert.That(data[0].DogName!.Second, Is.Not.Null);
				}
			}
		}

		[Test]
		public void TestStringAndConverterEnums([DataSources(false)] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, o => o.UseMappingSchema(ms)))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);
				var d = new Dog() { AnimalType = AnimalType.Big, AnimalType2 = AnimalType2.Big };
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.GetTable<Dog>().First(x => x.AnimalType == AnimalType.Big), Is.Not.Null);
					Assert.That(db.GetTable<Dog>().First(x => x.AnimalType == d.AnimalType), Is.Not.Null);

					Assert.That(db.GetTable<Dog>().First(x => x.AnimalType2 == AnimalType2.Big), Is.Not.Null);
					Assert.That(db.GetTable<Dog>().First(x => x.AnimalType2 == d.AnimalType2), Is.Not.Null);

					Assert.That(db.GetTable<Animal>().First(x => x is SuperWildAnimal), Is.Not.Null);

					Assert.That(db.GetTable<Test>().First(x => x.TestAnimal is Dog && ((Dog)x.TestAnimal).EyeId == 1), Is.Not.Null);
				}
			}
		}

		[Test]
		public void TestUpdateWithTypeAndBaseType([DataSources] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);

				var dog = db.GetTable<Dog>().First();

				db.Update(dog);
				db.Update((Animal)dog);

				var bdog = new SuperBadDog();
				db.Insert((Dog)bdog);
			}
		}

		public class PersonDerived : Person
		{
			[Column]
			public int ColumnForOtherDB;
		}

		[Test]
		public void TestInsertUsingDerivedObjectUsingAttributes([DataSources] string context)
		{
			ResetPersonIdentity(context);

			var ms = SetMappings();
			using (var db = GetDataContext(context, ms))
			using (new RestoreBaseTables(db))
			{
				Person person = new PersonDerived()
				{
					FirstName        = "test_inherited_insert",
					LastName         = "test",
					MiddleName       = "test",
					Gender           = Gender.Unknown,
					ColumnForOtherDB = 100500
				};

				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					person.ID = 10500;
					db.Insert(person);
				}
				else
					person.ID = db.InsertWithInt32Identity(person);

				Validate();

				db.Update(person);
				Validate();

				db.Delete(person);

				void Validate()
				{
					var data = db.GetTable<Person>().FirstOrDefault(_ => _.FirstName == "test_inherited_insert")!;
					Assert.That(data, Is.Not.Null);
					using (Assert.EnterMultipleScope())
					{
						Assert.That(data.ID, Is.EqualTo(person.ID));
						Assert.That(data.FirstName, Is.EqualTo(person.FirstName));
						Assert.That(data.LastName, Is.EqualTo(person.LastName));
						Assert.That(data.MiddleName, Is.EqualTo(person.MiddleName));
						Assert.That(data.Gender, Is.EqualTo(person.Gender));
					}
				}
			}
		}

		[Test]
		public void TestInsertUsingDerivedObjectUsingFluentMapping([InsertOrUpdateDataSources] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);

				Eye eye = new SauronsEye()
				{
					Id = 123,
					Xy = "test321"
				};

				var cnt = db.Insert(eye);
				Validate(false);

				cnt = db.InsertOrReplace(eye);
				Validate(true);

				cnt = db.Update(eye);
				Validate(false);

				db.Delete(eye);

				void Validate(bool insertOrReplace)
				{
					if (insertOrReplace && context.IsAnyOf(TestProvName.AllOracleNative))
						Assert.That(cnt, Is.EqualTo(-1));
					else
						Assert.That(cnt, Is.EqualTo(1));

					var data = db.GetTable<Eye>().Where(_ => _.Id == 123).FirstOrDefault()!;
					Assert.That(data, Is.Not.Null);
					using (Assert.EnterMultipleScope())
					{
						Assert.That(data.Id, Is.EqualTo(eye.Id));
						Assert.That(data.Xy, Is.EqualTo(eye.Xy));
					}
				}
			}
		}

		[Test]
		public void TestInheritanceByBaseType([InsertOrUpdateDataSources] string context)
		{
			var ms = SetMappings();

			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<Eye>())
			using (db.CreateLocalTable<Test>())
			{
				InsertData(db);

				var dog = new Dog()
				{
					Id          = 666,
					AnimalType  = AnimalType.Big,
					AnimalType2 = AnimalType2.Small,
					Name        = "Cerberus",
					DogName     = new Name()
					{
						First  = "Good",
						Second = "Dog"
					},
					EyeId = 2
				};

				var cnt = db.Insert((Animal)dog);
				Validate(false);

				cnt = db.InsertOrReplace((Animal)dog);
				Validate(true);

				cnt = db.Update((Animal)dog);
				Validate(false);

				db.Delete((Animal)dog);

				void Validate(bool insertOrReplace)
				{
					if (insertOrReplace && context.IsAnyOf(TestProvName.AllOracleNative))
						Assert.That(cnt, Is.EqualTo(-1));
					else
						Assert.That(cnt, Is.EqualTo(1));

					var data = db.GetTable<Dog>().Where(_ => _.Id == 666).FirstOrDefault()!;
					Assert.That(data, Is.Not.Null);
					using (Assert.EnterMultipleScope())
					{
						Assert.That(data.Id, Is.EqualTo(dog.Id));
						Assert.That(data.AnimalType, Is.EqualTo(dog.AnimalType));
						Assert.That(data.AnimalType2, Is.EqualTo(dog.AnimalType2));
						Assert.That(data.Name, Is.EqualTo(dog.Name));
						Assert.That(data.DogName!.First, Is.EqualTo(dog.DogName.First));
						Assert.That(data.DogName!.Second, Is.EqualTo(dog.DogName.Second));
						Assert.That(data.EyeId, Is.EqualTo(dog.EyeId));
					}
				}
			}
		}
	}
}
