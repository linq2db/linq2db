using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SubQuery : TestBase
	{
		[Test]
		public void Test1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID select ch.ChildID).Max(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID select ch.ChildID).Max()));
		}

		[Test]
		public void Test2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max()));
		}

		[Test]
		public void Test3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault()));
		}

		[Test]
		public void Test4()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault()));
		}

		static int _testValue = 3;

		[Test]
		public void Test5()
		{
			IEnumerable<int> ids = new[] { 1, 2 };

			var eids = Parent
				.Where(p => ids.Contains(p.ParentID))
				.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
				.Distinct();

			var expected = eids.Select(id =>
				new 
				{
					id,
					Count1 = Child.Where(p => p.ParentID == id).Count(),
					Count2 = Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
				});

			ForEachProvider(db =>
			{
				var rids   = db.Parent
					.Where(p => ids.Contains(p.ParentID))
					.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
					.Distinct();

				var result = rids.Select(id =>
					new
					{
						id,
						Count1 = db.Child.Where(p => p.ParentID == id).Count(),
						Count2 = db.Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
					});

				AreEqual(expected, result);
			});
		}

		[Test]
		public void Test6()
		{
			var id = 2;
			var b  = false;

			var q = Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
			q = b
				? q.OrderBy(m => m.ParentID)
				: q.OrderByDescending(m => m.ParentID);

			var gc = GrandChild;
			var expected = q.Select(c => new
			{
				ID     = c.ChildID,
				c.ParentID,
				Sum    = gc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID * g.GrandChildID),
				Count1 = gc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0)
			});

			ForEachProvider(db =>
			{
				var r = db.Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
				r = b
					? r.OrderBy(m => m.ParentID)
					: r.OrderByDescending(m => m.ParentID);

				var rgc = db.GrandChild;
				var result = r.Select(c => new
				{
					ID     = c.ChildID,
					c.ParentID,
					Sum    = rgc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID * g.GrandChildID),
					Count1 = rgc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0),
				});

				AreEqual(expected, result);
			});
		}

		[Test]
		public void Test7()
		{
			ForEachProvider(db => AreEqual(
				from c in Child select new
				{
					Count = GrandChild.Where(g => g.ChildID == c.ChildID).Count(),
				},
				from c in db.Child select new
				{
					Count = db.GrandChild.Where(g => g.ChildID == c.ChildID).Count(),
				}));
		}

		[Test]
		public void ObjectCompare()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from p in Parent
				from c in
					from c in
						from c in Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
					where c.ChildID > 0
					select c
				where p == c.Parent
				select new { p.ParentID, c.ChildID },
				from p in db.Parent
				from c in
					from c in
						from c in db.Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
					where c.ChildID > 0
					select c
				where p == c.Parent
				select new { p.ParentID, c.ChildID }));
		}

		[Test]
		public void Contains1()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p in Parent
					where (from p1 in    Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p,
					from p in db.Parent
					where (from p1 in db.Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p));
		}

		[Test]
		public void Contains2()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p in Parent
					where (from p1 in    Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
					select p,
					from p in db.Parent
					where (from p1 in db.Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
					select p));
		}

		[Test]
		public void SubSub1()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					}));
		}

		[Test]
		public void SubSub2()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase, ProviderName.Informix },
				db => AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c.c.ParentID + 1 into c
							where c < p1.ID
							select c
						).FirstOrDefault()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c.c.ParentID + 1 into c
							where c < p1.ID
							select c
						).FirstOrDefault()
					}));
		}

		//[Test]
		public void SubSub201()
		{
			ForEachProvider(
				//new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).FirstOrDefault()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).FirstOrDefault()
					}));
		}

		[Test]
		public void SubSub21()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase, ProviderName.Access },
				db => AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				}));
		}

		[Test]
		public void SubSub211()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							from g in c.GrandChildren
							select new { g, ID = g.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.g, ID = c.g.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							from g in c.GrandChildren
							select new { g, ID = g.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.g, ID = c.g.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					}));
		}

		[Test]
		public void SubSub212()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p1 in
						from p2 in Child
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Parent.GrandChildren
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Child
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Parent.GrandChildren
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					}));
		}

		[Test]
		public void SubSub22()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase },
				db => AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in Child
							where p1.p2.p2.ParentID == c.ParentID
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in db.Child
							where p1.p2.p2.ParentID == c.ParentID
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					}));
		}
	}
}
