using System.Linq;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;
using Tests.Model;

namespace Tests.Mapping
{
	[TestFixture]
	public class FluentMappingTests : TestBase
	{
		[Table]
		class MyClass
		{
			public int ID;
			public int ID1 { get; set; }

			[NotColumn]
			public MyClass Parent;
		}

		[Table(IsColumnAttributeRequired = true)]
		class MyClass2
		{
			public int ID { get; set; }

			public MyClass3 Class3 { get; set; }
		}

		[Table]
		class MyClass3
		{
			public int ID { get; set; }
		}

		class MyBaseClass
		{
			public int           Id;
			public MyClass       Assosiation;
			public List<MyClass> Assosiations;
		}

		/// <summary>
		/// [Table(Name = nameof(IInterfaceBase))]
		/// </summary>
		interface IInterfaceBase
		{
			/// <summary>
			/// [Column(SkipOnUpdate = true)]
			/// </summary>
			int IntValue { get; set; }
		}

		interface IInheritedInterface : IInterfaceBase
		{
			/// <summary>
			/// [Column(SkipOnUpdate = true, SkipOnInsert = true)]
			/// </summary>
			string StringValue { get; set; }
		}

		interface IInterface2
		{
			/// <summary>
			/// [Column(SkipOnInsert = true]
			/// </summary>
			int MarkedOnType { get; set; }
		}

		class MyInheritedClass : MyBaseClass
		{
		}

		class MyInheritedClass2 : MyInheritedClass
		{
		}

		class MyInheritedClass3 : IInheritedInterface
		{
			public string StringValue { get; set; }
			public int    IntValue    { get; set; }
		}

		class MyInheritedClass4 : MyInheritedClass3, IInterface2
		{
			public int MarkedOnType { get; set; }
		}

		[Test]
		public void LowerCaseMappingTest()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			ms.EntityDescriptorCreatedCallback = (mappingSchema, entityDescriptor) =>
			{
				entityDescriptor.TableName = entityDescriptor.TableName.ToLower();
				foreach (var entityDescriptorColumn in entityDescriptor.Columns)
				{
					entityDescriptorColumn.ColumnName = entityDescriptorColumn.ColumnName.ToLower();
				}
			};

			mb.Entity<MyClass>().HasTableName("NewName").Property(x => x.ID1).IsColumn();


			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.TableName, Is.EqualTo("newname"));
			Assert.That(ed.Columns.First().ColumnName, Is.EqualTo("id1"));
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

		[Test]
		public void TableNameAndSchema()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.HasTableName ("Table")
				.HasSchemaName("Schema");

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.TableName,  Is.EqualTo("Table"));
			Assert.That(ed.SchemaName, Is.EqualTo("Schema"));
		}

		[Test]
		public void Assosiation()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.Property(e => e.Parent)
					.HasAttribute(new AssociationAttribute { ThisKey = "ID", OtherKey = "ID1" });

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Associations, Is.Not.EqualTo(0));
		}

		[Test]
		public void PropertyIncluded()
		{
			var ms = new MappingSchema();

			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass2>()
				.Property(e => e.ID).IsPrimaryKey()
				.Property(e => e.Class3);

			var ed = ms.GetEntityDescriptor(typeof(MyClass2));

			Assert.That(ed["Class3"], Is.Not.Null);
		}

		[Test]
		public void FluentAssociation1()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.Association( e => e.Parent, e => e.ID, o => o.ID1 );

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That( ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void FluentAssociation2()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.Association( e => e.Parent, (e, o) => e.ID == o.ID1 );

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That( ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void FluentAssociation3()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyInheritedClass>()
				.Association( e => e.Assosiations, (e, o) => e.Id == o.ID1 );

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass));

			Assert.That( ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		public class TestInheritancePerson
		{
			public int    PersonID { get; set; }
			public Gender Gender   { get; set; }
		}

		public class TestInheritanceMale : TestInheritancePerson
		{
			public string FirstName { get; set; }
		}

		public class TestInheritanceFemale : TestInheritancePerson
		{
			public string FirstName { get; set; }
			public string LastName  { get; set; }
		}

		[Test, DataContextSource]
		public void FluentInheritance(string context)
		{
			var ms = MappingSchema.Default; // new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<TestInheritancePerson>()
				.Inheritance(e => e.Gender, Gender.Male,   typeof(TestInheritanceMale))
				.Inheritance(e => e.Gender, Gender.Female, typeof(TestInheritanceFemale));

			var ed = ms.GetEntityDescriptor(typeof(TestInheritancePerson));

			Assert.That(ed.InheritanceMapping.Count, Is.Not.EqualTo(0));

			using (var db = GetDataContext(context, ms))
			{
				var john = db.GetTable<TestInheritancePerson>().Where(_ => _.PersonID == 1).First();
				Assert.That(john, Is.TypeOf<TestInheritanceMale>());

				var jane = db.GetTable<TestInheritancePerson>().Where(_ => _.PersonID == 3).First();
				Assert.That(jane, Is.TypeOf<TestInheritanceFemale>());

			}
		}

		[Test, DataContextSource]
		public void FluentInheritance2(string context)
		{
			var ms = MappingSchema.Default; // new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<TestInheritancePerson>()
				.Inheritance(e => e.Gender, Gender.Male,   typeof(TestInheritanceMale))
				.Inheritance(e => e.Gender, Gender.Female, typeof(TestInheritanceFemale));

			var ed = ms.GetEntityDescriptor(typeof(TestInheritancePerson));

			Assert.That(ed.InheritanceMapping.Count, Is.Not.EqualTo(0));

			using (var db = GetDataContext(context, ms))
			{
				var john = db.GetTable<TestInheritanceMale>().Where(_ => _.PersonID == 1).FirstOrDefault();
				Assert.IsNotNull(john);

				var jane = db.GetTable<TestInheritanceFemale>().Where(_ => _.PersonID == 3).FirstOrDefault();
				Assert.IsNotNull(jane);

			}
		}

		[Test]
		public void DoubleNameChangeTest()
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b.Entity<MyClass>().HasTableName("Name1");

			var od1 = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.AreEqual("Name1", od1.TableName);

			b.Entity<MyClass>().HasTableName("Name2");

			var od2 = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.AreEqual("Name2", od2.TableName);

		}

		[Test]
		public void AssociationInheritance()
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b.Entity<MyInheritedClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"});

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass));
			Assert.AreEqual(2, ed.Associations.Count);
		}

		[Test]
		public void AttributeInheritance()
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b.Entity<MyBaseClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"});

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass));
			Assert.AreEqual(2, ed.Associations.Count);
			Assert.AreEqual(1, ed.Columns.Count(_ => _.IsPrimaryKey));

		}

		[Test]
		public void AttributeInheritance2()
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b.Entity<MyInheritedClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"});

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass2));
			Assert.AreEqual(2, ed.Associations.Count);
			Assert.AreEqual(1, ed.Columns.Count(_ => _.IsPrimaryKey));

			var ed1 = ms.GetEntityDescriptor(typeof(MyBaseClass));
			Assert.AreEqual(0, ed1.Associations.Count);
			Assert.AreEqual(0, ed1.Columns.Count(_ => _.IsPrimaryKey));

		}

		[Test] 
		public void InterfaceInheritance()
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b.Entity<IInterfaceBase>()
				.HasTableName(nameof(IInterfaceBase))
				.Property(x => x.IntValue).HasSkipOnUpdate();

			b.Entity<IInheritedInterface>()
				.Property(x => x.StringValue).HasSkipOnUpdate().HasSkipOnInsert();

			b.Entity<IInterface2>()
				.Property(x => x.MarkedOnType).HasSkipOnInsert();

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass4));

			Assert.AreEqual(nameof(IInterfaceBase), ed.TableName);

			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.IntValue)]    .SkipOnUpdate);
			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.StringValue)] .SkipOnInsert);
			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.MarkedOnType)].SkipOnInsert);
		}
	}
}
