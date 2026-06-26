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

		#region TPH (single-table inheritance)

		[Test]
		public void TPH_DeepHierarchy_FindRecord([DataSources] string context)
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
		public void TPH_DeepHierarchy_PolymorphicResult([DataSources] string context)
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
		public void TPH_DeepHierarchy_BulkCopy([DataSources(false)] string context, [Values] BulkCopyType copyType)
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
		public void TPH_PropertiesWithSameNameMapped([DataSources] string context)
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
		public void TPH_SiblingColumnNullPredicate([DataSources] string context)
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
		public void TPH_SiblingColumn_Projection([DataSources] string context)
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
		public void TPH_SiblingColumn_ValueFilter([DataSources] string context)
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
		public void TPH_SiblingColumn_Update([DataSources] string context)
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
		public void TPH_SiblingColumn_ThreeSiblings([DataSources] string context)
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
		public void TPH_SiblingColumn_SharedPhysicalColumn([DataSources] string context)
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
		public void TPH_CodeFilter([DataSources] string context)
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
		public void TPH_InterfaceFilter([DataSources] string context)
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
		public void TPH_ScalarProjection([DataSources] string context)
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
		public void TPH_ObjectProjection([DataSources] string context)
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
		#endregion

		#region Issue 4364
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_IntermediateThing>().OrderBy(r => r.Id).ToArray();

			AssertIssue4364(result);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_BaseThing>().OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(4));

			Assert.That(result[0], Is.InstanceOf<Issue4364_ConcreteBaseThingAlpha>());
			var item1 = (Issue4364_ConcreteBaseThingAlpha)(object)result[0]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item1.Id, Is.EqualTo(1));
				Assert.That(item1.Type, Is.EqualTo(1));
				Assert.That(item1.BaseField, Is.EqualTo(2));

				Assert.That(result[1], Is.InstanceOf<Issue4364_ConcreteBaseThingBeta>());
			}

			var item2 = (Issue4364_ConcreteBaseThingBeta)(object)result[1]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item2.Id, Is.EqualTo(2));
				Assert.That(item2.Type, Is.EqualTo(2));
				Assert.That(item2.BaseField, Is.EqualTo(3));
				Assert.That(item2.ConcreteField, Is.EqualTo(4));

				Assert.That(result[2], Is.InstanceOf<Issue4364_ConcreteIntermediateThingOne>());
			}

			var item3 = (Issue4364_ConcreteIntermediateThingOne)(object)result[2]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(6));

				Assert.That(result[3], Is.InstanceOf<Issue4364_ConcreteIntermediateThingTwo>());
			}

			var item4 = (Issue4364_ConcreteIntermediateThingTwo)(object)result[3]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item4.Id, Is.EqualTo(4));
				Assert.That(item4.Type, Is.EqualTo(102));
				Assert.That(item3.BaseField, Is.EqualTo(5));
				Assert.That(item4.IntermediateField, Is.EqualTo(6));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_BaseThing>().OfType<Issue4364_IntermediateThing>().OrderBy(r => r.Id).ToArray();

			AssertIssue4364(result);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Cast<Issue4364_IntermediateThing>().OrderBy(r => r.Id).ToArray();

			AssertIssue4364(result);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (Issue4364_IntermediateThing)x).OrderBy(r => r.Id).ToArray();

			AssertIssue4364(result);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			var result = db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).OrderBy(r => r.Id).ToArray();

			AssertIssue4364(result);
		}

		static void TestUpdateAndFind(IQueryable<Issue4364_IntermediateThing> table)
		{
			var query = table.Where(x => x.Id == 3);

			query.Set(y => y.IntermediateField, 333).Update();

			var item = query.Single();

			Assert.That(item, Is.InstanceOf<Issue4364_ConcreteIntermediateThingOne>());

			var item3 = (Issue4364_ConcreteIntermediateThingOne)item;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(333));
			}
		}

		static void TestJoinedAll(IDataContext db, IQueryable<Issue4364_BaseThing> table)
		{
			var result =
				(
					from b in table
					join i in db.GetTable<Issue4364_Interaction>() on b.Id equals i.ThingId
					join p in db.GetTable<Issue4364_Person>() on i.PersonId equals p.Id
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
			where T: Issue4364_BaseThing
		{
			var result =
				(
					from b in table
					join i in db.GetTable<Issue4364_Interaction>() on b.Id equals i.ThingId
					join p in db.GetTable<Issue4364_Person>() on i.PersonId equals p.Id
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

		static void TestUpdateAndFindBase(IQueryable<Issue4364_BaseThing> table)
		{
			var query = table.Where(x => x.Id == 3);

			query.Set(y => ((Issue4364_IntermediateThing)y).IntermediateField, 333).Update();

			var item = query.Single();

			Assert.That(item, Is.InstanceOf<Issue4364_ConcreteIntermediateThingOne>());

			var item3 = (Issue4364_ConcreteIntermediateThingOne)item;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(333));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFindBase(db.GetTable<Issue4364_BaseThing>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test12([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoinedAll(db, db.GetTable<Issue4364_BaseThing>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test21([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFind(db.GetTable<Issue4364_IntermediateThing>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test22([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoined(db, db.GetTable<Issue4364_IntermediateThing>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test31([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFind(db.GetTable<Issue4364_BaseThing>().OfType<Issue4364_IntermediateThing>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test32([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoined(db, db.GetTable<Issue4364_BaseThing>().OfType<Issue4364_IntermediateThing>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test41([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFind(db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Cast<Issue4364_IntermediateThing>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test42([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoined(db, db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Cast<Issue4364_IntermediateThing>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test51([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFindBase(db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test52([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoined(db, db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test61([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);

			TestUpdateAndFind(db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (Issue4364_IntermediateThing)x));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4364")]
		public void Issue4364Test62([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4364_BaseThing.TestData);
			using var tp = db.CreateLocalTable(Issue4364_Person.TestData);
			using var ti = db.CreateLocalTable(Issue4364_Interaction.TestData);

			TestJoined(db, db.GetTable<Issue4364_BaseThing>().Where(x => x.Type == 101 || x.Type == 102).Select(x => (Issue4364_IntermediateThing)x));
		}

		static void AssertIssue4364<T>(T[] result)
		{
			Assert.That(result, Has.Length.EqualTo(2));

			Assert.That(result[0], Is.InstanceOf<Issue4364_ConcreteIntermediateThingOne>());
			var item3 = (Issue4364_ConcreteIntermediateThingOne)(object)result[0]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item3.Id, Is.EqualTo(3));
				Assert.That(item3.Type, Is.EqualTo(101));
				Assert.That(item3.BaseField, Is.EqualTo(4));
				Assert.That(item3.ConcreteField, Is.EqualTo(5));
				Assert.That(item3.IntermediateField, Is.EqualTo(6));

				Assert.That(result[1], Is.InstanceOf<Issue4364_ConcreteIntermediateThingTwo>());
			}

			var item4 = (Issue4364_ConcreteIntermediateThingTwo)(object)result[1]!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item4.Id, Is.EqualTo(4));
				Assert.That(item4.Type, Is.EqualTo(102));
				Assert.That(item3.BaseField, Is.EqualTo(5));
				Assert.That(item4.IntermediateField, Is.EqualTo(6));
			}
		}

		[Table(IsColumnAttributeRequired = false)]
		[InheritanceMapping(Code = 1, Type = typeof(Issue4364_ConcreteBaseThingAlpha))]
		[InheritanceMapping(Code = 2, Type = typeof(Issue4364_ConcreteBaseThingBeta))]
		[InheritanceMapping(Code = 101, Type = typeof(Issue4364_ConcreteIntermediateThingOne))]
		[InheritanceMapping(Code = 102, Type = typeof(Issue4364_ConcreteIntermediateThingTwo))]
		abstract class Issue4364_BaseThing
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)] public int Type { get; set; }
			public int BaseField { get; set; }

			public static readonly Issue4364_BaseThing[] TestData = new Issue4364_BaseThing[]
			{
				new Issue4364_ConcreteBaseThingAlpha()
				{
					Id = 1,
					Type = 1,
					BaseField = 2
				},
				new Issue4364_ConcreteBaseThingBeta()
				{
					Id = 2,
					Type = 2,
					BaseField = 3,
					ConcreteField = 4
				},
				new Issue4364_ConcreteIntermediateThingOne()
				{
					Id = 3,
					Type = 101,
					BaseField = 4,
					ConcreteField = 5,
					IntermediateField = 6
				},
				new Issue4364_ConcreteIntermediateThingTwo()
				{
					Id = 4,
					Type = 102,
					BaseField = 5,
					IntermediateField = 6
				}
			};
		}

		class Issue4364_ConcreteBaseThingAlpha : Issue4364_BaseThing
		{
		}

		class Issue4364_ConcreteBaseThingBeta : Issue4364_BaseThing
		{
			// TODO: remove when fixed, nullable added due to Issue4364Test_CreateTableWithNullableRequiredFields
			[LinqToDB.Mapping.Nullable] public int ConcreteField { get; set; }
		}

		abstract class Issue4364_IntermediateThing : Issue4364_BaseThing
		{
			// TODO: remove when fixed, nullable added due to Issue4364Test_CreateTableWithNullableRequiredFields
			[LinqToDB.Mapping.Nullable] public int IntermediateField { get; set; }
		}

		class Issue4364_ConcreteIntermediateThingOne : Issue4364_IntermediateThing
		{
			// TODO: remove when fixed, nullable added due to Issue4364Test_CreateTableWithNullableRequiredFields
			[LinqToDB.Mapping.Nullable] public int ConcreteField { get; set; }
		}

		class Issue4364_ConcreteIntermediateThingTwo : Issue4364_IntermediateThing
		{
		}

		[Table(IsColumnAttributeRequired = false)]
		class Issue4364_Person
		{
			[PrimaryKey] public int Id { get; set; }
			[NotNull] public string FullName { get; set; } = null!;

			public static readonly Issue4364_Person[] TestData = new[]
			{
				new Issue4364_Person() { Id = 1, FullName = "Person 1" },
				new Issue4364_Person() { Id = 2, FullName = "Person 2" },
				new Issue4364_Person() { Id = 3, FullName = "Person 3" },
				new Issue4364_Person() { Id = 4, FullName = "Person 4" },
				new Issue4364_Person() { Id = 5, FullName = "Person 5" },
			};
		}

		[Table(IsColumnAttributeRequired = false)]
		class Issue4364_Interaction
		{
			[PrimaryKey] public int Id { get; set; }
			public int PersonId { get; set; }
			public int ThingId { get; set; }

			public static readonly Issue4364_Interaction[] TestData = new[]
			{
				new Issue4364_Interaction() { Id = 1, PersonId = 2, ThingId = 3 },
				new Issue4364_Interaction() { Id = 2, PersonId = 3, ThingId = 4 },
				new Issue4364_Interaction() { Id = 3, PersonId = 4, ThingId = 1 },
				new Issue4364_Interaction() { Id = 4, PersonId = 1, ThingId = 2 },
			};
		}
		#endregion

		#region Issue 4364 extra tests
		[ActiveIssue]
		[Test]
		public void Issue4364Test_InsertByConcreteType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CreateTableBase>();

			db.Insert(new CreateTable1() { Id = 1, Type = 1, Field1 = 1 });
			db.Insert(new CreateTable2() { Id = 2, Type = 2, Field2 = 2 });

			var result = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
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
		[Test]
		public void Issue4364Test_CreateTableWithNullableRequiredFields([DataSources] string context)
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
