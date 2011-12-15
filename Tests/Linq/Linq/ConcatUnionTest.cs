using System;
using System.Linq;

using LinqToDB.Data.DataProvider;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ConcatUnionTest : TestBase
	{
		[Test]
		public void Concat1()
		{
			var expected =
				(from p in Parent where p.ParentID == 1 select p).Concat(
				(from p in Parent where p.ParentID == 2 select p));

			ForEachProvider(db => AreEqual(expected, 
				(from p in db.Parent where p.ParentID == 1 select p).Concat(
				(from p in db.Parent where p.ParentID == 2 select p))));
		}

		[Test]
		public void Concat11()
		{
			ForEachProvider(db => AreEqual(
				(from ch in    Child where ch.ParentID == 1 select ch.Parent).Concat(
				(from ch in    Child where ch.ParentID == 2 select ch.Parent)),
				(from ch in db.Child where ch.ParentID == 1 select ch.Parent).Concat(
				(from ch in db.Child where ch.ParentID == 2 select ch.Parent))));
		}

		[Test]
		public void Concat12()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent where p.ParentID  == 1 select p).Concat(
				(from ch in    Child  where ch.ParentID == 2 select ch.Parent)),
				(from p  in db.Parent where p.ParentID  == 1 select p).Concat(
				(from ch in db.Child  where ch.ParentID == 2 select ch.Parent))));
		}

		[Test]
		public void Concat2()
		{
			var expected =
				(from p in Parent where p.ParentID == 1 select p).Concat(
				(from p in Parent where p.ParentID == 2 select p)).Concat(
				(from p in Parent where p.ParentID == 4 select p));

			ForEachProvider(db => AreEqual(expected, 
				(from p in db.Parent where p.ParentID == 1 select p).Concat(
				(from p in db.Parent where p.ParentID == 2 select p)).Concat(
				(from p in db.Parent where p.ParentID == 4 select p))));
		}

		[Test]
		public void Concat3()
		{
			var expected =
				(from p in Parent where p.ParentID == 1 select p).Concat(
				(from p in Parent where p.ParentID == 2 select p).Concat(
				(from p in Parent where p.ParentID == 4 select p)));

			ForEachProvider(db => AreEqual(expected, 
				(from p in db.Parent where p.ParentID == 1 select p).Concat(
				(from p in db.Parent where p.ParentID == 2 select p).Concat(
				(from p in db.Parent where p.ParentID == 4 select p)))));
		}

		[Test]
		public void Concat4()
		{
			var expected =
				(from c in Child where c.ParentID == 1 select c).Concat(
				(from c in Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 }).
				Where(c => c.ChildID != 1032));

			ForEachProvider(db => AreEqual(expected, 
				(from c in db.Child where c.ParentID == 1 select c).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 })).
				Where(c => c.ChildID != 1032)));
		}

		[Test]
		public void Concat401()
		{
			var expected =
				(from c in Child where c.ParentID == 1 select c).Concat(
				(from c in Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID }).
				Where(c => c.ChildID != 1032));

			ForEachProvider(db => AreEqual(expected, 
				(from c in db.Child where c.ParentID == 1 select c).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID })).
				Where(c => c.ChildID != 1032)));
		}

		[Test]
		public void Concat5()
		{
			ForEachProvider(
				new[] { ProviderName.DB2, ProviderName.Informix },
				db => AreEqual(
					(from c in Child where c.ParentID == 1 select c).Concat(
					(from c in Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 }).
					Where(c => c.ChildID != 1032)), 
					(from c in db.Child where c.ParentID == 1 select c).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 })).
					Where(c => c.ChildID != 1032)));
		}

		[Test]
		public void Concat501()
		{
			ForEachProvider(new[] { ProviderName.DB2, ProviderName.Informix },
				db => AreEqual(
					(from c in    Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
					(from c in    Child where c.ParentID == 3 select new Child { ChildID  = c.ChildID + 1000 }).
					Where(c => c.ParentID == 1)),
					(from c in db.Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ChildID  = c.ChildID + 1000 })).
					Where(c => c.ParentID == 1)));
		}

		[Test]
		public void Concat502()
		{
			ForEachProvider(new[] { ProviderName.DB2, ProviderName.Informix },
				db => AreEqual(
					(from c in    Child where c.ParentID == 1 select c.Parent).Concat(
					(from c in    Child where c.ParentID == 3 select c.Parent).
					Where(p => p.Value1.Value != 2)),
					(from c in db.Child where c.ParentID == 1 select c.Parent).Concat(
					(from c in db.Child where c.ParentID == 3 select c.Parent)).
					Where(p => p.Value1.Value != 2)));
		}

		[Test]
		public void Concat6()
		{
			ForEachProvider(new[] { ProviderName.SqlCe },
				db => AreEqual(
					   Child.Where(c => c.GrandChildren.Count == 2).Concat(   Child.Where(c => c.GrandChildren.Count() == 3)),
					db.Child.Where(c => c.GrandChildren.Count == 2).Concat(db.Child.Where(c => c.GrandChildren.Count() == 3))));
		}

		[Test]
		public void Concat7()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					   Customer.Where(c => c.Orders.Count <= 1).Concat(   Customer.Where(c => c.Orders.Count > 1)),
					db.Customer.Where(c => c.Orders.Count <= 1).Concat(db.Customer.Where(c => c.Orders.Count > 1)));
		}

		[Test]
		public void Concat81()
		{
			ForEachProvider(
				db => AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  }))));
		}

		[Test]
		public void Concat82()
		{
			ForEachProvider(
				db => AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }))));
		}

		[Test]
		public void Concat83()
		{
			ForEachProvider(
				db => AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, }))));
		}

		[Test]
		public void Concat84()
		{
			ForEachProvider(
				db => AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  }))));
		}

		[Test]
		public void Concat85()
		{
			ForEachProvider(
				db => AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }))));
		}

		[Test]
		public void Concat851()
		{
			ForEachProvider(
				db => AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID,     ID3 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID,     ID3 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }))));
		}

		[Test]
		public void Concat86()
		{
			ForEachProvider(
				db => AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, }))));
		}

		[Test]
		public void Concat87()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					   Child. Select(c => new Parent { ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent { Value1   = c.Value1   })),
					db.Child. Select(c => new Parent { ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent { Value1   = c.Value1   }))));
		}

		[Test]
		public void Concat871()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					   Parent.Select(c => new Parent { Value1   = c.Value1   }).Concat(
					   Child. Select(c => new Parent { ParentID = c.ParentID })),
					db.Parent.Select(c => new Parent { Value1   = c.Value1   }).Concat(
					db.Child. Select(c => new Parent { ParentID = c.ParentID }))));
		}

		[Test]
		public void Concat88()
		{
			ForEachProvider(
				db => AreEqual(
					   Child. Select(c => new Parent { Value1   = c.ChildID,  ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent { ParentID = c.ParentID, Value1   = c.Value1   })),
					db.Child. Select(c => new Parent { Value1   = c.ChildID,  ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent { ParentID = c.ParentID, Value1   = c.Value1   }))));
		}

		[Test]
		public void Concat89()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					   Child. Select(c => new Parent { Value1   = c.ParentID, ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent { ParentID = c.ParentID                        })),
					db.Child. Select(c => new Parent { Value1   = c.ParentID, ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent { ParentID = c.ParentID                        }))));
		}

		[Test]
		public void Union1()
		{
			ForEachProvider(db => AreEqual(
				(from g  in    GrandChild join ch in    Child  on g.ChildID   equals ch.ChildID select ch).Union(
				(from ch in    Child      join p  in    Parent on ch.ParentID equals p.ParentID select ch)),
				(from g  in db.GrandChild join ch in db.Child  on g.ChildID   equals ch.ChildID select ch).Union(
				(from ch in db.Child      join p  in db.Parent on ch.ParentID equals p.ParentID select ch))));
		}

		[Test]
		public void Union2()
		{
			ForEachProvider(db => AreEqual(
				from r  in
					(from g  in GrandChild join ch in Child  on g.ChildID   equals ch.ChildID select ch.ChildID).Union(
					(from ch in Child      join p  in Parent on ch.ParentID equals p.ParentID select ch.ChildID))
				join child in Child on r equals child.ChildID
				select child,
				from r in
					(from g  in db.GrandChild join ch in db.Child  on g.ChildID   equals ch.ChildID select ch.ChildID).Union(
					(from ch in db.Child      join p  in db.Parent on ch.ParentID equals p.ParentID select ch.ChildID))
				join child in db.Child on r equals child.ChildID
				select child));
		}

		[Test]
		public void Union3()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in    Child  select new { id = ch.ParentID, val = false })),
				(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in db.Child  select new { id = ch.ParentID, val = false }))));
		}

		[Test]
		public void Union4()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in    Child  select new { id = ch.ParentID, val = false }))
				.Select(p => new { p.id, p.val }),
				(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in db.Child  select new { id = ch.ParentID, val = false }))
				.Select(p => new { p.id, p.val })));
		}

		[Test]
		public void Union41()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in    Child  select new { id = ch.ParentID, val = false }))
				.Select(p => p),
				(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
				(from ch in db.Child  select new { id = ch.ParentID, val = false }))
				.Select(p => p)));
		}

		[Test]
		public void Union42()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent select new { id = p. ParentID, val = true  }).Union(
				(from ch in    Child  select new { id = ch.ParentID, val = false }))
				.Select(p => p.val),
				(from p  in db.Parent select new { id = p. ParentID, val = true  }).Union(
				(from ch in db.Child  select new { id = ch.ParentID, val = false }))
				.Select(p => p.val)));
		}

		[Test]
		public void Union421()
		{
			ForEachProvider(db => AreEqual(
				(from p  in    Parent select new { id = p. ParentID, val = true  }).Union(
				(from p  in    Parent select new { id = p. ParentID, val = false }).Union(
				(from ch in    Child  select new { id = ch.ParentID, val = false })))
				.Select(p => p.val),
				(from p  in db.Parent select new { id = p. ParentID, val = true  }).Union(
				(from p  in db.Parent select new { id = p. ParentID, val = false }).Union(
				(from ch in db.Child  select new { id = ch.ParentID, val = false })))
				.Select(p => p.val)));
		}

		[Test]
		public void Union5()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select p1).Union(
					(from p2 in    Parent select new Parent { ParentID = p2.ParentID }))
					.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 }),
					(from p1 in db.Parent select p1).Union(
					(from p2 in db.Parent select new Parent { ParentID = p2.ParentID }))
					.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 })));
		}

		[Test]
		public void Union51()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					(from p1  in   Parent select p1).Union(
					(from p2 in    Parent select new Parent { ParentID = p2.ParentID })),
					(from p1 in db.Parent select p1).Union(
					(from p2 in db.Parent select new Parent { ParentID = p2.ParentID }))));
		}

		[Test]
		public void Union52()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2)),
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2))));
		}

		[Test]
		public void Union521()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2))
					.Select(p => p.Value1),
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2))
					.Select(p => p.Value1)));
		}

		[Test]
		public void Union522()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new Parent { Value1 = p1.Value1 }).Union(
					(from p2 in    Parent select p2)),
					(from p1 in db.Parent select new Parent { Value1 = p1.Value1 }).Union(
					(from p2 in db.Parent select p2))));
		}

		[Test]
		public void Union523()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2)),
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2))));
		}

		[Test]
		public void Union53()
		{
			ForEachProvider(
				new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select new Parent { Value1   = p2.Value1   })),
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select new Parent { Value1   = p2.Value1   }))));
		}

		//[Test]
		public void Union54()
		{
			ForEachProvider(
				//new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new { ParentID = p1.ParentID,    p = p1,           ch = (Child)null }).Union(
					(from p2 in    Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent)null, ch = p2.Children.First() })),
					(from p1 in db.Parent select new { ParentID = p1.ParentID,    p = p1,           ch = (Child)null }).Union(
					(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent)null, ch = p2.Children.First() }))));
		}

		//[Test]
		public void Union541()
		{
			ForEachProvider(
				//new[] { ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					(from p1 in    Parent select new { ParentID = p1.ParentID,    p = p1,           ch = (Child)null }).Union(
					(from p2 in    Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent)null, ch = p2.Children.First() }))
					.Select(p => new { p.ParentID, p.p, p.ch }),
					(from p1 in db.Parent select new { ParentID = p1.ParentID,    p = p1,           ch = (Child)null }).Union(
					(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent)null, ch = p2.Children.First() }))
					.Select(p => new { p.ParentID, p.p, p.ch })));
		}
	}
}
