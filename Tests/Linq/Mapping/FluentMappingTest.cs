using System;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class FluentMappingTest : TestBase
	{
		[Table]
		class MyClass
		{
			public int ID;
			public int ID1 { get; set; }
		}

		[Test]
		public void AddAtribute1()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.HasAttribute<MyClass>(new TableAttribute("NewName"));

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.TableName, Is.EqualTo("NewName"));
		}

		[Test]
		public void AddAtribute2()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.HasAttribute<MyClass>(new TableAttribute("NewName") { Configuration = "Test"});

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.TableName, Is.EqualTo("MyClass"));
		}

		[Test]
		public void HasPrimaryKey1()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().HasPrimaryKey(e => e.ID);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"].IsPrimaryKey);
		}

		[Test]
		public void HasPrimaryKey2()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().HasPrimaryKey(e => e.ID1, 3);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID1"].IsPrimaryKey);
			Assert.That(ed["ID1"].PrimaryKeyOrder, Is.EqualTo(3));
		}

		[Test]
		public void HasPrimaryKey3()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().HasPrimaryKey(e => new { e.ID, e.ID1 }, 3);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]. IsPrimaryKey);
			Assert.That(ed["ID"]. PrimaryKeyOrder, Is.EqualTo(3));
			Assert.That(ed["ID1"].IsPrimaryKey);
			Assert.That(ed["ID1"].PrimaryKeyOrder, Is.EqualTo(4));
		}

		[Test]
		public void HasPrimaryKey4()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().Property(e => e.ID).IsPrimaryKey();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"].IsPrimaryKey);
		}

		[Test]
		public void IsPrimaryKeyIsIdentity()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.Property(e => e.ID)
					.IsPrimaryKey()
					.IsIdentity();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"].IsPrimaryKey);
			Assert.That(ed["ID"].IsIdentity);
		}
	}
}
