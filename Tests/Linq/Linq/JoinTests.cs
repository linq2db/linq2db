using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<TResult> SqlJoinInternal<TOuter, TInner, TResult>(
			this IEnumerable<TOuter>        outer,
			IEnumerable<TInner>             inner,
			SqlJoinType                     joinType,
			Func<TOuter, TInner, bool>      predicate,
			Func<TOuter?, TInner?, TResult> resultSelector)
			where TOuter : class
			where TInner : class
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
						secondItems.Where(s => predicate(f, s)).DefaultIfEmpty().Select(s => new {First = (TOuter?)f, Second = (TInner?)s }));

					var secondResult = secondItems.Where(s => !firstItems.Any(f => predicate(f, s)))
						.Select(s => new {First = default(TOuter), Second = (TInner?)s });

					var res = firstResult.Concat(secondResult).Select(r => resultSelector(r.First!, r.Second));
					return res;
				default:
					throw new ArgumentOutOfRangeException(nameof(joinType), joinType, null);
			}
		}

		public static IEnumerable<TResult> SqlJoinInternal<TOuter, TInner, TKey, TResult>(
			this IEnumerable<TOuter>        outer,
			IEnumerable<TInner>             inner,
			SqlJoinType                     joinType,
			Func<TOuter, TKey>              outerKeySelector,
			Func<TInner, TKey>              innerKeySelector,
			Func<TOuter?, TInner?, TResult> resultSelector)
			where TOuter: class
			where TInner: class
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

						res.AddRange(pair1.Select(r => resultSelector(r, default!)));
					}

					foreach (var pair2 in keys2)
					{
						if (keys1.Contains(pair2.Key))
						{
							continue;
						}

						res.AddRange(pair2.Select(r => resultSelector(default!, r)));
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
			using var db = GetDataContext(context);

			TestJohn(
				from p1 in db.Person
					join p2 in from p3 in db.Person select new { ID = p3.ID + 1, p3.FirstName } on p1.ID equals p2.ID - 1
				where p1.ID == 1
				select new Person { ID = p1.ID, FirstName = p2.FirstName });

			Assert.That(GetCurrentBaselines(), Does.Not.Contain("JOIN"));
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
		public void InnerJoin9([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from g in GrandChild
					join p in Parent4 on g.Child!.ParentID equals p.ParentID
					where g.ParentID < 10 && p.Value1 == TypeValue.Value3
					select g,
					from g in db.GrandChild
					join p in db.Parent4 on g.Child!.ParentID equals p.ParentID
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

				Assert.That(list, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0].p.ParentID, Is.EqualTo(1));
					Assert.That(list[0].lj.Count(), Is.EqualTo(1));
				}

				var ch = list[0].lj.ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(ch[0].ParentID, Is.EqualTo(1));
					Assert.That(ch[0].ChildID, Is.EqualTo(11));
				}
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

				Assert.That(list2, Has.Count.EqualTo(list1.Count));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list2[0].p.ParentID, Is.EqualTo(list1[0].p.ParentID));
					Assert.That(list2[0].lj1.lj1.Count(), Is.EqualTo(list1[0].lj1.lj1.Count()));
				}
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

				Assert.That(list2, Has.Count.EqualTo(list1.Count));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list2[0].p.ParentID, Is.EqualTo(list1[0].p.ParentID));
					Assert.That(list2[0].lj1.Count(), Is.EqualTo(list1[0].lj1.Count()));
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expectedQuery = from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID into lj1
					orderby p.ParentID
					where p.ParentID >= 1
					select lj1.OrderBy(c => c.ChildID).FirstOrDefault();

				var actualQuery = from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					orderby p.ParentID
					where p.ParentID >= 1
					select lj1.OrderBy(c => c.ChildID).FirstOrDefault();

				var expected = expectedQuery.ToArray();
				var actual   = actualQuery.ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin51([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result =
				(
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1, p2 = lj1.OrderByDescending(e => e.ChildID).First() }
				).ToList();

				var expected =
				(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
					where p.ParentID == 1
					select new { p1 = lj1, p2 = lj1.OrderByDescending(e => e.ChildID).First() }
				).ToList();

				Assert.That(result, Has.Count.EqualTo(expected.Count));
				AreEqual(expected[0].p1, result[0].p1);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin52([DataSources(TestProvName.AllClickHouse)] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin53([DataSources(TestProvName.AllClickHouse)] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin54([DataSources(TestProvName.AllClickHouse)] string context)
		{
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

				Assert.That(list2, Has.Count.EqualTo(list1.Count));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list2[0].p.ParentID, Is.EqualTo(list1[0].p.ParentID));
					Assert.That(list2[0].lj.Count(), Is.EqualTo(list1[0].lj.Count()));
				}

				var ch2 = list2[0].lj.OrderBy(_ => _.ChildID).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(ch2[0].ParentID, Is.EqualTo(ch1[0].ParentID));
					Assert.That(ch2[0].ChildID, Is.EqualTo(ch1[0].ChildID));
				}
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
				var ch1   = list1[0].j.OrderBy(_ => _.ChildID).ThenBy(_ => _.ParentID).ToList();

				var q2 =
					from p in db.Parent
						join c in db.Child on new { id = p.ParentID } equals new { id = c.ParentID - n } into j
					where p.ParentID == 1
					select new { p, j };

				var list2 = q2.ToList();

				Assert.That(list2, Has.Count.EqualTo(list1.Count));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list2[0].p.ParentID, Is.EqualTo(list1[0].p.ParentID));
					Assert.That(list2[0].j.Count(), Is.EqualTo(list1[0].j.Count()));
				}

				var ch2 = list2[0].j.OrderBy(_ => _.ChildID).ThenBy(_ => _.ParentID).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(ch2[0].ParentID, Is.EqualTo(ch1[0].ParentID));
					Assert.That(ch2[0].ChildID, Is.EqualTo(ch1[0].ChildID));
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoin8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID into g
					select new { Child = g.OrderBy(c => c.ChildID).FirstOrDefault() }
					,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into g
					select new { Child = g.OrderBy(c => c.ChildID).FirstOrDefault() });
		}

		// Access has strange order strategy
		// Informix move constant column value from left-joined subquery to top level even for null records
		[Test]
		public void GroupJoin9([DataSources(TestProvName.AllAccess, TestProvName.AllInformix)] string context)
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
						)
					);
		}

		[Test]
		public void GroupJoinAny1([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void GroupJoinAny2([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void GroupJoinAny3([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void GroupJoinAny4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					select new { p.ParentID, n = (from c in    Child where p.ParentID == c.ParentID select c).Any() },
					from p in db.Parent
					select new { p.ParentID, n = (from c in db.Child where p.ParentID == c.ParentID select c).Any() });
		}

		[Test]
		public void GroupJoinAny5([DataSources(TestProvName.AllClickHouse)] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void LeftJoin4([DataSources(TestProvName.AllClickHouse)] string context)
		{
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

		[Test]
		public void LeftJoinRemoval([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = 
				from p in db.Parent
				from ch in db.Child.Where(ch => p.ParentID == ch.ParentID).DefaultIfEmpty()
				from ch1 in db.Child.Where(ch1 => ch.ChildID == ch1.ChildID)
				select ch1;

			var ts = query.GetTableSource();

			ts.Joins.ShouldAllBe(j => j.JoinType == JoinType.Inner);

			AssertQuery(query);
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

				Assert.That(CountedChild.Count, Is.Zero);
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
		public void ReferenceJoin1([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = from c in db.Child
				join g in db.GrandChild on c equals g.Child
				select new { c.ParentID, g.GrandChildID };

			var xx = query.ToList();

			AreEqual(
				from c in    Child join g in    GrandChild on c equals g.Child select new { c.ParentID, g.GrandChildID },
				from c in db.Child join g in db.GrandChild on c equals g.Child select new { c.ParentID, g.GrandChildID });
		}

		[Test]
		public void ReferenceJoin2([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from g in    GrandChild
					join c in    Child on g.Child equals c
				select new { c.ParentID, g.GrandChildID },
				from g in db.GrandChild
					join c in db.Child on g.Child equals c
				select new { c.ParentID, g.GrandChildID });
		}

		[Test]
		public void JoinByAnonymousTest([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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
		public void FourTableJoin([DataSources(TestProvName.AllClickHouse)] string context)
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

		// MySQL: 61 joined tables limit
		// SQLite: 64 joined tables limit
		// ASE: The "default data cache (id: 0)" is configured with 410 buffers.  The current query plan requires 2448 buffers.  Please reconfigure the data cache and try the command again.
		// Access: Query is too complex (lol)
		// DB2: Processing was cancelled due to an interrupt.
		// SQLCE: slow (~2-3 min)
		[Test]
		public void StackOverflow([DataSources(TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllDB2, TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllSybase)] string context)
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
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public void ApplyJoin([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.Child
					from p in new Model.Functions(db).GetParentByID(ch.Parent!.ParentID)
					select p;

				var _ = q.ToList();
			}
		}

		// MySQL doesn't support user-defined table functions
		// system-defined JSON_TABLE function could be used with LATERAL, but it is not an easy task to define it...
		[ActiveIssue("Implement JSON_TABLE-like functions support")]
		[Test]
		public void ApplyJoin_MySql([IncludeDataSources(TestProvName.AllMySqlWithApply)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.Child
					from p in JsonTable()
					select p;

				var _ = q.ToList();
			}
		}

		[Sql.TableExpression("JSON_TABLE('[ {\"ParentID\": 1}, {\"Value1\": 2} ]', '$[*]' COLUMNS( ParentID INT PATH '$.ParentID', Value1 INT PATH '$.Value1')")]
		private static ITable<Parent> JsonTable() => throw new NotImplementedException();

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
		public void NullJoin1([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void NullJoin2([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void NullWhereJoin([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void JoinSubQueryCount([DataSources(TestProvName.AllClickHouse)]
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
		public void JoinSubQuerySum([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SqlNullWhereJoin([AllJoinsSource(TestProvName.AllClickHouse)] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.SqlJoinInternal(Parent, joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual =
					from p1 in db.Parent
					from p2 in db.Parent.Join(joinType, p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2;

				AreEqual(expected.ToList().OrderBy(r => r!.ParentID).ThenBy(r => r!.Value1),
					actual.ToList().OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		[Test]
		public void SqlNullWhereSubqueryJoin([AllJoinsSource(TestProvName.AllClickHouse)] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.Take(10).SqlJoinInternal(Parent.Take(10), joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual =
					from p1 in db.Parent.Take(10)
					from p2 in db.Parent.Take(10).Join(joinType, p => p1.ParentID == p.ParentID && p1.Value1 == p.Value1)
					select p2;

				AreEqual(expected.ToList().OrderBy(r => r!.ParentID).ThenBy(r => r!.Value1),
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
		public void SqlLinqNullWhereJoin([AllJoinsSource(TestProvName.AllClickHouse)] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.SqlJoinInternal(Parent, joinType, (p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual = db.Parent.Join(db.Parent, joinType, (p1, p2) => p1.ParentID == p2.ParentID && p1.Value1 == p2.Value1,
					(p1, p2) => p2)
					.ToList();

				AreEqual(expected.ToList().OrderBy(r => r!.ParentID).ThenBy(r => r!.Value1),
					actual.OrderBy(r => r.ParentID).ThenBy(r => r.Value1));
			}
		}

		[Test]
		public void SqlLinqNullWhereSubqueryJoin([AllJoinsSource(TestProvName.AllClickHouse)] string context, [Values] SqlJoinType joinType)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.Take(10).SqlJoinInternal(Parent.Take(10), joinType,
					(p1, p) => p1.ParentID == p.ParentID && p1.Value1 == p.Value1, (p1, p2) => p2);

				var actual = db.Parent.Take(10).Join(db.Parent.Take(10), joinType,
					(p1, p2) => p1.ParentID == p2.ParentID && p1.Value1 == p2.Value1, (p1, p2) => p2);

				AreEqual(expected.ToList().OrderBy(r => r!.ParentID).ThenBy(r => r!.Value1),
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
			TestProvName.AllAccess,
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

				Assert.That(areEqual, Is.True);
			}
		}

		[Test]
		public void SqlFullJoinWithCount2([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(areEqual, Is.False);
			}
		}

		[Test]
		public void SqlFullJoinWithCount3([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(areEqual, Is.False);
			}
		}

		[Test]
		public void SqlFullJoinWithCount4([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(areEqual, Is.False);
			}
		}

		[Test]
		public void SqlFullJoinWithCount5([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(areEqual, Is.True);
			}
		}

		[Test]
		public void SqlFullJoinWithBothFilters([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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
			TestProvName.AllAccess,
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
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
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
			TestProvName.AllClickHouse,
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
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
			TestProvName.AllClickHouse,
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
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
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
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
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context)))
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
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context)))
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
		public void SqlRightJoinWithInnerJoinOnLeftWithoutConditions([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SqlRightJoinWithInnerJoinOnLeftWithoutAllConditions([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SqlRightJoinWithInnerJoinOnRightWithConditions([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SqlRightJoinWithInnerJoinOnRightWithoutConditions([DataSources(TestProvName.AllSQLite, TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context)))
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
		public void SqlRightJoinWithInnerJoinOnRightWithoutAllConditions([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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
		public void JoinBuildersConflicts([IncludeDataSources(TestProvName.AllSQLiteClassic, TestProvName.AllClickHouse)] string context)
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

			[Association(ThisKey = "Id", OtherKey = "FactId", CanBeNull = true)]
			public IEnumerable<Tag> TagFactIdIds { get; set; } = null!;

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
			[Column]          public int    FactId { get; set; }
			[Column, NotNull] public string Name   { get; set; } = null!;

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

				var results = t.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = t.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = t.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = t.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = q.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = q.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				var results = t.OrderBy(_ => _.fact.Id).ToArray();

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
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

				Assert.That(results, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].fact.Id, Is.EqualTo(4));
					Assert.That(results[0].leftTag.Name, Is.EqualTo("Tag4"));
					Assert.That(results[1].fact.Id, Is.EqualTo(5));
					Assert.That(results[1].leftTag, Is.Null);
				}
			}
		}

		[Test]
		public void FullJoinWithRecordSelection1([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(results, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void FullJoinWithRecordSelection2([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(results, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void FullJoinWithRecordSelection3([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(results, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void FullJoinWithRecordSelection4([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
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

				Assert.That(results, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 5 && r.leftTag == null), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact == null && r.leftTag != null && r.leftTag.Name == "Tag6"), Is.EqualTo(1));
					Assert.That(results.Count(r => r.fact != null && r.fact.Id == 4 && r.leftTag != null && r.leftTag.Name == "Tag4"), Is.EqualTo(1));
				}
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

				var r = query2.SingleOrDefault(x => x.LinkId == 1)!;
				Assert.That(r, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.MinQuantity, Is.EqualTo(1));
					Assert.That(r.MaxQuantity, Is.EqualTo(2));
				}

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2)!;
				Assert.That(r2, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r2.MinQuantity, Is.EqualTo(3));
					Assert.That(r2.MaxQuantity, Is.EqualTo(4));
				}
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

				var r = query2.SingleOrDefault(x => x.LinkId == 1)!;
				Assert.That(r, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.MinQuantity, Is.EqualTo(1));
					Assert.That(r.MaxQuantity, Is.EqualTo(2));
				}

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2)!;
				Assert.That(r2, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r2.MinQuantity, Is.EqualTo(3));
					Assert.That(r2.MaxQuantity, Is.EqualTo(4));
				}
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

				var r = query2.SingleOrDefault(x => x.LinkId == 1)!;
				Assert.That(r, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.MinQuantity, Is.EqualTo(1));
					Assert.That(r.MaxQuantity, Is.EqualTo(2));
				}

				var r2 = query2.SingleOrDefault(x => x.LinkId == 2)!;
				Assert.That(r2, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r2.MinQuantity, Is.EqualTo(3));
					Assert.That(r2.MaxQuantity, Is.EqualTo(4));
				}
			}
		}

		[Table("stVersions")]
		public class StVersion
		{
			[Column("inId"), PrimaryKey] public int InId { get; set; }
			[Column("inIdMain")]         public int InIdMain { get; set; }

			[Association(ThisKey = "InIdMain", OtherKey = "InId", CanBeNull = false)]
			public StMain Main { get; set; } = null!;

			public static StVersion[] Data = [];
		}

		[Table("rlStatesTypesAndUserGroups")]
		public class RlStatesTypesAndUserGroup
		{
			[Column("inIdState"), PrimaryKey(1)] public int InIdState { get; set; }
			[Column("inIdType"),  PrimaryKey(2)] public int InIdType { get; set; }

			public static RlStatesTypesAndUserGroup[] Data = [];
		}

		[Table("stMain")]
		public class StMain
		{
			[Column("inId"), PrimaryKey]  public int InId { get; set; }
			[Column("inIdType")]          public int InIdType { get; set; }

			public static StMain[] Data = [];
		}

		[Test]
		public void Issue1816v1([DataSources] string context)
		{
			using (var db                        = GetDataContext(context))
			using (var stVersion                 = db.CreateLocalTable(StVersion.Data))
			using (var rlStatesTypesAndUserGroup = db.CreateLocalTable(RlStatesTypesAndUserGroup.Data))
			using (var stMain                    = db.CreateLocalTable(StMain.Data))
			{
				var q = from v in stVersion
						from t in rlStatesTypesAndUserGroup.Where(r => r.InIdType == v.Main.InIdType).DefaultIfEmpty()
						select new
						{
							v.InId,
							t.InIdState
						};

				q.ToList();
			}
		}

		[Test]
		public void Issue1816v2([DataSources] string context)
		{
			using (var db                        = GetDataContext(context))
			using (var stVersion                 = db.CreateLocalTable(StVersion.Data))
			using (var rlStatesTypesAndUserGroup = db.CreateLocalTable(RlStatesTypesAndUserGroup.Data))
			using (var stMain                    = db.CreateLocalTable(StMain.Data))
			{
				var q = from v in stVersion
						from t in rlStatesTypesAndUserGroup.Where(r => r.InIdType == v.Main.InIdType).DefaultIfEmpty()
						select new
						{
							v.InId,
							t.InIdState,
							v.Main.InIdType
						};

				q.ToList();
			}
		}

		#region issue 1455
		public class Alert
		{
			[Column(CanBeNull = false)]
			public string    AlertKey     { get; set; } = null!;
			[Column(CanBeNull = false)]
			public string    AlertCode    { get; set; } = null!;
			[Column(CanBeNull = false)]
			public DateTime CreationDate { get { return DateTime.Today; } }
		}
		public class AuditAlert : Alert
		{
			public DateTime? TransactionDate { get; set; }
		}
		public class Trade
		{
			public int     DealId       { get; set; }
			public int     ParcelId     { get; set; }
			public string? CounterParty { get; set; }
		}
		public class Nomin
		{
			public int     CargoId              { get; set; }
			public int     DeliveryId           { get; set; }
			public string? DeliveryCounterParty { get; set; }
		}
		public class Flat
		{
			public string?   AlertKey             { get; set; }
			public string?   AlertCode            { get; set; }
			public int?      CargoId              { get; set; }
			public int?      DeliveryId           { get; set; }
			public string?   DeliveryCounterParty { get; set; }
			public int?      DealId               { get; set; }
			public int?      ParcelId             { get; set; }
			public string?   CounterParty         { get; set; }
			public DateTime? TransactionDate      { get; set; }
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Issue1455Test1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false)))
			using (var queryLastUpd = db.CreateLocalTable<Alert>())
			using (db.CreateLocalTable<AuditAlert>())
			using (db.CreateLocalTable<Trade>())
			using (db.CreateLocalTable<Nomin>())
			using (db.CreateLocalTable<Flat>())
			{
				var queryAudit = from al in db.GetTable<Alert>()
								 from au in db.GetTable<AuditAlert>()
									.Where(au1 => au1.AlertKey == al.AlertKey && au1.AlertCode == au1.AlertCode).DefaultIfEmpty()
								 group au.TransactionDate by al into al_group
								 select new { alert = al_group.Key, LastUpdate = al_group.Max() ?? al_group.Key.CreationDate };

				var ungrouped =
					from al in queryAudit
					from trade in db.GetTable<Trade>()
						.Where(trade1 => al.alert.AlertKey == trade1.DealId.ToString()).DefaultIfEmpty()
					from nomin in db.GetTable<Nomin>()
						.Where(nomin1 => al.alert.AlertKey == nomin1.CargoId.ToString()).DefaultIfEmpty()
					select new { al, nomin, trade };

				string cpty = "C";

				if (!string.IsNullOrWhiteSpace(cpty))
					ungrouped = ungrouped
					.Where(u =>
						 u.nomin.DeliveryCounterParty!.Contains(cpty)
						 ||
						 u.trade.CounterParty!.Contains(cpty)
						 ||
						 u.al.alert.AlertCode!.Contains(cpty)
						 );

				var query =
					from u in ungrouped
					group new { u.nomin, u.trade, u.al.LastUpdate } by u.al.alert into al_group
					select new { alert = al_group.Key, first = al_group.FirstOrDefault() };
				var extract = query.ToArray();

				extract
					.Select(sql => new Flat()
					{
						AlertCode            = sql.alert.AlertCode,
						AlertKey             = sql.alert.AlertKey,
						TransactionDate      = sql.first?.LastUpdate,
						CargoId              = sql.first?.nomin?.CargoId,
						DeliveryId           = sql.first?.nomin?.DeliveryId,
						DeliveryCounterParty = sql.first?.nomin?.DeliveryCounterParty,
						DealId               = sql.first?.trade?.DealId,
						ParcelId             = sql.first?.trade?.ParcelId,
						CounterParty         = sql.first?.trade?.CounterParty
					}).ToArray();
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Issue1455Test2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			// UseGuardGrouping: For Sybase, which do not support Window functions
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false)))
			using (db.CreateLocalTable<Alert>())
			using (db.CreateLocalTable<AuditAlert>())
			using (db.CreateLocalTable<Trade>())
			using (db.CreateLocalTable<Nomin>())
			using (db.CreateLocalTable<Flat>())
			{
				var queryAudit = from al in db.GetTable<Alert>()
								 from au in db.GetTable<AuditAlert>()
									.Where(au1 => au1.AlertKey == al.AlertKey && au1.AlertCode == au1.AlertCode).DefaultIfEmpty()
								 group au.TransactionDate by al into al_group
								 select new { alert = al_group.Key, LastUpdate = al_group.Max() ?? al_group.Key.CreationDate };

				var ungrouped =
					from al in queryAudit
					from trade in db.GetTable<Trade>()
						.Where(trade1 => al.alert.AlertKey == trade1.DealId.ToString()).DefaultIfEmpty()
					from nomin in db.GetTable<Nomin>()
						.Where(nomin1 => al.alert.AlertKey == nomin1.CargoId.ToString()).DefaultIfEmpty()
					select new { al, nomin, trade };

				string cpty = "C";

				if (!string.IsNullOrWhiteSpace(cpty))
					ungrouped = ungrouped
					.Where(u =>
						 Sql.Like(u.nomin.DeliveryCounterParty, $"%{cpty}%")
						 ||
						 Sql.Like(u.trade.CounterParty, $"%{cpty}%")
						 ||
						 Sql.Like(u.al.alert.AlertCode, $"%{cpty}%")
						 );

				var query =
					from u in ungrouped
					group new { u.nomin, u.trade, u.al.LastUpdate } by u.al.alert into al_group
					select new { alert = al_group.Key, first = al_group.FirstOrDefault() };
				var extract = query.ToArray();

				extract
					.Select(sql => new Flat()
					{
						AlertCode            = sql.alert.AlertCode,
						AlertKey             = sql.alert.AlertKey,
						TransactionDate      = sql.first?.LastUpdate,
						CargoId              = sql.first?.nomin?.CargoId,
						DeliveryId           = sql.first?.nomin?.DeliveryId,
						DeliveryCounterParty = sql.first?.nomin?.DeliveryCounterParty,
						DealId               = sql.first?.trade?.DealId,
						ParcelId             = sql.first?.trade?.ParcelId,
						CounterParty         = sql.first?.trade?.CounterParty
					}).ToArray();
			}
		}
		#endregion

		#region issue 2421
		[Table]
		public class UserDTO
		{
			[PrimaryKey, Identity] public int     UserId { get; set; }
			[Column,     Nullable] public string? UserName { get; set; }
		}

		[Table]
		public class UserPositionDTO
		{
			[PrimaryKey, Identity] public int UserPositionId { get; set; }
			[Column,     NotNull ] public int UserId         { get; set; }
			[Column,     NotNull ] public int PositionId     { get; set; }

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(UserDTO.UserId), CanBeNull = false)]
			public UserDTO User { get; set; } = null!;

			[Association(ThisKey = nameof(PositionId), OtherKey = nameof(PositionDTO.PositionId), CanBeNull = false)]
			public PositionDTO Position { get; set; } = null!;
		}

		[Table("UPS")]
		public class UserPositionSectorDTO
		{
			[PrimaryKey, Identity] public int UserPositionSectorId { get; set; }
			[Column,     NotNull ] public int UserPositionId       { get; set; }
			[Column,     NotNull ] public int SectorId             { get; set; }

			[Association(ThisKey = nameof(UserPositionId), OtherKey = nameof(UserPositionDTO.UserPositionId), CanBeNull = false)]
			public UserPositionDTO UserPosition { get; set; } = null!;

			[Association(ThisKey = nameof(SectorId), OtherKey = nameof(SectorDTO.SectorId), CanBeNull = false)]
			public SectorDTO Sector { get; set; } = null!;
		}

		[Table]
		public class PositionDTO
		{
			[PrimaryKey, Identity] public int    PositionId   { get; set; }
			[Column,     NotNull ] public string PositionName { get; set; } = null!;
		}

		[Table]
		public class SectorDTO
		{
			[PrimaryKey, Identity] public int    SectorId { get; set; }
			[Column,     NotNull ] public string SectorName { get; set; } = null!;

			[Association(ThisKey = nameof(SectorId), OtherKey = nameof(UserPositionSectorDTO.SectorId))]
			public List<UserPositionSectorDTO> UserPositionSectors { get; set; } = null!;
		}

		// to sdanyliv: we generate same sql for sqlite and it works there, so fix should affect only access
		[Test]
		public void Issue2421([DataSources] string context)
		{
			using var db                  = GetDataContext(context);
			using var users               = db.CreateLocalTable<UserDTO>();
			using var userPositions       = db.CreateLocalTable<UserPositionDTO>();
			using var userPositionSectors = db.CreateLocalTable<UserPositionSectorDTO>();
			using var positions           = db.CreateLocalTable<PositionDTO>();
			using var sectors             = db.CreateLocalTable<SectorDTO>();

			var query = sectors
				.Select(x => new
				{
					SectorId = x.SectorId,
					UserId   = x.UserPositionSectors
						.Where(y => y.UserPosition.PositionId == 1)
						.Select(y => y.UserPosition.User.UserId)
				});

			var result = query.ToArray();
		}
		#endregion

		[ActiveIssue(1224, Configurations = new[]
		{
			TestProvName.AllAccess,
			TestProvName.AllMySql,
			TestProvName.AllSybase,
			ProviderName.SqlCe
		}, Details = "FULL OUTER JOIN support. Also check and enable other tests that do full join on fix")]
		[Test(Description = "Tests regression in v3.3 when for RightCount generated SQL started to use same field as for LeftCount")]
		// InformixDB2 disabled due to serious bug in provider: while query returns 3, data reader returns 0 here
		public void FullJoinCondition_Regression([DataSources(ProviderName.InformixDB2, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// query returns 0 if subqueries contain same values and number of non-matching records otherwise
				var count = LinqExtensions.FullJoin(
						db.Person.Select(p => p.ID).GroupBy(id => id).Select(g => new { g.Key, Count = g.Count() }),
						db.Patient.Select(p => p.PersonID).GroupBy(id => id).Select(g => new { g.Key, Count = g.Count() }),
						(q1, q2) => q1.Key == q2.Key && q1.Count == q2.Count,
						(q1, q2) => new { LeftCount = (int?)q1.Count, RightCount = (int?)q2.Count })
					.Where(q => q.LeftCount == null || q.RightCount == null)
					.Count();

				Assert.That(count, Is.Not.Zero);
			}
		}

		[Table]
		private class Issue4160Person
		{
			[Column] public string Code { get; set; } = default!;

			public static readonly Issue4160Person[] Data = new[]
			{
				new Issue4160Person() { Code = "SD" },
				new Issue4160Person() { Code = "SD" },
				new Issue4160Person() { Code = "SH" },
			};
		}

		[Table]
		private class Issue4160City
		{
			[Column] public string  Code { get; set; } = default!;
			[Column] public string? Name { get; set; }

			public static readonly Issue4160City[] Data = new[]
			{
				new Issue4160City() { Code = "SD", Name = "SYDNEY" },
				new Issue4160City() { Code = "SD", Name = "SUNDAY" },
				new Issue4160City() { Code = "SH", Name = "SYDHIP" }
			};
		}

		[ActiveIssue(Configuration = TestProvName.AllOracle12)]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Issue4160Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var persons = db.CreateLocalTable(Issue4160Person.Data);
			using var cities  = db.CreateLocalTable(Issue4160City.Data);

			var query = (
			 from pe in persons
			 select new
			 {
				 Value = (from cc in cities
						  where cc.Code == pe.Code
						  select cc.Name).FirstOrDefault()
			 }).Distinct();

			var data = query.ToList();

			Assert.That(data, Has.Count.EqualTo(2));
			// TODO: disable is_empty field generation for this query as it is not needed
			//Assert.That(query.GetSelectQuery().Select.Columns.Count, Is.EqualTo(1));
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllSybase, TestProvName.AllMySql57], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void Issue4160Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var persons = db.CreateLocalTable(Issue4160Person.Data);
			using var cities  = db.CreateLocalTable(Issue4160City.Data);

			var data = (
				from pe in persons
				from cc in cities.Where(cc => cc.Code == pe.Code).Take(1).DefaultIfEmpty()
				select new
				{
					Value = cc.Name
				}).Distinct().ToList();

			Assert.That(data, Has.Count.EqualTo(2));
		}

		class t_ws_submissions
		{
			public int submission_id;
		}

		class t_ws_policies
		{
			public int    submission_id;
			public string policy_nbr = null!;
		}

		class DoNotExecuteCommandInterceptor : CommandInterceptor
		{
			public static readonly IInterceptor Instance = new DoNotExecuteCommandInterceptor();

			class DoNotExecuteDataReader : DbDataReader
			{
				public override bool     GetBoolean (int ordinal) => default;
				public override byte     GetByte    (int ordinal) => default;
				public override char     GetChar    (int ordinal) => default;
				public override DateTime GetDateTime(int ordinal) => default;
				public override decimal  GetDecimal (int ordinal) => default;
				public override double   GetDouble  (int ordinal) => default;
				public override float    GetFloat   (int ordinal) => default;
				public override Guid     GetGuid    (int ordinal) => default;
				public override short    GetInt16   (int ordinal) => default;
				public override int      GetInt32   (int ordinal) => default;
				public override long     GetInt64   (int ordinal) => default;
				public override string   GetString  (int ordinal) => default!;
				public override object   GetValue   (int ordinal) => default!;

				public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => default;
				public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => default;

				public override Type   GetFieldType   (int      ordinal) => throw new NotImplementedException();
				public override string GetDataTypeName(int      ordinal) => throw new NotImplementedException();
				public override string GetName        (int      ordinal) => throw new NotImplementedException();
				public override int    GetOrdinal     (string   name)    => throw new NotImplementedException();
				public override int    GetValues      (object[] values)  => throw new NotImplementedException();
				public override bool   IsDBNull       (int      ordinal) => throw new NotImplementedException();

				public override object this[int    ordinal] => default!;
				public override object this[string name]    => default!;

				public override int  FieldCount      { get; }
				public override int  RecordsAffected { get; }
				public override bool HasRows         { get; }
				public override bool IsClosed        { get; }
				public override int  Depth           { get; }

				public override bool NextResult() => false;
				public override bool Read      () => false;

				public override IEnumerator GetEnumerator()
				{
					return Array.Empty<int>().GetEnumerator();
				}
			}

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				return new DoNotExecuteDataReader();
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2912")]
		public void Issue2912Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = (from employee in db.Parent
						  let user = employee.Children.FirstOrDefault()
						  select user != null ? user.ChildID : 0);
			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2912")]
		public void Issue2912Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = (from employee in db.Parent
						  join x in db.GrandChild on employee.ParentID equals x.ParentID into y
						  from names in y.DefaultIfEmpty()
						  join x2 in db.Parent on employee.ParentID equals x2.ParentID into y2
						  from user in y2.DefaultIfEmpty()
						  select user.ParentID);
			query.ToArray();
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2912")]
		public void Issue2912Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = (from employee in db.Parent
						 join x in db.GrandChild on employee.ParentID equals x.ParentID into y
						  from names in y.DefaultIfEmpty()
						  let user = employee.Children.FirstOrDefault()
						  select user != null ? user.ChildID : 0);
			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3311")]
		public void Issue3311Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from x in db.GetTable<Person>()
				from apply in db.SelectQuery(() => Sql.AsSql(x.ID + 1))
				select apply;

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3311")]
		public void Issue3311Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from x in db.GetTable<Person>()
				from apply in db.SelectQuery(() => Sql.AsSql(x.ID + 1)).DefaultIfEmpty()
				select apply;

			query.ToArray();
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSQLite, TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllFirebirdLess4, TestProvName.AllInformix, TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllOracle11, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllClickHouse], ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3311")]
		public void Issue3311Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			(from u in db.Person
			 from x in (from r in db.SelectQuery(() => 1)
						from l in db.Patient.LeftJoin(l => l.PersonID == u.ID)
						select l.PersonID).AsSubQuery()
			 select new { u.ID, x, }
			).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3560")]
		public void Issue3560Test1([DataSources(false, TestProvName.AllClickHouse)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataConnection(context, o => o.UseCompareNulls(compareNulls));

			var query = (from p1 in db.Person
						 join p2 in db.Person on new { p1.MiddleName } equals new { p2.MiddleName }
						 select new { p1, p2 });

			query.ToArray();

			var isNullCount = db.LastQuery!.Split(["IS NULL"], StringSplitOptions.None).Length - 1;
			Assert.That(isNullCount, Is.EqualTo(compareNulls == CompareNulls.LikeSql ? 0 : 2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3560")]
		public void Issue3560Test2([DataSources(false, TestProvName.AllClickHouse, TestProvName.AllMySql)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataConnection(context, o => o.UseCompareNulls(compareNulls));

			// null + str => str
			var query = (from p1 in db.Person
						 join p2 in db.Person on p1.MiddleName equals p2.MiddleName + " Jr."
						 select new { p1, p2 });
			query.ToArray();

			var isNullCount = db.LastQuery!.Split(["IS NULL"], StringSplitOptions.None).Length - 1;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(isNullCount, Is.Zero);
				Assert.That(db.LastQuery, Does.Contain(" Jr."));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3560")]
		public void Issue3560Test3([DataSources(false, TestProvName.AllClickHouse)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataConnection(context, o => o.UseCompareNulls(compareNulls));

			var query = (from p1 in db.Parent
						 join p2 in db.Parent on new { p1.Value1 } equals new { p2.Value1 }
						 select new { p1, p2 });
			query.ToArray();

			var isNullCount = db.LastQuery!.Split(["IS NULL"], StringSplitOptions.None).Length - 1;
			Assert.That(isNullCount, Is.EqualTo(compareNulls == CompareNulls.LikeSql ? 0 : 2));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3560")]
		public void Issue3560Test4([DataSources(false, TestProvName.AllClickHouse)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataConnection(context, o => o.UseCompareNulls(compareNulls));

			// null + int => null
			var query = (from p1 in db.Parent
						 join p2 in db.Parent on p1.Value1 equals p2.Value1 + 321
						 select new { p1, p2 });
			query.ToArray();

			var isNullCount = db.LastQuery!.Split(["IS NULL"], StringSplitOptions.None).Length - 1;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(isNullCount, Is.EqualTo(compareNulls == CompareNulls.LikeSql ? 0 : 2));
				Assert.That(db.LastQuery, Does.Contain(321));
			}
		}

		#region Issue 4714
		public class YearMap
		{
			public DateTime StartDate { get; set; }
			public DateTime EndDate { get; set; }
			public int Year { get; set; }
		}

		public class Sample
		{
			public int SampleId { get; set; }
		}

		public class Source
		{
			public int Key1 { get; set; }
			public int Key2 { get; set; }
		}

		public class SelectionMap
		{
			public int Key1 { get; set; }
			public int Key2 { get; set; }
			public decimal SelectionProperty { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4714")]
		public void Issue4714Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var sampleTable = db.CreateLocalTable<Sample>();
			using var sourceTable = db.CreateLocalTable<Source>();
			using var selectionMap = db.CreateLocalTable<SelectionMap>();
			using var yearTable = db.CreateLocalTable<YearMap>();

			var sampleIds = sampleTable.Select(entity => entity.SampleId);

			var sourcesBySelection = sourceTable
				.InnerJoin(selectionMap,
					(source, map) => source.Key1 == map.Key1 && source.Key2 == map.Key2,
					(source, map) => map.SelectionProperty)
				.CrossJoin(sampleIds, (selection, id) => new { Selection = selection, Id = id });

			var failure = yearTable
				.SelectMany(
					year => sourcesBySelection,
					(year, source) => new { source.Id, year.Year, year.StartDate, year.EndDate })
				.ToList();
		}
		#endregion

		[Test]
		public void NullableCoalesceJoinTest([DataSources(false, [TestProvName.AllAccess, TestProvName.AllClickHouse])] string context)
		{
			using var db   = GetDataContext(context);

			var data1 = new []
			{
				new { ID = 1, Value = (string?)"Value1" },
				new { ID = 2, Value = (string?)null     },
			};

			var data2 = new []
			{
				new { ID = 1, Value = "Value1" },
				new { ID = 3, Value = "Value2" },
			};

			var data3 = new []
			{
				new { ID = 1, Value = (string?)"Value1" },
				new { ID = 2, Value = (string?)null     },
			};

			using var temp1 = db.CreateTempTable("tmptbl1", data1, ed => ed.Property(p => p.Value).IsNullable());
			using var temp2 = db.CreateTempTable("tmptbl2", data2, ed => ed.Property(p => p.Value).IsNotNull());
			using var temp3 = db.CreateTempTable("tmptbl3", data3, ed => ed.Property(p => p.Value).IsNullable());

			var query =
				from t2 in temp1
				join t3 in temp2 on t2.ID equals t3.ID into gt3
				from t3 in gt3.DefaultIfEmpty()
				let Value = t3.Value ?? t2.Value
				join t4 in temp3 on new { Value } equals new { t4.Value } into gt5
				from t4 in gt5.DefaultIfEmpty()
				select t4;

			AssertQuery(query);

			if (db is DataConnection { DataProvider: FirebirdDataProvider})
				FirebirdTools.ClearAllPools();
		}
	}
}
