using System;
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
			public MyClass? Parent;

			public byte RowType { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object>? ExtendedColumns { get; set; }

			[DynamicColumnsStore(Configuration = ProviderName.SQLite)]
			public IDictionary<string, object>? ExtendedSQLiteColumns { get; set; }
		}

		[Table]
		public class MyClass2 : MyClass { }

		[Test]
		public void HasAttribute1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.HasAttribute<MyClass>(x => Sql.Property<int>(x, "ID"), new PrimaryKeyAttribute()).Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void HasAttribute2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.HasAttribute<MyClass>(x => Sql.Property<int>(x, "ID2"), new PrimaryKeyAttribute()).Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID2"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void Property1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Property(x => Sql.Property<int>(x, "ID")).IsPrimaryKey()
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void Property2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Property(x => Sql.Property<int>(x, "ID2")).IsPrimaryKey()
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID2"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void Association1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Association(x => Sql.Property<MyClass>(x, "Parent"), x => Sql.Property<int>(x, "ID1"), x => Sql.Property<int>(x, "ID"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.Multiple(() =>
			{
				Assert.That(ed.Associations.Single().MemberInfo.Name, Is.EqualTo("Parent"));
				Assert.That(ed.Associations.Single().ThisKey.Single(), Is.EqualTo("ID1"));
				Assert.That(ed.Associations.Single().OtherKey.Single(), Is.EqualTo("ID"));
			});
		}

		[Test]
		public void Association2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Association(x => Sql.Property<MyClass>(x, "Parent2"), x => Sql.Property<int>(x, "ID2"), x => Sql.Property<int>(x, "ID3"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.Multiple(() =>
			{
				Assert.That(ed.Associations.Single().MemberInfo.Name, Is.EqualTo("Parent2"));
				Assert.That(ed.Associations.Single().ThisKey.Single(), Is.EqualTo("ID2"));
				Assert.That(ed.Associations.Single().OtherKey.Single(), Is.EqualTo("ID3"));
			});
		}

		[Test]
		public void HasPrimaryKey1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void HasPrimaryKey2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasPrimaryKey(x => Sql.Property<int>(x, "ID2"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID2"]!.IsPrimaryKey, Is.True);
		}

		[Test]
		public void HasIdentity1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasIdentity(x => Sql.Property<int>(x, "ID"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsIdentity, Is.True);
		}

		[Test]
		public void HasIdentity2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasIdentity(x => Sql.Property<int>(x, "ID2"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID2"]!.IsIdentity, Is.True);
		}

		[Test]
		public void HasColumn1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasColumn(x => Sql.Property<int>(x, "ID"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"], Is.Not.Null);
		}

		[Test]
		public void HasColumn2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasColumn(x => Sql.Property<int>(x, "ID2"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID2"], Is.Not.Null);
		}

		[Test]
		public void Ignore()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Ignore(x => Sql.Property<int>(x, "ID"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"], Is.Null);
		}

		[Test]
		public void HasDynamicColumnStore1()
		{
			var ms = new MappingSchema();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.Multiple(() =>
			{
				Assert.That(ed.DynamicColumnsStore!.MemberName, Is.EqualTo(nameof(MyClass.ExtendedColumns)));
				Assert.That(ed.Columns.Any(c => c.MemberName == nameof(MyClass.ExtendedColumns)), Is.False);
				Assert.That(ed.Columns.Any(c => c.MemberName == nameof(MyClass.ExtendedSQLiteColumns)), Is.False);
			});
		}

		[Test]
		public void HasDynamicColumnStoreWithConfiguration()
		{
			var ms = new MappingSchema(ProviderName.SQLite);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.Multiple(() =>
			{
				Assert.That(ed.DynamicColumnsStore!.MemberName, Is.EqualTo(nameof(MyClass.ExtendedSQLiteColumns)));
				Assert.That(ed.Columns.Any(c => c.MemberName == nameof(MyClass.ExtendedColumns)), Is.False);
				Assert.That(ed.Columns.Any(c => c.MemberName == nameof(MyClass.ExtendedSQLiteColumns)), Is.False);
			});
		}

		[Test]
		public void Inheritance1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Inheritance(x => Sql.Property<byte>(x, "RowType"), 1, typeof(MyClass2))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.InheritanceMapping.Single().DiscriminatorName, Is.EqualTo("RowType"));
		}

		[Test]
		public void Inheritance2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Inheritance(x => Sql.Property<byte>(x, "RowType2"), 1, typeof(MyClass2))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.InheritanceMapping.Single().DiscriminatorName, Is.EqualTo("RowType2"));
		}
	}
}
