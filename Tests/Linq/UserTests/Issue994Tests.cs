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
	public class Eye
	{
		public int Id { get; set; }
		public string Xy { get; set; }
	}

	public class Animal
	{
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

		//Assert.NotNull(((Dog)listTest2.First()).Bla);
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
			Name = "FirstDog"
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
			db.SetCommand("CREATE TABLE `Animals` ( `Id` INTEGER NOT NULL PRIMARY KEY, `Name` TEXT,`Discriminator` TEXT, `EyeId` INTEGER )").Execute();
			db.SetCommand("CREATE TABLE `Eyes` ( `Id` INTEGER NOT NULL PRIMARY KEY, `Xy` TEXT )").Execute();
			db.SetCommand("CREATE TABLE `Test` ( `Id` INTEGER NOT NULL PRIMARY KEY, `TestAnimalId` INTEGER )").Execute();
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
			return db.GetTable<Animal>().LoadWithDerived<Animal, Dog>(x => x.Bla).ToList();
		}
	}

	private List<Test> LoadTest3()
	{
		using (var db = new TestDataConnection())
		{
			return db.GetTable<Test>().LoadWith(x => ((Dog)x.TestAnimal).Bla).ToList();
		}
	}

	private void SetMappings()
	{
		var mappingBuilder = MappingSchema.Default.GetFluentMappingBuilder();
		mappingBuilder.Entity<Animal>()
			.HasTableName("Animals")
			.Inheritance(x => x.Discriminator, "Dog", typeof(Dog))
			.Inheritance(x => x.Discriminator, "WildAnimal", typeof(WildAnimal))
			.Inheritance(x => x.Discriminator, "SuperWildAnimal", typeof(SuperWildAnimal))
			.Property(x => x.Name).IsColumn().IsNullable().HasColumnName("Name")
			.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator")
			.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id");

		mappingBuilder.Entity<Dog>()
			.HasTableName("Animals")
			.Property(x => x.Bla).IsNotColumn()
			.Association(x => x.Bla, x => x.EyeId, x => x.Id);

		mappingBuilder.Entity<WildAnimal>()
			.HasTableName("Animals");

		mappingBuilder.Entity<SuperWildAnimal>()
			.HasTableName("Animals");

		mappingBuilder.Entity<Eye>()
			.HasTableName("Eyes")
			.Property(x => x.Id).IsColumn().HasColumnName("Xy")
			.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy");

		mappingBuilder.Entity<Test>()
			.HasTableName("Test")
			.Association(x => x.TestAnimal, x => x.TestAnimalId, x => x.Id)
			.Property(x => x.TestAnimalId).IsColumn().IsNullable().HasColumnName("TestAnimalId")
			.Property(x => x.TestAnimal).IsNotColumn();
	}
}
