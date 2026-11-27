using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Linq.Helpers.UnionJoinDtos;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ConcatUnionTests : TestBase
	{
		[Test]
		public void Concat1([DataSources] string context)
		{
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child where ch.ParentID == 1 select ch.Parent).Concat(
				(from ch in Child where ch.ParentID == 2 select ch.Parent)),
				(from ch in db.Child where ch.ParentID == 1 select ch.Parent).Concat(
				(from ch in db.Child where ch.ParentID == 2 select ch.Parent)));
		}

		[Test]
		public void Concat12([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent where p.ParentID == 1 select p).Concat(
				(from ch in Child where ch.ParentID == 2 select ch.Parent)),
				(from p in db.Parent where p.ParentID == 1 select p).Concat(
				(from ch in db.Child where ch.ParentID == 2 select ch.Parent)));
		}

		[Test]
		public void Concat2([DataSources] string context)
		{
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);

				AreEqual(
					(from c in    Child where c.ParentID == 1 select c).Concat(
				(from c in    Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 })
				.Where(c => c.ChildID != 1032))
					,
					(from c in db.Child where c.ParentID == 1 select c).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1000 }))
				.Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat401([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from c in Child where c.ParentID == 1 select c).Concat(
				(from c in Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID }).
				Where(c => c.ChildID != 1032))
				,
				(from c in db.Child where c.ParentID == 1 select c).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000, ParentID = c.ParentID })).
				Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat5([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from c in Child where c.ParentID == 1 select c).Concat(
				(from c in Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 }).
				Where(c => c.ChildID != 1032))
				,
				(from c in db.Child where c.ParentID == 1 select c).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 })).
				Where(c => c.ChildID != 1032));
		}

		[Test]
		public void Concat501([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from c in Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
				(from c in Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 }).
				Where(c => c.ParentID == 1))
				,
				(from c in db.Child where c.ParentID == 1 select new Child { ParentID = c.ParentID }).Concat(
				(from c in db.Child where c.ParentID == 3 select new Child { ChildID = c.ChildID + 1000 })).
				Where(c => c.ParentID == 1));
		}

		[Test]
		public void Concat502([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from c in Child where c.ParentID == 1 select c.Parent).Concat(
				(from c in Child where c.ParentID == 3 select c.Parent).
				Where(p => p.Value1!.Value != 2))
				,
				(from c in db.Child where c.ParentID == 1 select c.Parent).Concat(
				(from c in db.Child where c.ParentID == 3 select c.Parent)).
				Where(p => p.Value1!.Value != 2));
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void Concat6([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Where(c => c.GrandChildren.Count == 2).Concat(Child.Where(c => c.GrandChildren.Count() == 3)),
				db.Child.Where(c => c.GrandChildren.Count == 2).Concat(db.Child.Where(c => c.GrandChildren.Count() == 3)));
		}

		[Test]
		public void Concat7([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				dd.Customer.Where(c => c.Orders.Count <= 1).Concat(dd.Customer.Where(c => c.Orders.Count > 1)),
				db.Customer.Where(c => c.Orders.Count <= 1).Concat(db.Customer.Where(c => c.Orders.Count > 1)));
		}

		[Test]
		public void Concat81([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, })),
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, }).Concat(
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, })));
		}

		[Test]
		public void Concat82([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, }).Concat(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, })),
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, }).Concat(
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, })));
		}

		[Test]
		public void Concat83([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0, }).Concat(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, ID3 = c.ParentID + 1, })),
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0, }).Concat(
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, ID3 = c.ParentID + 1, })));
		}

		[Test]
		public void Concat84([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, ID3 = c.ParentID + 1, }).Concat(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0, })),
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ChildID, ID3 = c.ParentID + 1, }).Concat(
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.Value1 ?? 0, })));
		}

		[Test]
		public void Concat85([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0, ID3 = c.ParentID, }).Concat(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, })),
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0, ID3 = c.ParentID, }).Concat(
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, })));
		}

		[Test]
		public void Concat851([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.ParentID, }).Concat(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, })),
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID, ID3 = c.ParentID, }).Concat(
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, })));
		}

		[Test]
		public void Concat86([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, }).Concat(
				   Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0, ID3 = c.ParentID, })),
				db.Child.Select(c => new { ID1 = c.ParentID, ID2 = c.ParentID + 1, ID3 = c.ChildID, }).Concat(
				db.Parent.Select(c => new { ID1 = c.ParentID, ID2 = c.Value1 ?? 0, ID3 = c.ParentID, })));
		}

		[Test]
		public void Concat87([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new Parent { ParentID = c.ParentID }).Concat(
				   Parent.Select(c => new Parent { Value1 = c.Value1 })),
				db.Child.Select(c => new Parent { ParentID = c.ParentID }).Concat(
				db.Parent.Select(c => new Parent { Value1 = c.Value1 })));
		}

		[Test]
		public void Concat871([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(c => new Parent { Value1 = c.Value1 }).Concat(
				   Child.Select(c => new Parent { ParentID = c.ParentID })),
				db.Parent.Select(c => new Parent { Value1 = c.Value1 }).Concat(
				db.Child.Select(c => new Parent { ParentID = c.ParentID })));
		}

		[Test]
		public void Concat88([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new Parent { Value1 = c.ChildID, ParentID = c.ParentID }).Concat(
				   Parent.Select(c => new Parent { ParentID = c.ParentID, Value1 = c.Value1 })),
				db.Child.Select(c => new Parent { Value1 = c.ChildID, ParentID = c.ParentID }).Concat(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID, Value1 = c.Value1 })));
		}

		[Test]
		public void Concat89([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Concat(
				   Parent.Select(c => new Parent { ParentID = c.ParentID })),
				db.Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Concat(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID })));
		}

		[Test]
		public void Concat891([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Union(
				   Parent.Select(c => new Parent { ParentID = c.ParentID })).Concat(
				   Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID })/*.Union(
					   Parent.Select(c => new Parent {                      ParentID = c.ParentID }))*/),
				db.Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID }).Union(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID })).Concat(
				db.Child.Select(c => new Parent { Value1 = c.ParentID, ParentID = c.ParentID })/*.Union(
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
		public void UnionOrderByNulls(
			[DataSources] string context,
			[Values(Sql.NullsPosition.First, Sql.NullsPosition.Last)] Sql.NullsPosition nulls,
			[Values] bool descending)
		{
			using var db = GetDataContext(context);

			// Set-operation + NULLS: the emulation CASE key must reach the outer ORDER BY and stay valid
			// (the union's ORDER BY column-promotion / sub-query wrapping must project it where required).
			var union = db.Parent.Where(p => p.ParentID < 5)
				.Union(db.Parent.Where(p => p.ParentID >= 3));

			var ordered = descending
				? union.OrderByDescending(p => p.Value1, nulls)
				: union.OrderBy          (p => p.Value1, nulls);

			AssertQuery(ordered
				.ThenBy(p => p.ParentID)
				.Take(3));
		}

		[Test]
		public void Union1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from g in GrandChild join ch in Child on g.ChildID equals ch.ChildID select ch).Union(
				(from ch in Child join p in Parent on ch.ParentID equals p.ParentID select ch))
				,
				(from g in db.GrandChild join ch in db.Child on g.ChildID equals ch.ChildID select ch).Union(
				(from ch in db.Child join p in db.Parent on ch.ParentID equals p.ParentID select ch)));
		}

		[Test]
		public void Union2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from r in
					(from g in GrandChild join ch in Child on g.ChildID equals ch.ChildID select ch.ChildID).Union(
					(from ch in Child join p in Parent on ch.ParentID equals p.ParentID select ch.ChildID))
				join child in Child on r equals child.ChildID
				select child
				,
				from r in
					(from g in db.GrandChild join ch in db.Child on g.ChildID equals ch.ChildID select ch.ChildID).Union(
					(from ch in db.Child join p in db.Parent on ch.ParentID equals p.ParentID select ch.ChildID))
				join child in db.Child on r equals child.ChildID
				select child);
		}

		[Test]
		public void Union3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in Child select new { id = ch.ParentID, val = false }))
				,
				(from p in db.Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in db.Child select new { id = ch.ParentID, val = false })));
		}

		[Test]
		public void Union4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in Child select new { id = ch.ParentID, val = false }))
				.Select(p => new { p.id, p.val })
				,
				(from p in db.Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in db.Child select new { id = ch.ParentID, val = false }))
				.Select(p => new { p.id, p.val }));
		}

		[Test]
		public void Union41([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in Child select new { id = ch.ParentID, val = false }))
				.Select(p => p)
				,
				(from p in db.Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in db.Child select new { id = ch.ParentID, val = false }))
				.Select(p => p));
		}

		[Test]
		public void Union42([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in Child select new { id = ch.ParentID, val = false }))
				.Select(p => p.val),
				(from p in db.Parent select new { id = p.ParentID, val = true }).Union(
				(from ch in db.Child select new { id = ch.ParentID, val = false }))
				.Select(p => p.val));
		}

		[Test]
		public void Union421([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p in Parent select new { id = p.ParentID, val = true }).Union(
				(from p in Parent select new { id = p.ParentID, val = false }).Union(
				(from ch in Child select new { id = ch.ParentID, val = false })))
				.Select(p => p.val),
				(from p in db.Parent select new { id = p.ParentID, val = true }).Union(
				(from p in db.Parent select new { id = p.ParentID, val = false }).Union(
				(from ch in db.Child select new { id = ch.ParentID, val = false })))
				.Select(p => p.val));
		}

		[Test]
		public void Union5([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select p1).Union(
				(from p2 in Parent select new Parent { ParentID = p2.ParentID }))
				.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 })
				,
				(from p1 in db.Parent select p1).Union(
				(from p2 in db.Parent select new Parent { ParentID = p2.ParentID }))
				.Select(p => new Parent { ParentID = p.ParentID, Value1 = p.Value1 }));
		}

		[Test]
		public void Union51([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select p1).Union(
				(from p2 in Parent select new Parent { ParentID = p2.ParentID }))
				,
				(from p1 in db.Parent select p1).Union(
				(from p2 in db.Parent select new Parent { ParentID = p2.ParentID })));
		}

		[Test]
		public void Union52([DataSources(TestProvName.AllNativeAccess, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in Parent select p2))
				,
				(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union521([DataSources(TestProvName.AllNativeAccess, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in Parent select p2))
				.Select(p => p.Value1)
				,
				(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in db.Parent select p2))
				.Select(p => p.Value1));
		}

		[Test]
		public void Union522([DataSources(TestProvName.AllNativeAccess, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new Parent { Value1 = p1.Value1 }).Union(
				(from p2 in Parent select p2))
				,
				(from p1 in db.Parent select new Parent { Value1 = p1.Value1 }).Union(
				(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union523([DataSources(TestProvName.AllNativeAccess, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in Parent select p2)),
				(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in db.Parent select p2)));
		}

		[Test]
		public void Union53([DataSources(TestProvName.AllNativeAccess, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in Parent select new Parent { Value1 = p2.Value1 }))
				,
				(from p1 in db.Parent select new Parent { ParentID = p1.ParentID }).Union(
				(from p2 in db.Parent select new Parent { Value1 = p2.Value1 })));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Union54([DataSources] string context)
		{
			using var db = GetDataContext(context);

				AreEqual(
					(from p1 in    Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
				(from p2 in    Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.OrderByDescending(x => x.ChildID).FirstOrDefault() })),
					(from p1 in db.Parent select new { ParentID = p1.ParentID,    p = p1,            ch = (Child?)null }).Union(
				(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.OrderByDescending(x => x.ChildID).FirstOrDefault() })), sort: e => e.OrderBy(x => x.ch == null).ThenBy(x => x.ParentID));
		}

		[Test]
		public void ConcatWithDifferentProjections([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
					(from p1 in db.Parent select new { ParentID = p1.ParentID, p = p1 })
				.Concat(
					(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null })
					);

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Union541([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent select new { ParentID = p1.ParentID, p = p1, ch = (Child?)null }).Union(
				(from p2 in Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.OrderByDescending(x => x.ChildID).FirstOrDefault() }))
				.Select(p => new { p.ParentID, p.p, p.ch })
				,
				(from p1 in db.Parent select new { ParentID = p1.ParentID, p = p1, ch = (Child?)null }).Union(
				(from p2 in db.Parent select new { ParentID = p2.Value1 ?? 0, p = (Parent?)null, ch = p2.Children.OrderByDescending(x => x.ChildID).FirstOrDefault() }))
				.Select(p => new { p.ParentID, p.p, p.ch }));
		}

		[Test]
		public void ObjectUnion1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent where p1.ParentID > 3 select p1).Union(
				(from p2 in Parent where p2.ParentID <= 3 select p2)),
				(from p1 in db.Parent where p1.ParentID > 3 select p1).Union(
				(from p2 in db.Parent where p2.ParentID <= 3 select p2)));
		}

		[Test]
		public void ObjectUnion2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent where p1.ParentID > 3 select p1).Union(
				(from p2 in Parent where p2.ParentID <= 3 select (Parent?)null)),
				(from p1 in db.Parent where p1.ParentID > 3 select p1).Union(
				(from p2 in db.Parent where p2.ParentID <= 3 select (Parent?)null)));
		}

		[Test]
		public void ObjectUnion3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent where p1.ParentID > 3 select new { p = p1 }).Union(
				(from p2 in Parent where p2.ParentID <= 3 select new { p = p2 })),
				(from p1 in db.Parent where p1.ParentID > 3 select new { p = p1 }).Union(
				(from p2 in db.Parent where p2.ParentID <= 3 select new { p = p2 })));
		}

		[Test]
		public void ObjectUnion4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent where p1.ParentID > 3 select new { p = new { p = p1, p1.ParentID } }).Union(
				(from p2 in Parent where p2.ParentID <= 3 select new { p = new { p = p2, p2.ParentID } })),
				(from p1 in db.Parent where p1.ParentID > 3 select new { p = new { p = p1, p1.ParentID } }).Union(
				(from p2 in db.Parent where p2.ParentID <= 3 select new { p = new { p = p2, p2.ParentID } })));
		}

		[Test]
		public void TupleUnion([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
					(from p1 in db.Parent where p1.ParentID > 3 select Tuple.Create(p1.ParentID, p1.Value1))
				.Union(
					from p2 in db.Parent where p2.ParentID <= 3 select Tuple.Create(p2.ParentID, p2.Value1));

			AssertQuery(query);
		}

		[Test]
		public void TupleUnionProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p1 in db.Parent where p1.ParentID > 3 select Tuple.Create((int?)p1.ParentID, p1.Value1))
				.Union(
					from p2 in db.Parent where p2.ParentID <= 3 select Tuple.Create(p2.Value1, (int?)p2.ParentID))
				.Select(x => new { x.Item2, x.Item1 });

			AssertQuery(query);
		}

		[Test]
		public void TupleConcatIncompatibleProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p1 in db.Parent where p1.ParentID > 3 select Tuple.Create((int?)p1.ParentID, p1.Value1))
				.Concat(
					from p2 in db.Parent where p2.ParentID <= 3 select default(Tuple<int?, int?>))
				.Select(x => new { x.Item2, x.Item1 });

			AssertQuery(query);
		}

		[Test]
		public void ObjectUnion5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from p1 in Parent where p1.ParentID > 3 select new { p = new { p = p1, ParentID = p1.ParentID + 1 } }).Union(
				(from p2 in Parent where p2.ParentID <= 3 select new { p = new { p = p2, ParentID = p2.ParentID + 1 } })),
				(from p1 in db.Parent where p1.ParentID > 3 select new { p = new { p = p1, ParentID = p1.ParentID + 1 } }).Union(
				(from p2 in db.Parent where p2.ParentID <= 3 select new { p = new { p = p2, ParentID = p2.ParentID + 1 } })));
		}

		[Test]
		public void ObjectUnion([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

			q.ToArray();
		}

		public class TestEntity1 { [PrimaryKey] public int Id; public string? Field1; }
		public class TestEntity2 { [PrimaryKey] public int Id; public string? Field1; }

		[Test]
		public void Concat90([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb1 = db.CreateLocalTable<TestEntity1>();
			using var tb2 = db.CreateLocalTable<TestEntity2>();
			var join1 =
					from t1 in db.GetTable<TestEntity1>()
					join t2 in db.GetTable<TestEntity2>()
						on t1.Id equals t2.Id
					into tmp
					from t2 in tmp.DefaultIfEmpty()
					select new { t1, t2 };

			join1.ToArray();

			var join2 =
					from t2 in db.GetTable<TestEntity2>()
					join t1 in db.GetTable<TestEntity1>()
						on t2.Id equals t1.Id
					into tmp
					from t1 in tmp.DefaultIfEmpty()
					where t1 == null
					select new { t1, t2 };

			join2.ToArray();

			var fullJoin = join1.Concat(join2);

			fullJoin.ToArray();
		}

		[Test]
		public void AssociationUnion1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child.Union(Child)
				let p = c.Parent
				select p.ParentID,
				from c in db.Child.Union(db.Child)
				let p = c.Parent
				select p.ParentID);
		}

		[Test]
		public void AssociationUnion2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child.Union(Child)
				select c.Parent!.ParentID,
				from c in db.Child.Union(db.Child)
				select c.Parent!.ParentID);
		}

		[Test]
		public void AssociationConcat2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child.Concat(Child)
				select c.Parent!.ParentID,
				from c in db.Child.Concat(db.Child)
				select c.Parent!.ParentID);
		}

		[Test]
		public void ConcatToString([DataSources] string context)
		{
			string pattern = "1";

			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
			AreEqual(
				Parent.Select(c => new Parent { ParentID = c.ParentID }).Union(
				Parent.Select(c => new Parent { ParentID = c.ParentID })).Concat(
				Parent.Select(c => new Parent { ParentID = c.ParentID }).Union(
				Parent.Select(c => new Parent { ParentID = c.ParentID })
					)
				),
				db.Parent.Select(c => new Parent { ParentID = c.ParentID }).Union(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID })).Concat(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID }).Union(
				db.Parent.Select(c => new Parent { ParentID = c.ParentID })
					)
				)
			);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void ConcatDefaultIfEmpty([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 =
				from p in db.Parent.LoadWith(p => p.Children)
				where p.ParentID == 1
				select p.Children.FirstOrDefault();

			var query2 =
				from p in db.Parent
				where p.ParentID != 1
				select (Child?)null;

			var query = query1.Concat(query2);

			AssertQuery(query);
		}

		[Test]
		public void UnionWithObjects([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void UnionGroupByTest1([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void UnionGroupByTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[ActiveIssue(Configuration = TestProvName.AllNativeAccess, ErrorMessage = "This operation is not allowed in subqueries.",
			Details = "no-issue: UNION in a subquery is not supported by Access; supporting it means transforming the shape, and nothing tracks that.")]
		[Test]
		public void ConcatInAny([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Parent.Select(p => p.ParentID)
					.Concat(db.Parent.Select(p => p.ParentID))
					.Any();

			result.ShouldBeTrue();
		}

		[Test]
		public void ConcatConditions([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var query1 =
					from c in db.Child
					from p in db.Parent.Where(p => p.ParentID == c.ParentID).DefaultIfEmpty()
					select new
					{
						Parent1 = Sql.ToNullable(p.ParentID) == null
							? null
							: new { ParentID = p.ParentID, Value1 = p.Value1 },
						Parent2 = Sql.ToNullable(p.ParentID) == null
							? null
							: new Parent { ParentID = p.ParentID, Value1 = p.Value1, Children = p.Children.ToList() }
					};

			var query2 =
					from c in db.Child
					from p in db.Parent.Where(p => p.ParentID == c.ParentID).DefaultIfEmpty()
					select new
					{
						Parent1 = Sql.ToNullable(p.ParentID) == null
							? null
							: new { ParentID = p.ParentID, Value1 = p.Value1 },
						Parent2 = (Parent?)null
					};

			var query = query1.Concat(query2);

			AssertQuery(query);
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
		sealed class DerivedEntity : BaseEntity;

		[Test]
		public void TestConcatInheritance1([IncludeDataSources(TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
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

				var expected = testData.OfType<BaseEntity>()
					.Concat(testData.OfType<DerivedEntity>())
					.ToArray();

				AreEqualWithComparer(expected, result);
			}
		}

		[ActiveIssue(ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "Type.op_Equality",
			Details = "no-issue: type !=/== type parsing is not supported currently. The fragment is the operator the translator names when it gives up, which is stable across the two providers this gate covers.")]
		[Test]
		public void TestConcatInheritance2([IncludeDataSources(TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
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
				var result = db.GetTable<BaseEntity>().Where(t => t.GetType() == typeof(BaseEntity))
					.Concat(db.GetTable<BaseEntity>().OfType<DerivedEntity>())
					.ToArray();

				var expected = testData.Where(t => t.GetType() == typeof(BaseEntity))
					.Concat(testData.OfType<DerivedEntity>())
					.ToArray();

				AreEqualWithComparer(expected, result);
			}
		}

		[Test]
		public void TestConcatInheritance3([IncludeDataSources(TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
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
				var result = db.GetTable<BaseEntity>().Where(t => t.Discr == 0)
					.Concat(db.GetTable<BaseEntity>().OfType<DerivedEntity>())
					.ToArray();

				var expected = testData.Where(t => t.GetType() == typeof(BaseEntity))
					.Concat(testData.OfType<DerivedEntity>())
					.ToArray();

				AreEqualWithComparer(expected, result);
			}
		}

		[Test]
		public void TestConcatWithParameterProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		// https://github.com/linq2db/linq2db/issues/1774
		[Test]
		public void SelectFromUnion([IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllClickHouse,
				TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var q1 = from p in db.Person where p.ID == 1 select new { p.ID };
			var q2 = from p in db.Person where p.ID != 1 select new { p.ID };
			var q = q1.Concat(q2);
			var f = q.Select(t => new { t.ID, rn = Sql.Ext.DenseRank().Over().OrderBy(t.ID).ToValue() }).ToList();
		}

		// https://github.com/linq2db/linq2db/issues/1774
		[Test]
		public void SelectFromUnionReverse([IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllClickHouse,
				TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var q1 = from p in db.Person where p.ID == 1 select new { p.ID };
			var q2 = from p in db.Person where p.ID != 1 select new { p.ID };
			var q = q1.Concat(q2);
			var f = q.Select(t => new { rn = Sql.Ext.DenseRank().Over().OrderBy(t.ID).ToValue(), t.ID }).ToList();
		}

		[Test]
		public void SelectWithNulls([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.GetTable<LinqDataTypes>();
			var query2 = db.GetTable<LinqDataTypes>().Select(d => new LinqDataTypes { });

			var query = query1.UnionAll(query2);

			query.ToArray();
		}

		[Test]
		public void SelectWithNulls2([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.GetTable<LinqDataTypes2>();
			var query2 = db.GetTable<LinqDataTypes2>().Select(d => new LinqDataTypes2 { });

			var query = query1.UnionAll(query2);

			query.ToArray();
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

			query.ToArray();
		}

		[Test]
		public void SelectWithToString([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 =
				from x in db.Parent
				select new
				{
					StrValue = x.Value1.ToString()
				};

			var query2 =
				from x in db.Parent
				from c in x.Children
				select new
				{
					StrValue = c.Parent1!.Value1!.ToString()
				};

			var query = query1.Concat(query2);

			query = query.Where(x => x.StrValue != null);

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3359")]
		public void ConcatChainIsSingleSetQuery([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var query1 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query2 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query3 = db.Person.Select(p => new { p.FirstName, p.LastName });

			var query = query1.Concat(query2).Concat(query3);
			_ = query.ToArray();

			var selectQuery = query.GetSelectQuery()!;

			selectQuery.HasSetOperators.ShouldBeTrue();
			selectQuery.SetOperators.Count.ShouldBe(2);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3359")]
		public void NestedConcatsAreFlattenedIntoSingleSetQuery([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var query1 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query2 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query3 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query4 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query5 = db.Person.Select(p => new { p.FirstName, p.LastName });
			var query6 = db.Person.Select(p => new { p.FirstName, p.LastName });

			var query = query1.Concat(query2.Concat(query3)).Concat(query4.Concat(query5).Concat(query6));
			_ = query.ToArray();

			var selectQuery = query.GetSelectQuery()!;

			selectQuery.HasSetOperators.ShouldBeTrue();
			selectQuery.SetOperators.Count.ShouldBe(5);
		}

		// only pgsql and CH support all 6 operators right now
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3359")]
		public void MixedSetOperatorsKeepOperatorAndQueryOrder([IncludeDataSources(TestProvName.AllPostgreSQL/*, TestProvName.AllClickHouse*/)] string context)
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
			sql.ShouldContain("SELECT", Exactly.Times(6 + 4));

			// operators generated
			sql.ShouldContain("UNION ALL", Exactly.Once());
			sql.ShouldContain("UNION", Exactly.Twice());
			sql.ShouldContain("INTERSECT", Exactly.Twice());
			sql.ShouldContain("INTERSECT ALL", Exactly.Once());
			sql.ShouldContain("EXCEPT", Exactly.Once());

			// operators order correct
			var i1 = sql.IndexOf("UNION");
			var i2 = sql.IndexOf("UNION ALL");
			var i3 = sql.IndexOf("INTERSECT");
			var i4 = sql.IndexOf("INTERSECT ALL");
			var i5 = sql.IndexOf("EXCEPT");
			Assert.That(i1, Is.Not.EqualTo(-1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(i1, Is.LessThan(i2));
				Assert.That(i2, Is.LessThan(i3));
				Assert.That(i3, Is.LessThan(i4));
				Assert.That(i4, Is.LessThan(i5));
			}

			// queries order correct
			i1 = sql.IndexOf("q1");
			i2 = sql.IndexOf("q2");
			i3 = sql.IndexOf("q3");
			i4 = sql.IndexOf("q4");
			i5 = sql.IndexOf("q5");
			Assert.That(i1, Is.Not.EqualTo(-1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(i1, Is.LessThan(i2));
				Assert.That(i2, Is.LessThan(i3));
				Assert.That(i3, Is.LessThan(i4));
				Assert.That(i4, Is.LessThan(i5));
			}
		}

		[Test(Description = "Ordering of selected columns inside UnionAll breaks query building")]
		public void ColumnOrderInUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Parent.Select(p => new { Column1 = 123, Id = p.ParentID });
			var query2 = db.Parent.Select(p => new { Column1 = 234, Id = p.ParentID });

			var query3 = query1.UnionAll(query2).Select(x => new { Id = x.Id, Column2 = 222, Column1 = x.Column1 });
			var query4 = query1.UnionAll(query2).Select(x => new { Id = x.Id, Column2 = 333, Column1 = x.Column1 });

			var result = query3.UnionAll(query4);

			AssertQuery(result);
		}

		public record class RecordClass (int Id, string FirstName, string LastName);

		public class RecordLikeClass
		{
			public RecordLikeClass(int Id, string FirstName, string LastName)
			{
				this.Id        = Id;
				this.FirstName = FirstName;
				this.LastName  = LastName;
			}

			public int    Id        { get; }
			public string FirstName { get; }
			public string LastName  { get; }
		}

		public record class NameRecord (string FirstName, string LastName);

		public record class RecordClassWithNestedRecord (int Id, NameRecord Name);

		[Test(Description = "record type support")]
		public void ConcatRecordClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Person.Select(p => new RecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(db.Person.Select(p => new RecordClass(p.ID, p.FirstName, p.LastName)));

			AssertQuery(query);
		}

		[Test(Description = "record type support")]
		public void ConcatRecordClassNested([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person.Select(p => new RecordClassWithNestedRecord(p.ID, new NameRecord(p.FirstName, p.LastName)))
				.Concat(db.Person.Select(p => new RecordClassWithNestedRecord(p.ID, new NameRecord(p.LastName, p.FirstName))));

			AssertQuery(query);
		}

		[Test(Description = "record type support")]
		public void ConcatRecordLikeClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqualWithComparer(
				Person.Select(p => new RecordLikeClass(p.ID, p.FirstName, p.LastName))
				.Concat(Person.Select(p => new RecordLikeClass(p.ID, p.FirstName, p.LastName))),

				db.Person.Select(p => new RecordLikeClass(p.ID, p.FirstName, p.LastName))
				.Concat(db.Person.Select(p => new RecordLikeClass(p.ID, p.FirstName, p.LastName))));
		}

		[Table]
		public class CalculatedColumnSetTable
		{
			[PrimaryKey                      ] public int     Id        { get; set; }
			[Column(SkipOnEntityFetch = true)] public string? FirstName { get; set; }
			[Column(SkipOnEntityFetch = true)] public string? LastName  { get; set; }
			[Column(CanBeNull = false)       ] public string  Text      { get; set; } = null!;

			[ExpressionMethod(nameof(FullNameExpr), IsColumn = true)]
			public string FullName { get; set; } = null!;

			private static Expression<Func<CalculatedColumnSetTable, string>> FullNameExpr() => entity => entity.FirstName + " " + entity.LastName;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3323")]
		public void ConcatCalculatedColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CalculatedColumnSetTable>();
			tb.Insert(() => new CalculatedColumnSetTable()
			{
				Id        = 1,
				FirstName = "one",
				LastName  = "two",
				Text      = "text"
			});

			var res = tb.Concat(tb).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].FullName, Is.EqualTo("one two"));
				Assert.That(res[1].FullName, Is.EqualTo("one two"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3323")]
		public void ConcatCalculatedColumnWithPlainColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CalculatedColumnSetTable>();
			tb.Insert(() => new CalculatedColumnSetTable()
			{
				Id        = 1,
				FirstName = "one",
				LastName  = "two",
				Text      = "text"
			});

			var query1 = tb.Select(r => new { r.Id, Text = r.FullName });
			var query2 = tb.Select(r => new { Id = r.Id + 1, Text = r.Text });

			var res = query1.Concat(query2).ToArray().OrderBy(r => r.Id).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Text, Is.EqualTo("one two"));
				Assert.That(res[1].Text, Is.EqualTo("text"));
			}

			res = query2.Concat(query1).ToArray().OrderBy(r => r.Id).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Text, Is.EqualTo("one two"));
				Assert.That(res[1].Text, Is.EqualTo("text"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2505")]
		public void UnionAllWithFilterOnCalculatedMemberAndOrdering([DataSources] string context)
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
		private class SetLiteralTypingTable
		{
			[PrimaryKey                         ] public int     Id  { get; set; }
			// by default we generate N-literal, which is not compatible with (var)char
			[Column(DataType = DataType.VarChar)] public string? Str { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatLiteralTypedByOtherQuery([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SetLiteralTypingTable>();

			var query1 = tb.Select(p => new { p.Id, p.Str                });
			var query2 = tb.Select(p => new { p.Id, Str = (string?)"str" });

			query1.Concat(query2).ToArray();
			if (db is TestDataConnection dc1)
				dc1.LastQuery!.ShouldNotContain("N'");

			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.ShouldNotContain("N'");
		}

		[ActiveIssue(3360, Configurations = [TestProvName.AllDB2], ErrorTypeName = "IBM.Data.Db2.DB2Exception", ErrorMessage = "SQL0604N")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatLiteralTypedByOtherQueryAllProviders([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SetLiteralTypingTable>();

			var query1 = tb.Select(p => new { p.Id, p.Str                });
			var query2 = tb.Select(p => new { p.Id, Str = (string?)"str" });

			query1.Concat(query2).ToArray();
			query2.Concat(query1).ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatLiteralTypedByProjectionProperty([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SetLiteralTypingTable>();

			var query1 = tb.Select(p => new SetLiteralTypingTable() { Id = p.Id, Str = (string?)"str1" });
			var query2 = tb.Select(p => new SetLiteralTypingTable() { Id = p.Id, Str = (string?)"str2" });

			query1.Concat(query2).ToArray();
			if (db is TestDataConnection dc1)
				dc1.LastQuery!.ShouldNotContain("N'");

			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.ShouldNotContain("N'");
		}

		[ActiveIssue(3360, Configurations = [TestProvName.AllDB2], ErrorTypeName = "IBM.Data.Db2.DB2Exception", ErrorMessage = "SQL0604N")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatLiteralTypedByProjectionPropertyAllProviders([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SetLiteralTypingTable>();

			var query1 = tb.Select(p => new SetLiteralTypingTable() { Id = p.Id, Str = (string?)"str1" });
			var query2 = tb.Select(p => new SetLiteralTypingTable() { Id = p.Id, Str = (string?)"str2" });

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
		public class SetColumnTypesTable
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

			public static SetColumnTypesTable[] Items = new[]
			{
				new SetColumnTypesTable() { Id = 1 },
				new SetColumnTypesTable() { Id = 2, Byte = 1, ByteN = 2, Guid = TestData.Guid1, GuidN = TestData.Guid2, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = true, BoolN = false },
				new SetColumnTypesTable() { Id = 4, Byte = 3, ByteN = 4, Guid = TestData.Guid3, GuidN = TestData.Guid1, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = false, BoolN = true },
			};
		}

		private record SetColumnTypesRecord(int Id, byte? Byte, byte? ByteN, Guid? Guid, Guid? GuidN, InvalidColumnIndexMappingEnum1? Enum, InvalidColumnIndexMappingEnum2? EnumN, bool? Bool, bool? BoolN);

		[ActiveIssue(Configuration = TestProvName.AllSybase, ErrorTypeName = "AdoNetCore.AseClient.AseException", ErrorMessage = "does not allow null",
			Details = "no-issue: a Sybase BIT column cannot hold NULL, so a bool? column cannot be created or compared against null. Update BoolN handling for sybase.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatNullLiteralsInFirstQuery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SetColumnTypesTable.Items);

			var query = table.Where(r => r.Id == 1)
				.Select(r => new SetColumnTypesRecord(r.Id, null, null, null, null, null, null, null, null))
				.Concat(
					table.Where(r => r.Id == 2)
						.Select(r => new SetColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(1));
				Assert.That(data[0].Byte, Is.Null);
				Assert.That(data[0].ByteN, Is.Null);
				Assert.That(data[0].Guid, Is.Null);
				Assert.That(data[0].GuidN, Is.Null);
				Assert.That(data[0].Enum, Is.Null);
				Assert.That(data[0].EnumN, Is.Null);
				Assert.That(data[0].Bool, Is.Null);
			}

			if (!context.IsAnyOf(TestProvName.AllSybase))
				Assert.That(data[0].BoolN, Is.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[1].Id, Is.EqualTo(2));
				Assert.That(data[1].Byte, Is.EqualTo(1));
				Assert.That(data[1].ByteN, Is.EqualTo(2));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid2));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.True);
				Assert.That(data[1].BoolN, Is.False);
			}
		}

		// Only the direct transport fails: over LinqService the seed insert goes through and the query answers.
		[ActiveIssue(Configuration = TestProvName.AllSybase, SkipForLinqService = true,
			ErrorTypeName = "AdoNetCore.AseClient.AseException", ErrorMessage = "does not allow null values.",
			Details = "no-issue: a Sybase BIT column cannot hold NULL, so the seed row with a null bool? cannot be inserted. Update BoolN handling for sybase.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatDoubleColumnSelectionInFirstQuery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SetColumnTypesTable.Items);

			var query = table.Where(r => r.Id == 2)
				.Select(r => new SetColumnTypesRecord(r.Id, r.Byte, r.Byte, r.Guid, r.Guid, null, null, r.Bool, r.Bool))
				.Concat(
					table.Where(r => r.Id == 4)
						.Select(r => new SetColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(2));
				Assert.That(data[0].Byte, Is.EqualTo(1));
				Assert.That(data[0].ByteN, Is.EqualTo(1));
				Assert.That(data[0].Guid, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].Enum, Is.Null);
				Assert.That(data[0].EnumN, Is.Null);
				Assert.That(data[0].Bool, Is.True);
				Assert.That(data[0].BoolN, Is.True);

				Assert.That(data[1].Id, Is.EqualTo(4));
				Assert.That(data[1].Byte, Is.EqualTo(3));
				Assert.That(data[1].ByteN, Is.EqualTo(4));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid3));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.False);
				Assert.That(data[1].BoolN, Is.True);
			}
		}

		// SQLite was gated here too and now passes on all four of its cases, so only the Sybase half is left - and
		// there, as in ConcatDoubleColumnSelectionInFirstQuery, only the direct transport fails.
		[ActiveIssue(Configuration = TestProvName.AllSybase, SkipForLinqService = true,
			ErrorTypeName = "AdoNetCore.AseClient.AseException", ErrorMessage = "does not allow null values.",
			Details = "no-issue: a Sybase BIT column cannot hold NULL, so the seed row with a null bool? cannot be inserted. Update BoolN handling for sybase.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void ConcatLiteralsInFirstQuery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SetColumnTypesTable.Items);

			var query = table.Where(r => r.Id == 2)
				.Select(r => new SetColumnTypesRecord(r.Id, 5, 5, new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), InvalidColumnIndexMappingEnum1.Value, InvalidColumnIndexMappingEnum2.Value, true, false))
				.Concat(
					table.Where(r => r.Id == 4)
						.Select(r => new SetColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN)))
				.OrderBy(r => r.Id);

			var data = query.ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(2));
				Assert.That(data[0].Byte, Is.EqualTo(5));
				Assert.That(data[0].ByteN, Is.EqualTo(5));
				Assert.That(data[0].Guid, Is.EqualTo(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29")));
				Assert.That(data[0].GuidN, Is.EqualTo(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30")));
				Assert.That(data[0].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[0].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[0].Bool, Is.True);
				Assert.That(data[0].BoolN, Is.False);

				Assert.That(data[1].Id, Is.EqualTo(4));
				Assert.That(data[1].Byte, Is.EqualTo(3));
				Assert.That(data[1].ByteN, Is.EqualTo(4));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid3));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.False);
				Assert.That(data[1].BoolN, Is.True);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2451")]
		public void ConcatComputedColumnWithoutVarCharConvert([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
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
				dc1.LastQuery!.ShouldNotContain("Convert(VarChar");
			query2.Concat(query1).ToArray();
			if (db is TestDataConnection dc2)
				dc2.LastQuery!.ShouldNotContain("Convert(VarChar");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2451")]
		public void ConcatComputedColumnAllProviders([DataSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3346")]
		public void UnionEntityWithComplexColumn([DataSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3346")]
		public void UnionEntityWithComplexColumnCount([DataSources] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3150")]
		public void ConcatPreservesConstantsInNestedProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Where(p => p.ID == 1).Select(p => new { p.ID, Name = new { p.FirstName, Marker = "id=1" } });
			var query2 = db.Person.Where(p => p.ID == 2).Select(p => new { p.ID, Name = new { p.FirstName, Marker = "id=2" } });

			var result = query1.Concat(query2).AsEnumerable().OrderBy(x => x.ID).ToArray();

			result.Length.ShouldBe(2);
			result[0].Name.Marker.ShouldBe("id=1");
			result[1].Name.Marker.ShouldBe("id=2");
		}

		public class RankedModel
		{
			public int    Id   { get; set; }
			public string Name { get; set; } = null!;
		}

		public class RankedData<T>
		{
			public long Rank  { get; set; }
			public T    Model { get; set; } = default!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2948")]
		public void ConcatMemberInitWithNestedMemberBinding([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var main = (from p in db.Person
						select new RankedData<RankedModel>()
						{
							Model = { Id = p.ID, Name = p.FirstName },
							Rank  = Sql.Ext.RowNumber().Over().PartitionBy(p.ID).OrderBy(p.ID).ToValue()
				}).Where(x => x.Rank == 1).Select(x => x.Model);

			var first  = main.Where(x => x.Id != 2);
			var second = main.Where(x => x.Id == 2).OrderByDescending(x => x.Name).Take(1);
			var third  = main.Where(x => x.Id != 3).OrderBy(x => x.Name).Take(1);

			var res = first.Concat(second).Concat(third).ToList();

			// order is not guaranted by DB
			res = res.OrderBy(r => r.Id).ToList();

			res.Count.ShouldBe(5);
			res[0].Id.ShouldBe(1);
			res[0].Name.ShouldBe("John");
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2932")]
		public void ConcatAnySubqueryColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(p => new { p.ChildID, Sub = p.GrandChildren.Any() });

			query.Concat(query).ToArray();
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2932")]
		public void ConcatAnySubqueryColumnInConditional([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(p => new { p.ChildID, Sub = p.GrandChildren.Any() ? true : false });

			query.Concat(query).ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void UnionWithOrderedFirstQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = ((from item in db.Person select item)
				.OrderBy(i => i.ID))
				.Union((from item in db.Person select item));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("ORDER BY");

			query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void UnionWithOrderedSecondQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = (from item in db.Person select item)
				.Union((from item in db.Person select item)
				.OrderBy(i => i.ID));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("ORDER BY");

			query.ToList();
		}

		// disabled databases doesn't support order by in specified position
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void UnionAllWithOrderedFirstQuery([DataSources(TestProvName.AllSybase, TestProvName.AllSqlServer, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var query = ((from item in db.Person select item)
				.OrderBy(i => i.ID))
				.UnionAll((from item in db.Person select item));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER BY", Exactly.Once());
			sql.Substring(sql.IndexOf("ORDER BY")).ShouldContain("UNION", Exactly.Once());

			query.ToList();
		}

		// disabled databases doesn't support order by in specified position
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void UnionAllWithOrderedSecondQuery([DataSources(TestProvName.AllSybase, TestProvName.AllSqlServer, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var query = (from item in db.Person select item)
				.UnionAll((from item in db.Person select item)
				.OrderBy(i => i.ID));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER BY", Exactly.Once());
			sql.ShouldContain("UNION", Exactly.Once());
			sql.Substring(sql.IndexOf("ORDER BY")).ShouldNotContain("UNION");

			query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2511")]
		public void ConcatLoadWithPatientAndTake([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var res = db.Person.LoadWith(p => p.Patient).Concat(db.Person.LoadWith(p => p.Patient).Take(2)).ToArray();

			Assert.That(res, Has.Length.EqualTo(6));
			Assert.That(res.Where(r => r.ID == 2).Count(), Is.EqualTo(2));
			var pat = res.Where(r => r.ID == 2).First();
			Assert.That(pat.Patient, Is.Not.Null);
			Assert.That(pat.Patient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
			pat = res.Where(r => r.ID == 2).Skip(1).First();
			Assert.That(pat.Patient, Is.Not.Null);
			Assert.That(pat.Patient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2511")]
		public void ConcatTakeOfProjectedPersonWithPatientAndLoadWith([DataSources] string context)
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

			res.Length.ShouldBe(6);

			var pat = res.Where(r => r.ID == 2).First();
			pat.Patient.ShouldNotBeNull();

			pat = res.Where(r => r.ID == 2).Skip(1).Single();
			pat.Patient.ShouldNotBeNull();

			pat.Patient!.Diagnosis.ShouldBe("Hallucination with Paranoid Bugs' Delirium of Persecution");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2511")]
		public void ConcatTakeOfProjectedPersonWithoutPatientAndLoadWith([DataSources] string context)
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
				.OrderBy(x => x.ID)
				.ToArray();

			res.Length.ShouldBe(6);

			var patients = res.Where(r => r.ID == 2).ToList();
			patients.Any(p => p.Patient != null).ShouldBeTrue();
			patients.Any(p => p.Patient == null).ShouldBeTrue();
		}

		[Test]
		public void ConcatEntities([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query =
					(from p in db.Parent where p.ParentID == 1 select p).Concat(
					(from p in db.Parent where p.ParentID == 2 select p));

			AssertQuery(query);
		}

		class ConcatEntity
		{
			public int? IntValue { get; set; }

			public class ConcatSubEntity
			{
				public int Id { get; set; }
				public int? Value { get; set; }
			}

			public ConcatSubEntity? Entity { get; set; }
		}

		[Test]
		public void ConcatEqualSelects([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query =
					(from p in db.Parent
					 where p.ParentID == 1
					 select new ConcatEntity
					 {
						 IntValue = p.ParentID + 1,
						 Entity = new ConcatEntity.ConcatSubEntity
						 {
							 Id = p.ParentID
						 },
					 })
					.Concat(
					from p in db.Parent
					where p.ParentID == 2
					select new ConcatEntity
					{
						Entity = new ConcatEntity.ConcatSubEntity
						{
							Id = p.ParentID
						},
					});

			AssertQuery(query);
		}

		[InheritanceMapping(Code = 1, Type = typeof(SetEntityA))]
		[InheritanceMapping(Code = 2, Type = typeof(SetEntityB))]
		[InheritanceMapping(Code = 3, Type = typeof(SetEntityC))]
		abstract class SetEntityBase
		{
			[Column]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public abstract int Discriminator { get; }
		}

		class SetEntityA : SetEntityBase
		{
			[Column]
			public          int? IntValue      { get; set; }

			public override int  Discriminator => 1;
		}

		class SetEntityB : SetEntityBase
		{
			[Column]
			public          string? StrValue      { get; set; }

			public override int  Discriminator => 2;
		}

		class SetEntityC : SetEntityBase
		{
			[Column]
			public double? DoubleValue { get; set; }

			public override int Discriminator => 3;
		}

		[Test]
		public void ConcatInheritance([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var items = new SetEntityBase[]
			{
				new SetEntityA{Id = 1, IntValue = 11},
				new SetEntityB{Id = 2, StrValue = "Str22" },
				new SetEntityC{Id = 3, DoubleValue = 33.33 }
			};

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(items);
			var query =
					(from t1 in table.Where(x => x.Id == 1) select t1)
					.Concat(
						from t2 in table.Where(x => x.Id == 2) select t2)
					.Concat(
						from t3 in table.Where(x => x.Id == 3) select t3);

			var result = query.ToList();
			result[0].ShouldBeOfType<SetEntityA>();
			result[1].ShouldBeOfType<SetEntityB>();
			result[2].ShouldBeOfType<SetEntityC>();
		}

		[Test]
		public void ConcatBrokenInheritance([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var items = new SetEntityBase[]
			{
				new SetEntityA{Id = 1, IntValue    = 11},
				new SetEntityB{Id = 2, StrValue    = "Str22" },
				new SetEntityC{Id = 3, DoubleValue = 33.33 }
			};

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(items);
			var query =
					(from t1 in table.Where(x => x.Id == 1) select t1)
					.Concat(
						from t2 in table.Where(x => x.Id == 2) select t2)
					.Concat(
						from t3 in table.Where(x => x.Id == 3)
						select new SetEntityC
						{
							Id = t3.Id,
							DoubleValue = 4.44
						});

			var result = query.ToList();
			result[0].ShouldBeOfType<SetEntityA>();
			result[1].ShouldBeOfType<SetEntityB>();
			result[2].ShouldBeOfType<SetEntityC>();
		}

		[Test]
		public void UnionFeedingLeftJoinWithConditionalColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = from x in db.Person
					 where x.ID == 1
					 select new
					 {
						 x.ID,
						 FirstName = "A",
						 OK = x.FirstName == "123" ? "Y" : "N",
					 };
			var q2 = from x in db.Person
					 where x.ID == 2
					 select new
					 {
						 x.ID,
						 x.FirstName,
						 OK = "N"
					 };
			var query = q1.Union(q2);
			var q3 = from x in query
					 from y in db.Person.LeftJoin(t => t.ID == x.ID)
					 where x.ID == 3
					 select new
					 {
						 x.ID,
						 x.OK,
						 FirstName = x.FirstName == "ddd" ? y.FirstName : x.FirstName,
					 };

			q3.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3738")]
		public void ConcatNullStringConstantWithConcatenatedString([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Select(x => new
			{
				Id     = (string?)("I-" + x.ID),
				Name   = x.FirstName
			});
			var query2 = db.Person.Select(x => new
			{
				Id   = (string?)null,
				Name = "QUASI-" + x.FirstName,
			});

			var resultingQuery = query1.Concat(query2);

			resultingQuery.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4225")]
		public void UnionAllEntitiesWithCastNullPlaceholders([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Select(x => new
			{
				person = (Person?)x,
				patient = (Patient?)null
			});
			var query2 = db.Patient.Select(x => new
			{
				person = (Person?)null,
				patient = (Patient?)x
			});

			var q = query1.UnionAll(query2).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4225")]
		public void UnionAllEntitiesWithNullForgivingPlaceholders([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Select(x => new
			{
				person = x,
				patient = (Patient?)null!
			});
			var query2 = db.Patient.Select(x => new
			{
				person = (Person?)null!,
				patient = x
			});

			var q = query1.UnionAll(query2).ToList();
		}

		[Test]
		public void ConcatCount_ShouldNotRemoveSingleColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = db.Person.Select(_ => _.ID)
				.Concat(db.Parent.Select(_ => _.ParentID))
				.Count();

			var q2 = Person.Select(_ => _.ID)
				.Concat(Parent.Select(_ => _.ParentID))
				.Count();

			Assert.That(q1, Is.EqualTo(q2));
		}

		[Test]
		public void UnionCount_ShouldNotRemoveSingleColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = db.Person.Select(_ => _.ID)
				.Union(db.Parent.Select(_ => _.ParentID))
				.Count();

			var q2 = Person.Select(_ => _.ID)
				.Union(Parent.Select(_ => _.ParentID))
				.Count();

			Assert.That(q1, Is.EqualTo(q2));
		}

		[Test]
		public void ConcatCountExt_ShouldNotRemoveSingleColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = db.Person.Select(_ => _.ID)
				.Concat(db.Parent.Select(_ => _.ParentID))
				.Select(_ => _)
				.CountExt(_ => _);

			var q2 = Person.Select(_ => _.ID)
				.Concat(Parent.Select(_ => _.ParentID))
				.Count();

			Assert.That(q1, Is.EqualTo(q2));
		}

		[Test]
		public void ConcatSumTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q1 = db.Person.Select(_ => _.ID)
				.Concat(db.Parent.Select(_ => _.ParentID))
				.Sum();

			var q2 = Person.Select(_ => _.ID)
				.Concat(Parent.Select(_ => _.ParentID))
				.Sum();

			Assert.That(q1, Is.EqualTo(q2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4220")]
		public void UnionAllSiblingEntitiesWithTypedNullPlaceholders([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable<ConcreteA>();
			using var tb = db.CreateLocalTable<ConcreteB>();

			db.Insert(new ConcreteA { Id = 1, AOnly = "a only" });
			db.Insert(new ConcreteB { Id = 2, BOnly = "b only" });

			var result = ta.Select(e => new { A = (ConcreteA?)e, B = (ConcreteB?)null })
				.UnionAll(tb.Select(e => new { A = (ConcreteA?)null, B = (ConcreteB?)e }))
				.ToArray()
				.Select(e => (Abstr?)e.A ?? e.B)
				.OrderBy(e => e!.Id)
				.ToArray()!;

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0]!.Id, Is.EqualTo(1));
				Assert.That(result[0], Is.InstanceOf<ConcreteA>());
				Assert.That(((ConcreteA)result[0]!).AOnly, Is.EqualTo("a only"));

				Assert.That(result[1]!.Id, Is.EqualTo(2));
				Assert.That(result[1], Is.InstanceOf<ConcreteB>());
				Assert.That(((ConcreteB)result[1]!).BOnly, Is.EqualTo("b only"));
			}
		}

		record Abstr
		{
			[PrimaryKey] public int Id { get; set; }
		}

		[Table]
		sealed record ConcreteA : Abstr
		{
			[Column] public string? AOnly { get; set; }
		}

		[Table]
		sealed record ConcreteB : Abstr
		{
			[Column] public string? BOnly { get; set; }
		}

		class UnionBillClient
		{
			[PrimaryKey] public int Id { get; set; }
			public string? Name { get; set; }

			public static readonly UnionBillClient[] Data =
			[
				new UnionBillClient() { Id = 1, Name = "Client 1" },
				new UnionBillClient() { Id = 2, Name = "Client 2" },
			];
		}

		class UnionBillContract
		{
			[PrimaryKey] public int Id { get; set; }
			public int IdClient { get; set; }

			public static readonly UnionBillContract[] Data =
			[
				new UnionBillContract() { Id = 1, IdClient = 1 },
				new UnionBillContract() { Id = 2, IdClient = 2 },
			];
		}

		[Table("UnionBill")]
		class UnionBill
		{
			[PrimaryKey] public int Id { get; set; }

			// either IdContract or IdClient will be null
			[Column] public int? IdContract { get; set; }
			[Column] public int? IdClient { get; set; }

			[Column] public decimal Sum { get; set; }

			public static readonly UnionBill[] Data =
			[
				new UnionBill() { Id = 1, IdClient = 1, IdContract = null },
				new UnionBill() { Id = 2, IdClient = null, IdContract = 2 },
				new UnionBill() { Id = 3 },
				new UnionBill() { Id = 4, IdClient = 1, IdContract = 2 },
			];
		}

		interface IUnionBill
		{
			int Id { get; set; }
			int? IdContract { get; set; }
			int? IdClient { get; set; }
			decimal Sum { get; set; }
		}

		interface IUnionBillWithClient : IUnionBill
		{
			public UnionBillClient Client { get; set; }
		}

		class UnionBillWithClient : UnionBill
		{
			public UnionBillClient Client { get; set; } = null!;
		}

		[Table("UnionBill")]
		class LegacyUnionBill : UnionBill, IUnionBillWithClient
		{
			[Association(ThisKey = nameof(IdClient), OtherKey = nameof(Client.Id), CanBeNull = false)]
			public UnionBillClient Client { get; set; } = null!;
		}

		[Table("UnionBill")]
		class ModernUnionBill : UnionBill, IUnionBillWithClient
		{
			[Association(ThisKey = nameof(IdContract), OtherKey = nameof(Contract.Id), CanBeNull = false)]
			public UnionBillContract Contract { get; set; } = null!;

			[Association(ExpressionPredicate = nameof(ModernBill_Client_Expr), CanBeNull = false)]
			public UnionBillClient Client { get; set; } = null!;

			static Expression<Func<ModernUnionBill, UnionBillClient, bool>> ModernBill_Client_Expr
				=> (b, cl) => b.Contract.IdClient == cl.Id;
		}

		[ActiveIssue(4620, ErrorTypeName = "System.ArgumentException", ErrorMessage = "Interface not found.",
			Details = "a union over associations of several classes implementing one interface cannot resolve it - #4620's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4620")]
		public void UnionAllAssociationsOfInterfaceImplementations([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(UnionBill.Data);
			using var t1 = db.CreateLocalTable(UnionBillClient.Data);
			using var t2 = db.CreateLocalTable(UnionBillContract.Data);

			var union = db.GetTable<LegacyUnionBill>().Where(b => b.IdClient != null)
				.UnionAll<IUnionBillWithClient>(
					db.GetTable<ModernUnionBill>().Where(b => b.IdContract != null));

			var result = union.Select(b => new { Id = b.Id, b.Client.Name }).OrderBy(r => r.Id).ThenBy(r => r.Name).ToArray();

			Assert.That(result, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Name, Is.EqualTo("Client 1"));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Name, Is.EqualTo("Client 2"));
				Assert.That(result[2].Id, Is.EqualTo(4));
				Assert.That(result[2].Name, Is.EqualTo("Client 1"));
				Assert.That(result[3].Id, Is.EqualTo(4));
				Assert.That(result[3].Name, Is.EqualTo("Client 2"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4620")]
		public void UnionAllAssociationsProjectedToCommonClass([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(UnionBill.Data);
			using var t1 = db.CreateLocalTable(UnionBillClient.Data);
			using var t2 = db.CreateLocalTable(UnionBillContract.Data);

			var union = db
					.GetTable<LegacyUnionBill>()
					.Where(b => b.IdClient != null)
					.Select(b => new UnionBillWithClient { Id = b.Id, IdClient = b.IdClient, IdContract = b.IdContract, Sum = b.Sum, Client = b.Client })
				.UnionAll<UnionBillWithClient>(
					db.GetTable<ModernUnionBill>()
					.Where(b => b.IdContract != null)
					.Select(b => new UnionBillWithClient { Id = b.Id, IdClient = b.IdClient, IdContract = b.IdContract, Sum = b.Sum, Client = b.Client }));

			var result = union.Select(b => new { Id = b.Id, b.Client.Name }).OrderBy(r => r.Id).ThenBy(r => r.Name).ToArray();

			Assert.That(result, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Name, Is.EqualTo("Client 1"));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Name, Is.EqualTo("Client 2"));
				Assert.That(result[2].Id, Is.EqualTo(4));
				Assert.That(result[2].Name, Is.EqualTo("Client 1"));
				Assert.That(result[3].Id, Is.EqualTo(4));
				Assert.That(result[3].Name, Is.EqualTo("Client 2"));
			}
		}

		#region Asymmetric branches

		sealed class ConvertedFlagRow
		{
			[PrimaryKey] public int   Id   { get; set; }
			[Column    ] public bool? Flag { get; set; }

			public static readonly ConvertedFlagRow[] Data =
			{
				new() { Id = 1, Flag = true  },
				new() { Id = 2, Flag = false },
			};
		}

		/// <summary>
		/// A branch supplying a plain <c>NULL</c> where the other reads a column through a conversion.
		/// </summary>
		/// <remarks>
		/// The branches are deliberately not the same query. Every other set-operation test here unions a query with
		/// itself, so both sides carry the same descriptor and agree by reference alone - which is exactly the shape
		/// that cannot catch a divergence check reading too much into an absent descriptor. A <c>NULL</c> is stored
		/// in no terms and read through no conversion, so it cannot disagree with what the other branch declares.
		/// </remarks>
		[Test]
		public void UnionPadsAConvertedColumnWithNull([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConvertedFlagRow>()
					.Property(e => e.Flag)
						.HasConversion(v => v == true ? 'Y' : 'N', p => (bool?)(p == 'Y'))
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable(ConvertedFlagRow.Data);

			var withValue = t.Select(x => new { x.Id, x.Flag });
			var withNull  = t.Select(x => new { x.Id, Flag = (bool?)null });

			var result = withValue.Union(withNull).ToArray();

			result.Length.ShouldBe(4);
			result.Count(r => r.Flag == null).ShouldBe(2);
			result.Single(r => r.Id == 1 && r.Flag != null).Flag.ShouldBe(true);
			result.Single(r => r.Id == 2 && r.Flag != null).Flag.ShouldBe(false);
		}

		/// <summary>
		/// A branch supplying a constant where the other reads a column through a conversion is refused rather than
		/// compared on unequal terms.
		/// </summary>
		/// <remarks>
		/// The companion to the <c>NULL</c> case above, and the one that behaves differently. A <c>NULL</c> is stored
		/// in no terms and read through none, so it cannot disagree with what the other branch declares. A constant
		/// has terms of its own and no descriptor to state them, and a set operation other than <c>UNION ALL</c>
		/// decides which rows survive by comparing values in the database - where a raw <see langword="true"/> and a column
		/// written as <c>'Y'</c> are two different values.
		/// <para>
		/// Which is what that comparison did: before this the query answered two rows where the CLR says one, the
		/// constant and the column each coming back through the converter as though they had matched nothing. A
		/// refusal replaces a wrong answer here rather than a right one, so the loud form is the improvement.
		/// </para>
		/// <para>
		/// What neither form does is convert the constant through the column's descriptor and answer the single
		/// correct row. That is a design question about descriptor-less branches rather than a defect in the
		/// refusal, and it is left open.
		/// </para>
		/// <para>
		/// Asked of every operation that compares, not only of <c>Union</c>, because the refusal is decided by a
		/// single equality against <c>UnionAll</c> - so the four others reach it by construction, and the message
		/// names the operation it refused. <c>ExceptAll</c> and <c>IntersectAll</c> are the pair worth having:
		/// they are the ones a reader most expects to behave like <c>UnionAll</c>, and the ones where reading each
		/// branch on its own terms inside a multiset difference would answer a wrong row set rather than refuse.
		/// </para>
		/// </remarks>
		[Test]
		public void SetOperationRefusesAConstantBranchAgainstAConvertedColumn(
			[DataSources] string context,
			[Values(
				SetOperation.Union,
				SetOperation.Except,
				SetOperation.ExceptAll,
				SetOperation.Intersect,
				SetOperation.IntersectAll)]
			SetOperation operation)
		{
			static IQueryable<T> Combine<T>(IQueryable<T> first, IQueryable<T> second, SetOperation operation)
			{
				return operation switch
				{
					SetOperation.Union        => first.Union(second),
					SetOperation.Except       => first.Except(second),
					SetOperation.ExceptAll    => first.ExceptAll(second),
					SetOperation.Intersect    => first.Intersect(second),
					SetOperation.IntersectAll => first.IntersectAll(second),
					_                         => throw new InvalidOperationException($"Unhandled set operation {operation}."),
				};
			}

			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConvertedFlagRow>()
					.Property(e => e.Flag)
						.HasConversion(v => v == true ? 'Y' : 'N', p => (bool?)(p == 'Y'))
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable(ConvertedFlagRow.Data);

			var fromColumn   = t.Select(x => new { x.Id, x.Flag });
			var fromConstant = t.Select(x => new { x.Id, Flag = (bool?)true });

			var combined = () => Combine(fromColumn, fromConstant, operation).ToArray();

			var refusal = combined.ShouldThrow<LinqToDBException>();

			refusal.Message.ShouldContain("in different terms");
			refusal.Message.ShouldContain(operation.ToString());
		}

		/// <summary>
		/// The one set operation that has no comparison to get wrong.
		/// </summary>
		/// <remarks>
		/// <c>UNION ALL</c> hands every row back and reads each on its own branch's terms, so a branch supplying a
		/// constant where the other reads a converted column is kept apart rather than refused. The control for the
		/// refusal above, and the reason that refusal is keyed on this operation alone.
		/// </remarks>
		[Test]
		public void UnionAllReadsAConstantBranchOnItsOwnTerms([DataSources(TestProvName.AllSybase)] string context)
		{
			// Not asked of Sybase, and the reason is the same gap seen from the other side: the constant is emitted
			// as the value it is rather than through the column's conversion, so it arrives as BIT against a column
			// stored as a character - which that provider rejects outright instead of comparing. Where the storage
			// tolerates both, the rows come back read on their own branch's terms.
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConvertedFlagRow>()
					.Property(e => e.Flag)
						.HasConversion(v => v == true ? 'Y' : 'N', p => (bool?)(p == 'Y'))
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable(ConvertedFlagRow.Data);

			var fromColumn   = t.Select(x => new { x.Id, x.Flag });
			var fromConstant = t.Select(x => new { x.Id, Flag = (bool?)true });

			var all = fromColumn.UnionAll(fromConstant).ToArray();

			all.Length.ShouldBe(4);
			all.Count(r => r.Flag == true).ShouldBe(3);
			all.Count(r => r.Flag == false).ShouldBe(1);
		}

		#endregion

		sealed class ScaledDecimalRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 2)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 2)]
			public decimal Value { get; set; }
		}

		/// <summary>
		/// A literal branch finer than an aggregated column's scale keeps its digits: MAX is a computed value with no
		/// stored scale to match.
		/// </summary>
		/// <remarks>
		/// YDB refuses to union <c>Decimal(18,2)</c> with the different decimal type the literal is written with, on master too.
		/// </remarks>
		[Test]
		public void LiteralBesideAggregateKeepsValueScale([DataSources(TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new ScaledDecimalRow { Id = 1, Value = 1.00m } });

			var values = t
				.GroupBy(r => r.Id)
				.Select(g => g.Max(r => r.Value))
				.Concat(t.Select(r => 1.004m))
				.ToArray();

			AreEqualWithinDelta([1.00m, 1.004m], values);

			var conditional = t
				.Select(r => r.Id > 0 ? r.Value : r.Value + 1)
				.Concat(t.Select(r => 1.004m))
				.ToArray();

			AreEqualWithinDelta([1.00m, 1.004m], conditional);
		}

		sealed class FineDecimalRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 12)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 12)]
			public decimal Value { get; set; }
		}

		/// <summary>
		/// A literal branch beside a computed decimal finer than the provider's default decimal keeps both branches'
		/// digits.
		/// </summary>
		[Test]
		public void LiteralBesideComputedDecimalKeepsOperandScale([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new FineDecimalRow { Id = 1, Value = 1.000000000005m } });

			var values = t
				.Select(r => Math.Abs(r.Value))
				.Concat(t.Select(r => 1.000000000005m))
				.ToArray();

			AreEqualWithinDelta([1.000000000005m, 1.000000000005m], values);
		}

		sealed class WideDecimalRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 2)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 2)]
			public decimal Value { get; set; }
		}

		/// <summary>
		/// A finer literal branch beside a computed decimal with many integer digits keeps both branches' values, in either
		/// branch order, when the two fit one decimal type.
		/// </summary>
		/// <remarks>
		/// SqlCe is left out: it types the column by the first branch alone, so a computed <c>Decimal(18,2)</c> narrows the
		/// literal whatever it is cast to. YDB refuses to union two different decimal types.
		/// </remarks>
		[Test]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", TestProvName.AllYdb, ErrorMessage = "Mismatch type of sequences being extended")]
		public void FinerLiteralBesideWideComputedDecimalKeepsBothValues([IncludeDataSources(TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllDuckDB, TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new WideDecimalRow { Id = 1, Value = 12345678901234.50m } });

			var fitting = t
				.Select(r => Math.Abs(r.Value))
				.Concat(t.Select(r => 1.0000000000005m))
				.ToArray();

			fitting.ShouldBe([12345678901234.50m, 1.0000000000005m], ignoreOrder: true);

			var fittingLiteralFirst = t
				.Select(r => 1.0000000000005m)
				.Concat(t.Select(r => Math.Abs(r.Value)))
				.ToArray();

			fittingLiteralFirst.ShouldBe([12345678901234.50m, 1.0000000000005m], ignoreOrder: true);
		}

		/// <summary>
		/// A literal branch too fine to share a decimal type with a computed decimal with many integer digits keeps both
		/// branches' values.
		/// </summary>
		/// <remarks>
		/// The providers here other than ClickHouse render a literal's precision in the set operation's cast. The literal
		/// needs more digits than any of them allows beside the column's integer part, so it is written with its own; the
		/// result type is then the server's. ClickHouse renders only the decimal family and scale, and moves the literal up
		/// to <c>Decimal128</c> instead. ClickHouse.Driver up to 1.5.0 reads that <c>Decimal128</c> without dropping its
		/// trailing zeros first, and overflows <see cref="decimal"/> (clickhouse-cs #647). YDB refuses to union two different
		/// decimal types, as in <see cref="FinerLiteralBesideWideComputedDecimalKeepsBothValues"/>.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(OverflowException), ProviderName.ClickHouseDriver, ErrorMessage = "Value cannot be represented as System.Decimal")]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", TestProvName.AllYdb, ErrorMessage = "Mismatch type of sequences being extended")]
		public void TooFineLiteralBesideWideComputedDecimalKeepsBothValues([IncludeDataSources(TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllDuckDB, TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new WideDecimalRow { Id = 1, Value = 12345678901234.50m } });

			var ownFacets = t
				.Select(r => Math.Abs(r.Value))
				.Concat(t.Select(r => 1.00000000000000005m))
				.ToArray();

			ownFacets.ShouldBe([12345678901234.50m, 1.00000000000000005m], ignoreOrder: true);
		}

		/// <inheritdoc cref="TooFineLiteralBesideWideComputedDecimalKeepsBothValues"/>
		[Test]
		[ThrowsForProvider(typeof(OverflowException), ProviderName.ClickHouseDriver, ErrorMessage = "Value cannot be represented as System.Decimal")]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", TestProvName.AllYdb, ErrorMessage = "Mismatch type of sequences being extended")]
		public void TooFineLiteralFirstBesideWideComputedDecimalKeepsBothValues([IncludeDataSources(TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllDuckDB, TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new WideDecimalRow { Id = 1, Value = 12345678901234.50m } });

			var ownFacetsLiteralFirst = t
				.Select(r => 1.00000000000000005m)
				.Concat(t.Select(r => Math.Abs(r.Value)))
				.ToArray();

			ownFacetsLiteralFirst.ShouldBe([12345678901234.50m, 1.00000000000000005m], ignoreOrder: true);
		}

		readonly record struct ParentValue(int Id, int? Value);

		abstract record ValueHolder(ParentValue Info);
		sealed record LowHolder(ParentValue Info) : ValueHolder(Info);
		sealed record HighHolder(ParentValue Info) : ValueHolder(Info);

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6068")]
		public void ConcatStructReferencedTwice([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent.Where(p => p.ParentID < 4)
				.Select(p => new { p.ParentID, Info = new ParentValue(p.ParentID, p.Value1) })
				.Concat(db.Parent.Where(p => p.ParentID >= 4)
					.Select(p => new { p.ParentID, Info = new ParentValue(p.ParentID, p.Value1) }))
				.Select(m => new { First = m.Info, Second = m.Info });

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6068")]
		public void ConcatStructInBothConditionalBranches([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent.Where(p => p.ParentID < 4)
				.Select(p => new { p.ParentID, Info = new ParentValue(p.ParentID, p.Value1) })
				.Concat(db.Parent.Where(p => p.ParentID >= 4)
					.Select(p => new { p.ParentID, Info = new ParentValue(p.ParentID, p.Value1) }))
				.Select(m => m.ParentID < 4 ? new LowHolder(m.Info) : (ValueHolder)new HighHolder(m.Info));

			AssertQuery(query);
		}

		static IQueryable<T> UnionOrConcat<T>(IQueryable<T> objects, IQueryable<T> nulls, bool union, bool nullFirst)
		{
			var (first, second) = nullFirst ? (nulls, objects) : (objects, nulls);

			return union ? first.Union(second) : first.Concat(second);
		}

		public enum ComparingSetOperation
		{
			Intersect,
			IntersectNullFirst,
			Except,
		}

		static IQueryable<T> Compare<T>(IQueryable<T> objects, IQueryable<T> nulls, ComparingSetOperation operation)
		{
			return operation switch
			{
				ComparingSetOperation.Intersect          => objects.Intersect(nulls),
				ComparingSetOperation.IntersectNullFirst => nulls.Intersect(objects),
				ComparingSetOperation.Except             => objects.Except(nulls),
				_                                        => throw new ArgumentOutOfRangeException(nameof(operation)),
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		public void JoinedEntityAgainstNullBranch([DataSources] string context, [Values] bool union, [Values] bool nullFirst)
		{
			using var db = GetDataContext(context);

			var objects =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, Child = c };

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			AssertQuery(UnionOrConcat(objects, nulls, union, nullFirst));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void JoinedEntityAgainstNullBranchCompared([DataSources] string context, [Values] ComparingSetOperation operation)
		{
			using var db = GetDataContext(context);

			var objects =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, Child = c };

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			AssertQuery(Compare(objects, nulls, operation));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		public void NestedSetAgainstNullBranch([DataSources] string context, [Values] bool joined, [Values] bool innerUnion, [Values] bool innerNullFirst, [Values] bool outerNullFirst)
		{
			using var db = GetDataContext(context);

			var objects = joined
				? from p in db.Parent
				  from c in db.Child
					  .Where(c => c.ParentID == p.ParentID)
					  .DefaultIfEmpty()
				  select new { p.ParentID, Child = (Child?)c }
				: db.Parent.Select(p => new { p.ParentID, Child = (Child?)new Child { ParentID = p.ParentID, ChildID = p.ParentID + 100 } });

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			var inner = UnionOrConcat(objects, nulls, innerUnion, innerNullFirst);

			AssertQuery(UnionOrConcat(inner, nulls, union: true, outerNullFirst));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void NestedSetAgainstNullBranchCompared([DataSources] string context, [Values] bool innerUnion, [Values] bool innerNullFirst, [Values(ComparingSetOperation.Intersect, ComparingSetOperation.Except)] ComparingSetOperation operation)
		{
			using var db = GetDataContext(context);

			var objects =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, Child = (Child?)c };

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			var inner = UnionOrConcat(objects, nulls, innerUnion, innerNullFirst);

			AssertQuery(Compare(inner, nulls, operation));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		// Informix fails when EXCEPT compares a NULL constant with a NOT NULL column read through a set-operation derived table.
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", TestProvName.AllInformix, ErrorMessage = "unexpected null pointer encountered",
			AlsoWhenParameter = "operation", AlsoWhenValue = ComparingSetOperation.Except)]
		public void NestedSetOfNewObjectsAgainstNullBranchCompared([DataSources] string context, [Values] bool innerUnion, [Values] bool innerNullFirst, [Values(ComparingSetOperation.Intersect, ComparingSetOperation.Except)] ComparingSetOperation operation)
		{
			using var db = GetDataContext(context);

			var objects = db.Parent.Select(p => new { p.ParentID, Child = (Child?)new Child { ParentID = p.ParentID, ChildID = p.ParentID + 100 } });

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			var inner = UnionOrConcat(objects, nulls, innerUnion, innerNullFirst);

			AssertQuery(Compare(inner, nulls, operation));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		public void JoinedEntityMemberAfterConcat([DataSources] string context, [Values] bool nullFirst)
		{
			using var db = GetDataContext(context);

			var objects =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, Child = c };

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			AssertQuery(UnionOrConcat(objects, nulls, union: false, nullFirst)
				.Select(x => new { x.ParentID, Id = x.Child == null ? -1 : x.Child.ChildID }));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		public void JoinedEntityNullTestAfterConcat([DataSources] string context, [Values] bool nullFirst)
		{
			using var db = GetDataContext(context);

			var objects =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, Child = c };

			var nulls =
				from p in db.Parent
				select new { p.ParentID, Child = (Child?)null };

			AssertQuery(UnionOrConcat(objects, nulls, false, nullFirst).Where(x => x.Child == null).Select(x => x.ParentID));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		public void AssociationAgainstNullBranch([DataSources] string context, [Values] bool union, [Values] bool nullFirst)
		{
			using var db = GetDataContext(context);

			var objects = db.Person
				.Select(p => new { p.ID, p.Patient });

			var nulls = db.Person
				.Select(p => new { p.ID, Patient = (Patient?)null });

			// AssertQuery's expected side does not navigate associations, so the expected rows are written out.
			var expectedObjects = Person
				.Select(p => new { p.ID, Patient = (Patient?)Patient.FirstOrDefault(t => t.PersonID == p.ID) })
				.AsQueryable();

			var expectedNulls = Person
				.Select(p => new { p.ID, Patient = (Patient?)null })
				.AsQueryable();

			AreEqual(
				UnionOrConcat(expectedObjects, expectedNulls, union, nullFirst),
				UnionOrConcat(objects, nulls, union, nullFirst));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void AssociationAgainstNullBranchCompared([DataSources] string context, [Values] ComparingSetOperation operation)
		{
			using var db = GetDataContext(context);

			var objects = db.Person
				.Select(p => new { p.ID, p.Patient });

			var nulls = db.Person
				.Select(p => new { p.ID, Patient = (Patient?)null });

			// AssertQuery's expected side does not navigate associations, so the expected rows are written out.
			var expectedObjects = Person
				.Select(p => new { p.ID, Patient = (Patient?)Patient.FirstOrDefault(t => t.PersonID == p.ID) })
				.AsQueryable();

			var expectedNulls = Person
				.Select(p => new { p.ID, Patient = (Patient?)null })
				.AsQueryable();

			AreEqual(
				Compare(expectedObjects, expectedNulls, operation),
				Compare(objects, nulls, operation));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void FirstOrDefaultAgainstNullBranch([DataSources] string context, [Values] bool union, [Values] bool nullFirst)
		{
			using var db = GetDataContext(context);

			var objects = db.Parent
				.Select(p => new
				{
					p.ParentID,
					Child = (Child?)db.Child
						.Where(c => c.ParentID == p.ParentID)
						.OrderBy(c => c.ChildID)
						.FirstOrDefault(),
				});

			var nulls = db.Parent
				.Select(p => new { p.ParentID, Child = (Child?)null });

			AssertQuery(UnionOrConcat(objects, nulls, union, nullFirst));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6156")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void FirstOrDefaultAgainstNullBranchCompared([DataSources] string context, [Values] ComparingSetOperation operation)
		{
			using var db = GetDataContext(context);

			var objects = db.Parent
				.Select(p => new
				{
					p.ParentID,
					Child = (Child?)db.Child
						.Where(c => c.ParentID == p.ParentID)
						.OrderBy(c => c.ChildID)
						.FirstOrDefault(),
				});

			var nulls = db.Parent
				.Select(p => new { p.ParentID, Child = (Child?)null });

			AssertQuery(Compare(objects, nulls, operation));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1222")]
		public void ConcatOfLinkQueriesJoinedToVersions([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (db.CreateLocalTable<ConcatLinkRecord>())
			using (db.CreateLocalTable<ConcatVersionRecord>())
			{
				var parentId = 111;

				var query =
						from u in Queryable.Concat(
							from link in db.GetTable<ConcatLinkRecord>()
							where link.InIdParent == parentId
							select new { VersionId = link.InIdChild, Link = link },

							from link in db.GetTable<ConcatLinkRecord>()
							where link.InIdChild == parentId
							select new { VersionId = link.InIdParent, Link = link })

						join version in db.GetTable<ConcatVersionRecord>() on u.VersionId equals version.InId
						select new
						{
							//LinkTypeId = u.Link.RlTypesAndType.InIdLinkType,
							//LinkId = u.Link.InId,
							MainId = version.InIdMain
						};

				var _ = query.ToList();
			}
		}

		[Table("ConcatLinks")]
		public class ConcatLinkRecord
		{
			[Column("inId"), PrimaryKey, Identity] public int       InId          { get; set; } // int
			[Column("inIdParent"),    NotNull]     public int       InIdParent    { get; set; } // int
			[Column("inIdChild"),     NotNull]     public int       InIdChild     { get; set; } // int
			[Column("inIdTypeRel"),   NotNull]     public int       InIdTypeRel   { get; set; } // int
			[Column("inMaxQuantity"),    Nullable] public double?   InMaxQuantity { get; set; } // float
			[Column("inMinQuantity"),    Nullable] public double?   InMinQuantity { get; set; } // float
			[Column("inIdMeasure"),      Nullable] public int?      InIdMeasure   { get; set; } // int
			[Column("inIdUnit"),         Nullable] public int?      InIdUnit      { get; set; } // int
			[Column,                     Nullable] public int?      State         { get; set; } // int
			[Column("dtModified"),    NotNull]     public DateTime  DtModified    { get; set; } // datetime
			[Column("inIdOrgOwner"),     Nullable] public int?      InIdOrgOwner  { get; set; } // int
			[Column("dtSynchDate"),      Nullable] public DateTime? DtSynchDate   { get; set; } // datetime
			[Column("stGUID"),        NotNull]     public string    StGUID        { get; set; } = null!;// varchar(255)

			[Association(ThisKey = "InIdTypeRel", OtherKey = "InId", CanBeNull = false)]
			public ConcatLinkTypeRecord RlTypesAndType { get; set; } = null!;
		}

		[Table("ConcatLinkTypes")]
		public class ConcatLinkTypeRecord
		{
			[Column("inId"), PrimaryKey, Identity] public int InId         { get; set; } // int
			[Column("inIdLinkType"), NotNull]      public int InIdLinkType { get; set; } // int
		}

		[Table("ConcatVersions")]
		public class ConcatVersionRecord
		{
			[Column("inId"), PrimaryKey, Identity] public int InId     { get; set; } // int
			[Column("inIdMain"), NotNull]          public int InIdMain { get; set; } // int
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1347")]
		public void UnionOfTableNamesLeftJoinedToUnion([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<UnionJoinGlobalTask>();
			using var t11 = db.CreateLocalTable<UnionJoinGlobalTask>("WMS_GlobalTaskA");
			using var t5 = db.CreateLocalTable<UnionJoinLoadCarrier>();
			using var t51 = db.CreateLocalTable<UnionJoinLoadCarrier>("WMS_LoadCarrierA");

			var gts = db.GetTable<UnionJoinGlobalTask>().Union(
					db.GetTable<UnionJoinGlobalTask>()
						.TableName(
							"WMS_GlobalTaskA"));

			var lcs = db.GetTable<UnionJoinLoadCarrier>().Union(
					db.GetTable<UnionJoinLoadCarrier>()
						.TableName(
							"WMS_LoadCarrierA"));

			var qry = from g in gts
					  join res in lcs on g.ResourceID equals res.Id into reslist
					  from res in reslist.DefaultIfEmpty()
					  select new UnionJoinCombined()
					  { GlobalTask = g, LoadCarrier = res };

			qry.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1347")]
		public void UnionOfTableNamesWithUnusedGroupJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<UnionJoinGlobalTask>();
			using var t11 = db.CreateLocalTable<UnionJoinGlobalTask>("WMS_GlobalTaskA");

			var gts = db.GetTable<UnionJoinGlobalTask>().Union(
					db.GetTable<UnionJoinGlobalTask>()
						.TableName(
							"WMS_GlobalTaskA"));

			var qry = from g in gts
					  join source1 in db.GetTable<UnionJoinResourcePoint>() on g.RPSourceID equals source1.Id into
						source1List
					  select new UnionJoinCombined()
					  {
						  GlobalTask = g,
					  };

			qry.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1347")]
		public void UnionOfTableNamesLeftJoinedToFiveSources([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<UnionJoinGlobalTask>();
			using var t11 = db.CreateLocalTable<UnionJoinGlobalTask>("WMS_GlobalTaskA");
			using var t2 = db.CreateLocalTable<UnionJoinResourcePoint>();
			using var t3 = db.CreateLocalTable<UnionJoinStorageShelf>();

			var gts = db.GetTable<UnionJoinGlobalTask>().Union(
					db.GetTable<UnionJoinGlobalTask>()
						.TableName(
							"WMS_GlobalTaskA"));

			var qry = from g in gts
					  join source1 in db.GetTable<UnionJoinResourcePoint>() on g.RPSourceID equals source1.Id into
						source1List
					  from source in source1List.DefaultIfEmpty()
					  join sourceShelf1 in db.GetTable<UnionJoinStorageShelf>() on g.StorageShelfSourceID equals sourceShelf1
						.Id into sourceShelf1List
					  from sourceShelf in sourceShelf1List.DefaultIfEmpty()
					  join dest1 in db.GetTable<UnionJoinResourcePoint>() on g.RPDestinationID equals dest1.Id into
						destList
					  from dest in destList.DefaultIfEmpty()
					  join destShelf1 in db.GetTable<UnionJoinStorageShelf>() on g.StorageShelfDestinationID equals
						destShelf1.Id into destShelf1List
					  from destShelf in destShelf1List.DefaultIfEmpty()
					  join origdest1 in db.GetTable<UnionJoinResourcePoint>() on g.RPOrigDestinationID equals origdest1.Id
						into origdestList
					  from origdest in origdestList.DefaultIfEmpty()
					  select new UnionJoinCombined()
					  {
						  GlobalTask = g,
						  Source = source,
						  SourceShelf = sourceShelf,
						  Destination = dest,
						  DestinationShelf = destShelf,
						  OriginDestination = origdest
					  };

			qry.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1347")]
		public void UnionOfTableNamesAllLeftJoinsQuerySyntax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<UnionJoinGlobalTask>();
			using var t11 = db.CreateLocalTable<UnionJoinGlobalTask>("WMS_GlobalTaskA");
			using var t2 = db.CreateLocalTable<UnionJoinResourcePoint>();
			using var t3 = db.CreateLocalTable<UnionJoinStorageShelf>();
			using var t4 = db.CreateLocalTable<UnionJoinOutfeedOrder>();
			using var t41 = db.CreateLocalTable<UnionJoinOutfeedOrder>("WMS_OutfeedTransportOrderA");
			using var t5 = db.CreateLocalTable<UnionJoinLoadCarrier>();
			using var t51 = db.CreateLocalTable<UnionJoinLoadCarrier>("WMS_LoadCarrierA");

			var gts = db.GetTable<UnionJoinGlobalTask>().Union(
					db.GetTable<UnionJoinGlobalTask>()
						.TableName(
							"WMS_GlobalTaskA"));

			var lcs = db.GetTable<UnionJoinLoadCarrier>().Union(
					db.GetTable<UnionJoinLoadCarrier>()
						.TableName(
							"WMS_LoadCarrierA"));

			var ots = db.GetTable<UnionJoinOutfeedOrder>().Union(
					db.GetTable<UnionJoinOutfeedOrder>()
						.TableName(
							"WMS_OutfeedTransportOrderA"));

			var qry = from g in gts
					  join source1 in db.GetTable<UnionJoinResourcePoint>() on g.RPSourceID equals source1.Id into source1List
					  from source in source1List.DefaultIfEmpty()
					  join sourceShelf1 in db.GetTable<UnionJoinStorageShelf>() on g.StorageShelfSourceID equals sourceShelf1.Id into sourceShelf1List
					  from sourceShelf in sourceShelf1List.DefaultIfEmpty()
					  join dest1 in db.GetTable<UnionJoinResourcePoint>() on g.RPDestinationID equals dest1.Id into destList
					  from dest in destList.DefaultIfEmpty()
					  join destShelf1 in db.GetTable<UnionJoinStorageShelf>() on g.StorageShelfDestinationID equals destShelf1.Id into destShelf1List
					  from destShelf in destShelf1List.DefaultIfEmpty()
					  join origdest1 in db.GetTable<UnionJoinResourcePoint>() on g.RPOrigDestinationID equals origdest1.Id into origdestList
					  from origdest in origdestList.DefaultIfEmpty()
					  join res in lcs on g.ResourceID equals res.Id into reslist
					  from res in reslist.DefaultIfEmpty()
					  join outfeed1 in ots on g.OutfeedTransportOrderID equals outfeed1.Id into outfeed1List
					  from outfeed in outfeed1List.DefaultIfEmpty()
					  select new UnionJoinCombined() { GlobalTask = g, LoadCarrier = res, Source = source, SourceShelf = sourceShelf, Destination = dest, DestinationShelf = destShelf, OriginDestination = origdest, OutfeedTransportOrder = outfeed };

			_ = qry.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1347")]
		public void UnionOfTableNamesAllLeftJoinsMethodSyntax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<UnionJoinGlobalTask>();
			using var t11 = db.CreateLocalTable<UnionJoinGlobalTask>("WMS_GlobalTaskA");
			using var t2 = db.CreateLocalTable<UnionJoinResourcePoint>();
			using var t3 = db.CreateLocalTable<UnionJoinStorageShelf>();
			using var t4 = db.CreateLocalTable<UnionJoinOutfeedOrder>();
			using var t41 = db.CreateLocalTable<UnionJoinOutfeedOrder>("WMS_OutfeedTransportOrderA");
			using var t5 = db.CreateLocalTable<UnionJoinLoadCarrier>();
			using var t51 = db.CreateLocalTable<UnionJoinLoadCarrier>("WMS_ResourceA");

			var query = db.GetTable<UnionJoinGlobalTask>()
					.Union(
						db.GetTable<UnionJoinGlobalTask>()
							.TableName(
								"WMS_GlobalTaskA"))
					.GroupJoin(
						db.GetTable<UnionJoinResourcePoint>(),
						g => g.RPSourceID,
						source1 => (Guid?)source1.Id,
						(g, source1List) => new
						{
							g = g,
							source1List = source1List
						})
					.SelectMany(
						tp0 => tp0.source1List
							.DefaultIfEmpty(),
						(tp0, source) => new
						{
							tp0 = tp0,
							source = source
						})
					.GroupJoin(
						db.GetTable<UnionJoinStorageShelf>(),
						tp1 => tp1.tp0.g.StorageShelfSourceID,
						sourceShelf1 => (Guid?)sourceShelf1.Id,
						(tp1, sourceShelf1List) => new
						{
							tp1 = tp1,
							sourceShelf1List = sourceShelf1List
						})
					.SelectMany(
						tp2 => tp2.sourceShelf1List
							.DefaultIfEmpty(),
						(tp2, sourceShelf) => new
						{
							tp2 = tp2,
							sourceShelf = sourceShelf
						})
					.GroupJoin(
						db.GetTable<UnionJoinResourcePoint>(),
						tp3 => tp3.tp2.tp1.tp0.g.RPDestinationID,
						dest1 => (Guid?)dest1.Id,
						(tp3, destList) => new
						{
							tp3 = tp3,
							destList = destList
						})
					.SelectMany(
						tp4 => tp4.destList
							.DefaultIfEmpty(),
						(tp4, dest) => new
						{
							tp4 = tp4,
							dest = dest
						})
					.GroupJoin(
						db.GetTable<UnionJoinStorageShelf>(),
						tp5 => tp5.tp4.tp3.tp2.tp1.tp0.g.StorageShelfDestinationID,
						destShelf1 => (Guid?)destShelf1.Id,
						(tp5, destShelf1List) => new
						{
							tp5 = tp5,
							destShelf1List = destShelf1List
						})
					.SelectMany(
						tp6 => tp6.destShelf1List
							.DefaultIfEmpty(),
						(tp6, destShelf) => new
						{
							tp6 = tp6,
							destShelf = destShelf
						})
					.GroupJoin(
						db.GetTable<UnionJoinResourcePoint>(),
						tp7 => tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.RPOrigDestinationID,
						origdest1 => (Guid?)origdest1.Id,
						(tp7, origdestList) => new
						{
							tp7 = tp7,
							origdestList = origdestList
						})
					.SelectMany(
						tp8 => tp8.origdestList
							.DefaultIfEmpty(),
						(tp8, origdest) => new
						{
							tp8 = tp8,
							origdest = origdest
						})
					.GroupJoin(
						db.GetTable<UnionJoinLoadCarrier>()
							.Union(
								db.GetTable<UnionJoinLoadCarrier>()
									.TableName(
										"WMS_ResourceA")),
						tp9 => tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.ResourceID,
						res => res.Id,
						(tp9, reslist) => new
						{
							tp9 = tp9,
							reslist = reslist
						})
					.SelectMany(
						tp10 => tp10.reslist
							.DefaultIfEmpty(),
						(tp10, res) => new
						{
							tp10 = tp10,
							res = res
						})
					.GroupJoin(
						db.GetTable<UnionJoinOutfeedOrder>()
							.Union(
								db.GetTable<UnionJoinOutfeedOrder>()
									.TableName(
										"WMS_OutfeedTransportOrderA")),
						tp11 => tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.OutfeedTransportOrderID,
						outfeed1 => (Guid?)outfeed1.Id,
						(tp11, outfeed1List) => new
						{
							tp11 = tp11,
							outfeed1List = outfeed1List
						})
					.SelectMany(
						tp12 => tp12.outfeed1List
							.DefaultIfEmpty(),
						(tp12, outfeed) => new UnionJoinCombined()
						{
							GlobalTask = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g,
							LoadCarrier = tp12.tp11.res,
							Source = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.source,
							SourceShelf = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.sourceShelf,
							Destination = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.dest,
							DestinationShelf = tp12.tp11.tp10.tp9.tp8.tp7.destShelf,
							OriginDestination = tp12.tp11.tp10.tp9.origdest,
							OutfeedTransportOrder = outfeed
						});

			_ = query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2461")]
		public void ConcatTablesWithAssociationExpressionAndTableName([DataSources(TestProvName.AllYdb)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ConcatReceipt>())
			using (db.CreateLocalTable<ConcatReceipt>(ConcatReceipt.ExternalReceiptsTableName))
			using (db.CreateLocalTable<ConcatCustomer>())
			{
				var query = db.GetTable<ConcatReceipt>()
					.Concat(db.GetTable<ConcatReceipt>().TableName(ConcatReceipt.ExternalReceiptsTableName))
					.Select(
						i =>
							new { i.ReceiptNo, a = ConcatCustomer.GetName(i.Customer.BillingGroup) });

				query.ToArray();
			}
		}

		[LinqToDB.Mapping.Table("MRECEIPT")]
		public class ConcatReceipt
		{
			[LinqToDB.Mapping.PrimaryKey] public int Id { get; set; }

			public static string TableName => "MRECEIPT";
			public static string ExternalReceiptsTableName => "EXTERNAL_RECEIPTS";

			[LinqToDB.Mapping.Column("RECEIPT_NO")]            public string           ReceiptNo            { get; set; } = null!;
			[LinqToDB.Mapping.Column("CUSTKEY")]               public string           Custkey              { get; set; } = null!;

			[LinqToDB.Mapping.Association(ThisKey=nameof(Custkey), OtherKey=nameof(ConcatCustomer.Custkey), CanBeNull=true)]
			public ConcatCustomer Customer { get; } = null!;
		}

		[LinqToDB.Mapping.Table("CUST_DTL")]
		public class ConcatCustomer
		{
			[LinqToDB.Mapping.PrimaryKey] public int Id { get; set; }

			[LinqToDB.Mapping.Column("CUSTKEY")]
			public string Custkey { get; set;} = null!;

			[LinqToDB.Mapping.Column("BILLGROUP")] public string BillingGroup { get; set; } = null!;

			public static string GetName(string name) => name;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2856")]
		public void UnionOfNullableGuidMembers([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var s = GetDataContext(context))
			using (s.CreateLocalTable<NullableGuidTask>())
			{
				var allRpIds = new[] { TestData.Guid1, TestData.Guid2 };

				_ = (
							from gt1 in s.GetTable<NullableGuidTask>()
							where allRpIds.Contains(gt1.RPSourceID!.Value)
							select gt1.RPSourceID!.Value
						)
						.Union(
							from gt2 in s.GetTable<NullableGuidTask>()
							select gt2.RPDestinationID!.Value
						)
						.ToList();
			}
		}

		public class NullableGuidTask
		{
			[PrimaryKey]
			public Guid Id { get; set; }

			public Guid? RPSourceID { get; set; }

			public Guid? RPDestinationID { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3089")]
		public void UnionTypedTimeBranchFirst([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<UnionNullTimeRow>();
			var query = (from x in table
					select new UnionNullTimeResult {Time = x.Time})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null});

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3089")]
		public void UnionTypedTimeBranchSecond([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<UnionNullTimeRow>();
			var query = (from x in table
				select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = x.Time})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
				select new UnionNullTimeResult {Time = null});

			query.ToArray();
		}

		[ActiveIssue(3360, Configuration = TestProvName.AllPostgreSQL, ErrorTypeName = "Npgsql.PostgresException",
			ErrorMessage = "42804: UNION types interval and timestamp without time zone cannot be matched",
			Details = "the untyped NULL branches are emitted before the typed one, so PostgreSQL settles the union column on the wrong type - #3360's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3089")]
		public void UnionTypedTimeBranchThird([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<UnionNullTimeRow>();
			var query = (from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = x.Time})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null});

			query.ToArray();
		}

		[ActiveIssue(3360, Configuration = TestProvName.AllPostgreSQL, ErrorTypeName = "Npgsql.PostgresException",
			ErrorMessage = "42804: UNION types timestamp without time zone and interval cannot be matched",
			Details = "as UnionTypedTimeBranchThird, with the operands the other way round because the typed branch is last here.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3089")]
		public void UnionTypedTimeBranchLast([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<UnionNullTimeRow>();
			var query = (from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = null})
				.Union(from x in table
					select new UnionNullTimeResult {Time = x.Time});

			query.ToArray();
		}

		[Table(Name ="UnionNullTimeRow")]
		public class UnionNullTimeRow
		{
			[Column, NotNull    ] public int       Id   { get; set; } // integer
			[Column(DataType = DataType.DateTime),    Nullable] public TimeSpan? Time { get; set; } // time without time zone
		}

		public class UnionNullTimeResult
		{
			public TimeSpan? Time { get; set; }
		}

		// Take(10) without ORDER BY: the server picks which rows come back,
		// so the check accepts any 10 distinct rows of the full expected set
		[Test(Description = "https://github.com/linq2db/linq2db/issues/356")]
		public void CorrelatedUnionDistinctSubqueryUnsortedTake([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).Distinct();
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.Take(10)
					.ToList();

			var expectedUnion = Child.Union(Child).Distinct();
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.ToList();

			result.Count.ShouldBe(Math.Min(10, expected.Count));
			result.ShouldBeUnique();
			result.ShouldBeSubsetOf(expected);
		}

		// Test without sorting order dependency
		// Generated SQL not supported by Access
		[Test(Description = "https://github.com/linq2db/linq2db/issues/356")]
		public void CorrelatedUnionDistinctSubquery([DataSources(TestProvName.AllPostgreSQL, TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).Distinct();
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID)
					.Take(10);

			var expectedUnion = Child.Union(Child).Distinct();
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID)
					.Take(10);

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/356")]
		public void CorrelatedUnionOrderByTakeSubquery([DataSources(TestProvName.AllSybase, TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).OrderBy(_ => _.ParentID).Take(10);
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			var expectedUnion = Child.Union(Child).OrderBy(_ => _.ParentID).Take(10);
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/356")]
		public void CorrelatedUnionOrderBySkipTakeSubquery([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).OrderBy(_ => _.ParentID).Skip(10).Take(10);
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			var expectedUnion = Child.Union(Child).OrderBy(_ => _.ParentID).Skip(10).Take(10);
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4654")]
		public void UnionOfProjectionsToSameResultType([DataSources] string configuration)
		{
			using var db = GetDataContext(configuration);
			using var t1 = db.CreateLocalTable<UnionSourceCustomer>();
			using var t2 = db.CreateLocalTable<UnionSourceOrder>();
			using var t3 = db.CreateLocalTable<UnionSourceProduct>();

			var customerResults = t1.Select(c => new UnionSourceResult()
			{
				Id = "" + c.Id,
				Name = c.Name
			});

			var orderResults = t2.Select(o => new UnionSourceResult()
			{
				Id ="" + o.Id,
				Name = o.ProductName
			});

			var productResults = t3.Select(p => new UnionSourceResult()
			{
				Id = "" + p.Id,
				Name = p.Name
			});

			// Union of all three results
			var combinedResults = customerResults
			.Union(orderResults)
			.Union(productResults);

			// Convert to list and return the results
			var result = combinedResults.ToArray();
		}

		[Table("UnionSourceCustomer")]
		public class UnionSourceCustomer
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(UnionSourceOrder.CustomerId))]
			public ICollection<UnionSourceOrder>? Orders { get; set; }
		}

		[Table("UnionSourceOrder")]
		public class UnionSourceOrder
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string ProductName { get; set; } = null!;
			[Column] public int Quantity { get; set; }
			[Column] public int CustomerId { get; set; }
			[Association(ThisKey = nameof(CustomerId), OtherKey = nameof(UnionSourceCustomer.Id), CanBeNull = true)]
			public UnionSourceCustomer? Customer { get; set; }
		}

		[Table("UnionSourceProduct")]
		public class UnionSourceProduct
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string Name { get; set; } = null!;
			[Column] public decimal Price { get; set; }
		}

		public class UnionSourceResult
		{
			public string Id { get; set; } = null!;

			public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5305")]
		public void CountInUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db               = GetDataContext(context);
			using var lineTable        = db.CreateLocalTable<UnionLine>([new UnionLine { Amount               = 10m }]);
			using var invoiceLineTable = db.CreateLocalTable<UnionInvoiceLine>([new UnionInvoiceLine { Amount = 5m }]);

			var queryable =
				db.GetTable<UnionLine>()
					.Select(o => new UnionRow
					{
						Order  = o.Id.ToString(),
						Amount = o.Amount
					})
					.UnionAll(
						db.GetTable<UnionInvoiceLine>()
							.Select(x => new UnionRow
							{
								Order  = x.Order,
								Amount = x.Amount,
							})
					);

			var pagedQuery =
				queryable.Select(x => new
					{
						Value      = x,
						TotalCount = queryable.Count(), // <-- triggers the malformed COUNT-subquery
					})
					.Skip(0).Take(10);

			AssertQuery(pagedQuery);
		}

		[Table("Lines")]
		public class UnionLine
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column]               public string  Order  { get; set; } = "";
			[Column]               public decimal Amount { get; set; }
		}

		[Table("InvoiceLines")]
		public class UnionInvoiceLine
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column]               public string  Order  { get; set; } = "";
			[Column]               public decimal Amount { get; set; }

			// If you comment this out so they have the same amount of columns it works again
			[Column] public string ExtraColumnSoTheyDontAlign { get; set; } = "";
		}

		// The DTO both sides of UNION project to
		public class UnionRow
		{
			public string  Order  { get; set; } = "";
			public decimal Amount { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5379")]
		public void GuidLiteralInUnionAllBranchWithLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var aisle = db.CreateLocalTable<GuidUnionResource>([new GuidUnionResource() { Id = TestData.Guid1, ResourceID = TestData.Guid2 }]);
			using var refTable = db.CreateLocalTable<GuidUnionCarrier>([new GuidUnionCarrier() { Id = TestData.Guid2 }]);

			var qryAssignment =
				from r in refTable
				select new GuidUnionResource()
				{
					Id = Guid.Empty,
					ResourceID = r.Id
				};

			var inventoryQry = aisle.UnionAll(qryAssignment);

			var qry =
				from r in refTable
				join inventory in inventoryQry on r.Id equals inventory.ResourceID into invlist
				from inventory in invlist.DefaultIfEmpty()
				select inventory;

			var _ = qry.ToList();
		}

		public class GuidUnionResource
		{
			[PrimaryKey]
			public Guid Id { get; set; }
			public Guid ResourceID { get; set; }
		}

		public class GuidUnionCarrier
		{
			[PrimaryKey]
			public Guid Id { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5458")]
		public void UnionAllWithJoinAndIsNullOrEmpty([DataSources] string context)
		{
			using var db     = GetDataContext(context);
			using var table  = db.CreateLocalTable(new[]
			{
				new UnionStringTable { Id = 1, Value = "hello" },
				new UnionStringTable { Id = 2, Value = null    },
			});
			using var table2 = db.CreateLocalTable(new[] { new UnionJoinedTable { Id = 1 }, new UnionJoinedTable { Id = 2 } });

			var query =
				from t in table.Where(t => t.Id <= 1).UnionAll(table.Where(t => t.Id > 1))
				join o in db.GetTable<UnionJoinedTable>() on t.Id equals o.Id
				select new UnionStringResult
				{
					Entity            = t,
					TranslatedMessage = !string.IsNullOrEmpty(t.Value) ? t.Value : "default",
				};

			var result = query.OrderBy(r => r.Entity!.Id).ToList();

			result.Count.ShouldBe(2);
			result[0].TranslatedMessage.ShouldBe("hello");
			result[1].TranslatedMessage.ShouldBe("default");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5458")]
		public void UnionAllStringIsNullOrEmpty([DataSources(TestProvName.AllSybase)] string context)
		{
			var data = new[]
			{
				new UnionStringTable { Id = 1, Value = "hello" },
				new UnionStringTable { Id = 2, Value = ""      },
				new UnionStringTable { Id = 3, Value = null    },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				table.Where(t => t.Id <= 2)
				.UnionAll(table.Where(t => t.Id > 2))
				.Select(t => new
				{
					t.Id,
					IsEmpty = string.IsNullOrEmpty(t.Value),
				})
				.OrderBy(t => t.Id);

			var result = query.ToList();

			result.Count.ShouldBe(3);
			result[0].IsEmpty.ShouldBeFalse();
			result[1].IsEmpty.ShouldBeTrue();
			result[2].IsEmpty.ShouldBeTrue();
		}

		[Table]
		sealed class UnionStringTable
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public string? Value { get; set; }
		}

		[Table]
		sealed class UnionJoinedTable
		{
			[Column, PrimaryKey] public int Id { get; set; }
		}

		sealed class UnionStringResult
		{
			public UnionStringTable? Entity            { get; set; }
			public string?      TranslatedMessage { get; set; }
		}

		[Sql.Extension("count_if({predicate})", IsAggregate = true, ServerSideOnly = true)]
		private static long CountIf<TSource>(IEnumerable<TSource> src, [ExprParameter] Expression<Func<TSource, bool>> predicate)
		{
			throw new ServerSideOnlyException(nameof(CountIf));
		}

		// A built-in aggregate (Average) in one UNION ALL branch, a constant in the other.
		// Issue #5616: building the query threw InvalidCastException
		// (SqlPathExpression -> SqlPlaceholderExpression) in VisitSqlReaderIsNullExpression.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5616")]
		public void AverageInUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(UnionAggregateLeft.Data);
			using var t2 = db.CreateLocalTable(UnionAggregateRight.Data);

			var aggregates =
				from tr in t2
				group tr by tr.Id into g
				select new
				{
					Count = g.Average(x => x.Id, Sql.AggregateModifier.None)
				};

			var query = t1
				.Select(_ => new { Count = 0d })
				.UnionAll(aggregates);

			var result = query.ToList();

			// constant branch: one row per UnionAggregateLeft row; aggregate branch: one row per UnionAggregateRight group (grouped by PK).
			result.Count.ShouldBe(UnionAggregateLeft.Data.Length + UnionAggregateRight.Data.Length);
			result.Count(x => x.Count == 0d).ShouldBe(UnionAggregateLeft.Data.Length);
		}

		// Same shape with the custom IsAggregate extension from the issue. count_if is not executable
		// on the default providers, so this asserts the query builds (the reported failure was at build time).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5616")]
		public void CustomAggregateInUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var aggregates =
				from tr in db.GetTable<UnionAggregateRight>()
				group tr by tr.Id into g
				select new
				{
					Count = CountIf(g, x => x.Id > 123)
				};

			var query = db.GetTable<UnionAggregateLeft>()
				.Select(_ => new { Count = 0L })
				.UnionAll(aggregates);

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("count_if");
		}

		sealed class UnionAggregateLeft
		{
			[PrimaryKey] public int Id { get; set; }

			public static readonly UnionAggregateLeft[] Data =
			{
				new() { Id = 1 },
				new() { Id = 2 },
				new() { Id = 3 },
			};
		}

		sealed class UnionAggregateRight
		{
			[PrimaryKey] public int Id { get; set; }

			public static readonly UnionAggregateRight[] Data =
			{
				new() { Id = 10 },
				new() { Id = 20 },
				new() { Id = 30 },
			};
		}

		// Issue #5625: a `.Concat(...)` whose result is then `.Join(...)`-ed to a table used
		// only for filtering (the join projects the left side and discards the joined row)
		// threw `LinqToDBException: Table not found for '...EntityId'` while building SQL.
		// The set-operation subquery was incorrectly folded into the parent, leaving the
		// filtering join attached to a single union branch with a dangling column reference.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5625")]
		public void ConcatThenFilteringJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(ConcatJoinThing.Data);
			using var items    = db.CreateLocalTable(ConcatJoinItem.Data);
			using var entities = db.CreateLocalTable(ConcatJoinEntity.Data);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join entity in entities.Where(entity => entity.Applicable)
					on itm.EntityId equals entity.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
		}

		// Same root cause, reached via a plain inner join with no WHERE on the joined table
		// (so it isn't blocked as a set-operation barrier). Also threw before the fix.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5625")]
		public void ConcatThenPlainInnerJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(ConcatJoinThing.Data);
			using var items    = db.CreateLocalTable(ConcatJoinItem.Data);
			using var entities = db.CreateLocalTable(ConcatJoinEntity.Data);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join entity in entities on itm.EntityId equals entity.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 1, 1, 2, 2, 3, 4 }, ignoreOrder: true);
		}

		// Two joins on top of the Concat — also threw before the fix.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5625")]
		public void ConcatThenTwoJoins([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(ConcatJoinThing.Data);
			using var items    = db.CreateLocalTable(ConcatJoinItem.Data);
			using var entities = db.CreateLocalTable(ConcatJoinEntity.Data);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join applicable in entities.Where(entity => entity.Applicable)
					on itm.EntityId equals applicable.EntityId
				join other in entities
					on itm.EntityId equals other.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
		}

		[Table]
		sealed class ConcatJoinEntity
		{
			[PrimaryKey] public int  EntityId   { get; set; }
			[Column]     public bool Applicable { get; set; }

			public static readonly ConcatJoinEntity[] Data =
			{
				new() { EntityId = 1, Applicable = false },
				new() { EntityId = 2, Applicable = false },
				new() { EntityId = 3, Applicable = true  },
				new() { EntityId = 4, Applicable = true  },
			};
		}

		[Table]
		sealed class ConcatJoinItem
		{
			[PrimaryKey] public int  ItemId     { get; set; }
			[Column]     public int  AltItemId  { get; set; }
			[Column]     public int? ItemNumber { get; set; }
			[Column]     public int  EntityId   { get; set; }

			public static readonly ConcatJoinItem[] Data =
			{
				new() { ItemId = 1, AltItemId = 10, ItemNumber = null, EntityId = 1 },
				new() { ItemId = 2, AltItemId = 15, ItemNumber = 640,  EntityId = 2 },
				new() { ItemId = 3, AltItemId = 29, ItemNumber = 480,  EntityId = 3 },
				new() { ItemId = 4, AltItemId = 42, ItemNumber = 600,  EntityId = 4 },
				new() { ItemId = 5, AltItemId = 50, ItemNumber = 800,  EntityId = 1 },
			};
		}

		[Table]
		sealed class ConcatJoinThing
		{
			[PrimaryKey] public int  Id     { get; set; }
			[Column]     public int? ItemId { get; set; }

			public static readonly ConcatJoinThing[] Data =
			{
				new() { Id = 1, ItemId = 1  },
				new() { Id = 2, ItemId = 2  },
				new() { Id = 3, ItemId = 3  },
				new() { Id = 4, ItemId = 10 },
				new() { Id = 5, ItemId = 15 },
				new() { Id = 6, ItemId = 42 },
			};
		}

		// https://github.com/linq2db/linq2db/issues/5683 - branches projecting differing-but-assignable types must meet in the same columns
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void ConcatBranchProjectingDerivedType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Parent where p.ParentID > 3 select new BranchProjection { Id = p.ParentID, Value = p.Value1 })
				.Concat(
					// cast to the projected type, so this branch reaches the set operation Convert-wrapped
					from p in db.Parent where p.ParentID <= 3 select (BranchProjection)new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 });

			var result = AssertQuery(query);

			// the branch-discriminator column exists so each branch is read with its own constructor
			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedBranchProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(BranchProjection));

			// A member is one column for both branches, and one more column tells which branch a row came from -
			// the branches construct different types, so the reader has to know which constructor to run. Keyed by
			// the constructed type rather than the type the projection is read as, the two branches would occupy
			// places of their own instead and each member would be emitted twice, once filled and once NULL-padded.
			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		// https://github.com/linq2db/linq2db/issues/5683 - a constructed object read through `as` projects through the conversion
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void SelectConstructedObjectThroughAs([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// `as` reaches the projection as a TypeAs the builder projects *through*, so the constructor is asked
			// for as a whole rather than member by member - which used to throw NotImplementedException.
			var query = from p in db.Parent select new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 } as BranchProjection;

			var result = AssertQuery(query);

			result.ShouldAllBe(x => x.GetType() == typeof(DerivedBranchProjection));
		}

		// https://github.com/linq2db/linq2db/issues/5683 - `as`-spelled branches projecting differing-but-assignable types must meet in the same columns
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void ConcatBranchProjectingDerivedTypeThroughAs([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Parent where p.ParentID > 3 select new BranchProjection { Id = p.ParentID, Value = p.Value1 })
				.Concat(
					// the `as` spelling of the same cast the test above writes as `(BranchProjection)`
					from p in db.Parent where p.ParentID <= 3 select new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 } as BranchProjection);

			var result = AssertQuery(query);

			// Same per-branch assertions as the `(BranchProjection)`-cast twin: AssertQuery compares member-wise, and the
			// column count is symmetric, so neither can see the branches read with each other's constructor.
			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedBranchProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(BranchProjection));

			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		// https://github.com/linq2db/linq2db/issues/5683 - branches pair by the element type even with no cast written anywhere
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void ConcatBranchProjectingDerivedTypeWithoutCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var baseBranch    = from p in db.Parent where p.ParentID >  3 select new BranchProjection        { Id = p.ParentID, Value = p.Value1 };
			// no cast: Concat binds TSource from the first operand and takes the second through IEnumerable<T>
			// covariance, so this branch reaches the set operation as a bare constructor typed DerivedBranchProjection
			var derivedBranch = from p in db.Parent where p.ParentID <= 3 select new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 };

			var query = baseBranch.Concat(derivedBranch);

			var result = AssertQuery(query);

			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedBranchProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(BranchProjection));

			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		// https://github.com/linq2db/linq2db/issues/5683 - a distinct set operation cannot pair differing construction types and must say so
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void UnionBranchProjectingDerivedTypeIsRefused([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Pairing the branches removes the NULL padding that used to double as the branch discriminator, and
			// GetDifferencePredicate only creates the __projection__set_id__ anchor for UNION ALL. So a distinct
			// set operation has no way to tell the branches apart, and refuses rather than comparing rows that
			// would need a discriminator column - which would itself stop equal rows from de-duplicating.
			// Before this was refused it silently returned wrong rows (9 where 7 were due).
			var query =
				(from p in db.Parent where p.ParentID > 2 select new BranchProjection { Id = p.ParentID, Value = p.Value1 })
				.Union(
					from p in db.Parent where p.ParentID <= 4 select (BranchProjection)new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 });

			Shouldly.Should.Throw<LinqToDBException>(() => query.ToList())
				.Message.ShouldContain("Could not decide which construction type to use");
		}

		// https://github.com/linq2db/linq2db/issues/5683 - Except needs no discriminator, so pairing makes it correct
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void ExceptBranchProjectingDerivedType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Except/ExceptAll read both sides through the left projection (InitializeProjections assigns
			// _projection2 = _projection1), so they never need to tell a row's branch apart and reach none of the
			// refusal above. Unpaired, the padded branches shared no column and nothing was ever removed.
			var query =
				(from p in db.Parent where p.ParentID > 2 select new BranchProjection { Id = p.ParentID, Value = p.Value1 })
				.Except(
					from p in db.Parent where p.ParentID <= 4 select (BranchProjection)new DerivedBranchProjection { Id = p.ParentID, Value = p.Value1 });

			query.ToList().Select(x => x.Id).OrderBy(x => x).ToArray().ShouldBe(new[] { 5, 6, 7 });
		}

		class BranchProjection
		{
			public int  Id    { get; set; }
			public int? Value { get; set; }
		}

		sealed class DerivedBranchProjection : BranchProjection;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/115")]
		public void UnionOfTwoJoinQueries([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var qs = (from c in db.Child
					  join r in db.Parent on c.ParentID equals r.ParentID
					  where r.ParentID > 4
					  select c
					)
					.Union(from c in db.Child
						   join r in db.Parent on c.ParentID equals r.ParentID
						   where r.ParentID <= 4
						   select c
					);

			var ql = (from c in Child
					  join r in Parent on c.ParentID equals r.ParentID
					  where r.ParentID > 4
					  select c
					)
					.Union(from c in Child
						   join r in Parent on c.ParentID equals r.ParentID
						   where r.ParentID <= 4
						   select c
					);

			AreEqual(ql, qs);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/508")]
		public void UnionOfJoinedKeyAndPlainKeyWithFilter([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = (
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					where c.ChildID == 11
					select p.ParentID
							 ).Union(
					from c in db.Child
					where c.ChildID == 11
					select c.ParentID
								   );
			var expected = (
					from c in Child
					join p in Parent on c.ParentID equals p.ParentID
					where c.ChildID == 11
					select p.ParentID
							 ).Union(
					from c in Child
					where c.ChildID == 11
					select c.ParentID
								   );

			AreEqual(expected, query);
		}
	}
}
