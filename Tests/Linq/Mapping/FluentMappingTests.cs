using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Mapping
{
	using Model;

	[TestFixture]
	public class FluentMappingTests : TestBase
	{
		[Table]
		sealed class MyClass
		{
			public int ID;
			public int ID1 { get; set; }

			[NotColumn]
			public MyClass? Parent;
		}

		[Table(IsColumnAttributeRequired = true)]
		sealed class MyClass2
		{
			public int ID { get; set; }

			public MyClass3? Class3 { get; set; }
		}

		[Table]
		sealed class MyClass3
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

		sealed class MyInheritedClass2 : MyInheritedClass
		{
		}

		class MyInheritedClass3 : IInheritedInterface
		{
			public string? StringValue { get; set; }
			public int     IntValue    { get; set; }
		}

		sealed class MyInheritedClass4 : MyInheritedClass3, IInterface2
		{
			public int MarkedOnType { get; set; }
		}

		[Test]
		public void LowerCaseMappingTest()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			MappingSchema.EntityDescriptorCreatedCallback = (mappingSchema, entityDescriptor) =>
			{
				entityDescriptor.TableName = entityDescriptor.TableName.ToLowerInvariant();
				foreach (var entityDescriptorColumn in entityDescriptor.Columns)
				{
					entityDescriptorColumn.ColumnName = entityDescriptorColumn.ColumnName.ToLowerInvariant();
				}
			};

			try
			{
				mb.Entity<MyClass>().HasTableName("NewName").Property(x => x.ID1).IsColumn().Build();

				var ed = ms.GetEntityDescriptor(typeof(MyClass));

				Assert.That(ed.Name.Name, Is.EqualTo("newname"));
				Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("id1"));
			}
			finally
			{
				MappingSchema.EntityDescriptorCreatedCallback = null;
			}
		}

		[Test]
		public void AddAttributeTest1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.HasAttribute<MyClass>(new TableAttribute("NewName"))
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Name.Name, Is.EqualTo("NewName"));

			var ms2 = new MappingSchema();
			var ed2 = ms2.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed2.Name.Name, Is.EqualTo("MyClass"));
		}

		[Test]
		public void AddAttributeTest2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.HasAttribute<MyClass>(new TableAttribute("NewName") { Configuration = "Test"}).Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Name.Name, Is.EqualTo("MyClass"));
		}

		[Test]
		public void HasPrimaryKey1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>().HasPrimaryKey(e => e.ID).Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey);
		}

		[Test]
		public void HasPrimaryKey2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>().HasPrimaryKey(e => e.ID1, 3).Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID1"]!.IsPrimaryKey);
			Assert.That(ed["ID1"]!.PrimaryKeyOrder, Is.EqualTo(3));
		}

		[Test]
		public void HasPrimaryKey3()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>().HasPrimaryKey(e => new { e.ID, e.ID1 }, 3).Build();

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
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>().Property(e => e.ID).IsPrimaryKey().Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey);
		}

		[Test]
		public void IsPrimaryKeyIsIdentity()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Property(e => e.ID)
					.IsPrimaryKey()
					.IsIdentity()
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed["ID"]!.IsPrimaryKey);
			Assert.That(ed["ID"]!.IsIdentity);
		}

		[Test]
		public void TableNameAndSchema()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.HasTableName ("Table")
				.HasSchemaName("Schema")
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Name.Name,   Is.EqualTo("Table"));
			Assert.That(ed.Name.Schema, Is.EqualTo("Schema"));
		}

		[Test]
		public void Association()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Property(e => e.Parent)
					.HasAttribute(new AssociationAttribute { ThisKey = "ID", OtherKey = "ID1" })
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Associations, Is.Not.EqualTo(0));
		}

		[Test]
		public void PropertyIncluded()
		{
			var ms = new MappingSchema();

			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass2>()
				.Property(e => e.ID).IsPrimaryKey()
				.Property(e => e.Class3)
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass2));

			Assert.That(ed["Class3"], Is.Not.Null);
		}

		[Test]
		public void FluentAssociation1()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Association( e => e.Parent, e => e.ID, o => o!.ID1 )
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That( ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void FluentAssociation2()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyClass>()
				.Association( e => e.Parent, (e, o) => e.ID == o!.ID1 )
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.That(ed.Associations, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void FluentAssociation3()
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<MyInheritedClass>()
				.Association( e => e.Assosiations, (e, o) => e.Id == o.ID1 )
				.Build();

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
		public void FluentInheritance([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<TestInheritancePerson>()
				.Inheritance(e => e.Gender, Gender.Male,   typeof(TestInheritanceMale))
				.Inheritance(e => e.Gender, Gender.Female, typeof(TestInheritanceFemale))
				.Build();

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
		public void FluentInheritance2([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<TestInheritancePerson>()
				.Inheritance(e => e.Gender, Gender.Male,   typeof(TestInheritanceMale))
				.Inheritance(e => e.Gender, Gender.Female, typeof(TestInheritanceFemale))
				.Build();

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

		sealed class DescendantEntity : BaseEntity
		{
		}

		[Test]
		public void FluentInheritanceExpression([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<DescendantEntity>()
				.Property(e => e.Value).IsExpression(e => e.Id + 100)
				.Member(e => e.ValueMethod()).IsExpression(e => e.Id + 1000)
				.Build();

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
			var b  = new FluentMappingBuilder(ms);

			b.Entity<MyClass>().HasTableName("Name1").Build();

			var od1 = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.AreEqual("Name1", od1.Name.Name);

			b.Entity<MyClass>().HasTableName("Name2").Build();

			var od2 = ms.GetEntityDescriptor(typeof(MyClass));

			Assert.AreEqual("Name2", od2.Name.Name);
		}

		[Test]
		public void AssociationInheritance()
		{
			var ms = new MappingSchema();
			var b  = new FluentMappingBuilder(ms);

			b.Entity<MyInheritedClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"})
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass));
			Assert.AreEqual(2, ed.Associations.Count);
		}

		[Test]
		public void AttributeInheritance()
		{
			var ms = new MappingSchema();
			var b  = new FluentMappingBuilder(ms);

			b.Entity<MyBaseClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"})
				.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass));
			Assert.AreEqual(2, ed.Associations.Count);
			Assert.AreEqual(1, ed.Columns.Count(_ => _.IsPrimaryKey));

		}

		[Test]
		public void AttributeInheritance2()
		{
			var ms = new MappingSchema();
			var b  = new FluentMappingBuilder(ms);

			b.Entity<MyInheritedClass>()
				.Property(_ => _.Id)          .IsPrimaryKey()
				.Property(_ => _.Assosiation) .HasAttribute(new AssociationAttribute() {ThisKey = "Assosiation.ID", OtherKey = "ID"})
				.Property(_ => _.Assosiations).HasAttribute(new AssociationAttribute() {ThisKey = "Id",             OtherKey = "ID1"})
				.Build();

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
			var b  = new FluentMappingBuilder(ms);

			b.Entity<IInterfaceBase>()
				.HasTableName(nameof(IInterfaceBase))
				.Property(x => x.IntValue).HasSkipOnUpdate();

			b.Entity<IInheritedInterface>()
				.Property(x => x.StringValue).HasSkipOnUpdate().HasSkipOnInsert();

			b.Entity<IInterface2>()
				.Property(x => x.MarkedOnType).HasSkipOnInsert();

			b.Build();

			var ed = ms.GetEntityDescriptor(typeof(MyInheritedClass4));

			Assert.AreEqual(nameof(IInterfaceBase), ed.Name.Name);

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
		public void Issue291Test2Attr([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				new FluentMappingBuilder(db.MappingSchema)

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
				  .Entity<DerivedClass1>().Property(t => t.SomeOtherField).HasColumnName("my_other_col")
				  .Build();

				using (db.CreateLocalTable<DerivedClass>())
				{
					DerivedClass item = new DerivedClass { NotACol = "test", MyCol1 = "MyCol1" };
					db.Insert(item);
					DerivedClass1 item1 = new DerivedClass1 { NotACol = "test" };
					db.Insert(item1);

					DerivedClass res = db.GetTable<DerivedClass>().First();
					var count = db.GetTable<DerivedClass>().Count();

					Assert.AreEqual(item.MyCol1, res.MyCol1);
					Assert.AreNotEqual(item.NotACol, res.NotACol);
					Assert.AreEqual(1, count);
				}
			}
		}

		[Test]
		public void Issue291Test1Attr([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			{
				new FluentMappingBuilder(db.MappingSchema)
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
				  .Entity<DerivedClass1>().Property(t => t.SomeOtherField).HasColumnName("my_other_col")
				  .Build();

				using (db.CreateLocalTable<DerivedClass>())
				{
					DerivedClass item = new DerivedClass { NotACol = "test", MyCol1 = "MyCol1" };
					db.Insert(item);
					DerivedClass1 item1 = new DerivedClass1 { NotACol = "test", MyCol1 = "MyCol2" };
					db.Insert(item1);

					DerivedClass res = db.GetTable<DerivedClass>().Where(o => o.MyCol1 == "MyCol1").First();
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

			new FluentMappingBuilder(ms)
				.Entity<PersonCustom>()
				.Property(p => p.Money).IsExpression(p => Sql.AsSql(p.Age * Sql.AsSql(1000) + p.Name.Length * 10), true, "MONEY")
				.Build();

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

		public class SequenceTable
		{
			public int Id { get; set; }
		}

		[Test]
		public void TestSequenceHelper([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<SequenceTable>()
				.Property(p => p.Id)
				.UseSequence("sequencetestseq")
				.Build();

			using var db = GetDataConnection(context, ms);
			var records = Enumerable.Range(1, 10).Select(x => new SequenceTable()).ToArray();

			records.RetrieveIdentity(db, true);

			for (var i = 0; i < records.Length; i++)
				Assert.AreEqual(records[0].Id + i, records[i].Id);
		}

		[Test]
		public void TestSequenceAttribute([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<SequenceTable>()
				.Property(p => p.Id)
				.HasAttribute(new SequenceNameAttribute("sequencetestseq"))
				.Build();

			using var db = GetDataConnection(context, ms);
			var records = Enumerable.Range(1, 10).Select(x => new SequenceTable()).ToArray();

			records.RetrieveIdentity(db, true);

			for (var i = 0; i < records.Length; i++)
				Assert.AreEqual(records[0].Id + i, records[i].Id);
		}

		[Table("Person")]
		sealed class EnumPerson
		{
			[Column] public int        PersonID;
			[Column] public GenderEnum Gender;
		}

		public enum GenderEnum
		{
			Male,
			Female,
			Unknown,
			Other,
		}

		[Test]
		public void MapValueTest([DataSources] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.HasAttribute(typeof(GenderEnum).GetField(nameof(GenderEnum.Male))!,    new MapValueAttribute("M"));
			mb.HasAttribute(typeof(GenderEnum).GetField(nameof(GenderEnum.Female))!,  new MapValueAttribute("F"));
			mb.HasAttribute(typeof(GenderEnum).GetField(nameof(GenderEnum.Unknown))!, new MapValueAttribute("U"));
			mb.HasAttribute(typeof(GenderEnum).GetField(nameof(GenderEnum.Other))!,   new MapValueAttribute("O"));

			mb.Build();

			using var db = GetDataContext(context, ms);

			var records = db.GetTable<EnumPerson>().OrderBy(r => r.PersonID).ToArray();

			Assert.AreEqual(4, records.Length);
			Assert.AreEqual(GenderEnum.Male,   records[0].Gender);
			Assert.AreEqual(GenderEnum.Male,   records[1].Gender);
			Assert.AreEqual(GenderEnum.Female, records[2].Gender);
			Assert.AreEqual(GenderEnum.Male,   records[3].Gender);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3119")]
		public void Issue3119Test()
		{
			var mb = new FluentMappingBuilder();

			mb.Entity<Issue3119Entity>()
				.Property(u => u.UserId)
				.HasAttribute(new ColumnAttribute() { IsIdentity = true })
				.HasAttribute(new ColumnAttribute() { IsPrimaryKey = true });

			mb.Build();

			var attrs = mb.MappingSchema.GetAttributes<ColumnAttribute>(typeof(Issue3119Entity), typeof(Issue3119Entity).GetProperty("UserId")!);

			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].IsIdentity, Is.True);
			Assert.That(attrs[0].IsPrimaryKey, Is.True);
		}

		sealed class Issue3119Entity
		{
			public int UserId { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3136")]
		public void Issue3136Test()
		{
			const string configuration = "MyConfiguration";
			var mb = new FluentMappingBuilder();

			mb
				.Entity<Issue3119Entity>()
				.Property(u => u.UserId)
				.Entity<Issue3119Entity>(configuration)
				.HasAttribute(new ColumnAttribute() { IsIdentity = true });

			mb.Build();

			var attrs = mb.MappingSchema.GetAttributes<ColumnAttribute>(typeof(Issue3119Entity), typeof(Issue3119Entity).GetProperty("UserId")!);

			Assert.That(attrs, Has.Length.EqualTo(2));
			Assert.That(attrs[0].IsIdentity, Is.False);
			Assert.That(attrs[0].Configuration, Is.Null);
			Assert.That(attrs[1].IsIdentity, Is.True);
			Assert.That(attrs[1].Configuration, Is.EqualTo(configuration));
		}
	}
}
