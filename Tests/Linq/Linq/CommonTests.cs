using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class CommonTests : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void CheckNullTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					where p.ParentID == 6
					select new
					{
						Root   = p,
						ChildA = db.GrandChild.FirstOrDefault(a => a.ParentID == p.ParentID),
						ChildB = db.Child.     FirstOrDefault(a => a.ParentID == p.ParentID),
					};

				var list = q.ToList();

				Assert.That(list.Count,     Is.EqualTo(1));
				Assert.That(list[0].ChildA, Is.Null);
				Assert.That(list[0].ChildB, Is.Not.Null);
			}
		}

		[Test, DataContextSource]
		public void AsQueryable(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent from ch in Child select p,
					from p in db.Parent from ch in db.Child.AsQueryable() select p);
		}

		[Test, DataContextSource]
		public void Convert(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from ch in                         Child                select p,
					from p in db.Parent from ch in ((IEnumerable<Child>)db.Child).AsQueryable() select p);
		}

		[Test, DataContextSource]
		public void NewCondition(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1 != null ? p.Value1 : 100 },
					from p in db.Parent select new { Value = p.Value1 != null ? p.Value1 : 100 });
		}

		[Test, DataContextSource]
		public void NewCoalesce(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1 ?? 100 },
					from p in db.Parent select new { Value = p.Value1 ?? 100 });
		}

		[Test, DataContextSource]
		public void CoalesceNew(string context)
		{
			Child ch = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select ch ?? new Child { ParentID = p.ParentID },
					from p in db.Parent select ch ?? new Child { ParentID = p.ParentID });
		}

		[Test, DataContextSource]
		public void ScalarCondition(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 != null ? p.Value1 : 100,
					from p in db.Parent select p.Value1 != null ? p.Value1 : 100);
		}

		[Test, DataContextSource]
		public void ScalarCoalesce(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 ?? 100,
					from p in db.Parent select p.Value1 ?? 100);
		}

		[Test, DataContextSource]
		public void ExprCoalesce(string context)
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

		[Test, DataContextSource]
		public void ClientCoalesce1(string context)
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

		[Test, DataContextSource]
		public void ClientCoalesce2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Value1 ?? GetDefault2(p.ParentID),
					from p in db.Parent select p.Value1 ?? GetDefault2(p.ParentID));
		}

		[Test, DataContextSource]
		public void CoalesceLike(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person
					where
						(p.FirstName == null ? (bool?)null : (bool?)p.FirstName.StartsWith("Jo")) == null ?
							false :
							(p.FirstName == null ? (bool?)null : p.FirstName.StartsWith("Jo")).Value
					select p,
					from p in db.Person
					where
						(p.FirstName == null ? (bool?)null : (bool?)p.FirstName.StartsWith("Jo")) == null ?
							false :
							(p.FirstName == null ? (bool?)null : p.FirstName.StartsWith("Jo")).Value
					select p);
		}

		[Test, DataContextSource]
		public void PreferServerFunc1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select p.FirstName.Length,
					from p in db.Person select p.FirstName.Length);
		}

		[Test, DataContextSource]
		public void PreferServerFunc2(string context)
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

		[Test, DataContextSource]
		public void ClosureTest(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreNotEqual(
					new Test().TestClosure(db),
					new Test().TestClosure(db));
		}

		[Test, NorthwindDataContext]
		public void ExecuteTest(string context)
		{
			using (var db = new NorthwindDB(context))
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

		[Test, DataContextSource]
		public void NewObjectTest1(string context)
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

		[Test, DataContextSource]
		public void NewObjectTest2(string context)
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

		public ITable<Person> People2(TestDataConnection db)
		{
			return db.GetTable<Person>();
		}

		[Test]
		public void TableAsMethod()
		{
			using (var db = new TestDataConnection())
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
			using (var db = new TestDataConnection())
			{
				var q =
					from d in db.Patient
					from p in db.People()
					select p;

				q.ToList();
			}
		}

		[Test, DataContextSource]
		public void Condition1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { Name = !string.IsNullOrEmpty(p.FirstName) ? p.FirstName : !string.IsNullOrEmpty(p.MiddleName) ? p.MiddleName : p.LastName },
					from p in db.Person select new { Name = !string.IsNullOrEmpty(p.FirstName) ? p.FirstName : !string.IsNullOrEmpty(p.MiddleName) ? p.MiddleName : p.LastName });
		}

		[Test, DataContextSource]
		public void Condition2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { Name = !p.FirstName.IsNullOrEmpty() ? p.FirstName : !p.MiddleName.IsNullOrEmpty() ? p.MiddleName : p.LastName },
					from p in db.Person select new { Name = !p.FirstName.IsNullOrEmpty() ? p.FirstName : !p.MiddleName.IsNullOrEmpty() ? p.MiddleName : p.LastName });
		}

		[Test, DataContextSource]
		public void Concat1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where string.Concat(p.FirstName, " I") == "John I" select p.FirstName,
					from p in db.Person where string.Concat(p.FirstName, " I") == "John I" select p.FirstName);
		}

		[Test, DataContextSource]
		public void Concat2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where string.Concat(p.FirstName, " ", 1) == "John 1" select p.FirstName,
					from p in db.Person where string.Concat(p.FirstName, " ", 1) == "John 1" select p.FirstName);
		}

		[Test, DataContextSource]
		public void Concat3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where string.Concat(p.FirstName, " ", 1, 2) == "John 12" select p.FirstName,
					from p in db.Person where string.Concat(p.FirstName, " ", 1, 2) == "John 12" select p.FirstName);
		}

		enum PersonID
		{
			Person1 = 1,
			Person2 = 2
		}

		[Test, DataContextSource]
		public void ConvertEnum1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.ID == (int)PersonID.Person1 select p,
					from p in db.Person where p.ID == (int)PersonID.Person1 select p);
		}

		[Test, DataContextSource]
		public void ConvertEnum2(string context)
		{
			var id = PersonID.Person1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.ID == (int)id select p,
					from p in db.Person where p.ID == (int)id select p);
		}

		[Test, DataContextSource]
		public void GroupByUnion1(string context)
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

		[Test, DataContextSource]
		public void GroupByUnion2(string context)
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

		[Test, DataContextSource]
		public void GroupByLeftJoin1(string context)
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

		void ProcessItem(ITestDataContext db, int id)
		{
			var hashQuery1 = Parent.Where(t => t.ParentID == id);

			var groups1 = Child
				.Where(p => hashQuery1.Any(e => e.ParentID == p.ParentID))
				.GroupBy(e => e.ParentID)
				.Select(g => g.Key);

			var hashQuery2 = db.Parent.Where(t => t.ParentID == id);

			var groups2 = db.Child
				.Where(p => hashQuery2.Any(e => e.ParentID == p.ParentID))
				.GroupBy(e => e.ParentID)
				.Select(g => g.Key);

			AreEqual(groups1, groups2);
		}

		[Test, DataContextSource]
		public void ParameterTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				ProcessItem(db, 1);
				ProcessItem(db, 2);
			}
		}

		[Table("Person")]
		public class PersonTest
		{
			[Column("FirstName"), PrimaryKey]
			public string ID;
		}

		int _i;

		string GetCustKey()
		{
			return ++_i % 2 == 0 ? "John" : null;
		}

		[Test, DataContextSource]
		public void Issue288Test(string context)
		{
			_i = 0;

			using (var db = GetDataContext(context))
			{
				var test = db.GetTable<PersonTest>().FirstOrDefault(p => p.ID == GetCustKey());

				Assert.That(test, Is.Null);
			}

			Assert.That(_i, Is.EqualTo(1));

			using (var db = GetDataContext(context))
			{
				var test = db.GetTable<PersonTest>().FirstOrDefault(p => p.ID == GetCustKey());

				Assert.That(test, Is.Not.Null);
			}

			Assert.That(_i, Is.EqualTo(2));
		}

		class User
		{
			public string FirstName;
			public int?   Status;
		}

		// https://github.com/linq2db/linq2db/issues/191
		[Test, DataContextSource]
		public void Issue191Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				string firstName = null;
				int?   status    = null;

				var str = db.GetTable<User>()
					.Where(user =>
						user.Status == status &&
						(string.IsNullOrEmpty(firstName) || user.FirstName.Contains(firstName)))
					.ToString();

				Debug.WriteLine(str);
			}
		}
	}

	static class Extender
	{
		public static ITable<Person> People(this DataConnection db)
		{
			return db.GetTable<Person>();
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}
	}
}
