using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using System;
	using System.Linq.Expressions;
	using Model;


	[TestFixture]
	public class ConcatUnionTests : TestBase
	{
		[Test]
		public void Concat1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent where p.ParentID == 1 select p).Concat(
					(from p in Parent where p.ParentID == 2 select p))
					,
					(from p in db.Parent where p.ParentID == 1 select p).Concat(
					(from p in db.Parent where p.ParentID == 2 select p)));
		}

		[Test]
		public async Task Concat1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent where p.ParentID == 1 select p).Concat(
					(from p in Parent where p.ParentID == 2 select p))
					,
					await
					(from p in db.Parent where p.ParentID == 1 select p).Concat(
					(from p in db.Parent where p.ParentID == 2 select p))
					.ToListAsync());
		}

		[Test]
		public void Concat11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child where ch.ParentID == 1 select ch.Parent).Concat(
					(from ch in    Child where ch.ParentID == 2 select ch.Parent)),
					(from ch in db.Child where ch.ParentID == 1 select ch.Parent).Concat(
					(from ch in db.Child where ch.ParentID == 2 select ch.Parent)));
		}

		[Test]
		public void Concat12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent where p.ParentID  == 1 select p).Concat(
					(from ch in    Child  where ch.ParentID == 2 select ch.Parent)),
					(from p  in db.Parent where p.ParentID  == 1 select p).Concat(
					(from ch in db.Child  where ch.ParentID == 2 select ch.Parent)));
		}

		[Test]
		public void Concat2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent where p.ParentID == 1 select p).Concat(
					(from p in Parent where p.ParentID == 2 select p)).Concat(
					(from p in Parent where p.ParentID == 4 select p))
					,
					(from p in db.Parent where p.ParentID == 1 select p).Concat(
					(from p in db.Parent where p.ParentID == 2 select p)).Concat(
					(from p in db.Parent where p.ParentID == 4 select p)));
		}

		[Test]
		public void Concat3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent where p.ParentID == 1 select p).Concat(
					(from p in Parent where p.ParentID == 2 select p).Concat(
					(from p in Parent where p.ParentID == 4 select p)))
					,
					(from p in db.Parent where p.ParentID == 1 select p).Concat(
					(from p in db.Parent where p.ParentID == 2 select p).Concat(
					(from p in db.Parent where p.ParentID == 4 select p))));
		}

		[Test]
		public void Concat4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from c in    Child where c.ParentID == 1 select c).Concat(
					(from c in    Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 }).
					Where(c => c.ChildID != 1032))
					,
					(from c in db.Child where c.ParentID == 1 select c).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 })).
					Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat401([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from c in    Child where c.ParentID == 1 select c).Concat(
					(from c in    Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID }).
					Where(c => c.ChildID != 1032))
					,
					(from c in db.Child where c.ParentID == 1 select c).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID })).
					Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat5([DataSources(ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from c in    Child where c.ParentID == 1 select c).Concat(
					(from c in    Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 }).
					Where(c => c.ChildID != 1032))
					,
					(from c in db.Child where c.ParentID == 1 select c).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 })).
					Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat501([DataSources(ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from c in    Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
					(from c in    Child where c.ParentID == 3 select new Child { ChildID  = c.ChildID + 1000 }).
					Where(c => c.ParentID == 1))
					,
					(from c in db.Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
					(from c in db.Child where c.ParentID == 3 select new Child { ChildID  = c.ChildID + 1000 })).
					Where(c => c.ParentID == 1));
		}

		[Test]
		public void Concat502([DataSources(ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from c in    Child where c.ParentID == 1 select c.Parent).Concat(
					(from c in    Child where c.ParentID == 3 select c.Parent).
					Where(p => p.Value1!.Value != 2))
					,
					(from c in db.Child where c.ParentID == 1 select c.Parent).Concat(
					(from c in db.Child where c.ParentID == 3 select c.Parent)).
					Where(p => p.Value1!.Value != 2));
		}

		[Test]
		public void Concat6([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Where(c => c.GrandChildren.Count == 2).Concat(   Child.Where(c => c.GrandChildren.Count() == 3)),
					db.Child.Where(c => c.GrandChildren.Count == 2).Concat(db.Child.Where(c => c.GrandChildren.Count() == 3)));
		}

		[Test]
		public void Concat7([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Customer.Where(c => c.Orders.Count <= 1).Concat(dd.Customer.Where(c => c.Orders.Count > 1)),
					db.Customer.Where(c => c.Orders.Count <= 1).Concat(db.Customer.Where(c => c.Orders.Count > 1)));
			}
		}

		[Test]
		public void Concat81([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  })));
		}

		[Test]
		public void Concat82([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, })));
		}

		[Test]
		public void Concat83([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, })));
		}

		[Test]
		public void Concat84([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID,  ID3 = c.ParentID + 1, }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0,  })));
		}

		[Test]
		public void Concat85([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })));
		}

		[Test]
		public void Concat851([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID,     ID3 = c.ParentID, }).Concat(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })),
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID,     ID3 = c.ParentID, }).Concat(
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  })));
		}

		[Test]
		public void Concat86([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }).Concat(
					   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, })),
					db.Child. Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID,  }).Concat(
					db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0,  ID3 = c.ParentID, })));
		}

		[Test]
		public void Concat87([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new Parent { ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent { Value1   = c.Value1   })),
					db.Child. Select(c => new Parent { ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent { Value1   = c.Value1   })));
		}

		[Test]
		public void Concat871([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(c => new Parent { Value1   = c.Value1   }).Concat(
					   Child. Select(c => new Parent { ParentID = c.ParentID })),
					db.Parent.Select(c => new Parent { Value1   = c.Value1   }).Concat(
					db.Child. Select(c => new Parent { ParentID = c.ParentID })));
		}

		[Test]
		public void Concat88([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new Parent { Value1   = c.ChildID,  ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent { ParentID = c.ParentID, Value1   = c.Value1   })),
					db.Child. Select(c => new Parent { Value1   = c.ChildID,  ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent { ParentID = c.ParentID, Value1   = c.Value1   })));
		}

		[Test]
		public void Concat89([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Concat(
					   Parent.Select(c => new Parent {                      ParentID = c.ParentID })),
					db.Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Concat(
					db.Parent.Select(c => new Parent {                      ParentID = c.ParentID })));
		}

		[Test]
		public void Concat891([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Union(
					   Parent.Select(c => new Parent {                      ParentID = c.ParentID })).Concat(
					   Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID })/*.Union(
					   Parent.Select(c => new Parent {                      ParentID = c.ParentID }))*/),
					db.Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Union(
					db.Parent.Select(c => new Parent {                      ParentID = c.ParentID })).Concat(
					db.Child. Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID })/*.Union(
					db.Parent.Select(c => new Parent {                      ParentID = c.ParentID }))*/),
					sort: x => x.OrderBy(_ => _.ParentID).ThenBy(_ => _.Value1));
		}

		[Test]
		public void Concat892([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(c => new Parent {Value1 = c.ParentID, ParentID = c.ParentID})
				.Union(db.Parent.Select(c => new Parent {                     ParentID = c.ParentID}))
				.Concat(db.Child.Select(c => new Parent {Value1 = c.ParentID, ParentID = c.ParentID}));

			AssertQuery(query);
		}

		[Test]
		public void Union1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from g  in    GrandChild join ch in    Child  on g.ChildID   equals ch.ChildID select ch).Union(
					(from ch in    Child      join p  in    Parent on ch.ParentID equals p.ParentID select ch))
					,
					(from g  in db.GrandChild join ch in db.Child  on g.ChildID   equals ch.ChildID select ch).Union(
					(from ch in db.Child      join p  in db.Parent on ch.ParentID equals p.ParentID select ch)));
		}

		[Test]
		public void Union2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from r  in
						(from g  in GrandChild join ch in Child  on g.ChildID   equals ch.ChildID select ch.ChildID).Union(
						(from ch in Child      join p  in Parent on ch.ParentID equals p.ParentID select ch.ChildID))
					join child in Child on r equals child.ChildID
					select child
					,
					from r in
						(from g  in db.GrandChild join ch in db.Child  on g.ChildID   equals ch.ChildID select ch.ChildID).Union(
						(from ch in db.Child      join p  in db.Parent on ch.ParentID equals p.ParentID select ch.ChildID))
					join child in db.Child on r equals child.ChildID
					select child);
		}

		[Test]
		public void Union3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in    Child  select new { id = ch.ParentID, val = false }))
					,
					(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in db.Child  select new { id = ch.ParentID, val = false })));
		}

		[Test]
		public void Union4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in    Child  select new { id = ch.ParentID, val = false }))
					.Select(p => new { p.id, p.val })
					,
					(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in db.Child  select new { id = ch.ParentID, val = false }))
					.Select(p => new { p.id, p.val }));
		}

		[Test]
		public void Union41([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in    Child  select new { id = ch.ParentID, val = false }))
					.Select(p => p)
					,
					(from p  in db.Parent select new { id = p.ParentID,  val = true }).Union(
					(from ch in db.Child  select new { id = ch.ParentID, val = false }))
					.Select(p => p));
		}

		[Test]
		public void Union42([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent select new { id = p. ParentID, val = true  }).Union(
					(from ch in    Child  select new { id = ch.ParentID, val = false }))
					.Select(p => p.val),
					(from p  in db.Parent select new { id = p. ParentID, val = true  }).Union(
					(from ch in db.Child  select new { id = ch.ParentID, val = false }))
					.Select(p => p.val));
		}

		[Test]
		public void Union421([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p  in    Parent select new { id = p. ParentID, val = true  }).Union(
					(from p  in    Parent select new { id = p. ParentID, val = false }).Union(
					(from ch in    Child  select new { id = ch.ParentID, val = false })))
					.Select(p => p.val),
					(from p  in db.Parent select new { id = p. ParentID, val = true  }).Union(
					(from p  in db.Parent select new { id = p. ParentID, val = false }).Union(
					(from ch in db.Child  select new { id = ch.ParentID, val = false })))
					.Select(p => p.val));
		}

		[Test]
		public void Union5([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select p1).Union(
					(from p2 in    Parent select new Parent { ParentID = p2.ParentID }))
					.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 })
					,
					(from p1 in db.Parent select p1).Union(
					(from p2 in db.Parent select new Parent { ParentID = p2.ParentID }))
					.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 }));
		}

		[Test]
		public void Union51([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1  in   Parent select p1).Union(
					(from p2 in    Parent select new Parent { ParentID = p2.ParentID }))
					,
					(from p1 in db.Parent select p1).Union(
					(from p2 in db.Parent select new Parent { ParentID = p2.ParentID })));
		}

		[Test]
		public void Union52([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2))
					,
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union521([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2))
					.Select(p => p.Value1)
					,
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2))
					.Select(p => p.Value1));
		}

		[Test]
		public void Union522([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new Parent { Value1 = p1.Value1 }).Union(
					(from p2 in    Parent select p2))
					,
					(from p1 in db.Parent select new Parent { Value1 = p1.Value1 }).Union(
					(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union523([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select p2)),
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union53([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in    Parent select new Parent { Value1   = p2.Value1   }))
					,
					(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
					(from p2 in db.Parent select new Parent { Value1   = p2.Value1   })));
		}

		//[Test]
		public void Union54([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
					(from p2 in    Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.First() })),
					(from p1 in db.Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
					(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.First() })));
		}

		//[Test]
		public void Union541([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
					(from p2 in    Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.First() }))
					.Select(p => new { p.ParentID, p.p, p.ch })
					,
					(from p1 in db.Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
					(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.First() }))
					.Select(p => new { p.ParentID, p.p, p.ch }));
		}

		[Test]
		public void ObjectUnion1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent where p1.ParentID >  3 select p1).Union(
					(from p2 in    Parent where p2.ParentID <= 3 select p2)),
					(from p1 in db.Parent where p1.ParentID >  3 select p1).Union(
					(from p2 in db.Parent where p2.ParentID <= 3 select p2)));
		}

		//////[Test]
		public void ObjectUnion2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent where p1.ParentID >  3 select p1).Union(
					(from p2 in    Parent where p2.ParentID <= 3 select (Parent?)null)),
					(from p1 in db.Parent where p1.ParentID >  3 select p1).Union(
					(from p2 in db.Parent where p2.ParentID <= 3 select (Parent?)null)));
		}

		[Test]
		public void ObjectUnion3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent where p1.ParentID >  3 select new { p = p1 }).Union(
					(from p2 in    Parent where p2.ParentID <= 3 select new { p = p2 })),
					(from p1 in db.Parent where p1.ParentID >  3 select new { p = p1 }).Union(
					(from p2 in db.Parent where p2.ParentID <= 3 select new { p = p2 })));
		}

		//////[Test]
		public void ObjectUnion4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent where p1.ParentID >  3 select new { p = new { p = p1, p1.ParentID } }).Union(
					(from p2 in    Parent where p2.ParentID <= 3 select new { p = new { p = p2, p2.ParentID } })),
					(from p1 in db.Parent where p1.ParentID >  3 select new { p = new { p = p1, p1.ParentID } }).Union(
					(from p2 in db.Parent where p2.ParentID <= 3 select new { p = new { p = p2, p2.ParentID } })));
		}

		//////[Test]
		public void ObjectUnion5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p1 in    Parent where p1.ParentID >  3 select new { p = new { p = p1, ParentID = p1.ParentID + 1 } }).Union(
					(from p2 in    Parent where p2.ParentID <= 3 select new { p = new { p = p2, ParentID = p2.ParentID + 1 } })),
					(from p1 in db.Parent where p1.ParentID >  3 select new { p = new { p = p1, ParentID = p1.ParentID + 1 } }).Union(
					(from p2 in db.Parent where p2.ParentID <= 3 select new { p = new { p = p2, ParentID = p2.ParentID + 1 } })));
		}

		[Test]
		public void ObjectUnion([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 =
					from p in db.Product
					join c in db.Category on p.CategoryID equals c.CategoryID into g
					from c in g.DefaultIfEmpty()
					select new
					{
						p,
						c.CategoryName,
						p.ProductName
					};

				var q2 =
					from p in db.Product
					join c in db.Category on p.CategoryID equals c.CategoryID into g
					from c in g.DefaultIfEmpty()
					select new
					{
						p,
						c.CategoryName,
						p.ProductName
					};

				var q = q1.Union(q2).Take(5);

				foreach (var item in q)
				{
					TestContext.WriteLine(item);
				}
			}
		}

		public class TestEntity1 { public int Id; public string? Field1; }
		public class TestEntity2 { public int Id; public string? Field1; }

		[Test]
		public void Concat90()
		{
			using(var context = new TestDataConnection())
			{
				var join1 =
					from t1 in context.GetTable<TestEntity1>()
					join t2 in context.GetTable<TestEntity2>()
						on t1.Id equals t2.Id
					into tmp
					from t2 in tmp.DefaultIfEmpty()
					select new { t1, t2 };

				var join1Sql = join1.ToString();
				Assert.IsNotNull(join1Sql);

				var join2 =
					from t2 in context.GetTable<TestEntity2>()
					join t1 in context.GetTable<TestEntity1>()
						on t2.Id equals t1.Id
					into tmp
					from t1 in tmp.DefaultIfEmpty()
					where t1 == null
					select new { t1, t2 };

				var join2Sql = join2.ToString();
				Assert.IsNotNull(join2Sql);

				var fullJoin = join1.Concat(join2);

				var fullJoinSql = fullJoin.ToString(); // BLToolkit.Data.Linq.LinqException : Types in Concat are constructed incompatibly.
				Assert.IsNotNull(fullJoinSql);
			}
		}

		[ActiveIssue("Associations with Concat/Union or other Set operations are not supported.")]
		[Test]
		public void AssociationUnion1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child.Union(Child)
					let p = c.Parent
					select p.ParentID,
					from c in db.Child.Union(db.Child)
					let p = c.Parent
					select p.ParentID);
		}

		[ActiveIssue("Associations with Concat/Union or other Set operations are not supported.")]
		[Test]
		public void AssociationUnion2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child.Union(Child)
					select c.Parent!.ParentID,
					from c in db.Child.Union(db.Child)
					select c.Parent!.ParentID);
		}

		[ActiveIssue("Associations with Concat/Union or other Set operations are not supported.")]
		[Test]
		public void AssociationConcat2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child.Concat(Child)
					select c.Parent!.ParentID,
					from c in db.Child.Concat(db.Child)
					select c.Parent!.ParentID);
		}

		[Test]
		public void ConcatToString([DataSources] string context)
		{
			string pattern = "1";

			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Person where p.FirstName.Contains(pattern) select p.FirstName).Concat(
					(from p in Person where p.ID.ToString().Contains(pattern) select p.FirstName)).Take(10)
					,
					(from p in db.Person where Sql.Like(p.FirstName, "1") select p.FirstName).Concat(
					(from p in db.Person where p.ID.ToString().Contains(pattern) select p.FirstName)).Take(10));
		}

		[Test]
		public void ConcatWithUnion([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent.Select(c => new Parent {ParentID = c.ParentID}). Union(
					Parent.Select(c => new Parent {ParentID = c.ParentID})).Concat(
					Parent.Select(c => new Parent {ParentID = c.ParentID}). Union(
					Parent.Select(c => new Parent {ParentID = c.ParentID})
						)
					),
					db.Parent.Select(c => new Parent {ParentID = c.ParentID}). Union(
					db.Parent.Select(c => new Parent {ParentID = c.ParentID})).Concat(
					db.Parent.Select(c => new Parent {ParentID = c.ParentID}). Union(
					db.Parent.Select(c => new Parent {ParentID = c.ParentID})
						)
					)
				);
		}

		[Test]
		public void UnionWithObjects([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in db.Parent
					from p2 in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					select new
					{
						P1 = p,
						P2 = p2,
						C = c
					};

				var q2 =
					from p in db.Parent
					from p2 in db.Parent
					join c in db.Child on p2.ParentID equals c.ParentID
					select new
					{
						P1 = p,
						P2 = p2,
						C = c
					};

				var q = q1.Union(q2);

				var qe1 =
					from p in Parent
					from p2 in Parent
					join c in Child on p.ParentID equals c.ParentID
					select new
					{
						P1 = p,
						P2 = p2,
						C = c
					};

				var qe2 =
					from p in Parent
					from p2 in Parent
					join c in Child on p2.ParentID equals c.ParentID
					select new
					{
						P1 = p,
						P2 = p2,
						C = c
					};

				var qe = qe1.Union(qe2);

				AreEqual(qe, q);
			}
		}

		[Test]
		public void UnionGroupByTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					db.Types
						.GroupBy(_ => new { month = _.DateTimeValue.Month, year = _.DateTimeValue.Year })
						.Select(_ => _.Key)
						.Select(_ => new { _.month, _.year, @int = 1 })
					.Union(
						db.Types.Select(_ => new { month = (int)_.SmallIntValue, year = (int)_.SmallIntValue, @int = 3 }))
					.Union(
						db.Types.Select(_ => new { month = _.DateTimeValue.Year, year = _.DateTimeValue.Year, @int = 2 }))
//					.AsEnumerable()
//					.OrderBy(_ => _.month)
//					.ThenBy (_ => _.year)
//					.ThenBy (_ => _.@int)
					.ToList();

				var expected =
					GetTypes(context)
						.GroupBy(_ => new { month = _.DateTimeValue.Month, year = _.DateTimeValue.Year })
						.Select(_ => _.Key)
						.Select(_ => new { _.month, _.year, @int = 1 })
					.Union(
						GetTypes(context).Select(_ => new { month = (int)_.SmallIntValue, year = (int)_.SmallIntValue, @int = 3 }))
					.Union(
						GetTypes(context).Select(_ => new { month = _.DateTimeValue.Year, year = _.DateTimeValue.Year, @int = 2 }))
//					.AsEnumerable()
//					.OrderBy(_ => _.month)
//					.ThenBy (_ => _.year)
//					.ThenBy (_ => _.@int)
					.ToList();

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void UnionGroupByTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					db.Types.Select(_ => new { month = (int)_.SmallIntValue, year = (int)_.SmallIntValue, @int = 3 })
					.Union(
						db.Types
							.GroupBy(_ => new { month = _.DateTimeValue.Month, year = _.DateTimeValue.Year })
							.Select(_ => _.Key)
							.Select(_ => new { _.month, _.year, @int = 1 }))
					.Union(
						db.Types.Select(_ => new { month = _.DateTimeValue.Year, year = _.DateTimeValue.Year, @int = 2 })
					)
					.ToList();

				var expected =
					Types.Select(_ => new { month = (int)_.SmallIntValue, year = (int)_.SmallIntValue, @int = 3 })
					.Union(
						Types
							.GroupBy(_ => new { month = _.DateTimeValue.Month, year = _.DateTimeValue.Year })
							.Select(_ => _.Key)
							.Select(_ => new { _.month, _.year, @int = 1 }))
					.Union(
						Types.Select(_ => new { month = _.DateTimeValue.Year, year = _.DateTimeValue.Year, @int = 2 })
					)
					.ToList();

				AreEqual(expected, actual);
			}
		}

		[Table("ConcatTest")]
		[InheritanceMapping(Code = 0, Type = typeof(BaseEntity), IsDefault = true)]
		[InheritanceMapping(Code = 1, Type = typeof(DerivedEntity))]
		class BaseEntity
		{
			[Column]
			public int EntityId { get; set; }
			[Column(IsDiscriminator = true)]
			public int Discr { get; set; }
			[Column]
			public string? Value { get; set; }
		}

		[Table("ConcatTest")]
		class DerivedEntity : BaseEntity
		{
		}

		[Test]
		public void TestConcatInheritance([IncludeDataSources(TestProvName.AllSQLiteClassic)] string context)
		{
			var testData = new[]
			{
				new BaseEntity { Discr = 0, EntityId = 1, Value = "VBase1" },
				new BaseEntity { Discr = 0, EntityId = 2, Value = "VBase2" },
				new BaseEntity { Discr = 0, EntityId = 3, Value = "VBase3" },

				new DerivedEntity { Discr = 1, EntityId = 10, Value = "Derived1" },
				new DerivedEntity { Discr = 1, EntityId = 20, Value = "Derived2" },
				new DerivedEntity { Discr = 1, EntityId = 30, Value = "Derived3" }
			};

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData))
			{
				var result = db.GetTable<BaseEntity>().OfType<BaseEntity>()
					.Concat(db.GetTable<BaseEntity>().OfType<DerivedEntity>())
					.ToArray();

				var expected = testData.Where(t => t.GetType() == typeof(BaseEntity))
					.Concat(testData.OfType<DerivedEntity>())
					.ToArray();

				AreEqualWithComparer(expected, result);
			}

		}

		[ActiveIssue("CI: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null", Configuration = ProviderName.DB2)]
		[Test]
		public void TestConcatWithParameterProjection([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var someValue = 3;
				var items1 = from c in db.Child
					where c.ChildID <= someValue
					select new
					{
						Value = someValue,
						c.ChildID
					};

				var items2 = from c in db.Child
					where c.ChildID > someValue
					select new
					{
						Value = someValue,
						c.ChildID
					};

				var actual = items1.Concat(items2);

				var items1_ = from c in Child
					where c.ChildID <= someValue
					select new
					{
						Value = someValue,
						c.ChildID
					};

				var items2_ = from c in Child
					where c.ChildID > someValue
					select new
					{
						Value = someValue,
						c.ChildID
					};

				var expected = items1_.Concat(items2_);

				AreEqual(expected, actual);
			}
		}

		// https://github.com/linq2db/linq2db/issues/1774
		[Test]
		public void SelectFromUnion([IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = from p in db.Person where p.ID == 1 select new { p.ID };
				var q2 = from p in db.Person where p.ID != 1 select new { p.ID };
				var q = q1.Concat(q2);
				var f = q.Select(t => new { t.ID, rn = Sql.Ext.DenseRank().Over().OrderBy(t.ID).ToValue() }).ToList();
			}
		}

		// https://github.com/linq2db/linq2db/issues/1774
		[Test]
		public void SelectFromUnionReverse([IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = from p in db.Person where p.ID == 1 select new { p.ID };
				var q2 = from p in db.Person where p.ID != 1 select new { p.ID };
				var q = q1.Concat(q2);
				var f = q.Select(t => new { rn = Sql.Ext.DenseRank().Over().OrderBy(t.ID).ToValue(), t.ID }).ToList();
			}
		}

		[Test]
		public void SelectWithNulls([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.GetTable<LinqDataTypes>();
			var query2 = db.GetTable<LinqDataTypes>().Select(d => new LinqDataTypes { });

			var query = query1.UnionAll(query2);

			query.Invoking(q => q.ToArray()).Should().NotThrow();
		}

		[Test]
		public void SelectWithNulls2([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.GetTable<LinqDataTypes2>();
			var query2 = db.GetTable<LinqDataTypes2>().Select(d => new LinqDataTypes2 { });

			var query = query1.UnionAll(query2);

			query.Invoking(q => q.ToArray()).Should().NotThrow();
		}

		[Test]
		public void SelectWithBooleanNulls([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = from x in db.Parent
				select new {a = db.Child.Any(), b = (bool?)(x.ParentID != 0)};

			var query2 = from x in db.Parent
				select new {a = db.Child.Any(), b = (bool?)null};

			var query = query1.UnionAll(query2);

			query.Invoking(q => q.ToList()).Should().NotThrow();
		}		

		[Test(Description = "Test that we generate plain UNION without sub-queries")]
		public void Issue3359_MultipleSets([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var query1 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query2 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query3 = db.Person.Select(p => new { p.FirstName, p.LastName });

			query1.Concat(query2).Concat(query3).ToArray();

			db.LastQuery!.Should().Contain("SELECT", Exactly.Thrice());
		}

		[Test(Description = "Test that we generate plain UNION without sub-queries")]
		public void Issue3359_MultipleSetsCombined([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var query1 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query2 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query3 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query4 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query5 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query6 = db.Person.Select(p => new { p.FirstName, p.LastName });

			query1.Concat(query2.Concat(query3)).Concat(query4.Concat(query5).Concat(query6)).ToArray();

			db.LastQuery!.Should().Contain("SELECT", Exactly.Times(6));
		}

		// only pgsql supports all 6 operators right now
		[Test(Description = "Test that we generate sub-queries for incompatible set operators and order queries properly")]
		public void Issue3359_MultipleSetsCombined_DifferentOperators([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var query1 = db.Person.Select(p => new { FirstName = p.FirstName + "q1", p.LastName });
			var query2 = db.Person.Select(p => new { FirstName = p.FirstName + "q2", p.LastName });
			var query3 = db.Person.Select(p => new { FirstName = p.FirstName + "q3", p.LastName });
			var query4 = db.Person.Select(p => new { FirstName = p.FirstName + "q4", p.LastName });
			var query5 = db.Person.Select(p => new { FirstName = p.FirstName + "q5", p.LastName });
			var query6 = db.Person.Select(p => new { FirstName = p.FirstName + "q6", p.LastName });

			query1.Union(query2.UnionAll(query3)).Intersect(query4.IntersectAll(query5).Except(query6)).ToArray();

			var sql = db.LastQuery!;
			// 6 main queries and 4 subqueries for incompatible operators
			sql.Should().Contain("SELECT", Exactly.Times(6 + 4));

			// operators generated
			sql.Should().Contain("UNION ALL", Exactly.Once());
			sql.Should().Contain("UNION", Exactly.Twice());
			sql.Should().Contain("INTERSECT", Exactly.Twice());
			sql.Should().Contain("INTERSECT ALL", Exactly.Once());
			sql.Should().Contain("EXCEPT", Exactly.Once());

			// operators order correct
			var i1 = sql.IndexOf("UNION");
			var i2 = sql.IndexOf("UNION ALL");
			var i3 = sql.IndexOf("INTERSECT");
			var i4 = sql.IndexOf("INTERSECT ALL");
			var i5 = sql.IndexOf("EXCEPT");
			Assert.AreNotEqual(-1, i1);
			Assert.Less(i1, i2);
			Assert.Less(i2, i3);
			Assert.Less(i3, i4);
			Assert.Less(i4, i5);

			// queries order correct
			i1 = sql.IndexOf("q1");
			i2 = sql.IndexOf("q2");
			i3 = sql.IndexOf("q3");
			i4 = sql.IndexOf("q4");
			i5 = sql.IndexOf("q5");
			Assert.AreNotEqual(-1, i1);
			Assert.Less(i1, i2);
			Assert.Less(i2, i3);
			Assert.Less(i3, i4);
			Assert.Less(i4, i5);
		}

		public record class  Issue3357RecordClass (int Id, string FirstName, string LastName);
		public class Issue3357RecordLike
		{
			public Issue3357RecordLike(int Id, string FirstName, string LastName)
			{
				this.Id        = Id;
				this.FirstName = FirstName;
				this.LastName  = LastName;
			}

			public int    Id        { get; }
			public string FirstName { get; }
			public string LastName  { get; }
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))),

				db.Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(db.Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))));
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordLikeClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqualWithComparer(
				Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))
				.Concat(Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))),

				db.Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))
				.Concat(db.Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))));
		}

		[Table]
		public class Issue3323Table
		{
			[PrimaryKey                      ] public int     Id       { get; set; }
			[Column(SkipOnEntityFetch = true)] public string? FistName { get; set; }
			[Column(SkipOnEntityFetch = true)] public string? LastName { get; set; }
			[Column(CanBeNull = false)       ] public string  Text     { get; set; } = null!;

			[ExpressionMethod(nameof(FullNameExpr), IsColumn = true)]
			public string FullName { get; set; } = null!;


			private static Expression<Func<Issue3323Table, string>> FullNameExpr() => entity => entity.FistName + " " + entity.LastName;
		}

		[Test(Description = "calculated column in set select")]
		public void Issue3323([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3323Table>();
			tb.Insert(() => new Issue3323Table()
			{
				Id       = 1,
				FistName = "one",
				LastName = "two",
				Text     = "text"
			});

			var res = tb.Concat(tb).ToArray();

			Assert.AreEqual(2, res.Length);
			Assert.AreEqual("one two", res[0].FullName);
			Assert.AreEqual("one two", res[1].FullName);
		}

		[Test(Description = "calculated column in set select")]
		public void Issue3323_Mixed([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3323Table>();
			tb.Insert(() => new Issue3323Table()
			{
				Id       = 1,
				FistName = "one",
				LastName = "two",
				Text     = "text"
			});

			var query1 = tb.Select(r => new { r.Id, Text = r.FullName });
			var query2 = tb.Select(r => new { Id = r.Id + 1, Text = r.Text });

			var res = query1.Concat(query2).ToArray().OrderBy(r => r.Id).ToArray();

			Assert.AreEqual(2        , res.Length);
			Assert.AreEqual("one two", res[0].Text);
			Assert.AreEqual("text"   , res[1].Text);

			res = query2.Concat(query1).ToArray().OrderBy(r => r.Id).ToArray();

			Assert.AreEqual(2        , res.Length);
			Assert.AreEqual("one two", res[0].Text);
			Assert.AreEqual("text"   , res[1].Text);
		}

		[Test(Description = "NullReferenceException : Object reference not set to an instance of an object.")]
		public void Issue2505([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var src = db.Person.AsQueryable();

			var query1 = src.Select(i => new
			{
				Person = i,
				Gender = i.MiddleName == null ? Gender.Male : Gender.Other,
			});

			var query2 = src.Select(i => new
			{
				Person = i,
				Gender = i.MiddleName == null ? Gender.Male : Gender.Other,
			});

			query1
				.UnionAll(query2)
				.Select(i => new
				{
					Person = i.Person,
					Gender = i.Gender,
				})
				.Where(i => i.Gender == Gender.Other)
				.OrderByDescending(i => i.Person.FirstName)
				.Select(i => new
				{
					Account = i.Person.LastName
				})
				.ToList();
		}

		[Table]
		private class Issue3360Table
		{
			[PrimaryKey                         ] public int     Id  { get; set; }
			// by default we generate N-literal, which is not compatible with (var)char
			[Column(DataType = DataType.VarChar)] public string? Str { get; set; }
		}

		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeByOtherQuery([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query1 = tb.Select(p => new { p.Id, p.Str                });
			var query2 = tb.Select(p => new { p.Id, Str = (string?)"str" });

			query1.Concat(query2).ToArray();
			if (db is TestDataConnection dc1)
				dc1.LastQuery!.Should().NotContain("N'");

			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.Should().NotContain("N'");
		}

		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeByOtherQuery_AllProviders([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query1 = tb.Select(p => new { p.Id, p.Str                });
			var query2 = tb.Select(p => new { p.Id, Str = (string?)"str" });

			query1.Concat(query2).ToArray();
			query2.Concat(query1).ToArray();
		}

		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeByProjectionProperty([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query1 = tb.Select(p => new Issue3360Table() { Id = p.Id, Str = (string?)"str1" });
			var query2 = tb.Select(p => new Issue3360Table() { Id = p.Id, Str = (string?)"str2" });

			query1.Concat(query2).ToArray();
			if (db is TestDataConnection dc1)
				dc1.LastQuery!.Should().NotContain("N'");

			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.Should().NotContain("N'");
		}

		[Test(Description = "Test that non-sqlserver providers work too")]
		public void Issue3360_TypeByProjectionProperty_AllProviders([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query1 = tb.Select(p => new Issue3360Table() { Id = p.Id, Str = (string?)"str1" });
			var query2 = tb.Select(p => new Issue3360Table() { Id = p.Id, Str = (string?)"str2" });

			query1.Concat(query2).ToArray();
			query2.Concat(query1).ToArray();
		}

		public enum InvalidColumnIndexMappingEnum1
		{
			[MapValue("ENUM1_VALUE")]
			Value
		}

		public enum InvalidColumnIndexMappingEnum2
		{
			[MapValue("ENUM2_VALUE")]
			Value
		}

		[Table]
		public class Issue3360Table1
		{
			[PrimaryKey] public int                             Id    { get; set; }
			[Column    ] public byte                            Byte  { get; set; }
			[Column    ] public byte?                           ByteN { get; set; }
			[Column]
			[Column    ] public Guid                            Guid  { get; set; }
			[Column    ] public Guid?                           GuidN { get; set; }
			[Column    ] public InvalidColumnIndexMappingEnum1  Enum  { get; set; }
			[Column    ] public InvalidColumnIndexMappingEnum2? EnumN { get; set; }
			[Column    ] public bool                            Bool  { get; set; }
			[Column    ] public bool?                           BoolN { get; set; }

			public static Issue3360Table1[] Items = new[]
			{
				new Issue3360Table1() { Id = 1 },
				new Issue3360Table1() { Id = 2, Byte = 1, ByteN = 2, Guid = TestData.Guid1, GuidN = TestData.Guid2, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = true, BoolN = false },
				new Issue3360Table1() { Id = 4, Byte = 3, ByteN = 4, Guid = TestData.Guid3, GuidN = TestData.Guid1, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = false, BoolN = true },
			};
		}

		private record Issue3360NullsRecord(int Id, byte? Byte, byte? ByteN, Guid? Guid, Guid? GuidN, InvalidColumnIndexMappingEnum1? Enum, InvalidColumnIndexMappingEnum2? EnumN, bool? Bool, bool? BoolN);

		[Test(Description = "null literals in first query")]
		public void Issue3360_NullsInAnchor([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = table.Where(r => r.Id == 1)
				.Select(r => new Issue3360NullsRecord(r.Id, null, null, null, null, null, null, null, null))
				.Concat(
					table.Where(r => r.Id == 2)
						.Select(r => new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(1, data[0].Id);
			Assert.IsNull(data[0].Byte);
			Assert.IsNull(data[0].ByteN);
			Assert.IsNull(data[0].Guid);
			Assert.IsNull(data[0].GuidN);
			Assert.IsNull(data[0].Enum);
			Assert.IsNull(data[0].EnumN);
			Assert.IsNull(data[0].Bool);
			Assert.IsNull(data[0].BoolN);

			Assert.AreEqual(2, data[1].Id);
			Assert.AreEqual(1, data[1].Byte);
			Assert.AreEqual(2, data[1].ByteN);
			Assert.AreEqual(TestData.Guid1, data[1].Guid);
			Assert.AreEqual(TestData.Guid2, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(true, data[1].Bool);
			Assert.AreEqual(false, data[1].BoolN);
		}

		[Test(Description = "double columns in first query")]
		public void Issue3360_DoubleColumnSelection([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = table.Where(r => r.Id == 2)
				.Select(r => new Issue3360NullsRecord(r.Id, r.Byte, r.Byte, r.Guid, r.Guid, null, null, r.Bool, r.Bool))
				.Concat(
					table.Where(r => r.Id == 4)
						.Select(r => new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(2, data[0].Id);
			Assert.AreEqual(1, data[0].Byte);
			Assert.AreEqual(1, data[0].ByteN);
			Assert.AreEqual(TestData.Guid1, data[0].Guid);
			Assert.AreEqual(TestData.Guid1, data[0].GuidN);
			Assert.IsNull(data[0].Enum);
			Assert.IsNull(data[0].EnumN);
			Assert.AreEqual(true, data[0].Bool);
			Assert.AreEqual(true, data[0].BoolN);

			Assert.AreEqual(4, data[1].Id);
			Assert.AreEqual(3, data[1].Byte);
			Assert.AreEqual(4, data[1].ByteN);
			Assert.AreEqual(TestData.Guid3, data[1].Guid);
			Assert.AreEqual(TestData.Guid1, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(false, data[1].Bool);
			Assert.AreEqual(true, data[1].BoolN);
		}

		[Test(Description = "null literals in first query")]
		public void Issue3360_LiteralsInFirstQuery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = table.Where(r => r.Id == 2)
				.Select(r => new Issue3360NullsRecord(r.Id, 5, 5, new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), InvalidColumnIndexMappingEnum1.Value, InvalidColumnIndexMappingEnum2.Value, true, false))
				.Concat(
					table.Where(r => r.Id == 4)
						.Select(r => new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(2, data[0].Id);
			Assert.AreEqual(5, data[0].Byte);
			Assert.AreEqual(5, data[0].ByteN);
			Assert.AreEqual(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), data[0].Guid);
			Assert.AreEqual(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), data[0].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[0].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[0].EnumN);
			Assert.AreEqual(true, data[0].Bool);
			Assert.AreEqual(false, data[0].BoolN);

			Assert.AreEqual(4, data[1].Id);
			Assert.AreEqual(3, data[1].Byte);
			Assert.AreEqual(4, data[1].ByteN);
			Assert.AreEqual(TestData.Guid3, data[1].Guid);
			Assert.AreEqual(TestData.Guid1, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(false, data[1].Bool);
			Assert.AreEqual(true, data[1].BoolN);
		}


		[Test(Description = "Test that we type non-field union column properly")]
		public void Issue2451_ComplexColumn([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Select(p => new Person() { FirstName = p.FirstName });
			var query2 = db.Person.Select(p => new Person() { FirstName = p.FirstName + '/' + p.LastName });

			query1.Concat(query2).ToArray();

			// too many things is wrong here:
			// [p].[FirstName] + Convert(VarChar(4000), N'/') + [p].[LastName]
			// 1. why we cast N-literal to varchar instead of varchar literal generation
			// 2. why we even mention varchar in expression with N-columns only
			if (db is TestDataConnection dc1)
				dc1.LastQuery!.Should().NotContain("Convert(VarChar");
			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.Should().NotContain("Convert(VarChar");
		}

		[Test(Description = "Test that other providers work")]
		public void Issue2451_ComplexColumn_All([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Select(p => new Person() { FirstName = p.FirstName });
			var query2 = db.Person.Select(p => new Person() { FirstName = p.FirstName + '/' + p.LastName });

			query1.Concat(query2).ToArray();
			query2.Concat(query1).ToArray();
		}

		[Table]
		[Column(MemberName = $"{nameof(Name)}.{nameof(FullName.FirstName)}")]
		[Column(MemberName = $"{nameof(Name)}.{nameof(FullName.LastName)}")]
		public class ComplexPerson
		{
			[PrimaryKey] public int       Id   { get; set; }
			             public FullName? Name { get; set; }
		}

		public class FullName
		{
			public string? FirstName { get; set; }
			public string? LastName  { get; set; }
		}

		[ActiveIssue(3346)]
		[Test(Description = "composite columns in union (also tests create table)")]
		public void Issue3346_ProjectionBuild([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ComplexPerson>();

			var query1 = from x in tb
						 where x.Id < 10
						 select x;

			var query2 = from x in tb
						 where x.Id < 20
						 select x;

			query1.Union(query2).ToArray();
		}

		[ActiveIssue(3346)]
		[Test(Description = "composite columns in union (also tests create table)")]
		public void Issue3346_Count([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ComplexPerson>();

			var query1 = from x in tb
						 where x.Id < 10
						 select x;

			var query2 = from x in tb
						 where x.Id < 20
						 select x;

			query1.Union(query2).Count();
		}

		[ActiveIssue(3150)]
		[Test(Description = "preserve constant columns")]
		public void Issue3150([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Where(p => p.ID == 1).Select(p => new { p.ID, Name = new { p.FirstName, Marker = "id=1" } });
			var query2 = db.Person.Where(p => p.ID == 2).Select(p => new { p.ID, Name = new { p.FirstName, Marker = "id=2" } });

			var result = query1.Concat(query2).ToArray();

			Assert.AreEqual(2, result.Length);
			Assert.AreEqual(1, result.Select(r => r.Name.Marker == "id=1").Count());
			Assert.AreEqual(1, result.Select(r => r.Name.Marker == "id=2").Count());
		}

		public class Issue2948MyModel
		{
			public int    Id   { get; set; }
			public string Name { get; set; } = null!;
		}

		public class Issue2948RankData<T>
		{
			public long Rank  { get; set; }
			public T    Model { get; set; } = default!;
		}

		[ActiveIssue(2948)]
		[Test(Description = "InvalidCastException : Unable to cast object of type 'System.Linq.Expressions.MemberMemberBinding' to type 'System.Linq.Expressions.MemberAssignment'.")]
		public void Issue2948([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var main = (from p in db.Person
						select new Issue2948RankData<Issue2948MyModel>()
						{
							Model = { Id = p.ID, Name = p.FirstName },
							Rank  = Sql.Ext.RowNumber().Over().PartitionBy(p.ID).OrderBy(p.ID).ToValue()
						}).Where(x => x.Rank == 1).Select(x => x.Model).AsSubQuery();

			var first  = main.Where(x => x.Id != 2);
			var second = main.Where(x => x.Id == 2).OrderByDescending(x => x.Name).Take(1);
			var third  = main.Where(x => x.Id != 3).OrderBy(x => x.Name).Take(1);

			var res = first.Concat(second).Concat(third).ToList();
		}

		[ActiveIssue(2932)]
		[Test(Description = "invalid SQL for Any() subquery")]
		public void Issue2932_Broken([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(p => new { p.ChildID, Sub = p.GrandChildren.Any() });

			query.Concat(query).ToArray();
		}

		[Test(Description = "invalid SQL for Any() subquery")]
		public void Issue2932_Works([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(p => new { p.ChildID, Sub = p.GrandChildren.Any() ? true : false });

			query.Concat(query).ToArray();
		}

		[ActiveIssue(2619)]
		[Test(Description = "set query with ORDER BY requires wrapping into subquery for some DBs")]
		public void Issue2619_Query1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			((from item in db.Person select item)
				.OrderBy(i => i.ID))
				.Union((from item in db.Person select item))
				.ToList();
		}

		[ActiveIssue(2619)]
		[Test(Description = "set query with ORDER BY requires wrapping into subquery for some DBs")]
		public void Issue2619_Query2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			(from item in db.Person select item)
				.Union((from item in db.Person select item)
				.OrderBy(i => i.ID))
				.ToList();
		}

		[Test(Description = "ArgumentOutOfRangeException : Index was out of range. Must be non-negative and less than the size of the collection.")]
		public void Issue2511_Query1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var res = db.Person.LoadWith(p => p.Patient).Concat(db.Person.LoadWith(p => p.Patient).Take(2)).ToArray();
			
			Assert.AreEqual(6, res.Length);
			Assert.AreEqual(2, res.Where(r => r.ID == 2).Count());
			var pat = res.Where(r => r.ID == 2).First();
			Assert.IsNotNull(pat.Patient);
			Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", pat.Patient!.Diagnosis);
			pat = res.Where(r => r.ID == 2).Skip(1).First();
			Assert.IsNotNull(pat.Patient);
			Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", pat.Patient!.Diagnosis);
		}

		[ActiveIssue(2511)]
		[Test(Description = "Associations with Concat/Union or other Set operations are not supported")]
		public void Issue2511_Query2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var res = db.Person.LoadWith(p => p.Patient)
				.Select(p => new Person()
				{
					ID         = p.ID,
					FirstName  = p.FirstName,
					LastName   = p.LastName,
					MiddleName = p.MiddleName,
					Gender     = p.Gender,
					Patient    = p.Patient
				}).Take(2)
				.Concat(db.Person.LoadWith(p => p.Patient))
				.ToArray();

			Assert.AreEqual(6, res.Length);
			var pat = res.Where(r => r.ID == 2).First();
			Assert.IsNull(pat.Patient);
			pat = res.Where(r => r.ID == 2).Skip(1).Single();
			Assert.IsNotNull(pat.Patient);
			Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", pat.Patient!.Diagnosis);
		}

		[Test(Description = "Working version of Issue2511_Query2")]
		public void Issue2511_Query3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var res = db.Person.LoadWith(p => p.Patient)
				.Select(p => new Person()
				{
					ID         = p.ID,
					FirstName  = p.FirstName,
					LastName   = p.LastName,
					MiddleName = p.MiddleName,
					Gender     = p.Gender,
				}).Take(2)
				.Concat(db.Person.LoadWith(p => p.Patient))
				.ToArray();

			Assert.AreEqual(6, res.Length);
			var pat = res.Where(r => r.ID == 2).First();
			Assert.IsNull(pat.Patient);
			pat = res.Where(r => r.ID == 2).Skip(1).Single();
			Assert.IsNull(pat.Patient);
		}
	}
}
