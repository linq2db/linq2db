using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class InheritanceTests : TestBase
	{
		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, db.ParentInheritance);
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, db.ParentInheritance.Select(p => p));
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritance1 select p,
					from p in db.ParentInheritance where p is ParentInheritance1 select p);
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where !(p is ParentInheritanceNull) select p,
					from p in db.ParentInheritance where !(p is ParentInheritanceNull) select p);
		}

		[Test, DataContextSource]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceValue select p,
					from p in db.ParentInheritance where p is ParentInheritanceValue select p);
		}

		[Test, DataContextSource]
		public void Test6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance2 where p is ParentInheritance12 select p;
				q.ToList();
			}
		}

		[Test, DataContextSource]
		public void Test7(string context)
		{
#pragma warning disable 183
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceBase select p,
					from p in db.ParentInheritance where p is ParentInheritanceBase select p);
#pragma warning restore 183
		}

		[Test, DataContextSource]
		public void Test8(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritance1>(),
					db.ParentInheritance.OfType<ParentInheritance1>());
		}

		[Test, DataContextSource]
		public void Test9(string context)
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

		[Test, DataContextSource]
		public void Test10(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritanceValue>(),
					db.ParentInheritance.OfType<ParentInheritanceValue>());
		}

		[Test, DataContextSource]
		public void Test11(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance3 where p is ParentInheritance13 select p;
				q.ToList();
			}
		}

		[Test, DataContextSource]
		public void Test12(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance1 where p.ParentID == 1 select p,
					from p in db.ParentInheritance1 where p.ParentID == 1 select p);
		}

		//[Test, DataContextSource]
		public void Test13(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance4
					join c in    Child on p.ParentID equals c.ParentID
					select p,
					from p in db.ParentInheritance4
					join c in db.Child on p.ParentID equals c.ParentID
					select p);
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest1(string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					   DiscontinuedProduct.ToList()
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName),
					db.DiscontinuedProduct
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName));
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest11(string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					   DiscontinuedProduct.ToList()
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName),
					db.DiscontinuedProduct
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName));
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest2(string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					   Product.ToList()
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName),
					db.Product
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName));
		}

		[Test, NorthwindDataContext]
		public void FirstOrDefault(string context)
		{
			using (var db = new NorthwindDB())
				Assert.AreEqual(
					   DiscontinuedProduct.FirstOrDefault().ProductID,
					db.DiscontinuedProduct.FirstOrDefault().ProductID);
		}

		[Test, DataContextSource]
		public void Cast1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
					db.ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>());
		}

		class ParentEx : Parent
		{
			[NotColumn]
			protected bool Field1;

			public static void Test(InheritanceTests inheritance, string context)
			{
				using (var db = inheritance.GetDataContext(context))
					inheritance.AreEqual(
						inheritance.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>(),
								 db.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>());
			}
		}

		[Test, DataContextSource]
		public void Cast2(string context)
		{
			ParentEx.Test(this, context);
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		class PersonEx : Person
		{
		}

		[Test]
		public void SimplTest()
		{
			using (var db = new TestDataConnection())
				Assert.AreEqual(1, db.GetTable<PersonEx>().Where(_ => _.FirstName == "John").Select(_ => _.ID).Single());
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
			public Value111 Value;
		}

		public class Value111
		{
			public int ID;
		}

		[Test]
		public void InheritanceMappingIssueTest()
		{
			using (var db = new TestDataConnection())
			{
				var q1 = db.GetTable<Parent222>();
				var q  = q1.Where(_ => _.Value.ID == 1);

				var sql = ((IExpressionQuery<Parent222>)q).SqlText;
				Assert.IsNotEmpty(sql);
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

		[Test, DataContextSource]
		public void InheritanceMappingIssue106Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var childIDs = db.GetTable<MyChildBase_11_21>().AsEnumerable()
					.Select(ch => ch.ChildID)
					.OrderBy(x => x)
					.ToList();

				Assert.IsTrue(childIDs.SequenceEqual(new [] {11, 21} ), "{0}: {1}, {2}", childIDs.Count, childIDs[0], childIDs[1]);
			}
		}

		[Test, NorthwindDataContext]
		public void ReferenceNavigation(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result =
					from od in db.OrderDetail
					where od.Product.Category.CategoryName == "Seafood"
					select new { od.Order, od.Product };
				
				var list = result.ToList();

				Assert.AreEqual(330, list.Count);

				foreach (var item in list)
				{
					Assert.IsNotNull(item);
					Assert.IsNotNull(item.Order);
					Assert.IsNotNull(item.Product);
					Assert.IsTrue(
						 item.Product.Discontinued && item.Product is Northwind.DiscontinuedProduct ||
						!item.Product.Discontinued && item.Product is Northwind.ActiveProduct);
				}
			}
		}

		[Test, NorthwindDataContext]
		public void TypeCastIsChildConditional1(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();
				var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct ? x : null).ToList();

				Assert.That(result.Count,                    Is.GreaterThan(0));
				Assert.That(expected.Count(),                Is.EqualTo(result.Count));
				Assert.That(result.Contains(null),           Is.True);
				Assert.That(result.Select(x => x == null ? (int?)null : x.ProductID).Except(expected.Select(x => x == null ? (int?)null : x.ProductID)).Count(), Is.EqualTo(0));
			}
		}

		[Test, NorthwindDataContext]
		public void TypeCastIsChildConditional2(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct);
				var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct);

				var list = result.ToList();

				Assert.Greater(list.Count, 0);
				Assert.AreEqual(expected.Count(), list.Count);
				Assert.IsTrue(list.Except(expected).Count() == 0);
			}
		}

		[Test, NorthwindDataContext]
		public void TypeCastIsChild(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();
				var expected =    Product.Where(x => x is Northwind.DiscontinuedProduct).ToList();

				Assert.Greater(result.Count, 0);
				Assert.AreEqual(result.Count, expected.Count);
			}
		}

		#region Models for Test14

		interface IChildTest14
		{
			int ChildID { get; set; }
		}

		[Table(Name="Child")]
		class ChildTest14 : IChildTest14
		{
			[PrimaryKey] public int ChildID { get; set; }

		}

		T FindById<T>(IQueryable<T> queryable, int id)
			where T : IChildTest14
		{
			return queryable.Where(x => x.ChildID == id).FirstOrDefault();
		}

		#endregion

		[Test, DataContextSource]
		public void Test14(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<ChildTest14>().Select(c => new ChildTest14 { ChildID = c.ChildID });
				FindById(q, 10);
			}
		}

		[Test, NorthwindDataContext]
		public void Test15(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.DiscontinuedProduct.Select(p => p).ToList();
				var expected =    DiscontinuedProduct.Select(p => p).ToList();

				Assert.That(result.Count, Is.Not.EqualTo(0).And.EqualTo(expected.Count));
			}
		}

		[Test, NorthwindDataContext]
		public void Test16(string context)
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.DiscontinuedProduct.ToList();
				var expected =    DiscontinuedProduct.ToList();

				Assert.That(result.Count, Is.Not.EqualTo(0).And.EqualTo(expected.Count));
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
			public virtual TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.Base; }
			}
		}

		[InheritanceMapping(Code = TypeCodeEnum.A1, Type = typeof(InheritanceA1), IsDefault = false)]
		[InheritanceMapping(Code = TypeCodeEnum.A2, Type = typeof(InheritanceA2), IsDefault = true)]
		public abstract class InheritanceA : InheritanceBase
		{
			[Association(CanBeNull = true, ThisKey = "GuidValue", OtherKey = "GuidValue")]
			public List<InheritanceB> Bs { get; set; }

			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A; }
			}
		}

		class InheritanceA1 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A1; }
			}
		}

		class InheritanceA2 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A2; }
			}
		}

		public class InheritanceB : InheritanceBase
		{
		}

		[Test]
		public void GuidTest()
		{
			using (var db = new TestDataConnection())
			{
				var list = db.GetTable<InheritanceA>().Where(a => a.Bs.Any()).ToList();
			}
		}

		[Test, DataContextSource]
		public void QuerySyntaxSimpleTest(string context)
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
			public string FirstName { get; set; }
		}

		public class Test17Tester : Test17Person
		{
			public string LastName { get; set; }
		}

		[Test, DataContextSource(false)]
		public void Test17(string data)
		{
			using (var context = GetDataContext(data))
			{
				var db = (TestDataConnection)context;
				db.GetTable<Test17Person>().OfType<Test17John>().ToList();
				Assert.False(db.LastQuery.ToLowerInvariant().Contains("lastname"), "Why select LastName field??");
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
			[Column] public string FirstName { get; set; }
		}

		public class Test18Female : Test18Person
		{
			[Column] public string FirstName { get; set; }
			[Column] public string LastName  { get; set; }
		}

		[Test, DataContextSource]
		public void Test18(string context)
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

		[Test, DataContextSource]
		public void Test19(string context)
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
	}
}
