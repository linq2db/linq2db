﻿using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectManyTests : TestBase
	{
		[Test]
		public void Basic1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child),
					db.Parent.SelectMany(p => db.Child));
		}

		[Test]
		public void Basic1_1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.SelectMany(t =>    GrandChild)),
					db.Parent.SelectMany(p => db.Child.SelectMany(t => db.GrandChild)));
		}

		[Test]
		public void Basic2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1)));
		}

		[Test]
		public void Basic3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => _ > 1)));
		}

		[Test]
		public void Basic4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _)),
					db.Parent.SelectMany(p => db.Child.Select(_ => _.ParentID + 1).Where(_ => p.ParentID == _)));
		}

		[Test]
		public void Basic5([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.SelectMany(t => t.Parent!.GrandChildren),
					db.Child.SelectMany(t => t.Parent!.GrandChildren));
		}

		[Test]
		public void Basic6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1)));
		}

		[Test]
		public void Basic61([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + 1).Where(_ => _ > 1 || _ > 2)).Where(_ => _ > 0 || _ > 3));
		}

		[Test]
		public void Basic62([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1)),
					db.Parent.SelectMany(p => p.Children.Select(_ => _.ParentID + p.ParentID).Where(_ => _ > 1)));
		}

		[Test]
		public void Basic7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children),
					db.Parent.SelectMany(p => p.Children));
		}

		[Test]
		public void Basic8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren)),
					db.Parent.SelectMany(p => p.Children.SelectMany(t => t.GrandChildren)));
		}

		[Test]
		public void Basic9([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren)),
					db.Parent.SelectMany(p => p.Children.SelectMany(t => p.GrandChildren)));
		}

		[Test]
		public void Basic10([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent)),
					db.Child.GroupBy(o => o.ParentID2).SelectMany(g => g.Select(o => o.Parent)));
		}

		[Test]
		public void Basic11([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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

		[Test]
		public void Test1([DataSources] string context)
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

		[Test]
		public void Test11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.SelectMany(p1 => db.Person.Select(p => p), (p1, p2) => new { p1, p2 })
					.Where     (t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
					.Select    (t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName }));
		}

		[Test]
		public void Test21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in from p in db.Person select new { ID1 = p.ID, p.LastName  }
					from p2 in from p in db.Person select new { ID2 = p.ID, p.FirstName }
					from p3 in from p in db.Person select new { ID3 = p.ID, p.LastName  }
					where p1.ID1 == p2.ID2 && p1.LastName == p3.LastName && p1.ID1 == 1
					select new Person { ID = p1.ID1, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test22([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in from p in db.Person select p
					from p2 in from p in db.Person select p
					from p3 in from p in db.Person select p
					where p1.ID == p2.ID && p1.LastName == p3.LastName && p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test31([DataSources(TestProvName.AllClickHouse)] string context)
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

		[Test]
		public void Test32([DataSources(TestProvName.AllClickHouse)] string context)
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

				Assert.That(list, Has.Count.EqualTo(1));

				var person = list[0].p;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(person.ID, Is.EqualTo(1));
					Assert.That(person.FirstName, Is.EqualTo("John"));
				}
			}
		}

		[Test]
		public void SubQuery1([DataSources] string context)
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

		private void SubQuery2(ITestDataContext db)
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(person.ID, Is.EqualTo(1));
					Assert.That(person.FirstName, Is.EqualTo("John"));
				}
			}
		}

		[Test]
		public void SubQuery2([DataSources] string context)
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

		[Test]
		public void SubQuery3([DataSources] string context)
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

		[Test]
		public void OneParam1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person.SelectMany(p => db.Person).Where(t => t.ID == 1).Select(t => t));
		}

		[Test]
		public void OneParam2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t),
					db.Parent.SelectMany(p => p.Children).Where(t => t.ParentID == 1).Select(t => t));
		}

		[Test]
		public void OneParam3([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.SelectMany(p => p.Parent!.GrandChildren).Where(t => t.ParentID == 1).Select(t => t),
					db.Child.SelectMany(p => p.Parent!.GrandChildren).Where(t => t.ParentID == 1).Select(t => t));
		}

		[Test]
		public void ScalarQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
					from p2 in (from p in db.Person select p.ID)
					where p1.ID == p2
					select new Person { ID = p2, FirstName = p1.FirstName });
		}

		[Test]
		public void SelectManyLeftJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					(from p in db.Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, o = c }).AsEnumerable().Count(), Is.EqualTo((from p in Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, o = c }).Count()));
		}

		[Test]
		public void SelectManyLeftJoin2([DataSources] string context)
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

		[Test]
		public void SelectManyLeftJoin3([DataSources(TestProvName.AllAccess)] string context)
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

		[Test]
		public void SelectManyLeftJoin4([DataSources] string context)
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

		[Test]
		public void SelectManyLeftJoinCount([DataSources] string context)
		{
			var expected =
				from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c };

			using (var db = GetDataContext(context))
				Assert.That(
					(from p in db.Parent
					from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
					select new { p.Value1, n = c.ChildID + 1, o = c }).Count(), Is.EqualTo(expected.Count()));
		}

		[Test]
		public void TestJoin1([DataSources] string context)
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

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					(from p in db.Parent
					 from g in p.GrandChildren
					 from t in db.Person
					 let c = g.Child
					 select c).Count(), Is.EqualTo((from p in Parent
					 from g in p.GrandChildren
					 from t in Person
					 let c = g.Child
					 select c).Count()));
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					(from p in db.Parent
					from g in p.GrandChildren
					join c in db.Child on g.ChildID equals c.ChildID
					join t in db.Types on c.ParentID equals t.ID
					select c).Count(), Is.EqualTo((from p in Parent
					from g in p.GrandChildren
					join c in Child on g.ChildID equals c.ChildID
					join t in Types on c.ParentID equals t.ID
					select c).Count()));
		}

		[Test]
		public void Test5([DataSources(TestProvName.AllAccess)] string context)
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

		[Test]
		public void Test6([DataSources(TestProvName.AllAccess)] string context)
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

		[Test]
		public void Test7([DataSources] string context)
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

		[Test]
		public void Test8([DataSources] string context)
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

		[Test]
		public void Test81([DataSources] string context)
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

		[Test]
		public void Test9([DataSources] string context)
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

		[Test]
		public void Test91([DataSources] string context)
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

		/////[Test]
		//public void Test92([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			db.Parent
		//				.SelectMany(c => c.Children, (c, p) => new { c, p, })
		//				.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
		//				.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch }),
		//			db.Parent
		//				.SelectMany(c => c.Children, (c, p) => new { c, p, })
		//				.Select(_ => new { _.c, p = new Child { ParentID = _.c.ParentID, ChildID = _.p.ChildID } })
		//				.SelectMany(ch => ch.p.GrandChildren, (ch, t) => new { t, ch }));
		//}

		[Test]
		public void Test157_1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = db.Employee
					.SelectMany(
						query => db.Employee.Where(join => (query.ReportsTo == join.EmployeeID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							Order             = root.Order,
							EmployeeTerritory = root.EmployeeTerritory,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = bind
						})
					.SelectMany(
						query => db.Order.Where(join => (query.EmployeeID == join.EmployeeID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							EmployeeTerritory = root.EmployeeTerritory,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = root.ReportsToEmployee,
							Order             = bind
						})
					.SelectMany(
						query => db.OrderDetail.Where(join => (query.Order!.OrderID == join.OrderID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							EmployeeTerritory = root.EmployeeTerritory,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = root.ReportsToEmployee,
							Order = new Northwind.Order
							{
								OrderID      = root.Order!.OrderID,
								EmployeeID   = root.Order.EmployeeID,
								OrderDate    = root.Order.OrderDate,
								RequiredDate = root.Order.RequiredDate,
								ShippedDate  = root.Order.ShippedDate,
								ShipVia      = root.Order.ShipVia,
								Freight      = root.Order.Freight,
								Shipper      = root.Order.Shipper,
								Employee     = root.Order.Employee,
								Customer     = root.Order.Customer,
								OrderDetail  = bind
							}
						})
					.SelectMany(
						query => db.EmployeeTerritory.Where(join => (query.EmployeeID == join.EmployeeID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							Order             = root.Order,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = root.ReportsToEmployee,
							EmployeeTerritory = bind
						})
					.SelectMany(
						query => db.Territory.Where(join => (query.EmployeeTerritory!.TerritoryID == join.TerritoryID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							Order             = root.Order,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = root.ReportsToEmployee,
							EmployeeTerritory = new Northwind.EmployeeTerritory
							{
								EmployeeID = root.EmployeeTerritory!.EmployeeID,
								Employee   = root.EmployeeTerritory.Employee,
								Territory  = bind
							}
						})
					.SelectMany(
						query => db.Region.Where(join => (query.EmployeeTerritory!.Territory!.RegionID == join.RegionID)).DefaultIfEmpty(),
						(root, bind) => new Northwind.Employee
						{
//							Employee2         = root.Employee2,
							Order             = root.Order,
							EmployeeID        = root.EmployeeID,
							BirthDate         = root.BirthDate,
							HireDate          = root.HireDate,
							ReportsTo         = root.ReportsTo,
							ReportsToEmployee = root.ReportsToEmployee,
							EmployeeTerritory = new Northwind.EmployeeTerritory
							{
								EmployeeID = root.EmployeeTerritory!.EmployeeID,
								Employee   = root.EmployeeTerritory.Employee,
								Territory  = new Northwind.Territory
								{
									EmployeeTerritory = root.EmployeeTerritory.Territory!.EmployeeTerritory,
									RegionID          = root.EmployeeTerritory.Territory.RegionID,
									Region            = bind
								}
							}
						})
					.Where(e => e.EmployeeID == 5)
					;

				q.ToList();
			}
		}

		[Test]
		public void Test157_2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = db.Employee
					.SelectMany(
						query => db.Employee.Where(join => (query.ReportsTo == join.EmployeeID)).DefaultIfEmpty(),
						(root1, bind1) => new
						{
//							Employee2         = root1.Employee2,
							Order             = root1.Order,
							EmployeeTerritory = root1.EmployeeTerritory,
							EmployeeID        = root1.EmployeeID,
							BirthDate         = root1.BirthDate,
							HireDate          = root1.HireDate,
							ReportsTo         = root1.ReportsTo,
							ReportsToEmployee = bind1
						})
					.SelectMany(
						query => db.Order.Where(join => (query.EmployeeID == join.EmployeeID)).DefaultIfEmpty(),
						(root2, bind2) => new
						{
//							Employee2         = root2.Employee2,
							EmployeeTerritory = root2.EmployeeTerritory,
							EmployeeID        = root2.EmployeeID,
							BirthDate         = root2.BirthDate,
							HireDate          = root2.HireDate,
							ReportsTo         = root2.ReportsTo,
							ReportsToEmployee = root2.ReportsToEmployee,
							Order             = bind2
						})
					.SelectMany(
						query => db.OrderDetail.Where(join => (query.Order!.OrderID == join.OrderID)).DefaultIfEmpty(),
						(root3, bind3) => new
						{
//							Employee2         = root3.Employee2,
							EmployeeTerritory = root3.EmployeeTerritory,
							EmployeeID        = root3.EmployeeID,
							BirthDate         = root3.BirthDate,
							HireDate          = root3.HireDate,
							ReportsTo         = root3.ReportsTo,
							ReportsToEmployee = root3.ReportsToEmployee,
							Order = new
							{
								OrderID      = root3.Order!.OrderID,
								EmployeeID   = root3.Order.EmployeeID,
								OrderDate    = root3.Order.OrderDate,
								RequiredDate = root3.Order.RequiredDate,
								ShippedDate  = root3.Order.ShippedDate,
								ShipVia      = root3.Order.ShipVia,
								Freight      = root3.Order.Freight,
								Shipper      = root3.Order.Shipper,
								Employee     = root3.Order.Employee,
								Customer     = root3.Order.Customer,
								OrderDetail  = bind3
							}
						})
					.SelectMany(
						query => db.EmployeeTerritory.Where(join => (query.EmployeeID == join.EmployeeID)).DefaultIfEmpty(),
						(root4, bind4) => new
						{
//							Employee2         = root4.Employee2,
							Order             = root4.Order,
							EmployeeID        = root4.EmployeeID,
							BirthDate         = root4.BirthDate,
							HireDate          = root4.HireDate,
							ReportsTo         = root4.ReportsTo,
							ReportsToEmployee = root4.ReportsToEmployee,
							EmployeeTerritory = bind4
						})
					.SelectMany(
						query => db.Territory.Where(join => (query.EmployeeTerritory!.TerritoryID == join.TerritoryID)).DefaultIfEmpty(),
						(root5, bind5) => new
						{
//							Employee2         = root5.Employee2,
							Order             = root5.Order,
							EmployeeID        = root5.EmployeeID,
							BirthDate         = root5.BirthDate,
							HireDate          = root5.HireDate,
							ReportsTo         = root5.ReportsTo,
							ReportsToEmployee = root5.ReportsToEmployee,
							EmployeeTerritory = new
							{
								EmployeeID = root5.EmployeeTerritory!.EmployeeID,
								Employee   = root5.EmployeeTerritory.Employee,
								Territory  = bind5
							}
						})
					.SelectMany(
						query => db.Region.Where(join => (query.EmployeeTerritory.Territory!.RegionID == join.RegionID)).DefaultIfEmpty(),
						(root6, bind6) => new
						{
//							Employee2         = root6.Employee2,
							Order             = root6.Order,
							EmployeeID        = root6.EmployeeID,
							BirthDate         = root6.BirthDate,
							HireDate          = root6.HireDate,
							ReportsTo         = root6.ReportsTo,
							ReportsToEmployee = root6.ReportsToEmployee,
							EmployeeTerritory = new
							{
								EmployeeID = root6.EmployeeTerritory.EmployeeID,
								Employee   = root6.EmployeeTerritory.Employee,
								Territory  = new
								{
									EmployeeTerritory = root6.EmployeeTerritory.Territory!.EmployeeTerritory,
									RegionID          = root6.EmployeeTerritory.Territory.RegionID,
									Region            = bind6
								}
							}
						})
					.Where(e => e.EmployeeID == 5)
					;

				q.ToList();
			}
		}
	}
}
