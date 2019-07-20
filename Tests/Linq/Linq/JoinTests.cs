using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	public static class EnumerableExtensions
	{
		public static IEnumerable<TResult> SqlJoinInternal<TOuter, TInner, TResult>(
			[JetBrains.Annotations.NotNull] this IEnumerable<TOuter>      outer,
			[JetBrains.Annotations.NotNull] IEnumerable<TInner>           inner,
			                                SqlJoinType                   joinType, 
			[JetBrains.Annotations.NotNull] Func<TOuter, TInner, bool>    predicate,
			[JetBrains.Annotations.NotNull] Func<TOuter, TInner, TResult> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (predicate      == null) throw new ArgumentNullException(nameof(predicate));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			switch (joinType)
			{
				case SqlJoinType.Inner:
					return outer.SelectMany(f => inner.Where(s => predicate(f, s)).Select(s => resultSelector(f, s)));
				case SqlJoinType.Left:
					return outer.SelectMany(f => inner.Where(s => predicate(f, s)).DefaultIfEmpty().Select(s => resultSelector(f, s)));
				case SqlJoinType.Right:
					return inner.SelectMany(s => outer.Where(f => predicate(f, s)).DefaultIfEmpty().Select(f => resultSelector(f, s)));
				case SqlJoinType.Full:
					var firstItems = outer.ToList();
					var secondItems = inner.ToList();
					var firstResult = firstItems.SelectMany(f =>
						secondItems.Where(s => predicate(f, s)).DefaultIfEmpty().Select(s => new {First = f, Second = s}));

					var secondResult = secondItems.Where(s => !firstItems.Any(f => predicate(f, s)))
						.Select(s => new {First = default(TOuter), Second = s});

					var res = firstResult.Concat(secondResult).Select(r => resultSelector(r.First, r.Second));
					return res;
				default:
					throw new ArgumentOutOfRangeException(nameof(joinType), joinType, null);
			}
		}

		public static IEnumerable<TResult> SqlJoinInternal<TOuter, TInner, TKey, TResult>(
			[JetBrains.Annotations.NotNull] this IEnumerable<TOuter>      outer,
			[JetBrains.Annotations.NotNull] IEnumerable<TInner>           inner, 
			                                SqlJoinType                   joinType,
			[JetBrains.Annotations.NotNull] Func<TOuter, TKey>            outerKeySelector, 
			[JetBrains.Annotations.NotNull] Func<TInner, TKey>            innerKeySelector,
			[JetBrains.Annotations.NotNull] Func<TOuter, TInner, TResult> resultSelector)
		{
			if (outer            == null) throw new ArgumentNullException(nameof(outer));
			if (inner            == null) throw new ArgumentNullException(nameof(inner));
			if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
			if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
			if (resultSelector   == null) throw new ArgumentNullException(nameof(resultSelector));

			switch (joinType)
			{
				case SqlJoinType.Inner:
					return outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector);
				case SqlJoinType.Left:
					return outer
						.GroupJoin(inner, outerKeySelector, innerKeySelector, (o, gr) => new {o, gr})
						.SelectMany(t => t.gr.DefaultIfEmpty(), (t1, t2) => resultSelector(t1.o, t2));
				case SqlJoinType.Right:
					return inner
						.GroupJoin(outer, innerKeySelector, outerKeySelector, (o, gr) => new { o, gr })
						.SelectMany(t => t.gr.DefaultIfEmpty(), (t1, t2) => resultSelector(t2, t1.o));
				case SqlJoinType.Full:
					var keys1 = outer.ToLookup(outerKeySelector);
					var keys2 = inner.ToLookup(innerKeySelector);
					var res = new List<TResult>();
					foreach (var pair1 in keys1)
					{
						if (keys2.Contains(pair1.Key))
						{
							res.AddRange(pair1.Join(keys2[pair1.Key], outerKeySelector, innerKeySelector, resultSelector));
							continue;
						}
						res.AddRange(pair1.Select(r => resultSelector(r, default)));
					}

					foreach (var pair2 in keys2)
					{
						if (keys1.Contains(pair2.Key))
						{
							continue;
						}
						res.AddRange(pair2.Select(r => resultSelector(default, r)));
					}

					return res;
				default:
					throw new ArgumentOutOfRangeException(nameof(joinType), joinType, null);
			}
		}
	}

	[TestFixture]
	public class JoinTests : TestBase
	{
		[Test]
		public void InnerJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
						join p2 in db.Person on p1.ID equals p2.ID
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName });
		}

		[Test]
		public void InnerJoin2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
						join p2 in db.Person on new { p1.ID, p1.FirstName } equals new { p2.ID, p2.FirstName }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName });
		}

		[Test]
		public void InnerJoin3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
						join p2 in
							from p2 in db.Person join p3 in db.Person on new { p2.ID, p2.LastName } equals new { p3.ID, p3.LastName } select new { p2, p3 }
						on new { p1.ID, p1.FirstName } equals new { p2.p2.ID, p2.p2.FirstName }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.p2.FirstName, LastName = p2.p3.LastName });
		}

		[Test]
		public void InnerJoin4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
						join p2 in db.Person on new { p1.ID, p1.FirstName } equals new { p2.ID, p2.FirstName }
							join p3 in db.Person on new { p2.ID, p2.LastName } equals new { p3.ID, p3.LastName }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName });
		}

		[Test]
		public void InnerJoin5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
					join p2 in db.Person on new { p1.ID, p1.FirstName } equals new { p2.ID, p2.FirstName }
					join p3 in db.Person on new { p1.ID, p2.LastName  } equals new { p3.ID, p3.LastName  }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName });
		}

		[Test]
		public void InnerJoin6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p1 in db.Person
						join p2 in from p3 in db.Person select new { ID = p3.ID + 1, p3.FirstName } on p1.ID equals p2.ID - 1
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName });
		}

		[Test]
		public void InnerJoin7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from ch in Child
							join p in Parent on ch.ParentID equals p.ParentID
						select ch.ParentID + p.ParentID
					where t > 2
					select t
					,
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select ch.ParentID + p.ParentID
					where t > 2
					select t);
		}

		[Test]
		public void InnerJoin8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from ch in Child
							join p in Parent on ch.ParentID equals p.ParentID
						select new { ID = ch.ParentID + p.ParentID }
					where t.ID > 2
					select t,
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select new { ID = ch.ParentID + p.ParentID }
					where t.ID > 2
					select t);
		}

		[Test]
		public void InnerJoin9([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from g in GrandChild
					join p in Parent4 on g.Child.ParentID equals p.ParentID
					where g.ParentID < 10 && p.Value1 == TypeValue.Value3
					select g,
					from g in db.GrandChild
					join p in db.Parent4 on g.Child.ParentID equals p.ParentID
					where g.ParentID < 10 && p.Value1 == TypeValue.Value3
					select g);
		}

		[Test]
		public void InnerJoin10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					join g in    GrandChild on p.ParentID equals g.ParentID into q
					from q1 in q
					select new { p.ParentID, q1.GrandChildID },
					from p in db.Parent
					join g in db.GrandChild on p.ParentID equals g.ParentID into q
					from q1 in q
					select new { p.ParentID, q1.GrandChildID });
		}

		[Test]
		public void GroupJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select p,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select p);
		}

		[Test]
		public void GroupJoin2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID into lj
					where p.ParentID == 1
					select new { p, lj };

				var list = q.ToList();

				Assert.AreEqual(1, list.Count);
				Assert.AreEqual(1, list[0].p.ParentID);
				Assert.AreEqual(1, list[0].lj.Count());

				var ch = list[0].lj.ToList();

				Assert.AreEqual( 1, ch[0].ParentID);
				Assert.AreEqual(11, ch[0].ChildID);
			}
		}

		[Test]
		public void GroupJoin3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = Parent
					.GroupJoin(
						Child,
						p  => p.ParentID,
						ch => ch.ParentID,
						(p, lj1) => new { p, lj1 = new { lj1 } }
					)
					.Where (t => t.p.ParentID == 2)
					.Select(t => new { t.p, t.lj1 });

				var list1 = q1.ToList();

				var q2 = db.Parent
					.GroupJoin(
						db.Child,
						p  => p.ParentID,
						ch => ch.ParentID,
						(p, lj1) => new { p, lj1 = new { lj1 } }
					)
					.Where (t => t.p.ParentID == 2)
					.Select(t => new { t.p, t.lj1 });

				var list2 = q2.ToList();

				Assert.AreEqual(list1.Count,              list2.Count);
				Assert.AreEqual(list1[0].p.ParentID,      list2[0].p.ParentID);
				Assert.AreEqual(list1[0].lj1.lj1.Count(), list2[0].lj1.lj1.Count());
			}
		}

		[Test]
		public void GroupJoin4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in Parent
						join ch in
							from c in Child select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 3
					select new { p, lj1 };

				var list1 = q1.ToList();

				var q2 =
					from p in db.Parent
						join ch in
							from c in db.Child select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 3
					select new { p, lj1 };

				var list2 = q2.ToList();

				Assert.AreEqual(list1.Count,          list2.Count);
				Assert.AreEqual(list1[0].p.ParentID,  list2[0].p.ParentID);
				Assert.AreEqual(list1[0].lj1.Count(), list2[0].lj1.Count());
			}
		}

		[Test]
		public void GroupJoin5([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.First()
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.First());
		}

		[Test]
		public void GroupJoin51([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var result =
				(
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1, p2 = lj1.First() }
				).ToList();

				var expected =
				(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1, p2 = lj1.First() }
				).ToList();

				Assert.AreEqual(expected.Count, result.Count);
				AreEqual(expected[0].p1, result[0].p1);
			}
		}

		[Test]
		public void GroupJoin52([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.First().ParentID
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.First().ParentID);
		}

		[Test]
		public void GroupJoin53([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.Select(_ => _.ParentID).First()
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select lj1.Select(_ => _.ParentID).First());
		}

		[Test]
		public void GroupJoin54([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1.Count(), p2 = lj1.First() }
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1.Count(), p2 = lj1.First() });
		}

		[Test]
		public void GroupJoin6([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in Parent
						join c in Child on p.ParentID + n equals c.ParentID into lj
					where p.ParentID == 1
					select new { p, lj };

				var list1 = q1.ToList();
				var ch1   = list1[0].lj.ToList();

				var q2 =
					from p in db.Parent
						join c in db.Child on p.ParentID + n equals c.ParentID into lj
					where p.ParentID == 1
					select new { p, lj };

				var list2 = q2.ToList();

				Assert.AreEqual(list1.Count,         list2.Count);
				Assert.AreEqual(list1[0].p.ParentID, list2[0].p.ParentID);
				Assert.AreEqual(list1[0].lj.Count(), list2[0].lj.Count());

				var ch2 = list2[0].lj.ToList();

				Assert.AreEqual(ch1[0].ParentID, ch2[0].ParentID);
				Assert.AreEqual(ch1[0].ChildID,  ch2[0].ChildID);
			}
		}

		[Test]
		public void GroupJoin7([DataSources(TestProvName.AllFirebird)] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in Parent
						join c in Child on new { id = p.ParentID } equals new { id = c.ParentID - n } into j
					where p.ParentID == 1
					select new { p, j };

				var list1 = q1.ToList();
				var ch1   = list1[0].j.ToList();

				var q2 =
					from p in db.Parent
						join c in db.Child on new { id = p.ParentID } equals new { id = c.ParentID - n } into j
					where p.ParentID == 1
					select new { p, j };

				var list2 = q2.ToList();

				Assert.AreEqual(list1.Count,         list2.Count);
				Assert.AreEqual(list1[0].p.ParentID, list2[0].p.ParentID);
				Assert.AreEqual(list1[0].j.Count(),  list2[0].j.Count());

				var ch2 = list2[0].j.ToList();

				Assert.AreEqual(ch1[0].ParentID, ch2[0].ParentID);
				Assert.AreEqual(ch1[0].ChildID,  ch2[0].ChildID);
			}
		}

		[Test]
		public void GroupJoin8([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID into g
					select new { Child = g.FirstOrDefault() }
					,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into g
					select new { Child = g.FirstOrDefault() });
		}

		[Test]
		public void GroupJoin9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.GroupJoin(
							Parent,
							x => new { Id = x.ParentID },
							y => new { Id = y.ParentID },
							(xid, yid) => new { xid, yid }
						)
						.SelectMany(
							y => y.yid.DefaultIfEmpty(),
							(x1, y) => new { x1.xid, y }
						)
						.GroupJoin(
							Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x2, y) => new { x2.xid, x2.y, h = y }
						)
						.SelectMany(
							a => a.h.DefaultIfEmpty(),
							(x3, a) => new { x3.xid, x3.y, a }
						)
						.GroupJoin(
							Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x4, y) => new { x4.xid, x4.y, x4.a, p = y }
						)
						.SelectMany(
							z => z.p.DefaultIfEmpty(),
							(x5, z) => new { x5.xid, z, x5.y, x5.a }
						)
						.GroupJoin(
							Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.Value1 ?? 1 },
							(x6, y) => new { x6.xid, xy = x6.y, x6.a, x6.z, y }
						)
						.SelectMany(
							z => z.y.DefaultIfEmpty(),
							(x7, z) => new { x7.xid, z, x7.xy, x7.a, xz = x7.z }
						)
						.GroupJoin(
							Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x8, y) => new { x8.xid, x8.z, x8.xy, x8.a, x8.xz, y }
						)
						.SelectMany(
							a => a.y.DefaultIfEmpty(),
							(x9, a) => new { x9.xid, x9.z, x9.xy, xa = x9.a, x9.xz, a }
						),
					db.Parent
						.GroupJoin(
							db.Parent,
							x => new { Id = x.ParentID },
							y => new { Id = y.ParentID },
							(xid, yid) => new { xid, yid }
						)
						.SelectMany(
							y => y.yid.DefaultIfEmpty(),
							(x1, y) => new { x1.xid, y }
						)
						.GroupJoin(
							db.Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x2, y) => new { x2.xid, x2.y, h = y }
						)
						.SelectMany(
							a => a.h.DefaultIfEmpty(),
							(x3, a) => new { x3.xid, x3.y, a }
						)
						.GroupJoin(
							db.Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x4, y) => new { x4.xid, x4.y, x4.a, p = y }
						)
						.SelectMany(
							z => z.p.DefaultIfEmpty(),
							(x5, z) => new { x5.xid, z, x5.y, x5.a }
						)
						.GroupJoin(
							db.Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.Value1 ?? 1 },
							(x6, y) => new { x6.xid, xy = x6.y, x6.a, x6.z, y }
						)
						.SelectMany(
							z => z.y.DefaultIfEmpty(),
							(x7, z) => new { x7.xid, z, x7.xy, x7.a, xz = x7.z }
						)
						.GroupJoin(
							db.Parent,
							x => new { Id = x.xid.ParentID },
							y => new { Id = y.ParentID     },
							(x8, y) => new { x8.xid, x8.z, x8.xy, x8.a, x8.xz, y }
						)
						.SelectMany(
							a => a.y.DefaultIfEmpty(),
							(x9, a) => new { x9.xid, x9.z, x9.xy, xa = x9.a, x9.xz, a }
						));
		}

		[Test]
		public void GroupJoinAny1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					join c in    Child on p.ParentID equals c.ParentID into t
					select new { p.ParentID, n = t.Any() },
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into t
					select new { p.ParentID, n = t.Any() });
		}

		[Test]
		public void GroupJoinAny2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					join c in    Child on p.ParentID equals c.ParentID into t
					select new { p.ParentID, n = t.Select(t1 => t1.ChildID > 0).Any() },
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into t
					select new { p.ParentID, n = t.Select(t1 => t1.ChildID > 0).Any() });
		}

		[Test]
		public void GroupJoinAny3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					let c = from c in    Child where p.ParentID == c.ParentID select c
					select new { p.ParentID, n = c.Any() },
					from p in db.Parent
					let c = from c in db.Child where p.ParentID == c.ParentID select c
					select new { p.ParentID, n = c.Any() });
		}

		[Test]
		public void GroupJoinAny4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					select new { p.ParentID, n = (from c in    Child where p.ParentID == c.ParentID select c).Any() },
					from p in db.Parent
					select new { p.ParentID, n = (from c in db.Child where p.ParentID == c.ParentID select c).Any() });
		}

		[Test]
		public void GroupJoinAny5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					join c in    Child on p.ParentID equals c.ParentID into t
					select new { n = t.Any() },
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into t
					where 1 > 0
					select new { n = t.Any() });
		}

		[Test]
		public void LeftJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where p.ParentID >= 4
					select new { p, ch }
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where p.ParentID >= 4
					select new { p, ch });
		}

		[Test]
		public void LeftJoin2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					select new { p, ch }
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					select new { p, ch });
		}

		[Test]
		public void LeftJoin3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child select c.Parent,
					from c in db.Child select c.Parent);
		}

		[Test]
		public void LeftJoin4([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.GroupJoin(Child,
							x => new { x.ParentID, x.Value1 },
							y => new { y.ParentID, Value1 = (int?)y.ParentID },
							(x1, y1) => new { Parent = x1, Child = y1 })
						.SelectMany(
							y2 => y2.Child.DefaultIfEmpty(),
							(x3, y3) => new { x3.Parent, Child = x3.Child.FirstOrDefault() })
						.Where(x4 => x4.Parent.ParentID == 1 && x4.Parent.Value1 != null)
						.OrderBy(x5 => x5.Parent.ParentID)
					,
					db.Parent
						.GroupJoin(db.Child,
							x1 => new { x1.ParentID, x1.Value1 },
							y1 => new { y1.ParentID, Value1 = (int?)y1.ParentID },
							(x2, y2) => new { Parent = x2, Child = y2 })
						.SelectMany(
							y3 => y3.Child.DefaultIfEmpty(),
							(x4, y4) => new { x4.Parent, Child = x4.Child.FirstOrDefault() })
						.Where(x5 => x5.Parent.ParentID == 1 && x5.Parent.Value1 != null)
						.OrderBy(x6 => x6.Parent.ParentID));
		}

		[Table("Child")]
		public class CountedChild
		{
			public static int Count;

			public CountedChild()
			{
				Count++;
			}

			[Column] public int ParentID;
			[Column] public int ChildID;
		}

		[Test]
		public void LeftJoin5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
						join ch in db.GetTable<CountedChild>() on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select new { p, ch, ch1 = ch };

				CountedChild.Count = 0;

				var _ = q.ToList();

				Assert.AreEqual(0, CountedChild.Count);
			}
		}

		[Test]
		public void LeftJoin6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					from ch in Child.Where(c => p.ParentID == c.ParentID).DefaultIfEmpty()
					where p.ParentID >= 4
					select new { p, ch }
					,
					from p in db.Parent
					from ch in db.Child.Where(c => p.ParentID == c.ParentID).DefaultIfEmpty()
					where p.ParentID >= 4
					select new { p, ch });
		}

		[ActiveIssue(577)]
		[Test]
		public void MultipleLeftJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result =
					from parent             in db.Parent
					join child              in db.Child      on parent.ParentID equals child.ParentID      into childTemp
					join grandChild         in db.GrandChild on parent.ParentID equals grandChild.ParentID into grandChildTemp
					from grandChildLeftJoin in grandChildTemp.DefaultIfEmpty()
					from childLeftJoin      in childTemp.DefaultIfEmpty()
					select new { parent.ParentID, ChildID = (int?)childLeftJoin.ChildID, GrandChildID = (int?)grandChildLeftJoin.GrandChildID };

				var expected =
					from parent             in Parent
					join child              in Child      on parent.ParentID equals child.ParentID      into childTemp
					join grandChild         in GrandChild on parent.ParentID equals grandChild.ParentID into grandChildTemp
					from grandChildLeftJoin in grandChildTemp.DefaultIfEmpty()
					from childLeftJoin      in childTemp.DefaultIfEmpty()
					select new { parent.ParentID, childLeftJoin?.ChildID, grandChildLeftJoin?.GrandChildID };

				AreEqual(expected, result);
			}
		}

		[Test]
		public void SubQueryJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in
							from c in Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					select p
					,
					from p in db.Parent
						join ch in
							from c in db.Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					select p);
		}

		[Test]
		public void ReferenceJoin1([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child join g in    GrandChild on c equals g.Child select new { c.ParentID, g.GrandChildID },
					from c in db.Child join g in db.GrandChild on c equals g.Child select new { c.ParentID, g.GrandChildID });
		}

		[Test]
		public void ReferenceJoin2([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from g in    GrandChild
						join c in    Child on g.Child equals c
					select new { c.ParentID, g.GrandChildID },
					from g in db.GrandChild
						join c in db.Child on g.Child equals c
					select new { c.ParentID, g.GrandChildID });
		}

		[Test]
		public void JoinByAnonymousTest([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					join c in    Child on new { Parent = p, p.ParentID } equals new { c.Parent, c.ParentID }
					select new { p.ParentID, c.ChildID },
					from p in db.Parent
					join c in db.Child on new { Parent = p, p.ParentID } equals new { c.Parent, c.ParentID }
					select new { p.ParentID, c.ChildID });
		}

		[Test]
		public void FourTableJoin([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c1 in Child      on p.ParentID  equals c1.ParentID
					join c2 in GrandChild on c1.ParentID equals c2.ParentID
					join c3 in GrandChild on c2.ParentID equals c3.ParentID
					select new { p, c1Key = c1.ChildID, c2Key = c2.GrandChildID, c3Key = c3.GrandChildID }
					,
					from p in db.Parent
					join c1 in db.Child      on p.ParentID  equals c1.ParentID
					join c2 in db.GrandChild on c1.ParentID equals c2.ParentID
					join c3 in db.GrandChild on c2.ParentID equals c3.ParentID
					select new { p, c1Key = c1.ChildID, c2Key = c2.GrandChildID, c3Key = c3.GrandChildID });
		}

		[Test]
		public void ProjectionTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in Person
					join p2 in Person on p1.ID equals p2.ID
					select new { ID1 = new { Value = p1.ID }, FirstName2 = p2.FirstName, } into p1
					select p1.ID1.Value
					,
					from p1 in db.Person
					join p2 in db.Person on p1.ID equals p2.ID
					select new { ID1 = new { Value = p1.ID }, FirstName2 = p2.FirstName, } into p1
					select p1.ID1.Value);
		}

		[Test]
		public void LeftJoinTest([DataSources] string context)
		{
			// Reproduces the problem described here: http://rsdn.ru/forum/prj.rfd/4221837.flat.aspx
			using (var db = GetDataContext(context))
			{
				var q =
					from p1 in db.Person
					join p2 in db.Person on p1.ID equals p2.ID into g
					from p2 in g.DefaultIfEmpty() // yes I know the join will always succeed and it'll never be null, but just for test's sake :)
					select new { p1, p2 };

				var list = q.ToList(); // NotImplementedException? :(
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public void LeftJoinTest2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// THIS TEST MUST BE RUN IN RELEASE CONFIGURATION (BECAUSE IT PASSES UNDER DEBUG CONFIGURATION)
			// Reproduces the problem described here: http://rsdn.ru/forum/prj.rfd/4221837.flat.aspx

			using (var db = GetDataContext(context))
			{
				var q =
					from p1 in db.Patient
					join p2 in db.Patient on p1.Diagnosis equals p2.Diagnosis into g
					from p2 in g.DefaultIfEmpty() // yes I know the join will always succeed and it'll never be null, but just for test's sake :)
					join p3 in db.Person on p2.PersonID equals p3.ID
					select new { p1, p2, p3 };

				var arr = q.ToArray(); // NotImplementedException? :(
				Assert.That(arr, Is.Not.Empty);
			}
		}

		[Test, Explicit]
		public void StackOverflow([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012/*, ProviderName.SqlServer2014*/)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select new { p, c };

				for (var i = 0; i < 100; i++)
				{
					q =
						from c in q
						join p in db.Parent on c.p.ParentID equals p.ParentID
						select new { p, c.c };
				}

				var list = q.ToList();
				Assert.IsNotEmpty(list);
			}
		}

		[Test]
		public void ApplyJoin([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.Child
					from p in new Model.Functions(db).GetParentByID(ch.Parent.ParentID)
					select p;

				var _ = q.ToList();
			}
		}

		[Test]
		public void BltIssue257([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from m in db.Types
						join p in db.Parent on m.ID equals p.ParentID
					group m by new
					{
						m.DateTimeValue.Date
					}
					into b
					select new
					{
						QualiStatusByDate = b.Key,
						Count             = b.Count()
					};

				var _ = q.ToList();
			}
		}

		[Test]
		public void NullJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent
					join p2 in    Parent on new { p1.ParentID, p1.Value1 } equals new { p2.ParentID, p2.Value1 }
					select p2
					,
					from p1 in db.Parent
					join p2 in db.Parent on new { p1.ParentID, p1.Value1 } equals new { p2.ParentID, p2.Value1 }
					select p2);
		}

		[Test]
		public void NullJoin2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent
					join p2 in    Parent
						on     new { a = new { p1.ParentID, p1.Value1 } }
						equals new { a = new { p2.ParentID, p2.Value1 } }
					select p2
					,
					from p1 in db.Parent
					join p2 in db.Parent
						on     new { a = new { p1.ParentID, p1.Value1 } }
						equals new { a = new { p2.ParentID, p2.Value1 } }
					select p2);
		}

		[Test]
		public void NullWhereJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent
					from p2 in    Parent.Where(p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2
					,
					from p1 in db.Parent
					from p2 in db.Parent.Where(p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2);
		}

		[Test]
		public void JoinSubQueryCount([DataSources(
			ProviderName.Access, ProviderName.SqlCe, ProviderName.SqlServer2000)]
			string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					where p.ParentID > 0
					join c in Child on p.ParentID equals c.ParentID into t
					//select new { p.ParentID, count = t.Count() }
					select new { p.ParentID, count = t.Where(c => c.ChildID != p.ParentID * 10 + n).Count() }
					,
					from p in db.Parent
					where p.ParentID > 0
					join c in db.Child on p.ParentID equals c.ParentID into t
					//select new { p.ParentID, count = t.Count() }
					select new { p.ParentID, count = t.Where(c => c.ChildID != p.ParentID * 10 + n).Count() }
					);
		}

		[Test]
		public void JoinSubQuerySum([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					where p.ParentID > 0
					join c in Child on p.ParentID equals c.ParentID into t
					//select new { p.ParentID, count = t.Count() }
					select new { p.ParentID, count = t.Where(c => c.ChildID != p.ParentID * 10 + 1).Sum(c => c.ChildID) }
					,
					from p in db.Parent
					where p.ParentID > 0
					join c in db.Child on p.ParentID equals c.ParentID into t
					//select new { p.ParentID, count = t.Count() }
					select new { p.ParentID, count = t.Where(c => c.ChildID != p.ParentID * 10 + 1).Sum(c => c.ChildID) }
					);
		}

		[Test]
		public void FromLeftJoinTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					from g in db.GrandChild
						.Where(t =>
							db.Person
								.Select(r => r.ID)
								.Contains(c.ChildID))
						.DefaultIfEmpty()
					select new { p.ParentID }
					;

				var _ = q.ToList();
			}
		}

		public class AllJoinsSourceAttribute : IncludeDataSourcesAttribute
		{
			public AllJoinsSourceAttribute() : base(
				TestProvName.AllSqlServer2005Plus,
				TestProvName.AllOracle,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL)
			{
			}
		}

		[Test]
		public void SqlJoinSimple([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent
						.SqlJoinInternal(Child, joinType, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { ParentID = p.p == null ? (int?) null : p.p.ParentID, ChildID = p.c == null ? (int?) null : p.c.ChildID};

				var actual = from p in db.Parent
					from c in db.Child.Join(joinType, r => p.ParentID == r.ParentID)
					select new {ParentID = (int?) p.ParentID, ChildID = (int?) c.ChildID};

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLeftJoinSimple1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.SqlJoinInternal(Child, SqlJoinType.Left, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { p.p?.ParentID, p.c?.ChildID };

				var actual =
					from p in db.Parent
					from c in db.Child.Join(SqlJoinType.Left, r => p.ParentID == r.ParentID)
					select new {ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID};

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLeftJoinSimple2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.SqlJoinInternal(Child, SqlJoinType.Left, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { p.p?.ParentID, p.c?.ChildID };

				var actual =
					from p in db.Parent
					from c in db.Child.LeftJoin(r => p.ParentID == r.ParentID)
					select new {ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID};

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlJoinSubQuery([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent.Where(p => p.ParentID > 0).Take(10)
						.SqlJoinInternal(Child, joinType, (p, c) => p.ParentID == c.ParentID, (p, c) => new { p, c })
					select new { ParentID = p.p == null ? (int?)null : p.p.ParentID, ChildID = p.c == null ? (int?)null : p.c.ChildID };

				var actual = from p in db.Parent.Where(p => p.ParentID > 0).Take(10)
					from c in db.Child.Join(joinType, r => p.ParentID == r.ParentID)
					select new { ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID };

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlNullWhereJoin([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.SqlJoinInternal(Parent, joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual =
					from p1 in db.Parent
					from p2 in db.Parent.Join(joinType, p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2;

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		[Test]
		public void SqlNullWhereSubqueryJoin([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.Take(10).SqlJoinInternal(Parent.Take(10), joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual =
					from p1 in db.Parent.Take(10)
					from p2 in db.Parent.Take(10).Join(joinType, p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2;

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		[Test]
		public void SqlLinqJoinSimple([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent
						.SqlJoinInternal(Child, joinType, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { ParentID = p.p == null ? (int?) null : p.p.ParentID, ChildID = p.c == null ? (int?) null : p.c.ChildID};

				var actual = db.Parent.Join(db.Child, joinType, (p, c) => p.ParentID == c.ParentID,
					(p, c) => new {ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID});

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLinqLeftJoinSimple1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.SqlJoinInternal(Child, SqlJoinType.Left, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { p.p?.ParentID, p.c?.ChildID };

				var actual = db.Parent.Join(db.Child, SqlJoinType.Left, (p, c) => p.ParentID == c.ParentID,
					(p, c) => new {ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID});

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLinqLeftJoinSimple2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.SqlJoinInternal(Child, SqlJoinType.Left, (p, c) => p.ParentID == c.ParentID, (p, c) => new {p, c})
					select new { p.p?.ParentID, p.c?.ChildID };

				var actual = db.Parent.LeftJoin(db.Child, (p, c) => p.ParentID == c.ParentID,
					(p, c) => new {ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID});

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLinqJoinSubQuery([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent.Where(p => p.ParentID > 0).Take(10)
						.SqlJoinInternal(Child, joinType, (p, c) => p.ParentID == c.ParentID, (p, c) => new { p, c })
					select new { ParentID = p.p == null ? (int?)null : p.p.ParentID, ChildID = p.c == null ? (int?)null : p.c.ChildID };

				var actual = db.Parent.Where(p => p.ParentID > 0).Take(10)
					.Join(db.Child, joinType, (p, c) => p.ParentID == c.ParentID, 
						(p, c) => new { ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID });

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlLinqNullWhereJoin([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.SqlJoinInternal(Parent, joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual = db.Parent.Join(db.Parent, joinType, (p1, p2) => p1.ParentID == p2.ParentID && p1.Value1 == p2.Value1,
					(p1, p2) => p2);

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		[Test]
		public void SqlLinqNullWhereSubqueryJoin([AllJoinsSource] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.Take(10).SqlJoinInternal(Parent.Take(10), joinType,
					(p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual = db.Parent.Take(10).Join(db.Parent.Take(10), joinType,
					(p1, p2) => p1.ParentID == p2.ParentID && p1.Value1 == p2.Value1, (p1, p2) => p2);

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		// https://imgflip.com/i/2a6oc8
		[ActiveIssue(
			Configuration = TestProvName.AllSybase,
			Details       = "Cross-join doesn't work in Sybase")]
		[Test]
		public void SqlLinqCrossJoinSubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent.Where(p => p.ParentID > 0).Take(10)
						.SqlJoinInternal(Child.Take(10), SqlJoinType.Inner, (p, c) => true, (p, c) => new { p, c })
					select new { ParentID = p.p == null ? (int?)null : p.p.ParentID, ChildID = p.c == null ? (int?)null : p.c.ChildID };

				var actual = db.Parent.Where(p => p.ParentID > 0).Take(10)
					.CrossJoin(db.Child.Take(10), (p, c) => new { ParentID = (int?)p.ParentID, ChildID = (int?)c.ChildID });

				AreEqual(expected.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.ChildID));
			}
		}

		[Test]
		public void SqlFullJoinWithCount1([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var areEqual =
					(from left in db.Parent
					from right in db.Parent.FullJoin(p => p.ParentID == left.ParentID)
					select Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count(right.ParentID, Sql.AggregateModifier.None).ToValue()
					&& Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count().ToValue())
					.Single();

				Assert.True(areEqual);
			}
		}

		[Test]
		public void SqlFullJoinWithCount2([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = Parent.First().ParentID;

				var areEqual =
					(from left in db.Parent.Where(p => p.ParentID != id)
					 from right in db.Parent.FullJoin(p => p.ParentID == left.ParentID)
					 select Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count(right.ParentID, Sql.AggregateModifier.None).ToValue()
					 && Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count().ToValue())
					.Single();

				Assert.False(areEqual);
			}
		}

		[Test]
		public void SqlFullJoinWithCount3([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = Parent.First().ParentID;

				var areEqual =
					(from left in db.Parent
					 from right in db.Parent.Where(p => p.ParentID != id).FullJoin(p => p.ParentID == left.ParentID)
					 select Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count(right.ParentID, Sql.AggregateModifier.None).ToValue()
					 && Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count().ToValue())
					.Single();

				Assert.False(areEqual);
			}
		}

		[Test]
		public void SqlFullJoinWithCount4([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var areEqual =
					(from left in db.Parent.Where(p => p.ParentID != id1)
					 from right in db.Parent.Where(p => p.ParentID != id2).FullJoin(p => p.ParentID == left.ParentID)
					 select Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count(right.ParentID, Sql.AggregateModifier.None).ToValue()
					 && Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count().ToValue())
					.Single();

				Assert.False(areEqual);
			}
		}

		[Test]
		public void SqlFullJoinWithCount5([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = Parent.First().ParentID;

				var areEqual =
					(from left in db.Parent.Where(p => p.ParentID != id)
					 from right in db.Parent.Where(p => p.ParentID != id).FullJoin(p => p.ParentID == left.ParentID)
					 select Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count(right.ParentID, Sql.AggregateModifier.None).ToValue()
					 && Sql.Ext.Count(left.ParentID, Sql.AggregateModifier.None).ToValue() == Sql.Ext.Count().ToValue())
					.Single();

				Assert.True(areEqual);
			}
		}

		[Test]
		public void SqlFullJoinWithBothFilters([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from left in db.Parent.Where(p => p.ParentID != id1)
					from right in db.Parent.Where(p => p.ParentID != id2).FullJoin(p => p.ParentID == left.ParentID)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id2), SqlJoinType.Full, o => o.ParentID, i => i.ParentID, (left, right) => new
							{
								Left = left?.ParentID,
								Right = right?.ParentID
							});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithBothFiltersAlternative([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from lr in db.Parent.Where(p => p.ParentID != id1)
						.FullJoin(db.Parent.Where(p => p.ParentID != id2), (left, right) => right.ParentID == left.ParentID, (left, right) => new { left, right})
					select new
					{
						Left = lr.left != null ? (int?)lr.left.ParentID : null,
						Right = lr.right != null ? (int?)lr.right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id2), SqlJoinType.Full, o => o.ParentID, i => i.ParentID, (left, right) => new
							{
								Left = left?.ParentID,
								Right = right?.ParentID
							});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnLeftWithConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from left in db.Parent.Where(p => p.ParentID != id1)
					from right in (
						from right in db.Parent.Where(p => p.ParentID != id2)
						join right2 in db.Parent.Where(p => p.ParentID != id1)
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2})
						.FullJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left  = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id2)
								.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right  = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnLeftWithoutConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;

				var actual =
					from left in db.Parent.Where(p => p.ParentID != id1)
					from right in (
						from right in db.Parent
						join right2 in db.Parent
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2 })
						.FullJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent
								.SqlJoinInternal(
								Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnLeftWithoutAllConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					from left in db.Parent
					from right in (
						from right in db.Parent
						join right2 in db.Parent
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2 })
						.FullJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent
								.SqlJoinInternal(
								Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnRightWithConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from left in (
						from left in db.Parent.Where(p => p.ParentID != id2)
						join left2 in db.Parent.Where(p => p.ParentID != id1)
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent.Where(p => p.ParentID != id1)
						.FullJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left  = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id2)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id1), SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnRightWithoutConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;

				var actual =
					from left in (
						from left in db.Parent
						join left2 in db.Parent
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent.Where(p => p.ParentID != id1)
						.FullJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left  = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlFullJoinWithInnerJoinOnRightWithoutAllConditions([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					from left in (
						from left in db.Parent
						join left2 in db.Parent
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent
						.FullJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent, SqlJoinType.Full, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnLeftWithConditions([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from left in db.Parent.Where(p => p.ParentID != id1)
					from right in (
						from right in db.Parent.Where(p => p.ParentID != id2)
						join right2 in db.Parent.Where(p => p.ParentID != id1)
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2 })
						.RightJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id2)
								.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnLeftWithoutConditions([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;

				var actual =
					from left in db.Parent.Where(p => p.ParentID != id1)
					from right in (
						from right in db.Parent
						join right2 in db.Parent
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2 })
						.RightJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id1)
						.SqlJoinInternal(
							Parent
								.SqlJoinInternal(
								Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnLeftWithoutAllConditions([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					from left in db.Parent
					from right in (
						from right in db.Parent
						join right2 in db.Parent
							on right.Value1 equals right2.Value1 + 2
						select new { right, right2 })
						.RightJoin(p => p.right.Value1 + 2 == left.Value1)
					select new
					{
						Left = left != null ? (int?)left.ParentID : null,
						Right = right.right != null ? (int?)right.right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent
								.SqlJoinInternal(
								Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (right, right2) => new
								{
									Right = right?.ParentID,
									Right2 = right2?.ParentID,
									Value1 = right?.Value1
								}), SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.ParentID,
									Right = right?.Right
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnRightWithConditions([DataSources(TestProvName.AllSQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;
				var id2 = Parent.Skip(1).First().ParentID;

				var actual =
					from left in (
						from left in db.Parent.Where(p => p.ParentID != id2)
						join left2 in db.Parent.Where(p => p.ParentID != id1)
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent.Where(p => p.ParentID != id1)
						.RightJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent.Where(p => p.ParentID != id2)
						.SqlJoinInternal(
							Parent.Where(p => p.ParentID != id1), SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnRightWithoutConditions([DataSources(TestProvName.AllSQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id1 = Parent.First().ParentID;

				var actual =
					from left in (
						from left in db.Parent
						join left2 in db.Parent
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent.Where(p => p.ParentID != id1)
						.RightJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent.Where(p => p.ParentID != id1), SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		[Test]
		public void SqlRightJoinWithInnerJoinOnRightWithoutAllConditions([DataSources(TestProvName.AllSQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual =
					from left in (
						from left in db.Parent
						join left2 in db.Parent
							on left.Value1 equals left2.Value1 + 2
						select new { left, left2 })
					from right in db.Parent
						.RightJoin(p => p.Value1 + 2 == left.left.Value1)
					select new
					{
						Left = left.left != null ? (int?)left.left.ParentID : null,
						Right = right != null ? (int?)right.ParentID : null,
					};

				var expected =
					Parent
						.SqlJoinInternal(
							Parent, SqlJoinType.Inner, o => o.Value1, i => i.Value1 + 2, (left, left2) => new
							{
								Value1 = left?.Value1,
								Left = left?.ParentID,
								left2 = left2?.ParentID
							})
							.SqlJoinInternal(
								Parent, SqlJoinType.Right, o => o.Value1, i => i.Value1 + 2, (left, right) => new
								{
									Left = left?.Left,
									Right = right?.ParentID
								});

				AreEqual(expected.OrderBy(p => p.Left), actual.OrderBy(p => p.Left));
			}
		}

		/// <summary>
		/// Tests that AllJoinsBuilder do not handle standard Joins
		/// </summary>
		/// <param name="context"></param>
		[Test]
		public void JoinBuildersConflicts([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Parent.Join(db.Child, SqlJoinType.Inner,
						(p, c) => p.ParentID == c.ChildID,
						(p, c) => new { p, c });

				var result1 = query1.ToArray();

				var query2 = from p in db.Parent
					join c in db.Child on p.ParentID equals c.ChildID
					select new
					{
						p,
						c
					};

				var result2 = query2.ToArray();
			}
		}

		[Table]
		public class Fact
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = "Id", OtherKey = "FactId", CanBeNull = true, Relationship = Relationship.OneToMany, IsBackReference = true)]
			public IEnumerable<Tag> TagFactIdIds { get; set; }

			public static readonly Fact[] Data = new[]
			{
				new Fact() { Id = 3 },
				new Fact() { Id = 4 },
				new Fact() { Id = 5 }
			};
		}

		[Table]
		public partial class Tag
		{
			[PrimaryKey]      public int    Id     { get; set; }
			[Column]          public int   FactId { get; set; }
			[Column, NotNull] public string Name   { get; set; }

			public static readonly Tag[] Data = new[]
			{
				new Tag() { Id = 1, FactId = 3, Name = "Tag3" },
				new Tag() { Id = 2, FactId = 3, Name = "Tag3" },
				new Tag() { Id = 3, FactId = 4, Name = "Tag4" }
			};

			public static readonly Tag[] FullJoinData = new[]
			{
				new Tag() { Id = 1, FactId = 3, Name = "Tag3" },
				new Tag() { Id = 2, FactId = 3, Name = "Tag3" },
				new Tag() { Id = 3, FactId = 4, Name = "Tag4" },
				new Tag() { Id = 4, FactId = 6, Name = "Tag6" }
			};
		}


		// https://github.com/linq2db/linq2db/issues/1773
		[Test]
		public void LeftJoinWithRecordSelection1([DataSources] string context)
		{
			using (var db        = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable  = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from fact in factTable
					join tag in tagTable on fact.Id equals tag.FactId into tagGroup
					from leftTag in tagGroup.DefaultIfEmpty()
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection2([DataSources] string context)
		{
			using (var db        = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable  = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from fact in factTable
					from leftTag in tagTable.LeftJoin(tag => tag.FactId == fact.Id)
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection3([DataSources] string context)
		{
			using (var db        = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable  = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from fact in factTable
					from leftTag in tagTable.Where(tag => tag.FactId == fact.Id).DefaultIfEmpty()
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection4([DataSources] string context)
		{
			using (var db        = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable  = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from fact in factTable
					from leftTag in tagTable.LeftJoin(tag => tag.FactId == fact.Id)
					where fact.Id > 3
					select new { fact, leftTag = leftTag != null ? leftTag : null };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var q =
					from ft in factTable.LeftJoin(tagTable, (f, t) => t.FactId == f.Id, (f, t) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var q =
					from ft in factTable.Join(tagTable, SqlJoinType.Left, (f, t) => t.FactId == f.Id, (f, t) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void LeftJoinWithRecordSelection7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from fact in factTable
					from leftTag in tagTable.Join(SqlJoinType.Left, tag => tag.FactId == fact.Id)
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void RightJoinWithRecordSelection1([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from leftTag in tagTable
					from fact in factTable.RightJoin(fact => leftTag.FactId == fact.Id)
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void RightJoinWithRecordSelection2([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var t =
					from leftTag in tagTable
					from fact in factTable.Join(SqlJoinType.Right, fact => leftTag.FactId == fact.Id)
					where fact.Id > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void RightJoinWithRecordSelection3([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var q =
					from ft in tagTable.RightJoin(factTable, (t, f) => t.FactId == f.Id, (t, f) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void RightJoinWithRecordSelection4([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.Data))
			{
				var q =
					from ft in tagTable.Join(factTable, SqlJoinType.Right, (t, f) => t.FactId == f.Id, (t, f) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreEqual(4, results[0].fact.Id);
				Assert.AreEqual("Tag4", results[0].leftTag.Name);
				Assert.AreEqual(5, results[1].fact.Id);
				Assert.IsNull(results[1].leftTag);
			}
		}

		[Test]
		public void FullJoinWithRecordSelection1([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.FullJoinData))
			{
				var t =
					from leftTag in tagTable
					from fact in factTable.FullJoin(fact => leftTag.FactId == fact.Id)
					where fact.Id > 3 || leftTag.FactId > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(3, results.Length);
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null));
				Assert.AreEqual(1, results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"));
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"));
			}
		}

		[Test]
		public void FullJoinWithRecordSelection2([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.FullJoinData))
			{
				var t =
					from leftTag in tagTable
					from fact in factTable.Join(SqlJoinType.Full, fact => leftTag.FactId == fact.Id)
					where fact.Id > 3 || leftTag.FactId > 3
					select new { fact, leftTag };

				var results = t.ToArray();

				Assert.AreEqual(3, results.Length);
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null));
				Assert.AreEqual(1, results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"));
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"));
			}
		}

		[Test]
		public void FullJoinWithRecordSelection3([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.FullJoinData))
			{
				var q =
					from ft in tagTable.FullJoin(factTable, (t, f) => t.FactId == f.Id, (t, f) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3 || ft.leftTag.FactId > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(3, results.Length);
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null));
				Assert.AreEqual(1, results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"));
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"));
			}
		}

		[Test]
		public void FullJoinWithRecordSelection4([DataSources(
			TestProvName.AllSQLite,
			ProviderName.Access,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (var factTable = db.CreateLocalTable(Fact.Data))
			using (var tagTable = db.CreateLocalTable(Tag.FullJoinData))
			{
				var q =
					from ft in tagTable.Join(factTable, SqlJoinType.Full, (t, f) => t.FactId == f.Id, (t, f) => new { fact = f, leftTag = t })
					where ft.fact.Id > 3 || ft.leftTag.FactId > 3
					select ft;

				var results = q.ToArray();

				Assert.AreEqual(3, results.Length);
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null));
				Assert.AreEqual(1, results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"));
				Assert.AreEqual(1, results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"));
			}
		}

		[Table]
		public class StLink
		{
			[PrimaryKey] public int     InId          { get; set; }
			[Column]     public double? InMaxQuantity { get; set; }
			[Column]     public double? InMinQuantity { get; set; }

			public static StLink[] Data = new[]
			{
				new StLink { InId = 1, InMinQuantity = 1,    InMaxQuantity = 2    },
				new StLink { InId = 2, InMinQuantity = null, InMaxQuantity = null }
			};
		}

		[Table]
		public class EdtLink
		{
			[PrimaryKey] public int     InId          { get; set; }
			[Column]     public double? InMaxQuantity { get; set; }
			[Column]     public double? InMinQuantity { get; set; }

			public static EdtLink[] Data = new[]
			{
				new EdtLink { InId = 2, InMinQuantity = 3, InMaxQuantity = 4 }
			};
		}

		[Test]
		public void Issue1815([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var stLinks  = db.CreateLocalTable(StLink.Data))
			using (var edtLinks = db.CreateLocalTable(EdtLink.Data))
			{
				var query1 = from l in stLinks
							 from e in edtLinks.LeftJoin(j => l.InId == j.InId)
							 select new
							 {
								 LinkId = l.InId,
								 MinQuantity = e == null ? l.InMinQuantity : e.InMinQuantity,
								 MaxQuantity = e == null ? l.InMaxQuantity : e.InMaxQuantity
							 };

				var query2 = from q in query1
							 select new
							 {
								 q.LinkId,
								 q.MinQuantity,
								 q.MaxQuantity
							 };

				var r = query2.SingleOrDefault(x => x.LinkId == 1);
				Assert.IsNotNull(r);
				Assert.AreEqual(1, r.MinQuantity);
				Assert.AreEqual(2, r.MaxQuantity);

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2);
				Assert.IsNotNull(r2);
				Assert.AreEqual(3, r2.MinQuantity);
				Assert.AreEqual(4, r2.MaxQuantity);
			}
		}

		[Test]
		public void Issue1815WithServerEvaluation1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var stLinks = db.CreateLocalTable(StLink.Data))
			using (var edtLinks = db.CreateLocalTable(EdtLink.Data))
			{
				var query1 = from l in stLinks
							 from e in edtLinks.LeftJoin(j => l.InId == j.InId)
							 select new
							 {
								 LinkId = l.InId,
								 MinQuantity = Sql.AsSql(e == null ? l.InMinQuantity : e.InMinQuantity),
								 MaxQuantity = Sql.AsSql(e == null ? l.InMaxQuantity : e.InMaxQuantity)
							 };

				var query2 = from q in query1
							 select new
							 {
								 q.LinkId,
								 q.MinQuantity,
								 q.MaxQuantity
							 };

				var r = query2.SingleOrDefault(x => x.LinkId == 1);
				Assert.IsNotNull(r);
				Assert.AreEqual(1, r.MinQuantity);
				Assert.AreEqual(2, r.MaxQuantity);

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2);
				Assert.IsNotNull(r2);
				Assert.AreEqual(3, r2.MinQuantity);
				Assert.AreEqual(4, r2.MaxQuantity);
			}
		}

		[Test]
		public void Issue1815WithServerEvaluation2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var stLinks = db.CreateLocalTable(StLink.Data))
			using (var edtLinks = db.CreateLocalTable(EdtLink.Data))
			{
				var query1 = from l in stLinks
							 from e in edtLinks.LeftJoin(j => l.InId == j.InId)
							 select new
							 {
								 LinkId = l.InId,
								 MinQuantity = e == null ? l.InMinQuantity : e.InMinQuantity,
								 MaxQuantity = e == null ? l.InMaxQuantity : e.InMaxQuantity
							 };

				var query2 = from q in query1
							 select new
							 {
								 q.LinkId,
								 MinQuantity = Sql.AsSql(q.MinQuantity),
								 MaxQuantity = Sql.AsSql(q.MaxQuantity)
							 };

				var r = query2.SingleOrDefault(x => x.LinkId == 1);
				Assert.IsNotNull(r);
				Assert.AreEqual(1, r.MinQuantity);
				Assert.AreEqual(2, r.MaxQuantity);

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2);
				Assert.IsNotNull(r2);
				Assert.AreEqual(3, r2.MinQuantity);
				Assert.AreEqual(4, r2.MaxQuantity);
			}
		}

		[Table("stVersions")]
		public class StVersion
		{
			[Column("inId"), PrimaryKey] public int InId { get; set; }
			[Column("inIdMain")]         public int InIdMain { get; set; }

			[Association(ThisKey = "InIdMain", OtherKey = "InId", CanBeNull = false, Relationship = Relationship.ManyToOne)]
			public StMain Main { get; set; }

			public static StVersion[] Data = new StVersion[]
			{
			};
		}

		[Table("rlStatesTypesAndUserGroups")]
		public class RlStatesTypesAndUserGroup
		{
			[Column("inIdState"), PrimaryKey(1)] public int InIdState { get; set; }
			[Column("inIdType"),  PrimaryKey(2)] public int InIdType { get; set; }

			public static RlStatesTypesAndUserGroup[] Data = new RlStatesTypesAndUserGroup[]
			{
			};
		}

		[Table("stMain")]
		public class StMain
		{
			[Column("inId"), PrimaryKey]  public int InId { get; set; }
			[Column("inIdType")]          public int InIdType { get; set; }

			public static StMain[] Data = new StMain[]
			{
			};
		}

		[Test]
		public void Issue1816v1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var stVersion = db.CreateLocalTable(StVersion.Data))
			using (var rlStatesTypesAndUserGroup = db.CreateLocalTable(RlStatesTypesAndUserGroup.Data))
			using (var stMain = db.CreateLocalTable(StMain.Data))
			{
				var q = from v in stVersion
						from t in rlStatesTypesAndUserGroup.Where(r => r.InIdType == v.Main.InIdType).DefaultIfEmpty()
						select new
						{
							v.InId,
							t.InIdState
						};
			}
		}

		[Test]
		public void Issue1816v2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var stVersion = db.CreateLocalTable(StVersion.Data))
			using (var rlStatesTypesAndUserGroup = db.CreateLocalTable(RlStatesTypesAndUserGroup.Data))
			using (var stMain = db.CreateLocalTable(StMain.Data))
			{
				var q = from v in stVersion
						from t in rlStatesTypesAndUserGroup.Where(r => r.InIdType == v.Main.InIdType).DefaultIfEmpty()
						select new
						{
							v.InId,
							t.InIdState,
							v.Main.InIdType
						};
			}
		}
	}
}
