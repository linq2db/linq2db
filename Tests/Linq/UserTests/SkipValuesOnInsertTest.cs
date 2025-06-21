using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class SkipValuesOnInsertTest: TestBase
	{
		[Table("PR_1598_Insert_Table")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John", "Max")]
			public string? Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(2, 5)]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Mixed_Table")]
		public class TestTableMixed
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John"), SkipValuesOnUpdate("Max")]
			public string? Name { get; set; }
			[Column("Age")]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Insert_Null_Table")]
		public class TestTableNull
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(null)]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Insert_Fluent_Table")]
		public class TestTableFluent
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age")]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Insert_Enum_Table")]
		public class TestTableEnum
		{
			public enum GenderType
			{
				[MapValue(null)]
				Undefined,
				[MapValue("Male")]
				Male,
				[MapValue("Female")]
				Female
			}

			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age")]
			public int? Age { get; set; }
			[Column("Gender"), SkipValuesOnInsert(GenderType.Female)]
			public GenderType Gender { get; set; }
		}

		[Test]
		public void TestSkipString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "John", Age = 14});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.Null);

					count = db.Insert(new TestTable() {Id = 2, Name = "Max", Age = 15});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.Null);
				}
			}
		}

		[Test]
		public void TestNotSkipString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Paul", Age = 14});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Paul"));

					count = db.Insert(new TestTable() {Id = 2, Name = "Mary", Age = 15});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Mary"));
				}
			}
		}

		[Test]
		public void TestSkipInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Smith", Age = 2});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Null);

					count = db.Insert(new TestTable() {Id = 2, Name = "Tommy", Age = 5});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Null);
				}
			}
		}

		[Test]
		public void TestNotSkipInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Smith", Age = 55});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.EqualTo(55));

					count = db.Insert(new TestTable() {Id = 2, Name = "Tommy", Age = 50});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.EqualTo(50));
				}
			}
		}

		[Test]
		public void TestSkipNull([DataSources] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				// Change default value, so that null is not inserted as default.
				db.MappingSchema.SetDefaultValue(typeof(int?), 0);
				using (db.CreateLocalTable<TestTableNull>())
				{
					
					var count = db.Insert(new TestTableNull() { Id = 1, Name = "Tommy", Age = null });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableNull>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Not.Null);
				}
			}
		}

		[Test]
		public void TestSkipWithFluentBuilder([DataSources] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				// Change default value, so that null is not inserted as default.
				db.MappingSchema.SetDefaultValue(typeof(int?), 0);

				var mapping = new FluentMappingBuilder(db.MappingSchema);
				mapping.Entity<TestTableFluent>().HasSkipValuesOnInsert(t => t.Age, 2, 5).Build();
				using (db.CreateLocalTable<TestTableFluent>())
				{
					var count = db.Insert(new TestTableFluent() { Id = 1, Name = null, Age = 2 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableFluent>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Zero);
				}
			}
		}

		[Test]
		public void TestSkipWithEnum([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTableEnum>())
				{
					var count = db.Insert(new TestTableEnum() { Id = 1, Name = "Max", Age = 20, Gender = TestTableEnum.GenderType.Male});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Gender, Is.EqualTo(TestTableEnum.GenderType.Male));

					count = db.Insert(new TestTableEnum() { Id = 2, Name = "Jenny", Age = 25, Gender = TestTableEnum.GenderType.Female });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Gender, Is.EqualTo(TestTableEnum.GenderType.Undefined));
				}
			}
		}

		[Test]
		public void TestSkipMixed([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTableMixed>())
				{
					var count = db.Insert(new TestTableMixed() { Id = 1, Name = "Jason", Age = 20 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Jason"));

					r.Name = "Max";
					count = db.Update(r);

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Jason"));

					count = db.Insert(new TestTableMixed() { Id = 2, Name = "John", Age = 25 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.Null);

					r.Name = "Jessy";
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 2)!;
					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Jessy"));
				}
			}
		}
	}
}
