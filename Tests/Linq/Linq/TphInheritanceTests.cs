using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TphInheritanceTests : TestBase
	{

		[Test]
		public void TPH_DeepHierarchy_FindRecord([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphDeepPersonBase>();

			db.Insert(new TphDeepPersonLeaf() { Id = 1, Name = "Tom", Surname = "Black" });

			var items = db.GetTable<TphDeepPersonLeaf>().ToList();

			Assert.That(items, Has.Count.EqualTo(1));
			Assert.That(items[0], Is.InstanceOf<TphDeepPersonLeaf>());
			var gc = (TphDeepPersonLeaf)items[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(gc.Name, Is.EqualTo("Tom"));
				Assert.That(gc.Surname, Is.EqualTo("Black"));
			}
		}

		[Test]
		public void TPH_DeepHierarchy_PolymorphicResult([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphDeepPersonBase>();

			db.Insert(new TphDeepPersonLeaf() { Id = 1, Name = "Tom", Surname = "Black" });

			var items = db.GetTable<TphDeepPersonChild>().ToList();

			Assert.That(items, Has.Count.EqualTo(1));
			Assert.That(items[0], Is.InstanceOf<TphDeepPersonLeaf>());
			var gc = (TphDeepPersonLeaf)items[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(gc.Name, Is.EqualTo("Tom"));
				Assert.That(gc.Surname, Is.EqualTo("Black"));
			}
		}

		[Test]
		public void TPH_DeepHierarchy_BulkCopy([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] BulkCopyType copyType)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphDeepPersonBase>();

			var items = new TphDeepPersonBase[]
			{
				new TphDeepPersonLeaf() { Id = 1, Name = "Tom", Surname = "Black" }
			};

			tb.BulkCopy(new BulkCopyOptions() { BulkCopyType = copyType }, items);

			var res = tb.ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0], Is.InstanceOf<TphDeepPersonLeaf>());
			var gc = (TphDeepPersonLeaf)res[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(gc.Name, Is.EqualTo("Tom"));
				Assert.That(gc.Surname, Is.EqualTo("Black"));
			}
		}

		[Table("TphDeepPerson")]
		[InheritanceMapping(Code = "Child", IsDefault = true, Type = typeof(TphDeepPersonChild))]
		[InheritanceMapping(Code = "GrandChild", Type = typeof(TphDeepPersonLeaf))]
		abstract class TphDeepPersonBase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)] public string? Code { get; set; }
		}

		[Table]
		class TphDeepPersonChild : TphDeepPersonBase
		{
			[Column] public string? Name { get; set; }
		}

		abstract class TphDeepPersonAbstract : TphDeepPersonChild
		{
			[Column] public string? Surname { get; set; }
		}

		[Table("TphDeepPerson")]
		class TphDeepPersonLeaf : TphDeepPersonAbstract
		{ }

		[Test]
		public void TPH_PropertiesWithSameNameMapped([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSiblingTicketBase>();

			db.Insert(new TphSiblingTicketTypeA() { Id = 1, Code = "Code1" });
			db.Insert(new TphSiblingTicketTypeB() { Id = 2, Code = "Code2", Price = 123 });

			var res = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));

			Assert.That(res[0], Is.InstanceOf<TphSiblingTicketTypeA>());
			var child = (TphSiblingTicketTypeA)res[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(child.Code, Is.EqualTo("Code1"));

				Assert.That(res[1], Is.InstanceOf<TphSiblingTicketTypeB>());
			}

			var child2 = (TphSiblingTicketTypeB)res[1];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(child2.Code, Is.EqualTo("Code2"));
				Assert.That(child2.Price, Is.EqualTo(123));
			}
		}

		[Test]
		public void TPH_SiblingColumnNullPredicate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSiblingTicketBase>();

			db.Insert(new TphSiblingTicketTypeA  { Id = 1, Code = "C1" });           // TicketChild2Code = NULL in DB
			db.Insert(new TphSiblingTicketTypeB { Id = 2, Code = null,  Price = 0 }); // sibling col is NULL
			db.Insert(new TphSiblingTicketTypeB { Id = 3, Code = "C3", Price = 99 }); // sibling col is not NULL

			// CanBeNull = true on the sibling field (TicketChild2Code) is required for these predicates
			// to emit a real IS NULL / IS NOT NULL check rather than being optimized away.
			var withNullCode = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Where(t => t.Code == null)
				.Count();

			var withNonNullCode = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Where(t => t.Code != null)
				.Count();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(withNullCode,    Is.EqualTo(1));
				Assert.That(withNonNullCode, Is.EqualTo(1));
			}
		}

		[Test]
		public void TPH_SiblingColumn_Projection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSiblingTicketBase>();

			db.Insert(new TphSiblingTicketTypeA { Id = 1, Code = "A-code" });
			db.Insert(new TphSiblingTicketTypeB { Id = 2, Code = "B-code", Price = 5 });

			// Scalar projection must resolve each sibling's own physical column
			// (TicketChildCode vs TicketChild2Code), not collapse to the primary field.
			var codeA = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeA>()
				.Select(t => t.Code)
				.Single();

			var codeB = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Select(t => t.Code)
				.Single();

			// Object projection of sibling members.
			var objB = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Select(t => new { t.Code, t.Price })
				.Single();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(codeA,      Is.EqualTo("A-code"));
				Assert.That(codeB,      Is.EqualTo("B-code"));
				Assert.That(objB.Code,  Is.EqualTo("B-code"));
				Assert.That(objB.Price, Is.EqualTo(5));
			}
		}

		[Test]
		public void TPH_SiblingColumn_ValueFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSiblingTicketBase>();

			// Both rows carry the same logical value, but stored in different physical columns.
			db.Insert(new TphSiblingTicketTypeA { Id = 1, Code = "shared" });
			db.Insert(new TphSiblingTicketTypeB { Id = 2, Code = "shared", Price = 5 });

			// Each filter must target its own column; a wrong-column resolution would
			// compare against the other sibling's NULL and drop the matching row.
			var a = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeA>()
				.Where(t => t.Code == "shared")
				.ToArray();

			var b = db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Where(t => t.Code == "shared")
				.ToArray();

			Assert.That(a, Has.Length.EqualTo(1));
			Assert.That(b, Has.Length.EqualTo(1));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(a[0].Id, Is.EqualTo(1));
				Assert.That(b[0].Id, Is.EqualTo(2));
			}
		}

		[Test]
		public void TPH_SiblingColumn_Update([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSiblingTicketBase>();

			db.Insert(new TphSiblingTicketTypeA { Id = 1, Code = "A-old" });
			db.Insert(new TphSiblingTicketTypeB { Id = 2, Code = "B-old", Price = 5 });

			// Update must write the concrete sibling's own physical column and leave
			// the other sibling's column untouched.
			db.GetTable<TphSiblingTicketBase>()
				.OfType<TphSiblingTicketTypeB>()
				.Where(t => t.Id == 2)
				.Set(t => t.Code, "B-new")
				.Update();

			var a = db.GetTable<TphSiblingTicketBase>().OfType<TphSiblingTicketTypeA>().Single();
			var b = db.GetTable<TphSiblingTicketBase>().OfType<TphSiblingTicketTypeB>().Single();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(a.Code, Is.EqualTo("A-old"));
				Assert.That(b.Code, Is.EqualTo("B-new"));
			}
		}

		[Table("Tickets")]
		[InheritanceMapping(Code = "TicketChild", IsDefault = true, Type = typeof(TphSiblingTicketTypeA))]
		[InheritanceMapping(Code = "TicketChild2", Type = typeof(TphSiblingTicketTypeB))]
		abstract class TphSiblingTicketBase
		{
			[Column(IsDiscriminator = true)] public string? EventCode { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		class TphSiblingTicketTypeA : TphSiblingTicketBase
		{
			[Column("TicketChildCode")] public string? Code { get; set; }
		}

		class TphSiblingTicketTypeB : TphSiblingTicketBase
		{
			[Column("TicketChild2Code")] public string? Code { get; set; }
			[Column(CanBeNull = true)] public int Price { get; set; }
		}

		[Test]
		public void TPH_SiblingColumn_ThreeSiblings([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphTriBase>();

			db.Insert(new TphTriTypeA { Id = 1, Code = "a" });
			db.Insert(new TphTriTypeB { Id = 2, Code = "b" });
			db.Insert(new TphTriTypeC { Id = 3, Code = "c" });

			// Three siblings -> two sibling columns (CodeB, CodeC) beside the primary (CodeA).
			// Each concrete type must materialize from its own physical column.
			var all = db.GetTable<TphTriBase>().OrderBy(r => r.Id).ToArray();

			Assert.That(all, Has.Length.EqualTo(3));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(all[0], Is.InstanceOf<TphTriTypeA>());
				Assert.That(all[1], Is.InstanceOf<TphTriTypeB>());
				Assert.That(all[2], Is.InstanceOf<TphTriTypeC>());

				Assert.That(((TphTriTypeA)all[0]).Code, Is.EqualTo("a"));
				Assert.That(((TphTriTypeB)all[1]).Code, Is.EqualTo("b"));
				Assert.That(((TphTriTypeC)all[2]).Code, Is.EqualTo("c"));
			}
		}

		[Table("TphTriColumn")]
		[InheritanceMapping(Code = "T1", IsDefault = true, Type = typeof(TphTriTypeA))]
		[InheritanceMapping(Code = "T2", Type = typeof(TphTriTypeB))]
		[InheritanceMapping(Code = "T3", Type = typeof(TphTriTypeC))]
		abstract class TphTriBase
		{
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		class TphTriTypeA : TphTriBase { [Column("CodeA")] public string? Code { get; set; } }
		class TphTriTypeB : TphTriBase { [Column("CodeB")] public string? Code { get; set; } }
		class TphTriTypeC : TphTriBase { [Column("CodeC")] public string? Code { get; set; } }

		[Test]
		public void TPH_SiblingColumn_SharedPhysicalColumn([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphSharedColumnBase>();

			db.Insert(new TphSharedTypeA { Id = 1, Code = "a" });
			db.Insert(new TphSharedTypeB { Id = 2, Code = "b" });

			// Two siblings map the same member to the SAME physical column: dedup must collapse
			// them to a single field, so the table (and SELECT) carries SharedCode exactly once.
			// Without dedup the DDL would emit a duplicate SharedCode column.
			var all = db.GetTable<TphSharedColumnBase>().OrderBy(r => r.Id).ToArray();

			Assert.That(all, Has.Length.EqualTo(2));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(all[0], Is.InstanceOf<TphSharedTypeA>());
				Assert.That(all[1], Is.InstanceOf<TphSharedTypeB>());

				Assert.That(((TphSharedTypeA)all[0]).Code, Is.EqualTo("a"));
				Assert.That(((TphSharedTypeB)all[1]).Code, Is.EqualTo("b"));
			}
		}

		[Table("TphSharedColumn")]
		[InheritanceMapping(Code = "S1", IsDefault = true, Type = typeof(TphSharedTypeA))]
		[InheritanceMapping(Code = "S2", Type = typeof(TphSharedTypeB))]
		abstract class TphSharedColumnBase
		{
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		class TphSharedTypeA : TphSharedColumnBase { [Column("SharedCode")] public string? Code { get; set; } }
		class TphSharedTypeB : TphSharedColumnBase { [Column("SharedCode")] public string? Code { get; set; } }

		[Test]
		public void TPH_CodeFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCodeFilterBase>();

			db.Insert(new TphCodeFilterNamed() { Id = 1, Name = "Jane" });
			db.Insert(new TphCodeFilterAged() { Id = 2, Age = 10 });

			var result = db.GetTable<TphCodeFilterBase>().Where(e => e.Code != "Child").ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0], Is.InstanceOf<TphCodeFilterAged>());
			var record = (TphCodeFilterAged)result[0];
			Assert.That(record.Age, Is.EqualTo(10));
		}

		[Test]
		public void TPH_InterfaceFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCodeFilterBase>();

			db.Insert(new TphCodeFilterNamed() { Id = 1, Name = "Jane" });
			db.Insert(new TphCodeFilterAged() { Id = 2, Age = 10 });

			var result = db.GetTable<TphCodeFilterBase>().Where(e => !(e is IChild)).ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0], Is.InstanceOf<TphCodeFilterAged>());
			var record = (TphCodeFilterAged)result[0];
			Assert.That(record.Age, Is.EqualTo(10));
		}

		[Table(Name = "Base")]
		[InheritanceMapping(Code = "Base", IsDefault = true, Type = typeof(TphCodeFilterBase))]
		[InheritanceMapping(Code = "Child", Type = typeof(TphCodeFilterNamed))]
		[InheritanceMapping(Code = "Child2", Type = typeof(TphCodeFilterAged))]
		class TphCodeFilterBase
		{
			[Column(IsDiscriminator = true)] public string? Code { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		interface IChild
		{
			string? Name { get; }
		}

		[Table(Name = "Base")]
		class TphCodeFilterNamed : TphCodeFilterBase, IChild
		{
			[Column(CanBeNull = true)] public string? Name { get; set; }

			public TphCodeFilterNamed()
			{
				Code = "Child";
			}
		}

		[Table(Name = "Base")]
		class TphCodeFilterAged : TphCodeFilterBase
		{
			[Column(CanBeNull = true)] public int Age { get; set; }

			public TphCodeFilterAged()
			{
				Code = "Child2";
			}
		}

		[Test]
		public void TPH_TernaryIsTypePredicate([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool additionalFlag)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphConditionBase>();

			db.Insert(new TphConditionMale() { Id = 1, Name = "Jane" });
			db.Insert(new TphConditionAged() { Id = 2, Age = 10 });

			var res = db.GetTable<TphConditionBase>()
				.OrderBy(r => r.Id)
				.Where(e => e is TphConditionMiddle ? additionalFlag || e.Id != default : e.Id == default).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));

			Assert.That(res[0], Is.InstanceOf<TphConditionMale>());
			var child = (TphConditionMale)res[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(child.Code, Is.EqualTo("Child"));
				Assert.That(child.Name, Is.EqualTo("Jane"));

				Assert.That(res[1], Is.InstanceOf<TphConditionAged>());
			}

			var child2 = (TphConditionAged)res[1];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(child2.Code, Is.EqualTo("Child2"));
				Assert.That(child2.Age, Is.EqualTo(10));
			}
		}

		[Table(Name = "Base")]
		[InheritanceMapping(Code = "Base", IsDefault = true, Type = typeof(TphConditionBase))]
		[InheritanceMapping(Code = "BaseChild", Type = typeof(TphConditionMiddle))]
		[InheritanceMapping(Code = "Child", Type = typeof(TphConditionMale))]
		[InheritanceMapping(Code = "Child2", Type = typeof(TphConditionAged))]
		class TphConditionBase
		{
			[Column(IsDiscriminator = true)] public string? Code { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		[Table(Name = "Base")]
		class TphConditionMiddle : TphConditionBase
		{
			[Column(CanBeNull = true)] public string? Name { get; set; }

			public TphConditionMiddle()
			{
				Code = "BaseChild";
			}
		}

		[Table(Name = "Base")]
		class TphConditionMale : TphConditionMiddle
		{
			[Column(CanBeNull = true)] public bool IsMale { get; set; }

			public TphConditionMale()
			{
				Code = "Child";
			}
		}

		[Table(Name = "Base")]
		class TphConditionAged : TphConditionMiddle
		{
			[Column(CanBeNull = true)] public int Age { get; set; }

			public TphConditionAged()
			{
				Code = "Child2";
			}
		}

		[Test]
		public void TPH_ScalarProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale() { Id = 1, Name = "Jane" });
			db.Insert(new TphCastAged() { Id = 2, Age = 10 });

			var res = db.GetTable<TphCastBase>()
					.Cast<TphCastMiddle>()
					.OrderBy(x => x.Id)
					.Select(x => x.Name)
					.ToArray();

			Assert.That(res, Has.Length.EqualTo(2));

			Assert.That(res[0], Is.EqualTo("Jane"));
			Assert.That(res[1], Is.Null);
		}

		[Test]
		public void TPH_ObjectProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale() { Id = 1, Name = "Jane" });
			db.Insert(new TphCastAged() { Id = 2, Age = 10 });

			var res = db.GetTable<TphCastBase>()
					.Cast<TphCastMiddle>()
					.OrderBy(x => x.Id)
					.Select(x => new { x.Name })
					.ToArray();

			Assert.That(res, Has.Length.EqualTo(2));

			Assert.That(res[0].Name, Is.EqualTo("Jane"));
			Assert.That(res[1].Name, Is.Null);
		}

		[Table(Name = "Base")]
		[InheritanceMapping(Code = "Child", IsDefault = true, Type = typeof(TphCastMale))]
		[InheritanceMapping(Code = "Child2", Type = typeof(TphCastAged))]
		public class TphCastBase
		{
			[Column(IsDiscriminator = true)] public string? Code { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		[Table(Name = "Base")]
		public class TphCastMiddle : TphCastBase
		{
			[Column(CanBeNull = true)] public string? Name { get; set; }

			public TphCastMiddle()
			{
				Code = "BaseChild";
			}
		}

		[Table(Name = "Base")]
		public class TphCastMale : TphCastMiddle
		{
			[Column(CanBeNull = true)] public bool IsMale { get; set; }

			public TphCastMale()
			{
				Code = "Child";
			}
		}

		[Table(Name = "Base")]
		public class TphCastAged : TphCastMiddle
		{
			[Column(CanBeNull = true)] public int Age { get; set; }

			public TphCastAged()
			{
				Code = "Child2";
			}
		}

		[Test]
		public void TPH_Downcast_RowToLeaf([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale { Id = 1, Name = "Jane", IsMale = true });
			db.Insert(new TphCastAged { Id = 2, Name = "Bob",  Age    = 10 });

			// Downcast the whole row to a concrete leaf inside the projection.
			var res = db.GetTable<TphCastBase>()
				.Where(x => x is TphCastMale)
				.Select(x => (TphCastMale)x)
				.OrderBy(x => x.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Name,   Is.EqualTo("Jane"));
				Assert.That(res[0].IsMale, Is.True);
			}
		}

		[Test]
		public void TPH_Downcast_ThenIntermediateMember([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale { Id = 1, Name = "Jane", IsMale = true });
			db.Insert(new TphCastAged { Id = 2, Name = "Bob",  Age    = 10 });

			// Inline downcast to an (unmapped) intermediate, then read its member: ((T)x).Name.
			var res = db.GetTable<TphCastBase>()
				.OrderBy(x => x.Id)
				.Select(x => ((TphCastMiddle)x).Name)
				.ToArray();

			Assert.That(res, Is.EqualTo(new[] { "Jane", "Bob" }));
		}

		[Test]
		public void TPH_Downcast_ThenLeafMember([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale { Id = 1, Name = "Jane", IsMale = true });
			db.Insert(new TphCastAged { Id = 2, Name = "Bob",  Age    = 10 });

			// Inline downcast to a leaf, then read a leaf-only member: ((Leaf)x).IsMale.
			var res = db.GetTable<TphCastBase>()
				.Where(x => x is TphCastMale)
				.Select(x => ((TphCastMale)x).IsMale)
				.ToArray();

			Assert.That(res, Is.EqualTo(new[] { true }));
		}

		[Test]
		public void TPH_Downcast_ThenLeafMember_MixedRows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphCastBase>();

			db.Insert(new TphCastMale { Id = 1, Name = "Jane", IsMale = true });
			db.Insert(new TphCastAged { Id = 2, Name = "Bob",  Age    = 10 });

			// ((Descendant)x).DValue with NO discriminator filter: the leaf-only column is read
			// directly for every row. The matching row carries its real value; a non-matching row
			// (Aged) has no value stored in that column, so it reads as the column default.
			var res = db.GetTable<TphCastBase>()
				.OrderBy(x => x.Id)
				.Select(x => ((TphCastMale)x).IsMale)
				.ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(res, Has.Length.EqualTo(2));
				Assert.That(res[0], Is.True);  // Male row
				Assert.That(res[1], Is.False); // Aged row: column unset -> default
			}
		}

		[Table]
		public class TphCastHolder
		{
			[PrimaryKey] public int  Id      { get; set; }
			[Column]     public int? ValueId { get; set; }

			[Association(ThisKey = nameof(ValueId), OtherKey = nameof(TphCastBase.Id), CanBeNull = true)]
			public TphCastBase? DValue { get; set; }
		}

		[Test]
		public void TPH_Downcast_MemberToLeaf([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db          = GetDataContext(context);
			using var baseTable   = db.CreateLocalTable<TphCastBase>();
			using var holderTable = db.CreateLocalTable<TphCastHolder>();

			db.Insert(new TphCastMale   { Id = 1, Name = "Jane", IsMale = true });
			db.Insert(new TphCastAged   { Id = 2, Name = "Bob",  Age    = 10 });
			db.Insert(new TphCastHolder { Id = 1, ValueId = 1 });

			// (Descendant)x.DValue : access an association typed as the TPH base, then downcast it
			// to a concrete leaf inside the projection.
			var res = db.GetTable<TphCastHolder>()
				.Where(h => h.ValueId == 1)
				.Select(h => (TphCastMale)h.DValue!)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Name,   Is.EqualTo("Jane"));
				Assert.That(res[0].IsMale, Is.True);
			}
		}

		#region Associations on derived types

		[Table]
		public class TphAssocCompany
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column]     public string? Name { get; set; }
		}

		[Table("TphAssocPerson")]
		[InheritanceMapping(Code = "E", IsDefault = true, Type = typeof(TphAssocEmployee))]
		[InheritanceMapping(Code = "C", Type = typeof(TphAssocContractor))]
		public abstract class TphAssocPerson
		{
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[PrimaryKey]                     public int     Id   { get; set; }
		}

		public class TphAssocEmployee : TphAssocPerson
		{
			// FK column lives in the shared TPH table; NULL for non-employee rows.
			[Column] public int? CompanyId { get; set; }

			[Association(ThisKey = nameof(CompanyId), OtherKey = nameof(TphAssocCompany.Id), CanBeNull = true)]
			public TphAssocCompany? Company { get; set; }
		}

		public class TphAssocContractor : TphAssocPerson
		{
			[Column] public string? Agency { get; set; }
		}

		static void InsertAssocData(IDataContext db)
		{
			db.Insert(new TphAssocCompany    { Id = 1, Name = "Acme" });
			db.Insert(new TphAssocEmployee   { Id = 1, CompanyId = 1 });    // employee -> Acme
			db.Insert(new TphAssocContractor { Id = 2, Agency = "Temp" });  // not an employee
			db.Insert(new TphAssocEmployee   { Id = 3, CompanyId = null }); // employee, no company
		}

		[Test]
		public void TPH_Assoc_OnDerived_Navigate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var company = db.CreateLocalTable<TphAssocCompany>();
			using var person  = db.CreateLocalTable<TphAssocPerson>();

			InsertAssocData(db);

			// Association declared on the derived type, reached via OfType.
			var res = db.GetTable<TphAssocPerson>()
				.OfType<TphAssocEmployee>()
				.OrderBy(e => e.Id)
				.Select(e => e.Company!.Name)
				.ToArray();

			Assert.That(res, Is.EqualTo(new[] { "Acme", null }));
		}

		[Test]
		public void TPH_Assoc_OnDerived_LoadWith([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var company = db.CreateLocalTable<TphAssocCompany>();
			using var person  = db.CreateLocalTable<TphAssocPerson>();

			InsertAssocData(db);

			var res = db.GetTable<TphAssocPerson>()
				.OfType<TphAssocEmployee>()
				.LoadWith(e => e.Company)
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Company,       Is.Not.Null);
				Assert.That(res[0].Company!.Name, Is.EqualTo("Acme"));
				Assert.That(res[1].Company,       Is.Null);
			}
		}

		[Test]
		public void TPH_Assoc_OnDerived_DowncastNavigate_Unfiltered([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var company = db.CreateLocalTable<TphAssocCompany>();
			using var person  = db.CreateLocalTable<TphAssocPerson>();

			InsertAssocData(db);

			// No discriminator filter: ((Derived)x).Association over the whole hierarchy. The FK is
			// NULL for non-employee rows (and for employees without a company), so the LEFT JOIN
			// yields NULL there without an explicit discriminator check.
			var res = db.GetTable<TphAssocPerson>()
				.OrderBy(x => x.Id)
				.Select(x => ((TphAssocEmployee)x).Company!.Name)
				.ToArray();

			Assert.That(res, Is.EqualTo(new[] { "Acme", null, null }));
		}

		#endregion
	}
}
