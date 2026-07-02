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

			// Two siblings map the same member to the SAME physical column. The entity SELECT must project
			// that column exactly once: the sibling's value expression is built from the primary member
			// (BuildGenericFromMembers), so both collapse to one column. Before the fix SELECT had it twice.
			var query = db.GetTable<TphSharedColumnBase>().OrderBy(r => r.Id);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql.Split(["SharedCode"], System.StringSplitOptions.None).Length - 1, Is.EqualTo(1), sql);

			var all = query.ToArray();

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
		public void TPH_SiblingColumn_ColumnNameEqualsOtherMemberName([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// A sibling maps its member to a physical column whose name equals a different member's C# name
			// (the base member Shared, which itself maps to a distinct physical column). The sibling field's
			// _fieldsLookup key must not collide with the base Shared field, or SqlTable construction throws.
			using var tb = db.CreateLocalTable<TphNameCollisionBase>();

			db.Insert(new TphNameCollisionTypeA { Id = 1, Shared = "base-a", Extra = "a" });
			db.Insert(new TphNameCollisionTypeB { Id = 2, Shared = "base-b", Extra = "b" });

			var all = db.GetTable<TphNameCollisionBase>().OrderBy(r => r.Id).ToArray();

			Assert.That(all, Has.Length.EqualTo(2));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(all[0], Is.InstanceOf<TphNameCollisionTypeA>());
				Assert.That(all[1], Is.InstanceOf<TphNameCollisionTypeB>());

				Assert.That(((TphNameCollisionTypeA)all[0]).Extra, Is.EqualTo("a"));
				Assert.That(((TphNameCollisionTypeB)all[1]).Extra, Is.EqualTo("b"));

				Assert.That(all[0].Shared, Is.EqualTo("base-a"));
				Assert.That(all[1].Shared, Is.EqualTo("base-b"));
			}
		}

		[Table("TphNameCollision")]
		[InheritanceMapping(Code = "N1", IsDefault = true, Type = typeof(TphNameCollisionTypeA))]
		[InheritanceMapping(Code = "N2", Type = typeof(TphNameCollisionTypeB))]
		abstract class TphNameCollisionBase
		{
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[PrimaryKey] public int Id { get; set; }
			[Column("shared_phys")] public string? Shared { get; set; }
		}

		class TphNameCollisionTypeA : TphNameCollisionBase { [Column("ExtraA")] public string? Extra { get; set; } }
		class TphNameCollisionTypeB : TphNameCollisionBase { [Column("Shared")] public string? Extra { get; set; } }

		[Test]
		public void TPH_SiblingColumn_ThreeSiblingsDivergentReadShapes([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphDupColBase>();

			db.Insert(new TphDupColFirst  { Id = 1, Payload = 10 });
			db.Insert(new TphDupColSecond { Id = 2, Payload = 20 });
			db.Insert(new TphDupColThird  { Id = 3, Payload = 30 });

			// Three siblings map the same physical column. The first reads it as long; the next two read it
			// identically as int and must collapse onto a single projected column — so Payload appears in the
			// SELECT once per distinct read-shape (twice: long + int), not three times. Before the cache fix
			// only the first read-shape per physical column was cached, so the two int siblings each rebuilt a
			// projection and the column was emitted three times.
			var query = db.GetTable<TphDupColBase>().OrderBy(r => r.Id);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql.Split(["[Payload]"], System.StringSplitOptions.None).Length - 1, Is.EqualTo(2), sql);

			var all = query.ToArray();
			Assert.That(all, Has.Length.EqualTo(3));
		}

		[Table("TphDupCol")]
		[InheritanceMapping(Code = "D1", IsDefault = true, Type = typeof(TphDupColFirst))]
		[InheritanceMapping(Code = "D2", Type = typeof(TphDupColSecond))]
		[InheritanceMapping(Code = "D3", Type = typeof(TphDupColThird))]
		abstract class TphDupColBase
		{
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[PrimaryKey] public int Id { get; set; }
		}

		class TphDupColFirst  : TphDupColBase { [Column("Payload")] public long Payload { get; set; } }
		class TphDupColSecond : TphDupColBase { [Column("Payload")] public int  Payload { get; set; } }
		class TphDupColThird  : TphDupColBase { [Column("Payload")] public int  Payload { get; set; } }

		[ActiveIssue("Sibling subtypes mapping the same physical column with different ValueConverters share one SqlField/ColumnDescriptor, so the second sibling's converter is not applied on read (pre-existing, independent of the duplicate-column projection fix).")]
		[Test]
		public void TPH_SiblingColumn_DifferentValueConverters([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TphConvBase>();

			db.Insert(new TphConvPlain  { Id = 1, Kind = 1, Value = 7 });
			db.Insert(new TphConvScaled { Id = 2, Kind = 2, Value = 7 });

			var all = db.GetTable<TphConvBase>().OrderBy(r => r.Id).ToArray();

			Assert.That(all, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				// Two siblings map the same physical column but read it through DIFFERENT value converters.
				// Each must read with its own converter (TphConvScaled stores Value*100 and reads /100), so
				// the dedup must NOT collapse them onto a single read expression.
				Assert.That(((TphConvPlain)all[0]).Value,  Is.EqualTo(7));
				Assert.That(((TphConvScaled)all[1]).Value, Is.EqualTo(7));
			}
		}

		sealed class TphConvTimesHundred() : ValueConverter<int, int>(v => v * 100, v => v / 100, false);

		[Table("TphConv")]
		[InheritanceMapping(Code = 1, IsDefault = true, Type = typeof(TphConvPlain))]
		[InheritanceMapping(Code = 2, Type = typeof(TphConvScaled))]
		abstract class TphConvBase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)] public int Kind { get; set; }
		}

		class TphConvPlain : TphConvBase
		{
			[Column("Payload")] public int Value { get; set; }
		}

		class TphConvScaled : TphConvBase
		{
			[Column("Payload"), ValueConverter(ConverterType = typeof(TphConvTimesHundred))] public int Value { get; set; }
		}

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
		public void TPH_TernaryIsTypePredicate([DataSources(TestProvName.AllSybase)] string context, [Values] bool additionalFlag)
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

		[Table]
		public class TphRefCompany
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column]     public string? Name { get; set; }
		}

		[Table("TphRefPerson")]
		[InheritanceMapping(Code = "E", IsDefault = true, Type = typeof(TphRefEmployee))]
		[InheritanceMapping(Code = "C", Type = typeof(TphRefContractor))]
		public abstract class TphRefPerson
		{
			[Column(IsDiscriminator = true)] public string? Kind  { get; set; }
			[PrimaryKey]                     public int     Id    { get; set; }
			// FK column declared on the base, so it is populated for EVERY row regardless of type.
			[Column]                         public int?    RefId { get; set; }
		}

		public class TphRefEmployee : TphRefPerson
		{
			// Association on the derived type, keyed on the inherited base column.
			[Association(ThisKey = nameof(RefId), OtherKey = nameof(TphRefCompany.Id), CanBeNull = true)]
			public TphRefCompany? Company { get; set; }
		}

		public class TphRefContractor : TphRefPerson
		{
		}

		[Test]
		public void TPH_Assoc_OnDerived_NonNullFkForNonMatchingRow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var company = db.CreateLocalTable<TphRefCompany>();
			using var person  = db.CreateLocalTable<TphRefPerson>();

			db.Insert(new TphRefCompany    { Id = 1, Name = "Acme" });
			db.Insert(new TphRefEmployee   { Id = 1, RefId = 1 }); // employee -> Company 1
			db.Insert(new TphRefContractor { Id = 2, RefId = 1 }); // contractor, same RefId, NOT an employee

			// ((Employee)x).Company over the whole hierarchy. RefId is a base column, so the contractor
			// row also has RefId=1. The association is declared on the derived Employee, so the join must
			// carry the Employee discriminator — otherwise the contractor would spuriously match Company 1.
			var res = db.GetTable<TphRefPerson>()
				.OrderBy(x => x.Id)
				.Select(x => ((TphRefEmployee)x).Company!.Name)
				.ToArray();

			Assert.That(res, Is.EqualTo(new[] { "Acme", null }));
		}

		#endregion

		#region Multi-level shared column (diagnostic)

		[Table("TphMl")]
		[InheritanceMapping(Code = 1, Type = typeof(TphMlDirect))]
		[InheritanceMapping(Code = 2, Type = typeof(TphMlLeaf))]
		[InheritanceMapping(Code = 3, Type = typeof(TphMlLeaf2))]
		public abstract class TphMlBase
		{
			[PrimaryKey]                     public int Id   { get; set; }
			[Column(IsDiscriminator = true)] public int Kind { get; set; }
		}

		public class TphMlDirect : TphMlBase
		{
			[Column, LinqToDB.Mapping.Nullable] public int Shared { get; set; }
		}

		public abstract class TphMlMid : TphMlBase
		{
			[Column, LinqToDB.Mapping.Nullable] public int MidField { get; set; }
		}

		public class TphMlLeaf : TphMlMid
		{
			[Column, LinqToDB.Mapping.Nullable] public int Shared { get; set; }
		}

		public class TphMlLeaf2 : TphMlMid
		{
		}

		[Test]
		public void TPH_MultiLevel_SharedAndInheritedColumns([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// A base-typed (mixed) bulk insert exercises two shared-storage cases at once:
			//  - "Shared" is one physical column owned by the sibling types Direct and Leaf;
			//  - "MidField" is declared on the abstract intermediate and inherited by both Leaf and the
			//    column-less Leaf2.
			// Every member must round-trip — including MidField on Leaf2, which owns no column of its own
			// and whose value is written through the intermediate's declaring type.
			var data = new TphMlBase[]
			{
				new TphMlDirect { Id = 1, Kind = 1, Shared = 10 },
				new TphMlLeaf   { Id = 2, Kind = 2, Shared = 20, MidField = 30 },
				new TphMlLeaf2  { Id = 3, Kind = 3, MidField = 40 },
			};
			using var tb = db.CreateLocalTable(data);

			var all = db.GetTable<TphMlBase>().OrderBy(x => x.Id).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(((TphMlDirect)all[0]).Shared,  Is.EqualTo(10));
				Assert.That(((TphMlLeaf)all[1]).Shared,    Is.EqualTo(20));
				Assert.That(((TphMlLeaf)all[1]).MidField,  Is.EqualTo(30));
				Assert.That(((TphMlLeaf2)all[2]).MidField, Is.EqualTo(40));
			}
		}

		[Test]
		public void TPH_OfTypeAbstractIntermediate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var data = new TphMlBase[]
			{
				new TphMlDirect { Id = 1, Kind = 1, Shared = 10 },
				new TphMlLeaf   { Id = 2, Kind = 2, Shared = 20, MidField = 30 },
				new TphMlLeaf2  { Id = 3, Kind = 3, MidField = 40 },
			};
			using var tb = db.CreateLocalTable(data);

			// OfType against the abstract intermediate must materialize the concrete subtypes per
			// discriminator (Leaf, Leaf2) — not attempt to construct the abstract TphMlMid itself.
			var mids = db.GetTable<TphMlBase>().OfType<TphMlMid>().OrderBy(x => x.Id).ToArray();

			Assert.That(mids, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(mids[0], Is.InstanceOf<TphMlLeaf>());
				Assert.That(mids[1], Is.InstanceOf<TphMlLeaf2>());
				Assert.That(((TphMlLeaf)mids[0]).MidField,  Is.EqualTo(30));
				Assert.That(((TphMlLeaf2)mids[1]).MidField, Is.EqualTo(40));
			}
		}

		#endregion

		#region TPH intermediate type
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_ViaIntermediateTable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingIntermediate>().OrderBy(r => r.Id).ToArray();

			AssertIntermediateThings(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_FullHierarchy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingBase>().OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(4));

			Assert.That(result[0], Is.InstanceOf<TphThingAlpha>());
			var item1 = (TphThingAlpha)(object)result[0]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item1.Id, Is.EqualTo(1));
				Assert.That(item1.Type, Is.EqualTo(1));
				Assert.That(item1.BaseField, Is.EqualTo(2));

				Assert.That(result[1], Is.InstanceOf<TphThingBeta>());
			}

			var item2 = (TphThingBeta)(object)result[1]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item2.Id, Is.EqualTo(2));
				Assert.That(item2.Type, Is.EqualTo(2));
				Assert.That(item2.BaseField, Is.EqualTo(3));
				Assert.That(item2.ConcreteField, Is.EqualTo(4));

				Assert.That(result[2], Is.InstanceOf<TphThingIntermediateOne>());
			}

			var item3 = (TphThingIntermediateOne)(object)result[2]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(6));

				Assert.That(result[3], Is.InstanceOf<TphThingIntermediateTwo>());
			}

			var item4 = (TphThingIntermediateTwo)(object)result[3]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item4.Id, Is.EqualTo(4));
				Assert.That(item4.Type, Is.EqualTo(102));
				Assert.That(item4.BaseField, Is.EqualTo(5));
				Assert.That(item4.IntermediateField, Is.EqualTo(6));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_ViaOfType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingBase>().OfType<TphThingIntermediate>().OrderBy(r => r.Id).ToArray();

			AssertIntermediateThings(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_ViaCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Cast<TphThingIntermediate>().OrderBy(r => r.Id).ToArray();

			AssertIntermediateThings(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_ViaSelectCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (TphThingIntermediate)x).OrderBy(r => r.Id).ToArray();

			AssertIntermediateThings(result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Read_Filtered([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			var result = db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).OrderBy(r => r.Id).ToArray();

			AssertIntermediateThings(result);
		}

		static void TestUpdateAndFind(IQueryable<TphThingIntermediate> table)
		{
			var query = table.Where(x => x.Id == 3);

			query.Set(y => y.IntermediateField, 333).Update();

			var item = query.Single();

			Assert.That(item, Is.InstanceOf<TphThingIntermediateOne>());

			var item3 = (TphThingIntermediateOne)item;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(333));
			}
		}

		static void TestJoinedAll(IDataContext db, IQueryable<TphThingBase> table)
		{
			var result =
				(
					from b in table
					join i in db.GetTable<TphThingInteraction>() on b.Id equals i.ThingId
					join p in db.GetTable<TphThingPerson>() on i.PersonId equals p.Id
					orderby b.Id
					select new
					{
						b.Type,
						p.FullName
					}
				)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Type, Is.EqualTo(1));
				Assert.That(result[0].FullName, Is.EqualTo("Person 4"));

				Assert.That(result[1].Type, Is.EqualTo(2));
				Assert.That(result[1].FullName, Is.EqualTo("Person 1"));

				Assert.That(result[2].Type, Is.EqualTo(101));
				Assert.That(result[2].FullName, Is.EqualTo("Person 2"));

				Assert.That(result[3].Type, Is.EqualTo(102));
				Assert.That(result[3].FullName, Is.EqualTo("Person 3"));
			}
		}

		static void TestJoined<T>(IDataContext db, IQueryable<T> table)
			where T: TphThingBase
		{
			var result =
				(
					from b in table
					join i in db.GetTable<TphThingInteraction>() on b.Id equals i.ThingId
					join p in db.GetTable<TphThingPerson>() on i.PersonId equals p.Id
					orderby b.Id
					select new
					{
						b.Type,
						p.FullName
					}
				)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Type, Is.EqualTo(101));
				Assert.That(result[0].FullName, Is.EqualTo("Person 2"));

				Assert.That(result[1].Type, Is.EqualTo(102));
				Assert.That(result[1].FullName, Is.EqualTo("Person 3"));
			}
		}

		static void TestUpdateAndFindBase(IQueryable<TphThingBase> table)
		{
			var query = table.Where(x => x.Id == 3);

			query.Set(y => ((TphThingIntermediate)y).IntermediateField, 333).Update();

			var item = query.Single();

			Assert.That(item, Is.InstanceOf<TphThingIntermediateOne>());

			var item3 = (TphThingIntermediateOne)item;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(333));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_ViaBaseTable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFindBase(db.GetTable<TphThingBase>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_AllTypes([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoinedAll(db, db.GetTable<TphThingBase>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_ViaIntermediateTable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFind(db.GetTable<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_ViaIntermediateTable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoined(db, db.GetTable<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_ViaOfType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFind(db.GetTable<TphThingBase>().OfType<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_ViaOfType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoined(db, db.GetTable<TphThingBase>().OfType<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_ViaCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFind(db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Cast<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_ViaCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoined(db, db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Cast<TphThingIntermediate>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_Filtered([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFindBase(db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_Filtered([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoined(db, db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Update_ViaSelectCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);

			TestUpdateAndFind(db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (TphThingIntermediate)x));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_Join_ViaSelectCast([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TphThingBase.TestData);
			using var tp = db.CreateLocalTable(TphThingPerson.TestData);
			using var ti = db.CreateLocalTable(TphThingInteraction.TestData);

			TestJoined(db, db.GetTable<TphThingBase>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (TphThingIntermediate)x));
		}

		static void AssertIntermediateThings<T>(T[] result)
		{
			Assert.That(result, Has.Length.EqualTo(2));

			Assert.That(result[0], Is.InstanceOf<TphThingIntermediateOne>());
			var item3 = (TphThingIntermediateOne)(object)result[0]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(6));

				Assert.That(result[1], Is.InstanceOf<TphThingIntermediateTwo>());
			}

			var item4 = (TphThingIntermediateTwo)(object)result[1]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item4.Id, Is.EqualTo(4));
				Assert.That(item4.Type, Is.EqualTo(102));
				Assert.That(item4.BaseField, Is.EqualTo(5));
				Assert.That(item4.IntermediateField, Is.EqualTo(6));
			}
		}

		[Table(IsColumnAttributeRequired = false)]
		[InheritanceMapping(Code = 1, Type = typeof(TphThingAlpha))]
		[InheritanceMapping(Code = 2, Type = typeof(TphThingBeta))]
		[InheritanceMapping(Code = 101, Type = typeof(TphThingIntermediateOne))]
		[InheritanceMapping(Code = 102, Type = typeof(TphThingIntermediateTwo))]
		abstract class TphThingBase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)] public int Type { get; set; }
			public int BaseField { get; set; }

			public static readonly TphThingBase[] TestData = new TphThingBase[]
			{
				new TphThingAlpha()
				{
					Id = 1,
					Type = 1,
					BaseField = 2
				},
				new TphThingBeta()
				{
					Id = 2,
					Type = 2,
					BaseField = 3,
					ConcreteField = 4
				},
				new TphThingIntermediateOne()
				{
					Id = 3,
					Type = 101,
					BaseField = 4,
					ConcreteField = 5,
					IntermediateField = 6
				},
				new TphThingIntermediateTwo()
				{
					Id = 4,
					Type = 102,
					BaseField = 5,
					IntermediateField = 6
				}
			};
		}

		class TphThingAlpha : TphThingBase
		{
		}

		class TphThingBeta : TphThingBase
		{
			// TODO: remove when fixed, nullable added due to TPH_Intermediate_CreateTableNullableFields
			[LinqToDB.Mapping.Nullable] public int ConcreteField { get; set; }
		}

		abstract class TphThingIntermediate : TphThingBase
		{
			// TODO: remove when fixed, nullable added due to TPH_Intermediate_CreateTableNullableFields
			[LinqToDB.Mapping.Nullable] public int IntermediateField { get; set; }
		}

		class TphThingIntermediateOne : TphThingIntermediate
		{
			// TODO: remove when fixed, nullable added due to TPH_Intermediate_CreateTableNullableFields
			[LinqToDB.Mapping.Nullable] public int ConcreteField { get; set; }
		}

		class TphThingIntermediateTwo : TphThingIntermediate
		{
		}

		[Table(IsColumnAttributeRequired = false)]
		class TphThingPerson
		{
			[PrimaryKey] public int Id { get; set; }
			[NotNull] public string FullName { get; set; } = null!;

			public static readonly TphThingPerson[] TestData = new[]
			{
				new TphThingPerson() { Id = 1, FullName = "Person 1" },
				new TphThingPerson() { Id = 2, FullName = "Person 2" },
				new TphThingPerson() { Id = 3, FullName = "Person 3" },
				new TphThingPerson() { Id = 4, FullName = "Person 4" },
				new TphThingPerson() { Id = 5, FullName = "Person 5" },
			};
		}

		[Table(IsColumnAttributeRequired = false)]
		class TphThingInteraction
		{
			[PrimaryKey] public int Id { get; set; }
			public int PersonId { get; set; }
			public int ThingId { get; set; }

			public static readonly TphThingInteraction[] TestData = new[]
			{
				new TphThingInteraction() { Id = 1, PersonId = 2, ThingId = 3 },
				new TphThingInteraction() { Id = 2, PersonId = 3, ThingId = 4 },
				new TphThingInteraction() { Id = 3, PersonId = 4, ThingId = 1 },
				new TphThingInteraction() { Id = 4, PersonId = 1, ThingId = 2 },
			};
		}
		#endregion

		#region TPH intermediate type (insert / DDL)
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_InsertConcreteTypes([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CreateTableBase>();

			db.Insert(new CreateTable1() { Id = 1, Type = 1, Field1 = 1 });
			db.Insert(new CreateTable2() { Id = 2, Type = 2, Field2 = 2 });

			var result = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.That(result[0], Is.InstanceOf<CreateTable1>());
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Type, Is.EqualTo(1));
				Assert.That(((CreateTable1)result[0]).Field1, Is.EqualTo(1));

				Assert.That(result[1], Is.InstanceOf<CreateTable2>());
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Type, Is.EqualTo(2));
				Assert.That(((CreateTable2)result[1]).Field2, Is.EqualTo(2));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void TPH_Intermediate_CreateTableNullableFields([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CreateTableBase>();

			tb.Insert(() => new CreateTable1() { Id = 1, Type = 1, Field1 = 1});
			tb.Insert(() => new CreateTable2() { Id = 2, Type = 2, Field2 = 2});

			// TODO: those asserts could be incorrect, depends on our fixed model
			var column = db.MappingSchema.GetEntityDescriptor(typeof(CreateTable1)).Columns.Single(c => c.ColumnName == "Field1");
			Assert.That(column.CanBeNull, Is.False);
			column = db.MappingSchema.GetEntityDescriptor(typeof(CreateTable1)).Columns.Single(c => c.ColumnName == "Field2");
			Assert.That(column.CanBeNull, Is.False);
		}

		[Table]
		[InheritanceMapping(Code = 1, Type = typeof(CreateTable1))]
		[InheritanceMapping(Code = 2, Type = typeof(CreateTable2))]
		abstract class CreateTableBase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)]  public int Type { get; set; }
		}

		[Table]
		sealed class CreateTable1 : CreateTableBase
		{
			[Column] public int Field1 { get; set; }
		}

		[Table]
		sealed class CreateTable2 : CreateTableBase
		{
			[Column] public int Field2 { get; set; }
		}
		#endregion
	}
}
