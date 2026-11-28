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
		sealed class PersonEx : Person;

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
		public void InheritanceMappingWithComplexColumnFilter([DataSources] string context)
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

		public class MyChildBase_11_21 : MyChildBase;
		public class MyChild11 : MyChildBase_11_21;
		public class MyChild21 : MyChildBase_11_21;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/106")]
		public void SelectIntermediateTypeReturnsOnlyMappedDescendants([DataSources] string context)
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

		public class InheritanceB : InheritanceBase;

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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2429")]
		public void ExpressionMethodColumnPropertyOnBaseTable([DataSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2429")]
		public void ExpressionMethodMethodOnBaseTable([DataSources] string context)
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

		[ActiveIssue(2429, ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "The LINQ expression could not be converted to SQL.",
			Details = "Expression 'x.BaseValue' is not a Field. (Invalid mappings?) - the overridden column property #2429 describes. That issue is closed; this shape is the residue.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2429")]
		public void ExpressionMethodColumnPropertyOverriddenInDerivedTable([DataSources] string context)
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

		[ActiveIssue(2429, ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "The LINQ expression could not be converted to SQL.",
			Details = "as ExpressionMethodColumnPropertyOverriddenInDerivedTable, through the method form.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2429")]
		public void ExpressionMethodMethodOverriddenInDerivedTable([DataSources] string context)
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

		[InheritanceMapping(Code = "DISPLAY", Type = typeof(DisplayDevice))]
		[InheritanceMapping(Code = "TV", Type = typeof(TvDevice))]
		[Table("DeviceInheritance")]
		public abstract class DeviceBase
		{
			[PrimaryKey                    ] public          int     Id           { get; set; }
			[Column                        ] public          string? SerialNumber { get; set; }
			[Column(IsDiscriminator = true)] public abstract string  DeviceType   { get; set; }
		}

		public class DisplayDevice : DeviceBase
		{
			public override string DeviceType { get; set; } = "DISPLAY";
		}

		public class TvDevice : DeviceBase
		{
			public override string DeviceType { get; set; } = "TV";

			[Column]
			public string? Location { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4280")]
		public void OverriddenDiscriminatorInsertUpdateAsBase([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<DeviceBase>();

			var displayDevice = new DisplayDevice { Id = 1, SerialNumber = "Disp00001" };
			var tvDevice      = new TvDevice { Id = 2, SerialNumber = "TV00001", Location = "Something" };

			db.Insert<DeviceBase>(tvDevice);
			db.Insert<DeviceBase>(displayDevice);

			var data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<DisplayDevice>());
				Assert.That(data[1], Is.InstanceOf<TvDevice>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((TvDevice)data[1]).Location, Is.EqualTo("Something"));
			}

			displayDevice.SerialNumber = "Disp00002";
			tvDevice.SerialNumber      = "TV00002";
			tvDevice.Location          = "Anything";

			db.Update<DeviceBase>(tvDevice);
			db.Update<DeviceBase>(displayDevice);

			data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<DisplayDevice>());
				Assert.That(data[1], Is.InstanceOf<TvDevice>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((TvDevice)data[1]).Location, Is.EqualTo("Anything"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4280")]
		public void OverriddenDiscriminatorInsertUpdateAsDerived([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<DeviceBase>();

			var displayDevice = new DisplayDevice { Id = 1, SerialNumber = "Disp00001" };
			var tvDevice      = new TvDevice { Id = 2, SerialNumber = "TV00001", Location = "Something" };

			db.Insert(tvDevice);
			db.Insert(displayDevice);

			var data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0], Is.InstanceOf<DisplayDevice>());
				Assert.That(data[1], Is.InstanceOf<TvDevice>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((TvDevice)data[1]).Location, Is.EqualTo("Something"));
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
				Assert.That(data[0], Is.InstanceOf<DisplayDevice>());
				Assert.That(data[1], Is.InstanceOf<TvDevice>());
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((TvDevice)data[1]).Location, Is.EqualTo("Anything"));
			}
		}

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
		public void InsertUpdateInheritedWithNestedColumnAttributes([InsertOrUpdateDataSources] string context)
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
		public void InsertUpdateInheritedWithNestedColumnFluentMapping([InsertOrUpdateDataSources] string context)
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

		#region Calculated column on inheritance subtype
		public class CalcSubtypeName
		{
			public string? First  { get; set; }
			public string? Second { get; set; }
		}

		[Table("CalcSubtypeTable")]
		[InheritanceMapping(Code = 1, Type = typeof(CalcSubtypeChild))]
		public abstract class CalcSubtypeBase
		{
			[Column, PrimaryKey]             public int Id   { get; set; }
			[Column(IsDiscriminator = true)] public int Type { get; set; }
		}

		// The complex member + its flattened (dotted-MemberName) columns AND the calculated column live on
		// the DERIVED type only. Building GetTable<CalcSubtypeBase>() resolves "Name.First", which isn't on
		// the base, so entity construction converts the construction path to the subtype. The calculated
		// column must still be expanded against the subtype's descriptor, not the base's.
		[Table("CalcSubtypeTable")]
		[Column(MemberName = "Name.First",  Name = "Name_First")]
		[Column(MemberName = "Name.Second", Name = "Name_Second")]
		public class CalcSubtypeChild : CalcSubtypeBase
		{
			public CalcSubtypeChild() => Type = 1;

			public CalcSubtypeName? Name { get; set; }

			[ExpressionMethod(nameof(FullNameImpl), IsColumn = true)]
			public string? FullName { get; set; }

			private static Expression<Func<CalcSubtypeChild, string?>> FullNameImpl()
				=> e => e.Name!.First + " " + e.Name!.Second;
		}

		[Test(Description = "Calculated column declared on an inheritance subtype reached via flattened-column conversion must be expanded against the subtype descriptor")]
		public void CalculatedColumnOnInheritanceSubtype([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CalcSubtypeBase>();

			db.Insert(new CalcSubtypeChild { Id = 1, Name = new CalcSubtypeName { First = "John", Second = "Doe" } });

			var res = db.GetTable<CalcSubtypeBase>().Single();
			Assert.That(res, Is.InstanceOf<CalcSubtypeChild>());
			Assert.That(((CalcSubtypeChild)res).FullName, Is.EqualTo("John Doe"));
		}

		// Variant where the subtype's calculated column is ALSO mapped as a physical column ([Column] +
		// [ExpressionMethod(IsColumn=true)]) with no backing DDL column. InitInheritanceMapping merges the
		// subtype's physical column into the base but not its calculated member, so the base query must
		// dedup it (and expand the calculated value) rather than emit a read of the non-existent column.
		[Table("CalcSubtypeDual")]
		public class CalcSubtypeStorage
		{
			[Column, PrimaryKey] public int     Id     { get; set; }
			[Column]             public int     Type   { get; set; }
			[Column]             public string? Stored { get; set; }
		}

		[Table("CalcSubtypeDual")]
		[InheritanceMapping(Code = 1, Type = typeof(CalcSubtypeDualChild))]
		public abstract class CalcSubtypeDualBase
		{
			[Column, PrimaryKey]             public int     Id     { get; set; }
			[Column(IsDiscriminator = true)] public int     Type   { get; set; }
			[Column]                         public string? Stored { get; set; }
		}

		public class CalcSubtypeDualChild : CalcSubtypeDualBase
		{
			public CalcSubtypeDualChild() => Type = 1;

			[Column]
			[ExpressionMethod(nameof(ComputedImpl), IsColumn = true)]
			public string? Computed { get; set; }

			private static Expression<Func<CalcSubtypeDualChild, string?>> ComputedImpl()
				=> e => e.Stored + "!";
		}

		[Test(Description = "Calculated column on an inheritance subtype that is also mapped as a physical column must dedup + expand on a base query, not read the non-existent column")]
		public void CalculatedColumnOnInheritanceSubtypeDualMapped([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CalcSubtypeStorage>();

			db.Insert(new CalcSubtypeStorage { Id = 1, Type = 1, Stored = "John" });

			var res = db.GetTable<CalcSubtypeDualBase>().Single();
			Assert.That(res, Is.InstanceOf<CalcSubtypeDualChild>());
			Assert.That(((CalcSubtypeDualChild)res).Computed, Is.EqualTo("John!"));
		}
		#endregion

		public enum TphMergeEntityType { None, Type1, Type2 }

		[InheritanceMapping(Code = TphMergeEntityType.None, Type = typeof(TphMergeBaseEntity))]
		[InheritanceMapping(Code = TphMergeEntityType.Type1, Type = typeof(TphMergeType1Entity))]
		[InheritanceMapping(Code = TphMergeEntityType.Type2, Type = typeof(TphMergeType2Entity))]
		[Table]
		public class TphMergeBaseEntity
		{
			[Column] public int Id { get; set; }
			[Column] public string? Description { get; set; }
			[Column(IsDiscriminator = true)] public TphMergeEntityType Type { get; set; }
		}

		public class TphMergeType1Entity : TphMergeBaseEntity
		{
			[Column] public string? Type1EntityProp { get; set; }

			public static TphMergeType1Entity[] Data =
			[
				new TphMergeType1Entity() { Id = 1, Description = "Test1", Type1EntityProp = "Prop1" },
				new TphMergeType1Entity() { Id = 2, Description = "Test2", Type1EntityProp = "Prop2" }
			];
		}

		public class TphMergeType2Entity : TphMergeBaseEntity
		{
			[Column] public string? Type2EntityProp { get; set; }
		}

		// also exists in efcore tests
		[ActiveIssue(4666, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "Member 'System.String Type1EntityProp' not found in type",
			Details = "merging into a TPH table cannot resolve the derived member - #4666's subject. The fragment stops before the type name.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4666")]
		public void MergeIntoTphDerivedTable([MergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var destination = db.CreateLocalTable<TphMergeType1Entity>();
			using var source = db.CreateLocalTable("TphMergeSourceTemp", TphMergeType1Entity.Data);

			destination
				.Merge()
				.Using(source)
				.On((target, source) => target.Id == source.Id)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.DeleteWhenNotMatchedBySourceAnd(i => i.Type == TphMergeEntityType.Type1)
				.Merge();

		}

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

		[ActiveIssue(ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Inheritance mapping is not defined for discriminator value",
			Details = "no-issue: Partial mapping is not supported for now")]
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

		// One cause, about twenty wordings: the discriminator column is never written, and every server rejects
		// the NULL in its own phrasing - twelve distinct texts across the drivers, plus their remote wrappers.
		// Declaring them individually would be a catalogue of vendor prose, so the blanket half declares nothing
		// and only ClickHouse, which fails differently in kind, is declared.
		[ActiveIssue(
			Details = "no-declaration: the Code discriminator column is never written, so every server rejects the NULL - in about twenty different wordings, none of them shared. ClickHouse is the exception and has its own attribute.")]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Inheritance mapping is not defined for discriminator value '0'",
			Details = "no-issue: ClickHouse fails a stage earlier and in kind - linq2db refuses the mapping rather than the server refusing the row.")]
		[Test]
		public void InsertWithoutDiscriminatorThroughBaseTable([DataSources] string context)
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

		// 104 of the cases share one assertion; four providers never reach it because their server rejects the
		// NULL first, each in its own words, so that half declares nothing.
		[ActiveIssue(ErrorMessage = "Assert.That(result, Has.Length.EqualTo(3))",
			Details = "no-issue: the rows inserted through the base table are not read back through the derived one.")]
		[ActiveIssue(Configurations = [TestProvName.AllMariaDB, TestProvName.AllSapHana, TestProvName.AllSybase, TestProvName.AllYdb],
			Details = "no-declaration: these four reject the NULL discriminator at the server before the assertion is reached, and each words it differently.")]
		[Test]
		public void InsertWithoutDiscriminatorThroughDerivedTable([DataSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void InsertDerivedThroughBaseTable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<BaseClass>();

			db.GetTable<BaseClass>().Insert(() => new Child1 { Id = 1, Code = 1, Child1Field = 11 });

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery, Does.Contain("Child1Field"));

			var result = db.GetTable<BaseClass>().OfType<Child1>().Single();
			Assert.That(result.Child1Field, Is.EqualTo(11));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void UpdateDerivedThroughBaseTable_Setter([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable(BaseClass.Data);

			db.GetTable<BaseClass>()
				.Where(t => t.Id == 1)
				.Update(t => new Child1 { Code = t.Code, Child1Field = 99 });

			var result = db.GetTable<BaseClass>().OfType<Child1>().Single(c => c.Id == 1);
			Assert.That(result.Child1Field, Is.EqualTo(99));
		}

		[Obsolete("Exercises the obsolete ITable<TTarget> Update() overload on purpose - covers Issue 5729's explicit-target path")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void UpdateDerivedThroughBaseTable_ExplicitTarget([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable(BaseClass.Data);

			db.GetTable<BaseClass>()
				.Where(t => t.Id == 2)
				.Update(db.GetTable<BaseClass>(), s => new Child2 { Code = s.Code, Child2Field = 88 });

			var result = db.GetTable<BaseClass>().OfType<Child2>().Single(c => c.Id == 2);
			Assert.That(result.Child2Field, Is.EqualTo(88));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void InsertOrUpdateDerivedThroughBaseTable([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<BaseClass>();

			db.GetTable<BaseClass>().InsertOrUpdate(
				() => new Child1 { Id = 1, Code = 1, Child1Field = 55 },
				_ => new Child1 { Child1Field = 66 },
				() => new Child1 { Id = 1 });

			var inserted = db.GetTable<BaseClass>().OfType<Child1>().Single();
			Assert.That(inserted.Child1Field, Is.EqualTo(55));

			// The row now exists, so the second call takes the update branch and the derived column is
			// exercised through the update setter as well as the insert one.
			db.GetTable<BaseClass>().InsertOrUpdate(
				() => new Child1 { Id = 1, Code = 1, Child1Field = 55 },
				_ => new Child1 { Child1Field = 66 },
				() => new Child1 { Id = 1 });

			var updated = db.GetTable<BaseClass>().OfType<Child1>().Single();
			Assert.That(updated.Child1Field, Is.EqualTo(66));
		}

		[Table("InheritanceFilterPositional")]
		[InheritanceMapping(Code = 1, Type = typeof(PositionalChild))]
		abstract class PositionalBase
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(IsDiscriminator = true)] public int Code { get; set; }
		}

		// Constructor-parameter (positional/record-style) TPH subtype: BaseClass/Child1 above are
		// property-only and can only exercise the Assignments arm of UpdateBuilder.ParseSetter's switch;
		// this exercises the Parameters arm, since Value only ever arrives via the constructor.
		class PositionalChild : PositionalBase
		{
			public PositionalChild(int id, int code, int value)
			{
				Id    = id;
				Code  = code;
				Value = value;
			}

			[Column(CanBeNull = true)] public int Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void InsertPositionalDerivedThroughBaseTable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<PositionalBase>();

			db.GetTable<PositionalBase>().Insert(() => new PositionalChild(1, 1, 42));

			var result = db.GetTable<PositionalBase>().OfType<PositionalChild>().Single();
			Assert.That(result.Id,    Is.EqualTo(1));
			Assert.That(result.Value, Is.EqualTo(42));
		}

		// FeatureUpdateOutputWithoutOldSingle from UpdateWithOutputTests, minus YDB: the default projection
		// emits Deleted and Inserted, so every column appears twice in RETURNING and YDB rejects that with
		// "Duplicated member".
		const string FeatureUpdateOutput = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite},{TestProvName.AllDuckDB}";

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void UpdateWithOutputDefaultProjectionOverInheritanceRoot([IncludeDataSources(true, FeatureUpdateOutput)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable(BaseClass.Data);

			// The setter names only Code, declared on BaseClass itself, so it reaches the guard nowhere. The
			// projection does: UpdateWithOutput without an explicit output expression builds full entities
			// for Deleted and Inserted, and over an inheritance root those carry every mapped subtype's
			// merged columns against a BaseClass-typed target. Pre-fix that threw ArgumentException before
			// any SQL was emitted. Asserting on Inserted only - the providers here do not supply the old
			// row, per FeatureUpdateOutputWithoutOldSingle.
			var output = db.GetTable<BaseClass>()
				.Where(t => t.Id == 1)
				.UpdateWithOutput(t => new Child1 { Code = t.Code })
				.ToArray();

			Assert.That(output, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(output[0].Inserted, Is.InstanceOf<Child1>());
				Assert.That(output[0].Inserted.Id, Is.EqualTo(1));
				Assert.That(((Child1)output[0].Inserted).Child1Field, Is.EqualTo(11));
			}
		}

		// OUTPUT ... INTO builds its target ref from the output table's object type rather than the
		// updated table's, so it reaches EnsureDeclaringType by a route no other test covers. Unlike the
		// row-returning form above it materializes nothing, so this one can assert success.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void UpdateWithOutputIntoDerivedThroughBaseTable([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db          = GetDataContext(context);
			using var _           = db.CreateLocalTable(BaseClass.Data);
			using var destination = db.CreateLocalTable<BaseClass>(tableName: "InheritanceFilterOutput");

			db.GetTable<BaseClass>()
				.Where(t => t.Id == 1)
				.UpdateWithOutputInto(
					t => new Child1 { Code = t.Code, Child1Field = 77 },
					destination,
					(deleted, inserted) => new Child1 { Id = inserted.Id, Code = inserted.Code, Child1Field = 88 });

			var updated = db.GetTable<BaseClass>().OfType<Child1>().Single(c => c.Id == 1);
			var written = destination.OfType<Child1>().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(updated.Child1Field, Is.EqualTo(77));
				Assert.That(written.Child1Field, Is.EqualTo(88));
			}
		}

		[Table("InheritanceShadow")]
		[InheritanceMapping(Code = 1, Type = typeof(ShadowChild))]
		[InheritanceMapping(Code = 2, Type = typeof(ShadowGrandchild))]
		abstract class ShadowBase
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(IsDiscriminator = true)] public int Code { get; set; }
		}

		class ShadowChild : ShadowBase
		{
			[Column("ChildValue", CanBeNull = true)] public int Value { get; set; }
		}

		class ShadowGrandchild : ShadowChild
		{
			[Column("GrandchildValue", CanBeNull = true)] public new int Value { get; set; }
		}

		// Raw SQL below, so SQLite only.
		// No ErrorTypeName: the two SQLite drivers raise their own exception types and only the message is shared.
		[ActiveIssue(5852, ErrorMessage = "no such column: GrandchildValue",
			Details = "the shadowing member is not mapped onto the base table at all, so the column the insert should have written is never created - #5852's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5852")]
		public void InsertShadowedMemberThroughBaseTable([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<ShadowBase>();

			db.GetTable<ShadowBase>().Insert(() => new ShadowGrandchild { Id = 1, Code = 2, Value = 42 });

			var dc = (DataConnection)db;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(dc.Execute<int?>("SELECT GrandchildValue FROM InheritanceShadow"), Is.EqualTo(42));
				Assert.That(dc.Execute<int?>("SELECT ChildValue FROM InheritanceShadow"),      Is.Null);
			}
		}

		// Derives from BaseClass but carries no [InheritanceMapping] entry, so UnmappedField maps to no
		// column on the base entity descriptor. EnsureDeclaringType retypes the target for it just the
		// same, so the guard must not turn the failure into a silently omitted column.
		class UnmappedChild : BaseClass
		{
			[Column(CanBeNull = true)] public int UnmappedField { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5729")]
		public void InsertUnmappedDerivedThroughBaseTable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<BaseClass>();

			Assert.That(
				() => db.GetTable<BaseClass>().Insert(() => new UnmappedChild { Id = 1, Code = 1, UnmappedField = 5 }),
				Throws.TypeOf<LinqToDBException>());

			Assert.That(db.GetTable<BaseClass>().Count(), Is.Zero);
		}

		#endregion

		[Table, InheritanceMapping(Code = OfTypeBdaTask.Code, Type = typeof(OfTypeBdaTask))]
		class OfTypeTaskBase
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }
		}

		[Table("OfTypeTaskBase")]
		sealed class OfTypeTaskTable
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Column]
			public string? BdaValue { get; set; }
		}

		[Table("OfTypeTaskBase")]
		sealed class OfTypeBdaTask : OfTypeTaskBase
		{
			public const string Code = "bda.Requests";

			[Column]
			public string? BdaValue { get; set; }
		}

		sealed class SelectAllAndExpand<T>
		{
			public T Instance { get; set; } = default!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1046")]
		public void OfTypeProjectionIntoGenericWrapper([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (db.CreateLocalTable<OfTypeTaskTable>())
			{
				db.Insert(new OfTypeBdaTask
				{
					BdaValue = "Bda value",
					TargetName = OfTypeBdaTask.Code,
					Id = 1
				});

				var items = db.GetTable<OfTypeTaskBase>()
					.OfType<OfTypeBdaTask>()
					.Select(p => new SelectAllAndExpand<OfTypeBdaTask>
					{
						Instance = p
					})
					.ToArray();

				Assert.That(items, Has.Length.EqualTo(1));
				Assert.That(items[0].Instance.BdaValue, Is.EqualTo("Bda value"));

				var items2 = db.GetTable<OfTypeTaskBase>()
					.OfType<OfTypeBdaTask>()
					.Select(p => new SelectAllAndExpand<OfTypeBdaTask>
					{
						Instance = new OfTypeBdaTask
						{
							BdaValue = p.BdaValue
						}
					})
					.ToArray();

				Assert.That(items2, Has.Length.EqualTo(1));
				Assert.That(items2[0].Instance.BdaValue, Is.EqualTo("Bda value"));
			}
		}

		[Table, InheritanceMapping(Code = "bda.Requests", Type = typeof(DiscriminatedBdaTask))]
		[Table, InheritanceMapping(Code = "None", Type = typeof(DiscriminatedOtherTask))]
		class DiscriminatedTaskBase
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public DiscriminatedTaskStage? ActualStage { get; set; }

			private static Expression<Func<DiscriminatedTaskBase, DiscriminatedTaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		sealed class DiscriminatedBdaTask : DiscriminatedTaskBase
		{
			public const string Code = "bda.Requests";
		}

		sealed class DiscriminatedOtherTask : DiscriminatedTaskBase
		{
			public const string Code = "None";
		}

		[Table]
		sealed class DiscriminatedTaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "char")]
			public bool Actual { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1057")]
		public void OfTypeSelectsExpressionPredicateAssociation([DataSources] string configuration)
		{
			using var db = GetDataContext(configuration);
			using (db.CreateLocalTable<DiscriminatedTaskBase>())
			using (db.CreateLocalTable<DiscriminatedTaskStage>())
			{
				db.Insert(new DiscriminatedTaskBase { Id = 1, TargetName = "bda.Requests" });
				db.Insert(new DiscriminatedTaskBase { Id = 2, TargetName = "None" });
				db.Insert(new DiscriminatedTaskStage { Id = 2, TaskId = 1, Actual = true });

				var query = db.GetTable<DiscriminatedTaskBase>()
						.OfType<DiscriminatedBdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = p.ActualStage!.Id
						});
				var res = query.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].Instance, Is.Not.Null);
					Assert.That(res[0].ActualStageId, Is.EqualTo(2));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1057")]
		public void ExpressionPredicateAssociationThroughBaseCast([DataSources] string configuration)
		{
			using var db = GetDataContext(configuration);
			using (db.CreateLocalTable<DiscriminatedTaskBase>())
			using (db.CreateLocalTable<DiscriminatedTaskStage>())
			{
				db.Insert(new DiscriminatedTaskBase { Id = 1, TargetName = "bda.Requests" });
				db.Insert(new DiscriminatedTaskBase { Id = 2, TargetName = "None" });
				db.Insert(new DiscriminatedTaskStage { Id = 2, TaskId = 1, Actual = true });

				var query = db.GetTable<DiscriminatedTaskBase>()
						.OfType<DiscriminatedBdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as DiscriminatedTaskBase).ActualStage!.Id
						});
				var res = query.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].Instance, Is.Not.Null);
					Assert.That(res[0].ActualStageId, Is.EqualTo(2));
				}

				var query2 = db.GetTable<DiscriminatedTaskBase>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as DiscriminatedTaskBase).ActualStage!.Id
						});

				var res2 = query2.OrderBy(_ => _.Instance.Id).ToArray();

				Assert.That(res2, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res2[0].Instance, Is.Not.Null);
					Assert.That(res2[0].ActualStageId, Is.EqualTo(2));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/513")]
		public void InheritanceMappedParentsAndChildrenMatchData([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(InheritanceParent[0].GetType(), Is.EqualTo(typeof(InheritanceParentBase)));
				Assert.That(InheritanceParent[1].GetType(), Is.EqualTo(typeof(InheritanceParent1)));
				Assert.That(InheritanceParent[2].GetType(), Is.EqualTo(typeof(InheritanceParent2)));
			}

			AreEqual(InheritanceParent, db.InheritanceParent);
			AreEqual(InheritanceChild, db.InheritanceChild);
		}

		// Base entity with discriminator
		[Table("WideInheritEntity")]
		[InheritanceMapping(Code = "Base", Type = typeof(WideInheritanceBase), IsDefault = true)]
		[InheritanceMapping(Code = "Type01", Type = typeof(WideInheritanceType01))]
		[InheritanceMapping(Code = "Type02", Type = typeof(WideInheritanceType02))]
		[InheritanceMapping(Code = "Type03", Type = typeof(WideInheritanceType03))]
		[InheritanceMapping(Code = "Type04", Type = typeof(WideInheritanceType04))]
		[InheritanceMapping(Code = "Type05", Type = typeof(WideInheritanceType05))]
		[InheritanceMapping(Code = "Type06", Type = typeof(WideInheritanceType06))]
		[InheritanceMapping(Code = "Type07", Type = typeof(WideInheritanceType07))]
		[InheritanceMapping(Code = "Type08", Type = typeof(WideInheritanceType08))]
		[InheritanceMapping(Code = "Type09", Type = typeof(WideInheritanceType09))]
		[InheritanceMapping(Code = "Type10", Type = typeof(WideInheritanceType10))]
		[InheritanceMapping(Code = "Type11", Type = typeof(WideInheritanceType11))]
		[InheritanceMapping(Code = "Type12", Type = typeof(WideInheritanceType12))]
		[InheritanceMapping(Code = "Type13", Type = typeof(WideInheritanceType13))]
		[InheritanceMapping(Code = "Type14", Type = typeof(WideInheritanceType14))]
		[InheritanceMapping(Code = "Type15", Type = typeof(WideInheritanceType15))]
		[InheritanceMapping(Code = "Type16", Type = typeof(WideInheritanceType16))]
		[InheritanceMapping(Code = "Type17", Type = typeof(WideInheritanceType17))]
		public class WideInheritanceBase
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string EntityType { get; set; } = null!;

			[Column]
			public string Name { get; set; } = null!;

			[Column]
			public DateTime CreatedDate { get; set; }
		}

		// 17 descendant types
		public class WideInheritanceType01 : WideInheritanceBase
		{
			[Column] public string? Property01 { get; set; }
		}

		public class WideInheritanceType02 : WideInheritanceBase
		{
			[Column] public string? Property02 { get; set; }
		}

		public class WideInheritanceType03 : WideInheritanceBase
		{
			[Column] public string? Property03 { get; set; }
		}

		public class WideInheritanceType04 : WideInheritanceBase
		{
			[Column] public string? Property04 { get; set; }
		}

		public class WideInheritanceType05 : WideInheritanceBase
		{
			[Column] public string? Property05 { get; set; }
		}

		public class WideInheritanceType06 : WideInheritanceBase
		{
			[Column] public string? Property06 { get; set; }
		}

		public class WideInheritanceType07 : WideInheritanceBase
		{
			[Column] public string? Property07 { get; set; }
		}

		public class WideInheritanceType08 : WideInheritanceBase
		{
			[Column] public string? Property08 { get; set; }
		}

		public class WideInheritanceType09 : WideInheritanceBase
		{
			[Column] public string? Property09 { get; set; }
		}

		public class WideInheritanceType10 : WideInheritanceBase
		{
			[Column] public string? Property10 { get; set; }
		}

		public class WideInheritanceType11 : WideInheritanceBase
		{
			[Column] public string? Property11 { get; set; }
		}

		public class WideInheritanceType12 : WideInheritanceBase
		{
			[Column] public string? Property12 { get; set; }
		}

		public class WideInheritanceType13 : WideInheritanceBase
		{
			[Column] public string? Property13 { get; set; }
		}

		public class WideInheritanceType14 : WideInheritanceBase
		{
			[Column] public string? Property14 { get; set; }
		}

		public class WideInheritanceType15 : WideInheritanceBase
		{
			[Column] public string? Property15 { get; set; }
		}

		public class WideInheritanceType16 : WideInheritanceBase
		{
			[Column] public string? Property16 { get; set; }
		}

		public class WideInheritanceType17 : WideInheritanceBase
		{
			[Column] public string? Property17 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5274")]
		public void LargeInheritanceHierarchy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<WideInheritanceBase>();
			// Insert test data for each type
			var baseDate = TestData.Date;

			db.Insert(new WideInheritanceType01 { EntityType = "Type01", Name = "Entity 01", CreatedDate = baseDate.AddDays(1), Property01  = "Value01" });
			db.Insert(new WideInheritanceType02 { EntityType = "Type02", Name = "Entity 02", CreatedDate = baseDate.AddDays(2), Property02  = "Value02" });
			db.Insert(new WideInheritanceType03 { EntityType = "Type03", Name = "Entity 03", CreatedDate = baseDate.AddDays(3), Property03  = "Value03" });
			db.Insert(new WideInheritanceType04 { EntityType = "Type04", Name = "Entity 04", CreatedDate = baseDate.AddDays(4), Property04  = "Value04" });
			db.Insert(new WideInheritanceType05 { EntityType = "Type05", Name = "Entity 05", CreatedDate = baseDate.AddDays(5), Property05  = "Value05" });
			db.Insert(new WideInheritanceType06 { EntityType = "Type06", Name = "Entity 06", CreatedDate = baseDate.AddDays(6), Property06  = "Value06" });
			db.Insert(new WideInheritanceType07 { EntityType = "Type07", Name = "Entity 07", CreatedDate = baseDate.AddDays(7), Property07  = "Value07" });
			db.Insert(new WideInheritanceType08 { EntityType = "Type08", Name = "Entity 08", CreatedDate = baseDate.AddDays(8), Property08  = "Value08" });
			db.Insert(new WideInheritanceType09 { EntityType = "Type09", Name = "Entity 09", CreatedDate = baseDate.AddDays(9), Property09  = "Value09" });
			db.Insert(new WideInheritanceType10 { EntityType = "Type10", Name = "Entity 10", CreatedDate = baseDate.AddDays(10), Property10 = "Value10" });
			db.Insert(new WideInheritanceType11 { EntityType = "Type11", Name = "Entity 11", CreatedDate = baseDate.AddDays(11), Property11 = "Value11" });
			db.Insert(new WideInheritanceType12 { EntityType = "Type12", Name = "Entity 12", CreatedDate = baseDate.AddDays(12), Property12 = "Value12" });
			db.Insert(new WideInheritanceType13 { EntityType = "Type13", Name = "Entity 13", CreatedDate = baseDate.AddDays(13), Property13 = "Value13" });
			db.Insert(new WideInheritanceType14 { EntityType = "Type14", Name = "Entity 14", CreatedDate = baseDate.AddDays(14), Property14 = "Value14" });
			db.Insert(new WideInheritanceType15 { EntityType = "Type15", Name = "Entity 15", CreatedDate = baseDate.AddDays(15), Property15 = "Value15" });
			db.Insert(new WideInheritanceType16 { EntityType = "Type16", Name = "Entity 16", CreatedDate = baseDate.AddDays(16), Property16 = "Value16" });
			db.Insert(new WideInheritanceType17 { EntityType = "Type17", Name = "Entity 17", CreatedDate = baseDate.AddDays(17), Property17 = "Value17" });

			// Test querying all entities - this should not cause stack overflow
			var allEntities = table.ToList();
			Assert.That(allEntities, Has.Count.EqualTo(17));

			// Test querying specific types
			var entity01 = table.OfType<WideInheritanceType01>().FirstOrDefault();
			Assert.That(entity01,             Is.Not.Null);
			Assert.That(entity01!.Property01, Is.EqualTo("Value01"));

			// Test filtering by discriminator
			var type05Entities = table.Where(e => e.EntityType == "Type05").ToList();
			Assert.That(type05Entities, Has.Count.EqualTo(1));

			// Test complex query with multiple type checks
			var multiTypeQuery = table
				.Where(e => e.EntityType == "Type01" || e.EntityType == "Type10" || e.EntityType == "Type17")
				.OrderBy(e => e.Name)
				.ToList();
			Assert.That(multiTypeQuery, Has.Count.EqualTo(3));

			// Test projection with inheritance
			var projectedData = table
				.Select(e => new { e.Id, e.Name, e.EntityType })
				.OrderBy(e => e.EntityType)
				.ToList();
			Assert.That(projectedData, Has.Count.EqualTo(17));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5274")]
		public void InheritanceWithComplexConditions([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var table    = db.CreateLocalTable<WideInheritanceBase>();
			var       baseDate = TestData.Date;

			// Insert subset of data
			db.Insert(new WideInheritanceType01 { EntityType = "Type01", Name = "A-Entity", CreatedDate = baseDate, Property01             = "Test1" });
			db.Insert(new WideInheritanceType05 { EntityType = "Type05", Name = "B-Entity", CreatedDate = baseDate.AddDays(5), Property05  = "Test5" });
			db.Insert(new WideInheritanceType10 { EntityType = "Type10", Name = "C-Entity", CreatedDate = baseDate.AddDays(10), Property10 = "Test10" });
			db.Insert(new WideInheritanceType17 { EntityType = "Type17", Name = "D-Entity", CreatedDate = baseDate.AddDays(17), Property17 = "Test17" });

			// Test complex condition that triggers OptimizeExpression
			var result = table
				.Where(e => 
					(e.EntityType == "Type01" && e.Name.StartsWith("A"))    ||
					(e.EntityType == "Type05" && e.CreatedDate > baseDate)  ||
					(e.EntityType == "Type10" && e.Name.Contains("Entity")) ||
					(e.EntityType == "Type17" && e.Id > 0))
				.ToList();

			Assert.That(result, Has.Count.EqualTo(4));
		}

		[Table("InheritInsertParent")]
		[InheritanceMapping(Code = "InheritanceInsertChildA", Type = typeof(InheritanceInsertChildA))]
		[InheritanceMapping(Code = "InheritanceInsertChildB", Type = typeof(InheritanceInsertChildB))]
		public class InheritInsertParent
		{
			[PrimaryKey, Identity]
			public int Id;

			[Column(IsDiscriminator = true)]
			public string? Type;
		}

		public class InheritanceInsertChildA : InheritInsertParent
		{
			public InheritanceInsertChildA()
			{
				Type = "InheritanceInsertChildA";
			}

			[Column(Length = 20)]
			public string? StringValue { get; set; }
		}

		public class InheritanceInsertChildB : InheritInsertParent
		{
			public InheritanceInsertChildB()
			{
				Type = "InheritanceInsertChildB";
			}

			[Column(CanBeNull = true)]
			public int IntValue { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/564")]
		public void InsertDerivedEntitiesThroughDiscriminatedParent([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InheritInsertParent>())
			{
				db.Insert(new InheritanceInsertChildA() {StringValue = "SomeValue"});
				db.Insert(new InheritanceInsertChildB() {IntValue    = 911});

				Assert.That(db.GetTable<InheritInsertParent>().Count(), Is.EqualTo(2));
			}
		}
	}
}
