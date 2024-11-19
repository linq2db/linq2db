using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class InheritanceTests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, db.ParentInheritance);
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, db.ParentInheritance.Select(p => p));
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritance1 select p,
					from p in db.ParentInheritance where p is ParentInheritance1 select p);
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where !(p is ParentInheritanceNull) select p,
					from p in db.ParentInheritance where !(p is ParentInheritanceNull) select p);
		}

		[Test]
		public void Test5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceValue select p,
					from p in db.ParentInheritance where p is ParentInheritanceValue select p);
		}

		[Test]
		public void Test6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance2 where p is ParentInheritance12 select p;
				var _ = q.ToList();
			}
		}

		[Test]
		public void Test7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceBase select p,
					from p in db.ParentInheritance where p is ParentInheritanceBase select p);
		}

		[Test]
		public void Test8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritance1>(),
					db.ParentInheritance.OfType<ParentInheritance1>());
		}

		[Test]
		public void Test9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritanceValue>(),
					db.ParentInheritance.OfType<ParentInheritanceValue>());
		}

		[Test]
		public void Test11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance3 where p is ParentInheritance13 select p;
				var _ = q.ToList();
			}
		}

		[Test]
		public void Test12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance1 where p.ParentID == 1 select p,
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
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<ParentInheritanceBase3>()
					.Where(x => x is ParentInheritance13)
					.ToList();
				Assert.That(q, Has.Count.EqualTo(2));
			}
		}

		[Test]
		public void TypeCastAsTest1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.DiscontinuedProduct.ToList()
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName),
					db.DiscontinuedProduct
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName));
			}
		}

		[Test]
		public void TypeCastAsTest11([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.DiscontinuedProduct.ToList()
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName),
					db.DiscontinuedProduct
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName));
			}
		}

		[Test]
		public void TypeCastAsTest2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Product.ToList()
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName),
					db.Product
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName));
			}
		}

		[Test]
		public void FirstOrDefault([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				Assert.That(
					db.DiscontinuedProduct.FirstOrDefault()!.ProductID, Is.EqualTo(dd.DiscontinuedProduct.FirstOrDefault()!.ProductID));
			}
		}

		[Test]
		public void Cast1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
					db.ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>());
		}

		[Test]
		public async Task Cast1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
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
				using (var db = inheritance.GetDataContext(context))
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
			using (var db = new DataConnection())
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
		public void InheritanceMappingIssueTest()
		{
			using (var db = new DataConnection())
			{
				var q1 = db.GetTable<Parent222>();
				var q  = q1.Where(_ => _.Value.ID == 1);

				var sql = ((IExpressionQuery<Parent222>)q).SqlText;
				Assert.That(sql, Is.Not.Empty);
			}
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
			using (var db = GetDataContext(context))
			{
				var childIDs = db.GetTable<MyChildBase_11_21>().AsEnumerable()
					.Select(ch => ch.ChildID)
					.OrderBy(x => x)
					.ToList();

				Assert.That(childIDs.SequenceEqual(new [] {11, 21} ), Is.True, $"{childIDs.Count}: {childIDs[0]}, {childIDs[1]}");
			}
		}

		[Test]
		public void ReferenceNavigation([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result =
					from od in db.OrderDetail
					where od.Product.Category!.CategoryName == "Seafood"
					select new { od.Order, od.Product };

				var list = result.ToList();

				Assert.That(list, Has.Count.EqualTo(330));

				foreach (var item in list)
				{
					Assert.That(item, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(item.Order, Is.Not.Null);
						Assert.That(item.Product, Is.Not.Null);
					});
					Assert.That(
						 item.Product.Discontinued && item.Product is Northwind.DiscontinuedProduct ||
						!item.Product.Discontinued && item.Product is Northwind.ActiveProduct, Is.True);
				}
			}
		}

		[Test]
		public void TypeCastIsChildConditional1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();
				var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(result, Is.Not.Empty);
					Assert.That(expected, Has.Count.EqualTo(result.Count));
				});
				Assert.That(result, Does.Contain(null));
				Assert.That(result.Select(x => x == null ? (int?)null : x.ProductID).Except(expected.Select(x => x == null ? (int?)null : x.ProductID)).Count(), Is.EqualTo(0));
			}
		}

		[Test]
		public void TypeCastIsChildConditional2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct);
				var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct);

				var list = result.ToList();

				Assert.That(list, Is.Not.Empty);
				Assert.That(list, Has.Count.EqualTo(expected.Count()));
				Assert.That(list.Except(expected).Count(), Is.EqualTo(0));
			}
		}

		[Test]
		public void TypeCastIsChild([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var result   = db.Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();
				var expected = dd.Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(result, Is.Not.Empty);
					Assert.That(expected, Has.Count.EqualTo(result.Count));
				});
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
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<ChildTest14>().Select(c => new ChildTest14 { ChildID = c.ChildID });
				FindById(q, 10);
			}
		}

		[Test]
		public void Test15([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var result   = db.DiscontinuedProduct.Select(p => p).ToList();
				var expected = dd.DiscontinuedProduct.Select(p => p).ToList();

				Assert.That(result, Has.Count.EqualTo(expected.Count));
			}
		}

		[Test]
		public void Test16([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var result   = db.DiscontinuedProduct.ToList();
				var expected = dd.DiscontinuedProduct.ToList();

				Assert.That(result, Has.Count.EqualTo(expected.Count));
			}
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
			using (var db = new DataConnection())
			{
				var list = db.GetTable<InheritanceA>().Where(a => a.Bs.Any()).ToList();
			}
		}

		[Test]
		public void QuerySyntaxSimpleTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// db.GetTable<Parent111>().OfType<Parent222>().ToList(); - it's work!!!
				(from p in db.GetTable<Parent111>().OfType<Parent222>() select p).ToList();
			}
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
			using (var context = GetDataContext(data))
			{
				var db = (TestDataConnection)context;
				db.GetTable<Test17Person>().OfType<Test17John>().ToList();
				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("lastname"), "Why select LastName field??");
			}
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
			using (var db = GetDataContext(context))
			{
				var ids = Enumerable.Range(0, 10).ToList();
				var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

				var list = q.Distinct().OfType<Test18Female>().ToList();
			}
		}

		[Test]
		public async Task Test18Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ids = Enumerable.Range(0, 10).ToList();
				var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

				var list = await q.Distinct().OfType<Test18Female>().ToListAsync();
			}
		}

		[Test]
		public void Test19([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ids = Enumerable.Range(0, 10).ToList();
				var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

				IQueryable iq   = q.Distinct();
				var        list = iq.OfType<Test18Female>().ToList();
			}
		}

		[Test]
		public void InheritanceAssociationTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
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
				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecordById?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordById?.Value, Is.EqualTo(100));
				});

				var baseTableRecordWithValuePredicate = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1 && x.Value == 100);
				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecordWithValuePredicate?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordWithValuePredicate?.Value, Is.EqualTo(100));
				});
			}
		}

		[Test]
		public void Issue2429MethodsTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(BaseTable.Data))
			{
				var baseTableRecordById = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1);
				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecordById?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordById?.Value, Is.EqualTo(100));
				});

				var baseTableRecordWithValuePredicate = db.GetTable<BaseTable>().FirstOrDefault(x => x.Id == 1 && x.GetValue() == 100);
				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecordWithValuePredicate?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecordWithValuePredicate?.Value, Is.EqualTo(100));
				});
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

				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecord?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecord?.Value, Is.EqualTo(100));

					Assert.That(derivedTableRecord?.Id, Is.EqualTo(1));
					Assert.That(derivedTableRecord?.Value * -1, Is.EqualTo(100));
				});
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

				Assert.Multiple(() =>
				{
					Assert.That(baseTableRecord?.Id, Is.EqualTo(1));
					Assert.That(baseTableRecord?.Value, Is.EqualTo(100));

					Assert.That(derivedTableRecord?.Id, Is.EqualTo(1));
					Assert.That(derivedTableRecord?.Value * -1, Is.EqualTo(100));
				});
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
			Assert.Multiple(() =>
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
			});
			Assert.Multiple(() =>
			{
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Something"));
			});

			displayDevice.SerialNumber = "Disp00002";
			tvDevice.SerialNumber      = "TV00002";
			tvDevice.Location          = "Anything";

			db.Update<Issue4280Base>(tvDevice);
			db.Update<Issue4280Base>(displayDevice);

			data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
			});
			Assert.Multiple(() =>
			{
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Anything"));
			});
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
			Assert.Multiple(() =>
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
			});
			Assert.Multiple(() =>
			{
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00001"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00001"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Something"));
			});

			displayDevice.SerialNumber = "Disp00002";
			tvDevice.SerialNumber      = "TV00002";
			tvDevice.Location          = "Anything";

			db.Update(tvDevice);
			db.Update(displayDevice);

			data = tb.OrderBy(r => r.Id).ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(data[0], Is.InstanceOf<Issue4280T1>());
				Assert.That(data[1], Is.InstanceOf<Issue4280T2>());
			});
			Assert.Multiple(() =>
			{
				Assert.That(data[0].SerialNumber, Is.EqualTo("Disp00002"));
				Assert.That(data[1].SerialNumber, Is.EqualTo("TV00002"));
				Assert.That(((Issue4280T2)data[1]).Location, Is.EqualTo("Anything"));
			});
		}
		#endregion
	}
}
