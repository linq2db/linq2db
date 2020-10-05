using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	using Model;

	[TestFixture]
	public class FluentMappingTests : TestBase
	{
		[Table]
		class MyClass
		{
			public int ID;
			public int ID1 { get; set; }

			[NotColumn]
			public MyClass? Parent;
		}

		[Table(IsColumnAttributeRequired = true)]
		class MyClass2
		{
			public int ID { get; set; }

			public MyClass3? Class3 { get; set; }
		}

		[Table]
		class MyClass3
		{
			public int ID { get; set; }
		}

		class MyBaseClass
		{
			public int           Id;
			public MyClass?      Assosiation;
			public List<MyClass> Assosiations = null!;
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
			string? StringValue { get; set; }
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
			public string? StringValue { get; set; }
			public int     IntValue    { get; set; }
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

			Assert.That(ed["ID"]!.IsPrimaryKey);
		}

		[Test]
		public void HasPrimaryKey2()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().HasPrimaryKey(e => e.ID1, 3);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID1"]!.IsPrimaryKey);
			Assert.That(ed["ID1"]!.PrimaryKeyOrder, Is.EqualTo(3));
		}

		[Test]
		public void HasPrimaryKey3()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().HasPrimaryKey(e => new { e.ID, e.ID1 }, 3);

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!. IsPrimaryKey);
			Assert.That(ed["ID"]!. PrimaryKeyOrder, Is.EqualTo(3));
			Assert.That(ed["ID1"]!.IsPrimaryKey);
			Assert.That(ed["ID1"]!.PrimaryKeyOrder, Is.EqualTo(4));
		}

		[Test]
		public void HasPrimaryKey4()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>().Property(e => e.ID).IsPrimaryKey();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey);
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

			Assert.That(ed["ID"]!.IsPrimaryKey);
			Assert.That(ed["ID"]!.IsIdentity);
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
				.Association( e => e.Parent, e => e.ID, o => o!.ID1 );

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That( ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void FluentAssociation2()
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<MyClass>()
				.Association( e => e.Parent, (e, o) => e.ID == o!.ID1 );

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
			public string FirstName { get; set; } = null!;
		}

		public class TestInheritanceFemale : TestInheritancePerson
		{
			public string FirstName { get; set; } = null!;
			public string LastName  { get; set; } = null!;
		}

		[Test]
		public void FluentInheritance([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

		[Test]
		public void FluentInheritance2([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

		class BaseEntity
		{
			public int Id { get; set; }

			[NotColumn]
			public int Value { get; set; }

			public int ValueMethod()
			{
				throw new NotImplementedException();
			}
		}

		class DescendantEntity : BaseEntity
		{
		}

		[Test]
		public void FluentInheritanceExpression([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = MappingSchema.Default; // new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<DescendantEntity>()
				.Property(e => e.Value).IsExpression(e => e.Id + 100)
				.Member(e => e.ValueMethod()).IsExpression(e => e.Id + 1000);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(
				new[] { new DescendantEntity{Id = 1, Value = 0}, new DescendantEntity{Id = 2, Value = 0} })
			)
			{
				var items1 = table.Where(e => e.Value == 101).ToArray();
				var items2 = table.Where(e => e.ValueMethod() == 1001).ToArray();

				Assert.AreEqual(1, items1.Length);

				AreEqualWithComparer(items1, items2);
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

			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.IntValue)]!    .SkipOnUpdate);
			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.StringValue)]! .SkipOnInsert);
			Assert.AreEqual(true, ed[nameof(MyInheritedClass4.MarkedOnType)]!.SkipOnInsert);
		}

		/// issue 291 Tests
		public enum GenericItemType
		{
			DerivedClass = 0,
			DerivedClass1 = 1,
		}

		public class BaseClass
		{
			public string? MyCol1;
			public string? NotACol;
		}

		public class DerivedClass : BaseClass
		{
			[Column(IsDiscriminator = true)]
			public GenericItemType itemType = GenericItemType.DerivedClass;
			public string? SomeOtherField;

		}

		public class DerivedClass1 : BaseClass
		{
			[Column(IsDiscriminator = true)]
			public GenericItemType itemType = GenericItemType.DerivedClass1;
			public string? SomeOtherField;
		}

		[Test]
		public void Issue291Test2Attr([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				db.MappingSchema.GetFluentMappingBuilder()

				   .Entity<BaseClass>().HasTableName("my_table")
				   .HasAttribute(new LinqToDB.Mapping.InheritanceMappingAttribute()
				   {
					   IsDefault = true,
					   Type = typeof(DerivedClass),
					   Code = GenericItemType.DerivedClass
				   })
				   .HasAttribute(new LinqToDB.Mapping.InheritanceMappingAttribute()
				   {
					   Type = typeof(DerivedClass1),
					   Code = GenericItemType.DerivedClass1
				   })
				  .Property(t => t.MyCol1).HasColumnName("my_col1")
				  .Property(t => t.NotACol).IsNotColumn()

				  .Entity<DerivedClass>().Property(t => t.SomeOtherField).HasColumnName("my_other_col")
				  .Entity<DerivedClass1>().Property(t => t.SomeOtherField).HasColumnName("my_other_col");

				using (db.CreateLocalTable<DerivedClass>())
				{
					DerivedClass item = new DerivedClass { NotACol = "test", MyCol1 = "MyCol1" };
					db.Insert(item);
					DerivedClass1 item1 = new DerivedClass1 { NotACol = "test" };
					db.Insert(item1);

					DerivedClass res = db.GetTable<DerivedClass>().FirstOrDefault();
					var count = db.GetTable<DerivedClass>().Count();

					Assert.AreEqual(item.MyCol1, res.MyCol1);
					Assert.AreNotEqual(item.NotACol, res.NotACol);
					Assert.AreEqual(1, count);
				}
			}
		}

		[Test]
		public void Issue291Test1Attr([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				db.MappingSchema.GetFluentMappingBuilder()
				   .Entity<BaseClass>().HasTableName("my_table")
				   .HasAttribute(new LinqToDB.Mapping.InheritanceMappingAttribute()
				   {
					   IsDefault = true,
					   Type = typeof(DerivedClass),
					   Code = GenericItemType.DerivedClass
				   })
				  .Property(t => t.MyCol1).HasColumnName("my_col1")
				  .Property(t => t.NotACol).IsNotColumn()
				  .Entity<DerivedClass>().Property(t => t.SomeOtherField).HasColumnName("my_other_col")
				  .Entity<DerivedClass1>().Property(t => t.SomeOtherField).HasColumnName("my_other_col");

				using (db.CreateLocalTable<DerivedClass>())
				{
					DerivedClass item = new DerivedClass { NotACol = "test", MyCol1 = "MyCol1" };
					db.Insert(item);
					DerivedClass1 item1 = new DerivedClass1 { NotACol = "test", MyCol1 = "MyCol2" };
					db.Insert(item1);

					DerivedClass res = db.GetTable<DerivedClass>().Where(o => o.MyCol1 == "MyCol1").FirstOrDefault();
					var count = db.GetTable<DerivedClass>().Count();

					Assert.AreEqual(item.MyCol1, res.MyCol1);
					Assert.AreNotEqual(item.NotACol, res.NotACol);
					Assert.AreEqual(2, count);
				}
			}
		}

		[Table("PERSON")]
		public class PersonCustom
		{
			[Column("FIRST_NAME")]
			public string Name { get; set; } = null!;

			[ExpressionMethod(nameof(AgeExpr), IsColumn = true, Alias = "AGE")]
			public int Age{ get; set; }

			public static Expression<Func<PersonCustom, int>> AgeExpr()
			{
				return p => Sql.AsSql(5);
			}

			public int Money { get; set; }

		}

		[Test]
		public void ExpressionAlias([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool finalAliases)
		{
			using (new GenerateFinalAliases(finalAliases))
			using (var db = GetDataContext(context))
			{
				Query.ClearCaches();

				var query = db.GetTable<PersonCustom>().Where(p => p.Name != "");
				var sql1 = query.ToString();
				TestContext.WriteLine(sql1);

				if (finalAliases)
					Assert.That(sql1, Does.Contain("[AGE]"));
				else
					Assert.That(sql1, Does.Not.Contain("[AGE]"));

				var sql2 = query.Select(q => new { q.Name, q.Age }).ToString();
				TestContext.WriteLine(sql2);

				if (finalAliases)
					Assert.That(sql2, Does.Contain("[Age]"));
				else
					Assert.That(sql2, Does.Not.Contain("[Age]"));
			}
		}

		[Test]
		public void ExpressionAliasFluent([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool finalAliases)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<PersonCustom>()
				.Property(p => p.Money).IsExpression(p => Sql.AsSql(p.Age * Sql.AsSql(1000) + p.Name.Length * 10), true, "MONEY");

			using (new GenerateFinalAliases(finalAliases))
			using (var db = GetDataContext(context, ms))
			{
				Query.ClearCaches();

				var query = db.GetTable<PersonCustom>().Where(p => p.Name != "");
				var sql1 = query.ToString();
				TestContext.WriteLine(sql1);

				if (finalAliases)
					Assert.That(sql1, Does.Contain("[MONEY]"));
				else
					Assert.That(sql1, Does.Not.Contain("[MONEY]"));

				var sql2 = query.Select(q => new { q.Name, q.Money }).ToString();
				TestContext.WriteLine(sql2);

				if (finalAliases)
					Assert.That(sql2, Does.Contain("[Money]"));
				else
					Assert.That(sql2, Does.Not.Contain("[Money]"));
			}
		}

	}
}
