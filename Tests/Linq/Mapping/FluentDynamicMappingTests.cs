using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
    public class FluentDynamicMappingTests : TestBase
    {
		[Table]
	    public class MyClass
	    {
		    public int ID;

			public int ID1 { get; set; }

			[NotColumn]
		    public MyClass Parent;

			public byte RowType { get; set; }

			public IDictionary<string, object> ExtendedColumns { get; set; }
	    }

		[Table]
		public class MyClass2 : MyClass { }

		[Table]
	    public class MyClass3
	    {
			[DynamicColumnsStore]
		    public IDictionary<string, object> ExtendedColumns { get; set; }
		}

	    [Test]
		public void HasAttribute1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.HasAttribute<MyClass>(x => Sql.Property<int>(x, "ID"), new PrimaryKeyAttribute());

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.IsTrue(ed["ID"].IsPrimaryKey);
	    }

	    [Test]
		public void HasAttribute2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.HasAttribute<MyClass>(x => Sql.Property<int>(x, "ID2"), new PrimaryKeyAttribute());

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.IsTrue(ed["ID2"].IsPrimaryKey);
	    }

	    [Test]
		public void Property1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Property(x => Sql.Property<int>(x, "ID")).IsPrimaryKey();

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID"].IsPrimaryKey);
		}

	    [Test]
		public void Property2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Property(x => Sql.Property<int>(x, "ID2")).IsPrimaryKey();

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID2"].IsPrimaryKey);
	    }

	    [Test]
		public void Association1()
	    {
			var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Association(x => Sql.Property<MyClass>(x, "Parent"), x => Sql.Property<int>(x, "ID1"), x => Sql.Property<int>(x, "ID"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.Associations.Single().MemberInfo.Name, "Parent");
		    Assert.AreEqual(ed.Associations.Single().ThisKey.Single(), "ID1");
		    Assert.AreEqual(ed.Associations.Single().OtherKey.Single(), "ID");
		}

	    [Test]
		public void Association2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Association(x => Sql.Property<MyClass>(x, "Parent2"), x => Sql.Property<int>(x, "ID2"), x => Sql.Property<int>(x, "ID3"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.Associations.Single().MemberInfo.Name, "Parent2");
		    Assert.AreEqual(ed.Associations.Single().ThisKey.Single(), "ID2");
		    Assert.AreEqual(ed.Associations.Single().OtherKey.Single(), "ID3");
	    }

	    [Test]
		public void HasPrimaryKey1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasPrimaryKey(x => Sql.Property<int>(x, "ID"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID"].IsPrimaryKey);
	    }

	    [Test]
		public void HasPrimaryKey2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasPrimaryKey(x => Sql.Property<int>(x, "ID2"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID2"].IsPrimaryKey);
	    }

	    [Test]
		public void HasIdentity1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasIdentity(x => Sql.Property<int>(x, "ID"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID"].IsIdentity);
	    }

	    [Test]
		public void HasIdentity2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasIdentity(x => Sql.Property<int>(x, "ID2"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsTrue(ed["ID2"].IsIdentity);
	    }

	    [Test]
		public void HasColumn1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasColumn(x => Sql.Property<int>(x, "ID"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.NotNull(ed["ID"]);
	    }

	    [Test]
		public void HasColumn2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasColumn(x => Sql.Property<int>(x, "ID2"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.NotNull(ed["ID2"]);
		}

	    [Test]
		public void Ignore()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Ignore(x => Sql.Property<int>(x, "ID"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.IsNull(ed["ID"]);
	    }

	    [Test]
		public void HasDynamicColumnStore1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasDynamicColumnsStore(x => x.ExtendedColumns);

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.DynamicColumnsStore.MemberName, "ExtendedColumns");
	    }

	    [Test]
		public void HasDynamicColumnStore2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .HasDynamicColumnsStore(x => Sql.Property<IDictionary<string, object>>(x, "ExtendedColumns"));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.DynamicColumnsStore.MemberName, "ExtendedColumns");
	    }

	    [Test]
	    public void HasDynamicColumnStore3()
	    {
		    var ms = new MappingSchema();
		    
		    var ed = ms.GetEntityDescriptor(typeof(MyClass3));

		    Assert.AreEqual(ed.DynamicColumnsStore.MemberName, "ExtendedColumns");
	    }

	    [Test]
	    public void HasDynamicColumnStore4()
	    {
		    var ms = new MappingSchema();

		    var ed = ms.GetEntityDescriptor(typeof(MyClass3));

		    Assert.IsFalse(ed.Columns.Any(c => c.MemberName == "ExtendedColumns"));
	    }

		[Test]
		public void Inheritance1()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Inheritance(x => Sql.Property<byte>(x, "RowType"), 1, typeof(MyClass2));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.InheritanceMapping.Single().DiscriminatorName, "RowType");
	    }

	    [Test]
		public void Inheritance2()
	    {
		    var ms = new MappingSchema();
		    var mb = ms.GetFluentMappingBuilder();

		    mb.Entity<MyClass>()
			    .Inheritance(x => Sql.Property<byte>(x, "RowType2"), 1, typeof(MyClass2));

		    var ed = ms.GetEntityDescriptor(typeof(MyClass));

		    Assert.AreEqual(ed.InheritanceMapping.Single().DiscriminatorName, "RowType2");
	    }
	}
}
