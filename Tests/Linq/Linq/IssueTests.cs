using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/38
		//
		[Test, DataContextSource(false)]
		public void Issue38Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from a in Child
					select new { Count = a.GrandChildren.Count() },
					from a in db.Child
					select new { Count = a.GrandChildren1.Count() });

				var sql = ((TestDataConnection)db).LastQuery;

				Assert.That(sql, Is.Not.Contains("INNER JOIN"));

				Debug.WriteLine(sql);
			}
		}

		// https://github.com/linq2db/linq2db/issues/42
		//
		[Test, DataContextSource()]
		public void Issue42Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var t1 = db.Types2.First();

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);

				var t2 = db.Types2.First();

				Assert.That(t2.BoolValue, Is.EqualTo(t1.BoolValue));

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);
			}
		}
#if !NETSTANDARD
		// https://github.com/linq2db/linq2db/issues/60
		//
		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2000,
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			TestProvName.SqlAzure,
			ProviderName.SqlCe)]
		public void Issue60Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var sp       = db.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(db);

				var q =
					from t in dbSchema.Tables
					from c in t.Columns
					where c.ColumnType.StartsWith("tinyint") && c.MemberType.StartsWith("sbyte")
					select c;

				var column = q.FirstOrDefault();

				Assert.That(column, Is.Null);
			}
		}
#endif
		// https://github.com/linq2db/linq2db/issues/67
		//
		[Test, DataContextSource]
		public void Issue67Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t);
			}
		}

		[Test, DataContextSource()]
 		public void Issue75Test(string context)
 		{
 			using (var db = GetDataContext(context))
 			{
 				var result = db.Child.Select(c => new
				{
 					c.ChildID,
					c.ParentID,
					CountChildren  = db.Child.Count(c2 => c2.ParentID == c.ParentID),
					CountChildren2 = db.Child.Count(c2 => c2.ParentID == c.ParentID),
					HasChildren    = db.Child.Any  (c2 => c2.ParentID == c.ParentID),
					HasChildren2   = db.Child.Any  (c2 => c2.ParentID == c.ParentID),
					AllChildren    = db.Child.All  (c2 => c2.ParentID == c.ParentID),
					AllChildrenMin = db.Child.Where(c2 => c2.ParentID == c.ParentID).Min(c2 => c2.ChildID)
 				});

 				result =
 					from child in result
 					join parent in db.Parent on child.ParentID equals parent.ParentID
 					where parent.Value1 < 7
 					select child;

 				var expected = Child.Select(c => new
				{
 					c.ChildID,
					c.ParentID,
					CountChildren  = Child.Count(c2 => c2.ParentID == c.ParentID),
					CountChildren2 = Child.Count(c2 => c2.ParentID == c.ParentID),
					HasChildren    = Child.Any  (c2 => c2.ParentID == c.ParentID),
					HasChildren2   = Child.Any  (c2 => c2.ParentID == c.ParentID),
					AllChildren    = Child.All  (c2 => c2.ParentID == c.ParentID),
					AllChildrenMin = Child.Where(c2 => c2.ParentID == c.ParentID).Min(c2 => c2.ChildID)
 				});

 				expected =
 					from child in expected
 					join parent in Parent on child.ParentID equals parent.ParentID
 					where parent.Value1 < 7
 					select child;

				AreEqual(expected, result);
 			}
		}

		[Test, DataContextSource]
		public void Issue115Test(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}


		[Test, DataContextSource]
		public void Issue424Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Take(1)
					);
			}
		}

		[Test, DataContextSource]
		public void Issue424Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1)
					);
			}
		}

		// https://github.com/linq2db/linq2db/issues/498
		//
		[Test, DataContextSource()]
		public void Issue498Test(string context)
		{
			using (new WithoutJoinOptimization())
			using (var db = GetDataContext(context))
			{
				var q = from x in db.Child
					//join y in db.GrandChild on new { x.ParentID, x.ChildID } equals new { ParentID = (int)y.ParentID, ChildID = (int)y.ChildID }
					from y in x.GrandChildren1
					select x.ParentID;

				var r = from x in q
					group x by x
					into g
					select new { g.Key, Cghildren = g.Count() };

				var qq = from x in Child
					from y in x.GrandChildren
					select x.ParentID;

				var rr = from x in qq
					group x by x
					into g
					select new { g.Key, Cghildren = g.Count() };

				AreEqual(rr, r);

				var sql = r.ToString();
				Assert.Less(0, sql.IndexOf("INNER", 1), sql);
			}
		}
	}
}
