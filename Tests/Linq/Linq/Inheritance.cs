using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data.Linq;
using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.DataAccess;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Inheritance : TestBase
	{
		[Test]
		public void Test1()
		{
			ForEachProvider(db => AreEqual(ParentInheritance, db.ParentInheritance));
		}

		[Test]
		public void Test2()
		{
			ForEachProvider(db => AreEqual(ParentInheritance, db.ParentInheritance.Select(p => p)));
		}

		[Test]
		public void Test3()
		{
			ForEachProvider(db => AreEqual(
				from p in    ParentInheritance where p is ParentInheritance1 select p,
				from p in db.ParentInheritance where p is ParentInheritance1 select p));
		}

		[Test]
		public void Test4()
		{
			ForEachProvider(db => AreEqual(
				from p in    ParentInheritance where !(p is ParentInheritanceNull) select p,
				from p in db.ParentInheritance where !(p is ParentInheritanceNull) select p));
		}

		[Test]
		public void Test5()
		{
			ForEachProvider(db => AreEqual(
				from p in    ParentInheritance where p is ParentInheritanceValue select p,
				from p in db.ParentInheritance where p is ParentInheritanceValue select p));
		}

		[Test]
		public void Test6()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.ParentInheritance2 where p is ParentInheritance12 select p;
				q.ToList();
			});
		}

		[Test]
		public void Test7()
		{
#pragma warning disable 183
			var expected = from p in ParentInheritance where p is ParentInheritanceBase select p;
			ForEachProvider(db => AreEqual(expected, from p in db.ParentInheritance where p is ParentInheritanceBase select p));
#pragma warning restore 183
		}

		[Test]
		public void Test8()
		{
			ForEachProvider(db => AreEqual(
				   ParentInheritance.OfType<ParentInheritance1>(),
				db.ParentInheritance.OfType<ParentInheritance1>()));
		}

		[Test]
		public void Test9()
		{
			ForEachProvider(db =>
				AreEqual(
					   ParentInheritance
						.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
						.OfType<ParentInheritanceNull>(),
					db.ParentInheritance
						.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
						.OfType<ParentInheritanceNull>()));
		}

		[Test]
		public void Test10()
		{
			var expected = ParentInheritance.OfType<ParentInheritanceValue>();
			ForEachProvider(db => AreEqual(expected, db.ParentInheritance.OfType<ParentInheritanceValue>()));
		}

		[Test]
		public void Test11()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.ParentInheritance3 where p is ParentInheritance13 select p;
				q.ToList();
			});
		}

		[Test]
		public void Test12()
		{
			ForEachProvider(db => AreEqual(
				from p in    ParentInheritance1 where p.ParentID == 1 select p,
				from p in db.ParentInheritance1 where p.ParentID == 1 select p));
		}

		//[Test]
		public void Test13()
		{
			ForEachProvider(db => AreEqual(
				from p in    ParentInheritance4
				join c in    Child on p.ParentID equals c.ParentID
				select p,
				from p in db.ParentInheritance4
				join c in db.Child on p.ParentID equals c.ParentID
				select p));
		}

		[Test]
		public void TypeCastAsTest1()
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

		[Test]
		public void TypeCastAsTest11()
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

		[Test]
		public void TypeCastAsTest2()
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

		[Test]
		public void FirstOrDefault()
		{
			using (var db = new NorthwindDB())
				Assert.AreEqual(
					   DiscontinuedProduct.FirstOrDefault().ProductID,
					db.DiscontinuedProduct.FirstOrDefault().ProductID);
		}

		[Test]
		public void Cast1()
		{
			ForEachProvider(db => AreEqual(
				   ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
				db.ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>()));
		}

		class ParentEx : Parent
		{
			[MapIgnore]
			protected bool Field1;

			public static void Test(Inheritance inheritance)
			{
				inheritance.ForEachProvider(db => inheritance.AreEqual(
					inheritance.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>(),
							 db.Parent.Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>()));
			}
		}

		[Test]
		public void Cast2()
		{
			ParentEx.Test(this);
		}

		[TableName("Person")]
		class PersonEx : Person
		{
		}

		[Test]
		public void SimplTest()
		{
			using (var db = new TestDbManager())
				Assert.AreEqual(1, db.GetTable<PersonEx>().Where(_ => _.FirstName == "John").Select(_ => _.ID).Single());
		}

		[InheritanceMapping(Code = 1, Type = typeof(Parent222))]
		[TableName("Parent")]
		public class Parent111
		{
			[MapField(IsInheritanceDiscriminator = true)]
			public int ParentID;
		}

		[MapField("Value1", "Value.ID")]
		public class Parent222 : Parent111
		{
			[MapIgnore]
			public Value111 Value;
		}

		public class Value111
		{
			public int ID;
		}

		[Test]
		public void InheritanceMappingIssueTest()
		{
			using (var db = new TestDbManager())
			{
				var q1 = db.GetTable<Parent222>();
				var q  = q1.Where(_ => _.Value.ID == 1);

				var sql = ((Table<Parent222>)q).SqlText;
				Assert.IsNotEmpty(sql);
			}
		}

		[Test]
		public void ReferenceNavigation()
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

		[Test]
		public void TypeCastIsChildConditional1()
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.Product.         Select(x => x is Northwind.DiscontinuedProduct ? x : null);
				var expected = db.Product.ToList().Select(x => x is Northwind.DiscontinuedProduct ? x : null);

				var list = result.ToList();

				Assert.Greater(list.Count, 0);
				Assert.AreEqual(expected.Count(), list.Count);
				Assert.IsTrue(list.Except(expected).Count() == 0);
				Assert.IsTrue(list.Contains(null));
			}
		}

		[Test]
		public void TypeCastIsChildConditional2()
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

		[Test]
		public void TypeCastIsChild()
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

		[TableName("Child")]
		class ChildTest14 : IChildTest14
		{
			[PrimaryKey]
			public int ChildID { get; set; }

		}

		T FindById<T>(IQueryable<T> queryable, int id)
			where T : IChildTest14
		{
			return queryable.Where(x => x.ChildID == id).FirstOrDefault();
		}

		#endregion

		[Test]
		public void Test14()
		{
			ForEachProvider(db =>
			{
				var q = db.GetTable<ChildTest14>().Select(c => new ChildTest14() { ChildID = c.ChildID });
				FindById(q, 10);
			});
		}

		[Test]
		public void Test15()
		{
			using (var db = new NorthwindDB())
			{
				var result   = db.DiscontinuedProduct.Select(p => p).ToList();
				var expected =    DiscontinuedProduct.Select(p => p).ToList();

				Assert.That(result.Count, Is.Not.EqualTo(0).And.EqualTo(expected.Count));
			}
		}

		[Test]
		public void Test16()
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

		[TableName("LinqDataTypes")]
		public abstract class InheritanceBase
		{
			public Guid GuidValue { get; set; }

			[MapField("ID")]
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

			[MapField("ID", IsInheritanceDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A; }
			}
		}

		class InheritanceA1 : InheritanceA
		{
			[MapField("ID", IsInheritanceDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A1; }
			}
		}

		class InheritanceA2 : InheritanceA
		{
			[MapField("ID", IsInheritanceDiscriminator = true)]
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
			using (var db = new TestDbManager())
			{
				var list = db.GetTable<InheritanceA>().Where(a => a.Bs.Any()).ToList();
			}
		}

		[Test]
		public void QuerySyntaxSimpleTest()
		{
			ForEachProvider(db =>
			{
				// db.GetTable<Parent111>().OfType<Parent222>().ToList(); - it's work!!!
				(from p in db.GetTable<Parent111>().OfType<Parent222>() select p).ToList();
			});
		}

		[TableName("Person")]
		[InheritanceMapping(Code = 1, Type = typeof(Test17John))]
		[InheritanceMapping(Code = 2, Type = typeof(Test17Tester))]
		public class Test17Person
		{
			[MapField(IsInheritanceDiscriminator = true)]
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

		[Test]
		public void Test17()
		{
			ForEachProvider(context =>
			{
				if (context is TestDbManager)
				{
					var db = (TestDbManager)context;
					db.GetTable<Test17Person>().OfType<Test17John>().ToList();
					Assert.False(db.LastQuery.ToLowerInvariant().Contains("lastname"), "Why select LastName field??");
				}
			});
		}

		[TableName("Person")]
		[InheritanceMapping(Code = Gender.Male,   Type = typeof(Test18Male))]
		[InheritanceMapping(Code = Gender.Female, Type = typeof(Test18Female))]
		public class Test18Person
		{
			[PrimaryKey, NonUpdatable(IsIdentity = true, OnInsert = true, OnUpdate = true), SequenceName("PERSONID")]
			public int    PersonID { get; set; }
			[MapField(IsInheritanceDiscriminator = true)]
			public Gender Gender   { get; set; }
		}

		public class Test18Male : Test18Person
		{
			public string FirstName { get; set; }
		}

		public class Test18Female : Test18Person
		{
			public string FirstName { get; set; }
			public string LastName  { get; set; }
		}

		[Test]
		public void Test18()
		{
			ForEachProvider(db =>
			{
				var ids = Enumerable.Range(0, 10).ToList();
				var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

				var list = q.Distinct().OfType<Test18Female>().ToList();
			});
		}

		[Test]
		public void Test19()
		{
			ForEachProvider(db =>
			{
				var ids = Enumerable.Range(0, 10).ToList();
				var q   =
					from p1 in db.GetTable<Test18Person>()
					where ids.Contains(p1.PersonID)
					join p2 in db.GetTable<Test18Person>() on p1.PersonID equals p2.PersonID
					select p1;

				IQueryable iq   = q.Distinct();
				var        list = iq.OfType<Test18Female>().ToList();
			});
		}
	}
}
