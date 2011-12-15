using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Data.DataProvider;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SelectManyTest : TestBase
	{
		[Test]
		public void Basic1()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child),
				db.Parent.SelectMany(p => db.Child)));
		}

		[Test]
		public void Basic1_1()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child.SelectMany(t =>    GrandChild)),
				db.Parent.SelectMany(p => db.Child.SelectMany(t => db.GrandChild))));
		}

		[Test]
		public void Basic2()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1)),
				db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1))));
		}

		[Test]
		public void Basic3()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
				db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1))));
		}

		[Test]
		public void Basic4()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _)),
				db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _))));
		}

		[Test]
		public void Basic5()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				   Child.SelectMany(t => t.Parent.GrandChildren),
				db.Child.SelectMany(t => t.Parent.GrandChildren)));
		}

		[Test]
		public void Basic6()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
				db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1))));
		}

		[Test]
		public void Basic61()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3),
				db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3)));
		}

		[Test]
		public void Basic62()
		{
			ForEachProvider(new[] { ProviderName.Access },
				db => AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1))));
		}

		[Test]
		public void Basic7()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children),
				db.Parent.SelectMany(p => p.Children)));
		}

		[Test]
		public void Basic8()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren)),
				db.Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren))));
		}

		[Test]
		public void Basic9()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren)),
				db.Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren))));
		}

		[Test]
		public void Basic10()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				   Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent)),
				db.Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent))));
		}

		[Test]
		public void Basic11()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				   Child
					.GroupBy(o => o.ParentID2)
					.SelectMany(g => g.Select(o => o.ParentID)),
				db.Child
					.GroupBy(o => o.ParentID2)
					.SelectMany(g => db.Child.Where(o => o.ParentID2 == g.Key).Select(o => o.ParentID))));
		}

		[Test]
		public void Test1()
		{
			TestJohn(db =>
			{
				var q = db.Person.Select(p => p);

				return db.Person
					.SelectMany(p1 => q, (p1, p2) => new { p1, p2 })
					.Where     (t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
					.Select    (t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName });
			});
		}

		[Test]
		public void Test11()
		{
			TestJohn(db => db.Person
				.SelectMany(p1 => db.Person.Select(p => p), (p1, p2) => new { p1, p2 })
				.Where     (t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
				.Select    (t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName }));
		}

		[Test]
		public void Test21()
		{
			TestJohn(db =>
				from p1 in from p in db.Person select new { ID1 = p.ID, p.LastName  }
				from p2 in from p in db.Person select new { ID2 = p.ID, p.FirstName }
				from p3 in from p in db.Person select new { ID3 = p.ID, p.LastName  }
				where p1.ID1 == p2.ID2 && p1.LastName == p3.LastName && p1.ID1 == 1
				select new Person { ID = p1.ID1, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test22()
		{
			TestJohn(db =>
				from p1 in from p in db.Person select p
				from p2 in from p in db.Person select p
				from p3 in from p in db.Person select p
				where p1.ID == p2.ID && p1.LastName == p3.LastName && p1.ID == 1
				select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test31()
		{
			TestJohn(db =>
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

		[Test]
		public void Test32()
		{
			ForEachProvider(db =>
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
			});
		}

		[Test]
		public void SubQuery1()
		{
			TestJohn(db =>
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				return 
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID
					select new Person { ID = p1.ID, FirstName = p2.FirstName };
			});
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

		[Test]
		public void SubQuery2()
		{
			ForEachProvider(db =>
			{
				SubQuery2(db);
				SubQuery2(db);
			});
		}

		IQueryable<Person> GetPersonQuery(ITestDataContext db, int id)
		{
			return from p in db.Person where p.ID == id select new Person { ID = p.ID + 1, FirstName = p.FirstName };
		}

		[Test]
		public void SubQuery3()
		{
			TestJohn(db =>
			{
				var q = GetPersonQuery(db, 1);

				return
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID - 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName };
			});
		}

		[Test]
		public void OneParam1()
		{
			TestJohn(db => db.Person.SelectMany(p => db.Person).Where(t => t.ID == 1).Select(t => t));
		}

		[Test]
		public void OneParam2()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t),
				db.Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t)));
		}

		[Test]
		public void OneParam3()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				   Child.SelectMany(p => p.Parent.GrandChildren).Where(t => t.ParentID == 1).Select(t => t),
				db.Child.SelectMany(p => p.Parent.GrandChildren).Where(t => t.ParentID == 1).Select(t => t)));
		}

		[Test]
		public void ScalarQuery()
		{
			TestJohn(db =>
				from p1 in db.Person
				from p2 in (from p in db.Person select p.ID)
				where p1.ID == p2
				select new Person { ID = p2, FirstName = p1.FirstName }
			);
		}

		[Test]
		public void SelectManyLeftJoin1()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c }).Count(),
				(from p in db.Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c }).AsEnumerable().Count()));
		}

		[Test]
		public void SelectManyLeftJoin2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent
				from ch in (from c in    Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
				select ch,
				from p in db.Parent
				from ch in (from c in db.Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
				select ch));
		}

		[Test]
		public void SelectManyLeftJoin3()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from p in Parent
				from ch in Child.DefaultIfEmpty()
				where p.ParentID == ch.ParentID
				select ch,
				from p in db.Parent
				from ch in db.Child.DefaultIfEmpty()
				where p.ParentID == ch.ParentID
				select ch));
		}

		[Test]
		public void SelectManyLeftJoin4()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				from ch in (from c in    Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
				select new { p.ParentID, ch },
				from p in db.Parent
				from ch in (from c in db.Child where p.ParentID == c.ParentID select c).DefaultIfEmpty()
				select new { p.ParentID, ch }));
		}

		[Test]
		public void SelectManyLeftJoinCount()
		{
			var expected =
				from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c };

			ForEachProvider(db => Assert.AreEqual(expected.Count(),
				(from p in db.Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, n = c.ChildID + 1, o = c }).Count()));
		}

		[Test]
		public void TestJoin1()
		{
			ForEachProvider(db => AreEqual(
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
				select p));
		}

		[Test]
		public void Test3()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => Assert.AreEqual(
				(from p in Parent
				 from g in p.GrandChildren
				 from t in Person
				 let c = g.Child
				 select c).Count(),
				(from p in db.Parent
				 from g in p.GrandChildren
				 from t in db.Person
				 let c = g.Child
				 select c).Count()));
		}

		[Test]
		public void Test4()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from p in Parent
				from g in p.GrandChildren
				join c in db.Child on g.ChildID equals c.ChildID
				join t in db.Types on c.ParentID equals t.ID
				select c).Count(),
				(from p in db.Parent
				from g in p.GrandChildren
				join c in db.Child on g.ChildID equals c.ChildID
				join t in db.Types on c.ParentID equals t.ID
				select c).Count()));
		}

		[Test]
		public void Test5()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
			{
				var q3 =
					from p in db.Parent
					from g in db.GrandChild
					from c in db.Parent2
					select g.Child;

				q3.ToList();
			});
		}

		[Test]
		public void Test6()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
			{
				var q3 =
					from p in db.Parent
					from g in db.GrandChild
					from c in db.Parent2
					let r = g.Child
					select g.Child;

				q3.ToList();
			});
		}

		[Test]
		public void Test7()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from p in db.Parent
				from g in p.GrandChildren
				from c in db.Parent2
				let r = g.Child
				where p.ParentID == g.ParentID
				select r,
				from p in db.Parent
				from g in p.GrandChildren
				from c in db.Parent2
				let r = g.Child
				where p.ParentID == g.ParentID
				select r));
		}

		[Test]
		public void Test8()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
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
			});
		}

		[Test]
		public void Test81()
		{
			ForEachProvider(db =>
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
			});
		}

		[Test]
		public void Test9()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
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
			});
		}

		[Test]
		public void Test91()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
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
			});
		}

		/////[Test]
		public void Test92()
		{
			ForEachProvider(db => AreEqual(
				db.Parent
					.SelectMany(c => c.Children, (c, p) => new { c, p, })
					.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
					.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch }),
				db.Parent
					.SelectMany(c => c.Children, (c, p) => new { c, p, })
					.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
					.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch })));
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
