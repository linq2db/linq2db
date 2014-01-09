using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SelectManyTest : TestBase
	{
		[Test, DataContextSource]
		public void Basic1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child),
					db.Parent.SelectMany(p => db.Child));
		}

		[Test, DataContextSource]
		public void Basic1_1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.SelectMany(t =>    GrandChild)),
					db.Parent.SelectMany(p => db.Child.SelectMany(t => db.GrandChild)));
		}

		[Test, DataContextSource]
		public void Basic2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1)));
		}

		[Test, DataContextSource]
		public void Basic3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1)));
		}

		[Test, DataContextSource]
		public void Basic4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _)));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Basic5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.SelectMany(t => t.Parent.GrandChildren),
					db.Child.SelectMany(t => t.Parent.GrandChildren));
		}

		[Test, DataContextSource]
		public void Basic6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1)));
		}

		[Test, DataContextSource]
		public void Basic61(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Basic62(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1)));
		}

		[Test, DataContextSource]
		public void Basic7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children),
					db.Parent.SelectMany(p => p.Children));
		}

		[Test, DataContextSource]
		public void Basic8(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren)),
					db.Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren)));
		}

		[Test, DataContextSource]
		public void Basic9(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren)),
					db.Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren)));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Basic10(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent)),
					db.Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent)));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Basic11(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child
						.GroupBy(o => o.ParentID2)
						.SelectMany(g => g.Select(o => o.ParentID)),
					db.Child
						.GroupBy(o => o.ParentID2)
						.SelectMany(g => db.Child.Where(o => o.ParentID2 == g.Key).Select(o => o.ParentID)));
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Person.Select(p => p);

				TestJohn(db.Person
					.SelectMany(p1 => q, (p1, p2) => new { p1, p2 })
					.Where     (t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
					.Select    (t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName }));
			}
		}

		[Test, DataContextSource]
		public void Test11(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.SelectMany(p1 => db.Person.Select(p => p), (p1, p2) => new { p1, p2 })
					.Where     (t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
					.Select    (t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName }));
		}

		[Test, DataContextSource]
		public void Test21(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in from p in db.Person select new { ID1 = p.ID, p.LastName  }
					from p2 in from p in db.Person select new { ID2 = p.ID, p.FirstName }
					from p3 in from p in db.Person select new { ID3 = p.ID, p.LastName  }
					where p1.ID1 == p2.ID2 && p1.LastName == p3.LastName && p1.ID1 == 1
					select new Person { ID = p1.ID1, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test, DataContextSource]
		public void Test22(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in from p in db.Person select p
					from p2 in from p in db.Person select p
					from p3 in from p in db.Person select p
					where p1.ID == p2.ID && p1.LastName == p3.LastName && p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test, DataContextSource]
		public void Test31(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p in
						from p in
							from p in db.Person
							where p.ID == 1
							select new { p, ID = p.ID + 1 }
						where p.ID == 2
						select new { p, ID = p.ID + 1 }
					where p.ID == 3
					select p.p.p);
		}

		[Test, DataContextSource]
		public void Test32(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in
						from p in
							from p in db.Person
							where p.ID == 1
							select new { p, ID = p.ID + 1 }
						where p.ID == 2
						select new { p, ID = p.ID + 1 }
					where p.ID == 3
					select new { p.p.p };

				var list = q.ToList();

				Assert.AreEqual(1, list.Count);

				var person = list[0].p;

				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
			}
		}

		[Test, DataContextSource]
		public void SubQuery1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				TestJohn(
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID
					select new Person { ID = p1.ID, FirstName = p2.FirstName });
			}
		}

		public void SubQuery2(ITestDataContext db)
		{
			var q1 = from p in db.Person where p.ID == 1 || p.ID == 2 select p;
			var q2 = from p in db.Person where !(p.ID == 2) select p;

			var q = 
				from p1 in q1
				from p2 in q2
				where p1.ID == p2.ID
				select new Person { ID = p1.ID, FirstName = p2.FirstName };

			foreach (var person in q)
			{
				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
			}
		}

		[Test, DataContextSource]
		public void SubQuery2(string context)
		{
			using (var db = GetDataContext(context))
			{
				SubQuery2(db);
				SubQuery2(db);
			}
		}

		IQueryable<Person> GetPersonQuery(ITestDataContext db, int id)
		{
			return from p in db.Person where p.ID == id select new Person { ID = p.ID + 1, FirstName = p.FirstName };
		}

		[Test, DataContextSource]
		public void SubQuery3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = GetPersonQuery(db, 1);

				TestJohn(
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID - 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName });
			}
		}

		[Test, DataContextSource]
		public void OneParam1(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person.SelectMany(p => db.Person).Where(t => t.ID == 1).Select(t => t));
		}

		[Test, DataContextSource]
		public void OneParam2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t),
					db.Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void OneParam3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.SelectMany(p => p.Parent.GrandChildren).Where(t => t.ParentID == 1).Select(t => t),
					db.Child.SelectMany(p => p.Parent.GrandChildren).Where(t => t.ParentID == 1).Select(t => t));
		}

		[Test, DataContextSource]
		public void ScalarQuery(string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
					from p2 in (from p in db.Person select p.ID)
					where p1.ID == p2
					select new Person { ID = p2, FirstName = p1.FirstName });
		}

		[Test, DataContextSource]
		public void SelectManyLeftJoin1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, o = c }).Count(),
					(from p in db.Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, o = c }).AsEnumerable().Count());
		}

		[Test, DataContextSource]
		public void SelectManyLeftJoin2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					from ch in (from c in    Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
					select ch,
					from p in db.Parent
					from ch in (from c in db.Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
					select ch);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void SelectManyLeftJoin3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					from ch in Child.DefaultIfEmpty()
					where p.ParentID == ch.ParentID
					select ch
					,
					from p in db.Parent
					from ch in db.Child.DefaultIfEmpty()
					where p.ParentID == ch.ParentID
					select ch);
		}

		[Test, DataContextSource]
		public void SelectManyLeftJoin4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					from ch in (from c in    Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
					select new { p.ParentID, ch }
					,
					from p in db.Parent
					from ch in (from c in db.Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
					select new { p.ParentID, ch });
		}

		[Test, DataContextSource]
		public void SelectManyLeftJoinCount(string context)
		{
			var expected =
				from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c };

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					expected.Count(),
					(from p in db.Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, n = c.ChildID + 1, o = c }).Count());
		}

		[Test, DataContextSource]
		public void TestJoin1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in 
						from p in Parent
						from g in p.GrandChildren
						join c in Child on g.ChildID equals c.ChildID
						join t in Types on c.ParentID equals t.ID
						select c
					join t in Person on p.ParentID equals t.ID
					select p,
					from p in 
						from p in db.Parent
						from g in p.GrandChildren
						join c in db.Child on g.ChildID equals c.ChildID
						join t in db.Types on c.ParentID equals t.ID
						select c
					join t in db.Person on p.ParentID equals t.ID
					select p);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in Parent
					 from g in p.GrandChildren
					 from t in Person
					 let c = g.Child
					 select c).Count(),
					(from p in db.Parent
					 from g in p.GrandChildren
					 from t in db.Person
					 let c = g.Child
					 select c).Count());
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in Parent
					from g in p.GrandChildren
					join c in db.Child on g.ChildID equals c.ChildID
					join t in db.Types on c.ParentID equals t.ID
					select c).Count(),
					(from p in db.Parent
					from g in p.GrandChildren
					join c in db.Child on g.ChildID equals c.ChildID
					join t in db.Types on c.ParentID equals t.ID
					select c).Count());
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q3 =
					from p in db.Parent
					from g in db.GrandChild
					from c in db.Parent2
					select g.Child;

				q3.ToList();
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q3 =
					from p in db.Parent
					from g in db.GrandChild
					from c in db.Parent2
					let r = g.Child
					select g.Child;

				q3.ToList();
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in db.Parent
					from g in p.GrandChildren
					from c in db.Parent2
					let r = g.Child
					where p.ParentID == g.ParentID
					select r
					,
					from p in db.Parent
					from g in p.GrandChildren
					from c in db.Parent2
					let r = g.Child
					where p.ParentID == g.ParentID
					select r);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test8(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from p in
						from p in db.Parent
						join c in db.GrandChild on p.ParentID equals c.ParentID
						select p
					from g in p.GrandChildren
					from c in db.Parent2
					let r = g.Child
					where
						p.ParentID == g.ParentID
					select r;

				q2.ToList();
			}
		}

		[Test, DataContextSource]
		public void Test81(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from p in
						from p in db.Parent
						join c in db.GrandChild on p.ParentID equals c.ParentID
						select p
					from g in p.GrandChildren
					//from c in db.Parent2
					let r = g.Child
					where
						p.ParentID == g.ParentID
					select r;

				q2.ToList();
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test9(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = db.Types.Where(_ => _.ID > 1).Where(_ => _.ID > 2);

				var q2 =
					from p in db.Parent
					join c in db.GrandChild on p.ParentID equals c.ParentID
					join t in q1            on c.ParentID equals t.ID
					where p.ParentID == 1
					select p;

				q2 = q2.Distinct().OrderBy(_ => _.ParentID);

				var q3 =
					from p in q2
					from g in p.GrandChildren
					from c in db.Parent2
					let r = g.Child
					where
						p.ParentID == g.ParentID && g.ParentID == c.ParentID
					select r;

				q3 = q3.Where(_ => _.ChildID == 1);

				q3.ToList();
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void Test91(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from p in db.Parent
					join c in db.GrandChild on p.ParentID equals c.ParentID
					where p.ParentID == 1
					select p;

				q2 = q2.Distinct();

				var q3 =
					from p in q2
					from g in p.GrandChildren
					let r = g.Child
					where
						p.ParentID == g.ParentID
					select r;

				q3.ToList();
			}
		}

		/////[Test, DataContextSource]
		public void Test92(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					db.Parent
						.SelectMany(c => c.Children, (c, p) => new { c, p, })
						.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
						.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch }),
					db.Parent
						.SelectMany(c => c.Children, (c, p) => new { c, p, })
						.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
						.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch }));
		}

		void Foo(Expression<Func<object[],object>> func)
		{
			/*
				ParameterExpression ps;
				Expression.Lambda<Func<object[],object>>(
					Expression.Add(
						Expression.Convert(
							Expression.ArrayIndex(
								ps = Expression.Parameter(typeof(object[]), "p"),
								Expression.Constant(0, typeof(int))),
							typeof(string)),
						Expression.Convert(
							Expression.Convert(
								Expression.ArrayIndex(
									ps,
									Expression.Constant(1, typeof(int))),
								typeof(int)),
							typeof(object)),
						(MethodInfo)methodof(string.Concat)),
					new ParameterExpression[] { ps });
			*/
		}

		Dictionary<string,string> _dic = new Dictionary<string,string>();

		void Bar()
		{
			Foo(p => (string)p[0] + (int)p[1]);
		}

		//[Test]
		public void Test___()
		{
			Bar();
		}
	}
}
