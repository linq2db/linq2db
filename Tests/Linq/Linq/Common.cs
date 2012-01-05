using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Common : TestBase
	{
		[Test]
		public void AsQueryable([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent from ch in Child select p,
					from p in db.Parent from ch in db.Child.AsQueryable() select p);
		}

		[Test]
		public void Convert([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from ch in                         Child                select p,
					from p in db.Parent from ch in ((IEnumerable<Child>)db.Child).AsQueryable() select p);
		}

		[Test]
		public void NewCondition([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1 != null ? p.Value1 : 100 },
					from p in db.Parent select new { Value = p.Value1 != null ? p.Value1 : 100 });
		}

		[Test]
		public void NewCoalesce([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1 ?? 100 },
					from p in db.Parent select new { Value = p.Value1 ?? 100 });
		}

		[Test]
		public void CoalesceNew([DataContexts] string context)
		{
			Child ch = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select ch ?? new Child { ParentID = p.ParentID },
					from p in db.Parent select ch ?? new Child { ParentID = p.ParentID });
		}

		[Test]
		public void ScalarCondition([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 != null ? p.Value1 : 100,
					from p in db.Parent select p.Value1 != null ? p.Value1 : 100);
		}

		[Test]
		public void ScalarCoalesce([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 ?? 100,
					from p in db.Parent select p.Value1 ?? 100);
		}

		[Test]
		public void ExprCoalesce([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select (p.Value1 ?? 100) + 50,
					from p in db.Parent select (p.Value1 ?? 100) + 50);
		}

		static int GetDefault1()
		{
			return 100;
		}

		[Test]
		public void ClientCoalesce1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 ?? GetDefault1(),
					from p in db.Parent select p.Value1 ?? GetDefault1());
		}

		static int GetDefault2(int n)
		{
			return n;
		}

		[Test]
		public void ClientCoalesce2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 ?? GetDefault2(p.ParentID),
					from p in db.Parent select p.Value1 ?? GetDefault2(p.ParentID));
		}

		[Test]
		public void PreferServerFunc1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select p.FirstName.Length,
					from p in db.Person select p.FirstName.Length);
		}

		[Test]
		public void PreferServerFunc2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select p.FirstName.Length + "".Length,
					from p in db.Person select p.FirstName.Length + "".Length);
		}

		class Test
		{
			class Entity
			{
				public Test TestField = null;
			}

			public Test TestClosure(ITestDataContext db)
			{
				return db.Person.Select(_ => new Entity { TestField = this }).First().TestField;
			}
		}

		[Test]
		public void ClosureTest([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreNotEqual(
					new Test().TestClosure(db),
					new Test().TestClosure(db));
		}

		[Test]
		public void ExecuteTest()
		{
			using (var db = new NorthwindDB())
			{
				var emp = db.Employee;

				Expression<Func<int>> m = () => emp.Count();

				var exp = Expression.Call(((MethodCallExpression)m.Body).Method, emp.Expression);

				var results = (int)((IQueryable)emp).Provider.Execute(exp);
			}
		}

		class MyClass
		{
			public int ID;

			public override bool Equals(object obj)
			{
				return ((MyClass)obj).ID == ID;
			}

			public override int GetHashCode()
			{
				return ID;
			}
		}

		[Test]
		public void NewObjectTest1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { ID = new MyClass { ID = p.ParentID } } into p1
					where p1.ID.ID == 1
					select p1,
					from p in db.Parent
					select new { ID = new MyClass { ID = p.ParentID } } into p1
					where p1.ID.ID == 1
					select p1);
		}

		[Test]
		public void NewObjectTest2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { ID = new MyClass { ID = p.ParentID } } into p
					join j in
						from c in Child
						select new { ID = new MyClass { ID = c.ParentID } }
					on p.ID.ID equals j.ID.ID
					where p.ID.ID == 1
					select p,
					from p in db.Parent
					select new { ID = new MyClass { ID = p.ParentID } } into p
					join j in
						from c in db.Child
						select new { ID = new MyClass { ID = c.ParentID } }
					on p.ID.ID equals j.ID.ID
					where p.ID.ID == 1
					select p);
		}

		public Table<Person> People2(DbManager db)
		{
			return db.GetTable<Person>();
		}

		[Test]
		public void TableAsMethod()
		{
			using (var db = new TestDbManager())
			{
				var q =
					from d in db.Patient
					from p in People2(db)
					select p;

				q.ToList();

				q =
					from d in db.Patient
					from p in People2(db)
					select p;

				q.ToList();
			}
		}

		[Test]
		public void TableAsExtensionMethod()
		{
			using (var db = new TestDbManager())
			{
				var q =
					from d in db.Patient
					from p in db.People()
					select p;

				q.ToList();
			}
		}

		[Test]
		public void Condition1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { Name = !string.IsNullOrEmpty(p.FirstName) ? p.FirstName : !string.IsNullOrEmpty(p.MiddleName) ? p.MiddleName : p.LastName },
					from p in db.Person select new { Name = !string.IsNullOrEmpty(p.FirstName) ? p.FirstName : !string.IsNullOrEmpty(p.MiddleName) ? p.MiddleName : p.LastName });
		}

		[Test]
		public void Condition2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { Name = !p.FirstName.IsNullOrEmpty() ? p.FirstName : !p.MiddleName.IsNullOrEmpty() ? p.MiddleName : p.LastName },
					from p in db.Person select new { Name = !p.FirstName.IsNullOrEmpty() ? p.FirstName : !p.MiddleName.IsNullOrEmpty() ? p.MiddleName : p.LastName });
		}

		enum PersonID
		{
			Person1 = 1,
			Person2 = 2
		}

		[Test]
		public void ConvertEnum1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.ID == (int)PersonID.Person1 select p,
					from p in db.Person where p.ID == (int)PersonID.Person1 select p);
		}

		[Test]
		public void ConvertEnum2([DataContexts] string context)
		{
			var id = PersonID.Person1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.ID == (int)id select p,
					from p in db.Person where p.ID == (int)id select p);
		}

		[Test]
		public void GroupByUnion1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in (
						from c in Child
						where c.ParentID < 4
						select new { c.ParentID, ID = c.ChildID })
					.Concat(
						from g in GrandChild
						where g.ParentID >= 4
						select new { ParentID = g.ParentID ?? 0, ID = g.GrandChildID ?? 0 })
					group t by t.ParentID into gr
					select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ID) } into tt
					where tt.Sum != 0
					select tt
					,
					from t in (
						from c in db.Child
						where c.ParentID < 4
						select new { c.ParentID, ID = c.ChildID })
					.Concat(
						from g in db.GrandChild
						where g.ParentID >= 4
						select new { ParentID = g.ParentID ?? 0, ID = g.GrandChildID ?? 0 })
					group t by t.ParentID into gr
					select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ID) } into tt
					where tt.Sum != 0
					select tt);
		}

		[Test]
		public void GroupByUnion2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qe1 =
					from t in (
						from c in Child
						where c.ParentID < 4
						select new { c.ParentID, ID = c.ChildID })
					.Concat(
						from g in GrandChild
						where g.ParentID >= 4
						select new { ParentID = g.ParentID ?? 0, ID = g.GrandChildID ?? 0 })
					group t by t.ParentID into gr
					select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ID) } into tt
					where tt.Sum != 0
					select tt;

				var qe2 =
					from p in Parent
						join tt in qe1 on p.ParentID equals tt.ParentID into gr
						from tt in gr.DefaultIfEmpty()
					select new { p.ParentID };

				var qr1 =
					from t in (
						from c in db.Child
						where c.ParentID < 4
						select new { c.ParentID, ID = c.ChildID })
					.Concat(
						from g in db.GrandChild
						where g.ParentID >= 4
						select new { ParentID = g.ParentID ?? 0, ID = g.GrandChildID ?? 0 })
					group t by t.ParentID into gr
					select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ID) } into tt
					where tt.Sum != 0
					select tt;

				var qr2 =
					from p in db.Parent
						join tt in qr1 on p.ParentID equals tt.ParentID into gr
						from tt in gr.DefaultIfEmpty()
					select new { p.ParentID };

				AreEqual(qe2, qr2);
			}
		}

		[Test]
		public void GroupByLeftJoin1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join tt in
							from t in Child
							group t by t.ParentID into gr
							select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ChildID) } into tt
							where tt.Sum != 0
							select tt
						on p.ParentID equals tt.ParentID into gr
						from tt in gr.DefaultIfEmpty()
					select p.ParentID,
					from p in db.Parent
						join tt in
							from t in db.Child
							group t by t.ParentID into gr
							select new { ParentID = gr.Key, Sum = gr.Sum(i => i.ChildID) } into tt
							where tt.Sum != 0
							select tt
						on p.ParentID equals tt.ParentID into gr
						from tt in gr.DefaultIfEmpty()
					select p.ParentID);
		}
	}

	static class Extender
	{
		public static Table<Person> People(this DbManager db)
		{
			return db.GetTable<Person>();
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}
	}
}
