using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue996Tests : TestBase
	{
		public class A
		{
			public A()
			{
				Discriminator = "A";
			}

			public int Id { get; set; }
			public string PropA { get; set; }
			public string Discriminator { get; set; }
		}

		public class B : A
		{
			public B()
			{
				Discriminator = "B";
			}

			public string PropB { get; set; }
		}

		public class C : B
		{
			public C()
			{
				Discriminator = "C";
			}

			public string PropC { get; set; }
		}

		public class Test
		{
			public int Id { get; set; }
			public int? TestAId { get; set; }
			public A TestA { get; set; }
		}

		[Test]
		public void TestIssue()
		{
			SetMappings();

			InsertData();

			// we can load all rows from A - everything is fine
			var listA = LoadA();

			// should not throw an exception
			var listTest = LoadTest();
		}

		private void InsertData()
		{
			var b = new B
			{
				Id = 2,
				PropA = "Test B",
				PropB = "Test B",
				Discriminator = "B"
			};

			var c = new C
			{
				Id = 3,
				PropA = "Test C",
				PropB = "Test C",
				PropC = "Test C",
				Discriminator = "C"
			};

			var test = new Test
			{
				Id = 1,
				TestAId = null
			};

			var test2 = new Test
			{
				Id = 2,
				TestAId = 2
			};


			using (var db = new TestDataConnection())
			{
				db.SetCommand("DROP TABLE IF EXISTS `A`").Execute();
				db.SetCommand("DROP TABLE IF EXISTS `Test`").Execute();
				db.SetCommand("CREATE TABLE `A` ( `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, `PropA` TEXT, `PropB` TEXT, `PropC` TEXT,`Discriminator` TEXT )").Execute();
				db.SetCommand("CREATE TABLE `Test` ( `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, `TestAId` INTEGER )").Execute();
			}

			using (var db = new TestDataConnection())
			{
				db.GetTable<A>().Delete();
				db.GetTable<Test>().Delete();

				db.Insert(b);
				db.Insert(c);
				db.Insert(test);
				db.Insert(test2);
			}
		}

		private List<A> LoadA()
		{
			using (var db = new TestDataConnection())
			{
				return db.GetTable<A>().ToList();
			}
		}

		private List<Test> LoadTest()
		{
			using (var db = new TestDataConnection())
			{
				return db.GetTable<Test>().LoadWith(x => x.TestA).ToList();
			}
		}

		private void SetMappings()
		{
			var mappingBuilder = MappingSchema.Default.GetFluentMappingBuilder();
			mappingBuilder.Entity<A>()
				.HasTableName("A")
				.Inheritance(x => x.Discriminator, "A", typeof(A))
				.Inheritance(x => x.Discriminator, "B", typeof(B))
				.Inheritance(x => x.Discriminator, "C", typeof(C))
				.Property(x => x.PropA).IsColumn().IsNullable().HasColumnName("PropA")
				.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator")
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id");

			mappingBuilder.Entity<B>()
				.HasTableName("A")
				.Property(x => x.PropB).IsColumn().IsNullable().HasColumnName("PropB");

			mappingBuilder.Entity<C>()
				.HasTableName("A")
				.Property(x => x.PropC).IsColumn().IsNullable().HasColumnName("PropC");

			mappingBuilder.Entity<Test>()
				.HasTableName("Test")
				.Association(x => x.TestA, x => x.TestAId, x => x.Id)
				.Property(x => x.TestAId).IsColumn().IsNullable().HasColumnName("TestAId")
				.Property(x => x.TestA).IsNotColumn();
		}
	}
}
