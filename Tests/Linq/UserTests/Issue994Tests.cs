using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests;
using Tests.Model;


[TestFixture]
public class Issue994Tests : TestBase
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
		public int Id { get; set; }
		public string Xy { get; set; }
	}

	public class Name
	{
		public string First { get; set; }
		public string Second { get; set; }
	}

	public class Animal
	{
		public AnimalType AnimalType { get; set; }

		public AnimalType2 AnimalType2 { get; set; }

		public int Id { get; set; }

		public string Name { get; set; }

		public string Discriminator { get; set; }
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

		public Eye Bla { get; set; }

		public int? EyeId { get; set; }

		public Name DogName { get; set; }
	}

	public class Test
	{
		public int Id { get; set; }
		public int? TestAnimalId { get; set; }
		public Animal TestAnimal { get; set; }
	}

	[Test]
	public void TestIssue()
	{
		SetMappings();

		InsertData();

		var listTest = LoadTest();

		Assert.Null(((Dog)listTest.First()).Bla);

		var listTest2 = LoadTest2();

		Assert.NotNull(((Dog)listTest2.First()).Bla);

		var listTest3 = LoadTest3();

		Assert.Null(listTest3.First().TestAnimal);
		Assert.NotNull(((Dog)listTest3.Skip(1).First().TestAnimal).Bla);
		Assert.NotNull(((Dog)listTest3.Skip(1).First().TestAnimal).DogName.First);
		Assert.NotNull(((Dog)listTest3.Skip(1).First().TestAnimal).DogName.Second);

		var listTest4 = LoadTest4();
		Assert.NotNull(listTest4[0].DogName.First);
		Assert.NotNull(listTest4[0].DogName.Second);

		LoadTest5();

		Test6();
	}

	private void InsertData()
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
			EyeId = 1,
			Name = "FirstDog",
			DogName = new Name {First = "a", Second = "b"},
			AnimalType = AnimalType.Big,
			AnimalType2 = AnimalType2.Big
		};

		var test = new Test
		{
			Id = 1,
			TestAnimalId = null
		};

		var test2 = new Test
		{
			Id = 2,
			TestAnimalId = 1
		};

		using (var db = new TestDataConnection())
		{
			db.SetCommand("DROP TABLE IF EXISTS `Animals`").Execute();
			db.SetCommand("DROP TABLE IF EXISTS `Eyes`").Execute();
			db.SetCommand("DROP TABLE IF EXISTS `Test`").Execute();
			db.SetCommand("CREATE TABLE `Animals` ( `Id` INTEGER NOT NULL PRIMARY KEY, `AnimalType` TEXT, `AnimalType2` TEXT, `Name` TEXT, `Discriminator` TEXT, `EyeId` INTEGER, `First` TEXT, `Second` TEXT )").Execute();
			db.SetCommand("CREATE TABLE `Eyes` ( `Id` INTEGER NOT NULL PRIMARY KEY, `Xy` TEXT )").Execute();
			db.SetCommand("CREATE TABLE `Test` ( `Id` INTEGER NOT NULL PRIMARY KEY, `TestAnimalId` INTEGER NULL )").Execute();
		}

		using (var db = new TestDataConnection())
		{
			db.GetTable<Eye>().Delete();
			db.GetTable<Animal>().Delete();

			db.Insert(eye);
			db.Insert(dog);
			db.Insert(test);
			db.Insert(test2);
		}
	}

	private List<Animal> LoadTest()
	{
		using (var db = new TestDataConnection())
		{
			return db.GetTable<Animal>().ToList();
		}
	}

	private List<Animal> LoadTest2()
	{
		using (var db = new TestDataConnection())
		{
			return db.GetTable<Animal>().LoadWith(x => ((Dog)x).Bla).ToList();
		}
	}

	private List<Test> LoadTest3()
	{
		using (var db = new TestDataConnection())
		{
			return db.GetTable<Test>().LoadWith(x => ((Dog)x.TestAnimal).Bla).ToList();
		}
	}

	private List<Dog> LoadTest4()
	{
		using (var db = new TestDataConnection())
		{
			return db.GetTable<Dog>().ToList();
		}
	}
	private void LoadTest5()
	{
		using (var db = new TestDataConnection())
		{
			var d = new Dog() { AnimalType = AnimalType.Big, AnimalType2 = AnimalType2.Big };

			var test1 =  db.GetTable<Dog>().First(x => x.AnimalType == AnimalType.Big);
			var test2 = db.GetTable<Dog>().First(x => x.AnimalType == d.AnimalType);

			var test3 = db.GetTable<Dog>().First(x => x.AnimalType2 == AnimalType2.Big);
			var test4 = db.GetTable<Dog>().First(x => x.AnimalType2 == d.AnimalType2);

			var test6 = db.GetTable<Animal>().First(x => x is SuperWildAnimal);

			var test7 = db.GetTable<Test>().First(x => x.TestAnimal is Dog && ((Dog)x.TestAnimal).EyeId == 1);
			var sql = db.LastQuery;
		}
	}

	private void Test6()
	{
		using (var db = new TestDataConnection())
		{
			var dog = db.GetTable<Dog>().First();
			db.Update(dog);
			db.Update((Animal)dog);
		}
	}

	private void SetMappings()
	{
		MappingSchema.Default.SetConverter<AnimalType, string>((obj) =>
		{
			return obj.ToString();
		});
		MappingSchema.Default.SetConverter<AnimalType, DataParameter>((obj) =>
		{
			return new DataParameter { Value = obj.ToString() };
		});
		MappingSchema.Default.SetConverter<string, AnimalType>((txt) =>
		{
			return (AnimalType)Enum.Parse(typeof(AnimalType), txt, true);
		});
		MappingSchema.Default.SetDefaultFromEnumType(typeof(AnimalType2), typeof(string));


		var mappingBuilder = MappingSchema.Default.GetFluentMappingBuilder();
		mappingBuilder.Entity<Animal>()
			.HasTableName("Animals")
			.Inheritance(x => x.Discriminator, "Dog", typeof(Dog))
			.Inheritance(x => x.Discriminator, "WildAnimal", typeof(WildAnimal))
			.Inheritance(x => x.Discriminator, "SuperWildAnimal", typeof(SuperWildAnimal))
			.Property(x => x.Name).IsColumn().IsNullable().HasColumnName("Name")
			.Property(x => x.AnimalType).IsColumn().HasColumnName("AnimalType").HasDataType(DataType.NVarChar)
			.Property(x => x.AnimalType2).IsColumn().HasColumnName("AnimalType2").HasDataType(DataType.NVarChar)
			.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator")
			.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

		mappingBuilder.Entity<Dog>()
			.HasTableName("Animals")
			.Property(x => x.Bla).IsNotColumn()
			.Property(x => x.EyeId).IsColumn().IsNullable().HasColumnName("EyeId")
			.Property(x => x.DogName.Second).HasColumnName("Second")
			.Property(x => x.DogName.First).HasColumnName("First")
			.Association(x => x.Bla, x => x.EyeId, x => x.Id);

		mappingBuilder.Entity<WildAnimal>()
			.HasTableName("Animals");

		mappingBuilder.Entity<SuperWildAnimal>()
			.HasTableName("Animals");

		mappingBuilder.Entity<Eye>()
			.HasTableName("Eyes")
			.Property(x => x.Id).IsColumn().HasColumnName("Id")
			.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy");

		mappingBuilder.Entity<Test>()
			.HasTableName("Test")
			.Association(x => x.TestAnimal, x => x.TestAnimalId, x => x.Id)
			.Property(x => x.TestAnimalId).IsColumn().IsNullable().HasColumnName("TestAnimalId")
			.Property(x => x.TestAnimal).IsNotColumn();
	}
}
