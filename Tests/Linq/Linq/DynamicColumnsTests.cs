using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DynamicColumnsTests : TestBase
	{
		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<int>(x, "ID") == 1)
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("John", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNavigationalNonDynamicColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(x.Patient, "Diagnosis") ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociation(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis") ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociationViaObject1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>()
					.Where(x => (string)Sql.Property<object>(Sql.Property<object>(x, "Patient"), "Diagnosis") ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual("Tester", result.Single().FirstName);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithNonDynamicAssociationViaObject2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
				var result = db.GetTable<Person>()
					.Select(x => Sql.Property<object>(Sql.Property<object>(x, "Patient"), "Diagnosis"))
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWithDynamicColumn(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var result = db.GetTable<MyClass>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(1, result.Single().ID);
			}
		}
		
		[Test, DataContextSource]
		public void SqlPropertyWithDynamicAssociation(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var result = db.GetTable<MyClass>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis") ==
								"Hallucination with Paranoid Bugs\' Delirium of Persecution")
					.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(2, result.Single().ID);
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectAll(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Select(p => p.FirstName).ToList();
				var result = db.GetTable<MyClass>().ToList().Select(p => p.ExtendedProperties["FirstName"]).ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectOne(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Select(p => p.FirstName).ToList();
				var result = db.GetTable<MyClass>()
					.Select(x => Sql.Property<string>(x, "FirstName"))
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectProject(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Select(p => new
				{
					PersonId = p.ID,
					Name = p.FirstName
				}).ToList();

				var result = db.GetTable<MyClass>()
					.Select(x => new
					{
						PersonId = x.ID,
						Name = Sql.Property<string>(x, "FirstName")
					})
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertySelectAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();

				var result = db.GetTable<MyClass>()
					.Select(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis"))
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWhere(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Where(p => p.FirstName == "John").Select(p => p.ID).ToList();
				var result = db.GetTable<MyClass>()
					.Where(x => Sql.Property<string>(x, "FirstName") == "John")
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyWhereAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Where(p => p.Patient?.Diagnosis != null).Select(p => p.ID).ToList();
				var result = db.GetTable<MyClass>()
					.Where(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis") != null)
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyOrderBy(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.OrderByDescending(p => p.FirstName).Select(p => p.ID).ToList();
				var result = db.GetTable<MyClass>()
					.OrderByDescending(x => Sql.Property<string>(x, "FirstName"))
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyOrderByAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.OrderBy(p => p.Patient?.Diagnosis).Select(p => p.ID).ToList();
				var result = db.GetTable<MyClass>()
					.OrderBy(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis"))
					.Select(x => x.ID)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyGroupBy(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.GroupBy(p => p.FirstName).Select(p => new {p.Key, Count = p.Count()}).ToList();
				var result = db.GetTable<MyClass>()
					.GroupBy(x => Sql.Property<string>(x, "FirstName"))
					.Select(p => new {p.Key, Count = p.Count()})
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyGroupByAssociated(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.GroupBy(p => p.Patient?.Diagnosis).Select(p => new {p.Key, Count = p.Count()}).ToList();
				var result = db.GetTable<MyClass>()
					.GroupBy(x => Sql.Property<string>(Sql.Property<Patient>(x, "Patient"), "Diagnosis"))
					.Select(p => new {p.Key, Count = p.Count()})
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyJoin(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = 
					from p in Person
					join pa in Patient on p.FirstName equals pa.Diagnosis
					select p;

				var result =
					from p in db.Person
					join pa in db.Patient on Sql.Property<string>(p, "FirstName") equals Sql.Property<string>(pa, "Diagnosis")
					select p;

				Assert.IsTrue(result.ToList().SequenceEqual(expected.ToList()));
			}
		}

		[Test, DataContextSource]
		public void SqlPropertyLoadWith(string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicMyClass()))
			{
				var expected = Person.Select(p => p.Patient?.Diagnosis).ToList();
				var result = db.GetTable<MyClass>()
					.LoadWith(x => Sql.Property<Patient>(x, "Patient"))
					.ToList()
					.Select(p => ((Patient)p.ExtendedProperties["Patient"])?.Diagnosis)
					.ToList();

				Assert.IsTrue(result.SequenceEqual(expected));
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
