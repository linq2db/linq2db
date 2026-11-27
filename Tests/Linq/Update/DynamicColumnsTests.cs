using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	public class DynamicColumnsTests : TestBase
	{
		// Introduced to ensure that we process not only constants in column names
		private static string ChildIDColumn  = "ChildID";
		private static string ParentIDColumn = "ParentID";

		[Test]
		public void InsertViaSqlProperty([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				var cnt = db
					.Into(db.Child)
					.Value(c => Sql.Property<int>(c, ParentIDColumn), () => 1)
					.Value(c => Sql.Property<int>(c, ChildIDColumn), () => id)
					.Insert();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id), Is.EqualTo(1));
			}
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void UpdateViaSqlProperty([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				Assert.That(db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id), Is.EqualTo(1));
				var cnt = db.Child
						.Where(c => Sql.Property<int>(c, ChildIDColumn) == id && Sql.Property<int?>(Sql.Property<Parent>(c, "Parent"), "Value1") == 1)
						.Set(c => Sql.Property<int>(c, ChildIDColumn), c => Sql.Property<int>(c, ChildIDColumn) + 1)
						.Update();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void UpdateViaSqlPropertyValue([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				Assert.That(db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id), Is.EqualTo(1));
				var cnt = db.Child
						.Where(c => Sql.Property<int>(c, ChildIDColumn) == id && Sql.Property<int?>(Sql.Property<Parent>(c, "Parent"), "Value1") == 1)
						.Set(c => Sql.Property<int>(c, ChildIDColumn), 5000)
						.Update();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == 5000), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void InsertDynamicColumns([DataSources] string context)
		{
			var firstNameColumn = "FirstName";
			var lastNameColumn  = "LastName";

			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			using (new RestoreBaseTables(db))
			{
				var cnt = db
					.GetTable<MyClass>()
					.Value(c => Sql.Property<string>(c, firstNameColumn), () => "John")
					.Value(c => Sql.Property<string>(c, lastNameColumn), () => "The Dynamic")
					.Value(c => Sql.Property<Gender>(c, "Gender"), () => Gender.Male)
					.Insert();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.GetTable<MyClass>().Count(c =>
						Sql.Property<string>(c, firstNameColumn) == "John" &&
						Sql.Property<string>(c, lastNameColumn) == "The Dynamic"), Is.EqualTo(1));
			}
		}

		[Test]
		public void UpdateDynamicColumn([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			using (new RestoreBaseTables(db))
			{
				db.GetTable<MyClass>()
					.Value(c => Sql.Property<string>(c, "FirstName"), () => "John")
					.Value(c => Sql.Property<string>(c, "LastName"), () => "Limonadovy")
					.Value(c => Sql.Property<Gender>(c, "Gender"), () => Gender.Male)
					.Insert();

				Assert.That(db.GetTable<MyClass>().Count(c => Sql.Property<string>(c, "LastName") == "Limonadovy"), Is.EqualTo(1));

				var cnt = db.GetTable<MyClass>()
						.Where(c => Sql.Property<string>(c, "LastName") == "Limonadovy")
						.Set(c => Sql.Property<string>(c, "FirstName"), () => "Johnny")
						.Update();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.GetTable<MyClass>().Count(c => Sql.Property<string>(c, "FirstName") == "Johnny" && Sql.Property<string>(c, "LastName") == "Limonadovy"), Is.EqualTo(1));
			}
		}

		private MappingSchema ConfigureDynamicMyClass()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<MyClass>().HasTableName("Person")
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID"))
				.Property(x => Sql.Property<string>(x, "FirstName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "LastName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "MiddleName"))
				.Property(x => Sql.Property<Gender>(x, "Gender")).IsNullable(false)
				.Association(x => Sql.Property<Patient>(x, "Patient"), x => Sql.Property<int>(x, "ID"), x => x.PersonID)
				.Build();

			return ms;
		}

		public class MyClass
		{
			[Column("PersonID"), Identity]
			public int ID { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1268")]
		public void InsertWithDynamicColumnFromFluentProperty([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);
			builder.Entity<DynamicColumnStoreRecord>()
				.Property(x => Sql.Property<bool>(x, "IsDeleted"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnFullRecord>())
			{
				var obj1 = new DynamicColumnStoreRecord { Id = 1, Name = "Some1" };
				obj1.Values.Add("IsDeleted", true);
				db.Insert(obj1);

				var obj2 = new DynamicColumnStoreRecord { Id = 2, Name = "Some2" };
				db.Insert(obj2);

				var loaded1 = db.GetTable<DynamicColumnStoreRecord>().First(e => e.Name == "Some1");
				Assert.That(loaded1.Values["IsDeleted"], Is.True);

				var loaded2 = db.GetTable<DynamicColumnStoreRecord>().First(e => e.Name == "Some2");
				Assert.That(loaded2.Values["IsDeleted"], Is.False);
			}
		}

		[Table("DynamicColumnTable")]
		sealed class DynamicColumnFullRecord
		{
			[Column]
			         public int     Id        { get; set; }
			[Column] public string? Name      { get; set; }
			[Column] public bool    IsDeleted { get; set; }
		}

		[Table("DynamicColumnTable")]
		sealed class DynamicColumnStoreRecord
		{
			[Column]
			         public int     Id        { get; set; }
			[Column] public string? Name      { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
		}
	}
}
