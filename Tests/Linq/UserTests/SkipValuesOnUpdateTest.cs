using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class SkipValuesOnUpdateTest : TestBase
	{
		[Table("PR_1598_Update_Table")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name"), SkipValuesOnUpdate("John")]
			public string? Name { get; set; }
			[Column("Age"), SkipValuesOnUpdate(2, 5)]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Update_Null_Table")]
		public class TestTableNull
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age"), SkipValuesOnUpdate(null)]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Update_Fluent_Table")]
		public class TestTableFluent
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age")]
			public int? Age { get; set; }
		}

		[Table("PR_1598_Update_Enum_Table")]
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
			[Column("Gender"), SkipValuesOnUpdate(GenderType.Female)]
			public GenderType Gender { get; set; }
		}

		[Test]
		public void TestSkipString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.Insert(new TestTable() { Id = 1, Name = "Manuel", Age = 14 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Name, Is.EqualTo("Manuel"));

					r.Name = "Jacob";
					r.Age = 15;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(15));
						Assert.That(r.Name, Is.EqualTo("Jacob"));
					});

					r.Name = "John";
					r.Age = 22;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(22));
						Assert.That(r.Name, Is.EqualTo("Jacob"));
					});
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

					var count = db.Insert(new TestTable() { Id = 1, Name = "Smith", Age = 2 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.EqualTo(2));

					r.Name = "Franki";
					r.Age = 15;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(15));
						Assert.That(r.Name, Is.EqualTo("Franki"));
					});

					r.Name = "Jack";
					r.Age = 2;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(15));
						Assert.That(r.Name, Is.EqualTo("Jack"));
					});
				}
			}
		}

		[Test]
		public void TestSkipNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTableNull>())
				{
					var count = db.Insert(new TestTableNull() { Id = 1, Name = "Tommy", Age = null });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					var r = db.GetTable<TestTableNull>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Null);

					r.Name = "Jack";
					r.Age = 2;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableNull>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(2));
						Assert.That(r.Name, Is.EqualTo("Jack"));
					});

					r.Name = "Franki";
					r.Age = null;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableNull>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(2));
						Assert.That(r.Name, Is.EqualTo("Franki"));
					});
				}
			}
		}
		[Test]
		public void TestSkipWithFluentBuilder([DataSources] string context)
		{
			var ms = new MappingSchema();
			var mapping = new FluentMappingBuilder(ms);
			mapping.Entity<TestTableFluent>().HasSkipValuesOnUpdate(t => t.Age, 2, 5).Build();

			using (var db = GetDataContext(context, ms))
			{
				using (db.CreateLocalTable<TestTableFluent>())
				{
					var count = db.Insert(new TestTableFluent() { Id = 1, Name = null, Age = 2 });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableFluent>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.EqualTo(2));

					r.Name = "Franki";
					r.Age = 18;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableFluent>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(18));
						Assert.That(r.Name, Is.EqualTo("Franki"));
					});

					r.Name = "Jack";
					r.Age = 2;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableFluent>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(18));
						Assert.That(r.Name, Is.EqualTo("Jack"));
					});
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
					var count = db.Insert(new TestTableEnum() { Id = 1, Name = "Max", Age = 20, Gender = TestTableEnum.GenderType.Female });

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Gender, Is.EqualTo(TestTableEnum.GenderType.Female));

					r.Name = "Jack";
					r.Age = 2;
					r.Gender = TestTableEnum.GenderType.Male;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(2));
						Assert.That(r.Name, Is.EqualTo("Jack"));
						Assert.That(r.Gender, Is.EqualTo(TestTableEnum.GenderType.Male));
					});

					r.Name = "Francine";
					r.Age = 20;
					r.Gender = TestTableEnum.GenderType.Female;
					count = db.Update(r);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(r.Age, Is.EqualTo(20));
						Assert.That(r.Name, Is.EqualTo("Francine"));
						Assert.That(r.Gender, Is.EqualTo(TestTableEnum.GenderType.Male));
					});
				}
			}
		}
	}
}
