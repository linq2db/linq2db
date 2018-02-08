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
		public string Name { get; set; }

		public string Discriminator { get; set; }
	}

	public class Dog : Animal
	{
		public Eye Bla { get; set; }

		public int? EyeId { get; set; }
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
			Discriminator = "Dog",
			EyeId = 1,
			Name = "FirstDog"
		};

		using (var db = new TestDataConnection())
		{
			db.SetCommand("DROP TABLE IF EXISTS `Animals`").Execute();
			db.SetCommand("DROP TABLE IF EXISTS `Dogs`").Execute();
			db.SetCommand("DROP TABLE IF EXISTS `Eyes`").Execute();
			db.SetCommand("CREATE TABLE `Animals` ( `Name` TEXT,`Discriminator` TEXT, `EyeId` INTEGER )").Execute();
			db.SetCommand("CREATE TABLE `Eyes` ( `Id` INTEGER NOT NULL PRIMARY KEY, `Xy` TEXT )").Execute();
		}

		using (var db = new TestDataConnection())
		{
			db.GetTable<Eye>().Delete();
			db.GetTable<Animal>().Delete();

			db.Insert(eye);
			db.Insert(dog);
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

	private void SetMappings()
	{
		var mappingBuilder = MappingSchema.Default.GetFluentMappingBuilder();
		mappingBuilder.Entity<Animal>()
			.HasTableName("Animals")
			.Inheritance(x => x.Discriminator, "Dog", typeof(Dog))
			.Property(x => x.Name).IsColumn().IsNullable().HasColumnName("Name")
			.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator");

		mappingBuilder.Entity<Dog>()
			.HasTableName("Animals")
			.Property(x => x.Bla).IsNotColumn()
			.Association(x => x.Bla, x => x.EyeId, x => x.Id);

		mappingBuilder.Entity<Eye>()
			.HasTableName("Eyes")
			.Property(x => x.Id).IsColumn().HasColumnName("Xy")
			.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy");
	}
}
