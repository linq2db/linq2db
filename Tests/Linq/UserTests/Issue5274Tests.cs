using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5274Tests : TestBase
	{
		// Base entity with discriminator
		[Table("Issue5274Entity")]
		[InheritanceMapping(Code = "Base", Type = typeof(Issue5274BaseEntity), IsDefault = true)]
		[InheritanceMapping(Code = "Type01", Type = typeof(Issue5274Entity01))]
		[InheritanceMapping(Code = "Type02", Type = typeof(Issue5274Entity02))]
		[InheritanceMapping(Code = "Type03", Type = typeof(Issue5274Entity03))]
		[InheritanceMapping(Code = "Type04", Type = typeof(Issue5274Entity04))]
		[InheritanceMapping(Code = "Type05", Type = typeof(Issue5274Entity05))]
		[InheritanceMapping(Code = "Type06", Type = typeof(Issue5274Entity06))]
		[InheritanceMapping(Code = "Type07", Type = typeof(Issue5274Entity07))]
		[InheritanceMapping(Code = "Type08", Type = typeof(Issue5274Entity08))]
		[InheritanceMapping(Code = "Type09", Type = typeof(Issue5274Entity09))]
		[InheritanceMapping(Code = "Type10", Type = typeof(Issue5274Entity10))]
		[InheritanceMapping(Code = "Type11", Type = typeof(Issue5274Entity11))]
		[InheritanceMapping(Code = "Type12", Type = typeof(Issue5274Entity12))]
		[InheritanceMapping(Code = "Type13", Type = typeof(Issue5274Entity13))]
		[InheritanceMapping(Code = "Type14", Type = typeof(Issue5274Entity14))]
		[InheritanceMapping(Code = "Type15", Type = typeof(Issue5274Entity15))]
		[InheritanceMapping(Code = "Type16", Type = typeof(Issue5274Entity16))]
		[InheritanceMapping(Code = "Type17", Type = typeof(Issue5274Entity17))]
		public class Issue5274BaseEntity
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string EntityType { get; set; } = null!;

			[Column]
			public string Name { get; set; } = null!;

			[Column]
			public DateTime CreatedDate { get; set; }
		}

		// 17 descendant types
		public class Issue5274Entity01 : Issue5274BaseEntity
		{
			[Column] public string? Property01 { get; set; }
		}

		public class Issue5274Entity02 : Issue5274BaseEntity
		{
			[Column] public string? Property02 { get; set; }
		}

		public class Issue5274Entity03 : Issue5274BaseEntity
		{
			[Column] public string? Property03 { get; set; }
		}

		public class Issue5274Entity04 : Issue5274BaseEntity
		{
			[Column] public string? Property04 { get; set; }
		}

		public class Issue5274Entity05 : Issue5274BaseEntity
		{
			[Column] public string? Property05 { get; set; }
		}

		public class Issue5274Entity06 : Issue5274BaseEntity
		{
			[Column] public string? Property06 { get; set; }
		}

		public class Issue5274Entity07 : Issue5274BaseEntity
		{
			[Column] public string? Property07 { get; set; }
		}

		public class Issue5274Entity08 : Issue5274BaseEntity
		{
			[Column] public string? Property08 { get; set; }
		}

		public class Issue5274Entity09 : Issue5274BaseEntity
		{
			[Column] public string? Property09 { get; set; }
		}

		public class Issue5274Entity10 : Issue5274BaseEntity
		{
			[Column] public string? Property10 { get; set; }
		}

		public class Issue5274Entity11 : Issue5274BaseEntity
		{
			[Column] public string? Property11 { get; set; }
		}

		public class Issue5274Entity12 : Issue5274BaseEntity
		{
			[Column] public string? Property12 { get; set; }
		}

		public class Issue5274Entity13 : Issue5274BaseEntity
		{
			[Column] public string? Property13 { get; set; }
		}

		public class Issue5274Entity14 : Issue5274BaseEntity
		{
			[Column] public string? Property14 { get; set; }
		}

		public class Issue5274Entity15 : Issue5274BaseEntity
		{
			[Column] public string? Property15 { get; set; }
		}

		public class Issue5274Entity16 : Issue5274BaseEntity
		{
			[Column] public string? Property16 { get; set; }
		}

		public class Issue5274Entity17 : Issue5274BaseEntity
		{
			[Column] public string? Property17 { get; set; }
		}

		[Test]
		public void TestLargeInheritanceHierarchy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue5274BaseEntity>();
			// Insert test data for each type
			var baseDate = TestData.Date;

			db.Insert(new Issue5274Entity01 { EntityType = "Type01", Name = "Entity 01", CreatedDate = baseDate.AddDays(1), Property01  = "Value01" });
			db.Insert(new Issue5274Entity02 { EntityType = "Type02", Name = "Entity 02", CreatedDate = baseDate.AddDays(2), Property02  = "Value02" });
			db.Insert(new Issue5274Entity03 { EntityType = "Type03", Name = "Entity 03", CreatedDate = baseDate.AddDays(3), Property03  = "Value03" });
			db.Insert(new Issue5274Entity04 { EntityType = "Type04", Name = "Entity 04", CreatedDate = baseDate.AddDays(4), Property04  = "Value04" });
			db.Insert(new Issue5274Entity05 { EntityType = "Type05", Name = "Entity 05", CreatedDate = baseDate.AddDays(5), Property05  = "Value05" });
			db.Insert(new Issue5274Entity06 { EntityType = "Type06", Name = "Entity 06", CreatedDate = baseDate.AddDays(6), Property06  = "Value06" });
			db.Insert(new Issue5274Entity07 { EntityType = "Type07", Name = "Entity 07", CreatedDate = baseDate.AddDays(7), Property07  = "Value07" });
			db.Insert(new Issue5274Entity08 { EntityType = "Type08", Name = "Entity 08", CreatedDate = baseDate.AddDays(8), Property08  = "Value08" });
			db.Insert(new Issue5274Entity09 { EntityType = "Type09", Name = "Entity 09", CreatedDate = baseDate.AddDays(9), Property09  = "Value09" });
			db.Insert(new Issue5274Entity10 { EntityType = "Type10", Name = "Entity 10", CreatedDate = baseDate.AddDays(10), Property10 = "Value10" });
			db.Insert(new Issue5274Entity11 { EntityType = "Type11", Name = "Entity 11", CreatedDate = baseDate.AddDays(11), Property11 = "Value11" });
			db.Insert(new Issue5274Entity12 { EntityType = "Type12", Name = "Entity 12", CreatedDate = baseDate.AddDays(12), Property12 = "Value12" });
			db.Insert(new Issue5274Entity13 { EntityType = "Type13", Name = "Entity 13", CreatedDate = baseDate.AddDays(13), Property13 = "Value13" });
			db.Insert(new Issue5274Entity14 { EntityType = "Type14", Name = "Entity 14", CreatedDate = baseDate.AddDays(14), Property14 = "Value14" });
			db.Insert(new Issue5274Entity15 { EntityType = "Type15", Name = "Entity 15", CreatedDate = baseDate.AddDays(15), Property15 = "Value15" });
			db.Insert(new Issue5274Entity16 { EntityType = "Type16", Name = "Entity 16", CreatedDate = baseDate.AddDays(16), Property16 = "Value16" });
			db.Insert(new Issue5274Entity17 { EntityType = "Type17", Name = "Entity 17", CreatedDate = baseDate.AddDays(17), Property17 = "Value17" });

			// Test querying all entities - this should not cause stack overflow
			var allEntities = table.ToList();
			Assert.That(allEntities, Has.Count.EqualTo(17));

			// Test querying specific types
			var entity01 = table.OfType<Issue5274Entity01>().FirstOrDefault();
			Assert.That(entity01,             Is.Not.Null);
			Assert.That(entity01!.Property01, Is.EqualTo("Value01"));

			// Test filtering by discriminator
			var type05Entities = table.Where(e => e.EntityType == "Type05").ToList();
			Assert.That(type05Entities, Has.Count.EqualTo(1));

			// Test complex query with multiple type checks
			var multiTypeQuery = table
				.Where(e => e.EntityType == "Type01" || e.EntityType == "Type10" || e.EntityType == "Type17")
				.OrderBy(e => e.Name)
				.ToList();
			Assert.That(multiTypeQuery, Has.Count.EqualTo(3));

			// Test projection with inheritance
			var projectedData = table
				.Select(e => new { e.Id, e.Name, e.EntityType })
				.OrderBy(e => e.EntityType)
				.ToList();
			Assert.That(projectedData, Has.Count.EqualTo(17));
		}

		[Test]
		public void TestInheritanceWithComplexConditions([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var table    = db.CreateLocalTable<Issue5274BaseEntity>();
			var       baseDate = TestData.Date;

			// Insert subset of data
			db.Insert(new Issue5274Entity01 { EntityType = "Type01", Name = "A-Entity", CreatedDate = baseDate, Property01             = "Test1" });
			db.Insert(new Issue5274Entity05 { EntityType = "Type05", Name = "B-Entity", CreatedDate = baseDate.AddDays(5), Property05  = "Test5" });
			db.Insert(new Issue5274Entity10 { EntityType = "Type10", Name = "C-Entity", CreatedDate = baseDate.AddDays(10), Property10 = "Test10" });
			db.Insert(new Issue5274Entity17 { EntityType = "Type17", Name = "D-Entity", CreatedDate = baseDate.AddDays(17), Property17 = "Test17" });

			// Test complex condition that triggers OptimizeExpression
			var result = table
				.Where(e => 
					(e.EntityType == "Type01" && e.Name.StartsWith("A"))    ||
					(e.EntityType == "Type05" && e.CreatedDate > baseDate)  ||
					(e.EntityType == "Type10" && e.Name.Contains("Entity")) ||
					(e.EntityType == "Type17" && e.Id > 0))
				.ToList();

			Assert.That(result, Has.Count.EqualTo(4));
		}
	}
}
