using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Update
{
	[TestFixture]
	public class DynamicColumnsTests : TestBase
	{
		// Introduced to ensure that we process not only constants in column names
		private static string ChildIDColumn  = "ChildID";
		private static string ParentIDColumn = "ParentID";

		[Test, DataContextSource]
		public void InsertViaSqlProperty(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => Sql.Property<int>(c, ChildIDColumn) > 1000);

					Assert.AreEqual(1,
						db
							.Into(db.Child)
							.Value(c => Sql.Property<int>(c, ParentIDColumn), () => 1)
							.Value(c => Sql.Property<int>(c, ChildIDColumn), () => id)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id));
				}
				finally
				{
					db.Child.Delete(c => Sql.Property<int>(c, ChildIDColumn) > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void UpdateViaSqlProperty(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => Sql.Property<int>(c, ChildIDColumn) > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

					Assert.AreEqual(1, db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id));
					Assert.AreEqual(1,
						db.Child
							.Where(c => Sql.Property<int>(c, ChildIDColumn) == id && Sql.Property<int?>(Sql.Property<Parent>(c, "Parent"), "Value1") == 1)
							.Set(c => Sql.Property<int>(c, ChildIDColumn), c => Sql.Property<int>(c, ChildIDColumn) + 1)
							.Update());
					Assert.AreEqual(1, db.Child.Count(c => Sql.Property<int>(c, ChildIDColumn) == id + 1));
				}
				finally
				{
					db.Child.Delete(c => Sql.Property<int>(c, ChildIDColumn) > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertDynamicColumns(string context)
		{
			var firstNameColumn = "FirstName";
			var lastNameColumn  = "LastName";
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				try
				{
					Assert.AreEqual(1,
						db
							.GetTable<MyClass>()
							.Value(c => Sql.Property<string>(c, firstNameColumn), () => "John")
							.Value(c => Sql.Property<string>(c, lastNameColumn), () => "The Dynamic")
							.Value(c => Sql.Property<Gender>(c, "Gender"), () => Gender.Male)
							.Insert());
					Assert.AreEqual(1,
						db.GetTable<MyClass>().Count(c =>
							Sql.Property<string>(c, firstNameColumn) == "John" &&
							Sql.Property<string>(c, lastNameColumn) == "The Dynamic"));
				}
				finally
				{
					db.GetTable<MyClass>().Delete(c => Sql.Property<string>(c, lastNameColumn) == "The Dynamic");
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void UpdateDynamicColumn(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				try
				{
					db.GetTable<MyClass>()
						.Value(c => Sql.Property<string>(c, "FirstName"), () => "John")
						.Value(c => Sql.Property<string>(c, "LastName"), () => "Limonadovy")
						.Value(c => Sql.Property<Gender>(c, "Gender"), () => Gender.Male)
						.Insert();

					Assert.AreEqual(1, db.GetTable<MyClass>().Count(c => Sql.Property<string>(c, "LastName") == "Limonadovy"));
					Assert.AreEqual(1,
						db.GetTable<MyClass>()
							.Where(c => Sql.Property<string>(c, "LastName") == "Limonadovy")
							.Set(c => Sql.Property<string>(c, "FirstName"), () => "Johnny")
							.Update());
					Assert.AreEqual(1, db.GetTable<MyClass>().Count(c => Sql.Property<string>(c, "FirstName") == "Johnny" && Sql.Property<string>(c, "LastName") == "Limonadovy"));
				}
				finally
				{
					db.GetTable<MyClass>().Delete(c => Sql.Property<string>(c, "LastName") == "Limonadovy");
				}
			}
		}

		private MappingSchema ConfigureDynamicMyClass()
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<MyClass>().HasTableName("Person")
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID"))
				.Property(x => Sql.Property<string>(x, "FirstName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "LastName")).IsNullable(false)
				.Property(x => Sql.Property<string>(x, "MiddleName"))
				.Property(x => Sql.Property<Gender>(x, "Gender")).IsNullable(false)
				.Association(x => Sql.Property<Patient>(x, "Patient"), x => Sql.Property<int>(x, "ID"), x => x.PersonID);

			return ms;
		}

		public class MyClass
		{
			[Column("PersonID"), Identity]
			public int ID { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; }
		}
	}
}
