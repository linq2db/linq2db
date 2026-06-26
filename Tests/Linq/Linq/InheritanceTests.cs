using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class InheritanceTests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(ParentInheritance, db.ParentInheritance);
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(ParentInheritance, db.ParentInheritance.Select(p => p));
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in ParentInheritance where p is ParentInheritance1 select p,
				from p in db.ParentInheritance where p is ParentInheritance1 select p);
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in ParentInheritance where !(p is ParentInheritanceNull) select p,
				from p in db.ParentInheritance where !(p is ParentInheritanceNull) select p);
		}

		[Test]
		public void Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in ParentInheritance where p is ParentInheritanceValue select p,
				from p in db.ParentInheritance where p is ParentInheritanceValue select p);
		}

		[Test]
		public void Test6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = from p in db.ParentInheritance2 where p is ParentInheritance12 select p;
			var _ = q.ToList();
		}

		[Test]
		public void Test7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in ParentInheritance where p is ParentInheritanceBase select p,
				from p in db.ParentInheritance where p is ParentInheritanceBase select p);
		}

		[Test]
		public void Test8([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   ParentInheritance.OfType<ParentInheritance1>(),
				db.ParentInheritance.OfType<ParentInheritance1>());
		}

		[Test]
		public void Test9([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   ParentInheritance
					.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
					.OfType<ParentInheritanceNull>(),
				db.ParentInheritance
					.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
					.OfType<ParentInheritanceNull>());
		}

		[Test]
		public void Test10([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   ParentInheritance.OfType<ParentInheritanceValue>(),
				db.ParentInheritance.OfType<ParentInheritanceValue>());
		}

		[Test]
		public void Test11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = from p in db.ParentInheritance3 where p is ParentInheritance13 select p;
			var _ = q.ToList();
		}

		[Test]
		public void Test12([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in ParentInheritance1 where p.ParentID == 1 select p,
				from p in db.ParentInheritance1 where p.ParentID == 1 select p);
		}

		//[Test]
		//public void Test13([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			from p in    ParentInheritance4
		//			join c in    Child on p.ParentID equals c.ParentID
		//			select p,
		//			from p in db.ParentInheritance4
		//			join c in db.Child on p.ParentID equals c.ParentID
		//			select p);
		//}

		[Test]
		public void TestGetBaseClass([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = db.GetTable<ParentInheritanceBase3>()
					.Where(x => x is ParentInheritance13)
					.ToList();
			Assert.That(q, Has.Count.EqualTo(2));
		}

		[Test]
		public void TypeCastAsTest1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				dd.DiscontinuedProduct.ToList()
					.Select(p => p as Northwind.Product)
					.Select(p => p == null ? "NULL" : p.ProductName),
				db.DiscontinuedProduct
					.Select(p => p as Northwind.Product)
					.Select(p => p == null ? "NULL" : p.ProductName));
		}

		[Test]
		public void TypeCastAsTest11([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				dd.DiscontinuedProduct.ToList()
					.Select(p => new { p = p as Northwind.Product })
					.Select(p => p.p == null ? "NULL" : p.p.ProductName),
				db.DiscontinuedProduct
					.Select(p => new { p = p as Northwind.Product })
					.Select(p => p.p == null ? "NULL" : p.p.ProductName));
		}

		[Test]
		public void TypeCastAsTest2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				dd.Product.ToList()
					.Select(p => p as Northwind.DiscontinuedProduct)
					.Select(p => p == null ? "NULL" : p.ProductName),
				db.Product
					.Select(p => p as Northwind.DiscontinuedProduct)
					.Select(p => p == null ? "NULL" : p.ProductName));
		}

		[Test]
		public void FirstOrDefault([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			Assert.That(
				db.DiscontinuedProduct.FirstOrDefault()!.ProductID, Is.EqualTo(dd.DiscontinuedProduct.FirstOrDefault()!.ProductID));
		}

		[Test]
		public void Cast1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
				db.ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>());
		}

		[Test]
		public async Task Cast1Async([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
					  ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
				await db.ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>().ToListAsync());
		}

		sealed class ParentEx : Parent
		{
			[NotColumn]
			public bool Field1;

			public static void Test(InheritanceTests inheritance, string context)
			{
				using var db = inheritance.GetDataContext(context);
				inheritance.AreEqual(
					inheritance.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>(),
							 db.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>());
			}
		}

		[Test]
		public void Cast2([DataSources] string context)
		{
			ParentEx.Test(this, context);
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		sealed class PersonEx : Person
		{
		}

		[Test]
		public void SimplTest()
		{
			using var db = new DataConnection();
			Assert.That(db.GetTable<PersonEx>().Where(_ => _.FirstName == "John").Select(_ => _.ID).Single(), Is.EqualTo(1));
		}

		[InheritanceMapping(Code = 1, Type = typeof(Parent222))]
		[Table("Parent")]
		public class Parent111
		{
			[Column(IsDiscriminator = true)]
			public int ParentID;
		}

		[Column("Value1", "Value.ID")]
		public class Parent222 : Parent111
		{
			public Value111 Value = null!;
		}

		public class Value111
		{
			public int ID;
		}

		[Test]
		public void InheritanceMappingIssueTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = db.GetTable<Parent222>();
			var q  = q1.Where(_ => _.Value.ID == 1);

			q.ToArray();
		}

		[Table(Name = "Child", IsColumnAttributeRequired = false)]
		[InheritanceMapping(Code = 1, IsDefault = true, Type = typeof(MyChildBase))]
		[InheritanceMapping(Code = 11, Type = typeof(MyChild11))]
		[InheritanceMapping(Code = 21, Type = typeof(MyChild21))]
		public class MyChildBase
		{
			[Column(IsDiscriminator = true)]
			public int ChildID { get; set; }
		}

		public class MyChildBase_11_21 : MyChildBase { }
		public class MyChild11 : MyChildBase_11_21 { }
		public class MyChild21 : MyChildBase_11_21 { }

		[Test]
		public void InheritanceMappingIssue106Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var childIDs = db.GetTable<MyChildBase_11_21>().AsEnumerable()
					.Select(ch => ch.ChildID)
					.OrderBy(x => x)
					.ToList();

			Assert.That(childIDs.SequenceEqual(new[] { 11, 21 }), Is.True, $"{childIDs.Count}: {childIDs[0]}, {childIDs[1]}");
		}

		[Test]
		public void ReferenceNavigation([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var result =
					from od in db.OrderDetail
					where od.Product.Category!.CategoryName == "Seafood"
					select new { od.Order, od.Product };

			var list = result.ToList();

			Assert.That(list, Has.Count.EqualTo(330));

			foreach (var item in list)
			{
				Assert.That(item, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(item.Order, Is.Not.Null);
					Assert.That(item.Product, Is.Not.Null);
				}

				Assert.That(
					 (item.Product.Discontinued && item.Product is Northwind.DiscontinuedProduct) ||
					(!item.Product.Discontinued && item.Product is Northwind.ActiveProduct), Is.True);
			}
		}

		[Test]
		public void TypeCastIsChildConditional1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();
			var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.Not.Empty);
				Assert.That(expected, Has.Count.EqualTo(result.Count));
			}

			Assert.That(result, Does.Contain(null));
			Assert.That(result.Select(x => x == null ? (int?)null : x.ProductID).Except(expected.Select(x => x == null ? (int?)null : x.ProductID)).Count(), Is.Zero);
		}

		[Test]
		public void TypeCastIsChildConditional2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct);
			var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct);

			var list = result.ToList();

			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
			Assert.That(list.Except(expected).Count(), Is.Zero);
		}

		[Test]
		public void TypeCastIsChild([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);

			var result   = db.Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();
			var expected = dd.Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.Not.Empty);
				Assert.That(expected, Has.Count.EqualTo(result.Count));
			}
		}

		#region Models for Test14

		interface IChildTest14
		{
			int ChildID { get; set; }
		}

		[Table(Name="Child")]
		sealed class ChildTest14 : IChildTest14
		{
			[PrimaryKey] public int ChildID { get; set; }

		}

		T? FindById<T>(IQueryable<T> queryable, int id)
			where T : class, IChildTest14
		{
			return queryable.Where(x => x.ChildID == id).FirstOrDefault();
		}

		#endregion

		[Test]
		public void Test14([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = db.GetTable<ChildTest14>().Select(c => new ChildTest14 { ChildID = c.ChildID });
			FindById(q, 10);
		}

		[Test]
		public void Test15([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);

			var result   = db.DiscontinuedProduct.Select(p => p).ToList();
			var expected = dd.DiscontinuedProduct.Select(p => p).ToList();

			Assert.That(result, Has.Count.EqualTo(expected.Count));
		}

		[Test]
		public void Test16([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);

			var result   = db.DiscontinuedProduct.ToList();
			var expected = dd.DiscontinuedProduct.ToList();

			Assert.That(result, Has.Count.EqualTo(expected.Count));
		}

		public enum TypeCodeEnum
		{
			Base,
			A,
			A1,
			A2,
		}

		[Table(Name="LinqDataTypes")]
		public abstract class InheritanceBase
		{
			[Column] public Guid GuidValue { get; set; }

			[Column("ID")]
			public virtual TypeCodeEnum TypeCode => TypeCodeEnum.Base;
		}

		[InheritanceMapping(Code = TypeCodeEnum.A1, Type = typeof(InheritanceA1), IsDefault = false)]
		[InheritanceMapping(Code = TypeCodeEnum.A2, Type = typeof(InheritanceA2), IsDefault = true)]
		public abstract class InheritanceA : InheritanceBase
		{
			[Association(CanBeNull = true, ThisKey = "GuidValue", OtherKey = "GuidValue")]
			public List<InheritanceB> Bs { get; set; } = null!;

			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode => TypeCodeEnum.A;
		}

		public class InheritanceA1 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode => TypeCodeEnum.A1;
		}

		public class InheritanceA2 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode => TypeCodeEnum.A2;
		}

		public class InheritanceB : InheritanceBase
		{
		}

		[Table(Name="LinqDataTypes")]
		public class InheritanceAssociation
		{
			[Column] public Guid GuidValue { get; set; }

			[Association(CanBeNull = true, ThisKey = "GuidValue", OtherKey = "GuidValue")]
			public InheritanceA1? A1 { get; set; }

			[Association(CanBeNull = true, ThisKey = "GuidValue", OtherKey = "GuidValue")]
			public InheritanceA2? A2 { get; set; }
		}

		[Test]
		public void GuidTest()
		{
			using var db = new DataConnection();
			var list = db.GetTable<InheritanceA>().Where(a => a.Bs.Any()).ToList();
		}

		[Test]
		public void QuerySyntaxSimpleTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			// db.GetTable<Parent111>().OfType<Parent222>().ToList(); - it's work!!!
			(from p in db.GetTable<Parent111>().OfType<Parent222>() select p).ToList();
		}

		[Table("Person")]
		[InheritanceMapping(Code = 1, Type = typeof(Test17John))]
		[InheritanceMapping(Code = 2, Type = typeof(Test17Tester))]
		public class Test17Person
		{
			[Column(IsDiscriminator = true)]
			public int PersonID { get; set; }
		}

		public class Test17John : Test17Person
		{
			public string FirstName { get; set; } = null!;
		}

		public class Test17Tester : Test17Person
		{
			public string LastName { get; set; } = null!;
		}

		[Test]
		public void Test17([DataSources(false)] string data)
		{
			using var context = GetDataContext(data);
			var db = (TestDataConnection)context;
			db.GetTable<Test17Person>().OfType<Test17John>().ToList();
			Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("lastname"), "Why select LastName field??");
		}

		[Table(Name="Person")]
		[InheritanceMapping(Code = Gender.Male,   Type = typeof(Test18Male))]
		[InheritanceMapping(Code = Gender.Female, Type = typeof(Test18Female))]
		public class Test18Person
		{
			[PrimaryKey, Identity, SequenceName("PERSONID")] public int    PersonID { get; set; }
			[Column(IsDiscriminator = true)]                 public Gender Gender   { get; set; }
		}

		public class Test18Male : Test18Person
		{
			[Column] public string FirstName { get; set; } = null!;
		}

		public class Test18Female : Test18Person
		{
			[Column] public string FirstName { get; set; } = null!;
			[Column] public string LastName  { get; set; } = null!;
		}

		[Test]
		public void Test18([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var ids = Enumerable.Range(0, 10).ToList();
			var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

			var list = q.Distinct().OfType<Test18Female>().ToList();
		}

		[Test]
		public async Task Test18Async([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var ids = Enumerable.Range(0, 10).ToList();
			var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

			var list = await q.Distinct().OfType<Test18Female>().ToListAsync();
		}

		[Test]
		public void Test19([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var ids = Enumerable.Range(0, 10).ToList();
			var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

			IQueryable iq   = q.Distinct();
			var        list = iq.OfType<Test18Female>().ToList();
		}

		[Test]
		public void InheritanceAssociationTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<InheritanceAssociation>().Select(ia =>
					new
					{
						TC1 = ia.A1!.TypeCode,
						TC2 = ia.A2!.TypeCode
					});

			var items = db.GetTable<LinqDataTypes>().ToList();
			var expected = items.Select(ia =>
					new
					{
						TC1 = items.Where(i => i.ID == ia.ID).Select(i => (TypeCodeEnum)i.ID).FirstOrDefault(i => i == TypeCodeEnum.A1),
						TC2 = items.Where(i => i.ID == ia.ID).Select(i => (TypeCodeEnum)i.ID).FirstOrDefault(i => i != TypeCodeEnum.A1)
					});

			AreEqual(expected, result);
		}

		#region issue 2429

		public abstract class Root
		{
			public abstract int Value { get; set; }
			public abstract int GetValue();
		}

		[Table]
		public class BaseTable : Root
		{
			[PrimaryKey, NotNull  ] public int Id { get; set; }
			[Column(nameof(Value))] public int BaseValue { get; set; }

			private static Expression<Func<BaseTable, int>> GeValueImpl() => e => e.BaseValue;

			[ExpressionMethod(nameof(GeValueImpl), IsColumn = true)]
			public override int Value { get => BaseValue; set => BaseValue = value; }

			[ExpressionMethod(nameof(GeValueImpl))]
			public override int GetValue() => BaseValue;

			public static readonly BaseTable[] Data = new []
			{
				new BaseTable() { Id = 1, Value = 100 }
			};
		}

		[Table]
		public class BaseTable2
		{
			[PrimaryKey, NotNull] public         int Id { get; set; }
			[Column             ] public virtual int Value { get; set; }

			[ExpressionMethod(nameof(GetBaseTableOverrideImpl), IsColumn = true)]
			public virtual int GetValue() => Value;

			private static Expression<Func<BaseTable2, int>> GetBaseTableOverrideImpl() => e => e.Value;

			public static readonly BaseTable2[] Data = new []
			{
				new BaseTable2() { Id = 1, Value = 100 }
			};
		}

		public class DerivedTable2 : BaseTable2
		{
			private static Expression<Func<DerivedTable2, int>> GetDerivedTableOverrideImpl() => e => e.BaseValue * -1;

			[Column(nameof(Value))] public int BaseValue { get; set; }

			[ExpressionMethod(nameof(GetDerivedTableOverrideImpl), IsColumn = true)]
			public override int Value { get; set; }

			[ExpressionMethod(nameof(GetDerivedTableOverrideImpl))]
			public override int GetValue() => BaseValue * -1;
		}

		[Test]
		public void Issue2429PropertiesTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(BaseTable.Data))
			{
					var baseTableRecordById = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecordById?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordById?.Value, Is.EqualTo(100));
				}

				var baseTableRecordWithValuePredicate = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1 && x.Value == 100);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecordWithValuePredicate?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordWithValuePredicate?.Value, Is.EqualTo(100));
				}
			}
		}

		[Test]
		public void Issue2429MethodsTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(BaseTable.Data))
			{
				var baseTableRecordById = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecordById?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordById?.Value, Is.EqualTo(100));
				}

				var baseTableRecordWithValuePredicate = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1 && x.GetValue() == 100);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecordWithValuePredicate?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordWithValuePredicate?.Value, Is.EqualTo(100));
				}
			}
		}

		[ActiveIssue(Details = "Expression 'x.BaseValue' is not a Field. (Invalid mappings?)")]
		[Test]
		public void Issue2429PropertiesTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(BaseTable2.Data))
			{
				var baseTableRecord    = db.GetTable<BaseTable2>().FirstOrDefault(x => x.Id == 1 && x.Value == 100);
				//var derivedTableRecord = db.GetTable<DerivedTable2>().FirstOrDefault(x => x.Id == 1 && x.Value == (100 * -1 ));
				var derivedTableRecord = db.GetTable<BaseTable2>().OfType<DerivedTable2>().FirstOrDefault(x => x.Id == 1 && x.Value == (100 * -1 ));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecord?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecord?.Value, Is.EqualTo(100));

					Assert.That(derivedTableRecord?.Id, Is.EqualTo(1));
					Assert.That(derivedTableRecord?.Value * -1, Is.EqualTo(100));
				}
			}
		}

		[ActiveIssue(Details = "Expression 'x.BaseValue' is not a Field. (Invalid mappings?)")]
		[Test]
		public void Issue2429MethodsTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(BaseTable2.Data))
			{
				var baseTableRecord    = db.GetTable<BaseTable2>().FirstOrDefault(x => x.Id == 1 && x.GetValue() == 100);
				//var derivedTableRecord = db.GetTable<DerivedTable2>().FirstOrDefault(x => x.Id == 1 && x.GetValue() == (100 * -1 ));
				var derivedTableRecord = db.GetTable<BaseTable2>().OfType<DerivedTable2>().FirstOrDefault(x => x.Id == 1 && x.GetValue() == (100 * -1 ));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(baseTableRecord?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecord?.Value, Is.EqualTo(100));

					Assert.That(derivedTableRecord?.Id, Is.EqualTo(1));
					Assert.That(derivedTableRecord?.Value * -1, Is.EqualTo(100));
				}
			}
		}
		#endregion

		#region issue4280

		[InheritanceMapping(Code = "DISPLAY", Type = typeof(Issue4280T1))]
		[InheritanceMapping(Code = "TV", Type = typeof(Issue4280T2))]
		[Table("Issue4280")]
		public abstract class Issue4280Base
		{
			[PrimaryKey                    ] public          int     Id           { get; set; }
			[Column                        ] public          string? SerialNumber { get; set; }
			[Column(IsDiscriminator = true)] public abstract string  DeviceType   { get; set; }
		}

		public class Issue4280T1 : Issue4280Base
		{
			public override string DeviceType { get; set; } = "DISPLAY";
		}

		public class Issue4280T2 : Issue4280Base
		{
			public override string DeviceType { get; set; } = "TV";

			[Column]
			public string? Location { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4280")]
		public void TestIssue4280AsBase([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4280Base>();

			var displayDevice = new Issue4280T1 { Id = 1, SerialNumber = "Disp00001" };
			var tvDevice      = new Issue4280T2 { Id = 2, SerialNumber = "TV00001", Location = "Something" };

			db.Insert<Issue4280Base>(tvDevice);
			db.Insert<Issue4280Base>(displayDevice);

			var data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Something"));
			}

			displayDevice.SerialNumber = "Disp00002";
			tvDevice.SerialNumber      = "TV00002";
			tvDevice.Location          = "Anything";

			db.Update<Issue4280Base>(tvDevice);
			db.Update<Issue4280Base>(displayDevice);

			data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Anything"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4280")]
		public void TestIssue4280AsIs([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4280Base>();

			var displayDevice = new Issue4280T1 { Id = 1, SerialNumber = "Disp00001" };
			var tvDevice      = new Issue4280T2 { Id = 2, SerialNumber = "TV00001", Location = "Something" };

			db.Insert(tvDevice);
			db.Insert(displayDevice);

			var data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Something"));
			}

			displayDevice.SerialNumber = "Disp00002";
			tvDevice.SerialNumber      = "TV00002";
			tvDevice.Location          = "Anything";

			db.Update(tvDevice);
			db.Update(displayDevice);

			data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Anything"));
			}
		}
		#endregion

		#region issue 3891
		public class Name
		{
			public string? First { get; set; }
			public string? Second { get; set; }
		}

		[Table("Base")]
		[InheritanceMapping(Code = 1, Type = typeof(ChildBase))]
		[Column(MemberName = "Name.First", Name = "Name_First")]
		[Column(MemberName = "Name.Second", Name = "Name_Second")]
		public abstract class Base
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			public Name? Name { get; set; }

			[Column(IsDiscriminator = true)]
			public int Type { get; set; }
		}

		[Table("Base")]
		public class ChildBase : Base
		{
			public ChildBase()
			{
				Type = 1;
			}

			[Column(Name = "Test_ChildId")]
			public int ChildId { get; set; }
		}

		public abstract class Base2
		{
			public int Id { get; set; }

			public Name? Name { get; set; }

			public int Type { get; set; }
		}

		public class ChildBase2 : Base2
		{
			public ChildBase2()
			{
				Type = 1;
			}

			public int ChildId { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3891")]
		public void Issue3891AttributesMapping([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Base>();

			var child = new ChildBase()
			{
				Id = 1,
				ChildId = 2,
				Name = new Name()
				{
					First = "First",
					Second = "Second"
				}
			};

			db.Insert(child);

			var res = tb.Single();
			Assert.That(res, Is.InstanceOf<ChildBase>());
			var cb = (ChildBase)res;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(cb.Id, Is.EqualTo(1));
				Assert.That(cb.ChildId, Is.EqualTo(2));
				Assert.That(cb.Name, Is.Not.Null);
				Assert.That(cb.Name!.First, Is.EqualTo("First"));
				Assert.That(cb.Name.Second, Is.EqualTo("Second"));
			}

			child.Name.First = "First1";
			db.Update(child);

			res = tb.Single();
			Assert.That(res, Is.InstanceOf<ChildBase>());
			cb = (ChildBase)res;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(cb.Id, Is.EqualTo(1));
				Assert.That(cb.ChildId, Is.EqualTo(2));
				Assert.That(cb.Name, Is.Not.Null);
				Assert.That(cb.Name!.First, Is.EqualTo("First1"));
				Assert.That(cb.Name.Second, Is.EqualTo("Second"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3891")]
		public void Issue3891FluentMapping([InsertOrUpdateDataSources] string context)
		{
			var ms = new FluentMappingBuilder()
				.Entity<Base2>()
					.HasTableName("Base2")
					.Inheritance(x => x.Type, 1, typeof(ChildBase2))
					.Property(x => x.Id).IsPrimaryKey()
					.Property(x => x.Name!.First).HasColumnName("Name_First")
					.Property(x => x.Name!.Second).HasColumnName("Name_Second")
				.Entity<ChildBase2>()
					.HasTableName("Base2")
					.Property(x => x.ChildId).HasColumnName("Test_ChildId")
				.Build()
				.MappingSchema;

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Base2>();

			var child = new ChildBase2()
			{
				Id = 1,
				ChildId = 2,
				Name = new Name()
				{
					First = "First",
					Second = "Second"
				}
			};

			db.Insert(child);

			var res = tb.Single();
			Assert.That(res, Is.InstanceOf<ChildBase2>());
			var cb = (ChildBase2)res;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(cb.Id, Is.EqualTo(1));
				Assert.That(cb.ChildId, Is.EqualTo(2));
				Assert.That(cb.Name, Is.Not.Null);
				Assert.That(cb.Name!.First, Is.EqualTo("First"));
				Assert.That(cb.Name.Second, Is.EqualTo("Second"));
			}

			child.Name.First = "First1";
			db.Update(child);

			res = tb.Single();
			Assert.That(res, Is.InstanceOf<ChildBase2>());
			cb = (ChildBase2)res;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(cb.Id, Is.EqualTo(1));
				Assert.That(cb.ChildId, Is.EqualTo(2));
				Assert.That(cb.Name, Is.Not.Null);
				Assert.That(cb.Name!.First, Is.EqualTo("First1"));
				Assert.That(cb.Name.Second, Is.EqualTo("Second"));
			}
		}
		#endregion

		#region Issue 4666
		public enum Issue4666EntityType { None, Type1, Type2 }

		[InheritanceMapping(Code = Issue4666EntityType.None, Type = typeof(Issue4666BaseEntity))]
		[InheritanceMapping(Code = Issue4666EntityType.Type1, Type = typeof(Issue4666Type1Entity))]
		[InheritanceMapping(Code = Issue4666EntityType.Type2, Type = typeof(Issue4666Type2Entity))]
		[Table]
		public class Issue4666BaseEntity
		{
			[Column] public int Id { get; set; }
			[Column] public string? Description { get; set; }
			[Column(IsDiscriminator = true)] public Issue4666EntityType Type { get; set; }
		}

		public class Issue4666Type1Entity : Issue4666BaseEntity
		{
			[Column] public string? Type1EntityProp { get; set; }

			public static Issue4666Type1Entity[] Data =
			[
				new Issue4666Type1Entity() { Id = 1, Description = "Test1", Type1EntityProp = "Prop1" },
				new Issue4666Type1Entity() { Id = 2, Description = "Test2", Type1EntityProp = "Prop2" }
			];
		}

		public class Issue4666Type2Entity : Issue4666BaseEntity
		{
			[Column] public string? Type2EntityProp { get; set; }
		}

		// also exists in efcore tests
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4666")]
		public void Issue4666Test([MergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var destination = db.CreateLocalTable<Issue4666Type1Entity>();
			using var source = db.CreateLocalTable("Issue4666Temp", Issue4666Type1Entity.Data);

			destination
				.Merge()
				.Using(source)
				.On((target, source) => target.Id == source.Id)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.DeleteWhenNotMatchedBySourceAnd(i => i.Type == Issue4666EntityType.Type1)
				.Merge();

		}
		#endregion

		#region Discriminator Filtering
		[Table("InheritanceFilter")]
		[InheritanceMapping(Code = 1, Type = typeof(Child1))]
		[InheritanceMapping(Code = 2, Type = typeof(Child2))]
		[InheritanceMapping(Code = 11, Type = typeof(Grandchild11))]
		[InheritanceMapping(Code = 12, Type = typeof(Grandchild12))]
		[InheritanceMapping(Code = 21, Type = typeof(Grandchild21))]
		[InheritanceMapping(Code = 22, Type = typeof(Grandchild22))]
		abstract class BaseClass
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(IsDiscriminator = true)] public int Code { get; set; }

			public static BaseClass[] Data =
			[
				new Child1() { Id = 1, Child1Field = 11 },
				new Child2() { Id = 2, Child2Field = 12 },
				new Grandchild11() { Id = 3, Grandchild11Field = 13, Child1Field = 23 },
				new Grandchild12() { Id = 4, Grandchild12Field = 14, Child1Field = 24 },
				new Grandchild21() { Id = 5, Grandchild21Field = 15, Child2Field = 25 },
				new Grandchild22() { Id = 6, Grandchild22Field = 16, Child2Field = 26 },
			];
		}

		// TODO: for now we mark optional columns nullable for create table
		// as db.Insert doesn't take nullability of such columns into account
		// First we need to decide how we want to address it:
		// - use default value as ColumnDescriptor. GetProviderValue
		// - or: force column nullability for CREATE TABLE
		class Child1 : BaseClass
		{
			[Column(CanBeNull = true)] public int Child1Field { get; set; }
		}

		class Child2 : BaseClass
		{
			[Column(CanBeNull = true)] public int Child2Field { get; set; }
		}

		class Grandchild11 : Child1
		{
			[Column(CanBeNull = true)] public int Grandchild11Field { get; set; }
		}

		class Grandchild12 : Child1
		{
			[Column(CanBeNull = true)] public int Grandchild12Field { get; set; }
		}

		class Grandchild21 : Child2
		{
			[Column(CanBeNull = true)] public int Grandchild21Field { get; set; }
		}

		class Grandchild22 : Child2
		{
			[Column(CanBeNull = true)] public int Grandchild22Field { get; set; }
		}

		[Table("InheritanceFilter")]
		[InheritanceMapping(Code = 1, Type = typeof(SubChild1))]
		[InheritanceMapping(Code = 11, Type = typeof(SubGrandchild11))]
		[InheritanceMapping(Code = 12, Type = typeof(SubGrandchild12))]
		abstract class SubBaseClass
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(IsDiscriminator = true)] public int Code { get; set; }
		}

		class SubChild1 : SubBaseClass
		{
			[Column] public int Child1Field { get; set; }
		}

		class SubGrandchild11 : SubChild1
		{
			[Column] public int Grandchild11Field { get; set; }
		}

		class SubGrandchild12 : SubChild1
		{
			[Column] public int Grandchild12Field { get; set; }
		}

		[Table("InheritanceFilter")]
		[InheritanceMapping(Code = 1, Type = typeof(SubWithDefaultChild1), IsDefault = true)]
		[InheritanceMapping(Code = 11, Type = typeof(SubWithDefaultGrandchild11))]
		[InheritanceMapping(Code = 12, Type = typeof(SubWithDefaultGrandchild12))]
		abstract class SubWithDefaultBaseClass
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(IsDiscriminator = true)] public int Code { get; set; }
		}

		class SubWithDefaultChild1 : SubWithDefaultBaseClass
		{
			[Column] public int Child1Field { get; set; }
		}

		class SubWithDefaultGrandchild11 : SubWithDefaultChild1
		{
			[Column] public int Grandchild11Field { get; set; }
		}

		class SubWithDefaultGrandchild12 : SubWithDefaultChild1
		{
			[Column] public int Grandchild12Field { get; set; }
		}

		[Test]
		public void TestInheritanceInsert([DataSources(false)] string context, [Values] BulkCopyType bcType)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable<BaseClass>();

			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bcType }, BaseClass.Data);

			var result = db.GetTable<BaseClass>().ToArray();

			Assert.That(result, Has.Length.EqualTo(6));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.OfType<Child1>().Count(), Is.EqualTo(3));
				Assert.That(result.OfType<Grandchild11>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Grandchild12>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Child2>().Count(), Is.EqualTo(3));
				Assert.That(result.OfType<Grandchild21>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Grandchild22>().Count(), Is.EqualTo(1));
			}

			var gc11 = result.OfType<Grandchild11>().Single();
			var gc12 = result.OfType<Grandchild12>().Single();
			var gc21 = result.OfType<Grandchild21>().Single();
			var gc22 = result.OfType<Grandchild22>().Single();
			var c1 = (Child1)result.Where(r => r.GetType() == typeof(Child1)).Single();
			var c2 = (Child2)result.Where(r => r.GetType() == typeof(Child2)).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c1.Child1Field, Is.EqualTo(11));
				Assert.That(c2.Child2Field, Is.EqualTo(12));
				Assert.That(gc11.Grandchild11Field, Is.EqualTo(13));
				Assert.That(gc11.Child1Field, Is.EqualTo(23));
				Assert.That(gc12.Grandchild12Field, Is.EqualTo(14));
				Assert.That(gc12.Child1Field, Is.EqualTo(24));
				Assert.That(gc21.Grandchild21Field, Is.EqualTo(15));
				Assert.That(gc21.Child2Field, Is.EqualTo(25));
				Assert.That(gc22.Grandchild22Field, Is.EqualTo(16));
				Assert.That(gc22.Child2Field, Is.EqualTo(26));
			}
		}

		[Test]
		public void TestFullTreeSelectionWithoutDefaultDiscriminator([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(BaseClass.Data);

			var result = db.GetTable<BaseClass>().ToArray();

			Assert.That(result, Has.Length.EqualTo(6));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.OfType<Child1>().Count(), Is.EqualTo(3));
				Assert.That(result.OfType<Grandchild11>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Grandchild12>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Child2>().Count(), Is.EqualTo(3));
				Assert.That(result.OfType<Grandchild21>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<Grandchild22>().Count(), Is.EqualTo(1));
			}
		}

		[ActiveIssue("Partial mapping is not supported for now")]
		[Test]
		public void TestSubTreeSelectionWithoutDefaultDiscriminator([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(BaseClass.Data);

			var result = db.GetTable<SubBaseClass>().ToArray();

			Assert.That(result, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.OfType<SubChild1>().Count(), Is.EqualTo(3));
				Assert.That(result.OfType<SubGrandchild11>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<SubGrandchild12>().Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void TestSubTreeSelectionWithDefaultDiscriminator([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(BaseClass.Data);

			var result = db.GetTable<SubWithDefaultBaseClass>().ToArray();

			Assert.That(result, Has.Length.EqualTo(6));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.OfType<SubWithDefaultChild1>().Count(), Is.EqualTo(6));
				Assert.That(result.OfType<SubWithDefaultGrandchild11>().Count(), Is.EqualTo(1));
				Assert.That(result.OfType<SubWithDefaultGrandchild12>().Count(), Is.EqualTo(1));
			}
		}

		[ActiveIssue]
		[Test]
		public void TestInsertIssue1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable<BaseClass>();

			db.GetTable<BaseClass>().Value(_ => _.Id, 1).Insert();
			db.GetTable<BaseClass>().Value(_ => _.Id, 2).Insert();
			db.GetTable<BaseClass>().Value(_ => _.Id, 3).Insert();

			var result = db.GetTable<BaseClass>().OrderBy(_ => _.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[2].Id, Is.EqualTo(3));
			}
		}

		[ActiveIssue]
		[Test]
		public void TestInsertIssue2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable<BaseClass>();

			db.GetTable<Child1>().Value(_ => _.Id, 1).Value(_ => _.Child1Field, 1).Insert();
			db.GetTable<Child1>().Value(_ => _.Id, 2).Value(_ => _.Child1Field, 1).Insert();
			db.GetTable<Child1>().Value(_ => _.Id, 3).Value(_ => _.Child1Field, 1).Insert();

			var result = db.GetTable<BaseClass>().OrderBy(_ => _.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[2].Id, Is.EqualTo(3));
			}
		}

		#endregion
	}
}
